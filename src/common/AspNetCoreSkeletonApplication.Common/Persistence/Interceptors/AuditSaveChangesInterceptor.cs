using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;

/// <summary>
/// 저장 직전에 감사 필드를 채운다. 주체는 <see cref="AuditorContext.CurrentOrSystem"/>, 시각은 <see cref="TimeProvider"/>(UTC).
/// <list type="bullet">
/// <item><see cref="BaseAuditedEntity"/> — 생성 · 수정 필드. 수정 때 생성 필드는 바뀌지 않게 막는다.</item>
/// <item><see cref="BaseCreateAuditedEntity"/> — 생성 필드만. 추가 전용이라 수정 · 삭제하면 예외다.</item>
/// <item><see cref="ISoftDeletable"/> — 삭제를 삭제 주체 · 시각을 채운 수정으로 바꾼다.</item>
/// </list>
/// 상태가 없고 AsyncLocal 만 읽으므로 싱글턴이다 — DbContext 풀링과 함께 쓸 수 있다.
/// </summary>
internal sealed class AuditSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var auditor = AuditorContext.CurrentOrSystem;

        // 논리 삭제를 먼저 바꾼다 — Modified 가 되면 아래에서 수정 필드도 함께 채워진다.
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(entry => entry.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = auditor;
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = auditor;
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedBy = auditor;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = auditor;
                    entry.Entity.UpdatedAt = now;
                    entry.Property(entity => entity.CreatedBy).IsModified = false;
                    entry.Property(entity => entity.CreatedAt).IsModified = false;
                    break;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseCreateAuditedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = auditor;
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified or EntityState.Deleted:
                    throw new InvalidOperationException(
                        $"{entry.Metadata.ClrType.Name} 는 추가 전용이다 — 고치거나 지우지 않고 새 행을 추가한다");
            }
        }
    }
}
