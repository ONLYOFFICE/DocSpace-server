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
/// <c>GET /api/2.0/ai/threads/get-message-by-id</c>. A never-issued id and a deleted message (both 404) are in
/// <see cref="MessageReadTests"/> and <see cref="MessageDeleteTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesGetByIdTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task GetMessageById_ServesTheStoredMessageFollowsAnEditAndReadingChangesNothing()
    {
        var threadId = await NewThreadAsync("By id");
        var first = await AppendAsync(threadId, "first");
        var second = await AppendAsync(threadId, "second");

        var read = await GetMessageAsync(first);
        read.Id.Should().Be(first);
        read.Role.Should().Be(AiThreadMessageLike.RoleEnum.User);
        TextOf(read).Should().Be("first");
        DateTimeOffset.TryParse(read.CreatedAt, out _).Should().BeTrue($"createdAt {read.CreatedAt} is a timestamp");
        Snapshot(await GetMessageAsync(first)).Should().Be(Snapshot(read), "a repeat read is identical");

        await UpdateTextAsync(first, "first, edited");
        TextOf(await GetMessageAsync(first)).Should().Be("first, edited");

        await DeleteMessageAsync(second);
        TextOf(await GetMessageAsync(first)).Should().Be("first, edited", "deleting another message leaves this one alone");
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("empty")]
    [InlineData("missing")]
    public async Task GetMessageById_MalformedEmptyOrMissingId_Returns400(string kind)
    {
        var threadId = await NewThreadAsync("Control");
        var control = await AppendAsync(threadId, "control");

        HttpStatusCode status;
        if (kind == "missing")
        {
            // Raw on purpose: messageId is a required parameter of the typed call, so it cannot be left out.
            status = await GetRawStatusAsync(GetMessageByIdPath);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await GetMessageAsync(kind == "malformed" ? MalformedId : string.Empty));
            status = (HttpStatusCode)exception.ErrorCode;
        }

        TextOf(await GetMessageAsync(control)).Should().Be("control");
        status.Should().Be(HttpStatusCode.BadRequest, $"{kind} id");
    }

    /// <summary>
    /// The route used to answer 200 with a body of <c>null</c> for a message that does not exist — a never-issued
    /// id, a deleted one, a cleared one, one whose thread was deleted — while update-message answered 404 for the
    /// same ids. Fixed: the storage's null is now a 404 "message not found".
    /// </summary>
    [Theory]
    [InlineData("cleared")]
    [InlineData("deleted thread")]
    [Trait("Bug", "84411")]
    public async Task GetMessageById_MessageThatNoLongerExists_Returns404(string kind)
    {
        var control = await NewThreadAsync("Control");
        var controlMessage = await AppendAsync(control, "alive");

        var threadId = await NewThreadAsync(kind);
        var messageId = await AppendAsync(threadId, "x");
        if (kind == "cleared")
        {
            await ClearMessagesAsync(threadId);
        }
        else
        {
            await DeleteThreadAsync(threadId);
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(messageId));

        TextOf(await GetMessageAsync(controlMessage)).Should().Be("alive");
        exception.ErrorCode.Should().Be(404, kind);
    }
}
