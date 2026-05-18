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

[Singleton]
public class InstanceCrypto(MachinePseudoKeys machinePseudoKeys)
{
    private readonly byte[] _eKey = machinePseudoKeys.GetMachineConstant(32);

    public string Encrypt(string data)
    {
        return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(data)));
    }

    private byte[] Encrypt(byte[] data)
    {
        using var hasher = Aes.Create();
        hasher.Key = _eKey;
        hasher.IV = new byte[hasher.BlockSize >> 3];

        using var ms = new MemoryStream();
        using var ss = new CryptoStream(ms, hasher.CreateEncryptor(), CryptoStreamMode.Write);
        using var plainTextStream = new MemoryStream(data);

        plainTextStream.CopyTo(ss);
        ss.FlushFinalBlock();
        hasher.Clear();

        return ms.ToArray();
    }

    public async Task<string> EncryptAsync(string data)
    {
        return Convert.ToBase64String(await EncryptAsync(Encoding.UTF8.GetBytes(data)));
    }

    public async Task<byte[]> EncryptAsync(byte[] data)
    {
        using var hasher = Aes.Create();
        hasher.Key = _eKey;
        hasher.IV = new byte[hasher.BlockSize >> 3];

        using var ms = new MemoryStream();
        await using (var ss = new CryptoStream(ms, hasher.CreateEncryptor(), CryptoStreamMode.Write))
        {
            using var plainTextStream = new MemoryStream(data);

            await plainTextStream.CopyToAsync(ss);
            await ss.FlushFinalBlockAsync();
        }

        hasher.Clear();

        return ms.ToArray();
    }

    public string Decrypt(string data) => Decrypt(Convert.FromBase64String(data));

    public string Decrypt(byte[] data, Encoding encoding = null)
    {
        using var hasher = Aes.Create();
        hasher.Key = _eKey;
        hasher.IV = new byte[hasher.BlockSize >> 3];

        using var msDecrypt = new MemoryStream(data);
        using var csDecrypt = new CryptoStream(msDecrypt, hasher.CreateDecryptor(), CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt, encoding);

        // Read the decrypted bytes from the decrypting stream
        // and place them in a string.
        return srDecrypt.ReadToEnd();
    }

    public Task<string> DecryptAsync(string data) => DecryptAsync(Convert.FromBase64String(data));

    public async Task<string> DecryptAsync(byte[] data, Encoding encoding = null)
    {
        using var hasher = Aes.Create();
        hasher.Key = _eKey;
        hasher.IV = new byte[hasher.BlockSize >> 3];

        using var msDecrypt = new MemoryStream(data);
        await using var csDecrypt = new CryptoStream(msDecrypt, hasher.CreateDecryptor(), CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt, encoding);

        // Read the decrypted bytes from the decrypting stream
        // and place them in a string.
        return await srDecrypt.ReadToEndAsync();
    }
}