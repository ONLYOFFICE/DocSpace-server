
# WebhookConfigInfo

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | **kotlin.Int** | The subscription ID. |  [optional] |
| **name** | **kotlin.String** | The subscription name given by its creator. |  [optional] |
| **url** | **kotlin.String** | The URL the delivery is posted to. |  [optional] |
| **triggers** | **kotlin.collections.List&lt;kotlin.String&gt;** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. |  [optional] |
| **target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  |  [optional] |
| **lastFailureOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | When the last delivery to this subscription failed, in UTC; absent when none has failed. |  [optional] |
| **lastFailureContent** | **kotlin.String** | What the receiver answered, or the network error, when the last delivery to this subscription failed. |  [optional] |
| **lastSuccessOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. |  [optional] |
| **retryCount** | **kotlin.Int** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. |  [optional] |
| **retryOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | When the next retry is scheduled, in UTC; absent when no retry is pending. |  [optional] |



