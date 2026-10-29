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
    TenantManager tenantManager,
    FormFillingReportCreator formFillingReportCreator,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    TenantUtil tenantUtil)
{
    // An Excel sheet name holds at most 31 characters.
    private const int SheetNameLimit = 31;

    public async Task<object> BuildAsync(Guid userId, int roomId, int originalFormId)
    {
        var user = await userManager.GetUsersAsync(userId);
        var fileDao = daoFactory.GetFileDao<int>();

        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        // Failing here keeps the report already saved instead of replacing it with an empty one.
        var reportSheets = await formFillingReportCreator.GetReportSheetsAsync(roomId, originalFormId)
            ?? throw new InvalidOperationException($"The submissions of form {originalFormId} cannot be read from the search index");

        var tenantCulture = tenantManager.GetCurrentTenant().GetCulture();
        CultureInfo.CurrentCulture = tenantCulture;
        CultureInfo.CurrentUICulture = tenantCulture;

        var submissionIds = reportSheets.SelectMany(sheet => sheet.Submissions).Select(submission => submission.Id).ToList();
        var formsDataFiles = submissionIds.Count > 0
            ? await fileDao.GetFilesAsync(submissionIds).ToDictionaryAsync(f => f.Id)
            : [];

        var properties = await fileDao.GetProperties(originalFormId);

        var title = properties?.FormFilling?.Title;
        if (string.IsNullOrEmpty(title))
        {
            var form = await fileDao.GetFileAsync(originalFormId);
            title = Path.GetFileNameWithoutExtension(form?.Title ?? string.Empty);
        }

        var dateFormat = DocumentBuilderScriptHelper.GetLongDateTimeFormat(tenantCulture);
        var sheets = reportSheets
            .Select(sheet => BuildSheet(sheet, reportSheets.Count == 1 ? title : GetSheetName(sheet.Versions), formsDataFiles, tenantCulture, dateFormat))
            .ToList();

        if (sheets.Count == 0)
        {
            sheets.Add(new { name = title, keys = new List<string>(), formats = new List<string>(), values = new List<List<string>>(), links = new List<string>() });
        }

        return new
        {
            linkText = FilesCommonResource.OpenForm,
            sheets
        };
    }

    // Columns are the fields of the newest version, then those only older versions had; a field repeated in a form
    // (as radio buttons are) keeps a column per occurrence. The script writes the values as they are and formats each
    // column, the form links are passed apart.
    private object BuildSheet(FormReportSheet sheet, string name, Dictionary<int, File<int>> formsDataFiles, CultureInfo culture, string dateFormat)
    {
        var rows = sheet.Submissions
            .Select(submission => (Submission: submission, Fields: GetFields(sheet, submission)))
            .ToList();

        var columnTypes = new Dictionary<(string Key, int Occurrence), string>();
        foreach (var (_, fields) in rows.OrderByDescending(row => row.Submission.OriginalFormVersion))
        {
            foreach (var (column, field) in fields)
            {
                columnTypes.TryAdd(column, field.Type);
            }
        }

        var columns = columnTypes.Keys.ToList();

        var keys = new List<string> { FilesCommonResource.FormNumber };
        keys.AddRange(columns.Select(column => column.Key));
        keys.Add(FilesCommonResource.Date);
        keys.Add(FilesCommonResource.LinkToForm);

        var formats = new List<string> { "@" };
        formats.AddRange(columns.Select(column => columnTypes[column] == "dateTime" ? culture.DateTimeFormat.ShortDatePattern : "@"));
        formats.Add(dateFormat);

        var values = new List<List<string>>(rows.Count);
        var links = new List<string>(rows.Count);
        foreach (var (submission, fields) in rows)
        {
            var row = new List<string>(columns.Count + 2) { submission.FormsData?.FirstOrDefault()?.Value };
            row.AddRange(columns.Select(column => fields.TryGetValue(column, out var field) ? field.Value : null));
            row.Add(tenantUtil.DateTimeFromUtc(submission.CreateOn).ConvertNumerals("G"));
            values.Add(row);

            links.Add(formsDataFiles.TryGetValue(submission.Id, out var formsDataFile)
                ? commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, formsDataFile.Title, formsDataFile.Id, formsDataFile.Version))
                : null);
        }

        return new { name, keys, formats, values, links };
    }

    private static Dictionary<(string Key, int Occurrence), FormsItemData> GetFields(FormReportSheet sheet, DbFormsItemDataSearch submission)
    {
        var fields = new Dictionary<(string Key, int Occurrence), FormsItemData>();
        foreach (var field in (submission.FormsData ?? []).Skip(1).Where(FormFillingReportCreator.IsExportableField))
        {
            var key = sheet.MapKey(submission.OriginalFormVersion, field.Key);
            var occurrence = 0;
            while (!fields.TryAdd((key, occurrence), field))
            {
                occurrence++;
            }
        }

        return fields;
    }

    // Consecutive versions are written as a range: "v1-v3, v5".
    internal static string GetSheetName(IReadOnlyList<int> versions)
    {
        var runs = new List<string>();
        for (var i = 0; i < versions.Count; i++)
        {
            var first = versions[i];
            while (i + 1 < versions.Count && versions[i + 1] == versions[i] + 1)
            {
                i++;
            }

            runs.Add(first == versions[i] ? $"v{first}" : $"v{first}-v{versions[i]}");
        }

        var name = string.Join(", ", runs);

        return name.Length <= SheetNameLimit ? name : name[..(SheetNameLimit - 3)] + "...";
    }
}

/// <summary>
/// Stores the generated form-filling report. Unlike the other report tasks this one updates an
/// existing results file, replacing its current version in place.
/// </summary>
[Scope]
public class FormFillingResultFileWriter(
    IDaoFactory daoFactory,
    IHttpClientFactory clientFactory,
    TenantUtil tenantUtil,
    SocketManager socketManager,
    FilesMessageService filesMessageService)
{
    public async Task<File<int>> SaveAsync(int originalFormId, bool isNewFile, Uri fileUri, Dictionary<string, StringValues> headers)
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

        // A new form version gets a sheet of its own in the same report rather than a new report version.
        resultFile.CreateOn = tenantUtil.DateTimeNow();
        resultFile.ContentLength = stream.Length;

        resultFile = await fileDao.ReplaceFileVersionAsync(resultFile, stream);

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
