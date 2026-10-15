using System.ComponentModel.DataAnnotations;
using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>에러 응답 형식을 확인하려고 테스트에서만 붙이는 컨트롤러. 요청한 종류의 실패를 그대로 일으킨다.</summary>
[ApiController]
[Route("error-probe")]
[AllowAnonymous]
public class ErrorProbeController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() => throw new ItemNotFoundException("상품 42 가 없다");

    [HttpGet("bad-request")]
    public IActionResult ThrowBadRequest() => throw new ClientBadRequestException("수량은 1 이상이다");

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new ConflictException("판매 종료된 상품은 다시 팔 수 없다");

    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() => throw new InvalidOperationException("내부 연결 문자열 노출 금지");

    [HttpGet("status-not-found")]
    public IActionResult ReturnNotFound() => NotFound();

    [HttpPost("validate")]
    public IActionResult Validate(ErrorProbeModel model) => Ok(model);

    public sealed record ErrorProbeModel([Required] string? Name);
}
