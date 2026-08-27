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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Tfa;

/// <summary>
/// PUT /api/2.0/settings/tfaapp — updating the portal's two-factor authentication policy: enabling
/// or disabling TFA App, and the field-level validation of the request body.
/// </summary>
[Trait("Category", "Settings")]
public class TfaSettingsUpdateTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    [Fact]
    public async Task UpdateTfaSettings_Owner_EnablesApp_ReturnsTrue()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTfaSettings_Owner_DisablesTfa_Returns200()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsWithHttpInfoAsync(
            new TfaRequestsDto(TfaRequestsDtoType.None), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Docs: PUT /settings/tfaapp returns 405 "SMS settings are not available" when no SMS provider
    // is configured. Live API returns 403 instead.
    [Trait("Bug", "82970")]
    [Fact]
    public async Task UpdateTfaSettings_NoSmsProviderConfigured_ShouldReturn405()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaSettingsAsync(
                new TfaRequestsDto(TfaRequestsDtoType.Sms), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    // The DTO's Type is a plain enum, so an out-of-range value ("99") isn't a value the typed
    // constructor can reject client-side — it's an ordinary int on the wire the server is free to
    // ignore, so this stays a typed call via an explicit cast.
    [Fact]
    public async Task UpdateTfaSettings_OutOfRangeType_IsIgnored_ReturnsFalse()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto((TfaRequestsDtoType)99), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTfaSettings_NonExistentMandatoryUsersId_IsIgnored_ReturnsFalse()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.None, mandatoryUsers: [Guid.Empty]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTfaSettings_NonExistentMandatoryGroupsId_IsIgnored_ReturnsFalse()
    {
        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.None, mandatoryGroups: [Guid.Empty]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
    }

    // mandatoryUsers is typed List<Guid> — a malformed id ("not-a-guid") is a value the typed
    // constructor cannot carry at all, so this goes raw.
    [Fact]
    public async Task UpdateTfaSettings_MalformedMandatoryUsersId_ReturnsValidationError()
    {
        // Act
        using var response = await _webApi.PutAsync(
            "api/2.0/settings/tfaapp",
            new { type = 0, mandatoryUsers = new[] { "not-a-guid" } },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("$.mandatoryUsers[0]");
    }

    // BUG 82994: a malformed trustedIps entry is accepted with no format validation (200) and
    // stored as-is. Every subsequent login attempt by any user on the portal then crashes with 500
    // (System.FormatException) when TfaEnabledForUserAsync tries to parse it as an IP. The correct
    // behaviour is that login keeps working (200) regardless of what's stored in trustedIps.
    [Trait("Bug", "82994")]
    [Fact]
    public async Task UpdateTfaSettings_MalformedTrustedIpsEntry_ShouldNotCrashSubsequentLogin()
    {
        // Arrange
        var enable = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App, trustedIps: ["not-an-ip"]), TestContext.Current.CancellationToken);
        enable.Response.Should().BeTrue();

        // Act
        await _webApiClient.Authenticate(null);
        var login = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: Owner.Email, password: Owner.Password), TestContext.Current.CancellationToken);

        // Assert
        login.Response.Should().NotBeNull();
        login.Response.Tfa.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTfaSettings_MandatoryUser_ForcesTfaSetupOnLogin()
    {
        // Arrange
        var mandatory = await InviteContact(EmployeeType.User);
        await EnableTfaAppAsync(mandatoryUsers: [mandatory.Id]);

        // Act
        await _webApiClient.Authenticate(null);
        var login = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: mandatory.Email, password: mandatory.Password), TestContext.Current.CancellationToken);

        // Assert
        login.Response.Tfa.Should().BeTrue();
        login.Response.TfaKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateTfaSettings_DocSpaceAdmin_EnablesApp_ReturnsTrue()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTfaSettings_DocSpaceAdmin_DisablesTfa_Returns200()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _tfaSettingsApi.UpdateTfaSettingsWithHttpInfoAsync(
            new TfaRequestsDto(TfaRequestsDtoType.None), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTfaSettings_EnablingApp_InvalidatesOwnersOwnToken()
    {
        // Arrange
        await EnableTfaAppAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.GetTfaSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    // Confirmed live: this isn't a self-invalidation of just the caller's own token — it's a
    // portal-wide security-stamp reset. An already-authenticated DocSpaceAdmin who never touched
    // TFA settings gets logged out too, purely as a side effect of the Owner's action.
    [Fact]
    public async Task UpdateTfaSettings_EnablingApp_InvalidatesEveryPortalSession()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var before = await _tfaSettingsApi.GetTfaSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);
        before.StatusCode.Should().Be(HttpStatusCode.OK);

        await EnableTfaAppAsync();

        // Act
        await _webApiClient.Authenticate(admin);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.GetTfaSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
