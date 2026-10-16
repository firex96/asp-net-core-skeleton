using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AspNetCoreSkeletonApplication.Server.Product.Models;

/// <summary>
/// 필수 값은 빠지면 400 이어야 한다 — 참조 타입은 <c>[Required]</c>, 값 타입은 빠져도 0 으로 채워지므로 <c>[JsonRequired]</c>.
/// 검증 특성은 record 생성자 매개변수에(MVC 규칙), <c>JsonRequired</c> 는 속성에(<c>property:</c>) 붙인다.
/// </summary>
public sealed record ProductCreateModel(
    [Required, StringLength(200)] string Name,
    [property: JsonRequired] [Range(typeof(decimal), "0", "1000000000")] decimal Price);
