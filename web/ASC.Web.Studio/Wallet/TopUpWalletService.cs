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

using System.Globalization;

using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Common.EF.Context;
using ASC.Core.Common.Hosting;
using ASC.Core.Common.Quota;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.MessagingSystem.Core;
using ASC.Web.Core.PublicResources;
using ASC.Web.Studio.Core.Notify;

using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class TopUpWalletService(
        IServiceScopeFactory scopeFactory,
        ILogger<TopUpWalletService> logger,
        IConfiguration configuration,
        TenantWalletSettingsConfig walletSettingsConfig)
    : ActivePassiveBackgroundService<TopUpWalletService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:topupperiod"] ?? "0:3:0", CultureInfo.InvariantCulture);


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
                activeTenants = await Queries.GetTenantWalletSettingsAsync(webstudioDbContext, TenantWalletSettings.ID).ToListAsync(stoppingToken);
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
        UserInfo payer = null;
        UserInfo owner = null;
        TenantWalletSettings settings = null;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return;
            }

            settings = JsonSerializer.Deserialize<TenantWalletSettings>(data.Setting, _options);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(tenant.OwnerId);

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            var customerInfo = await tariffService.GetCustomerInfoAsync(data.TenantId);
            if (!string.IsNullOrEmpty(customerInfo?.Email))
            {
                payer = await userManager.GetUserByEmailAsync(customerInfo.Email);
            }

            if (payer != null && payer.Id != ASC.Core.Users.Constants.LostUser.Id)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, payer.Id);
            }
            else
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, owner.Id);
            }

            var balance = await tariffService.GetCustomerBalanceAsync(data.TenantId, true);
            if (balance == null)
            {
                logger.Error($"TopUpWalletService: balance is null for tenant {data.TenantId}");
                return;
            }

            // one balance read per tenant per cycle: auto top-up and low-balance notification are
            // mutually exclusive branches on the same snapshot, so they can never race each other
            if (!settings.Enabled)
            {
                await CheckLowBalanceAsync(scope, data.TenantId, settings, balance, payer, owner);
                return;
            }

            var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == settings.Currency);
            if (subAccount == null || subAccount.Amount >= settings.MinBalance)
            {
                return;
            }

            var truncated = Math.Truncate(subAccount.Amount * 100) / 100; // Truncate to 2 decimal places
            var amount = settings.UpToBalance - truncated;

            var metadata = new Dictionary<string, string> { { BillingClient.MetadataDetails, Resource.AutoTopUp } };

            var coreSettings = scope.ServiceProvider.GetRequiredService<CoreSettings>();

            var siteName = tenant.GetTenantDomain(coreSettings);

            var result = await tariffService.TopUpDepositAsync(data.TenantId, amount, settings.Currency, null, siteName, metadata, true);

            if (result)
            {
                var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                var description = $"{amount} {settings.Currency}";
                messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerWalletToppedUp, description);

                var socketManager = scope.ServiceProvider.GetRequiredService<QuotaSocketManager>();
                await socketManager.TopUpWallet(true);

                logger.InfoTopUpWalletServiceDone(data.TenantId, description);

                return;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        await SendTopUpWalletErrorAsync(data.TenantId, payer, owner, settings);
    }

    // recovery must clear at a higher level than the drop threshold, otherwise a balance oscillating
    // right at the threshold would re-notify on every cycle
    private const decimal LowBalanceRecoveryMultiplier = 2m;


    // isolated from TopUpWalletAsync's try/catch on purpose: a failure here must never fall through to
    // SendTopUpWalletErrorAsync, which sends an "auto top-up failed" email that makes no sense for a
    // tenant who never enabled auto top-up in the first place
    private async Task CheckLowBalanceAsync(AsyncServiceScope scope, int tenantId, TenantWalletSettings settings, Balance balance, UserInfo payer, UserInfo owner)
    {
        try
        {
            if (settings.LowBalanceThreshold <= 0)
            {
                return;
            }

            var currency = string.IsNullOrEmpty(settings.Currency) ? "USD" : settings.Currency;
            var subAccount = balance.SubAccounts?.FirstOrDefault(x => x.Currency == currency);
            if (subAccount == null)
            {
                return;
            }

            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();

            if (subAccount.Amount < settings.LowBalanceThreshold)
            {
                if (settings.LowBalanceNotified)
                {
                    return;
                }

                var socketManager = scope.ServiceProvider.GetRequiredService<QuotaSocketManager>();
                await socketManager.WalletLowBalanceAsync();

                var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();
                await studioNotifyService.SendLowWalletBalanceAsync(payer, owner);

                settings.LowBalanceNotified = true;
                await settingsManager.SaveAsync(settings, tenantId);
            }
            else if (settings.LowBalanceNotified && subAccount.Amount >= settings.LowBalanceThreshold * LowBalanceRecoveryMultiplier)
            {
                settings.LowBalanceNotified = false;
                await settingsManager.SaveAsync(settings, tenantId);
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task SendTopUpWalletErrorAsync(int tenantId, UserInfo payer, UserInfo owner, TenantWalletSettings settings)
    {
        try
        {
            logger.ErrorTopUpWalletServiceFail(tenantId);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(tenantId, owner.Id);

            var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();
            await studioNotifyService.SendTopUpWalletErrorAsync(payer, owner);

            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();

            settings ??= new TenantWalletSettings();
            settings.Enabled = false;
            // auto top-up just got disabled: fall back to low-balance monitoring instead of going dark
            settings.LowBalanceThreshold = walletSettingsConfig.LowBalanceThreshold;
            await settingsManager.SaveAsync(settings, tenantId);

            messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerWalletTopUpSettingsUpdated);
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
