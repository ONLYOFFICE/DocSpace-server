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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// <c>POST /api/2.0/ai/text-to-docx</c> end to end: the Node route validates the body and answers 202, the AI worker
/// renders the markdown through the document server and saves the result into the target folder a few seconds later.
/// Every claim about the document is therefore polled, and a claim that nothing was created waits just as long
/// before it counts.
/// </summary>
public abstract class TextToDocxPortTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected override bool NeedsDocumentServer => true;

    protected const string TextToDocxPath = "/api/2.0/ai/text-to-docx";

    protected const string RequiredError = "title and content are required";
    protected const string FolderIdRequiredError = "folderId is required";
    protected const string FormatError = "format must be one of Docx, Pdf, Md";

    /// <summary>The Node body parser of this route caps the JSON body at 15 MiB, counted in bytes.</summary>
    protected const int BodyLimitBytes = 15 * 1024 * 1024;

    /// <summary>How long an accepted export may take to appear in its folder.</summary>
    protected static TimeSpan ExportTimeout { get; } = TimeSpan.FromSeconds(60);

    /// <summary>How long a refused export is given to land anyway before "nothing was created" counts.</summary>
    protected static TimeSpan SettleTime { get; } = TimeSpan.FromSeconds(20);

    protected static string RandomSuffix() => Guid.NewGuid().ToString("N")[..8];

    protected static AiExportTextToDocxRequest BuildExport(string title, string content, int folderId) =>
        new(title: title, content: content, folderId: new AiExportTextToDocxRequestFolderId((decimal)folderId));

    /// <summary>Exports through the typed client and asserts the job was accepted.</summary>
    protected async Task ExportAcceptedAsync(string title, string content, int folderId)
    {
        var response = await _aiExport.AiExportTextToDocxWithHttpInfoAsync(
            BuildExport(title, content, folderId),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted, $"title {title}");
        response.Data.Success.Should().BeTrue();
    }

    /// <summary>
    /// Exports through the typed client and reports whatever came back — for the bug tests, where the export is
    /// expected to fail but currently succeeds, and the folder has to be checked before the status is asserted.
    /// </summary>
    protected async Task<ExportResult> TryExportAsync(AiExportTextToDocxRequest request)
    {
        try
        {
            var response = await _aiExport.AiExportTextToDocxWithHttpInfoAsync(request, TestContext.Current.CancellationToken);

            return new ExportResult(response.StatusCode, null);
        }
        catch (ApiException exception)
        {
            return new ExportResult((HttpStatusCode)exception.ErrorCode, ErrorOf(exception.ErrorContent?.ToString()));
        }
    }

    /// <summary>Posts a body the SDK model cannot carry and reads the status and the <c>error</c> field.</summary>
    protected async Task<ExportResult> PostExportRawAsync(string json)
    {
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(TextToDocxPath, json, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return new ExportResult(response.StatusCode, ErrorOf(body), body);
    }

    /// <summary>A JSON object with <c>folderId</c> set (or replaced) on it.</summary>
    protected static string WithFolderId(string json, int folderId)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        node["folderId"] = folderId;

        return node.ToJsonString();
    }

    /// <summary>
    /// The SDK request with a <c>format</c> (and optionally an <c>extension</c>) added as raw JSON values.
    /// </summary>
    /// <remarks>
    /// Only for bodies the SDK model cannot carry: a <c>format</c> outside the <c>Docx</c> | <c>Pdf</c> | <c>Md</c> enum
    /// (another casing, a wrong JSON type, null), or an <c>extension</c>, which is not a property of
    /// <c>AiExportTextToDocxRequest</c>. A valid format goes through <see cref="AiExportTextToDocxRequest.Format"/>.
    /// </remarks>
    protected static string WithFormat(AiExportTextToDocxRequest request, string formatJson, string? extension = null)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(request.ToJson())!.AsObject();
        node["format"] = System.Text.Json.Nodes.JsonNode.Parse(formatJson);

        if (extension is not null)
        {
            node["extension"] = extension;
        }

        return node.ToJsonString();
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

    /// <summary>Creates a room of the given type as the owner; returns its id.</summary>
    protected async Task<int> CreateRoomOfTypeAsync(RoomType roomType, string? title = null)
    {
        await FilesHttpClient.Authenticate(Owner);

        var request = new CreateRoomRequestDto(title ?? $"Autotest Export {roomType} {RandomSuffix()}", roomType: roomType);

        return (await RoomsApi.CreateRoomAsync(request, TestContext.Current.CancellationToken)).Response.Id;
    }

    /// <summary>Creates a folder as whoever the Files client is signed in as; returns its id.</summary>
    protected async Task<int> CreateFolderAsync(int parentId, string title) =>
        (await FoldersApi.CreateFolderAsync(parentId, new CreateFolderRequest(title), TestContext.Current.CancellationToken)).Response.Id;

    /// <summary>
    /// The files of a folder. The SDK throws on a failed listing, so a folder nobody could read never looks like an
    /// empty one — which is exactly the shape a passing "no document was created" assertion expects.
    /// </summary>
    protected async Task<List<FileDto>> ListFolderFilesAsync(int folderId)
    {
        var content = (await FoldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files.OfType<FileDto>().ToList();
    }

    /// <summary>
    /// Waits for the file an export promised, matched on its exact title. Returns null when it never shows up, so the
    /// caller decides whether that is the expected outcome.
    /// </summary>
    protected async Task<FileDto?> WaitForExportedFileAsync(int folderId, string exactTitle, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? ExportTimeout);

        while (true)
        {
            var match = (await ListFolderFilesAsync(folderId)).Find(file => file.Title == exactTitle);

            if (match is not null || DateTime.UtcNow >= deadline)
            {
                return match;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Waits until <paramref name="count"/> files that were not in <paramref name="before"/> are in the folder, whatever
    /// their titles, and returns the new files of the last listing. For a test whose bug is the name an export lands
    /// under: waiting for the expected title would wait out the whole timeout while the file sits there misnamed.
    /// </summary>
    protected async Task<List<FileDto>> WaitForNewFilesAsync(int folderId, IReadOnlyCollection<int> before, int count = 1)
    {
        var deadline = DateTime.UtcNow + ExportTimeout;

        while (true)
        {
            var added = (await ListFolderFilesAsync(folderId)).FindAll(file => !before.Contains(file.Id));

            if (added.Count >= count || DateTime.UtcNow >= deadline)
            {
                return added;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The ids of the files in a folder: the baseline <see cref="WaitForNewFilesAsync"/> counts against.</summary>
    protected async Task<List<int>> FileIdsAsync(int folderId) => (await ListFolderFilesAsync(folderId)).ConvertAll(file => file.Id);

    /// <summary>Reads a folder until two consecutive listings hold the same files: the baseline for counting.</summary>
    protected async Task<List<FileDto>> WaitForStableFolderFilesAsync(int folderId, TimeSpan? timeout = null)
    {
        static string IdsOf(List<FileDto> files) => string.Join(",", files.Select(file => file.Id).Order());

        var deadline = DateTime.UtcNow + (timeout ?? SettleTime);
        var previous = await ListFolderFilesAsync(folderId);

        while (true)
        {
            await Task.Delay(2000, TestContext.Current.CancellationToken);

            var current = await ListFolderFilesAsync(folderId);

            if (IdsOf(current) == IdsOf(previous) || DateTime.UtcNow >= deadline)
            {
                return current;
            }

            previous = current;
        }
    }

    /// <summary>Gives a refused export as much time to land as an accepted one.</summary>
    protected static Task WaitForExportToSettleAsync() => Task.Delay(SettleTime, TestContext.Current.CancellationToken);

    /// <summary>
    /// The title the Files API gives a file created with <paramref name="title"/> — the oracle for what a normalised
    /// file name looks like, so a defect of the export is not frozen as the contract.
    /// </summary>
    protected async Task<string> FilesApiTitleForAsync(int folderId, string title) =>
        (await FilesApi.CreateFileAsync(folderId, new CreateFileRequest(title), TestContext.Current.CancellationToken)).Response.Title;

    /// <summary>Waits until no file operation of the caller is running any more.</summary>
    protected async Task WaitForFileOperationsAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (true)
        {
            var statuses = (await OperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (statuses.Count == 0 || statuses.TrueForAll(status => status.Finished) || DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// The visible text of an exported .docx, paragraphs separated by newlines, downloaded as whoever the Files client
    /// is signed in as. The SDK exposes no operation that returns a file's content, so it goes through the file
    /// handler, as the Files suite does.
    /// </summary>
    protected async Task<string> ReadDocxTextAsync(int fileId)
    {
        using var response = await FilesHttpClient.GetAsync($"filehandler.ashx?action=download&fileid={fileId}", TestContext.Current.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue($"the file {fileId} should be downloadable, got {(int)response.StatusCode}");

        return DocxText(bytes);
    }

    /// <summary>The text of <c>word/document.xml</c>: tags dropped, paragraph ends turned into newlines.</summary>
    protected static string DocxText(byte[] bytes)
    {
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(bytes), System.IO.Compression.ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml");

        entry.Should().NotBeNull($"a .docx has a word/document.xml; the archive holds {string.Join(", ", archive.Entries.Select(e => e.FullName))}");

        using var reader = new StreamReader(entry!.Open());
        var xml = reader.ReadToEnd().Replace("</w:p>", "\n", StringComparison.Ordinal);

        var text = new System.Text.StringBuilder(xml.Length);
        var inTag = false;

        foreach (var c in xml)
        {
            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
            }
            else if (!inTag)
            {
                text.Append(c);
            }
        }

        return WebUtility.HtmlDecode(text.ToString()).Trim();
    }

    /// <summary>The client-side half of "export this chat": the turns rendered as markdown.</summary>
    protected static string RenderTranscript(IEnumerable<AiThreadMessageLike> messages) =>
        string.Join(
            "\n\n",
            messages.Select(message =>
                $"**{(message.Role == AiThreadMessageLike.RoleEnum.User ? "You" : "Assistant")}:**\n\n{TextOf(message)}"));

    protected Task<List<AiThreadMessageLike>> ReadMessagesAsync(string threadId) =>
        _aiThreads.AiThreadsReadMessagesAsync(threadId, count: 100, cancellationToken: TestContext.Current.CancellationToken);

    /// <summary>Reads a thread until it holds <paramref name="count"/> messages; returns the last read.</summary>
    protected async Task<List<AiThreadMessageLike>> WaitForMessagesAsync(string threadId, int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var messages = await ReadMessagesAsync(threadId);

            if (messages.Count >= count || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>What came back from an export call: the status and, for a refusal, the <c>error</c> text.</summary>
    protected sealed record ExportResult(HttpStatusCode StatusCode, string? Error, string? Body = null);
}
