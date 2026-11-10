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

namespace ASC.Web.Api.Billing.Tests.Tests.Quota;

/// <summary>
/// The portal-level quota settings on a hosted (SaaS) portal: the statistics feature of the tariff gates them, the
/// tariff's total storage caps the defaults, the Standalone-only portal quota is refused, and the administrator count
/// of the free Startup plan is enforced.
/// </summary>
public class QuotaSettingsTariffTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // Far above the 250 GB of one unit of the admin plan, and above any int the typed request can carry.
    private const long AboveTotalStorage = 1_000_000_000_000_000;

    public static TheoryData<string> StatisticGatedSettings => ["roomquotasettings", "aiagentquotasettings", "userquotasettings", "resetquota", "tenantquotasettings"];

    [Theory]
    [MemberData(nameof(StatisticGatedSettings))]
    public async Task QuotaSetting_FreePlanWithoutStatistic_Returns402(string setting)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var status = await SendQuotaSettingAsync(setting, 1_048_576);

        // Assert
        status.Should().Be(HttpStatusCode.PaymentRequired, "the Startup plan has no statistic feature");
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task SetTenantQuotaSettings_HostedPortal_Returns403()
    {
        // Arrange
        await PayForAdminPlanAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
                new TenantQuotaSettingsRequestDto(TenantId, 1_048_576),
                TestContext.Current.CancellationToken));

        // Assert: the portal-wide quota exists on a Standalone installation only; a hosted portal is refused with
        // a NotSupportedException, which answers 415 Unsupported Media Type instead of a refusal status (Q3).
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [Trait("Category", "Bug")]
    [InlineData("roomquotasettings")]
    [InlineData("aiagentquotasettings")]
    public async Task SaveEntityDefaultQuota_AboveTariffTotalStorage_Returns400(string setting)
    {
        // Arrange
        await PayForAdminPlanAsync();

        // Act
        var status = await SendQuotaSettingAsync(setting, AboveTotalStorage);

        // Assert: the sibling user default answers 400 for the same condition; these two throw a plain
        // Exception, which the middleware reports as 500 (Q1).
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveUserQuotaSettings_AboveTariffTotalStorage_Returns400()
    {
        // Arrange
        await PayForAdminPlanAsync();

        // Act
        var status = await SendQuotaSettingAsync("userquotasettings", AboveTotalStorage);

        // Assert
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_PaidPlanWithStatistic_StoresTheDefault()
    {
        // Arrange
        await PayForAdminPlanAsync();

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(1_048_576)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.Response.EnableQuota.Should().BeTrue();
        settings.Response.DefaultQuota.Should().Be(1_048_576);
    }

    [Fact]
    public async Task AddMember_RoomAdminAboveStartupAdminCount_IsCreatedAsUser()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = new List<EmployeeFullDto>();

        // Act: four room admins on a plan of three administrators
        for (var i = 0; i < 4; i++)
        {
            var member = Initializer.FakerMember.Generate();
            created.Add((await _profilesApi.AddMemberAsync(new MemberRequestDto
            {
                CultureName = "en-US",
                Email = member.Email,
                Password = member.Password,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Type = EmployeeType.RoomAdmin
            }, TestContext.Current.CancellationToken)).Response);
        }

        // Assert: the creation past the count is not refused; the account gets the free User type instead
        created.Should().Contain(m => m.IsRoomAdmin, "the plan has room for some administrators");
        var firstDowngraded = created.FindIndex(m => !m.IsRoomAdmin);
        firstDowngraded.Should().BeGreaterThan(0, "the fourth administrator does not fit three");
        created.Skip(firstDowngraded).Should().OnlyContain(m => !m.IsRoomAdmin && m.IsCollaborator);
    }

    [Fact]
    public async Task InviteUsers_MoreRoomAdminsThanStartupAdminCount_Returns402()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var invitations = Enumerable.Range(0, 4)
            .Select(_ => new UserInvitationRequestDto(EmployeeType.RoomAdmin) { Email = Initializer.FakerMember.Generate().Email })
            .ToList();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto(invitations), TestContext.Current.CancellationToken));

        // Assert: the whole batch is refused, quoting asked / free
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("The number of admins exceeds the limit: 4/");
    }

    private async Task PayForAdminPlanAsync()
    {
        Payments.Paid(Plans.AdminProductId, quantity: 1, DateTime.UtcNow.Date.AddYears(1));
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        var quota = (await _portalQuotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken)).Response;
        quota.Statistic.Should().BeTrue("the admin plan carries the statistic feature");

        await _peopleClient.Authenticate(Owner);
    }

    /// <summary>
    /// Sends one of the quota settings with <paramref name="quota"/> and returns the status. The typed SDK call is used
    /// wherever it can carry the request; raw HTTP only for the user default, which is hidden from the SDK, and for a
    /// room or AI agent default above the int the typed default quota can hold.
    /// </summary>
    private async Task<HttpStatusCode> SendQuotaSettingAsync(string setting, long quota)
    {
        var ct = TestContext.Current.CancellationToken;
        var typedQuota = quota <= int.MaxValue ? new QuotaSettingsRequestDtoDefaultQuota((int)quota) : null;

        try
        {
            switch (setting)
            {
                case "resetquota":
                    await _peopleQuotaApi.ResetUsersQuotaAsync(new UpdateMembersQuotaRequestDto([Owner.Id]), ct);
                    return HttpStatusCode.OK;
                case "tenantquotasettings":
                    await _settingsQuotaApi.SetTenantQuotaSettingsAsync(new TenantQuotaSettingsRequestDto(TenantId, quota), ct);
                    return HttpStatusCode.OK;
                case "roomquotasettings" when typedQuota is not null:
                    await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestDto(true, typedQuota), ct);
                    return HttpStatusCode.OK;
                case "aiagentquotasettings" when typedQuota is not null:
                    await _settingsQuotaApi.SaveAiAgentQuotaSettingsAsync(new QuotaSettingsRequestDto(true, typedQuota), ct);
                    return HttpStatusCode.OK;
            }
        }
        catch (ApiException exception)
        {
            return (HttpStatusCode)exception.ErrorCode;
        }

        using var response = await _webApiClient.PostAsync($"api/2.0/settings/{setting}", Json(new { enableQuota = true, defaultQuota = quota }), ct);

        return response.StatusCode;
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
}
