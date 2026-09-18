# DocspaceWebhooksSdk::WebhookConfigInfo

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **Integer** |  | [optional] |
| **name** | **String** |  | [optional] |
| **url** | **String** |  | [optional] |
| **triggers** | **Array&lt;String&gt;** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] |
| **target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] |
| **last_failure_on** | **Time** |  | [optional] |
| **last_failure_content** | **String** |  | [optional] |
| **last_success_on** | **Time** |  | [optional] |
| **retry_count** | **Integer** | Omitted on the first attempt (0). Present from attempt 2. | [optional] |
| **retry_on** | **Time** |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::WebhookConfigInfo.new(
  id: null,
  name: null,
  url: null,
  triggers: null,
  target: null,
  last_failure_on: null,
  last_failure_content: null,
  last_success_on: null,
  retry_count: null,
  retry_on: null
)
```

