# WebhookEventInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat. | [optional]
**create_on** | **\DateTime** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body. | [optional]
**create_by** | **string** | Acting user. Omitted when the empty guid (background jobs). | [optional]
**trigger** | **string** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional]
**trigger_id** | **int** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
