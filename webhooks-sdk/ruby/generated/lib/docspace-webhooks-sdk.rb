# frozen_string_literal: true

# Entry point of the webhook payload models. Models-only output: the stock
# entry point would also load api_client (and with it typhoeus) and
# configuration, none of which a receiver uses - a webhook is parsed, never
# requested. Overrides the generator's gem.mustache; see ../../generate.sh.

require 'docspace-webhooks-sdk/api_model_base'
require 'docspace-webhooks-sdk/version'

require 'docspace-webhooks-sdk/models/contact_payload'
require 'docspace-webhooks-sdk/models/entry_id'
require 'docspace-webhooks-sdk/models/file_entry_payload'
require 'docspace-webhooks-sdk/models/file_payload'
require 'docspace-webhooks-sdk/models/folder_payload'
require 'docspace-webhooks-sdk/models/form_submit_payload'
require 'docspace-webhooks-sdk/models/group_payload'
require 'docspace-webhooks-sdk/models/group_summary_payload'
require 'docspace-webhooks-sdk/models/room_payload'
require 'docspace-webhooks-sdk/models/user_payload'
require 'docspace-webhooks-sdk/models/user_summary_payload'
require 'docspace-webhooks-sdk/models/webhook_config_info'
require 'docspace-webhooks-sdk/models/webhook_envelope'
require 'docspace-webhooks-sdk/models/webhook_event_info'
require 'docspace-webhooks-sdk/models/webhook_target_info'
