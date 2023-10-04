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

    public DocumentBuilderTaskManager(
        IDistributedTaskQueueFactory queueFactory,
        IServiceProvider serviceProvider,
        CommonLinkUtility commonLinkUtility,
        SettingsManager settingsManager,
        TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper)
    {
        _queue = queueFactory.CreateQueue(GetType());
        _serviceProvider = serviceProvider;
        _commonLinkUtility = commonLinkUtility;
        _settingsManager = settingsManager;
        _tenantWhiteLabelSettingsHelper = tenantWhiteLabelSettingsHelper;
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

    public async Task<DistributedTaskProgress> StartRoomIndexExport<T>(int tenantId, Guid userId, Folder<T> room)
    {
        var tenantWhiteLabelSettings = await _settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        var customColorThemesSettings = await _settingsManager.LoadAsync<CustomColorThemesSettings>();

        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var mainBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1);
        var lightBgColor = ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08);
        var mainFontColor = ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1);

        var logoPath = await _tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoTypeEnum.LightSmall, false);
        var fullAbsoluteLogoPath = _commonLinkUtility.GetFullAbsolutePath(logoPath);

        var data = new
        {
            resources = new {
                company = "Company:",
                room = "Room:",
                exportAuthor = "Export Author:",
                dateGenerated = "Date Generated:",
                index = "Index",
                name = "Name",
                type = "Type",
                pages = "Pages",
                size = "Size (MB)",
                author = "Author",
                created = "Created",
                modified = "Modified",
                total = "Total",
                sheetName = "Room index",
                numberFormat = "0.00",
                dateFormat = "dd.MM.yyyy hh:mm:ss"
            },

            //logoSrc = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAPsAAABFCAYAAACMspCjAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAx6SURBVHgB7ZxdTFzHFcfPAjHEXRzFjttdGjduDXYrVMWQVKocQx7ijxgreYhd6FNJIFKlxil+qRQXS1UVqLH6EijpU3Ct9sk4WGosQ+y2L8bQD6XgVOXBQKtWtgy1lUQ2YIPLR+Y/uQcNw727y3IXY8/5SVcse+fOnTsz/3POnLkQmZyamiNBEB56skgQBCcQsQuCI4jYBcERROyC4AgidkFwBBG7IDiCiF0QHEHELgiOIGIXBEcQsQuCI4jYBcERROyC4AgidkFwBBG7IDiCiF0QHEHELgiOIGIXBEcQsQuCI4jYBcERROyC4AgidkFwhBy6T1y59X/64NokXbszQ3em52jruhx6+clHadtj961JgvBQE1npfyXdc2OKem/eo8Hb077nn8jNopc25dGOjbkkCEJ4rIjY4bn/ODJJfxqd0p9TgUW/bd0jtCFXVhuCsFwyKvZ0RG4D0esQf9OjInpBWAYZETvW4wjVcYTJjo1rRPSCkCahi/2XA2OB6/Gw2BXPparNa1MuPzIyon/G43Fa7YyNjdH4+DhFo1HKz8+n1UCY/dff37/ou8LCwhV5VvRtd3e3fh48C46SkhJyhdBT35vWZmdU7GtzIvREXnbSchjYtrY26urs1OJhSkpLqWLfPqrYv39B+UNvvDE/EVGmtbXV97x5zu+7vr4+evPQIf25sqqK6urqFtSDiXbwwAH9uba2lmrUATrPnaNO1VZTDLFYjPardlZWVlLUEAPKNTY0kB+oD/Waz2PT09u7oJ2t7767aNL7tQfsq6igo0ePLrg+6B5+oF0QNowZxgXjBNCH9fX1GTPI7adOUXNzs743+nV0dFTfm5/HBUIX+/e/vpZ2FeTRn29Oqcz7PfpkapbCACLfFc+jF2K5+nMiIChMKgwoBhcDCi6riduvJikOnGOh2egyEHEaVr9UTVqIs729XU+w8vLyBfXAAAFMar4/hAthAUx6TEYIofviRV3+nBIeBLkSkQkEcOSttxaInMWJfkW/LZcfKwNY4Y0JgOE4oZ4TRvBYU5PuszBBuyF007iCUfW96QgedjKyqa0z6WrPHAcEf/ba3bRFj7peUGE7tuKSiZxhoUMc8LgxQyRt771HJ06c0CIqLCoKnFg4b3v3VMGk4nDRrAfesssTNU86tIeFjigA0QCDyVhdXa2fpbGx0bc973d0JDQCS/VcEAULHX2DdhapfmKGhoYWXQNDBAOVLjCQODBuMHyl6pmiVljf5xloGB20J+iZuRxAORz8PDDCJjGrDjwb6seBzzyHgox+Km3ipQPwK2ee5/Zmioy+wfLJxBBtjAxTfXEJ/ePWeu3tr6QY4m9TGfiXVDJu89pJGrp5lmZmylRrk08oCIoHGxPVHtDa11/XnYvBhPe1xV6mfmfvj7rscD8VMFHhvbSHVPXAw0PE7NXh1dizsdDxuyl0gLZjgsI4LSfaSBVM3i6jPfU+RsJvMkLoYUQdGC8sDdAn3BccpU1MTOi1PcYW3yHkN8cG38FQoI/QFl4inD9/ft4QDQ0PJ+y/5nfe0Q4AEVUkEtHXoT78NCOrcVXvARWFoAzahHthPtnGGvMHxhPleNlSVlY23698HvXiPO7lt/QLi4yIfeRWP/VdO0mjty/r36O5ajKs204/LHyNrk9toN4bwZl6U+QDI7+j9ivv073pcfrrf1qpaOOL+og/FjxgF9VAMWaoaIIBx+BAQBg424uwwDAQMAbRNJJHHL5jAFnkvKyo9by66YUQvvuBCY22gKHBwUWT1Q6rbdFhgnGCLagM0230XW3AEscPuw3pJhfh3XHtoBE9QOjxggJqOnZsfhwQDSHSwffcHxA62tGhooKYIUpcg3rxzA1vv62fK5EBP60cgLmU4OgKc6FJfa+fT9UJ8ZuGD4YC42waKbTRXjqMezkKPm8aCL5XkTIg6TiZZIQu9nMDdfMiZ8anRpV3/lAfEOvBTa8pQX+Zzl69S1cnZnQZ7KXvVmv9NXSDBq6fpL9d+VCL3ITreGr9Ttq1rdH3/rwGS+RpirZunf88oiZIkTExcX2V6nx4fXzGz5olTHwThM8YPNSDyQJqamrmJ6MpkqD2mt8P+oTQEIOJnWyDgE0Roz6E/n5wiA7BxZbgqe02cJIwHUwjwcaw6fjxBQaXozPkMvCsg8oIwqiinNlu8xosgSBGCAw/YVzRRrvf8b0Z7XF0peeD4RjsCAfXoQxn+hHNoR/tucPX4zzKmZEA7oV6O7u6HgyxFyiv+9mdYZqa9k98mKJ/EV668IuJiWjgL8MnFxkKm9ycaELPngpsXfVnnwQNBgSD1KIEegohuLXWS5WYl4Rr8YRuD+5SyVeTxyZZ+MzJNbNNQXC5pXplTFDzHmElEoc94+O3dEC4fdlbi89HR9u3B9aF50b4DIFfVIYCYntVGWJ8t0DcPrkHCBnRlekYzG08fLajGxhO06nY4Dyua7B2VVAfwv5MELrYS558VQv5v59eUmH4aRqb8s/esuhTBSIvjn+PimMHaU1ONLCcnsxqEowZgrYxw1qETH7Au5/2LDU8QTQafM9EoB4W+z5rWVFo3Bv38VtPwmsxfiL6lfJYicS1U60RU03QcT1oi9/yJgiEvWEIHPfEvbmfxrz3DZJe5xnsVNrLnhrbr4dUfgDhf/mFC5QKfB8YCswJiBljVqCWE37yTJS0RF28128SVx49P825loyMvIqGNXpx/CBVlqqtpy1HKD83/Uwtrv3u5jepsuSUNiSJhA5KvbUvOjNon5nX9RisRBPkpyoJBDiJFjYFXmIm0T2GjdA9aF0fFmWGh2vz8gQrCfcBe1pOtI37GG58x1EK96Gdm0gExh1Rll9OY9G92Jh424+8jYelAX7CeNhjg7YNq4RgEDAEUS9/Yx/Lif4SkdH3TiN3x2nbzfy0RI+EXkVxM1V9q42+/Wmccm+nth9arjwZW0skZOyBRHKHQ65k4TkMByy33vP2tkfCRE84rw2ctTdBqMdrfQgxk9sygJ8XoC0IMc3+Q7SEdbQNZ8jNY6mgXnhLJFX5OTGWwEy6AtSPvin12so/u5ZolP3eDER+wzYuuH/cW09zxIjkoF3GZKsqi4RqUF8UJTmfCTKSjYfIs7vPUNalDopMTtDc41+hbz67l7Y+00zXc0ap/+pvaCRgbQ6Rl6gEXsEjhV4dP9N1gJln9tDM7h/Q3Ppgo6EzpcriIkTjt9UgFFhlZDvZ2+usbEC23oS3gxIBoT63Y8ei61JJUiHZBLHAq0HYWE/qN7yMtmKiHc7QdowNQn7uO4gHB2fX8R36stTyYnaCDgS9QQdMgwFhQSj8JqK57aRzJyqhyQYP57VnVZlvrGt5/LgcL7e4HMSE/sUWF5KbaDeeA4LFOazD/caoQSXxsPzCOMDooQ94u4yjMSQHtXdWn1H/ZSuKRNuQsEPf1B0+rMdwxHuJB+dQf5dKxOE82oAoU7+0pA7UW/788xQ2oYs95/e/puyeMwu+i3z2P8r5w2+J1LFJCbZg9xEaU8vVPiV6Tsh97fGd9NSGMl+RM9l/v6CP6Z2v0MzLPwpsQ8x7mQaDj0lkZqM5C5vqfjV7u6AlQRhgIulsrppY/YYQMEH0/rv1umwmiXnZehgftAceVIfS6oBQlruexDNBGCwOiAXJtlYlSr8xgVhxX50gUyLEZ5Q/bmXeUQ5AYKZx4Hvie+RgGDYsdsiM/kabGr2oBtfWGW/8YRwwXnjjDwk+tAd75+xgGHY6mIO/UO2Ggcn3jJJ9vqWlZf48nq3WKxM2of8hTM4HSuyXziQtp7102QGaK9iif8/618eU9dF5Leak1xY/R9PVP6dUgfdAsgcTdaVEky6rra3cHvAg/CHRcoCXhQF5WN+VD92zTyuPCxFnKy+ercQbBHvpubwv6d9tL24zlxel2e/spRnl1bEsWAoQzWoXObPa2vog9Z2QmIys2SHG6cqf6PV1dncHZQ30qlDefwsuFZHPlL1Cs4gCPMMgCMLSyei78Vr0ytNH9lRT5J89et0eJPpF14rIhRUGIfxq+R8CmWDF/+Fk1kcXEop+dsvTaj2/l2af3UOCIITHioudyRro+SLr/u+PtRef++oWmsa22jeeJkEQwue+iV0QhJVF/nOjIDiCiF0QHEHELgiOIGIXBEcQsQuCI4jYBcERROyC4AgidkFwBBG7IDiCiF0QHEHELgiOIGIXBEcQsQuCI4jYBcERROyC4AgidkFwBBG7IDiCiF0QHEHELgiOIGIXBEcQsQuCI4jYBcERPgd78G2kb6u83gAAAABJRU5ErkJggg==",
            logoSrc = fullAbsoluteLogoPath,

            themeColors = new {
                mainBgColor,
                lightBgColor,
                mainFontColor
            },

            info = new {
                company = "ONLYOFFICE",
                room = room.Title,
                exportAuthor = "John Smith",
                dateGenerated = room.CreateOnString
            },

            data = new[]
            {
                new {
                    index = (string)null,
                    name = "ONLYOFFICE",
                    url = "http://onlyoffice.com",
                    type = "Room",
                    pages = (string)null,
                    size = (string)null,
                    author = "John Smith",
                    created = room.CreateOnString,
                    modified = room.CreateOnString
                },
                new {
                    index = "1",
                    name = "Docspace",
                    url = "http://onlyoffice.com",
                    type = "Folder",
                    pages = (string)null,
                    size = (string)null,
                    author = "John Smith",
                    created = room.CreateOnString,
                    modified = room.CreateOnString
                },
                new {
                    index = "1.1",
                    name = "document1",
                    url = "http://onlyoffice.com",
                    type = ".docx",
                    pages = "1",
                    size = "0.2",
                    author = "John Smith",
                    created = room.CreateOnString,
                    modified = room.CreateOnString
                },
                new {
                    index = "1.2",
                    name = "document2",
                    url = "http://onlyoffice.com",
                    type = ".pdf",
                    pages = "20",
                    size = "5.47",
                    author = "John Smith",
                    created = room.CreateOnString,
                    modified = room.CreateOnString
                },
                new {
                    index = "2",
                    name = "picture1",
                    url = "http://onlyoffice.com",
                    type = ".png",
                    pages = "1",
                    size = "1.23",
                    author = "John Smith",
                    created = room.CreateOnString,
                    modified = room.CreateOnString
                }
            }
        };

        var outputFileName = $"{room.Title}_index.xlsx";

        var (script, tempFileName) = DocumentBuilderScriptHelper.GetScript(data);

        var task = new DocumentBuilderTask<T>(_serviceProvider);

        task.Init(tenantId, userId, script, tempFileName, outputFileName);

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