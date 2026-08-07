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

/// Identifies one audit report: how its file is named and which period it covers.
public sealed record AuditReportDescriptor(
    string NameFormat,
    string NameArg0,
    string NameArg1,
    DateTime? From,
    DateTime? To,
    CultureInfo Culture);

/// What a writer produced, mirrored back onto the task so the client can pick the file up.
public sealed record AuditReportResult(int FileId, string FileName, string FileUrl);

/// <summary>
/// Renders an audit report as a spreadsheet through the document builder and saves it into the
/// author's "My documents" folder.
/// </summary>
[Scope]
public class AuditXlsxReportWriter(
    TempPath tempPath,
    DocumentBuilderTask documentBuilderTask,
    ReportHeaderService reportHeaderService,
    ReportResultFileSaver fileSaver,
    FilesLinkUtility filesLinkUtility)
{
    private const string ScriptName = "AuditReport.docbuilder";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AuditReportResult> WriteAsync<T>(
        Guid userId,
        IEnumerable<T> events,
        AuditReportDescriptor descriptor,
        Func<int, Task> onProgressAsync,
        CancellationToken cancellationToken) where T : BaseEvent
    {
        var (headers, props) = GetColumns<T>();

        var header = await reportHeaderService.BuildAsync(descriptor.Culture);

        var dateFormat = header.LongDateFormat;

        var period = descriptor.From.HasValue && descriptor.To.HasValue
            ? $"{descriptor.From.Value.ConvertNumerals("d")} – {descriptor.To.Value.ConvertNumerals("d")}"
            : descriptor.From?.ConvertNumerals("d") ?? descriptor.To?.ConvertNumerals("d") ?? string.Empty;

        var scriptInputData = new
        {
            resources = new
            {
                company = Resource.AccountingReportCompany + ":",
                report = Resource.AccountingReportTitle + ":",
                period = Resource.AccountingReportPeriod + ":",
                dateGenerated = Resource.AccountingReportDateGenerated + ":",
                sheetName = GetSheetName(descriptor.NameFormat),
                dateGeneratedFormat = dateFormat
            },
            info = new
            {
                company = header.Company,
                report = GetReportTitle(descriptor.NameFormat),
                period,
                dateGenerated = header.DateGenerated
            },
            logoSrc = header.LogoSrc,
            themeColors = new
            {
                mainBgColor = header.MainBgColor,
                lightBgColor = header.LightBgColor,
                mainFontColor = header.MainFontColor
            },
            keys = headers,
            aligns = headers.Select(_ => "left").ToList()
        };

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(descriptor.NameFormat + ".xlsx", descriptor.NameArg0, descriptor.NameArg1);

        script = script
            .Replace("${inputData}", JsonSerializer.Serialize(scriptInputData, _jsonOptions))
            .Replace("${tempFileName}", tempFileName);

        var scriptParts = script.Split("${dataValues}");

        await using (var writer = new StreamWriter(scriptFilePath))
        {
            await writer.WriteAsync(scriptParts[0]);

            foreach (var @event in events)
            {
                var cells = new List<Cell>(props.Count);

                foreach (var prop in props)
                {
                    var value = prop.GetValue(@event);

                    if (prop.PropertyType == typeof(DateTime))
                    {
                        cells.Add(new Cell(((DateTime)value).ConvertNumerals("G"), dateFormat));
                    }
                    else
                    {
                        // force text format to stop formulas from executing in user-controlled values
                        cells.Add(new Cell(value?.ToString(), "@"));
                    }
                }

                await writer.WriteAsync(JsonSerializer.Serialize(cells, _jsonOptions) + ",");
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        var inputData = new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);

        await onProgressAsync(30);

        cancellationToken.ThrowIfCancellationRequested();

        var fileUri = await documentBuilderTask.BuildFileAsync(inputData, cancellationToken);

        await onProgressAsync(60);

        cancellationToken.ThrowIfCancellationRequested();

        var file = await fileSaver.SaveToMyDocumentsAsync(userId, outputFileName, new Uri(fileUri));

        if (System.IO.File.Exists(scriptFilePath))
        {
            System.IO.File.Delete(scriptFilePath);
        }

        return new AuditReportResult(file.Id, file.Title, filesLinkUtility.GetFileWebEditorUrl(file.Id));
    }

    private static (List<string> Headers, List<PropertyInfo> Props) GetColumns<T>() where T : BaseEvent
    {
        var columns = typeof(T).GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<EventAttribute>()
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute!.Order)
            .ToList();

        var headers = columns
            .Select(c => AuditReportResource.ResourceManager.GetString(c.Attribute!.Resource))
            .ToList();

        var props = columns
            .Select(x => x.Property)
            .ToList();

        return (headers, props);
    }

    private static string GetReportTitle(string reportNameFormat)
    {
        var name = reportNameFormat;

        var index = name.IndexOf('(');
        if (index > 0)
        {
            name = name[..index].Trim();
        }

        return name;
    }

    private static string GetSheetName(string reportNameFormat)
    {
        var name = GetReportTitle(reportNameFormat);

        return name.Length > 31 ? name[..31] : name;
    }

    private sealed record Cell(string Value, string Format, string Halign = null);
}

/// <summary>
/// Renders an audit report as a CSV file and uploads it. This path bypasses the document builder
/// entirely, so it produces no file entry id.
/// </summary>
[Scope]
public class AuditCsvReportWriter(
    CsvFileHelper csvFileHelper,
    CsvFileUploader csvFileUploader)
{
    public async Task<AuditReportResult> WriteAsync<T>(
        IEnumerable<T> events,
        AuditReportDescriptor descriptor,
        Func<int, Task> onProgressAsync) where T : BaseEvent
    {
        var reportName = string.Format(descriptor.NameFormat + ".csv", descriptor.NameArg0, descriptor.NameArg1);

        await onProgressAsync(50);

        await using var stream = csvFileHelper.CreateFile(events, new BaseEventMap<T>());
        var fileUrl = await csvFileUploader.UploadFile(stream, reportName);

        return new AuditReportResult(default, reportName, fileUrl);
    }
}
