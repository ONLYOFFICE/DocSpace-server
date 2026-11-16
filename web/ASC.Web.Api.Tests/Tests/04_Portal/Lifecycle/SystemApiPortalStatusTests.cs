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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Lifecycle;

/// <summary>
/// The server-only methods of the system API on this test's own portal: PUT portal/status, GET portal/get and
/// DELETE portal/remove, signed in as the portal's owner. Nothing here touches another portal than the ones the
/// test registers itself.
/// </summary>
[Trait("Category", "Portal")]
public class SystemApiPortalStatusTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    private Task<HttpStatusCode> PortalInformationStatusAsync(HttpStatusCode expected)
    {
        return PollAsync(() => _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken), expected);
    }

    private static int[] TenantIdsOf(JsonElement body)
    {
        return body.GetProperty("tenants").EnumerateArray().Select(t => t.GetProperty("tenantId").GetInt32()).ToArray();
    }

    [Fact]
    public async Task ChangeStatus_Suspended_StopsThePortal_AndActiveBringsItBack()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 1 }, signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("tenant").GetProperty("status").GetString().Should().Be("Suspended");

        (await PortalInformationStatusAsync(HttpStatusCode.NotFound)).Should().Be(HttpStatusCode.NotFound, "a suspended portal answers 404 to getPortalInformation");

        (await PollAsync(() => _commonSettingsApi.GetPortalColorThemeAsync(TestContext.Current.CancellationToken), HttpStatusCode.OK))
            .Should().Be(HttpStatusCode.OK, "the colour theme is allowed while suspended");

        (status, body) = await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 0 }, signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("tenant").GetProperty("status").GetString().Should().Be("Active");

        (await PortalInformationStatusAsync(HttpStatusCode.OK)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeStatus_AnyOtherValue_IsSavedAsSuspended()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 7 }, signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("tenant").GetProperty("status").GetString().Should().Be("Suspended", "the method switches Active and Suspended only");

        (status, _) = await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 0 }, signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeStatus_NoPortal_ReturnsPortalNameEmpty()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Put, "portal/status", new { status = 1 }, signedIn: true);

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().Should().Be("portalNameEmpty");
    }

    [Fact]
    public async Task GetPortals_WithStatistics_ListsTheActivePortalWithOwnerAndWizard()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Get, $"portal/get?tenantId={TenantId}&statistics=true", signedIn: true);

        status.Should().Be(HttpStatusCode.OK);

        var tenant = body.GetProperty("tenants").EnumerateArray().Should().ContainSingle().Subject;

        tenant.GetProperty("tenantId").GetInt32().Should().Be(TenantId);
        tenant.GetProperty("owner").GetProperty("id").GetGuid().Should().Be(Owner.Id);
        tenant.GetProperty("owner").GetProperty("email").GetString().Should().Be(Owner.Email);
        tenant.TryGetProperty("quotaUsage", out _).Should().BeTrue();
        tenant.GetProperty("wizardSettings").GetProperty("completed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetPortals_SuspendedPortal_IsNotListed()
    {
        (await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 1 }, signedIn: true)).Status.Should().Be(HttpStatusCode.OK);

        var (status, body) = await ApiSystemAsync(HttpMethod.Get, $"portal/get?tenantId={TenantId}", signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        TenantIdsOf(body).Should().NotContain(TenantId, "only active portals are listed");

        (await ApiSystemAsync(HttpMethod.Put, "portal/status", new { tenantId = TenantId, status = 0 }, signedIn: true)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Remove_NoPortal_ReturnsPortalNameEmpty()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Delete, "portal/remove", signedIn: true);

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().Should().Be("portalNameEmpty");
    }

    [Fact]
    public async Task Remove_UnknownPortal_ReturnsPortalNameNotFound()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Delete, "portal/remove?tenantId=2000000000", signedIn: true);

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().Should().Be("portalNameNotFound");
    }

    [Fact]
    public async Task Remove_PortalWithCompletedWizard_OnlySendsTheInstructions()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Delete, $"portal/remove?tenantId={TenantId}", signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("removed").GetBoolean().Should().BeFalse("the owner has to confirm the removal through the mailed link");

        (await PortalInformationStatusAsync(HttpStatusCode.OK)).Should().Be(HttpStatusCode.OK, "the portal is still there");
    }

    [Fact]
    public async Task Remove_PortalWithPendingWizard_IsRemovedAtOnce()
    {
        using var wizardPortal = await Fixture.CreatePortalAsync(model => model.Password = null, TestContext.Current.CancellationToken);

        var (status, body) = await ApiSystemAsync(HttpMethod.Delete, $"portal/remove?tenantId={wizardPortal.TenantId}", signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("removed").GetBoolean().Should().BeTrue("a portal whose wizard was never completed goes without confirmation");

        // Not through Web.Api: a server installation answers an unknown address as its default portal.
        (status, body) = await ApiSystemAsync(HttpMethod.Get, $"portal/get?tenantId={wizardPortal.TenantId}", signedIn: true);

        status.Should().Be(HttpStatusCode.OK);
        TenantIdsOf(body).Should().NotContain(wizardPortal.TenantId, "the removed portal is no longer active");
    }
}
