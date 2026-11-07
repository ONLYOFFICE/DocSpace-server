# oauth2-applications Specification

## Purpose

Defines the OAuth 2.0 applications a portal registers with the server as the API exposes them: the short-lived
signature a signed-in member obtains to talk to the identity service, who may create, read, update, activate,
delete a client and regenerate its secret, the fields of a client and their validation, listing and paging, the
scope catalogue, a user's consents, the authorization and token endpoints as documented operations, and how the
developer tools setting gates all of it. The applications are stored and served by a separate identity service,
not by the portal API; only `generateJwtToken` belongs to the portal API. API keys, webhooks and reading or
changing the developer tools setting itself are in `webhooks-and-api-keys`; linking a third-party account to a
profile is in `people-and-groups`.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`08_OAuth2`, `05_Security/OAuth2`) and the OpenAPI
contract `sdk/docspace-api-spec/docspace-backend.yaml`. No test drives the authorization endpoints, so those
requirements come from the contract alone; the evidence per scenario, the differences and the suspected defects
are in `verification.md` next to this file.

## Requirements

### Requirement: Identity signature for the calling member

`generateJwtToken` (`GET /api/2.0/security/oauth2/token`) SHALL issue to any authenticated member, whatever their
type, a signed JWT that describes the caller and expires five minutes after it was issued. It takes no parameters
and cannot be issued for somebody else. It identifies the caller to the identity service and is not a credential
for the portal API. An anonymous call SHALL answer 401.

#### Scenario: Every member type obtains a signature

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin, a User or a Guest calls `generateJwtToken`
- **THEN** the call answers 200 with a three-part JWT

#### Scenario: Anonymous caller

- **WHEN** a caller who is not signed in calls `generateJwtToken`
- **THEN** the call answers 401

### Requirement: Signature required by the application API

Every operation on `/api/2.0/oauth2/clients` and `/api/2.0/oauth2/scopes`, except `getPublicClientInfo`, SHALL
require a valid `x-signature` value. A request without one SHALL answer 403, not 401, even when the same member
holds a valid portal session.

#### Scenario: No signature

- **WHEN** a caller without a signature calls `createClient`, `updateClient`, `deleteClient`, `deleteUserClients`, `deleteTenantClients`, `regenerateSecret`, `changeActivation`, `revokeUserClient`, `getClient`, `getClientInfo`, `getClients`, `getClientsInfo`, `getConsents` or `getScopes`
- **THEN** the call answers 403

#### Scenario: Portal session without a signature

- **WHEN** a Guest signed in to the portal calls a client operation or `getScopes` without a signature
- **THEN** the call answers 403

### Requirement: Registering a client

`createClient` (`POST /api/2.0/oauth2/clients`) SHALL let the DocSpace Owner, a DocSpace admin, a Room admin and a
User register a client in their tenant and answer 201 with the stored client: generated `client_id` and
`client_secret`, the submitted fields, `enabled` true, `created_on` and `created_by` set to the caller.

#### Scenario: Member registers a client

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User posts a valid client
- **THEN** the call answers 201 and the response repeats `name`, `description`, `website_url`, `terms_url`, `policy_url`, `is_public`, `redirect_uris`, `allowed_origins` and `scopes`
- **AND** `client_id` and `client_secret` are present, `enabled` is true and `created_on` and `created_by` are set

#### Scenario: Single logout address becomes a list

- **WHEN** a client is created with `logout_redirect_uri`
- **THEN** the response carries that address in the `logout_redirect_uris` array

#### Scenario: Tenant client cap

- **WHEN** the deployment caps the clients of a tenant and the cap is reached
- **THEN** `createClient` answers 400

### Requirement: Client field validation on create

`createClient` SHALL answer 400 with an `errors` array of `field`, `code` and `message` entries when a field is
invalid: `name` outside 3 to 256 characters, `scopes` empty, `logo` missing, or a value that is not a URL in
`redirect_uris`, `allowed_origins`, `website_url`, `terms_url`, `policy_url` or `logout_redirect_uri`. Every named
scope has to exist in the scope catalogue. `redirect_uris` and `allowed_origins` hold 1 to 12 entries.

#### Scenario: Name out of bounds

- **WHEN** `name` has 2 or 257 characters
- **THEN** the call answers 400 with an `errors` entry for `name` saying the length has to be between 3 and 256 characters

#### Scenario: Empty scopes

- **WHEN** `scopes` is empty
- **THEN** the call answers 400 with an `errors` entry for `scopes`

#### Scenario: Missing logo

- **WHEN** the body has no `logo`
- **THEN** the call answers 400 with an `errors` entry for `logo`

#### Scenario: Value that is not a URL

- **WHEN** `redirect_uris`, `allowed_origins`, `website_url`, `terms_url`, `policy_url` or `logout_redirect_uri` holds a value that is not a URL
- **THEN** the call answers 400 with an `errors` entry for that field

#### Scenario: Unknown scope

- **WHEN** `scopes` names a scope that is not in the catalogue
- **THEN** the call answers 400

### Requirement: Which clients a caller sees

The DocSpace Owner and a DocSpace admin SHALL see and manage every client of the tenant; a Room admin and a User
only the clients they created. A single-client operation on a client outside the caller's set SHALL answer 404,
the same as for an unknown id, so absence and lack of access cannot be told apart.

#### Scenario: Owner reads another member's client

- **WHEN** the DocSpace Owner calls `getClient` or `getClientInfo` for a client a User created
- **THEN** the call answers 200 with that client

#### Scenario: Plain member and another member's client

- **WHEN** a User calls `getClient`, `getClientInfo`, `updateClient`, `deleteClient`, `regenerateSecret` or `changeActivation` for a client somebody else created
- **THEN** the call answers 404

#### Scenario: Unknown client

- **WHEN** a signed caller calls `getClient`, `getClientInfo`, `updateClient`, `deleteClient`, `regenerateSecret` or `changeActivation` with an id no client has
- **THEN** the call answers 404

### Requirement: Guests hold no clients

A Guest SHALL NOT read, list or manage clients: with the Guest's own signature `getClient`, `getClientInfo`,
`getClients` and `getClientsInfo` SHALL answer 403, and so SHALL `createClient`, `updateClient`, `deleteClient`,
`deleteUserClients`, `deleteTenantClients`, `regenerateSecret` and `changeActivation`.

#### Scenario: Guest reads with their own signature

- **WHEN** a Guest with their own signature calls `getClient`, `getClientInfo`, `getClients` or `getClientsInfo`
- **THEN** the call answers 403

#### Scenario: Guest manages with their own signature

- **WHEN** a Guest with their own signature calls `createClient`, `updateClient`, `deleteClient`, `deleteUserClients`, `deleteTenantClients`, `regenerateSecret` or `changeActivation`
- **THEN** the call answers 403

### Requirement: Full client record

`getClient` (`GET /api/2.0/oauth2/clients/{clientId}`) SHALL return the whole stored record of a client the caller
may see, including `client_secret`, `redirect_uris`, `allowed_origins`, `logout_redirect_uris`, `enabled`, `tenant`
and the audit fields.

#### Scenario: Member reads their client

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User reads a client they created
- **THEN** the call answers 200 with its `client_id`, `name`, `website_url`, `scopes`, a non-empty `client_secret` and `enabled` true

### Requirement: Consent-facing client information

`getClientInfo` (`GET /api/2.0/oauth2/clients/{clientId}/info`) SHALL return the consent-facing subset of a client
the caller may see: name, description, logo, website, terms and policy URLs, authentication methods, scopes and
audit fields, and no `client_secret`, `redirect_uris` or `allowed_origins`.

#### Scenario: Member reads client info

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User reads the info of a client they created
- **THEN** the call answers 200 with its `client_id`, `name`, `website_url` and `scopes`

### Requirement: Public client information

`getPublicClientInfo` (`GET /api/2.0/oauth2/clients/{clientId}/public/info`) SHALL answer without a signature and
without a tenant or creator check, returning the same consent-facing subset as `getClientInfo`, so a login or
consent page can render a client before the user is known.

#### Scenario: Read without a signature

- **WHEN** a caller without a signature reads the public info of an existing client
- **THEN** the call answers 200 with its `client_id`

#### Scenario: Read by a member who did not create the client

- **WHEN** a Guest or a User with a signature reads the public info of a client somebody else created
- **THEN** the call answers 200 with its `client_id`

### Requirement: Listing clients

`getClients` (`GET /api/2.0/oauth2/clients`) with full records and `getClientsInfo` (`GET
/api/2.0/oauth2/clients/info`) with the consent-facing subset SHALL return one page of the clients the caller may
see, newest first, as `data` and `limit`. The DocSpace Owner and a DocSpace admin get the whole tenant; a Room
admin and a User only their own.

#### Scenario: Full list carries the secret

- **WHEN** the DocSpace Owner calls `getClients` after creating a client
- **THEN** the entry of that client carries `client_secret`, `name`, `website_url` and `scopes`

#### Scenario: Info list carries no secret

- **WHEN** the DocSpace Owner calls `getClientsInfo` after creating a client
- **THEN** the entry of that client carries `name`, `website_url` and `scopes` and has no `client_secret` property

#### Scenario: Administrator sees the tenant

- **WHEN** a DocSpace admin lists clients while the DocSpace Owner has one
- **THEN** both lists contain the Owner's client

#### Scenario: Plain member sees their own

- **WHEN** a Room admin or a User lists clients while the DocSpace Owner has one
- **THEN** neither list contains the Owner's client

#### Scenario: Member with no clients

- **WHEN** a member who created no client lists clients
- **THEN** the call answers 200 with an empty `data`

### Requirement: Keyset paging of client lists

`getClients` and `getClientsInfo` SHALL page by keyset: `limit` is 1 to 50, and `last_client_id` with
`last_created_on` from the previous page ask for the next one. `limit` SHALL default to 30 on `getClients` and be
required on `getClientsInfo`. A `limit` out of range or a `last_created_on` that is not a date-time SHALL answer 400.

#### Scenario: Requested limit is echoed

- **WHEN** either list is called with `limit` 50
- **THEN** the response has `limit` 50

#### Scenario: Limit outside the range

- **WHEN** either list is called with a `limit` outside 1 to 50, or `getClientsInfo` without one
- **THEN** the call answers 400

### Requirement: Updating a client

`updateClient` (`PUT /api/2.0/oauth2/clients/{clientId}`) SHALL change the mutable settings of a client the caller
may manage and answer 200 with an empty body. The body carries `name`, `logo`, `scopes`, `redirect_uris` and
`allowed_origins`, optionally `description`, `allow_pkce` and `is_public`. The client id, secret, tenant, creator
and the website, terms, policy and logout addresses SHALL NOT change this way.

#### Scenario: Member updates their client

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User updates a client they created with a valid body
- **THEN** the call answers 200 with an empty body

#### Scenario: Invalid origin

- **WHEN** `allowed_origins` holds a value that is not a URL
- **THEN** the call answers 400

### Requirement: Activating and deactivating a client

`changeActivation` (`PATCH /api/2.0/oauth2/clients/{clientId}/activation`, body `status`) SHALL enable or disable a
client the caller may manage and answer 200 with an empty body. A disabled client keeps its registration and the
tokens and consents it already holds but SHALL NOT obtain new tokens. A missing `status` SHALL answer 400.

#### Scenario: Member deactivates their client

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User sends `status` false for a client they created
- **THEN** the call answers 200

#### Scenario: Reactivation

- **WHEN** the owner of a deactivated client sends `status` true
- **THEN** the call answers 200

### Requirement: Regenerating a client secret

`regenerateSecret` (`PATCH /api/2.0/oauth2/clients/{clientId}/regenerate`) SHALL issue a new secret for a client the
caller may manage and return it as `client_secret`, with nothing else. The previous secret SHALL stop working at
once, while tokens already issued keep working.

#### Scenario: Member regenerates their secret

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User regenerates the secret of a client they created
- **THEN** the call answers 200 with a non-empty `client_secret` that differs from the one issued at creation

### Requirement: Deleting a client

`deleteClient` (`DELETE /api/2.0/oauth2/clients/{clientId}`) SHALL permanently delete a client the caller may manage
and answer 200 with an empty body. The authorizations and consents issued for it SHALL be removed too, after the
call has returned.

#### Scenario: Member deletes their client

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User deletes a client they created
- **THEN** the call answers 200

### Requirement: Deleting all of a caller's clients

`deleteUserClients` (`DELETE /api/2.0/oauth2/clients`) SHALL delete every client the caller created and only those,
also for an administrator, and answer 200 with an empty body, also when there is nothing to delete.

#### Scenario: Member wipes their clients

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User calls `deleteUserClients`
- **THEN** the call answers 200

#### Scenario: The clients are gone

- **WHEN** a member with clients calls `deleteUserClients` and then `getClients`
- **THEN** the list is empty

#### Scenario: Nothing to delete

- **WHEN** a member with no clients calls `deleteUserClients`
- **THEN** the call answers 200

#### Scenario: Other members' clients stay

- **WHEN** a User calls `deleteUserClients` while the DocSpace Owner has a client
- **THEN** the Owner can still read that client

### Requirement: Deleting all clients of a tenant

`deleteTenantClients` (`DELETE /api/2.0/oauth2/clients/tenant`) SHALL let the DocSpace Owner and a DocSpace admin
delete every client of the tenant, whoever created it, and answer 200 with an empty body, also when there is
nothing to delete. A Room admin or a User SHALL be refused with 403.

#### Scenario: Administrator wipes the tenant

- **WHEN** the DocSpace Owner or a DocSpace admin calls `deleteTenantClients`
- **THEN** the call answers 200

#### Scenario: The clients are gone

- **WHEN** the DocSpace Owner has clients, calls `deleteTenantClients` and then `getClients`
- **THEN** the list is empty

#### Scenario: Empty tenant

- **WHEN** no client exists and an administrator calls `deleteTenantClients`
- **THEN** the call answers 200

#### Scenario: Lesser role

- **WHEN** a Room admin or a User with their own signature calls `deleteTenantClients`
- **THEN** the call answers 403

### Requirement: Listing a user's consents

`getConsents` (`GET /api/2.0/oauth2/clients/consents`) SHALL return one page of the caller's own consents,
for every role but a Guest, with no parameter that widens it to another user, each with the scopes granted, the moment of
the last change and the client's consent-facing details. `limit` is required, 1 to 50, and `last_modified_on` asks
for the next page.

#### Scenario: No consents yet

- **WHEN** a Guest with their own signature calls `getConsents`
- **THEN** the system answers 500 (bug 81729)

#### Scenario: Member without consents

- **WHEN** a member who has authorized no client calls `getConsents` with `limit` 50
- **THEN** the call answers 200 with an empty `data` and `limit` 50

#### Scenario: Limit missing or out of range

- **WHEN** `limit` is missing or outside 1 to 50
- **THEN** the call answers 400

#### Scenario: Authorization service unreachable

- **WHEN** the authorization service cannot be reached
- **THEN** the call answers 503

### Requirement: Revoking a consent

`revokeUserClient` (`DELETE /api/2.0/oauth2/clients/{clientId}/revoke`) SHALL revoke the caller's own consent for a
client and answer 200 with an empty body, also when the caller has no consent for it or no such client exists. Other
users' consents and the client itself SHALL stay. The user has to authorize the client again afterwards.

#### Scenario: No prior consent

- **WHEN** a member who never authorized a client revokes it
- **THEN** the call answers 200

#### Scenario: Unknown client

- **WHEN** a member revokes an id no client has
- **THEN** the call answers 200

### Requirement: Scope catalogue

`getScopes` (`GET /api/2.0/oauth2/scopes`) SHALL return, to any caller with a valid signature, the scopes a client
may request, each with `name`, `group` and `type`, in alphabetical order with `openid` first. The catalogue does not
depend on which clients exist.

#### Scenario: Member reads the catalogue

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User calls `getScopes`
- **THEN** the list contains `openid` (group and type `openid`) and `files:read`, `files:write`, `rooms:read`, `rooms:write`, `accounts:read`, `accounts:write`, with the part before the colon as group and the part after it as type

### Requirement: Authorization request

`authorizeOAuth` (`GET /oauth2/authorize`) SHALL start the authorization code flow for `response_type` code, a
registered `client_id`, one of its registered `redirect_uri` values and a space-separated `scope`. It SHALL answer
302 to the login page without a valid signature, to the consent page without a consent, and otherwise to the
redirect URI with a code. Invalid parameters SHALL answer 400.

#### Scenario: Consent already given

- **WHEN** a signed user with a consent for the requested scopes sends a valid request
- **THEN** the call answers 302 to the client's redirect URI with an authorization code and the original `state`, if one was sent

#### Scenario: Signed-out user

- **WHEN** a request carries no valid signature
- **THEN** the call answers 302 to the portal login page, carrying the client id so the flow can resume

#### Scenario: Client that cannot follow redirects

- **WHEN** the request carries the `X-Disable-Redirect` header
- **THEN** the call answers 200 with an empty body and the target URL in the `X-Redirect-URI` response header

### Requirement: Submitting a consent

`submitConsent` (`POST /oauth2/authorize`, multipart form `client_id`, `state`, `scope`) SHALL store the consent per
user and client for the agreed scopes and answer 302 to the client's redirect URI with an authorization code. A later
authorization request for the same scopes SHALL no longer stop at the consent page.

#### Scenario: User agrees

- **WHEN** a signed user submits the consent form for the client of the authorization request
- **THEN** the call answers 302 to the client's redirect URI with an authorization code

#### Scenario: Invalid form

- **WHEN** the form parameters are invalid
- **THEN** the call answers 400

### Requirement: Exchanging an authorization code

`exchangeToken` (`POST /oauth2/token`, form-encoded `grant_type`, `code`, `redirect_uri`, `client_id`,
`client_secret`) SHALL answer 200 with `access_token`, `token_type` Bearer and `expires_in`, plus `refresh_token`
when the client is registered for that grant. The code is single use and `redirect_uri` has to match the one that
obtained it. A client registered for PKCE sends a code verifier instead of a secret.

#### Scenario: Valid exchange

- **WHEN** a client presents a valid code, the same redirect URI and its credentials
- **THEN** the call answers 200 with the token fields

#### Scenario: Client authentication fails

- **WHEN** the client id is unknown or the secret does not match
- **THEN** the call answers 401

#### Scenario: Bad code

- **WHEN** the code is malformed, unknown, expired or already redeemed
- **THEN** the call answers 400

### Requirement: Developer tools setting gates applications

The signature SHALL state whether the portal's `limitedAccessForUsers` setting leaves OAuth 2.0 applications open to
non-administrators. While it is `true`, a Room admin and a User SHALL be treated by the application API as a Guest is,
while the DocSpace Owner and a DocSpace admin are unaffected. The setting takes effect on the next signature issued.

#### Scenario: Restricted member

- **WHEN** `limitedAccessForUsers` is `true` and a Room admin or a User with a fresh signature calls `createClient`
- **THEN** the call is refused as for a Guest

#### Scenario: Administrator under the restriction

- **WHEN** `limitedAccessForUsers` is `true` and the DocSpace Owner or a DocSpace admin manages clients
- **THEN** the calls answer as they do without the restriction

### Requirement: Clients follow their owner and portal

Deleting a member who is not a Guest, or the reassign operation for such a member, SHALL delete the clients that
member created. Removing a portal SHALL delete all clients of its tenant; on an automatic removal a failure of that
step SHALL NOT stop the removal.

#### Scenario: Member deleted

- **WHEN** a member who is not a Guest is deleted
- **THEN** the identity service is told to delete the clients that member created

#### Scenario: Portal removed by its owner

- **WHEN** the owner removes the portal and deleting the tenant's clients fails
- **THEN** the failure is reported to the owner

#### Scenario: Portal removed automatically

- **WHEN** a portal is removed by the retention policy and deleting its clients fails
- **THEN** the removal continues
