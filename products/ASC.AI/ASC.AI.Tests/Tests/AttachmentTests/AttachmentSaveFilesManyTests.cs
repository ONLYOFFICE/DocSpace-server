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
/// <c>POST /api/2.0/ai/attachments/save-files-many</c>: the batch form of save-file. Every element is validated with
/// the single route's rules and a bad one is a 400 that names its position.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentSaveFilesManyTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    public async Task SaveFilesMany_Batch_StoresDraftsInOrderWithUniqueIds()
    {
        // `type` is not validated or normalized: each element's type is echoed back as sent.
        (string Title, string Body, FileType Type)[] files =
        [
            ("batch-1.docx", "one", FileType.Document),
            ("batch-2.docx", "two", FileType.Spreadsheet),
            ("batch-3.docx", "three", FileType.Pdf)
        ];
        var inputs = new List<AiAttachmentsSaveFileRequestInput>();

        // A backing file per element, so the titles the response is checked against are the files' own names.
        foreach (var file in files)
        {
            var fileId = await BackingFileIdAsync(Owner, file.Title, file.Body);
            inputs.Add(FileInput(fileId.ToString(), type: (decimal)(int)file.Type));
        }

        var saved = await OutcomeAsync(() => Attachments.AiAttachmentsSaveFilesManyWithHttpInfoAsync(
            new AiAttachmentsSaveFilesManyRequest(inputs),
            TestContext.Current.CancellationToken));

        saved.Status.Should().Be(200, saved.Error);
        saved.Data.Should().HaveCount(inputs.Count);
        saved.Data!.Select(item => item.Title).Should().Equal(files.Select(file => file.Title));
        saved.Data!.Select(item => item.Content).Should().Equal(files.Select(file => file.Body));
        saved.Data!.Select(item => item.Type).Should().Equal(files.Select(file => (decimal)(int)file.Type));
        saved.Data!.Select(item => item.Id).Distinct().Should().HaveCount(inputs.Count);
        saved.Data!.ForEach(item => ExpectDraftShape(item, AiAttachment.KindEnum.File));
    }

    [Fact]
    public async Task SaveFilesMany_FiftyDrafts_AreAcceptedWhole()
    {
        // No documented cap on the element count, and none observed. Fifty distinct files rather than fifty references
        // to one, because a batch collapses repeats of the same path into a single draft.
        var fileIds = new List<int>();

        for (var index = 0; index < 50; index++)
        {
            fileIds.Add(await BackingFileIdAsync(Owner, $"Autotest bulk-{index}.docx", $"body {index}"));
        }

        var saved = await SaveFilesManyAsync(fileIds);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data.Should().HaveCount(fileIds.Count);
        saved.Data!.Select(item => item.Id).Distinct().Should().HaveCount(fileIds.Count);
        saved.Data!.Select(item => item.Title).Should().Equal(Enumerable.Range(0, 50).Select(index => $"Autotest bulk-{index}.docx"));
    }

    [Fact]
    public async Task SaveFilesMany_RepeatsOfOnePath_CollapseToASingleDraft()
    {
        // Inside a batch the same file is stored once: the array keeps its length and every position carries the same
        // id. Two separate save-file calls do NOT collapse — which is what makes this a property of the batch.
        var fileId = await BackingFileIdAsync(Owner, "twin.docx", "same");

        var batch = await SaveFilesManyAsync([fileId, fileId, fileId]);

        batch.Status.Should().Be(200, batch.Error);
        batch.Data.Should().HaveCount(3);
        batch.Data!.Select(item => item.Id).Distinct().Should().HaveCount(1);

        var first = await AttachByIdAsync(fileId);
        var second = await AttachByIdAsync(fileId);
        first.Data!.Id.Should().NotBe(second.Data!.Id, "one at a time, the same file is stored twice");
    }

    [Fact]
    public async Task SaveFilesMany_EmptyInputsArray_ReturnsAnEmptyArray()
    {
        var saved = await OutcomeAsync(() => Attachments.AiAttachmentsSaveFilesManyWithHttpInfoAsync(
            new AiAttachmentsSaveFilesManyRequest(inputs: []),
            TestContext.Current.CancellationToken));

        saved.Status.Should().Be(200, saved.Error);
        saved.Data.Should().BeEmpty();
    }

    [Theory]
    [Trait("Bug", "82754")]
    [InlineData("{}")]
    [InlineData("""{"inputs":null}""")]
    public async Task SaveFilesMany_MissingOrNullInputs_Is400NotAnEmptyBatch(string body)
    {
        // Both used to answer 200 [], a success a client that sent the wrong field name could not tell apart from the
        // genuinely empty batch. Raw: the SDK requires `inputs`.
        var refused = await PostRawAsync("save-files-many", body);

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Be("inputs must be an array");
    }

    [Fact]
    [Trait("Bug", "82754")]
    public async Task SaveFilesMany_OneInvalidElement_Is400NamingIt()
    {
        // An invalid element used to take the batch down with a 500 naming neither the bad element nor the good ones.
        // Raw: an element without `content`, which the SDK input model cannot express.
        var fileId = await BackingFileIdAsync(Owner, "good.docx", "a");

        var refused = await PostRawAsync(
            "save-files-many",
            $$$"""
            {"inputs":[
              {"path":"{{{fileId}}}","title":"good-1.docx","content":"","type":7},
              {"path":"{{{fileId}}}","title":"no content here.docx","type":7},
              {"path":"{{{fileId}}}","title":"good-2.docx","content":"","type":7}]}
            """);

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Be("inputs[1]: input.content is required and must be a string");
    }

    [Theory]
    [Trait("Bug", "82754")]
    [InlineData("\"a string\"")]
    [InlineData("[null]")]
    [InlineData("[\"a string element\"]")]
    public async Task SaveFilesMany_NonArrayOrNonObjectInputs_Is400(string inputsJson)
    {
        // Every one of these used to be a 500. Raw: wrong-typed values.
        var refused = await PostRawAsync("save-files-many", $$$"""{"inputs":{{{inputsJson}}}}""");

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    public async Task SaveFilesMany_BatchOfOne_SavesTheFileTheWayTheSingleRouteDoes()
    {
        const string name = "Autotest single-batch.docx";
        var fileId = await BackingFileIdAsync(Owner, name, "only one");

        var saved = await SaveFilesManyAsync([fileId]);

        saved.Status.Should().Be(200, saved.Error);
        saved.Data.Should().HaveCount(1);
        ExpectDraftShape(saved.Data![0], AiAttachment.KindEnum.File);
        saved.Data[0].Title.Should().Be(name);
        saved.Data[0].Content.Should().Be("only one");
        saved.Data[0].Path.Should().Be($"{fileId}/{name}");

        (await ExpectStoredAsync(saved.Data[0].Id)).Content.Should().Be("only one");
    }

    [Fact]
    public async Task SaveFilesMany_NumericPathInsideAnElement_Is400NamingIt()
    {
        // The file id has to be a string, same as for save-file; the refusal also points at the offending element.
        var fileId = await BackingFileIdAsync(Owner, "Autotest numeric-path.docx", "x");

        // Control: the same elements with the id as a string are accepted, so the refusal below is about the number.
        var control = await SaveFilesManyAsync([fileId, fileId]);
        control.Status.Should().Be(200, control.Error);

        var refused = await PostRawAsync(
            "save-files-many",
            $$$"""
            {"inputs":[
              {"path":"{{{fileId}}}","content":"","type":7},
              {"path":{{{fileId}}},"content":"","type":7}]}
            """);

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Contain("inputs[1]");
    }

    [Fact]
    public async Task SaveFilesMany_BatchHoldingAFileThatDoesNotExist_Is404()
    {
        // Status only: a failed batch returns no ids, so whether the valid element was stored anyway cannot be observed.
        var fileId = await BackingFileIdAsync(Owner, "Autotest mixed-missing.docx", "x");

        var control = await SaveFilesManyAsync([fileId]);
        control.Status.Should().Be(200, "the valid element on its own");

        var mixed = await OutcomeAsync(() => Attachments.AiAttachmentsSaveFilesManyWithHttpInfoAsync(
            new AiAttachmentsSaveFilesManyRequest([FileInput(fileId.ToString()), FileInput("999999999")]),
            TestContext.Current.CancellationToken));

        mixed.Status.Should().Be(404);
    }

    [Fact]
    public async Task SaveFilesMany_BatchHoldingAnArchive_Is400()
    {
        // The archive rule is applied per element at attach time; in a batch it refuses the request.
        var text = await UploadTextAsync(Owner, "@my", "autotest-mixed.txt", "plain text");
        var archive = await UploadStoredAsync(Owner, "@my", "autotest-mixed.zip", CreateZip(("inner.txt", "inside")), "application/zip");

        var control = await SaveFilesManyAsync([text.Id]);
        control.Status.Should().Be(200, "the text file on its own");

        var mixed = await SaveFilesManyAsync([text.Id, archive.Id]);

        mixed.Status.Should().Be(400);
    }

    [Fact]
    public async Task SaveFilesMany_SavedBatch_ReadsBackIsDeletedAndThenReadsAsNull()
    {
        string[] names = ["Autotest lifecycle-1.docx", "Autotest lifecycle-2.docx", "Autotest lifecycle-3.docx"];
        var fileIds = new List<int>();

        for (var index = 0; index < names.Length; index++)
        {
            fileIds.Add(await BackingFileIdAsync(Owner, names[index], $"lifecycle {index}"));
        }

        var saved = await SaveFilesManyAsync(fileIds);
        saved.Status.Should().Be(200, saved.Error);
        saved.Data.Should().HaveCount(names.Length);
        var ids = saved.Data!.Select(item => item.Id).ToArray();
        ids.Distinct().Should().HaveCount(names.Length, "save-files-many stores one draft per file");

        var read = await GetManyAsync(ids);
        read.Status.Should().Be(200, read.Error);
        read.Data!.Select(item => item?.Id).Should().Equal(ids, "get-many returns every draft, in order");
        read.Data!.Select(item => item?.Title).Should().Equal(names);

        var deleted = await DeleteManyAsync(ids);
        deleted.Status.Should().Be(200, deleted.Error);
        deleted.Data!.Success.Should().BeTrue();

        foreach (var id in ids)
        {
            await ExpectAbsentAsync(id, "a draft after delete-many");
        }

        var after = await GetManyAsync(ids);
        after.Status.Should().Be(200);
        after.Data.Should().HaveCount(ids.Length, "the batch keeps every position");
        after.Data!.Should().OnlyContain(item => item == null, "every deleted id reads as null in place");
    }
}
