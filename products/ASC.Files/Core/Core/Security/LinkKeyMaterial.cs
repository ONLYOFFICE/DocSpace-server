// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Core.Security;

/// <summary>
/// The key material the creator's browser sends for an external link of an item in a private room. The server stores
/// it as received and can open none of it.
/// </summary>
/// <param name="PublicKey">The public X25519 key of the link, base64 of 32 bytes.</param>
/// <param name="PrivateKeyEnc">The private key of the link, sealed under the link's secret.</param>
/// <param name="ProofHash">SHA-256 of the proof value a visitor shows, base64 of 32 bytes.</param>
public record LinkKeyMaterial(string PublicKey, string PrivateKeyEnc, string ProofHash)
{
    /// <summary>
    /// The wraps of the link's secret the request sends, one per user it was made for: the room owner and room managers.
    /// A request may send them without the three key fields, to add the wrap of a manager to an existing link.
    /// </summary>
    public IReadOnlyList<LinkSecretWrap> SecretWraps { get; init; } = [];

    /// <summary>Whether the request carries the key material itself, and not only wraps of the link's secret.</summary>
    public bool HasKeys => !string.IsNullOrEmpty(PublicKey) || !string.IsNullOrEmpty(PrivateKeyEnc) || !string.IsNullOrEmpty(ProofHash);

    /// <summary>The size of the public key and of the proof hash, in bytes.</summary>
    public const int KeyBytes = 32;

    /// <summary>The longest sealed private key accepted, in characters.</summary>
    public const int MaxPrivateKeyEncLength = 4096;

    /// <summary>
    /// Builds the key material of a request, or returns null when the request carries none of the three fields: such a
    /// request is a link without key material, which a private item refuses as a whole, not as a malformed field.
    /// </summary>
    public static LinkKeyMaterial From(string publicKey, string privateKeyEnc, string proofHash, IReadOnlyList<LinkSecretWrap> secretWraps = null)
    {
        if (string.IsNullOrEmpty(publicKey) && string.IsNullOrEmpty(privateKeyEnc) && string.IsNullOrEmpty(proofHash) && secretWraps is not { Count: > 0 })
        {
            return null;
        }

        return new LinkKeyMaterial(publicKey, privateKeyEnc, proofHash) { SecretWraps = secretWraps ?? [] };
    }

    /// <summary>
    /// Checks the sizes of the three values. A missing, empty, malformed or oversized value throws an
    /// <see cref="ArgumentException"/>, which the API answers with 400.
    /// </summary>
    public void Validate()
    {
        ValidateSecretWraps();

        if (!HasKeys)
        {
            return;
        }

        if (!HasLength(PublicKey, KeyBytes))
        {
            throw new ArgumentException(null, "linkPublicKey");
        }

        if (string.IsNullOrEmpty(PrivateKeyEnc) || PrivateKeyEnc.Length > MaxPrivateKeyEncLength)
        {
            throw new ArgumentException(null, "linkPrivateKeyEnc");
        }

        if (!HasLength(ProofHash, KeyBytes))
        {
            throw new ArgumentException(null, "linkProofHash");
        }
    }

    /// <summary>
    /// Whether a proof a visitor sends opens the link: the proof is the ordinary base64 of 32 bytes, and SHA-256 of those
    /// bytes, not of the base64 text, must equal the stored hash. The comparison takes the same time wherever the hashes
    /// differ. Anything that does not decode to exactly 32 bytes is a wrong proof.
    /// </summary>
    public static bool ProofMatches(string proof, string proofHash)
    {
        if (string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(proofHash))
        {
            return false;
        }

        Span<byte> proofBytes = stackalloc byte[KeyBytes + 1];
        Span<byte> storedHash = stackalloc byte[KeyBytes + 1];

        if (!Convert.TryFromBase64String(proof, proofBytes, out var proofLength) || proofLength != KeyBytes ||
            !Convert.TryFromBase64String(proofHash, storedHash, out var hashLength) || hashLength != KeyBytes)
        {
            return false;
        }

        Span<byte> hash = stackalloc byte[KeyBytes];
        SHA256.HashData(proofBytes[..KeyBytes], hash);

        return CryptographicOperations.FixedTimeEquals(hash, storedHash[..KeyBytes]);
    }

    /// <summary>
    /// A wrap names a user and the public key it was made for, carries a value of the size of a sealed key, and each
    /// user has one wrap at most.
    /// </summary>
    private void ValidateSecretWraps()
    {
        var users = new HashSet<Guid>();

        foreach (var wrap in SecretWraps)
        {
            if (wrap == null || wrap.UserId == Guid.Empty || wrap.PublicKeyId == Guid.Empty ||
                string.IsNullOrEmpty(wrap.Wrap) || wrap.Wrap.Length > MaxPrivateKeyEncLength || !users.Add(wrap.UserId))
            {
                throw new ArgumentException(null, "linkSecretWraps");
            }
        }
    }

    private static bool HasLength(string base64, int bytes)
    {
        // Only the canonical form, with padding and nothing else: the value is stored and compared as sent, and
        // Convert skips the white space that would otherwise pass here.
        if (string.IsNullOrEmpty(base64) || base64.Length != (bytes + 2) / 3 * 4)
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[bytes + 1];

        // Convert ignores the unused bits of the last character, so the decoded bytes are encoded again and compared.
        return Convert.TryFromBase64String(base64, buffer, out var written)
               && written == bytes
               && Convert.ToBase64String(buffer[..written]) == base64;
    }
}

/// <summary>The secret of a link wrapped by a manager's browser for one user who manages the room's links.</summary>
/// <param name="UserId">The room owner or a room manager the wrap was made for.</param>
/// <param name="PublicKeyId">The id of that user's public key the secret was wrapped for.</param>
/// <param name="Wrap">The wrapped secret, which only that user's private key opens.</param>
public record LinkSecretWrap(Guid UserId, Guid PublicKeyId, string Wrap);
