using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    // 아래는 프레임워크가 만드는 상태(405 · 415 · 413 · 목록 밖 상태)와 검증 문구를 확인한다

    /// <summary>/api 아래 GET 전용 경로 — 다른 메서드는 405 다. Server 의 SPA 폴백이 이 405 를 가리지 않는지도 본다.</summary>
    [HttpGet("/api/error-probe/get-only")]
    public IActionResult GetOnly() => Ok();

    [HttpPost("json-only")]
    [Consumes("application/json")]
    public IActionResult JsonOnly(ErrorProbeModel model) => Ok(model);

    [HttpGet("bad-http-request")]
    public IActionResult ThrowBadHttpRequest() =>
        throw new BadHttpRequestException("Request body too large. The max request body size is 30000000 bytes.", StatusCodes.Status413PayloadTooLarge);

    [HttpGet("teapot")]
    public IActionResult ReturnTeapot() => StatusCode(StatusCodes.Status418ImATeapot);

    [HttpGet("unavailable")]
    public IActionResult ReturnUnavailable() => StatusCode(StatusCodes.Status503ServiceUnavailable);

    [HttpPost("typed")]
    public IActionResult Typed(TypedProbeModel model) => Ok(model);

    public sealed record ErrorProbeModel([Required] string? Name);

    public sealed record TypedProbeModel(
        [property: JsonRequired] decimal Price,
        [Required, StringLength(5)] string? Name,
        [Range(1, 10)] int Count);
}
