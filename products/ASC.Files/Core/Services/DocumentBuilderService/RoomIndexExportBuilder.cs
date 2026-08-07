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

namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// Writes the room index export script: the shared header block plus every folder and file of the
/// room, streamed page by page so that large rooms do not have to be held in memory.
/// </summary>
[Scope]
public class RoomIndexExportBuilder(
    UserManager userManager,
    IDaoFactory daoFactory,
    ReportHeaderService reportHeaderService,
    CommonLinkUtility commonLinkUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    TempPath tempPath,
    EntryManager entryManager,
    PathProvider pathProvider,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    BreadCrumbsManager breadCrumbsManager)
{
    private const string ScriptName = "RoomIndexExport.docbuilder";

    public async Task<(string ScriptFilePath, string TempFileName, string OutputFileName)> BuildAsync<T>(Guid userId, T roomId)
    {
        var user = await userManager.GetUsersAsync(userId);

        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);

        var header = await reportHeaderService.BuildAsync(userCulture);

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
                dateFormat = header.LongDateFormat
            },

            logoSrc = header.LogoSrc,

            themeColors = new
            {
                mainBgColor = header.MainBgColor,
                lightBgColor = header.LightBgColor,
                mainFontColor = header.MainFontColor
            },

            info = new
            {
                company = header.Company,
                room = room.Title,
                exportAuthor = user.DisplayUserName(false, displayUserSettingsHelper),
                dateGenerated = header.DateGenerated
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

            await WriteItemsToScriptAsync(room, writer);

            await writer.WriteAsync(scriptParts[1]);
        }

        return (scriptFilePath, tempFileName, outputFileName);
    }

    private async Task WriteItemsToScriptAsync<T>(Folder<T> room, StreamWriter writer)
    {
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
                created = FormatDate(room.CreateOn),
                modified = FormatDate(room.ModifiedOn)
            }
        };

        while (true)
        {
            var (entries, _) = await entryManager.GetEntriesAsync(room, room, from, count, [filterType], false, Guid.Empty, Guid.Empty, null, null, false, true, new OrderBy(SortedByType.CustomOrder, true));
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
                    created = FormatDate(entry.CreateOn),
                    modified = FormatDate(entry.ModifiedOn)
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

    // Mirrors FileEntry.CreateOnString/ModifiedOnString: an unset date must reach the script as null
    // so that the builder leaves the cell empty instead of printing 01/01/0001.
    private static string FormatDate(DateTime date)
    {
        return date.Equals(default) ? null : date.ConvertNumerals("G");
    }

    private record FolderIndex(int ChildFoldersCount, string Order);
}
