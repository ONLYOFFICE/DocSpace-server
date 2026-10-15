# notifications Specification

## Purpose

Defines how the server tells people what happened: the channels it delivers through (e-mail,
Telegram, mobile push through Firebase, and the in-portal "new items" badges and feeds), the
per-user notification settings (`settings/notification/*`: badges, room activity, daily feed, tips,
muted rooms), the Telegram link of a profile, the push device registration, the administrator-side
SMTP settings with the test message, the sign-in page contact form and join invitation, and the
catalogue of letters: which event sends which letter, to whom, over which channel, and whether a
subscription setting silences it. Letter texts are named by the subject a reader sees. Real-time
socket events, the audit trail, webhooks and the invitation letters' triggers are other
capabilities (`history-and-audit`, `webhooks-and-api-keys`, `invitations`,
`backup-and-restore`) and are referenced only.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`02_Settings/Notifications`,
`02_Settings/Messages`, `05_Security/Smtp`), `products/ASC.Files/Tests` (`03_Rooms/NewItems`),
the letter suite `common/Tests/ASC.Notify.Tests` (subjects, wording, schedule predicates,
recipients), the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations named
by `operationId`), the product code of the notify sources and the notify service, and the release
notes (v2.6, v3.1, v3.1.1, v3.5, v3.6, v3.6.1, v3.7, v4.0). The evidence per scenario, the
disagreements between notes, the OpenAPI document and the server, and the suspected defects are in
`verification.md` next to this file.

## Requirements

### Requirement: Delivery channels of the installation

`getNotificationChannels` SHALL list the channels the notify service of the installation is
configured with, each with `name` and `isEnabled`: `email.sender` (always enabled when listed) and
`telegram.sender` (enabled only while the portal has a Telegram bot name and token). Push is
delivered directly through Firebase and is not a listed channel. Any signed-in member SHALL get
the same list; an anonymous caller is refused with 401.

#### Scenario: Member reads the channel list

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls `getNotificationChannels`
- **THEN** the system answers 200 with a non-empty `channels` list whose first entry has a `name`

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller calls `getNotificationChannels`
- **THEN** the system answers 401

### Requirement: Queued delivery of e-mail and Telegram

A letter SHALL NOT be sent inline by the request that triggers it: the request publishes a
notification event, and the notify service renders the letter per recipient and hands it to the
channel senders. A message that fails to send SHALL be retried every 5 minutes up to 10 attempts
and then marked as a fatal error; sent messages are kept for 7 days. A 200 on the triggering
request therefore means "accepted", not "delivered".

#### Scenario: Mail server unreachable

- **WHEN** the portal's SMTP relay refuses a letter
- **THEN** the triggering request still answers as if the letter were sent, and the notify service retries the message up to 10 times

### Requirement: Notification kinds a member can switch

`getNotificationSettings` and `setNotificationSettings` SHALL read and switch one kind for the
calling account only: `type` 0 badges, 1 room activity, 2 daily feed, 3 useful tips, with
`isEnabled`. Every signed-in member may do so; an anonymous caller gets 401. All four kinds
SHALL be on for an account that never changed them. The write SHALL echo the request.

#### Scenario: Any member enables any kind

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls `setNotificationSettings` with any of the four `type` values and `isEnabled: true`
- **THEN** the system answers 200 with the same `type` and `isEnabled: true`

#### Scenario: Reading back a switched kind

- **WHEN** a member switches a kind on and then calls `getNotificationSettings` with that `type`
- **THEN** the system answers 200 with that `type` and `isEnabled: true`

#### Scenario: Switching a kind off is visible at once

- **WHEN** the owner switches badges, room activity, daily feed or useful tips off and reads the kind back, then switches it on and reads again
- **THEN** the first read reports `isEnabled: false` and the second `isEnabled: true`

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller calls `setNotificationSettings` or `getNotificationSettings`
- **THEN** the system answers 401

### Requirement: Muting single rooms

`setRoomsNotificationStatus` SHALL add the room named by `roomsId` to the caller's muted list
when `mute` is true and remove it when false; `getRoomsNotificationSettings` SHALL return that
list as `disabledRooms`. The id is stored as an opaque value without checking the room or the
caller's access. Both operations are open to every signed-in member; anonymous callers get 401.

#### Scenario: Member mutes a room

- **WHEN** any member calls `setRoomsNotificationStatus` with a `roomsId` and `mute: true`
- **THEN** the system answers 200 and `disabledRooms` contains that id

#### Scenario: Reading the muted list

- **WHEN** the member then calls `getRoomsNotificationSettings`
- **THEN** the system answers 200 and `disabledRooms` still contains the id

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller calls `setRoomsNotificationStatus` or `getRoomsNotificationSettings`
- **THEN** the system answers 401

### Requirement: What each kind silences

Badges off SHALL hide the new-item counters of the Files responses. Room activity off SHALL
stop the hourly room digest and the letters and pushes a room or a document sends at once
(sharing, mention, room invitation, role change, archive, new document, upload, folder, room
deleted). Daily feed off SHALL stop the daily digest. Useful tips off SHALL stop only the
after-registration marketing letters. A muted room SHALL be left out of the at-once letters,
pushes and mentions for that member alone.

#### Scenario: Room activity switched off

- **WHEN** a member has switched room activity off and somebody shares a document with them or mentions them
- **THEN** no letter and no push is sent to that member

#### Scenario: Useful tips switched off

- **WHEN** a member has switched useful tips off
- **THEN** the day-2 to day-14 after-registration letters are not sent to them, while a payment warning still is

#### Scenario: Muted room

- **WHEN** a member has muted a room and another member mentions them in a document of that room
- **THEN** no mention letter is sent to that member, while the same mention in another room is

### Requirement: Telegram link of a profile

`linkTelegram` SHALL return the caller's personal `t.me/<bot>?start=<token>` link, reusing an
outstanding token or issuing a fresh one, and an empty answer when the portal has no bot name.
`checkTelegram` SHALL report `status` 0 not linked, 1 linked (with `username`), 2 waiting for the
link to be opened. `unlinkTelegram` SHALL drop the portal-side link and answer `true` even when
nothing was linked. The bot keys are offered to the admin on every tariff, the free one included.

#### Scenario: Link requested twice

- **WHEN** a member calls `linkTelegram` twice while the first token is still valid
- **THEN** both calls answer 200 with the same link

#### Scenario: Portal without a bot

- **WHEN** a member calls `linkTelegram` on a portal whose Telegram keys are empty
- **THEN** the system answers 200 with an empty link and `checkTelegram` never reaches status 1

#### Scenario: Unlink without a link

- **WHEN** a member who never linked Telegram calls `unlinkTelegram`
- **THEN** the system answers 200 with `true`

### Requirement: Push device registration

`docRegisterPusnNotificationDevice` SHALL store the caller's Firebase device token for the
Documents application with `isSubscribed` and return the registration with `application` `doc`;
a known token is returned as stored. `subscribeDocumentsPushNotification` SHALL switch
`isSubscribed` of a registered token and answer empty for an unknown one. Pushes SHALL go only to
subscribed devices while Firebase credentials are configured; a token Firebase rejects is deleted.

#### Scenario: Device registered then unsubscribed

- **WHEN** a member registers a token and then calls `subscribeDocumentsPushNotification` with that token and `isSubscribed: false`
- **THEN** the token stays stored and the device receives no further pushes

#### Scenario: Subscription change for an unknown token

- **WHEN** a member calls `subscribeDocumentsPushNotification` with a token they never registered
- **THEN** the system answers 200 with an empty body and stores nothing

### Requirement: Push notification events

The system SHALL push to the subscribed devices of a room member when somebody else creates a
document in the room, uploads a file (up to three uploads within 30 seconds one by one, more
collapsed into one push with the count), creates a folder, invites them to the room or an AI
agent, changes their role, archives the room, shares a document or folder with them, mentions
them or submits a form. The actor never receives their own push.

#### Scenario: Several files uploaded at once

- **WHEN** a member uploads five files into a room within half a minute
- **THEN** the other members receive one push naming the count instead of five

#### Scenario: Actor excluded

- **WHEN** a member creates a document in a room
- **THEN** every other member subscribed to room activity, not muting the room and holding a subscribed device gets the push, and the creator gets none

### Requirement: New items of a room

`getNewRoomItems` SHALL list, grouped by day, the files somebody else created in a room since
the caller last opened it; `getNewFolderItems` on the room id SHALL report the same entries. A
new subfolder is not an entry, a file inside it is. Reading clears nothing; opening the room
clears everything. The caller needs access to the room (any level; a DocSpace admin always),
otherwise 403; an unknown or deleted room is 404; anonymous 401.

#### Scenario: File created by somebody else

- **WHEN** a member with Read access opened the room and the owner then creates a file in it, or in a subfolder of it
- **THEN** the member's `getNewRoomItems` lists that file with `title`, `createdBy` and `updated`

#### Scenario: Own file and a subfolder

- **WHEN** the member creates a file themselves, or the owner creates a subfolder
- **THEN** neither appears in the member's list

#### Scenario: Files that predate the first visit

- **WHEN** the owner created a file before the member first opened the room
- **THEN** it is not reported, and opening the room again empties the list

#### Scenario: Deleted file

- **WHEN** a reported file is deleted
- **THEN** it disappears from the list

#### Scenario: Who may read

- **WHEN** a User, a Guest or a Room admin who is not a member calls `getNewRoomItems`
- **THEN** the system answers 403, while an invited member of any access level, an invited Guest and a non-member DocSpace admin get 200

#### Scenario: Missing, deleted and archived rooms

- **WHEN** the id is unknown, 0 or belongs to a deleted room
- **THEN** the system answers 404, while an archived room still answers 200 to its owner

### Requirement: New items across all rooms

`getRoomsNewItems` SHALL aggregate the same entries across every active room the member can
see, grouped by day and then by room with the room's `title`, and SHALL leave out rooms the
member has no access to. An anonymous caller is refused with 401.

#### Scenario: Two rooms with new files

- **WHEN** a member of rooms A and B opened both and the owner creates a file in each
- **THEN** `getRoomsNewItems` lists both files under their rooms

#### Scenario: Room without access

- **WHEN** the owner also creates a file in a room the member is not invited to
- **THEN** neither that file nor that room appears in the member's answer

#### Scenario: Agreement with the per-room list

- **WHEN** a file is new in one room
- **THEN** it is reported by both `getRoomsNewItems` and `getNewRoomItems` of that room

### Requirement: Daily feed digest

Every hour, for each active portal with audit activity the previous day whose local time is 7
o'clock (configurable) and whose tariff is not in the not-paid state, the system SHALL send every
active member subscribed to the daily feed "What's new on your portal <host> for <date>" by e-mail
and Telegram, listing yesterday's events they may see: files created, comments, rooms and AI
agents created, renamed or archived, members removed, users added or updated. Nothing to report,
no letter.

#### Scenario: Member with activity

- **WHEN** another member created a file yesterday in a room the recipient can read and the recipient keeps the daily feed on
- **THEN** the recipient gets the digest at 7 o'clock portal time listing that file

#### Scenario: Not-paid portal

- **WHEN** the portal's tariff is not paid
- **THEN** no daily digest is sent

### Requirement: Hourly room activity digest

Every hour the system SHALL send every active member subscribed to room activity the letter
"What's new on your portal <host> for the last hour" listing the past hour's uploads, edits,
members added to rooms or agents, role changes, type changes and form-filling steps the member
may see, skipping muted rooms and members with nothing to report. It goes by e-mail and
Telegram.

#### Scenario: Upload in the past hour

- **WHEN** a member uploaded a file into a room an hour ago
- **THEN** the other members of the room subscribed to room activity get the hourly letter naming the file

### Requirement: SMTP settings of the portal

`getSmtpSettings`, `saveSmtpSettings` and `resetSmtpSettings` SHALL require the portal-settings
right: owner and DocSpace admin 200, Room admin, User and Guest 403 "Access denied", anonymous
401, hidden SMTP section 402. `credentialsUserPassword` SHALL always come back empty. Saving
SHALL persist without connecting, default `port` to 25 and keep credentials only with
`enableAuth`. Reset SHALL answer `isDefaultSettings: true` with the installation's values
(standalone) or empty ones (hosted).

#### Scenario: Admin saves and reads the settings

- **WHEN** the owner or a DocSpace admin calls `saveSmtpSettings` with host, port, sender address, display name, credentials, `enableSSL` and `enableAuth`, then `getSmtpSettings`
- **THEN** both answer 200 echoing `host`, `port`, `senderAddress`, `senderDisplayName`, `enableAuth` and `enableSSL`

#### Scenario: Field too long

- **WHEN** `host`, `senderAddress`, `senderDisplayName` or `credentialsUserName` is 256 characters long
- **THEN** the system answers 400 "The field <name> must be a string with a maximum length of 255."

#### Scenario: Reset to defaults

- **WHEN** the owner or a DocSpace admin calls `resetSmtpSettings` after saving settings
- **THEN** the system answers 200 with `isDefaultSettings: true`

#### Scenario: Non-admin and anonymous callers

- **WHEN** a Room admin, a User or a Guest calls any of the three operations
- **THEN** the system answers 403 "Access denied"; an unauthenticated caller gets 401

### Requirement: SMTP test message

`testSmtpSettings` SHALL queue one job per portal that sends "SMTP test message" through the
stored settings to the caller's address and answer the job state (`id`, `percents`, `status`,
`completed`, `error`); while a job runs the same job is returned. `getSmtpOperationStatus` SHALL
return that state; the first answer with `completed` true discards the job. Both need the
portal-settings right (403 "Access denied", 401 anonymous); the test allows 5 calls per 15 minutes.

#### Scenario: Admin starts a test

- **WHEN** the owner or a DocSpace admin saves settings and calls `testSmtpSettings`
- **THEN** the system answers 200 with a non-empty `id` and an empty `error`

#### Scenario: Non-admin reads the status

- **WHEN** a Room admin, a User or a Guest calls `getSmtpOperationStatus` or `testSmtpSettings`
- **THEN** the system answers 403 "Access denied"; an unauthenticated caller gets 401

### Requirement: Contact form for administrators

`enableAdminMessageSettings` SHALL switch the sign-in page contact form on or off portal-wide
with `turnOn`, needing the portal-settings right (403 "Access denied", 401 anonymous), and answer
"Settings have been successfully updated". `sendAdminMail` SHALL accept `email` and `message`
from anybody (a solved CAPTCHA from an anonymous caller when configured) and queue "User message
to administrators" to the admins subscribed to administrator notifications, by e-mail and Telegram.

#### Scenario: Admin toggles the form

- **WHEN** the owner or a DocSpace admin calls `enableAdminMessageSettings` with `turnOn` true, then false
- **THEN** each call answers 200 with status 0 and the message "Settings have been successfully updated"

#### Scenario: Non-admin toggles the form

- **WHEN** a Room admin, a User or a Guest calls `enableAdminMessageSettings`
- **THEN** the system answers 403 "Access denied"; an unauthenticated caller gets 401

#### Scenario: Visitor writes to the administrators

- **WHEN** the form is on and a visitor posts a real address and a non-empty message
- **THEN** the system answers 200 with a confirmation and the admins receive the letter carrying the address and the text

### Requirement: Join invitation from the sign-in page

`sendJoinInviteMail` SHALL send the letter "Invitation to join <host> portal" with a join link to
`email` only while the portal publishes a trusted-domain policy; without one every caller,
signed in or not, SHALL get 405 "Method not available". An address of an existing member or
outside the trusted domains SHALL be refused with 400.

#### Scenario: No trusted domains

- **WHEN** the owner, a DocSpace admin, a Room admin, a User, a Guest or an anonymous visitor calls `sendJoinInviteMail` on a portal without trusted domains
- **THEN** the system answers 405 "Method not available"

### Requirement: Activation and welcome letters

An account invited by e-mail SHALL get "You are invited to <brand>" (User and Guest) with the
confirmation link; a self-registration "Complete <brand> registration"; the portal creator
"Welcome to <brand>!" with the activation or password link. After the link is followed the new
member SHALL get "Welcome to <brand>!" and, on a hosted portal, the owner also "Discover business
subscription of <brand>". The edition (hosted, Enterprise, white-label, open-source) selects the
text, not the subject.

#### Scenario: Member joins

- **WHEN** an invited User completes the registration
- **THEN** they receive "Welcome to <brand>!" at the address they registered with

#### Scenario: Portal owner activated on a hosted portal

- **WHEN** the owner of a hosted portal is activated
- **THEN** they receive "Welcome to <brand>!" and then "Discover business subscription of <brand>"

### Requirement: Password and e-mail letters

The system SHALL send "Confirm changing your password" when a change is requested, "Set up a
password for <brand>" for a first password, and "Your password was changed successfully" with
date, device, browser and location afterwards; "Confirm changing your email" to a new address;
"<brand>. Please activate your email address" on an activation request; "<brand>. Mobile phone
change request" and "<brand>. Security notification" on phone and two-factor resets. No setting
silences them.

#### Scenario: Password changed

- **WHEN** a member changes their password
- **THEN** they receive "Your password was changed successfully" naming the browser and the platform, HTML-escaped

#### Scenario: First password

- **WHEN** a password is assigned to an account for the first time
- **THEN** the letter is "Set up a password for <brand>" with the set-password button, not the change letter

### Requirement: Account removal and self-lock letters

A member asking to delete their own profile SHALL get "Disabling account on <host>" with the
confirmation button; when the deletion is confirmed every DocSpace admin SHALL get "User has
deleted his/her profile" in their own language (silenced by the administrator subscription).
Deleting another member's data or reassigning it SHALL send the admin who started it "<brand>.
User data removal completed" / "failed" or "<brand>. User data reassignment is completed" /
"failed".

#### Scenario: Member removes their profile

- **WHEN** a member calls the profile-removal instructions
- **THEN** they receive "Disabling account on <host>" with the portal address linked twice and no word "permanently"

#### Scenario: Profile deletion confirmed

- **WHEN** the member follows the link and is disabled
- **THEN** every DocSpace admin and the owner receive "User has deleted his/her profile"

### Requirement: Type and role change letters

When a member's portal type is changed the system SHALL send them "<brand>: Your user type has
changed" naming the new type; when their role in a room or an AI agent is changed through the
sharing operation it SHALL send "<brand>: Your role in the room has changed" or "<brand>: Your
role in the AI agent has changed" naming the room (HTML-escaped) and the role, with a link to the
access-rights article. These go by e-mail only and ignore the subscription settings.

#### Scenario: Type raised

- **WHEN** an admin changes a User to a Room admin
- **THEN** the member receives "<brand>: Your user type has changed" greeting "Hello!" and naming the type

#### Scenario: Room title with markup

- **WHEN** the room is named `<b>Sales</b>`
- **THEN** the role letter contains the escaped title and not the raw markup

### Requirement: Sharing and mention letters

Sharing with `notify` SHALL send "Documents. Access granted to document: <title>" (or "… to
folder: <title>") with the access level and message, by e-mail, Telegram and push, to recipients
subscribed to room activity. `sendEditorNotify` SHALL send "<brand> notification. Mentioned in
document" with the note cut to 200 characters and a link to the mention, to listed accounts that
can read the file, are subscribed and did not mute the room; it needs comment rights (403) and an
existing file (404).

#### Scenario: Mention of a member who can read the file

- **WHEN** an editor of a room document lists a member's address in `sendEditorNotify`
- **THEN** the system answers 200 and the member receives the mention letter and push

#### Scenario: Address without an account

- **WHEN** one listed address belongs to nobody and the caller may share the file
- **THEN** no letter goes to it and the answer carries the file's access list as a cue to invite

#### Scenario: Caller without comment rights

- **WHEN** a Guest or a member without access calls `sendEditorNotify`
- **THEN** the system answers 403

### Requirement: Form-filling letters

When a form is submitted in a form-filling room by somebody other than the form's creator, the
filler SHALL get "<brand> notification. The form is filled out" with the button to check the
ready forms, the creator SHALL get the same subject with the filler's name, both by e-mail and
Telegram, and both get a push. The form-filling steps (started, your turn, completely filled,
stopped) SHALL send "<brand> notification." to the participants concerned by e-mail and Telegram.

#### Scenario: Form submitted

- **WHEN** a member fills and submits a form created by the room manager
- **THEN** the member and the manager each receive "<brand> notification. The form is filled out"

#### Scenario: Creator fills their own form

- **WHEN** the creator of the form submits it
- **THEN** no letter is sent

### Requirement: Room deleted letter

When a room or an AI agent is deleted the system SHALL send "<brand> notification. Room deleted"
or "… AI agent deleted" with the room title to its owner and every Room manager, except the
member who deleted it; group members are skipped.

#### Scenario: Owner deletes a room with a manager

- **WHEN** the owner deletes a room that has a Room manager
- **THEN** the manager receives the letter and the owner does not

### Requirement: Portal deactivation and deletion letters

Suspend and delete instructions SHALL go to the portal owner only: "Deactivation of the <host>
portal" with the suspend and continue links, "Deletion of the <host> portal" with the confirm
link (plus the auto-renew note while a hosted subscription is active). After the deletion the
owner SHALL get "<brand> has been deleted" with a feedback link; for a hosted portal on a paid
tariff the installation's support address SHALL get the same subject with domain, owner and
customer data.

#### Scenario: Paid hosted portal deleted

- **WHEN** the owner confirms the deletion of a hosted portal on a paid tariff
- **THEN** the owner receives "<brand> has been deleted" and the support address receives its own copy

#### Scenario: Free portal deleted

- **WHEN** the owner confirms the deletion of a free portal
- **THEN** only the owner receives "<brand> has been deleted"

### Requirement: Unused portal warnings and removal

On a hosted installation a daily job SHALL send the owner of a free portal idle for three months
"Your <brand> will be deleted" on the monthly anniversary of its creation, again from six
months, and remove the portal a week after the six-month warning. A portal whose paid tariff
lapsed SHALL be warned three and six months after the due date and removed six months and a
week after it. A forbidden domain is neither warned nor removed; nothing is sent before the
installation started counting.

#### Scenario: Free portal idle for six months

- **WHEN** nobody signed in or did anything on a free portal for six months and a week
- **THEN** the owner got "Your <brand> will be deleted" a week earlier and the portal is removed

#### Scenario: Free portal idle for one month

- **WHEN** the last activity is a month old
- **THEN** neither warning is sent

### Requirement: After-registration letters

On a hosted installation the daily job SHALL send the admins and the owner one letter on the
given day after the portal's creation: 2 "Discover 4 handy apps inside your <brand>", 3
"Configure your <brand>", 4 "Get more from <brand> with useful add-ons", 7 "Work faster in
<brand> with AI agents", 10 "Connect, integrate, and build with <brand> Docs", 14 "Get free
<brand> apps" (Users too; also Enterprise trials with default branding). All SHALL honour the
useful-tips setting.

#### Scenario: Day ten

- **WHEN** the portal is exactly ten days old
- **THEN** only the Developer Tools letter goes out, and nothing a day later

#### Scenario: Unsubscribed admin

- **WHEN** an admin switched useful tips off
- **THEN** none of these letters reaches them

### Requirement: Payment warning letters

The daily job SHALL send the owner and payer of a hosted paid portal "Upcoming subscription
payment for your <brand> tariff plan" three days before the due date, "Grace period for your
<brand> activated" the day after, "… expires tomorrow" the day before the grace period ends and
"… expired" on that day. Enterprise and Developer licences SHALL get "will expire soon" /
"activated" / "no longer available" letters to the admins seven days before and on the day. No
setting silences them.

#### Scenario: Three days before the due date

- **WHEN** a paid hosted portal's tariff runs out in three days
- **THEN** the owner and the payer receive the upcoming payment letter, and four days before nothing

#### Scenario: Trial portal

- **WHEN** a trial runs out
- **THEN** no payment warning is sent

### Requirement: Wallet letters

The system SHALL send the payer and the owner, by e-mail and Telegram, the "Action required:"
letters "Your <brand> Wallet balance is running low" (once per threshold crossing), "Unable to
auto top up the Wallet balance in your <brand>", "Automatic top-up is unavailable for your
<brand> Wallet" and "Unable to auto-renew the purchased services in your <brand>", and by e-mail
"Upcoming subscription payment for "<service>" in your <brand>" three days before an add-on renews.

#### Scenario: Balance under the threshold

- **WHEN** the wallet balance falls under the threshold for the first time
- **THEN** the payer and the owner receive the low-balance letter, and a second check below the threshold sends nothing

### Requirement: Letters owned by other capabilities

"Suspicious login to your <brand> account" (`history-and-audit`), "Your API key is expired"
(`webhooks-and-api-keys`), "Backup for your <brand> failed" / "Auto backup for your <brand>
failed" (`backup-and-restore`) and the invitation letters "Join <brand>", "<brand>: You are
invited to the room", "You're invited to the <brand> room" / "AI agent" (`invitations`) SHALL be
triggered as those specifications say. They go by e-mail only, except the suspicious login which
also goes to Telegram.

#### Scenario: Suspicious login detected

- **WHEN** the sign-in check of `history-and-audit` fires
- **THEN** the member receives "Suspicious login to your <brand> account" by e-mail and, if linked, Telegram

### Requirement: Rendering and branding of a letter

Every letter SHALL be rendered in the recipient's culture (the portal's when the account has
none), SHALL carry the portal's letter logo text where the product name appears and the portal
logo or a custom white-label logo at the top, SHALL turn every link into an absolute portal link,
and SHALL leave no raw placeholder in the text. On a hosted free or trial portal letters to
addresses that are not yet activated SHALL be capped per day by the installation's spam limit.

#### Scenario: Recipient with a culture

- **WHEN** a member whose profile language is German receives a letter
- **THEN** the subject and the body are the German texts, and the links point at the portal address

#### Scenario: White-labelled portal

- **WHEN** the portal's letter logo text is changed
- **THEN** every letter sent afterwards names that text instead of the product name
