// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Core.Entries;

[EnumExtensions]
[JsonConverter(typeof(EncryptionKeyTypeConverter))]
public enum EncryptionKeyType
{
    Private,
    Public
}

public class EncryptionKeyTypeConverter : System.Text.Json.Serialization.JsonConverter<EncryptionKeyType>
{
    public override EncryptionKeyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var result))
        {
            return (EncryptionKeyType)result;
        }

        if (reader.TokenType == JsonTokenType.String && EncryptionKeyTypeExtensions.TryParse(reader.GetString(), out var share))
        {
            return share;
        }

        return EncryptionKeyType.Private;
    }

    public override void Write(Utf8JsonWriter writer, EncryptionKeyType value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}

public class EncryptionKeyDto : IMapFrom<EncryptionKeyRequestDto>
{
    public Guid Id { get; set; }
    public string Key { get; set; }
    public EncryptionKeyType Type { get; set; }
    public EncryptionKeyValueDto Value { get; set; }
}

public class EncryptionKeyValueDto: IMapFrom<EncryptionKeyValueRequestDto>
{
    public string Version { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
}

[Scope]
public class EncryptionKeyPairDtoHelper(
    UserManager userManager,
    AuthContext authContext,
    EncryptionLoginProvider encryptionLoginProvider,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory)
{    
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true, PropertyNameCaseInsensitive = true
    };
    
    public async Task<List<EncryptionKeyDto>> SetKeyPairAsync(IEnumerable<EncryptionKeyDto> keyPairs, bool replace)
    {
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        if (!authContext.IsAuthenticated || await userManager.IsUserAsync(user))
        {
            throw new SecurityException();
        }

        var currentAddressString = await GetKeyPairAsync() ?? new List<EncryptionKeyDto>();

        foreach (var keyPair in keyPairs)
        {
            var keyPairSettings = currentAddressString.FirstOrDefault(r => r.Id == keyPair.Id);
            if (keyPairSettings != null)
            {
                if (replace)
                {
                    keyPairSettings.Value = keyPair.Value;
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
        
        return JsonSerializer.Deserialize<List<EncryptionKeyDto>>(currentAddressString, _jsonSerializerOptions);
    }

    public async Task<List<EncryptionKeyDto>> GetKeyPairAsync<T>(T fileId, FileStorageService fileStorageService)
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

        var tmpFiles = await fileStorageService.GetSharedInfoAsync(new List<T> { fileId }, new List<T>());
        var fileShares = tmpFiles.ToList();
        fileShares = fileShares.Where(share => !share.SubjectGroup && !share.Id.Equals(FileConstant.ShareLinkId)).ToList();

        var tasks = fileShares.Select(async share =>
        {
            var fileKeyPairString = await encryptionLoginProvider.GetKeysAsync(share.Id);
            if (string.IsNullOrEmpty(fileKeyPairString))
            {
                return null;
            }
            
            var fileKeyPair = JsonSerializer.Deserialize<List<EncryptionKeyDto>>(fileKeyPairString, _jsonSerializerOptions)
                .Where(r => r.Id == share.Id)//r.UserId == share.Id
                .ToList();
            
            return fileKeyPair;
        });

        var fileKeysPair = (await Task.WhenAll(tasks))
            .SelectMany(keyPair => keyPair);

        return fileKeysPair.ToList();
    }

    public async Task<List<EncryptionKeyDto>> DeleteAsync(IEnumerable<Guid> id)
    {
        var currentSettings = await GetKeyPairAsync();
        if(currentSettings == null)
        {
            return null;
        }

        currentSettings.RemoveAll(r => id.Contains(r.Id));

        await Save(currentSettings);

        return currentSettings;
    }

    private async Task Save(IEnumerable<EncryptionKeyDto> currentSettings)
    {
        var keyPairString = JsonSerializer.Serialize(currentSettings);
        await encryptionLoginProvider.SetKeysAsync(authContext.CurrentAccount.ID, keyPairString);
    }
}
