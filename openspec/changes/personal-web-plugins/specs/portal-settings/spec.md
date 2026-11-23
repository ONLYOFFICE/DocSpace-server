# Spec Delta

## MODIFIED Requirements

### Requirement: Reading the Content Security Policy

`getCspSettings` SHALL answer 200 with `domains` and a non-empty `header` to every caller, an anonymous one
included, and SHALL answer 304 with an empty body when the settings did not change since `If-Modified-Since`.
A portal with no stored domains SHALL answer an empty `domains`. A signed-in member with enabled personal plugins
that declare domains, none shadowed, whose request carries the `asc_personal_csp` cookie SHALL get those domains
in `header`, not in `domains`, and never 304.

#### Scenario: Every role reads what was configured

- **WHEN** the owner configured a domain and the owner, a DocSpace admin, a Room admin, a User or a Guest reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`

#### Scenario: Anonymous caller

- **WHEN** the owner configured a domain and a caller without a session reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`

#### Scenario: Owner of a personal plugin with domains

- **WHEN** a User has an enabled personal plugin declaring `cspDomains` `https://a.example` and reads the policy with the `asc_personal_csp` cookie and `If-Modified-Since`
- **THEN** the system answers 200 with `https://a.example` in `header` and not in `domains`

#### Scenario: Another member with the cookie

- **WHEN** another User, who has no personal plugin, reads the policy with the `asc_personal_csp` cookie of the first User
- **THEN** `header` does not contain `https://a.example`

#### Scenario: Cookie without personal domains

- **WHEN** a User without enabled personal plugins reads the policy with the `asc_personal_csp` cookie and an `If-Modified-Since` the policy has not changed since
- **THEN** the system answers 304 with an empty body
