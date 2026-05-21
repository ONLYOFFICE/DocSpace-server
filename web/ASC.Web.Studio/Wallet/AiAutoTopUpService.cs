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
public class AiAutoTopUpService(
        IServiceScopeFactory scopeFactory,
        ILogger<AiAutoTopUpService> logger,
        IConfiguration configuration)
    : ActivePassiveBackgroundService<AiAutoTopUpService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:aiautotopupperiod"] ?? "0:10:0", CultureInfo.InvariantCulture);

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<TenantAiAutoTopUpSettingsData> activeTenants;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var webstudioDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebstudioDbContext>>().CreateDbContextAsync(stoppingToken);
                activeTenants = await Queries.GetTenantAiAutoTopUpSettingsAsync(webstudioDbContext, TenantAiAutoTopUpSettings.ID).ToListAsync(stoppingToken);
            }

            if (activeTenants.Count == 0)
            {
                return;
            }

            logger.InfoAiAutoTopUpFound(activeTenants.Count);

            await Parallel.ForEachAsync(activeTenants,
                new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken },
                AutoTopUpAiAsync);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask AutoTopUpAiAsync(TenantAiAutoTopUpSettingsData data, CancellationToken cancellationToken)
    {
        UserInfo payer = null;
        UserInfo owner = null;
        TenantAiAutoTopUpSettings settings = null;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return;
            }

            settings = JsonSerializer.Deserialize<TenantAiAutoTopUpSettings>(data.Setting, _options);
            if (!settings.Enabled)
            {
                return;
            }

            if (settings.LastTopUp != default && DateTime.UtcNow < TenantAiAutoTopUpSettings.GetNextTopUpDate(settings.LastTopUp, settings.Period))
            {
                return;
            }

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

            var aiBalance = await tariffService.GetCustomerAiBalanceAsync(data.TenantId, true);
            if (aiBalance == null)
            {
                logger.ErrorAiAutoTopUpBalanceNull(data.TenantId);
                return;
            }

            var aiSubAccount = aiBalance.SubAccounts.FirstOrDefault(x => x.Currency == settings.Currency);
            if (aiSubAccount == null || aiSubAccount.Amount >= settings.Amount)
            {
                return;
            }

            var amount = settings.Amount - aiSubAccount.Amount;

            var mainBalance = await tariffService.GetCustomerBalanceAsync(data.TenantId, true);
            var mainSubAccount = mainBalance?.SubAccounts?.FirstOrDefault(x => x.Currency == settings.Currency);
            if (mainSubAccount != null && mainSubAccount.Amount >= amount)
            {
                var customerParticipantName = securityContext.CurrentAccount.ID.ToString();
                var metadata = new Dictionary<string, string> { { BillingClient.MetadataDetails, Resource.AutoTopUp } };

                var result = await tariffService.MakeAiCreditAsync(data.TenantId, amount, settings.Currency, customerParticipantName, metadata);

                if (result != null)
                {
                    settings.LastTopUp = DateTime.UtcNow;
                    var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
                    await settingsManager.SaveAsync(settings, data.TenantId);

                    var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                    var description = $"{amount} {settings.Currency}";
                    messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerAiToppedUp, description);

                    var socketManager = scope.ServiceProvider.GetRequiredService<QuotaSocketManager>();
                    await socketManager.TopUpAiAsync(true);

                    logger.InfoAiAutoTopUpDone(data.TenantId, description);

                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        await SendAiAutoTopUpErrorAsync(data.TenantId, payer, owner, settings);
    }

    private async Task SendAiAutoTopUpErrorAsync(int tenantId, UserInfo payer, UserInfo owner, TenantAiAutoTopUpSettings settings)
    {
        try
        {
            logger.ErrorAiAutoTopUpFail(tenantId);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(tenantId, owner.Id);

            var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();
            await studioNotifyService.SendAiAutoTopUpErrorAsync(payer, owner);

            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();

            settings ??= new TenantAiAutoTopUpSettings();
            settings.Enabled = false;
            await settingsManager.SaveAsync(settings, tenantId);

            messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerAiAutoTopUpSettingsUpdated);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }
}

static file class Queries
{
    public static readonly Func<WebstudioDbContext, Guid, IAsyncEnumerable<TenantAiAutoTopUpSettingsData>>
        GetTenantAiAutoTopUpSettingsAsync = EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, Guid id) =>
                ctx.WebstudioSettings
                   .Join(ctx.Tenants, x => x.TenantId, y => y.Id, (settings, tenants) => new { settings, tenants })
                   .Where(x => x.tenants.Status == TenantStatus.Active && x.settings.Id == id && x.settings.Data.Contains("\"Enabled\":true"))
                   .Select(r => new TenantAiAutoTopUpSettingsData(r.tenants.Id, r.settings.Data)));
}

public record TenantAiAutoTopUpSettingsData(int TenantId, string Setting);
