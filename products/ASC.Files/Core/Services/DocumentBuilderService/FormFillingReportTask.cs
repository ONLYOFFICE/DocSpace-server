// (c) Copyright Ascensio System SIA 2009-2024
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

[Transient]
public class FormFillingReportTask(IServiceScopeFactory serviceProvider) : DocumentBuilderTask<int, FormFillingReportTaskData>(serviceProvider)
{
    private const string ScriptName = "FormFillingReport.docbuilder";
    
    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
        
        var data = await GetFormFillingReportData(serviceProvider, _userId, _data.RoomId, _data.OriginalFormId);

        script = script
            .Replace("${tempFileName}", tempFileName)
            .Replace("${inputData}", JsonConvert.SerializeObject(data));
        
        return new DocumentBuilderInputData(script, null, tempFileName, "");
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();

        var fileDao = daoFactory.GetFileDao<int>();
        var origProperties = await daoFactory.GetFileDao<int>().GetProperties(_data.OriginalFormId);
        var resultFile = await fileDao.GetFileAsync(origProperties.FormFilling.ResultsFileID);

        using var request = new HttpRequestMessage();
        request.RequestUri = fileUri;

        using var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        
        resultFile.Version++;
        resultFile.VersionGroup++;
        resultFile.ContentLength = stream.Length;

        resultFile = await fileDao.SaveFileAsync(resultFile, stream, false);

        if (resultFile.Id != origProperties.FormFilling.ResultsFileID)
        {
            origProperties.FormFilling.ResultsFileID = resultFile.Id;
            await fileDao.SaveProperties(_data.OriginalFormId, origProperties);
        }

        var filesMessageService = serviceProvider.GetService<FilesMessageService>();
        
        var headers = _data.Headers != null 
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value)) 
            : [];
        
        var room = await daoFactory.GetFolderDao<int>().GetFolderAsync(_data.RoomId);
        
        await filesMessageService.SendAsync(MessageAction.RoomIndexExportSaved, room, headers: headers);

        return resultFile;
    }

    private static async Task<object> GetFormFillingReportData(IServiceProvider serviceProvider, Guid userId, int roomId, int originalFormId)
    {
        var userManager = serviceProvider.GetService<UserManager>();
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var settingsManager = serviceProvider.GetService<SettingsManager>();
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var formFillingReportCreator = serviceProvider.GetService<FormFillingReportCreator>();
        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();
        var filesLinkUtility = serviceProvider.GetService<FilesLinkUtility>();
        var fileUtility = serviceProvider.GetService<FileUtility>();

        var user = await userManager.GetUsersAsync(userId);
        var fileDao = daoFactory.GetFileDao<int>();

        var userCulture = user.GetCulture();
        CultureInfo.CurrentCulture = userCulture;
        CultureInfo.CurrentUICulture = userCulture;

        var formFillingResults = await formFillingReportCreator.GetFormFillingResults(roomId, originalFormId);
        var tenantCulture = tenantManager.GetCurrentTenant().GetCulture();

        var keys = new List<string>();
        var values = new List<List<object>>();
        if (formFillingResults.Any())
        {
            var formsData = formFillingResults.FirstOrDefault().FormsData;
            if(formsData.Any())
            {
                foreach(var field in formsData)
                {
                    keys.Add(field.Key);
                }
                keys.Add(FilesCommonResource.ResourceManager.GetString("Date", tenantCulture));
                keys.Add(FilesCommonResource.ResourceManager.GetString("LinkToForm", tenantCulture));

                foreach (var formFillingRes in formFillingResults)
                {
                    var t = new List<object>();
                    foreach (var field in formFillingRes.FormsData)
                    {
                        if (field.Type == "picture")
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
                        format = $"{tenantCulture.DateTimeFormat.LongTimePattern}",
                        value = formFillingRes.CreateOn.ToString("G", tenantCulture),
                        url = ""
                    });
                    var formsDataFile = await fileDao.GetFileAsync(formFillingRes.Id);
                    var resultUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, formsDataFile.Title, formsDataFile.Id, formsDataFile.Version));
                    t.Add(new
                    {
                        format = "@",
                        value = FilesCommonResource.ResourceManager.GetString("OpenForm", tenantCulture),
                        url = resultUrl
                    });
                    values.Add(t);
                }
            }
        }

        var properties = await daoFactory.GetFileDao<int>().GetProperties(originalFormId);
        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();
        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        var data = new
        {
            resources = new
            {
                sheetName = properties.FormFilling.Title
            },

            themeColors = new
            {
                mainBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
                lightBgColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
                mainFontColor = DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1)
            },

            data = new {
                keys,
                values
            }
        };

        return data;
    }
}

public record FormFillingReportTaskData(int RoomId, int OriginalFormId, IDictionary<string, string> Headers);