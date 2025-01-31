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
        var (scriptFilePath, tempFileName, outputFileName) = await GetRoomIndexExportData(serviceProvider, _userId, _data.RoomId);

        return new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var socketManager = serviceProvider.GetService<SocketManager>();

        var file = serviceProvider.GetService<File<int>>();

        file.ParentId = await daoFactory.GetFolderDao<int>().GetFolderIDUserAsync(false, _userId);
        file.Title = inputData.OutputFileName;

        using var request = new HttpRequestMessage();
        request.RequestUri = fileUri;

        using var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        
        var fileDao = daoFactory.GetFileDao<int>();

        file.ContentLength = stream.Length;

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);

        var filesMessageService = serviceProvider.GetService<FilesMessageService>();

        var headers = _data.Headers != null 
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value)) 
            : [];

        var room = await daoFactory.GetFolderDao<int>().GetFolderAsync(_data.RoomId);

        await filesMessageService.SendAsync(MessageAction.RoomIndexExportSaved, room, headers: headers);

        if (System.IO.File.Exists(inputData.Script))
        {
            System.IO.File.Delete(inputData.Script);
        }

        return file;
    }

    private static async Task<(string scriptFilePath, string tempFileName, string outputFileName)> GetRoomIndexExportData<T>(IServiceProvider serviceProvider, Guid userId, T roomId)
    {
        var userManager = serviceProvider.GetService<UserManager>();
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var settingsManager = serviceProvider.GetService<SettingsManager>();
        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
        var tenantWhiteLabelSettingsHelper = serviceProvider.GetService<TenantWhiteLabelSettingsHelper>();
        var displayUserSettingsHelper = serviceProvider.GetService<DisplayUserSettingsHelper>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();
        var documentServiceConnector = serviceProvider.GetService<DocumentServiceConnector>();
        var tempPath = serviceProvider.GetService<TempPath>();

        var user = await userManager.GetUsersAsync(userId);

        var usertCulture = user.GetCulture();
        CultureInfo.CurrentCulture = usertCulture;
        CultureInfo.CurrentUICulture = usertCulture;

        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);

        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.LightSmall);

        logoPath = documentServiceConnector.ReplaceCommunityAddress(logoPath);

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
                numberFormat = "0.000",
                dateFormat = $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}"
            },

            logoSrc = commonLinkUtility.GetFullAbsolutePath(logoPath.Split('?')[0]),

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
                exportAuthor = user.DisplayUserName(false, displayUserSettingsHelper),
                dateGenerated = tenantUtil.DateTimeNow().ConvertNumerals("g")
            }
        };

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = $"{room.Title}_{FilesCommonResource.RoomIndex_Index.ToLowerInvariant()}.xlsx";

        script = script
            .Replace("${tempFileName}", tempFileName)
            .Replace("${inputData}", JsonSerializer.Serialize(data));

        var scriptParts = script.Split("${inputDataItems}");

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            await WriteItemsToScript(serviceProvider, room, writer);

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private static async Task WriteItemsToScript<T>(IServiceProvider serviceProvider, Folder<T> room, StreamWriter writer)
    {
        var entryManager = serviceProvider.GetService<EntryManager>();
        var pathProvider = serviceProvider.GetService<PathProvider>();
        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
        var filesLinkUtility = serviceProvider.GetService<FilesLinkUtility>();
        var fileUtility = serviceProvider.GetService<FileUtility>();
        var breadCrumbsManager = serviceProvider.GetService<BreadCrumbsManager>();

        var from = 0;
        var count = 1000;
        var separator = string.Empty;
        var filterType = FilterType.FoldersOnly;
        var foldersIndex = new Dictionary<T, FolderIndex> { { room.Id, new FolderIndex(0, string.Empty) } };

        var items = new List<object>
        {
            new
            {
                index = (string)null,
                name = room.Title,
                url = commonLinkUtility.GetFullAbsolutePath(pathProvider.GetRoomsUrl(room.Id.ToString(), false)),
                type = FilesCommonResource.RoomIndex_Room,
                size = (string)null,
                author = room.CreateByString,
                created = room.CreateOnString,
                modified = room.CreateOnString
            }
        };

        while (true)
        {
            var (entries, _) = await entryManager.GetEntriesAsync(room, room, from, count, [filterType], false, Guid.Empty, null, null, false, true, new OrderBy(SortedByType.CustomOrder, true));
            var typedEntries = entries.OfType<FileEntry<T>>().ToList();

            if (filterType == FilterType.FoldersOnly)
            {
                foreach (var entry in typedEntries)
                {
                    if (foldersIndex.TryGetValue(entry.ParentId, out var value))
                    {
                        foldersIndex[entry.ParentId] = value with { ChildFoldersCount = value.ChildFoldersCount + 1 };
                    }
                    else
                    {
                        var order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(entry.ParentId);
                        foldersIndex[entry.ParentId] = new FolderIndex(1, order);
                    }

                    if (!foldersIndex.ContainsKey(entry.Id))
                    {
                        foldersIndex.Add(entry.Id, new FolderIndex(0, string.Join(".", foldersIndex[entry.ParentId].Order, entry.Order)));
                    }
                }
            }

            foreach (var entry in typedEntries)
            {
                var isFolder = entry.FileEntryType == FileEntryType.Folder;
                var index = isFolder ? foldersIndex[entry.Id].Order : string.Join(".", foldersIndex[entry.ParentId].Order, entry.Order);
                var url = isFolder ? pathProvider.GetRoomsUrl(entry.Id.ToString(), false) : filesLinkUtility.GetFileWebPreviewUrl(fileUtility, entry.Title, entry.Id);

                items.Add(new
                {
                    index = index.TrimStart('.'),
                    name = entry.Title,
                    url = commonLinkUtility.GetFullAbsolutePath(url),
                    type = isFolder ? FilesCommonResource.RoomIndex_Folder : Path.GetExtension(entry.Title),
                    size = isFolder ? null : Math.Round(((File<T>)entry).ContentLength / 1024d / 1024d, 3).ToString(CultureInfo.InvariantCulture),
                    author = entry.CreateByString,
                    created = entry.CreateOnString,
                    modified = entry.ModifiedOnString
                });
            }

            if (items.Count > 0)
            {
                var jsonArray = JsonSerializer.Serialize(items);

                var text = separator + jsonArray.TrimStart('[').TrimEnd(']');

                await writer.WriteAsync(text);

                if (string.IsNullOrEmpty(separator))
                {
                    separator = ",";
                }
            }

            if (typedEntries.Count < count)
            {
                if (filterType == FilterType.FoldersOnly)
                {
                    filterType = FilterType.FilesOnly;
                    from = 0;
                    items = [];
                }
                else
                {
                    break;
                }
            }
            else
            {
                from += count;
                items = [];
            }
        }
    }

    private record FolderIndex(int ChildFoldersCount, string Order);
}

public record RoomIndexExportTaskData(int RoomId, IDictionary<string, string> Headers);