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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Quota;

/// <summary>
/// GET /api/2.0/portal/quota — the current portal quota. A freshly registered portal always
/// starts on the free "startup" plan, so the values here are the fixed defaults of that plan
/// rather than something the test computes. Billing is not configured in this environment
/// (payment.url is empty), so the "paid portal" variant of this suite in the TypeScript source
/// — which upgrades the tariff through the real payments.teamlab.info billing service before
/// asserting on the paid plan's numbers — cannot be reproduced here and is dropped.
/// </summary>
[Trait("Category", "Portal")]
public class PortalQuotaTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: these assert the SaaS "startup" tariff seed data, which this
    // integration host (standalone by base-domain, no billing) does not have — the portal runs
    // on the single "default" quota. Re-enable on a SaaS-seeded environment.
    /*
    [Fact]
    public async Task GetPortalQuota_Owner_ReturnsFreeStartupQuota()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var quota = await _portalQuotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertFreeStartupQuota(quota);
    }

    [Fact]
    public async Task GetPortalQuota_DocSpaceAdmin_ReturnsFreeStartupQuota()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var quota = await _portalQuotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertFreeStartupQuota(quota);
    }

    private static void AssertFreeStartupQuota(TenantQuotaWrapper quota)
    {
        quota.StatusCode.Should().Be(200);

        var response = quota.Response;
        response.Should().NotBeNull();
        response.Name.Should().Be("startup");
        response.Price.Should().Be(0);
        response.Visible.Should().BeFalse();
        response.Wallet.Should().BeFalse();
        response.Features.Should().NotBeNullOrEmpty();
        response.MaxTotalSize.Should().Be(2147483648);
        response.CountRoomAdmin.Should().Be(3);
        response.CountRoom.Should().Be(12);
        response.NonProfit.Should().BeFalse();
        response.Trial.Should().BeFalse();
        response.Free.Should().BeTrue();
        response.Update.Should().BeFalse();
        response.Audit.Should().BeFalse();
        response.DocsEdition.Should().BeFalse();
        response.Ldap.Should().BeFalse();
        response.Sso.Should().BeFalse();
        response.Statistic.Should().BeFalse();
        response.Branding.Should().BeFalse();
        response.Customization.Should().BeFalse();
        response.Lifetime.Should().BeFalse();
        response.AutomationApi.Should().BeTrue();
        response.Custom.Should().BeFalse();
        response.Restore.Should().BeFalse();
        response.Oauth.Should().BeTrue();
        response.ContentSearch.Should().BeFalse();
        response.ThirdParty.Should().BeFalse();
        response.Year.Should().BeFalse();
        response.CountFreeBackup.Should().Be(0);
        response.Backup.Should().BeFalse();
        response.AiTools.Should().BeFalse();
    }
    */
}
