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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// Shared plumbing of the <c>/api/2.0/ai/attachments/*</c> suites ported from <c>ai/attachments/*.spec.ts</c>: the typed
/// attachments client, drafts backed by real DocSpace files, uploads, acting as a member, and the raw requests for the
/// bodies the SDK model cannot carry.
/// </summary>
/// <remarks>
/// A file draft is a reference to a stored DocSpace file: <c>path</c> is the file id as a string, the server resolves
/// it, checks the caller's access and extracts the text itself (the <c>content</c> a client sends is required and then
/// discarded). So a test that needs a draft holding some text uploads a file holding it (<see cref="SaveDraftAsync"/>).
/// The model is a <see cref="StubModelProvider"/> of the test's own; the TypeScript suite asked a real model and read its
/// reply, here the assertion is on what the provider was sent. Derives from the chat suites' base for its providers,
/// profiles, stored-message reads and the Files helpers (rooms, members, listings).
/// </remarks>
public abstract class AttachmentTestBase(AspireAppFixture fixture) : ChatTests.ChatPart3TestBase(fixture)
{
    protected const string AttachmentsPath = "/api/2.0/ai/attachments";

    /// <summary>Well-formed, never issued.</summary>
    protected const string MissingId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    /// <summary>Well-formed, never issued (a v7-looking uuid, as the chat service mints).</summary>
    protected const string UnknownAttachmentId = "01a1112f-0000-7000-8000-000000000000";

    protected const string DeviceText = "Autotest device file. Line two.\nLine three.";

    /// <summary>A 1x1 PNG as a data URL.</summary>
    protected const string Png1x1DataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    /// <summary>The same picture as bytes, for an upload.</summary>
    protected static byte[] Png1x1 { get; } =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==");

    /// <summary>A body this size is accepted; about 120 KB and up is a 413 (the limit is on the whole request).</summary>
    protected const int LargeContentBytes = 100_000;

    /// <summary>Comfortably over the request-body limit.</summary>
    protected const int OversizedContentBytes = 200_000;

    /// <summary>The ONLYOFFICE file type code of a document, what the composer sends in <c>type</c>.</summary>
    protected const decimal DocumentType = 7m;

    private static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, int> _backingFiles = [];

    /// <summary>The typed attachments client over the chat service; it signs in as whoever <c>_aiChatClient</c> is.</summary>
    protected DocSpace.API.SDK.Api.AI.AttachmentsApi Attachments => _aiAttachments;

    protected sealed record Outcome<T>(int Status, T? Data, string? Error);

    protected sealed record RawOutcome(int Status, string Body)
    {
        public string? Error => ErrorOf(Body);
    }

    protected sealed record UploadedFile(int Id, string Title, int FolderId, long? PureContentLength);

    // ---------------------------------------------------------------------------------------------------------------
    // Acting as

    /// <summary>Signs both the chat client and the Files client in as <paramref name="user"/> (null = anonymous).</summary>
    protected async Task ActAsAsync(User? user)
    {
        await _aiChatClient.Authenticate(user);
        await FilesHttpClient.Authenticate(user);
    }

    /// <summary>A new member of the given type; a Guest goes through the guest-creation route.</summary>
    protected Task<User> AddMemberAsync(EmployeeType type) =>
        type == EmployeeType.Guest
            ? InviteGuest(TestContext.Current.CancellationToken)
            : InviteContact(type, TestContext.Current.CancellationToken);

    // ---------------------------------------------------------------------------------------------------------------
    // Outcomes

    /// <summary>The status, the data and the relayed <c>{error}</c> text of a typed call; a non-2xx never throws here.</summary>
    protected static async Task<Outcome<T>> OutcomeAsync<T>(Func<Task<ApiResponse<T>>> call)
        where T : class
    {
        try
        {
            var response = await call();

            return new Outcome<T>((int)response.StatusCode, response.Data, null);
        }
        catch (ApiException exception)
        {
            return new Outcome<T>(exception.ErrorCode, null, ErrorOf(exception.ErrorContent?.ToString()));
        }
    }

    /// <summary>The <c>error</c> field of a <c>{"error":"..."}</c> body, or null for anything else.</summary>
    protected static string? ErrorOf(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// A raw request against the public AI surface — only for what the typed SDK cannot send: no body at all, a field of
    /// the wrong JSON type, a required field left out. <paramref name="json"/> null sends no body.
    /// </summary>
    protected async Task<RawOutcome> SendRawAsync(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        return new RawOutcome((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    protected Task<RawOutcome> PostRawAsync(string route, string? json) =>
        SendRawAsync(HttpMethod.Post, $"{AttachmentsPath}/{route}", json);

    protected static string Json(object body) => JsonSerializer.Serialize(body);

    // ---------------------------------------------------------------------------------------------------------------
    // Requests

    protected static AiAttachmentsSaveFileRequestInput FileInput(string path, string? title = null, decimal type = DocumentType, string content = "") =>
        new(path: path, content: content, type: type, title: title!);

    protected static AiAttachmentsSaveFileRequest SaveRequest(string path, string? title = null, decimal type = DocumentType, string? entityId = null) =>
        new(input: FileInput(path, title, type), entityId: entityId!);

    protected Task<Outcome<AiAttachment>> SaveFileAsync(AiAttachmentsSaveFileRequest request) =>
        OutcomeAsync(() => Attachments.AiAttachmentsSaveFileWithHttpInfoAsync(request, TestContext.Current.CancellationToken));

    /// <summary>Attaches the stored file by id, the way the composer does: the id as a string, an empty content, a document type.</summary>
    protected Task<Outcome<AiAttachment>> AttachByIdAsync(int fileId, string? title = null, decimal type = DocumentType, string? entityId = null) =>
        SaveFileAsync(SaveRequest(fileId.ToString(), title, type, entityId));

    protected Task<Outcome<List<AiAttachment>>> SaveFilesManyAsync(IEnumerable<int> fileIds, string? entityId = null) =>
        OutcomeAsync(() => Attachments.AiAttachmentsSaveFilesManyWithHttpInfoAsync(
            new AiAttachmentsSaveFilesManyRequest(
                inputs: [.. fileIds.Select(id => FileInput(id.ToString()))],
                entityId: entityId!),
            TestContext.Current.CancellationToken));

    protected Task<Outcome<AiAttachment>> GetAsync(string id) =>
        OutcomeAsync(() => Attachments.AiAttachmentsGetWithHttpInfoAsync(id, TestContext.Current.CancellationToken));

    protected Task<Outcome<List<AiAttachment>>> GetManyAsync(params string[] ids) =>
        OutcomeAsync(() => Attachments.AiAttachmentsGetManyWithHttpInfoAsync([.. ids], TestContext.Current.CancellationToken));

    protected Task<Outcome<AiSuccessResponse>> DeleteAsync(string id) =>
        OutcomeAsync(() => Attachments.AiAttachmentsDeleteWithHttpInfoAsync(id, TestContext.Current.CancellationToken));

    protected Task<Outcome<AiSuccessResponse>> DeleteManyAsync(params string[] ids) =>
        OutcomeAsync(() => Attachments.AiAttachmentsDeleteManyWithHttpInfoAsync([.. ids], TestContext.Current.CancellationToken));

    protected Task<Outcome<AiSuccessResponse>> LinkAsync(IEnumerable<string> ids, string messageId, string threadId) =>
        OutcomeAsync(() => Attachments.AiAttachmentsLinkToMessageWithHttpInfoAsync(
            new AiAttachmentsLinkToMessageRequest(ids: [.. ids], messageId: messageId, threadId: threadId),
            TestContext.Current.CancellationToken));

    // ---------------------------------------------------------------------------------------------------------------
    // Drafts

    /// <summary>
    /// A real DocSpace file in <paramref name="user"/>'s My Documents holding <paramref name="content"/>; returns its id.
    /// Uploaded once per user, name and content. The name matters: its extension decides whether the portal extracts
    /// text at all (<c>.docx</c> and <c>.txt</c> are, <c>.bin</c> and a name with no extension are a 400).
    /// </summary>
    protected async Task<int> BackingFileIdAsync(User user, string name, string content)
    {
        var key = $"{user.Id}|{name}|{content}";

        if (_backingFiles.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var (status, file) = await UploadDeviceFileAsync(user, "@my", name, System.Text.Encoding.UTF8.GetBytes(content), "text/plain");
        status.Should().Be(HttpStatusCode.OK, $"uploading the backing file {name}");
        file.Should().NotBeNull($"the upload of {name} answered with an entry");

        _backingFiles[key] = file!.Id;

        return file.Id;
    }

    /// <summary>
    /// A stored draft carrying <paramref name="content"/>, addressed by its id: a backing file named <paramref name="title"/>
    /// is uploaded and attached, both as <paramref name="user"/>. Fails unless a draft really came back.
    /// </summary>
    protected async Task<string> SaveDraftAsync(User user, string title, string content = "x", string? entityId = null)
    {
        var fileId = await BackingFileIdAsync(user, title, content);

        await ActAsAsync(user);
        var saved = await AttachByIdAsync(fileId, entityId: entityId);
        saved.Status.Should().Be(200, $"save-file of {title}: {saved.Error}");
        saved.Data!.Id.Should().NotBeNullOrEmpty();

        return saved.Data.Id;
    }

    /// <summary>
    /// A stored image draft: a real picture is uploaded to <paramref name="user"/>'s My Documents and attached by id, which
    /// is how a picture is attached now (a dedicated base64 route no longer exists). Fails unless the draft is an image.
    /// </summary>
    protected async Task<string> SaveImageDraftAsync(User user, string name, byte[]? bytes = null, string mimeType = "image/png", string? entityId = null)
    {
        var file = await UploadStoredAsync(user, "@my", name, bytes ?? Png1x1, mimeType);

        await ActAsAsync(user);
        var saved = await AttachByIdAsync(file.Id, name, entityId: entityId);
        saved.Status.Should().Be(200, $"attaching the picture {name}: {saved.Error}");
        saved.Data!.Kind.Should().Be(AiAttachment.KindEnum.Image);

        return saved.Data.Id;
    }

    /// <summary>
    /// The drafts a composer holds while a message is written: <paramref name="files"/> file drafts followed by
    /// <paramref name="images"/> image drafts, as the records that ride on a message's <c>attachments</c>. <paramref name="tag"/>
    /// must differ between calls within one test: two inputs that resolve to the same file collapse to one draft.
    /// </summary>
    protected async Task<List<(string Id, string Kind, string Title)>> StageComposerDraftsAsync(int files, int images, string tag)
    {
        var drafts = new List<(string Id, string Kind, string Title)>();

        for (var index = 0; index < files; index++)
        {
            var title = $"autotest-{tag}-file-{index}.txt";
            drafts.Add((await SaveDraftAsync(Owner, title, $"{tag} file {index}"), "file", title));
        }

        for (var index = 0; index < images; index++)
        {
            var title = $"autotest-{tag}-image-{index}.png";
            drafts.Add((await SaveImageDraftAsync(Owner, title), "image", title));
        }

        return drafts;
    }

    /// <summary>The records of <see cref="StageComposerDraftsAsync"/> as the message attachments a client sends.</summary>
    protected static List<object> AsMessageAttachments(IEnumerable<(string Id, string Kind, string Title)> drafts) =>
        [.. drafts.Select(draft => (object)new Dictionary<string, object> { ["id"] = draft.Id, ["kind"] = draft.Kind, ["title"] = draft.Title })];

    /// <summary>The draft with that id as <paramref name="id"/>'s owner sees it, or null; polled until it is readable.</summary>
    protected async Task<AiAttachment?> FindAsync(string id)
    {
        var deadline = DateTime.UtcNow + _readTimeout;

        while (true)
        {
            var read = await GetManyAsync(id);

            if (read.Status == 200 && read.Data is { Count: > 0 } && read.Data[0] is { } found)
            {
                return found;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return null;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    protected async Task<AiAttachment> ExpectStoredAsync(string id, string what = "attachment")
    {
        var found = await FindAsync(id);
        found.Should().NotBeNull($"{what} {id} should be readable");

        return found!;
    }

    /// <summary>Waits for the draft to stop resolving; asserts it did within the deadline.</summary>
    protected async Task ExpectAbsentAsync(string id, string what = "attachment")
    {
        var deadline = DateTime.UtcNow + _readTimeout;
        AiAttachment? last;

        while (true)
        {
            var read = await GetManyAsync(id);

            // A failed read is not a missing draft: only an answered read that holds null counts as "gone".
            read.Status.Should().Be(200, $"reading {what} {id}");
            last = read.Data is { Count: > 0 } ? read.Data[0] : null;

            if (last is null || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        last.Should().BeNull($"{what} {id} should be gone");
    }

    /// <summary>One read, no polling: the draft as the current caller sees it right now (null for an unknown or foreign id).</summary>
    protected async Task<AiAttachment?> ReadOnceAsync(string id)
    {
        var read = await GetManyAsync(id);
        read.Status.Should().Be(200);

        return read.Data is { Count: > 0 } ? read.Data[0] : null;
    }

    /// <summary>The shape every successful save answers with.</summary>
    protected static void ExpectDraftShape(AiAttachment? attachment, AiAttachment.KindEnum kind)
    {
        attachment.Should().NotBeNull("save-file answered with a body");
        Guid.TryParse(attachment!.Id, out _).Should().BeTrue($"the attachment id {attachment.Id} is a uuid");
        attachment.Kind.Should().Be(kind);
        attachment.CreatedAt.Should().BeGreaterThan(0);
        // A draft is a draft: nothing ties it to a message or a thread yet.
        attachment.MessageId.Should().BeNull();
        attachment.ThreadId.Should().BeNull();
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Threads and messages

    /// <summary>A thread on the test's default profile holding one stored user message; whoever the chat client is signed in as owns it.</summary>
    protected async Task<(string ThreadId, string MessageId)> ThreadWithMessageAsync(string? title = null, string? entityId = null, string? text = null)
    {
        var thread = await CreateThreadAsync(title ?? "Autotest Attachments Thread", entityId: entityId);
        var messageId = await AppendUserMessageAsync(thread.ThreadId, text ?? "Autotest attachment carrier");

        return (thread.ThreadId, messageId);
    }

    /// <summary>
    /// Stores a user message that carries <paramref name="attachments"/> without running the model; returns the status.
    /// </summary>
    protected async Task<HttpStatusCode> AppendWithAttachmentsAsync(string threadId, string profileId, string text, List<object> attachments)
    {
        var message = new AiThreadMessageLike(
            role: AiThreadMessageLike.RoleEnum.User,
            content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: text)]),
            attachments: attachments);
        var request = new AiThreadsAppendUserMessageRequest(threadId: threadId, message: message, profileId: profileId);

        try
        {
            var response = await _aiThreads.AiThreadsAppendUserMessageWithHttpInfoAsync(request, TestContext.Current.CancellationToken);

            return response.StatusCode;
        }
        catch (ApiException exception)
        {
            return (HttpStatusCode)exception.ErrorCode;
        }
    }

    /// <summary>The attachments the first stored user message of the thread carries, as JSON.</summary>
    protected async Task<JsonElement?> StoredUserAttachmentsAsync(string threadId)
    {
        var user = UserMessages(await ReadStoredMessagesAsync(threadId)).FirstOrDefault();

        return user.ValueKind == JsonValueKind.Object && user.TryGetProperty("attachments", out var attachments) ? attachments : null;
    }

    /// <summary>
    /// Runs one round through <c>ai/send-with-stream</c> whose user message carries <paramref name="attachments"/>, as whoever
    /// the chat client is signed in as. Raw for the same reason as <see cref="BaseTest.SendWithStreamAsync"/>.
    /// </summary>
    protected async Task<ChatRound> SendWithAttachmentsAsync(string threadId, string profileId, string text, List<object> attachments, string? entityId = null)
    {
        var message = new AiThreadMessageLike(
            role: AiThreadMessageLike.RoleEnum.User,
            content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: text)]),
            attachments: attachments);
        var body = new AiSendStreamBody(threadId: threadId, userMessage: message, entityId: entityId!, profileId: profileId);

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/send-with-stream",
            body.ToJson(),
            TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>The stored JSON equals what was sent, property order aside.</summary>
    protected static void ExpectSameJson(JsonElement? stored, object expected, string because)
    {
        stored.Should().NotBeNull(because);

        var storedNode = System.Text.Json.Nodes.JsonNode.Parse(stored!.Value.GetRawText());
        var expectedNode = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(expected));

        System.Text.Json.Nodes.JsonNode.DeepEquals(storedNode, expectedNode)
            .Should().BeTrue($"{because}\nstored:   {stored.Value.GetRawText()}\nexpected: {JsonSerializer.Serialize(expected)}");
    }

    /// <summary>A stand-in model, a profile declaring <paramref name="capabilities"/> on it, and a thread bound to that profile.</summary>
    protected async Task<(StubModelProvider Provider, AiProfile Profile, string ThreadId)> ArrangeChatAsync(decimal capabilities = ChatCapabilities)
    {
        var provider = NewProvider();
        var profile = await CreateCapableProfileAsync(provider, capabilities);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        return (provider, profile, thread.ThreadId);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Files: uploads, folders, AI rooms

    /// <summary>
    /// Uploads one file as <paramref name="user"/> through <c>POST /files/{folder}/upload</c> (multipart). Raw on purpose —
    /// an SDK defect: the generated upload sends JSON, which serializes the file as <c>{}</c> and answers "No input files".
    /// </summary>
    protected async Task<(HttpStatusCode Status, UploadedFile? File)> UploadDeviceFileAsync(User? user, string folder, string name, byte[] bytes, string mimeType)
    {
        await FilesHttpClient.Authenticate(user);

        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(file, "file", name);

        using var response = await FilesHttpClient.PostAsync($"/api/2.0/files/{folder}/upload", form, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        using var document = JsonDocument.Parse(body);

        // The route answers an array of the created entries even for one file.
        var entry = document.RootElement.TryGetProperty("response", out var payload)
            ? payload.ValueKind == JsonValueKind.Array ? payload.EnumerateArray().FirstOrDefault() : payload
            : default;

        if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("id", out var id))
        {
            return (response.StatusCode, null);
        }

        return (response.StatusCode, new UploadedFile(
            id.GetInt32(),
            entry.TryGetProperty("title", out var title) ? title.GetString() ?? name : name,
            entry.TryGetProperty("folderId", out var folderId) && folderId.ValueKind == JsonValueKind.Number ? folderId.GetInt32() : 0,
            entry.TryGetProperty("pureContentLength", out var length) && length.ValueKind == JsonValueKind.Number ? length.GetInt64() : null));
    }

    /// <summary>Uploads and asserts the file was really created.</summary>
    protected async Task<UploadedFile> UploadStoredAsync(User user, string folder, string name, byte[] bytes, string mimeType)
    {
        var (status, file) = await UploadDeviceFileAsync(user, folder, name, bytes, mimeType);

        status.Should().Be(HttpStatusCode.OK, $"uploading {name} into {folder}");
        file.Should().NotBeNull($"an entry came back for {name}");

        return file!;
    }

    protected Task<UploadedFile> UploadTextAsync(User user, string folder, string name, string text) =>
        UploadStoredAsync(user, folder, name, System.Text.Encoding.UTF8.GetBytes(text), "text/plain");

    /// <summary>Creates an AI room as the owner (the agent of the TypeScript suite) and returns its id.</summary>
    protected async Task<int> CreateAiRoomAsync(string? title = null) =>
        (await CreateRoomOfTypeAsync(RoomType.AiRoom, title ?? $"Autotest AI Room {Guid.NewGuid():N}"[..24])).Id;

    /// <summary>Creates an ordinary (custom) room as the owner and returns its id.</summary>
    protected async Task<int> CreateCustomRoomAsync(string title) =>
        (await CreateRoomOfTypeAsync(RoomType.CustomRoom, title)).Id;

    /// <summary>The id of one of an AI room's built-in subfolders (Knowledge, Chat outputs).</summary>
    protected async Task<int> RoomSubfolderIdAsync(User user, int roomId, FolderType type)
    {
        await FilesHttpClient.Authenticate(user);

        var folders = (await FoldersApi.GetFoldersAsync(roomId, TestContext.Current.CancellationToken)).Response ?? [];
        var found = folders.OfType<FolderDto>().FirstOrDefault(folder => folder.Type == type);

        found.Should().NotBeNull($"the AI room {roomId} has a {type} folder; it holds {string.Join(", ", folders.Select(f => f.Title))}");

        return found!.Id;
    }

    /// <summary>
    /// The <c>security.Create</c> flag a folder advertises. Raw: the generated folder model carries no <c>security</c>
    /// block, which is exactly what is asserted here.
    /// </summary>
    protected async Task<bool?> AdvertisedCreateAsync(User user, int folderId)
    {
        await FilesHttpClient.Authenticate(user);

        using var response = await FilesHttpClient.GetAsync($"/api/2.0/files/{folderId}?count=1", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var current = document.RootElement.GetProperty("response").GetProperty("current");

        return current.TryGetProperty("security", out var security) && security.TryGetProperty("Create", out var create) ? create.GetBoolean() : null;
    }

    /// <summary>The bytes of a stored file, downloaded through the file handler as <paramref name="user"/>.</summary>
    protected async Task<byte[]> DownloadAsync(User user, int fileId)
    {
        await FilesHttpClient.Authenticate(user);

        using var response = await FilesHttpClient.GetAsync($"filehandler.ashx?action=download&fileid={fileId}", TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue($"the file {fileId} should be downloadable, got {(int)response.StatusCode}");

        return await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Waits for a file with that exact title to be listed in the folder; null when it never was.</summary>
    protected async Task<FileDto?> WaitForFileAsync(User user, int folderId, string exactTitle)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var match = (await ListFilesAsync(user, folderId)).Find(file => file.Title == exactTitle);

            if (match is not null || DateTime.UtcNow >= deadline)
            {
                return match;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Test data

    protected static string Suffix() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>A valid deflate-compressed zip archive holding the given members.</summary>
    protected static byte[] CreateZip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }

    /// <summary>A valid gzip stream, for the <c>.gz</c> / <c>.tgz</c> names the composer also refuses.</summary>
    protected static byte[] CreateGzip(string content)
    {
        using var stream = new MemoryStream();

        using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(System.Text.Encoding.UTF8.GetBytes(content));
        }

        return stream.ToArray();
    }

    /// <summary>A valid solid-colour RGB PNG of any size.</summary>
    protected static byte[] CreatePng(int width, int height, byte red = 0, byte green = 0, byte blue = 0)
    {
        using var raw = new MemoryStream();

        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);

            for (var x = 0; x < width; x++)
            {
                raw.WriteByte(red);
                raw.WriteByte(green);
                raw.WriteByte(blue);
            }
        }

        using var compressed = new MemoryStream();

        using (var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw.ToArray());
        }

        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8; // bit depth
        header[9] = 2; // colour type: RGB

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
        WritePngChunk(png, "IHDR", header);
        WritePngChunk(png, "IDAT", compressed.ToArray());
        WritePngChunk(png, "IEND", []);

        return png.ToArray();
    }

    /// <summary>A 1x1 transparent GIF.</summary>
    protected static byte[] CreateGif() => Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    /// <summary>A 1x1 JPEG.</summary>
    protected static byte[] CreateJpeg() =>
        Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRof" +
            "Hh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/wAALCAABAAEBAREA/8QAFAAB" +
            "AAAAAAAAAAAAAAAAAAAAAv/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAA" +
            "AAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AVf/Z");

    /// <summary>A minimal RIFF/WEBP container (lossy VP8 header).</summary>
    protected static byte[] CreateWebp() => Convert.FromBase64String("UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==");

    private static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        var body = new byte[4 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(body, 0);
        data.CopyTo(body, 4);
        stream.Write(body);

        var crc = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(body));
        stream.Write(crc);
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xffffffffu;

        foreach (var b in bytes)
        {
            crc ^= b;

            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc ^ 0xffffffffu;
    }
}
