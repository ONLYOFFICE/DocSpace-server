# Spec Delta

## ADDED Requirements

### Requirement: Yearly administrators plan paid from the wallet

`updateWalletPayment` SHALL take the product `adminwalletyear`, the administrators plan paid from the wallet
with the `year` feature, under the rules of `adminwallet`, and `getPaymentQuotas` with `wallet=true` lists
it. The billing service decides the purchase and its period. With `Add`, the call SHALL refuse one
administrators plan while the tariff holds the other with its period not ended (400 "Quota is already set").

#### Scenario: Yearly plan bought from the wallet

- **WHEN** the owner calls `updateWalletPayment` with `Add` and `adminwalletyear` in a count not below the portal's administrators, and the tariff holds no wallet administrators plan
- **THEN** the system answers 200 `true`
- **AND** the billing service is asked for the yearly product in that count

#### Scenario: Fewer administrators than the portal has

- **WHEN** the owner calls `updateWalletPayment` with `Add` and `adminwalletyear` in a count below the portal's administrators, and the plan is not active
- **THEN** the system answers 400 "Invalid quantity" and nothing is bought

#### Scenario: The other administrators plan is in force

- **WHEN** the owner calls `updateWalletPayment` with `Add` for `adminwalletyear` while the tariff holds `adminwallet` with a due date in the future, or for `adminwallet` while it holds `adminwalletyear` with a due date in the future
- **THEN** the system answers 400 "Quota is already set" and nothing is paid

#### Scenario: Both plans asked for at the same moment

- **WHEN** two `updateWalletPayment` requests with `Add`, one for `adminwallet` and one for `adminwalletyear`, reach a portal without an administrators plan together, and the billing service lists a product as paid once it sells it
- **THEN** one answers 200 `true`, the other 400 "Quota is already set", and the billing service is asked for one product

#### Scenario: The other administrators plan has ended

- **WHEN** the due date of the other administrators plan in the tariff has passed and the owner calls `updateWalletPayment` with `Add` for `adminwalletyear` or `adminwallet`
- **THEN** the system answers 200 `true` and the billing service is asked for that product

#### Scenario: More administrators of the plan the portal holds

- **WHEN** the tariff holds `adminwalletyear` in force and the owner calls `updateWalletPayment` with `Add` and `adminwalletyear`
- **THEN** the system answers 200 `true` and the billing service is asked for the yearly product

#### Scenario: Next quantity of a plan the portal does not hold

- **WHEN** the tariff holds `adminwallet` and the owner calls `updateWalletPayment` with `Set` and `adminwalletyear`
- **THEN** the system answers 200 `false` and nothing is bought

#### Scenario: Yearly plan in the catalogue

- **WHEN** the owner calls `getPaymentQuotas` with `wallet=true` on a hosted portal
- **THEN** the list holds the yearly plan with the `year` feature, at the catalogue price of 220 when the accounting service gives no price for `adminyear`

#### Scenario: Seats of the yearly plan

- **WHEN** the tariff holds 3 units of `adminwalletyear` next to a storage add-on
- **THEN** the merged quota of the portal carries 3 paid seats (`manager`), the price of 660 and the `year` feature, as `Admin` units give the seats of "Paid seats and tariff limits"

### Requirement: An administrators plan is bought on the billing service's own answer

Before `updateWalletPayment` with `Add` buys `adminwallet` or `adminwalletyear`, it SHALL ask the billing
service for the portal's current payments and check the other plan on that answer. If the billing service
does not answer, the call answers 402 and buys nothing. If another such purchase holds the portal's lock
until the wait for it ends, the call answers 409 and buys nothing.

#### Scenario: Billing service does not list the payments

- **WHEN** the owner calls `updateWalletPayment` with `Add` for `adminwallet` or `adminwalletyear` and the billing service answers 500 to the request for the current payments
- **THEN** the system answers 402, and neither a purchase nor a deposit is sent to the billing service

### Requirement: Moving a subscription keeps its period

`moveSubscriptionToWallet` SHALL move a subscription to the administrators plan of its own period only:
`adminwalletyear` for a subscription that carries the `year` feature, `adminwallet` for one that does not.
The plan of the other period answers 400 "Invalid product" before the balance is moved.

#### Scenario: Plan of the other period

- **WHEN** the payer of a portal on a paid yearly subscription calls `moveSubscriptionToWallet` with `adminwallet`, or the payer of one on a paid monthly subscription calls it with `adminwalletyear`
- **THEN** the system answers 400 "Invalid product", the balance is not moved and nothing is bought

#### Scenario: Yearly subscription to the yearly plan

- **WHEN** the payer of a portal on a paid yearly subscription calls `moveSubscriptionToWallet` with `adminwalletyear` for 2 administrators, with an empty wallet, no bonus and nothing left of the period to move
- **THEN** the wallet is topped up by 440 USD and the billing service is asked for 2 of the yearly product

### Requirement: Yearly administrators plan as a wallet service

`getWalletService` SHALL answer `service` `AdminYear` with the yearly administrators plan on a hosted portal:
its `serviceName` `adminyear` and its price. The `serviceName` filter of `getCustomerOperations` SHALL take
`adminyear` and pass the request to the accounting service.

#### Scenario: Yearly plan read as a service

- **WHEN** the owner calls `getWalletService` with `service` `AdminYear` on a hosted portal
- **THEN** the system answers 200 with `serviceName` `adminyear`, at the catalogue price of 220 when the accounting service gives no price for it

#### Scenario: Wallet movements of the yearly plan

- **WHEN** a billing customer calls `getCustomerOperations` with `serviceName` `adminyear`
- **THEN** the system answers 200 with what the accounting service returns

## MODIFIED Requirements

### Requirement: Moving the subscription balance to the wallet

`getSubscriptionBalanceInfo` SHALL show the payer (403) the unused balance of the subscription period
(402 when the plan is not paid, 404 without a subscription, 400 for a wallet plan).
`moveSubscriptionToWallet` SHALL, for the payer with a payment method set (403), take a wallet
administrators plan only (400 "Invalid product") in a count above 0 and not below the portal's administrators (400),
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

#### Scenario: Monthly or yearly plan

- **WHEN** the payer names `adminwallet` or `adminwalletyear`
- **THEN** the call goes on to the count checks for either
- **AND** any other wallet product answers 400 "Invalid product"

### Requirement: Wallet services switched on per portal

`getTenantWalletServiceSettings` SHALL answer a DocSpace admin (else 403, also without billing) with
`enabledServices` and `lastModified`. `changeTenantWalletServiceState` SHALL require the portal-settings
right, a DocSpace admin and a customer (404), take `service` (`Storage`, `Backup`, `AITools`, `Admin`,
`DocsCloud`, `DocsCloudDevPack`, `AISearch`, `AdminYear`; else 400) and `enabled`, refuse an AI service while AI is
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

#### Scenario: Administrators plans in the set

- **WHEN** the owner posts `service` `Admin` or `AdminYear` with `enabled` true
- **THEN** the system answers 200 and `enabledServices` holds that member
