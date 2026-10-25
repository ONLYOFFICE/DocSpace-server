# user-type-permissions Specification

## Purpose

Defines what each portal-level user type — DocSpace Owner, DocSpace admin, Room admin, User, Guest — is
allowed to do in every section of a DocSpace portal: My documents, Rooms, Archive, Trash, Accounts
(Contacts), Portal settings and file sharing. The user type is the outer limit of a person's rights; the
role a person holds inside a particular room (see `room-role-permissions`) narrows them further.

Sources: product matrix "Types and Roles. Access rights", sheet "Types", checked against the integration
tests and a live run against the server (October 2026). Where the matrix and the server disagree, this
specification follows the server; the differences are listed in `openspec/deviations.md` and, with the
evidence for every row, in `verification.md` next to this file. In the tables below `✔` means the action is
allowed and an empty cell means it is denied. The matrices themselves are stated in the first scenario of
each requirement.

## Requirements

### Requirement: Section availability by user type

The system SHALL expose portal sections to a user according to the user type: My documents to every type
except Guest, Rooms, Archive and Trash to every type, Accounts to Room admins and above, Portal settings to
DocSpace admins and the Owner only.

#### Scenario: Section matrix by user type

- **WHEN** a user of the given type opens a section
- **THEN** access follows this table

| Section         | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|-----------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| My documents    | ✔ | ✔ | ✔ | ✔ |   |
| Rooms           | ✔ | ✔ | ✔ | ✔ | ✔ |
| Archive         | ✔ | ✔ | ✔ | ✔ | ✔ |
| Trash           | ✔ | ✔ | ✔ | ✔ | ✔ |
| Accounts        | ✔ | ✔ | ✔ |   |   |
| Portal settings | ✔ | ✔ |   |   |   |

#### Scenario: Guest has no My documents section

- **WHEN** a Guest requests the My documents root or creates a file in it
- **THEN** the system answers that the folder does not exist (404)

#### Scenario: User has no Accounts section

- **WHEN** a User or a Guest requests the people list, the people filter or the group list
- **THEN** the system denies access (403)

#### Scenario: Room admin has no Portal settings

- **WHEN** a Room admin, a User or a Guest changes a portal setting
- **THEN** the system denies access (403)

### Requirement: My documents operations

Every user type that has the My documents section (DocSpace Owner, DocSpace admin, Room admin, User)
SHALL be able to create, upload, move, copy, rename, download and delete files and folders in their own
My documents.

#### Scenario: User manages own documents

- **WHEN** a User creates, uploads, moves, copies, renames, downloads or deletes a file or folder in their My documents
- **THEN** the operation succeeds

### Requirement: Room visibility by user type

The system SHALL limit which rooms a user sees in the Rooms and Archive sections according to the user
type: the Owner and DocSpace admins see every room, a Room admin the rooms they own or were invited to, a
User or a Guest only the rooms they were invited to. End-to-end encrypted (private) rooms SHALL be visible
only to their members, the DocSpace Owner and DocSpace admins included.

#### Scenario: Room visibility matrix

- **WHEN** a user of the given type lists the Rooms or the Archive section
- **THEN** the rooms shown follow this table

| Action                               | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|--------------------------------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| See all rooms of the portal          | ✔ | ✔ |   |   |   |
| See own rooms (where I am the owner) | ✔ | ✔ | ✔ |   |   |
| See rooms I was invited to           | ✔ | ✔ | ✔ | ✔ | ✔ |

#### Scenario: DocSpace admin sees a room they are not a member of

- **WHEN** a DocSpace Owner or DocSpace admin lists rooms
- **THEN** the list includes rooms they neither own nor were invited to

#### Scenario: Room admin does not see other people's rooms

- **WHEN** a Room admin lists rooms
- **THEN** the list contains only rooms they own or were invited to

#### Scenario: Private room is hidden from a non-member admin

- **WHEN** a DocSpace admin who is not a member lists rooms
- **THEN** the list does not include private rooms

### Requirement: Room creation and management in the Rooms section

The system SHALL allow room-level actions in the Rooms section by user type as stated in the matrix
scenario. Creating, editing, inviting, assigning roles, removing members, archiving and copying rooms
need the Room admin type or higher; pinning and viewing members, history and info are open to every type.
Actions on a room additionally require a room role that grants them (see `room-role-permissions`).

#### Scenario: Room action matrix by user type

- **WHEN** a user of the given type performs a room action in the Rooms section
- **THEN** the outcome follows this table

| Action                                         | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|------------------------------------------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| Create rooms                                   | ✔ | ✔ | ✔ |   |   |
| Pin rooms                                      | ✔ | ✔ | ✔ | ✔ | ✔ |
| View the member list                           | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room history                              | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room info                                 | ✔ | ✔ | ✔ | ✔ | ✔ |
| Edit rooms                                     | ✔ | ✔ | ✔ |   |   |
| Invite external users to a room                | ✔ | ✔ | ✔ |   |   |
| Invite internal users / groups to a room       | ✔ | ✔ | ✔ |   |   |
| Assign member roles when inviting              | ✔ | ✔ | ✔ |   |   |
| Change roles of room members / groups          | ✔ | ✔ | ✔ |   |   |
| Remove users / groups from a room              | ✔ | ✔ | ✔ |   |   |
| Archive own rooms                              | ✔ | ✔ | ✔ |   |   |
| Create a copy of own room                      | ✔ | ✔ | ✔ |   |   |

#### Scenario: Inviting needs the Room manager role

- **WHEN** a member invites to a room, assigns or changes roles or removes members
- **THEN** the member needs the Room manager role in the room (or room ownership), which a User or a Guest can never hold; the product matrix plans invitations by Users and Guests for a later version

#### Scenario: User cannot create a room

- **WHEN** a User or a Guest attempts to create a room
- **THEN** the system rejects the request (403)

#### Scenario: User and Guest cannot invite

- **WHEN** a User or a Guest who is a member of a room invites an external email or a portal user to that room
- **THEN** the system rejects the request (403) and the member list is unchanged

#### Scenario: Room admin makes another Room admin a Room manager

- **WHEN** a Room admin assigns the Room manager role to a room member whose user type is Room admin
- **THEN** the role is assigned

#### Scenario: Room manager role is refused for a User

- **WHEN** anyone, the DocSpace Owner included, assigns the Room manager role to a member whose user type is User or Guest
- **THEN** the system rejects the request (403, "The role is not available for this user type")
- **AND** a Room admin can therefore hand out any role except Room manager to a User or a Guest

### Requirement: Actions on rooms owned by someone else

Seeing every room SHALL NOT give a DocSpace Owner or DocSpace admin member-level control over a room they
are not a member of. For a room owned by someone else the system SHALL allow only viewing the member list,
pinning, downloading content, reading the primary link of a Public room, copying the room, changing its
owner and archiving it — and only to the Owner and DocSpace admins. Editing, inviting, role changes,
removals and link management are refused to every non-member.

#### Scenario: Foreign room action matrix

- **WHEN** a user of the given type acts on a room they do not own and are not a member of
- **THEN** the outcome follows this table

| Action on someone else's room                | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|----------------------------------------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| View the member list                         | ✔ | ✔ |   |   |   |
| Pin the room                                 | ✔ | ✔ |   |   |   |
| Download content                             | ✔ | ✔ |   |   |   |
| Read the primary link of a public room       | ✔ | ✔ |   |   |   |
| Create a copy of the room                    | ✔ | ✔ |   |   |   |
| Change the room owner                        | ✔ | ✔ |   |   |   |
| Archive the room                             | ✔ | ✔ |   |   |   |
| Invite users / groups                        |   |   |   |   |   |
| Assign or change member / group roles        |   |   |   |   |   |
| Edit the room                                |   |   |   |   |   |
| Remove users / groups                        |   |   |   |   |   |
| Create, edit or delete links                 |   |   |   |   |   |

#### Scenario: DocSpace admin changes the owner of another user's room

- **WHEN** a DocSpace Owner or DocSpace admin reassigns the owner of a room they do not own to a Room admin
- **THEN** the owner is changed and the previous owner becomes a Room manager of the room

#### Scenario: Room owner cannot be a User

- **WHEN** the owner of a room is reassigned to a member whose type is User or Guest, or to a disabled account
- **THEN** the system rejects the request (403); the new owner MUST be an active user whose type is Room admin or higher

#### Scenario: DocSpace admin cannot edit another user's room

- **WHEN** a DocSpace admin who is not a member of a room attempts to edit it, invite or remove members, change member roles, or create a link
- **THEN** the system rejects the request (403)

#### Scenario: DocSpace admin reads the primary link of a public room

- **WHEN** a DocSpace admin who is not a member requests the primary link of a Public room
- **THEN** the link is returned

#### Scenario: Room admin cannot archive another user's room

- **WHEN** a Room admin attempts to archive, copy or change the owner of a room owned by someone else
- **THEN** the system rejects the request (403)

### Requirement: Archive section actions

Archived rooms SHALL be read-only: no user type can create rooms in the Archive, pin archived rooms, edit
them, invite users, assign or change member roles, or remove users from them. Viewing members, history and
info stays open to every type; copying, restoring and deleting an archived room need the Room admin type
for own rooms and the DocSpace admin type or the Owner for any room.

#### Scenario: Archive action matrix by user type

- **WHEN** a user of the given type acts on an archived room
- **THEN** the outcome follows this table

| Action                                         | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|------------------------------------------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| View the member list                           | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room history                              | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room info                                 | ✔ | ✔ | ✔ | ✔ | ✔ |
| Create a copy of own archived room in Rooms    | ✔ | ✔ | ✔ |   |   |
| Create a copy of someone else's room in Rooms  | ✔ | ✔ |   |   |   |
| Restore own room                               | ✔ | ✔ | ✔ |   |   |
| Restore any room                               | ✔ | ✔ |   |   |   |
| Delete own room                                | ✔ | ✔ | ✔ |   |   |
| Delete any room                                | ✔ | ✔ |   |   |   |

#### Scenario: Archived room rejects membership changes

- **WHEN** any user, including the DocSpace Owner, attempts to invite a user to an archived room, change a member role or remove a member
- **THEN** the system rejects the request (403)

#### Scenario: Room admin restores own archived room

- **WHEN** a Room admin restores or deletes an archived room they own
- **THEN** the operation succeeds

#### Scenario: Room admin cannot restore another user's archived room

- **WHEN** a Room admin attempts to restore or delete an archived room owned by someone else
- **THEN** the system rejects the request (403)

#### Scenario: DocSpace admin restores and deletes another user's archived room

- **WHEN** a DocSpace admin who is not a member restores an archived room, archives it again and deletes it
- **THEN** every step succeeds

### Requirement: Inviting users by type

The system SHALL restrict which user types an inviter can invite to the portal: the Owner invites any type
but Guest, a DocSpace admin invites Room admins and Users, a Room admin invites Users only. Nobody, the
DocSpace Owner included, SHALL invite a person with the Guest type through the people endpoints: guests
appear only through room invitations.

#### Scenario: Invitation matrix by inviter type

- **WHEN** an inviter of the given type invites a person with the given type to the portal
- **THEN** the outcome follows this table

| Invite as        | DocSpace Owner | DocSpace admin | Room admin |
|------------------|:--------------:|:--------------:|:----------:|
| DocSpace admin   | ✔ |   |   |
| Room admin       | ✔ | ✔ |   |
| User             | ✔ | ✔ | ✔ |
| Guest            |   |   |   |

#### Scenario: DocSpace admin cannot invite another DocSpace admin

- **WHEN** a DocSpace admin invites a person with the DocSpace admin type
- **THEN** the system rejects the request (403)

#### Scenario: Room admin invites a User

- **WHEN** a Room admin invites a person with the User type
- **THEN** a pending account is created

#### Scenario: Guest type cannot be invited directly

- **WHEN** the DocSpace Owner invites a person with the Guest type through the people endpoints
- **THEN** the system rejects the request

### Requirement: Changing a user's type

The system SHALL restrict user type changes by the caller's type: only the Owner promotes to or demotes
from DocSpace admin; the Owner and DocSpace admins promote to Room admin, demote Room admins and Users, and
promote any Guest; a Room admin promotes only the Guests they invited themselves. Demotions run as a queued
job that reports its result.

#### Scenario: Type change matrix by caller type

- **WHEN** a caller of the given type requests the given type change
- **THEN** the outcome follows this table; "own Guest" is a guest the Room admin invited

| Type change                               | DocSpace Owner | DocSpace admin | Room admin |
|-------------------------------------------|:--------------:|:--------------:|:----------:|
| Promote to DocSpace admin                 | ✔ |   |   |
| Promote to Room admin                     | ✔ | ✔ |   |
| Promote own Guest to User                 | ✔ | ✔ | ✔ |
| Promote any Guest to User                 | ✔ | ✔ |   |
| Demote DocSpace admin to Room admin       | ✔ |   |   |
| Demote DocSpace admin to User             | ✔ |   |   |
| Demote Room admin to User                 | ✔ | ✔ |   |
| Demote User to Guest                      | ✔ | ✔ |   |

#### Scenario: Only the owner changes a DocSpace admin's type

- **WHEN** a DocSpace admin attempts to promote someone to DocSpace admin or to demote an existing DocSpace admin
- **THEN** the system rejects the request and the type stays unchanged

#### Scenario: Room admin promotes own Guest

- **WHEN** a Room admin promotes a Guest they invited to User
- **THEN** the type is changed

#### Scenario: Room admin cannot promote another inviter's Guest

- **WHEN** a Room admin promotes a Guest invited by someone else to User
- **THEN** the system rejects the request (403)

#### Scenario: Room admin cannot demote a User

- **WHEN** a Room admin requests the demotion of a User to Guest
- **THEN** the request is accepted as a job, the type stays unchanged and the job ends with a permission error

#### Scenario: Room admin cannot promote to Room admin

- **WHEN** a Room admin attempts to promote a User to Room admin
- **THEN** the system rejects the request (403)

### Requirement: Disabling, deleting and reassigning users

The system SHALL allow disabling (blocking) and deleting a user, and reassigning the data of a disabled
user, to the DocSpace Owner for every type and to a DocSpace admin for every type but DocSpace admin.
Room admins, Users and Guests SHALL NOT perform any of these actions. An account MUST be disabled before it
can be deleted.

#### Scenario: Disable and delete matrix by target type

- **WHEN** the Owner or a DocSpace admin disables, deletes or reassigns the data of an account of the given type
- **THEN** the outcome follows this table; reassigning is allowed for exactly the target types the caller may disable

| Target user type | Disable: Owner | Disable: DocSpace admin | Delete: Owner | Delete: DocSpace admin |
|------------------|:--------------:|:-----------------------:|:-------------:|:----------------------:|
| DocSpace admin   | ✔ |   | ✔ |   |
| Room admin       | ✔ | ✔ | ✔ | ✔ |
| User             | ✔ | ✔ | ✔ | ✔ |
| Guest            | ✔ | ✔ | ✔ | ✔ |

#### Scenario: DocSpace admin cannot disable a DocSpace admin

- **WHEN** a DocSpace admin attempts to disable, delete or reassign the data of another DocSpace admin
- **THEN** the system rejects the request

#### Scenario: DocSpace admin deletes a Room admin

- **WHEN** a DocSpace admin disables and then deletes a Room admin, User or Guest
- **THEN** the operation succeeds

#### Scenario: Active account cannot be deleted

- **WHEN** the DocSpace Owner deletes an account that is not disabled
- **THEN** the system rejects the request (403)

#### Scenario: Reassigning to a User is refused

- **WHEN** a DocSpace admin reassigns the data of a disabled Room admin to a User, a Guest or a disabled account
- **THEN** the system rejects the request (400); data is reassigned only to an active Room admin or DocSpace admin

### Requirement: Groups and guest lists in Accounts

The system SHALL allow group management (create, edit, change membership) to the DocSpace Owner and
DocSpace admins only; a Room admin reads groups and their members. Guests SHALL NOT be members or managers
of groups. A Room admin sees only the Guests they invited; the Owner and DocSpace admins see every Guest.

#### Scenario: Accounts matrix by user type

- **WHEN** a user of the given type performs the given action in Accounts
- **THEN** the outcome follows this table

| Action                                    | DocSpace Owner | DocSpace admin | Room admin |
|-------------------------------------------|:--------------:|:--------------:|:----------:|
| Create groups                             | ✔ | ✔ |   |
| Edit groups                               | ✔ | ✔ |   |
| Change group membership                   | ✔ | ✔ |   |
| See the list of groups and their members  | ✔ | ✔ | ✔ |
| See other inviters' Guests                | ✔ | ✔ |   |
| See own Guests                            | ✔ | ✔ | ✔ |
| Add Guests to groups                      |   |   |   |

#### Scenario: Room admin sees only own guests

- **WHEN** a Room admin lists Guests or requests the profile of a guest invited by someone else
- **THEN** the list contains only their own Guests and the foreign profile is denied (403)

#### Scenario: Guest is skipped when added to a group

- **WHEN** a guest is among the members added to an existing group
- **THEN** the request succeeds, the other members are added and the guest is not

#### Scenario: Group cannot be created with a guest

- **WHEN** a group is created with a guest among its members or as its manager
- **THEN** the system rejects the request (400)

#### Scenario: Demoted group member leaves the group

- **WHEN** a User who belongs to a group is demoted to Guest
- **THEN** the user is removed from the group

#### Scenario: Room admin cannot manage groups

- **WHEN** a Room admin attempts to create a group, edit it or change its membership
- **THEN** the system rejects the request (403)

### Requirement: Portal settings

The system SHALL allow the DocSpace Owner and DocSpace admins to manage portal settings. The following
SHALL be available to the DocSpace Owner only: deleting the portal, suspending the portal, granting and
revoking the DocSpace admin type.

#### Scenario: DocSpace admin cannot delete or suspend the portal

- **WHEN** a DocSpace admin requests portal deletion or portal suspension
- **THEN** the system rejects the request (403)

#### Scenario: DocSpace admin cannot grant DocSpace admin

- **WHEN** a DocSpace admin grants or revokes the DocSpace admin type through the security settings
- **THEN** the system rejects the request (403)

#### Scenario: DocSpace admin changes other settings

- **WHEN** a DocSpace admin changes an ordinary portal setting, such as the greeting title
- **THEN** the change is applied

### Requirement: Sharing files from My documents

The system SHALL allow every type with a My documents section to share files with portal users, the Owner
and DocSpace admins to share with any guest, and a Room admin only with the guests the Room admin invited.
A recipient the sharer may not share with SHALL be skipped while the request succeeds. Quick share of a
form for filling through a link SHALL be offered in My documents, Form filling rooms and Virtual Data
Rooms only.

#### Scenario: Sharing matrix by user type

- **WHEN** a user of the given type shares a file from My documents
- **THEN** the outcome follows this table

| Action                                                              | DocSpace Owner | DocSpace admin | Room admin | User | Guest |
|---------------------------------------------------------------------|:--------------:|:--------------:|:----------:|:----:|:-----:|
| Share files with portal users                                       | ✔ | ✔ | ✔ | ✔ |   |
| Share files with own guests                                         | ✔ | ✔ | ✔ |   |   |
| Share files with any guest                                          | ✔ | ✔ |   |   |   |
| See the list of users and groups in Contacts                        | ✔ | ✔ | ✔ |   |   |
| Quick share for forms                                               | ✔ | ✔ | ✔ | ✔ |   |

#### Scenario: User shares a file with a portal user

- **WHEN** a User shares a file from My documents with another portal user
- **THEN** the share is created although the User has no access to Contacts

#### Scenario: User cannot share with a guest

- **WHEN** a User shares a file with a Guest
- **THEN** the request succeeds, the guest is skipped and the guest cannot open the file

#### Scenario: Room admin shares with an unrelated guest

- **WHEN** a Room admin shares a file with a guest invited by someone else, even a member of the Room admin's room
- **THEN** the request succeeds, the guest is skipped and the guest cannot open the file

#### Scenario: Room admin shares with own guest

- **WHEN** a Room admin shares a file with a guest they invited
- **THEN** the share is created and the guest can open the file

#### Scenario: Quick share of a form from My documents

- **WHEN** a User creates an external link with the form-filling access on a PDF form in My documents
- **THEN** the link is created

#### Scenario: Form in a Collaboration room is not shared for filling

- **WHEN** an external link with the form-filling access is requested for a PDF form in a Custom, Public or Collaboration room
- **THEN** the system rejects the request (403, "The role is not available for this user type"); a link to such a form MAY grant editing, commenting or reading instead
