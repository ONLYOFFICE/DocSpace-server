# DocSpace.Webhooks.SDK.Model.WebhookConfigInfo
The subscription, plus its delivery state at send time.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **int** |  | [optional] 
**Name** | **string** |  | [optional] 
**Url** | **string** |  | [optional] 
**Triggers** | **List&lt;string&gt;** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**Target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**LastFailureOn** | **DateTime** |  | [optional] 
**LastFailureContent** | **string** |  | [optional] 
**LastSuccessOn** | **DateTime** |  | [optional] 
**RetryCount** | **int** | Omitted on the first attempt (0). Present from attempt 2. | [optional] 
**RetryOn** | **DateTime** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

