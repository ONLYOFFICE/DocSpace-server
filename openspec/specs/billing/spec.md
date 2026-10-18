# billing Specification

## Purpose

Defines the tariff and payment side of a portal: the tariff the portal is on and its state (trial, paid,
grace period, not paid), what the server computes from the due date, the gate that refuses most API calls
once the payment has lapsed, the payment settings and catalogue a client needs to sell a plan
(`settings/payment`, `portal/payment/prices`, `currencies`, `quotas`, `walletservices`), the purchase and
change of a subscription through the hosted checkout (`portal/payment/url`, `update`, `account`,
`checkoutsetupurl`, `customerinfo`), the sales request, the portal wallet of a hosted portal (balance,
top-up, automatic top-up, low balance, movements, usage and reports), the wallet services switched on per
portal and bought from the wallet (storage, backup, AI tools, AI search, administrators, Docs Connect), the
AI prices and model restrictions, and who may see or change any of it. The storage and count limits of a
tariff and the refusals they cause are in `quotas` and only referred to here; the letters a lapsed payment
or a low balance produce are in `notifications`; the licence file of a self-hosted installation
(`settings/license`) is in `branding-and-licensing`. A self-hosted installation has one tariff for all its
portals and no billing service, so most of this specification is about hosted portals.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`04_Portal/Payments`, `04_Portal/Quota`,
`02_Settings/Common`, `04_Portal/Users`) and the TypeScript suite they were ported from, the OpenAPI
contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`), the product
code of the payment controller, the tariff service and the wallet job where the tests stop, the runtime
configuration, and the release notes (v1.1, v3.1, v3.2, v3.5, v3.6, v3.7, v4.0). The integration host has
no billing service, so every operation that needs one answers 403 there and only its authentication guard
is test-confirmed; the evidence per scenario, the differences and the suspected defects are in
`verification.md` next to this file.

## Requirements

### Requirement: Tariff state of a portal

A portal SHALL be in one of four tariff states: `Trial`, `Paid`, `Delay` (the grace period) and
`NotPaid`. The state is derived from the tariff's due date on every read: a trial quota makes the state
`Trial` and gives the tariff a due date a trial period (30 days unless the installation sets another)
after the portal was created or its trial was reset; a due date in the past makes it `Delay` with
`delayDueDate` = due date + grace days; a due date older than the grace days makes it `NotPaid`. A
lifetime quota never expires and a tariff without a due date is `Paid`.

#### Scenario: Paid tariff crosses its due date

- **WHEN** a paid tariff's due date was yesterday and the installation grants a grace period
- **THEN** `getPortalTariff` reports `state` Delay and `delayDueDate` the due date plus the grace days
- **AND** once the grace days have passed as well the state is NotPaid

#### Scenario: Free plan never lapses

- **WHEN** a hosted portal has never bought a plan
- **THEN** it runs on the free plan with `state` Paid and a due date that never comes

#### Scenario: Trial period

- **WHEN** the installation enables trials and the portal's quota is a trial one
- **THEN** `state` is Trial and the due date is the trial period after the portal's creation, with no grace period afterwards

### Requirement: Tariff of a hosted portal follows the billing service

On a hosted portal with a billing service the tariff SHALL be re-read from the current payments of the
billing customer when the cache expires or `refresh=true` is passed: the paid quotas with their
quantities, the payer e-mail as `customerId` and the due date of the earliest payment. When the billing
service no longer reports the payment of a paid, non-free tariff (a refund), the due date SHALL be set to
yesterday so the portal enters the grace period; a portal with no payment at all and no free quota SHALL
become NotPaid.

#### Scenario: Refund

- **WHEN** the billing service stops reporting the payment of a portal on a priced plan
- **THEN** the tariff's due date becomes yesterday and the portal is in the grace period on the next read

### Requirement: Single tariff of a self-hosted installation

A self-hosted installation SHALL have one tariff shared by all its portals, read from the licence file
(`branding-and-licensing`) and never from a billing service. An Enterprise installation without an
activated licence SHALL get a 30-day trial tariff on first read; an installation without a licence path
(open source) SHALL report `openSource` true and is never treated as not paid. Every operation that needs
the billing service SHALL answer 403 "Tariff service is not configured" on such an installation.

#### Scenario: Integration host without billing

- **WHEN** the owner calls `getCustomerBalance`, `topUpDeposit` or `updatePayment` on an installation with no billing service
- **THEN** the system answers 403 regardless of the caller's role, and an anonymous caller 401

#### Scenario: Open-source installation

- **WHEN** the owner reads `getPortalTariff` on an installation without a licence path
- **THEN** `openSource` is true, `enterprise` and `developer` false and `state` Paid

### Requirement: Reading the tariff

`getPortalTariff` SHALL answer every signed-in member (anonymous 401) and grade the body by role: every
member gets `state`; a Room admin or DocSpace admin also `dueDate` and `delayDueDate`; a caller with the
portal-settings right also `id`, `customerId`, `licenseDate`, `openSource`, `enterprise`, `developer` and
`quotas` — one row per paid quota and per overdue quota with `id`, `quantity`, `wallet`, `additional`,
`dueDate`, `nextQuantity` and `nextQuota`. Everyone else gets `id` 0 and no dates. `refresh=true`
re-reads the billing service.

#### Scenario: Owner and DocSpace admin read the full tariff

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the system answers 200 with `state` Paid, `id` of 0 or more and a non-empty `quotas`

#### Scenario: Other members get the state only

- **WHEN** a Room admin, a User or a Guest calls `getPortalTariff`
- **THEN** the system answers 200 with `id` 0 and `state` Paid, and no `customerId` or `quotas`

### Requirement: Upcoming payments

`getUpcomingPayments` SHALL list, to a caller with the portal-settings right (403 otherwise, 401
anonymous), one entry per quota of the tariff that will be charged next, with `id`, `name`, `title`,
`unitOfMeasure`, `quantity`, `wallet`, `dueDate`, `amount` (unit price times quantity) and `currency`.
An overdue quota and a quota without a price (trial, free) SHALL be skipped, and a quota with a switch
scheduled for the next period SHALL be listed as the quota it switches to with its next quantity. The
call keeps answering while the payment has lapsed.

#### Scenario: Free portal

- **WHEN** the owner of a portal on the free plan calls `getUpcomingPayments`
- **THEN** the system answers 200 with an empty list

### Requirement: Payment gate while the portal is not paid

While a portal's tariff is NotPaid the server SHALL answer 402 with no body to every API call except the
ones marked as allowed without payment: the portal record, `getPortalTariff`, `getUpcomingPayments`,
`getPortalQuota`, the extra licence information, every `portal/payment/*` operation, `getPaymentSettings`
and the suspend and delete instructions. The gate applies only where tariff settings are enabled: a
hosted portal, or a self-hosted installation with a licence path, that is not open to anyone. During the
grace period no gate applies, but the count limits of `quotas` refuse new rooms and paid members with 402.

#### Scenario: Lapsed portal lists files

- **WHEN** a member of a NotPaid hosted portal lists a folder
- **THEN** the system answers 402 and the body is empty

#### Scenario: Lapsed portal reads its tariff

- **WHEN** a member of a NotPaid hosted portal calls `getPortalTariff` or any `portal/payment/*` operation
- **THEN** the call is answered as usual, so the client can show the payment page

#### Scenario: Grace period

- **WHEN** a portal is in the grace period and the owner creates a room
- **THEN** the system answers 402 "Your pricing plan does not support this option"

### Requirement: Payment settings

`getPaymentSettings` SHALL answer a caller with the portal-settings right (403 otherwise, 401 anonymous)
with `salesEmail`, `buyUrl` (both regional, from the installation's external resources), `standalone`,
`currentLicense` (`trial` of the current quota and `dueDate` of the tariff, time of day cut off) and
`max`, the largest quantity one purchase may ask for (999 unless configured). `feedbackAndSupportUrl` is
never filled. The call keeps answering while the payment has lapsed.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getPaymentSettings`
- **THEN** the system answers 200 with a non-empty `salesEmail` and `buyUrl`, a `currentLicense` and `max` greater than 0

### Requirement: Price and currency catalogue

`getPortalPrices` SHALL return, to a caller with the portal-settings right (403 for a Room admin, a User
and a Guest, 401 anonymous), the unit price of every purchasable product keyed by product name in the
currency of the request, `0` where unpriced; `getPaymentCurrencies` SHALL return the default currency of
the portal region first (`isoCountryCode`, `isoCurrencySymbol`, `currencyNativeName`), then the currency
of the request when it differs. Neither needs the billing service.

#### Scenario: Prices and currencies for the owner

- **WHEN** the owner calls `getPortalPrices` and `getPaymentCurrencies`
- **THEN** prices are a map with no negative value and the first currency is `US` / `$` / `US Dollar`

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls either operation, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

### Requirement: Purchasable quotas and wallet services

`getPaymentQuotas` SHALL list the visible quotas, filtered by `wallet` and `additional` when given, each
with `id`, `title`, `price`, `nonProfit`, `free`, `trial`, `features` and `dueDate`; on a non-profit
portal `additional=false` returns only the current quota. `getWalletServices` SHALL list the services
sold from the wallet with price, unit and features, variants folded into the visible one; `getWalletService`
SHALL return one by `service` name (404 "Service could not be found" when not sold, 400 for an unknown
name). All three need the portal-settings right (403, anonymous 401).

#### Scenario: Catalogue on a host without a tariff catalogue

- **WHEN** the owner calls `getPaymentQuotas` with no filter, `wallet=true` or `additional=true` on an installation that seeds no quotas
- **THEN** the system answers 200 with a list that may be empty, every entry carrying `features`

#### Scenario: Six wallet services of a hosted portal

- **WHEN** the owner calls `getWalletServices` on a hosted portal
- **THEN** the list holds `disk-storage-1-hour`, `backup`, `ai-tools`, `docscloud-1-hour`, `docscloud-devpack-1-hour` and `ai-search`, each priced in USD, AI tools and AI search at 0

#### Scenario: Wallet service by name

- **WHEN** the owner calls `getWalletService` with `service` `AITools` on an installation that does not sell it
- **THEN** the system answers 404 "Service could not be found"
- **AND** a Room admin, a User or a Guest gets 403 and an anonymous caller 401

### Requirement: Current plan with its usage

`getQuotaPaymentInformation` SHALL answer every signed-in member except a Guest (403; anonymous 401) with
the quota the portal is on — `id`, `title`, `price`, `free`, `trial`, `nonProfit`, `features` and
`dueDate` — graded by role as `quotas` describes: admin-only features are dropped for other roles and a
User sees `used` only on `total_size`. The call needs no billing service, so a fresh hosted portal
reports its free plan (`id` -3, title "Startup", price 0, `free` true).

#### Scenario: Room admin and User read the plan

- **WHEN** a Room admin or a User calls `getQuotaPaymentInformation`
- **THEN** both get 200 with a non-empty `features`, and for the User only `total_size` carries `used`

### Requirement: Buying a plan through the hosted checkout

`getPaymentUrl` SHALL require a DocSpace admin (403 "Access denied" for a Room admin, a User and a Guest,
401 anonymous) and the billing service, a body with `backUrl` and `successUrl` (absolute URLs of at most
255 characters) and `quantity` holding exactly one product with a quantity above 0 (400 "Invalid
quantity"). The product SHALL be a visible monthly plan (400 "Only monthly product can be purchased per
transaction"); yearly plans are sold by sales only. A portal whose customer is already on a paid tariff
gets an empty result. The plan at the asked quantity SHALL fit the portal's current usage (402).

#### Scenario: Owner asks for the checkout page

- **WHEN** the owner calls `getPaymentUrl` with `quantity` `{"admin": 1}` and the portal's own URL as `backUrl` and `successUrl`
- **THEN** the system answers 200 with a checkout URL carrying `qty` 1, `currency`, `language`, `refId` and `email`

#### Scenario: Quantity of zero

- **WHEN** the owner sends `quantity` `{"admin": 0}` or a negative number
- **THEN** the system answers 400 "Invalid quantity"

#### Scenario: Back URL not a URL

- **WHEN** `backUrl` is not an absolute `http`, `https` or `ftp` URL, or is longer than 255 characters
- **THEN** the system answers 400 naming `BackUrl`

### Requirement: Changing the subscription quantity

`updatePayment` SHALL require the billing service, a billing customer (404) and the payer (403 "Access
denied" for everyone else, the owner and other DocSpace admins included; 401 anonymous), and a body with
exactly one non-wallet product. While the portal is on a priced plan the product SHALL be that plan (400
"Invalid product"); the quantity already in effect is refused (400 "Invalid quantity"); the new quantity
SHALL fit the portal's usage (402). The change is charged to the payment method on file, recorded in the
audit trail, and answers `true` when the provider accepted it. Ten requests a minute per user.

#### Scenario: DocSpace admin who is not the payer

- **WHEN** the owner is the payer and a DocSpace admin calls `updatePayment`
- **THEN** the system answers 403 "Access denied"

### Requirement: Billing account page and payment method

`getPaymentAccount` SHALL answer the payer or the portal owner (403 otherwise) with the portal-relative
address of the billing account handler, `backUrl` appended when given, or an empty result without a
customer; opening the handler redirects to the provider's account page. `getCheckoutSetupUrl` SHALL give a
DocSpace admin — the payer once a customer with an e-mail exists — the hosted page that attaches a
payment method, with `backUrl` and `successUrl` required, or an empty result when a method is already
set. `getCustomerInfo` SHALL show a DocSpace admin the customer: `portalId`, `email`,
`paymentMethodStatus`, `paymentMethodType`, `isDelayedPaymentMethod` and the `payer` account.

#### Scenario: Owner reads the account link

- **WHEN** the owner of a portal with a billing customer calls `getPaymentAccount` with a `backUrl`
- **THEN** the system answers 200 with a string containing `payment.ashx` and `backUrl`
- **AND** a DocSpace admin who is not the payer, a Room admin, a User and a Guest get 403 "Access denied"

#### Scenario: Checkout setup page

- **WHEN** the owner calls `getCheckoutSetupUrl` with `backUrl` and `successUrl`
- **THEN** the URL carries `currency`, `language`, `refId`, `email`, `successUrl` and `cancelUrl`
- **AND** a Room admin, a User and a Guest get 403, an anonymous caller 401

#### Scenario: Customer with the owner as payer

- **WHEN** the owner or a DocSpace admin calls `getCustomerInfo` on a portal that bought a plan
- **THEN** `paymentMethodStatus` is 1 (set) and `payer` carries the owner's `id` and `displayName`

### Requirement: Who is the payer

The payer of a portal SHALL be the portal account whose e-mail is the e-mail of the billing customer.
Operations that charge the payment method on file or change what is charged — `updatePayment`,
`topUpDeposit`, `setTenantWalletSettings`, `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet` —
SHALL be refused to every other caller with 403 "Access denied: insufficient permissions for this payment
operation", the owner included unless the owner is the payer. Reading the wallet, its movements and the
catalogue, switching wallet services and buying from the wallet SHALL be open to every DocSpace admin.

#### Scenario: DocSpace admin tops up the wallet of a portal the owner pays for

- **WHEN** a DocSpace admin who is not the payer calls `topUpDeposit`
- **THEN** the system answers 403 "Access denied"

#### Scenario: DocSpace admin switches a service

- **WHEN** a DocSpace admin who is not the payer calls `changeTenantWalletServiceState`
- **THEN** the system answers 200 and the service is switched

### Requirement: Contacting the sales team

`sendPaymentRequest` SHALL require a DocSpace admin (403 "Access denied" for a Room admin, a User and a
Guest, 401 anonymous) and a body with `userName` (at most 255 characters), `email` (a well-formed address
of at most 64 characters) and `message` (at most 255 characters), none empty: 400 with "Incorrect
firstname or lastname", "Incorrect email" or "Message text is empty" for an empty field and the field
name for an overlong one. It SHALL mail the message to the vendor's sales address, record it in the audit
trail and answer 200 with no body; it needs no billing service and allows ten requests a minute per user.

#### Scenario: Owner and DocSpace admin send a request

- **WHEN** the owner or a DocSpace admin posts a name, an e-mail and a message
- **THEN** the system answers 200

#### Scenario: Field too long or empty

- **WHEN** `userName` or `message` has 256 characters, `email` has 65, or any of the three is empty
- **THEN** the system answers 400 naming the field or the empty-field message

### Requirement: Wallet balance

`getCustomerBalance` SHALL answer a DocSpace admin (403 otherwise and without a billing service, 401
anonymous) with the wallet as the accounting service holds it: `accountNumber`, `subAccountNumber`,
`accountCurrency`, one entry of `subAccounts` per currency with its `amount`, and `lastCredit`; a portal
that has never been a customer gets an empty result. `refresh=true` re-reads the accounting service. The
wallet is what every wallet service is charged against; an AI charge SHALL reduce the balance by exactly
the amount the matching movement records.

#### Scenario: Funded wallet

- **WHEN** the owner or a DocSpace admin of a funded portal calls `getCustomerBalance`
- **THEN** `accountCurrency` is USD, the USD sub-account has an amount above 0 and `lastCredit` has a date, currency and amount
- **AND** a Room admin, a User and a Guest get 403 "Access denied", an anonymous caller 401

#### Scenario: AI charge taken off the wallet

- **WHEN** an AI chat reply is produced on a portal with AI tools on
- **THEN** the balance after the charge is the balance before minus the debit of the `Chat` movement

### Requirement: Wallet top-up

`topUpDeposit` SHALL require the billing service, a customer (404) and the payer (403), `currency` among
the accounting currencies of the installation (400 "Unsupported currency") and a whole `amount` from 1 to
999999 (400), and a payment method set on the customer (403 "Customer payment method is not set"). It
charges that method, credits the wallet, records the top-up in the audit trail and answers `true`; with
an immediate payment method it also pushes the new balance to the portal's clients and re-arms the
low-balance warning. Two identical requests charge twice; ten requests a minute per user.

#### Scenario: Top-up pushed to clients

- **WHEN** the payer tops up with a payment method that settles at once
- **THEN** the system answers `true` and the portal's clients receive the top-up event with `auto` false

#### Scenario: Delayed payment method

- **WHEN** the payer tops up with a payment method that settles later
- **THEN** the system answers `true`, no balance event is sent and the low-balance warning is not re-armed

### Requirement: Automatic top-up settings

`getTenantWalletSettings` SHALL answer a DocSpace admin (403 otherwise, 401 anonymous) with `enabled`,
`minBalance`, `upToBalance`, `currency`, `lowBalanceThreshold`, `lowBalanceNotified` and `lastModified`,
defaults when never set; it needs no billing service. `setTenantWalletSettings` SHALL require the payer
(403) and an existing balance (404 "Balance could not be found"), accept `settings` with `enabled`,
`minBalance` 5–1000, `upToBalance` 6–5000 and greater than `minBalance` (400 naming `UpToBalance`), and
`currency`; the low-balance fields sent are ignored. Enabling re-arms the low-balance warning.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getTenantWalletSettings`
- **THEN** the system answers 200 with a `response` body, and after the payer stored `enabled` true, 100 and 1000 it reports those values with a `lastModified`

#### Scenario: Bounds rejected before the payer check

- **WHEN** the owner sends `minBalance` 4, or `minBalance` 100 with `upToBalance` 50 or 100
- **THEN** the system answers 400, naming `UpToBalance` when the upper bound is not above the lower one

#### Scenario: Valid bounds on a host without billing

- **WHEN** the owner sends `minBalance` 5 and `upToBalance` 6 on an installation without a billing service
- **THEN** the body passes validation and the system answers 403 from the configuration check

### Requirement: Automatic top-up and low-balance watch

A background job SHALL visit every portal with stored wallet settings every three minutes (configurable).
With automatic top-up on and the balance in `currency` below `minBalance` it SHALL charge the payment
method `upToBalance` minus the balance, record the top-up in the audit trail and push the balance with
`auto` true; a customer without a payment method or with a delayed one SHALL have automatic top-up
switched off and be sent the matching letter. With it off, a balance below the threshold (1 unless
configured) SHALL send the low-balance letter once, re-armed when the balance reaches twice the
threshold.

#### Scenario: Balance below the trigger

- **WHEN** automatic top-up is on with `minBalance` 100 and `upToBalance` 1000 and the balance is 80
- **THEN** the job charges 920 and the clients receive the top-up event with `auto` true

#### Scenario: Balance under the low-balance threshold

- **WHEN** automatic top-up is off and the balance falls under 1
- **THEN** the payer and the owner get the low-balance letter of `notifications` once, and not again until the balance recovered to 2 and fell again

### Requirement: Wallet services switched on per portal

`getTenantWalletServiceSettings` SHALL answer a DocSpace admin (403 otherwise and without billing) with
`enabledServices` and `lastModified`. `changeTenantWalletServiceState` SHALL require the portal-settings
right, a DocSpace admin and a customer (404), take `service` (`Storage`, `Backup`, `AITools`, `Admin`,
`DocsCloud`, `DocsCloudDevPack`, `AISearch`; 400 otherwise) and `enabled`, refuse an AI service while AI
is disabled for the portal and `AISearch` while `AITools` is off (403), switch `AISearch` off with
`AITools`, record the change in the audit trail, notify clients when `AITools` changed, and answer the
whole set.

#### Scenario: Owner switches AI tools, backup and storage on and off

- **WHEN** the owner or a DocSpace admin posts `service` `AITools`, `Backup` or `Storage` with `enabled` true, then false
- **THEN** each answer is 200 and `enabledServices` contains the service after the first call and not after the second

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `changeTenantWalletServiceState`, or an anonymous caller does
- **THEN** the system answers 403 "Access denied" (401 for anonymous)

#### Scenario: AI search needs AI tools

- **WHEN** `AITools` is off and the owner switches `AISearch` on
- **THEN** the system answers 403 "AI Tools service must be enabled before Search"

### Requirement: What the wallet pays for

An AI chat reply, a vectorization and a web search SHALL be charged to the wallet as movements of the
`ai-tools` and `ai-search` services, each naming the feature (`Chat`, `Vectorization`), the model and
the agent it ran in (`agentId`, `agentTitle`, empty for a chat outside an agent); a backup beyond the free
ones of the plan to `backup`; extra storage hourly to `disk-storage-1-hour`. While `AITools` is off an AI
request SHALL be refused with 402 and nothing charged. Backups stay free and unlimited on a portal
blocked for non-payment of a Business plan.

#### Scenario: Chat inside an agent

- **WHEN** a chat is sent inside an AI agent with AI tools on
- **THEN** a `Chat` movement for `ai-tools` appears in `getCustomerOperations` with the agent's id and title, and `getCustomerServiceUsage` sums it under `ai-tools`

#### Scenario: AI tools switched off

- **WHEN** the owner switches `AITools` off and sends a chat message
- **THEN** the send answers 402 and no `ai-tools` movement is added

### Requirement: Wallet movements, usage and reports

`getCustomerOperations` SHALL page the wallet movements of a DocSpace admin's portal (`offset` 0 and
`limit` 25 by default, echoed with `totalQuantity`, `totalPage`, `currentPage`), filtered by
`serviceName` (404 for a name not sold), `startDate`/`endDate` in the portal time zone,
`participantName`, `credit`, `debit`, `type`, `status`, sorted by `orderBy`/`orderType`.
`getCustomerServiceUsage` SHALL add them up per service, `getCustomerMonthlyUsage` per calendar month.
Each has a report trio (create / get / terminate) producing a document in My documents through a
document-builder task; all need a DocSpace admin (403) and a customer.

#### Scenario: Owner pages the movements

- **WHEN** the owner or a DocSpace admin calls `getCustomerOperations` without paging
- **THEN** `offset` is 0, `limit` 25, `totalQuantity` 0 or more and `collection` present; `orderBy` date descending returns the newest first

#### Scenario: Unknown participant

- **WHEN** `participantName` names nobody
- **THEN** the system answers 200 with an empty `collection`

#### Scenario: Usage per service

- **WHEN** the owner calls `getCustomerServiceUsage`, or a DocSpace admin with `serviceName` `ai-search`
- **THEN** every row carries `service`, `operationCount` and `totalAmount` of 0 or more, and the filtered answer holds `ai-search` rows only

#### Scenario: Operations report

- **WHEN** the owner or a DocSpace admin calls `createCustomerOperationsReport` for `ai-tools`, `backup` or `disk-storage-1-hour` and polls `getCustomerOperationsReport`
- **THEN** the task id starts with `DocumentBuilderTask_`, `isCompleted` turns true with `percentage` 100, an empty `error`, a `resultFileId` and a `resultFileUrl`
- **AND** a Room admin, a User and a Guest get 403 "Access denied" on create, get and terminate, an anonymous caller 401

### Requirement: Buying wallet services

`updateWalletPayment` SHALL require the billing service, a DocSpace admin and a customer, a body with
exactly one wallet product and `productQuantityType` `Set` or `Add` (400 "Invalid product quantity type").
`Add` buys the units now from the wallet balance (404 without a sub-account; 402 when the plan is not paid
and the service is an add-on; minimums: storage 100, Docs Connect Dev Pack 10, administrators not fewer
than the portal has; 400 "Invalid quantity"). `Set` writes the quantity for the next period, an empty or
zero quantity cancelling a scheduled change. `calculateWalletPayment` SHALL price an `Add` without buying.

#### Scenario: Owner adds, lowers and cancels storage

- **WHEN** the owner or a DocSpace admin buys 100 storage units with `Add`, schedules a lower quantity with `Set`, then cancels it with an empty `Set`
- **THEN** every answer is 200 `true`
- **AND** a Room admin, a User and a Guest get 403 "Access denied", an anonymous caller 401

#### Scenario: Price before buying

- **WHEN** the owner or a DocSpace admin calls `calculateWalletPayment` with 100 storage units
- **THEN** the answer carries `operationId`, `amount`, `currency` USD and `quantity` 100

#### Scenario: Negative or non-numeric quantity

- **WHEN** the quantity is negative, a string or special characters
- **THEN** the system answers 400 ("Invalid quantity", or the JSON conversion error for a non-number)

### Requirement: Moving the subscription balance to the wallet

`getSubscriptionBalanceInfo` SHALL show the payer (403) the unused balance of the current subscription
period with its boundaries and currencies (402 when the plan is not paid, 404 without a subscription, 400
for a wallet plan). `moveSubscriptionToWallet` SHALL, for the payer with a payment method set, move that
balance to the wallet, top the wallet up to the price of the asked number of administrators when it is
short (402 "Insufficient balance" when still short), buy them from the wallet, record both steps in the
audit trail and answer `true`.

#### Scenario: Fewer administrators than the portal has

- **WHEN** the payer asks for fewer administrators than the portal already has
- **THEN** the system answers 400 "Invalid quantity" and nothing is moved

### Requirement: Active services

`getActiveServices` SHALL list to a DocSpace admin (403 otherwise and without billing) the wallet services
in force: the active additional quotas of the tariff that are wallet products or the Docs Connect trial,
with their quantity, plus the services switched on by hand, each with its limits and usage where known;
AI services SHALL be left out while AI is disabled for the portal.

#### Scenario: Storage bought and backup switched on

- **WHEN** a portal has 100 storage units active in its tariff and backup switched on
- **THEN** `getActiveServices` lists storage with quantity 100 and backup without one

### Requirement: AI prices and model restrictions

`getAiPrices` SHALL need the billing service and the AI gateway (403) and a DocSpace admin, and return
chat, embedding and image model prices and web search prices with `currency`. `getRestrictedAiModels`
SHALL return `models`, the chat models barred on the portal (402 when AI tools is on but the billing
customer is missing). `setRestrictedAiModels` SHALL require the portal-settings right, a DocSpace admin, a
customer and AI tools on (403), and a `models` set that is present and holds no null entry (400 naming
`Models`); it replaces the stored set.

#### Scenario: Null entry rejected before the configuration check

- **WHEN** the owner sends `models` `[null]` or `["openai/gpt-4o", null]`
- **THEN** the system answers 400 naming `Models`

#### Scenario: Valid set on a host without billing

- **WHEN** the owner sends `["openai/gpt-4o"]` or `[]` on an installation without a billing service or AI gateway
- **THEN** the body passes validation and the system answers 403

#### Scenario: Restriction read back

- **WHEN** the owner or a DocSpace admin stores one model id and reads `getRestrictedAiModels`
- **THEN** `models` equals that one id

### Requirement: Paid seats and tariff limits

The number of DocSpace admins and Room admins a portal may hold is the `manager` count of its tariff and
the checks are specified in `quotas` ("Count limits of the tariff"): the invitation link of a paid role
answers 402 once the paid seats are used up, a bulk invitation that would exceed them is refused as a
whole, and the same checks answer 402 "Your pricing plan does not support this option" during the grace
period and after. Buying `Admin` units from the wallet raises the seats of a hosted portal.

#### Scenario: Paid seats used up

- **WHEN** the portal's paid-user limit is reached and the owner reads the invitation link of DocSpace admin or Room admin
- **THEN** the system answers 402

### Requirement: Tariff change is announced

When a re-read tariff differs from the cached one the server SHALL push the new storage, paid-user and
room limits to the portal's clients as quota feature values, and the free flag when it changed, so an
open client updates its limits without a reload; a top-up pushes the balance, a switched AI tools
service pushes the AI configuration. The letters sent three days before a payment is due, in the grace
period, and on a low or non-renewable wallet are in `notifications` ("Payment warning letters", "Wallet
letters").

#### Scenario: Plan bought

- **WHEN** the billing service reports a bought plan with a larger storage allowance
- **THEN** every connected client of the portal receives the new `total_size` and `manager` values

### Requirement: Audit trail of payment changes

A subscription change, a wallet top-up (by the payer or by the automatic job), a change of the top-up
settings, a switch of a wallet service, a subscription balance moved to the wallet and a sales request
SHALL each write one audit event; reads, price calculations and the checkout URLs SHALL write none.

#### Scenario: Top-up audited

- **WHEN** the payer tops the wallet up by 10 USD
- **THEN** the audit trail of `history-and-audit` shows the top-up with "10 USD"
