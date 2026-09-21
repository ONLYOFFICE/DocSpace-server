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
/// GET /api/2.0/settings/tfaappcodes and PUT /api/2.0/settings/tfaappnewcodes — reading and
/// regenerating the current user's TFA backup codes, and using one of them in place of a TOTP code
/// at login.
/// </summary>
[Trait("Category", "Settings")]
public class TfaAppCodesTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    [Fact]
    public async Task GetTfaAppCodes_Owner_AlreadyLinkedAccount_ReturnsCodes()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();

        // Act
        var result = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
    }

    // Docs: 405 "TFA application settings are not available" when TFA App is disabled. Live API
    // returns 403 instead.
    [Trait("Bug", "82976")]
    [Fact]
    public async Task GetTfaAppCodes_TfaAppDisabled_ShouldReturn405()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    // Same doc/behavior mismatch as above.
    [Trait("Bug", "82978")]
    [Fact]
    public async Task UpdateTfaAppCodes_TfaAppDisabled_ShouldReturn405()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UpdateTfaAppCodesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    [Fact]
    public async Task UpdateTfaAppCodes_Owner_RegeneratesCodes()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var before = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Act
        var after = await _tfaSettingsApi.UpdateTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Assert
        after.Response.Should().NotBeEmpty();
        after.Response.Select(c => c.Code).Should().NotBeEquivalentTo(before.Response.Select(c => c.Code));
    }

    [Fact]
    public async Task Authentication_BackupCode_AuthenticatesInPlaceOfTotpCode_AndIsSingleUse()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var codes = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);
        var backupCode = codes.Response.First(c => !c.IsUsed).Code;

        // Act
        await _webApiClient.Authenticate(null);
        var login = await _authenticationApi.AuthenticateMeFromBodyWithCodeAsync(
            backupCode,
            new AuthWithCodeRequestsDto { UserName = Owner.Email, Password = Owner.Password, Code = backupCode },
            TestContext.Current.CancellationToken);

        // Assert
        login.Response.Token.Should().NotBeNullOrEmpty();

        // The same backup code is marked used and cannot be reused.
        Owner.Token = login.Response.Token;
        await _webApiClient.Authenticate(Owner);
        var after = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);
        after.Response.First(c => c.Code == backupCode).IsUsed.Should().BeTrue();

        await _webApiClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeFromBodyWithCodeAsync(
                backupCode,
                new AuthWithCodeRequestsDto { UserName = Owner.Email, Password = Owner.Password, Code = backupCode },
                TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetTfaAppCodes_DocSpaceAdmin_AlreadyLinkedAccount_ReturnsCodes()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await EnableTfaAppAsync();
        await LinkTfaAppAsync(admin);

        // Act
        var result = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateTfaAppCodes_DocSpaceAdmin_RegeneratesCodes()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await EnableTfaAppAsync();
        await LinkTfaAppAsync(admin);
        var before = await _tfaSettingsApi.GetTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Act
        var after = await _tfaSettingsApi.UpdateTfaAppCodesAsync(TestContext.Current.CancellationToken);

        // Assert
        after.Response.Should().NotBeEmpty();
        after.Response.Select(c => c.Code).Should().NotBeEquivalentTo(before.Response.Select(c => c.Code));
    }
}
