using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Server.Product.Models;

public sealed record ProductStatusChangeModel([property: JsonRequired] ProductStatus Status);
