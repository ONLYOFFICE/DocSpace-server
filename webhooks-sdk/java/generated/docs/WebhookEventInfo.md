

# WebhookEventInfo


## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **Integer** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  |  [optional] |
|**createOn** | **OffsetDateTime** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  |  [optional] |
|**createBy** | **UUID** | Acting user. Omitted when the empty guid (background jobs). |  [optional] |
|**trigger** | **String** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. |  [optional] |
|**triggerId** | **Long** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). |  [optional] |



