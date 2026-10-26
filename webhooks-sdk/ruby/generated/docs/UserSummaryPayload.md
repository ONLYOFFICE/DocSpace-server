# DocspaceWebhooksSdk::UserSummaryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** | The user ID. | [optional] |
| **display_name** | **String** | The HTML-encoded user display name. | [optional] |
| **user_name** | **String** | The user name. | [optional] |
| **avatar** | **String** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] |
| **profile_url** | **String** | The absolute link to the user in the portal&#39;s list of people, or of guests for a guest; today it is a search of that list by the user&#39;s email address. Absent when the subscription&#39;s creator is a User or a Guest, who may not see the contacts of other users, unless this user is that creator. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::UserSummaryPayload.new(
  id: null,
  display_name: null,
  user_name: null,
  avatar: null,
  profile_url: null
)
```

