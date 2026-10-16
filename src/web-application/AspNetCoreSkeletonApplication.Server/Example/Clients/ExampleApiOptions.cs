using AspNetCoreSkeletonApplication.Common.Web;

namespace AspNetCoreSkeletonApplication.Server.Example.Clients;

/// <summary>구성 경로 <c>Clients:ExampleApi</c>. 환경마다 다른 주소는 환경변수 <c>Clients__ExampleApi__BaseAddress</c> 로 준다.</summary>
public sealed class ExampleApiOptions : ApiClientOptions;
