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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Quota;

/// <summary>
/// Validation of the portal's default user, room and AI agent quotas against the Standalone portal-wide quota
/// (<c>PUT settings/tenantquotasettings</c>), how non-positive and disabled values are stored, and the conditional
/// read of the user default. Every setting is portal-level, so it stays on the test's own portal.
/// </summary>
[Trait("Category", "Settings")]
public class QuotaSettingsValidationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int PortalQuota = 1_048_576; // 1 MB
    private const int AbovePortalQuota = 2 * PortalQuota;
    private const int OneGigabyte = 1_073_741_824;

    [Fact]
    [Trait("Category", "Bug")]
    public async Task SaveRoomQuotaSettings_AbovePortalQuota_Returns400()
    {
        // Arrange
        await CapPortalAsync(PortalQuota);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
                new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(AbovePortalQuota)),
                TestContext.Current.CancellationToken));

        // Assert: a plain Exception is thrown, which answers 500 instead of the 400 of the user default (Q1)
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Failed to set quota per room. The entered value is greater than the total storage.");
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task SaveAiAgentQuotaSettings_AbovePortalQuota_Returns400()
    {
        // Arrange
        await CapPortalAsync(PortalQuota);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveAiAgentQuotaSettingsAsync(
                new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(AbovePortalQuota)),
                TestContext.Current.CancellationToken));

        // Assert (Q1)
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task SaveRoomQuotaSettings_NotWholeNumber_Returns400()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act: a float where a whole number is required, which the typed request cannot carry
        using var response = await _webApi.PostRawAsync(
            "api/2.0/settings/roomquotasettings", """{"enableQuota":true,"defaultQuota":1.5}""", TestContext.Current.CancellationToken);

        // Assert (Q1)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveUserQuotaSettings_AbovePortalQuota_Returns400()
    {
        // Arrange
        await CapPortalAsync(PortalQuota);

        // Act: the user default is hidden from the SDK
        using var response = await _webApi.PostAsync(
            "api/2.0/settings/userquotasettings", new { enableQuota = true, defaultQuota = AbovePortalQuota }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Failed to set quota per user. The entered value is greater than the total storage.");
    }

    [Fact]
    public async Task SaveUserQuotaSettings_NotWholeNumber_Returns400()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PostRawAsync(
            "api/2.0/settings/userquotasettings", """{"enableQuota":true,"defaultQuota":1.5}""", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveUserQuotaSettings_NegativeValue_StoredAsZero()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PostAsync(
            "api/2.0/settings/userquotasettings", new { enableQuota = true, defaultQuota = -5 }, TestContext.Current.CancellationToken);

        // Assert
        var settings = await _webApi.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
        settings.GetProperty("enableQuota").GetBoolean().Should().BeTrue();
        settings.GetProperty("defaultQuota").GetInt64().Should().Be(0);
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_NegativeValue_StoredAsZero()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(-1)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.Response.EnableQuota.Should().BeTrue();
        settings.Response.DefaultQuota.Should().Be(0);
    }

    [Fact]
    public async Task SaveAiAgentQuotaSettings_Disabled_KeepsTheSize()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _settingsQuotaApi.SaveAiAgentQuotaSettingsAsync(
            new QuotaSettingsRequestDto(false, new QuotaSettingsRequestDtoDefaultQuota(OneGigabyte)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.Response.EnableQuota.Should().BeFalse();
        settings.Response.DefaultQuota.Should().Be(OneGigabyte);
    }

    [Fact]
    public async Task SetTenantQuotaSettings_NegativeQuota_RemovesThePortalQuota()
    {
        // Arrange
        await CapPortalAsync(PortalQuota);

        // Act
        var removed = await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestDto(TenantId, -1), TestContext.Current.CancellationToken);

        // Assert: the cap is gone, so a default above the old cap is accepted again
        removed.Response.EnableQuota.Should().BeFalse();
        removed.Response.Quota.Should().Be(-1);

        var room = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(AbovePortalQuota)),
            TestContext.Current.CancellationToken);
        room.Response.DefaultQuota.Should().Be(AbovePortalQuota);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SetTenantQuotaSettings_NonAdministrator_Returns403(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
                new TenantQuotaSettingsRequestDto(TenantId, PortalQuota), TestContext.Current.CancellationToken));

        // Assert: the portal-settings demand comes before the DocSpace-admin check, which is why no caller sees its 415
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetUserQuotaSettings_IfModifiedSinceLastModified_Returns304()
    {
        // Arrange: the server writes Last-Modified in the invariant culture and compares the request header as a string
        await _webApiClient.Authenticate(Owner);
        using var first = await _webApi.GetAsync("api/2.0/settings/userquotasettings", TestContext.Current.CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastModified = first.Content.Headers.NonValidated.TryGetValues("Last-Modified", out var values)
            ? values.ToString()
            : first.Headers.NonValidated["Last-Modified"].ToString();
        lastModified.Should().NotBeNullOrEmpty();

        // Act: a conditional request header, which the typed call cannot send
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/2.0/settings/userquotasettings");
        request.Headers.TryAddWithoutValidation("If-Modified-Since", lastModified);
        using var response = await _webApiClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    private async Task CapPortalAsync(long quota)
    {
        await _webApiClient.Authenticate(Owner);

        await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestDto(TenantId, quota), TestContext.Current.CancellationToken);
    }
}
