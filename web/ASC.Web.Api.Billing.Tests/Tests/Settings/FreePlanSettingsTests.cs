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

namespace ASC.Web.Api.Billing.Tests.Tests.Settings;

/// <summary>
/// Access settings a hosted portal on the free plan may not use. No payment is scripted, so the portal runs on the
/// stack's default quota, the free startup one: promoting a product administrator, single sign-on and the keys of
/// a paid provider answer 402, the anonymous single sign-on read says the sign-in page is not hidden, and SQLite is
/// refused because the portal is not standalone.
/// </summary>
public class FreePlanSettingsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private DocSpace.API.SDK.Api.Settings.SecurityApi? _securityApi;
    private DocSpace.API.SDK.Api.Settings.SSOApi? _ssoApi;
    private DocSpace.API.SDK.Api.Settings.AuthorizationApi? _authorizationApi;

    private Configuration ApiConfiguration => new() { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') };

    private DocSpace.API.SDK.Api.Settings.SecurityApi Security => _securityApi ??= new(_webApiClient, ApiConfiguration);

    private DocSpace.API.SDK.Api.Settings.SSOApi Sso => _ssoApi ??= new(_webApiClient, ApiConfiguration);

    private DocSpace.API.SDK.Api.Settings.AuthorizationApi Authorization => _authorizationApi ??= new(_webApiClient, ApiConfiguration);

    [Fact]
    public async Task SetProductAdministrator_FreePlan_ThrowsPaymentRequired()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Security.SetProductAdministratorAsync(
                new SecurityRequestDto(Guid.Empty, user.Id, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Your pricing plan does not support this option");
    }

    [Fact]
    public async Task GetSsoSettings_OwnerOnFreePlan_ThrowsPaymentRequired()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task GetSsoSettings_AnonymousOnFreePlan_ReturnsAuthPageNotHidden()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var result = await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.HideAuthPage.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAuthKeys_PaidProviderOnFreePlan_ThrowsPaymentRequired()
    {
        // Arrange: s3 is "paid" in buildtools/config/autofac.consumers.json
        var s3 = new SaveAuthKeysRequestDto(
            name: "s3",
            props: [new AuthKeyRequest(name: "acesskey", value: "key"), new AuthKeyRequest(name: "secretaccesskey", value: "secret")]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Authorization.SaveAuthKeysAsync(s3, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task TestExternalDatabaseConnection_SqliteOnHostedPortal_IsRefused()
    {
        // Act
        var result = await Authorization.TestExternalDatabaseConnectionAsync(
            new ExternalDatabaseConnectionRequestDto(databaseType: "sqlite", sqliteFilePath: "any.db"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Success.Should().BeFalse();
        result.Response.Error.Should().Be("SQLite is only available in Standalone installations.");
    }
}
