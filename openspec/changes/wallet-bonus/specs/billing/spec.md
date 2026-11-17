# Spec Delta

## ADDED Requirements

### Requirement: Redeeming a promo code

`applyPromoCode` SHALL let a DocSpace admin (403 otherwise and without billing, 401 anonymous) redeem
`promoCode`, 1 to 100 characters after surrounding spaces are trimmed (400), passing it to the billing
service with the caller as initiator. It SHALL answer `status` `Completed` or `Pending`, `amount`, `currency`
in upper case and `expiresAt` (null when the bonus never expires). A code the billing service refuses SHALL
answer 400 with its reason code and credit nothing.

#### Scenario: Bonus credited at once

- **WHEN** a DocSpace admin redeems ` K7QX-M42P ` and the billing service credits 25 usd that expire in a month
- **THEN** the system answers 200 with `status` `Completed`, `amount` 25, `currency` `USD` and that `expiresAt`
- **AND** the billing service received the code trimmed, with the caller's e-mail as initiator

#### Scenario: Bonus credited later

- **WHEN** the billing service accepts the code but credits the bonus later, without an expiry
- **THEN** the system answers 200 with `status` `Pending` and `expiresAt` null

#### Scenario: Code refused

- **WHEN** the billing service refuses the code as unknown or expired
- **THEN** the system answers 400 with the reason code, such as `promo_not_found`, and no bonus is credited

#### Scenario: Billing service fails or does not know the portal

- **WHEN** the billing service cannot be reached, fails on its side, or does not know the portal
- **THEN** the system answers 402, not 400, since the bonus may have been credited all the same

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

Every promo code the billing service accepts SHALL write one audit event naming the amount and the currency,
never the code itself. A bonus credited at once SHALL be pushed to the portal's clients as a wallet change,
like a top-up; a pending one SHALL push nothing. A failure to push SHALL NOT fail the redemption.

#### Scenario: Audit record of a redeemed code

- **WHEN** a DocSpace admin redeems a code worth 25 USD
- **THEN** the audit trail of `history-and-audit` shows "Promo code applied: 25 USD" and not the code

#### Scenario: Pending bonus

- **WHEN** the billing service answers `Pending`
- **THEN** the audit event is written and no wallet change is pushed

### Requirement: Bonus balance

`getCustomerGoodwill` SHALL return to a DocSpace admin (403 otherwise and without billing, 401 anonymous) the
portal's goodwill account, read from the accounting service on every call: the account with one sub-account
per currency, and `lots`, the bonus credits neither expired nor spent in full, in spending order, each with
`operationId`, `amount`, `remaining`, `currency`, `date` and `expiresAt` in UTC. A portal without a customer,
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

### Requirement: Paying a renewal from the wallet with the bonus counted

The automatic renewal of a wallet subscription SHALL top the wallet up from the payment method only for the
part of the price that the wallet money and the bonus in the wallet currency do not cover. A bonus in another
currency, or one that cannot be read, SHALL count as none. With a delayed payment method no top-up SHALL be
made, and the wallet and the bonus together SHALL cover the price.

#### Scenario: Bonus covers the price

- **WHEN** a renewal costs 40 USD, the wallet holds 0 and the bonus 40 USD
- **THEN** the wallet is not topped up and the renewal goes on

#### Scenario: Bonus covers part of the price

- **WHEN** a renewal costs 40 USD and the bonus holds 15 USD
- **THEN** the wallet is topped up by 25 USD

#### Scenario: Bonus in another currency

- **WHEN** the price is in USD and the bonus is in EUR
- **THEN** the bonus is not counted and the wallet is topped up by the whole price

#### Scenario: Bonus that cannot be read

- **WHEN** the accounting service does not answer the bonus balance
- **THEN** the wallet is topped up as if there were no bonus, never by less than it needs

#### Scenario: Delayed payment method

- **WHEN** the payment method is delayed and the wallet and the bonus together cover the price
- **THEN** no top-up is made and the renewal goes on

## MODIFIED Requirements

### Requirement: Moving the subscription balance to the wallet

`getSubscriptionBalanceInfo` SHALL show the payer (403) the unused balance of the subscription period
(402 when the plan is not paid, 404 without a subscription, 400 for a wallet plan).
`moveSubscriptionToWallet` SHALL, for the payer with a payment method set (403), take the administrators
product only (400 "Invalid product") in a count above 0 and not below the portal's administrators (400),
move the balance, top the wallet up for the part of price times count that the wallet money and the bonus
in the wallet currency do not cover (402 when they still fall short) and buy the administrators, the bonus
being spent first.

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
- **THEN** the wallet is topped up by 25 USD and the administrators are bought, paid first from the bonus
