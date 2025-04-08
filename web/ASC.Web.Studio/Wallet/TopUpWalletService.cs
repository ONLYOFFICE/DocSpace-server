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
using ASC.Core.Common.EF.Context;
using ASC.Core.Common.Hosting;
using ASC.Core.Tenants;
using ASC.MessagingSystem.Core;

using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class TopUpWalletService(
        IServiceScopeFactory scopeFactory,
        ILogger<TopUpWalletService> logger,
        IConfiguration configuration)
    : ActivePassiveBackgroundService<TopUpWalletService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:topupperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);


    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<TenantWalletSettingsData> activeTenants;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var webstudioDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebstudioDbContext>>().CreateDbContextAsync(stoppingToken);
                activeTenants = await Queries.GetTenantWalletSettingsAsync(webstudioDbContext, new TenantWalletSettings().ID).ToListAsync(stoppingToken);
            }

            if (activeTenants.Count == 0)
            {
                return;
            }

            logger.InfoTopUpWalletServiceFound(activeTenants.Count);

            await Parallel.ForEachAsync(activeTenants,
                new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                TopUpWalletAsync);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask TopUpWalletAsync(TenantWalletSettingsData data, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(data.TenantId);

            var settings = JsonSerializer.Deserialize< TenantWalletSettings>(data.Setting, _options);
            if (!settings.Enabled)
            {
                return;
            }

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var balance = await tariffService.GetCustomerBalanceAsync(data.TenantId);
            var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == settings.Currency);
            if (subAccount == null || subAccount.Amount >= settings.MinBalance)
            {
                return;
            }

            var amount = (long)(settings.UpToBalance - subAccount.Amount);
            await tariffService.PutOnDepositAsync(data.TenantId, amount, settings.Currency);

            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
            var description = $"{amount} {settings.Currency}";
            messageService.Send(MessageInitiator.System, MessageAction.CustomerWalletToppedUp, description);

            logger.InfoTopUpWalletServiceDone(data.TenantId, description);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }
}

static file class Queries
{
    public static readonly Func<WebstudioDbContext, Guid, IAsyncEnumerable<TenantWalletSettingsData>>
        GetTenantWalletSettingsAsync = EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, Guid id) =>
                ctx.WebstudioSettings
                   .Join(ctx.Tenants, x => x.TenantId, y => y.Id, (settings, tenants) => new { settings, tenants })
                   .Where(x => x.tenants.Status == TenantStatus.Active && x.settings.Id == id)
                   .Select(r => new TenantWalletSettingsData(r.tenants.Id, r.settings.Data)));
}

public record TenantWalletSettingsData(int TenantId, string Setting);