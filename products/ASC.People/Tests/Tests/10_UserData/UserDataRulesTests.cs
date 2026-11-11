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

namespace ASC.People.Tests.Tests._10_UserData;

/// <summary>
/// The argument and target rules of the data-lifecycle endpoints: who <c>necessary</c> may ask
/// about and what it counts for a guest conversion, which sources and recipients
/// <c>reassign/start</c> and <c>remove/start</c> refuse, and the personal folder deletion of a
/// guest that still has one.
/// </summary>
public class UserDataRulesTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task NecessaryReassign_DocSpaceAdminAboutAnotherDocSpaceAdmin_ReturnsForbidden()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var peer = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.NecessaryReassignAsync(peer.Id, EmployeeType.User, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task NecessaryReassign_DocSpaceAdminAboutUser_Answers()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(admin);

        var result = await _userDataApi.NecessaryReassignAsync(member.Id, EmployeeType.User, TestContext.Current.CancellationToken);

        result.Response.Should().BeFalse("the user owns no room");
    }

    /// <summary>
    /// A user without rooms who shared a file from their personal folder: nothing to reassign for
    /// an ordinary type change, but a conversion to Guest counts the shared entries.
    /// </summary>
    [Fact]
    public async Task NecessaryReassign_UserWithSharedPersonalFile_TrueOnlyForGuestConversion()
    {
        var member = await InviteContact(EmployeeType.User);
        var recipient = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        var file = (await _filesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest("Autotest Shared File"), TestContext.Current.CancellationToken)).Response;
        await _sharingApi.SetFileSecurityInfoAsync(
            file.Id,
            new SecurityInfoSimpleRequestDto([new FileShareParams(recipient.Id, FileShare.Read)], notify: false),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);
        var forGuest = await _userDataApi.NecessaryReassignAsync(member.Id, EmployeeType.Guest, TestContext.Current.CancellationToken);
        var forUser = await _userDataApi.NecessaryReassignAsync(member.Id, EmployeeType.User, TestContext.Current.CancellationToken);

        forGuest.Response.Should().BeTrue();
        forUser.Response.Should().BeFalse();
    }

    public enum ReassignCase
    {
        ToUser,
        ToGuest,
        ToDisabledRoomAdmin,
        FromOwner,
        FromSelf
    }

    [Theory]
    [InlineData(ReassignCase.ToUser)]
    [InlineData(ReassignCase.ToGuest)]
    [InlineData(ReassignCase.ToDisabledRoomAdmin)]
    [InlineData(ReassignCase.FromOwner)]
    [InlineData(ReassignCase.FromSelf)]
    public async Task StartReassign_RefusedSourceOrRecipient_ReturnsBadRequest(ReassignCase reassignCase)
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var source = await InviteContact(EmployeeType.User);
        var recipient = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(source);

        var (fromId, toId) = reassignCase switch
        {
            ReassignCase.ToUser => (source.Id, (await InviteContact(EmployeeType.User)).Id),
            ReassignCase.ToGuest => (source.Id, (await InviteGuest()).Id),
            ReassignCase.ToDisabledRoomAdmin => (source.Id, await DisabledRoomAdminIdAsync()),
            ReassignCase.FromOwner => (Owner.Id, recipient.Id),
            ReassignCase.FromSelf => (admin.Id, recipient.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(reassignCase))
        };

        await _peopleClient.Authenticate(admin);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(new StartReassignRequestDto(fromId, toId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Can not reassign data");
    }

    public enum RemoveCase
    {
        Unknown,
        Owner,
        Self,
        Active
    }

    [Theory]
    [InlineData(RemoveCase.Unknown)]
    [InlineData(RemoveCase.Owner)]
    [InlineData(RemoveCase.Self)]
    [InlineData(RemoveCase.Active)]
    public async Task StartRemove_RefusedTarget_ReturnsBadRequest(RemoveCase removeCase)
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var targetId = removeCase switch
        {
            RemoveCase.Unknown => Guid.NewGuid(),
            RemoveCase.Owner => Owner.Id,
            RemoveCase.Self => admin.Id,
            RemoveCase.Active => (await InviteContact(EmployeeType.User)).Id,
            _ => throw new ArgumentOutOfRangeException(nameof(removeCase))
        };

        await _peopleClient.Authenticate(admin);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartRemoveAsync(new TerminateRequestDto(targetId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetDeletePersonalFolderProgress_NothingQueued_ReturnsEmptyResponse()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var result = await _userDataApi.GetDeletePersonalFolderProgressAsync(TestContext.Current.CancellationToken);

        result.Response.Should().BeNull();
    }

    /// <summary>
    /// A User who already has a personal folder keeps it when converted to Guest; such a guest may
    /// queue its deletion and watch it through the progress endpoint until it completes.
    /// </summary>
    [Fact]
    public async Task StartDeletePersonalFolder_GuestWhoKeptAPersonalFolder_RunsTheJobToCompletion()
    {
        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        await _filesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest("Autotest Personal File"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(new StartUpdateUserTypeDto(EmployeeType.Guest, member.Id), TestContext.Current.CancellationToken);
        var conversion = await PollUntilAsync(
            async () => (await _userTypeApi.GetUserTypeUpdateProgressAsync(member.Id, TestContext.Current.CancellationToken)).Response,
            p => p.IsCompleted);
        conversion.Error.Should().BeEmpty();

        await _peopleClient.Authenticate(member);
        var started = await _userDataApi.StartDeletePersonalFolderAsync(TestContext.Current.CancellationToken);
        var progress = await PollUntilAsync(
            async () => (await _userDataApi.GetDeletePersonalFolderProgressAsync(TestContext.Current.CancellationToken)).Response,
            p => p is { IsCompleted: true });

        started.Response.Should().NotBeNull();
        progress.Should().NotBeNull();
        progress.IsCompleted.Should().BeTrue();
        progress.Error.Should().BeNullOrEmpty();
    }

    private async Task<Guid> DisabledRoomAdminIdAsync()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(roomAdmin);

        return roomAdmin.Id;
    }
}
