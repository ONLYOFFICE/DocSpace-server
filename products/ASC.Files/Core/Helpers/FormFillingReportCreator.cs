// (c) Copyright Ascensio System SIA 2009-2025
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
    ExportToXLSX exportToXLSX,
    ExternalDatabaseClient externalDatabaseClient,
    IDaoFactory daoFactory,
    IHttpClientFactory clientFactory,
    TenantManager tenantManager,
    FactoryIndexerForm factoryIndexerForm)
{

    public async Task UpdateFormFillingReport<T>(int originalFormId, int originalFormVersion, int roomId, int resultFormNumber, string formsDataUrl, File<T> formsDataFile)
    {
        var (formData, metaData) = await GetSubmitFormsData(formsDataFile, originalFormId, roomId, resultFormNumber, formsDataUrl);

        if (!externalDatabaseClient.IsEnabled())
        {
            return;
        }

        var fileId = formsDataFile.Id is int id ? id : 0;
        var tableName = $"form_{originalFormId}_v{originalFormVersion}";
        var normalizedMeta = NormalizeMetadata(metaData);
        await externalDatabaseClient.CreateTableIfNotExistsAsync(tableName, BuildColumnDefinitions(normalizedMeta));

        var rowData = BuildRowData(formData, normalizedMeta, fileId);
        await externalDatabaseClient.UpsertDataAsync(tableName, rowData, keyColumn: "form_id");
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

    public async Task<IEnumerable<DbFormsItemDataSearch>> GetFormFillingResults(int roomId, int originalFormId)
    {
        factoryIndexerForm.Refresh();
        var (success, result) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.RoomId, roomId).Where(s => s.OriginalFormId, originalFormId));

        if (success)
        {
            var sortedResult = result
                .Select(item =>
                {
                    var formValue = item.FormsData?.FirstOrDefault(f => f.Key == "FormNumber").Value;
                    int.TryParse(formValue, out var number);
                    return (item, number);
                })
            .OrderBy(x => x.number)
            .Select(x => x.item)
            .ToList();

            return sortedResult;
        }
        return [];
    }

    private async Task<(SubmitFormsData fromData, List<FormMetadata> fromMetaData)> GetSubmitFormsData<T>(
        File<T> formsDataFile,
        int originalFormId,
        int roomId,
        int resultFormNumber,
        string url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get
        };

        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var data = await response.Content.ReadAsStringAsync();

        var formNumber = new List<FormsItemData>
        {
            new()
            {
                Key = "FormNumber",
                Value = resultFormNumber.ToString()
            }
        };

        var parsed = ParseSubmitAndMetadata(data);

        var fromData = parsed.Data;
        var fromMetaData = parsed.MetaData;

        fromData.FormsData = fromData.FormsData
            .Where(f => f.Type != "picture" && f.Type != "signature")
            .ToList();

        var now = DateTime.UtcNow;
        var tenantId = tenantManager.GetCurrentTenantId();

        if (formsDataFile.Id is int id && formsDataFile.ParentId is int parentId)
        {
            var searchItems = new DbFormsItemDataSearch
            {
                Id = id,
                TenantId = tenantId,
                ParentId = parentId,
                OriginalFormId = originalFormId,
                RoomId = roomId,
                CreateOn = now,
                FormsData = formNumber.Concat(fromData.FormsData)
            };

            _ = factoryIndexerForm.IndexAsync(searchItems);
        }

        return (fromData, fromMetaData);
    }

    private (SubmitFormsData Data, List<FormMetadata> MetaData)ParseSubmitAndMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        if (!root.TryGetProperty("formsdata", out var formsArray) ||
            formsArray.ValueKind != JsonValueKind.Array)
        {
            return (new SubmitFormsData
            {
                FormsData = Enumerable.Empty<FormsItemData>()
            }, new List<FormMetadata>());
        }

        var formsDataList = new List<FormsItemData>(formsArray.GetArrayLength());
        var metaDataList = new List<FormMetadata>(formsArray.GetArrayLength());

        foreach (var form in formsArray.EnumerateArray())
        {
            var key = form.TryGetProperty("key", out var keyProp)
                ? keyProp.GetString()
                : null;

            var type = form.TryGetProperty("type", out var typeProp)
                ? typeProp.GetString()
                : null;

            var tag = form.TryGetProperty("tag", out var tagProp)
                ? tagProp.GetString()
                : null;

            var value = form.TryGetProperty("value", out var valueProp)
                ? valueProp.ToString()
                : null;

            formsDataList.Add(new FormsItemData
            {
                Key = key,
                Tag = tag,
                Value = value,
                Type = type
            });

            var metadata = new FormMetadata
            {
                Key = key ?? "",
                Type = type ?? "",
                Format = form.TryGetProperty("format", out var formatProp)
                    ? formatProp.GetString()
                    : null
            };

            if (form.TryGetProperty("options", out var optionsProp) &&
                optionsProp.ValueKind == JsonValueKind.Array)
            {
                var possibleValues = new List<string>();

                foreach (var option in optionsProp.EnumerateArray())
                {
                    if (option.ValueKind == JsonValueKind.Object &&
                        option.TryGetProperty("value", out var valProp))
                    {
                        possibleValues.Add(valProp.ToString());
                    }
                    else
                    {
                        possibleValues.Add(option.ToString());
                    }
                }

                metadata.PossibleValues = possibleValues;
            }

            metaDataList.Add(metadata);
        }

        return (
            new SubmitFormsData
            {
                FormsData = formsDataList
            },
            metaDataList
        );
    }

    private static IEnumerable<FormMetadata> NormalizeMetadata(IEnumerable<FormMetadata> metaData)
    {
        return metaData
            .GroupBy(m => m.Type == "radio"
                ? $"radio::{NormalizeColumnName(m.Key)}"
                : NormalizeColumnName(m.Key))
            .Select(g => g.First());
    }

    private static IEnumerable<DbColumnDefinition> BuildColumnDefinitions(IEnumerable<FormMetadata> metaData)
    {
        yield return new DbColumnDefinition("form_id", DbColumnType.Integer, IsPrimaryKey: true);
        yield return new DbColumnDefinition("created_on", DbColumnType.DateTime);

        foreach (var field in metaData)
        {
            var name = NormalizeColumnName(field.Key);
            var (type, enumValues) = field.Type switch
            {
                "checkBox" => (DbColumnType.Boolean, (IReadOnlyList<string>?)null),
                "dateTime" => (DbColumnType.Date, null),
                "comboBox" or "dropDownList" or "radio" => (DbColumnType.Enum, (IReadOnlyList<string>?)field.PossibleValues),
                _ => (DbColumnType.Text, null)
            };
            yield return new DbColumnDefinition(name, type, enumValues);
        }
    }

    private static Dictionary<string, object> BuildRowData(SubmitFormsData data, IEnumerable<FormMetadata> metaData, int formId)
    {
        var result = new Dictionary<string, object>
        {
            ["form_id"] = formId,
            ["created_on"] = DateTime.UtcNow
        };

        var metaByKey = metaData.ToDictionary(m => NormalizeColumnName(m.Key));

        foreach (var item in data.FormsData)
        {
            var column = NormalizeColumnName(item.Key);
            if (!metaByKey.TryGetValue(column, out var meta))
            {
                continue;
            }
            result[column] = ConvertFieldValue(item.Value, meta) ?? DBNull.Value;
        }

        return result;
    }

    private static string NormalizeColumnName(string key)
    {
        var name = Regex.Replace(key ?? "", @"[^a-zA-Z0-9_]", "_");
        return name.ToLower();
    }

    private static object? ConvertFieldValue(string? value, FormMetadata meta)
    {
        if (value == null)
        {
            return null;
        }

        return meta.Type switch
        {
            "checkBox" => bool.TryParse(value, out var b) ? b : (object)value,
            "dateTime" => ParseDate(value, meta.Format),
            _ => value
        };
    }

    private static DateTime? ParseDate(string value, string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        }

        var dotNetFormat = format
            .Replace("mm", "MM")
            .Replace("yyyy", "yyyy")
            .Replace("yy", "yy");

        return DateTime.TryParseExact(value, dotNetFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
    }

    public class BoolToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException("Unexpected token type")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

}