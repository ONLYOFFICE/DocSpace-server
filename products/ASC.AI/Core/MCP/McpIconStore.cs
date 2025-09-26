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

using ASC.Data.Storage;
using ASC.Web.Core.Users;
using ASC.Web.Core.WhiteLabel;
using ASC.Web.Studio.Core;

using ImageMagick;

using NetEscapades.EnumGenerators;

namespace ASC.AI.Core.MCP;

[Scope]
public class McpIconStore(StorageFactory storageFactory, SetupInfo setupInfo, TenantManager tenantManager)
{
    private const string IconModuleName = "mcp_icons";
    private const string IconPathTemplate = "{0}_{1}.png";
    private IDataStore? _iconStore;
    
    private static readonly (IconSize Size, IMagickGeometry Geometry) _masterIconSize = 
        (IconSize.Icon1280, new MagickGeometry(1280, 1280));
    
    private static readonly FrozenDictionary<IconSize, IMagickGeometry> _iconSizes = new Dictionary<IconSize, IMagickGeometry> 
    {
        { IconSize.Icon48, new MagickGeometry(48, 48) },
        { IconSize.Icon32, new MagickGeometry(32, 32) },
        { IconSize.Icon24, new MagickGeometry(24, 24) },
        { IconSize.Icon16, new MagickGeometry(16, 16) }
    }.ToFrozenDictionary();
    
    public async Task SaveAsync(int tenantId, Guid serverId, IconParams iconParams)
    {
        var store = await InitStoreAsync(tenantId);
        
        var imageData = Convert.FromBase64String(iconParams.ImageBase64);
        
        imageData = await UserPhotoThumbnailManager.TryParseImage(imageData, setupInfo.MaxImageUploadSize, _masterIconSize.Geometry);
        if (imageData == null)
        {
            return;
        }
        
        var serverIdStr = serverId.ToString("N");

        using (var masterImageStream = new MemoryStream(imageData))
        {
            await store.SaveAsync(GetFilePath(serverIdStr, _masterIconSize.Size), masterImageStream);
        }
        
        try
        {
            var position = new Point(iconParams.X, iconParams.Y);
            var cropSize = new MagickGeometry(iconParams.Width, iconParams.Height);
            
            using var imgStream = new MemoryStream(imageData);
            using var img = new MagickImage(imgStream);
            
            foreach (var size in _iconSizes)
            {
                if (size.Value.Width != img.Width || size.Value.Height != img.Height)
                {
                    using var img2 = UserPhotoThumbnailManager.GetImage(
                        img, size.Value, new UserPhotoThumbnailSettings(position, cropSize));
                    
                    imageData = await CommonPhotoManager.SaveToBytes(img2);
                }
                else
                {
                    imageData = await CommonPhotoManager.SaveToBytes(img);
                }

                var fileName = GetFilePath(serverIdStr, size.Key);

                using var processedImgStream = new MemoryStream(imageData);
                await store.SaveAsync(fileName, processedImgStream);
            }
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }
    }

    public async Task<Icon> GetAsync(int tenantId, Guid serverId, DateTime modifiedOn)
    {
        var store = await InitStoreAsync(tenantId);
        var serverIdStr = serverId.ToString("N");
        
        return new Icon
        {
            IconOrig = await GetIconPathAsync(IconSize.Icon1280),
            Icon48 = await GetIconPathAsync(IconSize.Icon48),
            Icon32 = await GetIconPathAsync(IconSize.Icon32),
            Icon24 = await GetIconPathAsync(IconSize.Icon24),
            Icon16 = await GetIconPathAsync(IconSize.Icon16)
        };

        async Task<string> GetIconPathAsync(IconSize size)
        {
            var iconPath = GetFilePath(serverIdStr, size);
            var uri = await store.GetPreSignedUriAsync(string.Empty, iconPath, TimeSpan.MaxValue, null);
            if (uri == null)
            {
                return string.Empty;
            }
            
            return uri + "?hash=" + Math.Abs(modifiedOn.GetHashCode());
        }
    }
    
    public async Task<Icon> GetAsync(Guid serverId, DateTime modifiedOn)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await GetAsync(tenantId, serverId, modifiedOn);
    }
    
    public async Task DeleteAsync(int tenantId, Guid serverId)
    {
        var store = await InitStoreAsync(tenantId);

        var serverIdStr = serverId.ToString("N");

        await store.DeleteFilesAsync(string.Empty, $"{serverIdStr}*.*", false);
    }
    
    private async ValueTask<IDataStore> InitStoreAsync(int tenantId)
    {
        if (_iconStore != null)
        {
            return _iconStore;
        }
        
        return _iconStore = await storageFactory.GetStorageAsync(tenantId, IconModuleName);
    }

    private static string GetFilePath(string serverId, IconSize size)
    {
        return string.Format(IconPathTemplate, serverId, size.ToStringLowerFast());
    }
}

[EnumExtensions]
public enum IconSize
{
    Icon1280,
    Icon48,
    Icon32,
    Icon24,
    Icon16
}