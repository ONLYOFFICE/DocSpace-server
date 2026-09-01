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
/// Shared setup for the portal users / invitation-link suites: <see cref="_portalUsersApi"/> is
/// wired onto <see cref="_webApiClient"/>, so acting as a role means re-authenticating that client.
/// </summary>
public abstract class UsersTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Re-authenticates <see cref="_webApiClient"/> as the given role. <c>null</c> means the portal
    /// owner, who is already authenticated by <see cref="BaseTest.InitializeAsync"/> but may have
    /// been switched away from by an earlier step in the same test.
    /// </summary>
    protected async Task<User> ActAsAsync(EmployeeType? role)
    {
        if (role is null)
        {
            await _webApiClient.Authenticate(Owner);
            return Owner;
        }

        var member = await InviteMember(role.Value);
        await _webApiClient.Authenticate(member);
        return member;
    }

    /// <summary>Creates an invitation link for the given employee type, acting as the portal owner.</summary>
    protected async Task<InvitationLinkDto> CreateLinkAsOwnerAsync(EmployeeType employeeType, int? maxUseCount = null, DateTime? expiration = null)
    {
        await _webApiClient.Authenticate(Owner);

        var link = await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(employeeType, expiration, maxUseCount),
            TestContext.Current.CancellationToken);

        return link.Response;
    }
}
