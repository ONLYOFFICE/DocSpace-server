// Signature check of a DocSpace delivery. Hand-written in swift/shim/ and copied
// into generated/Sources/DocSpaceWebhooksSDK/ after every regeneration.

import Foundation
import Crypto

/// The `x-docspace-signature-256` header: `sha256=` + the HMAC-SHA256 of the raw body, keyed with the secret of the
/// subscription, as hex.
public enum WebhookSignature {
    /// The header the signature arrives in.
    public static let headerName = "x-docspace-signature-256"

    /// The header DocSpace echoes `event.id` in; it is not signed.
    public static let eventIdHeaderName = "x-docspace-event-id"

    /// `sha256=` + UPPERCASE hex, mirroring `WebhookSender.GetSecretHash`.
    public static func compute(body: Data, secret: String) -> String {
        let mac = HMAC<SHA256>.authenticationCode(for: body, using: SymmetricKey(data: Data(secret.utf8)))
        return "sha256=" + mac.map { String(format: "%02X", $0) }.joined()
    }

    /// Checks the header against the RAW body.
    ///
    /// Comparing the decoded bytes rather than the strings gets both awkward parts for free:
    /// `isValidAuthenticationCode` is constant-time, and hex parsing is case-insensitive -- which matters because
    /// DocSpace emits uppercase hex where GitHub emits lowercase. A missing or malformed header is `false`.
    public static func verify(body: Data, secret: String, signature: String?) -> Bool {
        guard let signature else { return false }

        let trimmed = signature.trimmingCharacters(in: .whitespacesAndNewlines)
        let prefix = "sha256="
        guard trimmed.lowercased().hasPrefix(prefix),
              let expected = hexToData(trimmed.dropFirst(prefix.count)),
              expected.count == SHA256.byteCount
        else { return false }

        return HMAC<SHA256>.isValidAuthenticationCode(
            expected,
            authenticating: body,
            using: SymmetricKey(data: Data(secret.utf8))
        )
    }

    // Strict: two hex digits per byte, nothing else (UInt8(_:radix:) would also take a sign).
    private static func hexToData(_ hex: Substring) -> Data? {
        let digits = Array(hex.utf8)
        guard !digits.isEmpty, digits.count % 2 == 0 else { return nil }

        var out = Data(capacity: digits.count / 2)
        var index = 0
        while index < digits.count {
            guard let high = nibble(digits[index]), let low = nibble(digits[index + 1]) else { return nil }
            out.append(high << 4 | low)
            index += 2
        }
        return out
    }

    private static func nibble(_ c: UInt8) -> UInt8? {
        switch c {
        case UInt8(ascii: "0")...UInt8(ascii: "9"): return c - UInt8(ascii: "0")
        case UInt8(ascii: "a")...UInt8(ascii: "f"): return c - UInt8(ascii: "a") + 10
        case UInt8(ascii: "A")...UInt8(ascii: "F"): return c - UInt8(ascii: "A") + 10
        default: return nil
        }
    }
}
