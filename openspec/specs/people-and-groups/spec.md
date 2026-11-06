# people-and-groups Specification

## Purpose

Defines the accounts and groups of a portal as API objects: reading and editing a profile, its language, its
email address and its photo, the per-account theme, the searches over people and over the groups and accounts
an entry can be shared with, the jobs that reassign or erase the data of a user and watch a type conversion,
linking and unlinking third-party identities on a profile, and creating, editing and filling groups.

Cross-references, not repeated here: which user type may do what in general is `user-type-permissions`;
invitations and invitation links are `invitations`; the audit records are `history-and-audit`; user quota is
`quotas`. Passwords, signing in, two-factor authentication and account recovery belong to a separate
capability; third-party accounts appear here only as profile operations.

Sources: the integration tests of `products/ASC.People/Tests`, the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`), and the product code where
the tests stop. Evidence per requirement, differences and suspected defects are in `verification.md` next to
this file. Every operation here answers 401 to an anonymous caller unless the requirement says otherwise.

## Requirements

### Requirement: Reading another profile by ID

`getProfileByUserId` SHALL return the profile to the Owner, a DocSpace admin and a Room admin, and SHALL answer 403 to a User or a Guest asking for another account. An unknown or deleted ID SHALL answer 404.

#### Scenario: Room admin reads a profile

- **WHEN** a Room admin calls `getProfileByUserId` for a Room admin
- **THEN** the answer holds that account's `id` and `email`

#### Scenario: User asks for the Owner

- **WHEN** a User or a Guest calls `getProfileByUserId` for the Owner
- **THEN** the answer is 403 "Access denied"

#### Scenario: Deleted profile

- **WHEN** the profile has been removed through `removeUsers`
- **THEN** `getProfileByUserId` answers 404

### Requirement: Reading a profile by email

`getProfileByEmail` (`GET people/email`) SHALL return the caller's own profile to every user type and another account's profile only to a caller allowed to see that account. A User or a Guest looking up someone else SHALL get 403, and an address no account holds SHALL get 404.

#### Scenario: Own address

- **WHEN** any user type looks up their own email
- **THEN** the answer is the same profile `getSelfProfile` returns

#### Scenario: Someone else's address

- **WHEN** a User or a Guest looks up another member's email
- **THEN** the answer is 403 "Access denied"

#### Scenario: Encrypted address

- **WHEN** `email` is empty and `encemail` is given
- **THEN** the server decrypts `encemail` and looks up that address; with neither parameter the answer is 400

### Requirement: Own profile and profile list

`getSelfProfile` SHALL return the caller's own profile to every user type, and `getAllProfiles` SHALL list members with their type flags to the Owner and DocSpace admins and answer 403 to a User or a Guest.

#### Scenario: Guest reads own profile

- **WHEN** a Guest calls `getSelfProfile`
- **THEN** the profile comes back without a personal folder

#### Scenario: User lists profiles

- **WHEN** a User or a Guest calls `getAllProfiles`
- **THEN** the answer is 403

### Requirement: Checking whether an email is taken

`checkUserExistsByEmail` (`GET people/exists`) SHALL tell an authenticated caller or the holder of an invitation or guest-share confirmation key whether an address belongs to an account and, when it does, that account's status. A missing address SHALL answer 400.

#### Scenario: Address in use

- **WHEN** the address belongs to a profile
- **THEN** the answer has `exists` true and the `status` of that profile

#### Scenario: Free address

- **WHEN** no profile holds the address
- **THEN** the answer has `exists` false and no `status`

### Requirement: Token diagnostics

`getClaims` (`GET people/tokendiagnostics`) SHALL return to an authenticated caller the name of their identity and the list of its claims, each as `type:value`.

#### Scenario: Reading own claims

- **WHEN** an authenticated caller calls `getClaims`
- **THEN** the answer holds `name` and `claims` of the identity of that request

### Requirement: Editing a profile

`updateMember` SHALL let every user type change their own names. First and last names SHALL be validated: a name such as `12345` answers 400 "Incorrect firstname or lastname", an overlong one 400, and an unknown ID 404.

#### Scenario: Own names change

- **WHEN** a member of any type sends new letters-only `firstName` and `lastName` for themselves
- **THEN** the answer holds both names and `displayName` built from them

#### Scenario: Invalid name

- **WHEN** `firstName` is `12345`
- **THEN** the answer is 400 and the name is not stored

### Requirement: Nobody else edits the Owner

`updateMember` MUST refuse any caller other than the Owner who targets the Owner's profile with 403.

#### Scenario: DocSpace admin edits the Owner

- **WHEN** a DocSpace admin, Room admin, User or Guest calls `updateMember` for the Owner
- **THEN** the answer is 403 "Access denied"

### Requirement: Interface language of a profile

`updateMemberCulture` SHALL change only the caller's own language. Every other caller, the Owner and DocSpace admins included, SHALL get 403. An overlong culture name SHALL answer 400 and an unknown ID 404.

#### Scenario: Own language

- **WHEN** a member of any type sends `es` for themselves
- **THEN** the answer holds `cultureName` `es`

#### Scenario: Owner changes someone else's language

- **WHEN** the Owner calls `updateMemberCulture` for another member
- **THEN** the answer is 403 "Access denied"

### Requirement: Deleting one disabled profile

`deleteMember` SHALL delete only a disabled account and return its profile. An account that is not disabled SHALL answer 403, and an unknown ID 404. Who may delete which type is in `user-type-permissions`.

#### Scenario: Disabled member

- **WHEN** the Owner or a DocSpace admin deletes a disabled Room admin, User or Guest
- **THEN** the answer holds the deleted account's `id`

#### Scenario: Active guest

- **WHEN** the Owner deletes a Guest who is not disabled
- **THEN** the answer is 403

### Requirement: Deleting several disabled profiles

`removeUsers` (`PUT people/delete`) SHALL delete the disabled accounts listed in `userIds` and return them, SHALL answer 403 when an account is not disabled or the caller is not an administrator, and SHALL answer 400, not a server error, when the list is missing.

#### Scenario: Two disabled members

- **WHEN** the Owner or a DocSpace admin removes a disabled Room admin and a disabled User
- **THEN** the answer lists both with `status` Terminated and their type flags

#### Scenario: Wrong field name

- **WHEN** the body carries `userId` instead of `userIds`
- **THEN** the answer is 400

### Requirement: Deleted profile leaves the contact list

A deleted profile SHALL no longer appear in `getAllProfiles`, including when its content had been reassigned first.

#### Scenario: Reassigned, then deleted

- **WHEN** a disabled user's content is reassigned and the user is deleted
- **THEN** `getAllProfiles` does not list that user

### Requirement: Disabling and re-enabling accounts

`updateUserStatus` SHALL set the given status on the listed accounts and return them with their type flags unchanged. The permissions are in `user-type-permissions`.

#### Scenario: Owner disables one account of each type

- **WHEN** the Owner sends a Guest, a User, a Room admin and a DocSpace admin with status Terminated
- **THEN** the answer lists the four with `status` Terminated and each keeps its type flag

### Requirement: Listing accounts by status

`getByStatus` SHALL list accounts of the given status to the Owner and DocSpace admins, SHALL give a Room admin the accounts visible to them and an empty list for disabled accounts, and SHALL answer 403 to a User or a Guest.

#### Scenario: Room admin asks for disabled accounts

- **WHEN** a Room admin calls `getByStatus` with Terminated
- **THEN** the answer is 200 with no accounts

#### Scenario: User asks for active accounts

- **WHEN** a User calls `getByStatus` with Active
- **THEN** the answer is 403

### Requirement: Activation status through a confirmation link

`updateUserActivationStatus` SHALL accept only a caller authorised by an activation confirmation key and only a list naming that caller alone; any other list SHALL answer 400. It SHALL store the status and return the caller's profile.

#### Scenario: Someone else's ID

- **WHEN** the list holds an ID other than the caller's, or more than one ID
- **THEN** the answer is 400

#### Scenario: Owner confirms for the first time

- **WHEN** the portal Owner sets Activated for the first time on this portal
- **THEN** the welcome letter for administrators is sent once

### Requirement: Who may start an email change

`sendEmailChangeInstructions` (`POST people/email`) SHALL allow every user type on their own account, the Owner on anyone else's, and a DocSpace admin on Room admins and Users. All other pairs, including a DocSpace admin on another DocSpace admin or on the Owner, SHALL answer 403.

#### Scenario: Member on self

- **WHEN** a Room admin, User or Guest targets their own account
- **THEN** the answer is 200 with the instructions-sent message

#### Scenario: Peer administrator

- **WHEN** a DocSpace admin targets another DocSpace admin or the Owner
- **THEN** the answer is 403 "Access denied"

### Requirement: Immediate change versus instructions

`sendEmailChangeInstructions` SHALL change the address at once when the caller is the Owner or a DocSpace admin, and SHALL only send a letter when the caller is anyone else. The response text is the same in both cases.

#### Scenario: Owner changes their own address

- **WHEN** the Owner sends a new `email` for themselves
- **THEN** the answer is 200 and signing in with the new address works

#### Scenario: Member changes their own address

- **WHEN** a User sends a new `email` for themselves
- **THEN** the answer is 200, the letter goes out and the stored address stays until the link is followed

### Requirement: Validation of an email change

`sendEmailChangeInstructions` SHALL answer 400 to a malformed address or one longer than 255 characters, to an address another account holds and to an administrator's call with the current address, and SHALL answer 404 to an unknown, disabled or pending target.

#### Scenario: Malformed address

- **WHEN** `email` is not a valid address
- **THEN** the answer is 400 naming the Email field

#### Scenario: Unknown target

- **WHEN** `userId` matches no account
- **THEN** the answer is 404 "The user could not be found"

### Requirement: Applying an email change from a link

`changeUserEmail` (`PUT people/{userId}/email`) SHALL accept only a caller authorised by an email-change confirmation key, SHALL refuse a disabled or pending account and any caller other than the Owner on the Owner's account with 403, and SHALL store the new address in lower case and mark the account activated.

#### Scenario: Link followed

- **WHEN** the key holder sends a valid new address for an active account
- **THEN** the answer is the profile with that address

#### Scenario: Empty address

- **WHEN** neither `email` nor `encEmail` yields an address
- **THEN** the answer is 400

### Requirement: Reading a user photo

`getMemberPhoto` SHALL return the photo URLs of the caller's own profile to every user type, of any profile to the Owner and DocSpace admins, and of any profile except a Guest's to a Room admin. A User or a Guest asking for another account SHALL get 403, and an unknown ID 404.

#### Scenario: Room admin and a guest

- **WHEN** a Room admin asks for a Guest's photo
- **THEN** the answer is 403 "Access denied"

#### Scenario: User and another account

- **WHEN** a User or a Guest asks for anyone else's photo
- **THEN** the answer is 403 "Access denied"

### Requirement: Uploading a photo

`uploadMemberPhoto` SHALL let every user type upload an image for their own profile. With `autosave` the image becomes the avatar and the answer carries its URLs; without it the image is kept as a temporary file and the answer carries its name.

#### Scenario: Own avatar

- **WHEN** any user type uploads an image to their own ID
- **THEN** the answer has `success` true

### Requirement: A refused upload answers 200

`uploadMemberPhoto` SHALL report a refused file part in the body with `success` false rather than through the status code, except that a request with no file part at all SHALL answer 400 and an anonymous one 401.

#### Scenario: Someone else's profile

- **WHEN** any user type, the Owner included, uploads to another account's ID
- **THEN** the answer is 200 with `success` false and `message` "Security error."

#### Scenario: Image too large

- **WHEN** the file is larger than the limit
- **THEN** the answer is 200 with `success` false and a message that the maximum size (5 MB) is exceeded

#### Scenario: Unsupported type

- **WHEN** the file is an SVG image
- **THEN** the answer is 200 with `success` false

#### Scenario: No file part

- **WHEN** the multipart body has no file part
- **THEN** the answer is 400

### Requirement: Setting a photo from a URL

`updateMemberPhoto` (`PUT people/{userId}/photo`) SHALL work only on the caller's own profile: any other ID SHALL answer 403, an empty `files` 400 and an unknown ID 404. The server downloads the image itself after validating the URL and answers 403 to a refused URL.

#### Scenario: Administrator on another profile

- **WHEN** an administrator calls `updateMemberPhoto` for another account
- **THEN** the answer is 403

#### Scenario: Same URL as the current photo

- **WHEN** `files` equals the current photo's address
- **THEN** nothing is downloaded and the current URLs are returned

### Requirement: Cropping a photo

`createMemberPhotoThumbnails` SHALL work only on the caller's own profile. With `tmpFile` it SHALL make that temporary upload the avatar using the given area, and without it SHALL rebuild the sizes of the current avatar. Another ID SHALL answer 403, and an unknown ID or temporary file 404.

#### Scenario: Temporary upload becomes the avatar

- **WHEN** the caller sends `tmpFile` from an upload without `autosave`, with `width` and `height` zero
- **THEN** the whole image becomes the avatar, the temporary file is removed and the URLs are returned

### Requirement: Deleting a photo

`deleteMemberPhoto` SHALL remove the avatar of the caller's own profile for every user type. Any other caller, administrators included, SHALL get 403, and an unknown ID 404.

#### Scenario: Own photo

- **WHEN** a member of any type deletes their own photo
- **THEN** the answer is 200 with the URLs

#### Scenario: Owner on another profile

- **WHEN** the Owner deletes another account's photo
- **THEN** the answer is 403 "Access denied"

### Requirement: People search reads the stored names

The text filter of the people searches SHALL match the stored names and email address directly, not a search index, so an empty result is a real miss. The HTML-encoded `displayName` of a profile is not a valid search term when encoding changes it.

#### Scenario: Name with an apostrophe

- **WHEN** a profile's surname holds an apostrophe and a search sends the `displayName` as returned by the API
- **THEN** the search finds nothing, and sending the decoded name finds the profile

### Requirement: Filtered people lists

`getSimpleByFilter` and `searchUsersByExtendedFilter` SHALL find accounts of every type for the Owner and DocSpace admins, SHALL hide from a Room admin a Guest invited by someone else, and SHALL answer 403 to a User or a Guest.

#### Scenario: Room admin and someone else's guest

- **WHEN** a Room admin filters by the name of a Guest the Owner invited
- **THEN** the guest is not in the answer

#### Scenario: User filters

- **WHEN** a User or a Guest calls either operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Type flags in search results

`searchUsersByExtendedFilter` SHALL report each account's type through its flags, and the Owner SHALL be `isOwner` only.

#### Scenario: Searching for the Owner

- **WHEN** a caller finds the Owner through `searchUsersByExtendedFilter`
- **THEN** `isOwner` is true and `isAdmin`, `isRoomAdmin`, `isVisitor` and `isCollaborator` are false

### Requirement: Administrator-only searches

`getSearch`, `searchUsersByQuery` and `searchUsersByStatus` SHALL be available to the Owner and DocSpace admins only and SHALL answer 403 to a Room admin, a User or a Guest.

#### Scenario: Administrator finds any type

- **WHEN** the Owner or a DocSpace admin searches by email or name through `getSearch` or `searchUsersByQuery`
- **THEN** accounts of every type are found

#### Scenario: Room admin searches

- **WHEN** a Room admin calls any of the three
- **THEN** the answer is 403 "Access denied"

### Requirement: Searching by status

`searchUsersByStatus` SHALL look only among accounts of the status in the route.

#### Scenario: User gets disabled

- **WHEN** a User is found with status Active and is then disabled
- **THEN** the same search with status Terminated finds that user

### Requirement: People an entry can be shared with

`getUsersWithRoomShared`, `getUsersWithFoldersShared` and `getUsersWithFilesShared` SHALL answer to any non-guest caller who can read the entry. A User without access to it and every Guest, even one invited to the room, SHALL get 403; an unknown entry SHALL get 404.

#### Scenario: Invited User

- **WHEN** a User invited to a room filters by a fellow member's name
- **THEN** that member is found

#### Scenario: Invited Guest

- **WHEN** a Guest invited to the room calls `getUsersWithRoomShared`
- **THEN** the answer is 403 "Access denied"

### Requirement: Accounts and groups an entry can be shared with

`getAccountsEntriesWithRoomsShared`, `getAccountsEntriesWithFoldersShared` and `getAccountsEntriesWithFilesShared` SHALL answer only to a caller who can manage access to the entry and SHALL answer 403 to everyone else, including a User with editing access and a DocSpace admin outside the room.

#### Scenario: Room manager

- **WHEN** a Room admin who manages the room filters by a member's name
- **THEN** the single matching entry comes back

#### Scenario: Member with editing access

- **WHEN** a User with editing access calls the operation for a file in the room
- **THEN** the answer is 403 "Access denied"

#### Scenario: DocSpace admin outside the room

- **WHEN** a DocSpace admin who is not in the Owner's room calls the operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Accounts search needs a text

The accounts-and-groups searches SHALL return an empty list with a zero total when `filterValue` is empty, instead of listing everything.

#### Scenario: No filter value

- **WHEN** a caller who can manage access sends no `filterValue`
- **THEN** the answer is 200 with no entries and a total of zero

### Requirement: Theme of an account

`getPortalTheme` and `changePortalTheme` (`people/theme`) SHALL read and set the theme of the calling account, for every user type. The default SHALL be `System`; `Base`, `Dark` and `System` SHALL be accepted.

#### Scenario: Fresh account

- **WHEN** a member who has never changed the theme calls `getPortalTheme`
- **THEN** `theme` is System

#### Scenario: Switching through the themes

- **WHEN** any user type sets Base, then Dark, then System
- **THEN** each answer holds the theme just set

### Requirement: Whether a reassignment is necessary

`necessaryReassign` SHALL tell the Owner or a DocSpace admin whether a user still holds rooms, or for a conversion to Guest shared entries. A non-administrator SHALL get 403, and so SHALL a DocSpace admin asking about a DocSpace admin.

#### Scenario: Rooms exist

- **WHEN** the Owner or a DocSpace admin asks and rooms exist
- **THEN** the answer is true

#### Scenario: Room admin asks

- **WHEN** a Room admin, User or Guest calls `necessaryReassign`
- **THEN** the answer is 403 "Access denied"

### Requirement: Starting a data reassignment

`startReassign` SHALL queue the transfer of a disabled user's data to an active Room admin or DocSpace admin and return the running job. An active source, the Owner, the caller or an unsuitable recipient SHALL answer 400; a non-administrator SHALL get 403.

#### Scenario: Disabled source

- **WHEN** the Owner or a DocSpace admin reassigns a disabled Room admin's data to themselves
- **THEN** the job comes back with `isCompleted` false and an empty `error`

#### Scenario: Active source

- **WHEN** the source account is not disabled
- **THEN** the answer is 400 "Can not reassign data"

### Requirement: Watching a data reassignment

`getReassignProgress` SHALL report the job to the Owner and to a DocSpace admin, until it reaches Completed with an empty `error`. A non-administrator SHALL get 403, and so SHALL a DocSpace admin asking about another DocSpace admin.

#### Scenario: Polling to the end

- **WHEN** the Owner polls after `startReassign`
- **THEN** the status reaches Completed and `error` is empty

#### Scenario: Peer administrator's job

- **WHEN** a DocSpace admin asks about a job for another DocSpace admin started by the Owner
- **THEN** the answer is 403 "Access denied"

### Requirement: Cancelling a data reassignment

`terminateReassign` SHALL cancel the queued job and return it with `isCompleted` true and status Canceled. A non-administrator SHALL get 403, and so SHALL a DocSpace admin on another DocSpace admin's job.

#### Scenario: Owner cancels

- **WHEN** the Owner cancels a job just started
- **THEN** the answer has status Canceled and an empty `error`

### Requirement: Starting a data removal

`startRemove` SHALL queue the erasure of a disabled user's data and return the running job. The rooms of that user SHALL pass to the Owner. The Owner, the caller, an account that is not disabled or an unknown ID SHALL answer 400; a non-administrator SHALL get 403.

#### Scenario: Room owner removed

- **WHEN** the data of a disabled DocSpace admin or Room admin who owns a room is removed
- **THEN** the job runs to completion and the room's `ownedBy` is the Owner

### Requirement: Watching and cancelling a data removal

`getRemoveProgress` SHALL report the removal job and `terminateRemove` SHALL cancel it, both for the Owner and DocSpace admins. A Room admin, User or Guest SHALL get 403.

#### Scenario: Reading the job

- **WHEN** the Owner or a DocSpace admin reads the job after `startRemove`
- **THEN** `error` is empty

#### Scenario: Cancelling

- **WHEN** the Owner or a DocSpace admin calls `terminateRemove`
- **THEN** the call succeeds

#### Scenario: Non-administrator

- **WHEN** a Room admin, User or Guest calls either operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Asking to delete one's own profile

`sendInstructionsToDelete` (`PUT people/self/delete`) SHALL send the caller a confirmation letter and name the caller's address in the answer. The Owner and an LDAP account SHALL get 403.

#### Scenario: Member asks

- **WHEN** a DocSpace admin, Room admin, User or Guest calls the operation
- **THEN** the answer holds the caller's email

#### Scenario: Owner asks

- **WHEN** the Owner calls the operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Deleting a guest's personal folder

`startDeletePersonalFolder` SHALL queue the deletion of the caller's personal folder only for a Guest who has one. Every other user type, and a Guest without such a folder, SHALL get 403.

#### Scenario: Not a guest

- **WHEN** the Owner, a DocSpace admin, a Room admin or a User calls the operation
- **THEN** the answer is 403 "Access denied"

#### Scenario: Guest without a personal folder

- **WHEN** a Guest whose personal folder was never created calls the operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Watching the personal folder deletion

`getDeletePersonalFolderProgress` SHALL report the job of the calling account only, and SHALL answer 200 with an empty body when nothing is queued for it.

#### Scenario: Nothing queued

- **WHEN** a caller with no queued deletion polls
- **THEN** the answer is 200 with an empty body

### Requirement: Watching a type conversion

`getUserTypeUpdateProgress` SHALL report a conversion started by `startUserTypeUpdate` to the Owner and DocSpace admins, ending with `isCompleted` true, `percentage` 100 and an empty `error`. A Room admin, User or Guest SHALL get 403. Who may convert whom is in `user-type-permissions`.

#### Scenario: Polling to the end

- **WHEN** the Owner polls a demotion of a DocSpace admin to User
- **THEN** the job ends complete at 100 percent with no error

#### Scenario: Room admin polls

- **WHEN** a Room admin, User or Guest calls the operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Cancelling a type conversion

`terminateUserTypeUpdate` SHALL cancel the conversion for the Owner and DocSpace admins, whoever started it, and SHALL return it with status Canceled. A Room admin, User or Guest SHALL get 403.

#### Scenario: Administrator cancels the Owner's conversion

- **WHEN** a DocSpace admin cancels a demotion the Owner started before it ran
- **THEN** the answer has `isCompleted` true and status Canceled, and the target is still a DocSpace admin

### Requirement: Listing third-party providers

`getThirdPartyAuthProviders` SHALL list the providers enabled on the installation, each with its name, login URL and whether it is linked to the caller. It SHALL answer without authentication; for an anonymous caller nothing is linked.

#### Scenario: Fresh account

- **WHEN** any user type with no linked account calls the operation
- **THEN** every entry names its `provider` and none has `linked` true

#### Scenario: Nothing configured

- **WHEN** no provider is enabled on the installation
- **THEN** the list is empty

### Requirement: Linking a third-party account

`linkThirdPartyAccount` SHALL attach the identity in `serializedProfile` to the calling profile. An identity already linked to a profile, or a missing or foreign `serializedProfile`, SHALL answer 400; a tariff without third-party authorization SHALL answer 403. A cancellation at the provider is not an error.

#### Scenario: Authorization cancelled

- **WHEN** the profile carries the provider's cancellation
- **THEN** the answer is 200 and nothing is linked

### Requirement: Unlinking a third-party account

`unlinkThirdPartyAccount` SHALL remove the link between the calling profile and the named `provider` and SHALL answer 200.

#### Scenario: Removing a link

- **WHEN** a caller unlinks a provider
- **THEN** that provider is reported with `linked` false from then on

### Requirement: Joining through a third-party account

`signupThirdPartyAccount` SHALL answer without authentication and, given a valid invitation `key` and `serializedProfile`, SHALL create or find the profile, link the identity and return the profile. An invalid or expired invitation SHALL answer 403. The invitation rules are in `invitations`.

#### Scenario: Invalid invitation

- **WHEN** `key` is not a valid invitation
- **THEN** the answer is 403

#### Scenario: Authorization cancelled

- **WHEN** the profile carries the provider's cancellation
- **THEN** the answer is 200 with an empty body and no profile is created

### Requirement: Who may manage groups

Creating, updating, deleting and changing the membership of a group SHALL be open to the Owner and DocSpace admins and SHALL answer 403 to a Room admin, a User and a Guest, including a regular User who is that group's manager.

#### Scenario: DocSpace admin

- **WHEN** a DocSpace admin calls `addGroup`, `updateGroup`, `addMembersTo`, `setMembersTo`, `moveMembersTo`, `removeMembersFrom` or `setGroupManager`
- **THEN** the call succeeds

#### Scenario: Manager who is a User

- **WHEN** a User who manages the group calls `removeMembersFrom` or `setMembersTo`
- **THEN** the answer is 403

### Requirement: Who may read groups

`getGroups`, `getGroup` and `getGroupByUserId` SHALL answer to the Owner, DocSpace admins and Room admins, and SHALL answer 403 to a User and a Guest, even when they ask for their own groups.

#### Scenario: User asks for their own groups

- **WHEN** a User or a Guest calls `getGroupByUserId` with their own ID
- **THEN** the answer is 403

#### Scenario: Room admin reads a group

- **WHEN** a Room admin calls `getGroup`
- **THEN** the group comes back

### Requirement: Creating a group

`addGroup` SHALL create a group from `groupName` with an optional `groupManager` and `members`, the manager also becoming a member. A missing, empty, blank or overlong name, an empty manager string, an unknown manager or member, and wrongly typed fields SHALL answer 400.

#### Scenario: Manager only

- **WHEN** a group is created with a manager and an empty `members` list
- **THEN** `membersCount` is 1

#### Scenario: No manager

- **WHEN** no `groupManager` is sent
- **THEN** the group is created

#### Scenario: Unknown member

- **WHEN** `members` holds an ID no account has
- **THEN** the answer is 400 and no group is created

#### Scenario: Repeated member

- **WHEN** `members` lists one user twice
- **THEN** that user is a member once

### Requirement: Reading a group

`getGroup` SHALL return the group with its members by default and without them when `includeMembers` is false. An unknown or malformed ID SHALL answer 404.

#### Scenario: Members left out

- **WHEN** `includeMembers` is false
- **THEN** the answer holds no members

#### Scenario: Malformed ID

- **WHEN** the ID in the route is not a GUID
- **THEN** the answer is 404

### Requirement: Groups of a user

`getGroupByUserId` SHALL list summaries of the groups a user belongs to, following later membership changes, and SHALL answer with an empty list for a user with no groups or an unknown ID. A malformed ID SHALL answer 404.

#### Scenario: Membership changes

- **WHEN** a user is added to a group and later removed from it
- **THEN** the group appears and then disappears from the list

#### Scenario: Unknown user

- **WHEN** the ID is a well-formed GUID no account has
- **THEN** the answer is 200 with an empty list

### Requirement: Listing and filtering groups

`getGroups` SHALL filter by `filterValue` (partial name), `userId` and `manager`, SHALL page by `count` and `startIndex` and SHALL sort by name. An unknown `userId` gives an empty list; an unknown `sortBy` is ignored; a malformed `userId` or unknown `sortOrder` answers 400.

#### Scenario: Partial name

- **WHEN** `filterValue` is part of a group's name
- **THEN** that group is returned

#### Scenario: Manager only

- **WHEN** `userId` and `manager` true are sent
- **THEN** only the groups that user manages are returned

#### Scenario: Past the end

- **WHEN** `startIndex` is above the total
- **THEN** the list is empty

### Requirement: Updating a group

`updateGroup` SHALL apply `groupName`, `groupManager`, `membersToAdd` and `membersToRemove` together and leave a group unchanged for an empty body. A user in both lists SHALL end up outside the group. An unknown group SHALL answer 404.

#### Scenario: All fields at once

- **WHEN** all four fields are sent
- **THEN** the name and manager change and the membership reflects both lists

#### Scenario: Same user in both lists

- **WHEN** one user is in `membersToAdd` and `membersToRemove`
- **THEN** the user is not a member afterwards

### Requirement: Update ignores unknown users

`updateGroup` SHALL silently skip an unknown `groupManager` and unknown IDs in `membersToAdd`, and a non-member in `membersToRemove`, answering 200.

#### Scenario: Unknown manager

- **WHEN** `groupManager` is an ID no account has
- **THEN** the answer is 200 and the group's manager is not that ID

### Requirement: Deleting a group

`deleteGroup` SHALL delete the group so that it leaves `getGroups` and `getGroup` answers 404 for it. A second delete and an unknown ID SHALL answer 404.

#### Scenario: Group deleted

- **WHEN** a group is deleted
- **THEN** `getGroup` answers 404 and the group is not in `getGroups`

### Requirement: Moving members between groups

`moveMembersTo` SHALL move every member of `fromId` into `toId`, keeping the target's members and manager and not duplicating a shared member, and SHALL return the target. An unknown or deleted group on either side SHALL answer 404. A group moved onto itself SHALL stay unchanged.

#### Scenario: One member moved

- **WHEN** the source group has one member
- **THEN** the member is in the target and no longer in the source

#### Scenario: Same group on both sides

- **WHEN** `fromId` equals `toId`
- **THEN** the group keeps its members

#### Scenario: Empty source

- **WHEN** the source group has no members
- **THEN** the target is unchanged

### Requirement: Replacing the members of a group

`setMembersTo` SHALL make the group hold exactly the valid listed users. A list with no valid user, empty, or absent SHALL answer 400 and change nothing; so SHALL a list holding only disabled users or a malformed ID. A manager left out SHALL lose both membership and the manager role.

#### Scenario: Manager not in the list

- **WHEN** the new list omits the current manager
- **THEN** the manager is no longer a member and the group has no manager

#### Scenario: Only a disabled user

- **WHEN** the list holds one disabled user
- **THEN** the answer is 400 and the existing members stay

#### Scenario: Only unknown IDs

- **WHEN** the list holds an ID no account has
- **THEN** the answer is 400

### Requirement: Adding members to a group

`addMembersTo` SHALL add the listed users to the existing members and return the group. A repeated user SHALL not be duplicated; unknown IDs, an empty list and absent `members` SHALL leave the group unchanged with 200. An unknown group SHALL answer 404.

#### Scenario: Adding to existing members

- **WHEN** two users are added to a group with members
- **THEN** `membersCount` rises by two and the earlier members stay

#### Scenario: Unknown ID

- **WHEN** `members` holds an ID no account has
- **THEN** the answer is 200 and that ID is not a member

### Requirement: Removing members from a group

`removeMembersFrom` SHALL remove the listed users without deleting their accounts and keep the group's name, manager and other members. Unknown IDs, non-members, an empty list and null `members` SHALL leave the group unchanged. A malformed ID in the list SHALL change nothing and not be a server error.

#### Scenario: Last member removed

- **WHEN** every member is removed
- **THEN** the group still exists with the same `id`

#### Scenario: Valid and malformed IDs together

- **WHEN** the list holds a member and a malformed ID
- **THEN** the status is below 500 and the group is unchanged

#### Scenario: Removed user stays on the portal

- **WHEN** a member is removed
- **THEN** `getProfileByUserId` still returns that user and `getGroupByUserId` no longer lists the group

### Requirement: Setting the group manager

`setGroupManager` SHALL make the user the manager, adding them as a member and keeping the other members and the name, for a member or a non-member alike, and repeating it SHALL change nothing. An unknown group or user SHALL answer 404, and an empty, null or absent `userId` 400.

#### Scenario: New manager from outside

- **WHEN** the new manager is not a member
- **THEN** the user is manager and member, and the earlier members stay

#### Scenario: Unknown user

- **WHEN** `userId` is an ID no account has
- **THEN** the answer is 404

### Requirement: Groups an entry is shared with

`getGroupsWithRoomsShared`, `getGroupsWithFoldersShared` and `getGroupsWithFilesShared` SHALL list the portal's groups, each once, with `shared` true for those holding access and otherwise not true. A group SHALL stay shared after its access level changes, lose the flag when access is removed, and stay listed when a member is disabled.

#### Scenario: Group without access

- **WHEN** one group has access to the file and another has not
- **THEN** the first has `shared` true and the second does not

#### Scenario: Access removed

- **WHEN** the group's access is removed
- **THEN** it is no longer marked shared

### Requirement: Everyone in the group lists

The group lists of a file or folder SHALL include the system group Everyone, marked `isSystem`, counted and paged like any group, marked `shared` when it holds access directly or through a parent folder, and found by its name.

#### Scenario: Fresh portal

- **WHEN** a file is shared with nobody on a fresh portal
- **THEN** Everyone is the only entry, not shared, and the count is 1

#### Scenario: Through the parent folder

- **WHEN** Everyone holds access on the parent folder
- **THEN** Everyone is shared in the file's list, and `excludeShared` hides it

### Requirement: Filtering the group lists

The group lists SHALL hide groups already holding access when `excludeShared` is true and return them when false, SHALL filter by `filterValue` with no error for special characters, treat an empty value as no filter, and page by `count` and `startIndex`.

#### Scenario: Exclude shared

- **WHEN** `excludeShared` is true
- **THEN** groups holding access are not in the answer

#### Scenario: No match

- **WHEN** `filterValue` matches no group
- **THEN** the list is empty

### Requirement: Who may read the group lists

The group lists SHALL answer to a caller who can manage access to the entry, such as its owner, and SHALL answer 403 otherwise, including a DocSpace admin on the Owner's entry, a User or Guest without access and a User invited with read access to the room.

#### Scenario: Owner of the file

- **WHEN** a User who owns a file shared with a group calls `getGroupsWithFilesShared`
- **THEN** that group has `shared` true

#### Scenario: DocSpace admin and the Owner's room

- **WHEN** a DocSpace admin calls `getGroupsWithRoomsShared` for the Owner's room
- **THEN** the answer is 403

### Requirement: Unknown entries in the group lists

The group lists SHALL answer 404 for an entry that does not exist, a deleted folder, a negative room ID, a non-numeric ID and an empty ID.

#### Scenario: Non-numeric ID

- **WHEN** the route is `group/file/not-a-number`
- **THEN** the answer is 404

### Requirement: Guests of a Room admin's list

`deleteGuests` SHALL let a Room admin remove guests from their own guest list only, and SHALL answer 403 to the Owner, a DocSpace admin, a User, a Guest and a Room admin naming a guest from someone else's list.

#### Scenario: Own guest

- **WHEN** a Room admin removes a guest they invited
- **THEN** the guest is excluded from that admin's rooms

#### Scenario: Someone else's guest

- **WHEN** a Room admin names a guest invited by another user
- **THEN** the answer is 403

### Requirement: Marking the gift message as read

`markGiftMessageAsRead` (`POST portal/present/mark`) SHALL store for the calling account that the gift message was read and SHALL answer 200 with no content, even when saving the flag fails.

#### Scenario: Reading the message

- **WHEN** an authenticated caller calls the operation
- **THEN** the answer is 200 with no content

### Requirement: Letters after portal registration

`sendCongratulations` (`POST portal/sendcongratulations`) SHALL answer without authentication and, given the `userid` and a confirmation `key` no older than one hour, SHALL send that account the welcome letters on every such call. Any other key SHALL answer 403 and send nothing.

#### Scenario: Valid key

- **WHEN** the key matches the user and is less than an hour old
- **THEN** the answer is 200 and the letters are sent

#### Scenario: Expired key

- **WHEN** the key is older than one hour
- **THEN** the answer is 403 and nothing is sent
