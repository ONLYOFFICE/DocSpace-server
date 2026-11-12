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

namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// The events that are not about an entry: users (<c>UserSocketManager</c>, room <c>users</c>) and quota
/// (<c>QuotaSocketManager</c>, room <c>quota</c>).
/// </summary>
[Trait("Category", "Socket")]
public class SocketPortalEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    [Fact]
    public async Task InviteUser_ReachesTheUsersRoom()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(false, cancellationToken, "users");

        var user = await InviteContact(EmployeeType.User);

        await socket.WaitForAsync(
            "s:add-user",
            payload => payload.GetProperty("id").GetGuid() == user.Id,
            cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task ChangeUserType_ReachesThatUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var user = await InviteContact(EmployeeType.User);
        await using var socket = await ConnectSocket(user);

        await _peopleClient.Authenticate(Owner);
        var userTypeApi = new UserTypeApi(_peopleClient, new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });
        await userTypeApi.UpdateUserTypeAsync(EmployeeType.RoomAdmin, new UpdateMembersRequestDto([user.Id], resendAll: false), cancellationToken);

        await socket.WaitForAsync(
            "s:change-my-type",
            payload => payload.GetProperty("id").GetGuid() == user.Id,
            cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task CreateRoom_ReachesTheQuotaRoom()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _filesClient.Authenticate(Owner);
        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(false, cancellationToken, "quota");

        await CreateRoom(new CreateRoomRequestDto("Socket Quota Room", roomType: RoomType.CustomRoom));

        await socket.WaitForAsync(
            "s:change-quota-used-value",
            payload => payload.GetProperty("featureId").GetString() == "room",
            cancellationToken: cancellationToken);
    }
}
