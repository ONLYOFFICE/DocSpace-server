# DocspaceWebhooksSdk::UserSummaryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** |  | [optional] |
| **display_name** | **String** |  | [optional] |
| **user_name** | **String** |  | [optional] |
| **avatar** | **String** | Absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] |
| **profile_url** | **String** |  | [optional] |

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

