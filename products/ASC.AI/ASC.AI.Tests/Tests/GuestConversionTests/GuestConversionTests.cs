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

namespace ASC.AI.Tests.Tests.GuestConversionTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/GuestConversion")]
public class GuestConversionTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private sealed record MemberThread(User Member, Guid ThreadId, ThreadFile Artifact, ThreadFile Attachment);

    [Fact]
    public async Task ConvertMemberToGuest_RemovesTheMemberAndDeletesTheThreadFiles()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);
        var membersBefore = await MemberIdsAsync(projectId);

        var progress = await ConvertAsync(member, EmployeeType.Guest);

        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);
        var membersAfter = await MemberIdsAsync(projectId);

        progress.Error.Should().BeNullOrEmpty();
        progress.IsCompleted.Should().BeTrue();
        membersBefore.Should().Contain(member.Id);
        membersAfter.Should().NotContain(member.Id);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task ConvertMemberToGuest_ClosesTheThreadWhenTheJobCompletes()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, member);
        var readBefore = await ThreadStatusAsync(member, thread.ThreadId);

        var toGuest = await ConvertAsync(member, EmployeeType.Guest);
        var toUser = await ConvertAsync(member, EmployeeType.User);

        var readAfter = await ThreadStatusAsync(member, thread.ThreadId);

        toGuest.Error.Should().BeNullOrEmpty();
        toUser.Error.Should().BeNullOrEmpty();
        readBefore.Should().Be(HttpStatusCode.OK);
        readAfter.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConvertMemberToGuest_KeepsTheThreadsOfTheOtherMembers()
    {
        var projectId = await CreateProjectAsync();
        var kept = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));
        var spaceAfterKept = await UsedSpaceAsync(projectId);
        var converted = await CreateMemberThreadAsync(projectId, await InviteMemberAsync(projectId));

        await ConvertAsync(converted.Member, EmployeeType.Guest);

        var state = await WaitForCleanupAsync(projectId, converted.Artifact.Id, converted.Attachment.Id, spaceAfterKept);
        var keptRead = await ThreadStatusAsync(kept.Member, kept.ThreadId);
        var keptArtifacts = await ListArtifactsAsAsync(kept.Member, projectId, kept.ThreadId);

        state.ArtifactStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceAfterKept);
        keptRead.Should().Be(HttpStatusCode.OK);
        keptArtifacts.Files.Should().ContainSingle().Which.Id.Should().Be(kept.Artifact.Id);
    }

    [Fact]
    public async Task ConvertProjectOwnerToGuest_PassesTheProjectOnAndRemovesTheFormerOwner()
    {
        var projectOwner = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(projectOwner);
        var projectId = await CreateAiProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateMemberThreadAsync(projectId, projectOwner);

        var progress = await ConvertAsync(projectOwner, EmployeeType.Guest);

        var state = await WaitForCleanupAsync(projectId, thread.Artifact.Id, thread.Attachment.Id, spaceBefore);
        var project = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;
        var members = await MemberIdsAsync(projectId);

        progress.Error.Should().BeNullOrEmpty();
        project.CreatedBy.Id.Should().Be(Owner.Id);
        members.Should().NotContain(projectOwner.Id);
        state.ArtifactStatus.Should().Be(404);
    }

    [Fact]
    public async Task ConvertAgentMemberToGuest_LeavesTheUserAViewerOfTheAgent()
    {
        var agentId = await CreateRoomAsync();
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            agentId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);
        var accessBefore = await AccessOfAsync(agentId, member.Id);

        var progress = await ConvertAsync(member, EmployeeType.Guest);

        var accessAfter = await AccessOfAsync(agentId, member.Id);

        progress.Error.Should().BeNullOrEmpty();
        accessBefore.Should().Be(FileShare.ContentCreator);
        accessAfter.Should().Be(FileShare.Read);
    }

    private async Task<TaskProgressResponseDto> ConvertAsync(User user, EmployeeType type)
    {
        await PeopleHttpClient.Authenticate(Owner);
        await UserTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(type, user.Id, Owner.Id),
            TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var progress = (await UserTypeApi.GetUserTypeUpdateProgressAsync(user.Id, TestContext.Current.CancellationToken)).Response;

            if (progress.IsCompleted || DateTime.UtcNow >= deadline)
            {
                return progress;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
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

    private async Task<List<Guid?>> MemberIdsAsync(int projectId)
    {
        await FilesHttpClient.Authenticate(Owner);
        var shares = (await RoomsApi.GetRoomSecurityInfoAsync(projectId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.Select(s => s.SharedToUser?.Id).ToList();
    }

    private async Task<FileShare?> AccessOfAsync(int roomId, Guid userId)
    {
        await FilesHttpClient.Authenticate(Owner);
        var shares = (await RoomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.FirstOrDefault(s => s.SharedToUser?.Id == userId)?.Access;
    }
}
