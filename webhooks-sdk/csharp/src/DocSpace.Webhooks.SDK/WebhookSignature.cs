using System.Security.Cryptography;
using System.Text;

namespace DocSpace.Webhooks.SDK;

/// <summary>
/// Verifies the <c>x-docspace-signature-256</c> header DocSpace sends with
/// every delivery.
/// </summary>
public static class WebhookSignature
{
    /// <summary>The header DocSpace signs each delivery with.</summary>
    public const string HeaderName = "x-docspace-signature-256";

    private const string Prefix = "sha256=";

    /// <summary>
    /// The signature DocSpace would send for this body, in the server's format:
    /// <c>sha256=</c> followed by UPPERCASE hex.
    /// </summary>
    /// <remarks>Mirrors <c>WebhookSender.GetSecretHash</c>.</remarks>
    public static string Compute(ReadOnlySpan<byte> body, string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        Span<byte> hash = stackalloc byte[32];
        hmac.TryComputeHash(body, hash, out _);

        return Prefix + Convert.ToHexString(hash);
    }

    /// <inheritdoc cref="Compute(ReadOnlySpan{byte}, string)"/>
    public static string Compute(string body, string secret)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Compute(Encoding.UTF8.GetBytes(body), secret);
    }

    /// <summary>
    /// Checks the header against the body.
    /// </summary>
    /// <remarks>
    /// Three things this gets right that a hand-rolled check usually does not:
    /// <list type="number">
    /// <item>It hashes the <b>raw</b> body. Verify before deserializing: a body
    /// that has been through a round trip is a different byte sequence and will
    /// never match. In ASP.NET Core that means enabling buffering and reading
    /// the stream, not the model-bound object.</item>
    /// <item>It compares case-insensitively. DocSpace emits uppercase hex where
    /// GitHub emits lowercase, so receiver code ported from GitHub's docs fails
    /// against DocSpace.</item>
    /// <item>It compares in constant time.</item>
    /// </list>
    /// Returns <c>false</c> rather than throwing for a missing or malformed
    /// header, so every rejection can be handled the same way.
    /// </remarks>
    public static bool Verify(ReadOnlySpan<byte> body, string secret, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        // Lowercase both sides: only the hex casing differs between senders,
        // and ASCII lowering is not data-dependent in a way that leaks.
        var expected = Encoding.UTF8.GetBytes(Compute(body, secret).ToLowerInvariant());
        var received = Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant());

        // FixedTimeEquals requires equal lengths, and the length is not secret:
        // a SHA-256 digest is always 64 hex characters.
        return expected.Length == received.Length
               && CryptographicOperations.FixedTimeEquals(expected, received);
    }

    /// <inheritdoc cref="Verify(ReadOnlySpan{byte}, string, string?)"/>
    public static bool Verify(string body, string secret, string? signature)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Verify(Encoding.UTF8.GetBytes(body), secret, signature);
    }
}
