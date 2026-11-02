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
/// The bodies go raw: most of them cannot be expressed by the SDK request (no thread id, a null or role-less
/// message, an invented role), and the SDK cannot read the success answer either, which is what a still-open bug
/// would return (see <see cref="MessageTestBase.AppendAsync"/>).
/// </remarks>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Messages")]
public class ThreadMessagesAppendValidationTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Theory]
    [InlineData("no threadId", 400)]
    [InlineData("a malformed threadId", 400)]
    [InlineData("a deleted threadId", 404)]
    [InlineData("an empty body", 400)]
    [InlineData("no body at all", 400)]
    public async Task Append_RefusedRequest_StoresNothing(string kind, int expected)
    {
        var threadId = await NewThreadAsync("Strict");
        await AppendAsync(threadId, "control");
        var message = UserMessageJson("x");

        string? body;
        switch (kind)
        {
            case "no threadId":
                body = $$"""{"message":{{message}}}""";
                break;
            case "a malformed threadId":
                body = $$"""{"threadId":"{{MalformedId}}","message":{{message}}}""";
                break;
            case "a deleted threadId":
                var doomed = await NewThreadAsync("Doomed");
                (await DeleteThreadAsync(doomed)).Success.Should().BeTrue();
                body = $$"""{"threadId":"{{doomed}}","message":{{message}}}""";
                break;
            case "an empty body":
                body = "{}";
                break;
            default:
                body = null;
                break;
        }

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

        var status = await PostAppendRawAsync($$"""{"threadId":"{{threadId}}","message":{{UserMessageJson(text)}}}""");

        (await ReadTextsAsync(threadId)).Should().Equal(["control"], "nothing blank was stored");
        status.Should().Be(HttpStatusCode.BadRequest);
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
    [InlineData("assistant")]
    [InlineData("system")]
    [InlineData("invented")]
    [InlineData(null)]
    [Trait("Bug", "84410")]
    public async Task Append_NonUserRole_IsRefused(string? role)
    {
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
