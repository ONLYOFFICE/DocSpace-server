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
/// Attaching a stored DocSpace file by id — the mechanism both "Upload from device" and "Add files from DocSpace" end in.
/// <c>save-file</c>'s <c>path</c> is the file id as a string; the server resolves it, checks the caller's access, extracts
/// the text itself (or hands an image back as a data URL) and answers a draft whose <c>path</c> is <c>"&lt;id&gt;/&lt;title&gt;"</c>.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentStoredFileTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    protected override bool NeedsDocumentServer => true;

    /// <summary>Comfortably past the ~128 KB cap on a request body.</summary>
    private static readonly string _largeFileText = string.Concat(Enumerable.Repeat("lorem ipsum ", 20000));

    public static TheoryData<string, string, bool> FormatCases { get; } = new()
    {
        { "txt", "autotest.txt", true },
        { "csv", "autotest.csv", true },
        { "md", "autotest.md", true },
        { "png", "autotest.png", true },
        { "zip", "autotest.zip", false },
        { "tgz", "autotest.tgz", false }
    };

    public static TheoryData<string, string> PictureCases { get; } = new()
    {
        { "png", "image/png" },
        { "jpeg", "image/jpeg" },
        { "gif", "image/gif" },
        { "webp", "image/webp" }
    };

    private static (byte[] Bytes, string Mime) FormatBytes(string label) => label switch
    {
        "txt" => (System.Text.Encoding.UTF8.GetBytes(DeviceText), "text/plain"),
        "csv" => (System.Text.Encoding.UTF8.GetBytes("a,b\n1,2\n"), "text/csv"),
        "md" => (System.Text.Encoding.UTF8.GetBytes("# Heading\n\nbody"), "text/markdown"),
        "png" => (CreatePng(2, 2), "image/png"),
        "zip" => (CreateZip(("inner.txt", "inside the archive")), "application/zip"),
        "tgz" => (CreateGzip(DeviceText), "application/gzip"),
        _ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
    };

    [Fact]
    public async Task SaveFile_PathIsTheDocSpaceFileId_AndTheServerExtractsTheTextItself()
    {
        // The client sends an id and no payload; everything else on the draft is produced server-side.
        var stored = await UploadTextAsync(Owner, "@my", $"autotest-source-{Suffix()}.txt", DeviceText);

        var attached = await AttachByIdAsync(stored.Id, stored.Title);

        attached.Status.Should().Be(200, attached.Error);
        attached.Data!.Content.Should().Be(DeviceText, "the server read the file and extracted its text");
        attached.Data.Path.Should().Be($"{stored.Id}/{stored.Title}", "the draft points back at the file it was made from");
        attached.Data.Kind.Should().Be(AiAttachment.KindEnum.File);

        // The reference wins over anything the client supplies with it: `content` is a required field whose value is
        // discarded.
        var both = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput(stored.Id.ToString(), "CLIENT SUPPLIED TEXT", DocumentType, stored.Title)));
        both.Status.Should().Be(200, both.Error);
        both.Data!.Content.Should().Be(DeviceText, "content sent next to a path is ignored in favour of the real file");

        // `title` is the only optional field of the three: the server takes it from the file when it is missing.
        var untitled = await AttachByIdAsync(stored.Id);
        untitled.Status.Should().Be(200, untitled.Error);
        untitled.Data!.Title.Should().Be(stored.Title, "the title comes from the file");
    }

    [Fact]
    public async Task SaveFile_AttachingById_IsCheckedAgainstTheCallersAccessToTheFile()
    {
        // The route reads a file on the caller's behalf, so the question is whose files it will read. Both directions: a
        // stranger is refused, and a room member who may only view the file can still attach it.
        var secret = $"OWNER-SECRET-{Suffix().ToUpperInvariant()}";
        var privateFile = await UploadTextAsync(Owner, "@my", "autotest-owner-private.txt", $"Confidential. {secret}");
        var roomId = await CreateCustomRoomAsync("Autotest Attach By Id Room");
        var roomFile = await UploadTextAsync(Owner, roomId.ToString(), "autotest-room-file.txt", DeviceText);

        var member = await AddMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.Read);
        await ActAsAsync(member);

        // Control: the member really has no access to the owner's private file.
        var direct = await Assert.ThrowsAsync<ApiException>(
            async () => await FilesApi.GetFileInfoAsync(privateFile.Id, cancellationToken: TestContext.Current.CancellationToken));
        direct.ErrorCode.Should().Be(403, "the member cannot read the owner's file directly");

        var refused = await AttachByIdAsync(privateFile.Id, "stolen.txt");
        refused.Status.Should().Be(403, "attaching a file the caller cannot read");
        JsonSerializer.Serialize(refused).Should().NotContain(secret, "and no part of the file came back");

        // Positive control: Read is enough to attach a file from a room, so the 403 above is about access to that file.
        var allowed = await AttachByIdAsync(roomFile.Id, roomFile.Title);
        allowed.Status.Should().Be(200, $"a Read-level member attaching a room file: {allowed.Error}");
        allowed.Data!.Content.Should().Be(DeviceText);
    }

    [Theory]
    [MemberData(nameof(FormatCases))]
    public async Task SaveFile_ArchivesAreRefusedAtAttachTimeWhileDocumentsAndImagesAreAccepted(string label, string name, bool attachable)
    {
        // The composer's "unsupported type" toast has a server-side counterpart, and it sits at attach time rather than
        // at upload time — `POST /files/{id}/upload` storing a .zip is correct for a file storage. Real archives, not
        // renamed text, so a refusal cannot be attributed to the bytes and an acceptance cannot be explained away.
        var roomId = await CreateCustomRoomAsync("Autotest Attach Formats Room");
        var (bytes, mime) = FormatBytes(label);

        // Control for every case: the file storage takes all of them, so the difference below is the attachment rule.
        var stored = await UploadStoredAsync(Owner, roomId.ToString(), name, bytes, mime);

        var attached = await AttachByIdAsync(stored.Id, name);

        attached.Status.Should().Be(attachable ? 200 : 400, $"attaching a .{label}: {attached.Error}");
    }

    [Fact]
    public async Task SaveFile_RealDocxIsExtractedAndAnEmptyOneIsRefused()
    {
        // The format a user is most likely to attach, and the one case where a 400 is about the file being empty rather
        // than about its type.
        var folderId = await MyDocumentsOfAsync(Owner);
        var marker = $"DOCX-{Suffix().ToUpperInvariant()}";
        var title = $"Autotest Docx {Suffix()}";

        await ExportDocxAsync(title, $"Document body. {marker}", folderId);
        var docx = await WaitForFileAsync(Owner, folderId, $"{title}.docx");
        docx.Should().NotBeNull("the .docx the portal was asked to build");

        var attached = await AttachByIdAsync(docx!.Id, $"{title}.docx");
        attached.Status.Should().Be(200, attached.Error);
        attached.Data!.Content.Should().Contain(marker, "the document's own text, not its raw bytes");

        await FilesHttpClient.Authenticate(Owner);
        var empty = await FilesApi.CreateFileAsync(folderId, new CreateFileRequest("autotest-empty.docx"), TestContext.Current.CancellationToken);
        var refused = await AttachByIdAsync(empty.Response.Id, "autotest-empty.docx");

        refused.Status.Should().Be(400, "an empty document has nothing to extract");
    }

    [Fact]
    public async Task SaveFile_FileFarLargerThanTheRequestBodyLimit_AttachesWhole()
    {
        // Attaching by id has no size ceiling, because the payload never travels in the request.
        var bytes = System.Text.Encoding.UTF8.GetBytes(_largeFileText);
        bytes.Length.Should().BeGreaterThan(200_000);
        var stored = await UploadStoredAsync(Owner, "@my", $"autotest-large-{Suffix()}.txt", bytes, "text/plain");
        stored.PureContentLength.Should().Be(bytes.Length);

        var attached = await AttachByIdAsync(stored.Id, stored.Title);

        attached.Status.Should().Be(200, "attaching the large file by id");
        attached.Data!.Content!.Length.Should().BeGreaterThan(200_000, "the whole document came through");
    }

    [Theory]
    [InlineData("pdf", Skip = "SaveFileAsPdf never produces the .pdf on the integration-test stand, even with the document server up before the test starts (checked 2026-11-03); the cause is not investigated. The .xlsx row runs.")]
    [InlineData("xlsx")]
    public async Task SaveFile_RealPdfAndXlsx_AreExtractedTheWayADocxIs(string format)
    {
        // The two formats the requirement names beside .docx, and the only two a user cannot get into the composer any
        // other way: they are not accepted by drag and drop, so "Add files from DocSpace" — this route — is the whole
        // feature for them. Both files come from the portal's own converters, so a 400 would be about the attachment rule
        // and not about bytes assembled by a test.
        var folderId = await MyDocumentsOfAsync(Owner);
        var marker = $"{format.ToUpperInvariant()}-{Suffix().ToUpperInvariant()}";
        var title = $"Autotest {format} {Suffix()}";
        var file = format == "pdf"
            ? await PdfWithMarkerAsync(folderId, title, marker)
            : await XlsxWithMarkerAsync(folderId, title, marker);

        var attached = await AttachByIdAsync(file.Id, file.Title);

        attached.Status.Should().Be(200, $"attaching a .{format}: {attached.Error}");
        attached.Data!.Content.Should().Contain(marker, $"the text extracted from the .{format}");
        attached.Data.Title.Should().Be(file.Title);
        attached.Data.Path.Should().Be($"{file.Id}/{file.Title}");
        (await ExpectStoredAsync(attached.Data.Id, format)).Content.Should().Contain(marker);
    }

    [Theory]
    [MemberData(nameof(PictureCases))]
    public async Task SaveFile_PictureAttachedById_ComesBackAsAnImageDraftCarryingTheFilesBytes(string label, string mime)
    {
        // The route recognises the image, switches `kind` to "image" on its own and hands back the file's bytes as a data
        // URL, byte for byte. `kind` follows the file behind `path`, never the `type` a client sends. A multi-pixel
        // payload would show a server that truncates or re-encodes.
        var bytes = label switch
        {
            "png" => CreatePng(16, 16, 10, 20, 30),
            "jpeg" => CreateJpeg(),
            "gif" => CreateGif(),
            _ => CreateWebp()
        };
        var name = $"autotest-picture-{Suffix()}.{label}";
        var stored = await UploadStoredAsync(Owner, "@my", name, bytes, mime);
        var expectedBase64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

        var attached = await AttachByIdAsync(stored.Id, name);

        attached.Status.Should().Be(200, $"attaching a .{label}: {attached.Error}");
        attached.Data!.Title.Should().Be(name);
        attached.Data.Path.Should().Be($"{stored.Id}/{name}");
        attached.Data.Kind.Should().Be(AiAttachment.KindEnum.Image, "the endpoint does not decide the kind here");
        // Not re-encoded and not re-compressed: the same bytes uploaded.
        attached.Data.Base64.Should().Be(expectedBase64, $"payload of the {label} on the attach");
        attached.Data.Content.Should().BeNull("a picture carries no extracted text");

        var draft = await ExpectStoredAsync(attached.Data.Id, $"{label} picture");
        draft.Kind.Should().Be(AiAttachment.KindEnum.Image);
        draft.Base64.Should().Be(expectedBase64);
    }

    [Theory]
    [InlineData("autotest-noext")]
    [InlineData("autotest-binary.bin")]
    public async Task SaveFile_StoredFileThePortalCannotReadAsText_IsRefused(string name)
    {
        // The reason several tests are careful to give their backing files a real extension: the same bytes attach or do
        // not attach depending on the name they were stored under.
        var suffix = Suffix();
        var bytes = System.Text.Encoding.UTF8.GetBytes(DeviceText);

        // Positive control first: these exact bytes are attachable under a name the portal understands, so the refusal
        // below is about the name alone.
        var readable = await UploadStoredAsync(Owner, "@my", $"autotest-control-{suffix}.txt", bytes, "text/plain");
        (await AttachByIdAsync(readable.Id, readable.Title)).Status.Should().Be(200, "the same bytes under a .txt name");

        var stored = await UploadStoredAsync(Owner, "@my", name.Replace("autotest-", $"autotest-{suffix}-", StringComparison.Ordinal), bytes, "application/octet-stream");

        // The upload itself succeeded — a file storage takes any name — so the difference really is at attach time.
        var refused = await AttachByIdAsync(stored.Id, stored.Title);

        refused.Status.Should().Be(400, $"attaching {stored.Title}");
    }

    [Fact]
    public void TestData_TheArchiveTheseTestsUploadReallyIsAnArchive()
    {
        // Guard for the test data. The 400 on a .zip only means "archives are refused" if the bytes are a genuine archive.
        var archive = CreateZip(("notes.txt", DeviceText), ("inner/second.txt", "second member"));

        System.Text.Encoding.Latin1.GetString(archive, 0, 2).Should().Be("PK");
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(archive), System.IO.Compression.ZipArchiveMode.Read);
        zip.Entries.Select(entry => entry.FullName).Should().Equal("notes.txt", "inner/second.txt");
    }

    private async Task ExportDocxAsync(string title, string content, int folderId)
    {
        await _aiChatClient.Authenticate(Owner);

        var exported = await _aiExport.AiExportTextToDocxWithHttpInfoAsync(
            new AiExportTextToDocxRequest(title: title, content: content, folderId: new AiExportTextToDocxRequestFolderId((decimal)folderId)),
            TestContext.Current.CancellationToken);

        exported.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    /// <summary>
    /// A real .pdf holding <paramref name="marker"/>, built by the portal itself: text-to-docx writes a document and the
    /// file storage converts it. Hand-assembled PDF bytes would make a 400 impossible to read.
    /// </summary>
    private async Task<FileDto> PdfWithMarkerAsync(int folderId, string title, string marker)
    {
        await ExportDocxAsync(title, $"Document body. {marker}", folderId);
        var docx = await WaitForFileAsync(Owner, folderId, $"{title}.docx");
        docx.Should().NotBeNull($"the .docx behind {title}.pdf was never exported");

        await FilesHttpClient.Authenticate(Owner);
        await FilesApi.SaveFileAsPdfAsync(docx!.Id, new SaveAsPdfRequest(folderId, $"{title}.pdf"), TestContext.Current.CancellationToken);

        var pdf = await WaitForFileAsync(Owner, folderId, $"{title}.pdf");
        pdf.Should().NotBeNull($"{title}.pdf never appeared in folder {folderId}");

        return pdf!;
    }

    /// <summary>A real .xlsx holding <paramref name="marker"/>, converted from a .csv by the file storage (<c>copyFileAs</c> converts when the extension differs).</summary>
    private async Task<FileDto> XlsxWithMarkerAsync(int folderId, string title, string marker)
    {
        var csv = await UploadStoredAsync(Owner, folderId.ToString(), $"{title}.csv", System.Text.Encoding.UTF8.GetBytes($"marker,value\n{marker},1\n"), "text/csv");

        await FilesHttpClient.Authenticate(Owner);
        await FilesApi.CopyFileAsAsync(
            csv.Id,
            new CopyAsRequest(destTitle: $"{title}.xlsx", destFolderId: new CopyAsRequestDestFolderId(folderId)),
            TestContext.Current.CancellationToken);

        var xlsx = await WaitForFileAsync(Owner, folderId, $"{title}.xlsx");
        xlsx.Should().NotBeNull($"{title}.xlsx never appeared in folder {folderId}");

        return xlsx!;
    }
}
