using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 인증 앱 코드는 한 번만 — 통과한 시간 단계(30초)를 사용자 토큰에 두고, 그 단계와 그보다 이른 단계의 코드는 받지 않는다(RFC 6238 5.2, 형제 스켈레톤과 같은 규칙).
/// 받아들이는 범위는 Identity 기본값과 같은 앞뒤 2단계다. Identity 의 TOTP 계산은 internal 이라 RFC 6238(HMAC-SHA1, 6자리)대로 계산한다.
/// 같은 코드가 동시에 오면 사용자의 <c>ConcurrencyStamp</c> 로 하나만 저장되고, 저장하지 못한 쪽은 틀린 코드다.
/// </summary>
internal sealed class SingleUseAuthenticatorTokenProvider(TimeProvider timeProvider) : AuthenticatorTokenProvider<ApplicationUser>
{
    private const string LastStepTokenName = "LastAuthenticatorStep";
    private const int AllowedSteps = 2;

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var key = await manager.GetAuthenticatorKeyAsync(user);
        if (key is null || !int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var code))
        {
            return false;
        }

        var keyBytes = Base32Decode(key);
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds() / 30;
        long? step = null;
        for (var candidate = now - AllowedSteps; candidate <= now + AllowedSteps && step is null; candidate++)
        {
            if (Code(keyBytes, candidate) == code)
            {
                step = candidate;
            }
        }

        if (step is null)
        {
            return false;
        }

        var last = await manager.GetAuthenticationTokenAsync(user, TwoFactorEndpoints.InternalLoginProvider, LastStepTokenName);
        if (last is not null && long.Parse(last, CultureInfo.InvariantCulture) >= step)
        {
            return false;
        }

        var saved = await manager.SetAuthenticationTokenAsync(
            user, TwoFactorEndpoints.InternalLoginProvider, LastStepTokenName, step.Value.ToString(CultureInfo.InvariantCulture));
        return saved.Succeeded;
    }

    private static int Code(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0F;
        return (BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7FFFFFFF) % 1_000_000;
    }

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in value.ToUpperInvariant().Where(character => character != '='))
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

/// <summary>
/// 메일 코드는 한 번만 — 코드 계산 · 유효 시간은 Identity 에 맡기고(보안 스탬프로 만든다, 로그인해도 스탬프는 그대로다), 코드를 만드는 값(modifier)에
/// 마지막으로 받아들인 코드를 넣는다. 받아들이면 그 값이 바뀌어, 쓴 코드는 더는 맞지 않고 곧바로 다시 받은 코드도 새 코드다.
/// 동시에 온 같은 코드는 인증 앱과 같이 하나만 저장된다.
/// </summary>
internal sealed class SingleUseEmailTokenProvider : EmailTokenProvider<ApplicationUser>
{
    private const string LastCodeTokenName = "LastEmailCode";

    public override async Task<string> GetUserModifierAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        $"{await base.GetUserModifierAsync(purpose, manager, user)}:"
        + await manager.GetAuthenticationTokenAsync(user, TwoFactorEndpoints.InternalLoginProvider, LastCodeTokenName);

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        await base.ValidateAsync(purpose, token, manager, user)
        && (await manager.SetAuthenticationTokenAsync(user, TwoFactorEndpoints.InternalLoginProvider, LastCodeTokenName, token)).Succeeded;
}
