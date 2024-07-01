﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using Image = SixLabors.ImageSharp.Image;
using UnknownImageFormatException = ASC.Web.Core.Users.UnknownImageFormatException;

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class RoomLogoManager(
    StorageFactory storageFactory,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ILogger<RoomLogoManager> logger,
    FilesMessageService filesMessageService,
    EmailValidationKeyProvider emailValidationKeyProvider,
    SecurityContext securityContext,
    FileUtilityConfiguration fileUtilityConfiguration, 
    ExternalShare externalShare)
{
    internal const string LogosPathSplitter = "_";
    private const string LogosPath = $"{{0}}{LogosPathSplitter}{{1}}.png";
    private const string ModuleName = "room_logos";
    private const string TempDomainPath = "logos_temp";

    private static readonly (SizeName, Size) _originalLogoSize = (SizeName.Original, new Size(1280, 1280));
    private static readonly (SizeName, Size) _largeLogoSize = (SizeName.Large, new Size(96, 96));
    private static readonly (SizeName, Size) _mediumLogoSize = (SizeName.Medium, new Size(32, 32));
    private static readonly (SizeName, Size) _smallLogoSize = (SizeName.Small, new Size(16, 16));

    private IDataStore _dataStore;

    public bool EnableAudit { get; set; } = true;

    private async ValueTask<IDataStore> GetDataStoreAsync()
    {
        if (_dataStore == null)
        {
            var tenantId = await tenantManager.GetCurrentTenantIdAsync();
            _dataStore = await storageFactory.GetStorageAsync(tenantId, ModuleName);
        }

        return _dataStore;
    }

    public async Task<Folder<T>> CreateAsync<T>(T id, string tempFile, int x, int y, int width, int height)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(id);

        if (string.IsNullOrEmpty(tempFile))
        {
            return room;
        }

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var store = await GetDataStoreAsync();
        var fileName = Path.GetFileName(tempFile);
        var data = await GetTempAsync(store, fileName);

        var stringId = GetId(room);

        await SaveWithProcessAsync(store, stringId, data, -1, new Point(x, y), new Size(width, height));
        await RemoveTempAsync(store, fileName);

        room.SettingsHasLogo = true;

        await SaveRoomAsync(folderDao, room);

        if (EnableAudit)
        {
            await filesMessageService.SendAsync(MessageAction.RoomLogoCreated, room, room.Title);
        }

        return room;
    }

    public async Task<Folder<T>> DeleteAsync<T>(T id, bool checkPermissions = true)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(id);

        if (!room.SettingsHasLogo)
        {
            return room;
        }

        if (checkPermissions && !await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var stringId = GetId(room);

        try
        {
            var store = await GetDataStoreAsync(); 
            await store.DeleteFilesAsync(string.Empty, $"{ProcessFolderId(stringId)}*.*", false);
            room.SettingsHasLogo = false;

            await SaveRoomAsync(folderDao, room);

            if (EnableAudit)
            {
                await filesMessageService.SendAsync(MessageAction.RoomLogoDeleted, room, room.Title);
            }
        }
        catch (Exception e)
        {
            logger.ErrorRemoveRoomLogo(e);
        }

        return room;
    }

    public async ValueTask<Logo> GetLogoAsync<T>(Folder<T> room)
    {
        if (!room.SettingsHasLogo)
        {
            if (string.IsNullOrEmpty(room.SettingsColor))
            {
                room.SettingsColor = GetRandomColour();
                
                await SaveRoomAsync(daoFactory.GetFolderDao<T>(), room);
            }

            return new Logo
            {
                Original = string.Empty,
                Large = string.Empty,
                Medium = string.Empty,
                Small = string.Empty,
                Color = room.SettingsColor
            };
        }

        var id = GetId(room);

        var cacheKey = Math.Abs(room.ModifiedOn.GetHashCode());
        var secure = !securityContext.IsAuthenticated;

        return new Logo
        {
            Original = await GetLogoPathAsync(id, SizeName.Original, cacheKey, secure),
            Large = await GetLogoPathAsync(id, SizeName.Large, cacheKey, secure),
            Medium = await GetLogoPathAsync(id, SizeName.Medium, cacheKey, secure),
            Small = await GetLogoPathAsync(id, SizeName.Small, cacheKey, secure)
        };
    }

    public async Task<string> SaveTempAsync(byte[] data, long maxFileSize)
    {
        data = UserPhotoThumbnailManager.TryParseImage(data, maxFileSize, _originalLogoSize.Item2);

        var fileName = $"{Guid.NewGuid()}.png";

        using var stream = new MemoryStream(data);
        var store = await GetDataStoreAsync();
        var path = await store.SaveAsync(TempDomainPath, fileName, stream);

        var pathAsString = path.ToString();

        var pathWithoutQuery = pathAsString;

        if (pathAsString.IndexOf('?') > 0)
        {
            pathWithoutQuery = pathAsString[..pathAsString.IndexOf('?')];
        }

        return pathWithoutQuery;
    }

    public async Task<bool> CopyAsync<TFrom, TTo>(Folder<TFrom> from, Folder<TTo> to)
    {
        if (!from.SettingsHasLogo)
        {
            return false;
        }

        var storage = await GetDataStoreAsync();

        foreach (var size in Enum.GetValues<SizeName>())
        {
            var fileNameFrom = GetFileName(from.Id, size);
            var fileNameTo = GetFileName(to.Id, size);
            await storage.CopyAsync(fileNameFrom, string.Empty, fileNameTo);
        }

        return true;
    }
    
    internal string GetRandomColour()
    {
        var rand = new Random();
        var color = fileUtilityConfiguration.LogoColors[rand.Next(fileUtilityConfiguration.LogoColors.Count - 1)];
        var result = Color.FromRgba(color.R, color.G, color.B, 1).ToHex();
        return result[..^2];//without opacity
    }

    private async Task RemoveTempAsync(IDataStore store, string fileName)
    {
        var index = fileName.LastIndexOf('.');
        var fileNameWithoutExt = (index != -1) ? fileName[..index] : fileName;

        try
        {
            await store.DeleteFilesAsync(TempDomainPath, "", fileNameWithoutExt + "*.*", false);
        }
        catch (Exception e)
        {
            logger.ErrorRemoveTempPhoto(e);
        }
    }

    private async Task SaveWithProcessAsync(IDataStore store, string id, byte[] imageData, long maxFileSize, Point position, Size cropSize)
    {
        imageData = UserPhotoThumbnailManager.TryParseImage(imageData, maxFileSize, _originalLogoSize.Item2);
        
        var fileName = GetFileName(id, SizeName.Original);
        
        if (imageData == null || imageData.Length == 0)
        {
            return;
        }

        using (var stream = new MemoryStream(imageData))
        {
            await store.SaveAsync(fileName, stream);
        }

        var sizes = new[] { _mediumLogoSize, _smallLogoSize, _largeLogoSize};

        if (imageData is not { Length: > 0 })
        {
            throw new UnknownImageFormatException();
        }
        if (maxFileSize != -1 && imageData.Length > maxFileSize)
        {
            throw new ImageWeightLimitException();
        }

        try
        {
            using var imageStream = new MemoryStream(imageData);
            using var img = await Image.LoadAsync(imageStream);
            foreach (var size in sizes)
            {
                if (size.Item2 != img.Size)
                {
                    using var img2 = UserPhotoThumbnailManager.GetImage(img, size.Item2, new UserPhotoThumbnailSettings(position, cropSize));
                    imageData = CommonPhotoManager.SaveToBytes(img2);
                }
                else
                {
                    imageData = CommonPhotoManager.SaveToBytes(img);
                }

                var imageFileName = string.Format(LogosPath, ProcessFolderId(id), size.Item1.ToStringLowerFast());

                using var stream2 = new MemoryStream(imageData);
                await store.SaveAsync(imageFileName, stream2);
            }
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }
    }

    private async ValueTask<string> GetLogoPathAsync<T>(T id, SizeName size, int hash, bool secure = false)
    {
        var fileName = GetFileName(id, size);
        var headers = secure ? new[] { SecureHelper.GenerateSecureKeyHeader(fileName, emailValidationKeyProvider) } : null;

        var store = await GetDataStoreAsync();

        var uri = await store.GetPreSignedUriAsync(string.Empty, fileName, TimeSpan.MaxValue, headers);

        return externalShare.GetUrlWithShare(uri + (secure ? "&" : "?") + $"hash={hash}");
    }

    private static string GetFileName<T>(T id, SizeName size)
    {
        var fileName = string.Format(LogosPath, ProcessFolderId(id), size.ToStringLowerFast());
        return fileName;
    }

    private static async Task<byte[]> GetTempAsync(IDataStore store, string fileName)
    {
        await using var stream = await store.GetReadStreamAsync(TempDomainPath, fileName);

        var data = new MemoryStream();
        var buffer = new byte[1024 * 10];
        while (true)
        {
            var count = await stream.ReadAsync(buffer);
            if (count == 0)
            {
                break;
            }

            await data.WriteAsync(buffer.AsMemory(0, count));
        }

        return data.ToArray();
    }
    
    private async Task SaveRoomAsync<T>(IFolderDao<T> folderDao, Folder<T> room)
    {
        if (room.ProviderEntry)
        {
            var provider = await daoFactory.ProviderDao.UpdateRoomProviderInfoAsync(new ProviderData
            {
                Id = room.ProviderId,
                HasLogo = room.SettingsHasLogo,
                Color = room.SettingsColor
            });
            
            room.ModifiedOn = provider.ModifiedOn;
        }
        else
        {
            await folderDao.SaveFolderAsync(room);
        }
    }

    private static string ProcessFolderId<T>(T id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        return id.GetType() != typeof(string)
            ? id.ToString()
            : id.ToString()?.Replace("|", "");
    }

    private static string GetId<T>(Folder<T> room)
    {
        if (!room.MutableId)
        {
            return room.Id.ToString();
        }

        var match = Selectors.Pattern.Match(room.Id.ToString()!);

        return $"{match.Groups["selector"]}-{match.Groups["id"]}";
    }
}

[EnumExtensions]
public enum SizeName
{
    Original = 0,
    Large = 1,
    Medium = 2,
    Small = 3
}