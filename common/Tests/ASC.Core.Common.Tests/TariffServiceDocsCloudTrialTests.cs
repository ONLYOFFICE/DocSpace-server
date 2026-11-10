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


namespace ASC.Core.Common.Tests;

/// <summary>
/// The Docs Connect trial through <see cref="TariffService"/>: what the caches hold once billing has been asked for it.
/// </summary>
public class TariffServiceDocsCloudTrialTests
{
    private const int TenantId = 1;

    [Fact]
    public async Task GetDocsCloudTrial_AnswerUnreadable_ClearsTheCachedTariff()
    {
        // A proxy's page in place of billing's "ok" says nothing about the trial: it may have been granted. The tariff
        // cached before it would then hide the new quota, and a retry would pass the check that the trial is not set.
        using var billing = new HttpStub(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));
        var (billingClient, _) = CreateBillingClient(billing);
        var (docsCloudClient, _) = CreateDocsCloudClient(HttpStub.Unreachable());

        var hybridCache = new FusionCache(new FusionCacheOptions());
        await hybridCache.SetAsync(TariffService.GetTariffCacheKey(TenantId), "the tariff before the trial", TimeSpan.FromMinutes(5),
            token: TestContext.Current.CancellationToken);

        var tariffService = PaymentStubs.CreateTariffService(billingClient: billingClient, hybridCache: hybridCache,
            coreSettings: CreateCoreSettings("portal-"), docsCloudClient: docsCloudClient);

        var granted = await tariffService.GetDocsCloudTrialAsync(TenantId,
            new DocsCloudTrialCustomer("owner@example.com", "https://portal.example.com", "Owner", null, "en-US", null));

        granted.Should().BeFalse("whether the trial was granted is unknown");
        billing.Requests.Should().ContainSingle(request => request.Path == "/billing/getwdocstrial");
        (await hybridCache.TryGetAsync<string>(TariffService.GetTariffCacheKey(TenantId), token: TestContext.Current.CancellationToken))
            .HasValue.Should().BeFalse("the tariff is read again from billing after an answer that may hide a granted trial");
    }
}
