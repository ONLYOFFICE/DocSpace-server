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

namespace ASC.Web.Api.Tests.Tests._02_Settings.IpRestrictions;

/// <summary>
/// Enforcement of switched-on IP restrictions (<c>IpSecurityFilter</c> → <c>IPSecurity.VerifyAsync</c>): a signed-in
/// caller whose address matches no applicable entry gets 403, a <c>forAdmin</c> entry applies to DocSpace
/// administrators only, and the owner is never checked.
///
/// The test client always connects over the loopback, which the check lets through as "my network". The caller's
/// address is therefore set with <c>X-Forwarded-For</c>: the services trust it from 127.0.0.1
/// (<c>core:hosting:forwardedHeadersOptions:knownProxies</c> in buildtools/config/appsettings.json), the way the
/// router passes the real client address. Addresses come from the documentation ranges (RFC 5737). Every member
/// signs in before the header is set, and each test owns its portal, so nothing outside it can be locked out.
/// </summary>
[Trait("Category", "Settings")]
public class IpRestrictionsEnforcementTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string AllowedIp = "198.51.100.7";
    private const string OtherIp = "203.0.113.9";

    [Fact]
    public async Task SignedInMember_FromAddressNotListed_ThrowsForbidden()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await RestrictToAsync(forAdmin: false);
        await _webApiClient.Authenticate(user);
        ComeFrom(OtherIp);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SignedInMember_FromListedAddress_IsServed()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await RestrictToAsync(forAdmin: false);
        await _webApiClient.Authenticate(user);
        ComeFrom(AllowedIp);

        // Act
        var result = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Owner_FromAddressNotListed_IsServed()
    {
        // Arrange
        await RestrictToAsync(forAdmin: false);
        ComeFrom(OtherIp);

        // Act
        var result = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ForAdminEntry_FromThatAddress_ServesDocSpaceAdminAndRefusesUser()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);
        await RestrictToAsync(forAdmin: true);
        await _webApiClient.Authenticate(admin);
        await _webApiClient.Authenticate(user);
        ComeFrom(AllowedIp);

        // Act
        await _webApiClient.Authenticate(admin);
        var adminResult = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        adminResult.StatusCode.Should().Be(200);
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SignedInMember_RestrictionSavedSwitchedOff_IsServedFromAnyAddress()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto(AllowedIp, false)], false), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(user);
        ComeFrom(OtherIp);

        // Act
        var result = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
    }

    private async Task RestrictToAsync(bool forAdmin)
    {
        await _webApiClient.Authenticate(Owner);
        await _ipRestrictionsApi.SaveIpRestrictionsAsync(
            new IpRestrictionsDto([new IpRestrictionEntryDto(AllowedIp, forAdmin)], true), TestContext.Current.CancellationToken);
    }

    private void ComeFrom(string address)
    {
        _webApiClient.DefaultRequestHeaders.Remove("X-Forwarded-For");
        _webApiClient.DefaultRequestHeaders.Add("X-Forwarded-For", address);
    }
}
