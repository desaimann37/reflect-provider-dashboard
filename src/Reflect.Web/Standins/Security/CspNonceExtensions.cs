using System.Security.Cryptography;

namespace Reflect.Web.Standins.Security;

/// <summary>STAND-IN for the real app's per-request CSP nonce helper.</summary>
public static class CspNonceExtensions
{
    private const string ItemKey = "Reflect.CspNonce";

    public static string GetCspNonce(this HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existing) && existing is string nonce)
        {
            return nonce;
        }
        nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[ItemKey] = nonce;
        return nonce;
    }
}
