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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Security;

/// <summary>
/// GET /api/2.0/settings/security/security and GET /api/2.0/settings/security/{id} — reading
/// per-module web-item security. This DocSpace build registers no classic ASC products
/// (CRM, Projects, Mail, ...) in <c>WebItemManager</c>, so the enumeration endpoints come back
/// empty; the per-id security store itself is decoupled from that registry and happily
/// reads/writes an enabled/disabled flag plus subjects for any well-formed GUID.
/// </summary>
[Trait("Category", "Settings")]
public class WebItemSecurityReadTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_Owner_NoIds_ReturnsEmptyList()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _securityApi.GetWebItemSettingsSecurityInfoAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_User_CanRead()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var settings = await _securityApi.GetWebItemSettingsSecurityInfoAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_UnknownId_DefaultsToDisabled()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var id = Guid.NewGuid().ToString();

        // Act
        var settings = await _securityApi.GetWebItemSettingsSecurityInfoAsync(
            [id], TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.Should().ContainSingle(s => s.WebItemId == id && !s.Enabled);
    }

    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetWebItemSettingsSecurityInfoAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    // BUG 83186: a malformed (non-GUID) id crashes this endpoint with an unhandled 500
    // (System.FormatException: "Unrecognized Guid format") instead of a clean 400 -
    // WebItemSecurity.GetSecurityInfoAsync parses the id manually and never catches it.
    [Trait("Bug", "83186")]
    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_MalformedId_ThrowsValidationError()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetWebItemSettingsSecurityInfoAsync(
                ["not-a-guid"], TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // BUG 83192: this endpoint returns full user profiles (id, displayName, avatar, profileUrl -
    // which embeds the user's email in a search query) for anyone listed in a web item's
    // subjects, to ANY authenticated caller including Guest. People API's own
    // GetProfileByUserId denies Guest with 403 "Access denied" for the same lookup - a Guest
    // should not be able to see a portal member's PII here either.
    [Trait("Bug", "83192")]
    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_Guest_DoesNotSeeSubjectProfile()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var id = Guid.NewGuid().ToString();
        var target = await InviteMember(EmployeeType.User);

        await _securityApi.SetWebItemSecurityAsync(
            new WebItemSecurityRequestsDto(id, true, [target.Id]), TestContext.Current.CancellationToken);

        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var settings = await _securityApi.GetWebItemSettingsSecurityInfoAsync(
            [id], TestContext.Current.CancellationToken);

        // Assert
        var leakedUserIds = settings.Response.SelectMany(s => s.Users).Select(u => u.Id);
        leakedUserIds.Should().NotContain(target.Id);
    }

    // BUG 83193: same leak as BUG 83192, this time for a plain User - People API's own
    // GetProfileByUserId denies User with 403 "Access denied" for the same lookup.
    [Trait("Bug", "83193")]
    [Fact]
    public async Task GetWebItemSettingsSecurityInfo_User_DoesNotSeeSubjectProfile()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var id = Guid.NewGuid().ToString();
        var target = await InviteMember(EmployeeType.User);

        await _securityApi.SetWebItemSecurityAsync(
            new WebItemSecurityRequestsDto(id, true, [target.Id]), TestContext.Current.CancellationToken);

        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var settings = await _securityApi.GetWebItemSettingsSecurityInfoAsync(
            [id], TestContext.Current.CancellationToken);

        // Assert
        var leakedUserIds = settings.Response.SelectMany(s => s.Users).Select(u => u.Id);
        leakedUserIds.Should().NotContain(target.Id);
    }

    [Fact]
    public async Task GetWebItemSecurityInfo_UnknownId_ReturnsFalse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _securityApi.GetWebItemSecurityInfoAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeFalse();
    }

    [Fact]
    public async Task GetWebItemSecurityInfo_User_CanCheckAvailability()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var result = await _securityApi.GetWebItemSecurityInfoAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
    }
}
