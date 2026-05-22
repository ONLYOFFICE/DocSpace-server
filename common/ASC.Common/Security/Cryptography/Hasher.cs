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

namespace ASC.Security.Cryptography;

public static class Hasher
{
    private const HashAlg DefaultAlg = HashAlg.SHA256;

    public static byte[] Hash(string data, HashAlg hashAlg)
    {
        return ComputeHash(data, hashAlg);
    }

    public static byte[] Hash(string data)
    {
        return Hash(data, DefaultAlg);
    }

    public static byte[] Hash(byte[] data, HashAlg hashAlg)
    {
        return ComputeHash(data, hashAlg);
    }

    public static byte[] Hash(byte[] data)
    {
        return Hash(data, DefaultAlg);
    }

    public static string Base64Hash(string data, HashAlg hashAlg)
    {
        return ComputeHash64(data, hashAlg);
    }

    public static string Base64Hash(string data)
    {
        return Base64Hash(data, DefaultAlg);
    }

    public static string Base64Hash(byte[] data, HashAlg hashAlg)
    {
        return ComputeHash64(data, hashAlg);
    }

    public static string Base64Hash(byte[] data)
    {
        return Base64Hash(data, DefaultAlg);
    }

    public static bool EqualHash(byte[] dataToCompare, byte[] hash)
    {
        return EqualHash(dataToCompare, hash, DefaultAlg);
    }

    public static bool EqualHash(string dataToCompare, string hash, HashAlg hashAlg)
    {
        return EqualHash(S2B(dataToCompare), S642B(hash), hashAlg);
    }

    public static bool EqualHash(string dataToCompare, string hash)
    {
        return EqualHash(dataToCompare, hash, DefaultAlg);
    }

    public static bool EqualHash(byte[] dataToCompare, byte[] hash, HashAlg hashAlg)
    {
        return string.Equals(
            ComputeHash64(dataToCompare, hashAlg),
            B2S64(hash)
            );
    }

    private static HashAlgorithm GetAlg(HashAlg hashAlg)
    {
        return hashAlg switch
        {
            HashAlg.MD5 => MD5.Create(),
            HashAlg.SHA1 => SHA1.Create(),
            HashAlg.SHA256 => SHA256.Create(),
            HashAlg.SHA512 => SHA512.Create(),
            _ => SHA256.Create()
        };
    }

    private static byte[] S2B(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return Encoding.UTF8.GetBytes(str);
    }

    private static byte[] S642B(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return Convert.FromBase64String(str);
    }

    private static string B2S64(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Convert.ToBase64String(data);
    }

    private static byte[] ComputeHash(byte[] data, HashAlg hashAlg)
    {
        using var alg = GetAlg(hashAlg);

        return alg.ComputeHash(data);
    }

    private static byte[] ComputeHash(string data, HashAlg hashAlg)
    {
        return ComputeHash(S2B(data), hashAlg);
    }

    private static string ComputeHash64(byte[] data, HashAlg hashAlg)
    {
        return B2S64(ComputeHash(data, hashAlg));
    }

    private static string ComputeHash64(string data, HashAlg hashAlg)
    {
        return ComputeHash64(S2B(data), hashAlg);
    }
}