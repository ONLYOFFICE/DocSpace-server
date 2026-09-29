# WebhookEventInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | Delivery-log record id, and the idempotency key. Stable across every resend of one delivery -- the server&#39;s automatic retries and a manual retry from the admin UI alike, since a manual retry requeues the same record rather than minting a new one. The same id therefore means the same logical event, always. It is also echoed in x-docspace-event-id. The delivery log keeps only the latest attempt of a record, so a manual retry overwrites the outcome of the previous one. | [optional]
**create_on** | **\DateTime** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body. | [optional]
**create_by** | **string** | Acting user. Omitted when the empty guid (background jobs). | [optional]
**trigger** | **string** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional]
**trigger_id** | **int** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
