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
    TenantManager tenantManager)
{

    private static readonly JsonSerializerOptions _options = new() {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task UpdateFormFillingReport<T>(T resultsFileId, int resultFormNumber, string formsDataUrl, string resultUrl)
    {

        if (formsDataUrl != null)
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var submitFormsData = await GetSubmitFormsData(resultFormNumber, formsDataUrl, resultUrl);

            if (resultsFileId != null)
            {
                var resultsFile = await fileDao.GetFileAsync(resultsFileId);

                var updateDt = exportToCSV.CreateDataTable(submitFormsData.FormsData);
                await exportToCSV.UpdateCsvReport(resultsFile, updateDt);

            }
        }
    }

    private async Task<SubmitFormsData> GetSubmitFormsData(int resultFormNumber, string url, string resultUrl)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get
        };
        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var data = await response.Content.ReadAsStringAsync();

        var tenantCulture = (await tenantManager.GetCurrentTenantAsync()).GetCulture();
        var formNumber = new List<FormsItemData>
        {
            new()
            {
                Key = FilesCommonResource.ResourceManager.GetString("FormNumber", tenantCulture),
                Value = resultFormNumber
            },
        };

        var formLink = new FormsItemData
        {
            Key = FilesCommonResource.ResourceManager.GetString("LinkToForm", tenantCulture),
            Value = $"=HYPERLINK(\"{resultUrl}\";\"{FilesCommonResource.ResourceManager.GetString("OpenForm", tenantCulture)}\")"
        };
        var date = new FormsItemData
        {
            Key = FilesCommonResource.ResourceManager.GetString("Date", tenantCulture),
            Value = $"=\"{tenantUtil.DateTimeNow().ToString("G", tenantCulture)}\""
        };
        var result = JsonSerializer.Deserialize<SubmitFormsData>(data, _options);

        result.FormsData = formNumber.Concat(result.FormsData);
        result.FormsData = result.FormsData.Append(date);
        result.FormsData = result.FormsData.Append(formLink);

        return result;
    }

}