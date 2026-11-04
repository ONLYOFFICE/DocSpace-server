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
/// The refusals of <c>POST /api/2.0/ai/attachments/save-file</c>: a malformed body, an id that resolves to nothing, a
/// title or content of the wrong type. Every one used to collapse to a 500 inside the engine.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentSaveFileValidationTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "82739")]
    public async Task SaveFile_TheBodyTheSdkDocumentsWithAStoragePathInPath_Is404()
    {
        // `AiAttachmentsSaveFileRequestInput` describes `path` as "Storage path/key of the file", so a client generated
        // from the SDK sends a path-shaped string, which used to answer 500 and was once expected to answer 200. 200 was
        // never right: `path` is the DocSpace file id as a string, so a path-shaped value is an id that resolves to
        // nothing — a 404, like BUG 82742. What stays wrong is the SDK description of `path`, a documentation matter.
        var fileId = await BackingFileIdAsync(Owner, "Autotest sdk-body.docx", "x");

        // Control: the same body with the file id as the string `path` is accepted, so the 404 below is about what
        // `path` holds and not about the body shape.
        var control = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput(fileId.ToString(), "x", DocumentType)));
        control.Status.Should().Be(200, "the documented body with a real file id");

        var pathShaped = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput("My Documents/a.docx", "x", DocumentType)));

        pathShaped.Status.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "82740")]
    public async Task SaveFile_OptionalTitleOmitted_IsAcceptedAndTheTitleComesFromTheFile()
    {
        const string name = "Autotest untitled.docx";
        var fileId = await BackingFileIdAsync(Owner, name, "x");

        var saved = await AttachByIdAsync(fileId);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data!.Title.Should().Be(name, "the title comes from the file");
    }

    [Theory]
    [Trait("Bug", "82741")]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("""{"input":null}""")]
    [InlineData("""{"title":"a.docx","content":"x"}""")]
    [InlineData("""{"inputs":[{"title":"a.docx"}]}""")]
    [InlineData("""{"input":{"title":"a.docx","content":"","type":7}}""")]
    [InlineData("""{"input":{"path":null,"title":"a.docx","content":"","type":7}}""")]
    public async Task SaveFile_MalformedBody_Is400(string? body)
    {
        // Every one of these used to be a 500: no body, an empty object, a null input, a flat body without the input
        // wrapper, `inputs[]` instead of `input`, an input without a path, a null path. Raw: bodies the SDK model, which
        // requires path, content and type, cannot express.
        var refused = await PostRawAsync("save-file", body);

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    [Trait("Bug", "84389")]
    public async Task SaveFile_EmptyPath_Is400()
    {
        // A missing path and a null path are both a 400 (above); an empty string is the one malformed value that used
        // to crash the request with a 500.
        var refused = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput(string.Empty, string.Empty, DocumentType, "a.docx")));

        refused.Status.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "82742")]
    public async Task SaveFile_PathThatResolvesToNoFile_Is404()
    {
        // `path` is the DocSpace file id as a string — a resolvable one answers 200 with the file's text extracted
        // server-side. An id that resolves to nothing used to crash the request with a 500; it is a 404 now. Both
        // shapes of "nothing": an id no file has and a path-shaped string, which is just another id that resolves to
        // nothing.
        var stored = await UploadTextAsync(Owner, "@my", "autotest-path-control.txt", "control body");

        // Control: a real file id is accepted, so the 404s below are about the id resolving to nothing.
        var resolvable = await AttachByIdAsync(stored.Id, stored.Title);
        resolvable.Status.Should().Be(200, "a path that is a real file id");
        resolvable.Data!.Content.Should().Be("control body", "the server extracted the file's text");

        var missingId = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput("999999999", string.Empty, DocumentType, "a.docx")));
        var pathShaped = await SaveFileAsync(new AiAttachmentsSaveFileRequest(
            new AiAttachmentsSaveFileRequestInput("My Documents/a.docx", string.Empty, DocumentType, "a.docx")));

        missingId.Status.Should().Be(404, "an id that resolves to nothing is a client error, not a crash");
        pathShaped.Status.Should().Be(404, "a path-shaped string is just another id that resolves to nothing");
    }

    [Theory]
    [Trait("Bug", "82748")]
    [InlineData("123")]
    [InlineData("true")]
    public async Task SaveFile_TitleThatIsNotAString_Is400(string titleJson)
    {
        // Raw: the SDK types `title` as a string.
        var fileId = await BackingFileIdAsync(Owner, "Autotest titled.docx", "x");

        var refused = await PostRawAsync(
            "save-file",
            $$$"""{"input":{"path":"{{{fileId}}}","title":{{{titleJson}}},"content":"","type":7}}""");

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Be("input.title must be a string when present");
    }

    [Theory]
    [Trait("Bug", "82748")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveFile_BlankTitle_IsAcceptedAndIgnored(string title)
    {
        // No draft can carry a blank or non-string title: a blank one is accepted but never used — the title of a
        // draft comes from the file behind `path`.
        const string name = "Autotest titled.docx";
        var fileId = await BackingFileIdAsync(Owner, name, "x");

        var saved = await AttachByIdAsync(fileId, title);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data!.Title.Should().Be(name, "the file's name is used instead");
    }

    [Fact]
    [Trait("Bug", "82749")]
    public async Task SaveFile_NonStringContent_IsRefused()
    {
        // Raw: the SDK types `content` as a string.
        var fileId = await BackingFileIdAsync(Owner, "Autotest typed.docx", "x");

        var refused = await PostRawAsync(
            "save-file",
            $$$"""{"input":{"path":"{{{fileId}}}","title":"Autotest typed.docx","content":123,"type":7}}""");

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Be("input.content is required and must be a string");
    }

    [Fact]
    public async Task SaveFile_PathSentAsANumber_Is400()
    {
        // The id has to be a string: a JSON number is refused outright, which is worth pinning because the field is
        // called `path`. Raw: the SDK types `path` as a string.
        var stored = await UploadTextAsync(Owner, "@my", $"autotest-numeric-path-{Suffix()}.txt", DeviceText);

        var control = await AttachByIdAsync(stored.Id, stored.Title);
        control.Status.Should().Be(200, "the same id as a string is accepted");

        var refused = await PostRawAsync(
            "save-file",
            $$$"""{"input":{"path":{{{stored.Id}}},"title":"{{{stored.Title}}}","content":"","type":7}}""");

        refused.Status.Should().Be(400, refused.Body);
    }
}
