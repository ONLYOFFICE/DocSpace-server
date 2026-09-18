

# WebhookConfigInfo

The subscription, plus its delivery state at send time.

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **Integer** |  |  [optional] |
|**name** | **String** |  |  [optional] |
|**url** | **String** |  |  [optional] |
|**triggers** | **List&lt;String&gt;** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. |  [optional] |
|**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  |  [optional] |
|**lastFailureOn** | **OffsetDateTime** |  |  [optional] |
|**lastFailureContent** | **String** |  |  [optional] |
|**lastSuccessOn** | **OffsetDateTime** |  |  [optional] |
|**retryCount** | **Integer** | Omitted on the first attempt (0). Present from attempt 2. |  [optional] |
|**retryOn** | **OffsetDateTime** |  |  [optional] |



