# Spec Delta

## MODIFIED Requirements

### Requirement: Count limits of the tariff

The tariff SHALL limit the number of rooms (`room` / `countRoom`), paid users (`manager` / `countPaidUser`:
DocSpace and Room admins), free users (`users` / `countUser`) and AI agents (`aiagent`, v3.6); `-1` means
unlimited. Exceeding a limit SHALL be refused, under a per-portal lock, with 402 "The number of
rooms/admins/AI agents should not exceed <limit>". A 402 from an AI request may instead be a wallet
spending limit (`billing`, "Spending limits on the AI services").

#### Scenario: Which actions count against which limit

- **WHEN** a room is created, created from a template or as a third-party room, copied or restored from the archive; an AI agent is created; an account is invited or promoted to DocSpace admin or Room admin, re-enabled in such a role, provisioned by SSO/LDAP in one, or the invitation link of a paid role is read or created; a User or a Guest is invited
- **THEN** the room actions are checked against `room`, the agent against `aiagent`, the paid-role actions against `manager` and the User or Guest invitation against `users`

#### Scenario: Invitation link of a paid role when the paid limit is reached

- **WHEN** the paid-user limit of the portal is reached and the owner reads the invitation link for DocSpace admin or Room admin
- **THEN** the system answers 402 instead of returning a link that would downgrade the invitee

#### Scenario: Unlimited plan has no quota-reached branch

- **WHEN** the portal's `manager` feature reports `-1`
- **THEN** the limit cannot be reached and the invitation link is returned

#### Scenario: Room limit on creation and on restore

- **WHEN** the number of rooms equals `countRoom` and a room is created, copied or restored from the archive
- **THEN** the operation is refused with 402 "The number of rooms should not exceed <limit>"

#### Scenario: Member added directly above the paid limit

- **WHEN** the paid-user limit is reached and an administrator adds a member with `POST api/2.0/people` and the Room admin or DocSpace admin type
- **THEN** the member is not refused: the account is created with the User type instead

#### Scenario: Bulk invitation and a lapsed tariff

- **WHEN** a bulk invitation would exceed the paid limit, or any counted action is attempted while the tariff is restricted: in the grace period without a delayed payment method (`billing`), or not paid
- **THEN** the bulk invitation is refused as a whole with "The number of admins exceeds the limit: <asked>/<free>.", and the lapsed tariff answers 402 "Your pricing plan does not support this option"

#### Scenario: Paid role for a User or a Guest

- **WHEN** a User or a Guest is invited to a room directly with a paid role, or follows a room invitation link that grants one
- **THEN** the direct invitation is refused regardless of the quota (403, the type rule in `room-role-permissions`), and the link lowers the role to the highest free role of that room type (see `invitations`)

#### Scenario: Counted action in the grace period of a delayed payment method

- **WHEN** a hosted portal is in the grace period, its billing customer has a delayed payment method, and the owner creates a room or invites a User below the limits
- **THEN** the action succeeds as on a paid tariff, and the room limit still answers 402 "The number of rooms should not exceed <limit>" once reached
