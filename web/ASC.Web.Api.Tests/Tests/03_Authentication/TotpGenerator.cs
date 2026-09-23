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

namespace ASC.Web.Api.Tests.Tests._03_Authentication;

/// <summary>
/// A minimal RFC 6238 TOTP generator over the Base32 secret (<c>tfaKey</c>) the portal hands back
/// once TFA App is enabled. Mirrors <c>src/utils/totp.ts</c> from the TypeScript suite — there is
/// no TOTP client library referenced anywhere in this repo, so this stays test-only code rather
/// than pulling in a new package for four call sites.
/// </summary>
internal static class TotpGenerator
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Generates the 6-digit code for the current 30-second time step.
    /// </summary>
    public static string GenerateCurrent(string base32Secret)
    {
        return GenerateAtCounter(base32Secret, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
    }

    public static string GenerateAtCounter(string base32Secret, long counter)
    {
        var key = Base32ToKey(base32Secret);

        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0xf;
        var code =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);

        return (code % 1_000_000).ToString("D6");
    }

    private static byte[] Base32ToKey(string base32Secret)
    {
        var cleaned = base32Secret.TrimEnd('=').ToUpperInvariant();

        var bits = 0;
        var value = 0;
        var bytes = new List<byte>();

        foreach (var c in cleaned)
        {
            value = (value << 5) | Base32Alphabet.IndexOf(c);
            bits += 5;

            if (bits >= 8)
            {
                bytes.Add((byte)((value >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }

        return [.. bytes];
    }
}
