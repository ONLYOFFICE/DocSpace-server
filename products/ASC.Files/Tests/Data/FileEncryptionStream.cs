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

namespace ASC.Files.Tests.Data;

public static class FileEncryptionStream
{
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const int ChunkSize = 64 * 1024; // 64 KB

    public static async Task EncryptFileAsync(
        Stream input, 
        Stream output, 
        byte[] aesKey,
        CancellationToken ct = default)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(aesKey, salt);
        
        await output.WriteAsync(salt, ct);

        var buffer = new byte[ChunkSize];
        int bytesRead;

        while ((bytesRead = await input.ReadAsync(buffer, ct)) > 0)
        {
            var chunk = buffer.AsSpan(0, bytesRead);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[bytesRead];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, chunk, ciphertext, tag);

            var chunkHeader = BitConverter.GetBytes(bytesRead);
            await output.WriteAsync(chunkHeader, ct);
            await output.WriteAsync(nonce, ct);
            await output.WriteAsync(tag, ct);
            await output.WriteAsync(ciphertext, ct);
        }
    }

    public static async Task DecryptFileAsync(
        Stream input, 
        Stream output, 
        byte[] aesKey,
        CancellationToken ct = default)
    {
        var salt = new byte[SaltSize];
        await input.ReadExactlyAsync(salt, ct);
        
        var key = DeriveKey(aesKey, salt);
        
        var headerBuffer = new byte[4];
        var nonceBuffer = new byte[NonceSize];
        var tagBuffer = new byte[TagSize];

        while (input.Position < input.Length)
        {
            await input.ReadExactlyAsync(headerBuffer, ct);
            var chunkSize = BitConverter.ToInt32(headerBuffer);

            await input.ReadExactlyAsync(nonceBuffer, ct);
            await input.ReadExactlyAsync(tagBuffer, ct);

            var ciphertext = new byte[chunkSize];
            await input.ReadExactlyAsync(ciphertext, ct);

            var plaintext = new byte[chunkSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonceBuffer, ciphertext, tagBuffer, plaintext);

            await output.WriteAsync(plaintext, ct);
        }
    }

    private static byte[] DeriveKey(byte[] password, ReadOnlySpan<byte> salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }
}