# notifications — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`). `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `Actions.cs` is
`web/ASC.Web.Core/Notify/Actions.cs` (studio letters), `NotifyConstants.cs` is
`products/ASC.Files/Core/Services/NotifyService/NotifyConstants.cs` (Files letters), `NotifyClient.cs` is next to
it, `StudioNotifyService.cs` / `StudioPeriodicNotify.cs` / `StudioWhatsNewNotify.cs` / `PortalRetentionJob.cs` /
`PortalRetentionSchedule.cs` are in `web/ASC.Web.Core/Notify/`, `WebstudioNotifyPatternResource.resx` is in
`web/ASC.Web.Core/PublicResources/`,
`FilesPatternResource.resx` next to `NotifyConstants.cs`. Status: **TEST-CONFIRMED** (a test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the release
notes say it). Exception → status mapping: `SecurityException` 403 "Access denied", `ArgumentException` 400,
`BillingException` 402, `MethodAccessException` 405, other exceptions 500 (`CustomExceptionHandler`,
`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`).

The letter suite `common/Tests/ASC.Notify.Tests` renders a letter through the real pipeline and asserts the
subject, wording, buttons and links (`Infrastructure/LetterTestBase.cs`), and `Periodic/PeriodicLetterScheduleTests.cs`
asserts the day each periodic letter claims; neither sends through a trigger, so "TEST-CONFIRMED" on a letter row
means the subject/text and (for periodic letters) the schedule, while the trigger is CODE-ONLY unless an integration
test is named. The retention suite `Periodic/Retention/` (hotfix/v4.0.1, merge `183da52625`) is the exception: it
drives the daily retention job against real portals of the stack — blocks, unblocks over HTTP and removes them — so
its rows confirm the trigger as well; `PortalRetentionScheduleTests.cs` asserts the day-by-day timeline of the pure
schedule and `PortalRetentionJobTests.cs` the job's decisions with a recording notify client.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Delivery channels of the installation** | | |
| Channels = senders named in the notify config ∩ channels the build implements; `email.sender` always enabled, `telegram.sender` enabled while the Telegram consumer has bot name and token | `web/ASC.Web.Core/Notify/NotificationChannelsHelper.cs:45-52`; `web/ASC.Web.Core/Notify/Channels/TelegramNotificationChannel.cs:36-41`; `common/ASC.FederatedLogin/LoginProviders/TelegramLoginProvider.cs:62-65`; `../buildtools/config/notify.json` (senders `email.sender`, `telegram.sender`); yaml `getNotificationChannels` | CODE-ONLY |
| Push is not a listed channel: it is a sink of the in-process notify engine that calls Firebase | `common/ASC.Core.Common/Notify/PushSenderSink.cs`; `common/ASC.Core.Common/Notify/Push/FirebaseHelper.cs:45-67` | CODE-ONLY |
| Any role gets a non-empty list with a named first entry, 200 | `web/ASC.Web.Api.Tests/Tests/02_Settings/Notifications/NotificationChannelsTests.cs:45-58` | TEST-CONFIRMED |
| Anonymous 401 | `NotificationsPermissionsTests.cs:102-115` | TEST-CONFIRMED |
| **Queued delivery of e-mail and Telegram** | | |
| Trigger publishes an integration event; notify service renders and sends | `web/ASC.Web.Core/Notify/StudioNotifyServiceHelper.cs:56-95` (`NotifyItemIntegrationEvent`); `common/services/ASC.Notify/Services/NotifySenderService.cs` | CODE-ONLY |
| Retry every 5 min, 10 attempts, then fatal; messages stored 7 days | `../buildtools/config/notify.json` (`process.maxAttempts` 10, `attemptsInterval` 0:5:0, `storeMessagesDays` 7); `common/services/ASC.Notify/DbWorker.cs:88, 140-146` | CODE-ONLY |
| **Notification kinds a member can switch** | | |
| Types 0 badges, 1 rooms activity, 2 daily feed, 3 useful tips; badges stored as a per-user setting, the others as subscriptions | `web/ASC.Web.Core/Notify/NotificationControllerHelper.cs:49-104`; `web/ASC.Web.Core/Notify/BadgesSettings.cs`; yaml `getNotificationSettings`, `setNotificationSettings` | CODE-ONLY |
| Every role sets any type → 200 echo | `NotificationSettingsSetTests.cs:45-58` (5 roles × 4 types) | TEST-CONFIRMED |
| Set then get → 200, `isEnabled` true | `NotificationSettingsGetTests.cs:45-58` | TEST-CONFIRMED |
| Off then read → false; on then read → true (regression of the cached-store bug) | `NotificationSettingsGetTests.cs:66-93` | TEST-CONFIRMED |
| Default on for every kind: badges default `EnableBadges = true`; `send_whats_new`, `rooms_activity`, `periodic_notify` are subscribed for the Everyone group at tenant -1 and the lookup walks up to the groups | `BadgesSettings.cs:44-47`; `common/ASC.Core.Common/EF/Model/Core/Subscription.cs:67-68, 87`; `common/ASC.Core.Common/Notify/TopSubscriptionProvider.cs` (`GetSubscriptionRecordAsync` walks parents); `common/Tests/ASC.Notify.Tests/Periodic/PeriodicLetterDeliveryTests.cs:181-199` ("a portal owner is subscribed to the periodic notifications to begin with") | TEST-CONFIRMED (tips), CODE-ONLY (others) |
| Anonymous 401 on get and set | `NotificationsPermissionsTests.cs:46-59, 88-100` | TEST-CONFIRMED |
| **Muting single rooms** | | |
| Opaque id, no existence/access check, add on `mute: true`, remove on false | `web/ASC.Web.Core/Notify/RoomsNotificationSettings.cs:68-96`; yaml `setRoomsNotificationStatus` | CODE-ONLY |
| Any role mutes a random id → 200, id in `disabledRooms` | `RoomsNotificationStatusTests.cs:50-63` | TEST-CONFIRMED |
| Read back → 200, still listed | `RoomsNotificationSettingsGetTests.cs:45-61` | TEST-CONFIRMED |
| Anonymous 401 on both | `NotificationsPermissionsTests.cs:61-86` | TEST-CONFIRMED |
| **What each kind silences** | | |
| Rooms-activity subscription gates sharing letters, mentions, room pushes (invite, role, archive, new document/upload/folder) and the hourly digest | `NotifyClient.cs:118-121, 220-223, 334-337, 489-512 (GetNotifiableUsersAsync → CanNotifyRoom)`; `StudioWhatsNewNotify.cs` (`CheckSubscriptionAsync`) | CODE-ONLY |
| Muted room skips mention, room pushes and the room-deleted letter (see N1) | `NotifyClient.cs:225-228, 339-342, 556-566` | CODE-ONLY |
| Daily feed subscription gates the daily digest | `StudioWhatsNewNotify.cs` (`CheckSubscriptionAsync`, `SendWhatsNewNotifyAction`) | CODE-ONLY |
| Useful tips gate only the seven marketing periodic letters; billing and retention letters ignore it (the retention letters even drop the footer link to the settings) | `common/Tests/ASC.Notify.Tests/Periodic/PeriodicLetterScheduleTests.cs:430-467` (`OnlyTheMarketingLettersHonourTheUnsubscribe`); `Actions.cs:2489, 2627, 2668, 2708, 2751, 2792, 2832, 2878`; `Actions.cs:1725, 2491-2541` (`OffersUnsubscribe`); `Periodic/Retention/PortalRetentionLetterTestBase.cs:84-87` | TEST-CONFIRMED |
| Badges off hides `new` counters | yaml `setNotificationSettings` description; `BadgesSettingsHelper` read by the Files DTO helpers | CODE-ONLY |
| **Telegram link of a profile** | | |
| Link reused while a token is outstanding, else a fresh one; empty when no bot name; status 0/1/2 with username only when linked; unlink deletes the row and answers true | `web/ASC.Web.Api/Api/Settings/TelegramController.cs:36-62`; `common/ASC.Core.Common/Notify/Telegram/TelegramHelper.cs` (`RegisterUserAsync`, `GetTelegramUserStatus`, `CurrentRegistrationLink`, `DisconnectAsync`, `GetLink`); yaml `linkTelegram`, `checkTelegram`, `unlinkTelegram` | CODE-ONLY |
| Token lifetime 20 min by default | `../buildtools/config/autofac.consumers.json` (`telegramAuthTokenLifespan` 20); yaml `linkTelegram` | CODE-ONLY |
| Telegram keys offered on every tariff (v3.6 "Telegram on Startup") | `autofac.consumers.json` telegram consumer `"paid": false`; release notes v3.6 lines 20, 50 | CODE-ONLY |
| **Push device registration** | | |
| Register stores token + `isSubscribed` for app `doc`; existing token returned as stored; subscribe updates only `isSubscribed`, empty answer for a foreign token | `web/ASC.Web.Api/Api/Settings/PushController.cs:38-50`; `FirebaseHelper.cs` (`RegisterUserDeviceAsync`, `UpdateUserAsync`); yaml `docRegisterPusnNotificationDevice`, `subscribeDocumentsPushNotification` | CODE-ONLY |
| Only subscribed devices; needs Firebase credentials (`firebase-mobile` section); invalid/unregistered token deleted | `FirebaseHelper.cs:45-67, 129-160` (`GetSubscribedUserDeviceTokensAsync`, `HandleInvalidTokenAsync`); `../buildtools/config/appsettings.json:307-314` | CODE-ONLY |
| **Push notification events** | | |
| Push-only actions: `DocumentCreatedInRoom`, `DocumentUploadedTo`, `DocumentsUploadedTo`, `FolderCreatedInRoom`, `InvitedToRoom`/`InvitedToAgent`, `RoomUpdateAccessForUser`/`AgentUpdateAccessForUser`, `RoomMovedArchive`; push also on `ShareDocument`, `ShareFolder`, `EditorMentions`, `FormSubmitted` | `NotifyConstants.cs:104-106, 149-151, 162-164, 222-224, 292, 315, 337, 348, 371, 382, 406, 430, 453`; release notes v3.1 line 69 | CODE-ONLY |
| Uploads debounced per room every 30 s: ≤3 sent one by one, more collapsed into one push with the count | `products/ASC.Files/Core/Services/NotifyService/NotifyEventQueue.cs:56-80`; `PushNotificationService.cs:44-62` | CODE-ONLY |
| Actor excluded, recipients filtered by subscription and mute | `NotifyClient.cs:489-512` | CODE-ONLY |
| Triggers: `FileStorageService.CreateNewFileAsync`/`CreateFolderAsync` (create), upload queue, sharing (`FileSharing.SetAceObjectAsync`), archive operation, `FileSharing` role change | codegraph call graph of `NotifyClient` (callers in `FileStorageService.cs:1852, 635`, `FileSharing.cs`, `NotifyEventQueue.cs`) | CODE-ONLY |
| **New items of a room** | | |
| Created by other appears, nested file appears, several files appear | `products/ASC.Files/Tests/Tests/03_Rooms/NewItems/RoomNewItemsSemanticsTests.cs:45-57, 207-233` | TEST-CONFIRMED |
| Own file and a subfolder do not appear | `RoomNewItemsSemanticsTests.cs:59-73, 192-205` | TEST-CONFIRMED |
| Pre-visit files not reported; re-visit clears (bug 81712) | `RoomNewItemsSemanticsTests.cs:79-123` | TEST-CONFIRMED |
| Deleted file disappears | `RoomNewItemsSemanticsTests.cs:283-313` | TEST-CONFIRMED |
| Mixed old/new, own/others | `RoomNewItemsSemanticsTests.cs:235-281` | TEST-CONFIRMED |
| Entry fields title, createdBy, updated | `RoomNewItemsContractTests.cs:71-104` | TEST-CONFIRMED |
| Non-member User/Guest/RoomAdmin 403; any access level 200; Guest 200; non-member DocSpace admin 200; anonymous 401 | `RoomNewItemsPermissionsTests.cs:44-162` | TEST-CONFIRMED |
| Unknown id / 0 / deleted room 404; archived room 200 for the owner; empty room (also private) empty list | `RoomNewItemsContractTests.cs:44-69, 106-152` | TEST-CONFIRMED |
| `getNewFolderItems` on the room id agrees | `RoomNewItemsCrossCheckTests.cs:44-77` | TEST-CONFIRMED |
| **New items across all rooms** | | |
| Two rooms aggregated with room titles; room without access and its file left out | `RoomsNewItemsTests.cs:195-262` | TEST-CONFIRMED |
| Same semantics as per-room (other's file, own file, subfolder, nested, deleted, mixed); shape; empty | `RoomsNewItemsTests.cs:45-191` | TEST-CONFIRMED |
| Agreement with the per-room endpoint; anonymous 401 | `RoomsNewItemsTests.cs:270-306` | TEST-CONFIRMED |
| **Daily feed digest** | | |
| Hourly cron; tenants with audit events in the previous day; active tenants; local hour 7 (`web:whatsnew-time`); skipped when `TariffState >= NotPaid`; per active user subscribed; actions list; nothing sent when no activity; subject "What's new on your portal … for $Date" | `web/ASC.Web.Core/Notify/StudioNotifyServiceSender.cs:26, 39-42`; `StudioWhatsNewNotify.cs` (`DailyActions`, `GetChangedTenantsAsync`, `SendMsgWhatsNewAsync`, `TimeToSendWhatsNew`); `WebstudioNotifyPatternResource.resx` `subject_send_whats_new` | CODE-ONLY |
| E-mail + Telegram: subscription method seed `email.sender|telegram.sender` for `send_whats_new` | `common/ASC.Core.Common/EF/Model/Core/DbSubscriptionMethod.cs:66`; `Actions.cs:2941-2942` | CODE-ONLY |
| **Hourly room activity digest** | | |
| Hourly cron; previous hour; actions list; subject "… for the last hour"; no sender-method row so the engine's default senders (e-mail + Telegram) apply | `StudioNotifyServiceSender.cs:27, 44-47`; `StudioWhatsNewNotify.cs` (`RoomsActivityActions`); `Actions.cs:2904-2911`; `common/ASC.Core.Common/Context/WorkContext.cs:54` (`DefaultClientSenders`) | CODE-ONLY |
| **SMTP settings of the portal** | | |
| Portal-settings right then hidden-section check (`BillingException` 402) | `web/ASC.Web.Api/Api/Settings/SmtpSettingsController.cs` (`CheckSmtpPermissionsAsync`); yaml 402/403 rows | CODE-ONLY |
| Owner and DocSpace admin save and read back host, port, sender address, display name, enableAuth, enableSSL (200) | `web/ASC.Web.Api.Tests/Tests/05_Security/Smtp/SmtpSaveSettingsTests.cs:45-82`; `SmtpGetSettingsTests.cs:45-86` | TEST-CONFIRMED |
| 256-char host / senderAddress / senderDisplayName / credentialsUserName → 400 "The field X must be a string with a maximum length of 255." | `SmtpSaveSettingsTests.cs:118-148` | TEST-CONFIRMED |
| Reset → 200 `isDefaultSettings: true` (owner, DocSpace admin) | `SmtpResetSettingsTests.cs:45-78` | TEST-CONFIRMED |
| Password never returned; port default 25; credentials only with enableAuth; hosted default = empty settings | `SmtpSettingsController.cs` (`GetSmtpSettings`, `ToSmtpSettingsConfig`, `ResetSmtpSettings`) | CODE-ONLY |
| RoomAdmin/User/Guest 403 "Access denied", anonymous 401 on get/save/reset | `SmtpGetSettingsTests.cs:88-121`; `SmtpSaveSettingsTests.cs:84-116`; `SmtpResetSettingsTests.cs:80-114` | TEST-CONFIRMED |
| **SMTP test message** | | |
| Owner / DocSpace admin → 200, non-empty `id`, empty `error` | `SmtpTestSendTests.cs:49-82` | TEST-CONFIRMED |
| RoomAdmin/User/Guest 403, anonymous 401 on test and status | `SmtpTestSendTests.cs:84-117`; `SmtpOperationStatusTests.cs:52-84` | TEST-CONFIRMED |
| One job per portal, same job returned while running, completed job discarded on first read, 30 s connect timeout, rate limit 5/15 min, letter "SMTP test message" | `SmtpSettingsController.cs` (`TestSmtpSettings` with `SensitiveApi` limiter); yaml `testSmtpSettings`, `getSmtpOperationStatus`; `WebstudioNotifyPatternResource.resx` `subject_smtp_test` | CODE-ONLY |
| Completed-without-error outcome is not asserted (no SMTP server in the suite) | `SmtpOperationStatusTests.cs` summary | — |
| **Contact form for administrators** | | |
| Owner / DocSpace admin toggle → 200, status 0, "Settings have been successfully updated", count 1 | `web/ASC.Web.Api.Tests/Tests/02_Settings/Messages/EnableAdminMessageSettingsTests.cs:45-102` | TEST-CONFIRMED |
| RoomAdmin/User/Guest 403 "Access denied", anonymous 401 | `EnableAdminMessageSettingsPermissionsTests.cs:46-79` | TEST-CONFIRMED |
| `sendAdminMail`: anonymous allowed, CAPTCHA, 400 on bad address/empty message, letter "User message to administrators" to the admin subscription (seeded for the admins group, `email.sender|telegram.sender`) | yaml `sendAdminMail`; `StudioNotifyService.cs:61-67` (`SendNoticeAsync` → subscription recipients); `web/ASC.Web.Core/Notify/StudioNotifySource.cs` (`for_admin_notify` mapped to `admin_notify`); `DbSubscriptionMethod.cs:90`; `WebstudioNotifyPatternResource.resx` `subject_for_admin_notify` | CODE-ONLY |
| **Join invitation from the sign-in page** | | |
| No trusted domains → 405 "Method not available" for every role and anonymous (bug 80727) | `SendJoinInviteMailTests.cs:52-86` | TEST-CONFIRMED |
| Letter "Invitation to join <host> portal", 400 for an existing member / outside the domains | yaml `sendJoinInviteMail`; `Actions.cs:724-757` (`join`); `StudioNotifyService.cs:161-170`; the positive cases are commented out in `SendJoinInviteMailTests.cs:97-187` (tenant save breaks on the integration host) | CODE-ONLY |
| **Activation and welcome letters** | | |
| Activation "You are invited to <brand>" (user / guest), registration "Complete <brand> registration", admin activation and welcome "Welcome to <brand>!", owner "Discover business subscription of <brand>" (SaaS only) | `common/Tests/ASC.Notify.Tests/Onboarding/*.cs` (subjects at `SaasUserActivationLetterTests.cs:64`, `SaasGuestActivationLetterTests.cs:56`, `SaasDocSpaceRegistrationLetterTests.cs:66`, `SaasAdminActivationLetterTests.cs:70`, `EnterpriseAdminActivationLetterTests.cs:61`, `SaasUserWelcomeLetterTests.cs:63`, `SaasGuestWelcomeLetterTests.cs:62`, `SaasAdminWelcomeLetterTests.cs:60`) | TEST-CONFIRMED |
| Edition selection and triggers (`UserInfoActivationAsync`, `GuestInfoActivationAsync`, `UserInfoAddedAfterInviteAsync`, `GuestInfoAddedAfterInviteAsync`, `SendCongratulationsAsync`, `SendAdminWelcomeAsync`) | `StudioNotifyService.cs:171-296, 343-360, 429-486` | CODE-ONLY |
| **Password and e-mail letters** | | |
| "Confirm changing your password" / "Set up a password for <brand>" chosen by `initialPasswordAssignment`; "Your password was changed successfully" with browser/platform escaped | `StudioNotifyService.cs:81-114`; `common/Tests/ASC.Notify.Tests/Account/PasswordSetLetterTests.cs:58-65`; `PasswordChangedLetterTests.cs:78-103`; trigger of the changed letter `products/ASC.People/Server/Api/UserController.cs:648`; password-reminder tests `products/ASC.People/Tests/Tests/05_Password/*` | TEST-CONFIRMED (texts), CODE-ONLY (triggers) |
| "Confirm changing your email", "<brand>. Please activate your email address", phone and TFA letters | `StudioNotifyService.cs:129-155, 144-155 (SendMsgMobilePhoneChangeAsync, SendMsgTfaResetAsync)`; `WebstudioNotifyPatternResource.resx` `subject_change_email_v115`, `subject_activate_email`, `subject_change_phone`, `subject_change_tfa` | CODE-ONLY |
| **Account removal and self-lock letters** | | |
| "Disabling account on <host>" with the remove button, portal address linked twice, no "permanently" | `common/Tests/ASC.Notify.Tests/Account/ProfileDeleteLetterTests.cs:45-70`; trigger `products/ASC.People/Server/Api/RemoveUserDataController.cs` (`SendInstructionsToDelete`) | TEST-CONFIRMED (text), CODE-ONLY (trigger) |
| "User has deleted his/her profile" to every member of the admins group in their culture, mapped to the admin subscription | `StudioNotifyService.cs:421-434`; `StudioNotifySource.cs` (`profile_has_deleted_itself` → `admin_notify`); trigger `UserController.cs:881`; release notes v2.6 line 33 | CODE-ONLY |
| Reassign / remove-data completed and failed letters to the admin who started the job | `StudioNotifyService.cs:314-342`; `Actions.cs:217-346` | CODE-ONLY |
| **Type and role change letters** | | |
| Subjects, "Hello!", escaped room title, access-rights link, no hard-coded product name | `common/Tests/ASC.Notify.Tests/Account/UserTypeChangedLetterTests.cs:52-76`; `UserRoleChangedLetterTests.cs:61-87`; `UserAgentRoleChangedLetterTests.cs:61-87`; release notes v3.1.1 lines 30-33 | TEST-CONFIRMED |
| Triggers: `UserController.UpdateUserType`, `UpdateUserTypeProgressItem`, `FileSharing.SetAceObjectAsync`; e-mail only; errors swallowed | `StudioNotifyService.cs:497-528`; codegraph callers (`products/ASC.People/Server/Api/UserController.cs`, `common/ASC.Data.Reassigns/UpdateUserTypeProgressItem.cs`, `products/ASC.Files/Core/Core/FileStorageService.cs`) | CODE-ONLY |
| **Sharing and mention letters** | | |
| Share letters by e-mail, Telegram and push; recipient must be subscribed to rooms activity; access string and message; encrypted variant | `NotifyClient.cs:93-170`; `NotifyConstants.cs:98-154`; `FilesPatternResource.resx` `subject_ShareDocument`, `subject_ShareFolder`; trigger `products/ASC.Files/Core/Utils/FileSharing.cs` (`SetAceObjectAsync`, cross-reference `invitations` "with notify") | CODE-ONLY |
| Mention: comment right (403), file exists (404), recipients must read the file, subscription and mute checks, 200-char cut, anchor link, access list returned when an address is unknown and the caller can share | `products/ASC.Files/Core/Core/FileStorageService.cs:5136-5200`; `NotifyClient.cs:172-239`; yaml `sendEditorNotify` | CODE-ONLY |
| **Form-filling letters** | | |
| Submitted: filler and creator letters (e-mail + Telegram) plus one push to both; skipped when the filler is the creator; guest account named by the audit "guest" label | `NotifyClient.cs:241-308`; trigger `products/ASC.Files/Core/Utils/EntryManager.cs` | CODE-ONLY |
| Started / your turn / completely filled / stopped letters (e-mail + Telegram) | `NotifyClient.cs:513-566`; `NotifyConstants.cs:470-526`; triggers `FileStorageService.cs:5610 (SaveFormRoleMapping), 5722 (ManageFormFilling)` | CODE-ONLY |
| **Room deleted letter** | | |
| To owner and Room managers, actor and groups skipped; AI agent variant; e-mail + Telegram | `NotifyClient.cs:310-363`; trigger `products/ASC.Files/Core/Services/WCFService/FileOperations/FileDeleteOperation.cs`; `FilesPatternResource.resx` `subject_RoomRemoved`, `subject_AgentRemoved` | CODE-ONLY (see N1) |
| **Portal deactivation and deletion letters** | | |
| Suspend / delete instructions to the owner; auto-renew note on a hosted paid tariff; owner-only demand | `web/ASC.Web.Api/Api/PortalController.cs:1025-1041, 1065-1085, 1328-1340`; `StudioNotifyService.cs:532-552` | CODE-ONLY |
| Deletion success letter with the feedback button | `common/Tests/ASC.Notify.Tests/Account/PortalDeleteSuccessLetterTests.cs:55-73`; `PortalController.cs:1238-1240` (now inside the `PortalRemovalService.RemoveAsync` callback, `web/ASC.Web.Core/RemovePortal/PortalRemovalService.cs`) | TEST-CONFIRMED (text), CODE-ONLY (trigger) |
| Support copy when hosted, not free and `TariffState >= Paid`; skipped when the installation has no support address | `PortalController.cs:1246-1251`; `StudioNotifyService.cs:581-593`; release notes v3.6.1 line 15 | CODE-ONLY |
| **Which portals the retention policy counts** | | |
| Counted: free, or lapsed (`!Quota.Free && State == NotPaid && DueDateIsNotMax`) with no add-on paid from the wallet; paid, grace period (`Delay`), trial and add-on-paying portals left alone; forbidden domain never touched | `PortalRetentionJob.cs:178-215, 323, 415, 472-482`; `common/Tests/ASC.Notify.Tests/Periodic/Retention/PortalRetentionJobTests.cs:430-445, 800-818` (add-on, paid, grace) | TEST-CONFIRMED |
| Anchor: last activity (audit event or successful login, kept with the portal so a purged login history does not move it) for a free portal, due date for a lapsed one; never earlier than the last status change (unblocking), the add-on's end or the portal's day of the first wave (`FirstWaveDays` 30, id modulo) | `PortalRetentionJob.cs:252-309, 579-603`; `PortalRetentionSchedule.cs:266-272`; `PortalRetentionJobTests.cs:158-189, 447-495, 543-612, 764-798`; `PortalRetentionScheduleTests.cs:327-358`; `PortalActivityQueryTests.cs:46-138` | TEST-CONFIRMED |
| Policy start stamped on the first run (installation-wide settings), last-run day kept only when every portal got through | `PortalRetentionJob.cs:92-155`; `web/ASC.Web.Core/Notify/PortalRetentionPolicyStartSettings.cs`; `PortalRetentionJobTests.cs:191-355` | TEST-CONFIRMED |
| Thresholds are defaults of `core:retention` (free 30/60, retention 30, final notice 7; former paying 30/60/90, retention 90, notices 30 and 7; with balance 30, monthly 6-11, 365, retention 90, notices 30 and 7); `../buildtools/config` sets no `core:retention`, so the defaults apply | `PortalRetentionSchedule.cs:113-215`; `PortalRetentionScheduleTests.cs:360-384` (options), `135-227` (timelines) | TEST-CONFIRMED (schedule), CODE-ONLY (config) |
| **Retention warnings before the block** | | |
| Free: day 30 warning, day 60 block; lapsed: 30, 60, block 90; with balance: 30, monthly months 6-11, block 365 | `PortalRetentionScheduleTests.cs:135-143, 155-164, 199-214`; `PortalRetentionJobTests.cs:143-156, 466-495` | TEST-CONFIRMED |
| Grace period: lapsed portal is the policy's from the first unpaid day (`GetPaymentDelay() + 1`); a warning due earlier is sent that day; two due in one run → only the later | `PortalRetentionJob.cs:257-259`; `PortalRetentionSchedule.cs:418-435`; `PortalRetentionScheduleTests.cs:166-186` | TEST-CONFIRMED |
| Subjects "Your <brand> space misses you (action needed by <date>)" (free and lapsed), "You still have funds in your <brand> space (action needed by <date>)"; buttons sign in / renew / keep space and funds; block and deletion days in the body | `Periodic/Retention/SaasOwnerRetentionInactivityWarningLetterTests.cs:44-65`, `SaasOwnerRetentionUnpaidWarningLetterTests.cs:44-65`, `SaasOwnerRetentionWalletWarningLetterTests.cs:47-67`, `SaasOwnerRetentionWalletWarningUnpaidLetterTests.cs:50-74`; `Actions.cs:1808-1905`; `WebstudioNotifyPatternResource.resx:2158-2166` | TEST-CONFIRMED |
| Recipients: owner, plus the payer for a lapsed portal; e-mail only; no unsubscribe footer | `Actions.cs:1716-1725`; `PeriodicLetterDeliveryTests.cs:85-100`; `PortalRetentionLetterTestBase.cs:84-87`; `common/ASC.Core.Common/Notify/Stylers/TextileStyler.cs:288-296` | TEST-CONFIRMED |
| Days written in the portal's time zone: the local day on which the UTC day begins (west of UTC the day before) | `Actions.cs:1755-1776`; `SaasOwnerRetentionInactivityWarningWestLetterTests.cs:48-57` | TEST-CONFIRMED |
| Missed days: a run covers the days since the last one that got through, at most 7 back; only the latest warning goes; a warning already sent is not repeated; a run that failed on a portal or could not read its balance leaves its days to the next | `PortalRetentionJob.cs:64, 107-113, 316-338`; `PortalRetentionSchedule.cs:350-354, 418-435`; `PortalRetentionScheduleTests.cs:260-300`; `PortalRetentionJobTests.cs:191-428, 692-762` | TEST-CONFIRMED |
| Block never before the day the last warning named, never unannounced: without a warning the portal is warned and blocked as far after it as the schedule keeps its last warning from its block (30 days for every default schedule) | `PortalRetentionSchedule.cs:366-388`; `PortalRetentionBlockWarningTests.cs:60-139`; `PortalRetentionJobTests.cs:497-541` | TEST-CONFIRMED |
| **Blocking an unused portal** | | |
| Block: tenant status `Blocked`, category kept with the portal, audit `PortalBlocked`, owner letter "Your <brand> space is blocked, but your data is safe until <date>"; support letter "The portal <host> has been blocked by the retention policy" for every category but `Free`; day of the letters kept, so a failure after the block sends them on the next run, once | `PortalRetentionJob.cs:218-249, 518-570`; `StudioNotifyService.cs:562-579`; `Actions.cs:1962-1990, 2108-2141`; `PortalRetentionLifecycleTests.cs:146-241`; `PortalRetentionBlockedToSupportLetterTests.cs:52-69`; `WebstudioNotifyPatternResource.resx:2143-2148` | TEST-CONFIRMED |
| Unblocking button only for the owner of a non-free portal (the link signs its holder in as the owner); free owner and payer get the support button, or no button when support links are off | `Actions.cs:1907-1975`; `SaasOwnerRetentionBlockedLetterTests.cs:47-69`, `SaasOwnerRetentionBlockedFreeLetterTests.cs:47-63`, `SaasOwnerRetentionBlockedPayerLetterTests.cs:50-69` | TEST-CONFIRMED |
| A blocked portal gets none of the ordinary periodic letters; the job walks active and blocked portals | `StudioPeriodicNotify.cs:98-128`; `PortalRetentionJob.cs:195-201`; `PortalRetentionJobTests.cs:173-189`; `PortalRetentionTenantQueryTests.cs:43-58` | TEST-CONFIRMED |
| `PUT api/2.0/portal/unblock` with the link: active again, audit `PortalUnblocked`, retention record cleared, CSP refreshed; 401 for an invalid link, a link of an earlier block or a portal not blocked; 403 for an ordinary token | `PortalController.cs:1114-1162`; `PortalRetentionLifecycleTests.cs:193-314` | TEST-CONFIRMED |
| **Deletion reminders and removal of a blocked portal** | | |
| Free: final reminder day 23, deletion day 30; others: early reminder day 60, final day 83, deletion day 90; a late final reminder moves the deletion a week after itself; early reminder once; the block's category decides, whatever the wallet does later | `PortalRetentionSchedule.cs:302-348, 458-466`; `PortalRetentionScheduleTests.cs:145-153, 188-227, 243-325`; `PortalRetentionLifecycleTests.cs:316-376, 419-452`; `PortalRetentionJobTests.cs:380-428` | TEST-CONFIRMED |
| Subjects "Your <brand> space will be deleted soon (deleted on <date>)", "Last chance to keep your <brand> space (deleted on <date>)" (shared body, `FinalNotice` flag), "Your <brand> space has been deleted" naming the old address | `SaasOwnerRetentionDeletionReminderLetterTests.cs:47-64`, `SaasOwnerRetentionFinalReminderLetterTests.cs:47-66`, `SaasOwnerRetentionDeletedLetterTests.cs:47-67`, `SaasOwnerRetentionDeletedUnpaidLetterTests.cs:54-64`; `Actions.cs:1992-2107`; `WebstudioNotifyPatternResource.resx:2149-2157` | TEST-CONFIRMED |
| Removal only of the block the decision was made for (status and status day re-read); letters to the owner (+ payer) and the "<brand> has been deleted" support copy for a lapsed portal go from inside the removal, and a failing letter does not stop the purge | `PortalRetentionJob.cs:609-651`; `PortalRetentionLifecycleTests.cs:316-417`; `PortalRemovalTests.cs:42-90`; `PortalPurgeTests.cs:44-91` | TEST-CONFIRMED |
| **After-registration letters** | | |
| Days 2, 3, 4, 7, 10, 14; exactly one per day; subjects; admins + owner (day 14 also users); Enterprise trial day 14 with default branding | `PeriodicLetterScheduleTests.cs:107-178, 239-250, 399-428`; `Periodic/Tips/*.cs` subjects; `Actions.cs:2615-2884`; `StudioPeriodicNotify.cs:66-77`; release notes v2.6 line 31 | TEST-CONFIRMED |
| Honour the tips subscription | `PeriodicLetterScheduleTests.cs:430-467` | TEST-CONFIRMED |
| Cron 5:00 daily (`core:notify:cron`), disabled by `core:notify:tariff=false`, SaaS or Enterprise sender chosen per installation | `StudioNotifyServiceSender.cs:14-24` | CODE-ONLY |
| **Payment warning letters** | | |
| SaaS: -3 days, +1 day (grace), grace last day, grace expired; owner + payer; free and trial ignored | `PeriodicLetterScheduleTests.cs:179-238, 469-487`; `Periodic/PaymentWarnings/Saas/*.cs` subjects; `Actions.cs:2974-3109`; `PeriodicLetterDeliveryTests.cs:85-139` (owner / payer recipients) | TEST-CONFIRMED |
| Enterprise / Developer / Lifetime: -7 days and on the day, grace -7 and on the day; exactly one flavour claims a portal; admins | `PeriodicLetterScheduleTests.cs:251-397`; `Periodic/PaymentWarnings/Enterprise/*.cs`, `Developer/*.cs`; `Actions.cs:3162-3532` | TEST-CONFIRMED |
| Not silenced by the tips subscription | `PeriodicLetterScheduleTests.cs:430-467`; `PeriodicLetterDeliveryTests.cs:181-199` | TEST-CONFIRMED |
| **Wallet letters** | | |
| Low balance: once per threshold crossing (`LowBalanceNotified`), payer + owner, e-mail + Telegram | `web/ASC.Web.Studio/Wallet/TopUpWalletService.cs:240-270`; `StudioNotifyService.cs:909-924`; `common/Tests/ASC.Notify.Tests/Billing/LowWalletBalanceLetterTests.cs:45-69`; release notes v4.0 line 77 | TEST-CONFIRMED (text), CODE-ONLY (trigger) |
| Top-up error, auto top-up unavailable, renew error (e-mail + Telegram, payer + owner); upcoming add-on payment 3 days before (e-mail, owner + payer) | `StudioNotifyService.cs:658-709`; `StudioPeriodicNotify.cs` (`SendUpcomingSubscriptionPaymentAsync`); `Billing/TopUpWalletErrorLetterTests.cs:60`, `RenewSubscriptionErrorLetterTests.cs:67`, `UpcomingSubscriptionPaymentLetterTests.cs:55-67`, `WalletAutoTopUpUnavailableLetterTests.cs:61` | TEST-CONFIRMED (texts), CODE-ONLY (triggers) |
| **Letters owned by other capabilities** | | |
| Suspicious login by e-mail + Telegram | `StudioNotifyService.cs:116-123`; `openspec/specs/history-and-audit/spec.md` | CODE-ONLY |
| API key expired (e-mail), key name escaped | `common/Tests/ASC.Notify.Tests/Account/ApiKeyExpiredLetterTests.cs:58-71`; `openspec/specs/webhooks-and-api-keys/spec.md` | TEST-CONFIRMED |
| Backup failed / scheduled backup failed, e-mail and Telegram texts agree | `Backup/BackupFailedLetterTests.cs:55-91`; `Backup/ScheduledBackupFailedLetterTests.cs:57-95`; `openspec/specs/backup-and-restore/spec.md` | TEST-CONFIRMED |
| Invitation letters (room, agent, DocSpace, existing user) | `Invites/*.cs` subjects; `openspec/specs/invitations/spec.md` | TEST-CONFIRMED |
| **Rendering and branding of a letter** | | |
| Recipient culture (tenant fallback), `${LetterLogoText}` resolved from the white-label settings, letter logo attached or `$TopGif`, links absolutized, no raw tags, no hard-coded product name or URL | `common/Tests/ASC.Notify.Tests/Infrastructure/LetterTestBase.cs` (`Letter_Renders` checks per culture); `web/ASC.Web.Core/Notify/NotifyConfiguration.cs:60-140`; `.claude/skills/notify-emails/SKILL.md` §4-5, §9 | TEST-CONFIRMED |
| Spam cap for unactivated addresses on hosted free/trial portals (`core:notify:countspam`, per day) | `web/ASC.Web.Core/Notify/StudioNotifyHelper.cs:96-150`; `web/ASC.Web.Core/Notify/SpamEmailSettings.cs` | CODE-ONLY |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| yaml `getNotificationSettings`: "kinds 1, 2 and 3 come back switched off until they are switched on" | Off is not the default: `send_whats_new`, `rooms_activity` and `periodic_notify` are subscribed for the Everyone group at tenant -1 (`Subscription.cs:67-68, 87`) and the lookup walks up to group records, so a fresh account reads `isEnabled: true` for all four kinds; `PeriodicLetterDeliveryTests.cs:191` asserts it for tips. The spec states default on. |
| yaml `getNotificationSettings`: 400 for a `type` outside 0-3 | A value that is not a member name fails model binding with 400; an integer the enum does not define (for example 7) binds and reaches `throw new Exception("Incorrect parameters")`, answered 500 (`NotificationControllerHelper.cs:69`). See N3. |
| v3.5: Telegram carries "daily feed, room activity, mention" | Also the sharing letters, room/agent deleted, form filling, suspicious login, the wallet letters and the administrator message; the backup-failed letters carry a Telegram text that no sender list uses (`common/ASC.Data.Backup.Core/Core/NotifyHelper.cs` sends e-mail only). |
| v3.1: push list names "folder uploaded" | No push action for a folder upload exists; `FolderCreatedInRoom` is sent on folder creation (`NotifyConstants.cs:447-453`). |
| v2.6: "Explore useful integrations … (day 10, for managers)" | Day 10 is now "Connect, integrate, and build with <brand> Docs" to admins and the owner (`Actions.cs:2739-2756`). |
| v3.1 line 17: a free portal idle for six months is warned and deleted ("Your <brand> will be deleted", three- and six-month warnings on the creation anniversary); a lapsed paid portal deleted six months and a week after its due date | Replaced by the retention policy of hotfix/v4.0.1 (`183da52625`): a free portal is warned after 30 idle days and blocked after 60, a lapsed one warned after 30 and 60 days from the due date and blocked after 90, one with money on its wallet blocked after a year; deletion 30 or 90 days after the block with reminders. The old letters, their resource keys and `Periodic/InactivityWarnings/` are gone. The spec follows the server. |
| yaml: no `unblock` operation under `portal` | `PUT api/2.0/portal/unblock` exists (`PortalController.cs:1114-1162`, confirm-link authentication, 200/401/403) and is exercised by `PortalRetentionLifecycleTests.cs:193-314`; the OpenAPI document was not regenerated in the merge. The spec names the route, not an `operationId`. |
| Test name `GetNotificationSettings_AnyRole_ReturnsThePreviouslySetType` | Assertion matches the name. |
| `SmtpOperationStatusTests` summary says the completed-without-error case is covered "by this summary" | It is not asserted anywhere; left CODE-ONLY above. |

## Left out of the spec, and why

- **Socket / in-app real-time events** (`SocketManager`, `QuotaSocketManager.WalletLowBalanceAsync`): out of scope by the
  task statement.
- **Letter bodies and tags** beyond the subject and the facts the tests assert: `notify-emails` skill territory.
- **Zoom welcome, portal rename, region migration, storage encryption / decryption, DocuSign, mail merge, LDAP
  activation, custom-mode registration data to sales, the sales request letters**: letters of integrations or of
  standalone operations without an API operation in this document (`Actions.cs:762-847, 1137-1296, 2817`;
  `NotifyConstants.cs:37-96`).
- **Reassign / remove-user-data mechanics** beyond the two letters: `user-type-permissions` territory.
- **What a blocked portal answers** (404 to the API, `tenantStatus` in `GET api/2.0/settings`, `[AllowBlocked]` actions),
  the accounting call behind "money on the wallet" (`ITariffService.HasPositiveBalanceAsync`, 5 s timeout, 20 failures
  in a row stop asking for the run) and the removal mechanics (`PortalRemovalService`): `portal-lifecycle` territory
  (`openspec/specs/portal-lifecycle/spec.md`); this spec keeps only what the letters say and when.
- **The Telegram bot service itself** (`common/services/ASC.TelegramService`): command handling and message delivery
  plumbing.
- **Which actions fill the digests for a room member** (`IProduct.GetAuditEventsAsync` in the Files product): the
  per-product collection of activity; the spec names only the action families.
- **Rate-limit headers and 429 rows** of `sendAdminMail`, `testSmtpSettings` and `sendEditorNotify`: generic policy.
- **Enum numbers** (`NotificationType`, `RegStatus`): kept out by convention; the yaml lists them.

## Suspected defects

Re-checked against the merge of hotfix/v4.0.1 (`34df477e4e..183da52625`): none of the files behind N1–N9 changed
(`NotifyClient.cs`, `NotificationControllerHelper.cs`, `RoomsNotificationSettings.cs`, `SmtpSettingsController.cs`,
`StudioWhatsNewNotify.cs`, `RoomNewItemsSemanticsTests.cs`, `FilesPatternResource.resx`; the `Actions.cs` hunks of the
merge are the retention letters only), so every one of them is still open; N7's `Actions.cs` lines moved with the
retention insert.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| N1 | Room deleted letter | The subscription and the mute list are checked for `userId` — the member who deleted the room — instead of each recipient, so a manager who switched room activity off or muted the room still gets the letter, and nobody gets it when the deleter is unsubscribed. | `NotifyClient.cs:334-342` (`IsSubscribedToNotifyAsync(userId, …)`, `CheckMuteForRoomAsync(folderId, userId)`) | STILL OPEN |
| N2 | Form letters | `SendFormSubmittedAsync` and `SendFormFillingEvent` send their letters and the submitted push without the rooms-activity subscription or the mute check every other room letter applies, so "room activity off" does not silence them although the setting's description says it stops "the letters a room sends at once". | `NotifyClient.cs:241-308, 513-566`; yaml `setNotificationSettings` | STILL OPEN |
| N3 | `getNotificationSettings` | An integer `type` outside the defined values but accepted by enum binding (e.g. 7) is answered 500 "Incorrect parameters" instead of the documented 400; `setNotificationSettings` with the same value answers 200 and stores nothing. | `NotificationControllerHelper.cs:69, 76-104`; yaml 400 rows | STILL OPEN |
| N4 | New items (bug 81713) | A file updated or renamed by somebody else is not reported as new; the .NET tests `News_FileUpdatedByAnotherUser_Appears` and `News_RenamedFile_AppearsWithNewTitle` demand it, the TypeScript suite marks the case `test.fail`. | `RoomNewItemsSemanticsTests.cs:125-190` | STILL OPEN |
| N5 | Muted rooms | `setRoomsNotificationStatus` stores any id, so a mistyped or deleted room id stays in the list forever; documented, but there is no cleanup. | `RoomsNotificationSettings.cs:68-96`; yaml | STILL OPEN |
| N6 | SMTP save | `useNtlm` is accepted and dropped, and the controller carries `//TODO: Add validation check`: `host`/`senderAddress` emptiness is enforced only by the DTO attributes, not by the settings object. | `SmtpSettingsController.cs` (`SaveSmtpSettings`, `ToSmtpSettingsConfig`) | STILL OPEN |
| N7 | Telegram patterns | Several studio actions declare a Telegram text that no call site ever sends (`portal_deactivate`, `portal_delete`, `user_has_join`, `self_profile_updated`, `backup_failed`), while the sharing letters' `subject_*_tg` keys in the Files resx are referenced by nothing; dead translations that every culture still has to maintain. | `Actions.cs:46, 76, 719, 3839-3850`; `StudioNotifyService.cs:532-552`; `FilesPatternResource.resx` `subject_ShareDocument_tg` | STILL OPEN |
| N8 | Form submitted push | The push goes to the filler and the creator as direct recipients regardless of their device subscription setting for room activity and even when the creator is the actor's own account in a different role; consistent with nothing else in the push catalogue. | `NotifyClient.cs:298-307` | STILL OPEN |
| N9 | Daily feed on hosted portals | The not-paid skip uses `TariffState.NotPaid <= state`, so a portal inside its grace period (`Delay`) still gets digests while a lapsed one does not — intended — but the hourly digest and the at-once letters have no such check, so a lapsed portal keeps sending room letters. | `StudioWhatsNewNotify.cs` (`SendMsgWhatsNewAsync`, tariff check) | STILL OPEN |
