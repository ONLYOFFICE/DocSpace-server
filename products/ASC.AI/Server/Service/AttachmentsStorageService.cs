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
    TenantManager tenantManager,
    AttachmentsStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings)
{
    private static readonly TimeSpan _downloadUrlExpiration = TimeSpan.FromHours(1);

    public async IAsyncEnumerable<AttachmentResult> CreateManyAsync(HashSet<string> entryIds)
    {
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

        var intDao = daoFactory.GetFileDao<int>();
        var strDao = daoFactory.GetFileDao<string>();

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

        var created = await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), createParams);
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
        var attachment = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), id)
            ?? throw new ItemNotFoundException();

        var dataUrl = attachment.Kind == AttachmentKind.Image
            ? await GetDataUrlAsync(attachment)
            : null;

        return ToResult(attachment, dataUrl);
    }

    public async IAsyncEnumerable<AttachmentResult> ReadManyByIdsAsync(HashSet<Guid> ids)
    {
        var attachments = await storage.ReadManyByIdsAsync(tenantManager.GetCurrentTenantId(), ids);

        foreach (var attachment in attachments)
        {
            var dataUrl = attachment.Kind == AttachmentKind.Image
                ? await GetDataUrlAsync(attachment)
                : null;

            yield return ToResult(attachment, dataUrl);
        }
    }

    public async Task UpdateManyAsync(HashSet<Guid> ids, Guid messageId)
    {
        await storage.UpdateManyAsync(tenantManager.GetCurrentTenantId(), ids, messageId);
    }

    public async Task DeleteAsync(Guid id)
    {
        await storage.DeleteAsync(tenantManager.GetCurrentTenantId(), id);
    }

    public async Task DeleteManyAsync(HashSet<Guid> ids)
    {
        await storage.DeleteManyAsync(tenantManager.GetCurrentTenantId(), ids);
    }

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

            if (!await fileSecurity.CanReadAsync(file))
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
                var (hashId, _) = await daoFactory.GetMapping<string>().MappingIdAsync(strFile.Id, saveIfNotExist: true);
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

        return ToResult(attachment, dataUrl);
    }

    private static AttachmentResult ToResult(Attachment attachment, string? dataUrl)
    {
        return new AttachmentResult
        {
            Id = attachment.Id,
            Kind = attachment.Kind,
            Title = attachment.Title,
            Content = attachment.Content,
            DataUrl = dataUrl,
            EntryId = attachment.EntryId,
            ThirdpartyEntryId = attachment.ThirdpartyEntryId,
            CreatedAt = attachment.CreatedAt
        };
    }

    private async Task<string?> GetDataUrlAsync(Attachment attachment)
    {
        if (attachment.EntryId.HasValue)
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var file = await fileDao.GetFileAsync(attachment.EntryId.Value);
            return file == null ? null : await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration);
        }

        if (!string.IsNullOrEmpty(attachment.ThirdpartyEntryId))
        {
            var fileDao = daoFactory.GetFileDao<string>();
            var file = await fileDao.GetFileAsync(attachment.ThirdpartyEntryId);
            return file == null ? null : await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration);
        }

        return null;
    }
}
