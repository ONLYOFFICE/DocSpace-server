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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// <c>POST threads/regenerate-title</c>: the model is asked for a title built from the thread's first message, and the
/// thread is renamed to it. The title request is not streamed, so the stand-in model answers it with
/// <see cref="StubModelProvider.DefaultTitle"/>.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatRegenerateTitleTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    /// <summary>
    /// The route answered 500 with the full profile object the SDK types ask for, on a thread the model had really
    /// answered — so the failure could not be blamed on there being nothing to build a title from.
    /// </summary>
    [Fact]
    [Trait("Bug", "82828")]
    public async Task RegenerateTitle_ThreadWithAConversation_RenamesTheThread()
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Title me", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("Masses attract each other."));

        var sent = await SendWithStreamAsync(thread.ThreadId, "Explain gravity in one sentence.", profile.Id);
        sent.StreamError.Should().BeNull(sent.ToString());
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(thread.ThreadId), "Masses attract each other.");

        // Act
        var result = await _aiThreads.AiThreadsRegenerateTitleAsync(
            new AiThreadsRegenerateTitleRequest(thread.ThreadId, profile),
            TestContext.Current.CancellationToken);

        // Assert
        result.Title.Should().Be(StubModelProvider.DefaultTitle, Provider.Describe());
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be(result.Title, "the generated title is stored on the thread");
    }

    [Fact]
    public async Task RegenerateTitle_ProfileIdInsteadOfTheProfileObject_Returns400()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Title me", profile.Id);

        // Act
        // Raw on purpose: profile is a required constructor parameter of AiThreadsRegenerateTitleRequest, so the SDK
        // never sends the profileId a caller would try instead.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/threads/regenerate-title",
            JsonSerializer.Serialize(new { threadId = thread.ThreadId, profileId = profile.Id }),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, content);
        content.Should().Contain("threadId (string) and profile (object) are required");
        (await GetThreadAsync(thread.ThreadId)).Title.Should().Be("Title me", "nothing was half-applied");
    }
}
