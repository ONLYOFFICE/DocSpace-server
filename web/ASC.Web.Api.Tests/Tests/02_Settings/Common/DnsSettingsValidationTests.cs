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
/// PUT /api/2.0/settings/dns with a domain the portal cannot take: not a host name, too long, or already the alias of a
/// portal. Each used to fail with a plain exception, so the call returned 500; it now returns 400 with the reason, and
/// the portal keeps no mapped domain. The section hidden by the installation (402) cannot be reached on the stack, which
/// shows every settings section.
/// </summary>
[Trait("Category", "Settings")]
public class DnsSettingsValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SaveDnsSettings_NotAHostName_ThrowsBadRequest()
    {
        await SaveDnsSettingsShouldBeRefusedAsync("bad domain!", "Invalid domain name");
    }

    [Fact]
    public async Task SaveDnsSettings_DomainTooLong_ThrowsBadRequest()
    {
        await SaveDnsSettingsShouldBeRefusedAsync(new string('a', 64) + ".example.com", "The domain name must be between");
    }

    /// <summary>
    /// The bare alias of the test's own portal passes every check before the address lookup, which counts the portal's
    /// own alias as taken (the stack's base domain is <c>localhost</c>, so the alias is not the portal's own domain).
    /// </summary>
    [Fact]
    public async Task SaveDnsSettings_AliasOfAPortal_ThrowsBadRequest()
    {
        await SaveDnsSettingsShouldBeRefusedAsync(PortalName, "Address busy.");
    }

    private async Task SaveDnsSettingsShouldBeRefusedAsync(string dnsName, string message)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDnsSettingsAsync(
                new DnsSettingsRequestDto(dnsName, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(message);

        var portal = await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken);
        portal.Response.MappedDomain.Should().BeNullOrEmpty();
    }
}
