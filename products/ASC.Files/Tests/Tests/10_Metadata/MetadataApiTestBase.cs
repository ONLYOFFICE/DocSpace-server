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

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// The helpers of the metadata API suites that exercise the templates, the fields and the metadata of single entries:
/// a template with a known field, the status an action answers with, the wait for a cascade pass.
/// </summary>
public abstract class MetadataApiTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const int MissingId = 99999999;

    protected static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    /// <summary>
    /// Creates a visible template with a unique name; without <paramref name="fields"/> it holds one string field.
    /// </summary>
    protected async Task<MetadataTemplateDto> CreateTemplateAsync(List<MetadataFieldRequest>? fields = null, string? name = null, bool visible = true)
    {
        fields ??= [new MetadataFieldRequest("Field " + Suffix(), MetadataFieldType.String)];

        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name ?? "Template " + Suffix(), visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<MetadataTemplateDto> GetTemplateAsync(int templateId)
    {
        return (await _metadataApi.GetTemplateAsync(templateId, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<EntryMetadataDto> GetFileMetadataAsync(int fileId)
    {
        return (await _metadataApi.GetFileMetadataAsync(fileId, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<EntryMetadataDto> GetFolderMetadataAsync(int folderId)
    {
        return (await _metadataApi.GetFolderMetadataAsync(folderId, TestContext.Current.CancellationToken)).Response;
    }

    protected Task AssignToFolderAsync(int folderId, params int[] templateIds)
    {
        return _metadataApi.AssignFolderTemplatesAsync(folderId, new AssignMetadataTemplates([.. templateIds]), TestContext.Current.CancellationToken);
    }

    protected Task AssignToFileAsync(int fileId, params int[] templateIds)
    {
        return _metadataApi.AssignFileTemplatesAsync(fileId, new AssignMetadataTemplates([.. templateIds]), TestContext.Current.CancellationToken);
    }

    protected Task SetFileStringAsync(int fileId, int fieldId, string value)
    {
        return _metadataApi.SetFileValuesAsync(fileId, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: value)]), TestContext.Current.CancellationToken);
    }

    protected Task SetFolderStringAsync(int folderId, int fieldId, string value)
    {
        return _metadataApi.SetFolderValuesAsync(folderId, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: value)]), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The status an action answers with: 200 when it succeeds, the HTTP status of the <see cref="ApiException"/> otherwise.
    /// </summary>
    protected static async Task<int> StatusOfAsync(Func<Task> action)
    {
        try
        {
            await action();

            return 200;
        }
        catch (ApiException exception)
        {
            return exception.ErrorCode;
        }
    }

    /// <summary>
    /// The value of the field on the entry, or <c>null</c> when the template, the field or the value is absent.
    /// </summary>
    protected static MetadataValueDto? ValueOf(EntryMetadataDto metadata, int templateId, int fieldId)
    {
        return metadata.Templates?.FirstOrDefault(t => t.Id == templateId)?.Fields?.FirstOrDefault(f => f.Id == fieldId)?.Value;
    }

    /// <summary>
    /// Starts a cascading assignment and waits for exactly that pass.
    /// </summary>
    protected async Task<MetadataOperationDto> AssignWithCascadeAsync(int folderId, int[] templateIds, MetadataConflictResolveType? conflictResolveType = null)
    {
        var started = (await _metadataApi.AssignFolderTemplatesAsync(folderId, new AssignMetadataTemplates([.. templateIds], cascade: true, conflictResolveType), TestContext.Current.CancellationToken)).Response;

        started.Id.Should().NotBeNullOrEmpty("a cascading assignment queues a pass");

        return await WaitForCascadeAsync(folderId, started.Id);
    }

    /// <summary>
    /// Turns the cascade of an assigned template on and waits for exactly that pass.
    /// </summary>
    protected async Task<MetadataOperationDto> UpdateWithCascadeAsync(int folderId, int templateId, MetadataConflictResolveType? conflictResolveType = null)
    {
        var started = (await _metadataApi.UpdateFolderTemplateAsync(folderId, templateId, new UpdateFolderMetadataTemplateRequest(cascade: true, conflictResolveType), TestContext.Current.CancellationToken)).Response;

        started.Id.Should().NotBeNullOrEmpty("turning the cascade on queues a pass");

        return await WaitForCascadeAsync(folderId, started.Id);
    }

    /// <summary>
    /// Polls the cascade progress of the folder until the pass <paramref name="operationId"/> is completed; a report of
    /// another pass is ignored. Ends on a deadline by returning the last state, so the assertion shows what was there.
    /// </summary>
    protected async Task<MetadataOperationDto> WaitForCascadeAsync(int folderId, string operationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var state = (await _metadataApi.GetCascadeProgressAsync(folderId, TestContext.Current.CancellationToken)).Response;

            if ((state.Id == operationId && state.IsCompleted) || DateTime.UtcNow >= deadline)
            {
                state.Id.Should().Be(operationId, "the pass must have been reported");
                state.IsCompleted.Should().BeTrue("the pass must finish: {0}", state.Error);
                state.Error.Should().BeNullOrEmpty();
                state.Progress.Should().Be(100);

                return state;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }
}
