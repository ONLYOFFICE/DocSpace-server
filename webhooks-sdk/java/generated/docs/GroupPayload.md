

# GroupPayload

ASC.Api.Core/Webhook/Payloads/GroupWebhookDto.cs. A copy of the REST GroupDto.  The member list is not carried - a group can hold thousands of users and each would be expanded into every group event. `membersCount` is the hint that the roster changed; read it from GET api/2.0/group/{id}. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **UUID** |  |  [optional] |
|**name** | **String** |  |  [optional] |
|**parent** | **UUID** |  |  [optional] |
|**category** | **UUID** |  |  [optional] |
|**isLDAP** | **Boolean** |  |  [optional] |
|**isSystem** | **Boolean** |  |  [optional] |
|**manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**membersCount** | **Integer** |  |  [optional] |



