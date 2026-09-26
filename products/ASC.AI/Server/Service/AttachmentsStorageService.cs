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
    public bool CanAnalyze { get; init; }
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
    FormSchemaProvider formSchemaProvider,
    FormAnalyzeIntent formAnalyzeIntent,
    IFusionCache fusionCache,
    AiSocketManager aiSocketManager,
    ILogger<AttachmentsStorageService> logger,
    AiGateway gateway) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, gateway)
{
    private static readonly TimeSpan _downloadUrlExpiration = TimeSpan.FromHours(1);

    // A form with hundreds of fields would blow the model's context and time budget.
    private const int MaxPromptColumns = 60;
    private const int MaxEnumValuesPerColumn = 10;

    private static readonly TimeSpan _formQuestionsCacheDuration = TimeSpan.FromHours(12);
    private static readonly FormAnalysisDto _unavailableFormAnalysis = new() { Status = "unavailable" };

    private static readonly EmployeeType[] _allowedTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    public async IAsyncEnumerable<AttachmentResult> CreateManyAsync(HashSet<string> entryIds, HashSet<string> analyzeEntryIds)
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

        // Only files the client marked analyzeOnly are analysed: for the rest nothing form-analysis runs
        // (no external-DB probe, no model warm-up, no intent). Done before the loop, which is consumed
        // lazily, so the model calls do not spread across the whole response.
        var analyzeFiles = internalFiles.Where(f => analyzeEntryIds.Contains(f.Id.ToString())).ToList();
        var analyses = await AnalyzeFormsAsync(analyzeFiles);

        foreach (var file in internalFiles)
        {
            var attachment = created[index++];
            var analysis = analyses.GetValueOrDefault(file.Id);

            // Remember the intent per attachment, so another chat that attaches the same form is isolated.
            if (analysis is { CanAnalyze: true })
            {
                await formAnalyzeIntent.SetAsync(attachment.Id);
            }

            yield return await ToResultAsync(intDao, attachment, file, analysis);
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

        return await ToResultAsync(attachment, withFormAnalysis: true);
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
    /// The attached files that are started filling-forms with an analysable submission table, and the
    /// starter questions to offer for them. Anything else is absent from the result.
    /// </summary>
    private async Task<Dictionary<int, FormAnalysis>> AnalyzeFormsAsync(List<File<int>> files)
    {
        if (!externalDatabaseClient.IsEnabled() || !files.Exists(f => f.IsPdf))
        {
            return [];
        }

        var analyses = await Task.WhenAll(files
            .Where(f => f.IsPdf)
            .Select(async file => (file.Id, Analysis: await AnalyzeFormAsync(file))));

        return analyses.ToDictionary(pair => pair.Id, pair => pair.Analysis);
    }

    private async Task<FormAnalysis> AnalyzeFormAsync(File<int> file)
    {
        try
        {
            return await formSchemaProvider.TryGetTableNameAsync(file) is null
                ? FormAnalysis.None
                : new FormAnalysis(true);
        }
        catch (Exception e)
        {
            logger.WarnFormAnalysisFailed(e, file.Id);
            return FormAnalysis.None;
        }
    }

    /// <summary>
    /// The form starter-questions state for ASC.NewAi, which runs the model call: "unavailable" (not an
    /// analysable launched form), "ready" (cached questions), or "generate" (the caller should generate
    /// from the returned schema and post the result back with <see cref="SaveFormQuestionsAsync"/>).
    /// </summary>
    public async Task<FormAnalysisDto> GetFormAnalysisAsync(Guid attachmentId)
    {
        if (!externalDatabaseClient.IsEnabled() || !await formAnalyzeIntent.GetAsync(attachmentId))
        {
            return _unavailableFormAnalysis;
        }

        try
        {
            var file = await ResolveFormAsync(attachmentId);
            if (file is null)
            {
                return _unavailableFormAnalysis;
            }

            var culture = CultureInfo.CurrentUICulture;

            var cached = await fusionCache.TryGetAsync<List<FormQuestionDto>>(GetFormQuestionsCacheKey(tenantManager.GetCurrentTenantId(), file, culture));
            if (cached is { HasValue: true, Value.Count: > 0 })
            {
                return new FormAnalysisDto { Status = "ready", Questions = cached.Value };
            }

            var schema = await formSchemaProvider.TryReadAsync(file);
            if (schema is null || schema.RowCount == 0 || schema.Columns.Count == 0)
            {
                return _unavailableFormAnalysis;
            }

            return new FormAnalysisDto { Status = "generate", Schema = ToSchemaDto(file, schema, culture) };
        }
        catch (Exception e)
        {
            logger.WarnFormQuestionsFailed(e, attachmentId);
            return _unavailableFormAnalysis;
        }
    }

    /// <summary>Caches questions ASC.NewAi generated for the attachment's form, so later polls are instant.</summary>
    public async Task SaveFormQuestionsAsync(Guid attachmentId, IReadOnlyList<FormQuestionDto> questions)
    {
        if (questions.Count == 0 || !await formAnalyzeIntent.GetAsync(attachmentId))
        {
            return;
        }

        try
        {
            var file = await ResolveFormAsync(attachmentId);
            if (file is null)
            {
                return;
            }

            await fusionCache.SetAsync(
                GetFormQuestionsCacheKey(tenantManager.GetCurrentTenantId(), file, CultureInfo.CurrentUICulture),
                questions.ToList(),
                opt => opt.SetDuration(_formQuestionsCacheDuration));

            // Push to the chat client over the socket so the questions arrive without a long-poll.
            await aiSocketManager.SendFormQuestionsAsync(attachmentId, questions);
        }
        catch (Exception e)
        {
            logger.WarnFormQuestionsFailed(e, attachmentId);
        }
    }

    private async Task<File<int>?> ResolveFormAsync(Guid attachmentId)
    {
        var attachment = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, attachmentId);
        if (attachment?.EntryId is not { } fileId)
        {
            return null;
        }

        var file = await DaoFactory.GetFileDao<int>().GetFileAsync(fileId);
        return file is { IsPdf: true } && await FileSecurity.CanReadAsync(file) ? file : null;
    }

    private static FormSchemaDto ToSchemaDto(File<int> file, FormSchema schema, CultureInfo culture)
    {
        var columns = schema.Columns
            .Take(MaxPromptColumns)
            .Select(c => new FormColumnDto
            {
                Name = c.Name,
                Label = c.Label is not null && c.Label != c.Name ? c.Label : null,
                Type = c.Type.ToString(),
                Values = c.EnumValues is { Count: > 0 } values ? values.Take(MaxEnumValuesPerColumn).ToList() : null
            })
            .ToList();

        return new FormSchemaDto
        {
            Title = file.Title,
            RowCount = schema.RowCount,
            Columns = columns,
            Culture = culture.Name,
            CultureName = culture.EnglishName
        };
    }

    private static string GetFormQuestionsCacheKey(int tenantId, File<int> file, CultureInfo culture)
    {
        return $"ai:form:preanalysis:{tenantId}:{file.Id}:{file.Version}:{culture.Name}";
    }

    /// <summary>
    /// The analysable flag for a single read, from whether the form has a submission table. Batch reads
    /// skip it: they hydrate whole threads, and a file lookup per attachment would not pay for itself.
    /// The starter questions are fetched separately from the long-poll endpoint.
    /// </summary>
    private async Task<FormAnalysis?> ReadFormAnalysisAsync(Attachment attachment)
    {
        if (attachment.EntryId is not { } entryId || !externalDatabaseClient.IsEnabled())
        {
            return null;
        }

        try
        {
            var file = await DaoFactory.GetFileDao<int>().GetFileAsync(entryId);
            if (file is not { IsPdf: true })
            {
                return null;
            }

            return await formSchemaProvider.TryGetTableNameAsync(file) is null ? null : new FormAnalysis(true);
        }
        catch (Exception e)
        {
            logger.WarnFormAnalysisFailed(e, entryId);
            return null;
        }
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

    private static async Task<AttachmentResult> ToResultAsync<T>(IFileDao<T> fileDao, Attachment attachment, File<T> file, FormAnalysis? analysis = null)
    {
        var dataUrl = attachment.Kind == AttachmentKind.Image
            ? await fileDao.GetPreSignedUriAsync(file, _downloadUrlExpiration)
            : null;

        return ToResult(attachment, dataUrl, file is File<string> thirdpartyFile ? thirdpartyFile.Id : null, analysis);
    }

    private async Task<AttachmentResult> ToResultAsync(Attachment attachment, bool withFormAnalysis = false)
    {
        var thirdpartyEntryId = await ResolveThirdpartyEntryIdAsync(attachment.ThirdpartyEntryId);

        var dataUrl = attachment.Kind == AttachmentKind.Image
            ? await GetDataUrlAsync(attachment.EntryId, thirdpartyEntryId)
            : null;

        var analysis = withFormAnalysis ? await ReadFormAnalysisAsync(attachment) : null;

        return ToResult(attachment, dataUrl, thirdpartyEntryId, analysis);
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

    private static AttachmentResult ToResult(Attachment attachment, string? dataUrl, string? thirdpartyEntryId, FormAnalysis? analysis = null)
    {
        analysis ??= FormAnalysis.None;

        return new AttachmentResult
        {
            Id = attachment.Id,
            Kind = attachment.Kind,
            Title = attachment.Title,
            Content = attachment.Content,
            DataUrl = dataUrl,
            EntryId = attachment.EntryId,
            ThirdpartyEntryId = thirdpartyEntryId,
            CreatedAt = attachment.CreatedAt,
            CanAnalyze = analysis.CanAnalyze
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

    private sealed record FormAnalysis(bool CanAnalyze)
    {
        public static readonly FormAnalysis None = new(false);
    }
}

internal static partial class AttachmentsStorageServiceLogger
{
    [LoggerMessage(LogLevel.Warning, "Form analysis check failed for file {fileId}")]
    public static partial void WarnFormAnalysisFailed(this ILogger<AttachmentsStorageService> logger, Exception exception, int fileId);

    [LoggerMessage(LogLevel.Warning, "Form starter-questions failed for attachment {attachmentId}")]
    public static partial void WarnFormQuestionsFailed(this ILogger<AttachmentsStorageService> logger, Exception exception, Guid attachmentId);
}
