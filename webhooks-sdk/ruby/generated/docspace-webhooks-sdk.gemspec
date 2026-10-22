# -*- encoding: utf-8 -*-

# Overrides the generator's gemspec.mustache: models only, so no HTTP client
# dependency.

$:.push File.expand_path("../lib", __FILE__)
require "docspace-webhooks-sdk/version"

Gem::Specification.new do |s|
  s.name        = "docspace-webhooks-sdk"
  s.version     = DocspaceWebhooksSdk::VERSION
  s.platform    = Gem::Platform::RUBY
  s.authors     = ["Ascensio System SIA"]
  s.homepage    = "https://www.onlyoffice.com"
  s.summary     = "Payload models for ONLYOFFICE DocSpace webhook deliveries"
  s.description = "Models for the body of every DocSpace webhook delivery, generated from the webhook wire contract."
  s.license     = "AGPL-3.0-only"
  s.required_ruby_version = ">= 2.7"

  s.files         = Dir["lib/**/*.rb"]
  s.require_paths = ["lib"]
end
