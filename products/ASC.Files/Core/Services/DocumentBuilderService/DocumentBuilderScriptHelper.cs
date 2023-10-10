// (c) Copyright Ascensio System SIA 2010-2022
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

[Scope]
public class DocumentBuilderScriptHelper
{
    private readonly UserManager _userManager;
    private readonly IDaoFactory _daoFactory;
    private readonly EntryManager _entryManager;
    private readonly SettingsManager _settingsManager;
    private readonly TenantWhiteLabelSettingsHelper _tenantWhiteLabelSettingsHelper;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly FilesLinkUtility _filesLinkUtility;
    private readonly FileUtility _fileUtility;
    private readonly DisplayUserSettingsHelper _displayUserSettingsHelper;
    private readonly PathProvider _pathProvider;

    public DocumentBuilderScriptHelper(
        UserManager userManager,
        IDaoFactory daoFactory,
        EntryManager entryManager,
        SettingsManager settingsManager,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
        CommonLinkUtility commonLinkUtility,
        FilesLinkUtility filesLinkUtility,
        FileUtility fileUtility,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        PathProvider pathProvider)
    {
        _userManager = userManager;
        _daoFactory = daoFactory;
        _entryManager = entryManager;
        _settingsManager = settingsManager;
        _tenantWhiteLabelSettingsHelper = tenantWhiteLabelSettingsHelper;
        _commonLinkUtility = commonLinkUtility;
        _filesLinkUtility = filesLinkUtility;
        _fileUtility = fileUtility;
        _displayUserSettingsHelper = displayUserSettingsHelper;
        _pathProvider = pathProvider;
    }

    private async Task<(object data, string outputFileName)> GetDataObject<T>(Guid userId, T roomId)
    {
        var user = await _userManager.GetUsersAsync(userId);

        var folderDao = _daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(roomId);

        var outputFileName = GetOutputFileName(room.Title);

        //TODO: think about loop by N
        var (entries, total) = await _entryManager.GetEntriesAsync(room, 0, int.MaxValue, FilterType.None, false, Guid.Empty, string.Empty, false, true, null);

        var customColorThemesSettings = await _settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var mainBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1);
        var lightBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08);
        var mainFontColor = ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1);

        var tenantWhiteLabelSettings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoPath = await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoTypeEnum.LightSmall, false);

        var fullAbsoluteLogoPath = _commonLinkUtility.GetFullAbsolutePath(logoPath);

        var items = new List<object>
        {
            new
            {
                index = (string)null,
                name = room.Title,
                url = _commonLinkUtility.GetFullAbsolutePath(_pathProvider.GetRoomsUrl(room.Id.ToString())),
                type = FilesCommonResource.RoomIndex_Room,
                pages = (string)null,
                size = (string)null,
                author = room.CreateByString,
                created = room.CreateOnString,
                modified = room.CreateOnString
            }
        };


        items.AddRange(entries.OfType<FileEntry<T>>().Select(item => new
        {
            index = "1.N",
            name = item.Title,
            url = _commonLinkUtility.GetFullAbsolutePath(item.FileEntryType == FileEntryType.Folder ? _pathProvider.GetRoomsUrl(item.Id.ToString()) : _filesLinkUtility.GetFileWebPreviewUrl(_fileUtility, item.Title, item.Id)),
            type = item.FileEntryType == FileEntryType.Folder ? FilesCommonResource.RoomIndex_Folder : Path.GetExtension(item.Title),
            pages = item.FileEntryType == FileEntryType.Folder ? null : "1",
            size = item.FileEntryType == FileEntryType.Folder ? null : Math.Round(((File<T>)item).ContentLength/1024d/1024d, 2).ToString(CultureInfo.InvariantCulture),
            author = item.CreateByString,
            created = item.CreateOnString,
            modified = item.ModifiedOnString
        }));

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
                pages = FilesCommonResource.RoomIndex_Pages,
                size = FilesCommonResource.RoomIndex_Size,
                author = FilesCommonResource.RoomIndex_Author,
                created = FilesCommonResource.RoomIndex_Created,
                modified = FilesCommonResource.RoomIndex_Modified,
                total = FilesCommonResource.RoomIndex_Total,
                sheetName = FilesCommonResource.RoomIndex_SheetName,
                numberFormat = $"0.00",
                dateFormat = $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}"
            },

            logoSrc = fullAbsoluteLogoPath,

            themeColors = new
            {
                mainBgColor,
                lightBgColor,
                mainFontColor
            },

            info = new
            {
                company = tenantWhiteLabelSettings.LogoText,
                room = room.Title,
                exportAuthor = user.DisplayUserName(_displayUserSettingsHelper),
                dateGenerated = room.CreateOnString
            },

            data = items
        };

        return (data, outputFileName);
    }

    private static int[] ConvertHtmlColorToRgb(string color, double opacity)
    {
        if (color[0] != '#' || color.Length != 7 || opacity < 0 || opacity > 1)
        {
            throw new ArgumentException();
        }

        return new[]
        {
            ApplyOpacity(255, Convert.ToInt32(color.Substring(1, 2), 16), opacity),
            ApplyOpacity(255, Convert.ToInt32(color.Substring(3, 2), 16), opacity),
            ApplyOpacity(255, Convert.ToInt32(color.Substring(5, 2), 16), opacity),
        };

        static int ApplyOpacity(int background, int overlay, double opacity)
        {
            return (int)((1 - opacity) * background + opacity * overlay);
        }
    }

    private static string GetTempFileName()
    {
        return $"temp{DateTime.UtcNow.Ticks}.xlsx";
    }

    private static string GetOutputFileName(string title)
    {
        return $"{title}_{FilesCommonResource.RoomIndex_Index.ToLowerInvariant()}.xlsx";
    }

    private static string ReadTemplateFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var templateNamespace = typeof(DocumentBuilderScriptHelper).Namespace;

        var resourceName = $"{templateNamespace}.ScriptTemplates.RoomIndex.docbuilder";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            return null;
        }

        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }

    public async Task<(string script, string tempFileName, string outputFileName)> GetScript<T>(Guid userId, T roomId)
    {
        var script = ReadTemplateFromEmbeddedResource() ?? throw new Exception("Template not found");

        var tempFileName = GetTempFileName();

        var (data, outputFileName) = await GetDataObject(userId, roomId);

        script = script.Replace("${tempFileName}", tempFileName)
                     .Replace("${inputData}", JsonConvert.SerializeObject(data));

        return (script, tempFileName, outputFileName);
    }
}