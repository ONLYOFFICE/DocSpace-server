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

    // An audit row takes about 300 bytes of the script, so the script budget below fits some 300 000 rows. The
    // default row limit stays under that and keeps the build near a minute (the document builder takes about 25 s
    // per 100 000 rows); a sheet could not hold more than about a million rows in any case.
    private const int DefaultMaxRows = 200_000;
    private const int SheetMaxRows = 1_000_000;

    // The document server downloads the script before it runs it and refuses one larger than its download limit,
    // FileConverter.converter.maxDownloadBytes (100 MB by default). The events are written into the script itself,
    // so the default budget stays safely under that limit.
    private const long DefaultMaxScriptBytes = 90L * 1024 * 1024;

    // The note is written once the rows are known and takes no more than this, whatever the language.
    private const int RowLimitNoteReserve = 1024;

    /// <summary>
    /// How many events a workbook holds at most. The events come newest first, so a longer period keeps its most
    /// recent events and the report header says how many were left out; the CSV format has no such limit.
    /// </summary>
    public int MaxRows { get; } = Math.Clamp(
        int.TryParse(configuration["files:audit-report:xlsx-max-rows"], out var maxRows) ? maxRows : DefaultMaxRows,
        1,
        SheetMaxRows);

    private readonly long _maxScriptBytes =
        long.TryParse(configuration["files:audit-report:xlsx-max-script-bytes"], out var maxScriptBytes) && maxScriptBytes > 0
            ? maxScriptBytes
            : DefaultMaxScriptBytes;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Keeps non-Latin text as it is: escaped, every Cyrillic letter would take six bytes of the script instead
        // of two. Quotes, backslashes and control characters are still escaped, so a value cannot leave its string.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        var period = descriptor.From.HasValue && descriptor.To.HasValue
            ? $"{descriptor.From.Value.ConvertNumerals("d")} – {descriptor.To.Value.ConvertNumerals("d")}"
            : descriptor.From?.ConvertNumerals("d") ?? descriptor.To?.ConvertNumerals("d") ?? string.Empty;

        // Every cell of a column shares its format, so the rows carry the values alone. Text is forced for all but
        // the dates to stop formulas from executing in user-controlled values, and the page column, which holds a
        // URL long enough to wrap onto a second line, is kept on one.
        var columns = props
            .Select(p => new
            {
                format = p.PropertyType == typeof(DateTime) ? dateFormat : "@",
                wrap = p.Name == nameof(BaseEvent.Page) ? false : (bool?)null
            })
            .ToList();

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");

        var scriptFilePath = tempPath.GetTempFileName(".docbuilder");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        var outputFileName = string.Format(descriptor.NameFormat + ".xlsx", descriptor.NameArg0, descriptor.NameArg1);

        // The input data follows the rows in the template, so it is filled in after them, once the number of rows
        // that fit is known.
        var scriptParts = script
            .Replace("${tempFileName}", tempFileName)
            .Split("${dataValues}");

        string GetInputData(string rowLimitNote)
        {
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
                aligns = headers.Select(_ => "left").ToList(),
                columns
            };

            return JsonSerializer.Serialize(scriptInputData, _jsonOptions);
        }

        var rowBudget = _maxScriptBytes
            - Encoding.UTF8.GetByteCount(scriptParts[0])
            - Encoding.UTF8.GetByteCount(scriptParts[1].Replace("${inputData}", GetInputData(null)))
            - RowLimitNoteReserve;

        try
        {
            await using (var writer = new StreamWriter(scriptFilePath))
            {
                await writer.WriteAsync(scriptParts[0]);

                var written = 0;
                var full = false;

                // Leaving the loop at a limit disposes the stream, which stops reading the older events. The row
                // limit is checked before a row is serialized and before the next batch is asked for, so a report
                // cut at the end of a batch reads no batch it would drop.
                await foreach (var batch in batches.WithCancellation(cancellationToken))
                {
                    foreach (var @event in batch)
                    {
                        if (written >= MaxRows)
                        {
                            full = true;
                            break;
                        }

                        var row = new string[props.Count];

                        for (var i = 0; i < props.Count; i++)
                        {
                            var value = props[i].GetValue(@event);

                            row[i] = value is DateTime date ? date.ConvertNumerals("G") : value?.ToString();
                        }

                        var json = JsonSerializer.Serialize(row, _jsonOptions) + ",";

                        rowBudget -= Encoding.UTF8.GetByteCount(json);

                        if (rowBudget < 0)
                        {
                            full = true;
                            break;
                        }

                        await writer.WriteAsync(json);

                        written++;
                    }

                    if (full || written >= MaxRows)
                    {
                        full = true;
                        break;
                    }
                }

                // Only a report stopped at a limit says it left events out. The count is taken before the events
                // are read, and events the retention cleanup removes meanwhile would otherwise read as cut off.
                var rowLimitNote = full && written < descriptor.TotalCount
                    ? string.Format(descriptor.Culture, AuditReportResource.ReportRowLimitNote,
                        written.ToString("N0", descriptor.Culture), descriptor.TotalCount.ToString("N0", descriptor.Culture))
                    : null;

                await writer.WriteAsync(scriptParts[1].Replace("${inputData}", GetInputData(rowLimitNote)));
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
