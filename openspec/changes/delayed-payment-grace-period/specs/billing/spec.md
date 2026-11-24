# Spec Delta

## ADDED Requirements

### Requirement: Grace period of a delayed payment method

A hosted portal in the grace period SHALL be restricted unless its billing customer pays by a delayed method
(`isDelayedPaymentMethod` of `getCustomerInfo`, as billing last reported it). A restricted tariff refuses what the grace period refuses; an
unrestricted one SHALL let those actions through as a paid tariff does, keeping `state` Delay, `delayDueDate`,
the grace letters and the payment URL. NotPaid and the grace period of a self-hosted installation SHALL always be
restricted.

#### Scenario: Grace period with a delayed payment method

- **WHEN** the due date of a hosted portal's plan was two days ago, within the portal's grace days, and its billing customer has a delayed payment method
- **THEN** `getPortalTariff` reports `state` Delay and `restricted` false
- **AND** the owner creates the invitation link of a Room admin with 200 and adds a Room admin, who keeps that type

#### Scenario: Grace period without a delayed payment method

- **WHEN** the due date of a hosted portal's plan was two days ago, within the portal's grace days, and its billing customer pays by card
- **THEN** `getPortalTariff` reports `state` Delay and `restricted` true
- **AND** creating the invitation link of a Room admin answers 402 "Your pricing plan does not support this option"

#### Scenario: Grace period over with a delayed payment method

- **WHEN** the due date of a hosted portal's plan passed more than the grace days ago and its billing customer has a delayed payment method
- **THEN** `getPortalTariff` reports `state` NotPaid and `restricted` true, and the payment gate answers 402 to reading the portal logo

#### Scenario: Portal that is not a billing customer

- **WHEN** a hosted portal is in the grace period and billing reports no customer for it
- **THEN** `getPortalTariff` reports `restricted` true

### Requirement: Delayed payment method when billing does not answer

Without a customer answer, the last answer of billing for the portal SHALL decide for the grace days plus one
after it was given, while the distributed cache answers; with none, the grace period SHALL be restricted. A
changed answer SHALL take effect once a call reads the customer again after its 10-minute cache, at once after
`getCustomerInfo` or `getPortalTariff` with `refresh=true`, and SHALL queue `change-tariff` to the portal's
quota room once remembered.

#### Scenario: Billing fails after a delayed answer

- **WHEN** billing reported a delayed payment method for a portal in the grace period and then answers the customer request with 500
- **THEN** `getPortalTariff` keeps reporting `restricted` false and creating the invitation link of a Room admin answers 200

#### Scenario: Billing fails with no earlier answer

- **WHEN** a portal in the grace period has never had a customer answer from billing and billing answers the customer request with 500
- **THEN** `getPortalTariff` reports `restricted` true

#### Scenario: Customer switches to a delayed payment method

- **WHEN** the billing customer of a portal in the grace period switches from card to a delayed payment method and a DocSpace admin calls `getCustomerInfo` with `refresh=true`
- **THEN** `change-tariff` is queued for the portal's quota room and `getPortalTariff` reports `restricted` false

#### Scenario: Refresh of the tariff after a switch

- **WHEN** the billing customer of a portal in the grace period switches from card to a delayed payment method and a member calls `getPortalTariff` with `refresh=true`
- **THEN** it reports `restricted` false

### Requirement: Restricted flag of the tariff

`getPortalTariff` SHALL give `restricted` to every member who gets `state`: true while the tariff refuses what
the grace period refuses (a restricted grace period, NotPaid), false on a trial, a paid tariff and an
unrestricted grace period.

#### Scenario: Paid tariff

- **WHEN** any member of a portal on a paid or free plan calls `getPortalTariff`
- **THEN** `restricted` is false

#### Scenario: Not paid

- **WHEN** a member of a NotPaid hosted portal calls `getPortalTariff`
- **THEN** `state` is NotPaid and `restricted` true

## MODIFIED Requirements

### Requirement: Reading the tariff

`getPortalTariff` SHALL answer every signed-in member (anonymous 401) and grade the body by role: every
member gets `state`; a Room admin or DocSpace admin also `dueDate` and `delayDueDate`; a caller with the
portal-settings right also `id`, `customerId`, `licenseDate`, `openSource`, `enterprise`, `developer` and
`quotas` — a row per paid and per overdue quota (`id`, `quantity`, `wallet`, `additional`, `dueDate`,
`nextQuantity`, `nextQuota`). Others get `id` 0. `refresh=true` re-reads billing.

#### Scenario: Owner and DocSpace admin read the full tariff

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the system answers 200 with `state` Paid, `restricted` false, `id` of 0 or more and a non-empty `quotas`

#### Scenario: Other members get the state only

- **WHEN** a Room admin, a User or a Guest calls `getPortalTariff`
- **THEN** the system answers 200 with `id` 0, `state` Paid and `restricted` false, and no `customerId` or `quotas`

### Requirement: Tariff of a hosted portal follows the billing service

On a hosted portal with a billing service the tariff SHALL be re-read from the payments of the billing
customer when the cache expires or `refresh=true` is passed: the paid quotas with their quantities, the
payer e-mail as `customerId` and the due date of the earliest payment. When billing stops reporting the
payment of a paid, non-free tariff (a refund), the due date SHALL become yesterday so the portal enters the
grace period; a portal with no payment and no free quota SHALL become NotPaid.

#### Scenario: Refund

- **WHEN** the billing service stops reporting the payment of a portal on a priced plan
- **THEN** the tariff's due date becomes yesterday and the portal is in the grace period on the next read

#### Scenario: Not-found answer under a failing status

- **WHEN** the billing service answers the payments request of a portal on a priced plan with "error: cannot find" under a 5xx, 408 or 429
- **THEN** the answer counts as a billing failure, not as a stop of reporting, and the tariff keeps its due date

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

- **WHEN** a portal is in the grace period, its billing customer has no delayed payment method, and the owner creates a room
- **THEN** the system answers 402 "Your pricing plan does not support this option" (the count limits of `quotas`), while every other call is answered

#### Scenario: No request header opens the gate

- **WHEN** a request to a NotPaid portal carries a `Payment-Info` header, whatever its value
- **THEN** the gate answers 402 as for any other request; only the operations allowed without payment pass

### Requirement: Buying wallet services

`updateWalletPayment` SHALL require the billing service, a DocSpace admin and a customer, one wallet
product and `productQuantityType` `Set` or `Add` (400 "Invalid product quantity type"), each 402 for
an add-on on a restricted tariff. `Add` buys from the wallet (404 if known to have no sub-account and no
bonus not ended in the accounting currency; minimums: storage 100, Dev Pack 10, the current admins; 400
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
- **THEN** with a bonus of 5 USD, or when the bonus read fails with 500, the call goes on to the price
- **AND** without a bonus it answers 404 "Subaccount could not be found"

### Requirement: Moving the subscription balance to the wallet

`getSubscriptionBalanceInfo` SHALL show the payer (403) the unused balance of the subscription period
(402 on trial or restricted plans, 404 without a subscription, 400 for a wallet plan).
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

- **WHEN** the due date of an additional wallet service comes near while the portal's tariff is restricted
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

### Requirement: Paid seats and tariff limits

The number of DocSpace admins and Room admins a portal may hold SHALL be the `manager` count of its tariff;
the checks are in `quotas` ("Count limits of the tariff"): the invitation link of a paid role answers 402
once the paid seats are used up, a bulk invitation over the limit is refused as a whole, and the same
checks answer 402 "Your pricing plan does not support this option" while the tariff is restricted. Buying
`Admin` units from the wallet SHALL raise the seats of a hosted portal.

#### Scenario: Paid seats used up

- **WHEN** the portal's paid-user limit is reached and the owner reads the invitation link of DocSpace admin or Room admin
- **THEN** the system answers 402
