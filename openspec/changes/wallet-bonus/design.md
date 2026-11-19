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

- **A missing wallet sub-account is an empty wallet.** A customer who has linked a card but never topped up has no
  wallet sub-account, also with a bonus credited. The top-up reads it as 0, and a purchase from the wallet is let
  through when a bonus in the currency has not ended (`GoodwillBalance.CanPayFromWallet`, unit-tested). A bonus
  that cannot be read is unknown, not "none" (`TariffService.ReadCustomerGoodwillAsync`): the purchase goes on and
  billing decides. Without a balance, or when the portal is known to have neither a sub-account nor such a bonus,
  the answer stays 404; `moveSubscriptionToWallet` and the renewal answer 404 only without a balance.

- **The bonus is read past every cache, and again with the wallet after each top-up.** The decision how much
  to charge must use fresh data, as the wallet read already does. Charges running during the top-up spend the
  bonus before the wallet, so a bonus read only once could make the check pass while the purchase that follows
  is short. Alternative: read it once, or cache it like a failed wallet read - rejected for that reason.
- **The accounting service spends the bonus before the wallet money, also on a wallet purchase.** This order
  belongs to the accounting service; the product owner confirmed it for this change. The payment stubs do not
  model it: they answer the purchase without checking funds, so the tests prove the top-up amounts only.
- **A bonus that cannot be read before the first top-up counts as none; a later failed read keeps the last
  one.** Before any deposit, counting none tops the wallet up by the whole shortfall, the safe direction. After a
  confirmed deposit, counting none would fire a second deposit for money the first already covered
  (`failure-paths.md` §4), so the bonus read before stays. Alternative: fail the payment - rejected, the bonus is an extra.
- **Only the bonus in the wallet currency counts.** The accounting service spends a bonus only in its own
  currency, so a bonus in another currency would leave the purchase short.
- **Only credits that have not ended count, at most the sub-account amount.** The accounting service may write an
  ended credit off only later, so the sub-account alone can still hold money it will not spend
  (`failure-paths.md` §9). The bonus counted is the rest of the `lots` in the wallet currency whose `expiresAt`
  is later than the read or empty, capped by the sub-account; an answer without `lots` counts the sub-account
  (`GoodwillBalance.SpendableAmount`, unit-tested with the end a day and an hour ago, now, an hour and a day ahead).
  An end without an offset is read as UTC, not as the host's local time.
- **Sources are passed on unsigned.** The accounting service may give the parts of a movement with a minus;
  the direction is the `credit`/`debit` of the movement, so `amount` of a part is its magnitude.
- **A refused code is 400 with the billing reason code; a failure of the billing service or an unknown portal
  is 402, and an answer that cannot be read 500.** Only the refusal shape of the billing contract, without
  `promo_failed`, is a refusal; a timeout, a 5xx, `promo_failed` or a truncated body may hide a credited bonus,
  so the client must read the balance before retrying. An unreadable answer is a contract failure, which the
  server answers 500 like every payment service answer it cannot read.
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
- [The bonus read adds one accounting read before the top-up and one after each of the at most five attempts,
  so at most six reads, each retried up to twice on a failed answer] → The callers are a user action and the
  renewal job, not a hot path.
- [A credit that ends between the last bonus read and the purchase still counts, so the purchase may be short]
  → Accepted: the window is the time between that read and the purchase call.
- [Whether the accounting service lists an ended credit in `lots`, or counts it in the sub-account, until it writes
  the credit off is its contract, still to be confirmed] → The server counts neither: it compares each end with
  the time of the read and caps by the sub-account, so either answer gives the same bonus.
- [An unknown promo outcome writes no audit event: a 402 or 500 can hide a credited bonus] → The bonus shows
  in `getCustomerGoodwill` and in the movements as `GoodwillCredit`; the audit trail misses that redemption.

## Migration Plan

No migration. Deploy with an accounting service that serves the goodwill account and the source split, and a
billing service that accepts promo codes. Rollback restores the old top-up amounts; no data to undo.
