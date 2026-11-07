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
/// Rules of the invitation links that do not depend on the caller's role: one link per type, the
/// expiration check on update, and the portal setting that blocks members' invitations.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkRulesTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    private async Task DisableMemberInvitationsAsync() =>
        await _commonSettingsApi.UpdateInvitationSettingsAsync(
            new TenantUserInvitationSettingsRequestDto(allowInvitingMembers: false, allowInvitingGuests: true),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task CreateInvitationLink_SecondOfSameType_ReturnsBadRequest()
    {
        await CreateLinkAsOwnerAsync(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(new InvitationLinkCreateRequestDto(EmployeeType.User), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateInvitationLink_PastExpiration_ReturnsBadRequest()
    {
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(new InvitationLinkUpdateRequestDto(created.Id, expiration: DateTime.UtcNow.AddDays(-1)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateInvitationLink_MembersInvitationsDisabled_ReturnsForbidden()
    {
        await _webApiClient.Authenticate(Owner);
        await DisableMemberInvitationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(new InvitationLinkCreateRequestDto(EmployeeType.User), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateInvitationLink_MembersInvitationsDisabled_ReturnsForbidden()
    {
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User, maxUseCount: 5);
        await DisableMemberInvitationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(new InvitationLinkUpdateRequestDto(created.Id, maxUseCount: 6), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteInvitationLink_MembersInvitationsDisabled_ReturnsForbidden()
    {
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);
        await DisableMemberInvitationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(new InvitationLinkDeleteRequestDto(created.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
