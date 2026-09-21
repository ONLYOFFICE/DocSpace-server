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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// POST /api/2.0/security/audit/settings/lifetime — sets the login history and audit trail
/// retention lifetimes for the current portal. Only an Owner or a DocSpaceAdmin may call it.
///
/// The TS suite calls <c>paymentsApi.setupPayment()</c> first; see
/// <see cref="AuditTrailReportTests"/> for why that is not needed on this Aspire-hosted
/// (<c>Standalone</c>) integration portal.
/// </summary>
/// <remarks>
/// SDK gap: the response is a flat <c>TenantAuditSettings</c>, not the nested
/// <c>{"settings": {...}}</c> shape the generated <see cref="TenantAuditSettingsWrapper"/> expects
/// — see <see cref="AuditSettingsGetTests"/> for the full explanation. The request body genuinely
/// is nested that way (that part of the generated model is correct), so the negative cases go
/// through the typed SDK method and only the positive responses are read as raw JSON.
/// </remarks>
[Trait("Category", "Security")]
public class AuditSettingsSetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static readonly TenantAuditSettingsWrapper _settingsUpdate = new(new TenantAuditSettings(180, 180));

    [Fact]
    public async Task SetAuditSettings_Owner_UpdatesSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PostAsync(
            "api/2.0/security/audit/settings/lifetime",
            new { settings = new { loginHistoryLifeTime = 180, auditTrailLifeTime = 180 } },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertUpdatedSettings(body);
    }

    [Fact]
    public async Task SetAuditSettings_DocSpaceAdmin_UpdatesSettings()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        using var response = await _webApi.PostAsync(
            "api/2.0/security/audit/settings/lifetime",
            new { settings = new { loginHistoryLifeTime = 180, auditTrailLifeTime = 180 } },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertUpdatedSettings(body);
    }

    [Fact]
    public async Task SetAuditSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.SetAuditSettingsAsync(_settingsUpdate, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetAuditSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.SetAuditSettingsAsync(_settingsUpdate, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    private static void AssertUpdatedSettings(string body)
    {
        using var json = JsonDocument.Parse(body);
        var settings = json.RootElement.GetProperty("response");

        settings.GetProperty("loginHistoryLifeTime").GetInt32().Should().Be(180);
        settings.GetProperty("auditTrailLifeTime").GetInt32().Should().Be(180);
        settings.TryGetProperty("lastModified", out _).Should().BeTrue();
    }
}
