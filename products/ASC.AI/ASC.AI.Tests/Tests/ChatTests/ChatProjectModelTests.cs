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
/// The model of an AI project, the successor of "the model of an agent room" in chat.spec.ts: the project's own model
/// (else the portal's Chat binding) answers every round that names the project, whatever the composer sends, and the
/// only way to change it is <c>PUT files/rooms/{id}/model</c>. The TypeScript suite ran this against an agent with a
/// fixed profile; the agent routes are gone and the AI project took over that role.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatProjectModelTests(AspireAppFixture fixture) : ChatProjectTestBase(fixture)
{
    [Fact]
    public async Task Send_ProjectWithItsOwnModel_RunsOnItAndNotOnThePortalBinding()
    {
        // Arrange: the portal-wide Chat binding points somewhere else on purpose, so "the project's model answered"
        // cannot be "it fell back to the portal's".
        var (ownProvider, own) = await NewChatModelAsync();
        var (portalProvider, portal) = await NewChatModelAsync();
        await AssignAsync(AiActionType.Chat, portal.Id);

        var projectId = await CreateProjectOnAsync(own);
        var entityId = projectId.ToString();
        var question = NewQuestion();

        // A composer with no picker creates the thread and sends with no profileId at all.
        var thread = await CreateProjectThreadAsync(projectId);

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: entityId);

        // Assert
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(ownProvider, question).Should().NotBeEmpty($"the project's own model answered\n{ownProvider.Describe()}");
        StreamedRequestsCarrying(portalProvider, question).Should().BeEmpty($"not the portal's\n{portalProvider.Describe()}");
        (await StoredModelOfAsync(thread.ThreadId)).Should().Be(own.Id, "the conversation ended up on the project's model");
        (await ProjectModelAsync(projectId)).Should().Be(own.Id);
    }

    [Fact]
    public async Task Send_ProjectWithNoModelOfItsOwn_RunsOnThePortalChatBinding()
    {
        // Arrange
        var (portalProvider, portal) = await NewChatModelAsync();
        await AssignAsync(AiActionType.Chat, portal.Id);

        var projectId = await CreateProjectAsync();
        var question = NewQuestion();
        var thread = await CreateProjectThreadAsync(projectId);

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: projectId.ToString());

        // Assert
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(portalProvider, question).Should().NotBeEmpty($"the portal's Chat model answered\n{portalProvider.Describe()}");
        (await ProjectModelAsync(projectId)).Should().BeNull("the project never had a model of its own, and the round did not give it one");
    }

    [Fact]
    public async Task SetModel_AnotherProfile_MovesTheProjectAndLeavesExistingThreadsAlone()
    {
        // Arrange: the only way to change the fixed model, and the counterpart of "rebinding Chat does not move an
        // existing thread" for the project scope.
        var (firstProvider, first) = await NewChatModelAsync();
        var (secondProvider, second) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(first);
        var entityId = projectId.ToString();
        var before = await CreateProjectThreadAsync(projectId, first.Id);

        // Act
        await SetProjectModelAsync(projectId, second);

        // Assert
        (await ProjectModelAsync(projectId)).Should().Be(second.Id, "the project carries the new model");
        (await StoredModelOfAsync(before.ThreadId)).Should().Be(first.Id, "a conversation started before the switch keeps the model it ran on");

        // A new thread, sent the way a picker-less client sends, is answered by the new model.
        var after = await CreateProjectThreadAsync(projectId);
        var question = NewQuestion();
        var round = await SendWithStreamAsync(after.ThreadId, question, entityId: entityId);

        round.ShouldBeHealthy();
        StreamedRequestsCarrying(secondProvider, question).Should().NotBeEmpty($"the new model answered\n{secondProvider.Describe()}");
        StreamedRequestsCarrying(firstProvider, question).Should().BeEmpty($"the old one was not asked\n{firstProvider.Describe()}");
        (await StoredModelOfAsync(before.ThreadId)).Should().Be(first.Id, "and the older thread is still untouched");
    }

    /// <summary>
    /// Product hole: the chat context drops a project the caller cannot "use AI" in (<c>FileSecurity.CanUseAi</c> needs
    /// RoomManager or ContentCreator, while a project member holds Read), so a Read member's round is not bound to the
    /// project and runs on whatever the composer sent or the portal binding names.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetModel_ProjectMember_ReadsTheFixedModelAndChatsOnIt()
    {
        // Arrange: where a composer with a hidden picker gets the name of the fixed model from, for a member as well.
        var (provider, model) = await NewChatModelAsync();
        var (otherProvider, other) = await NewChatModelAsync();
        await AssignAsync(AiActionType.Chat, other.Id);

        var projectId = await CreateProjectOnAsync(model);
        var member = await InviteToProjectAsync(projectId, EmployeeType.User);

        // Act
        var seenByMember = await ProjectModelAsync(projectId, member);

        await _aiChatClient.Authenticate(member);
        var catalogue = await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken);
        var thread = await CreateProjectThreadAsync(projectId);
        var question = NewQuestion();
        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: projectId.ToString());

        // Assert
        seenByMember.Should().Be(model.Id, "a member reads the model the project is fixed on");
        catalogue.Select(p => p.Id).Should().Contain(new[] { model.Id, other.Id }, "the catalogue is served whole inside a project, the other model included");
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(provider, question).Should().NotBeEmpty($"the member's round ran on the project's model\n{provider.Describe()}");
        StreamedRequestsCarrying(otherProvider, question).Should().BeEmpty($"not on the portal's\n{otherProvider.Describe()}");
    }

    [Fact]
    public async Task OpenOrCreate_ProjectThreadWithAnotherProfile_DoesNotStampIt()
    {
        // Arrange
        var (_, first) = await NewChatModelAsync();
        var (_, second) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(first);
        var entityId = projectId.ToString();
        var thread = await CreateProjectThreadAsync(projectId);

        // Act: the open call carries a whole profile object — the client's default pick, not a write.
        var opened = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                threadId: thread.ThreadId,
                profile: second,
                profileId: second.Id,
                firstMessage: BuildUserMessage("hello"),
                entityId: entityId),
            TestContext.Current.CancellationToken);

        // Assert
        opened.ThreadId.Should().Be(thread.ThreadId, "the existing thread is reused");
        (await StoredModelOfAsync(thread.ThreadId)).Should().NotBe(second.Id, "the open did not overwrite the thread with the profile it carried");

        var round = await SendWithStreamAsync(thread.ThreadId, NewQuestion(), entityId: entityId);
        round.ShouldBeHealthy();
        (await StoredModelOfAsync(thread.ThreadId)).Should().Be(first.Id, "the project's own model is what the conversation ended up on");
    }

    [Fact]
    public async Task Send_BlankProfileIdInProject_DoesNotLetThePortalModelIn()
    {
        // Arrange
        var (firstProvider, first) = await NewChatModelAsync();
        var (secondProvider, second) = await NewChatModelAsync();
        await AssignAsync(AiActionType.Chat, second.Id);

        var projectId = await CreateProjectOnAsync(first);
        var entityId = projectId.ToString();
        var thread = await CreateProjectThreadAsync(projectId);
        var question = NewQuestion();

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, question, profileId: "", entityId: entityId);

        // Assert
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(firstProvider, question).Should().NotBeEmpty($"the project decided\n{firstProvider.Describe()}");
        StreamedRequestsCarrying(secondProvider, question).Should().BeEmpty($"not the portal\n{secondProvider.Describe()}");

        var stored = await StoredModelOfAsync(thread.ThreadId);
        stored.Should().NotBe(second.Id, "a blank pick leaves the project deciding, not the portal");
        (await _aiThreads.AiThreadsListAsync(entityId, cancellationToken: TestContext.Current.CancellationToken)).Single(t => t.ThreadId == thread.ThreadId).ProfileId.Should().Be(stored, "and the list agrees with get-by-id");
    }

    [Fact]
    public async Task Send_ProfileIdThatNamesNoModelInProject_IsIgnoredAndTheProjectsModelAnswers()
    {
        // Arrange: outside a project an unknown profileId is a 400 (Bug 83160, ChatThreadModelRulesTests). Inside one
        // the project's model replaces whatever the composer sent before that check, so the choice cannot be
        // unresolvable — and the thread stays on the project's model.
        var (provider, first) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(first);
        var thread = await CreateProjectThreadAsync(projectId, first.Id);
        var question = NewQuestion();

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, question, profileId: Guid.NewGuid().ToString(), entityId: projectId.ToString());

        // Assert
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(provider, question).Should().NotBeEmpty($"the project's model answered\n{provider.Describe()}");
        (await StoredModelOfAsync(thread.ThreadId)).Should().Be(first.Id, "the thread stays on the project's model");
    }

    /// <summary>
    /// The agent factory took an image profile "without a word" and the agent could not chat at all (82926 on create,
    /// 82927 on update). A project has no model at creation: <c>PUT files/rooms/{id}/model</c> is the one way to give
    /// it one, and it has to refuse a profile that cannot hold a conversation.
    /// </summary>
    [Fact]
    [Trait("Bug", "82927")]
    public async Task SetModel_ImageProfileOnAWorkingProject_IsRefusedAndTheProjectKeepsItsModel()
    {
        // Arrange
        var (_, text) = await NewChatModelAsync();
        var image = await CreateCapableProfileAsync(NewProvider(), ImageOnlyCapabilities);
        var projectId = await CreateProjectOnAsync(text);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SetProjectModelAsync(projectId, image));

        // Assert
        exception.ErrorCode.Should().Be(400, "a profile that cannot chat is not a model for a project");
        (await ProjectModelAsync(projectId)).Should().Be(text.Id, "a working project is not moved onto a model that cannot hold a conversation");
    }

    [Fact]
    [Trait("Bug", "82926")]
    public async Task SetModel_ImageProfileOnAFreshProject_IsRefusedAndTheProjectStaysWithoutModel()
    {
        // Arrange
        var image = await CreateCapableProfileAsync(NewProvider(), ImageOnlyCapabilities);
        var projectId = await CreateProjectAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SetProjectModelAsync(projectId, image));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ProjectModelAsync(projectId)).Should().BeNull("no project is built on a model that cannot hold a conversation");
    }

    /// <summary>
    /// The project is bound to profile A and the conversation starts on it; the send then names profile B. The model a
    /// project runs on is not the caller's to change, so B has to be refused or ignored — it must not be written onto
    /// the thread and used by every following turn.
    /// </summary>
    [Theory]
    [Trait("Bug", "82914")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_OtherProfileInProject_CannotOverrideTheFixedModel(bool asMember)
    {
        // Arrange
        var (firstProvider, first) = await NewChatModelAsync();
        var (secondProvider, second) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(first);
        var entityId = projectId.ToString();

        if (asMember)
        {
            var member = await InviteToProjectAsync(projectId, EmployeeType.User);
            await _aiChatClient.Authenticate(member);
        }

        var thread = await CreateProjectThreadAsync(projectId, first.Id);
        var question = NewQuestion();

        // Act
        var round = await SendWithStreamAsync(thread.ThreadId, question, profileId: second.Id, entityId: entityId);

        // Assert
        round.ShouldBeHealthy();
        StreamedRequestsCarrying(firstProvider, question).Should().NotBeEmpty($"the project's model answered\n{firstProvider.Describe()}");
        StreamedRequestsCarrying(secondProvider, question).Should().BeEmpty($"the model named in the send was not asked\n{secondProvider.Describe()}");
        (await StoredModelOfAsync(thread.ThreadId)).Should().NotBe(second.Id, "the conversation does not move onto a model the project was not built on");
    }

    /// <summary>
    /// The same hole one step earlier: the override does not need a send, it can be baked in when the conversation is
    /// created. A thread in a project has no model of its own to choose — the project's is the model — so the create
    /// either refuses profile B or stores the project's A.
    /// </summary>
    [Theory]
    [Trait("Bug", "82915")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_ThreadInProjectOnAnotherModel_DoesNotStoreIt(bool asMember)
    {
        // Arrange
        var (_, first) = await NewChatModelAsync();
        var (_, second) = await NewChatModelAsync();
        var projectId = await CreateProjectOnAsync(first);

        if (asMember)
        {
            var member = await InviteToProjectAsync(projectId, EmployeeType.User);
            await _aiChatClient.Authenticate(member);
        }

        // Act: a refusal leaves no thread to read, and that is one of the two shapes a fix can take, so the read is
        // conditional rather than asserted.
        string? stored = null;

        try
        {
            var created = await CreateProjectThreadAsync(projectId, second.Id);
            stored = await StoredModelOfAsync(created.ThreadId);
        }
        catch (ApiException exception) when (exception.ErrorCode is >= 400 and < 500)
        {
            // refused: nothing stored
        }

        // Assert
        stored.Should().NotBe(second.Id, "a thread in a project cannot be created on another model");
    }
}
