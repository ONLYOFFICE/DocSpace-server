# DocSpace.Webhooks.SDK.Model.GroupPayload
ASC.Api.Core/Webhook/Payloads/GroupWebhookDto.cs. A copy of the REST GroupDto.  The member list is not carried - a group can hold thousands of users and each would be expanded into every group event. `membersCount` is the hint that the roster changed; read it from GET api/2.0/group/{id}. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** |  | [optional] 
**Name** | **string** |  | [optional] 
**Parent** | **Guid** |  | [optional] 
**Category** | **Guid** |  | [optional] 
**IsLDAP** | **bool** |  | [optional] 
**IsSystem** | **bool** |  | [optional] 
**Manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**MembersCount** | **int** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

