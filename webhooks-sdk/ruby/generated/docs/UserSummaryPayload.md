# DocspaceWebhooksSdk::UserSummaryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** | The user ID. | [optional] |
| **display_name** | **String** | The HTML-encoded user display name. | [optional] |
| **user_name** | **String** | The user name. | [optional] |
| **avatar** | **String** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] |
| **profile_url** | **String** | The user profile URL. | [optional] |

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

