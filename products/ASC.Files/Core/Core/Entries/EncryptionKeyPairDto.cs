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

[EnumExtensions]
[JsonConverter(typeof(EncryptionKeyTypeConverter))]
public enum EncryptionKeyType
{
    Crypt,
    Sign
}

public class EncryptionKeyTypeConverter : JsonConverter<EncryptionKeyType>
{
    public override EncryptionKeyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var result))
        {
            return (EncryptionKeyType)result;
        }

        if (reader.TokenType == JsonTokenType.String && EncryptionKeyTypeExtensions.TryParse(reader.GetString(), true, out var share))
        {
            return share;
        }

        return EncryptionKeyType.Sign;
    }

    public override void Write(Utf8JsonWriter writer, EncryptionKeyType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToStringFast());
    }
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

    public async Task<List<EncryptionKeyDto>> SetKeyPairAsync(IEnumerable<EncryptionKeyDto> keyPairs, bool replace)
    {
        var userId = authContext.CurrentAccount.ID;
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(userId))
        {
            throw new SecurityException();
        }

        var currentAddressString = await GetKeyPairAsync() ?? [];

        foreach (var keyPair in keyPairs)
        {
            keyPair.UserId = userId;

            var index = currentAddressString.FindIndex(r=> r.Id == keyPair.Id);
            if (index > -1)
            {
                if (replace)
                {
                    currentAddressString[index] = keyPair;
                }
            }
            else if (!replace)
            {
                currentAddressString.Add(keyPair);
            }
        }

        await Save(currentAddressString);

        return currentAddressString;
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairAsync()
    {
        var currentAddressString = await encryptionLoginProvider.GetKeysAsync();
        if (string.IsNullOrEmpty(currentAddressString))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<EncryptionKeyDto>>(currentAddressString, JsonSerializerOptions.Web);
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        await fileDao.InvalidateCacheAsync(fileId);

        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(file))
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
        fileShares = fileShares.Where(share => !share.SubjectGroup).ToList();

        var tasks = fileShares.Select(async share =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(share.Id);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }

            var fileKeyPair = JsonSerializer.Deserialize<List<EncryptionKeyDto>>(fileKeyPairString, JsonSerializerOptions.Web)
                .ToList();

            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .Where(keyPair => keyPair != null)
            .SelectMany(keyPair => keyPair);

        return fileKeysPair.ToList();
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairForRoomAsync<T>(T roomId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        var locatedInPrivateRoom = room.RootFolderType == FolderType.VirtualRooms && DocSpaceHelper.LocatedInPrivateRoom(room);

        if (room.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            throw new NotSupportedException();
        }

        var tmpFiles = await fileSharing.GetSharedInfoAsync(room);
        var fileShares = tmpFiles.ToList();
        fileShares = fileShares.Where(share => !share.SubjectGroup).ToList();

        var tasks = fileShares.Select(async share =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(share.Id);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }

            var fileKeyPair = JsonSerializer.Deserialize<List<EncryptionKeyDto>>(fileKeyPairString, JsonSerializerOptions.Web)
                .ToList();

            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .Where(keyPair => keyPair != null)
            .SelectMany(keyPair => keyPair);

        return fileKeysPair.ToList();
    }

    public async Task<List<EncryptionKeyDto>> DeleteAsync(Guid id)
    {
        var currentSettings = await GetKeyPairAsync();
        if(currentSettings == null)
        {
            return null;
        }

        currentSettings.RemoveAll(r => r.Id == id);

        return await Save(currentSettings);
    }

    private async Task<List<EncryptionKeyDto>> Save(List<EncryptionKeyDto> currentSettings)
    {
        var keyPairString = JsonSerializer.Serialize(currentSettings, JsonSerializerOptions.Web);
        await encryptionLoginProvider.SetKeysAsync(authContext.CurrentAccount.ID, keyPairString);
        return currentSettings;
    }
}
