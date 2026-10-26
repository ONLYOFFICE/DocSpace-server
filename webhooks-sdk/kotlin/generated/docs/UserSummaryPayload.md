
# UserSummaryPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**java.util.UUID**](java.util.UUID.md) | The user ID. |  [optional] |
| **displayName** | **kotlin.String** | The HTML-encoded user display name. |  [optional] |
| **userName** | **kotlin.String** | The user name. |  [optional] |
| **avatar** | **kotlin.String** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. |  [optional] |
| **profileUrl** | **kotlin.String** | The absolute link to the user in the portal&#39;s list of people, or of guests for a guest; today it is a search of that list by the user&#39;s email address. Absent when the subscription&#39;s creator is a User or a Guest, who may not see the contacts of other users, unless this user is that creator. |  [optional] |



