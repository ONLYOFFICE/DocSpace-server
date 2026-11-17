# Design

## Context

See proposal.md - Why. The change is already implemented on `feature/wallet-bonus`; this design records the
decisions behind it. It relies on these requirements of `billing`:

- "Wallet balance": the money paid into the wallet stays what `getCustomerBalance` returns; the bonus is on a
  separate goodwill account and is read on its own.
- "Who is the payer" and the DocSpace-admin rule of the read operations: the new operations follow the
  read rule (DocSpace admin), not the payer rule, because redeeming a code spends no money.
- "Unreadable answers of the payment services": an answer that cannot be read is an unknown outcome, never a
  refusal.
- "Payment changes are announced" and "Audit trail of payment changes": the existing wallet socket event and
  audit trail carry the redemption.

## Goals / Non-Goals

**Goals:**

- Count the bonus wherever the server decides how much to charge the payment method.
- Expose the bonus without changing the shape of the wallet balance.

**Non-Goals:**

- Counting the bonus in the retention checks: whether money would be lost with a portal concerns the
  customer's own money only (`notifications`, "Retention warnings before the block").
- Counting the bonus in the automatic top-up job: it keeps the wallet money itself above `minBalance`.
- Crediting a bonus from DocSpace; only the billing service credits one, through a promo code.

## Decisions

- **The bonus is read once per payment, past every cache.** The decision how much to charge must use fresh
  data, as the wallet read already does. A top-up does not change the bonus, so the top-up loop re-reads only
  the wallet. Alternative: cache the bonus like a failed wallet read - rejected, a stale bonus would top up
  too little and fail the purchase with 402.
- **An unreadable bonus counts as none.** The wallet is then topped up by the whole shortfall, the safe
  direction: never by less than it needs. Alternative: fail the payment - rejected, the bonus is an extra.
- **Only the bonus in the wallet currency counts.** The accounting service spends a bonus only in its own
  currency, so a bonus in another currency would leave the purchase short.
- **Sources are passed on unsigned.** The accounting service may give the parts of a movement with a minus;
  the direction is the `credit`/`debit` of the movement, so `amount` of a part is its magnitude.
- **A refused code is 400 with the billing reason code, every other failure 402.** Only the refusal shape
  of the billing contract is a refusal; a timeout, a 5xx, a truncated body or an unknown portal may hide a
  credited bonus, so the client must read the balance before retrying.
- **The code stays out of the audit trail.** Anyone who reads the trail could redeem a reusable code; the
  amount and currency identify the event.
- **The `type` filter is sent as `operationTypes`**, the name the accounting service reads.

Existing data: the change adds no stored state. Movements written before the accounting service split them
have `sources` null, and the report files show 0 in the bonus columns for them.

## Risks / Trade-offs

- [A client computes the top-up from the wallet balance alone and shows more than is charged] → The
  description of `moveSubscriptionToWallet` says that the bonus is counted; the web client has to read
  `getCustomerGoodwill` too.
- [An unknown bonus credit is not announced: a pending credit arrives without a socket event] → The
  description of `applyPromoCode` tells the client to read the bonus balance again.
- [The bonus read adds one accounting call to each payment from the wallet] → The callers are a user action
  and the renewal job, not a hot path.

## Migration Plan

No migration. Deploy with an accounting service that serves the goodwill account and the source split, and a
billing service that accepts promo codes. Rollback restores the old top-up amounts; no data to undo.
