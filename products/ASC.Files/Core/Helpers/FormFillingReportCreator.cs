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
public class FormFillingReportCreator
{
    private readonly ExportToCSV _exportToCSV;
    private readonly UserManager _userManager;
    private readonly SecurityContext _securityContext;
    private readonly IDaoFactory _daoFactory;
    private readonly IHttpClientFactory _clientFactory;
    private readonly TenantUtil _tenantUtil;

    private static readonly JsonSerializerOptions _options = new() {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public FormFillingReportCreator(
        ExportToCSV exportToCSV,
        UserManager userManager,
        SecurityContext securityContext,
        IDaoFactory daoFactory,
        IHttpClientFactory clientFactory,
        TenantUtil tenantUtil)
    {
        _exportToCSV = exportToCSV;
        _userManager = userManager;
        _securityContext = securityContext;
        _daoFactory = daoFactory;
        _clientFactory = clientFactory;
        _tenantUtil = tenantUtil;
    }

    public async Task UpdateFormFillingReport<T>(T resultsFileId, string formsDataUrl)
    {

        if (formsDataUrl != null)
        {
            var fileDao = _daoFactory.GetFileDao<T>();
            var submitFormsData = await GetSubmitFormsData(formsDataUrl);

            if (resultsFileId != null)
            {
                var resultsFile = await fileDao.GetFileAsync(resultsFileId);

                var updateDt = _exportToCSV.CreateDataTable(submitFormsData.FormsData);
                await _exportToCSV.UpdateCsvReport(resultsFile, updateDt);

            }
        }
    }

    private async Task<SubmitFormsData> GetSubmitFormsData(string url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get
        };
        var httpClient = _clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var data = await response.Content.ReadAsStringAsync();

        var u = await _userManager.GetUsersAsync(_securityContext.CurrentAccount.ID);
        var name = new List<FormsItemData>()
        {
            new FormsItemData()
            {
                Key= FilesCommonResource.User,
                Value = $"{u.FirstName} {u.LastName}"
            },
            new FormsItemData()
            {
                Key= FilesCommonResource.Date,
                Value = $"{_tenantUtil.DateTimeNow().ToString("dd.MM.yyyy H:mm:ss")}"
            },
        };

        var result = JsonSerializer.Deserialize<SubmitFormsData>(data, _options);
        result.FormsData = name.Concat(result.FormsData);

        return result;
    }

}