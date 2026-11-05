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


namespace ASC.Web.Api.Billing.Tests.Tests.DocsCloud;

/// <summary>
/// The portal's DocsCloud tenant as the DocsCloud service reports it (<c>settings/docscloud/tenant</c>,
/// <c>.../tenant/info</c>, <c>.../tenant/config</c>, <c>.../tenant/quota</c>), asked about under the portal's payment key.
/// </summary>
public class DocsCloudTenantTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The wallet service of DocsCloud in the SaaS catalogue (quota -15).
    private const int DocsCloudProductId = 1014;

    private const string TrialInfo =
        """{"license":{"valid":"2027-01-01T00:00:00Z","trial":true,"buildDate":"2026-10-01T00:00:00Z"},"server":{"version":"9.1.0","packageType":"Developer"},"usersLimit":{"edit":20,"view":40}}""";

    [Fact]
    public async Task GetTenant_ActivatedTenant_ReturnsItAskedForThePortal()
    {
        // Arrange
        Payments.On(PaymentService.DocsCloud, "GET", "/tenant",
            """{"dedicatedResourceExId":7,"alias":"docs-portal","name":"Docs portal","isActive":true,"address":"https://docs-portal.example.com","endDate":"2027-01-01T00:00:00Z"}""");

        // Act
        var tenant = (await _docsCloudApi.GetTenantAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tenant.Alias.Should().Be("docs-portal");
        tenant.IsActive.Should().BeTrue();
        tenant.Address.Should().Be("https://docs-portal.example.com");

        var request = PaymentCalls.Should().ContainSingle(call => call.Service == PaymentService.DocsCloud && call.Route == "/tenant").Subject;
        HttpUtility.ParseQueryString(request.Query)["portalId"].Should().Be(Payments.PortalId);
    }

    [Fact]
    public async Task GetTenant_NeverActivated_ReturnsNothing()
    {
        // Act
        var tenant = (await _docsCloudApi.GetTenantAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tenant.Should().BeNull();
    }

    [Fact]
    public async Task GetTenantInfo_TrialLicense_IsShownAsTrial()
    {
        // Arrange
        Payments.On(PaymentService.DocsCloud, "GET", "/tenant/info", TrialInfo);

        // Act
        var info = (await _docsCloudApi.GetTenantInfoAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.License.Trial.Should().BeTrue();
        info.UsersLimit.Edit.Should().Be(20);
    }

    [Fact]
    public async Task GetTenantInfo_TrialLicenseOfAPortalPayingForDocsCloud_IsNotShownAsTrial()
    {
        // Arrange
        Payments.Paid(DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1))
            .On(PaymentService.DocsCloud, "GET", "/tenant/info", TrialInfo);
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var info = (await _docsCloudApi.GetTenantInfoAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.License.Trial.Should().BeFalse("the DocsCloud service still has the trial licence of a portal that has paid since");
    }

    [Fact]
    [Trait("Bug", "83321")]
    public async Task GetTenantInfo_NeverActivated_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.GetTenantInfoAsync(true, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateTenantConfig_ChangesTheConfigTheNextReadReturns()
    {
        // Arrange
        var stored = """{"tenantName":"Docs portal","wopi":{"enable":false}}""";
        Payments
            .On(PaymentService.DocsCloud, "GET", "/tenant/config", _ => PaymentServiceAnswer.Json(stored))
            .On(PaymentService.DocsCloud, "PUT", "/tenant/config", call =>
            {
                stored = call.Body!;
                return PaymentServiceAnswer.Json(stored);
            });

        var before = (await _docsCloudApi.GetTenantConfigAsync(true, TestContext.Current.CancellationToken)).Response;

        // Act
        var updated = (await _docsCloudApi.UpdateTenantConfigAsync(
            new DocsCloudConfigRequestDto(tenantName: "Docs portal", wopi: new DocsCloudWopiConfigRequest(true)),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        before.Wopi.Enable.Should().BeFalse();
        updated.Wopi.Enable.Should().BeTrue();

        // The cached config is dropped with the change, so a read without refresh already sees it.
        var after = (await _docsCloudApi.GetTenantConfigAsync(false, TestContext.Current.CancellationToken)).Response;
        after.Wopi.Enable.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantQuota_ReturnsTheEditorsAndViewersOfThePeriod()
    {
        // Arrange
        Payments.On(PaymentService.DocsCloud, "GET", "/tenant/quota",
            """{"users":[{"userId":"u1","expire":"2026-12-01"},{"userId":"u2","expire":"2026-12-02"}],"usersView":[{"userId":"v1","expire":"2026-12-03"}]}""");

        // Act
        var quota = (await _docsCloudApi.GetTenantQuotaAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        quota.Users.Select(user => user.UserId).Should().Equal("u1", "u2");
        quota.UsersView.Select(user => user.UserId).Should().Equal("v1");
    }
}
