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

/// <summary>
/// A pass with the real <see cref="ITariffService"/> rather than <see cref="TestTariffs"/>. A pass logs and skips a
/// tenant it fails on, so a tariff service that does not resolve in this service graph, or fails to answer, would
/// leave every event in place with nothing but a warning per tenant, while every test on the stand-in stayed green.
/// </summary>
public class AuditTrailRealTariffTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ClearAsync_RealTariffService_ClearsThePortal()
    {
        // Arrange: past both life times, so it goes whatever tariff the installation has.
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        var host = await Fixture.RealTariffCleanerAsync();
        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await host.CreateCleaner(logger).ClearAsync(Retention(), TestContext.Current.CancellationToken);

        // Assert: the pass walks every portal of the database, and a warning about another one says nothing about the
        // tariff service. What is checked is this portal's and the settings', the warnings that name no portal.
        var warnings = logger.Entries.Where(e => e.Level >= LogLevel.Warning).ToList();

        warnings.Should().NotContain(e => !e.Has("tenantId"), "the settings of the pass are valid");
        warnings.Should().NotContain(e => Equals(e.Values.GetValueOrDefault("tenantId"), TenantId), "the real tariff service has to answer for this portal");
        (await RemainingEventsAsync([expired])).Should().BeEmpty();
    }
}
