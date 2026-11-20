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
/// POST /api/2.0/settings/maildomainsettings — the trusted mail domains of the portal
/// (<c>SettingsController.SaveMailDomainSettings</c>). The stored type and list are read back through the portal
/// settings.
/// </summary>
[Trait("Category", "Settings")]
public class MailDomainSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SaveMailDomainSettings_CustomType_StoresDomainsTrimmedAndLowerCasedAndConfirms()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var saved = await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.Custom, ["  Example-Trusted.COM ", "second.example.org"], false),
            TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.Should().Be("Settings have been successfully updated");
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.TrustedDomainsType.Should().Be(TenantTrustedDomainsType.Custom);
        settings.TrustedDomains.Should().BeEquivalentTo("example-trusted.com", "second.example.org");
    }

    // PS-7: the controller clears and refills TrustedDomains of the current tenant object before the check; the
    // check throws before SaveTenantAsync, but that object is the cached one, so later reads report the entries
    // accepted up to the malformed one ("good.example.com") instead of the stored list.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task SaveMailDomainSettings_CustomTypeWithMalformedDomain_ThrowsBadRequestAndStoredDomainsStay()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.Custom, ["kept.example.com"], false), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveMailDomainSettingsAsync(
                new MailDomainSettingsRequestDto(TenantTrustedDomainsType.Custom, ["good.example.com", "bad domain!"], false),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.TrustedDomainsType.Should().Be(TenantTrustedDomainsType.Custom);
        settings.TrustedDomains.Should().BeEquivalentTo("kept.example.com");
    }

    [Fact]
    public async Task SaveMailDomainSettings_CustomTypeWithEmptyDomains_ThrowsBadRequestAndTypeStays()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.All, [], false), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveMailDomainSettingsAsync(
                new MailDomainSettingsRequestDto(TenantTrustedDomainsType.Custom, [], false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.TrustedDomainsType.Should().Be(TenantTrustedDomainsType.All);
    }

    [Fact]
    public async Task SaveMailDomainSettings_AllTypeWithEmptyDomains_ReturnsOk()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var saved = await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.All, [], false), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.TrustedDomainsType.Should().Be(TenantTrustedDomainsType.All);
    }

    [Fact]
    public async Task SaveMailDomainSettings_NoneTypeWithMalformedDomains_IgnoresDomainsAndReturnsOk()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act: only the Custom type reads the list, so a malformed entry is not even looked at
        var saved = await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.None, ["bad domain!"], false), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        settings.TrustedDomainsType.Should().Be(TenantTrustedDomainsType.None);
        (settings.TrustedDomains ?? []).Should().NotContain("bad domain!");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SaveMailDomainSettings_MemberWithoutSettingsRight_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveMailDomainSettingsAsync(
                new MailDomainSettingsRequestDto(TenantTrustedDomainsType.All, [], false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveMailDomainSettings_Saved_WritesTrustedMailDomainSettingsUpdatedAuditEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _commonSettingsApi.SaveMailDomainSettingsAsync(
            new MailDomainSettingsRequestDto(TenantTrustedDomainsType.All, [], false), TestContext.Current.CancellationToken);

        // Assert
        var events = await SettingsAudit.PollAsync(_auditTrailDataApi, MessageAction.TrustedMailDomainSettingsUpdated);
        events.Should().NotBeEmpty();
    }
}
