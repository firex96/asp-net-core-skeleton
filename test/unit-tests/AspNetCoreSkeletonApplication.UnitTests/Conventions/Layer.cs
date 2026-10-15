namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>네임스페이스로 판정하는 계층. 판정 규칙은 <see cref="ConventionScan.LayerOf"/>.</summary>
public enum Layer
{
    Controller,
    MappingService,
    BizService,
    EntityService,
    Domain,
    Mapper,
    Client,
    Model,
    Dto,
    Entity,
}
