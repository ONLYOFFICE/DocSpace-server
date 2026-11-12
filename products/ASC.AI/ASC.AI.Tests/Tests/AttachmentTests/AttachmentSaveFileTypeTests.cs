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
/// The <c>type</c> of <c>POST /api/2.0/ai/attachments/save-file</c>: an open-ended ONLYOFFICE file type code that the
/// server neither validates against the DocSpace file categories nor rewrites — any JSON number is accepted and echoed
/// back, a numeric string is coerced to a number, and anything that is not a number is a 400.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentSaveFileTypeTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "83003")]
    public async Task SaveFile_RequestedType_IsNotValidatedOrNormalized()
    {
        // The by-design normalization of BUG 83003/82743 regressed: the save response echoes whatever `type` the client
        // sent instead of deriving it from the backing file. `type` is a field the server no longer polices; this pins
        // that the save still succeeds, does not silently rewrite the value on its own response, and that a later read
        // keeps the rest of the record.
        var fileId = await BackingFileIdAsync(Owner, "Autotest typed.docx", "x");

        var saved = await AttachByIdAsync(fileId, type: (decimal)(int)FileType.Spreadsheet);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data!.Type.Should().Be((decimal)(int)FileType.Spreadsheet);

        var stored = await ExpectStoredAsync(saved.Data.Id);
        stored.Title.Should().Be("Autotest typed.docx", "the rest of the record survives the read");
    }

    [Theory]
    [InlineData(FileType.Unknown)]
    [InlineData(FileType.Archive)]
    [InlineData(FileType.Video)]
    [InlineData(FileType.Audio)]
    [InlineData(FileType.Image)]
    [InlineData(FileType.Spreadsheet)]
    [InlineData(FileType.Presentation)]
    [InlineData(FileType.Document)]
    [InlineData(FileType.Pdf)]
    [InlineData(FileType.Diagram)]
    public async Task SaveFile_EveryFileTypeValue_IsAcceptedUnchecked(FileType type)
    {
        var fileId = await BackingFileIdAsync(Owner, "Autotest type.docx", "x");

        var saved = await AttachByIdAsync(fileId, type: (decimal)(int)type);

        saved.Status.Should().Be(200, saved.Error);
        // `kind` is decided by the file behind `path`, never by `type` — even FileType.Image saved for a .docx stays a file.
        saved.Data!.Kind.Should().Be(AiAttachment.KindEnum.File);
        saved.Data.Type.Should().Be((decimal)(int)type);
    }

    [Theory]
    [InlineData(999.0)]
    [InlineData(-1.0)]
    [InlineData(8.0)]
    [InlineData(1.5)]
    public async Task SaveFile_AnyNumericType_IsAcceptedAndEchoedLikeAnInEnumOne(double type)
    {
        // `type` is a client-decided number: an out-of-range int and a fractional one are echoed back unchanged. Only
        // a value that is not a JSON number at all is a type error (below).
        var fileId = await BackingFileIdAsync(Owner, "Autotest type.docx", "x");

        var saved = await AttachByIdAsync(fileId, "Autotest undefined type.docx", (decimal)type);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data!.Type.Should().Be((decimal)type);
    }

    [Theory]
    [Trait("Bug", "82745")]
    [InlineData("true")]
    [InlineData("""{"value":7}""")]
    [InlineData("[7]")]
    public async Task SaveFile_TypeThatIsNotAJsonNumber_Is400(string typeJson)
    {
        // Any number is accepted (above — 1.5 included, which this bug used to ask to be refused), but a boolean, an
        // object and an array are not numbers at all. Raw: the SDK types `type` as a decimal.
        var fileId = await BackingFileIdAsync(Owner, "Autotest mistyped.docx", "x");

        var refused = await PostRawAsync(
            "save-file",
            $$$"""{"input":{"path":"{{{fileId}}}","title":"Autotest mistyped.docx","content":"","type":{{{typeJson}}}}}""");

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    [Trait("Bug", "82746")]
    public async Task SaveFile_NumericStringType_IsCoercedToANumber()
    {
        // Accepting "7" is defensible — a binder coercing a numeric string is normal. What was not: the value came back
        // as the string "7", so this record's `type` had a different JSON type from every other record's. Raw: the SDK
        // types `type` as a decimal and cannot send a string.
        var fileId = await BackingFileIdAsync(Owner, "Autotest string type.docx", "x");

        var saved = await PostRawAsync(
            "save-file",
            $$$"""{"input":{"path":"{{{fileId}}}","title":"Autotest string type.docx","content":"","type":"7"}}""");

        saved.Status.Should().Be(200, saved.Body);
        using var document = JsonDocument.Parse(saved.Body);
        var type = document.RootElement.GetProperty("type");
        type.ValueKind.Should().Be(JsonValueKind.Number, "a numeric string type is coerced");
        type.GetDecimal().Should().Be(DocumentType);
    }
}
