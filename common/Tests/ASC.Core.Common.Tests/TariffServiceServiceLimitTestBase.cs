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
/// The setup shared by the service limit suites of <see cref="TariffService"/>: a service over a faked accounting
/// service, and the answers it gives.
/// </summary>
/// <remarks>
/// The tenant's balance is seeded into the cache under the key <see cref="TariffService"/> reads it from, so the
/// balance lookup never reaches the portal key, the distributed lock or the accounting service. The portal key comes
/// from a real <see cref="CoreSettings"/> over a tenant service that knows no tenants, and every other dependency of
/// <see cref="TariffService"/> is unused on these paths and passed as null.
/// </remarks>
public abstract class TariffServiceServiceLimitTestBase
{
    protected const int TenantId = 1;
    protected const int OwnAccountNumber = 1001;
    protected const int OtherAccountNumber = 2002;
    protected const string PaymentRegion = "portal-";
    protected const int TotalParticipantLimits = 3;
    protected const string UserA = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    protected const string UserB = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
    protected const string UserC = "16fd2706-8baf-433b-82eb-8c7fada847da";

    // How the accounting service answers an update or a deletion of a limit it does not have.
    protected const string ServiceLimitNotFoundBody =
        """{"title":"Bad request","status":400,"detail":"Service limit '42' not found","instance":"/api/serviceLimit"}""";

    private protected static async Task<(TariffService tariffService, AccountingClientTests.CapturingHandler handler)> CreateTariffServiceAsync(
        Balance balance, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var (accountingClient, handler) = AccountingClientTests.CreateClient(responder);

        var hybridCache = new FusionCache(new FusionCacheOptions());
        await hybridCache.SetAsync(TariffService.GetAccountingBalanceCacheKey(TenantId), balance, TimeSpan.FromMinutes(5),
            token: TestContext.Current.CancellationToken);

        // A SaaS portal unknown to the tenant service gets the portal ID "<payment region><tenant ID>".
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:payment:region"] = PaymentRegion })
            .Build();
        var coreSettings = new CoreSettings(DefaultResultProxy<ITenantService>.Create(), new CoreBaseSettings(configuration), configuration, null!);

        var tariffService = new TariffService(
            quotaService: null!,
            tenantService: null!,
            coreBaseSettings: null!,
            coreSettings: coreSettings,
            configuration: null!,
            coreDbContextManager: null!,
            cache: null!,
            hybridCache: hybridCache,
            distributedLockProvider: null!,
            logger: NullLogger<TariffService>.Instance,
            billingClient: null!,
            accountingClient: accountingClient,
            docsCloudClient: null!,
            serviceProvider: null!,
            resiliencePipelineProvider: null!,
            tenantExtraConfig: null!);

        return (tariffService, handler);
    }

    // A body that takes its time to arrive: the client waits for it asynchronously, as it does for a hanging service.
    protected sealed class DelayedContent(string json, TimeSpan delay) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await Task.Delay(delay);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(json));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    protected static HttpResponseMessage ParticipantLimitsPage(params string[] participants)
    {
        var limits = participants.Select((participant, index) =>
            $$"""{"id":{{100 + index}},"serviceGroupName":"ai-services","scope":"Custom","customerAccountNumber":{{OwnAccountNumber}},"participant":"{{participant}}","amountValue":40,"currency":"USD","period":"Day","enabled":true}""");

        return AccountingClientTests.Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", limits)}}],"totalQuantity":{{TotalParticipantLimits}}}""");
    }

    protected static bool IsBalanceRequest(HttpRequestMessage request)
    {
        return request.RequestUri!.AbsolutePath.EndsWith("/balance", StringComparison.Ordinal);
    }

    protected static bool IsGlobalRequest(HttpRequestMessage request)
    {
        return request.RequestUri!.AbsolutePath.EndsWith("/global", StringComparison.Ordinal);
    }

    protected static HttpResponseMessage GlobalLimitResponse(bool enabled)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":43,"serviceGroupName":"ai-services","customerAccountNumber":{{OwnAccountNumber}},"scope":"Global","amountValue":50,"currency":"USD","period":"Day","enabled":{{(enabled ? "true" : "false")}}}""");
    }

    protected static string Row(int id, string scope, string participant, bool enabled = true)
    {
        return $$$"""{"id":{{{id}}},"serviceGroupName":"ai-services","customerAccountNumber":{{{OwnAccountNumber}}},"scope":"{{{scope}}}","participant":"{{{participant}}}","amountValue":40,"currency":"USD","period":"Day","enabled":{{{(enabled ? "true" : "false")}}},"usage":{"periodStart":"2026-10-18T00:00:00Z","periodEnd":"2026-10-18T23:59:59Z","amountConsumed":12.5,"amountAvailable":27.5,"quantityConsumed":0}}""";
    }

    // One page holding every row, so the paged read stops after it.
    protected static HttpResponseMessage ParticipantRows(params string[] rows)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", rows)}}],"totalQuantity":{{rows.Length}}}""");
    }

    protected static HttpResponseMessage ServiceLimitResponse(int customerAccountNumber)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":42,"serviceGroupName":"ai-services","scope":"Custom","customerAccountNumber":{{customerAccountNumber}},"amountValue":50,"currency":"USD","period":"Day","enabled":true}""");
    }
}
