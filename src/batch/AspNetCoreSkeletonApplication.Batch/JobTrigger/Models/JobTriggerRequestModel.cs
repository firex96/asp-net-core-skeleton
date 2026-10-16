namespace AspNetCoreSkeletonApplication.Batch.JobTrigger.Models;

/// <summary>본문은 없어도 된다. 파라미터는 그 Job 의 <c>AllowedParameters</c> 에 있는 이름만 받는다.</summary>
public sealed record JobTriggerRequestModel(IReadOnlyDictionary<string, string>? Parameters);
