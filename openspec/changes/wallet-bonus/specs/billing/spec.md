# Spec Delta

## ADDED Requirements

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

### Requirement: Bonus balance

`getCustomerGoodwill` SHALL return to a DocSpace admin (403 otherwise and without billing, 401 anonymous) the
portal's goodwill account, read from the accounting service on every call: the account with one sub-account
per currency, and `lots`, the bonus credits not spent in full, in spending order, each with
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

## MODIFIED Requirements

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
