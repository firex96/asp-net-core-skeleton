namespace AspNetCoreSkeletonApplication.Batch.Models;

/// <summary>202 응답. <see cref="TraceId"/> 로 Job 로그와 Quartz 실행 이력을 찾는다.</summary>
public sealed record JobTriggerModel(string JobName, string TraceId);
