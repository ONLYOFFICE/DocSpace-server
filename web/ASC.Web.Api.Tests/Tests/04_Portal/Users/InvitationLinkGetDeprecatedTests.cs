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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Users;

/// <summary>
/// GET /api/2.0/portal/users/invite/{employeeType} — the deprecated invitation-link endpoint.
/// Superseded by <see cref="InvitationLinkGetTests"/>; it returns a plain URL string instead of an
/// <see cref="InvitationLinkDto"/> and, unlike the current endpoint, does not enforce role-based
/// access control at all — every authenticated caller gets 200, just with an empty body when denied.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkGetDeprecatedTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    [Theory]
    [InlineData(null, EmployeeType.User)]
    [InlineData(null, EmployeeType.DocSpaceAdmin)]
    [InlineData(null, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.User)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.User)]
    public async Task GetInvitationLink_ByRole_ReturnsUrl(EmployeeType? actingRole, EmployeeType targetType)
    {
        // Arrange — a fresh portal has no default invitation links, so create one first.
        await CreateLinkAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
#pragma warning disable CS0612 // deprecated endpoint under test
        var result = await _portalUsersApi.GetInvitationLinkAsync(targetType, TestContext.Current.CancellationToken);
#pragma warning restore CS0612

        // Assert
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetInvitationLink_Owner_ForGuestType_ReturnsLink()
    {
        // Act
#pragma warning disable CS0612
        var result = await _portalUsersApi.GetInvitationLinkAsync(EmployeeType.Guest, TestContext.Current.CancellationToken);
#pragma warning restore CS0612

        // Assert
        // The TS test's title says "empty response", but its body asserts toBeTruthy — the
        // endpoint does return a link for the Guest type, and that is what it verifies.
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetInvitationLink_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
#pragma warning disable CS0612
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkAsync(EmployeeType.User, TestContext.Current.CancellationToken));
#pragma warning restore CS0612

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User, EmployeeType.User)]
    [InlineData(EmployeeType.Guest, EmployeeType.User)]
    public async Task GetInvitationLink_ByRole_WithoutAccess_ReturnsEmptyResponse(EmployeeType actingRole, EmployeeType targetType)
    {
        // Arrange
        await CreateLinkAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
#pragma warning disable CS0612
        var result = await _portalUsersApi.GetInvitationLinkAsync(targetType, TestContext.Current.CancellationToken);
#pragma warning restore CS0612

        // Assert — the deprecated endpoint does not enforce role-based access control: it returns
        // 200 (no ApiException) with an empty response instead of 403.
        result.Response.Should().BeNullOrEmpty();
    }
}
