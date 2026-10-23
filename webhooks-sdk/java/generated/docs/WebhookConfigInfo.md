

# WebhookConfigInfo

The subscription, plus its delivery state at send time.

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **Integer** | The subscription ID. |  [optional] |
|**name** | **String** | The subscription name given by its creator. |  [optional] |
|**url** | **String** | The URL the delivery is posted to. |  [optional] |
|**triggers** | **List&lt;String&gt;** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. |  [optional] |
|**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  |  [optional] |
|**lastFailureOn** | **OffsetDateTime** | When the last delivery to this subscription failed, in UTC; absent when none has failed. |  [optional] |
|**lastFailureContent** | **String** | What the receiver answered, or the network error, when the last delivery to this subscription failed. |  [optional] |
|**lastSuccessOn** | **OffsetDateTime** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. |  [optional] |
|**retryCount** | **Integer** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. |  [optional] |
|**retryOn** | **OffsetDateTime** | When the next retry is scheduled, in UTC; absent when no retry is pending. |  [optional] |



