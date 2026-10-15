using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>열거형 외부 코드가 쿼리 · JSON 양쪽에 쓰이는지 확인하려고 테스트에서만 붙이는 컨트롤러.</summary>
[ApiController]
[Route("binding-probe")]
public class BindingProbeController : ControllerBase
{
    [HttpGet("color")]
    public ColorProbeModel Echo([FromQuery] ProbeColor color) => new(color);

    [HttpPost("color")]
    public ColorProbeModel EchoBody(ColorProbeModel model) => model;

    public sealed record ColorProbeModel(ProbeColor Color);

    public enum ProbeColor
    {
        [JsonStringEnumMemberName("dark-red")]
        DarkRed,

        [JsonStringEnumMemberName("light-blue")]
        LightBlue,
    }
}
