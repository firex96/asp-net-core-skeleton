using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Mail.Persistence.Mappers;

[Mapper]
internal static partial class MailOutboxEntityMapper
{
    /// <summary>발송 대상으로 잡은 행 — 본문이 남아 있다.</summary>
    [MapperIgnoreSource(nameof(MailOutboxEntity.NextAttemptAt))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.SentAt))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.FailedAt))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.LastError))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(MailOutboxEntity.UpdatedAt))]
    public static partial MailOutboxDto ToDto(this MailOutboxEntity entity);

    /// <summary>식별자는 DB 가, 감사 필드는 인터셉터가 채운다. 시도 · 상태는 EntityService 가 정한다.</summary>
    [MapProperty(nameof(OutgoingMailDto.To), nameof(MailOutboxEntity.Recipient))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.Id))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.Attempts))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.NextAttemptAt))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.SentAt))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.FailedAt))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.LastError))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MailOutboxEntity.UpdatedAt))]
    public static partial MailOutboxEntity ToEntity(this OutgoingMailDto dto);
}
