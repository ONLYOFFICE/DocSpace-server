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

#### Scenario: Wallet never topped up

- **WHEN** a payment costs 30 USD and the wallet has no sub-account, as for a customer who has never topped it up
- **THEN** with a bonus of 50 USD the wallet is not topped up, and without a bonus it is topped up by 30 USD

#### Scenario: Delayed payment method

- **WHEN** the payment method is delayed, the payment costs 30 USD, the wallet holds 5 USD and the bonus 25 USD
- **THEN** no top-up is made and the payment goes on

## MODIFIED Requirements

### Requirement: Buying wallet services

`updateWalletPayment` SHALL require the billing service, a DocSpace admin and a customer, one wallet
product and `productQuantityType` `Set` or `Add` (400 "Invalid product quantity type"). `Add` buys
from the wallet (404 if known to have no sub-account and no bonus not ended in the accounting currency;
402 for an add-on while the plan is overdue; minimums: storage 100, Dev Pack 10, the portal's
administrators; 400 "Invalid quantity"). `Set` writes the next period's quantity, zero cancelling it.

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
- **THEN** with a bonus of 5 USD, or when the bonus read fails with 500, the call goes on to the price
- **AND** without a bonus it answers 404 "Subaccount could not be found"

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
