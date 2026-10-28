using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NovaBank.Http;

/// <summary>
/// Signs the raw request body with a secret both banks share, so the receiving
/// bank can tell a request it trusts from anything else that can reach the port.
/// The secret never travels on the wire - only the hash of it.
/// </summary>
internal static class RequestSignature
{
    // A signed request older (or newer) than this is refused. It bounds how long
    // a captured request stays useful to someone replaying it.
    private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);

    public static string Sign(string secret, string timestamp, string body)
    {
        byte[] key = Encoding.UTF8.GetBytes(secret);

        // The timestamp is inside the signed payload, so it cannot be changed
        // without breaking the signature.
        byte[] payload = Encoding.UTF8.GetBytes($"{timestamp}\n{body}");

        using var hmac = new HMACSHA256(key);

        return Convert.ToHexString(hmac.ComputeHash(payload)).ToLowerInvariant();
    }

    public static bool Verify(string secret, string timestamp, string body, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        byte[] expected = Encoding.UTF8.GetBytes(Sign(secret, timestamp, body));
        byte[] received = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());

        // Fixed-time compare: a plain == leaks how many characters matched.
        return CryptographicOperations.FixedTimeEquals(expected, received);
    }

    public static bool IsFresh(string timestamp)
    {
        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds))
        {
            return false;
        }

        DateTimeOffset sent = DateTimeOffset.FromUnixTimeSeconds(seconds);

        return (DateTimeOffset.UtcNow - sent).Duration() <= MaxSkew;
    }
}
