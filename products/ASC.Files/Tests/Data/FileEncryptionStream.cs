// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode


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
        string password,
        CancellationToken ct = default)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(password, salt);
        
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
        string password,
        CancellationToken ct = default)
    {
        var salt = new byte[SaltSize];
        await input.ReadExactlyAsync(salt, ct);
        
        var key = DeriveKey(password, salt);
        
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

    private static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }
}