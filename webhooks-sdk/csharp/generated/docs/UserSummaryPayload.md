# DocSpace.Webhooks.SDK.Model.UserSummaryPayload
A user referenced from another webhook payload.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | The user ID. | [optional] 
**DisplayName** | **string** | The HTML-encoded user display name. | [optional] 
**UserName** | **string** | The user name. | [optional] 
**Avatar** | **string** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] 
**ProfileUrl** | **string** | The absolute link to the user in the portal&#39;s list of people, or of guests for a guest; today it is a search of that list by the user&#39;s email address. Absent when the subscription&#39;s creator is a User or a Guest, who may not see the contacts of other users, unless this user is that creator. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

