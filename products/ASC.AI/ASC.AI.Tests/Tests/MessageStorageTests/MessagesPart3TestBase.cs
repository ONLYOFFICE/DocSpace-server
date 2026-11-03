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


namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// Helpers shared by the message-level thread suites (<c>ThreadMessages*</c>) and the suites of
/// <c>POST /api/2.0/ai/text-to-docx</c>, the route that replaced the removed per-message export
/// (<c>MessagesPermission*</c>, <c>MessagesAiDisabled*</c>).
/// </summary>
public abstract class MessagesPart3TestBase(AspireAppFixture fixture) : MessageTestBase(fixture)
{
    protected override bool NeedsDocumentServer => true;

    protected const string TextToDocxPath = "/api/2.0/ai/text-to-docx";
    protected const string ReadMessagesPath = "/api/2.0/ai/threads/read-messages";
    protected const string GetThreadByIdPath = "/api/2.0/ai/threads/get-by-id";
    protected const string GetMessageByIdPath = "/api/2.0/ai/threads/get-message-by-id";
    protected const string DeleteMessagePath = "/api/2.0/ai/threads/delete-message";

    /// <summary>A thread id that has the right shape but was never issued.</summary>
    protected const string UnknownId = "019ed117-0000-7000-8000-000000000000";

    /// <summary>An id that is not a UUID at all.</summary>
    protected const string MalformedId = "not-a-guid";

    /// <summary>
    /// Texts that stress the write path: non-Latin letters, emoji and a zero-width space, significant whitespace,
    /// quoting and markup characters, and a long body. Keyed so a theory row names its case instead of printing
    /// 20 000 characters.
    /// </summary>
    protected static IReadOnlyDictionary<string, string> AwkwardTexts { get; } = new Dictionary<string, string>
    {
        ["cyrillic"] = "Привет, как дела? №1 — «ёлка»",
        ["emoji"] = "emoji 😀🚀 and a zero-width​joiner",
        ["whitespace"] = "line one\nline two\n\nline four with trailing spaces   ",
        ["markup"] = "quotes \"double\" 'single' `tick` <b>markup</b> & &amp; % _ \\ /",
        ["long"] = new string('x', 20000)
    };

    /// <summary>The order <see cref="AwkwardTexts"/> are stored in when a test writes all of them.</summary>
    protected static string[] AwkwardKeys { get; } = ["cyrillic", "emoji", "whitespace", "markup", "long"];

    /// <summary>
    /// How long a refused export is given to land anyway, so "no file was created" is a real absence rather than an
    /// unfinished job. The TS suite measured ~10 s between the 202 and the file at worst.
    /// </summary>
    protected static TimeSpan ExportSettle { get; } = TimeSpan.FromSeconds(20);

    /// <summary>How long an accepted export is waited for.</summary>
    protected static TimeSpan ExportTimeout { get; } = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    // ---------------------------------------------------------------------------------------------------------------
    // Threads and messages

    /// <summary>Creates a thread bound to the test's default profile; returns its id.</summary>
    protected async Task<string> NewThreadAsync(string title) => (await CreateThreadAsync(title)).ThreadId;

    protected async Task<List<string?>> ReadTextsAsync(string threadId) =>
        [.. (await ReadMessagesAsync(threadId)).Select(TextOf)];

    /// <summary>A message reduced to what the API serialises, for "is this the same message" comparisons.</summary>
    protected static string Snapshot(AiThreadMessageLike message) => message.ToJson();

    protected Task<AiThread> GetThreadAsync(string threadId) =>
        _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);

    protected async Task<List<string>> ListThreadIdsAsync() =>
        [.. (await _aiThreads.AiThreadsListAsync(count: DefaultPageCount, cancellationToken: TestContext.Current.CancellationToken))
            .Select(t => t.ThreadId)];

    /// <summary>The history <c>threads/open-or-create</c> hands the chat widget for an existing thread.</summary>
    protected async Task<List<AiThreadMessageLike>> OpenPriorMessagesAsync(string threadId)
    {
        var profile = await GetDefaultProfileAsync();

        // The SDK requires a first message; the engine ignores it for an existing thread and only reads its history.
        var result = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(threadId: threadId, profile: profile, profileId: profile.Id, firstMessage: BuildUserMessage("ignored")),
            TestContext.Current.CancellationToken);

        return result.PriorMessages ?? [];
    }

    protected Task<AiSuccessResponse> UpdateTextAsync(string messageId, string text) =>
        _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: messageId, message: BuildUserMessage(text)),
            TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> DeleteMessageAsync(string messageId) =>
        _aiThreads.AiThreadsDeleteMessageAsync(messageId, TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> ClearMessagesAsync(string threadId) =>
        _aiThreads.AiThreadsClearMessagesAsync(threadId, TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> DeleteThreadAsync(string threadId) =>
        _aiThreads.AiThreadsDeleteAsync(threadId, TestContext.Current.CancellationToken);

    /// <summary>
    /// Posts a verbatim JSON body to <c>append-user-message</c> and returns the status. Raw on purpose: the bodies
    /// here are ones the SDK request model cannot carry (no thread id, a null or role-less message, an invented
    /// role), and a successful answer is the shape the SDK cannot read (see <see cref="MessageTestBase.AppendAsync"/>).
    /// </summary>
    protected async Task<HttpStatusCode> PostAppendRawAsync(string? json)
    {
        var raw = new RawApiClient(_aiChatClient);

        using var response = json is null
            ? await _aiChatClient.PostAsync(AppendUserMessagePath, null, TestContext.Current.CancellationToken)
            : await raw.PostRawAsync(AppendUserMessagePath, json, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    /// <summary>
    /// Puts a verbatim JSON body to <c>update-message</c> and returns the status — for bodies the SDK request model
    /// rejects client-side (no message id, no message) or cannot express (an empty parts list, no body at all).
    /// </summary>
    protected async Task<HttpStatusCode> PutUpdateRawAsync(string? json)
    {
        var raw = new RawApiClient(_aiChatClient);

        using var response = json is null
            ? await _aiChatClient.PutAsync(UpdateMessagePath, null, TestContext.Current.CancellationToken)
            : await raw.PutRawAsync(UpdateMessagePath, json, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    /// <summary>A GET whose query the typed signature cannot produce (a required parameter left out).</summary>
    protected async Task<HttpStatusCode> GetRawStatusAsync(string path)
    {
        using var response = await new RawApiClient(_aiChatClient).GetAsync(path, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    /// <summary>A user message as JSON with the given text blocks.</summary>
    protected static string UserMessageJson(params string[] texts) =>
        JsonSerializer.Serialize(new { role = "user", content = texts.Select(text => new { type = "text", text }) });

    // ---------------------------------------------------------------------------------------------------------------
    // Members, rooms and folders (the Files side of text-to-docx)

    /// <summary>Adds a member of the given type; a Guest goes through the guest-creation route.</summary>
    protected Task<User> AddMemberAsync(EmployeeType type) =>
        type == EmployeeType.Guest
            ? InviteGuest(TestContext.Current.CancellationToken)
            : InviteContact(type, TestContext.Current.CancellationToken);

    /// <summary>The id of <paramref name="user"/>'s own My Documents.</summary>
    protected async Task<int> MyDocumentsOfAsync(User user)
    {
        await FilesHttpClient.Authenticate(user);

        var content = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Current.Id;
    }

    /// <summary>Creates a Custom room as the Owner — the room type that offers Viewer and Editor access as well.</summary>
    protected async Task<int> CreateCustomRoomAsync(string title)
    {
        await FilesHttpClient.Authenticate(Owner);

        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>Grants <paramref name="user"/> the access level in the room, as the Owner, without a notification.</summary>
    protected async Task ShareRoomAsync(int roomId, User user, DocSpace.API.SDK.Model.FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);

        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(invitations: [new RoomInvitation(user.Id, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Creates an empty document in the folder as <paramref name="user"/>; returns its id.</summary>
    protected async Task<int> CreateFileAsAsync(User user, int folderId, string title)
    {
        await FilesHttpClient.Authenticate(user);

        var file = await FilesApi.CreateFileAsync(folderId, new CreateFileRequest(title), TestContext.Current.CancellationToken);

        return file.Response.Id;
    }

    /// <summary>
    /// The files of a folder, read as <paramref name="user"/>. A read the caller is refused throws instead of coming
    /// back empty, so a failed listing can never pass for "nothing was created".
    /// </summary>
    protected async Task<List<FileDto>> ListFilesAsync(User user, int folderId)
    {
        await FilesHttpClient.Authenticate(user);

        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            folderId,
            count: DefaultPageCount,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Files ?? []).OfType<FileDto>()];
    }

    /// <summary>
    /// The files of a folder once two consecutive listings agree. A fresh portal or member keeps filling My
    /// Documents with the sample documents in the background, so a baseline taken straight away grows on its own.
    /// </summary>
    protected async Task<List<int>> StableFileIdsAsync(User user, int folderId)
    {
        var deadline = DateTime.UtcNow + ExportSettle;
        var previous = IdsOf(await ListFilesAsync(user, folderId));

        while (true)
        {
            await Task.Delay(_pollInterval, TestContext.Current.CancellationToken);
            var current = IdsOf(await ListFilesAsync(user, folderId));

            if (current.SequenceEqual(previous) || DateTime.UtcNow >= deadline)
            {
                return current;
            }

            previous = current;
        }
    }

    /// <summary>
    /// Waits for the file an accepted export promised, matched on its exact title. Returns null when it never shows
    /// up, so the assertion message stays the caller's.
    /// </summary>
    protected async Task<FileDto?> WaitForExportedFileAsync(User user, int folderId, string exactTitle)
    {
        var deadline = DateTime.UtcNow + ExportTimeout;

        while (true)
        {
            var match = (await ListFilesAsync(user, folderId)).FirstOrDefault(f => f.Title == exactTitle);

            if (match is not null || DateTime.UtcNow >= deadline)
            {
                return match;
            }

            await Task.Delay(_pollInterval, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Gives a refused export as much time to land as an accepted one would need.</summary>
    protected static Task WaitForExportToSettleAsync() => Task.Delay(ExportSettle, TestContext.Current.CancellationToken);

    protected static List<int> IdsOf(IEnumerable<FileDto> files) => [.. files.Select(f => f.Id).Order()];

    protected static string ExportTitle(string prefix = "Exported") => $"{prefix} {Guid.NewGuid():N}"[..(prefix.Length + 9)];

    protected static AiExportTextToDocxRequest BuildExport(int folderId, string title, string content = "The assistant said hello.") =>
        new(title: title, content: content, folderId: new AiExportTextToDocxRequestFolderId((decimal)folderId));

    /// <summary>Calls <c>text-to-docx</c> as <paramref name="caller"/> (null = anonymous) through the typed SDK.</summary>
    protected async Task<ApiResponse<AiExportTextToDocx202Response>> ExportAsAsync(User? caller, AiExportTextToDocxRequest request)
    {
        await _aiChatClient.Authenticate(caller);

        return await _aiExport.AiExportTextToDocxWithHttpInfoAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Calls <c>text-to-docx</c> as <paramref name="caller"/> and expects a refusal; returns the exception for the
    /// status and the relayed <c>{error}</c> text.
    /// </summary>
    protected async Task<ApiException> ExportRefusedAsAsync(User? caller, AiExportTextToDocxRequest request)
    {
        await _aiChatClient.Authenticate(caller);

        return await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(request, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Posts a verbatim body to <c>text-to-docx</c> as the Owner. Raw on purpose: the SDK request rejects a missing
    /// title, content or folder id client-side, and its <c>format</c> enum cannot carry an unrecognised value.
    /// </summary>
    protected async Task<(HttpStatusCode Status, string Body)> PostExportRawAsync(string json)
    {
        await _aiChatClient.Authenticate(Owner);

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(TextToDocxPath, json, TestContext.Current.CancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
