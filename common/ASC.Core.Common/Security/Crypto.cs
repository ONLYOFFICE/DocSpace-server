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

namespace ASC.Core;

public static class Crypto
{
    private static byte[] GetSK1(bool rewrite)
    {
        return GetSK(rewrite.GetType().Name.Length);
    }

    private static byte[] GetSK2(bool rewrite)
    {
        return GetSK(rewrite.GetType().Name.Length * 2);
    }

    private static byte[] GetSK(int seed)
    {
        var random = new AscRandom(seed);
        var randomKey = new byte[32];
        for (var i = 0; i < randomKey.Length; i++)
        {
            randomKey[i] = (byte)random.Next(byte.MaxValue);
        }

        return randomKey;
    }

    public static string GetV(string data, int keyno, bool reverse)
    {
        using var hasher = Aes.Create();
        hasher.Key = keyno == 1 ? GetSK1(false) : GetSK2(false);
        hasher.IV = new byte[hasher.BlockSize >> 3];

        if (reverse)
        {
            using var ms = new MemoryStream();
            using var ss = new CryptoStream(ms, hasher.CreateEncryptor(), CryptoStreamMode.Write);
            using var plainTextStream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            plainTextStream.CopyTo(ss);
            ss.FlushFinalBlock();
            hasher.Clear();

            return Convert.ToBase64String(ms.ToArray());
        }
        else
        {
            using var ms = new MemoryStream(Convert.FromBase64String(data));
            using var ss = new CryptoStream(ms, hasher.CreateDecryptor(), CryptoStreamMode.Read);
            using var plainTextStream = new MemoryStream();
            ss.CopyTo(plainTextStream);
            hasher.Clear();

            return Encoding.Unicode.GetString(plainTextStream.ToArray());
        }
    }

    internal static byte[] GetV(byte[] data, int keyno, bool reverse)
    {
        using var hasher = Aes.Create();
        hasher.Key = keyno == 1 ? GetSK1(false) : GetSK2(false);
        hasher.IV = new byte[hasher.BlockSize >> 3];

        if (reverse)
        {
            using var ms = new MemoryStream();
            using var ss = new CryptoStream(ms, hasher.CreateEncryptor(), CryptoStreamMode.Write);
            using var plainTextStream = new MemoryStream(data);
            plainTextStream.CopyTo(ss);
            ss.FlushFinalBlock();
            hasher.Clear();

            return ms.ToArray();
        }
        else
        {
            using var ms = new MemoryStream(data);
            using var ss = new CryptoStream(ms, hasher.CreateDecryptor(), CryptoStreamMode.Read);
            using var plainTextStream = new MemoryStream();
            ss.CopyTo(plainTextStream);
            hasher.Clear();

            return plainTextStream.ToArray();
        }
    }
}