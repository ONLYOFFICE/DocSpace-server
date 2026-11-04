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
/// <c>POST ai/send-with-stream</c>: the question is stored verbatim, the reply is stored, and a thread's own history —
/// and only its own — is what the model is sent on the next turn. The model is scripted, so "the model remembered"
/// becomes "the server sent it the history to remember from".
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatSendTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    private const string Question = "What is 2+2? Answer in one word.";
    private const string Teach = "Remember the code word ORANGE. Reply with just: OK.";
    private const string Recall = "What code word did I ask you to remember? Reply with just that word.";
    private const string CodeWord = "ORANGE";

    [Fact]
    public async Task SendWithStream_Owner_StoresTheQuestionAndTheReply()
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest thread", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("Four"));

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, Question, profile.Id);

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        UserMessages(messages).Select(TextOf).Should().Equal(new[] { Question }, "the question is stored verbatim, once");
        AssertHealthyReplies(messages, "Four");
    }

    [Fact]
    public async Task SendWithStream_SecondTurn_SendsTheThreadHistoryToTheModel()
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest thread", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("OK, noted."), StubModelProvider.Text(CodeWord));

        var first = await SendWithStreamAsync(thread.ThreadId, Teach, profile.Id);
        first.StreamError.Should().BeNull(first.ToString());
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(thread.ThreadId), "OK, noted.");

        // Act
        var second = await SendWithStreamAsync(thread.ThreadId, Recall, profile.Id);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());
        second.StreamError.Should().BeNull(second.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId, 2);
        UserMessages(messages).Should().HaveCount(2);
        AssertHealthyReplies(messages, "OK, noted.", CodeWord);

        // The second question can only be answered from the first turn: both halves of it reach the model.
        var recallRequest = ChatRequestsCarrying(Recall);
        recallRequest.Should().ContainSingle(Provider.Describe());
        recallRequest[0].Should().Contain(Teach, "the first question is part of the history sent with the second")
            .And.Contain("OK, noted.", "and so is the first answer");
    }

    [Fact]
    public async Task SendWithStream_SiblingThread_DoesNotInheritTheOtherThreadsHistory()
    {
        // Arrange
        const string askSibling =
            "What code word did I ask you to remember earlier in this conversation? If no code word was ever mentioned here, reply with just: NONE.";

        var profile = await GetOwnProfileAsync();
        var first = await CreateThreadAsync("Autotest thread that knows the word", profile.Id);
        var second = await CreateThreadAsync("Autotest thread that must not", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("OK"), StubModelProvider.Text("NONE"), StubModelProvider.Text(CodeWord));

        var taught = await SendWithStreamAsync(first.ThreadId, Teach, profile.Id);
        taught.StreamError.Should().BeNull(taught.ToString());
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(first.ThreadId), "OK");

        // Act
        var asked = await SendWithStreamAsync(second.ThreadId, askSibling, profile.Id);
        var reasked = await SendWithStreamAsync(first.ThreadId, Recall, profile.Id);

        // Assert
        asked.StreamError.Should().BeNull(asked.ToString());
        reasked.StreamError.Should().BeNull(reasked.ToString());

        var siblingRequest = ChatRequestsCarrying(askSibling);
        siblingRequest.Should().ContainSingle(Provider.Describe());
        siblingRequest[0].Should().NotContain(CodeWord, "the sibling thread's request carries none of the other thread's history");

        var secondThread = await WaitForAssistantRepliesAsync(second.ThreadId);
        AssertHealthyReplies(secondThread, "NONE");
        UserMessages(secondThread).Select(TextOf).Should().Equal(askSibling);
        secondThread.Select(TextOf).Should().NotContain(t => t != null && t.Contains(CodeWord), "the sibling thread holds nothing of the other one");

        // The first thread still carries its own history — otherwise the miss above proves nothing.
        var recallRequest = ChatRequestsCarrying(Recall);
        recallRequest.Should().ContainSingle(Provider.Describe());
        recallRequest[0].Should().Contain(Teach).And.NotContain(askSibling);

        var firstThread = await WaitForAssistantRepliesAsync(first.ThreadId, 2);
        AssertHealthyReplies(firstThread, "OK", CodeWord);
        UserMessages(firstThread).Select(TextOf).Should().Equal(new[] { Teach, Recall }, "the first thread's transcript stayed its own");
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SendWithStream_Member_GetsAReply(EmployeeType employeeType)
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        var member = await InviteContact(employeeType, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        var thread = await CreateThreadAsync("Member thread", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("Four"));

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, Question, profile.Id);

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        UserMessages(messages).Select(TextOf).Should().Equal(new[] { Question });
        AssertHealthyReplies(messages, "Four");
    }
}
