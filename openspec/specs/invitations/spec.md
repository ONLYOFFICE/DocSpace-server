# invitations Specification

## Purpose

Defines how people get onto a DocSpace portal and into its rooms: inviting members by email and by type,
adding them directly, the Pending invite / Active / Disabled statuses and the activation emails, the
portal invitation links kept per user type, room invitations by account, group or email address, the
invitation link of a room, what happens when somebody follows an invitation link, the portal Invitation
Settings, the invitation quota and the daily email rate limit, the paid-user limit, sharing a guest with
another manager, and the encrypted email carried by confirmation links.

Who may invite whom is already specified elsewhere and is only referenced here: which user types may invite
which types to the portal and who may invite to a room are in `user-type-permissions` (Inviting users by
type, User and Guest cannot invite); which room role may invite and which roles may be handed out are in
`room-role-permissions`.

Sources: the integration tests (`products/ASC.People/Tests`, `products/ASC.Files/Tests`,
`web/ASC.Web.Api.Tests`), the OpenAPI document `sdk/docspace-api-spec/docspace-backend.yaml`, the product
release notes v2.0-v4.0 and, for the points the tests do not reach, the server code (October 2026). Where
the release notes and the server disagree, this specification follows the server; the evidence for every
scenario and the differences are listed in `verification.md` next to this file. HTTP status codes are
stated where the tests assert them.

## Requirements

### Requirement: Inviting members to the portal by email

The system SHALL let an allowed inviter invite people to the portal by email address and user type through
`POST /api/2.0/people/invite` (`inviteUsers`). For every address that has no account the system SHALL
create a pending profile (status Pending, activation Pending, created by the inviter) and mail it an
invitation link. The Guest type SHALL be refused through this operation for everybody.

#### Scenario: Allowed inviter creates a pending member

- **WHEN** the DocSpace Owner, a DocSpace admin or a Room admin invites a new address with a type they may invite
- **THEN** the answer contains a profile whose display name is that address, with a non-empty id, no avatar and not anonymous
- **AND** the profile has status Pending and activation status Pending
- **AND** the answer lists every pending profile of the portal the caller may see, not only the ones just invited

#### Scenario: Guest type is refused for everyone

- **WHEN** anybody, the DocSpace Owner included, invites an address with the Guest type
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Inviting a type of equal or higher privilege is refused

- **WHEN** a DocSpace admin invites a DocSpace admin, a Room admin invites a Room admin, or a User invites a User
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Overlong address is rejected

- **WHEN** an invitation carries an email address longer than 255 characters
- **THEN** the system rejects the request (400, "maximum length of '255'")

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller invites by email
- **THEN** the system answers 401

#### Scenario: Address that already has an account

- **WHEN** an invited address already belongs to a profile of the portal
- **THEN** no email is sent; the account is related to the inviter, and its type is raised when the invitation asks for a higher type than it has

#### Scenario: Address of a disabled account

- **WHEN** an invited address belongs to a disabled (Terminated) account
- **THEN** the whole call is rejected (400, "user is disabled")

#### Scenario: Not enough paid seats

- **WHEN** the invitations ask for more DocSpace admin or Room admin seats than the tariff has left
- **THEN** the whole call is rejected before anything is sent (402)

### Requirement: Adding a member directly

The system SHALL let an allowed caller create a portal account without an invitation email through
`POST /api/2.0/people` (`addMember`) with a password or password hash. Such an account SHALL be Active at
once, with its email activation still Pending, and SHALL record the caller as its creator. Guests SHALL
NOT be created through this operation; the server creates them through the undocumented
`POST /api/2.0/people/active`, which produces an activated account.

#### Scenario: Member created with the flags of the requested type

- **WHEN** the DocSpace Owner adds a Room admin or a DocSpace admin, a DocSpace admin adds a Room admin or a User, or a Room admin adds a User
- **THEN** the profile is returned with a non-empty id and the flags of the requested type (`isRoomAdmin`, `isAdmin` or `isCollaborator`), not LDAP, not owner, not visitor

#### Scenario: Created-by is the caller

- **WHEN** the DocSpace Owner adds a User
- **THEN** the profile's `createdBy` names the owner

#### Scenario: Directly added member is Active

- **WHEN** the active-status list is read after a member was added directly
- **THEN** the member appears with status Active

#### Scenario: Guest type is refused

- **WHEN** a caller adds a member with the Guest type through `POST /api/2.0/people`
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Equal or higher type is refused

- **WHEN** a DocSpace admin adds a DocSpace admin, a Room admin adds a Room admin, or a User adds a User
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Field length validation

- **WHEN** the first name, the last name or the email is longer than 255 characters
- **THEN** the system rejects the request (400, "must be a string with a maximum length of 255")

#### Scenario: Avatar URL pointing at a link-local address

- **WHEN** `files` names a URL in the link-local or metadata range
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: No free paid seat

- **WHEN** a Room admin or DocSpace admin is added while the portal has no free paid seat
- **THEN** the account is still created, as a User

### Requirement: Account statuses Pending invite, Active and Disabled

The system SHALL keep each account in one of the statuses Pending (invited by email, link not yet
followed), Active or Terminated (disabled), separately from its email activation status, and SHALL
expose the status in profiles, filter accounts by it through `GET /api/2.0/people/status/{status}` and
change it through `PUT /api/2.0/people/status/{status}`.

#### Scenario: Invited by email is Pending

- **WHEN** a profile created by `POST /api/2.0/people/invite` is read back
- **THEN** its status is Pending and its activation status is Pending

#### Scenario: Disabling accounts

- **WHEN** the DocSpace Owner sets the status Terminated on a guest, a User, a Room admin and a DocSpace admin
- **THEN** all four are returned with status Terminated and their type flags unchanged
- **AND** the Terminated filter lists all of them

#### Scenario: Re-enabling accounts

- **WHEN** the status Active is set on accounts that were Terminated
- **THEN** they are returned with status Active and their type flags unchanged

#### Scenario: Disabled account cannot be invited to a room

- **WHEN** a disabled account is listed in a room invitation, even with `notify` and `force`
- **THEN** it is skipped silently and the returned member list is empty

### Requirement: Resending the portal invitation or activation email

The system SHALL let the inviter of a pending account send its invitation email again through
`PUT /api/2.0/people/invite` (`resendUserInvites`), issuing a fresh invitation link, and SHALL send
activation instructions instead to an account that exists but has not confirmed its address. A User or a
Guest MAY list nothing but their own id.

#### Scenario: Allowed inviter resends to a pending member

- **WHEN** the DocSpace Owner, a DocSpace admin or a Room admin resends to a member they invited
- **THEN** the member is returned with its email, status Pending and activation status Pending

#### Scenario: Skipped accounts and resendAll

- **WHEN** the list names an account that is already active or disabled
- **THEN** it is skipped without an error
- **AND** `resendAll` reaches every pending account and is allowed to Room admins and DocSpace admins only

#### Scenario: Guest or User resends for another account

- **WHEN** a Guest, or a User (bug 79545), resends the invitation of another pending account
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller resends
- **THEN** the system answers 401

#### Scenario: Throttling

- **WHEN** a caller resends more than 5 times within 15 minutes
- **THEN** the system answers 429

### Requirement: Portal invitation links per user type

The system SHALL keep at most one invitation link per user type (DocSpace admin, Room admin, User) for
the whole portal, managed through `POST`, `GET .../{employeeType}`, `PUT` and `DELETE` on
`/api/2.0/portal/users/invitationlink`. The link SHALL be visible to every admin allowed to invite that
type and MAY carry a use limit and an expiration. A caller MAY act on the link of a type only when they
may invite that type, and only the DocSpace Owner MAY act on the DocSpace admin link.

#### Scenario: Creating a link for a type the caller may invite

- **WHEN** the DocSpace Owner creates a link for User, Room admin or DocSpace admin, a DocSpace admin for User or Room admin, or a Room admin for User
- **THEN** the answer carries a non-empty id, the requested type and a non-empty URL

#### Scenario: One link per type

- **WHEN** a link is created for a type that already has one
- **THEN** the system rejects the request (400)

#### Scenario: Link is shared between admins

- **WHEN** a DocSpace admin or a Room admin reads the link of a type they may invite, created by the DocSpace Owner
- **THEN** the same link is returned, whoever created it
- **AND** a Room admin or a DocSpace admin can delete the User link the owner created

#### Scenario: Type above the caller's reach

- **WHEN** a DocSpace admin creates, reads or deletes the DocSpace admin link, a Room admin the Room admin or DocSpace admin link, a User the User link, or a Guest any link
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Guest type has no link

- **WHEN** a link is created or read for the Guest type
- **THEN** the system rejects the request (400, naming `EmployeeType`)

#### Scenario: Use limit and expiration validation

- **WHEN** `maxUseCount` is 1001 or `expiration` lies in the past, on create or update
- **THEN** the system rejects the request (400; "The field MaxUseCount must be between 1 and 1000." for the count), because the use limit is 1 to 1000 and the expiration must lie in the future

#### Scenario: Limits are stored

- **WHEN** a link is created with `maxUseCount` 1000, or with an expiration seven days ahead
- **THEN** the answer reports `maxUseCount` 1000 and `currentUseCount` 0, or `isExpired` false

#### Scenario: Updating a link

- **WHEN** an allowed caller changes `maxUseCount` or `expiration` of an existing link
- **THEN** the answer carries the same id with the new values

#### Scenario: Unknown link

- **WHEN** an update or delete names an id no link has
- **THEN** the system answers 404

#### Scenario: Deleted link is gone

- **WHEN** a link is deleted and then deleted again
- **THEN** the second delete answers 404

#### Scenario: Paid role when the paid-user quota is exhausted

- **WHEN** the portal has no free paid seat and the DocSpace admin or Room admin link is created or read (bug 81564)
- **THEN** the system answers 402 instead of returning a link

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller creates, reads, updates or deletes a link
- **THEN** the system answers 401

### Requirement: Legacy invitation link endpoint

The system SHALL keep the deprecated `GET /api/2.0/portal/users/invite/{employeeType}`
(`getInvitationLink`), which builds a shortened, unstored link for the type in the path and answers with a
bare URL string; it SHALL return an empty string instead of 403 when the caller may not invite that type.

#### Scenario: Allowed caller gets a URL

- **WHEN** the DocSpace Owner asks for the User, Room admin or DocSpace admin link, a DocSpace admin for User or Room admin, or a Room admin for User
- **THEN** a non-empty URL string is returned

#### Scenario: Denied caller gets an empty answer

- **WHEN** a DocSpace admin asks for the DocSpace admin link, a Room admin for the Room admin or DocSpace admin link, a User for the User link, or a Guest for the User link
- **THEN** the system answers 200 with an empty body

#### Scenario: Guest type

- **WHEN** the DocSpace Owner asks for the Guest link
- **THEN** a non-empty URL string is returned

### Requirement: Room invitation by account or group id

The system SHALL let a manager of a room add, change and remove members in one batch through
`PUT /api/2.0/files/rooms/{id}/share` (`setRoomSecurity`), each entry naming an account or a group of
the portal and an access level; access 0 SHALL remove the subject. The answer SHALL be the resulting access
entries of the named subjects.

#### Scenario: Invisible subjects and notification

- **WHEN** an entry names a subject the caller is not allowed to see
- **THEN** the subject is dropped without an error
- **AND** with `notify` the existing accounts that gained access are emailed, with the optional `message`

#### Scenario: Room owner or invited Room manager adds a member

- **WHEN** the room owner, or a Room admin holding the Room manager role in the room, invites a User
- **THEN** the answer lists the member

#### Scenario: Revoked manager

- **WHEN** a member whose Room manager access was removed invites somebody
- **THEN** the system denies the request (403)

#### Scenario: DocSpace admin outside the room

- **WHEN** a DocSpace admin who is not a member of somebody else's room invites to it
- **THEN** the system denies the request (403)

#### Scenario: Room manager role for a User or a Guest

- **WHEN** a User or a Guest is invited with the Room manager role
- **THEN** the system rejects the request (403) and the member is not in the room

#### Scenario: Guest invited to an AI agent with a creator role

- **WHEN** a Guest is invited to an AI agent room as Room manager or Content creator
- **THEN** the system rejects the request (403, role not available)

#### Scenario: Group as subject

- **WHEN** an entry names a group id that is not an existing share of the room
- **THEN** the share is recorded for the group and every member of the group gets that access

### Requirement: Everyone system group as a share subject

The system SHALL accept the Everyone system group (id `c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e`) as a share
subject, giving every member of the portal the granted access.

#### Scenario: File shared with Everyone

- **WHEN** a file is shared with the Everyone group with read-write access and an anonymous viewer opens it through a read link
- **THEN** the share is accepted, and the link viewer gets read access only

### Requirement: Room invitation by email

The system SHALL let a manager of a room invite somebody who has no account by `email` in the same
`PUT /api/2.0/files/rooms/{id}/share` batch, when the portal allows inviting guests. For an unknown
address the system SHALL create a pending Guest profile created by the caller, record the share and mail an
invitation link that carries the room id. For an address that already has an account the system SHALL
relate that account to the caller and apply the access.

#### Scenario: Unknown address

- **WHEN** an entry carries an email that belongs to no account and an access other than 0
- **THEN** a Guest profile with status Pending is created for it, the share is recorded and the invitation email with the room link is sent

#### Scenario: Address of an existing pending account

- **WHEN** an entry carries the email of a pending account
- **THEN** the account is related to the caller, its access is set and the room invitation email is sent again

#### Scenario: Address of an existing active account

- **WHEN** an entry carries the email of an account that is no longer pending
- **THEN** the account is related to the caller and its access is set; no invitation link is mailed, and the account is notified as an existing member when `notify` is set

#### Scenario: Guest invitations disabled

- **WHEN** an entry carries an email with an access other than 0 while the portal forbids inviting guests
- **THEN** the system denies the whole request (403)

#### Scenario: Malformed or overlong address

- **WHEN** an `email` is not an email address or is longer than 255 characters
- **THEN** the system rejects the request (400)

#### Scenario: Address rejected during profile creation

- **WHEN** the address cannot be turned into a profile (for example it is linked to another account)
- **THEN** the entry is skipped and its reason is returned in `warning`; the other entries are applied

### Requirement: Resending room invitations

The system SHALL let a manager of a room send the room invitation email again through
`POST /api/2.0/files/rooms/{id}/resend` (`resendEmailInvitations`) to members whose account is still
pending. `resendAll` SHALL cover every pending member and make `usersIds` irrelevant. An account that is
already active, not a member of the room or invisible to the caller SHALL be skipped without an error.
The operation SHALL change nothing about the room or its membership.

#### Scenario: Owner or Room manager resends

- **WHEN** the room owner, or a Room admin holding the Room manager role in the room, resends to one, several or all pending members, in a Custom, Public, Form filling, Collaboration or Virtual data room
- **THEN** the call succeeds and returns no body

#### Scenario: Membership is untouched

- **WHEN** invitations are resent, with `resendAll` or with a list
- **THEN** the members, their access levels and their count are the same as before; an already active member keeps its access and appears once

#### Scenario: No-op bodies and outsiders

- **WHEN** the request lists nobody without `resendAll`, lists an empty array, names an active member, or names an account that is not a member
- **THEN** the call succeeds and the invite records are untouched

#### Scenario: Not a manager

- **WHEN** a DocSpace admin or Room admin who is not a member, or a member with any role below Room manager, resends
- **THEN** the system denies the request (403, "You don't have enough permission to perform the operation")

#### Scenario: Anonymous or disabled caller

- **WHEN** an unauthenticated caller, or a Room manager whose account has been disabled, resends
- **THEN** the system answers 401

#### Scenario: Malformed account id

- **WHEN** `usersIds` contains `0`, `-1`, a non-GUID string or a numeric id
- **THEN** the system rejects the request (400)

#### Scenario: Missing, unknown or deleted room

- **WHEN** the room id is `0`, `-1`, unknown, or the room was deleted (bugs 81879, 81880)
- **THEN** the system answers 404

#### Scenario: Archived room

- **WHEN** the room is archived
- **THEN** the system denies the request (403, "You don't have enough permission to perform the operation")

#### Scenario: Throttling

- **WHEN** a caller resends more than 5 times within 15 minutes
- **THEN** the system answers 429

### Requirement: Invitation link of a room

The system SHALL keep at most one invitation link per room, created through
`PUT /api/2.0/files/rooms/{id}/links` with `linkType` Invitation (the default when `linkType` is omitted),
and SHALL treat it as a different thing from the external links covered by `shared-links`. The link
SHALL carry the access level that a person joining through it receives.

#### Scenario: Second invitation link is refused

- **WHEN** a second invitation link is created for a room that has one
- **THEN** the system rejects the request (403) and the first link remains

#### Scenario: Recreating after deletion

- **WHEN** the invitation link is deleted (access 0) and a new one is created
- **THEN** the room has exactly one invitation link again

#### Scenario: Accepted access levels

- **WHEN** an invitation link is created with Read, Review, Comment or Editing access in a Custom room
- **THEN** the link is stored with that access

#### Scenario: Default link type

- **WHEN** a room link is created without `linkType`
- **THEN** the created link is an invitation link

#### Scenario: No access, no link

- **WHEN** an invitation link is created without an access level
- **THEN** no link is created

#### Scenario: Room types

- **WHEN** an invitation link is created, updated and deleted in a Custom, Public, Collaboration or Virtual data room
- **THEN** every step succeeds

#### Scenario: Form filling room accepts only its own roles

- **WHEN** an invitation link is created in a Form filling room with the Form Filler or Content creator access
- **THEN** the link is created, and a second invitation link is refused (403, "The maximum number of links may not exceed 1")
- **AND** an invitation link with the Read access is refused (403, "The role is not available for this user type")

#### Scenario: Not the primary link

- **WHEN** a Public room has an invitation link and its primary link is read
- **THEN** the primary link is an external link with a different id

### Requirement: Following an invitation link

The system SHALL check an invitation link through `POST /api/2.0/authentication/confirm`
(`checkConfirm`, type LinkInvite) and complete the registration through `POST /api/2.0/people` with
`fromInviteLink` and the link `key`. For a link into a room the check SHALL answer with the room id and
title. An existing account is added to the room by the check itself; an unknown person joins the room when
the registration is completed, which SHALL set the type the link grants, ignoring the `type` sent.

#### Scenario: Existing account not in the room

- **WHEN** a signed-in account that is not a member follows a room invitation link whose key is valid
- **THEN** the account is given the link's access in the room, the room id and title are returned, and `result` is `UserExisted`

#### Scenario: Existing member

- **WHEN** a signed-in member of the room follows its invitation link
- **THEN** no share is added, the room id and title are returned, and `result` is `UserExisted`

#### Scenario: Unknown person

- **WHEN** nobody is signed in and the key is valid
- **THEN** `result` is `Ok` with the room id and title and the email the link was issued for
- **AND** completing `POST /api/2.0/people` with `fromInviteLink` creates the account, counts one use on the link and adds the account to the room

#### Scenario: Check that is not Ok cannot be completed

- **WHEN** `POST /api/2.0/people` is sent with `fromInviteLink` and a key whose check is not `Ok`
- **THEN** the system denies the request (403)

#### Scenario: Previously removed member

- **WHEN** a signed-in account that another user removed from the room follows its invitation link
- **THEN** `result` is `UserExcluded` and the account is not re-added

#### Scenario: Use limit reached

- **WHEN** the link's `currentUseCount` has reached its `maxUseCount`
- **THEN** `result` is `QuotaFailed`

#### Scenario: Expired or invalid link

- **WHEN** the link has passed its expiration, or the key is not a link of this portal, or the link's creator is disabled or removed
- **THEN** `result` is `Expired` or `Invalid` and no room data is returned

#### Scenario: Paid role for a free type

- **WHEN** a User or a Guest follows a room link that grants a paid role
- **THEN** the access is lowered to the highest free role of that room type, and `result` is `QuotaFailed` when the room type has none

#### Scenario: Guest joining an AI agent

- **WHEN** a Guest follows a link into an AI agent room that grants Room manager or Content creator
- **THEN** the guest is added with Viewer access

#### Scenario: Individual email link already accepted

- **WHEN** the person behind an individual email invitation already has a password, or the account is disabled or gone
- **THEN** `result` is `UserExisted`, or `Invalid`

#### Scenario: Individual email link visited too long ago

- **WHEN** an individual email link is checked again after the visit-link interval has passed since its first visit
- **THEN** `result` is `Expired`

#### Scenario: Key from the cookie

- **WHEN** the check is sent without `key`
- **THEN** the key stored in the confirmation cookie of that type is used

#### Scenario: IP restriction

- **WHEN** the portal's IP restrictions do not allow the caller's address
- **THEN** the system denies the check (403)

### Requirement: Invitation settings

The system SHALL keep two portal-wide switches, `allowInvitingMembers` and `allowInvitingGuests`, both
on by default, readable by anyone through `GET /api/2.0/settings/invitationsettings` and changeable by
the DocSpace Owner and DocSpace admins through `PUT /api/2.0/settings/invitationsettings`. Each switch,
when off, SHALL block the invitations of its kind. Issued links SHALL stay stored but SHALL NOT be accepted
while the switch for their type is off.

#### Scenario: Anonymous read and conditional request

- **WHEN** an unauthenticated caller reads the settings, or sends `If-Modified-Since` with the last `lastModified`
- **THEN** the two switches are returned, or 304 with an empty body when nothing changed

#### Scenario: Only portal-settings holders may change them

- **WHEN** a caller without the portal-settings right updates the settings
- **THEN** the system denies the request (403)

#### Scenario: Member invitations off

- **WHEN** `allowInvitingMembers` is off and a caller invites by email, adds a member directly, or creates, reads, updates or deletes a portal invitation link
- **THEN** the system denies the request (403, "Access denied"); these are the member invitations through Contacts

#### Scenario: Guest invitations off

- **WHEN** `allowInvitingGuests` is off and a room invitation lists an email with an access other than 0, or an account is created as a Guest
- **THEN** the system denies the request (403, "Access denied"); inviting a new email through a room and creating a guest are the guest invitations

#### Scenario: Registration through a stored link while invitations are off

- **WHEN** somebody completes `POST /api/2.0/people` through a link whose type is blocked by the current settings
- **THEN** the system denies the request (403, "Access denied")

### Requirement: Invitation quota

The system SHALL keep a replenishable per-portal quota of email invitations (unlimited by default, with a
24-hour replenishment window), exposed as `invitationLimit` in `GET /api/2.0/settings`. Sending an
invitation email to the portal or to a room SHALL reduce the quota by one, completing a registration
through an email invitation SHALL give one back, and a request whose email invitations exceed the remaining
quota SHALL fail request validation.

#### Scenario: Sending reduces the quota

- **WHEN** the portal has a finite quota and one member is invited by email
- **THEN** `invitationLimit` in the settings is one lower than before

#### Scenario: Quota exceeded

- **WHEN** a single request invites more addresses than the quota has left
- **THEN** the system rejects the request (400, "Invitation limit is exceeded")

#### Scenario: Following the link gives the quota back

- **WHEN** an invited person completes the registration through the email link
- **THEN** the quota is increased by one, never beyond the configured limit

### Requirement: Daily email invitation rate limit

The system SHALL enforce a configurable daily number of email invitations per portal, when configured, as
a fixed one-day window on `POST /api/2.0/people/invite` and `PUT /api/2.0/files/rooms/{id}/share`,
counting each address in the request body as one permit; a request that names no address SHALL not be
counted. Without the setting no limit SHALL apply.

#### Scenario: Limit exceeded

- **WHEN** the addresses of a request would take the portal past the daily number
- **THEN** the system answers 429

### Requirement: Paid-user limit when inviting

The system SHALL enforce the paid-user quota of the tariff when a paid type (DocSpace admin, Room admin)
is invited, and SHALL refuse rather than downgrade a paid room role for a free user type. A paid role
granted to a free type through a room invitation link SHALL be lowered to the highest free role of the
room type.

#### Scenario: Room manager role for a User or a Guest

- **WHEN** a User or a Guest is invited to a room with the Room manager role
- **THEN** the system rejects the request (403) and the member is not added

#### Scenario: Paid type beyond the quota

- **WHEN** inviting by email would need more paid seats than the tariff has left, or the paid-role invitation link is created or read with no free seat
- **THEN** the system answers 402

#### Scenario: Direct creation with no free seat

- **WHEN** a paid type is added directly with no free seat
- **THEN** the account is created as a User

### Requirement: Sharing a guest with another manager

The system SHALL let a caller who can see a guest mint a shortened confirmation link for it through
`GET /api/2.0/people/guests/{userId}/share` (`getGuestSharingLink`), and SHALL let a Room admin or
DocSpace admin who opens that link take the guest over through `POST /api/2.0/people/guests/share/approve`
(`approveGuestShareLink`), which relates the guest to the caller and returns its profile; a guest already
related is simply returned again.

#### Scenario: Caller who can see the guest

- **WHEN** the DocSpace Owner asks for the link of a guest created by itself or by a DocSpace admin, a DocSpace admin for a guest created by the owner or by itself, or a Room admin for its own guest
- **THEN** a URL whose path starts with `/s/` is returned

#### Scenario: Caller who cannot see the guest

- **WHEN** a Room admin or a User asks for the link of a guest created by the owner
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Target is not a guest

- **WHEN** the id names a member, or no account
- **THEN** the system answers 400, or 404

#### Scenario: Approving the link

- **WHEN** a Room admin or DocSpace admin opens the link and approves it
- **THEN** the guest is related to the caller and its full profile is returned
- **AND** a User or a Guest is refused (403)

### Requirement: Guest relations decide whose guest it is

The system SHALL relate a guest to the account that invited it, and SHALL let only a related Room admin
remove the guest from its own guest list through `DELETE /api/2.0/people/guests`.

#### Scenario: Removing own guests

- **WHEN** a Room admin removes guests it created from its list
- **THEN** the call succeeds and the guests are no longer members of the Room admin's room

#### Scenario: Somebody else's guest

- **WHEN** the DocSpace Owner, a DocSpace admin, a User, another guest, or a Room admin who did not invite the guest removes it
- **THEN** the system denies the request (403, "Access denied")

#### Scenario: Invited by email then turned into a guest

- **WHEN** a member invited by email by a Room admin is later changed to a Guest
- **THEN** only that Room admin may remove it; another Room admin is refused (403)

### Requirement: Encrypted email in confirmation links

The system SHALL put the email of a confirmation link into the link as `encemail`, an encrypted value,
never in clear text, and SHALL accept `encemail` wherever a confirmation link is checked or consumed,
decrypting it when `email` is absent.

#### Scenario: Link built by the server

- **WHEN** a confirmation link is issued for an address
- **THEN** its query carries `type`, `key`, `encemail` and, when known, `uid`, and no `email`

#### Scenario: Check with encemail

- **WHEN** `POST /api/2.0/authentication/confirm` or a confirm-authorized operation receives `encemail` without `email`
- **THEN** the address is decrypted and used for the check
