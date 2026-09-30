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

/// Identifies one audit report: how its file is named, which period it covers, which columns it shows and how
/// many events the period holds.
public sealed record AuditReportDescriptor(
    string NameFormat,
    string NameArg0,
    string NameArg1,
    DateTime? From,
    DateTime? To,
    CultureInfo Culture,
    IReadOnlyList<AuditReportColumn> Columns,
    int TotalCount);

public sealed record AuditReportColumn(string ResourceKey, PropertyInfo Property);

public static class AuditReportColumns
{
    private static readonly string[] _excludedFromFolderHistory =
    [
        nameof(BaseEvent.Country),
        nameof(BaseEvent.City),
        nameof(BaseEvent.Page),
        nameof(AuditEvent.Action),
        nameof(AuditEvent.Context),
    ];

    private static readonly string[] _clientColumns =
    [
        nameof(BaseEvent.IP),
        nameof(BaseEvent.Browser),
        nameof(BaseEvent.Platform)
    ];

    public static List<AuditReportColumn> Resolve<T>(AuditReportKind kind, bool isDocSpaceAdmin) where T : BaseEvent
    {
        var columns = typeof(T).GetProperties()
            .Select(p => new { Property = p, Attribute = p.GetCustomAttribute<EventAttribute>() })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute.Order)
            .Select(x => new AuditReportColumn(x.Attribute.Resource, x.Property));

        if (kind == AuditReportKind.FolderHistory)
        {
            columns = columns.Where(c => !_excludedFromFolderHistory.Contains(c.Property.Name));

            if (!isDocSpaceAdmin)
            {
                columns = columns.Where(c => !_clientColumns.Contains(c.Property.Name));
            }
        }

        return [.. columns];
    }
}

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
    FilesLinkUtility filesLinkUtility,
    IConfiguration configuration)
{
    private const string ScriptName = "AuditReport.docbuilder";

    // The events are written into the script, and the document server refuses to download a script larger than
    // its FileConverter.converter.maxDownloadBytes (100 MB by default). An audit row takes about 800 bytes of the
    // script as it is written now, so the default keeps a period well under that limit; a sheet could not hold
    // more than about a million rows in any case.
    private const int DefaultMaxRows = 100_000;
    private const int SheetMaxRows = 1_000_000;

    /// <summary>
    /// How many events a workbook holds at most. The events come newest first, so a longer period keeps its most
    /// recent events and the report header says how many were left out; the CSV format has no such limit.
    /// </summary>
    public int MaxRows { get; } = Math.Clamp(
        int.TryParse(configuration["files:audit-report:xlsx-max-rows"], out var maxRows) ? maxRows : DefaultMaxRows,
        1,
        SheetMaxRows);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AuditReportResult> WriteAsync<T>(
        Guid userId,
        IAsyncEnumerable<IReadOnlyList<T>> batches,
        AuditReportDescriptor descriptor,
        Func<int, Task> onProgressAsync,
        CancellationToken cancellationToken) where T : BaseEvent
    {
        var headers = descriptor.Columns
            .Select(c => AuditReportResource.ResourceManager.GetString(c.ResourceKey))
            .ToList();

        var props = descriptor.Columns
            .Select(c => c.Property)
            .ToList();

        var header = await reportHeaderService.BuildAsync(descriptor.Culture);

        var dateFormat = header.LongDateFormat;

        var rowLimitNote = descriptor.TotalCount > MaxRows
            ? string.Format(descriptor.Culture, AuditReportResource.ReportRowLimitNote,
                MaxRows.ToString("N0", descriptor.Culture), descriptor.TotalCount.ToString("N0", descriptor.Culture))
            : null;

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
                rowLimit = AuditReportResource.ReportRowLimitLabel + ":",
                sheetName = GetSheetName(descriptor.NameFormat),
                dateGeneratedFormat = dateFormat
            },
            info = new
            {
                company = header.Company,
                report = GetReportTitle(descriptor.NameFormat),
                period,
                dateGenerated = header.DateGenerated,
                rowLimit = rowLimitNote
            },
            logoSrc = header.LogoSrc,
            logoWidthMm = header.LogoWidthMm,
            logoHeightMm = header.LogoHeightMm,
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

        // The page column holds a URL, which is long enough to wrap onto a second line in a cell that
        // wraps. Resolved once per column rather than for every cell of every event.
        var wraps = props
            .Select(p => p.Name == nameof(BaseEvent.Page) ? false : (bool?)null)
            .ToList();

        try
        {
            await using (var writer = new StreamWriter(scriptFilePath))
            {
                await writer.WriteAsync(scriptParts[0]);

                var written = 0;

                // Leaving the loop at the limit disposes the stream, which stops reading the older events.
                await foreach (var batch in batches.WithCancellation(cancellationToken))
                {
                    foreach (var @event in batch.Take(MaxRows - written))
                    {
                        var cells = new List<Cell>(props.Count);

                        for (var i = 0; i < props.Count; i++)
                        {
                            var prop = props[i];
                            var value = prop.GetValue(@event);

                            if (prop.PropertyType == typeof(DateTime))
                            {
                                cells.Add(new Cell(((DateTime)value).ConvertNumerals("G"), dateFormat));
                            }
                            else
                            {
                                // force text format to stop formulas from executing in user-controlled values
                                cells.Add(new Cell(value?.ToString(), "@", Wrap: wraps[i]));
                            }
                        }

                        await writer.WriteAsync(JsonSerializer.Serialize(cells, _jsonOptions) + ",");

                        written++;
                    }

                    if (written >= MaxRows)
                    {
                        break;
                    }
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

            return new AuditReportResult(file.Id, file.Title, filesLinkUtility.GetFileWebEditorUrl(file.Id));
        }
        finally
        {
            DocumentBuilderScriptHelper.DeleteScriptFile(scriptFilePath);
        }
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

    private sealed record Cell(string Value, string Format, string Halign = null, bool? Wrap = null);
}

/// <summary>
/// Renders an audit report as a CSV file and saves it into the author's "My documents" folder. This
/// path bypasses the document builder entirely: the file is written batch by batch into a temporary
/// file, so neither its size nor the length of the period is bounded by memory.
/// </summary>
[Scope]
public class AuditCsvReportWriter(
    TempStream tempStream,
    CsvFileHelper csvFileHelper,
    ReportResultFileSaver fileSaver,
    FilesLinkUtility filesLinkUtility,
    CommonLinkUtility commonLinkUtility,
    SetupInfo setupInfo)
{
    public async Task<AuditReportResult> WriteAsync<T>(
        Guid userId,
        IAsyncEnumerable<IReadOnlyList<T>> batches,
        AuditReportDescriptor descriptor,
        Func<int, Task> onProgressAsync,
        CancellationToken cancellationToken) where T : BaseEvent
    {
        var reportName = string.Format(descriptor.NameFormat + ".csv", descriptor.NameArg0, descriptor.NameArg1);

        await using var stream = tempStream.Create();

        // UTF-8 with a byte order mark, which is what lets spreadsheet applications read non-Latin text right.
        await csvFileHelper.CreateLargeFileAsync(stream, batches, new BaseEventMap<T>(), encoding: Encoding.UTF8, cancellationToken: cancellationToken);

        await onProgressAsync(50);

        cancellationToken.ThrowIfCancellationRequested();

        stream.Position = 0;

        var file = await fileSaver.SaveToMyDocumentsAsync(userId, reportName, stream);

        return new AuditReportResult(file.Id, file.Title, GetFileUrl(file));
    }

    // The editor opens a CSV by having the document server convert it, and the document server refuses
    // a source larger than it downloads in one go, by default the same 100 MB as the single-request
    // limit of the portal. Past that size the report is offered for download instead.
    private string GetFileUrl(File<int> file)
    {
        if (file.ContentLength > setupInfo.AvailableFileSize)
        {
            return commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileDownloadUrl(file.Id));
        }

        var fileUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id));

        return fileUrl + $"&options={{\"codePage\":{Encoding.UTF8.CodePage}}}";
    }
}
