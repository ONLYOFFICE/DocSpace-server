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

namespace ASC.People.Tests.Tests._07_Quota;

/// <summary>
/// <c>PUT /people/userquota</c> functional behaviour: who may change a quota limit, and whose.
/// </summary>
public class UpdateUserQuotaTests(AspireAppFixture fixture) : QuotaTestBase(fixture)
{
    [Fact]
    public async Task UpdateUserQuota_Owner_ChangesOwnQuotaLimit()
    {
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);

        var result = await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([Owner.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        result.Response.Should().ContainSingle();
        result.Response![0].IsOwner.Should().BeTrue();
        result.Response[0].QuotaLimit.Should().Be(QuotaMinimalBytes);
    }

    [Fact]
    public async Task UpdateUserQuota_Owner_ChangesOtherUsersQuotaLimit()
    {
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        var result = await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto(
                [docSpaceAdmin.Id, roomAdmin.Id, user.Id],
                new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(3);
        result.Response![0].IsAdmin.Should().BeTrue();
        result.Response[0].QuotaLimit.Should().Be(QuotaMinimalBytes);
        result.Response[1].IsRoomAdmin.Should().BeTrue();
        result.Response[1].QuotaLimit.Should().Be(QuotaMinimalBytes);
        result.Response[2].IsCollaborator.Should().BeTrue();
        result.Response[2].QuotaLimit.Should().Be(QuotaMinimalBytes);
    }

    [Fact]
    public async Task UpdateUserQuota_DocSpaceAdmin_ChangesOtherUsersQuotaLimit()
    {
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(docSpaceAdmin);

        var result = await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto(
                [Owner.Id, roomAdmin.Id, user.Id],
                new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(3);
        result.Response![0].IsOwner.Should().BeTrue();
        result.Response[0].QuotaLimit.Should().Be(QuotaMinimalBytes);
        result.Response[1].IsRoomAdmin.Should().BeTrue();
        result.Response[1].QuotaLimit.Should().Be(QuotaMinimalBytes);
        result.Response[2].IsCollaborator.Should().BeTrue();
        result.Response[2].QuotaLimit.Should().Be(QuotaMinimalBytes);
    }
}
