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

namespace ASC.AI.Service;

public class AttachmentResult
{
    public required Guid Id { get; init; }
    public required AttachmentKind Kind { get; init; }
    public required string Title { get; init; }
    public string? Content { get; init; }
    public string? DataUrl { get; init; }
    public int? EntryId { get; init; }
    public string? ThirdpartyEntryId { get; init; }
    public DateTime CreatedAt { get; init; }
}

[Scope]
public class AttachmentsStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    AttachmentsStorage storage,
    MessageStorageService messageStorageService,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    ExternalDatabaseClient externalDatabaseClient,
    BuiltinFormsDatabaseClient builtinFormsDatabaseClient,
    FormFillingReportCreator formFillingReportCreator,
    ILogger<AttachmentsStorageService> logger,
    AiGateway gateway) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, gateway)
{
    private static readonly TimeSpan _downloadUrlExpiration = TimeSpan.FromHours(1);
    private static readonly EmployeeType[] _allowedTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async IAsyncEnumerable<AttachmentResult> CreateManyAsync(HashSet<string> entryIds)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        if (entryIds.Count == 0)
        {
            yield break;
        }

        var internalIds = new HashSet<int>();
        var thirdpartyIds = new HashSet<string>();

        foreach (var entryId in entryIds)
        {
            if (int.TryParse(entryId, out var id))
            {
                internalIds.Add(id);
            }
            else
            {
                thirdpartyIds.Add(entryId);
            }
        }

        var intDao = DaoFactory.GetFileDao<int>();
        var strDao = DaoFactory.GetFileDao<string>();

        var internalFiles = await LoadFilesAsync(intDao, internalIds);
        var thirdpartyFiles = await LoadFilesAsync(strDao, thirdpartyIds);

        var createParams = new List<CreateAttachmentParams>(entryIds.Count);

        foreach (var file in internalFiles)
        {
            createParams.Add(await BuildParamAsync(intDao, file));
        }

        foreach (var file in thirdpartyFiles)
        {
            createParams.Add(await BuildParamAsync(strDao, file));
        }

        var created = await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, createParams);
        var index = 0;

        foreach (var file in internalFiles)
        {
            yield return await ToResultAsync(intDao, created[index++], file);
        }

        foreach (var file in thirdpartyFiles)
        {
            yield return await ToResultAsync(strDao, created[index++], file);
        }
    }

    public async Task<AttachmentResult> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var attachment = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id)
            ?? throw new ItemNotFoundException();

        return await ToResultAsync(attachment);
    }

    public async IAsyncEnumerable<AttachmentResult> ReadManyByIdsAsync(HashSet<Guid> ids)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var attachments = await storage.ReadManyByIdsAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, ids);

        foreach (var attachment in attachments)
        {
            yield return await ToResultAsync(attachment);
        }
    }

    public async Task UpdateManyAsync(HashSet<Guid> ids, Guid messageId)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var message = await messageStorageService.ReadByIdAsync(messageId);

        await storage.UpdateManyAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, ids, message.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        await storage.DeleteAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id);
    }

    public async Task DeleteManyAsync(HashSet<Guid> ids)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        await storage.DeleteManyAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, ids);
    }

    /// <summary>
    /// Reports whether an attached file is a started filling-form whose submissions can be analysed by the
    /// form-data tools, plus the field keys/types that make up its report columns. Returns (false, []) when no
    /// forms database is configured, the file is not such a form, or its submission table does not yet exist.
    /// </summary>
    public async Task<(bool CanAnalyze, IReadOnlyList<(string Key, string Type)> Keys)> GetFormAnalysisInfoAsync(int fileId)
    {
        var client = GetEnabledFormsDatabaseClient();
        if (client == null)
        {
            return (false, []);
        }

        try
        {
            var fileDao = DaoFactory.GetFileDao<int>();
            var file = await fileDao.GetFileAsync(fileId);
            if (file is not { IsForm: true })
            {
                return (false, []);
            }

            var properties = await fileDao.GetProperties(fileId);
            var formFilling = properties?.FormFilling;
            if (formFilling?.StartFilling != true || formFilling.OriginalFormId != fileId)
            {
                return (false, []);
            }

            var tableName = FormFillingReportCreator.GetTableName(fileId, file.Version);
            if (!await client.TableExistsAsync(tableName))
            {
                return (false, []);
            }

            var fields = await formFillingReportCreator.GetFormFieldsMetadataAsync(fileId, file.Version);
            return (true, fields.Select(f => (f.Key, f.Type)).ToList());
        }
        catch (Exception e)
        {
            logger.WarnFormAnalysisFailed(e, fileId);
            return (false, []);
        }
    }

    private IFormsDatabaseClient? GetEnabledFormsDatabaseClient() =>
        externalDatabaseClient.IsEnabled() ? externalDatabaseClient :
        builtinFormsDatabaseClient.IsEnabled() ? builtinFormsDatabaseClient :
        null;

    private async Task<List<File<T>>> LoadFilesAsync<T>(IFileDao<T> fileDao, IReadOnlyCollection<T> entryIds)
    {
        if (entryIds.Count == 0)
        {
            return [];
        }

        var files = new List<File<T>>(entryIds.Count);
        await foreach (var file in fileDao.GetFilesAsync(entryIds))
        {
            if (file == null)
            {
                continue;
            }

            if (!await FileSecurity.CanReadAsync(file))
            {
                throw new SecurityException();
            }

            files.Add(file);
        }

        return files;
    }

    private async Task<CreateAttachmentParams> BuildParamAsync<T>(IFileDao<T> fileDao, File<T> file)
    {
        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        int? internalEntryId = null;
        string? thirdpartyEntryId = null;

        switch (file)
        {
            case File<int> intFile:
                internalEntryId = intFile.Id;
                break;
            case File<string> strFile:
                var (hashId, _) = await DaoFactory.GetMapping<string>().MappingIdAsync(strFile.Id, saveIfNotExist: true);
                thirdpartyEntryId = hashId;
                break;
        }

        if (fileType == FileType.Image)
        {
            return new CreateAttachmentParams
            {
                Kind = AttachmentKind.Image,
                Title = file.Title,
                EntryId = internalEntryId,
                ThirdpartyEntryId = thirdpartyEntryId
            };
        }

        if (!vectorizationGlobalSettings.IsSupportedContentExtraction(file.Title))
        {
            throw new ArgumentException($"File '{file.Title}' has an unsupported format");
        }

        await using var stream = await fileDao.GetFileStreamAsync(file);

        var content = await textExtractor.ExtractAsync(stream, file.ContentLength);
        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException($"Failed to extract content from file '{file.Title}'");
        }

        return new CreateAttachmentParams
        {
            Kind = AttachmentKind.File,
            Title = file.Title,
            Content = content,
            EntryId = internalEntryId,
            ThirdpartyEntryId = thirdpartyEntryId
        };
    }

    private static async Task<AttachmentResult> ToResultAsync<T>(IFileDao<T> fileDao, Attachment attachment, File<T> file)
    {
        var dataUrl = attachment.Kind == AttachmentKind.Image
            ? await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration)
            : null;

        return ToResult(attachment, dataUrl, file is File<string> thirdpartyFile ? thirdpartyFile.Id : null);
    }

    private async Task<AttachmentResult> ToResultAsync(Attachment attachment)
    {
        var thirdpartyEntryId = await ResolveThirdpartyEntryIdAsync(attachment.ThirdpartyEntryId);

        var dataUrl = attachment.Kind == AttachmentKind.Image
            ? await GetDataUrlAsync(attachment.EntryId, thirdpartyEntryId)
            : null;

        return ToResult(attachment, dataUrl, thirdpartyEntryId);
    }

    private async Task<string?> ResolveThirdpartyEntryIdAsync(string? hashId)
    {
        if (string.IsNullOrEmpty(hashId))
        {
            return null;
        }

        var (entryId, _) = await DaoFactory.GetMapping<string>().MappingIdAsync(hashId);

        return string.IsNullOrEmpty(entryId) ? null : entryId;
    }

    private static AttachmentResult ToResult(Attachment attachment, string? dataUrl, string? thirdpartyEntryId)
    {
        return new AttachmentResult
        {
            Id = attachment.Id,
            Kind = attachment.Kind,
            Title = attachment.Title,
            Content = attachment.Content,
            DataUrl = dataUrl,
            EntryId = attachment.EntryId,
            ThirdpartyEntryId = thirdpartyEntryId,
            CreatedAt = attachment.CreatedAt
        };
    }

    private async Task<string?> GetDataUrlAsync(int? entryId, string? thirdpartyEntryId)
    {
        if (entryId.HasValue)
        {
            var fileDao = DaoFactory.GetFileDao<int>();
            var file = await fileDao.GetFileAsync(entryId.Value);
            return file == null ? null : await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration);
        }

        if (!string.IsNullOrEmpty(thirdpartyEntryId))
        {
            var fileDao = DaoFactory.GetFileDao<string>();
            var file = await fileDao.GetFileAsync(thirdpartyEntryId);
            return file == null ? null : await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration);
        }

        return null;
    }
}

internal static partial class AttachmentsStorageServiceLogger
{
    [LoggerMessage(LogLevel.Warning, "Form analysis check failed for file {fileId}")]
    public static partial void WarnFormAnalysisFailed(this ILogger<AttachmentsStorageService> logger, Exception exception, int fileId);
}
