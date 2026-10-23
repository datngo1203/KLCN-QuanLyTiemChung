using System.Security.Cryptography;
using System.Text;

namespace TiemChung.IdentityOperator;

public static class OperatorAuthentication
{
    public static void Validate(string osIdentity,string[] allowlist,string configuredSecret,string suppliedSecret)
    {
        if(!allowlist.Contains(osIdentity,StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Operator identity denied.");
        byte[] key;
        try { key=Convert.FromBase64String(configuredSecret); }
        catch(FormatException) { throw new InvalidOperationException("Operator secret configuration invalid."); }
        if(key.Length<32) throw new InvalidOperationException("Operator secret configuration invalid.");
        if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(configuredSecret)),
            SHA256.HashData(Encoding.UTF8.GetBytes(suppliedSecret)))) throw new InvalidOperationException("Operator authentication failed.");
    }
}
