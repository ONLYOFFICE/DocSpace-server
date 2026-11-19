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
/// POST /api/2.0/settings/roomquotasettings and aiagentquotasettings with a default size the portal cannot take: not a
/// whole number of the 64-bit range, or above the portal's own quota. Both answered with plain <c>Exception</c>s, which
/// nothing maps, so a 500 came back; they answer 400 now, as userquotasettings does for the same checks.
/// </summary>
[Trait("Category", "Settings")]
public class QuotaDefaultSizeValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string RoomRoute = "api/2.0/settings/roomquotasettings";
    private const string AiAgentRoute = "api/2.0/settings/aiagentquotasettings";

    private const string RoomError = "Failed to set quota per room. The entered value is greater than the total storage.";
    private const string AiAgentError = "Failed to set quota per AI agent. The entered value is greater than the total storage.";

    // A fraction and a number above long.MaxValue: the SDK carries only an int or a string here, so the body is raw.
    [Theory]
    [InlineData(RoomRoute, "1.5", RoomError)]
    [InlineData(RoomRoute, "9223372036854775808", RoomError)]
    [InlineData(AiAgentRoute, "1.5", AiAgentError)]
    [InlineData(AiAgentRoute, "9223372036854775808", AiAgentError)]
    public async Task SaveDefaultQuota_NotAWholeInt64_Returns400(string route, string defaultQuota, string error)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PostRawAsync(
            route, $$"""{"enableQuota":true,"defaultQuota":{{defaultQuota}}}""", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain(error);
    }

    // The stand is a Standalone installation, so the portal-wide quota of the test's own portal can be set.
    [Fact]
    public async Task SaveRoomQuotaSettings_AbovePortalQuota_Returns400()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestDto(TenantId, 1048576), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
                new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(1073741824)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(RoomError);
    }

    [Fact]
    public async Task SaveAiAgentQuotaSettings_AbovePortalQuota_Returns400()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestDto(TenantId, 1048576), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveAiAgentQuotaSettingsAsync(
                new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(1073741824)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(AiAgentError);
    }
}
