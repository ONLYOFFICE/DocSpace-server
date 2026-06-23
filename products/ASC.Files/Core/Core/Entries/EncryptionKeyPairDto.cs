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

namespace ASC.Web.Files.Core.Entries;

/// <summary>
/// The encryption key pair parameters.
/// </summary>
public class EncryptionKeyPairDto
{
    /// <summary>
    /// The encrypted private key.
    /// </summary>
    /// <example>MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDQg...</example>
    public string PrivateKeyEnc { get; set; }

    /// <summary>
    /// The public key.
    /// </summary>
    /// <example>-----BEGIN PUBLIC KEY----------END PUBLIC KEY-----</example>
    public string PublicKey { get; set; }

    /// <summary>
    /// The user ID of the encryption keys.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; set; }
}

[Scope]
public class EncryptionKeyPairDtoHelper(
    UserManager userManager,
    AuthContext authContext,
    EncryptionLoginProvider encryptionLoginProvider,
    FileSecurity fileSecurity,
    FileSharing fileSharing,
    IDaoFactory daoFactory)
{
    public async Task SetKeyPairAsync(string publicKey, string privateKeyEnc)
    {
        ArgumentException.ThrowIfNullOrEmpty(publicKey);
        ArgumentException.ThrowIfNullOrEmpty(privateKeyEnc);

        var userId = authContext.CurrentAccount.ID;
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(userId))
        {
            throw new SecurityException();
        }

        var keyPair = new EncryptionKeyPairDto
        {
            PrivateKeyEnc = privateKeyEnc,
            PublicKey = publicKey,
            UserId = userId
        };

        var keyPairString = JsonSerializer.Serialize(keyPair);
        await encryptionLoginProvider.SetKeysAsync(userId, keyPairString);
    }

    public async Task<EncryptionKeyPairDto> GetKeyPairAsync()
    {
        var currentAddressString = await encryptionLoginProvider.GetKeysAsync();
        if (string.IsNullOrEmpty(currentAddressString))
        {
            return null;
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };
        var keyPair = JsonSerializer.Deserialize<EncryptionKeyPairDto>(currentAddressString, options);
        if (keyPair.UserId != authContext.CurrentAccount.ID)
        {
            return null;
        }

        return keyPair;
    }

    public async Task<IEnumerable<EncryptionKeyPairDto>> GetKeyPairAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        await fileDao.InvalidateCacheAsync(fileId);

        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanEditAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        var locatedInPrivateRoom = file.RootFolderType == FolderType.VirtualRooms
            && await DocSpaceHelper.LocatedInPrivateRoomAsync(file, folderDao);

        if (file.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            throw new NotSupportedException();
        }

        var tmpFiles = await fileSharing.GetSharedInfoAsync([fileId], []);
        var fileShares = tmpFiles.ToList();
        fileShares = fileShares.Where(share => !share.SubjectGroup && share.Access == FileShare.ReadWrite).ToList();

        var tasks = fileShares.Select(async share =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(share.Id);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };
            var fileKeyPair = JsonSerializer.Deserialize<EncryptionKeyPairDto>(fileKeyPairString, options);
            if (fileKeyPair.UserId != share.Id)
            {
                return null;
            }

            fileKeyPair.PrivateKeyEnc = null;

            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .Where(keyPair => keyPair != null);

        return fileKeysPair;
    }
}