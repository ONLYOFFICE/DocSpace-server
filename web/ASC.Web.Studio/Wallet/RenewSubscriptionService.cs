// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Globalization;

using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Common.EF;
using ASC.Core.Common.Hosting;
using ASC.Core.Tenants;
using ASC.MessagingSystem.Core;

using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class RenewSubscriptionService(
        IServiceScopeFactory scopeFactory,
        ILogger<RenewSubscriptionService> logger,
        IConfiguration configuration)
    : ActivePassiveBackgroundService<RenewSubscriptionService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private Dictionary<int, string> _walletQuotas;
    private List<DbTariffRow> _closeToExpirationWalletQuotas;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:renewperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);


    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (_closeToExpirationWalletQuotas != null && _closeToExpirationWalletQuotas.Count > 0)
            {
                var expiredWalletQuotas = _closeToExpirationWalletQuotas.Where(x => x.DueDate < DateTime.UtcNow).ToList();

                await Parallel.ForEachAsync(expiredWalletQuotas,
                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                    RenewSubscriptionAsync);
            }

            var now = DateTime.UtcNow;
            var from = now.Subtract(ExecuteTaskPeriod * 3);
            var to = now.Add(ExecuteTaskPeriod * 3);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var coreDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync(stoppingToken);

                if (_walletQuotas == null)
                {
                    _walletQuotas = await Queries.GetWalletQuotasAsync(coreDbContext).ToDictionaryAsync(x => x.Key, x => x.Value, stoppingToken);
                }

                _closeToExpirationWalletQuotas = await Queries.GetWalletQuotasCloseToExpirationAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);
            }

            if (_closeToExpirationWalletQuotas.Count > 0)
            {
                logger.InfoRenewSubscriptionServiceFound(_closeToExpirationWalletQuotas.Count);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask RenewSubscriptionAsync(DbTariffRow data, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return;
            }

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();

            var quotaName = _walletQuotas[data.Quota];

            var quantity = new Dictionary<string, int>
            {
                { quotaName, data.Quantity }
            };

            var result = await tariffService.PaymentChangeAsync(data.TenantId, quantity, BillingClient.ProductQuantityType.Set);
            if (result)
            {
                var description = $"{quotaName} {data.Quantity}";
                var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                messageService.Send(MessageInitiator.System, MessageAction.CustomerSubscriptionUpdated, description);

                logger.InfoRenewSubscriptionServiceDone(data.TenantId, description);
            }
            else
            {
                logger.ErrorRenewSubscriptionServiceFail(data.TenantId);
            }
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceFail(data.TenantId);
            logger.ErrorWithException(ex);
        }
    }
}

static file class Queries
{
    public static readonly Func<CoreDbContext, int[], DateTime, DateTime, IAsyncEnumerable<DbTariffRow>>
        GetWalletQuotasCloseToExpirationAsync = EF.CompileAsyncQuery(
            (CoreDbContext ctx, int[] quotas, DateTime from, DateTime to) =>
                ctx.TariffRows
                    .Join(
                        ctx.Tenants.Where(t => t.Status == TenantStatus.Active),
                        tariffRow => tariffRow.TenantId,
                        tenant => tenant.Id,
                        (tariffRow, tenant) => tariffRow
                    )
                    .GroupBy(tariffRow => tariffRow.TenantId)
                    .Select(group => new {
                        TenantId = group.Key,
                        MaxTariffId = group.Max(tariffRow => tariffRow.TariffId)
                    })
                    .Join(
                        ctx.TariffRows,
                        x => new { TenantId = x.TenantId, MaxTariffId = x.MaxTariffId },
                        tariffRow => new { TenantId = tariffRow.TenantId, MaxTariffId = tariffRow.TariffId },
                        (x, tariffRow) => tariffRow
                    )
                    .Where(r => quotas.Contains(r.Quota) && r.DueDate.HasValue && r.DueDate > from && r.DueDate < to)
            );

    public static readonly Func<CoreDbContext, IAsyncEnumerable<KeyValuePair<int, string>>>
        GetWalletQuotasAsync = EF.CompileAsyncQuery(
            (CoreDbContext ctx) =>
                ctx.Quotas
                    .Where(r => r.Wallet)
                    .Select(r => new KeyValuePair<int, string>(r.TenantId, r.Name)));
}