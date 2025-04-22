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
using ASC.Core.Common.Hosting;
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

    private List<TenantWalletQuotaData> _expiredWalletQuotas;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:renewperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);


    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (_expiredWalletQuotas != null && _expiredWalletQuotas.Count > 0)
            {
                await Parallel.ForEachAsync(_expiredWalletQuotas,
                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                    RenewSubscriptionAsync);
            }

            var to = DateTime.UtcNow;
            var from = to.Subtract(ExecuteTaskPeriod);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var coreDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync(stoppingToken);
                _expiredWalletQuotas = await Queries.GetWalletQuotasCloseToExpirationAsync(coreDbContext, from, to).ToListAsync(stoppingToken);
            }

            if (_expiredWalletQuotas.Count > 0)
            {
                logger.InfoRenewSubscriptionServiceFound(_expiredWalletQuotas.Count);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask RenewSubscriptionAsync(TenantWalletQuotaData data, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            _ = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();

            var quantity = new Dictionary<string, int>
            {
                { data.QuotaName, data.Quantity }
            };

            var result = await tariffService.PaymentChangeAsync(data.TenantId, quantity);
            if (result)
            {
                var description = $"{data.QuotaName} {data.Quantity}";
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
    public static readonly Func<CoreDbContext, DateTime, DateTime, IAsyncEnumerable<TenantWalletQuotaData>>
        GetWalletQuotasCloseToExpirationAsync = EF.CompileAsyncQuery(
            (CoreDbContext ctx, DateTime from, DateTime to) =>
                ctx.TariffRows
                    .Join(ctx.Quotas, x => x.Quota, y => y.TenantId, (tariffRow, quota) => new { tariffRow, quota })
                    .Where(r => r.quota.Wallet)
                    .Where(r => r.tariffRow.DueDate.HasValue && r.tariffRow.DueDate >= from && r.tariffRow.DueDate <= to)
                    .Select(r => new TenantWalletQuotaData(r.tariffRow.TenantId, r.tariffRow.Quota, r.quota.Name, r.tariffRow.Quantity, r.tariffRow.DueDate)));
}

public record TenantWalletQuotaData(int TenantId, int QuotaId, string QuotaName, int Quantity, DateTime? DueDate);