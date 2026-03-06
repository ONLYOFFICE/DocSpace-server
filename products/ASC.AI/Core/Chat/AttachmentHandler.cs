// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Core.Chat;

public class AttachmentResult
{
    public required FileEntry File { get; init; }
    public AttachmentMessageContent? AttachmentContent { get; init; }
    public bool Success { get; init; }
    public ToolWrapper? DynamicTool { get; init; }
}

[Scope]
public class AttachmentHandler(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    FormFillingReportCreator formFillingReportCreator,
    ExternalDatabaseClient externalDatabaseClient,
    FormDataQueryTool formDataQueryTool)
{
    
    public async IAsyncEnumerable<AttachmentResult> HandleAsync(IEnumerable<int> filesIds, IEnumerable<string> thirdPartyFilesIds)
    {
        await foreach (var files in HandleAsync(filesIds))
        {
            yield return files;
        }
        
        await foreach (var files in HandleAsync(thirdPartyFilesIds))
        {
            yield return files;
        }
    }
    
    private async IAsyncEnumerable<AttachmentResult> HandleAsync<T>(IEnumerable<T> filesIds)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        
        await foreach(var fileEntry in fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesIds)))
        {
            if (fileEntry is not File<T> file)
            {
                continue;
            }

            if (!vectorizationGlobalSettings.IsSupportedContentExtraction(file.Title) ||
                file.ContentLength > vectorizationGlobalSettings.MaxContentLength)
            {
                continue;
            }

            await using var stream = await fileDao.GetFileStreamAsync(file);
            
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var slice = new Memory<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            var content = await textExtractor.ExtractAsync(slice);
            if (string.IsNullOrEmpty(content))
            {
                yield return new AttachmentResult
                {
                    File = file,
                    Success = false
                };
                
                continue;
            }

            ToolWrapper? formTool = null;
            if (file.IsForm)
            {
                var (formData, tool) = await TryGetFormDataAsync(file);
                formTool = tool;
                if (formData != null)
                {
                    content += formData;
                }
            }

            yield return new AttachmentResult
            {
                File = file,
                Success = true,
                DynamicTool = formTool,
                AttachmentContent = new AttachmentMessageContent
                {
                    Id = JsonSerializer.SerializeToElement(file.Id),
                    Title = file.Title,
                    Extension = FileUtility.GetFileExtension(file.Title),
                    Content = content
                }
            };
        }
    }

    private async Task<(string? contextText, ToolWrapper? tool)> TryGetFormDataAsync<T>(File<T> file)
    {
        if (file is not File<int> intFile)
        {
            return (null, null);
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var properties = await fileDao.GetProperties(intFile.Id);
        var formFilling = properties?.FormFilling;

        if (formFilling?.StartFilling != true || formFilling.OriginalFormId != intFile.Id)
        {
            return (null, null);
        }

        if (!externalDatabaseClient.IsEnabled())
        {
            return (null, null);
        }

        var tableName = FormFillingReportCreator.GetTableName(intFile.Id, intFile.Version);
        if (!await externalDatabaseClient.TableExistsAsync(tableName))
        {
            return (null, null);
        }

        var rowCount = await externalDatabaseClient.CountAsync(tableName);
        var columns = await formFillingReportCreator.GetColumnDefinitionsAsync(intFile.Id, intFile.Version);
        var columnList = columns.ToList();

        var tool = formDataQueryTool.Init(tableName, rowCount, columnList);
        var toolWrapper = new ToolWrapper
        {
            Tool = tool,
            Context = new ToolContext { Name = tool.Name, AutoInvoke = true }
        };

        return (FormatFormDataFromExternalDb(tableName, rowCount, columnList), toolWrapper);
    }

    private static string FormatFormDataFromExternalDb(string tableName, long rowCount, IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\n\n##Form Submissions ({rowCount} total, stored in external database)");
        sb.AppendLine($"Table: {tableName}");
        sb.AppendLine("Schema:");
        foreach (var col in columns)
        {
            sb.Append($"- {col.Name} ({col.Type})");
            if (col.EnumValues?.Count > 0)
            {
                sb.Append($": {string.Join(", ", col.EnumValues)}");
            }
            sb.AppendLine();
        }
        sb.AppendLine($"\nUse the '{FormDataQueryTool.Name}' tool to query this data with SQL SELECT statements.");
        return sb.ToString();
    }
}