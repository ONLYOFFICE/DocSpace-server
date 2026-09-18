# WebhookConfigInfo

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **Int** |  | [optional] 
**name** | **String** |  | [optional] 
**url** | **String** |  | [optional] 
**triggers** | **[String]** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**lastFailureOn** | **Date** |  | [optional] 
**lastFailureContent** | **String** |  | [optional] 
**lastSuccessOn** | **Date** |  | [optional] 
**retryCount** | **Int** | Omitted on the first attempt (0). Present from attempt 2. | [optional] 
**retryOn** | **Date** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


