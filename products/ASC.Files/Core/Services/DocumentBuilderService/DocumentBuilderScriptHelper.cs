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

using System.Reflection;

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Scope]
public class DocumentBuilderScriptHelper(UserManager userManager,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    SettingsManager settingsManager,
    TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    PathProvider pathProvider,
    BreadCrumbsManager breadCrumbsManager)
{
    private record FolderIndex(int ChildFoldersCount, string Order);
    
    public async Task<(string script, string tempFileName, string outputFileName)> GetRoomIndexExportScript<T>(Guid userId, T roomId)
    {
        var script = await ReadTemplateFromEmbeddedResource("RoomIndexExport.docbuilder") ?? throw new Exception("Template not found");

        var tempFileName = GetTempFileName();

        var (data, outputFileName) = await GetRoomIndexExportData(userId, roomId);

        script = script
            .Replace("${tempFileName}", tempFileName)
            .Replace("${inputData}", JsonConvert.SerializeObject(data));

        return (script, tempFileName, outputFileName);
    }
    
    private static string GetTempFileName()
    {
        return $"temp{DateTime.UtcNow.Ticks}.xlsx";
    }
    
    private static async Task<string> ReadTemplateFromEmbeddedResource(string templateFileName)
    {
        var templateNamespace = typeof(DocumentBuilderScriptHelper).Namespace;
        var resourceName = $"{templateNamespace}.ScriptTemplates.{templateFileName}";
        
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            return null;
        }

        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync();
    }
    
    private async Task<(object data, string outputFileName)> GetRoomIndexExportData<T>(Guid userId, T roomId)
    {
        var user = await userManager.GetUsersAsync(userId);

        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);

        var outputFileName = $"{room.Title}_{FilesCommonResource.RoomIndex_Index.ToLowerInvariant()}.xlsx";

        //TODO: think about loop by N
        var (entries, _) = await entryManager.GetEntriesAsync(room, 0, -1, FilterType.None, false, Guid.Empty, null, null, false, true, new OrderBy(SortedByType.CustomOrder, true));

        var typedEntries = entries.OfType<FileEntry<T>>().ToList();

        var foldersIndex = await GetFoldersIndex(roomId, typedEntries);

        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.LightSmall);

        var items = new List<object>
        {
            new
            {
                index = (string)null,
                name = room.Title,
                url = commonLinkUtility.GetFullAbsolutePath(pathProvider.GetRoomsUrl(room.Id.ToString())),
                type = FilesCommonResource.RoomIndex_Room,
                size = (string)null,
                author = room.CreateByString,
                created = room.CreateOnString,
                modified = room.CreateOnString
            }
        };

        foreach (var entry in typedEntries)
        {
            var isFolder = entry.FileEntryType == FileEntryType.Folder;
            var index = isFolder ? foldersIndex[entry.Id].Order : string.Join(".", foldersIndex[entry.ParentId].Order, foldersIndex[entry.ParentId].ChildFoldersCount + entry.Order);
            var url = isFolder ? pathProvider.GetRoomsUrl(entry.Id.ToString()) : filesLinkUtility.GetFileWebPreviewUrl(fileUtility, entry.Title, entry.Id);

            items.Add(new
            {
                index = index.TrimStart('.'),
                name = entry.Title,
                url = commonLinkUtility.GetFullAbsolutePath(url),
                type = isFolder ? FilesCommonResource.RoomIndex_Folder : Path.GetExtension(entry.Title),
                size = isFolder ? null : Math.Round(((File<T>)entry).ContentLength / 1024d / 1024d, 2).ToString(CultureInfo.InvariantCulture),
                author = entry.CreateByString,
                created = entry.CreateOnString,
                modified = entry.ModifiedOnString
            });
        }

        var data = new
        {
            resources = new
            {
                company = FilesCommonResource.RoomIndex_Company + ":",
                room = FilesCommonResource.RoomIndex_Room + ":",
                exportAuthor = FilesCommonResource.RoomIndex_ExportAuthor + ":",
                dateGenerated = FilesCommonResource.RoomIndex_DateGenerated + ":",
                index = FilesCommonResource.RoomIndex_Index,
                name = FilesCommonResource.RoomIndex_Name,
                type = FilesCommonResource.RoomIndex_Type,
                size = FilesCommonResource.RoomIndex_Size,
                author = FilesCommonResource.RoomIndex_Author,
                created = FilesCommonResource.RoomIndex_Created,
                modified = FilesCommonResource.RoomIndex_Modified,
                total = FilesCommonResource.RoomIndex_Total,
                sheetName = FilesCommonResource.RoomIndex_SheetName,
                numberFormat = "0.00",
                dateFormat = $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}"
            },

            logoSrc = commonLinkUtility.GetFullAbsolutePath(logoPath),

            themeColors = new
            {
                mainBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
                lightBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
                mainFontColor = ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1)
            },

            info = new
            {
                company = tenantWhiteLabelSettings.LogoText,
                room = room.Title,
                exportAuthor = user.DisplayUserName(displayUserSettingsHelper),
                dateGenerated = room.CreateOnString
            },

            data = items
        };

        return (data, outputFileName);
    }

    private async Task<Dictionary<T, FolderIndex>> GetFoldersIndex<T>(T roomId, IEnumerable<FileEntry<T>> entries)
    {
        var result = new Dictionary<T, FolderIndex> { { roomId, new FolderIndex(0, string.Empty) } };

        foreach (var entry in entries.Where(x => x.FileEntryType == FileEntryType.Folder))
        {
            if (result.ContainsKey(entry.ParentId))
            {
                result[entry.ParentId] = new FolderIndex(result[entry.ParentId].ChildFoldersCount + 1, result[entry.ParentId].Order);
            }
            else
            {
                var order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(entry.ParentId);
                result[entry.ParentId] = new FolderIndex(1, order);
            }

            if (!result.ContainsKey(entry.Id))
            {
                result.Add(entry.Id, new FolderIndex(0, string.Join(".", result[entry.ParentId].Order, entry.Order)));
            }
        }

        return result;
    }

    private static int[] ConvertHtmlColorToRgb(string color, double opacity)
    {
        if (color[0] != '#' || color.Length != 7 || opacity < 0 || opacity > 1)
        {
            throw new ArgumentException();
        }

        return
        [
            ApplyOpacity(255, Convert.ToInt32(color.Substring(1, 2), 16), opacity),
            ApplyOpacity(255, Convert.ToInt32(color.Substring(3, 2), 16), opacity),
            ApplyOpacity(255, Convert.ToInt32(color.Substring(5, 2), 16), opacity)
        ];

        static int ApplyOpacity(int background, int overlay, double opacity)
        {
            return (int)((1 - opacity) * background + opacity * overlay);
        }
    }
}