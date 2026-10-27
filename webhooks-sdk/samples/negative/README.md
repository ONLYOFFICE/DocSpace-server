# Negative fixtures

Deliveries every sample must refuse. The generate-webhook-sdk skill (stage A4)
runs each sample over them and counts a language as FAILED when it exits 0 on
any of them. They sit in this subfolder so that the positive run, which globs
`samples/*.json`, never picks them up.

| Fixture | What is wrong | Expected |
| --- | --- | --- |
| `bad-signature.json` | a genuine body, signed with another secret | the signature check rejects it |
| `no-trigger.json` | correctly signed, but `event.trigger` is missing | the envelope check rejects it |
| `no-payload.json` | correctly signed `user.updated`, but `payload` is missing | the payload parse rejects it rather than building an empty `UserPayload` |

All are signed like the positive fixtures: `<name>.sig` holds `sha256=` plus
the uppercase hex HMAC-SHA256 of the raw file bytes, keyed with `../SECRET`
(for `bad-signature`, deliberately with a different key).
