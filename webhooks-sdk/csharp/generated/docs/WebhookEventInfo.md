# DocSpace.Webhooks.SDK.Model.WebhookEventInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **int** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  | [optional] 
**CreateOn** | **DateTime** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  | [optional] 
**CreateBy** | **Guid** | Acting user. Omitted when the empty guid (background jobs). | [optional] 
**Trigger** | **string** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] 
**TriggerId** | **long** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

