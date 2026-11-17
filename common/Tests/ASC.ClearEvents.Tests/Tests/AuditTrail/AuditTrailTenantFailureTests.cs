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

namespace ASC.ClearEvents.Tests.Tests.AuditTrail;

/// <summary>A tenant a pass fails on is logged and skipped; the tenants after it are still cleared.</summary>
public class AuditTrailTenantFailureTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ClearAsync_TariffOfOneTenantFails_ClearsTheOthers()
    {
        // Arrange: a pass visits the tenants by id, so the portal of this test, registered first, is visited before
        // the one registered here.
        using var laterPortal = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        TestTariffs.Fail(TenantId);

        var failingPortalEvent = await SeedEventAsync(PaidLifeTimeDays + 30);
        var laterPortalEvent = await SeedEventForTenantAsync(laterPortal.TenantId, PaidLifeTimeDays + 30);

        // Its last activity, which a pass keeps: the expired event is then not the newest of the portal.
        await SeedEventForTenantAsync(laterPortal.TenantId, 0);

        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(), TestContext.Current.CancellationToken);

        // Assert
        laterPortal.TenantId.Should().BeGreaterThan(TenantId);

        (await RemainingEventsAsync([failingPortalEvent, laterPortalEvent])).Should().BeEquivalentTo([failingPortalEvent]);

        logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning && e.Exception is InvalidOperationException && Equals(e.Values.GetValueOrDefault("tenantId"), TenantId));
    }
}
