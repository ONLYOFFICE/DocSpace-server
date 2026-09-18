# WebhookConfigInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional]
**name** | **string** |  | [optional]
**url** | **string** |  | [optional]
**triggers** | **string[]** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional]
**target** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional]
**last_failure_on** | **\DateTime** |  | [optional]
**last_failure_content** | **string** |  | [optional]
**last_success_on** | **\DateTime** |  | [optional]
**retry_count** | **int** | Omitted on the first attempt (0). Present from attempt 2. | [optional]
**retry_on** | **\DateTime** |  | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
