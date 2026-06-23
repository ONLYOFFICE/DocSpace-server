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

namespace ASC.Data.Encryption;

[Transient]
public class Crypt(IConfiguration configuration, TempPath tempPath) : ICrypt
{
    private string Storage { get; set; }
    private EncryptionSettings Settings { get; set; }
    private string TempDir { get; set; }

    public void Init(string storageName, EncryptionSettings encryptionSettings)
    {
        Storage = storageName;
        Settings = encryptionSettings;
        TempDir = tempPath.GetTempPath();
    }

    public byte Version => 1;

    public async ValueTask EncryptFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(Settings.Password))
        {
            return;
        }

        var metadata = new Metadata(configuration);

        metadata.Initialize(Settings.Password);

        await using (var fileStream = File.OpenRead(filePath))
        {
            if (await metadata.TryReadFromStreamAsync(fileStream, Version))
            {
                return;
            }
        }

        await EncryptFileAsync(filePath, Settings.Password);
    }

    public async ValueTask DecryptFileAsync(string filePath)
    {
        if (Settings.Status == EncryprtionStatus.Decrypted)
        {
            return;
        }

        await DecryptFileAsync(filePath, Settings.Password);
    }

    public async Task<Stream> GetReadStreamAsync(string filePath)
    {
        if (Settings.Status == EncryprtionStatus.Decrypted)
        {
            return File.OpenRead(filePath);
        }

        return await GetReadStreamAsync(filePath, Settings.Password);
    }

    public async Task<long> GetFileSizeAsync(string filePath)
    {
        if (Settings.Status == EncryprtionStatus.Decrypted)
        {
            return new FileInfo(filePath).Length;
        }

        return await GetFileSize(filePath, Settings.Password);
    }


    private async Task EncryptFileAsync(string filePath, string password)
    {
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.IsReadOnly)
        {
            fileInfo.IsReadOnly = false;
        }

        var encryptedFilePath = GetUniqFileName(filePath, ".enc");
        try
        {
            var metadata = new Metadata(configuration);

            metadata.Initialize(Version, password, fileInfo.Length);

            await using (var encryptedFileStream = new FileStream(encryptedFilePath, FileMode.Create))
            {
                await metadata.WriteToStreamAsync(encryptedFileStream);

                using (var algorithm = metadata.GetCryptographyAlgorithm())
                {
                    using var transform = algorithm.CreateEncryptor();
                    await using var cryptoStream = new CryptoStreamWrapper(encryptedFileStream, transform, CryptoStreamMode.Write);
                    await using (var fileStream = File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(cryptoStream);
                        fileStream.Close();
                    }

                    await cryptoStream.FlushFinalBlockAsync();

                    await metadata.ComputeAndWriteHmacHashAsync(encryptedFileStream);

                    cryptoStream.Close();
                }

                encryptedFileStream.Close();
            }

            ReplaceFile(encryptedFilePath, filePath);
        }
        catch (Exception)
        {
            if (File.Exists(encryptedFilePath))
            {
                File.Delete(encryptedFilePath);
            }

            throw;
        }
    }

    private async Task DecryptFileAsync(string filePath, string password)
    {
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.IsReadOnly)
        {
            fileInfo.IsReadOnly = false;
        }

        var decryptedFilePath = GetUniqFileName(filePath, ".dec");

        try
        {
            var metadata = new Metadata(configuration);

            metadata.Initialize(password);

            await using (var fileStream = File.OpenRead(filePath))
            {
                if (!await metadata.TryReadFromStreamAsync(fileStream, Version))
                {
                    return;
                }

                metadata.ComputeAndValidateHmacHash(fileStream);

                await using (var decryptedFileStream = new FileStream(decryptedFilePath, FileMode.Create))
                {
                    using (var algorithm = metadata.GetCryptographyAlgorithm())
                    {
                        using var transform = algorithm.CreateDecryptor();
                        await using var cryptoStream = new CryptoStreamWrapper(decryptedFileStream, transform, CryptoStreamMode.Write);
                        await fileStream.CopyToAsync(cryptoStream);

                        await cryptoStream.FlushFinalBlockAsync();
                        cryptoStream.Close();
                    }

                    decryptedFileStream.Close();
                }

                fileStream.Close();
            }

            ReplaceFile(decryptedFilePath, filePath);
        }
        catch (Exception)
        {
            if (File.Exists(decryptedFilePath))
            {
                File.Delete(decryptedFilePath);
            }

            throw;
        }
    }

    private async Task<Stream> GetReadStreamAsync(string filePath, string password)
    {
        var metadata = new Metadata(configuration);

        metadata.Initialize(password);

        var fileStream = File.OpenRead(filePath);

        if (!await metadata.TryReadFromStreamAsync(fileStream, Version))
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            return fileStream;
        }

        metadata.ComputeAndValidateHmacHash(fileStream);

        var wrapper = new StreamWrapper(fileStream, metadata);

        return wrapper;
    }

    private async Task<long> GetFileSize(string filePath, string password)
    {
        var metadata = new Metadata(configuration);

        metadata.Initialize(password);

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, metadata.GetMetadataLength(), FileOptions.SequentialScan);
        if (await metadata.TryReadFromStreamAsync(fileStream, Version))
        {
            return metadata.GetFileSize();
        }

        return new FileInfo(filePath).Length;
    }


    private string GetUniqFileName(string filePath, string ext)
    {
        var dir = string.IsNullOrEmpty(TempDir) ? Path.GetDirectoryName(filePath) : TempDir;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var result = CrossPlatform.PathCombine(dir, $"{Storage}_{name}{ext}");
        var index = 1;

        while (File.Exists(result))
        {
            result = CrossPlatform.PathCombine(dir, $"{Storage}_{name}({index++}){ext}");
        }

        return result;
    }

    private void ReplaceFile(string modifiedFilePath, string originalFilePath)
    {
        var tempFilePath = GetUniqFileName(originalFilePath, ".tmp");

        File.Move(originalFilePath, tempFilePath);

        try
        {
            File.Move(modifiedFilePath, originalFilePath);
        }
        catch (Exception)
        {
            File.Move(tempFilePath, originalFilePath);
            throw;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}