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
/// Collects the filled-in form results of a room and shapes them into the input data of the
/// form-filling report script.
/// </summary>
[Scope]
public class FormFillingReportBuilder(
    UserManager userManager,
    IDaoFactory daoFactory,
    SettingsManager settingsManager,
    TenantManager tenantManager,
    FormFillingReportCreator formFillingReportCreator,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    TenantUtil tenantUtil)
{
    public async Task<FormFillingReportBuildResult> BuildAsync(Guid userId, int roomId, int originalFormId, int originalFormVersion)
    {
        var user = await userManager.GetUsersAsync(userId);

        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        // A report version is defined by its field-key set: pull every submission of the form across all
        // versions, take the current version's key set (falling back to the latest submission), and keep only
        // the submissions that share it. An edit that keeps the same fields therefore stays in one report.
        var allSubmissions = (await formFillingReportCreator.GetFormFillingResults(roomId, originalFormId)).ToList();

        var currentSubmission = allSubmissions.LastOrDefault(s => s.OriginalFormVersion == originalFormVersion)
                                ?? allSubmissions.LastOrDefault();
        var currentKeySet = currentSubmission != null ? FormFillingReportCreator.KeySetSignature(currentSubmission) : "";

        var previousSubmission = allSubmissions
            .Where(s => s.OriginalFormVersion < originalFormVersion)
            .OrderBy(s => s.OriginalFormVersion)
            .LastOrDefault();
        var keySetChanged = previousSubmission != null && FormFillingReportCreator.KeySetSignature(previousSubmission) != currentKeySet;

        var formFillingResults = allSubmissions.Where(s => FormFillingReportCreator.KeySetSignature(s) == currentKeySet).ToList();

        var data = await BuildReportDataAsync(originalFormId, formFillingResults);

        return new FormFillingReportBuildResult(data, keySetChanged);
    }

    /// <summary>
    /// Assembles the report input data (columns, rows, theme, sheet name) from an explicit set of submissions.
    /// Shared by the normal key-set-filtered build and by form recovery, which builds one report per key set.
    /// </summary>
    public async Task<object> BuildReportDataAsync(int originalFormId, IReadOnlyCollection<DbFormsItemDataSearch> formFillingResults)
    {
        var fileDao = daoFactory.GetFileDao<int>();

        var tenantCulture = tenantManager.GetCurrentTenant().GetCulture();
        CultureInfo.CurrentCulture = tenantCulture;
        CultureInfo.CurrentUICulture = tenantCulture;

        var keys = new List<string>();
        var values = new List<List<object>>();
        if (formFillingResults.Any())
        {
            var formsData = formFillingResults.FirstOrDefault().FormsData;
            if (formsData.Any())
            {
                keys.Add(FilesCommonResource.FormNumber);
                keys.AddRange(formsData.Skip(1).Where(d => d.Type != "picture" && d.Type != "signature").Select(field => field.Key));
                keys.Add(FilesCommonResource.Date);
                keys.Add(FilesCommonResource.LinkToForm);

                var dateFormat = DocumentBuilderScriptHelper.GetLongDateTimeFormat(tenantCulture);

                foreach (var formFillingRes in formFillingResults)
                {
                    var t = new List<object>();
                    foreach (var field in formFillingRes.FormsData)
                    {
                        if (field.Type is "picture" or "signature")
                        {
                            continue;
                        }
                        t.Add(new
                        {
                            format = field.Type == "dateTime" ? $"{tenantCulture.DateTimeFormat.ShortDatePattern}" : "@",
                            value = field.Value,
                            url = ""
                        });
                    }
                    t.Add(new
                    {
                        format = dateFormat,
                        value = tenantUtil.DateTimeFromUtc(formFillingRes.CreateOn).ConvertNumerals("G"),
                        url = ""
                    });
                    var formsDataFile = await fileDao.GetFileAsync(formFillingRes.Id);
                    if (formsDataFile != null)
                    {
                        var resultUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, formsDataFile.Title, formsDataFile.Id, formsDataFile.Version));
                        t.Add(new
                        {
                            format = "@",
                            value = FilesCommonResource.OpenForm,
                            url = resultUrl
                        });
                    }
                    values.Add(t);
                }
            }
        }

        var properties = await daoFactory.GetFileDao<int>().GetProperties(originalFormId);
        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();
        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var sheetName = properties?.FormFilling?.Title;
        if (string.IsNullOrEmpty(sheetName))
        {
            var form = await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId);
            sheetName = Path.GetFileNameWithoutExtension(form?.Title ?? string.Empty);
        }

        var data = new
        {
            resources = new
            {
                sheetName
            },

            themeColors = new
            {
                mainBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
                lightBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
                mainFontColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1)
            },

            data = new
            {
                keys,
                values
            }
        };

        return data;
    }
}

/// <summary>
/// The assembled report input data plus whether the form's field-key set changed versus the previous
/// version - the writer bumps to a new report version only when the key set actually changed.
/// </summary>
public record FormFillingReportBuildResult(object Data, bool KeySetChanged);

/// <summary>
/// Stores the generated form-filling report. Unlike the other report tasks this one updates an
/// existing results file - bumping its version or replacing the current one in place.
/// </summary>
[Scope]
public class FormFillingResultFileWriter(
    IDaoFactory daoFactory,
    IHttpClientFactory clientFactory,
    TenantUtil tenantUtil,
    SocketManager socketManager,
    FilesMessageService filesMessageService)
{
    public async Task<File<int>> SaveAsync(int originalFormId, bool isNewFile, Uri fileUri, Dictionary<string, StringValues> headers, bool keySetChanged)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var origProperties = await fileDao.GetProperties(originalFormId);
        var resultFile = await fileDao.GetFileAsync(origProperties.FormFilling.ResultsFileID);

        using var request = new HttpRequestMessage { RequestUri = fileUri };

#pragma warning disable CA2000
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();

        // New report version only when a form edit actually changed the field-key set; edits that keep the
        // same fields (adding a word, moving fields) keep filling the current version in place. Clearing of
        // IsVersionChanged is decoupled from that decision - it is always reset once it was set.
        if (origProperties.FormFilling.IsVersionChanged && keySetChanged)
        {
            resultFile.Version++;
            resultFile.VersionGroup++;
            resultFile.ContentLength = stream.Length;

            resultFile = await fileDao.SaveFormFileAsync(resultFile, stream, false);
        }
        else
        {
            resultFile.CreateOn = tenantUtil.DateTimeNow();
            resultFile.ContentLength = stream.Length;

            resultFile = await fileDao.ReplaceFileVersionAsync(resultFile, stream);
        }

        if (origProperties.FormFilling.IsVersionChanged)
        {
            origProperties.FormFilling.IsVersionChanged = false;
            await fileDao.SaveProperties(originalFormId, origProperties);
        }

        if (resultFile.Id != origProperties.FormFilling.ResultsFileID)
        {
            origProperties.FormFilling.ResultsFileID = resultFile.Id;
            await fileDao.SaveProperties(originalFormId, origProperties);
        }

        var xlsxProperties = new EntryProperties<int>
        {
            FormFilling = new FormFillingProperties<int>
            {
                StartFilling = false,
                OriginalFormId = origProperties.FormFilling.OriginalFormId,
                OriginalFormVersion = origProperties.FormFilling.OriginalFormVersion,
                RoomId = origProperties.FormFilling.RoomId,
                ResultsFolderId = origProperties.FormFilling.ResultsFolderId,
                ResultsFileID = resultFile.Id
            }
        };
        await fileDao.SaveProperties(resultFile.Id, xlsxProperties);

        if (isNewFile)
        {
            await socketManager.CreateFileAsync(resultFile);
        }
        else
        {
            await socketManager.UpdateFileAsync(resultFile);
        }

        await filesMessageService.SendAsync(isNewFile ? MessageAction.FileCreated : MessageAction.FileUpdated, resultFile, headers, resultFile.Title);

        return resultFile;
    }
}
