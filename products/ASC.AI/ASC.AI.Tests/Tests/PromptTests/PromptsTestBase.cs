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

namespace ASC.AI.Tests.Tests.PromptTests;

/// <summary>
/// Shared plumbing of the prompt suites (<c>/api/2.0/ai/prompts</c>, the Node chat service): setup helpers that fail the
/// test when the service refuses, a snapshot of the caller's whole library read through <c>export</c>, and the raw
/// requests for what the typed client cannot say.
/// </summary>
/// <remarks>
/// Raw HTTP is used in three places only. A wrong-typed or missing field (<c>"name": 5</c>) cannot be built from the
/// DTOs. A bundle with a version other than 1 or a prompt without a text cannot either: the bundle version is a
/// one-member enum and the constructors reject null. And <c>update</c> is an SDK defect:
/// <c>AiPromptsUpdateRequestUpdates.FolderId</c> is emitted even when null, so every typed update also asks to move the
/// prompt to the root; an update that must leave the prompt in its folder goes through <see cref="UpdateRawAsync"/>.
/// </remarks>
public abstract class PromptsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>Well-formed ids that no library holds.</summary>
    protected const string UnknownPromptId = "019fcc1d-2c4d-7557-b8d2-6b4f1be1b212";
    protected const string UnknownFolderId = "019fcc1d-2ccd-7274-974a-cc335f583f58";

    protected const string NameRequired = "Name is required";
    protected const string PromptNameTaken = "Prompt name already exists in this folder";
    protected const string FolderNameTaken = "Folder name already exists";

    /// <summary>The creation date is stored in whole seconds: two creates are ordered only when a second lies between them.</summary>
    protected const int NextSecond = 1100;

    private const string PromptsRoute = "/api/2.0/ai/prompts";

    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    protected sealed record PromptRow(string Id, string Name, string Text, string? FolderId, decimal CreatedAt, decimal UpdatedAt);

    protected sealed record FolderRow(string Id, string Name, decimal CreatedAt, decimal UpdatedAt);

    /// <summary>Everything the caller owns as plain data, ordered by id, timestamps included.</summary>
    protected sealed record Library(List<FolderRow> Folders, List<PromptRow> Prompts);

    /// <summary>The library without its ids: the folder names and one <c>folder/name: text</c> line per prompt.</summary>
    protected sealed record Shape(List<string> Folders, List<string> Prompts);

    // ------------------------------------------------------------------ setup

    protected async Task<AiPrompt> CreatePromptAsync(string name, string text, string? folderId = null)
    {
        var result = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput(name, text, folderId!), Token);
        result.Success.Should().BeTrue($"prompts/create rejected the prompt: {result.Error?.Message}");

        return result.Prompt;
    }

    protected async Task<AiPromptFolder> CreateFolderAsync(string name)
    {
        var result = await _aiPrompts.AiPromptsCreateFolderAsync(name, Token);
        result.Success.Should().BeTrue($"prompts/create-folder rejected the folder: {result.Error?.Message}");

        return result.Folder;
    }

    // ------------------------------------------------------------------ reads

    protected async Task<List<string>> ListIdsAsync(string? folderId = null) =>
        (await _aiPrompts.AiPromptsListAsync(folderId, Token)).Select(p => p.Id).ToList();

    protected async Task<List<string>> ListFolderIdsAsync() =>
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Select(f => f.Id).ToList();

    /// <summary>The prompt, or null when the id is unknown (the route answers 200 with a null body).</summary>
    protected async Task<AiPrompt?> FindPromptAsync(string id)
    {
        var response = await _aiPrompts.AiPromptsGetByIdWithHttpInfoAsync(id, Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response.Data;
    }

    /// <summary>The folder, or null when the id is unknown (the route answers 200 with a null body).</summary>
    protected async Task<AiPromptFolder?> FindFolderAsync(string id)
    {
        var response = await _aiPrompts.AiPromptsGetFolderByIdWithHttpInfoAsync(id, Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response.Data;
    }

    protected async Task<Library> ReadLibraryAsync()
    {
        var bundle = await _aiPrompts.AiPromptsExportAsync(Token);

        return new Library(
            bundle.Folders
                .Select(f => new FolderRow(f.Id, f.Name, f.CreatedAt, f.UpdatedAt))
                .OrderBy(f => f.Id, StringComparer.Ordinal)
                .ToList(),
            bundle.Prompts
                .Select(p => new PromptRow(p.Id, p.Name, p.Text, p.FolderId, p.CreatedAt, p.UpdatedAt))
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .ToList());
    }

    protected static Shape ShapeOf(Library library)
    {
        var folderNames = library.Folders.ToDictionary(f => f.Id, f => f.Name);

        return new Shape(
            library.Folders.Select(f => f.Name).Order(StringComparer.Ordinal).ToList(),
            library.Prompts
                .Select(p => $"{(p.FolderId is null ? "<root>" : folderNames[p.FolderId])}/{p.Name}: {p.Text}")
                .Order(StringComparer.Ordinal)
                .ToList());
    }

    /// <summary>A library to import into or compare against: two folders (one empty), one prompt inside the first, one at the root.</summary>
    protected async Task<AiPromptFolder> SeedLibraryAsync()
    {
        var folder = await CreateFolderAsync("Autotest S");
        await CreateFolderAsync("Autotest empty");
        await CreatePromptAsync("Autotest SP", "in S\nsecond line — 界 🚀", folder.Id);
        await CreatePromptAsync("Autotest SR", "root");

        return folder;
    }

    /// <summary>Waits out a second so the next create gets a later creation date.</summary>
    protected static Task NextSecondAsync() => Task.Delay(NextSecond, Token);

    // ------------------------------------------------------------------ failures

    /// <summary>
    /// Runs a call that must be refused. When <paramref name="message"/> is given the body is the engine's refusal
    /// (<c>{success: false, error: {message}}</c>) and must say exactly that.
    /// </summary>
    protected static async Task<ApiException> ExpectRefusalAsync(Func<Task> call, string? message, params int[] statuses)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(call);
        var content = exception.ErrorContent?.ToString();

        exception.ErrorCode.Should().BeOneOf(statuses, content);

        if (message is not null)
        {
            var body = BodyOf(exception);
            body.GetProperty("success").GetBoolean().Should().BeFalse(content);
            body.GetProperty("error").GetProperty("message").GetString().Should().Be(message, content);
        }

        return exception;
    }

    protected static JsonElement BodyOf(ApiException exception) => ParseBody(exception.ErrorContent?.ToString() ?? "null");

    protected static JsonElement ParseBody(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.Clone();
    }

    /// <summary>Runs a call whose outcome the test does not pin (only what it left behind): a refusal is swallowed.</summary>
    protected static async Task IgnoreRefusalAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (ApiException)
        {
            // Refused; the caller looks at the library.
        }
    }

    // ------------------------------------------------------------------ raw requests

    protected static string Json(object? value) => JsonSerializer.Serialize(value);

    protected async Task<(int Status, string Body)> SendRawAsync(HttpMethod method, string route, string? json = null)
    {
        using var request = new HttpRequestMessage(method, $"{PromptsRoute}/{route}");

        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, Token);

        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(Token));
    }

    protected Task<(int Status, string Body)> GetRawAsync(string route) => SendRawAsync(HttpMethod.Get, route);

    /// <summary>
    /// A partial update that leaves the prompt where it is. The typed request always carries <c>folderId: null</c>,
    /// which moves the prompt to the root, so a prompt that sits in a folder is updated through here.
    /// </summary>
    protected Task<(int Status, string Body)> UpdateRawAsync(string id, Dictionary<string, object?> updates) =>
        SendRawAsync(HttpMethod.Put, "update", Json(new { id, updates }));

    /// <summary>The export as the service wrote it, to feed back into an import untouched.</summary>
    protected async Task<string> ExportRawAsync()
    {
        var (status, body) = await GetRawAsync("export");
        status.Should().Be(200, body);

        return body;
    }

    /// <summary>
    /// <c>import-bundle</c> with a hand-written body: <paramref name="members"/> are the members of the body object
    /// (<c>"bundle": {...}</c>), <paramref name="mode"/> the import mode, or null to send no options.
    /// </summary>
    protected Task<(int Status, string Body)> ImportRawAsync(string members, string? mode)
    {
        var options = mode is null ? string.Empty : $"\"options\":{{\"mode\":\"{mode}\"}}";
        var parts = new[] { members, options }.Where(p => p.Length > 0);

        return SendRawAsync(HttpMethod.Post, "import-bundle", "{" + string.Join(",", parts) + "}");
    }

    protected Task<(int Status, string Body)> ImportBundleRawAsync(string bundle, string? mode) =>
        ImportRawAsync($"\"bundle\":{bundle}", mode);

    /// <summary>A bundle document: <c>{"version": 1, "folders": [...], "prompts": [...]}</c>.</summary>
    protected static string BundleJson(string folders, string prompts, string version = "1") =>
        $"{{\"version\":{version},\"folders\":[{folders}],\"prompts\":[{prompts}]}}";

    /// <summary>The members a field of the wrong type is sent as (<c>"name": 5</c>); empty leaves the field out.</summary>
    protected static TheoryData<string, string> BadValues(string field) => new()
    {
        { "missing", string.Empty },
        { "null", $"\"{field}\":null" },
        { "a number", $"\"{field}\":5" },
        { "a boolean", $"\"{field}\":true" },
        { "an array", $"\"{field}\":[\"a\"]" },
        { "an object", $"\"{field}\":{{\"a\":1}}" }
    };

    /// <summary>A JSON object from members written out by hand; empty members are skipped.</summary>
    protected static string ObjectOf(params string[] members) => "{" + string.Join(",", members.Where(m => m.Length > 0)) + "}";

    /// <summary>The typed request cannot be built with a null folder (the constructor rejects it), but the property takes one: the root.</summary>
    protected static AiPromptsMoveRequest MoveToRoot(string id) => new(id, string.Empty) { FolderId = null! };

    protected sealed record ImportCounts(int Folders, int Prompts);

    protected sealed record ImportOutcome(bool Success, ImportCounts Imported);

    /// <summary>
    /// Imports a version 1 bundle built from the typed models and returns the counts; a refusal throws the
    /// <see cref="ApiException"/> the typed call would have.
    /// </summary>
    /// <remarks>
    /// Sent raw on purpose, an SDK defect: <c>AiPromptBundle.Version</c> is an enum whose member serializes as the
    /// string <c>"1"</c>, while the service only accepts the number 1 ("Unsupported bundle version: 1"), so no typed
    /// import can succeed.
    /// </remarks>
    protected async Task<ImportOutcome> ImportAsync(List<AiPromptFolder> folders, List<AiPrompt> prompts, AiImportMode? mode)
    {
        var bundle = new
        {
            version = 1,
            folders = folders.Select(f => new { id = f.Id, name = f.Name }),
            prompts = prompts.Select(p => new { id = p.Id, name = p.Name, text = p.Text, folderId = p.FolderId })
        };

        var (status, body) = await ImportBundleRawAsync(Json(bundle), mode?.ToString().ToLowerInvariant());

        if (status is < 200 or >= 300)
        {
            throw new ApiException(status, $"Error calling AiPromptsImportBundle: {body}", body);
        }

        var imported = ParseBody(body).GetProperty("imported");

        return new ImportOutcome(true, new ImportCounts(imported.GetProperty("folders").GetInt32(), imported.GetProperty("prompts").GetInt32()));
    }
}
