using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Mail.Persistence.Configurations;

internal sealed class MailOutboxEntityConfiguration : IEntityTypeConfiguration<MailOutboxEntity>
{
    public const int LastErrorMaxLength = 1000;

    public void Configure(EntityTypeBuilder<MailOutboxEntity> builder)
    {
        builder.ToTable("mail_outbox");
        builder.Property(mail => mail.Recipient).HasMaxLength(256);
        builder.Property(mail => mail.Subject).HasMaxLength(200);
        builder.Property(mail => mail.LastError).HasMaxLength(LastErrorMaxLength);
        // 발송 대상(아직 보내지도 포기하지도 않은 행)만 담는 인덱스 — 보낸 행이 쌓여도 잡는 쿼리가 느려지지 않는다.
        builder.HasIndex(mail => mail.NextAttemptAt).HasFilter("[SentAt] IS NULL AND [FailedAt] IS NULL");
    }
}
