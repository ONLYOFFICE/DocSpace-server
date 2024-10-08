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

namespace ASC.Files.Core.Helpers;

[Scope]
public class FormFillingReportCreator(
    ExportToCSV exportToCSV,
    IDaoFactory daoFactory,
    IHttpClientFactory clientFactory,
    TenantUtil tenantUtil,
    TenantManager tenantManager,
    FactoryIndexerForm factoryIndexerForm,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility)
{

    private static readonly JsonSerializerOptions _options = new() {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task UpdateFormFillingReport<T>(T resultsFileId, int resultFormNumber, string formsDataUrl, File<T> formsDataFile)
    {
        if (formsDataUrl != null)
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var resultUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, formsDataFile.Title, formsDataFile.Id, formsDataFile.Version));
            var submitFormsData = await GetSubmitFormsData(formsDataFile.Id, resultFormNumber, formsDataUrl, resultUrl);

            if (resultsFileId != null)
            {
                var resultsFile = await fileDao.GetFileAsync(resultsFileId);
                
                await exportToCSV.UpdateCsvReport(resultsFile, submitFormsData.FormsData);
            }
        }
    }

    public async Task<IEnumerable<FormsItemData>> GetFormsFields(int folderId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder?.FolderType != FolderType.FormFillingFolderDone)
        {
            return [];
        }
        
        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFilesAsync([folderId], FilterType.Pdf, false, Guid.Empty, null, null, false).FirstOrDefaultAsync();
        var (success, result) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.Id, file.Id));

        if (success)
        {
            return result.SelectMany(r => r.FormsData);
        }

        return [];
    }
    
    private async Task<SubmitFormsData> GetSubmitFormsData<T>(T resultFormId, int resultFormNumber, string url, string resultUrl)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get
        };
        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var data = await response.Content.ReadAsStringAsync();

        List<FormsItemData> formNumber = 
        [
                new() { Key = FilesCommonResource.FormNumber, Value = resultFormNumber.ToString() }, 
                new() { Key = FilesCommonResource.LinkToForm, Value = $"=HYPERLINK(\"{resultUrl}\";\"{FilesCommonResource.OpenForm}\")" },
                new() { Key = FilesCommonResource.Date, Value = $"{tenantUtil.DateTimeNow():MM/dd/yyyy H:mm:ss}" }
        ];
        
        var fromData = JsonSerializer.Deserialize<SubmitFormsData>(data, _options);
        var result = new SubmitFormsData
        {
            FormsData =  fromData.FormsData.Concat(formNumber).ToList()
        };

        var now = DateTime.UtcNow;
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        if (resultFormId is int id)
        {
            var searchItems = new DbFormsItemDataSearch
            {
                Id = id,
                TenantId = tenantId,
                CreateOn = now,
                FormsData = fromData.FormsData
            };

            await factoryIndexerForm.IndexAsync(searchItems);
        }

        return result;
    }

}