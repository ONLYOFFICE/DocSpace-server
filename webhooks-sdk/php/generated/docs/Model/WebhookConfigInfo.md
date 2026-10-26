# WebhookConfigInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | The subscription ID. | [optional]
**name** | **string** | The subscription name given by its creator. | [optional]
**url** | **string** | The URL the delivery is posted to. | [optional]
**triggers** | **string[]** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional]
**target** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional]
**last_failure_on** | **\DateTime** | When the last delivery to this subscription failed, in UTC. Sent only on an automatic retry, together with &#x60;retryCount&#x60;; absent on a first attempt, the first attempt of a manual retry included. | [optional]
**last_failure_content** | **string** | What the receiver answered, or the network error, when the last delivery to this subscription failed. Sent only on an automatic retry, like &#x60;lastFailureOn&#x60;. | [optional]
**last_success_on** | **\DateTime** | When the last delivery to this subscription succeeded, in UTC. Sent only on an automatic retry, like &#x60;lastFailureOn&#x60;; absent there too when no delivery has succeeded yet. | [optional]
**retry_count** | **int** | How many times this delivery has been retried automatically. Omitted on the first attempt (0); present from attempt 2. A manual retry is a new delivery and starts again from 0, so its first attempt carries no &#x60;retryCount&#x60;, &#x60;lastFailureOn&#x60; or &#x60;lastSuccessOn&#x60;; only the repeated &#x60;event.id&#x60; shows that the event was sent before. | [optional]
**retry_on** | **\DateTime** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
