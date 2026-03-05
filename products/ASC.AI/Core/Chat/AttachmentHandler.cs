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
}

[Scope]
public class AttachmentHandler(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    FormFillingReportCreator formFillingReportCreator)
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

            var formData = await TryGetFormDataAsync(file);
            if (formData != null)
            {
                content += formData;
            }

            yield return new AttachmentResult
            {
                File = file,
                Success = true,
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

    private async Task<string?> TryGetFormDataAsync<T>(File<T> file)
    {
        if (file is not File<int> intFile)
        {
            return null;
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var properties = await fileDao.GetProperties(intFile.Id);
        var formFilling = properties?.FormFilling;

        if (formFilling?.StartFilling != true || formFilling.OriginalFormId != intFile.Id)
        {
            return null;
        }

        var (metadata, submissions) = await formFillingReportCreator.GetFormSnapshotAsync(
            formFilling.RoomId, intFile.Id, intFile.Version);

        return FormatFormData(metadata, submissions);
    }

    private static string FormatFormData(
        IEnumerable<ASC.Files.Core.Services.OFormService.FormMetadata> metadata,
        IEnumerable<SubmitFormsData> submissions)
    {
        var sb = new StringBuilder();

        var metaList = metadata.ToList();
        if (metaList.Count > 0)
        {
            sb.AppendLine("\n\n##Form Fields:");
            foreach (var field in metaList)
            {
                sb.Append($"- {field.Key} ({field.Type})");
                if (field.PossibleValues?.Count > 0)
                {
                    sb.Append($": {string.Join(", ", field.PossibleValues)}");
                }
                sb.AppendLine();
            }
        }

        var submissionList = submissions.ToList();
        if (submissionList.Count > 0)
        {
            sb.AppendLine($"\n##Form Submissions ({submissionList.Count}):");
            foreach (var submission in submissionList)
            {
                var formNumber = submission.FormsData?.FirstOrDefault(f => f.Key == "FormNumber")?.Value;
                sb.AppendLine($"\nSubmission #{formNumber}:");
                foreach (var item in submission.FormsData ?? [])
                {
                    if (item.Key == "FormNumber")
                    {
                        continue;
                    }
                    sb.AppendLine($"- {item.Key}: {item.Value}");
                }
            }
        }

        return sb.ToString();
    }
}