#!/usr/bin/env ruby
# frozen_string_literal: true

# Logs a DocSpace webhook delivery to the console.
#
#     ruby log_webhook.rb                          # replay the shared fixture
#     ruby log_webhook.rb <body> <secret> <sig>
#
# Ruby has no src/ runtime yet, so signature checking lives here.
#
# NOT VERIFIED: no Ruby toolchain was available when this was written. It is
# written against the generated models' actual API (build_from_hash, and the
# EntryId oneOf module), but it has never been executed.

require 'json'
require 'openssl'
require 'pathname'

ROOT     = Pathname.new(__dir__).parent
FIXTURES = ROOT.parent.join('samples')

# The models cross-reference each other through DocspaceWebhooksSdk.const_get at
# deserialization time, so every one of them has to be loaded up front.
Dir[ROOT.join('generated', 'lib', 'docspace-webhooks-sdk', 'models', '*.rb').to_s].sort.each do |f|
  require f
end

SIGNATURE_HEADER = 'x-docspace-signature-256'

# `sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash.
def compute_signature(body, secret)
  "sha256=#{OpenSSL::HMAC.hexdigest('SHA256', secret, body).upcase}"
end

# Constant-time, case-insensitive check over the RAW body. DocSpace emits
# uppercase hex where GitHub emits lowercase, so the comparison folds case.
#
# OpenSSL.secure_compare needs the openssl gem 2.2+ (Ruby 2.7+).
def verify_signature(body, secret, signature)
  return false if signature.nil? || signature.strip.empty?

  OpenSSL.secure_compare(
    compute_signature(body, secret).downcase,
    signature.strip.downcase
  )
end

# Which model a trigger's payload deserializes to.
def payload_class(trigger)
  return DocspaceWebhooksSdk::UserPayload  if trigger.start_with?('user.')
  return DocspaceWebhooksSdk::GroupPayload if trigger.start_with?('group.')
  return DocspaceWebhooksSdk::FormSubmitPayload if trigger == 'form.submit'

  # file.*, folder.*, room.*, agent.* and the other form.* triggers all arrive
  # as the FileEntry<T> base -- no subtype-specific field is ever sent.
  DocspaceWebhooksSdk::FileEntryPayload
end

body_path = ARGV[0] || FIXTURES.join('file.created.json').to_s
secret    = ARGV[1] || File.read(FIXTURES.join('SECRET')).strip
signature = ARGV[2] || File.read(FIXTURES.join('file.created.sig')).strip

# Binary read: the signature covers exactly the bytes that arrived.
body = File.binread(body_path)

unless verify_signature(body, secret, signature)
  warn 'REJECTED: signature does not match'
  exit 1
end

envelope = JSON.parse(body)
trigger  = envelope.dig('event', 'trigger')

if trigger.nil? || trigger.empty?
  warn 'envelope has no event.trigger'
  exit 1
end

event   = DocspaceWebhooksSdk::WebhookEventInfo.build_from_hash(envelope['event'])
config  = DocspaceWebhooksSdk::WebhookConfigInfo.build_from_hash(envelope['webhook'])
payload = payload_class(trigger).build_from_hash(envelope['payload'])

puts trigger
puts '  signature ok'
puts format('  event #%s  at %s  by %s', event.id, event.create_on, event.create_by)
puts format('  subscription #%s "%s"', config.id, config.name)

case payload
when DocspaceWebhooksSdk::FileEntryPayload
  # fileEntryType is the only file/folder discriminator: 1 folder, 2 file.
  # EntryId.build returns the scalar itself, so id needs no unwrapping here
  # (unlike PHP and Kotlin, where the oneOf value is dropped).
  kind = payload.file_entry_type == 2 ? 'file' : 'folder'
  puts format('  %s: %s  id=%s  parent=%s',
              kind, payload.title, payload.id, payload.parent_id)
when DocspaceWebhooksSdk::UserPayload
  puts format('  user: %s <%s>', payload.user_name, payload.email)
when DocspaceWebhooksSdk::GroupPayload
  puts format('  group: %s', payload.name)
end

puts
puts JSON.pretty_generate(envelope['payload']).gsub(/^/, '  ')
