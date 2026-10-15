using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Persistence.Conventions;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Persistence;

/// <summary>열거형 컬럼에는 C# 이름이 아니라 외부 코드가 저장되고, 그 코드로 다시 읽힌다 — 실제 SQL Server 로 확인한다.</summary>
[Collection(InfrastructureCollection.Name)]
public class StringEnumCodeTests(InfrastructureFixture database) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Column_StoresCode_AndReadsBack()
    {
        await using (var context = CreateContext())
        {
            context.Probes.Add(new EnumProbeEntity { Color = ProbeColor.DarkRed, OptionalColor = null });
            context.Probes.Add(new EnumProbeEntity { Color = ProbeColor.LightBlue, OptionalColor = ProbeColor.DarkRed });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var stored = await context.Database
                .SqlQuery<string>($"SELECT [Color] AS [Value] FROM [enum_probe] ORDER BY [Id]")
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(["dark-red", "light-blue"], stored);

            var read = await context.Probes.OrderBy(probe => probe.Id).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([ProbeColor.DarkRed, ProbeColor.LightBlue], read.Select(probe => probe.Color));
            Assert.Equal([null, ProbeColor.DarkRed], read.Select(probe => probe.OptionalColor));
        }
    }

    private ProbeDbContext CreateContext() => new(new DbContextOptionsBuilder<ProbeDbContext>()
        .UseSqlServer(database.ConnectionStringFor("enum_probe"))
        .Options);

    public enum ProbeColor
    {
        [JsonStringEnumMemberName("dark-red")]
        DarkRed,

        [JsonStringEnumMemberName("light-blue")]
        LightBlue,
    }

    internal sealed class EnumProbeEntity
    {
        public long Id { get; set; }

        public ProbeColor Color { get; set; }

        public ProbeColor? OptionalColor { get; set; }
    }

    internal sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
    {
        public DbSet<EnumProbeEntity> Probes => Set<EnumProbeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EnumProbeEntity>().ToTable("enum_probe");
            modelBuilder.UseStringEnumCodes();
        }
    }
}
