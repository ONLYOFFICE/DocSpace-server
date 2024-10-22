// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Transient]
public class RoomIndexExportTask(IServiceScopeFactory serviceProvider) : DocumentBuilderTask<int, RoomIndexExportTaskData>(serviceProvider)
{
    private const string ScriptName = "RoomIndexExport.docbuilder";
    
    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName();
        
        var (data, outputFileName) = await GetRoomIndexExportData(serviceProvider, _userId, _data.RoomId);
        
        script = script
            .Replace("${tempFileName}", tempFileName)
            .Replace("${inputData}", JsonConvert.SerializeObject(data));
        
        return new DocumentBuilderInputData(script, tempFileName, outputFileName);
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, string fileName)
    {
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var socketManager = serviceProvider.GetService<SocketManager>();
        
        var file = serviceProvider.GetService<File<int>>();
        
        file.ParentId = await daoFactory.GetFolderDao<int>().GetFolderIDUserAsync(false, _userId);
        file.Title = fileName;
        
        using var request = new HttpRequestMessage();
        request.RequestUri = fileUri;

        using var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        
        var fileDao = daoFactory.GetFileDao<int>();

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);
        
        var filesMessageService = serviceProvider.GetService<FilesMessageService>();
        
        var headers = _data.Headers != null 
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value)) 
            : [];
        
        var room = await daoFactory.GetFolderDao<int>().GetFolderAsync(_data.RoomId);
        
        await filesMessageService.SendAsync(MessageAction.RoomIndexExportSaved, room, headers: headers);

        return file;
    }

    private static async Task<(object data, string outputFileName)> GetRoomIndexExportData<T>(IServiceProvider serviceProvider, Guid userId, T roomId)
    {
        var userManager = serviceProvider.GetService<UserManager>();
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var settingsManager = serviceProvider.GetService<SettingsManager>();
        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
        var pathProvider = serviceProvider.GetService<PathProvider>();
        var filesLinkUtility = serviceProvider.GetService<FilesLinkUtility>();
        var fileUtility = serviceProvider.GetService<FileUtility>();
        var tenantWhiteLabelSettingsHelper = serviceProvider.GetService<TenantWhiteLabelSettingsHelper>();
        var displayUserSettingsHelper = serviceProvider.GetService<DisplayUserSettingsHelper>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
        var documentServiceConnector = serviceProvider.GetService<DocumentServiceConnector>();

        var user = await userManager.GetUsersAsync(userId);

        var usertCulture = user.GetCulture();
        CultureInfo.CurrentCulture = usertCulture;
        CultureInfo.CurrentUICulture = usertCulture;

        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);

        var outputFileName = $"{room.Title}_{FilesCommonResource.RoomIndex_Index.ToLowerInvariant()}.xlsx";

        //TODO: think about loop by N
        var (entries, _) = await serviceProvider.GetService<EntryManager>()
            .GetEntriesAsync(room, room, 0, -1, null, false, Guid.Empty, null, null, false, true, new OrderBy(SortedByType.CustomOrder, true));

        var typedEntries = entries.OfType<FileEntry<T>>().ToList();

        var foldersIndex = await GetFoldersIndex(roomId, typedEntries, serviceProvider);

        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.LightSmall);

        logoPath = await documentServiceConnector.ReplaceCommunityAddressAsync(logoPath);

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
            var index = isFolder ? foldersIndex[entry.Id].Order : string.Join(".", foldersIndex[entry.ParentId].Order, entry.Order);
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
                dateFormat = $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}"
            },

            logoSrc = commonLinkUtility.GetFullAbsolutePath(logoPath),

            themeColors = new
            {
                mainBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
                lightBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
                mainFontColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1)
            },

            info = new
            {
                company = tenantWhiteLabelSettings.LogoText ?? TenantWhiteLabelSettings.DefaultLogoText,
                room = room.Title,
                exportAuthor = user.DisplayUserName(displayUserSettingsHelper),
                dateGenerated = tenantUtil.DateTimeNow().ConvertNumerals("g")
            },

            data = items
        };

        return (data, outputFileName);
    }
    
    private static async Task<Dictionary<T, FolderIndex>> GetFoldersIndex<T>(T roomId, IEnumerable<FileEntry<T>> entries, IServiceProvider serviceProvider)
    {
        var result = new Dictionary<T, FolderIndex> { { roomId, new FolderIndex(0, string.Empty) } };

        foreach (var entry in entries.Where(x => x.FileEntryType == FileEntryType.Folder))
        {
            if (result.TryGetValue(entry.ParentId, out var value))
            {
                result[entry.ParentId] = value with { ChildFoldersCount = value.ChildFoldersCount + 1 };
            }
            else
            {
                var order = await serviceProvider.GetService<BreadCrumbsManager>().GetBreadCrumbsOrderAsync(entry.ParentId);
                result[entry.ParentId] = new FolderIndex(1, order);
            }

            if (!result.ContainsKey(entry.Id))
            {
                result.Add(entry.Id, new FolderIndex(0, string.Join(".", result[entry.ParentId].Order, entry.Order)));
            }
        }

        return result;
    }
    
    private record FolderIndex(int ChildFoldersCount, string Order);
}

public record RoomIndexExportTaskData(int RoomId, IDictionary<string, string> Headers);