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
    PaymentHelper paymentHelper)
    : ControllerBase
{
    /// <remarks>
    /// Starts the purchase of a monthly paid plan for this portal by handing back the hosted checkout page the buyer
    /// has to open; nothing is bought until that page is completed. The portal must have no paid plan yet - a portal
    /// whose plan is already paid gets an empty result and changes its subscription through
    /// `PUT api/2.0/portal/payment/update` instead - and the product name in `quantity` must be one of the monthly,
    /// non-wallet plans listed by `GET api/2.0/portal/payment/quotas`. Only a DocSpace administrator may call it. The
    /// call itself changes nothing on the portal and may be repeated: the money is taken by the payment provider on
    /// the checkout page, and the plan becomes active once the provider confirms it. The returned URL is absolute and
    /// single-purpose - it carries the caller's e-mail, the language of the request and the currency of the request
    /// region, and it redirects to `successUrl` or `backUrl` when the buyer finishes or cancels. Exactly one product
    /// per call is accepted and its quantity has to be greater than zero; yearly and wallet products are refused, and
    /// wallet services are bought with `PUT api/2.0/portal/payment/updatewallet` instead.
    /// </remarks>
    /// <summary>
    /// Get the payment page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/url</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The absolute URL of the checkout page to open, or an empty result when the portal already has a paid plan", typeof(Uri))]
    [SwaggerResponse(400, "`quantity` holds more than one product, a quantity that is not greater than zero, or a product that is not a monthly plan")]
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
    /// Changes how many units of the plan the portal is paying for - the number of administrators it covers - and
    /// lets the payment provider bill the difference against the payment method already on file. The portal must have
    /// a billing customer and a plan bought through `PUT api/2.0/portal/payment/url`, and while the portal is on a
    /// priced plan the product name in `quantity` has to be that same plan, which `GET api/2.0/portal/payment/quota`
    /// reports, because a subscription is changed here and not swapped. Only the payer - the portal user whose e-mail
    /// is the billing customer's e-mail - may call it. The call is mutating and charges money, and it is guarded
    /// against a double submission: once the new quantity is in effect, repeating the same request fails with 400
    /// because that quantity is already set. The result is `true` when the provider accepted the change and `false`
    /// when it declined it without an error. Exactly one product per call is accepted, the operation is limited to
    /// ten requests a minute per user by default and answers 429 above that, and wallet services are not bought here
    /// - use `PUT api/2.0/portal/payment/updatewallet` for those.
    /// </remarks>
    /// <summary>
    /// Change the subscription quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/update</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the provider accepted the new quantity, `false` when it declined it", typeof(bool))]
    [SwaggerResponse(400, "The product is not the plan currently paid, or the quantity is already the one in effect")]
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
    /// Buys more units of a wallet service - extra administrators, disk storage, backup, AI tools, AI search or
    /// DocsCloud - or writes down the quantity that service will have after the next renewal, depending on
    /// `productQuantityType`. With `Add` (1) the units are bought at once and paid out of the portal wallet, so the
    /// wallet needs a sub-account in the accounting currency and enough money on it; with `Set` (0) nothing is
    /// charged now and the quantity only takes effect in the next period, where an empty or zero quantity cancels a
    /// change scheduled earlier. `Renew` and `Sub` are not accepted here. The portal needs a billing customer and the
    /// caller has to be a DocSpace administrator; a service that is an add-on to the plan also needs the plan itself
    /// to be paid, otherwise the answer is 402. Minimum quantities apply - disk storage starts at 100 units, the
    /// DocsCloud developer pack at 10, and the administrators may not be fewer than the portal already has - and in
    /// the `Add` form they are checked only while the portal does not hold that service yet. Asking for the DocsCloud
    /// plan in the `Set` form while the developer pack is active schedules the reversion to it at the next period,
    /// while the upgrade in the other direction is not done here at all: use
    /// `POST api/2.0/settings/docscloud/switchtodevpack`. The result is `true` when the change was accepted; the call
    /// is mutating, spends money in its `Add` form and is limited to ten requests a minute per user by default. Price
    /// the same purchase without paying for it with `PUT api/2.0/portal/payment/calculatewallet`.
    /// </remarks>
    /// <summary>
    /// Change a wallet service quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/updatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the purchase or the scheduled change was accepted, `false` when the provider declined it", typeof(bool))]
    [SwaggerResponse(400, "The quantity type is not `Set` or `Add`, the product is not a wallet service, the quantity is below the minimum for it, or that service is already set")]
    [SwaggerResponse(402, "The plan of the portal is not paid and the requested service is an add-on to it")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no sub-account in the accounting currency")]
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
    /// Prices a wallet-service purchase without making it: it returns what buying the requested number of units would
    /// cost right now, so a client can show the amount before asking for a confirmation. Only `productQuantityType`
    /// `Add` (1) is accepted, the quantity must be greater than zero, and the portal needs a billing customer whose
    /// wallet has a sub-account in the accounting currency. The caller has to be a DocSpace administrator. Nothing is
    /// bought, charged or written down - the call is read-only and may be repeated - and the purchase itself is
    /// `PUT api/2.0/portal/payment/updatewallet`. The answer carries the amount with its currency, the quantity it
    /// was computed for and the identifier of the calculation. It is the price of this moment and is not held: it can
    /// differ by the time the purchase is made.
    /// </remarks>
    /// <summary>
    /// Calculate the wallet payment amount
    /// </summary>
    /// <path>api/2.0/portal/payment/calculatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The amount the purchase would cost, its currency and the quantity it was calculated for", typeof(PaymentCalculation))]
    [SwaggerResponse(400, "The quantity type is not `Add`, the quantity is not greater than zero, or the product is not a wallet service")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no sub-account in the accounting currency")]
    [HttpPut("calculatewallet")]
    public async Task<PaymentCalculation> CalculateWalletPayment(WalletQuantityRequestDto inDto)
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

        return result;
    }

    /// <remarks>
    /// Reports in money how much of the portal's paid subscription period is still unused - the credit that
    /// `POST api/2.0/portal/payment/subscription/movetowallet` would carry over to the wallet if the subscription
    /// were ended now. The portal must have a billing customer and a plan in the paid state; a plan that is not paid
    /// answers 402, and a paid plan without a subscription row gives 404. Only the payer - the portal user whose
    /// e-mail is the billing customer's e-mail - may read it, and the call is read-only. The answer states the total
    /// cost of the current period with its currency, the start and the end of that period in UTC, the moment the
    /// unused part is measured up to, the days already elapsed, and the remaining balance both in the subscription
    /// currency and converted to the wallet currency. Every figure is computed for the instant of the request, so it
    /// changes between calls.
    /// </remarks>
    /// <summary>
    /// Get the subscription balance information
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The unused balance of the current subscription period with its period boundaries and currencies", typeof(SubscriptionBalanceInfo))]
    [SwaggerResponse(400, "The plan currently paid is a wallet product or has no product identifier")]
    [SwaggerResponse(402, "The plan of the portal is not in the paid state")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its paid plan has no subscription")]
    [HttpGet("subscription/balance")]
    public async Task<SubscriptionBalanceInfo> GetSubscriptionBalanceInfo()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var productId = await paymentHelper.GetCurrentSubscriptionProductIdAsync(tenant.Id);

        return await tariffService.GetSubscriptionBalanceInfoAsync(tenant.Id, productId);
    }

    /// <remarks>
    /// Ends the portal's paid subscription and moves it onto the wallet: the unused balance of the running period is
    /// credited to the wallet, the wallet is topped up from the payment method on file if that credit does not cover
    /// the purchase, and the requested number of administrators is then bought as a wallet service. The portal needs
    /// a billing customer with a payment method set and a plan in the paid state, `quantity` has to name the
    /// administrators wallet product, and the number asked for may not be below the administrators the portal already
    /// has - read the credit that will be carried over from `GET api/2.0/portal/payment/subscription/balance` first.
    /// Only the payer may call it. The call is mutating, spends money and cannot be undone: the subscription is ended
    /// before the purchase is attempted, so a failure in the second half leaves the portal on the wallet with the
    /// money credited but the administrators unbought, and a repeat would then buy them a second time. It is limited
    /// to ten requests a minute per user by default. The result is `true` when the administrators were bought.
    /// </remarks>
    /// <summary>
    /// Move the subscription to the wallet
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/movetowallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "`true` when the balance was moved to the wallet and the administrators were bought", typeof(bool))]
    [SwaggerResponse(400, "`quantity` does not name the administrators wallet product, or the number asked for is below the administrators the portal already has")]
    [SwaggerResponse(402, "The plan of the portal is not paid, the balance could not be moved, or the wallet is still short of the price after the top-up")]
    [SwaggerResponse(403, "The caller is not the payer of this portal, the portal has no billing service configured, or the customer has no payment method set")]
    [SwaggerResponse(404, "This portal has no billing customer, its paid plan has no subscription, or the price of the administrators product is unknown")]
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

        // Calculate the cost of the requested admins from the known quota price (price * quantity).
        var walletQuotas = await tariffHelper.GetQuotasAsync(wallet: true).ToListAsync();
        var quotaDto = walletQuotas.FirstOrDefault(q => q.Id == quota.TenantId);
        if (quotaDto?.Price?.Value is not { } unitPrice)
        {
            throw new ItemNotFoundException("Quota price could not be found");
        }

        var requiredAmount = unitPrice * productQty;

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
    /// Hands back the address of the portal page on which the billing account is managed - the payment method on
    /// file, the invoices and the receipts - so a client can link to it instead of assembling the address itself. The
    /// portal must already have a billing customer: one that has never had it gets an empty result, and an
    /// installation without a billing service answers 403. Only the payer or the portal owner may read it, and the
    /// call changes nothing. The value is relative to the portal root (`payment.ashx`), and the optional `backUrl` is
    /// appended to it as a query parameter so the page can send the user back where they came from. It is not a
    /// checkout page: a plan is bought with `PUT api/2.0/portal/payment/url` and a payment method is attached with
    /// `GET api/2.0/portal/payment/checkoutsetupurl`.
    /// </remarks>
    /// <summary>
    /// Get the billing account page
    /// </summary>
    /// <path>api/2.0/portal/payment/account</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The portal-relative address of the billing account page, or an empty result when the portal has no billing customer", typeof(string))]
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
    /// Lists what one unit of every purchasable product costs, keyed by the product name that `quantity` takes in the
    /// purchase operations, so a client can price a plan or a wallet service without reading the whole quota list.
    /// Nothing has to be called first, and the caller needs the permission to edit the portal settings, which portal
    /// administrators and the owner have. The call is read-only. Prices are given in the one currency resolved for
    /// this request from the portal region, which `GET api/2.0/portal/payment/currencies` reports; a product with no
    /// price in that currency comes back as `0` rather than being left out, so a zero means unpriced and not free.
    /// The list covers the products on offer, not the portal's own plan - the plan in force, with its limits and its
    /// usage, is `GET api/2.0/portal/payment/quota`.
    /// </remarks>
    /// <summary>
    /// Get the product prices
    /// </summary>
    /// <path>api/2.0/portal/payment/prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Product name to the price of one unit in the currency of the request, `0` where the product has no price in it", typeof(Dictionary<string, decimal>))]
    [SwaggerResponse(403, "The caller may not edit the portal settings")]
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
    /// Tells a client which currency the portal is billed in: the default currency of the portal region always comes
    /// first, followed by the currency resolved for the current request when that one differs, so the answer holds
    /// one or two items. Nothing has to be called first, the caller needs the permission to edit the portal settings,
    /// and the call is read-only. Each item carries the country code of the region, the currency symbol and the
    /// native name of the currency; the first item is the currency the amounts from
    /// `GET api/2.0/portal/payment/prices` are expressed in. These are the currencies of the subscription prices, and
    /// they are not the accounting currencies the wallet is topped up in - those come with the balance in
    /// `GET api/2.0/portal/payment/customer/balance`.
    /// </remarks>
    /// <summary>
    /// Get the billing currencies
    /// </summary>
    /// <path>api/2.0/portal/payment/currencies</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The default currency of the portal region first, followed by the currency of the current request when it differs", typeof(IAsyncEnumerable<CurrenciesDto>))]
    [SwaggerResponse(403, "The caller may not edit the portal settings")]
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
    /// Lists the quotas the portal can be put on - the paid plans and the wallet services - each with its price, its
    /// features and the limits it grants, which is what a pricing page is built from. Nothing has to be called first,
    /// the caller needs the permission to edit the portal settings, and the call is read-only. Only quotas marked
    /// visible are listed, newest first, and the two optional filters narrow that: `wallet` selects the wallet
    /// services (`true`) or the subscription plans (`false`), `additional` selects the add-ons to a plan (`true`) or
    /// the plans themselves (`false`), and an omitted filter keeps both kinds. A portal on a non-profit quota is a
    /// special case - asking for `additional=false` returns that single quota and nothing else, because no other plan
    /// may be bought for it. The quota the portal is actually on is not marked here; read it from
    /// `GET api/2.0/portal/payment/quota`.
    /// </remarks>
    /// <summary>
    /// Get the purchasable quotas
    /// </summary>
    /// <path>api/2.0/portal/payment/quotas</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The visible quotas matching the filters, newest first, each with its price, features and limits", typeof(IEnumerable<QuotaDto>))]
    [SwaggerResponse(403, "The caller may not edit the portal settings")]
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
    /// Lists every service the portal may pay for out of its wallet - extra administrators, disk storage, backup, AI
    /// tools, AI search and DocsCloud - with the price of a unit, the unit it is sold in and whether the portal has
    /// it switched on. Nothing has to be called first, the caller needs the permission to edit the portal settings,
    /// and the call is read-only. Services that are variants of one another are folded together: the visible one
    /// carries the rest in its `innerServices`, so a client renders one card per group. The AI services are left out
    /// entirely when AI is not enabled for the portal. This is the catalogue and not the state of the portal - what
    /// is actually running is `GET api/2.0/portal/payment/activeservices`, one service on its own is
    /// `GET api/2.0/portal/payment/walletservice`, and switching one on or off is
    /// `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservices</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services on offer, with their prices, units and grouped variants", typeof(IEnumerable<WalletServiceDto>))]
    [SwaggerResponse(403, "The caller may not edit the portal settings")]
    [HttpGet("walletservices")]
    public async Task<IEnumerable<WalletServiceDto>> GetWalletServices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await tariffHelper.GetWalletServicesAsync();
    }

    /// <remarks>
    /// Returns one wallet service by name, for a client that already knows which service it needs and does not want
    /// the whole catalogue. `service` is the name of the service - `Storage`, `Backup`, `AITools`, `Admin`,
    /// `DocsCloud`, `DocsCloudDevPack` or `AISearch` - and a name this installation does not sell answers 404.
    /// Nothing has to be called first, the caller needs the permission to edit the portal settings, and the call is
    /// read-only. The answer has the same shape as one item of `GET api/2.0/portal/payment/walletservices` - the
    /// price of a unit, the unit, the limits the service grants and its service name - except that the variants of a
    /// service are not grouped into `innerServices` here, because a single service is looked up directly. The price
    /// is in the currency resolved for the request.
    /// </remarks>
    /// <summary>
    /// Get a wallet service
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservice</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet service with its price, unit and the limits it grants", typeof(WalletServiceDto))]
    [SwaggerResponse(403, "The caller may not edit the portal settings")]
    [SwaggerResponse(404, "This installation does not sell a wallet service under that name")]
    [HttpGet("walletservice")]
    public async Task<WalletServiceDto> GetWalletService(GetWalletServiceRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quotaList = await quotaService.GetTenantQuotasAsync();
        var quota = quotaList.FirstOrDefault(q => q.Wallet && q.TenantId == (int)inDto.Service);
        if (quota == null)
        {
            throw new ItemNotFoundException("Service could not be found");
        }

        var quotaDto = await tariffHelper.ToQuotaDtoAsync(quota, false);
        var walletServiceDto = quotaDto.MapToWalletServiceDto();
        walletServiceDto.ServiceName = quota.ServiceName;
        return walletServiceDto;
    }

    /// <remarks>
    /// Returns the quota the portal is on right now - its paid plan or the free one - with everything a client needs
    /// to render itself: the price, the features that are switched on, the limits they grant (rooms, storage in
    /// bytes, users, administrators, AI) and how much of each is already used. Every signed-in member of the portal
    /// reads it, so it is not restricted to administrators; only guests are refused with 403. The call is read-only.
    /// The plan is served from the cache by default, which is what a start-up needs; `refresh=true` fetches it from
    /// the billing service instead, so use that right after a purchase and not routinely, because it is a remote
    /// call. The catalogue of the quotas that could be bought instead is `GET api/2.0/portal/payment/quotas`, and the
    /// money side of the same portal - customer, wallet and balance - starts at
    /// `GET api/2.0/portal/payment/customerinfo`.
    /// </remarks>
    /// <summary>
    /// Get the current plan and limits
    /// </summary>
    /// <path>api/2.0/portal/payment/quota</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The quota the portal is on, with its price, features, limits and current usage", typeof(QuotaDto))]
    [SwaggerResponse(403, "The caller is a guest of this portal")]
    [HttpGet("quota")]
    public async Task<QuotaDto> GetQuotaPaymentInformation(PaymentInformationRequestDto inDto)
    {
        // Every non-guest member reads this at start-up: the client's AuthStore.getPaymentInfo needs
        // the plan's feature limits (rooms, storage, AI agents) to render at all, and rethrows on
        // failure. Narrowing this to administrators leaves a RoomAdmin or a User with a blank page.
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        return await tariffHelper.GetCurrentQuotaAsync(inDto.Refresh);
    }

    /// <remarks>
    /// Sends the portal's message to the ONLYOFFICE sales team - the contact-sales form behind a request for a quote,
    /// an invoice or a plan that cannot be bought online. `email` has to be a well-formed address and is where the
    /// answer will go, while `userName` and `message` say who is asking and what for; all three are required and none
    /// may be empty. Only a DocSpace administrator may call it. Nothing on the portal changes: no plan, no quota and
    /// no payment is touched, a message is mailed out and the request is written to the portal audit trail. There is
    /// no response body - status 200 means the message was handed to the mail service - and the call is not
    /// idempotent, so a repeat sends a second message. It is limited to ten requests a minute per user by default and
    /// answers 429 above that.
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
    /// Hands back the hosted page on which a payment method is attached to the portal's billing account, for the case
    /// where money has to be taken later - a wallet top-up or an automatic one - rather than a plan bought now. A
    /// portal that already has a payment method on file answers with an empty result; a DocSpace administrator may
    /// ask for the page, but once the portal has a billing customer with an e-mail, only its payer may. The call
    /// itself changes nothing and may be repeated: the payment method is stored by the payment provider when the
    /// returned page is completed, after which `GET api/2.0/portal/payment/customerinfo` reports it as set. The URL
    /// is absolute, carries the caller's e-mail, the language of the request and the currency of the region, and
    /// redirects to `successUrl` or `backUrl` when the user finishes or cancels. It buys nothing - a plan is bought
    /// with `PUT api/2.0/portal/payment/url`.
    /// </remarks>
    /// <summary>
    /// Get the checkout setup page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/checkoutsetupurl</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The absolute URL of the payment method setup page, or an empty result when the portal already has a payment method", typeof(Uri))]
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
    /// Returns the billing customer behind the portal: the e-mail its billing account is registered to, whether a
    /// payment method is stored for it, and the portal user who is the payer of that account. Only a DocSpace
    /// administrator may read it, and the call is read-only. The answer is empty in two ordinary cases - the
    /// installation has no billing service configured at all, and the portal has never been a customer - so an empty
    /// body is not an error. `payer` is filled in only when the billing e-mail belongs to a portal user; when it does
    /// not, the e-mail is still shown but the field stays empty, and that is what makes every payer-only operation of
    /// this group unreachable for everybody. `refresh=true` re-reads the customer from the billing provider instead
    /// of the cache, which is worth doing right after a payment method has been attached.
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
    /// Charges the payment method on file and adds the amount to the portal's wallet, the balance every wallet
    /// service is paid from. The portal needs a billing customer with a payment method set - attach one with
    /// `GET api/2.0/portal/payment/checkoutsetupurl` - `currency` has to be one of the accounting currencies this
    /// installation supports, and `amount` is a whole number of currency units between 1 and 999999. Only the payer
    /// may call it. The call takes money and is not idempotent in any way: two identical requests charge twice, so a
    /// client must not retry it blindly after a timeout, and it is limited to ten requests a minute per user by
    /// default. A successful top-up pushes the new balance to the portal clients over their socket connection and
    /// re-arms the low-balance notification. The result is `true` when the payment provider accepted the charge; read
    /// the resulting balance back from `GET api/2.0/portal/payment/customer/balance`.
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
    /// Returns the money the portal has in its wallet as the accounting service holds it: the account with its own
    /// currency, one sub-account per currency with the amount on it, and the most recent credit movement. Only a
    /// DocSpace administrator may read it, an installation without a billing service answers 403, and a portal that
    /// has never been a customer gets an empty result. The call is read-only. This balance is what the wallet
    /// services are charged against, so it falls as they are used and rises with
    /// `POST api/2.0/portal/payment/deposit`; the movements behind a change are listed by
    /// `GET api/2.0/portal/payment/customer/operations`. Pass `refresh=true` to re-read it from the accounting
    /// service rather than the cache - right after a top-up the cached figure is still the old one.
    /// </remarks>
    /// <summary>
    /// Get the customer balance
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet account with its sub-account per currency, or an empty result when the portal has no billing customer", typeof(Balance))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("customer/balance")]
    public async Task<Balance> GetCustomerBalance(PaymentInformationRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        return await tariffService.GetCustomerBalanceAsync(tenant.Id, inDto.Refresh);
    }

    /// <remarks>
    /// Lists the money movements on the portal's wallet - top-ups, the charges of the wallet services, refunds and
    /// corrections - one page at a time, which is what a billing history is built from. Only a DocSpace administrator
    /// may read it, a portal with no billing customer answers with an empty result, and the call is read-only. Every
    /// filter is optional: `startDate` and `endDate` are read in the portal time zone and default to the portal
    /// creation date and the present moment, `serviceName` narrows to particular wallet services and fails with 404
    /// on a name this installation does not sell, `participantName`, `type` and `status` narrow to who caused a
    /// movement and how it ended, and `credit` and `debit` include or exclude the two directions. `offset` and
    /// `limit` page through the result and default to 0 and 25, `orderBy` and `orderType` sort it, and the answer
    /// repeats them next to `totalQuantity`, `totalPage` and `currentPage` so a client can page without counting. The
    /// same data as a downloadable file is `POST api/2.0/portal/payment/customer/operationsreport`, and the figures
    /// added up per service are `GET api/2.0/portal/payment/customer/usage`.
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

        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime);
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
    /// Returns what the portal spent from its wallet added up per calendar month, so a client can draw a spending
    /// chart without paging through every movement. Only a DocSpace administrator may read it, a portal with no
    /// billing customer answers with an empty result, and the call is read-only. `startDate` and `endDate` bound the
    /// period, both inclusive, and default to the portal creation date and the present moment; the months are cut in
    /// the portal time zone, so a movement at the edge of a month falls where the portal sees it and not where UTC
    /// does. Each item names its year and month, the total charged in it with the currency, and how many operations
    /// that total came from. The movements behind a month are in `GET api/2.0/portal/payment/customer/operations`,
    /// and the same figures as a file come from `POST api/2.0/portal/payment/customer/usage/monthly/report`.
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
            UtcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime),
            UtcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow),
            TimeZome = tenant.TimeZone ?? "UTC"
        };

        var usage = await tariffService.GetCustomerMonthlyUsageAsync(tenant.Id, filter);

        return usage?.Select(u => new CustomerMonthlyUsageDto(u)).ToList();
    }

    /// <remarks>
    /// Returns how much of each wallet service the portal consumed and what that cost, added up per service instead
    /// of listed per movement. Only a DocSpace administrator may read it, a portal with no billing customer answers
    /// with an empty result, and the call is read-only. The filters are optional: `serviceName` narrows to particular
    /// services and fails with 404 on a name this installation does not sell, `participantName` and `status` narrow
    /// to who consumed and how the operation ended, `startDate` and `endDate` bound the period in the portal time
    /// zone, `metadata` matches the key and value pairs a service records with its usage, and `offset`, `limit`,
    /// `orderBy` and `orderType` page and sort the result. Amounts come with the unit the service is sold in, except
    /// AI tools, whose consumption is reported in tokens rather than in AI credits. The individual charges behind
    /// these totals are `GET api/2.0/portal/payment/customer/operations`, and the same figures as a downloadable file
    /// are `POST api/2.0/portal/payment/customer/usage/report`.
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

        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime);
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
    /// Lists the wallet services the portal is running right now: the add-ons its plan pays for that are in the
    /// active state, plus the ones an administrator switched on by hand in the wallet service settings; the DocsCloud
    /// trial is listed as well, although it is not paid from the wallet. Only a DocSpace administrator may call it,
    /// no billing customer is needed for it, and the call is read-only. Every item names the service, its title and
    /// the unit it is measured in, and says whether it is a subscription; a subscribed service also carries the limit
    /// it grants and how much of it is used where that number is known - the editor seats and the editors currently
    /// active for DocsCloud, the purchased units and the units already consumed for disk storage. A service listed
    /// with no limit is one whose usage is not counted this way, not one without a limit. The catalogue of what could
    /// be switched on is `GET api/2.0/portal/payment/walletservices`, and switching one is
    /// `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get the active wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/activeservices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services active on the portal, with their limits and usage where those are known", typeof(IEnumerable<ActiveServiceDto>))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("activeservices")]
    public async Task<List<ActiveServiceDto>> GetActiveServices()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return await paymentHelper.GetActiveServicesAsync(tenant.Id);
    }

    /// <remarks>
    /// Queues the history of the wallet movements as an `xlsx` file and returns the task that will build it; the file
    /// is not ready when the response arrives. The portal needs a billing customer and the caller has to be a
    /// DocSpace administrator. The body takes the same filters as `GET api/2.0/portal/payment/customer/operations` -
    /// the service names, the date range, the participant, the operation type and status, the credit and debit
    /// directions and the ordering - and an empty body reports everything from the portal creation date to now; a
    /// service name this installation does not sell fails with 404. Poll
    /// `GET api/2.0/portal/payment/customer/operationsreport` until `isCompleted` is true, then take the file from
    /// `resultFileUrl` or open `resultFileId`: the finished file is saved into the caller's own My documents section,
    /// where it counts against the portal storage like any other file. One operations report per user is tracked at a
    /// time - a call made while the previous one is still running answers with that task - and
    /// `DELETE api/2.0/portal/payment/customer/operationsreport` stops it. A build that fails ends the task with
    /// `error` filled in rather than failing this call.
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
    /// Returns the state of the `xlsx` wallet operations report this user started with
    /// `POST api/2.0/portal/payment/customer/operationsreport`: `percentage` while it is being built, `isCompleted`
    /// when it is done, `resultFileId`, `resultFileName` and `resultFileUrl` pointing at the file in the caller's My
    /// documents, and `error` when the build failed. The portal needs a billing customer and the caller has to be a
    /// DocSpace administrator; the call is read-only and is the one to poll. The task is kept per user and per report
    /// kind, so it never reports another administrator's report, nor the service usage and monthly usage ones, which
    /// have their own status operations. An empty result means this user has no operations report at all - none was
    /// started, or the finished one was already picked up or terminated. A completed task is dropped as soon as the
    /// next report is started, so read the file link out of the same answer that first reports `isCompleted`.
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
    /// Stops the `xlsx` wallet operations report this user has running and drops its task, for a report that was
    /// started with the wrong filters or is no longer wanted. The portal needs a billing customer and the caller has
    /// to be a DocSpace administrator. The stop is asked of the worker that builds the file rather than done here, so
    /// `GET api/2.0/portal/payment/customer/operationsreport` can still answer for a moment afterwards. The call is
    /// safe to repeat and does nothing at all when this user has no report running: there is no response body, and
    /// status 200 says the stop was requested, not that a report was really stopped. A report that had already
    /// finished keeps its file in My documents - nothing is deleted from there.
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
    /// Queues the usage of the wallet services as an `xlsx` file and returns the task that will build it; the file is
    /// not ready when the response arrives. The portal needs a billing customer and the caller has to be a DocSpace
    /// administrator. The body takes the same filters as `GET api/2.0/portal/payment/customer/usage` - the service
    /// names, the date range, the participant, the operation status, the usage metadata and the ordering - and an
    /// empty body reports every service from the portal creation date to now; a service name this installation does
    /// not sell fails with 404. Poll `GET api/2.0/portal/payment/customer/usage/report` until `isCompleted` is true,
    /// then take the file from `resultFileUrl` or open `resultFileId`: the finished file is saved into the caller's
    /// own My documents section, where it counts against the portal storage like any other file. One service usage
    /// report per user is tracked at a time - a call made while the previous one is still running answers with that
    /// task - and `DELETE api/2.0/portal/payment/customer/usage/report` stops it. It is a different report from the
    /// operations one and does not interfere with it: per-movement history is
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
    /// Returns the state of the `xlsx` service usage report this user started with
    /// `POST api/2.0/portal/payment/customer/usage/report`: `percentage` while it is being built, `isCompleted` when
    /// it is done, `resultFileId`, `resultFileName` and `resultFileUrl` pointing at the file in the caller's My
    /// documents, and `error` when the build failed. The portal needs a billing customer and the caller has to be a
    /// DocSpace administrator; the call is read-only and is the one to poll. The task is kept per user and per report
    /// kind, so it reports neither another administrator's report nor the operations and monthly usage ones, which
    /// have their own status operations. An empty result means this user has no service usage report at all - none
    /// was started, or the finished one was already picked up or terminated. A completed task is dropped as soon as
    /// the next report is started, so read the file link out of the same answer that first reports `isCompleted`.
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
    /// Stops the `xlsx` service usage report this user has running and drops its task, for a report that was started
    /// with the wrong filters or is no longer wanted. The portal needs a billing customer and the caller has to be a
    /// DocSpace administrator. The stop is asked of the worker that builds the file rather than done here, so
    /// `GET api/2.0/portal/payment/customer/usage/report` can still answer for a moment afterwards. The call is safe
    /// to repeat and does nothing at all when this user has no such report running: there is no response body, and
    /// status 200 says the stop was requested, not that a report was really stopped. It leaves the operations and
    /// monthly usage reports alone, and a report that had already finished keeps its file in My documents.
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
    /// Queues the wallet spending added up per calendar month as an `xlsx` file and returns the task that will build
    /// it; the file is not ready when the response arrives. The portal needs a billing customer and the caller has to
    /// be a DocSpace administrator. The body takes only the period - `startDate` and `endDate`, both inclusive - and
    /// an empty body covers everything from the portal creation date to now; the months are cut in the portal time
    /// zone, exactly as in `GET api/2.0/portal/payment/customer/usage/monthly`. Poll
    /// `GET api/2.0/portal/payment/customer/usage/monthly/report` until `isCompleted` is true, then take the file
    /// from `resultFileUrl` or open `resultFileId`: the finished file is saved into the caller's own My documents
    /// section, where it counts against the portal storage like any other file. One monthly usage report per user is
    /// tracked at a time - a call made while the previous one is still running answers with that task - and
    /// `DELETE api/2.0/portal/payment/customer/usage/monthly/report` stops it. There is no service filter here: for a
    /// report per service use `POST api/2.0/portal/payment/customer/usage/report`.
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
    /// Returns the state of the `xlsx` monthly usage report this user started with
    /// `POST api/2.0/portal/payment/customer/usage/monthly/report`: `percentage` while it is being built,
    /// `isCompleted` when it is done, `resultFileId`, `resultFileName` and `resultFileUrl` pointing at the file in
    /// the caller's My documents, and `error` when the build failed. The portal needs a billing customer and the
    /// caller has to be a DocSpace administrator; the call is read-only and is the one to poll. The task is kept per
    /// user and per report kind, so it reports neither another administrator's report nor the operations and service
    /// usage ones, which have their own status operations. An empty result means this user has no monthly usage
    /// report at all - none was started, or the finished one was already picked up or terminated. A completed task is
    /// dropped as soon as the next report is started, so read the file link out of the same answer that first reports
    /// `isCompleted`.
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
    /// Stops the `xlsx` monthly usage report this user has running and drops its task, for a report that was started
    /// for the wrong period or is no longer wanted. The portal needs a billing customer and the caller has to be a
    /// DocSpace administrator. The stop is asked of the worker that builds the file rather than done here, so
    /// `GET api/2.0/portal/payment/customer/usage/monthly/report` can still answer for a moment afterwards. The call
    /// is safe to repeat and does nothing at all when this user has no such report running: there is no response
    /// body, and status 200 says the stop was requested, not that a report was really stopped. It leaves the
    /// operations and service usage reports alone, and a report that had already finished keeps its file in My
    /// documents.
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
    /// Returns the portal's automatic wallet top-up settings: whether it is switched on, the balance that triggers
    /// it, the balance it tops the wallet up to and the currency it charges in. Only a DocSpace administrator may
    /// read it, no billing customer is needed, and the call is read-only. A portal that has never configured it gets
    /// the defaults rather than an empty result, so `enabled` is the field that says whether anything happens at all.
    /// Two of the values are kept by the portal itself and cannot be set through this API: `lowBalanceThreshold` is
    /// the balance below which the portal warns its administrators by mail, and `lowBalanceNotified` says whether
    /// that warning has already gone out for the current dip. Change the rest with
    /// `POST api/2.0/portal/payment/topupsettings`.
    /// </remarks>
    /// <summary>
    /// Get the service prices from the accounting service
    /// </summary>
    /// <remarks>
    /// Returns the list of prices of the specified service from the accounting service.
    /// </remarks>
    /// <path>api/2.0/portal/payment/accounting/prices/{serviceName}</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of the service prices", typeof(List<ServicePriceInfo>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("accounting/prices/{serviceName}")]
    public async Task<List<ServicePriceInfo>> GetAccountingServicePrices(ServicePricesRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        return await tariffService.GetAccountingServicePricesAsync(inDto.ServiceName, inDto.Active);
    }

    /// <remarks>
    /// Returns the portal's automatic wallet top-up settings - whether it is on, the balance that triggers a
    /// charge, the balance it is topped up to, and the currency both are expressed in. Any DocSpace
    /// administrator may read them, and unlike the operation that changes them this one needs neither a
    /// billing customer nor a configured billing service, so it answers on a portal that has never paid for
    /// anything. It is read-only and changes nothing.
    /// A portal that has never configured top-up gets the defaults rather than an empty result: `enabled` is
    /// false, `currency` is null, and `minBalance` and `upToBalance` are 0. Those two zeros are outside the
    /// ranges `POST api/2.0/portal/payment/topupsettings` accepts - 5 to 1000 and 6 to 5000 - so the answer
    /// cannot be sent straight back to it; supply real values instead. `lastModified` is
    /// `0001-01-01T00:00:00` until the settings are stored for the first time.
    /// `lowBalanceThreshold` and `lowBalanceNotified` are maintained by the portal itself: they are reported
    /// here, but ignored when the settings are written.
    /// </remarks>
    /// <summary>
    /// Get the auto top-up settings
    /// </summary>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The automatic top-up settings of the portal, or their defaults when it has never configured them", typeof(TenantWalletSettings))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("topupsettings")]
    public async Task<TenantWalletSettings> GetTenantWalletSettings()
    {
        await paymentHelper.DemandAdminAsync();

        var result = await settingsManager.LoadAsync<TenantWalletSettings>();
        return result;
    }

    /// <remarks>
    /// Switches the portal's automatic wallet top-up on or off and sets its thresholds: while it is on, the payment
    /// method on file is charged whenever the wallet balance falls below `minBalance`, enough to bring it up to
    /// `upToBalance`, in `currency`. The portal needs a billing customer whose wallet balance exists - a portal that
    /// has never had one answers 404, so top the wallet up once with `POST api/2.0/portal/payment/deposit` first -
    /// and only the payer may change the settings. The body replaces the stored settings as a whole and an omitted
    /// body resets them to the defaults; `minBalance` is accepted between 5 and 1000 and `upToBalance` between 6 and
    /// 5000, while `lowBalanceThreshold` and `lowBalanceNotified` are ignored on the way in and kept as the portal
    /// had them. The call is mutating and idempotent, it charges nothing by itself, it is written to the portal audit
    /// trail, and switching the top-up on also re-arms the low-balance warning. The settings as they were stored come
    /// back in the answer.
    /// </remarks>
    /// <summary>
    /// Set the auto top-up settings
    /// </summary>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The automatic top-up settings as they were stored", typeof(TenantWalletSettings))]
    [SwaggerResponse(403, "The caller is not the payer of this portal, or the portal has no billing service configured")]
    [SwaggerResponse(404, "This portal has no billing customer, or its wallet has no balance yet")]
    [HttpPost("topupsettings")]
    public async Task<TenantWalletSettings> SetTenantWalletSettings(TenantWalletSettingsWrapper inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        var settings = inDto?.Settings ?? new TenantWalletSettings();

        // LowBalanceThreshold/LowBalanceNotified are internal-only: never trust them from client input,
        // always recompute from what was previously persisted so a stale GET->POST round-trip can't
        // resurrect an old value (e.g. permanently suppressing the low-balance notification)
        var existing = await settingsManager.LoadAsync<TenantWalletSettings>();
        settings.LowBalanceThreshold = existing.LowBalanceThreshold;
        settings.LowBalanceNotified = existing.LowBalanceNotified;

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

        return settings;
    }

    /// <remarks>
    /// Returns which wallet services an administrator has switched on for this portal by hand, as opposed to the ones
    /// its plan pays for. Only a DocSpace administrator may read it, an installation without a billing service
    /// answers 403, no billing customer is needed, and the call is read-only. `enabledServices` holds the names of
    /// those services and is empty when none was switched on. This is the stored setting and not the state of the
    /// portal: a service the plan brings with it is active without appearing here, so the honest answer to what is
    /// running is `GET api/2.0/portal/payment/activeservices`. One entry is changed with
    /// `POST api/2.0/portal/payment/servicestate`.
    /// </remarks>
    /// <summary>
    /// Get the wallet service settings
    /// </summary>
    /// <path>api/2.0/portal/payment/servicessettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet services switched on by hand for this portal", typeof(TenantWalletServiceSettings))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the portal has no billing service configured")]
    [HttpGet("servicessettings")]
    public async Task<TenantWalletServiceSettings> GetTenantWalletServiceSettings()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        return await settingsManager.LoadAsync<TenantWalletServiceSettings>();
    }

    /// <remarks>
    /// Switches one wallet service on or off for the portal: `service` names it and `enabled` says which way. The
    /// portal needs a billing customer, and the caller needs both the permission to edit the portal settings and
    /// DocSpace administrator rights. Order matters between the two AI services - AI tools has to be on before AI
    /// search may be switched on, and switching AI tools off switches AI search off with it - so a request that
    /// breaks that order is refused with 403. The call is mutating and idempotent: switching on a service that is
    /// already on changes nothing. It is written to the portal audit trail, and switching AI tools notifies the
    /// portal clients so the AI features appear or disappear for them without a reload. The whole updated set of
    /// switched-on services comes back. Switching a service on does not buy it - its units are still bought with
    /// `PUT api/2.0/portal/payment/updatewallet`.
    /// </remarks>
    /// <summary>
    /// Switch a wallet service
    /// </summary>
    /// <path>api/2.0/portal/payment/servicestate</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The whole set of wallet services switched on for the portal after the change", typeof(TenantWalletServiceSettings))]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, the portal has no billing service configured, or AI search was switched on while AI tools is off")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPost("servicestate")]
    public async Task<TenantWalletServiceSettings> ChangeTenantWalletServiceState(ChangeWalletServiceStateRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        return await paymentHelper.ChangeWalletServiceStateAsync(inDto.Service, inDto.Enabled);
    }

    /// <remarks>
    /// Returns the price list of the AI features the portal pays for out of its wallet: the chat models with the
    /// price of their prompt and completion tokens, the embedding models, the image models with their per-image
    /// price, and the web search providers with the price of one search. The installation needs both a billing
    /// service and the AI gateway configured, otherwise the answer is 403, and only a DocSpace administrator may read
    /// it; the call is read-only. Token prices are normalised per million tokens, and every price is in the single
    /// `currency` the answer names. Each entry carries the model identifier to use when talking to the AI operations,
    /// its display alias, its provider with the provider icon, and a link to the model's own page. It is a list of
    /// what the models cost and not of what the portal spent - that is `GET api/2.0/portal/payment/customer/usage` -
    /// and it says nothing about which of them are allowed here, which is
    /// `GET api/2.0/portal/payment/ai-model/restrictions`.
    /// </remarks>
    /// <summary>
    /// Get AI model prices
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The prices of the chat, embedding and image models and of the web search providers, with the currency they are in", typeof(AiPricesDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation has no billing service or no AI gateway configured")]
    [HttpGet("ai-prices")]
    public async Task<AiPricesDto> GetAiPrices()
    {
        paymentHelper.DemandAiGatewayConfiguration();

        await paymentHelper.DemandAdminAsync();

        return await paymentHelper.GetAiPricesAsync();
    }

    /// <remarks>
    /// Returns the AI chat models that are barred on this portal - the ones no user of it may pick for a
    /// conversation, whatever the price list offers. Only a DocSpace administrator may read it, and the call is
    /// read-only. When the installation has no billing service or AI is not enabled for the portal, the answer is an
    /// empty set instead of an error, which is indistinguishable from a portal that restricts nothing. An empty
    /// `models` therefore means every model in `GET api/2.0/portal/payment/ai-prices` may be used. The set names the
    /// barred models and not the allowed ones; replace it with `PUT api/2.0/portal/payment/ai-model/restrictions`.
    /// </remarks>
    /// <summary>
    /// Get restricted AI models
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The identifiers of the AI chat models barred on this portal, empty when none is", typeof(RestrictedModelsResponse))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> GetRestrictedAiModels()
    {
        if (!tariffService.IsConfigured() || !await aiGateway.IsAiEnabledAsync())
        {
            return new RestrictedModelsResponse { Models = [] };
        }

        await paymentHelper.DemandAdminAsync();

        return await aiGateway.GetRestrictedModelsAsync();
    }

    /// <remarks>
    /// Replaces the whole set of AI chat models barred on this portal: the body is the complete set that is to hold,
    /// so adding one restriction means sending the new model together with the ones already restricted, lifting one
    /// means leaving it out, and an empty set lifts them all. Read the current set from
    /// `GET api/2.0/portal/payment/ai-model/restrictions` and the model identifiers from
    /// `GET api/2.0/portal/payment/ai-prices` before calling. The installation needs a billing service and the AI
    /// gateway configured, the portal needs a billing customer, and the caller needs the permission to edit the
    /// portal settings as well as DocSpace administrator rights. The call is mutating and idempotent - sending the
    /// same set twice leaves the same state - and it is written to the portal audit trail. It takes effect on the
    /// next AI request, so a conversation already open on a model that has just been barred cannot go on with it. The
    /// stored set comes back in the answer.
    /// </remarks>
    /// <summary>
    /// Set restricted AI models
    /// </summary>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The set of barred AI chat models as it was stored", typeof(RestrictedModelsResponse))]
    [SwaggerResponse(403, "The caller may not edit the portal settings or is not a DocSpace administrator, or the installation has no billing service or no AI gateway configured")]
    [SwaggerResponse(404, "This portal has no billing customer yet")]
    [HttpPut("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> SetRestrictedAiModels(SetRestrictedAiModelsRequestDto inDto)
    {
        paymentHelper.DemandAiGatewayConfiguration();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        return await paymentHelper.SetRestrictedAiModelsAsync(inDto.Models);
    }
}
