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
/// <c>POST /api/2.0/ai/threads/append-user-message</c>: refused requests, and that a refusal stores nothing. A missing
/// message and an unknown thread are in <see cref="MessageCreateTests"/>.
/// </summary>
/// <remarks>
/// A request the SDK can express goes through it; only the bodies the SDK request cannot carry (no thread id, a null
/// or role-less message, an invented role, no body at all) go raw.
/// </remarks>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesAppendValidationTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Theory]
    [InlineData("a malformed threadId", 400)]
    [InlineData("a deleted threadId", 404)]
    public async Task Append_UnusableThreadId_IsRefusedAndStoresNothing(string kind, int expected)
    {
        var threadId = await NewThreadAsync("Strict");
        await AppendAsync(threadId, "control");

        var target = MalformedId;
        if (kind == "a deleted threadId")
        {
            target = await NewThreadAsync("Doomed");
            (await DeleteThreadAsync(target)).Success.Should().BeTrue();
        }

        var status = await StatusOfAsync(() => _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: target, message: BuildUserMessage("x")),
            TestContext.Current.CancellationToken));

        (await ReadTextsAsync(threadId)).Should().Equal(["control"], "nothing was stored");
        status.Should().Be(expected, kind);
    }

    [Theory]
    [InlineData("no threadId", 400)]
    [InlineData("an empty body", 400)]
    [InlineData("no body at all", 400)]
    public async Task Append_RefusedRequest_StoresNothing(string kind, int expected)
    {
        // Raw: the SDK request requires both the thread id and the message, so none of these bodies can be built typed.
        var threadId = await NewThreadAsync("Strict");
        await AppendAsync(threadId, "control");

        var body = kind switch
        {
            "no threadId" => $$"""{"message":{{UserMessageJson("x")}}}""",
            "an empty body" => "{}",
            _ => null
        };

        var status = await PostAppendRawAsync(body);

        (await ReadTextsAsync(threadId)).Should().Equal(["control"], "nothing was stored");
        ((int)status).Should().Be(expected, kind);
    }

    /// <summary>
    /// send-with-stream refuses an empty message (BUG 82720); this route used to store it verbatim and answer 200,
    /// leaving an empty user turn in the history every later model call is built from. Fixed: the message shape is
    /// validated before it is stored.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Bug", "84409")]
    public async Task Append_EmptyOrWhitespaceText_IsRefused(string text)
    {
        var threadId = await NewThreadAsync("No blanks");
        await AppendAsync(threadId, "control");

        var status = await StatusOfAsync(() => _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: threadId, message: BuildUserMessage(text)),
            TestContext.Current.CancellationToken));

        (await ReadTextsAsync(threadId)).Should().Equal(["control"], "nothing blank was stored");
        status.Should().Be(400);
    }

    /// <summary>
    /// <c>message: null</c> used to be stored as the four-character string "null", and a message with no
    /// <c>content</c> as a message with no content. Fixed together with the empty text.
    /// </summary>
    [Theory]
    [InlineData("null")]
    [InlineData("""{"role":"user"}""")]
    [Trait("Bug", "84409")]
    public async Task Append_NullMessageOrNoContent_IsRefused(string messageJson)
    {
        var threadId = await NewThreadAsync("Real content only");
        await AppendAsync(threadId, "control");

        // Raw: the SDK request refuses a null message and a message without content client-side.
        var status = await PostAppendRawAsync($$"""{"threadId":"{{threadId}}","message":{{messageJson}}}""");

        (await ReadTextsAsync(threadId)).Should().Equal(["control"], "nothing was stored");
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// The route is named for user messages, yet it used to store <c>assistant</c>, <c>system</c>, an invented role
    /// and no role at all; an <c>assistant</c> turn written here is replayed to the model as something it said
    /// itself. Fixed: anything but a user message is a 400.
    /// </summary>
    [Theory]
    [InlineData(AiThreadMessageLike.RoleEnum.Assistant)]
    [InlineData(AiThreadMessageLike.RoleEnum.System)]
    [Trait("Bug", "84410")]
    public async Task Append_NonUserRole_IsRefused(AiThreadMessageLike.RoleEnum role)
    {
        var threadId = await NewThreadAsync("Users only");
        await AppendAsync(threadId, "control");

        var forged = BuildUserMessage("forged");
        forged.Role = role;
        var status = await StatusOfAsync(() => _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: threadId, message: forged),
            TestContext.Current.CancellationToken));

        var stored = await ReadMessagesAsync(threadId);
        stored.Select(m => m.Role).Should().Equal([AiThreadMessageLike.RoleEnum.User], "only the control user message is stored");
        status.Should().Be(400, $"role {role}");
    }

    [Theory]
    [InlineData("invented")]
    [InlineData(null)]
    [Trait("Bug", "84410")]
    public async Task Append_InventedOrMissingRole_IsRefused(string? role)
    {
        // Raw: the SDK role enum cannot carry an invented value, and the required role cannot be left out.
        var threadId = await NewThreadAsync("Users only");
        await AppendAsync(threadId, "control");

        var message = role is null
            ? JsonSerializer.Serialize(new { content = new[] { new { type = "text", text = "forged" } } })
            : JsonSerializer.Serialize(new { role, content = new[] { new { type = "text", text = "forged" } } });
        var status = await PostAppendRawAsync($$"""{"threadId":"{{threadId}}","message":{{message}}}""");

        var stored = await ReadMessagesAsync(threadId);
        stored.Select(m => m.Role).Should().Equal([AiThreadMessageLike.RoleEnum.User], "only the control user message is stored");
        status.Should().Be(HttpStatusCode.BadRequest, $"role {role ?? "(none)"}");
    }
}
