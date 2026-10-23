# DocSpace.Webhooks.SDK.Model.WebhookConfigInfo
The subscription, plus its delivery state at send time.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **int** | The subscription ID. | [optional] 
**Name** | **string** | The subscription name given by its creator. | [optional] 
**Url** | **string** | The URL the delivery is posted to. | [optional] 
**Triggers** | **List&lt;string&gt;** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**Target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**LastFailureOn** | **DateTime** | When the last delivery to this subscription failed, in UTC; absent when none has failed. | [optional] 
**LastFailureContent** | **string** | What the receiver answered, or the network error, when the last delivery to this subscription failed. | [optional] 
**LastSuccessOn** | **DateTime** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. | [optional] 
**RetryCount** | **int** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. | [optional] 
**RetryOn** | **DateTime** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

