using System.Reflection;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>상태로 코드를 고른다 — 프레임워크가 정하는 상태마다 코드가 있고, 목록 밖 상태는 400 · 500 코드로 낮춘다.</summary>
public class ErrorCodeTests
{
    public static TheoryData<int, string> MappedStatuses => new()
    {
        { 400, "BAD_REQUEST_ERROR" }, { 401, "UNAUTHORIZED_ERROR" }, { 403, "FORBIDDEN_ERROR" }, { 404, "NOT_FOUND_ERROR" },
        { 405, "METHOD_NOT_ALLOWED_ERROR" }, { 406, "NOT_ACCEPTABLE_ERROR" }, { 409, "CONFLICT_ERROR" }, { 413, "PAYLOAD_TOO_LARGE_ERROR" },
        { 415, "UNSUPPORTED_MEDIA_TYPE_ERROR" }, { 423, "ACCOUNT_LOCKED_ERROR" }, { 500, "INTERNAL_SERVER_ERROR" },
        { 502, "EXTERNAL_API_ERROR" }, { 503, "SERVICE_UNAVAILABLE_ERROR" },
    };

    [Theory]
    [MemberData(nameof(MappedStatuses))]
    public void FromStatus_MappedStatus_ReturnsItsCode(int status, string code)
    {
        var errorCode = ErrorCode.FromStatus(status);

        Assert.Equal(code, errorCode.Code);
        Assert.Equal(status, errorCode.Status);
    }

    [Theory]
    [InlineData(418, "BAD_REQUEST_ERROR")]
    [InlineData(504, "INTERNAL_SERVER_ERROR")]
    public void FromStatus_UnmappedStatus_FallsBack(int status, string code) =>
        Assert.Equal(code, ErrorCode.FromStatus(status).Code);

    [Fact]
    public void CodesAndDivisionCodes_AreUnique()
    {
        var codes = typeof(ErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(ErrorCode))
            .Select(field => (ErrorCode)field.GetValue(null)!)
            .ToList();

        Assert.Equal(codes.Count, codes.Select(errorCode => errorCode.Code).Distinct().Count());
        Assert.Equal(codes.Count, codes.Select(errorCode => errorCode.DivisionCode).Distinct().Count());
    }
}
