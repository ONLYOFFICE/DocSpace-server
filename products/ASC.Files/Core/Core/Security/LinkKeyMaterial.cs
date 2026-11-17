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
    /// <summary>The size of the public key and of the proof hash, in bytes.</summary>
    public const int KeyBytes = 32;

    /// <summary>The longest sealed private key accepted, in characters.</summary>
    public const int MaxPrivateKeyEncLength = 4096;

    /// <summary>
    /// Builds the key material of a request, or returns null when the request carries none of the three fields: such a
    /// request is a link without key material, which a private item refuses as a whole, not as a malformed field.
    /// </summary>
    public static LinkKeyMaterial From(string publicKey, string privateKeyEnc, string proofHash)
    {
        if (string.IsNullOrEmpty(publicKey) && string.IsNullOrEmpty(privateKeyEnc) && string.IsNullOrEmpty(proofHash))
        {
            return null;
        }

        return new LinkKeyMaterial(publicKey, privateKeyEnc, proofHash);
    }

    /// <summary>
    /// Checks the sizes of the three values. A missing, empty, malformed or oversized value throws an
    /// <see cref="ArgumentException"/>, which the API answers with 400.
    /// </summary>
    public void Validate()
    {
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
