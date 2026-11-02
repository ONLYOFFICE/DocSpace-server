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

/// <summary><c>PUT /api/2.0/ai/threads/update-message</c>: what a successful edit changes, and what it leaves alone.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesUpdateTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task Update_RewritesOneMessageInPlace_AndLeavesNeighboursAndTheThreadAlone()
    {
        var threadId = await NewThreadAsync("Editing");
        var bystander = await NewThreadAsync("Bystander");
        List<string> ids =
        [
            await AppendAsync(threadId, "alpha"),
            await AppendAsync(threadId, "beta"),
            await AppendAsync(threadId, "gamma")
        ];
        await AppendAsync(bystander, "other thread");
        var before = await ReadMessagesAsync(threadId);
        var bystanderBefore = (await ReadMessagesAsync(bystander)).Select(Snapshot).ToList();
        var titleBefore = (await GetThreadAsync(threadId)).Title;

        var result = await UpdateTextAsync(ids[1], "beta, rewritten");

        result.Success.Should().BeTrue();
        var after = await ReadMessagesAsync(threadId);
        after.Select(m => m.Id).Should().Equal(ids, "same ids, same positions");
        after.Select(TextOf).Should().Equal("alpha", "beta, rewritten", "gamma");
        Snapshot(after[0]).Should().Be(Snapshot(before[0]), "the message before is untouched");
        Snapshot(after[2]).Should().Be(Snapshot(before[2]), "the message after is untouched");
        DateTimeOffset.Parse(after[1].CreatedAt).Should().BeOnOrAfter(DateTimeOffset.Parse(before[1].CreatedAt));
        (await ReadMessagesAsync(bystander)).Select(Snapshot).Should().Equal(bystanderBefore, "another thread is untouched");
        (await GetThreadAsync(threadId)).Title.Should().Be(titleBefore);
    }

    [Theory]
    [InlineData("cyrillic")]
    [InlineData("emoji")]
    [InlineData("whitespace")]
    [InlineData("markup")]
    [InlineData("long")]
    public async Task Update_AwkwardText_IsStoredExactlyAsSent(string key)
    {
        var text = AwkwardTexts[key];
        var threadId = await NewThreadAsync("Verbatim edit");
        var messageId = await AppendAsync(threadId, "plain");

        var result = await UpdateTextAsync(messageId, text);

        result.Success.Should().BeTrue();
        (await ReadTextsAsync(threadId)).Should().Equal(text);
    }

    [Fact]
    public async Task Update_EditingAgainOrToTheSameText_Works()
    {
        var threadId = await NewThreadAsync("Edit twice");
        var messageId = await AppendAsync(threadId, "v1");

        foreach (var text in new[] { "v2", "v2", "v3" })
        {
            (await UpdateTextAsync(messageId, text)).Success.Should().BeTrue(text);
            (await ReadTextsAsync(threadId)).Should().Equal(text);
        }

        (await ReadMessagesAsync(threadId)).Select(m => m.Id).Should().Equal(messageId);
    }

    [Fact]
    public async Task Update_ReadsDuringABurstOfEdits_OnlyEverSeeAWholeMessage()
    {
        var threadId = await NewThreadAsync("Race");
        var messageId = await AppendAsync(threadId, "v0");
        string[] versions = ["v1", "v2", "v3", "v4", "v5"];

        var writes = Task.WhenAll(versions.Select(text => UpdateTextAsync(messageId, text)));
        var reads = Task.WhenAll(versions.Select(_ => GetMessageAsync(messageId)));
        await Task.WhenAll(writes, reads);

        (await writes).Should().OnlyContain(result => result.Success);

        // Which version wins is not promised; that it is one of them is.
        string[] any = ["v0", .. versions];
        (await reads).Select(TextOf).Should().OnlyContain(text => text != null && any.Contains(text));
        (await ReadTextsAsync(threadId)).Should().ContainSingle().Which.Should().BeOneOf(versions);
    }
}
