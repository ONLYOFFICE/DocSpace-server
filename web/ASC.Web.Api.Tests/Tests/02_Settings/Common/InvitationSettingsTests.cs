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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// GET and PUT /api/2.0/settings/invitationsettings. Reading needs no permission (the
/// invitation flow may run before sign-in); saving needs the EditPortalSettings right.
/// </summary>
[Trait("Category", "Settings")]
public class InvitationSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetInvitationSettings_Owner_BothFlagsEnabledByDefault()
    {
        // Act
        var settings = await _commonSettingsApi.GetTenantUserInvitationSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.AllowInvitingMembers.Should().BeTrue();
        settings.Response.AllowInvitingGuests.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvitationSettings_Anonymous_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var settings = await _commonSettingsApi.GetTenantUserInvitationSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateInvitationSettings_OwnerDisablesBoth_ReadBackConfirms()
    {
        // Act
        var saved = await _commonSettingsApi.UpdateInvitationSettingsAsync(
            new TenantUserInvitationSettingsRequestDto(false, false), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);

        var settings = await _commonSettingsApi.GetTenantUserInvitationSettingsAsync(TestContext.Current.CancellationToken);
        settings.Response.AllowInvitingMembers.Should().BeFalse();
        settings.Response.AllowInvitingGuests.Should().BeFalse();
    }

    /// <summary>
    /// Documents the current behaviour: a field omitted from the request is not left untouched but
    /// treated as the type default (false). The typed DTO always emits both fields, so this goes over
    /// raw HTTP with a body the DTO cannot carry.
    /// </summary>
    [Fact]
    public async Task UpdateInvitationSettings_FieldOmitted_ResetsItToFalse()
    {
        // Arrange
        await _commonSettingsApi.UpdateInvitationSettingsAsync(
            new TenantUserInvitationSettingsRequestDto(true, true), TestContext.Current.CancellationToken);

        // Act
        using var response = await _webApiClient.PutAsJsonAsync(
            "api/2.0/settings/invitationsettings", new { allowInvitingGuests = false }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await _commonSettingsApi.GetTenantUserInvitationSettingsAsync(TestContext.Current.CancellationToken);
        settings.Response.AllowInvitingGuests.Should().BeFalse();
        settings.Response.AllowInvitingMembers.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateInvitationSettings_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var saved = await _commonSettingsApi.UpdateInvitationSettingsAsync(
            new TenantUserInvitationSettingsRequestDto(true, true), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateInvitationSettings_User_ThrowsForbidden()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.UpdateInvitationSettingsAsync(
                new TenantUserInvitationSettingsRequestDto(true, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateInvitationSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.UpdateInvitationSettingsAsync(
                new TenantUserInvitationSettingsRequestDto(true, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
