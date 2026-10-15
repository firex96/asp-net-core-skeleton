using AspNetCoreSkeletonApplication.Common.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Conventions;

/// <summary>열거형을 외부 코드 문자열로 저장한다(<see cref="EnumCodes"/>).</summary>
internal sealed class StringEnumValueConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => EnumCodes.ToCode(value),
    code => EnumCodes.Parse<TEnum>(code))
    where TEnum : struct, Enum;
