using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace TiemChung.Api;

// Configuration must come from user secrets/environment, never checked-in defaults.
public sealed class IdentityTokens
{
    private readonly byte[] signingKey;
    public IdentityTokens(IConfiguration configuration)
    {
        var configured = configuration["Identity:SigningKey"]
            ?? throw new InvalidOperationException("Identity signing key is required.");
        signingKey = Convert.FromBase64String(configured);
        if (signingKey.Length < 32) throw new InvalidOperationException("Identity signing key must have at least 256 bits.");
    }

    public static string RandomToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public string Issue(int accountId, Guid sessionId, DateTimeOffset now)
    {
        var header = WebEncoders.Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"u8.ToArray());
        var payload = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new {
            sub = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            sid = sessionId, iss = "KLCN.Identity", aud = "KLCN.Api",
            iat = now.ToUnixTimeSeconds(), exp = now.AddMinutes(5).ToUnixTimeSeconds()
        }));
        var body = header + "." + payload;
        return body + "." + WebEncoders.Base64UrlEncode(HMACSHA256.HashData(signingKey, Encoding.ASCII.GetBytes(body)));
    }

    public (int AccountId, Guid SessionId)? Validate(string token, DateTimeOffset now)
    {
        if (token.Length > 4096) return null;
        try {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var signature = HMACSHA256.HashData(signingKey, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));
            if (!CryptographicOperations.FixedTimeEquals(signature, WebEncoders.Base64UrlDecode(parts[2]))) return null;
            using var header = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[0]));
            if (header.RootElement.GetProperty("alg").GetString() != "HS256") return null;
            using var json = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[1]));
            var p = json.RootElement;
            if (p.GetProperty("iss").GetString() != "KLCN.Identity" || p.GetProperty("aud").GetString() != "KLCN.Api"
                || p.GetProperty("exp").GetInt64() <= now.ToUnixTimeSeconds()
                || p.GetProperty("iat").GetInt64() > now.ToUnixTimeSeconds()) return null;
            return (int.Parse(p.GetProperty("sub").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                p.GetProperty("sid").GetGuid());
        }
        catch (Exception e) when (e is FormatException or JsonException or KeyNotFoundException or InvalidOperationException or OverflowException) {
            return null;
        }
    }
}
