using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;

/// <summary>
/// 저장 직전에 <see cref="BaseAuditedEntity"/> 의 감사 필드를 채운다. 주체는 <see cref="AuditorContext.CurrentOrSystem"/>,
/// 시각은 <see cref="TimeProvider"/>(UTC). 수정 때는 생성 필드가 바뀌지 않게 막는다.
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
    }
}
