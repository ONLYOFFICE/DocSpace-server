# access-settings Specification

## Purpose

Defines the settings that decide who may enter a portal and how: per-module access and product administrators
(`settings/security`), the password policy, the failed-sign-in limits, the IP restrictions, the two-factor
authentication policy and each person's own authenticator application and backup codes, single sign-on, the keys of
third-party authorization services, the sign-in steps that use a second factor, and the password reminder and
password change of an account. For each operation it states who may call it, which fields it accepts and which status
codes it answers. Login history and active connections are in `history-and-audit`, OAuth applications in
`oauth2-applications`, linking a third-party account in `people-and-groups`, session lifetime in `portal-settings`.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`02_Settings/Security`, `Tfa`, `Sso`, `IpRestrictions`,
`Login`, `Authorization`, `03_Authentication`) and `products/ASC.People/Tests` (`05_Password`), the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`) and the product code where the tests
stop. Evidence per scenario, differences and suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Reading module access

`getWebItemSettingsSecurityInfo` SHALL answer 200 to any signed-in role with one entry per requested `ids` value (`webItemId`, `enabled`, `isSubItem`, `users`, `groups`), and with the registered modules when `ids` is absent. An identifier nobody configured SHALL read as disabled. `users` SHALL hold only people the caller may view. A non-GUID identifier SHALL answer 400; an anonymous caller 401.

#### Scenario: Unknown module

- **WHEN** a signed-in caller asks for a well-formed identifier that has no configuration
- **THEN** the answer is 200 with a single entry for it and `enabled` false

#### Scenario: No identifiers on a build without classic modules

- **WHEN** the owner calls it without `ids`
- **THEN** the answer is 200 with an empty list

#### Scenario: Profiles the caller may not view

- **WHEN** a guest or a user reads a module whose `users` lists a person that role may not view
- **THEN** that person is absent from `users`

#### Scenario: Malformed identifier

- **WHEN** an `ids` value is not a GUID
- **THEN** the answer is 400

### Requirement: Checking a module and listing enabled modules

`getWebItemSecurityInfo` SHALL answer 200 with a boolean that is true only for a registered module readable by the caller. `getEnabledModules` SHALL answer 200 with the modules the caller can open. Both SHALL be open to every signed-in role.

#### Scenario: Unregistered module

- **WHEN** a signed-in caller checks an identifier that is not a registered module
- **THEN** the answer is 200 and false

#### Scenario: No modules registered

- **WHEN** an owner calls `getEnabledModules` on this build, which registers no classic modules
- **THEN** the answer is 200 with an empty list

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls `getEnabledModules`
- **THEN** the answer is 401

### Requirement: Changing module access

`setWebItemSecurity` and `setAccessToWebItems` SHALL be available to the owner and a DocSpace administrator and SHALL answer 200 with the resulting access entries. Other roles SHALL get 403 and a non-GUID `id` or `items` key 400. Per-module access SHALL be refused with 403 on a portal open to everyone.

#### Scenario: Disable one module

- **WHEN** the owner or a DocSpace administrator calls `setWebItemSecurity` with `enabled` false
- **THEN** the answer is 200 with the entry disabled, and a later read reports it disabled

#### Scenario: Several modules at once

- **WHEN** the owner calls `setAccessToWebItems` with one item enabled and one disabled
- **THEN** the answer is 200 and each returned entry has the requested state

#### Scenario: Role below administrator

- **WHEN** a room administrator, user or guest calls `setWebItemSecurity`
- **THEN** the answer is 403 "Access denied"

### Requirement: Reading product administrators

`getProductAdministrators` and `getIsProductAdministrator` SHALL be available only to the owner and a DocSpace administrator; other roles SHALL get 403. The all-zero `productId` SHALL stand for the portal-wide administrators, which include the owner.

#### Scenario: Default administrators

- **WHEN** the owner lists the all-zero product on a new portal
- **THEN** the answer is 200 and the list holds the owner alone

#### Scenario: Check one person

- **WHEN** the owner calls `getIsProductAdministrator` for a plain user
- **THEN** the answer is 200 with `administrator` false, and for the owner or a promoted administrator with `administrator` true

### Requirement: Changing product administrators

`setProductAdministrator` SHALL answer 200 with `productId`, `userId` and `administrator` as stored. Only the owner SHALL grant the portal-wide administrator role or take a role from a DocSpace administrator; another caller SHALL get 403. A hosted portal on the free plan SHALL get 402.

#### Scenario: Owner promotes

- **WHEN** the owner sets `administrator` true on the all-zero product for a member
- **THEN** the answer is 200 and that member appears in `getProductAdministrators`

#### Scenario: Administrator promotes or demotes

- **WHEN** a DocSpace administrator promotes a room administrator to the portal-wide role, or demotes another DocSpace administrator
- **THEN** the answer is 403 "Access denied"

### Requirement: Password policy

`getPasswordSettings` SHALL answer 200 to any signed-in role with `minLength`, `upperCase`, `digits`, `specSymbols` and the regular expressions built from them, and 304 when unchanged since `If-Modified-Since`. A new portal SHALL read minimum length 8 and no other requirement.

#### Scenario: Defaults

- **WHEN** the owner reads the policy of a new portal
- **THEN** the answer is 200 with `minLength` 8 and `upperCase`, `digits`, `specSymbols` false

#### Scenario: Read by a plain user

- **WHEN** a user reads the policy
- **THEN** the answer is 200

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller reads the policy
- **THEN** the answer is 401

### Requirement: Changing the password policy

`updatePasswordSettings` SHALL be available to the owner and a DocSpace administrator and SHALL answer 200 with the stored policy. A `minLength` outside the range the installation allows, whose upper end is 30, SHALL answer 400 naming the field. Other roles SHALL get 403.

#### Scenario: Stricter policy

- **WHEN** the owner sets `minLength` 12 and all three character requirements
- **THEN** the answer is 200 with those values and a later read returns them

#### Scenario: Out of range

- **WHEN** `minLength` is 999 or -5
- **THEN** the answer is 400 and the body names `MinLength`

#### Scenario: Role below administrator

- **WHEN** a room administrator, user or guest calls it
- **THEN** the answer is 403 "Access denied"

### Requirement: Failed sign-in limits

`getLoginSettings`, `updateLoginSettings` and `setDefaultLoginSettings` SHALL be available only to the owner and a DocSpace administrator; other roles SHALL get 403 and an anonymous caller 401. Each SHALL answer 200 with `attemptCount`, `blockTime`, `checkPeriod` and `isDefault`. The defaults SHALL be 5, 60 and 60. `getLoginSettings` SHALL answer 304 when unchanged since `If-Modified-Since`.

#### Scenario: Defaults

- **WHEN** the owner or a DocSpace administrator reads a new portal
- **THEN** the answer is `attemptCount` 5, `blockTime` 60, `checkPeriod` 60 and `isDefault` true

#### Scenario: Custom values

- **WHEN** an administrator saves values that differ from the defaults
- **THEN** the answer and a later read return them with `isDefault` false

#### Scenario: Reset

- **WHEN** an administrator calls `setDefaultLoginSettings`
- **THEN** the answer and a later read return the defaults with `isDefault` true

#### Scenario: Member refused

- **WHEN** a room administrator, user or guest calls any of the three
- **THEN** the answer is 403 "Access denied"

### Requirement: Range of the sign-in limits

`updateLoginSettings` SHALL accept `attemptCount`, `blockTime` and `checkPeriod` only between 1 and 9999, and SHALL answer 400 naming the field otherwise.

#### Scenario: Value too large

- **WHEN** one of the three is 99999
- **THEN** the answer is 400 "The field <name> must be between 1 and 9999."

### Requirement: Sign-in block

Once failed password attempts for an account exceed `attemptCount`, `authenticateMe` SHALL answer 403 "Too many login attempts. Please try again later" even for correct credentials, SHALL record the event in the login history, and SHALL leave other accounts of the portal unaffected.

#### Scenario: Limit used up

- **WHEN** a user has used up the attempts with wrong passwords, each answered 401
- **THEN** the next attempt with the correct password answers 403 and the login history holds the blocked-attempt event

#### Scenario: Another account

- **WHEN** one account is blocked
- **THEN** the owner of the same portal still signs in and receives a token

### Requirement: Reading IP restrictions

`getIpRestrictions` and `readIpRestrictionsSettings` SHALL be available only to the owner and a DocSpace administrator; other roles SHALL get 403 and an anonymous caller 401. The first SHALL return the addresses with their `forAdmin` flag, the second `enable` and `lastModified`. Each SHALL answer 304 when its conditional header shows no change.

#### Scenario: Administrator reads

- **WHEN** the owner or a DocSpace administrator reads either operation
- **THEN** the answer is 200, and `readIpRestrictionsSettings` carries a `lastModified` value

#### Scenario: Member refused

- **WHEN** a room administrator, user or guest reads either operation
- **THEN** the answer is 403

### Requirement: Saving IP restrictions

`saveIpRestrictions` and `updateIpRestrictionsSettings` SHALL behave the same: owner and DocSpace administrator only, other roles 403, anonymous 401; answer 200 echoing `enable` and `ipRestrictions`. `ipRestrictions` SHALL be required (absent 400; explicit null means empty). An omitted `enable` SHALL mean "true when the list is not empty". A non-address entry, or `enable` true with an empty list, SHALL answer 400.

#### Scenario: Disabled restrictions saved

- **WHEN** an administrator saves one address with `enable` false
- **THEN** the answer is 200 with `enable` false and that entry echoed with its `forAdmin` value

#### Scenario: Explicit null list

- **WHEN** an administrator sends `ipRestrictions` null and `enable` false
- **THEN** the answer is 200 with `enable` false and an empty array

#### Scenario: Nothing to enforce

- **WHEN** `enable` is true and the list is empty
- **THEN** the answer is 400

### Requirement: Enforcing IP restrictions

While `enable` is true and at least one address is stored, the server SHALL answer 403 to a signed-in caller whose address matches no applicable entry. An entry with `forAdmin` true SHALL apply only to DocSpace administrators. The portal owner SHALL not be subject to the check.

#### Scenario: Address not listed

- **WHEN** restrictions are enforced and a signed-in member calls from an address no applicable entry lists
- **THEN** the call answers 403

### Requirement: Listing two-factor methods

`getTfaSettings` SHALL answer 200 to a signed-in caller with the methods the portal offers, each with `id`, `title`, `enabled`, `available`, `trustedIps`, `mandatoryUsers` and `mandatoryGroups`. An anonymous caller SHALL get 401.

#### Scenario: Methods listed

- **WHEN** the owner reads the methods
- **THEN** the answer is 200 and the list is not empty

### Requirement: Changing the two-factor policy

`updateTfaSettings` SHALL be available to the owner and a DocSpace administrator; other roles SHALL get 403 and an anonymous caller 401. `type` `App` or `Sms` SHALL switch that method on, switch the other off and answer 200 true. `type` `None` SHALL switch both off and answer 200 false. A method that is not available SHALL answer 405.

#### Scenario: Enable the authenticator method

- **WHEN** the owner or a DocSpace administrator sends `type` App
- **THEN** the answer is 200 true

#### Scenario: Disable

- **WHEN** an administrator sends `type` None
- **THEN** the answer is 200 false

#### Scenario: SMS without a provider

- **WHEN** an administrator sends `type` Sms and no SMS provider is configured
- **THEN** the answer is 405

#### Scenario: Role below administrator

- **WHEN** a room administrator, user or guest calls it
- **THEN** the answer is 403 "Access denied"

### Requirement: Validation of the two-factor policy

`updateTfaSettings` SHALL answer 400 for a malformed identifier in `mandatoryUsers` or a `trustedIps` entry that is not an address or range, and SHALL store nothing in that case, so later sign-ins keep working. Well-formed identifiers of accounts or groups that do not exist SHALL be accepted. A request naming the owner in `id` or `mandatoryUsers` while `id` is not the caller's own SHALL answer 403.

#### Scenario: Malformed list entry

- **WHEN** `mandatoryUsers` holds a value that is not a GUID
- **THEN** the answer is 400 and the body names `$.mandatoryUsers[0]`

#### Scenario: Malformed trusted address

- **WHEN** `trustedIps` holds a value that is not an address
- **THEN** the answer is 400 and the owner can still sign in with a password afterwards

#### Scenario: Unknown account or group

- **WHEN** `mandatoryUsers` or `mandatoryGroups` holds a well-formed identifier that exists nowhere, with `type` None
- **THEN** the answer is 200 false

### Requirement: Switching a two-factor method on ends sessions

When `updateTfaSettings` or `updateTfaSettingsLink` switches a method on, the server SHALL invalidate the sessions of every account of the portal, including the caller's.

#### Scenario: Caller's own token

- **WHEN** the owner enables the authenticator method
- **THEN** the owner's previous token answers 401

#### Scenario: Another administrator

- **WHEN** a DocSpace administrator holds a working session and the owner enables the method
- **THEN** that administrator's next call with the old session answers 401

### Requirement: Who must pass the second factor

With a method on, `authenticateMe` SHALL ask for the second factor instead of issuing a token, answering `tfa` true and `tfaKey`. A request from an address in `trustedIps` SHALL be exempt unless the account is in `mandatoryUsers` or belongs to a group in `mandatoryGroups`; those lists SHALL take precedence over `trustedIps`.

#### Scenario: Listed user from a trusted address

- **WHEN** `trustedIps` covers the request and the account is in `mandatoryUsers`
- **THEN** sign-in answers `tfa` true with `tfaKey`

#### Scenario: Unlisted user from a trusted address

- **WHEN** `trustedIps` covers the request and the account is in neither list
- **THEN** sign-in answers `tfa` false with a token

#### Scenario: Member of a mandatory group

- **WHEN** `trustedIps` covers the request and the account belongs to a group in `mandatoryGroups`
- **THEN** sign-in answers `tfa` true with `tfaKey`

### Requirement: Policy change with a confirmation link

`updateTfaSettingsLink` SHALL apply the same checks and changes as `updateTfaSettings` and SHALL answer 200 with the caller's own confirmation link, or with an empty string when the method was switched off or the caller is exempt.

#### Scenario: Enable with link

- **WHEN** the owner sends `type` App
- **THEN** the answer is 200 with a non-empty link

#### Scenario: SMS without a provider

- **WHEN** an administrator sends `type` Sms and no SMS provider is configured
- **THEN** the answer is 405

#### Scenario: Refused callers

- **WHEN** a room administrator, user or guest calls it, or an anonymous caller does
- **THEN** the answer is 403 "Access denied" and 401 respectively

### Requirement: Confirmation data of the second factor

`getTfaConfirmData` SHALL answer 200 to a signed-in caller with the `url` of the caller's next two-factor step and, for the authenticator method, `cookieName` and `cookieValue`. An account with an application linked SHALL get the re-verification link, others the activation link. With no second factor applying, the body SHALL be empty.

#### Scenario: Linked account

- **WHEN** a person with a linked application calls it
- **THEN** the answer is 200 with a re-verification `url`, `cookieName` and a non-empty `cookieValue`

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls it
- **THEN** the answer is 401

### Requirement: Authenticator setup and code validation take a confirmation link

`tfaAppGenerateSetupCode` and `tfaValidateAuthCode` SHALL be authorised only by the confirmation link of the matching two-factor step. A caller with an ordinary signed-in token SHALL get 403 and an anonymous caller 401. Through the link, setup SHALL answer 405 when the method is off or an application is already linked, and validation SHALL answer 400 for a wrong code.

#### Scenario: Signed-in token

- **WHEN** a signed-in caller calls `tfaAppGenerateSetupCode` or `tfaValidateAuthCode` with an ordinary token
- **THEN** the answer is 403, whatever the portal's two-factor state

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls either
- **THEN** the answer is 401

### Requirement: Backup codes

`getTfaAppCodes` and `updateTfaAppCodes` SHALL act on the caller's own account for every role once an application is linked. The first SHALL return each code with `isUsed`; the second SHALL replace the set and return the new one. With the method not available or no application linked, each SHALL answer 405. An anonymous caller SHALL get 401.

#### Scenario: Read and regenerate

- **WHEN** an owner, DocSpace administrator, room administrator, user or guest with a linked application calls both
- **THEN** each answers 200 with a non-empty list, and the regenerated codes differ from the previous ones

#### Scenario: Nothing linked

- **WHEN** the caller has no linked application or the method is off
- **THEN** each answers 405

### Requirement: A backup code signs in once

`authenticateMeFromBodyWithCode` SHALL accept an unused backup code in place of an application code, and SHALL mark it `isUsed`. A second use of the same code SHALL answer 401.

#### Scenario: Single use

- **WHEN** a person signs in with a backup code and then repeats the same code
- **THEN** the first call issues a token, the second answers 401, and `getTfaAppCodes` reports the code `isUsed`

### Requirement: Unlinking an authenticator application

`unlinkTfaApp` SHALL let the owner unlink another person's application: answer 200 with an empty string, send that person a letter and invalidate that person's sessions. Any other caller naming another person SHALL get 403 "No permissions to perform this action". A request whose `id` resolves to no account SHALL answer 403, a target with no linked application 405, a malformed or empty `id` 400, an anonymous caller 401.

#### Scenario: Owner unlinks another person

- **WHEN** the owner unlinks the application of a member
- **THEN** the answer is 200 and that member's previous session answers 401

#### Scenario: Other roles

- **WHEN** a DocSpace administrator, room administrator, user or guest names another person
- **THEN** the answer is 403

#### Scenario: Request without an identifier

- **WHEN** the owner sends a request with no `id`
- **THEN** the answer is 403

#### Scenario: Nothing to unlink

- **WHEN** the owner names a member who has no linked application
- **THEN** the answer is 405

### Requirement: Reading single sign-on settings

`getSsoSettingsV2` SHALL answer an anonymous caller 200 with only `hideAuthPage`, false when the plan has no single sign-on. A signed-in owner or DocSpace administrator SHALL get the full settings, with the default login label when none is stored. Other signed-in roles SHALL get 403, and on a hosted portal whose plan has no single sign-on an administrator SHALL get 402.

#### Scenario: Anonymous sign-in page

- **WHEN** an anonymous caller reads the settings
- **THEN** the answer is 200 with no `idpSettings` and no `spLoginLabel`

#### Scenario: Owner

- **WHEN** the owner reads the settings of a new portal
- **THEN** the answer is 200 with `spLoginLabel` "Single Sign-on"

#### Scenario: Member refused

- **WHEN** a user or room administrator reads the settings
- **THEN** the answer is 403

### Requirement: Single sign-on template and constants

`getDefaultSsoSettingsV2` SHALL be available to the owner and a DocSpace administrator and SHALL return the built-in template with single sign-on disabled; other roles SHALL get 403 and an anonymous caller 401. `getSsoSettingsV2Constants` SHALL answer 200 to every signed-in role with the accepted SAML values, and 401 to an anonymous caller.

#### Scenario: Template

- **WHEN** the owner or a DocSpace administrator reads the template
- **THEN** `enableSso` is false, the entity ID is empty and `spLoginLabel` is "Single Sign-on"

#### Scenario: Constants

- **WHEN** a user reads the constants
- **THEN** the answer is 200 and includes the SAML 2.0 HTTP POST binding value

### Requirement: Saving single sign-on settings

`saveSsoSettingsV2` SHALL take the configuration as a JSON string in `serializeSettings`, be available only to the owner and a DocSpace administrator (others 403, anonymous 401), and answer 200 with what was stored. An empty `serializeSettings` SHALL answer 400. `spLoginLabel` SHALL be cut to 100 characters. A `usersType` other than user, room administrator or DocSpace administrator SHALL become user.

#### Scenario: Valid configuration

- **WHEN** the owner saves a complete configuration with single sign-on enabled
- **THEN** the answer and a later read return `enableSso` true and the saved entity ID and label

#### Scenario: Long label

- **WHEN** `spLoginLabel` is longer than 100 characters
- **THEN** the stored label has 100 characters

#### Scenario: Recognised and unrecognised user types

- **WHEN** `usersType` is a recognised type
- **THEN** it is kept, and an unrecognised value is stored as the user type

#### Scenario: Empty settings

- **WHEN** `serializeSettings` is empty
- **THEN** the answer is 400

### Requirement: Required single sign-on fields

`saveSsoSettingsV2` SHALL refuse with 400, without storing, settings that are not valid JSON, a configuration with no IdP settings or no IdP entity ID, with an SSO URL that is missing or not absolute HTTP or HTTPS, with an SLO URL that is present and not absolute HTTP or HTTPS, or with no field mapping or no first name, last name or email in it.

#### Scenario: Missing entity ID

- **WHEN** the configuration has no IdP entity ID
- **THEN** the answer is 400 and the stored settings do not change
- **AND** a missing SSO URL, a malformed SLO URL, an incomplete or missing field mapping, missing IdP settings and settings that are not valid JSON answer 400 too

### Requirement: Switching single sign-on off

`resetSsoSettingsV2` SHALL be available only to the owner and a DocSpace administrator (others 403, anonymous 401) and SHALL store and return the defaults. Both a reset and a save with single sign-on disabled SHALL convert the accounts created through single sign-on into ordinary accounts.

#### Scenario: Reset

- **WHEN** an administrator resets a saved configuration
- **THEN** the answer is 200 and a later read has `enableSso` false and an empty entity ID

### Requirement: Third-party service keys

`getAuthServices` SHALL answer the owner and a DocSpace administrator 200 with each third-party provider that has manageable keys and its last-saved key values; other roles SHALL get 403 and an anonymous caller 401.

#### Scenario: Catalogue

- **WHEN** an administrator reads it after keys of one provider were saved
- **THEN** each entry has `name` and `title`, the list includes s3, dropbox, box, google, googlecloud and telegram, and the saved provider shows the saved value

#### Scenario: Member refused

- **WHEN** a room administrator, user or guest reads it
- **THEN** the answer is 403 "Access denied"

### Requirement: Saving third-party service keys

`saveAuthKeys` SHALL be available to the owner and a DocSpace administrator (others 403, anonymous 401) and SHALL answer 200 with whether the keys changed. All `props` values empty SHALL clear the provider. Keys the provider rejects SHALL be cleared and answer 400. An unknown provider or one whose keys cannot be set SHALL answer 403, and a paid provider outside the plan 402.

#### Scenario: Save

- **WHEN** the owner or a DocSpace administrator saves new keys for a provider
- **THEN** the answer is 200 true

#### Scenario: Keys rejected by the provider

- **WHEN** the provider validates the submitted keys and rejects them
- **THEN** the answer is 400 and the provider's keys are empty afterwards

### Requirement: Testing an external database connection

`testExternalDatabaseConnection` SHALL be available to the owner and a DocSpace administrator (others 403, anonymous 401) and SHALL probe the connection without saving it. A failed connection SHALL still answer 200, with `success` false and a non-empty `error`. SQLite SHALL be reported as a failure outside a self-hosted installation.

#### Scenario: Unreachable database

- **WHEN** an administrator tests settings that cannot connect
- **THEN** the answer is 200 with `success` false and a non-empty `error`

#### Scenario: Member refused

- **WHEN** a room administrator, user or guest calls it
- **THEN** the answer is 403 "Access denied"

### Requirement: Sign-in with a second-factor code

`authenticateMeFromBodyWithCode` SHALL be open to anonymous callers, read the code from the body's `code`, and answer 200 with a new `token` for valid credentials and a valid code. Rejected credentials, a wrong code, an account the second factor does not apply to and an empty body SHALL all answer 401 "User authentication failed".

#### Scenario: Valid code

- **WHEN** the owner or a member with a linked application sends valid credentials and a current code
- **THEN** the answer is 200 with a token, and signing in again issues a different token

#### Scenario: Second factor not required

- **WHEN** valid credentials and a code are sent while no second factor applies to the account
- **THEN** the answer is 401

#### Scenario: Wrong credentials or empty body

- **WHEN** the credentials are wrong or the body is empty
- **THEN** the answer is 401 "User authentication failed"

### Requirement: Registering a mobile phone

`saveMobilePhone` SHALL be authorised only by a phone-activation confirmation link and SHALL answer 200 with `sms` true, the masked phone number and the code's expiry, and no token. An anonymous caller SHALL get 401, and a caller with an ordinary signed-in token 403 with the stored phone unchanged.

#### Scenario: Signed-in token

- **WHEN** the owner calls it with an ordinary token and any `mobilePhone` value
- **THEN** the answer is 403 and the profile's `mobilePhone` is unchanged

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls it
- **THEN** the answer is 401

### Requirement: Resending the SMS code

`sendSmsCode` SHALL be open to anonymous callers and SHALL check credentials the same way as `authenticateMe`: wrong or empty credentials, an unknown account and an empty body SHALL answer 401 "User authentication failed". With SMS on for the account it SHALL answer 200 with `sms` true, the masked phone number and the expiry, and no token. Valid credentials when SMS is not available on the portal or not required for the account SHALL answer 405.

#### Scenario: Rejected credentials

- **WHEN** the password is wrong, the account is unknown, the credentials are empty or the body is empty
- **THEN** the answer is 401 "User authentication failed"

#### Scenario: SMS not in use

- **WHEN** the credentials are valid and the account signs in without SMS codes
- **THEN** the answer is 405 and no code is sent

### Requirement: Password reminder for a signed-in caller

`sendUserPassword` SHALL answer a signed-in caller 200 with a message naming the address when the caller asks for their own account, when a DocSpace administrator asks for a member who is neither owner nor administrator, or when the owner asks. Every other pairing SHALL answer 403 "Access denied".

#### Scenario: Own account

- **WHEN** an owner, DocSpace administrator, room administrator, user or guest asks for their own address
- **THEN** the answer is 200 with the message that the instruction was sent to that address

#### Scenario: Administrator for a member

- **WHEN** a DocSpace administrator asks for a room administrator, user or guest
- **THEN** the answer is 200

#### Scenario: Refused pairing

- **WHEN** a DocSpace administrator asks for the owner or another DocSpace administrator, or a room administrator, user or guest asks for anyone else
- **THEN** the answer is 403 "Access denied"

### Requirement: Password reminder from the sign-in page

`sendUserPassword` SHALL answer an anonymous caller 200 with a message saying the instruction was sent if a user with that address exists, in the same form whether or not one does. When the installation uses a CAPTCHA, a request that does not pass it SHALL answer 403.

#### Scenario: Anonymous request

- **WHEN** an anonymous caller submits an address
- **THEN** the answer is 200 with the conditional message

### Requirement: Changing a password through a confirmation link

`changeUserPassword` SHALL be authorised only by a password-change or activation confirmation link and SHALL answer 200 with the profile, invalidate that account's sessions and notify the person. No password sent, or one failing the portal policy, SHALL answer 400; no such account 404; an inactive account, or the owner's password changed by someone else, 403.

#### Scenario: Policy not met

- **WHEN** the new `password` does not meet the portal policy
- **THEN** the answer is 400 and the password does not change

#### Scenario: Owner's password

- **WHEN** a link issued to a different account targets the owner's `userId`
- **THEN** the answer is 403
