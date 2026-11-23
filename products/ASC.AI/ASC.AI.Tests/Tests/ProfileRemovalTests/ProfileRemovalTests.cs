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

namespace ASC.AI.Tests.Tests.ProfileRemovalTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ProfileRemoval")]
public class ProfileRemovalTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private sealed record MemberThread(User Member, Guid ThreadId, ThreadFile Artifact, ThreadFile Attachment);

    [Fact]
    public async Task StartRemove_Member_DeletesTheMemberThreadAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateThreadAsync(projectId, await InviteMemberAsync(projectId));

        await TerminateAsync(thread.Member);
        var job = await RemoveDataAsync(thread.Member);

        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        job.Error.Should().BeNullOrEmpty();
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task StartRemove_Member_TheProjectOwnerIsRefusedTheMemberArtifact()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateThreadAsync(projectId, await InviteMemberAsync(projectId));

        await TerminateAsync(thread.Member);
        await RemoveDataAsync(thread.Member);

        var readAfterJob = await ReadFileAsAsync(Owner, thread.Artifact.Id);
        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        readAfterJob.Should().BeOneOf(403, 404);
        state.ArtifactStatus.Should().Be(404);
    }

    [Fact]
    public async Task DeleteMember_DeletesTheMemberThreadAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateThreadAsync(projectId, await InviteMemberAsync(projectId));

        await TerminateAsync(thread.Member);
        await PeopleHttpClient.Authenticate(Owner);
        await ProfilesApi.DeleteMemberAsync(thread.Member.Id.ToString(), TestContext.Current.CancellationToken);
        var job = await WaitForJobAsync(() => UserDataApi.GetRemoveProgressAsync(thread.Member.Id, TestContext.Current.CancellationToken));

        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task StartRemove_ProjectOwner_PassesTheProjectToTheCallerAndKeepsTheMemberThread()
    {
        var projectOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var projectId = await CreateProjectAsAsync(projectOwner);
        var member = await InviteToProjectAsync(projectId, projectOwner);
        var memberThread = await CreateThreadAsync(projectId, member);
        var spaceWithMember = await UsedSpaceAsAsync(projectOwner, projectId);
        var ownerThread = await CreateThreadAsync(projectId, projectOwner);

        await TerminateAsync(projectOwner);
        var job = await RemoveDataAsync(projectOwner);

        var state = await WaitForCleanupAsync(projectId, ownerThread.Artifact.Id, ownerThread.Attachment.Id, spaceWithMember);
        var project = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;
        var memberRead = await ThreadStatusAsync(member, memberThread.ThreadId);
        var memberArtifacts = await ListArtifactsAsAsync(member, projectId, memberThread.ThreadId);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        project.CreatedBy.Id.Should().Be(Owner.Id);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceWithMember);
        memberRead.Should().Be(HttpStatusCode.OK);
        memberArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(memberThread.Artifact.Id);
    }

    [Fact]
    public async Task StartReassign_WithTheProfileDeleted_TheRecipientIsRefusedTheMemberArtifact()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateThreadAsync(projectId, await InviteMemberAsync(projectId));

        await TerminateAsync(thread.Member);
        var job = await ReassignDataAsync(thread.Member, Owner, deleteProfile: true);

        var readAfterJob = await ReadFileAsAsync(Owner, thread.Artifact.Id);
        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        readAfterJob.Should().BeOneOf(403, 404);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task StartReassign_KeepingTheProfile_TheMemberKeepsTheThreadAndTheProjectOwnerIsRefused()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadAsync(projectId, await InviteMemberAsync(projectId));

        await TerminateAsync(thread.Member);
        var job = await ReassignDataAsync(thread.Member, Owner, deleteProfile: false);
        await ActivateAsync(thread.Member);

        var memberRead = await ThreadStatusAsync(thread.Member, thread.ThreadId);
        var memberArtifact = await ReadFileAsAsync(thread.Member, thread.Artifact.Id);
        var ownerArtifact = await ReadFileAsAsync(Owner, thread.Artifact.Id);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        memberRead.Should().Be(HttpStatusCode.OK);
        memberArtifact.Should().Be(200);
        ownerArtifact.Should().Be(403);
    }

    [Fact]
    public async Task StartReassign_KeepingTheProfile_FromTheProjectOwner_DeletesTheFormerOwnerThread()
    {
        var projectOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var recipient = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var projectId = await CreateProjectAsAsync(projectOwner);
        var spaceBefore = await UsedSpaceAsAsync(projectOwner, projectId);
        var ownerThread = await CreateThreadAsync(projectId, projectOwner);

        await TerminateAsync(projectOwner);
        var job = await ReassignDataAsync(projectOwner, recipient, deleteProfile: false);

        await FilesHttpClient.Authenticate(recipient);
        var state = await WaitForCleanupAsync(projectId, ownerThread.Artifact.Id, ownerThread.Attachment.Id, spaceBefore);
        var project = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;
        await FilesHttpClient.Authenticate(Owner);

        job.Status.Should().Be(DistributedTaskStatus.Completed);
        project.CreatedBy.Id.Should().Be(recipient.Id);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task StartReassign_KeepingTheProfile_FromTheProjectOwner_KeepsTheMemberThread()
    {
        var projectOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var projectId = await CreateProjectAsAsync(projectOwner);
        var member = await InviteToProjectAsync(projectId, projectOwner);
        var memberThread = await CreateThreadAsync(projectId, member);
        var spaceWithMember = await UsedSpaceAsAsync(projectOwner, projectId);
        var ownerThread = await CreateThreadAsync(projectId, projectOwner);

        await TerminateAsync(projectOwner);
        await ReassignDataAsync(projectOwner, Owner, deleteProfile: false);

        var state = await WaitForCleanupAsync(projectId, ownerThread.Artifact.Id, ownerThread.Attachment.Id, spaceWithMember);
        var memberRead = await ThreadStatusAsync(member, memberThread.ThreadId);
        var memberArtifacts = await ListArtifactsAsAsync(member, projectId, memberThread.ThreadId);

        state.ArtifactStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceWithMember);
        memberRead.Should().Be(HttpStatusCode.OK);
        memberArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(memberThread.Artifact.Id);
    }

    [Fact]
    public async Task StartReassign_KeepingTheProfile_FromTheProjectOwner_KeepsTheFormerOwnerThreadInAnotherProject()
    {
        var formerOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var ownedProjectId = await CreateProjectAsAsync(formerOwner);
        var spaceBefore = await UsedSpaceAsAsync(formerOwner, ownedProjectId);
        var ownedThread = await CreateThreadAsync(ownedProjectId, formerOwner);
        var otherProjectId = await CreateProjectAsync();
        await InviteToProjectAsync(otherProjectId, Owner, formerOwner);
        var otherThread = await CreateThreadAsync(otherProjectId, formerOwner);

        await TerminateAsync(formerOwner);
        await ReassignDataAsync(formerOwner, Owner, deleteProfile: false);

        var state = await WaitForCleanupAsync(ownedProjectId, ownedThread.Artifact.Id, ownedThread.Attachment.Id, spaceBefore);
        await ActivateAsync(formerOwner);
        var otherRead = await ThreadStatusAsync(formerOwner, otherThread.ThreadId);
        var otherArtifact = await ReadFileAsAsync(formerOwner, otherThread.Artifact.Id);
        var otherArtifacts = await ListArtifactsAsAsync(formerOwner, otherProjectId, otherThread.ThreadId);

        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        otherRead.Should().Be(HttpStatusCode.OK);
        otherArtifact.Should().Be(200);
        otherArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(otherThread.Artifact.Id);
    }

    private async Task<int> CreateProjectAsAsync(User projectOwner)
    {
        await FilesHttpClient.Authenticate(projectOwner);
        var projectId = await AgentsTests.AiProjects.CreateAsync(RoomsApi);
        await FilesHttpClient.Authenticate(Owner);

        return projectId;
    }

    private async Task<User> InviteToProjectAsync(int projectId, User inviter, User? member = null)
    {
        member ??= await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(inviter);
        await RoomsApi.SetRoomSecurityAsync(
            projectId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(Owner);

        return member;
    }

    private async Task<MemberThread> CreateThreadAsync(int projectId, User author)
    {
        await _aiClient.Authenticate(author);
        await FilesHttpClient.Authenticate(author);

        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var artifact = await UploadArtifactAsync(thread.Id, "report.txt");
        var attachment = await SendAsync(projectId, thread.Id, "notes.txt");

        await _aiClient.Authenticate(Owner);
        await FilesHttpClient.Authenticate(Owner);

        return new MemberThread(author, thread.Id, artifact, attachment);
    }

    private async Task<long> UsedSpaceAsAsync(User reader, int projectId)
    {
        await FilesHttpClient.Authenticate(reader);
        var space = await UsedSpaceAsync(projectId);
        await FilesHttpClient.Authenticate(Owner);

        return space;
    }

    private async Task<int> ReadFileAsAsync(User reader, int fileId)
    {
        await FilesHttpClient.Authenticate(reader);
        var status = await StatusOfAsync(() => FilesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));
        await FilesHttpClient.Authenticate(Owner);

        return status;
    }

    private async Task<HttpStatusCode> ThreadStatusAsync(User author, Guid threadId)
    {
        await _aiClient.Authenticate(author);
        using var response = await _ai.GetAsync($"{ThreadsPath}/{threadId}", TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(Owner);

        return response.StatusCode;
    }

    private async Task<ThreadFilesPage> ListArtifactsAsAsync(User author, int projectId, Guid threadId)
    {
        await FilesHttpClient.Authenticate(author);
        var page = await ListArtifactsAsync(projectId, threadId);
        await FilesHttpClient.Authenticate(Owner);

        return page;
    }

    private Task TerminateAsync(User user)
    {
        return SetStatusAsync(user, EmployeeStatus.Terminated);
    }

    private async Task ActivateAsync(User user)
    {
        await SetStatusAsync(user, EmployeeStatus.Active);

        user.Token = null;
    }

    private async Task SetStatusAsync(User user, EmployeeStatus status)
    {
        await PeopleHttpClient.Authenticate(Owner);
        await UserStatusApi.UpdateUserStatusAsync(
            status,
            new UpdateMembersRequestDto([user.Id], resendAll: false),
            TestContext.Current.CancellationToken);
    }

    private async Task<TaskProgressResponseDto> RemoveDataAsync(User user)
    {
        await PeopleHttpClient.Authenticate(Owner);
        await UserDataApi.StartRemoveAsync(new TerminateRequestDto(user.Id), TestContext.Current.CancellationToken);

        return await WaitForJobAsync(() => UserDataApi.GetRemoveProgressAsync(user.Id, TestContext.Current.CancellationToken));
    }

    private async Task<TaskProgressResponseDto> ReassignDataAsync(User from, User to, bool deleteProfile)
    {
        await PeopleHttpClient.Authenticate(Owner);
        await UserDataApi.StartReassignAsync(new StartReassignRequestDto(from.Id, to.Id, deleteProfile), TestContext.Current.CancellationToken);

        return await WaitForJobAsync(() => UserDataApi.GetReassignProgressAsync(from.Id, TestContext.Current.CancellationToken));
    }

    private static async Task<TaskProgressResponseDto> WaitForJobAsync(Func<Task<TaskProgressResponseWrapper>> progress)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var state = (await progress()).Response;

            if (state is { IsCompleted: true } || DateTime.UtcNow >= deadline)
            {
                state.Should().NotBeNull();

                return state;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
