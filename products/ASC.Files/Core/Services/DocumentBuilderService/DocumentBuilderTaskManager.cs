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

using ASC.Web.Core.Utility.Settings;

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Singletone(Additional = typeof(DocumentBuilderTaskManagerHelperExtention))]
public class DocumentBuilderTaskManager
{
    private readonly object _synchRoot = new object();

    private readonly DistributedTaskQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly SettingsManager _settingsManager;
    private readonly TenantWhiteLabelSettingsHelper _tenantWhiteLabelSettingsHelper;
    private readonly DisplayUserSettingsHelper _displayUserSettingsHelper;

    public DocumentBuilderTaskManager(
        IDistributedTaskQueueFactory queueFactory,
        IServiceProvider serviceProvider,
        CommonLinkUtility commonLinkUtility,
        SettingsManager settingsManager,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
        DisplayUserSettingsHelper displayUserSettingsHelper)
    {
        _queue = queueFactory.CreateQueue(GetType());
        _serviceProvider = serviceProvider;
        _commonLinkUtility = commonLinkUtility;
        _settingsManager = settingsManager;
        _tenantWhiteLabelSettingsHelper = tenantWhiteLabelSettingsHelper;
        _displayUserSettingsHelper = displayUserSettingsHelper;
    }

    public DistributedTaskProgress GetTask(string taskId)
    {
        return _queue.PeekTask<DistributedTaskProgress>(taskId);
    }

    public void TerminateTask(string taskId)
    {
        var task = GetTask(taskId);

        if (task != null)
        {
            _queue.DequeueTask(task.Id);
        }
    }

    private DistributedTaskProgress StartTask<T>(DocumentBuilderTask<T> newTask)
    {
        lock (_synchRoot)
        {
            var task = GetTask(newTask.Id);

            if (task != null && task.IsCompleted)
            {
                _queue.DequeueTask(task.Id);
                task = null;
            }

            if (task == null)
            {
                task = newTask;
                _queue.EnqueueTask(task);
            }

            return task;
        }
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

    public async Task<DistributedTaskProgress> StartRoomIndexExport<T>(Tenant tenant, UserInfo user, Folder<T> room, DataWrapper<T> items)
    {
        var tenantWhiteLabelSettings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        var customColorThemesSettings = await _settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var mainBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1);
        var lightBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08);
        var mainFontColor = ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1);

        var logoPath = await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoTypeEnum.LightSmall, false);
        var fullAbsoluteLogoPath = _commonLinkUtility.GetFullAbsolutePath(logoPath);

        var entries = new List<object>
        {
            new
            {
                index = (string)null,
                name = room.Title,
                url = "http://onlyoffice.com",
                type = "Room",
                pages = (string)null,
                size = (string)null,
                author = room.CreateByString,
                created = room.CreateOnString,
                modified = room.CreateOnString
            }
        };

        entries.AddRange(items.Entries.Select(item => new
        {
            index = "1.N",
            name = item.Title,
            url = "http://onlyoffice.com",
            type = item.FileEntryType == FileEntryType.Folder ? "Folder" : Path.GetExtension(item.Title),
            pages = item.FileEntryType == FileEntryType.Folder ? null : "1",
            size = item.FileEntryType == FileEntryType.Folder ? null : "1",
            author = item.CreateByString,
            created = item.CreateOnString,
            modified = item.ModifiedOnString
        }));

        var data = new
        {
            resources = new {
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

            themeColors = new {
                mainBgColor,
                lightBgColor,
                mainFontColor
            },

            info = new {
                company = tenantWhiteLabelSettings.LogoText,
                room = room.Title,
                exportAuthor = user.DisplayUserName(_displayUserSettingsHelper),
                dateGenerated = room.CreateOnString
            },

            data = entries
        };

        var outputFileName = $"{room.Title}_index.xlsx";

        var (script, tempFileName) = DocumentBuilderScriptHelper.GetScript(data);

        var task = new DocumentBuilderTask<T>(_serviceProvider);

        task.Init(tenant.Id, user.Id, script, tempFileName, outputFileName);

        return StartTask(task);
    }
}

public static class DocumentBuilderTaskManagerHelperExtention
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<DocumentBuilderTaskScope>();
    }
}