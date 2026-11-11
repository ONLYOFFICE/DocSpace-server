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

namespace ASC.AI.Tests.Tests.DocumentAgentTests;

/// <summary>
/// Helpers for the portal settings of the document agent, read and written through the internal API
/// (<c>/internal/ai/document-agent/settings</c>): only the Node chat service calls it, so it has no public route.
/// </summary>
public abstract class DocumentAgentSettingsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string DocumentAgentSettingsPath = "/internal/ai/document-agent/settings";
    protected const int DefaultMaxToolCalls = 150;
    protected const int DefaultMaxFileBytes = 32 * 1024 * 1024;

    protected static UpdateDocumentAgentSettingsRequestDto BuildSettings(
        bool enabled = true,
        Guid? profileId = null,
        DocumentAgentReasoning reasoning = DocumentAgentReasoning.Default,
        int maxToolCalls = DefaultMaxToolCalls,
        int maxFileBytes = DefaultMaxFileBytes,
        bool taskLog = false,
        int? taskLogFolderId = null) =>
        new()
        {
            Enabled = enabled,
            ProfileId = profileId,
            Reasoning = reasoning,
            MaxToolCalls = maxToolCalls,
            MaxFileBytes = maxFileBytes,
            TaskLog = taskLog,
            TaskLogFolderId = taskLogFolderId
        };

    /// <summary>A whole request body as raw JSON, for values the typed request cannot carry.</summary>
    protected static string BuildRawSettings(string reasoning = "\"default\"", string taskLogFolderId = "null") =>
        $$"""
          {"enabled":true,"profileId":null,"reasoning":{{reasoning}},"maxToolCalls":{{DefaultMaxToolCalls}},"maxFileBytes":{{DefaultMaxFileBytes}},"taskLog":true,"taskLogFolderId":{{taskLogFolderId}}}
          """;

    protected async Task<HttpResponseMessage> GetSettingsResponseAsync() =>
        await _ai.GetAsync(DocumentAgentSettingsPath, TestContext.Current.CancellationToken);

    protected async Task<DocumentAgentSettingsDto> ReadSettingsAsync()
    {
        using var response = await GetSettingsResponseAsync();
        return await _ai.ReadAsync<DocumentAgentSettingsDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<HttpResponseMessage> PutSettingsAsync(UpdateDocumentAgentSettingsRequestDto body) =>
        await _ai.PutAsync(DocumentAgentSettingsPath, body, TestContext.Current.CancellationToken);

    protected async Task<HttpResponseMessage> PutRawSettingsAsync(string json) =>
        await _ai.PutRawAsync(DocumentAgentSettingsPath, json, TestContext.Current.CancellationToken);

    protected async Task<DocumentAgentSettingsDto> UpdateSettingsAsync(UpdateDocumentAgentSettingsRequestDto body)
    {
        using var response = await PutSettingsAsync(body);
        return await _ai.ReadAsync<DocumentAgentSettingsDto>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>Creates a folder in the owner's "My documents"; also signs the Files client in as the owner.</summary>
    protected async Task<int> CreateFolderAsync()
    {
        var myDocumentsId = await GetMyDocumentsFolderIdAsync();

        var folder = (await FoldersApi.CreateFolderAsync(
            myDocumentsId,
            new CreateFolderRequest($"task-log-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        return folder.Id;
    }

    /// <summary>Deletes a folder as the owner, to the trash or for good, and waits until the operation is done.</summary>
    protected async Task DeleteFolderAsync(int folderId, bool immediately)
    {
        var operations = (await FoldersApi.DeleteFolderAsync(
            folderId,
            new DeleteFolderRequest(immediately: immediately),
            TestContext.Current.CancellationToken)).Response;

        foreach (var operation in operations)
        {
            (await WaitForOperationAsync(operation.Id)).Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));
        }
    }

    protected async Task DeleteProfileInternalAsync(Guid profileId)
    {
        using var response = await _ai.DeleteAsync($"{ProfilesPath}/{profileId}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<User> InviteMemberAsync(EmployeeType type) =>
        type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);
}
