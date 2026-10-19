using System.Buffers.Binary;
using System.Security.Cryptography;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>인증 앱 흉내 — 공유 키(Base32)로 지금의 6자리 코드를 계산한다(RFC 6238, HMAC-SHA1, 30초). Identity 의 인증 앱 공급자와 같은 계산이다.</summary>
public static class Totp
{
    public static string Code(string base32Key, DateTimeOffset? at = null)
    {
        var step = (at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(Base32Decode(base32Key), counter);
        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7FFFFFFF;
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>30초 뒤의 코드 — 서버가 앞뒤 2단계를 받아들이므로 지금도 맞고, 방금 쓴 지금 코드와는 다른 단계다(코드는 한 번만 쓰인다).</summary>
    public static string NextCode(string base32Key) => Code(base32Key, DateTimeOffset.UtcNow.AddSeconds(30));

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var buffer = 0;
        var bytes = new List<byte>();
        foreach (var character in value.ToUpperInvariant().Where(character => character is not (' ' or '=' or '-')))
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character, StringComparison.Ordinal);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
            }
        }

        return [.. bytes];
    }
}
