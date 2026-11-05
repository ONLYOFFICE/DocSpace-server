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

using ASC.Files.Core.ApiModels.ResponseDto;
using ASC.Files.Core.IntegrationEvents.Events;
using ASC.Files.Core.Services.DocumentBuilderService;

using Microsoft.AspNetCore.RateLimiting;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// The portal itself and the money behind it: its identity and its used space, the invitation links it issues, the
/// tariff and quota it runs on, and - under `payment` - buying and changing its paid plan, the prepaid wallet the
/// pay-as-you-go services are charged against, the billing customer with its payment method, the balance, the usage
/// and the operation history with their downloadable reports, and the settings that govern automatic top-up, wallet
/// services and AI model restrictions. Two callers are told apart throughout the payment operations: a DocSpace
/// administrator, who may read the billing state of the portal, and the payer - the portal user whose e-mail is the
/// billing customer's e-mail - who alone may move money. They all keep working while the portal's own payment has
/// lapsed, which is what lets a locked-out portal pay its way back in, and where the installation has no billing
/// service configured at all, the operations that need one answer 403 rather than reporting the service as missing.
/// </remarks>
///<name>portal</name>
[Scope]
[ApiEndpoint("portal", "payment")]
[AllowNotPayment]
public class PaymentController(
    CoreSettings coreSettings,
    UserManager userManager,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    ITariffService tariffService,
    IQuotaService quotaService,
    SecurityContext securityContext,
    RegionHelper regionHelper,
    QuotaHelper tariffHelper,
    IConfiguration configuration,
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    PermissionContext permissionContext,
    TenantUtil tenantUtil,
    AiGateway aiGateway,
    ApiDateTimeHelper apiDateTimeHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IEventBus eventBus,
    CommonLinkUtility commonLinkUtility,
    DocumentBuilderTaskManager<CustomerOperationsReportTask, int, CustomerOperationsReportTaskData> documentBuilderTaskManager,
    IServiceProvider serviceProvider,
    PaymentHelper paymentHelper,
    QuotaSocketManager quotaSocketManager,
    DepositDtoMapper depositDtoMapper)
    : ControllerBase
{
    /// <remarks>
    /// Starts the purchase of a monthly paid plan for the portal and returns the URL of the hosted checkout page.
    /// Nothing is bought until the buyer completes that page. The plan becomes active after the payment provider
    /// confirms the payment. Requires a DocSpace admin. Other callers get 403. It also returns 403 if the
    /// installation has no billing service. The call itself is read-only, so you can repeat it. If the portal already
    /// has a paid plan, the call returns an empty result. To change a paid plan, use
    /// `PUT api/2.0/portal/payment/update`. The URL carries the email of the caller and the language of the request.
    /// It also carries a currency that the call picks from the IP address of the caller. The page redirects to
    /// `successUrl` or `backUrl` when the buyer finishes or cancels. Take a monthly plan for `quantity` from
    /// `GET api/2.0/portal/payment/quotas`. The call returns 400 if `quantity` holds more than one product or a
    /// quantity of zero or less. It also returns 400 for a yearly plan or a wallet service. To buy wallet services,
    /// use `PUT api/2.0/portal/payment/updatewallet`. The call returns 402 if the portal already exceeds a limit of
    /// the requested plan. These limits cover admins, users, rooms, AI agents, and storage.
    /// </remarks>
    /// <summary>
    /// Get the payment page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/url</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The absolute URL of the checkout page to open, or an empty result when the portal already has a paid plan", typeof(Uri))]
    [SwaggerResponse(400, "The request body cannot be read or has no `backUrl`, `successUrl` or `quantity`, `backUrl` or `successUrl` does not start with `http://`, `https://` or `ftp://` or is longer than 255 characters, or `quantity` does not hold exactly one product, holds a quantity that is not greater than zero, or a product that is not a monthly plan")]
    [SwaggerResponse(402, "The portal already has more administrators, users, rooms or AI agents, or stores more data, than the plan in the requested quantity allows")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrl(PaymentUrlRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        ArgumentNullException.ThrowIfNull(inDto);

        if (inDto.Quantity.Any(item => item.Value <= 0))
        {
            throw new ArgumentException("Invalid quantity");
        }

        var tenant = tenantManager.GetCurrentTenant();
        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            var tariff = await tariffService.GetTariffAsync(tenant.Id);
            if (tariff.State == TariffState.Paid)
            {
                return null;
            }
        }

        var monthQuotas = (await quotaService.GetTenantQuotasAsync())
            .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible && !q.Wallet && !q.Year)
            .ToList();

        // Only monthly tariff available for purchase.
        if (monthQuotas.All(q => q.Name != inDto.Quantity.First().Key))
        {
            throw new ArgumentException("Only monthly product can be purchased per transaction");
        }

        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        return await tariffService.GetShoppingUriAsync(
            tenant.Id,
            tenant.AffiliateId,
            tenant.PartnerId,
            currency,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            (await userManager.GetUsersAsync(securityContext.CurrentAccount.ID)).Email,
            inDto.Quantity,
            inDto.BackUrl,
            inDto.SuccessUrl);
    }

    /// <remarks>
    /// Changes the number of admins that the paid plan of the portal covers. The payment provider bills the
    /// difference to the payment method on file. The portal needs a plan bought with
    /// `PUT api/2.0/portal/payment/url`. If the portal has no billing customer, the call returns 404. The payer is
    /// the portal user whose email is the email of the billing customer. Only the payer can call it. Other callers
    /// get 403. The call also returns 403 if the installation has no billing service. The call spends money. A
    /// repeated call with the same quantity returns 400, since that quantity is already in effect. The call returns
    /// `true` if the provider accepted the change and `false` if it declined it. On a priced plan, `quantity` must
    /// name the current plan from `GET api/2.0/portal/payment/quota`. The call returns 400 for more than one product,
    /// another plan, or a wallet service. It returns 402 if the portal already exceeds a limit of the plan in the new
    /// quantity. By default, the limit is 10 calls a minute per user. More calls return 429. To buy wallet services,
    /// use `PUT api/2.0/portal/payment/updatewallet`.
    /// </remarks>
    /// <summary>
    /// Change the subscription quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/update</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the provider accepted the new quantity, `false` when it declined it", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read or has no `quantity`, `quantity` does not hold exactly one product, the product is not a subscription plan or not the plan currently paid, or the quantity is already the one in effect")]
    [SwaggerResponse(402, "The portal already has more administrators, users, rooms or AI agents, or stores more data, than the plan in the new quantity allows")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPut("update")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdatePayment(QuantityRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: false);

        var currentQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (currentQuota.Price > 0 && currentQuota.Name != productName)
        {
            throw new ArgumentException("Invalid product");
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.Quotas.Any(q => q.Id == quota.TenantId && q.Quantity == productQty))
        {
            throw new ArgumentException("Invalid quantity");
        }

        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        return await paymentHelper.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Set, currency, true, securityContext.CurrentAccount.ID.ToString());
    }

    /// <remarks>
    /// Buys units of a wallet service, or schedules the quantity of the service for the next period. The mode comes
    /// from `productQuantityType`. With `Add`, the call buys the units immediately and pays for them from the portal
    /// wallet. With `Set`, the call charges nothing and changes the quantity from the next period. With `Set`, an
    /// empty or zero quantity cancels a change that was scheduled earlier. The call returns 400 for `Renew` and
    /// `Sub`. If the portal has no billing customer, the call returns 404. Requires a DocSpace admin. Other callers
    /// get 403. The call also returns 403 if the installation has no billing service. An `Add` call spends money. Do
    /// not repeat an `Add` call. Each call buys the units again. You can repeat a `Set` call with the same body. With
    /// `Add`, the wallet needs a sub-account in the accounting currency with enough money. If the wallet has no such
    /// sub-account, the call returns 404. If the billing service declines the purchase, fails, or cannot be reached,
    /// the call returns 402. If the service is an add-on to the plan and the plan is not paid, the call returns 402.
    /// The call returns 400 if the quantity is below the minimum of the service. The minimum is 100 units for disk
    /// storage and 10 for Docs Connect Dev Pack. With `Add`, the minimum for admins is the number of admins the
    /// portal already has. With `Add`, the call checks the minimum only while the portal does not have the service
    /// yet. With `Add`, an empty or zero quantity also returns 400. While Docs Connect is active, a request for Docs
    /// Connect Dev Pack returns 400. To move to Docs Connect Dev Pack, use
    /// `POST api/2.0/settings/docscloud/switchtodevpack`. While Docs Connect Dev Pack is active, `Set` with Docs
    /// Connect schedules the return to it. The return happens at the next period and needs a quantity above zero. In
    /// this case, `Add` with Docs Connect returns 400. The call returns `true` if the change was accepted and `false`
    /// if the provider declined it. By default, the limit is 10 calls a minute per user. More calls return 429. To
    /// get the price first, use `PUT api/2.0/portal/payment/calculatewallet`.
    /// </remarks>
    /// <summary>
    /// Change a wallet service quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/updatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the purchase or the scheduled change was accepted, `false` when the provider declined it", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read or has no `quantity`, `quantity` does not hold exactly one product, the quantity type is not `Set` or `Add`, the product is not a wallet service, the quantity is below the minimum for it or is empty or zero where a purchase needs one, or that service is already set")]
    [SwaggerResponse(402, "The plan of the portal is not paid and the requested service is an add-on to it, or in the `Add` form the billing service declines the purchase, answers with an error or cannot be reached")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no balance or no sub-account in the accounting currency")]
    [HttpPut("updatewallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdateWalletPayment(WalletQuantityRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        if (inDto.ProductQuantityType is ProductQuantityType.Renew or ProductQuantityType.Sub)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync(refresh: true);

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        var tariff = await tariffService.GetTariffAsync(tenantId);

        if (tariff.State > TariffState.Paid && quota.Additional)
        {
            throw new BillingException("Tariff is not paid");
        }

        var minValue = quota.TenantId switch
        {
            (int)TenantWalletService.Storage => configuration.GetValue<int?>("core:accounting:minStorageQuantity") ?? 100,
            (int)TenantWalletService.DocsCloudDevPack => configuration.GetValue<int?>("core:docscloud:minDevPackQuantity") ?? 10,
            _ => 1
        };

        // requesting DocsCloudDevPack while DocsCloud is active is never valid here, in either flow -
        // that upgrade goes through DocsCloudController.SwitchToDevPack instead. Only the reverse
        // direction (DocsCloud while DevPack is active) is allowed here, and only to schedule a
        // reversion via the Set branch below
        if (quota.TenantId == (int)TenantWalletService.DocsCloudDevPack &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloud))
        {
            throw new ArgumentException("Quota is already set");
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Set)
        {
            if (productQty.HasValue && productQty.Value != 0 && productQty.Value < minValue)
            {
                throw new ArgumentException("Invalid quantity");
            }

            // requesting the DocsCloud product while DocsCloudDevPack is active schedules a reversion to
            // DocsCloud at the next period, rather than an immediate switch
            var targetQuota = quota.TenantId;
            int? nextQuota = null;
            if (targetQuota == (int)TenantWalletService.DocsCloud &&
                tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloudDevPack))
            {
                targetQuota = (int)TenantWalletService.DocsCloudDevPack;
                nextQuota = (int)TenantWalletService.DocsCloud;

                // a scheduled switch is a real purchase of a new product, so unlike a plain quantity
                // change there's no "reset to default" for 0/null - it would just be silently dropped
                // at renewal by RenewSubscriptionAsync's NextQuantity <= 0 guard
                if (productQty is null or <= 0)
                {
                    throw new ArgumentException("Invalid quantity");
                }
            }

            // saving null value is equivalent to resetting to default
            return await paymentHelper.UpdateNextQuantityAsync(tenantId, tariff, targetQuota, productQty, productName, nextQuota);
        }

        // inDto.ProductQuantityType === ProductQuantityType.Add

        if (quota.TenantId == (int)TenantWalletService.DocsCloud &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloudDevPack))
        {
            throw new ArgumentException("Quota is already set");
        }

        if (productQty is null or <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        if (quota.TenantId == (int)TenantWalletService.Admin)
        {
            minValue = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupRoomAdmin.ID)).Length;
        }

        var hasActiveWalletQuota = tariff.Quotas.Any(q => q.Id == quota.TenantId && q.State == QuotaState.Active);
        if (!hasActiveWalletQuota && productQty < minValue)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

        await paymentHelper.GetSubAccountRequiredAsync(tenantId, defaultCurrency, refresh: true);

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        return await paymentHelper.PaymentChangeAsync(tenantId, quantity, inDto.ProductQuantityType, defaultCurrency, false, securityContext.CurrentAccount.ID.ToString(), true);
    }

    /// <remarks>
    /// Returns the price of a wallet service purchase without making it. Use it to show the amount before the user
    /// confirms the purchase. The call returns 400 if `productQuantityType` is not `Add` or the quantity is not above
    /// zero. It also returns 400 if the product is not a wallet service. If the portal has no billing customer or no
    /// wallet sub-account in the accounting currency, the call returns 404. Requires a DocSpace admin. Other callers
    /// get 403. The call also returns 403 if the installation has no billing service. The call is read-only and buys
    /// nothing, so you can repeat it. To make the purchase, use `PUT api/2.0/portal/payment/updatewallet`. The price
    /// is for the moment of the call and is not reserved. So it can change before the purchase.
    /// </remarks>
    /// <summary>
    /// Calculate the wallet payment amount
    /// </summary>
    /// <path>api/2.0/portal/payment/calculatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The amount the purchase would cost, its currency and the quantity it was calculated for", typeof(PaymentCalculationDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `quantity`, `quantity` does not hold exactly one product, the quantity type is not `Add`, the quantity is not greater than zero, or the product is not a wallet service")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no balance or no sub-account in the accounting currency")]
    [HttpPut("calculatewallet")]
    public async Task<PaymentCalculationDto> CalculateWalletPayment(WalletQuantityRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        if (inDto.ProductQuantityType is not ProductQuantityType.Add)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;

        await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        if (productQty is null or <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

        await paymentHelper.GetSubAccountRequiredAsync(tenantId, defaultCurrency);

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentCalculateAsync(tenantId, quantity, inDto.ProductQuantityType, defaultCurrency);

        return result?.Map();
    }

    /// <remarks>
    /// Returns the unused part of the current paid subscription period of the portal, in money. This is the credit
    /// that `POST api/2.0/portal/payment/subscription/movetowallet` moves to the wallet. If the portal has no billing
    /// customer, the call returns 404. The payer is the portal user whose email is the email of the billing customer.
    /// Only the payer can call it. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. The call is read-only. If the plan is not paid, the call returns 402. It also returns 402 if
    /// the billing service fails or cannot be reached. If the paid plan has no subscription, the call returns 404. If
    /// the paid plan is a wallet product or has no product ID, the call returns 400. The figures are for the moment
    /// of the call, so they change between calls.
    /// </remarks>
    /// <summary>
    /// Get the subscription balance information
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The unused balance of the current subscription period with its period boundaries and currencies", typeof(SubscriptionBalanceDto))]
    [SwaggerResponse(400, "The plan currently paid is a wallet product or has no product identifier")]
    [SwaggerResponse(402, "The plan of the portal is not in the paid state, or the billing service answers with an error or cannot be reached")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its paid plan has no subscription")]
    [HttpGet("subscription/balance")]
    public async Task<SubscriptionBalanceDto> GetSubscriptionBalanceInfo()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var productId = await paymentHelper.GetCurrentSubscriptionProductIdAsync(tenant.Id);

        return (await tariffService.GetSubscriptionBalanceInfoAsync(tenant.Id, productId))?.Map();
    }

    /// <remarks>
    /// Ends the paid subscription of the portal and moves the portal to the wallet. First, the call adds the unused
    /// balance of the period to the wallet. Then it tops up the wallet from the payment method if that credit does
    /// not cover the price. Then it buys the requested number of admins as a wallet service. To read the credit
    /// first, use `GET api/2.0/portal/payment/subscription/balance`. If the portal has no billing customer, the call
    /// returns 404. If the customer has no payment method, the call returns 403. The payer is the portal user whose
    /// email is the email of the billing customer. Only the payer can call it. Other callers get 403. The call also
    /// returns 403 if the installation has no billing service. The call spends money and cannot be undone. The call
    /// ends the subscription before it buys the admins. So after a failed purchase, the portal is on the wallet and
    /// the credit is in the wallet. Do not repeat the call then, because a repeat buys the admins again. With a
    /// delayed payment method, the call does not top up the wallet. Then the wallet must already cover the price. The
    /// call returns `true` if the admins were bought. The product in `quantity` must be the admins wallet service.
    /// The call returns 400 for another product or a quantity of zero or less. It also returns 400 for fewer admins
    /// than the portal already has. If the paid plan is a wallet product, the call returns 400. If the plan is not
    /// paid, the call returns 402. It also returns 402 if the balance cannot be moved or the wallet still lacks money
    /// after the top-up. If the paid plan has no subscription, the call returns 404. By default, the limit is 10
    /// calls a minute per user. More calls return 429.
    /// </remarks>
    /// <summary>
    /// Move the subscription to the wallet
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/movetowallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the balance was moved to the wallet and the administrators were bought", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read or has no `quantity`, `quantity` does not hold exactly one product or does not name the administrators wallet product, the number asked for is not greater than zero or is below the administrators the portal already has, or the plan currently paid is a wallet product or has no product identifier")]
    [SwaggerResponse(402, "The plan of the portal is not paid, the balance could not be moved, or the wallet is still short of the price after the top-up")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, the portal has no billing service configured, or the customer has no payment method set")]
    [SwaggerResponse(404, "This portal has no billing customer, its paid plan has no subscription, or after the move its wallet has no balance or no sub-account in the accounting currency")]
    [HttpPost("subscription/movetowallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> MoveSubscriptionToWallet(QuantityRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        if (customerInfo.PaymentMethodStatus != PaymentMethodStatus.Set)
        {
            throw new InvalidOperationException("Customer payment method is not set");
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;

        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        if (quota.TenantId != (int)TenantWalletService.Admin)
        {
            throw new ArgumentException("Invalid product");
        }

        if (productQty <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // The requested number of admins must not be less than the current number of portal admins.
        var currentAdminCount = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupRoomAdmin.ID)).Length;
        if (productQty < currentAdminCount)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // Resolve the current Stripe subscription product before it is cancelled.
        var productId = await paymentHelper.GetCurrentSubscriptionProductIdAsync(tenant.Id);

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var participant = securityContext.CurrentAccount.ID.ToString();

        // Calculate the cost of the requested admins from the quota price (price * quantity). The price is read from
        // the quota itself: the payer need not be a DocSpace admin, and QuotaHelper empties the price for anyone else.
        var requiredAmount = quota.Price * productQty;

        // Move the unused subscription balance to the wallet.
        await paymentHelper.SubscriptionBalanceToWalletAsync(tenant.Id, productId);

        // Make sure the wallet balance covers the cost, topping it up for the missing amount if necessary.
        // A delayed payment method cannot be topped up on the fly, so its wallet has to cover the cost already.
        var siteName = tenant.GetTenantDomain(coreSettings);
        var allowTopUp = !customerInfo.IsDelayedPaymentMethod;

        if (!await tariffService.EnsureWalletBalanceAsync(tenant.Id, requiredAmount, defaultCurrency, participant, siteName, false, null, allowTopUp))
        {
            throw new BillingException("Insufficient balance");
        }

        // Purchase the requested admins from the wallet.
        return await paymentHelper.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Add, defaultCurrency, false, participant);
    }

    /// <remarks>
    /// Returns the address of the portal page where the payer manages the billing account. The page shows the payment
    /// method, the invoices, and the receipts. The address is relative to the portal root. The call adds `backUrl` to
    /// the address as a query parameter. If the portal has no billing customer, the call returns an empty result to
    /// any caller. The payer is the portal user whose email is the email of the billing customer. Otherwise, only the
    /// payer or the portal owner can call it. Other callers get 403. The call also returns 403 if the installation
    /// has no billing service. The call is read-only. If `backUrl` is not a valid URL or is too long, the call
    /// returns 400. The page buys nothing. To buy a plan, use `PUT api/2.0/portal/payment/url`. To attach a payment
    /// method, use `GET api/2.0/portal/payment/checkoutsetupurl`.
    /// </remarks>
    /// <summary>
    /// Get the billing account page
    /// </summary>
    /// <path>api/2.0/portal/payment/account</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The portal-relative address of the billing account page, or an empty result when the portal has no billing customer", typeof(string))]
    [SwaggerResponse(400, "`backUrl` does not start with `http://`, `https://` or `ftp://`, or is longer than 255 characters")]
    [SwaggerResponse(403, "The caller is neither the payer nor the portal owner, or the portal has no billing service configured")]
    [HttpGet("account")]
    public async Task<string> GetPaymentAccount(PaymentAccountRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        await paymentHelper.DemandPayerOrOwnerAsync(tenant, customerInfo);

        var result = "payment.ashx";
        return !string.IsNullOrEmpty(inDto.BackUrl) ? $"{result}?backUrl={inDto.BackUrl}" : result;
    }

    /// <remarks>
    /// Returns the price of one unit of each product that the portal can buy. The keys are the product names that
    /// `quantity` takes in the purchase operations. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get 403. The call is read-only. All prices are in one currency,
    /// which the call picks from the IP address of the caller. If no product has a price in that currency, the call
    /// uses EUR in Europe and USD elsewhere. A product without a price in the chosen currency has the price 0. So 0
    /// means that the product has no price, not that it is free. To read the currencies of the portal, use
    /// `GET api/2.0/portal/payment/currencies`. To read the current plan with its limits and usage, use
    /// `GET api/2.0/portal/payment/quota`.
    /// </remarks>
    /// <summary>
    /// Get the product prices
    /// </summary>
    /// <path>api/2.0/portal/payment/prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Product name to the price of one unit in the currency of the request, `0` where the product has no price in it", typeof(Dictionary<string, decimal>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("prices")]
    public async Task<Dictionary<string, decimal>> GetPortalPrices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var currency = await regionHelper.GetCurrencyFromRequestAsync();
        var result = (await tenantManager.GetProductPriceInfoAsync())
            .ToDictionary(pr => pr.Key, pr => pr.Value.GetValueOrDefault(currency, 0));
        return result;
    }

    /// <remarks>
    /// Returns the currencies of the plan prices for the portal. The first item is always the default currency, USD.
    /// The second item, if any, is the currency of the region of the request. The call finds that region from the IP
    /// address of the caller or the language of the portal owner. If no product has a price in the currency of that
    /// region, the list has only the first item. Requires the EditPortalSettings permission, which the portal owner
    /// and DocSpace admins have. Other callers get 403. The call is read-only. The wallet uses other currencies, the
    /// accounting currencies. To read them with the balance, use `GET api/2.0/portal/payment/customer/balance`.
    /// </remarks>
    /// <summary>
    /// Get the billing currencies
    /// </summary>
    /// <path>api/2.0/portal/payment/currencies</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The default currency of the portal region first, followed by the currency of the current request when it differs", typeof(IAsyncEnumerable<CurrenciesDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("currencies")]
    public async IAsyncEnumerable<CurrenciesDto> GetPaymentCurrencies()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var defaultRegion = regionHelper.GetDefaultRegionInfo();
        var currentRegion = await regionHelper.GetCurrentRegionInfoAsync();

        yield return new CurrenciesDto(defaultRegion);

        if (!currentRegion.Name.Equals(defaultRegion.Name))
        {
            yield return new CurrenciesDto(currentRegion);
        }
    }

    /// <remarks>
    /// Returns the quotas that the portal can buy: the paid plans and the wallet services. Each quota has its price,
    /// features, and limits, so you can build a pricing page from the list. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The
    /// list has only the visible quotas, the newest first. The filters `wallet` and `additional` narrow the list, and
    /// an omitted filter keeps both kinds. If the portal is on a non-profit quota, `additional=false` returns only
    /// that quota. The list does not mark the current quota of the portal. To read it, use
    /// `GET api/2.0/portal/payment/quota`.
    /// </remarks>
    /// <summary>
    /// Get the purchasable quotas
    /// </summary>
    /// <path>api/2.0/portal/payment/quotas</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The visible quotas matching the filters, newest first, each with its price, features and limits", typeof(IEnumerable<QuotaDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("quotas")]
    public async Task<IEnumerable<QuotaDto>> GetPaymentQuotas(QuotasRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.Additional.HasValue && !inDto.Additional.Value)
        {
            var currentQuota = await tariffHelper.GetCurrentQuotaAsync(false, false);
            if (currentQuota.NonProfit)
            {
                return [currentQuota];
            }
        }

        return await tariffHelper.GetQuotasAsync(false, inDto.Additional, inDto.Wallet).ToListAsync();
    }

    /// <remarks>
    /// Returns every service that the portal can pay for from its wallet. The services are extra admins, disk
    /// storage, backup, AI tools, AI search, and Docs Connect. Each item has the price of a unit, the unit, and
    /// whether the service is turned on. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call is read-only. Variants of one service form a group. The
    /// visible variant holds the others in `innerServices`. If AI is turned off for the portal, the list has no AI
    /// services. To read the services that run on the portal, use `GET api/2.0/portal/payment/activeservices`. To
    /// read one service, use `GET api/2.0/portal/payment/walletservice`. To turn a service on or off, use
    /// `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservices</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services on offer, with their prices, units and grouped variants", typeof(IEnumerable<WalletServiceDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("walletservices")]
    public async Task<IEnumerable<WalletServiceDto>> GetWalletServices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await tariffHelper.GetWalletServicesAsync();
    }

    /// <remarks>
    /// Returns one wallet service by its name. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call is read-only. The call returns 404 if the installation
    /// does not sell the service. It also returns 404 for AI tools and AI search while AI is turned off for the
    /// portal. If `service` is empty or is not a wallet service, the call returns 400. The item has the fields of an
    /// item of `GET api/2.0/portal/payment/walletservices`. Unlike that list, it does not group the variants of a
    /// service in `innerServices`. The price is in the currency of the request.
    /// </remarks>
    /// <summary>
    /// Get a wallet service
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservice</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet service with its price, unit and the limits it grants", typeof(WalletServiceDto))]
    [SwaggerResponse(400, "`service` is sent empty, or is neither the name nor the number of a wallet service")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(404, "This installation does not sell a wallet service under that name")]
    [HttpGet("walletservice")]
    public async Task<WalletServiceDto> GetWalletService(GetWalletServiceRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quotaList = await quotaService.GetTenantQuotasAsync();
        var quota = quotaList.FirstOrDefault(q => q.Wallet && q.TenantId == (int)inDto.Service);
        if (quota == null || ((quota.AITools || quota.AISearch) && !await aiGateway.IsAiAccessEnabledAsync()))
        {
            throw new ItemNotFoundException("Service could not be found");
        }

        var quotaDto = await tariffHelper.ToQuotaDtoAsync(quota, false);
        var walletServiceDto = quotaDto.MapToWalletServiceDto();
        walletServiceDto.ServiceName = quota.ServiceName;
        return walletServiceDto;
    }

    /// <remarks>
    /// Returns the quota of the portal, paid or free, with the limits of its features and their usage. Any signed-in
    /// user can call it, except a guest. A guest gets 403. The call is read-only. By default, the call reads the plan
    /// from the cache. With `refresh=true`, it reads the plan from the billing service, so use it right after a
    /// purchase. The answer depends on the user type of the caller. A DocSpace admin or the portal owner gets the
    /// price, the due date, and all features. A room admin gets only the `manager`, `room`, `total_size`, and
    /// `statistic` features. A user gets only the `total_size` feature. For a room admin and a user, `price` is an
    /// empty object and `dueDate` is missing. Every caller gets `usersQuota`, `roomsQuota`, `aiAgentsQuota`, and
    /// `tenantCustomQuota`. To read the quotas that the portal can buy, use `GET api/2.0/portal/payment/quotas`.
    /// </remarks>
    /// <summary>
    /// Get the current plan and limits
    /// </summary>
    /// <path>api/2.0/portal/payment/quota</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The quota the portal is on with its limits and current usage, trimmed to what the caller's user type may see", typeof(QuotaDto))]
    [SwaggerResponse(403, "The caller is a guest of this portal")]
    [HttpGet("quota")]
    public async Task<QuotaDto> GetQuotaPaymentInformation(PaymentInformationRequestDto inDto)
    {
        // Every non-guest member reads this at start-up: the client's AuthStore.getPaymentInfo rethrows
        // on failure, so a 403 would leave a RoomAdmin or a User with a blank page. Instead QuotaHelper
        // trims the answer to what the caller's type may see.
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        return await tariffHelper.GetCurrentQuotaAsync(inDto.Refresh);
    }

    /// <remarks>
    /// Sends a message from the portal to the ONLYOFFICE sales team. Use it to ask for a quote, an invoice, or a plan
    /// that cannot be bought online. The team sends the answer to `email`. Requires a DocSpace admin. Other callers
    /// get 403. The call queues an email and changes no plan, quota, or payment. Do not repeat it. Each call sends
    /// the message again. The response has no body. The call returns 400 if `email` is not a valid address or a field
    /// is empty. By default, the limit is 10 calls a minute per user. More calls return 429.
    /// </remarks>
    /// <summary>
    /// Contact the sales team
    /// </summary>
    /// <path>api/2.0/portal/payment/request</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The message has been handed to the mail service; the response carries no content")]
    [SwaggerResponse(400, "`email` is not a well-formed address, or one of the required fields is empty")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [SwaggerResponse(429, "This user has made more than ten requests in a minute")]
    [HttpPost("request")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task SendPaymentRequest(SalesRequestsDto inDto)
    {
        await paymentHelper.DemandAdminAsync();

        if (!inDto.Email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

        await studioNotifyService.SendMsgToSalesAsync(inDto.Email, inDto.UserName, inDto.Message);
        messageService.Send(MessageAction.ContactSalesMailSent);
    }

    /// <remarks>
    /// Returns the URL of the hosted page that attaches a payment method to the billing account of the portal. Use it
    /// before a wallet top-up or an automatic top-up. Requires a DocSpace admin. Other callers get 403. The call also
    /// returns 403 if the installation has no billing service. The payer is the portal user whose email is the email
    /// of the billing customer. If the portal has a billing customer with an email, only the payer can call it. Other
    /// admins get 403. If the portal already has a payment method, the call returns an empty result. The call is
    /// read-only. The payment provider stores the payment method when the user completes the page. After this,
    /// `GET api/2.0/portal/payment/customerinfo` reports the method as set. The URL carries the email of the caller
    /// and the language of the request. It also carries a currency that the call picks from the IP address of the
    /// caller. The page redirects to `successUrl` or `backUrl` when the user finishes or cancels. If `backUrl` or
    /// `successUrl` is missing, invalid, or too long, the call returns 400. The page buys nothing. To buy a plan, use
    /// `PUT api/2.0/portal/payment/url`.
    /// </remarks>
    /// <summary>
    /// Get the checkout setup page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/checkoutsetupurl</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The absolute URL of the payment method setup page, or an empty result when the portal already has a payment method", typeof(Uri))]
    [SwaggerResponse(400, "`backUrl` or `successUrl` is missing, does not start with `http://`, `https://` or `ftp://`, or is longer than 255 characters")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator or, once a billing customer exists, not its payer; or the portal has no billing service configured")]
    [HttpGet("checkoutsetupurl")]
    public async Task<Uri> GetCheckoutSetupUrl(CheckoutSetupUrlRequestsDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            if (!string.IsNullOrEmpty(customerInfo.Email))
            {
                await paymentHelper.DemandPayerAsync(customerInfo);
            }

            if (customerInfo.PaymentMethodStatus == PaymentMethodStatus.Set)
            {
                return null;
            }
        }

        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        return await tariffService.GetShoppingUriAsync(
            tenant.Id,
            tenant.AffiliateId,
            tenant.PartnerId,
            currency,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            user.Email,
            [],
            inDto.BackUrl,
            inDto.SuccessUrl,
            true);
    }

    /// <remarks>
    /// Returns the billing customer of the portal. It has the billing email, whether a payment method is stored, and
    /// the payer. The payer is the portal user whose email is the billing email. Requires a DocSpace admin. Other
    /// callers get 403. The call is read-only. The call returns an empty result if the installation has no billing
    /// service. It also returns an empty result if the portal has no billing customer. If no portal user has the
    /// billing email, `payer` is empty. Then nobody can call the operations that only the payer can call. By default,
    /// the call reads the customer from the cache. With `refresh=true`, it reads the customer from the billing
    /// provider. Use it right after a payment method is attached.
    /// </remarks>
    /// <summary>
    /// Get the customer information
    /// </summary>
    /// <path>api/2.0/portal/payment/customerinfo</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The billing customer with its payer, or an empty result when the portal has no customer or billing is not configured", typeof(CustomerInfoDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("customerinfo")]
    public async Task<CustomerInfoDto> GetCustomerInfo(PaymentInformationRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            // do not throw an exception, just return null
            return null;
        }

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id, inDto.Refresh);

        if (customerInfo == null)
        {
            return null;
        }

        var payerUserInfo = await userManager.GetUserByEmailAsync(customerInfo.Email);

        var payerDto = payerUserInfo.Id == ASC.Core.Users.Constants.LostUser.Id
                ? null
                : await employeeWrapperHelper.GetAsync(payerUserInfo);

        var result = new CustomerInfoDto(customerInfo, payerDto);

        return result;
    }

    /// <remarks>
    /// Charges the payment method of the portal and adds the amount to the portal wallet. The wallet services are
    /// paid from this balance. If the portal has no billing customer, the call returns 404. If the customer has no
    /// payment method, the call returns 403. To attach one, use `GET api/2.0/portal/payment/checkoutsetupurl`. The
    /// payer is the portal user whose email is the email of the billing customer. Only the payer can call it. Other
    /// callers get 403. The call also returns 403 if the installation has no billing service. The call spends money.
    /// Do not repeat it. Each call charges the payment method again. After a timeout, check the balance before you
    /// call it again. The call returns 400 if `currency` is not a supported accounting currency or `amount` is out of
    /// range. The call returns `true` if the payment provider accepted the charge. To read the new balance, use
    /// `GET api/2.0/portal/payment/customer/balance`. By default, the limit is 10 calls a minute per user. More calls
    /// return 429.
    /// </remarks>
    /// <summary>
    /// Top up the wallet
    /// </summary>
    /// <path>api/2.0/portal/payment/deposit</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the payment provider accepted the charge and the wallet was credited", typeof(bool))]
    [SwaggerResponse(400, "`currency` is not one of the supported accounting currencies, or `amount` is outside 1 to 999999")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, the portal has no billing service configured, or the customer has no payment method set")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPost("deposit")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> TopUpDeposit(TopUpDepositRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();
        if (!supportedCurrencies.Contains(inDto.Currency))
        {
            throw new ArgumentException("Unsupported currency");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        if (customerInfo.PaymentMethodStatus != PaymentMethodStatus.Set)
        {
            throw new InvalidOperationException("Customer payment method is not set");
        }

        var siteName = tenant.GetTenantDomain(coreSettings);
        var waitForChanges = !customerInfo.IsDelayedPaymentMethod;

        return await paymentHelper.TopUpDepositAsync(tenant.Id, inDto.Amount, inDto.Currency, securityContext.CurrentAccount.ID.ToString(), siteName, waitForChanges);
    }

    /// <remarks>
    /// Returns the money in the portal wallet as the accounting service has it. The answer has a sub-account with its
    /// amount for each currency and the last credit movement. Requires a DocSpace admin. Other callers get 403. The
    /// call also returns 403 if the installation has no billing service. If the portal has no billing customer, the
    /// call returns an empty result. The call is read-only. The wallet services spend this balance, and
    /// `POST api/2.0/portal/payment/deposit` adds to it. To list the movements, use
    /// `GET api/2.0/portal/payment/customer/operations`. By default, the call reads the cache, so right after a
    /// top-up the balance is still the old one. Use `refresh=true` to read it from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer balance
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet account with its sub-account per currency, or an empty result when the portal has no billing customer", typeof(BalanceDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("customer/balance")]
    public async Task<BalanceDto> GetCustomerBalance(PaymentInformationRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        return (await tariffService.GetCustomerBalanceAsync(tenant.Id, inDto.Refresh))?.Map();
    }

    /// <remarks>
    /// Returns a page of the money movements of the portal wallet. The movements are top-ups, service charges,
    /// refunds, and corrections. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the
    /// installation has no billing service. If the portal has no billing customer, the call returns an empty result.
    /// The call is read-only. All filters are optional. If `serviceName` has a name that the installation does not
    /// sell, the call returns 404. If neither `credit` nor `debit` is set, the list has both directions. By default,
    /// `offset` is 0 and `limit` is 25. The answer repeats them next to `totalQuantity`, `totalPage`, and
    /// `currentPage`. To get the same data as a file, use `POST api/2.0/portal/payment/customer/operationsreport`. To
    /// get the totals per service, use `GET api/2.0/portal/payment/customer/usage`.
    /// </remarks>
    /// <summary>
    /// Get the wallet operations
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operations</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "A page of wallet movements with its paging information, or an empty result when the portal has no billing customer", typeof(ReportDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "One of the names in `serviceName` is not a wallet service of this installation")]
    [HttpGet("customer/operations")]
    public async Task<ReportDto> GetCustomerOperations([FromQuery]CustomerOperationsRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var utcStartDate = GetUtcStartDate(inDto.StartDate, tenant);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow);

        var filter = new OperationFilter
        {
            ServiceName = inDto.ServiceName,
            UtcStartDate = utcStartDate,
            UtcEndDate = utcEndDate,
            ParticipantName = inDto.ParticipantName,
            Credit = inDto.Credit,
            Debit = inDto.Debit,
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            Type = inDto.Type,
            Status = inDto.Status,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType
        };

        var report = await tariffService.GetCustomerOperationsAsync(tenant.Id, filter);
        if (report == null)
        {
            return null;
        }

        var participantDisplayNames = await report.GetParticipantDisplayNamesAsync(displayUserSettingsHelper, true);

        return new ReportDto(report, apiDateTimeHelper, participantDisplayNames);
    }

    /// <remarks>
    /// Returns a page of the top-ups of the portal wallet. Requires a DocSpace admin. Other callers get 403. The call
    /// also returns 403 if the installation has no billing service. The caller does not have to be the payer. The
    /// call is read-only. The call returns an empty result if the portal has no billing customer or no wallet yet. It
    /// also returns an empty result if the accounting service cannot be read. A period without top-ups returns an
    /// empty `collection`. By default, `offset` is 0 and `limit` is 25. The answer repeats them next to
    /// `totalQuantity`, `totalPage`, and `currentPage`. The call returns 400 if `offset` or `limit` is out of range.
    /// A top-up with `hasInvoice` true has an invoice. To open it, use
    /// `GET api/2.0/portal/payment/customer/deposits/{id}/invoice`. To get the full history with charges and refunds,
    /// use `GET api/2.0/portal/payment/customer/operations`.
    /// </remarks>
    /// <summary>
    /// Get the wallet top-ups
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/deposits</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "A page of wallet top-ups with its paging information, or an empty result when the portal is not a billing customer, has no wallet at the accounting service yet, or that service could not be read", typeof(DepositReportDto))]
    [SwaggerResponse(400, "`offset` is negative, `limit` is outside 1 to 100, or a parameter cannot be read, such as a `status` that is not one of its values or a date that is not ISO 8601")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("customer/deposits")]
    public async Task<DepositReportDto> GetCustomerDeposits(GetCustomerDepositsRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var offset = inDto.Offset ?? 0;
        var limit = inDto.Limit ?? 25;

        var filter = new DepositFilter
        {
            UtcStartDate = GetUtcStartDate(inDto.StartDate, tenant),
            UtcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow),
            Status = inDto.Status,
            Offset = offset,
            Limit = limit
        };

        var report = await tariffService.GetCustomerDepositsAsync(tenant.Id, filter);
        if (report == null)
        {
            return null;
        }

        return depositDtoMapper.Map(report, offset, limit);
    }

    /// <remarks>
    /// Returns the address of the invoice page for one top-up of the portal wallet. The address is absolute and leads
    /// to the billing provider. A top-up has an invoice only if `hasInvoice` is true. Requires a DocSpace admin.
    /// Other callers get 403. The call also returns 403 if the installation has no billing service. The caller does
    /// not have to be the payer. The call is read-only. Do not store the address. Request it right before the user
    /// opens the invoice. The call returns 404 for an unknown top-up, a top-up without an invoice, or no billing
    /// customer. If the billing or accounting service rejects the request or does not answer, the call returns 402. A
    /// later retry may succeed.
    /// </remarks>
    /// <summary>
    /// Get the top-up invoice
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/deposits/{id}/invoice</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The absolute address of the hosted invoice page for the top-up", typeof(string))]
    [SwaggerResponse(400, "`id` is not a number")]
    [SwaggerResponse(402, "The billing or the accounting service rejected the request for the top-up or its invoice link, or did not answer")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, no top-up with this ID, or the top-up has no invoice")]
    [HttpGet("customer/deposits/{id}/invoice")]
    public async Task<string> GetDepositInvoice(GetDepositInvoiceRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        // The default window of the top-up list, so every top-up the list can show is found here as well.
        var utcStartDate = GetUtcStartDate(null, tenant);
        var utcEndDate = DateTime.UtcNow;

        Deposit deposit;

        try
        {
            deposit = await tariffService.FindCustomerDepositAsync(tenant.Id, inDto.Id, utcStartDate, utcEndDate, HttpContext.RequestAborted);
        }
        catch (AccountingCustomerNotFoundException)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }
        catch (AccountingException exception)
        {
            // A failed accounting read is answered like a failed billing request, with the retryable 402, rather
            // than taken for a missing top-up.
            throw new BillingException("The top-up could not be read from the accounting service", exception);
        }

        if (deposit is not { HasInvoice: true })
        {
            throw new ItemNotFoundException("Invoice could not be found");
        }

        try
        {
            return await tariffService.GetInvoiceLinkAsync(tenant.Id, deposit.PaymentRef);
        }
        catch (BillingNotFoundException)
        {
            throw new ItemNotFoundException("Invoice could not be found");
        }
    }

    // A requested start is read in the portal time zone; without one the period starts at the portal creation date.
    private DateTime GetUtcStartDate(DateTime? startDate, Tenant tenant)
    {
        return startDate.HasValue
            ? tenantUtil.DateTimeToUtc(startDate.Value)
            : TenantUtil.GetUtcCreationDate(tenant);
    }

    /// <remarks>
    /// Returns the spending of the portal wallet, added up per calendar month. Use it to draw a spending chart.
    /// Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no billing
    /// service. If the portal has no billing customer, the call returns an empty result. The call is read-only. The
    /// call cuts the months in the portal time zone. Months without spending are not listed. To list the movements of
    /// a month, use `GET api/2.0/portal/payment/customer/operations`. To get the same figures as a file, use
    /// `POST api/2.0/portal/payment/customer/usage/monthly/report`.
    /// </remarks>
    /// <summary>
    /// Get the customer monthly usage
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "One item per calendar month that had spending, or an empty result when the portal has no billing customer", typeof(IEnumerable<CustomerMonthlyUsageDto>))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("customer/usage/monthly")]
    public async Task<List<CustomerMonthlyUsageDto>> GetCustomerMonthlyUsage([FromQuery] CustomerMonthlyUsageRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var filter = new MonthlyUsageFilter
        {
            UtcStartDate = GetUtcStartDate(inDto.StartDate, tenant),
            UtcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow),
            TimeZome = tenant.TimeZone ?? "UTC"
        };

        var usage = await tariffService.GetCustomerMonthlyUsageAsync(tenant.Id, filter);

        return usage?.Select(u => new CustomerMonthlyUsageDto(u)).ToList();
    }

    /// <remarks>
    /// Returns the consumption and the cost of each wallet service of the portal, added up per service. Requires a
    /// DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no billing service.
    /// If the portal has no billing customer, the call returns an empty result. The call is read-only. All filters
    /// are optional. If `serviceName` has a name that the installation does not sell, the call returns 404. The
    /// amounts use the unit of each service. For AI tools, the unit is tokens, not AI credits. To list the charges
    /// behind the totals, use `GET api/2.0/portal/payment/customer/operations`. To get the same figures as a file,
    /// use `POST api/2.0/portal/payment/customer/usage/report`.
    /// </remarks>
    /// <summary>
    /// Get the customer service usage
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The usage and cost per wallet service with its paging information, or an empty result when the portal has no billing customer", typeof(CustomerServiceUsageReportDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "One of the names in `serviceName` is not a wallet service of this installation")]
    [HttpGet("customer/usage")]
    public async Task<CustomerServiceUsageReportDto> GetCustomerServiceUsage([FromQuery] CustomerServiceUsageRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var utcStartDate = GetUtcStartDate(inDto.StartDate, tenant);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow);

        var filter = new UsageFilter
        {
            ServiceName = inDto.ServiceName,
            ParticipantName = inDto.ParticipantName,
            Status = inDto.Status,
            UtcStartDate = utcStartDate,
            UtcEndDate = utcEndDate,
            Metadata = inDto.Metadata,
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType
        };

        var report = await tariffService.GetCustomerServiceUsageAsync(tenant.Id, filter);
        if (report == null)
        {
            return null;
        }

        var tenantQuotas = (await quotaService.GetTenantQuotasAsync()).ToList();
        var walletQuotas = tenantQuotas.Where(x => x.Wallet)
            .ToDictionary(x => x.ServiceName, x => x);

        var customUom = new Dictionary<string, string>();
        var aiQuota = tenantQuotas.SingleOrDefault(q => q.TenantId == (int)TenantWalletService.AITools);
        if (aiQuota != null)
        {
            // For ai-tools, usage is displayed in Tokens instead of AI Credits.
            customUom.Add(aiQuota.ServiceName, "chat");
        }

        return new CustomerServiceUsageReportDto(report, walletQuotas, customUom);
    }

    /// <remarks>
    /// Returns the wallet services that run on the portal now. The list has the active add-ons of the plan and the
    /// services that an admin turned on by hand. It also has the Docs Connect trial, which the wallet does not pay
    /// for. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. The portal does not need a billing customer. The call is read-only. A subscription service
    /// can have its limit and its usage. For Docs Connect, these are the editor seats and the active editors. For
    /// disk storage, these are the bought units and the used units. An item without a limit is a service whose usage
    /// is not counted this way. The call returns 400 if the Docs Connect service does not know the active Docs
    /// Connect subscription. If the Docs Connect service fails or cannot be reached, the call returns 500. To read
    /// all services that the portal can buy, use `GET api/2.0/portal/payment/walletservices`. To turn a service on or
    /// off, use `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get the active wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/activeservices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services active on the portal, with their limits and usage where those are known", typeof(IEnumerable<ActiveServiceDto>))]
    [SwaggerResponse(400, "The portal has an active Docs Connect subscription that the Docs Connect service does not know")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(500, "The portal has an active Docs Connect subscription, and the Docs Connect service answers with an error or cannot be reached")]
    [HttpGet("activeservices")]
    public async Task<List<ActiveServiceDto>> GetActiveServices()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return await paymentHelper.GetActiveServicesAsync(tenant.Id);
    }

    /// <remarks>
    /// Starts a job that builds the history of the wallet movements as an `xlsx` file. If the portal has no billing
    /// customer, the call returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if
    /// the installation has no billing service. The call queues the job and returns it. The file is not ready when
    /// the response arrives. The body takes the filters of `GET api/2.0/portal/payment/customer/operations`. An empty
    /// body covers the time from the creation of the portal until now. If `serviceName` has a name that the
    /// installation does not sell, the call returns 404. Repeat
    /// `GET api/2.0/portal/payment/customer/operationsreport` until `isCompleted` is true. Then take the file from
    /// `resultFileUrl` or `resultFileId`. The portal saves the file in the My documents section of the caller. The
    /// file counts against the portal storage. Each user has one report of this kind at a time. While it runs, a
    /// repeated call returns the running job. To stop it, use
    /// `DELETE api/2.0/portal/payment/customer/operationsreport`. If the build fails, the job ends with `error`
    /// filled in.
    /// </remarks>
    /// <summary>
    /// Start the operations report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The queued task, to be polled until `isCompleted` is true", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or one of the names in `serviceName` is not a wallet service of this installation")]
    [HttpPost("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerOperationsReport(CustomerOperationsReportRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerOperationsReportRequestDto();

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.Operations));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.Operations,
            inDto.ServiceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            inDto.Credit,
            inDto.Debit,
            inDto.Type,
            inDto.Status,
            orderBy: inDto.OrderBy,
            orderType: inDto.OrderType,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the state of the `xlsx` operations report that the caller started with
    /// `POST api/2.0/portal/payment/customer/operationsreport`. This is the call to poll. Repeat it until
    /// `isCompleted` is true. While the build runs, `percentage` shows its progress. When the file is ready,
    /// `resultFileId`, `resultFileName`, and `resultFileUrl` point to it. The file is in the My documents section of
    /// the caller. If the build fails, `error` is filled in. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. The call is read-only. It returns only the operations report of the caller, not the service
    /// usage or monthly usage reports. An empty result means that the caller has no operations report. Either none
    /// was started, or the job was stopped or removed. Starting a new report removes the completed job. So read the
    /// file link from the first answer with `isCompleted` true.
    /// </remarks>
    /// <summary>
    /// Get the operations report status
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The state of this user's operations report, or an empty result when there is none", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpGet("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> GetCustomerOperationsReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.Operations);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Stops the `xlsx` operations report that the caller has running and removes its job. Use it for a report with
    /// wrong filters or a report that is no longer needed. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. The call queues a stop request for the worker that builds the file. So
    /// `GET api/2.0/portal/payment/customer/operationsreport` can still return the job for a short time. You can
    /// repeat the call. If the caller has no running report, the call does nothing. The response has no body. A 200
    /// response means that the stop was requested, not that a report was stopped. The call does not stop the service
    /// usage or monthly usage reports. A finished report keeps its file in My documents.
    /// </remarks>
    /// <summary>
    /// Terminate the operations report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The stop has been requested; the response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpDelete("customer/operationsreport")]
    public async Task TerminateCustomerOperationsReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.Operations, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Starts a job that builds the usage of the wallet services as an `xlsx` file. If the portal has no billing
    /// customer, the call returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if
    /// the installation has no billing service. The call queues the job and returns it. The file is not ready when
    /// the response arrives. The body takes the filters of `GET api/2.0/portal/payment/customer/usage`. An empty body
    /// covers the time from the creation of the portal until now. If `serviceName` has a name that the installation
    /// does not sell, the call returns 404. Repeat `GET api/2.0/portal/payment/customer/usage/report` until
    /// `isCompleted` is true. Then take the file from `resultFileUrl` or `resultFileId`. The portal saves the file in
    /// the My documents section of the caller. The file counts against the portal storage. Each user has one report
    /// of this kind at a time. While it runs, a repeated call returns the running job. To stop it, use
    /// `DELETE api/2.0/portal/payment/customer/usage/report`. If the build fails, the job ends with `error` filled
    /// in. This report does not affect the operations report. To get the history of the movements, use
    /// `POST api/2.0/portal/payment/customer/operationsreport`.
    /// </remarks>
    /// <summary>
    /// Start the service usage report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The queued task, to be polled until `isCompleted` is true", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or one of the names in `serviceName` is not a wallet service of this installation")]
    [HttpPost("customer/usage/report")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerServiceUsageReport(CustomerServiceUsageReportRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerServiceUsageReportRequestDto();

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.ServiceUsage));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.ServiceUsage,
            inDto.ServiceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            status: inDto.Status,
            metadata: inDto.Metadata,
            orderBy: inDto.OrderBy,
            orderType: inDto.OrderType,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the state of the `xlsx` service usage report that the caller started with
    /// `POST api/2.0/portal/payment/customer/usage/report`. This is the call to poll. Repeat it until `isCompleted`
    /// is true. While the build runs, `percentage` shows its progress. When the file is ready, `resultFileId`,
    /// `resultFileName`, and `resultFileUrl` point to it. The file is in the My documents section of the caller. If
    /// the build fails, `error` is filled in. If the portal has no billing customer, the call returns 404. Requires a
    /// DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no billing service.
    /// The call is read-only. It returns only the service usage report of the caller, not the operations or monthly
    /// usage reports. An empty result means that the caller has no service usage report. Either none was started, or
    /// the job was stopped or removed. Starting a new report removes the completed job. So read the file link from
    /// the first answer with `isCompleted` true.
    /// </remarks>
    /// <summary>
    /// Get the service usage report status
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The state of this user's service usage report, or an empty result when there is none", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpGet("customer/usage/report")]
    public async Task<DocumentBuilderTaskDto> GetCustomerServiceUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.ServiceUsage);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Stops the `xlsx` service usage report that the caller has running and removes its job. Use it for a report
    /// with wrong filters or a report that is no longer needed. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation
    /// has no billing service. The call queues a stop request for the worker that builds the file. So
    /// `GET api/2.0/portal/payment/customer/usage/report` can still return the job for a short time. You can repeat
    /// the call. If the caller has no running report, the call does nothing. The response has no body. A 200 response
    /// means that the stop was requested, not that a report was stopped. The call does not stop the operations or
    /// monthly usage reports. A finished report keeps its file in My documents.
    /// </remarks>
    /// <summary>
    /// Terminate the service usage report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The stop has been requested; the response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpDelete("customer/usage/report")]
    public async Task TerminateCustomerServiceUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.ServiceUsage, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Starts a job that builds the wallet spending per calendar month as an `xlsx` file. If the portal has no
    /// billing customer, the call returns 404. Requires a DocSpace admin. Other callers get 403. The call also
    /// returns 403 if the installation has no billing service. The call queues the job and returns it. The file is
    /// not ready when the response arrives. The body takes only the period, `startDate` and `endDate`. An empty body
    /// covers the time from the creation of the portal until now. The call cuts the months in the portal time zone,
    /// as `GET api/2.0/portal/payment/customer/usage/monthly` does. Repeat
    /// `GET api/2.0/portal/payment/customer/usage/monthly/report` until `isCompleted` is true. Then take the file
    /// from `resultFileUrl` or `resultFileId`. The portal saves the file in the My documents section of the caller.
    /// The file counts against the portal storage. Each user has one report of this kind at a time. While it runs, a
    /// repeated call returns the running job. To stop it, use
    /// `DELETE api/2.0/portal/payment/customer/usage/monthly/report`. If the build fails, the job ends with `error`
    /// filled in. The body has no service filter. For a report per service, use
    /// `POST api/2.0/portal/payment/customer/usage/report`.
    /// </remarks>
    /// <summary>
    /// Start the monthly usage report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The queued task, to be polled until `isCompleted` is true", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPost("customer/usage/monthly/report")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerMonthlyUsageReport(CustomerMonthlyUsageReportRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerMonthlyUsageReportRequestDto();

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.MonthlyUsage));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.MonthlyUsage,
            startDate: inDto.StartDate,
            endDate: inDto.EndDate,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the state of the `xlsx` monthly usage report that the caller started with
    /// `POST api/2.0/portal/payment/customer/usage/monthly/report`. This is the call to poll. Repeat it until
    /// `isCompleted` is true. While the build runs, `percentage` shows its progress. When the file is ready,
    /// `resultFileId`, `resultFileName`, and `resultFileUrl` point to it. The file is in the My documents section of
    /// the caller. If the build fails, `error` is filled in. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. The call is read-only. It returns only the monthly usage report of the caller, not the
    /// operations or service usage reports. An empty result means that the caller has no monthly usage report. Either
    /// none was started, or the job was stopped or removed. Starting a new report removes the completed job. So read
    /// the file link from the first answer with `isCompleted` true.
    /// </remarks>
    /// <summary>
    /// Get the monthly usage report status
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The state of this user's monthly usage report, or an empty result when there is none", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpGet("customer/usage/monthly/report")]
    public async Task<DocumentBuilderTaskDto> GetCustomerMonthlyUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.MonthlyUsage);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Stops the `xlsx` monthly usage report that the caller has running and removes its job. Use it for a report
    /// with wrong filters or a report that is no longer needed. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation
    /// has no billing service. The call queues a stop request for the worker that builds the file. So
    /// `GET api/2.0/portal/payment/customer/usage/monthly/report` can still return the job for a short time. You can
    /// repeat the call. If the caller has no running report, the call does nothing. The response has no body. A 200
    /// response means that the stop was requested, not that a report was stopped. The call does not stop the
    /// operations or service usage reports. A finished report keeps its file in My documents.
    /// </remarks>
    /// <summary>
    /// Terminate the monthly usage report
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The stop has been requested; the response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpDelete("customer/usage/monthly/report")]
    public async Task TerminateCustomerMonthlyUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.MonthlyUsage, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <summary>
    /// Get currencies from the accounting service
    /// </summary>
    /// <remarks>
    /// Returns the list of available currencies from the accounting service.
    /// </remarks>
    /// <path>api/2.0/portal/payment/accounting/currencies</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of currencies", typeof(List<Currency>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("accounting/currencies")]
    public async Task<List<Currency>> GetAccountingCurrencies()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();

        var allCurrencies = await tariffService.GetAllAccountingCurrenciesAsync();

        return allCurrencies.Where(x => supportedCurrencies.Contains(x.Code)).ToList();
    }

    /// <remarks>
    /// Returns the price list of one wallet service as the accounting service keeps it. Requires a DocSpace admin.
    /// Other callers get 403. It also returns 403 if the installation has no billing or accounting service. The call
    /// is read-only. By default, the list also has retired prices and prices that have not started yet. With
    /// `active=true`, the list has only the prices in force now. A service without prices returns an empty list. The
    /// amounts are in the currency of the service, which the records do not name. The installation caches the list
    /// for all portals for up to an hour. So a change in the accounting service can take an hour to appear. To read
    /// the current prices that the customer sees, use `GET api/2.0/portal/payment/quotas`. If `serviceName` is too
    /// long, the call returns 400. The call returns 500 if the accounting service does not know the name or cannot be
    /// reached.
    /// </remarks>
    /// <summary>
    /// Get the service prices from the accounting service
    /// </summary>
    /// <path>api/2.0/portal/payment/accounting/prices/{serviceName}</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of the service prices", typeof(List<ServicePriceDto>))]
    [SwaggerResponse(400, "The `serviceName` is longer than 255 characters")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the billing and accounting services are not configured on the server")]
    [SwaggerResponse(500, "The accounting service does not know the service name, answers the price request with an error or cannot be reached")]
    [HttpGet("accounting/prices/{serviceName}")]
    public async Task<List<ServicePriceDto>> GetAccountingServicePrices(ServicePricesRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var prices = await tariffService.GetAccountingServicePricesAsync(inDto.ServiceName, inDto.Active);

        return prices?.Select(r => r.Map()).ToList();
    }

    /// <remarks>
    /// Returns one spending limit on the AI services of the portal by its ID. A limit applies to the portal as a
    /// whole, to every user, or to one user. Requires a DocSpace admin. Other callers get 403. The call also returns
    /// 403 if the installation has no billing service. The call is read-only. The answer has the scope, thresholds,
    /// period, and state of the limit, but no spending. To read the spending of the portal, use
    /// `GET api/2.0/portal/payment/servicelimit/total`. To read the spending of each user, use
    /// `GET api/2.0/portal/payment/servicelimit/participants`. The call returns 404 if the portal has no billing
    /// customer or no AI service limit with this ID.
    /// </remarks>
    /// <summary>
    /// Get a service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/{id}</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service limit with its scope, thresholds, period and state", typeof(ServiceLimitDto))]
    [SwaggerResponse(400, "The ID is zero or negative")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, or the portal has no limit on the AI services with this ID")]
    [HttpGet("servicelimit/{id:int}")]
    public async Task<ServiceLimitDto> GetServiceLimit(ServiceLimitIdRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var serviceLimit = await tariffService.GetServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices, inDto.Id);

        return serviceLimit?.Map() ?? throw new ItemNotFoundException("Service limit could not be found");
    }

    /// <remarks>
    /// Returns the spending limit on the AI services for the portal as a whole. The spending of all users counts
    /// towards this limit. The answer also has the spending in the current period and what is left. The spending
    /// includes the expected cost of AI operations that still run. Requires a DocSpace admin. Other callers get 403.
    /// The call also returns 403 if the installation has no billing service. The call is read-only. The AI services
    /// check this limit together with the limits of the users. So a user can reach it before their own limit. If the
    /// limit is not set or was deleted, the call returns 200 with a null `response`. To create it, use
    /// `POST api/2.0/portal/payment/servicelimit` with the scope `Total`. A turned-off limit is returned with
    /// `enabled` false. If the portal has no billing customer, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get the portal service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/total</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The limit of the portal as a whole with its spending, or null when no such limit is set", typeof(ServiceLimitDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer")]
    [HttpGet("servicelimit/total")]
    public async Task<ServiceLimitDto> GetTotalServiceLimit()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var serviceLimit = await tariffService.GetTotalServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices);

        return serviceLimit?.Map();
    }

    /// <remarks>
    /// Returns the spending limit on the AI services for every user. It applies to each user separately, if the user
    /// has no personal limit in force. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if
    /// the installation has no billing service. The call is read-only. The answer has no spending, since each user
    /// has their own. To read the spending of each user, use `GET api/2.0/portal/payment/servicelimit/participants`.
    /// The `periodEnd` field still shows when the current period ends for everyone. If the limit is not set or was
    /// deleted, the call returns 200 with a null `response`. To create it, use
    /// `POST api/2.0/portal/payment/servicelimit` with the scope `Global`. A turned-off limit is returned with
    /// `enabled` false. If the portal has no billing customer, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get the global service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/global</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The limit for every user, or null when no such limit is set", typeof(ServiceLimitDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer")]
    [HttpGet("servicelimit/global")]
    public async Task<ServiceLimitDto> GetGlobalServiceLimit()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var serviceLimit = await tariffService.GetGlobalServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices);

        return serviceLimit?.Map();
    }

    /// <remarks>
    /// Returns a page of the spending limits on the AI services that apply to the portal users. Each item has what
    /// the user spent in the current period and what is left. Requires a DocSpace admin. Other callers get 403. The
    /// call also returns 403 if the installation has no billing service. If the portal has no billing customer, the
    /// call returns 404. The call is read-only. If the portal has no limits, the page is empty. A personal limit wins
    /// over the limit for every user. An item with the scope `Custom` is a personal limit. An item with the scope
    /// `Global` is the limit for every user. Its ID repeats for each user that it applies to. The list has no
    /// turned-off limits. A user who has not used the AI services yet can be missing. By default, `offset` is 0 and
    /// `limit` is 25. The answer repeats them next to `totalQuantity`, `totalPage`, and `currentPage`. By default,
    /// the newest limits come first. The call returns 400 if `offset`, `limit`, `orderBy`, or `orderType` is out of
    /// range.
    /// </remarks>
    /// <summary>
    /// Get the user service limits
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/participants</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "A page of the limits in force for the users with their spending and paging information", typeof(ServiceLimitReportDto))]
    [SwaggerResponse(400, "`offset` is negative, `limit` is outside the allowed page size, or `orderBy` or `orderType` is not one of its values")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer")]
    [HttpGet("servicelimit/participants")]
    public async Task<ServiceLimitReportDto> GetParticipantServiceLimits(GetParticipantServiceLimitsRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var filter = new ServiceLimitFilter
        {
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType,
            IncludeUsage = true
        };

        return (await tariffService.GetParticipantServiceLimitsAsync(tenantId, ServiceLimitGroups.AiServices, filter))?.Map();
    }

    /// <remarks>
    /// Creates a spending limit on the AI services, which are AI tools and AI search together. The `scope` field sets
    /// whom the limit applies to. The scope `Total` is for the portal as a whole, and `Global` is for every user
    /// separately. The scope `Custom` is for the user in `userId`. Only `Custom` takes `userId`, and other cases
    /// return 400. A personal limit applies to its user instead of the limit for every user. The AI services also
    /// check the limit of the portal. If the portal has no billing customer, the call returns 404. Requires a
    /// DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no billing service.
    /// The call changes data. The call returns 400 if neither `amountValue` nor `quantityValue` is set. With both,
    /// the AI services stop when either is reached. The accounting service can also reject a threshold with 400. An
    /// example is a quantity for services with different units. If the user does not exist, the call returns 404. A
    /// second limit of the same scope or for the same user returns 409. So a repeated call returns 409. To change a
    /// limit, use `PUT api/2.0/portal/payment/servicelimit`. You cannot change the scope, the user, or the period
    /// later.
    /// </remarks>
    /// <summary>
    /// Create a service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service limit as it was created", typeof(ServiceLimitDto))]
    [SwaggerResponse(400, "`scope` is missing, neither `amountValue` nor `quantityValue` is given, `userId` is missing for the scope `Custom` or given for another scope, a value is outside its range, or the accounting service rejects a threshold, such as a quantity for services with different units")]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, or the user does not exist")]
    [SwaggerResponse(409, "A limit of this scope, or for this user, exists already")]
    [HttpPost("servicelimit")]
    public async Task<ServiceLimitDto> CreateServiceLimit(CreateServiceLimitRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        if (!inDto.AmountValue.HasValue && !inDto.QuantityValue.HasValue)
        {
            throw new ArgumentException("Either the amount or the quantity threshold must be set");
        }

        var scope = inDto.Scope.Value;

        string participantName = null;
        if (scope == ServiceLimitScope.Custom)
        {
            if (!inDto.UserId.HasValue)
            {
                throw new ArgumentException("A personal limit needs the user it is set for");
            }

            await DemandPortalUserAsync(inDto.UserId.Value);

            participantName = inDto.UserId.Value.ToString();
        }
        else if (inDto.UserId.HasValue)
        {
            throw new ArgumentException("Only a personal limit is set for a user");
        }

        var serviceLimit = await tariffService.CreateServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices, scope, participantName,
            inDto.AmountValue, inDto.QuantityValue, inDto.Period);

        await SendServiceLimitAuditMessageAsync(MessageAction.CustomerServiceLimitCreated, serviceLimit.ServiceGroupName, scope, participantName);

        await NotifyServiceLimitChangeAsync(serviceLimit.ServiceGroupName, serviceLimit.Id, scope, participantName);

        return serviceLimit.Map();
    }

    /// <remarks>
    /// Changes the thresholds of a spending limit on the AI services, or turns it off and on with `enabled`. If the
    /// portal has no billing customer, the call returns 404. Requires a DocSpace admin. Other callers get 403. The
    /// call also returns 403 if the installation has no billing service. The call changes data, but only in the
    /// fields that you send. An omitted threshold keeps its value, so you cannot remove a threshold here. To remove
    /// one, delete the limit with `DELETE api/2.0/portal/payment/servicelimit/{id}`. Then create it again with
    /// `POST api/2.0/portal/payment/servicelimit`. This is also the way to change the scope, the user, or the period.
    /// A turned-off limit stops restricting the AI services immediately. A new threshold applies from the next AI
    /// operation. If it is below the spending, the AI services stop until the period ends. You can repeat the call
    /// with the same body. The call returns 404 if the portal has no AI service limit with this ID. The accounting
    /// service can reject a threshold with 400. An example is a quantity for services with different units.
    /// </remarks>
    /// <summary>
    /// Update a service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service limit as it was stored", typeof(ServiceLimitDto))]
    [SwaggerResponse(400, "The ID is zero or negative, a value is outside its range, or the accounting service rejects a threshold, such as a quantity for services with different units")]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, or the portal has no limit on the AI services with this ID")]
    [HttpPut("servicelimit")]
    public async Task<ServiceLimitDto> UpdateServiceLimit(UpdateServiceLimitRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var serviceLimit = await tariffService.UpdateServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices, inDto.Id, inDto.AmountValue, inDto.QuantityValue, inDto.Enabled)
            ?? throw new ItemNotFoundException("Service limit could not be found");

        await SendServiceLimitAuditMessageAsync(MessageAction.CustomerServiceLimitUpdated, serviceLimit.ServiceGroupName, serviceLimit.Scope, serviceLimit.Participant);

        await NotifyServiceLimitChangeAsync(serviceLimit.ServiceGroupName, serviceLimit.Id, serviceLimit.Scope, serviceLimit.Participant);

        return serviceLimit.Map();
    }

    /// <remarks>
    /// Sets the same personal spending limit on the AI services for each listed portal user. It replaces the personal
    /// limit that a user already has and turns it on. If the portal has no billing customer, the call returns 404.
    /// Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no billing
    /// service. The call returns 400 if neither `amountValue` nor `quantityValue` is set. If a user in `userIds` does
    /// not exist, the call returns 404. The call changes data. A user listed twice gets one limit. The call returns
    /// 400 for an empty list or more than 1000 users. So split a larger group into several calls. Unlike
    /// `PUT api/2.0/portal/payment/servicelimit`, this call sets the whole limit. So a threshold that you leave out
    /// is removed. The period can change. A lifetime limit that stays lifetime keeps its count. Each personal limit
    /// applies instead of the limit for every user. The AI services also check the limit of the portal. You can
    /// repeat the call with the same body. The accounting service can reject a threshold with 400. The call returns
    /// the number of personal limits that it set. To list them, use
    /// `GET api/2.0/portal/payment/servicelimit/participants`.
    /// </remarks>
    /// <summary>
    /// Set the user service limits
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/participants</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The number of personal limits that were set", typeof(int))]
    [SwaggerResponse(400, "`userIds` is empty or lists more than 1000 users, neither `amountValue` nor `quantityValue` is given, a value is outside its range, or the accounting service rejects a threshold, such as a quantity for services with different units")]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, or one of the users does not exist")]
    [HttpPut("servicelimit/participants")]
    public async Task<int> SetParticipantServiceLimits(SetParticipantServiceLimitsRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        if (!inDto.AmountValue.HasValue && !inDto.QuantityValue.HasValue)
        {
            throw new ArgumentException("Either the amount or the quantity threshold must be set");
        }

        var userIds = inDto.UserIds.Distinct().ToList();
        foreach (var userId in userIds)
        {
            await DemandPortalUserAsync(userId);
        }

        var participantNames = userIds.Select(userId => userId.ToString()).ToList();

        var count = await tariffService.SetParticipantServiceLimitsAsync(tenantId, ServiceLimitGroups.AiServices, participantNames,
            inDto.AmountValue, inDto.QuantityValue, inDto.Period);

        // The explicit type argument picks the list overload; with a List<Guid> the compiler takes the single-value one.
        messageService.Send(MessageAction.CustomerServiceLimitsSet, MessageTarget.Create<Guid>(userIds), ServiceLimitGroups.AiServices);

        // The users who got the limit, and the administrators, who see it in the list of the users.
        var recipients = await GetAdministratorIdsAsync();
        recipients.UnionWith(userIds);

        await quotaSocketManager.ChangeServiceLimitsAsync(ServiceLimitGroups.AiServices, userIds: recipients);

        return count;
    }

    /// <remarks>
    /// Turns off every personal spending limit on the AI services. Each user then falls back to the limit for every
    /// user, if it is set. The limit of the portal and the limit for every user do not change. If the portal has no
    /// billing customer, the call returns 404. Requires a DocSpace admin. Other callers get 403. The call also
    /// returns 403 if the installation has no billing service. The turned-off limits stop restricting their users
    /// immediately. The portal keeps them, so you can turn each one back on with
    /// `PUT api/2.0/portal/payment/servicelimit`. The call changes data, and a repeated call turns off nothing more.
    /// The call returns the number of limits that it turned off, or 0 if none was in force. To turn off the limit of
    /// one user, use `PUT api/2.0/portal/payment/servicelimit` with `enabled` set to false.
    /// </remarks>
    /// <summary>
    /// Disable the user service limits
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/participants/disable</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The number of personal limits that were switched off", typeof(int))]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer")]
    [HttpPut("servicelimit/participants/disable")]
    public async Task<int> DisableParticipantServiceLimits()
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var count = await tariffService.DisableParticipantServiceLimitsAsync(tenantId, ServiceLimitGroups.AiServices);

        messageService.Send(MessageAction.CustomerServiceLimitsDisabled, ServiceLimitGroups.AiServices);

        // The accounting service reports how many personal limits it switched off, not whose, so every user re-reads.
        await quotaSocketManager.ChangeServiceLimitsAsync(ServiceLimitGroups.AiServices);

        return count;
    }

    /// <remarks>
    /// Deletes one spending limit on the AI services. This can be the limit of the portal, the limit for every user,
    /// or a personal limit. If the portal has no billing customer, the call returns 404. Requires a DocSpace admin.
    /// Other callers get 403. The call also returns 403 if the installation has no billing service. The call cannot
    /// be undone. To keep the settings of a limit, turn it off with `PUT api/2.0/portal/payment/servicelimit`
    /// instead. To get a deleted limit back, create a new one with `POST api/2.0/portal/payment/servicelimit`. After
    /// a personal limit is deleted, the limit for every user applies to that user. The call returns 404 if the portal
    /// has no AI service limit with this ID. A repeated call returns 404. The response has no body.
    /// </remarks>
    /// <summary>
    /// Delete a service limit
    /// </summary>
    /// <path>api/2.0/portal/payment/servicelimit/{id}</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service limit has been deleted; the response carries no content")]
    [SwaggerResponse(400, "The ID is zero or negative")]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "The portal has no billing customer, or the portal has no limit on the AI services with this ID")]
    [HttpDelete("servicelimit/{id:int}")]
    public async Task DeleteServiceLimit(ServiceLimitIdRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var serviceLimit = await tariffService.DeleteServiceLimitAsync(tenantId, ServiceLimitGroups.AiServices, inDto.Id)
            ?? throw new ItemNotFoundException("Service limit could not be found");

        await SendServiceLimitAuditMessageAsync(MessageAction.CustomerServiceLimitDeleted, serviceLimit.ServiceGroupName, serviceLimit.Scope, serviceLimit.Participant);

        await NotifyServiceLimitChangeAsync(serviceLimit.ServiceGroupName, serviceLimit.Id, serviceLimit.Scope, serviceLimit.Participant);
    }

    // The event goes to whoever may read the limit: the limit for every user to every user, as each reads it as their own
    // limit; the limit of the portal as a whole to the administrators only; a personal limit to its user and to the
    // administrators, who see it in the list of the users. The event only asks the clients to re-read, so it carries no
    // thresholds.
    private async Task NotifyServiceLimitChangeAsync(string serviceGroupName, int id, ServiceLimitScope scope, string participantName)
    {
        if (scope == ServiceLimitScope.Global)
        {
            await quotaSocketManager.ChangeServiceLimitsAsync(serviceGroupName, id);
            return;
        }

        var userIds = await GetAdministratorIdsAsync();
        if (Guid.TryParse(participantName, out var userId))
        {
            userIds.Add(userId);
        }

        await quotaSocketManager.ChangeServiceLimitsAsync(serviceGroupName, id, userIds);
    }

    // A personal limit is set only for a real user of the portal. The system accounts count as existing users, but they
    // never use the AI services, so a limit for one is refused like one for a user that does not exist.
    private async Task DemandPortalUserAsync(Guid userId)
    {
        if (userManager.IsSystemUser(userId) || !await userManager.UserExistsAsync(userId))
        {
            throw new ItemNotFoundException($"User {userId} could not be found");
        }
    }

    // The owner is a member of the administrators group too.
    private async Task<HashSet<Guid>> GetAdministratorIdsAsync()
    {
        var administrators = await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID);

        return administrators.Select(administrator => administrator.Id).ToHashSet();
    }

    // The audit record names the service group and, as its second parameter, the scope of the limit - for a personal
    // limit together with the user it is set for. The user ID goes into the target, because the display name may
    // change later.
    private async Task SendServiceLimitAuditMessageAsync(MessageAction action, string serviceGroupName, ServiceLimitScope scope, string participantName)
    {
        if (!Guid.TryParse(participantName, out var userId))
        {
            messageService.Send(action, null, serviceGroupName, scope.ToString());
            return;
        }

        var userName = await displayUserSettingsHelper.GetFullUserNameAsync(userId, false, false);

        messageService.Send(action, MessageTarget.Create(userId), serviceGroupName, $"{scope} ({userName})");
    }

    /// <remarks>
    /// Returns the automatic top-up settings of the portal wallet. Requires a DocSpace admin. Other callers get 403.
    /// The portal does not need a billing customer or a billing service. The call is read-only. If the portal never
    /// set up the top-up, the call returns the defaults. Then `enabled` is false, `currency` is null, and
    /// `minBalance` and `upToBalance` are 0. These zeros are out of range for
    /// `POST api/2.0/portal/payment/topupsettings`. So send real values instead of the defaults. Until the portal
    /// first saves the settings, `lastModified` is `0001-01-01T00:00:00`. The portal sets `lowBalanceThreshold` and
    /// `lowBalanceNotified`, and you cannot change them.
    /// </remarks>
    /// <summary>
    /// Get the auto top-up settings
    /// </summary>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The automatic top-up settings of the portal, or their defaults when it has never configured them", typeof(TenantWalletSettingsDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("topupsettings")]
    public async Task<TenantWalletSettingsDto> GetTenantWalletSettings()
    {
        await paymentHelper.DemandAdminAsync();

        var result = await settingsManager.LoadAsync<TenantWalletSettings>();
        return result.Map();
    }

    /// <remarks>
    /// Turns the automatic top-up of the portal wallet on or off and sets its thresholds. While it is on, the portal
    /// charges the payment method when the balance falls below `minBalance`. The charge brings the balance up to
    /// `upToBalance` in `currency`. If the portal has no billing customer or no wallet balance, the call returns 404.
    /// To create the balance, top up the wallet once with `POST api/2.0/portal/payment/deposit`. The payer is the
    /// portal user whose email is the email of the billing customer. Only the payer can call it. Other callers
    /// get 403. The call also returns 403 if the installation has no billing service. The body replaces all stored
    /// settings, and an omitted body resets them to the defaults. The call returns 400 if `minBalance` or
    /// `upToBalance` is missing or out of range. It also returns 400 if `upToBalance` is not greater than
    /// `minBalance`. The call accepts `lowBalanceThreshold`, `lowBalanceNotified`, and `lastModified` but ignores
    /// them. The call changes data but charges nothing. You can repeat it with the same body. Turning the top-up on
    /// also resets the low-balance warning. The call returns the settings as the portal saved them.
    /// </remarks>
    /// <summary>
    /// Set the auto top-up settings
    /// </summary>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The automatic top-up settings as they were stored", typeof(TenantWalletSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read, or `settings` is sent with `minBalance` outside 5-1000 or `upToBalance` outside 6-5000, including when either of them is left out, or with `upToBalance` not greater than `minBalance`")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no balance yet")]
    [HttpPost("topupsettings")]
    public async Task<TenantWalletSettingsDto> SetTenantWalletSettings(TenantWalletSettingsRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        // The request carries only what a payer chooses; the low-balance warning state is always carried over from
        // what was stored, so a stale GET->POST round-trip cannot resurrect an old value.
        var existing = await settingsManager.LoadAsync<TenantWalletSettings>();
        var settings = new TenantWalletSettings
        {
            Enabled = inDto?.Settings?.Enabled ?? false,
            MinBalance = inDto?.Settings?.MinBalance ?? 0,
            UpToBalance = inDto?.Settings?.UpToBalance ?? 0,
            Currency = inDto?.Settings?.Currency,
            LowBalanceThreshold = existing.LowBalanceThreshold,
            LowBalanceNotified = existing.LowBalanceNotified
        };

        if (settings.Enabled)
        {
            settings.LowBalanceNotified = false;
        }
        else
        {
            // keep the settings row persisted (not equal to GetDefault()) even when auto top-up is
            // turned off, so the low-balance poller can still discover this tenant
            settings.LowBalanceThreshold = paymentHelper.GetDefaultLowBalanceThreshold();
        }

        var result = await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomerWalletTopUpSettingsUpdated);

        return (await settingsManager.LoadAsync<TenantWalletSettings>()).Map();
    }

    /// <remarks>
    /// Returns the wallet services that an admin turned on for the portal by hand. The services that the plan pays
    /// for are not listed. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the
    /// installation has no billing service. The portal does not need a billing customer. The call is read-only. If no
    /// service was turned on, `enabledServices` is empty. To read the services that run on the portal, use
    /// `GET api/2.0/portal/payment/activeservices`. To change one entry, use
    /// `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get the wallet service settings
    /// </summary>
    /// <path>api/2.0/portal/payment/servicessettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services switched on by hand for this portal", typeof(TenantWalletServiceSettingsDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("servicessettings")]
    public async Task<TenantWalletServiceSettingsDto> GetTenantWalletServiceSettings()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>();

        return settings.Map();
    }

    /// <remarks>
    /// Turns one wallet service on or off for the portal. If the portal has no billing customer, the call
    /// returns 404. Requires a DocSpace admin. Other callers get 403. The call also returns 403 if the installation has no
    /// billing service. AI tools must be on before you turn on AI search. If it is not, the call returns 403. Turning
    /// off AI tools also turns off AI search. If AI is turned off for the portal, turning on an AI service
    /// returns 403. The call changes data, and you can repeat it with the same body. The call returns all wallet services
    /// that are on after the change. If `service` is not a wallet service, the call returns 400. Turning a service on
    /// does not buy it. To buy units, use `PUT api/2.0/portal/payment/updatewallet`.
    /// </remarks>
    /// <summary>
    /// Switch a wallet service
    /// </summary>
    /// <path>api/2.0/portal/payment/servicestate</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The whole set of wallet services switched on for the portal after the change", typeof(TenantWalletServiceSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read, including a `service` name that is not a wallet service")]
    [SwaggerResponse(403, "The caller has no portal-settings right or is not a DocSpace administrator, the portal has no billing service configured, AI tools or AI search was switched on while AI is disabled for the portal, or AI search was switched on while AI tools is off")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPost("servicestate")]
    public async Task<TenantWalletServiceSettingsDto> ChangeTenantWalletServiceState(ChangeWalletServiceStateRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var settings = await paymentHelper.ChangeWalletServiceStateAsync(inDto.Service, inDto.Enabled);

        return settings.Map();
    }

    /// <remarks>
    /// Returns the prices of the AI features that the portal pays for from its wallet. The list covers chat models,
    /// embedding models, image models, and web search providers. Requires a DocSpace admin. Other callers get 403. It
    /// also returns 403 if the installation has no billing service or no AI gateway. The call is read-only. Token
    /// prices are per million tokens, and all prices are in the `currency` of the answer. Each entry has the model ID
    /// that the AI operations take. The list does not show what the portal spent. To read the spending, use
    /// `GET api/2.0/portal/payment/customer/usage`. To read the models that the portal bars, use
    /// `GET api/2.0/portal/payment/ai-model/restrictions`. If the AI gateway fails or cannot be reached, the call
    /// returns 500.
    /// </remarks>
    /// <summary>
    /// Get AI model prices
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The prices of the chat, embedding and image models and of the web search providers, with the currency they are in", typeof(AiPricesDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation has no billing service or no AI gateway configured")]
    [SwaggerResponse(500, "The AI gateway answers with an error or cannot be reached")]
    [HttpGet("ai-prices")]
    public async Task<AiPricesDto> GetAiPrices()
    {
        paymentHelper.DemandAiGatewayConfiguration();

        await paymentHelper.DemandAdminAsync();

        return await paymentHelper.GetAiPricesAsync();
    }

    /// <remarks>
    /// Returns the AI chat models that no user of the portal can pick. Requires a DocSpace admin. Other callers
    /// get 403. The call is read-only. If the installation has no billing service or AI tools is off, the call returns an
    /// empty set. That answer looks the same as a portal without restrictions. An empty `models` means that every
    /// model of `GET api/2.0/portal/payment/ai-prices` is allowed. The call returns 402 if AI tools is on but the
    /// billing service has no customer or fails. If the AI gateway fails or cannot be reached, the call returns 500.
    /// To replace the set, use `PUT api/2.0/portal/payment/ai-model/restrictions`.
    /// </remarks>
    /// <summary>
    /// Get restricted AI models
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The identifiers of the AI chat models barred on this portal, empty when none is", typeof(RestrictedAiModelsDto))]
    [SwaggerResponse(402, "AI tools is on for the portal, but the billing service reports no customer for it or fails to answer")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [SwaggerResponse(500, "The AI gateway answers with an error or cannot be reached")]
    [HttpGet("ai-model/restrictions")]
    public async Task<RestrictedAiModelsDto> GetRestrictedAiModels()
    {
        if (!tariffService.IsConfigured() || !await aiGateway.IsAiEnabledAsync())
        {
            return new RestrictedAiModelsDto { Models = [] };
        }

        await paymentHelper.DemandAdminAsync();

        return (await aiGateway.GetRestrictedModelsAsync())?.Map();
    }

    /// <remarks>
    /// Replaces the whole set of AI chat models that are barred on the portal. To bar one more model, send it
    /// together with the models already barred. To lift a restriction, leave the model out. An empty set lifts all
    /// restrictions. First, read the current set from `GET api/2.0/portal/payment/ai-model/restrictions`. Take the
    /// model IDs from `GET api/2.0/portal/payment/ai-prices`. If the portal has no billing customer, the call
    /// returns 404. AI tools must be on, and you turn it on with `POST api/2.0/portal/payment/servicestate`. While AI tools
    /// is off, the call returns 403. Requires a DocSpace admin. Other callers get 403. It also returns 403 if the
    /// installation has no billing service or no AI gateway. The call changes data, and you can repeat it with the
    /// same body. The set applies from the next AI request. So an open conversation on a barred model cannot go on
    /// with it. The call returns the stored set. If `models` is missing or has a null entry, the call returns 400. If
    /// the AI gateway fails or cannot be reached, the call returns 500.
    /// </remarks>
    /// <summary>
    /// Set restricted AI models
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The set of barred AI chat models as it was stored", typeof(RestrictedAiModelsDto))]
    [SwaggerResponse(400, "The request body cannot be read, or `models` is missing, `null` or holds a `null` entry")]
    [SwaggerResponse(403, "The caller has no portal-settings right or is not a DocSpace administrator, the installation has no billing service or no AI gateway configured, or the AI tools wallet service is off for the portal")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [SwaggerResponse(500, "The AI gateway answers with an error or cannot be reached")]
    [HttpPut("ai-model/restrictions")]
    public async Task<RestrictedAiModelsDto> SetRestrictedAiModels(SetRestrictedAiModelsRequestDto inDto)
    {
        paymentHelper.DemandAiGatewayConfiguration();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        await paymentHelper.DemandAiEnabledAsync();

        return (await paymentHelper.SetRestrictedAiModelsAsync(inDto.Models))?.Map();
    }
}
