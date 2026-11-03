# Fixtures

Signed webhook deliveries as a DocSpace server really sent them, the key they are signed with (`SECRET`) and,
in `negative/`, deliveries a receiver must refuse. The sample and the tests of this SDK replay them.

**Do not edit these files.** They are a copy, rewritten every time the SDKs are regenerated. The source is
`web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures/` in the DocSpace server repository: the captured
deliveries are written there by `WebhookFixtureCaptureTests`, the hand-written ones are changed there and
re-signed with `SECRET`, and the `generate-webhook-sdk` skill copies the folder into every SDK.

`SECRET` is a throwaway key that signs these fixtures only; it belongs to no real subscription.
