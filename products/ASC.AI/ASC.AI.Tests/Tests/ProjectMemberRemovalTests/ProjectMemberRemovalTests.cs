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

namespace ASC.AI.Tests.Tests.ProjectMemberRemovalTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ProjectMemberRemoval")]
public class ProjectMemberRemovalTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private sealed record MemberThread(User Member, Guid ThreadId, ThreadFile Artifact, ThreadFile Attachment);

    [Fact]
    public async Task RemoveMember_DeletesTheMemberThreadsAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);
        var readBefore = await ThreadStatusAsync(member, thread.ThreadId);

        await SetMemberAccessAsync(projectId, member, FileShare.None);

        var readAfter = await ThreadStatusAsync(member, thread.ThreadId);
        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        readBefore.Should().Be(HttpStatusCode.OK);
        readAfter.Should().Be(HttpStatusCode.NotFound);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task LeaveProject_DeletesTheMemberThreads()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);

        await SetMemberAccessAsync(projectId, member, FileShare.None, actor: member);

        var readAfter = await ThreadStatusAsync(member, thread.ThreadId);
        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        readAfter.Should().Be(HttpStatusCode.NotFound);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task RemoveDeactivatedMember_DeletesTheMemberThreads()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);

        await PeopleHttpClient.Authenticate(Owner);
        await UserStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([member.Id], resendAll: false),
            TestContext.Current.CancellationToken);
        await SetMemberAccessAsync(projectId, member, FileShare.None);

        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task RemoveMember_KeepsTheThreadsOfTheOtherMembers()
    {
        var projectId = await CreateProjectAsync();
        var kept = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));
        var spaceAfterKept = await UsedSpaceAsync(projectId);
        var removed = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));

        await SetMemberAccessAsync(projectId, removed.Member, FileShare.None);

        var state = await WaitForCleanupAsync(projectId, removed.Artifact.Id, removed.Attachment.Id, spaceAfterKept);
        var keptRead = await ThreadStatusAsync(kept.Member, kept.ThreadId);
        var keptArtifacts = await ListArtifactsAsAsync(kept.Member, projectId, kept.ThreadId);

        state.ArtifactStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceAfterKept);
        keptRead.Should().Be(HttpStatusCode.OK);
        keptArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(kept.Artifact.Id);
    }

    [Fact]
    public async Task RemoveMember_ThenInviteAgain_TheOldThreadStaysDeleted()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);

        await SetMemberAccessAsync(projectId, member, FileShare.None);
        await SetMemberAccessAsync(projectId, member, FileShare.ContentCreator);

        var read = await ThreadStatusAsync(member, thread.ThreadId);
        await FilesHttpClient.Authenticate(member);
        using var artifacts = await GetArtifactsAsync(projectId, thread.ThreadId);
        await FilesHttpClient.Authenticate(Owner);
        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);

        read.Should().Be(HttpStatusCode.NotFound);
        artifacts.StatusCode.Should().Be(HttpStatusCode.NotFound);
        state.ArtifactStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task RemoveFromList_ByTheOwner_DeletesTheThreadsOfEveryMember()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var first = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));
        var second = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));

        await SharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FolderIds = [new(projectId)] }, TestContext.Current.CancellationToken);

        var firstState = await WaitForCleanupAsync(projectId, first.Artifact.Id, first.Attachment.Id, spaceBefore);
        var secondState = await WaitForCleanupAsync(projectId, second.Artifact.Id, second.Attachment.Id, spaceBefore);
        var firstRead = await ThreadStatusAsync(first.Member, first.ThreadId);
        var secondRead = await ThreadStatusAsync(second.Member, second.ThreadId);

        firstState.ArtifactStatus.Should().Be(404);
        firstState.AttachmentStatus.Should().Be(404);
        secondState.ArtifactStatus.Should().Be(404);
        secondState.AttachmentStatus.Should().Be(404);
        secondState.UsedSpace.Should().Be(spaceBefore);
        firstRead.Should().Be(HttpStatusCode.NotFound);
        secondRead.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveFromList_ByAMember_KeepsTheMembershipAndTheThreads()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);

        await FilesHttpClient.Authenticate(member);
        await SharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FolderIds = [new(projectId)] }, TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(Owner);

        var read = await ThreadStatusAsync(member, thread.ThreadId);
        var artifacts = await ListArtifactsAsAsync(member, projectId, thread.ThreadId);
        var members = await MemberIdsAsync(projectId);

        read.Should().Be(HttpStatusCode.OK);
        artifacts.Files.Should().ContainSingle().Which.Id.Should().Be(thread.Artifact.Id);
        members.Should().Contain(member.Id);
    }

    [Fact]
    public async Task ChangeOwner_KeepsTheThreadsOfTheFormerAndTheNewOwner()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var formerOwnerThread = await CreateMemberThreadAsync(projectId, Owner);
        var newOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await SetMemberAccessAsync(projectId, newOwner, FileShare.ContentCreator);
        var newOwnerThread = await CreateMemberThreadAsync(projectId, newOwner);
        var spaceWithOwners = await UsedSpaceAsync(projectId);
        var control = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));

        await SharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(projectId)], userId: newOwner.Id),
            TestContext.Current.CancellationToken);
        await SetMemberAccessAsync(projectId, control.Member, FileShare.None, actor: newOwner);

        var controlState = await WaitForCleanupAsync(projectId, control.Artifact.Id, control.Attachment.Id, spaceWithOwners);
        var formerRead = await ThreadStatusAsync(Owner, formerOwnerThread.ThreadId);
        var newRead = await ThreadStatusAsync(newOwner, newOwnerThread.ThreadId);
        var newArtifacts = await ListArtifactsAsAsync(newOwner, projectId, newOwnerThread.ThreadId);

        spaceWithOwners.Should().BeGreaterThan(spaceBefore);
        controlState.ArtifactStatus.Should().Be(404);
        controlState.UsedSpace.Should().Be(spaceWithOwners);
        formerRead.Should().Be(HttpStatusCode.OK);
        newRead.Should().Be(HttpStatusCode.OK);
        newArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(newOwnerThread.Artifact.Id);
    }

    private async Task<MemberThread> CreateMemberThreadAsync(int projectId, User member)
    {
        await _aiClient.Authenticate(member);
        await FilesHttpClient.Authenticate(member);

        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var artifact = await UploadArtifactAsync(thread.Id, "report.txt");
        var attachment = await SendAsync(projectId, thread.Id, "notes.txt");

        await _aiClient.Authenticate(Owner);
        await FilesHttpClient.Authenticate(Owner);

        return new MemberThread(member, thread.Id, artifact, attachment);
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

    private async Task SetMemberAccessAsync(int projectId, User member, FileShare access, User? actor = null)
    {
        await FilesHttpClient.Authenticate(actor ?? Owner);
        await RoomsApi.SetRoomSecurityAsync(
            projectId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, access)], notify: false),
            TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(Owner);
    }

    private async Task<List<Guid?>> MemberIdsAsync(int projectId)
    {
        var shares = (await RoomsApi.GetRoomSecurityInfoAsync(projectId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.Select(s => s.SharedToUser?.Id).ToList();
    }
}
