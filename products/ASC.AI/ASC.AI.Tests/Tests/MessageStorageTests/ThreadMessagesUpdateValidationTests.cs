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
/// <c>PUT /api/2.0/ai/threads/update-message</c>: refused edits, and that a refusal leaves the message as it was. A
/// missing message and an unknown id are in <see cref="MessageUpdateTests"/>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesUpdateValidationTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Theory]
    [InlineData("no messageId", 400)]
    [InlineData("a malformed messageId", 400)]
    [InlineData("a deleted message", 404)]
    [InlineData("a cleared message", 404)]
    [InlineData("an empty body", 400)]
    [InlineData("no body at all", 400)]
    public async Task Update_RefusedEdit_LeavesTheMessageAsItWas(string kind, int expected)
    {
        var threadId = await NewThreadAsync("Strict edit");
        await AppendAsync(threadId, "original");

        HttpStatusCode status;
        switch (kind)
        {
            case "no messageId":
                // Raw: messageId is a required constructor parameter of the SDK request.
                status = await PutUpdateRawAsync($$"""{"message":{{UserMessageJson("changed")}}}""");
                break;
            case "an empty body":
                status = await PutUpdateRawAsync("{}");
                break;
            case "no body at all":
                status = await PutUpdateRawAsync(null);
                break;
            default:
                var target = MalformedId;
                if (kind == "a deleted message")
                {
                    target = await AppendAsync(threadId, "to delete");
                    await DeleteMessageAsync(target);
                }
                else if (kind == "a cleared message")
                {
                    var cleared = await NewThreadAsync("Cleared");
                    target = await AppendAsync(cleared, "x");
                    await ClearMessagesAsync(cleared);
                }

                var exception = await Assert.ThrowsAsync<ApiException>(async () => await UpdateTextAsync(target, "changed"));
                status = (HttpStatusCode)exception.ErrorCode;
                break;
        }

        (await ReadTextsAsync(threadId)).Should().Equal(["original"], "the original text survived");
        ((int)status).Should().Be(expected, kind);
    }

    /// <summary>
    /// Same defect as an empty append, but destructive: the stored message used to be really replaced by an empty
    /// one (<c>content: []</c> for an empty parts list), as rename once did to a title (BUG 83094). Fixed: the
    /// replacement is validated before it is stored.
    /// </summary>
    [Theory]
    [InlineData("an empty text")]
    [InlineData("a whitespace text")]
    [InlineData("an empty parts list")]
    [Trait("Bug", "84412")]
    public async Task Update_EmptyText_IsRefusedAndDoesNotWipeTheMessage(string kind)
    {
        var threadId = await NewThreadAsync("Do not wipe");
        var messageId = await AppendAsync(threadId, "precious");

        HttpStatusCode status;
        if (kind == "an empty parts list")
        {
            // Raw: the SDK content model cannot be built with no blocks at all.
            status = await PutUpdateRawAsync($$$"""{"messageId":"{{{messageId}}}","message":{"role":"user","content":[]}}""");
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await UpdateTextAsync(messageId, kind == "an empty text" ? string.Empty : "   "));
            status = (HttpStatusCode)exception.ErrorCode;
        }

        (await ReadTextsAsync(threadId)).Should().Equal(["precious"], "the message survived");
        status.Should().Be(HttpStatusCode.BadRequest, kind);
    }

    /// <summary>
    /// An edit replaces the content; it used to let a user message be turned into an <c>assistant</c> one — a model
    /// reply nobody ever generated. Fixed: the role is pinned to the stored one.
    /// </summary>
    [Fact]
    [Trait("Bug", "84413")]
    public async Task Update_RoleOfAStoredMessage_CannotBeChanged()
    {
        var threadId = await NewThreadAsync("Role flip");
        var messageId = await AppendAsync(threadId, "I am the user");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(
                messageId: messageId,
                message: new AiThreadMessageLike(
                    role: AiThreadMessageLike.RoleEnum.Assistant,
                    content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: "I am the model")]))),
            TestContext.Current.CancellationToken));

        var stored = (await ReadMessagesAsync(threadId))[0];
        stored.Role.Should().Be(AiThreadMessageLike.RoleEnum.User, "the role is still user");
        TextOf(stored).Should().Be("I am the user", "the text is still the user's");
        exception.ErrorCode.Should().Be(400, "changing the role");
    }
}
