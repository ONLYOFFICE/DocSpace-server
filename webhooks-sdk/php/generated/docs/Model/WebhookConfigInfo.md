# WebhookConfigInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | The subscription ID. | [optional]
**name** | **string** | The subscription name given by its creator. | [optional]
**url** | **string** | The URL the delivery is posted to. | [optional]
**triggers** | **string[]** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional]
**target** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional]
**last_failure_on** | **\DateTime** | When the last delivery to this subscription failed, in UTC; absent when none has failed. | [optional]
**last_failure_content** | **string** | What the receiver answered, or the network error, when the last delivery to this subscription failed. | [optional]
**last_success_on** | **\DateTime** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. | [optional]
**retry_count** | **int** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. | [optional]
**retry_on** | **\DateTime** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
