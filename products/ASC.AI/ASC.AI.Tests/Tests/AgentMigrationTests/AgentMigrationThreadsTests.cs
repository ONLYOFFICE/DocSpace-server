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


namespace ASC.AI.Tests.Tests.AgentMigrationTests;

/// <summary>
/// Requirements "Threads of an agent become threads of the project" and "The model of an agent becomes the model of
/// the project".
/// </summary>
/// <remarks>
/// A thread or a Chat binding that names the agent as its entity has no public route to be created through: the
/// chat service stores every thread without an entity unless the entity is an AI project
/// (<c>threadsStorage.ts</c>), and its assign route takes no entity. They are arranged through the internal storage
/// API of ASC.AI, which is where such rows of an agent come from; what the conversion made of them is read through
/// the public routes.
/// </remarks>
[Trait("Category", "Migration")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationThreadsTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    [Fact]
    public async Task Convert_ThreadOfAMemberInTheAgent_FollowsTheAgentIntoTheProject()
    {
        // Arrange
        var title = NewTitle("Chat agent");
        var agent = await CreateAgentAsync(title);
        var member = await InviteContentCreatorAsync(agent.Id);

        await _aiClient.Authenticate(member);
        var thread = await CreateThreadInternalAsync("Autotest agent thread", entityId: agent.Id.ToString());

        // The messages go through the public route: a message stored by the internal one carries no role, and
        // the public read of the thread cannot be parsed by the SDK then.
        await _aiChatClient.Authenticate(member);
        var first = await AppendUserMessageAsync(thread.Id.ToString());
        var second = await AppendUserMessageAsync(thread.Id.ToString());

        (await ReadThreadInternalAsync(thread.Id)).EntityId.Should().Be(agent.Id.ToString(), "the thread belongs to the agent before the conversion");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        await _aiChatClient.Authenticate(member);
        (await ListedThreadIdsAsync(project.Id.ToString())).Should().Contain(thread.Id, "the thread is listed for the project");
        (await ListedThreadIdsAsync(null)).Should().NotContain(thread.Id, "a thread of a project is not listed without an entity");

        var messages = await _aiThreads.AiThreadsReadMessagesAsync(thread.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken);
        messages.Select(message => message.Id).Should().BeEquivalentTo(new[] { first, second });
    }

    [Fact]
    public async Task Convert_ThreadStartedWithoutAnEntity_StaysWhereItWas()
    {
        // Arrange
        var title = NewTitle("Chat agent");
        var agent = await CreateAgentAsync(title);
        var member = await InviteContentCreatorAsync(agent.Id);

        await _aiClient.Authenticate(member);
        var agentThread = await CreateThreadInternalAsync("Autotest agent thread", entityId: agent.Id.ToString());
        var freeThread = await CreateThreadInternalAsync("Autotest free thread");

        // Act
        await ConvertAgentsAsync();

        // Assert - the agent's thread moved, so the list of the project is not empty for another reason
        var project = await ProjectOfAsync(title);

        await _aiChatClient.Authenticate(member);
        var ofProject = await ListedThreadIdsAsync(project.Id.ToString());
        ofProject.Should().Contain(agentThread.Id);
        ofProject.Should().NotContain(freeThread.Id);
        (await ListedThreadIdsAsync(null)).Should().Contain(freeThread.Id);
    }

    [Fact]
    public async Task Convert_AgentBoundToAProfile_GivesTheProjectThatModel()
    {
        // Arrange - the agent runs on profile A while the portal's Chat binding is B
        var title = NewTitle("Model agent");
        var agent = await CreateAgentAsync(title);

        await _aiChatClient.Authenticate(Owner);
        var profileA = Guid.Parse((await CreateProfileAsync($"autotest-a-{Guid.NewGuid():N}")).Id);
        var profileB = Guid.Parse((await CreateProfileAsync($"autotest-b-{Guid.NewGuid():N}")).Id);

        await _aiClient.Authenticate(Owner);
        await CreateAssignmentInternalAsync("Chat", profileB);
        await CreateAssignmentInternalAsync("Chat", profileA, agent.Id.ToString());

        (await ReadAssignmentInternalAsync("Chat", agent.Id.ToString())).Should().Be(profileA);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        (await ProjectModelAsync(project.Id)).Should().Be(profileA);
        (await ReadAssignmentInternalAsync("Chat")).Should().Be(profileB, "the portal's own binding is not touched");
    }

    [Fact]
    public async Task Convert_AgentWithoutAnAssignment_LeavesTheProjectWithoutAModel()
    {
        // Arrange - the portal has a Chat binding, the agent has none of its own
        var title = NewTitle("Model agent");
        await CreateAgentAsync(title);

        await _aiChatClient.Authenticate(Owner);
        var profile = Guid.Parse((await CreateProfileAsync($"autotest-{Guid.NewGuid():N}")).Id);

        await _aiClient.Authenticate(Owner);
        await CreateAssignmentInternalAsync("Chat", profile);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        (await ProjectModelAsync(project.Id)).Should().BeNull();
    }

    /// <summary>
    /// Scenario "Thread of a Guest": a Guest is refused on every thread route by its account type
    /// (<c>ThreadStorageService</c>), before the thread is looked at, so the thread stays stored in the project and
    /// out of the Guest's reach.
    /// </summary>
    [Fact]
    public async Task Convert_ThreadOfAGuestOfTheAgent_StaysStoredAndAnswers403ToTheGuest()
    {
        // Arrange - a Guest cannot start a thread, so the thread is started by a User who is made a Guest afterwards
        var title = NewTitle("Guest agent");
        var agent = await CreateAgentAsync(title);
        var member = await InviteContentCreatorAsync(agent.Id);

        await _aiClient.Authenticate(member);
        var thread = await CreateThreadInternalAsync("Autotest guest thread", entityId: agent.Id.ToString());

        await MakeGuestAsync(member);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        Lists(await RoomSharesAsync(project.Id), member.Id).Should().BeFalse("a Guest is not carried over to the project");
        (await StoredEntryIdAsync(thread.Id)).Should().Be(project.Id, "the thread is stored as a thread of the project");

        // the token of the account was issued while it was a User: sign in again as the Guest it is now
        await _aiClient.Authenticate(member with { Token = null });
        using var response = await _ai.GetAsync($"{ThreadsPath}/{thread.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>A User with the lowest role of an agent that may chat in it: a Viewer cannot start a thread.</summary>
    private async Task<User> InviteContentCreatorAsync(int agentId)
    {
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await ShareRoomAsync(agentId, member.Id, FileShare.ContentCreator);

        return member;
    }

    /// <summary>The ids <c>aiThreadsList</c> answers to whoever the chat client is signed in as.</summary>
    private async Task<List<Guid>> ListedThreadIdsAsync(string? entityId)
    {
        // The generated parameter is not annotated for nullability; null leaves the entity out of the query.
        var threads = await _aiThreads.AiThreadsListAsync(entityId: entityId!, count: DefaultPageCount, cancellationToken: TestContext.Current.CancellationToken);

        return [.. threads.Select(thread => Guid.Parse(thread.ThreadId))];
    }

    private async Task<Guid?> ProjectModelAsync(int projectId)
    {
        await FilesHttpClient.Authenticate(Owner);

        return (await RoomsAiApi.GetProjectModelAsync(projectId, TestContext.Current.CancellationToken)).Response.ProfileId;
    }

    private async Task MakeGuestAsync(User user)
    {
        await PeopleHttpClient.Authenticate(Owner);
        await UserTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id, Owner.Id),
            TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var progress = (await UserTypeApi.GetUserTypeUpdateProgressAsync(user.Id, TestContext.Current.CancellationToken)).Response;

            if (progress.IsCompleted || DateTime.UtcNow >= deadline)
            {
                progress.IsCompleted.Should().BeTrue("the account type change finishes");
                progress.Error.Should().BeNullOrEmpty();

                return;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// The entity a thread is stored under. Read from the database: after the conversion nobody who may call a
    /// thread route is the creator of this thread, so no route shows it.
    /// </summary>
    private async Task<int?> StoredEntryIdAsync(Guid threadId)
    {
        await using var db = await CreateDbContextAsync<AiIntegrationContext>();

        return await db.Threads
            .Where(thread => thread.Id == threadId)
            .Select(thread => thread.EntryId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
    }
}
