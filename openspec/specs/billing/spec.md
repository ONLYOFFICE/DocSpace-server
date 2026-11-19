# billing Specification

## Purpose

Defines the tariff and payment side of a portal: the tariff the portal is on and its state (trial, paid,
grace period, not paid), what the server computes from the due date, the gate that refuses most API calls
once the payment has lapsed, the payment settings and catalogue a client needs to sell a plan
(`settings/payment`, `portal/payment/prices`, `currencies`, `quotas`, `walletservices`), the purchase and
change of a subscription through the hosted checkout (`portal/payment/url`, `update`, `account`,
`checkoutsetupurl`, `customerinfo`), the sales request, the portal wallet of a hosted portal (balance,
top-up, automatic top-up, low balance, movements, the top-up history with its invoices, usage and
reports), the wallet services switched on per
portal and bought from the wallet (storage, backup, AI tools, AI search, administrators, Docs Connect), the
spending limits on the AI services, the AI prices and model restrictions, and who may see or change any of
it. The storage and count limits of a tariff and the refusals they cause are in `quotas` and only referred
to here; the letters a lapsed payment or a low balance produce are in `notifications`; the licence file of a
self-hosted installation (`settings/license`) is in `branding-and-licensing`. A self-hosted installation
has one tariff for all its portals and no billing service, so most of this specification is about hosted
portals.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`04_Portal/Payments`, `04_Portal/Quota`,
`02_Settings/Common`) and the TypeScript suite they were ported from, the unit tests of
`common/Tests/ASC.Core.Common.Tests` (prices, service limits, top-ups, invoice links), the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`), the product code of the
payment controller, the tariff service, the accounting client and the wallet job where the tests stop, the
runtime configuration, and the release notes (v1.1, v3.1, v3.2, v3.5, v3.6, v3.7, v4.0; v4.1 has none, so its
changes come from the code and the tests alone). The integration
host has no billing service, so every operation that needs one answers 403 there; a second suite runs them
on a hosted stack whose billing, accounting and DocsCloud services are stubs, and confirms their role
checks and much of their behaviour; the evidence per scenario, the differences
and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Tariff state of a portal

A portal SHALL be in one of four tariff states — `Trial`, `Paid`, `Delay` (the grace period), `NotPaid` —
derived from the due date on every read. A trial quota gives `Trial`, a due date one trial period (30 days
unless configured) after the portal was created or its trial reset, and no grace period. A due date in the
past gives `Delay` with `delayDueDate` = due date plus the grace days; one older than that gives `NotPaid`.
A lifetime quota never expires; a tariff without a due date is `Paid`.

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

On a hosted portal with a billing service the tariff SHALL be re-read from the payments of the billing
customer when the cache expires or `refresh=true` is passed: the paid quotas with their quantities, the
payer e-mail as `customerId` and the due date of the earliest payment. When billing stops reporting the
payment of a paid, non-free tariff (a refund), the due date SHALL become yesterday so the portal enters the
grace period; a portal with no payment and no free quota SHALL become NotPaid.

#### Scenario: Refund

- **WHEN** the billing service stops reporting the payment of a portal on a priced plan
- **THEN** the tariff's due date becomes yesterday and the portal is in the grace period on the next read

### Requirement: Single tariff of a self-hosted installation

A self-hosted installation SHALL have one tariff shared by all its portals, read from the licence file
(`branding-and-licensing`), never from a billing service. An Enterprise installation without an activated
licence SHALL get a 30-day trial tariff on first read; one without a licence path (open source) SHALL report
`openSource` true and is never treated as not paid. Every operation that needs the billing service SHALL
answer 403 "Tariff service is not configured" there.

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
`quotas` — a row per paid and per overdue quota (`id`, `quantity`, `wallet`, `additional`, `dueDate`,
`nextQuantity`, `nextQuota`). Others get `id` 0. `refresh=true` re-reads billing.

#### Scenario: Owner and DocSpace admin read the full tariff

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the system answers 200 with `state` Paid, `id` of 0 or more and a non-empty `quotas`

#### Scenario: Other members get the state only

- **WHEN** a Room admin, a User or a Guest calls `getPortalTariff`
- **THEN** the system answers 200 with `id` 0 and `state` Paid, and no `customerId` or `quotas`

### Requirement: Upcoming payments

`getUpcomingPayments` SHALL list, to a caller with the portal-settings right (403 otherwise, 401
anonymous), one entry per quota of the tariff charged next, with `id`, `name`, `title`, `unitOfMeasure`,
`quantity`, `wallet`, `dueDate`, `amount` (unit price times quantity) and `currency`. Overdue and unpriced
(trial, free) quotas SHALL be skipped; a quota with a switch scheduled for the next period SHALL be listed
as the quota it switches to. The call keeps answering while the payment has lapsed.

#### Scenario: Free portal

- **WHEN** the owner of a portal on the free plan calls `getUpcomingPayments`
- **THEN** the system answers 200 with an empty list

### Requirement: Payment gate while the portal is not paid

While a portal's tariff is NotPaid the server SHALL answer 402 without a reason to every call except the
ones allowed without payment: the portal record, `getPortalTariff`, `getUpcomingPayments`, `getPortalQuota`,
the extra licence information, every `portal/payment/*` operation, `getPaymentSettings`, authentication,
suspend and delete. It applies only where tariff settings are enabled: a hosted portal, or a self-hosted
installation with a licence path. During the grace period no gate applies.

#### Scenario: Lapsed portal lists files

- **WHEN** a member of a NotPaid hosted portal lists a folder
- **THEN** the system answers 402 and the body carries the status and a trace id but no error message

#### Scenario: Lapsed portal reads its tariff

- **WHEN** a member of a NotPaid hosted portal calls `getPortalTariff` or any `portal/payment/*` operation
- **THEN** the call is answered as usual, so the client can show the payment page

#### Scenario: Grace period

- **WHEN** a portal is in the grace period and the owner creates a room
- **THEN** the system answers 402 "Your pricing plan does not support this option" (the count limits of `quotas`), while every other call is answered

#### Scenario: No request header opens the gate

- **WHEN** a request to a NotPaid portal carries a `Payment-Info` header, whatever its value
- **THEN** the gate answers 402 as for any other request; only the operations allowed without payment pass

### Requirement: Payment settings

`getPaymentSettings` SHALL answer a caller with the portal-settings right (403 otherwise, 401 anonymous)
with `salesEmail`, `buyUrl` (both regional, from the installation's external resources), `standalone`,
`currentLicense` (`trial` of the current quota and `dueDate` of the tariff, time of day cut off) and
`max`, the largest quantity one purchase may ask for (999 unless configured). `feedbackAndSupportUrl` is
never filled. It keeps answering while the payment has lapsed.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getPaymentSettings`
- **THEN** the system answers 200 with a non-empty `salesEmail` and `buyUrl`, a `currentLicense` and `max` greater than 0

### Requirement: Price and currency catalogue

`getPortalPrices` SHALL return, to a caller with the portal-settings right (403 for a Room admin, a User
and a Guest, 401 anonymous), the unit price of every purchasable product keyed by product name in the
currency of the request, `0` where unpriced; `getPaymentCurrencies` SHALL return the default currency of
the portal region first (`isoCountryCode`, `isoCurrencySymbol`, `currencyNativeName`), then the request's
currency when it differs. Neither needs the billing service.

#### Scenario: Prices and currencies for the owner

- **WHEN** the owner calls `getPortalPrices` and `getPaymentCurrencies`
- **THEN** prices are a map with no negative value and the first currency is `US` / `$` / `US Dollar`

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls either operation, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

### Requirement: Prices are resolved per region and cached

The price shown for a plan or wallet service SHALL be the one in the currency of the request region, else
the one in the default region's currency, else the price stored with the quota. Prices SHALL be asked from
the payment services once per product list and kept for an hour (minutes in process memory); a failing
payment service costs only its own products their price for that request, and those are asked again next
time. A product the service does not price is remembered as unpriced too.

#### Scenario: One payment service down

- **WHEN** the accounting service fails while the billing service answers
- **THEN** the billing-priced products keep their prices, the accounting-priced ones come back unpriced, and the next request asks the accounting service again

### Requirement: Purchasable quotas and wallet services

`getPaymentQuotas` SHALL list the visible quotas, filtered by `wallet` and `additional` when given, each
with `id`, `title`, `price`, `nonProfit`, `free`, `trial`, `features`, `dueDate`; on a non-profit portal
`additional=false` returns only the current quota. `getWalletServices` SHALL list the services sold from
the wallet with price, unit and features; `getWalletService` one by `service` name (404 when not sold,
400 for an unknown name). All three need the portal-settings right.

#### Scenario: Catalogue on a host without a tariff catalogue

- **WHEN** the owner calls `getPaymentQuotas` with no filter, `wallet=true` or `additional=true` on an installation that seeds no quotas
- **THEN** the system answers 200 with a list that may be empty, every entry carrying `features`

#### Scenario: Six wallet services of a hosted portal

- **WHEN** the owner calls `getWalletServices` on a hosted portal
- **THEN** the list holds `disk-storage-1-hour`, `backup`, `ai-tools`, `docscloud-1-hour`, `docscloud-devpack-1-hour` and `ai-search`, each priced in USD, AI tools and AI search at 0

#### Scenario: Wallet service by name

- **WHEN** the owner calls `getWalletService` with `service` `Backup` or `Storage` on the integration host
- **THEN** the system answers 200 with the service name, a price above 0 and `features`
- **AND** a Room admin, a User or a Guest gets 403 and an anonymous caller 401

#### Scenario: AI service while AI is disabled for the portal

- **WHEN** the owner asks for `AITools` or `AISearch` while AI access is disabled for the portal
- **THEN** the system answers 404 "Service could not be found"

### Requirement: Current plan trimmed to the caller's type

`getQuotaPaymentInformation` SHALL answer every signed-in member except a Guest (403; anonymous 401) with
the quota the portal is on — `id`, `title`, `free`, `trial`, `nonProfit`, `features` with `used` — trimmed
by user type: a DocSpace admin also gets `price`, `dueDate` and every feature; a Room admin only `manager`,
`room`, `total_size`, `statistic`; a User only `total_size`; for both `price` is empty and `dueDate` absent.
The storage quota settings are the same for everyone.

#### Scenario: Fresh hosted portal

- **WHEN** the owner or a DocSpace admin calls `getQuotaPaymentInformation` on a portal that never bought a plan
- **THEN** the answer is the free plan (`id` -3, title "Startup", `free` true, `trial` false) with a `price` value of 0 and more features than a Room admin sees

#### Scenario: Room admin and User read the plan

- **WHEN** a Room admin or a User calls `getQuotaPaymentInformation`
- **THEN** the Room admin gets exactly `manager`, `room`, `total_size` and `statistic`, the User only `total_size` with its `used`, both without price and due date and with the same `usersQuota`, `roomsQuota`, `aiAgentsQuota` and `tenantCustomQuota` as the owner

### Requirement: Buying a plan through the hosted checkout

`getPaymentUrl` SHALL require a DocSpace admin (403 "Access denied" for a Room admin, a User and a Guest,
401 anonymous), the billing service, a body with `backUrl` and `successUrl` (absolute URLs of at most 255
characters) and `quantity` holding one product above 0 (400 "Invalid quantity"). The product SHALL be a
visible monthly plan (400 "Only monthly product can be purchased per transaction"). A customer already on
a paid tariff gets an empty result. The plan SHALL fit the portal (402).

#### Scenario: Owner asks for the checkout page

- **WHEN** the owner calls `getPaymentUrl` with `quantity` `{"admin": 1}` and the portal's own URL as `backUrl` and `successUrl`
- **THEN** the system answers 200 with a checkout URL carrying `qty` 1, `currency`, `language`, `refId` and `email`

#### Scenario: Quantity of zero

- **WHEN** the owner sends `quantity` `{"admin": 0}` or a negative number
- **THEN** the system answers 400 "Invalid quantity"

#### Scenario: Body validated before the configuration check

- **WHEN** `backUrl` or `successUrl` is not an absolute `http`, `https` or `ftp` URL or is longer than 255 characters, or `quantity` is empty or holds two products
- **THEN** the system answers 400 naming the field, even on an installation without a billing service

### Requirement: Changing the subscription quantity

`updatePayment` SHALL require the billing service, a billing customer (404) and the payer (403 "Access
denied" for everyone else, the owner and other DocSpace admins included; 401 anonymous), and a body with
one non-wallet product. On a priced plan the product SHALL be that plan (400 "Invalid product"); the
quantity already in effect is refused (400 "Invalid quantity"); the new one SHALL fit the portal (402).
The change is charged to the payment method on file, audited, and answers `true`.

#### Scenario: DocSpace admin who is not the payer

- **WHEN** the owner is the payer and a DocSpace admin calls `updatePayment`
- **THEN** the system answers 403 "Access denied"

#### Scenario: Provider declines or does not answer

- **WHEN** the payment provider declines the new quantity, or the call to it fails or times out
- **THEN** `updatePayment` answers 200 `false`

### Requirement: Billing account page and payment method

`getPaymentAccount` SHALL answer the payer or the owner (403 otherwise) with the portal-relative address
of the billing account handler, `backUrl` appended, or nothing without a customer.
`getCheckoutSetupUrl` SHALL give a DocSpace admin — the payer once a customer with an e-mail exists — the
hosted page that attaches a payment method, or nothing when one is set. `getCustomerInfo` SHALL show a
DocSpace admin the customer (`email`, `paymentMethodStatus`, `isDelayedPaymentMethod`, `payer`).

#### Scenario: Owner reads the account link

- **WHEN** the owner of a portal with a billing customer calls `getPaymentAccount` with a `backUrl`
- **THEN** the system answers 200 with a string containing `payment.ashx` and `backUrl`
- **AND** a DocSpace admin who is not the payer, a Room admin, a User and a Guest get 403 "Access denied"

#### Scenario: Checkout setup page

- **WHEN** the owner calls `getCheckoutSetupUrl` with `backUrl` and `successUrl` (both required absolute URLs of at most 255 characters)
- **THEN** the URL carries `currency`, `language`, `refId`, `email`, `successUrl` and `cancelUrl`
- **AND** a Room admin, a User and a Guest get 403, an anonymous caller 401

#### Scenario: Customer with the owner as payer

- **WHEN** the owner or a DocSpace admin calls `getCustomerInfo` on a portal that bought a plan
- **THEN** `paymentMethodStatus` is 1 (set) and `payer` carries the owner's `id` and `displayName`

### Requirement: Who is the payer

The payer of a portal SHALL be the portal account whose e-mail is the e-mail of the billing customer.
Operations that charge the payment method on file or change what is charged — `updatePayment`,
`topUpDeposit`, `setTenantWalletSettings`, `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet` —
SHALL be refused to every other caller with 403 "Access denied", the owner included. Reading the wallet
and catalogue, switching services and buying from the wallet SHALL be open to any DocSpace admin.

#### Scenario: DocSpace admin tops up the wallet of a portal the owner pays for

- **WHEN** a DocSpace admin who is not the payer calls `topUpDeposit`
- **THEN** the system answers 403 "Access denied"

#### Scenario: DocSpace admin switches a service

- **WHEN** a DocSpace admin who is not the payer calls `changeTenantWalletServiceState`
- **THEN** the system answers 200 and the service is switched

#### Scenario: Payer below DocSpace admin

- **WHEN** the payer is a Room admin or a User
- **THEN** `updatePayment`, `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet`, `getPaymentAccount`, `topUpDeposit` and `setTenantWalletSettings` let the payer through
- **AND** `getCheckoutSetupUrl` answers 403 "Access denied", since it also asks for a DocSpace admin

#### Scenario: Owner who does not pay

- **WHEN** the billing customer's e-mail belongs to nobody of the portal and the owner calls a payer operation
- **THEN** the system answers 403 "Access denied", except `getPaymentAccount`, which the owner may always call

### Requirement: Contacting the sales team

`sendPaymentRequest` SHALL require a DocSpace admin (403 "Access denied" for a Room admin, a User and a
Guest, 401 anonymous) and a body with `userName` (at most 255 characters), `email` (a well-formed address of
at most 64) and `message` (at most 255), none empty: 400 with "Incorrect firstname or lastname", "Incorrect
email" or "Message text is empty" for an empty field and the field name for an overlong one. It SHALL mail
the vendor's sales address, write an audit event and answer 200.

#### Scenario: Owner and DocSpace admin send a request

- **WHEN** the owner or a DocSpace admin posts a name, an e-mail and a message
- **THEN** the system answers 200

#### Scenario: Field too long or empty

- **WHEN** `userName` or `message` has 256 characters, `email` has 65, or any of the three is empty
- **THEN** the system answers 400 naming the field or the empty-field message

### Requirement: Wallet balance

`getCustomerBalance` SHALL answer a DocSpace admin (403 otherwise and without a billing service, 401
anonymous) with the wallet as the accounting service holds it: `accountNumber`, `subAccountNumber`,
`accountCurrency`, one `subAccounts` entry per currency with its `amount`, and `lastCredit`; a portal that
was never a customer gets an empty result. Every read asks accounting; only a failed read is remembered.
An AI charge SHALL reduce the balance by the debit of its movement.

#### Scenario: Funded wallet

- **WHEN** the owner or a DocSpace admin of a funded portal calls `getCustomerBalance`
- **THEN** `accountCurrency` is USD, the USD sub-account has an amount above 0 and `lastCredit` has a date, currency and amount
- **AND** a Room admin, a User and a Guest get 403 "Access denied", an anonymous caller 401

#### Scenario: AI charge taken off the wallet

- **WHEN** an AI chat reply is produced on a portal with AI tools on
- **THEN** the balance after the charge is the balance before minus the debit of the `Chat` movement

#### Scenario: Portal that is no customer

- **WHEN** a DocSpace admin calls `getCustomerBalance` on a portal that never became a billing customer
- **THEN** the system answers with an empty result without asking the accounting service

#### Scenario: Failed read

- **WHEN** the accounting service cannot be reached or does not know the portal
- **THEN** the answer is an empty result, and for ten minutes later reads answer the same without asking again unless they pass `refresh=true`

### Requirement: Bonus balance

`getCustomerGoodwill` SHALL return to a DocSpace admin (403 otherwise and without billing, 401 anonymous) the
portal's goodwill account, read from the accounting service on each call: the account with one sub-account
per currency, and `lots`, the bonus credits not spent in full, in spending order, each with
`operationId`, `amount`, `remaining`, `currency`, `date` and `expiresAt` with the portal offset. A portal without a customer,
or an unreadable accounting service, SHALL get an empty result.

#### Scenario: Customer with bonuses

- **WHEN** a DocSpace admin reads the bonus balance of a customer with two bonus credits left
- **THEN** the answer holds the goodwill account with its sub-accounts and both credits with their remaining amounts and expiries

#### Scenario: Portal that is not a customer

- **WHEN** the portal has no billing customer
- **THEN** the system answers 200 with an empty result without asking the accounting service

#### Scenario: Caller who is not an administrator

- **WHEN** a room admin, a user or a guest calls `getCustomerGoodwill`
- **THEN** the system answers 403

### Requirement: Redeeming a promo code

`applyPromoCode` SHALL let a DocSpace admin (403 otherwise and without billing, 401 anonymous) redeem
`promoCode`, 1 to 100 characters after surrounding spaces are trimmed (400), passing it to the billing
service with the caller as initiator. It SHALL answer `status` `Completed` or `Pending`, `amount`, `currency`
in upper case and `expiresAt` (null when the bonus never expires). A code the billing service refuses SHALL
answer 400 with its reason code and credit nothing.

#### Scenario: Bonus credited at once

- **WHEN** a DocSpace admin redeems `K7QX-M42P-9TRB` and the billing service credits 10 USD that expire at 2027-02-07T12:00:00Z
- **THEN** the system answers 200 with `status` `Completed`, `amount` 10, `currency` `USD` and that `expiresAt`
- **AND** the billing service received the code with the caller's e-mail as initiator

#### Scenario: Code with surrounding spaces

- **WHEN** a DocSpace admin redeems the code with two spaces before and after it
- **THEN** the billing service receives the code without them

#### Scenario: Currency in lower case

- **WHEN** the billing service answers the currency as `usd`
- **THEN** the system answers `currency` `USD`

#### Scenario: Bonus credited later

- **WHEN** the billing service accepts the code but credits the bonus later, without an expiry
- **THEN** the system answers 200 with `status` `Pending` and `expiresAt` null

#### Scenario: Code refused

- **WHEN** the billing service refuses the code as `promo_not_found` or `promo_already_used`
- **THEN** the system answers 400 with the reason code, such as `promo_not_found`, and no bonus is credited

#### Scenario: Billing service fails or does not know the portal

- **WHEN** the billing service answers `promo_failed` in the shape of a refusal, or does not know the portal
- **THEN** the system answers 402, not 400

#### Scenario: Empty code

- **WHEN** `promoCode` is empty or only spaces
- **THEN** the system answers 400 without asking the billing service

#### Scenario: Longest code

- **WHEN** `promoCode` has 100 characters
- **THEN** it is passed to the billing service

#### Scenario: Code too long

- **WHEN** `promoCode` has 101 characters
- **THEN** the system answers 400 without asking the billing service

#### Scenario: Caller who is not an administrator

- **WHEN** a room admin, a user or a guest calls `applyPromoCode`
- **THEN** the system answers 403 and the owner and a DocSpace admin who is not the owner are let through

### Requirement: Redeeming a promo code is audited and announced

Every promo code the billing service accepts, in an answer the server can read, SHALL write one audit event
naming the amount and the currency, never the code itself. A bonus the answer reports `Completed` SHALL be
pushed to the portal's clients as a wallet change, like a top-up; a pending one SHALL push nothing. A failure
to push SHALL NOT fail the redemption.

#### Scenario: Audit record of a redeemed code

- **WHEN** a DocSpace admin redeems a code and the billing service answers 25 `usd`, `Completed` or `Pending`
- **THEN** the audit trail of `history-and-audit` shows one "Promo code applied: 25 USD" and not the code

#### Scenario: Refused code

- **WHEN** the billing service refuses the code
- **THEN** no audit event is written

#### Scenario: Pending bonus

- **WHEN** the billing service answers `Pending`
- **THEN** the audit event is written and no wallet change is pushed

### Requirement: Wallet top-up

`topUpDeposit` SHALL require the billing service, a customer (404) and the payer (403), `currency` among
the accounting currencies (400 "Unsupported currency"), a whole `amount` from 1 to
999999 (400), and a payment method set on the customer (403 "Customer payment method is not set"). It
charges that method, credits the wallet, writes an audit event and answers `true`; with an immediate
payment method it also pushes the balance to the clients and re-arms the low-balance letter.

#### Scenario: Top-up pushed to the clients

- **WHEN** the payer tops up with a payment method that settles at once
- **THEN** the system answers `true` and the portal's clients receive the top-up event with `auto` false

#### Scenario: Delayed payment method

- **WHEN** the payer tops up with a payment method that settles later
- **THEN** the system answers `true`, no balance event is sent and the low-balance warning is not re-armed

#### Scenario: Repeated request

- **WHEN** the payer sends the same top-up twice
- **THEN** the wallet is charged twice; above ten requests a minute the caller gets 429

#### Scenario: Charge declined

- **WHEN** billing declines the charge of the payment method
- **THEN** the system answers `false`, does not try again and does not wait for the wallet to change

#### Scenario: Outcome of the charge unknown

- **WHEN** the billing service fails or its answer to the charge cannot be read
- **THEN** `topUpDeposit` answers `false` as for a decline, although the charge may have been applied
- **AND** a top-up the server starts itself to cover a purchase stops there instead of charging again

### Requirement: Automatic top-up settings

`getTenantWalletSettings` SHALL answer a DocSpace admin (403 otherwise, 401 anonymous) with `enabled`,
`minBalance`, `upToBalance`, `currency`, `lowBalanceThreshold`, `lowBalanceNotified`, `lastModified`,
defaults when never set, without a billing service. `setTenantWalletSettings` SHALL require the payer (403)
and a balance (404 "Balance could not be found"), take `settings` with `enabled`, `minBalance` 5–1000,
`upToBalance` 6–5000 and above `minBalance` (400 naming `UpToBalance`), `currency`.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getTenantWalletSettings`
- **THEN** the system answers 200 with a `response` body, and after the payer stored `enabled` true, 100 and 1000 it reports those values with a `lastModified`

#### Scenario: Bounds rejected before the payer check

- **WHEN** the owner sends `minBalance` 4, or `minBalance` 100 with `upToBalance` 50 or 100
- **THEN** the system answers 400, naming `UpToBalance` when the upper bound is not above the lower one

#### Scenario: Valid bounds on a host without billing

- **WHEN** the owner sends `minBalance` 5 and `upToBalance` 6 on an installation without a billing service
- **THEN** the body passes validation and the system answers 403 from the configuration check

#### Scenario: Enabling re-arms the warning

- **WHEN** the payer stores `enabled` true
- **THEN** the stored low-balance warning state is reset so the next dip is reported again
- **AND** a save with `enabled` false keeps that state and resets `lowBalanceThreshold` to the default instead

### Requirement: Automatic top-up and low-balance watch

A job SHALL visit every portal with stored wallet settings every three minutes (configurable). With
automatic top-up on and the balance below `minBalance` it SHALL charge the payment method
`upToBalance` minus the balance, audit the top-up and push the balance with `auto` true; a customer
without a payment method, or with a delayed one, SHALL have automatic top-up switched off and get a letter.
With it off, a balance below the threshold (1 by default) SHALL send the low-balance letter once.

#### Scenario: Balance below the trigger

- **WHEN** automatic top-up is on with `minBalance` 100 and `upToBalance` 1000 and the balance is 80
- **THEN** the job charges 920 and the clients receive the top-up event with `auto` true

#### Scenario: Balance under the low-balance threshold

- **WHEN** automatic top-up is off and the balance falls under 1
- **THEN** the payer and the owner get the low-balance letter of `notifications` once, and not again until the balance recovered to 2 and fell again

#### Scenario: Payer disabled in the portal

- **WHEN** the billing e-mail belongs to a disabled portal user, or to nobody on the portal
- **THEN** the job and the subscription renewal run as the portal owner, the low-balance warning is still raised, and the wallet letters go to the owner alone

### Requirement: Wallet services switched on per portal

`getTenantWalletServiceSettings` SHALL answer a DocSpace admin (403 otherwise and without billing) with
`enabledServices` and `lastModified`. `changeTenantWalletServiceState` SHALL require the portal-settings
right, a DocSpace admin and a customer (404), take `service` (`Storage`, `Backup`, `AITools`, `Admin`,
`DocsCloud`, `DocsCloudDevPack`, `AISearch`; 400 otherwise) and `enabled`, refuse an AI service while AI is
disabled and `AISearch` while `AITools` is off (403), and answer the set.

#### Scenario: Owner switches AI tools, backup and storage on and off

- **WHEN** the owner or a DocSpace admin posts `service` `AITools`, `Backup` or `Storage` with `enabled` true, then false
- **THEN** each answer is 200 and `enabledServices` contains the service after the first call and not after the second

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `changeTenantWalletServiceState`, or an anonymous caller does
- **THEN** the system answers 403 "Access denied" (401 for anonymous)

#### Scenario: AI search needs AI tools

- **WHEN** `AITools` is off and the owner switches `AISearch` on
- **THEN** the system answers 403 "AI Tools service must be enabled before Search"

#### Scenario: AI tools switched off takes AI search with it

- **WHEN** `AISearch` is on and the owner switches `AITools` off
- **THEN** `enabledServices` holds neither, the change is audited and the clients receive the AI configuration event

### Requirement: What the wallet pays for

An AI chat reply, a vectorization and a web search SHALL be charged to the wallet as movements of the
`ai-tools` and `ai-search` services, each naming the feature (`Chat`, `Vectorization`), the model and the
agent it ran in (`agentId`, `agentTitle`, empty outside an agent); a backup beyond the free ones of the plan
to `backup`; extra storage hourly to `disk-storage-1-hour`. While `AITools` is off an AI request SHALL be
refused with 402 and nothing charged.

#### Scenario: Chat inside an agent

- **WHEN** a chat is sent inside an AI agent with AI tools on
- **THEN** a `Chat` movement for `ai-tools` appears in `getCustomerOperations` with the agent's id and title, and `getCustomerServiceUsage` sums it under `ai-tools`

#### Scenario: AI tools switched off

- **WHEN** the owner switches `AITools` off and sends a chat message
- **THEN** the send answers 402 and no `ai-tools` movement is added

### Requirement: Automatic renewal of a wallet subscription

A wallet subscription SHALL be renewed shortly before its due date; for the main plan the wallet
is topped up first for what it and the bonus lack, unless its payment method is delayed. A renewal a stopped
service left unfinished SHALL be resumed by asking billing first and SHALL NOT be paid again once its payment
was requested or billing reports it extended. A renewal that fails SHALL send the failure letter to the payer and the
owner, unless it is no longer wanted or its due date moved.

#### Scenario: Wallet covers the next period

- **WHEN** the due date of a wallet subscription comes near and the wallet and the bonus hold its price
- **THEN** the next period is paid for the quantity scheduled for it, raised to the seats or the storage in use, and the wallet is not topped up

#### Scenario: Switch to another plan scheduled

- **WHEN** a switch to another wallet plan is scheduled for the due date
- **THEN** that plan is bought for the next period instead of renewing the current one

#### Scenario: Wallet short of the price

- **WHEN** the due date of the main plan comes near, the wallet and the bonus hold less than the price and the payment method is not a delayed one
- **THEN** the wallet is topped up for the shortfall and the next period is then paid from it; while other spending keeps them short, it is topped up again, five deposits at most

#### Scenario: Top-up cut short

- **WHEN** the top-up of a renewal is still running eight minutes after the renewal was started, or the service stops during it
- **THEN** a deposit already sent is let finish, no further deposit is begun, the payment is not requested, and the renewal is resumed later by asking billing first

#### Scenario: Wallet short of the price with a delayed payment method

- **WHEN** the due date of the main plan comes near, the wallet and the bonus hold less than the price and the payment method is a delayed one
- **THEN** the wallet is not topped up, nothing is paid and the failure letter is sent

#### Scenario: Additional wallet service

- **WHEN** the due date of an additional wallet service, not the main plan, comes near
- **THEN** its next period is requested without topping the wallet up first

#### Scenario: Additional wallet service while the tariff is overdue

- **WHEN** the due date of an additional wallet service comes near while the portal's tariff is overdue
- **THEN** it is not renewed and no failure letter is sent

#### Scenario: Interrupted and already extended

- **WHEN** the service stopped during a renewal and billing reports the subscription extended
- **THEN** nothing is paid again and the renewal is closed

#### Scenario: Interrupted after the payment was requested

- **WHEN** the service stopped after requesting the payment, whether or not the request left, billing still reports the subscription not extended ten minutes after the request, and the renewal is still wanted with its due date unchanged
- **THEN** the failure letter is sent and the payment is not requested a second time

#### Scenario: Interrupted and billing gives no answer

- **WHEN** the service stopped during a renewal and billing does not answer about the subscription
- **THEN** the renewal is asked about again on the following runs, nothing is paid and no letter is sent meanwhile
- **AND** thirty minutes after the payment was requested, or after the renewal was last started or queued when it was not, the failure letter is sent, unless the renewal is no longer wanted or its due date moved

#### Scenario: Interrupted three times

- **WHEN** a renewal that never got to request its payment was interrupted on each of three starts, billing does not report it extended, and it is still wanted with its due date unchanged
- **THEN** its payment is not attempted a fourth time and the failure letter is sent

#### Scenario: Renewal no longer wanted

- **WHEN** the automatic renewal was switched off while an interrupted renewal waited
- **THEN** the subscription is not renewed and no failure letter is sent

#### Scenario: Due date moved meanwhile

- **WHEN** the due date of the subscription changed while an interrupted renewal waited, and billing does not report it extended
- **THEN** that renewal is closed without a payment and without the failure letter; the new due date is renewed when it comes near

#### Scenario: Run interval and renewal advance not set

- **WHEN** the installation sets neither how often due subscriptions are looked for nor how long before the due date one is renewed
- **THEN** both are one minute

#### Scenario: Run interval and renewal advance set empty

- **WHEN** the run interval or the renewal advance is set to an empty value
- **THEN** it is taken as not set, without a warning

#### Scenario: Run interval below its minimum

- **WHEN** the run interval is set to zero, to a negative value or to less than one second
- **THEN** it is kept at one second and a warning is logged

#### Scenario: Run interval above its maximum

- **WHEN** the run interval is set to more than one hour
- **THEN** it is kept at one hour and a warning is logged

#### Scenario: Renewal advance shorter than the run interval

- **WHEN** the renewal advance is set to zero, to a negative value or to less than the run interval
- **THEN** it is kept at the run interval and a warning is logged

#### Scenario: Renewal advance above its maximum

- **WHEN** the renewal advance is set to more than one day
- **THEN** it is kept at one day and a warning is logged

#### Scenario: Interval value that cannot be read

- **WHEN** the run interval or the renewal advance is set to text that is not an interval
- **THEN** the default is used - one minute for the interval, the interval for the advance - and a warning is logged

### Requirement: Spending limits on the AI services

A portal with a billing customer SHALL carry spending limits on the AI services (AI tools and AI search
together) in three scopes: `Total` for the portal as a whole, `Global` for every user separately, `Custom`
for one user, who then follows it instead of the global one; the portal limit is checked on top. A limit
has a money threshold (`amountValue`, 0 to 10000) and/or a count threshold (`quantityValue`), a period
(`Day`, `Week`, `Month`, `Lifetime`) and `enabled`. A refused charge answers 402.

#### Scenario: Charge above the limit

- **WHEN** a user's AI request would take the spending of the period above the limit in force
- **THEN** the AI request answers 402 with the accounting service's reason and nothing is charged

#### Scenario: Personal limit wins over the global one

- **WHEN** a user has an enabled personal limit and a global limit exists
- **THEN** the personal limit is the one in force for that user; switched off or deleted, the global one applies again

### Requirement: Managing the AI spending limits

The operations under `portal/payment/servicelimit` SHALL need the billing service (403), a DocSpace admin
and a billing customer (404; a write also when the accounting service does not know the portal). Writes
also need the portal-settings right: `createServiceLimit` (`scope` required; 409 for a duplicate),
`updateServiceLimit`, `setParticipantServiceLimits` for up to 1000 `userIds`,
`disableParticipantServiceLimits`, `deleteServiceLimit`.

#### Scenario: Reading the limits

- **WHEN** a DocSpace admin calls `getServiceLimit`, `getTotalServiceLimit`, `getGlobalServiceLimit` or `getParticipantServiceLimits`
- **THEN** `getServiceLimit` answers the limit without spending, 404 for an `id` the portal has no limit with and 400 for an `id` below 1
- **AND** `getTotalServiceLimit` (with the spending of the period) and `getGlobalServiceLimit` answer 200 with a null `response` when that limit is not set
- **AND** `getParticipantServiceLimits` pages (`offset` 0, `limit` 25 by default, 400 out of range) the enabled limit in force for each user with its spending, a global one repeated for every user it applies to

#### Scenario: Changing, switching off and deleting limits

- **WHEN** a DocSpace admin calls `updateServiceLimit` with an `id` and some thresholds or `enabled`
- **THEN** only the sent fields change, an omitted threshold is kept, and an `id` the portal has no limit with answers 404
- **AND** `setParticipantServiceLimits` replaces each listed user's personal limit as a whole, switches it on and answers the count; a user listed twice gets one limit
- **AND** `disableParticipantServiceLimits` switches every personal limit off and answers the count, 0 on a repeat, leaving the portal and global limits as they are
- **AND** `deleteServiceLimit` answers 200 with no body and 404 on a repeat

#### Scenario: Create a personal limit

- **WHEN** a DocSpace admin posts scope `Custom` with a `userId`, `amountValue` 50 and period `Day`
- **THEN** the system answers 200 with the limit, writes "Service spending limit created" to the audit trail and tells the user and the administrators to re-read the limits

#### Scenario: Invalid create requests

- **WHEN** neither `amountValue` nor `quantityValue` is given, scope `Custom` lacks `userId`, another scope carries one, or `userId` is a system or unknown account
- **THEN** the system answers 400 for the body faults and 404 for the account

#### Scenario: Scope left out

- **WHEN** the body of the create call has `amountValue` and `period` but no `scope`
- **THEN** the system answers 400 naming `Scope` before the configuration check, instead of treating the limit as a portal-wide one

#### Scenario: Wallet never topped up

- **WHEN** a DocSpace admin of a billing customer whose wallet the accounting service does not know yet creates a limit, sets personal limits or disables them
- **THEN** the system answers 404 "Customer could not be found", as for a portal without billing, while the reads answer as if no limit existed

#### Scenario: Limit of another portal

- **WHEN** a DocSpace admin reads, updates or deletes a limit `id` that belongs to another portal or to another service group
- **THEN** the system answers 404 and nothing is sent to the accounting service for it

#### Scenario: Integration host without billing

- **WHEN** any caller uses a limit operation on an installation without a billing service
- **THEN** the system answers 403 for a signed-in caller and 401 for an anonymous one

### Requirement: A user's AI spending limit in the profile

The full profile of a user (`getUserById`, the people lists) SHALL carry `aiServicesLimit` — the personal
limit or else the global one, with `id`, `scope`, `amountValue`, `currency`, `period`, `amountConsumed`,
`periodEnd` — for a DocSpace admin and for the user themself; nothing for others, without a billing
service, when the portal is not a customer, or when no limit applies. It comes from a cache refreshed every
few minutes and kept while accounting is down. User socket events carry none.

#### Scenario: Global limit for a user never counted

- **WHEN** a global limit exists and a user has never used the AI services
- **THEN** the profile shows the global limit with `amountConsumed` 0 and `periodEnd` the end of the current calendar period (none for a lifetime limit)

#### Scenario: Accounting service unreachable

- **WHEN** the accounting service does not answer within a second and no limits are cached yet
- **THEN** the people list is served with `aiServicesLimit` empty rather than failing or waiting

### Requirement: Wallet movements, usage and reports

`getCustomerOperations` SHALL page the wallet movements for a DocSpace admin (`offset` 0 and `limit` 25 by
default, echoed with `totalQuantity`, `totalPage`, `currentPage`), filtered by `serviceName` (404 for a
name not sold), `startDate`/`endDate` in the portal time zone, `participantName`, `credit`, `debit`, `type`,
`status`, sorted by `orderBy`/`orderType`. `getCustomerServiceUsage` SHALL sum them per service, also
filtered by `metadata` pairs (a pair with an empty or blank value is ignored),
`getCustomerMonthlyUsage` per month. Each has a report trio.

#### Scenario: Owner pages the movements

- **WHEN** the owner or a DocSpace admin calls `getCustomerOperations` without paging
- **THEN** `offset` is 0, `limit` 25, `totalQuantity` 0 or more and `collection` present; `orderBy` date descending returns the newest first

#### Scenario: Unknown participant

- **WHEN** `participantName` names nobody
- **THEN** the system answers 200 with an empty `collection`

#### Scenario: AI paid with the portal's own keys

- **WHEN** `serviceName` is `ai-tools-byok` or `ai-search-byok`
- **THEN** the name is accepted when its AI service is sold, and each movement reads like its AI service with `byok` true and the AI `provider` that ran it
- **AND** the operations and usage report files mark those rows "(BYOK)"

#### Scenario: BYOK totals merged

- **WHEN** `getCustomerServiceUsage` is called with `mergeByok=true`
- **THEN** each BYOK total is added into the total of its AI service, `byok` is false on every row, and the merged list is paged by `offset` and `limit`

#### Scenario: Period without a start

- **WHEN** `getCustomerOperations`, `getCustomerServiceUsage`, `getCustomerMonthlyUsage` or `getCustomerDeposits` is called without `startDate`
- **THEN** the period starts at the portal's creation moment taken as UTC, not shifted by the portal time zone, and ends now

#### Scenario: Usage per service

- **WHEN** the owner calls `getCustomerServiceUsage`, or a DocSpace admin with `serviceName` `ai-search`
- **THEN** every row carries `service`, `operationCount` and `totalAmount` of 0 or more, and the filtered answer holds `ai-search` rows only

#### Scenario: Operations report

- **WHEN** the owner or a DocSpace admin calls `createCustomerOperationsReport` for `ai-tools`, `backup` or `disk-storage-1-hour` and polls `getCustomerOperationsReport`
- **THEN** the task id starts with `DocumentBuilderTask_`, `isCompleted` turns true with `percentage` 100, an empty `error`, a `resultFileId` and a `resultFileUrl` of a document in My documents
- **AND** a Room admin, a User and a Guest get 403 "Access denied" on create, get and terminate, an anonymous caller 401

#### Scenario: Usage reports and stopping a report

- **WHEN** a DocSpace admin calls `createCustomerServiceUsageReport` (the filters of `getCustomerServiceUsage` but `mergeByok`) or `createCustomerMonthlyUsageReport` (only `startDate`, `endDate`) and polls `getCustomerServiceUsageReport` or `getCustomerMonthlyUsageReport`
- **THEN** each kind builds its own `xlsx` file in the caller's My documents, one job of a kind per user; the status read answers empty when the caller has no job of that kind
- **AND** `terminateCustomerOperationsReport`, `terminateCustomerServiceUsageReport` and `terminateCustomerMonthlyUsageReport` request a stop of the caller's job of that kind and remove it, answer 200 with no body also without a job, and leave a finished file in place

### Requirement: Bonus part of wallet movements and usage

`getCustomerOperations` SHALL give each movement `sources`, its parts per sub-account with `isBonus` and an
amount of zero or more, null when the accounting service gave no split; `getCustomerServiceUsage` SHALL do the
same per total, adding the parts up per sub-account when `mergeByok` merges totals. Bonus credits and
expirations SHALL be the movement kinds `GoodwillCredit` and `GoodwillExpiration`, and the `type` filter SHALL
reach the accounting service.

#### Scenario: Expired bonus and a charge paid partly by the bonus

- **WHEN** the accounting service lists the expired rest of a bonus and a charge paid partly from the bonus, both with negative parts
- **THEN** the expiration is `GoodwillExpiration` described "Bonus expiration" with one bonus part, and the charge has a bonus and a wallet part, all of them zero or more

#### Scenario: Usage total with a split

- **WHEN** a usage total comes with its parts per sub-account
- **THEN** `sources` lists them with the bonus part marked by `isBonus`, and a total without a split has `sources` null

#### Scenario: BYOK totals merged with their splits

- **WHEN** `getCustomerServiceUsage` is called with `mergeByok=true` and both totals have splits
- **THEN** the merged total adds the parts up per sub-account, and it has no split when one of them had none

#### Scenario: Filter by kind

- **WHEN** `getCustomerOperations` is called with `type` `GoodwillCredit`
- **THEN** the accounting service is asked for that kind only

#### Scenario: Bonus columns in the report files

- **WHEN** the operations report or the service usage report is built
- **THEN** the operations file has "Credit (bonuses)" and "Debit (bonuses)" columns and the usage file a "Debit (bonuses)" column, each the bonus part of the amount next to it

### Requirement: Wallet top-ups and their invoices

`getCustomerDeposits` SHALL page the payments into the wallet for a DocSpace admin (403 otherwise and
without billing, 401 anonymous), filtered by `startDate`/`endDate`, `status`, `initiator` and `metadata` pairs
(all of them must match; a pair with an empty or blank value is ignored), with `offset` of 0 or more
and `limit` 1–100 (400 otherwise) echoed with the totals; each entry has `id`, `date`, `amount`, `currency`,
`status`, `initiator` (null when accounting does not report it or reports an unknown kind), `hasInvoice`.
A portal without a customer or a readable wallet gets an empty result.
`getDepositInvoice` SHALL answer the invoice page address of one top-up by `id`.

#### Scenario: Portal that never topped up

- **WHEN** the owner of a portal that is a billing customer but never paid into the wallet calls `getCustomerDeposits`
- **THEN** the system answers 200 with an empty result

#### Scenario: Invoice of a card payment

- **WHEN** a DocSpace admin calls `getDepositInvoice` with the `id` of a top-up listed with `hasInvoice` true
- **THEN** the system answers 200 with the absolute address of the billing provider's invoice page, asked for anew on every call

#### Scenario: Top-up without an invoice

- **WHEN** the `id` names a top-up listed with `hasInvoice` false, a top-up of another portal, or the portal is not a customer
- **THEN** the system answers 404; when the accounting or billing service fails or does not answer, 402, so the client can retry

#### Scenario: Paging window of the top-up list

- **WHEN** `offset` is negative or `limit` is 0 or above 100
- **THEN** the system answers 400 naming the field, even on an installation without a billing service
- **AND** a period in which nothing was paid answers the requested `offset` and `limit` with an empty `collection`

### Requirement: Accounting price list of a wallet service

`getAccountingServicePrices` SHALL return to a DocSpace admin (403 otherwise and without the billing and
accounting services; 401 anonymous) the price records of one wallet service as the accounting service
keeps them — time unit, cost, resulting price, volume threshold, validity, status and discount category —
all records by default and the ones in force with `active=true`. `serviceName` is limited to 255
characters (400). The list is cached for all portals for up to an hour.

#### Scenario: Name length checked before the configuration check

- **WHEN** the owner asks for a service name of 256 characters, then of 255, on an installation without a billing service
- **THEN** the first answers 400 naming the length, the second 403 from the configuration check

### Requirement: Buying wallet services

`updateWalletPayment` SHALL require the billing service, a DocSpace admin and a customer, one wallet
product and `productQuantityType` `Set` or `Add` (400 "Invalid product quantity type"). `Add` buys
now from the wallet (404 if neither a sub-account nor a bonus; 402 when the plan payment is overdue and the
service is an add-on; minimums: storage 100, Dev Pack 10, administrators not fewer than the portal has; 400
"Invalid quantity"). `Set` writes the next period's quantity, zero cancelling it.

#### Scenario: Owner adds, lowers and cancels storage

- **WHEN** the owner or a DocSpace admin buys 100 storage units with `Add`, schedules a lower quantity with `Set`, then cancels it with an empty `Set`
- **THEN** every answer is 200 `true`
- **AND** a Room admin, a User and a Guest get 403 "Access denied", an anonymous caller 401

#### Scenario: Price before buying

- **WHEN** the owner or a DocSpace admin calls `calculateWalletPayment` with 100 storage units and `Add`
- **THEN** the answer carries `operationId`, `amount`, `currency` USD and `quantity` 100, and nothing is bought
- **AND** when the billing service fails the answer is an empty body; `updateWalletPayment` answers `false` only for a `Set` the portal cannot apply (it does not have the service, or the change is not saved)

#### Scenario: Negative or non-numeric quantity

- **WHEN** the quantity is negative, a string or special characters
- **THEN** the system answers 400 ("Invalid quantity", or the JSON conversion error for a non-number)

#### Scenario: Wallet never topped up

- **WHEN** a customer with a linked card has never topped up the wallet, so the wallet has no sub-account, and `calculateWalletPayment` is called with 100 storage units and `Add`
- **THEN** with a bonus of 5 USD the call goes on to the price, and without a bonus it answers 404 "Subaccount could not be found"

### Requirement: Moving the subscription balance to the wallet

`getSubscriptionBalanceInfo` SHALL show the payer (403) the unused balance of the subscription period
(402 when the plan is not paid, 404 without a subscription, 400 for a wallet plan).
`moveSubscriptionToWallet` SHALL, for the payer with a payment method set (403), take the administrators
product only (400 "Invalid product") in a count above 0 and not below the portal's administrators (400),
move the balance, pay price times count from the wallet (402 when short) and buy the administrators.

#### Scenario: Fewer administrators than the portal has

- **WHEN** the payer asks for fewer administrators than the portal already has
- **THEN** the system answers 400 "Invalid quantity" and nothing is moved

#### Scenario: Price of the administrators product

- **WHEN** the price of the product is 0, or the payer is not a DocSpace admin
- **THEN** the price is still read from the quota itself, so a zero price is valid and no top-up is asked for

#### Scenario: Portal without a subscription

- **WHEN** the payer of a portal marked paid without a provider subscription calls `getSubscriptionBalanceInfo`, or `moveSubscriptionToWallet` without a payment method set
- **THEN** the first answers 402 with an empty body and the second 403

#### Scenario: Bonus covers part of the price

- **WHEN** the administrators cost 40 USD, the moved balance leaves 0 in the wallet and the bonus holds 15 USD
- **THEN** the wallet is topped up by 25 USD and the administrators are bought

### Requirement: Paying from the wallet with the bonus counted

When `moveSubscriptionToWallet` or the administrators renewal pays from the wallet, the server SHALL top it up
only for what the wallet money and the bonus do not cover, both read again after each top-up. The bonus SHALL
be the rest of the credits in the wallet currency not ended at the read, capped by the sub-account (the
sub-account without `lots`); one unreadable before the first top-up counts as none, a later failed read keeps
the last. A delayed method or an add-on renewal gets no top-up.

#### Scenario: Bonus covers the price

- **WHEN** a renewal costs 40 USD, the wallet holds 0 and the bonus 40 USD
- **THEN** the wallet is not topped up and the renewal goes on

#### Scenario: Bonus covers part of the price

- **WHEN** a renewal costs 40 USD, the wallet holds 0 and the bonus 15 USD
- **THEN** the wallet is topped up by 25 USD

#### Scenario: Bonus spent during the top-up

- **WHEN** a payment costs 40 USD, the wallet holds 0, the bonus 15 USD, and a charge spends 10 USD of it while the wallet is topped up by 25 USD
- **THEN** the wallet is topped up again by 10 USD

#### Scenario: Bonus in another currency

- **WHEN** the administrators cost 40 USD, the wallet holds 0 and the bonus 40 EUR
- **THEN** the bonus is not counted and the wallet is topped up by 40 USD

#### Scenario: Bonus credit that has ended

- **WHEN** a payment costs 50 USD, the wallet holds 10 USD, and the only bonus credit, 15 USD, ended an hour ago but is still on the sub-account
- **THEN** the wallet is topped up by 40 USD

#### Scenario: Bonus that cannot be read

- **WHEN** a payment costs 50 USD, the wallet holds 10 USD, and the accounting service answers the bonus read with 400 or with a page that is not JSON
- **THEN** the wallet is topped up by 40 USD, as if there were no bonus

#### Scenario: Bonus unreadable after a top-up

- **WHEN** a payment costs 40 USD, the wallet holds 0, the bonus 15 USD, the wallet is topped up by 25 USD and the bonus then cannot be read
- **THEN** the 15 USD read before still count and no second top-up is made

#### Scenario: Delayed payment method

- **WHEN** the payment method is delayed, the payment costs 30 USD, the wallet holds 5 USD and the bonus 25 USD
- **THEN** no top-up is made and the payment goes on

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
chat, embedding, image and web search prices with `currency`. `getRestrictedAiModels` SHALL return
`models`, the barred chat models: empty to any member while billing or AI tools is off, else to a DocSpace
admin (402 without a billing customer). `setRestrictedAiModels` SHALL require the portal-settings right, a
DocSpace admin, a customer and AI tools on (403), and `models` with no null entry.

#### Scenario: Null entry rejected before the configuration check

- **WHEN** the owner sends `models` `[null]` or `["openai/gpt-4o", null]`
- **THEN** the system answers 400 naming `Models`

#### Scenario: Valid set on a host without billing

- **WHEN** the owner sends `["openai/gpt-4o"]` or `[]` on an installation without a billing service or AI gateway
- **THEN** the body passes validation and the system answers 403

#### Scenario: Restriction read back

- **WHEN** the owner or a DocSpace admin stores one model id and reads `getRestrictedAiModels`
- **THEN** `models` equals that one id, and the stored set is replaced as a whole on every write

#### Scenario: Model running on a key of the portal

- **WHEN** the portal has a BYOK key for a model and calls `getAiPrices`
- **THEN** that model is marked `byok` and priced by the key's provider, or by the built-in provider when the key's provider has no price

### Requirement: Unreadable answers of the payment services

An answer of the billing, accounting or DocsCloud service that cannot be read SHALL NOT be taken for a
refusal: the operation fails with 500, not 402, and no refusal path (an unpaid portal, a dropped schedule)
acts on it. A charge, a trial or a renewal whose answer cannot be read SHALL be an unknown outcome instead,
never retried as if refused. Error messages SHALL name the call, never the service address or portal key.

#### Scenario: Proxy page instead of the answer

- **WHEN** a payment service answers with something that is not its contract, such as an HTML page
- **THEN** the operation answers 500 and the message names the method and route template of the call only

#### Scenario: Renewal answer unreadable

- **WHEN** the answer to a subscription renewal cannot be read
- **THEN** the renewal is checked again on the following runs and its payment is not sent again; the failure letter is sent only if billing still reports the subscription not extended ten minutes later, or gives no answer for thirty minutes

### Requirement: Paid seats and tariff limits

The number of DocSpace admins and Room admins a portal may hold SHALL be the `manager` count of its tariff;
the checks are in `quotas` ("Count limits of the tariff"): the invitation link of a paid role answers 402
once the paid seats are used up, a bulk invitation over the limit is refused as a whole, and the same
checks answer 402 "Your pricing plan does not support this option" in the grace period and after. Buying
`Admin` units from the wallet SHALL raise the seats of a hosted portal.

#### Scenario: Paid seats used up

- **WHEN** the portal's paid-user limit is reached and the owner reads the invitation link of DocSpace admin or Room admin
- **THEN** the system answers 402

### Requirement: Payment changes are announced

When a re-read tariff differs from the cached one the server SHALL push the new storage, paid-user and
room limits to the portal's clients as quota feature values, and the free flag when it changed; a top-up
pushes the balance; a switched AI tools service pushes the AI configuration; a changed AI spending limit
sends `change-service-limits` (service group, limit id) to those who may read it — every user for a global
limit, the administrators and the user concerned otherwise.

#### Scenario: Plan bought

- **WHEN** the billing service reports a bought plan with a larger storage allowance
- **THEN** every connected client of the portal receives the new `total_size` and `manager` values

#### Scenario: Personal limits set in bulk

- **WHEN** a DocSpace admin sets a personal limit for a list of users
- **THEN** each of those users and every administrator receives the re-read event in their personal room

### Requirement: Audit trail of payment changes

A subscription change, a wallet top-up (manual or automatic), a change of the top-up
settings, a switch of a wallet service, a change of the restricted AI models, a subscription balance moved
to the wallet, a sales request, and the creation, update, bulk set, bulk disable and deletion of an AI
spending limit SHALL each write one audit event; reads, price calculations and the checkout URLs SHALL
write none. A renewal resumed after it was started SHALL write one when billing reports it extended.

#### Scenario: Top-up audited

- **WHEN** the payer tops the wallet up by 10 USD
- **THEN** the audit trail of `history-and-audit` shows the top-up with "10 USD"

#### Scenario: Renewal interrupted after its audit event

- **WHEN** the service stops after writing the audit event of an automatic renewal and before closing that renewal
- **THEN** the resumed renewal finds the subscription extended and writes the event again: two events for one renewal

#### Scenario: Personal limit audited with its user

- **WHEN** a personal AI spending limit is created, updated or deleted
- **THEN** the audit record names the service group, the scope and the user's name, and targets the user's id
