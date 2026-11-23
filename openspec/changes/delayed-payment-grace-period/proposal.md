# Proposal

## Why

A hosted portal whose subscription renews by a delayed payment method (SEPA direct debit) has its renewal
charged, but the money reaches billing only days later. Meanwhile the due date passes, the tariff enters the
grace period and the portal loses the actions the grace period refuses (new members, rooms, admins, paid
operations), although the customer has already paid. The grace period should still be shown for such a
portal, but should not limit it until it ends and the tariff becomes NotPaid.

## What Changes

- A hosted portal in the grace period whose billing customer has a delayed payment method (the
  `isDelayedPaymentMethod` that `getCustomerInfo` already shows) is **not restricted**: every action the grace
  period refuses today goes through as on a paid tariff.
  - The count checks of the tariff (rooms, AI agents, members, guests, paid roles, invitation links of a paid
    role, type changes, re-enabling) no longer answer 402 "Your pricing plan does not support this option" for it.
  - `updateWalletPayment` buys an add-on, `getSubscriptionBalanceInfo` and `moveSubscriptionToWallet` answer
    instead of 402 "Tariff is not paid", `startDocsCloudTrial`, `calculateDevPack` and `switchToDevPack` pass the
    tariff check.
  - The automatic renewal of an additional wallet service renews it; the backup scheduler runs the schedule.
- `getPortalTariff` keeps `state` Delay and `delayDueDate` and gains a boolean `restricted`, given to every
  member like `state`: true in the grace period without a delayed payment method and in NotPaid, false otherwise.
  The client shows the grace period by `state` and limits its controls by `restricted`.
- When billing does not answer `getCustomerInfo`, the portal keeps the last answer billing gave (kept for the
  grace days plus one after it was given); with no answer ever received, or one older than that, the grace period
  restricts as today. A read of a cached tariff never waits for billing: it decides by the cached customer.
- When that last answer changes, the portal's clients are told that the tariff changed (the existing tariff
  socket event), so they re-read `getPortalTariff`.
- NotPaid is unchanged: the payment gate answers 402 to every call outside the allowed list, whatever the
  payment method.
- **BREAKING** for the requirements listed under "Breaks current behaviour": they say the grace period refuses
  these actions for every portal.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `billing`: found by `getPortalTariff` (the `state`, `delayDueDate` and the new `restricted`), `getCustomerInfo`
  (`isDelayedPaymentMethod`), `updateWalletPayment`, `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet` and
  the automatic renewal of wallet subscriptions. Requirements modified: "Reading the tariff", "Payment gate while
  the portal is not paid" (scenario "Grace period"), "Buying wallet services", "Moving the subscription balance to
  the wallet", "Automatic renewal of a wallet subscription" (scenario "Additional wallet service while the tariff
  is overdue"), "Paid seats and tariff limits". Requirement added: the grace period of a delayed payment method.
- `quotas`: found by `createRoom`, `createRoomFromTemplate`, `createRoomThirdParty`, `addMember`,
  `getInvitationLink`, `getInvitationLinkByEmployeeType`, `createInvitationLink`. Requirement modified: "Count
  limits of the tariff" (scenario "Bulk invitation and a lapsed tariff").
- `backup-and-restore`: found by `createBackupSchedule` and the scheduled run. Requirement modified: "Running
  the schedule" (its body and the scenario "Portal in the grace period").
- `storage-and-integrations`: found by `startDocsCloudTrial`, `calculateDevPack`, `switchToDevPack`.
  Requirements modified: "Docs Connect trial", "Moving to Docs Connect Dev Pack".

## Breaks current behaviour

- **Payment gate while the portal is not paid**, scenario "Grace period"; **Paid seats and tariff limits**;
  **Count limits of the tariff**, scenario "Bulk invitation and a lapsed tariff". A portal in the grace period
  with a delayed payment method gets 200 instead of 402 "Your pricing plan does not support this option" for
  `createRoom`, `createRoomFromTemplate`, `createRoomThirdParty`, the room copy and restore operations, AI agent
  creation, `addMember`, the invitation links of a paid role, the type changes and re-enabling of members.
  The paid-seat and room limits still apply. Portals without a delayed method see nothing new.
- **Buying wallet services**: `updateWalletPayment` with `Add` of an add-on answers 200 instead of 402 for
  such a portal.
- **Moving the subscription balance to the wallet**: `getSubscriptionBalanceInfo` and
  `moveSubscriptionToWallet` answer as on a paid plan instead of 402 "Tariff is not paid".
- **Automatic renewal of a wallet subscription**: an additional wallet service of such a portal is renewed in
  the grace period instead of being left to lapse.
- **Docs Connect trial**, **Moving to Docs Connect Dev Pack**: `startDocsCloudTrial`, `calculateDevPack`,
  `switchToDevPack` no longer answer 402 for such a portal.
- **Running the schedule**: the scheduled backup of such a portal runs in the grace period instead of being
  skipped; it counts against the free allowance as on a paid tariff.
- **Reading the tariff**: `getPortalTariff` gains `restricted`; `state` and every other field are unchanged.

Who sees it:

- web client (`../client`): it decides today from `state` Delay which controls to disable; until it reads
  `restricted`, a portal with a delayed method sees the server accept actions the client still hides. The client
  was not checked from this repository.
- SDKs and the OpenAPI document: `TariffDto` gains `restricted`; the descriptions of the operations above change
  where they name the grace period. The SDKs need regenerating.
- webhooks: none, no payload carries the tariff state.
- socket events: the existing tariff event is also sent when the delayed-method answer of billing changes; no
  new event or payload.
- backup and restore, portal migration, stored data: none. The last delayed-method answer is kept in the
  distributed cache, not in the database; the tariff rows are unchanged.

## Deviations

No row of `openspec/deviations.md` is touched. N9 (`notifications`, the What's New digest in the grace period)
is about the not-paid state and stays as it is.

## Impact

- Code: the tariff (`common/ASC.Core.Common/Billing`), the count checks (`web/ASC.Web.Core/Quota`,
  `products/ASC.Files/Core`), the payment and Docs Connect controllers (`web/ASC.Web.Api`), the wallet renewal
  (`web/ASC.Web.Studio/Wallet`), the backup scheduler (`common/services/ASC.Data.Backup.Worker`), the queue lanes
  that favour paid portals.
- Billing: one more `getCustomerInfo` call per portal in the grace period, cached as today; no new billing API.
- Billing: `isDelayedPaymentMethod` is true for a customer who pays by SEPA and false for one who pays by
  card, the method the subscription renewal is charged to.