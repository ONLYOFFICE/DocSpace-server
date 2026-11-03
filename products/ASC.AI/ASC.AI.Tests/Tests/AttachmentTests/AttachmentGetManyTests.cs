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
/// <c>POST /api/2.0/ai/attachments/get-many</c>: a positional batch read — an unknown id is a null in its place — and the
/// guarantee that a saved draft is readable, and a deleted one gone, on the very next call.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentGetManyTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    public async Task GetMany_UnknownIds_ComeBackAsNullInPlace()
    {
        var id = await SaveDraftAsync(Owner, "Autotest positional.docx");
        await ExpectStoredAsync(id);

        var read = await GetManyAsync(id, MissingId, id);

        read.Status.Should().Be(200, read.Error);
        read.Data.Should().HaveCount(3);
        read.Data![0].Should().NotBeNull();
        read.Data[1].Should().BeNull();
        // The same id twice keeps two positions rather than being deduplicated.
        read.Data[2]!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetMany_ALeadingUnknownId_DoesNotHideTheIdsAfterIt()
    {
        var id = await SaveDraftAsync(Owner, "Autotest order.docx");
        await ExpectStoredAsync(id);

        var read = await GetManyAsync(MissingId, id);

        read.Status.Should().Be(200, read.Error);
        read.Data![0].Should().BeNull();
        read.Data[1].Should().NotBeNull("the id after a miss still resolves");
    }

    [Fact]
    public async Task GetMany_EmptyArray_IsRefused()
    {
        // It used to answer 200 [] — a read of nothing. `ids` now has to name at least one draft.
        var refused = await GetManyAsync();

        refused.Status.Should().Be(400);
        refused.Error.Should().Be("ids is required and must be a non-empty array");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    public async Task GetMany_OneMalformedElement_RejectsTheWholeCall(string element)
    {
        // The route pulls uuids out of whatever JSON it gets and 400s when a value is not one.
        var id = await SaveDraftAsync(Owner, "Autotest poison.docx");

        var refused = await GetManyAsync(id, element);

        refused.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetMany_OneNullElement_RejectsTheWholeCall()
    {
        // Raw: a null array element is a body the typed list of strings is not meant to carry.
        var id = await SaveDraftAsync(Owner, "Autotest poison-null.docx");

        var refused = await PostRawAsync("get-many", $$"""["{{id}}",null]""");

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    public async Task GetMany_WrappedIdsList_IsAcceptedEvenThoughTheDocumentedBodyIsABareArray()
    {
        var id = await SaveDraftAsync(Owner, "Autotest wrapped.docx");

        var read = await PostRawAsync("get-many", Json(new { ids = new[] { id } }));

        read.Status.Should().Be(200, read.Body);
    }

    [Fact]
    [Trait("Bug", "82763")]
    public async Task GetMany_MissingBody_Is400()
    {
        // It used to be a 500.
        var refused = await PostRawAsync("get-many", null);

        refused.Status.Should().Be(400, refused.Body);
        refused.Error.Should().Be("ids is required and must be a non-empty array");
    }

    [Fact]
    [Trait("Bug", "82764")]
    public async Task GetMany_FreshlySavedDraft_IsAvailableOnAnImmediateRead()
    {
        // After a 200 on save a draft used to be readable on some calls and not others. Over ten independent drafts
        // rather than ten reads of one: a single draft can be readable by luck, ten in a row cannot.
        var misses = new List<string>();

        for (var index = 0; index < 10; index++)
        {
            var probeId = await SaveDraftAsync(Owner, $"Autotest first-read-{index}.docx");

            if (await ReadOnceAsync(probeId) is null)
            {
                misses.Add(probeId);
            }
        }

        misses.Should().BeEmpty("drafts invisible to the read right after their save");
    }

    [Fact]
    [Trait("Bug", "82767")]
    public async Task GetMany_AfterASingleDelete_NoLongerServesTheDraft()
    {
        // The same symptom on the write side, and the more dangerous half: a client that deleted an attachment was told
        // `{success:true}` while the record kept being served to later reads.
        var survivors = new List<string>();

        for (var index = 0; index < 10; index++)
        {
            var probeId = await SaveDraftAsync(Owner, $"Autotest single-delete-{index}.docx");

            var deleted = await DeleteAsync(probeId);
            deleted.Status.Should().Be(200, deleted.Error);
            deleted.Data!.Success.Should().BeTrue();

            // Three immediate reads: one lucky miss must not clear a draft that is still served.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (await ReadOnceAsync(probeId) is not null)
                {
                    survivors.Add(probeId);
                    break;
                }
            }
        }

        survivors.Should().BeEmpty("drafts still readable after a delete that reported success");
    }
}
