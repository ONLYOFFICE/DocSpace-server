# billing — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml` (submodule commit `a0fc3a2`,
2026-10-20); `Controller` is `web/ASC.Web.Api/Api/PaymentsController.cs` (class `PaymentController`,
`[AllowNotPayment]` on the class at line 56), `Helper` is `web/ASC.Web.Api/Core/PaymentHelper.cs`,
`QuotaHelper` is `web/ASC.Web.Api/Core/QuotaHelper.cs`, `Portal` is `web/ASC.Web.Api/Api/PortalController.cs`,
`Settings` is `web/ASC.Web.Api/Api/Settings/SettingsController.cs`, `Tariff` is
`common/ASC.Core.Common/Billing/TariffService.cs`, `Accounting` is `common/ASC.Core.Common/Billing/AccountingClient.cs`,
`Job` is `web/ASC.Web.Studio/Wallet/TopUpWalletService.cs`, `Filter` is
`common/ASC.Api.Core/Middleware/PaymentFilter.cs`, `Extra` is `web/ASC.Web.Core/Utility/TenantExtra.cs`,
`Quota` is `common/ASC.Core.Common/Tenants/TenantQuota.cs`, `Socket` is
`common/ASC.Core.Common/Quota/QuotaSocketManager.cs`, `Requests` is
`web/ASC.Web.Api/ApiModels/RequestsDto/PaymentRequestsDto.cs`, `AccountingRequests` is
`web/ASC.Web.Api/ApiModels/RequestsDto/AccountingRequestsDto.cs`, `appsettings` is
`../buildtools/config/appsettings.json`. Tests: `Tests/` is `web/ASC.Web.Api.Tests/Tests/04_Portal/Payments/`,
`Quota/` is `web/ASC.Web.Api.Tests/Tests/04_Portal/Quota/`, `Unit/` is `common/Tests/ASC.Core.Common.Tests/`,
`TS` is `../tests/api-tests/src/tests/portal/payments/` (`payment.spec.ts`, `payments.permissions.spec.ts`,
`lowBalance.spec.ts`) and `TS-AI` is `../tests/api-tests/src/tests/ai/billing/billing.spec.ts`. Status:
**TEST-CONFIRMED** (an integration or unit test of this repository asserts it), **TS-ONLY** (only the
TypeScript suite, which runs against a real billing sandbox, asserts it — a claim, not proof), **CODE-ONLY**
(read from the product code, the configuration or the OpenAPI document, no test), **NOTES-ONLY** (only the
release notes say it — nothing of that kind was written into the spec). Exception → status mapping:
`ItemNotFoundException` 404, `ArgumentException` 400, `SecurityException` 403 "Access denied",
`InvalidOperationException` 403, `TenantQuotaException` / `BillingException` / `AccountingPaymentRequiredException`
402, `AccountingServiceLimitExceededException` 402 with the accounting message, `AccountingConflictException` 409,
`AccountingBadRequestException` 400, anything else 500 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:55-115`).
Model validation 400s carry `error.message` with the field name since `CommonApiResponse.cs` (hotfix/v4.0.1).

The integration host runs without a billing or accounting service (`Helper:53-58` throws 403 "Tariff service is
not configured"), so for every operation that needs one the .NET suite confirms only the 401 of an anonymous
caller, the request validation that runs before the action, and the 403 of the configuration check. The
behaviour behind that check comes from the TypeScript suite (TS-ONLY) and from reading the code (CODE-ONLY).
The .NET suite for this capability is 14 classes in `Tests/` (113 tests) plus `02_Settings/Common/PaymentSettingsTests.cs`
and `Quota/PortalTariff*Tests.cs`; the service-limit and price-retrieval logic of `Tariff` and `Accounting` is
covered by 9 unit classes in `Unit/` with faked HTTP handlers.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Tariff state of a portal** | | |
| Four states; trial → `Trial`, due date = creation (or version change) + trial period, default 30 days, no grace | `Tariff:1149-1228` (`CalculateTariffAsync`: trial at 1182-1206, `DefaultTrialPeriod` 73, `GetPeriodAsync("TrialPeriod")` 1230-1235) | CODE-ONLY |
| Past due date → `Delay`, `delayDueDate` = due + grace days; older than grace → `NotPaid`; lifetime stays `Paid`; `DateTime.MaxValue` due date never lapses | `Tariff:1208-1227`; `PaymentDelay` 91 ← `appsettings:19-20` (`"payment": {"delay": 10}`) | CODE-ONLY |
| Free plan: state Paid, `id` ≥ 0, `quotas` non-empty for owner/admin | `Quota/PortalTariffTests.cs:41-62`; `Quota/PortalTariffPermissionsTests.cs:48-62` (state Paid for the other roles) | TEST-CONFIRMED |
| v3.1: refund scenario, yearly plan via sales only | release notes v3.1.0 lines 15, 48 | NOTES-ONLY (refund is also CODE-ONLY below) |
| **Tariff of a hosted portal follows the billing service** | | |
| Re-read from payments when the cache expires (5 min, 15 min standalone) or `refresh`; `customerId` = payer e-mail; due date of the earliest payment | `Tariff:99-297` (`GetTariffAsync`), 69-71, 2161-2176 | CODE-ONLY |
| Refund: `BillingNotFoundException` on a paid non-free tariff → due date yesterday → `Delay` | `Tariff:239-253`; release notes v3.1.0 line 15 | CODE-ONLY |
| No payment, no free quota → due date yesterday, `NotPaid` | `Tariff:260-275` | CODE-ONLY |
| **Single tariff of a self-hosted installation** | | |
| Standalone tariff from the licence, Enterprise without licence → 30-day `start_trial` quota | `Tariff:278-291`; `branding-and-licensing` spec for the licence file | CODE-ONLY |
| `openSource` / `enterprise` / `developer` from the edition config; open source never not paid | `Portal:611-617`; `Tariff:35-45` (`TenantExtraConfig`); `Extra:48-63, 96-106` | CODE-ONLY |
| 403 "Tariff service is not configured" on `getCustomerBalance`, `topUpDeposit`, `updatePayment` …, anonymous 401 | `Helper:53-58, 116-129, 131-146`; `Tests/PaymentReadEndpointsAnonymousAccessTests.cs:41-151` and `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:41-153` (401 for 29 operations); 403 for the owner: `Tests/PaymentUrlValidationTests.cs:120`, `Tests/PaymentTopUpSettingsValidationTests.cs:73`, `Tests/PaymentAccountingPricesTests.cs:58-74`, `Tests/PaymentAiModelRestrictionsValidationTests.cs:64-82` | TEST-CONFIRMED |
| **Reading the tariff** | | |
| Grading by role: `state` for all; `dueDate`/`delayDueDate` for Room admin and DocSpace admin; `id`, `customerId`, `licenseDate`, flags, `quotas` with overdue rows for the portal-settings right | `Portal:593-622`; yaml `getPortalTariff` (50752) | CODE-ONLY |
| Owner / DocSpace admin: 200, Paid, `id` ≥ 0, `quotas` non-empty | `Quota/PortalTariffTests.cs:41-62` | TEST-CONFIRMED |
| Room admin / User / Guest: 200, `id` 0, Paid; anonymous 401 | `Quota/PortalTariffPermissionsTests.cs:41-62` (raw JSON, `id` and `state` asserted; absence of `customerId`/`quotas` is CODE-ONLY) | TEST-CONFIRMED |
| **Upcoming payments** | | |
| Portal-settings right; overdue skipped; unpriced skipped; `nextQuota`/`nextQuantity` applied; `amount` = price × quantity | `Portal:648-711`; yaml `getUpcomingPayments` (50843) | CODE-ONLY |
| Empty list on the free plan | `Portal:669-673` (`definition.Price <= 0` → skipped) — no test | CODE-ONLY |
| **Payment gate while the portal is not paid** | | |
| 402 with no body unless the action carries the allow-without-payment marker | `Filter:38-58`; `common/ASC.Api.Core/Routing/FormatRoute.cs:37` (the attribute) | CODE-ONLY |
| Allowed: portal record, tariff, upcoming, quota, tenantextra, suspend/delete, every payment operation, authentication, licence, version, capabilities | `Portal:116, 487, 591, 646, 713, 1024, 1064, 1138, 1220`; `Controller:56` (class level); `web/ASC.Web.Api/Api/AuthenticationController.cs:99-574`; `Settings:102, 661, 684, 723, 828, 1366`; `LicenseController.cs:78-264`; `VersionController.cs:55`; `CapabilitiesController.cs:80` (LSP references of the attribute, 63 sites) | CODE-ONLY |
| Gate only where tariff settings are enabled (hosted, or standalone with a licence path); `NotPaid` or Enterprise unpaid | `Extra:48-55, 96-106` | CODE-ONLY |
| Grace period: 402 "Your pricing plan does not support this option" from the count checkers | `quotas` spec, "Count limits of the tariff"; `web/ASC.Web.Core/Quota/CountRoomAdminFeature.cs` | CODE-ONLY |
| **Payment settings** | | |
| `salesEmail`, `buyUrl` regional, `currentLicense`, `max` 999 unless `core:payment:max-quantity` | `Settings:1366-1386`; yaml `getPaymentSettings` (56854) | CODE-ONLY |
| Owner / DocSpace admin: 200, non-empty `salesEmail`, `buyUrl`, `currentLicense`, `max` > 0 | `web/ASC.Web.Api.Tests/Tests/02_Settings/Common/PaymentSettingsTests.cs:41-62` | TEST-CONFIRMED |
| **Price and currency catalogue** | | |
| Portal-settings right; price per product in the request currency, 0 default; default region first then request region | `Controller:552-612` | CODE-ONLY |
| Owner: prices map, no negative value; first currency `US` / `$` / `US Dollar`; DocSpace admin 200 | `Tests/PaymentPricesTests.cs:41-57`; `Tests/PaymentCurrenciesTests.cs:41-59`; `TS/payment.spec.ts:766-795, 1757-1792` | TEST-CONFIRMED |
| Room admin / User / Guest 403 (bugs 81512, 81516), anonymous 401 | `Tests/PaymentPricesTests.cs:59-97`; `Tests/PaymentCurrenciesTests.cs:61-99`; `TS/payments.permissions.spec.ts:1033-1082, 2132-2180` | TEST-CONFIRMED |
| **Prices are resolved per region and cached** | | |
| Request-region currency, else default region, else stored price | `Quota:838-878` (`TenantQuotaMapper.Resolve`); `Unit/TenantQuotaPriceResolveTests.cs` (4 cases) | TEST-CONFIRMED |
| One request per product list, per-product cache key, 1 h Redis / 5 min memory, failed service leaves its products unpriced and uncached, unpriced product cached as empty | `Tariff:759-918`, 89-90; `Unit/TariffServiceProductPriceTests.cs` (`Wallet_AccountingFails_KeepsBillingPricesAndAsksForFailedServiceAgain`, `Plan_SomeProductsCached_AsksBillingOnlyForMissingOnes`, `Plan_BillingFails_KeepsCachedPricesAndDoesNotCacheFailedProducts`, `Wallet_ProductWithoutPrice_IsCachedAsEmpty`) | TEST-CONFIRMED |
| Quota list built once on concurrent misses (30 s lock timeout) | `common/ASC.Core.Common/Caching/CachedQuotaService.cs:44, 66-86` | CODE-ONLY |
| Accounting catalogue per service cached 1 h | `Accounting` (`_servicePricesCacheDuration`, `GetProductPriceInfoAsync` over `GetServicePages`) | CODE-ONLY |
| **Purchasable quotas and wallet services** | | |
| `getPaymentQuotas` filters, non-profit `additional=false` → current quota only | `Controller:614-647`; `QuotaHelper:45-56` | CODE-ONLY |
| Owner: no filter / `wallet=true` / `additional=true` → 200, `features` on every entry; DocSpace admin 200; Room admin / User / Guest 403; anonymous 401 | `Tests/PaymentQuotasTests.cs:41-110`; `TS/payment.spec.ts:1795-1883`; `TS/payments.permissions.spec.ts:2181-2229` | TEST-CONFIRMED |
| Six wallet services on the integration host with the expected USD prices (storage 0.14, backup 10, Docs Connect 8, Dev Pack 12, AI tools 0, AI search 0), `features` non-empty | `Tests/PaymentWalletServicesTests.cs:42-82`; `TS/payment.spec.ts:1256-1327` (same six and prices) | TEST-CONFIRMED |
| `getWalletService` by name: `AITools` and `AISearch` priced 0 in USD, `Backup` and `Storage` above 0; 403 for Room admin / User / Guest, 401 anonymous | `Tests/PaymentWalletServiceTests.cs:42-142`; `TS/payment.spec.ts:1330-1452` | TEST-CONFIRMED |
| 404 "Service could not be found" when not sold or AI service while AI access is disabled; 400 for a name that is not an enum member | `Controller:677-712`; `common/ASC.Core.Common/Tenants/TenantWalletService.cs:38-61` (the seven members) | CODE-ONLY |
| **Current plan trimmed to the caller's type** | | |
| Guest 403, anonymous 401; no configuration check | `Controller:714-745`; `Tests/PaymentQuotaInformationTests.cs:67-76, 136-145`; `TS/payments.permissions.spec.ts:2079-2130` | TEST-CONFIRMED |
| Fresh portal: `id` -3, "Startup", price 0, `free` true, `trial` false, `nonProfit` false, features non-empty (owner, DocSpace admin) | `Tests/PaymentQuotaInformationTests.cs:42-65` | TEST-CONFIRMED |
| DocSpace admin: the Room admin's four features plus more, `price.value` present | `Tests/PaymentQuotaInformationTests.cs:78-89` (bug 81534) | TEST-CONFIRMED |
| Room admin: exactly `manager`, `room`, `total_size`, `statistic`; empty `price`, no `dueDate`; same `usersQuota`, `roomsQuota`, `aiAgentsQuota`, `tenantCustomQuota` as the owner | `Tests/PaymentQuotaInformationTests.cs:91-102, 118-133`; `QuotaHelper:121-150` (`isAdmin`), 173-183 (lowest type per feature); `Quota:540-571` (`EmployeeType` per feature: `manager`/`room`/`statistic` RoomAdmin, `total_size` User, default DocSpaceAdmin); commit `8074ce5b54` | TEST-CONFIRMED |
| User: only `total_size`, with `used`; same quota settings | `Tests/PaymentQuotaInformationTests.cs:104-116`; `QuotaHelper:256-262` (`used` whenever a statistic exists) | TEST-CONFIRMED |
| **Buying a plan through the hosted checkout** | | |
| DocSpace admin, configured billing, quantity > 0, monthly visible plan, empty result on a paid customer, 402 on the plan not fitting | `Controller:84-150`; `Tariff:299-353` (`CheckQuotaAndGetProductIds`); yaml `getPaymentUrl` (46987) | CODE-ONLY |
| Owner / DocSpace admin get a checkout URL with `qty`, `currency`, `language`, `refId`, `email` | `TS/payment.spec.ts:19-67` | TS-ONLY |
| Quantity 0 / negative → 400 "Invalid quantity" (bug 81436) | `Controller:114-118`; `TS/payments.permissions.spec.ts:94-129` | TS-ONLY (on the integration host the configuration check answers 403 first) |
| Room admin / User / Guest 403 "Access denied" | `TS/payments.permissions.spec.ts:21-92` | TS-ONLY (same reason) |
| `backUrl`/`successUrl` `[Url]`, `[Required]`, `[StringLength(255)]`; `quantity` exactly one product (`[Required]`, custom validation) → 400 naming the field, before the configuration check; 255 characters pass | `Requests:46-90`; `Tests/PaymentUrlValidationTests.cs:44-130` (bugs 81433, 81434); `TS/payments.permissions.spec.ts:130-184` | TEST-CONFIRMED |
| **Changing the subscription quantity** | | |
| Payer only (404 without customer), non-wallet product, same plan on a priced plan, same quantity refused, 402 on quota, audit "subscription updated", rate limit | `Controller:154-205`; `Helper:116-129, 173-188, 288-298`; `Tariff:364-408`; yaml `updatePayment` (47075) | CODE-ONLY |
| DocSpace admin who is not the payer → 403 "Access denied"; Room admin / User / Guest 403 | `TS/payments.permissions.spec.ts:875-953`; `Helper:95-101` (message "Access denied: insufficient permissions for this payment operation") | TS-ONLY |
| Owner updates the quantity → 200 | `TS/payment.spec.ts:3054-3069` | TS-ONLY |
| **Billing account page and payment method** | | |
| `getPaymentAccount`: configured, customer or empty, payer or owner, `payment.ashx?backUrl=` | `Controller:515-548`; `Helper:103-114` | CODE-ONLY |
| Owner gets a string containing `payment.ashx`; DocSpace admin / Room admin / User / Guest 403 | `TS/payment.spec.ts:798-814`; `TS/payments.permissions.spec.ts:1084-1160` | TS-ONLY |
| `getCheckoutSetupUrl`: configured, DocSpace admin, payer once a customer has an e-mail, empty when a method is set, URLs required | `Controller:780-830`; `Requests:184-207` | CODE-ONLY |
| Owner gets the setup URL; DocSpace admin refused when the owner is the payer; Room admin / User / Guest 403 | `TS/payment.spec.ts:903-946`; `TS/payments.permissions.spec.ts:1272-1349` | TS-ONLY |
| `getCustomerInfo`: empty result without billing (no role check), DocSpace admin, `payer` resolved by e-mail | `Controller:838-880` | CODE-ONLY |
| `paymentMethodStatus` 1, `payer.id`/`displayName` of the owner (owner and DocSpace admin) | `TS/payment.spec.ts:817-853`; `TS/lowBalance.spec.ts:133-193` | TS-ONLY |
| **Who is the payer** | | |
| Payer = account whose e-mail is the customer e-mail; payer-only operations; "Access denied: insufficient permissions for this payment operation" | `Helper:95-101, 116-129, 619-624`; `Controller:154-205, 396-430, 429-512, 886-930, 2043-2107` | CODE-ONLY |
| DocSpace admin who is not the payer: `topUpDeposit` 403, `updatePayment` 403, `setTenantWalletSettings` 403, `getSubscriptionBalanceInfo` 403, `moveSubscriptionToWallet` 403 | `TS/payments.permissions.spec.ts:966-981, 886-901, 1696-1711, 2303-2316, 2790-2805` | TS-ONLY |
| DocSpace admin switches a service (200) and buys storage (200) | `TS/payment.spec.ts:202-245, 332-350`; release notes v4.0.0 line 72 (admins manage services, payer tops up) | TS-ONLY |
| **Contacting the sales team** | | |
| DocSpace admin, e-mail regex, mail to sales, audit "contact sales", no body, rate limit | `Controller:746-780`; `web/ASC.Web.Api/ApiModels/RequestsDto/SalesRequestsDto.cs` (`[StringLength(255)]`, `[StringLength(64)]`, required) | CODE-ONLY |
| Owner / DocSpace admin 200; Room admin / User / Guest 403; anonymous 401 | `Tests/PaymentRequestTests.cs:42-89`; `TS/payment.spec.ts:1232-1253`; `TS/payments.permissions.spec.ts:1351-1406` | TEST-CONFIRMED |
| 256-character `userName` / `message`, 65-character `email` → 400 naming the field; empty fields → "Incorrect firstname or lastname" (bug 81525), "Incorrect email", "Message text is empty" | `Tests/PaymentRequestTests.cs:90-150`; `TS/payments.permissions.spec.ts:1408-1522` | TEST-CONFIRMED |
| **Wallet balance** | | |
| DocSpace admin, configured, empty without customer, `refresh` re-reads, balance cached 10 min, empty balance cached on failure | `Controller:933-965`; `Tariff:1616-1664` | CODE-ONLY |
| Funded wallet: `accountCurrency` USD, sub-account amount > 0, `lastCredit`; DocSpace admin same; Room admin / User / Guest 403 | `TS/payment.spec.ts:856-900`; `TS/lowBalance.spec.ts:48-130`; `TS/payments.permissions.spec.ts:1217-1270`; .NET only 401 (`Tests/PaymentReadEndpointsAnonymousAccessTests.cs:89-97`) | TS-ONLY |
| AI charge reduces the balance by the `Chat` debit | `TS-AI:644-699` | TS-ONLY |
| **Wallet top-up** | | |
| Configured, supported currency, `[Range(1, 999999)]`, customer, payer, payment method set, audit, socket `top-up-wallet` with `auto` false only with an immediate method, low-balance re-arm | `Controller:886-930`; `Requests:220-231`; `Helper:312-363`; `Socket:71-75`; `Tariff:1444-1511` | CODE-ONLY |
| Zero / negative amount, invalid currency → 400; owner tops up → `true` | `TS/payment.spec.ts:2995-3051` | TS-ONLY |
| Rate limit ten a minute (`payments_api` policy) | `Controller:179, 238, 453, 767, 908`; `common/ASC.Api.Core/Core/RateLimiterPolicy.cs:40`; `common/ASC.Api.Core/Extensions/RateLimitOperationFilter.cs:91` | CODE-ONLY |
| **Automatic top-up settings** | | |
| Read: DocSpace admin, defaults, no billing needed; write: payer, balance required (404), `[Range(5,1000)]`, `[Range(6,5000)]`, `UpToBalance > MinBalance`, low-balance state carried over, re-armed when enabled, audit | `Controller:2013-2107`; `web/ASC.Web.Api/ApiModels/RequestsDto/TenantWalletSettingsRequestDto.cs:58-105`; `common/ASC.Core.Common/Tenants/TenantWalletSettings.cs:73-103` | CODE-ONLY |
| Owner / DocSpace admin read 200 with `response`; Room admin / User / Guest 403; anonymous 401 | `Tests/PaymentTopUpSettingsReadTests.cs:41-96`; `TS/payment.spec.ts:1455-1509` | TEST-CONFIRMED |
| `minBalance` 4 → 400; `upToBalance` ≤ `minBalance` → 400 naming `UpToBalance` (bug 84314); 5/6 → 403 from the configuration check | `Tests/PaymentTopUpSettingsValidationTests.cs:41-80`; `TS/lowBalance.spec.ts:201-295` | TEST-CONFIRMED |
| Payer stores `enabled` true, 100, 1000 and reads it back with `lastModified`; settings survive a low balance; stored with a currency | `TS/payment.spec.ts:2266-2310`; `TS/lowBalance.spec.ts:297-354` | TS-ONLY |
| **Automatic top-up and low-balance watch** | | |
| Period `core:accounting:topupperiod`, default 3 min; active portals with a stored row; payer or owner authenticated; one balance read per cycle | `Job:62, 70-130, 330-346` | CODE-ONLY |
| Enabled and sub-account below `minBalance` → charge `upToBalance − balance`, audit "wallet topped up", socket `top-up-wallet` `auto` true | `Job:160-210` | CODE-ONLY |
| No payment method → disabled + "unable to auto top up" letter; delayed method → disabled + "auto top-up unavailable" letter; threshold restored to the default | `Job:174-186, 282-327`; letter texts in `notifications` ("Wallet letters"); `common/Tests/ASC.Notify.Tests/Billing/TopUpWalletErrorLetterTests.cs`, `WalletAutoTopUpUnavailableLetterTests.cs` | CODE-ONLY (trigger), TEST-CONFIRMED (letters, in `notifications`) |
| Disabled: below `lowBalanceThreshold` (default `core:accounting:lowbalancethreshold` 1) → letter once + socket `wallet-low-balance`; re-armed at twice the threshold | `Job:231-280`; `Socket:77-81`; `common/Tests/ASC.Notify.Tests/Billing/LowWalletBalanceLetterTests.cs` | CODE-ONLY (trigger) |
| v4.0: low-balance banners below 1 dollar and letters | release notes v4.0.0 line 76; v3.7.0 lines 97, 113 | NOTES-ONLY |
| **Wallet services switched on per portal** | | |
| Read: DocSpace admin + configured; write: portal-settings right, DocSpace admin, customer, `service` enum (400 otherwise), AI disabled → 403 "AI is disabled for the portal", `AISearch` before `AITools` → 403 "AI Tools service must be enabled before Search", `AITools` off removes `AISearch`, audit, `change-ai-config` socket | `Controller:2109-2167`; `Helper:404-461`; `Socket:88-92` | CODE-ONLY |
| Owner / DocSpace admin toggle `AITools`, `Backup`, `Storage`; `enabledServices` reflects it | `TS/payment.spec.ts:70-245, 1512-1609` | TS-ONLY |
| Room admin / User / Guest 403, anonymous 401 | `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:65-73` (401); `TS/payments.permissions.spec.ts:499-562` | TEST-CONFIRMED (401), TS-ONLY (403) |
| v4.0: web search depends on AI features; v3.2 non-payer may not manage services | release notes v4.0.0 lines 50-52, 72; v3.2.0 line 28 (superseded, see deviations) | NOTES-ONLY |
| **What the wallet pays for** | | |
| Chat / vectorization / web search movements with feature, model, agent; backup beyond the free ones; hourly storage | `TS-AI:140-640` (service names, `Chat`, agent id/title, no source outside an agent, filters); `Quota:565-566` (`CountFreeBackupFeature`); release notes v3.5.0 lines 51-61, v3.6.0 line 18, v4.0.0 lines 50, 79 | TS-ONLY |
| AI tools off → chat answers 402 and no movement | `TS-AI:701-767`; `common/ASC.Core.Common/AI/AiGateway.cs:106-156` (`AccountingPaymentRequiredException` → 402) | TS-ONLY |
| Backups stay free on a portal blocked for a Business plan | release notes v3.5.0 line 60 | NOTES-ONLY |
| **Spending limits on the AI services** | | |
| Scopes `Total`/`Global`/`Custom`, periods `Day`/`Week`/`Month`/`Lifetime`, group `ai-services`, thresholds, `enabled` | `Accounting` (`ServiceLimitScope`, `ServiceLimitPeriod`, `ServiceLimitGroups.AiServices`, `ServiceLimit`); `AccountingRequests:401-405, 455-510` (`MaxAmountValue` 10000, `MaxUserIds` 1000) | CODE-ONLY |
| Refused charge → 402 with the accounting reason ("Service limit exceeded for customer …") | `Accounting:1696-1718`; `ResponseWrapper.cs:97-101`; `Unit/AccountingClientServiceLimitErrorTests.cs` (`Charge_ServiceLimitExceeded_ThrowsServiceLimitExceededWithTheReason`) | TEST-CONFIRMED (mapping of the accounting answer), CODE-ONLY (status code) |
| Personal limit wins; switched-off personal limit falls back to the global one; disabled global → none | `Tariff:1964-2003`; `Unit/TariffServiceServiceLimitCacheTests.cs` (`GetCachedUserServiceLimit_UserWithAPersonalLimit_ReturnsItWithTheUsage`, `…_PersonalLimitSwitchedOff_FallsBackToTheLimitForEveryUser`, `…_LimitForEveryUserSwitchedOff_ReturnsNull`, `…_NoLimitAtAll_ReturnsNull`) | TEST-CONFIRMED |
| **Managing the AI spending limits** | | |
| Every operation: `DemandConfigured` → `DemandAdminAsync` → customer required (404) | `Controller:1603-1957`; `Helper:131-146` | CODE-ONLY |
| Reads: by `id` (404 "Service limit could not be found"), total with `includeUsage`, global, participants page (`offset` ≥ 0, `limit` 1–max count, `orderBy`, `orderType`); missing total/global → 200 null | `Controller:1603-1722`; `AccountingRequests:425-453`; `Tariff:1844-1910`; `Unit/TariffServiceServiceLimitReadTests.cs` (4 cases); `Unit/AccountingClientServiceLimitRequestTests.cs` (request lines and query strings) | TEST-CONFIRMED (service and client), CODE-ONLY (HTTP layer) |
| Create: portal-settings right, both thresholds missing → 400, `Custom` without `userId` → 400, `userId` on another scope → 400, system or unknown user → 404, duplicate → 409, accounting-rejected threshold → 400 with its message, audit, socket | `Controller:1724-1784, 1979-1987`; `Accounting:1672-1716`; `Unit/AccountingClientServiceLimitErrorTests.cs` (`CreateServiceLimit_SecondLimitOfTheScope_ThrowsConflictWithTheReason`, `CreateServiceLimit_RejectedThreshold_ThrowsBadRequestWithTheReason`, `CreateServiceLimit_UnknownServiceGroup_ThrowsBadRequestNotCustomerNotFound`) | TEST-CONFIRMED (client), CODE-ONLY (controller) |
| Update by `id`: thresholds and `enabled`, omitted fields kept, 404 for another portal / group / vanished limit | `Controller:1786-1824`; `Tariff:2082-2109`; `Unit/TariffServiceServiceLimitOwnerTests.cs` (`UpdateServiceLimit_LimitOfAnotherPortal_ReturnsNullWithoutSendingUpdate`, `…_LimitOnAnotherServiceGroup_…`, `…_AlreadyDeletedByAConcurrentRequest_ReturnsNull`, `…_RejectedThreshold_ThrowsBadRequest`) | TEST-CONFIRMED |
| Owner check: limit id is global in the accounting service, matched against the tenant's account number; balance read failure throws instead of hiding the limit | `Tariff:1787-1842`; `Unit/TariffServiceServiceLimitOwnerTests.cs` (`GetServiceLimit_LimitOfAnotherPortal_ReturnsNull`, `GetServiceLimit_BalanceUnavailable_ThrowsInsteadOfReportingNoLimit`, `GetServiceLimit_PortalWithoutBillingCustomer_ReturnsNull`) | TEST-CONFIRMED |
| Set for `userIds` (1–1000, de-duplicated, each a real non-system user), audit with the user ids as target, socket to admins + users | `Controller:1826-1883`; `AccountingRequests:576-595`; `Unit/AccountingClientServiceLimitRequestTests.cs` (`SetParticipantServiceLimits_PutsBodyWithGroupAndParticipantsAndReturnsCount`) | CODE-ONLY (controller), TEST-CONFIRMED (client) |
| Disable all personal limits → count, audit, socket to the quota room | `Controller:1885-1920`; `Unit/AccountingClientServiceLimitRequestTests.cs` (`DisableParticipantServiceLimits_PutsToDisablePathAndReturnsCount`) | CODE-ONLY |
| Delete by `id`: 404 for foreign / unknown / already deleted, audit, socket | `Controller:1922-1956`; `Tariff:2131-2158`; `Unit/TariffServiceServiceLimitOwnerTests.cs` (`DeleteServiceLimit_*`, 3 cases) | TEST-CONFIRMED |
| Cache of the limits dropped by a version key on every change; a read in flight during a change cannot bring the old list back | `Tariff:2043-2070`; `Unit/TariffServiceServiceLimitCacheTests.cs` (`CreateServiceLimit_ReadInFlightDuringTheChange_DoesNotBringTheOldListBack`, `SetParticipantServiceLimits_DropsTheCache_SoTheNextReadSeesTheChange`, `CreateServiceLimit_DropsBothCaches_…`) | TEST-CONFIRMED |
| Integration host: 403 for the owner, 401 anonymous | no .NET test calls these routes (not in the SDK, see deviations); `Helper:53-58, 131-146` | CODE-ONLY |
| **A user's AI spending limit in the profile** | | |
| `aiServicesLimit` for a DocSpace admin or the user themself; `UserServiceLimitDto` fields; null without accounting / customer / limit | `common/ASC.Api.Core/Model/EmployeeFullDto.cs:236, 408-410`; `common/ASC.Api.Core/Model/UserServiceLimitDto.cs`; `common/ASC.Core.Common/Billing/UserServiceLimit.cs` | CODE-ONLY |
| Global limit for a user never counted: `amountConsumed` 0, `periodEnd` = end of the calendar period (UTC, week from Monday), none for lifetime | `common/ASC.Core.Common/Billing/ServiceLimitPeriodExtensions.cs:36-58`; `Unit/ServiceLimitPeriodExtensionsTests.cs`; `Unit/TariffServiceServiceLimitCacheTests.cs` (`GetCachedUserServiceLimit_UserNotInTheList_FallsBackToTheLimitForEveryUser`) | TEST-CONFIRMED |
| Cache 5 min, failure 1 min, soft timeout 1 s / hard 3 s, fail-safe 1 h, null (not empty) when unreadable; portal not a customer decided from the cache only | `Tariff:76-87, 1912-2041`; `Unit/TariffServiceServiceLimitCacheTests.cs` (`…_AccountingFails_ReturnsNullInsteadOfThrowing`, `…_AccountingHangs_ReturnsNullWithinTheTimeoutAndDoesNotWaitAgain`, `…_PortalNotABillingCustomer_ReturnsNullWithoutAskingTheAccountingService`, `…_ReadsEveryPageOnce_ThenServesFromCache`) | TEST-CONFIRMED |
| User socket events carry no limit | `common/ASC.Api.Core/Socket/UserSocketManager.cs:185` | CODE-ONLY |
| **Wallet movements, usage and reports** | | |
| Paging defaults, filters, service-name correction (404 for a name not sold), portal time zone, participant display names | `Controller:968-1034, 1081-1150`; `Helper:214-235, 626-640`; `Tariff:1694-1770` | CODE-ONLY |
| Owner / DocSpace admin page the movements (`offset` 0, `limit` 25, `totalQuantity`, `collection`); date order; unknown participant → empty; bug 81050 (200 after disabling storage) | `TS/payment.spec.ts:949-1147` | TS-ONLY |
| Usage per service, `ai-search` filter; monthly usage | `TS/payment.spec.ts:1150-1223, 2932-2992` | TS-ONLY |
| Reports: document-builder task `DocumentBuilderTask_…`, `isCompleted`, `percentage` 100, `resultFileId`/`resultFileUrl`; terminate; DocSpace admin terminates the owner's | `Controller:1189-1540`; `TS/payment.spec.ts:440-674, 2313-2379, 2382-2655, 2658-2929` | TS-ONLY |
| Room admin / User / Guest 403 on operations, usage, reports (create / get / terminate); anonymous 401 | `TS/payments.permissions.spec.ts:564-873, 2378-2775`; `Tests/PaymentReadEndpointsAnonymousAccessTests.cs:41-151`; `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:73-137` | TEST-CONFIRMED (401), TS-ONLY (403) |
| **Accounting price list of a wallet service** | | |
| Configured billing + accounting, DocSpace admin, `active` filter, `[StringLength(255)]`, cached 1 h, unknown name → 500 | `Controller:1569-1600`; `AccountingRequests:381-399`; `Tariff:1782-1785` | CODE-ONLY |
| Anonymous 401; 256 characters → 400 naming the length; 255 → 403 from the configuration check; owner 403 | `Tests/PaymentAccountingPricesTests.cs:41-74` | TEST-CONFIRMED |
| Shape, `active=true` subset, `servicePrice = costPrice × (1 + extraCharge/100)`, owner/admin parity | `TS/payment.spec.ts:3152-3341`; `TS/payments.permissions.spec.ts:2864-2906` | TS-ONLY |
| **Buying wallet services** | | |
| `Set`/`Add` only, `Renew`/`Sub` → 400; `Add`: not-paid plan + add-on → 402, minimums (`core:accounting:minStorageQuantity` 100, `core:docscloud:minDevPackQuantity` 10, administrators ≥ current Room-admin group), sub-account required (404), purchase with `throwIfFailure`; `Set`: next quantity, null/0 resets, Dev Pack → Docs Connect reversion rule; `calculateWalletPayment` prices an `Add` | `Controller:209-394`; `Helper:190-212, 288-310` | CODE-ONLY |
| Owner / DocSpace admin add 100 storage, `Set` lower, cancel → `true`; calculate → `operationId`, `amount`, USD, `quantity` 100 | `TS/payment.spec.ts:249-437` | TS-ONLY |
| Negative / string / special characters → 400; Room admin / User / Guest 403; anonymous 401 | `TS/payments.permissions.spec.ts:186-498`; `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:49-64` | TEST-CONFIRMED (401), TS-ONLY (rest) |
| **Moving the subscription balance to the wallet** | | |
| `getSubscriptionBalanceInfo`: payer, 402 "Tariff is not paid", 404 "Subscription could not be found", 400 "Invalid product" for a wallet plan | `Controller:396-427`; `Helper:148-171` | CODE-ONLY |
| `moveSubscriptionToWallet`: payer, payment method set (403), administrators product only, quantity > 0 and ≥ current Room-admin group, price read from the quota (zero valid, no DocSpace-admin requirement), move, `EnsureWalletBalanceAsync` with top-up only for an immediate method, 402 "Insufficient balance", `Add` purchase, two audit events | `Controller:429-512`; `Helper:365-378`; `Tariff:472-485, 1513-1554`; commits `7b7d8429ec`, `e043354c2a`, `19d36757af`, `67747193d3` | CODE-ONLY |
| Subscription balance on a test portal → 402 with no body; `movetowallet` → 403 (no payment method) | `TS/lowBalance.spec.ts:357-384`; `TS/payment.spec.ts:3093-3110` | TS-ONLY |
| Anonymous 401 on both | `Tests/PaymentReadEndpointsAnonymousAccessTests.cs:122-129`; `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:145-153` | TEST-CONFIRMED |
| **Active services** | | |
| Active additional quotas that are wallet products or Docs Connect, plus enabled services, AI left out while AI access is disabled | `Controller:1156-1186`; `Helper:237-286, 561-617` | CODE-ONLY |
| Storage with quantity 100 and backup without | derived from `Helper:255-283` — no test | CODE-ONLY |
| **AI prices and model restrictions** | | |
| `getAiPrices`: billing + AI gateway configured, DocSpace admin, chat / embedding / image / web search with `currency` | `Controller:2171-2198`; `Helper:60-66, 463-541` | CODE-ONLY (the TS cases are `test.skip`, `TS/payment.spec.ts:677-760`) |
| `getRestrictedAiModels`: empty set to any member while billing or AI tools is off, else DocSpace admin, 402 without customer | `Controller:2201-2230`; `common/ASC.Core.Common/AI/AiGateway.cs:78-156` | CODE-ONLY |
| `setRestrictedAiModels`: AI gateway configured, portal-settings right, DocSpace admin, customer, AI tools on (403 "AI Tools service is not enabled for the portal"), `models` required, no null entry → 400 naming `Models`, replaces the set, audit | `Controller:2231-2266`; `Helper:68-74, 543-550`; `Tests/PaymentAiModelRestrictionsValidationTests.cs:42-82` (bug 84317) | TEST-CONFIRMED (validation), CODE-ONLY (rest) |
| One / many / cleared / idempotent / sorted / de-duplicated / unknown id dropped; DocSpace admin writes; bug 84316 (write without AI tools does not persist) | `TS/payment.spec.ts:1612-1693, 1886-2263` | TS-ONLY |
| Room admin / User / Guest 403 on GET and PUT; anonymous 401 | `TS/payments.permissions.spec.ts:1822-2077`; `Tests/PaymentReadEndpointsAnonymousAccessTests.cs:106-113`; `Tests/PaymentMutatingEndpointsAnonymousAccessTests.cs:105-113` | TEST-CONFIRMED (401), TS-ONLY (403) |
| **Paid seats and tariff limits** | | |
| `manager` = paid seats; checks and messages | `quotas` spec and its `verification.md` ("Count limits of the tariff") | — (cross-reference) |
| Buying `Admin` units raises the seats | `Controller:300-340` (`Admin` wallet product, minimum = current Room-admin group); `Quota:540` (`CountPaidUserFeature`) | CODE-ONLY |
| **Payment changes are announced** | | |
| New tariff vs cached: `change-quota-feature-value` for storage, paid users, rooms and the free flag | `Tariff:1328-1361` (`NotifyWebSocketAsync`); `Socket:56-60` | CODE-ONLY |
| `top-up-wallet` (`auto`), `wallet-low-balance`, `change-ai-config` | `Socket:71-92`; `Helper:312-331, 365-378`; `Job:200-205, 254-257` | CODE-ONLY |
| `change-service-limits`: quota room for a global limit or the bulk disable; personal rooms `{tenant}-{userId}` of the administrators (+ the user) otherwise, batches of 1000; Node route and hub function | `Socket:138-162`; `Controller:1958-1977, 1872-1877, 1911-1915`; `common/ASC.Socket.IO/app/controllers/files.js:132-135`; `common/ASC.Socket.IO/app/hubs/files.js:444-455` | CODE-ONLY |
| **Audit trail of payment changes** | | |
| Subscription updated, wallet topped up (payer and job), top-up settings updated, wallet services settings updated (also for restricted models), subscription balance moved, contact sales; none on reads / calculations / URLs | `Helper:288-298, 312-331, 365-378, 410-461, 543-550`; `Controller:746-780, 2043-2107`; `Job:200-205, 297-327` | CODE-ONLY |
| Service limit events "Service spending limit created / updated / deleted", "Personal service spending limits set / disabled"; personal limit → target user id, description `Scope (user name)` | `common/ASC.Core.Common/Messaging/MessageAction.cs:1068-1082`; `Controller:1997-2011, 1869, 1904` | CODE-ONLY |
| Top-up audited with "10 USD" | `Helper:312-321` (`$"{amount} {currency}"`); no test reads the audit trail for it | CODE-ONLY |

## Where the server differs from the release notes, the tests or the OpenAPI document (spec follows the server)

| Release note / document / test | Server |
|---|---|
| yaml `getQuotaPaymentInformation` 200: "The quota the portal is on, with its price, features, limits and current usage"; `TenantQuotaFeatureDto.used` text about guests and plain members | Since `8074ce5b54` the answer is trimmed by user type (`QuotaHelper:121-150, 173-183`): a Room admin gets four features, a User one, both an empty `price` and no `dueDate`; `used` is written for every feature a caller is shown. The yaml predates the change (submodule `a0fc3a2`, 2026-10-20). |
| TS `payments.permissions.spec.ts:2090-2116` (`test.fail`, bug 81534): a Room admin and a User "cannot get quota information" (403 "Access denied") | The server deliberately answers 200 trimmed, because the web client reads this call at start-up for every non-guest; the .NET suite pins the 200 (`Tests/PaymentQuotaInformationTests.cs:91-116`). The TS cases are marked as expected failures. |
| yaml has no `servicelimit` path and no `aiServicesLimit` on the user profile | `Controller:1603-1957` publishes nine service-limit operations and `EmployeeFullDto.cs:236` the profile field; the published contract and therefore every SDK lack them, so no integration test can call them and the spec names them by route. Probably the contract has not been regenerated since `feature/service-limit` was merged. |
| yaml `getAccountingServicePrices` / `serviceName` (50039): "a name the accounting service does not price yields an empty list rather than an error" | The accounting service answers 404 for an unknown name and the server relays it as 500 (`Controller:1591`, measured in `TS/payment.spec.ts:3131-3149`); the sentence was removed from the source text in `b3d19d0bd4` and the `[SwaggerResponse(500)]` now says so, but the published yaml still carries it. |
| yaml `moveSubscriptionToWallet` 404: "the price of the administrators product is unknown" | Dropped in `67747193d3`: the price is read from the quota itself (`Controller:496`), so the 404 cannot occur; a zero price is valid (`e043354c2a`). |
| Previous draft of this spec: `getWalletService` with `AITools` "on an installation that does not sell it" → 404 | The integration host sells it: `Tests/PaymentWalletServiceTests.cs:42-51` asserts 200 with price 0. The 404 exists only for a quota that is not in the catalogue or for an AI service while AI access is disabled (`Controller:701-706`). |
| Release notes v3.2.0 line 28: the non-payer cannot interact with the services | Superseded by v4.0.0 line 72: every DocSpace admin switches services and buys from the wallet (`Controller:2137-2167`, `Helper:131-146`); only charging the payment method stays with the payer. |
| Release notes v3.2.0 line 30: top-up "only a whole number" | `amount` is an integer `[Range(1, 999999)]` (`Requests:220`). Agrees. |
| Release notes v3.5.0 line 56: two free backups a month on Business | The free count is a quota feature (`Quota:565`, `CountFreeBackupFeature`) whose value comes from the quota row, not a constant; the spec says "the free ones of the plan". |
| TS `payment.spec.ts:3093-3110`: `movetowallet` "returns 403 … no real Stripe subscription" | The 403 comes from "Customer payment method is not set" (`Controller:435-438`, `InvalidOperationException`), checked before the subscription is read; the comment names the wrong cause. The .NET suite has only the 401 case. |
| TS `lowBalance.spec.ts:357-384`: subscription balance on a test portal → 402 | Agrees with `Helper:151-154` ("Tariff is not paid" when the sandbox-paid portal has no provider subscription). |
| Test name `GetQuotaPaymentInformation_User_ReturnsTotalSizeOnly` | The assertion is `ContainSingle` on `total_size` with `Used != null`, plus empty price, no due date and the owner's quota settings — the name understates it. |
| `Tests/PaymentCurrenciesTests.cs`, `PaymentPricesTests.cs`, `PaymentRequestTests.cs:121` carry bug traits 81512, 81516, 81525 | All three assert the fixed behaviour (403 / 400); the traits are history, not open bugs. |

## Left out of the spec, and why

- **Docs Connect** (`docscloud-1-hour`, `docscloud-devpack-1-hour`, the trial, `SwitchToDevPack`, `DocsCloudController`,
  the Docs Connect tenant quota and its report): a feature of its own with its own controller and tests
  (`web/ASC.Web.Api.Tests/Tests/02_Settings/DocsCloud/`); here only as two of the six wallet services and the
  reversion rule inside `updateWalletPayment`.
- **The accounting currencies list** (`GET portal/payment/accounting/currencies`): a bare relay of the accounting
  catalogue for DocSpace admins (`Controller:1544-1566`), not in the yaml excerpt the spec cites; nothing to specify
  beyond "DocSpace admin, configured".
- **The provider checkout pages, the payment handler (`payment.ashx`) and the billing/accounting HTTP contracts**
  (`common/ASC.Core.Common/Billing/IBillingApi.cs`, `IAccountingApi.cs`, `BillingClient.cs`): external systems; the
  spec states only what the portal API answers.
- **Session-based charging** (`OpenCustomerSessionAsync` / `CloseCustomerSessionAsync` / `CompleteCustomerSessionAsync`,
  `Tariff:1666-1692`): the mechanism behind the AI and storage movements; the spec names only the movements.
- **The exact cache keys and durations** of tariff, customer, balance, prices and service limits (`Tariff:69-90,
  594-614, 2043-2056`): implementation; the spec says "minutes" / "an hour".
- **Price retrieval internals** (billing products are numeric ids, accounting services are names; `Tariff:835-878`).
- **The wallet dashboard, banners, dialogs and letters' wording** (release notes v3.2, v3.7, v4.0): client and
  `notifications`.
- **Rate-limit headers and 429/5xx rows** of the yaml beyond the one 429 the spec mentions: generic policy.
- **`Payment-Info` header semantics** of the gate: see W1.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| W1 | Payment gate | The 402 gate is skipped whenever the request carries a `Payment-Info` header that is not `true` (any value that does not parse as `true`, e.g. `false`), so any client of a NotPaid portal can keep using every API by adding one header. The marker is meant for the web client's own start-up requests but is not restricted to them. | `Filter:48-56` |
| W2 | `getCustomerInfo`, `getRestrictedAiModels` | Both return 200 with an empty result to every signed-in member, Guests included, before any role check when billing (or AI tools) is not configured, and 403 for the same roles once it is. The access rule depends on the installation rather than on the caller. | `Controller:838-848, 2201-2210` |
| W3 | Contract | Nine `servicelimit` operations and the `aiServicesLimit` profile field are missing from the published OpenAPI document and the SDKs although the controller and DTO publish them; agents and generated clients cannot reach them and no integration test exists. | `Controller:1603-1957`; `EmployeeFullDto.cs:236`; yaml |
| W4 | `moveSubscriptionToWallet` | `quota.Price` is the price resolved for the *request region's* currency (`Quota:838-878`) while the wallet is ensured in the first supported accounting currency (USD); on a portal whose request region prices in EUR the required amount is an EUR figure compared with a USD balance. | `Controller:489-507`; `Tariff:1513-1554` |
| W5 | `setTenantWalletSettings` | `currency` is stored unchecked (any string, `[StringLength(3)]` only on the deposit DTO, not here). The job looks up the sub-account by that currency and silently never tops up when none matches; the low-balance branch falls back to the default currency, the top-up branch does not. | `Controller:2074-2083`; `Job:139-144, 240-246` |
| W6 | Service limit writes | The accounting change is committed before the audit record and the socket event; a failure in `GetAdministratorIdsAsync` or the socket call turns a successful create/update/delete into a 500 while the limit is already changed and the cache already dropped. | `Controller:1777-1783, 1815-1821, 1949-1955` |
| W7 | `setParticipantServiceLimits` | Every one of up to 1000 `userIds` is checked with a separate user lookup before one accounting call, and the audit record targets all of them; a 1000-user request costs 1000 lookups per call. | `Controller:1861-1869`; `Helper` none |
| W8 | `getParticipantServiceLimits` | A 404 from the accounting route is logged as an error and relayed as 500 on purpose (a 404 means a missing route, not "no limits"), but the same path returns an empty page for a portal unknown to the accounting service — a client cannot tell "no limits" from "portal not yet a customer" except by the 404 of the other service-limit reads. | `Tariff:1883-1910` |
| W9 | `getPaymentUrl` | Quantity ≤ 0 is checked only after `DemandConfigured` and `DemandAdminAsync`, and "exactly one product" by the DTO validator, so an installation without billing answers 403 to a body with `{"admin": 0}` that a billing portal refuses with 400; the two environments disagree on the validation order. | `Controller:109-118`; `Tests/PaymentUrlValidationTests.cs:97-130` |
| W10 | `getCustomerBalance` | A failed accounting read is cached as an empty balance for 10 minutes, indistinguishable from "never a customer" for the clients (`subAccounts` missing); `refresh=true` is needed to recover earlier. | `Tariff:1650-1662` |
