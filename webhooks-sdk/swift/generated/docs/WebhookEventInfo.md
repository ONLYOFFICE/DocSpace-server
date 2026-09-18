# WebhookEventInfo

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **Int** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  | [optional] 
**createOn** | **Date** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  | [optional] 
**createBy** | **UUID** | Acting user. Omitted when the empty guid (background jobs). | [optional] 
**trigger** | **String** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] 
**triggerId** | **Int64** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


