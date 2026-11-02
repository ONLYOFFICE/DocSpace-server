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
/// <c>ai/send</c> — one AI action, answered as a single JSON message, on the model the action type is assigned to
/// (falling back to Default). Nothing is persisted: no thread, no history.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageOneShotSendTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    // Capability bits of a profile: Chat 0x001.
    private const decimal ChatCapability = 1m;

    [Fact]
    [Trait("Bug", "82833")]
    public async Task Send_Chat_TheNonStreamingPathReachesTheModel()
    {
        using var provider = new StubModelProvider();
        // Chat may only be bound to a profile that declares the Chat capability (the engine refuses "Profile lacks
        // capabilities required for ActionType.Chat" otherwise); a profile without capabilities only serves Default.
        var input = BuildProfileInput();
        input.BaseUrl = provider.BaseUrl;
        input.Capabilities = ChatCapability;
        var created = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        created.Success.Should().BeTrue($"profiles/create rejected the profile: {created.Error}\n{provider.Describe()}");
        var profile = created.Profile;

        // `send` resolves the model through the assignments, so Chat gets a binding of its own.
        var assigned = await _aiAssignments.AiAssignmentsAssignAsync(
            new AiAssignmentsAssignRequest(AiActionType.Chat, profile.Id),
            TestContext.Current.CancellationToken);
        assigned.Success.Should().BeTrue();

        // The same profile answering a streamed round normally — so a failure below is this route, not the portal.
        var thread = await CreateThreadAsync(profileId: profile.Id);
        var control = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word OK.", profile.Id);
        control.StreamError.Should().BeNull(control.ToString());
        control.IncompleteStatus.Should().BeNull(control.ToString());

        AnswerEveryCompletionWith(provider, "OK");
        var question = $"Reply with the single word OK. {Guid.NewGuid():N}";

        var reply = await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage(question)),
            TestContext.Current.CancellationToken);

        // The reply used to be an empty assistant message carrying a gateway auth failure.
        reply.Role.Should().Be(AiThreadMessageLike.RoleEnum.Assistant);
        reply.Status?.Type.Should().NotBe("incomplete", reply.ToJson());
        TextOf(reply).Should().Be("OK", $"the model's answer\n{provider.Describe()}");
        RequestsAsking(provider, question, streamedOnly: false).Should().NotBeEmpty(provider.Describe());
    }

    [Fact]
    [Trait("Bug", "82835")]
    public async Task Send_ActionTypeWithoutItsOwnBinding_ResolvesThroughDefault()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);

        // Vision has no binding of its own on a fresh portal, but the assignments resolve it through Default.
        var resolved = await _aiAssignments.AiAssignmentsResolveForActionAsync("Vision", cancellationToken: TestContext.Current.CancellationToken);
        resolved.ProfileId.Should().Be(profile.Id, "Vision resolves through Default");

        AnswerEveryCompletionWith(provider, "A description");
        var question = $"Describe this. {Guid.NewGuid():N}";

        // `send` used to crash here with 500: it only reached the model for an action type with an explicit binding.
        var reply = await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Vision, BuildUserMessage(question)),
            TestContext.Current.CancellationToken);

        reply.Status?.Type.Should().NotBe("incomplete", reply.ToJson());
        RequestsAsking(provider, question, streamedOnly: false).Should().NotBeEmpty(provider.Describe());
    }

    [Theory]
    [Trait("Bug", "84294")]
    [InlineData("a missing actionType")]
    [InlineData("an unknown actionType")]
    [InlineData("an empty actionType")]
    [InlineData("a missing userMessage")]
    public async Task Send_MalformedRequest_IsAValidationErrorNotACrash(string malformation)
    {
        await CreateProfileAsync();
        var userMessage = BuildUserMessage("Reply with the single word OK.").ToJson();

        // Raw on purpose: an action type outside the enum, an empty one and a missing required field cannot be sent
        // through the typed request.
        var body = malformation switch
        {
            "a missing actionType" => $$"""{"userMessage":{{userMessage}}}""",
            "an unknown actionType" => $$"""{"actionType":"Bogus","userMessage":{{userMessage}}}""",
            "an empty actionType" => $$"""{"actionType":"","userMessage":{{userMessage}}}""",
            _ => """{"actionType":"Chat"}"""
        };

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(SendPath, body, TestContext.Current.CancellationToken);
        var answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // AiSendRequest declares both actionType and userMessage required.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"send with {malformation}: {answer}");

        // `send` has no thread of its own to corrupt, but this rules out the request having created one regardless.
        (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_EmptyUserMessage_IsRefusedLikeSendWithStream(bool withEmptyTextPart)
    {
        await CreateProfileAsync();

        List<AiMessageContentBlock> parts = withEmptyTextPart ? [new AiMessageContentBlock(type: "text", text: "")] : [];
        var empty = new AiThreadMessageLike(role: AiThreadMessageLike.RoleEnum.User, content: new AiMessageContent(parts));

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, empty),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(EmptyUserMessageError);
    }

    [Fact]
    public async Task Send_EntityScope_NonexistentFallsBack_AccessibleIsUsed_InaccessibleIsRefused()
    {
        using var provider = new StubModelProvider();
        await CreateProfileOnAsync(provider);
        AnswerEveryCompletionWith(provider, "OK");
        var roomId = (await CreateRoomAsync()).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var nonexistent = await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage("Reply with the single word OK."), entityId: "019f0000-0000-7000-8000-000000000000"),
            TestContext.Current.CancellationToken);
        nonexistent.Status?.Type.Should().NotBe("incomplete",
            "an entityId that resolves to nothing degrades to the portal-wide assignment rather than failing");

        var accessible = await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage("Reply with the single word OK."), entityId: roomId),
            TestContext.Current.CancellationToken);
        accessible.Status?.Type.Should().NotBe("incomplete", "the caller's own room is a valid scope");

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage("Reply with the single word OK."), entityId: roomId),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403, "an entity that exists but the caller cannot see is refused, not silently ignored");
    }

    [Fact]
    public async Task Send_CreatesNoThread_AndTwoCallsDoNotShareHistory()
    {
        using var provider = new StubModelProvider();
        await CreateProfileOnAsync(provider);
        AnswerEveryCompletionWith(provider, "OK");

        var firstQuestion = $"Remember the code word ORANGE. Reply with just: OK. {Guid.NewGuid():N}";
        var first = await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage(firstQuestion)),
            TestContext.Current.CancellationToken);

        // No thread-side identity comes back — "nothing is persisted, no thread, no storage writes".
        first.Id.Should().BeNull("the response carries no thread-side identity");

        var secondQuestion = $"What code word did I ask you to remember? {Guid.NewGuid():N}";
        await _aiChat.AiSendAsync(
            new AiSendRequest(AiActionType.Chat, BuildUserMessage(secondQuestion)),
            TestContext.Current.CancellationToken);

        RequestsAsking(provider, secondQuestion, streamedOnly: false).Should().ContainSingle(provider.Describe())
            .Which.Should().NotContain("ORANGE", "the second call has no memory of the first — send keeps no history between calls");

        (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should().BeEmpty("neither call created a thread");
    }
}
