
# WebhookConfigInfo

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | **kotlin.Int** |  |  [optional] |
| **name** | **kotlin.String** |  |  [optional] |
| **url** | **kotlin.String** |  |  [optional] |
| **triggers** | **kotlin.collections.List&lt;kotlin.String&gt;** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. |  [optional] |
| **target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  |  [optional] |
| **lastFailureOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **lastFailureContent** | **kotlin.String** |  |  [optional] |
| **lastSuccessOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **retryCount** | **kotlin.Int** | Omitted on the first attempt (0). Present from attempt 2. |  [optional] |
| **retryOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |



