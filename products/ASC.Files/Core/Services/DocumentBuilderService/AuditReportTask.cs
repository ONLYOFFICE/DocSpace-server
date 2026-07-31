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

[Transient]
public class AuditReportTask : DocumentBuilderTask<int, AuditReportTaskData>
{
    private const string ScriptName = "AuditReport.docbuilder";

    public AuditReportTask()
    {
    }

    public AuditReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    public static string GetTaskDiscriminator(AuditReportKind kind, int? folderId = null)
    {
        return folderId.HasValue
            ? $"{kind}_{folderId.Value.ToString(CultureInfo.InvariantCulture)}"
            : $"{kind}";
    }

    protected override Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        throw new NotSupportedException();
    }

    protected override Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        throw new NotSupportedException();
    }

    protected override async Task DoJob()
    {
        ILogger logger = null;

        try
        {
            CancellationToken.ThrowIfCancellationRequested();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var serviceProvider = scope.ServiceProvider;

            if (!string.IsNullOrEmpty(_baseUri))
            {
                serviceProvider.GetService<CommonLinkUtility>().ServerUri = _baseUri;
            }

            var tenantManager = serviceProvider.GetService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = serviceProvider.GetService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            logger = serviceProvider.GetService<ILogger<AuditReportTask>>();

            var userManager = serviceProvider.GetService<UserManager>();
            var user = await userManager.GetUsersAsync(_userId);
            var userCulture = user.GetCulture();
            CultureInfo.CurrentCulture = userCulture;
            CultureInfo.CurrentUICulture = userCulture;

            CancellationToken.ThrowIfCancellationRequested();

            switch (_data.Kind)
            {
                case AuditReportKind.LoginHistory:
                    {
                        var events = await serviceProvider.GetService<LoginEventsRepository>()
                            .GetByFilterAsync(fromDate: _data.From, to: _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.LoginHistoryReportName,
                            _data.From?.ToShortDateString(), _data.To?.ToShortDateString(), userCulture);
                        break;
                    }
                case AuditReportKind.FolderHistory:
                    {
                        var events = await serviceProvider.GetService<HistoryService>()
                            .GetFolderAuditEventsAsync(_data.FolderId.Value, _data.From, _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.AuditTrailReportName,
                            "room", _data.FolderId.Value.ToString(CultureInfo.InvariantCulture), userCulture);
                        break;
                    }
                default:
                    {
                        var events = await serviceProvider.GetService<AuditEventsRepository>()
                            .GetByFilterAsync(from: _data.From, to: _data.To);

                        await ProduceAsync(serviceProvider, events, AuditReportResource.AuditTrailReportName,
                            _data.From?.ToShortDateString(), _data.To?.ToShortDateString(), userCulture);
                        break;
                    }
            }

            Percentage = 100;
            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception ex)
        {
            logger?.ErrorWithException(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            IsCompleted = true;
            await PublishChanges();
        }
    }

    private Task ProduceAsync<T>(IServiceProvider serviceProvider, IEnumerable<T> events, string reportNameFormat, string nameArg0, string nameArg1, CultureInfo culture) where T : BaseEvent
    {
        return _data.Format == AuditReportFormat.Csv
            ? BuildCsvAsync(serviceProvider, events, reportNameFormat, nameArg0, nameArg1)
            : BuildXlsxAsync(serviceProvider, events, reportNameFormat, nameArg0, nameArg1, culture);
    }

    private async Task BuildXlsxAsync<T>(IServiceProvider serviceProvider, IEnumerable<T> events, string reportNameFormat, string nameArg0, string nameArg1, CultureInfo culture) where T : BaseEvent
    {
        var tempPath = serviceProvider.GetService<TempPath>();
        var documentBuilderTask = serviceProvider.GetService<DocumentBuilderTask>();

        var (headers, props) = GetColumns<T>();

        var dateFormat = $"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.ShortTimePattern.Replace("tt", "AM/PM")}";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var header = await BuildReportHeaderAsync(serviceProvider, culture);

        var period = _data.From.HasValue && _data.To.HasValue
            ? $"{_data.From.Value.ToShortDateString()} - {_data.To.Value.ToShortDateString()}"
            : _data.From?.ToShortDateString() ?? _data.To?.ToShortDateString() ?? string.Empty;

        var scriptInputData = new
        {
            resources = new
            {
                company = Resource.AccountingReportCompany + ":",
                report = Resource.AccountingReportTitle + ":",
                period = Resource.AccountingReportPeriod + ":",
                dateGenerated = Resource.AccountingReportDateGenerated + ":",
                sheetName = GetSheetName(reportNameFormat),
                dateGeneratedFormat = header.DateGeneratedFormat
            },
            info = new
            {
                company = header.Company,
                report = GetReportTitle(reportNameFormat),
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
        var outputFileName = string.Format(reportNameFormat + ".xlsx", nameArg0, nameArg1);

        script = script
            .Replace("${inputData}", JsonSerializer.Serialize(scriptInputData, jsonOptions))
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
                        cells.Add(new Cell(((DateTime)value).ToString("G", CultureInfo.InvariantCulture), dateFormat));
                    }
                    else
                    {
                        // force text format to stop formulas from executing in user-controlled values
                        cells.Add(new Cell(value?.ToString(), "@"));
                    }
                }

                await writer.WriteAsync(JsonSerializer.Serialize(cells, jsonOptions) + ",");
            }

            await writer.WriteAsync(scriptParts[1]);
        }

        var inputData = new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);

        Percentage = 30;
        await PublishChanges();

        CancellationToken.ThrowIfCancellationRequested();

        var fileUri = await documentBuilderTask.BuildFileAsync(inputData, CancellationToken);

        Percentage = 60;
        await PublishChanges();

        CancellationToken.ThrowIfCancellationRequested();

        var file = await SaveResultFileAsync(serviceProvider, new Uri(fileUri), outputFileName);

        var filesLinkUtility = serviceProvider.GetService<FilesLinkUtility>();

        ResultFileId = file.Id;
        ResultFileName = file.Title;
        ResultFileUrl = filesLinkUtility.GetFileWebEditorUrl(file.Id);

        SendDownloadedMessage(serviceProvider);

        if (System.IO.File.Exists(scriptFilePath))
        {
            System.IO.File.Delete(scriptFilePath);
        }
    }

    private async Task BuildCsvAsync<T>(IServiceProvider serviceProvider, IEnumerable<T> events, string reportNameFormat, string nameArg0, string nameArg1) where T : BaseEvent
    {
        var csvFileHelper = serviceProvider.GetService<CsvFileHelper>();
        var csvFileUploader = serviceProvider.GetService<CsvFileUploader>();

        var reportName = string.Format(reportNameFormat + ".csv", nameArg0, nameArg1);

        Percentage = 50;
        await PublishChanges();

        await using var stream = csvFileHelper.CreateFile(events, new BaseEventMap<T>());
        var fileUrl = await csvFileUploader.UploadFile(stream, reportName);

        ResultFileName = reportName;
        ResultFileUrl = fileUrl;

        SendDownloadedMessage(serviceProvider);
    }

    private async Task<File<int>> SaveResultFileAsync(IServiceProvider serviceProvider, Uri fileUri, string outputFileName)
    {
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var socketManager = serviceProvider.GetService<SocketManager>();
        var globalFolder = serviceProvider.GetService<GlobalFolder>();

        var file = serviceProvider.GetService<File<int>>();

        file.CreateBy = _userId;
        file.ParentId = await globalFolder.GetFolderMyAsync(daoFactory);
        file.Title = outputFileName;

        using var request = new HttpRequestMessage { RequestUri = fileUri };

#pragma warning disable CA2000
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();

        var fileDao = daoFactory.GetFileDao<int>();

        file.ContentLength = stream.Length;

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);

        return file;
    }

    private void SendDownloadedMessage(IServiceProvider serviceProvider)
    {
        if (_data.Kind == AuditReportKind.FolderHistory)
        {
            return;
        }

        var messageService = serviceProvider.GetService<MessageService>();

        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        var action = _data.Kind == AuditReportKind.LoginHistory
            ? MessageAction.LoginHistoryReportDownloaded
            : MessageAction.AuditTrailReportDownloaded;

        messageService.SendHeadersMessage(action, target: null, httpHeaders: headers, null);
    }

    private static (List<string> Headers, List<PropertyInfo> Props) GetColumns<T>() where T : BaseEvent
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<EventAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<EventAttribute>().Order)
            .ToList();

        var headers = props
            .Select(p => AuditReportResource.ResourceManager.GetString(p.GetCustomAttribute<EventAttribute>().Resource))
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

    private static async Task<ReportHeader> BuildReportHeaderAsync(IServiceProvider serviceProvider, CultureInfo culture)
    {
        var settingsManager = serviceProvider.GetService<SettingsManager>();
        var tenantLogoManager = serviceProvider.GetService<TenantLogoManager>();
        var tenantUtil = serviceProvider.GetService<TenantUtil>();

        var logoText = await tenantLogoManager.GetLogoTextAsync();

        // the document builder currently cannot embed a logo referenced by URL, so we inline it
        // as a base64 data URI. Once the builder's image handling is fixed, switch back to the URL:
        var logoSrc = await tenantLogoManager.GetTopLogoDataUriAsync()
                      ?? await tenantLogoManager.GetTopLogoAbsoluteUrlAsync();

        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();
        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var dateGeneratedFormat = $"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.LongTimePattern.Replace("tt", "AM/PM")}";

        return new ReportHeader(
            logoSrc,
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1),
            logoText,
            tenantUtil.DateTimeNow().ToString("G", CultureInfo.InvariantCulture),
            dateGeneratedFormat);
    }

    private sealed record ReportHeader(
        string LogoSrc,
        int[] MainBgColor,
        int[] LightBgColor,
        int[] MainFontColor,
        string Company,
        string DateGenerated,
        string DateGeneratedFormat);

    private sealed record Cell(string Value, string Format, string Halign = null);
}

public record AuditReportTaskData(
    AuditReportKind Kind,
    AuditReportFormat Format,
    DateTime? From,
    DateTime? To,
    IDictionary<string, string> Headers,
    int? FolderId = null);
