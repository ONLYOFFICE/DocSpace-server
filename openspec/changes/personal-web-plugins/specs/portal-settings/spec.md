# Spec Delta

## ADDED Requirements

### Requirement: Personal domains in the policy header

A signed-in member other than a guest whose request to `getCspSettings` carries the `asc_personal_csp` cookie,
and who has personal domains (of enabled personal plugins that no portal or installation-wide plugin shadows,
while plugins and personal plugins are on), SHALL never get 304 and SHALL find those domains in `header`,
outside its `frame-ancestors`, unless that would pass the header size limit, and never in `domains`. A failed
read of the personal domains SHALL count as none.

#### Scenario: Owner of a personal plugin with domains

- **WHEN** a User has an enabled personal plugin that no portal or installation-wide plugin shadows, declaring `cspDomains` `https://a.example`, and reads the policy with the `asc_personal_csp` cookie and an `If-Modified-Since` the policy has not changed since
- **THEN** the system answers 200 with `https://a.example` in `header` and not in `domains`

#### Scenario: Another member with the cookie

- **WHEN** another User, who has no personal plugin, reads the policy with the `asc_personal_csp` cookie of the first User
- **THEN** `header` does not contain `https://a.example`

#### Scenario: Cookie without personal domains

- **WHEN** a User without enabled personal plugins reads the policy with the `asc_personal_csp` cookie and an `If-Modified-Since` the policy has not changed since
- **THEN** the system answers 304 with an empty body

#### Scenario: Personal domains over the size limit

- **WHEN** a User whose enabled, unshadowed personal plugins declare domains that would push `header` over its size limit reads the policy with the `asc_personal_csp` cookie and an `If-Modified-Since` the policy has not changed since
- **THEN** the system answers 200 with the portal's `header`, which holds none of those domains

## MODIFIED Requirements

### Requirement: Reading the Content Security Policy

`getCspSettings` SHALL answer 200 with `domains` and a non-empty `header` to every caller, an anonymous one
included, and, unless the request carries `asc_personal_csp` and the caller has personal domains (requirement "Personal domains in the policy header"),
SHALL answer 304 with an empty body when the settings did not change since `If-Modified-Since`. A portal with no
stored domains SHALL answer an empty `domains`.

#### Scenario: Every role reads what was configured

- **WHEN** the owner configured a domain and the owner, a DocSpace admin, a Room admin, a User or a Guest reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`

#### Scenario: Anonymous caller

- **WHEN** the owner configured a domain and a caller without a session reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`
