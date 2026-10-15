using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;
using AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Persistence;

/// <summary>저장 직전에 감사 필드가 채워지고, 수정 때 생성 필드는 바뀌지 않는다 — 실제 SQL Server 에 저장해 확인한다.</summary>
[Collection(SqlServerCollection.Name)]
public class AuditSaveChangesInterceptorTests(SqlServerFixture database) : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedTime = new(2026, 10, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedTime = CreatedTime.AddHours(1);

    private readonly SettableTimeProvider _time = new(CreatedTime);

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Add_WithoutAuditor_RecordsSystem()
    {
        var id = await AddAsync(auditor: null);

        var saved = await FindAsync(id);
        Assert.Equal(AuditorIds.System, saved.CreatedBy);
        Assert.Equal(AuditorIds.System, saved.UpdatedBy);
        Assert.Equal(CreatedTime, saved.CreatedAt);
        Assert.Equal(CreatedTime, saved.UpdatedAt);
    }

    [Fact]
    public async Task Update_ChangesOnlyUpdatedFields()
    {
        var id = await AddAsync(auditor: 7);
        _time.Now = UpdatedTime;

        await using (var context = CreateContext())
        using (AuditorContext.Begin(9))
        {
            var entity = await context.Probes.SingleAsync(probe => probe.Id == id, TestContext.Current.CancellationToken);
            entity.Name = "changed";
            entity.CreatedBy = 999; // 생성 필드를 건드려도 저장되지 않아야 한다
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var saved = await FindAsync(id);
        Assert.Equal(7, saved.CreatedBy);
        Assert.Equal(CreatedTime, saved.CreatedAt);
        Assert.Equal(9, saved.UpdatedBy);
        Assert.Equal(UpdatedTime, saved.UpdatedAt);
    }

    [Fact]
    public async Task AppendOnly_RecordsCreatorAndRefusesChanges()
    {
        long id;
        await using (var context = CreateContext())
        using (AuditorContext.Begin(5))
        {
            var entry = new AppendOnlyProbeEntity { Note = "first" };
            context.AppendOnlyProbes.Add(entry);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            id = entry.Id;
            Assert.Equal(5, entry.CreatedBy);
            Assert.Equal(CreatedTime, entry.CreatedAt);
        }

        await using (var context = CreateContext())
        {
            var entry = await context.AppendOnlyProbes.SingleAsync(probe => probe.Id == id, TestContext.Current.CancellationToken);
            entry.Note = "changed";
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        await using (var context = CreateContext())
        {
            context.AppendOnlyProbes.Remove(await context.AppendOnlyProbes.SingleAsync(probe => probe.Id == id, TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    private async Task<long> AddAsync(long? auditor)
    {
        await using var context = CreateContext();
        using var scope = AuditorContext.Begin(auditor);
        var entity = new AuditedProbeEntity { Name = "probe" };
        context.Probes.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity.Id;
    }

    private async Task<AuditedProbeEntity> FindAsync(long id)
    {
        await using var context = CreateContext();
        return await context.Probes.AsNoTracking().SingleAsync(probe => probe.Id == id, TestContext.Current.CancellationToken);
    }

    private ProbeDbContext CreateContext() => new(new DbContextOptionsBuilder<ProbeDbContext>()
        .UseSqlServer(database.ConnectionStringFor("audit_probe"))
        .AddInterceptors(new AuditSaveChangesInterceptor(_time))
        .Options);

    internal sealed class AuditedProbeEntity : BaseAuditedEntity
    {
        public required string Name { get; set; }
    }

    internal sealed class AppendOnlyProbeEntity : BaseCreateAuditedEntity
    {
        public required string Note { get; set; }
    }

    internal sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedProbeEntity> Probes => Set<AuditedProbeEntity>();

        public DbSet<AppendOnlyProbeEntity> AppendOnlyProbes => Set<AppendOnlyProbeEntity>();
    }

    private sealed class SettableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
