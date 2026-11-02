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
/// Which model a regenerate runs on inside an AI project (the successor of the agent room in messages.spec.ts): the
/// project's own, whether or not the button names another one.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageRegenerateProjectTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegenerateStream_InAProject_RunsOnTheProjectsModel(bool namesAnotherProfile)
    {
        // Arrange: the portal's Chat binding and the profile the regenerate may name are both elsewhere.
        using var projectProvider = new StubModelProvider("project answer");
        using var otherProvider = new StubModelProvider("other answer");
        var projectModel = await CreateChatProfileOnAsync(projectProvider);
        var other = await CreateChatProfileOnAsync(otherProvider);
        await _aiAssignments.AiAssignmentsAssignAsync(new AiAssignmentsAssignRequest(AiActionType.Chat, other.Id), TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(Owner);
        var projectId = await AgentsTests.AiProjects.CreateAsync(RoomsApi);
        await AgentsTests.AiProjects.SetModelAsync(
            AgentsTests.AiProjects.ClientOf(RoomsApi, FilesHttpClient), projectId, projectModel.Id);
        var entityId = projectId.ToString();

        var thread = await CreateThreadAsync(profileId: projectModel.Id, entityId: entityId);
        var sent = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word OK.", entityId: entityId);
        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());

        var before = await WaitForMessagesAsync(thread.ThreadId, m => Replies(m).Any(r => TextOf(r) == "project answer"));
        var firstReply = Replies(before).Should().ContainSingle().Subject;
        var projectRequests = StreamedRequests(projectProvider);
        var otherRequests = StreamedRequests(otherProvider);

        // Act
        var round = await RegenerateAsync(thread.ThreadId, namesAnotherProfile ? other.Id : null, entityId);

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var after = await WaitForReplacedReplyAsync(thread.ThreadId, firstReply.Id);
        Replies(after).Select(TextOf).Should().ContainSingle().Which.Should().Be("project answer");
        StreamedRequests(projectProvider).Should().Be(projectRequests + 1, projectProvider.Describe());
        StreamedRequests(otherProvider).Should().Be(otherRequests, $"the other model was not asked\n{otherProvider.Describe()}");
        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).ProfileId
            .Should().Be(projectModel.Id, "the regenerate left the conversation on the project's own model");
    }
}
