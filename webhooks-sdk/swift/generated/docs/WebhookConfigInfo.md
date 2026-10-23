# WebhookConfigInfo

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **Int** | The subscription ID. | [optional] 
**name** | **String** | The subscription name given by its creator. | [optional] 
**url** | **String** | The URL the delivery is posted to. | [optional] 
**triggers** | **[String]** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**lastFailureOn** | **Date** | When the last delivery to this subscription failed, in UTC; absent when none has failed. | [optional] 
**lastFailureContent** | **String** | What the receiver answered, or the network error, when the last delivery to this subscription failed. | [optional] 
**lastSuccessOn** | **Date** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. | [optional] 
**retryCount** | **Int** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. | [optional] 
**retryOn** | **Date** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


