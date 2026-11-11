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
/// PUT /api/2.0/settings/dns on the stand's self-hosted installation (<c>DnsSettings.SaveDnsSettingsAsync</c>).
/// Every test maps a domain onto its own portal only; the clients reach the portal through its alias in the
/// <c>Origin</c> header, which keeps resolving after a mapping, and the mapping is read back from
/// <c>getPortalInformation</c>.
/// </summary>
[Trait("Category", "Settings")]
public class DnsSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static string NewDomain() => $"dns{Guid.NewGuid():N}.example.com";

    private async Task<string?> MappedDomainAsync()
    {
        return (await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response.MappedDomain;
    }

    private async Task<string?> StoredMappedDomainAsync()
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT mappeddomain FROM tenants_tenants WHERE id = @id";
        command.Parameters.AddWithValue("@id", TenantId);

        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken) as string;
    }

    [Fact]
    public async Task SaveDnsSettings_Owner_MapsTheNameTrimmedAndLowerCased()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var domain = NewDomain();

        // Act
        var saved = await _commonSettingsApi.SaveDnsSettingsWithHttpInfoAsync(
            new DnsSettingsRequestDto($"  {domain.ToUpperInvariant()} ", true), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        (await MappedDomainAsync()).Should().Be(domain);
    }

    [Fact]
    public async Task SaveDnsSettings_EnableFalse_IgnoresTheNameAndClearsTheMapping()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(NewDomain(), true), TestContext.Current.CancellationToken);

        // Act
        var saved = await _commonSettingsApi.SaveDnsSettingsWithHttpInfoAsync(
            new DnsSettingsRequestDto(NewDomain(), false), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        (await MappedDomainAsync()).Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SaveDnsSettings_EmptyName_ClearsTheMapping()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(NewDomain(), true), TestContext.Current.CancellationToken);

        // Act
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto("   ", true), TestContext.Current.CancellationToken);

        // Assert
        (await MappedDomainAsync()).Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SaveDnsSettings_MemberWithoutSettingsRight_ThrowsForbiddenAndMappingStays(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var domain = NewDomain();
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(domain, true), TestContext.Current.CancellationToken);
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(NewDomain(), true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await _webApiClient.Authenticate(Owner);
        (await MappedDomainAsync()).Should().Be(domain);
    }

    // PS-5: the refusal is a plain exception, so the code is 500 (documented in the yaml); the row only claims the
    // refusal and that nothing changes, which is what is asserted.
    [Fact]
    public async Task SaveDnsSettings_NameUnderTheBaseDomain_IsRefusedAndMappingStays()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var domain = NewDomain();
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(domain, true), TestContext.Current.CancellationToken);
        var baseDomain = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.BaseDomain;
        baseDomain.Should().NotBeNullOrEmpty();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDnsSettingsAsync(
                new DnsSettingsRequestDto($"other{Guid.NewGuid():N}.{baseDomain}", true), TestContext.Current.CancellationToken));

        // Assert: the database keeps the old mapping; the portal must report the same
        exception.ErrorCode.Should().BeGreaterThanOrEqualTo(400);
        (await StoredMappedDomainAsync()).Should().Be(domain);
        (await MappedDomainAsync()).Should().Be(domain);
    }

    // PS-7: DnsSettings sets MappedDomain on the cached current tenant before SaveTenantAsync refuses the taken
    // name, so the refusal is answered but the portal then reports the refused name as its mapping.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task SaveDnsSettings_NameMappedByAnotherPortal_IsRefusedAndMappingStays()
    {
        // Arrange
        var taken = NewDomain();
        using (var other = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken))
        {
            await other.WebApiHttpClient.Authenticate(other.Owner);
            await other.CommonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(taken, true), TestContext.Current.CancellationToken);
            (await other.PortalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response.MappedDomain.Should().Be(taken);
        }

        await _webApiClient.Authenticate(Owner);
        var domain = NewDomain();
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(domain, true), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(taken, true), TestContext.Current.CancellationToken));

        // Assert: the database keeps the old mapping; the portal must report the same
        exception.ErrorCode.Should().BeGreaterThanOrEqualTo(400);
        (await StoredMappedDomainAsync()).Should().Be(domain);
        (await MappedDomainAsync()).Should().Be(domain);
    }

    [Fact]
    public async Task SaveDnsSettings_Mapped_WritesDnsSettingsUpdatedAuditEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        await _commonSettingsApi.SaveDnsSettingsAsync(new DnsSettingsRequestDto(NewDomain(), true), TestContext.Current.CancellationToken);

        // Assert
        var events = await SettingsAudit.PollAsync(_auditTrailDataApi, MessageAction.DnsSettingsUpdated);
        events.Should().NotBeEmpty();
    }
}
