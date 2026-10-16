using AspNetCoreSkeletonApplication.Batch.JobTrigger.Models;
using AspNetCoreSkeletonApplication.Batch.Security;
using AspNetCoreSkeletonApplication.Batch.JobTrigger.Services.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCoreSkeletonApplication.Batch.JobTrigger.Controllers;

/// <summary>
/// 수동 실행 웹훅. 인가가 먼저다 — API 키(401)와 키에 허용된 Job(403)을 통과한 요청만 파라미터를 본다(400).
/// 실행은 비동기라 202 와 추적 ID 를 돌려준다.
/// </summary>
[ApiController]
[Route("api/jobs")]
public class JobController(JobTriggerMappingService mappingService) : ControllerBase
{
    [HttpPost("{jobName}/trigger")]
    [Authorize(Policy = BatchPolicies.JobTrigger)]
    public async Task<AcceptedResult> Trigger(
        string jobName,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JobTriggerRequestModel? request,
        CancellationToken cancellationToken) =>
        Accepted(await mappingService.TriggerAsync(jobName, request, cancellationToken));
}
