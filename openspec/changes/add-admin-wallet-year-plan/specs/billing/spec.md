# Spec Delta

## ADDED Requirements

### Requirement: Yearly administrators plan paid from the wallet

A hosted portal SHALL be able to buy the administrators plan from the wallet for a year: the product
`adminwalletyear` of `updateWalletPayment` and `moveSubscriptionToWallet`, listed by `getPaymentQuotas`
with `wallet=true` and carrying the `year` feature. It follows the rules of `adminwallet`. A portal SHALL
hold one of the two: buying one while the tariff holds the other answers 400 "Quota is already set".

#### Scenario: Yearly plan bought from the wallet

- **WHEN** the owner calls `updateWalletPayment` with `Add` and `adminwalletyear` in a count not below the portal's administrators, and the tariff holds no wallet administrators plan
- **THEN** the system answers 200 `true`
- **AND** the tariff holds the yearly plan with the due date the billing service gave

#### Scenario: Fewer administrators than the portal has

- **WHEN** the owner calls `updateWalletPayment` with `Add` and `adminwalletyear` in a count below the portal's administrators, and the plan is not active
- **THEN** the system answers 400 "Invalid quantity" and nothing is bought

#### Scenario: The other administrators plan is in the tariff

- **WHEN** the owner calls `updateWalletPayment` with `Add` for `adminwalletyear` while the tariff holds `adminwallet`, or for `adminwallet` while it holds `adminwalletyear`
- **THEN** the system answers 400 "Quota is already set" and nothing is paid

#### Scenario: Yearly plan in the catalogue

- **WHEN** the owner calls `getPaymentQuotas` with `wallet=true` on a hosted portal
- **THEN** the list holds the yearly plan with a price of 220 and the `year` feature, while the payment system does not know its price

#### Scenario: Price once the payment system knows the plan

- **WHEN** the payment system has a price for the plan and the price answer cached for up to an hour has expired
- **THEN** `getPaymentQuotas` shows that price instead of 220

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
