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

namespace ASC.People.Tests.Tests._03_UserStatus;

/// <summary>
/// <c>GET /people/status/{status}</c> — functional coverage. Who may call it (permission
/// coverage) lives in <see cref="UserStatusGetPermissionsTests"/>.
/// </summary>
public class UserStatusGetTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Owner_FilterActive_ReturnsAllUserTypes()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Active, cancellationToken: TestContext.Current.CancellationToken);

        var docSpaceAdminInfo = result.Response.Should().ContainSingle(u => u.Email == docSpaceAdmin.Email).Subject;
        docSpaceAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        docSpaceAdminInfo.IsAdmin.Should().BeTrue();

        var roomAdminInfo = result.Response.Should().ContainSingle(u => u.Email == roomAdmin.Email).Subject;
        roomAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        roomAdminInfo.IsRoomAdmin.Should().BeTrue();

        var userInfo = result.Response.Should().ContainSingle(u => u.Email == user.Email).Subject;
        userInfo.Status.Should().Be(EmployeeStatus.Active);
        userInfo.IsCollaborator.Should().BeTrue();

        var guestInfo = result.Response.Should().ContainSingle(u => u.Email == guest.Email).Subject;
        guestInfo.Status.Should().Be(EmployeeStatus.Active);
        guestInfo.IsVisitor.Should().BeTrue();
    }

    [Fact]
    public async Task Owner_FilterTerminated_ReturnsAllUserTypes()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([docSpaceAdmin.Id, roomAdmin.Id, user.Id, guest.Id], false),
            TestContext.Current.CancellationToken);

        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Terminated, cancellationToken: TestContext.Current.CancellationToken);

        var docSpaceAdminInfo = result.Response.Should().ContainSingle(u => u.Email == docSpaceAdmin.Email).Subject;
        docSpaceAdminInfo.Status.Should().Be(EmployeeStatus.Terminated);
        docSpaceAdminInfo.IsAdmin.Should().BeTrue();

        var roomAdminInfo = result.Response.Should().ContainSingle(u => u.Email == roomAdmin.Email).Subject;
        roomAdminInfo.Status.Should().Be(EmployeeStatus.Terminated);
        roomAdminInfo.IsRoomAdmin.Should().BeTrue();

        var userInfo = result.Response.Should().ContainSingle(u => u.Email == user.Email).Subject;
        userInfo.Status.Should().Be(EmployeeStatus.Terminated);
        userInfo.IsCollaborator.Should().BeTrue();

        var guestInfo = result.Response.Should().ContainSingle(u => u.Email == guest.Email).Subject;
        guestInfo.Status.Should().Be(EmployeeStatus.Terminated);
        guestInfo.IsVisitor.Should().BeTrue();
    }

    [Fact]
    public async Task DocSpaceAdmin_FilterActive_ReturnsAllUserTypes()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(docSpaceAdmin);
        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Active, cancellationToken: TestContext.Current.CancellationToken);

        var docSpaceAdminInfo = result.Response.Should().ContainSingle(u => u.Email == docSpaceAdmin.Email).Subject;
        docSpaceAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        docSpaceAdminInfo.IsAdmin.Should().BeTrue();

        var roomAdminInfo = result.Response.Should().ContainSingle(u => u.Email == roomAdmin.Email).Subject;
        roomAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        roomAdminInfo.IsRoomAdmin.Should().BeTrue();

        var userInfo = result.Response.Should().ContainSingle(u => u.Email == user.Email).Subject;
        userInfo.Status.Should().Be(EmployeeStatus.Active);
        userInfo.IsCollaborator.Should().BeTrue();

        var guestInfo = result.Response.Should().ContainSingle(u => u.Email == guest.Email).Subject;
        guestInfo.Status.Should().Be(EmployeeStatus.Active);
        guestInfo.IsVisitor.Should().BeTrue();
    }

    [Fact]
    public async Task DocSpaceAdmin_FilterTerminated_ReturnsAllUserTypes()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdminTarget = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([docSpaceAdminTarget.Id, roomAdmin.Id, user.Id, guest.Id], false),
            TestContext.Current.CancellationToken);

        // The actor is a fresh, still-active DocSpace admin - distinct from the terminated one above.
        var docSpaceAdminActor = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(docSpaceAdminActor);

        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Terminated, cancellationToken: TestContext.Current.CancellationToken);

        var docSpaceAdminInfo = result.Response.Should().ContainSingle(u => u.Email == docSpaceAdminTarget.Email).Subject;
        docSpaceAdminInfo.Status.Should().Be(EmployeeStatus.Terminated);
        docSpaceAdminInfo.IsAdmin.Should().BeTrue();

        var roomAdminInfo = result.Response.Should().ContainSingle(u => u.Email == roomAdmin.Email).Subject;
        roomAdminInfo.Status.Should().Be(EmployeeStatus.Terminated);
        roomAdminInfo.IsRoomAdmin.Should().BeTrue();

        var userInfo = result.Response.Should().ContainSingle(u => u.Email == user.Email).Subject;
        userInfo.Status.Should().Be(EmployeeStatus.Terminated);
        userInfo.IsCollaborator.Should().BeTrue();

        var guestInfo = result.Response.Should().ContainSingle(u => u.Email == guest.Email).Subject;
        guestInfo.Status.Should().Be(EmployeeStatus.Terminated);
        guestInfo.IsVisitor.Should().BeTrue();
    }

    [Fact]
    public async Task RoomAdmin_FilterActive_ReturnsVisibleUserTypes()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Active, cancellationToken: TestContext.Current.CancellationToken);

        var docSpaceAdminInfo = result.Response.Should().ContainSingle(u => u.Email == docSpaceAdmin.Email).Subject;
        docSpaceAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        docSpaceAdminInfo.IsAdmin.Should().BeTrue();

        var roomAdminInfo = result.Response.Should().ContainSingle(u => u.Email == roomAdmin.Email).Subject;
        roomAdminInfo.Status.Should().Be(EmployeeStatus.Active);
        roomAdminInfo.IsRoomAdmin.Should().BeTrue();

        var userInfo = result.Response.Should().ContainSingle(u => u.Email == user.Email).Subject;
        userInfo.Status.Should().Be(EmployeeStatus.Active);
        userInfo.IsCollaborator.Should().BeTrue();
    }

    [Fact]
    public async Task RoomAdmin_FilterTerminated_ReturnsNoResults()
    {
        await _peopleClient.Authenticate(Owner);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdminTarget = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([docSpaceAdmin.Id, roomAdminTarget.Id, user.Id, guest.Id], false),
            TestContext.Current.CancellationToken);

        // The actor is a fresh, still-active room admin - distinct from the terminated one above.
        var roomAdminActor = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdminActor);

        var result = await _userStatusApi.GetByStatusAsync(EmployeeStatus.Terminated, cancellationToken: TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Count.Should().Be(0);
        result.Response.Should().BeEmpty();
    }
}
