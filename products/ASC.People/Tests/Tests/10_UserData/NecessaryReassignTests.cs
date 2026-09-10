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
/// <c>GET /people/reassign/necessary</c> - only the Owner and a DocSpaceAdmin may check whether
/// their own room/file data would need reassigning.
/// </summary>
public class NecessaryReassignTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task NecessaryReassign_AsOwnerWithOwnData_ReturnsTrue()
    {
        await CreateOwnRoomWithFileAsync(Owner, "Autotest Reassign Room", "Autotest Reassign File");

        await _peopleClient.Authenticate(Owner);
        var result = await _userDataApi.NecessaryReassignAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task NecessaryReassign_AsDocSpaceAdminWithOwnData_ReturnsTrue()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await CreateOwnRoomWithFileAsync(admin, "Autotest Reassign Room", "Autotest Reassign File");

        await _peopleClient.Authenticate(admin);
        var result = await _userDataApi.NecessaryReassignAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task NecessaryReassign_AsNonAdminRole_ThrowsAccessDenied(EmployeeType type)
    {
        var member = await InviteMember(type);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.NecessaryReassignAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task NecessaryReassign_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.NecessaryReassignAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
