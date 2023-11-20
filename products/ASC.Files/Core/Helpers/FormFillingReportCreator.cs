// (c) Copyright Ascensio System SIA 2010-2022
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
    private readonly SocketManager _socketManager;
    private readonly IDaoFactory _daoFactory;
    private readonly IHttpClientFactory _clientFactory;

    public FormFillingReportCreator(
        ExportToCSV exportToCSV,
        SocketManager socketManager,
        IDaoFactory daoFactory,
        IHttpClientFactory clientFactory)
    {
        _exportToCSV = exportToCSV;
        _socketManager = socketManager;
        _daoFactory = daoFactory;
        _clientFactory = clientFactory;
    }

    public async Task<EntryProperties> UpdateFormFillingReport<T>(File<T> form, string formsDataUrl)
    {
        var linkDao = _daoFactory.GetLinkDao();
        var sourceId = await linkDao.GetSourceAsync(form.Id.ToString());

        if (sourceId != null && formsDataUrl != null)
        {
            EntryProperties properties;

            var fileDao = _daoFactory.GetFileDao<T>();

            if (int.TryParse(sourceId, out var sourceInt))
            {
                properties = _daoFactory.GetFileDao<int>().GetProperties(sourceInt).Result;
            }
            else
            {
                properties = _daoFactory.GetFileDao<string>().GetProperties(sourceId).Result;
            }
            var submitFormsData = await GetSubmitFormsData(formsDataUrl);

            if (properties.FormFilling.ResultsFileID == null)
            {
                var dt = _exportToCSV.CreateDataTable(submitFormsData.FormsData);

                var resultFolderId = (T)Convert.ChangeType(properties.FormFilling.ResultsFolderId, typeof(T));
                var resultsFileID = await _exportToCSV.UploadCsvReport(resultFolderId, properties.FormFilling.Title, dt);

                properties.FormFilling.ResultsFileID = resultsFileID.ToString();
                await fileDao.SaveProperties((T)Convert.ChangeType(sourceId, typeof(T)), properties);
            }
            else
            {
                var resultsFile = await fileDao.GetFileAsync((T)Convert.ChangeType(properties.FormFilling.ResultsFileID, typeof(T)));

                var updateDt = _exportToCSV.CreateDataTable(submitFormsData.FormsData);
                await _exportToCSV.UpdateCsvReport(resultsFile, updateDt);
            }
            await _socketManager.DeleteFileAsync(form);
            await linkDao.DeleteLinkAsync(sourceId);

            return properties;
        }

        return null;
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

        return JsonConvert.DeserializeObject<SubmitFormsData>(data);
    }

}