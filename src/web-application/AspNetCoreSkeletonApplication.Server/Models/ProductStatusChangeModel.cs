using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Server.Models;

public sealed record ProductStatusChangeModel([property: JsonRequired] ProductStatus Status);
