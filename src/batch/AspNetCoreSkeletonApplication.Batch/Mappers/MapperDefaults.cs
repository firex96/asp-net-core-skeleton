using Riok.Mapperly.Abstractions;

// Mapperly 엄격 모드 — 이 어셈블리의 모든 [Mapper] 기본값. 대상 누락(RMG012)은 .editorconfig 에서 error 다.
// 속성 단위 null 불일치는 조용히 건너뛰지 않고 예외로, ToString 같은 암시적 문자열 변환은 끈다.
[assembly: MapperDefaults(
    RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnPropertyMappingNullMismatch = true,
    EnabledConversions = MappingConversionType.All & ~MappingConversionType.ToStringMethod)]
