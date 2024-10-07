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

namespace ASC.Web.Files.Utils;
[Transient]
public class ExportToCSV(
    ILogger<AuditReportUploader> logger,
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    TenantUtil tenantUtil)
{
    public async Task<T> UploadCsvReport<T>(T parentId, string title, DataTable dataTable)
    {
        try
        {
            var data = DataTableToCsv(dataTable);
            using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            var csvFile = serviceProvider.GetService<File<T>>();
            csvFile.ParentId = parentId;
            csvFile.Title = Global.ReplaceInvalidCharsAndTruncate(title + ".csv");

            var fileDao = daoFactory.GetFileDao<T>();
            var file = await fileDao.SaveFileAsync(csvFile, textStream);

            return file.Id;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileUploading(ex);
            throw;
        }
    }

    public async Task UpdateCsvReport<T>(File<T> file, IEnumerable<FormsItemData> list)
    {
        try
        {                
            var dataTable = CreateDataTable(list);
            var fileDao = daoFactory.GetFileDao<T>();

            await using var source = await fileDao.GetFileStreamAsync(file);
            using var reader = new StreamReader(source);
            var oldData = await reader.ReadToEndAsync();

            var data = DataTableToCsv(dataTable, !string.IsNullOrEmpty(oldData));
            var resultData = oldData + data;
            using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(resultData));

            file.Version++;
            file.VersionGroup++;
            file.ContentLength = textStream.Length;
            file.ModifiedOn = tenantUtil.DateTimeNow();
            await fileDao.SaveFileAsync(file, textStream, false);
        }
        catch (Exception ex)
        {
            logger.ErrorWhileUploading(ex);
            throw;
        }
    }

    private DataTable CreateDataTable(IEnumerable<FormsItemData> list)
    {
        var dataTable = new DataTable();
        dataTable.TableName = typeof(FormsItemData).FullName;
        var values = new object[list.Count()];

        var i = 0;
        foreach (var entity in list)
        {
            dataTable.Columns.Add(new DataColumn(entity.Key));

            values[i] = entity.Value; i++;
        }
        dataTable.Rows.Add(values);

        return dataTable;
    }

    private static string DataTableToCsv(DataTable dataTable, bool onlyRows = false)
    {
        var result = new StringBuilder();

        var columnsCount = dataTable.Columns.Count;

        if (!onlyRows)
        {
            for (var index = 0; index < columnsCount; index++)
            {
                if (index != columnsCount - 1)
                {
                    result.Append(dataTable.Columns[index].Caption + ",");
                }
                else
                {
                    result.Append(dataTable.Columns[index].Caption);
                }
            }
            result.Append(Environment.NewLine);
        }
        
        foreach (DataRow row in dataTable.Rows)
        {
            for (var i = 0; i < columnsCount; i++)
            {
                var itemValue = WrapDoubleQuote(row[i].ToString());

                if (i != columnsCount - 1)
                {
                    result.Append(itemValue + ",");
                }
                else
                {
                    result.Append(itemValue);
                }
            }

            result.Append(Environment.NewLine);
        }

        return result.ToString();
    }

    private static string WrapDoubleQuote(string value)
    {
        return "\"" + value.Trim().Replace("\"", "\"\"") + "\"";
    }

}
