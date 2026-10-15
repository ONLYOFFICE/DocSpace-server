# notifications — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`). `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `Actions.cs` is
`web/ASC.Web.Core/Notify/Actions.cs` (studio letters), `NotifyConstants.cs` is
`products/ASC.Files/Core/Services/NotifyService/NotifyConstants.cs` (Files letters), `NotifyClient.cs` is next to
it, `StudioNotifyService.cs` / `StudioPeriodicNotify.cs` / `StudioWhatsNewNotify.cs` are in
`web/ASC.Web.Core/Notify/`, `WebstudioNotifyPatternResource.resx` is in `web/ASC.Web.Core/PublicResources/`,
`FilesPatternResource.resx` next to `NotifyConstants.cs`. Status: **TEST-CONFIRMED** (a test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the release
notes say it). Exception → status mapping: `SecurityException` 403 "Access denied", `ArgumentException` 400,
`BillingException` 402, `MethodAccessException` 405, other exceptions 500 (`CustomExceptionHandler`,
`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`).

The letter suite `common/Tests/ASC.Notify.Tests` renders a letter through the real pipeline and asserts the
subject, wording, buttons and links (`Infrastructure/LetterTestBase.cs`), and `Periodic/PeriodicLetterScheduleTests.cs`
asserts the day each periodic letter claims; neither sends through a trigger, so "TEST-CONFIRMED" on a letter row
means the subject/text and (for periodic letters) the schedule, while the trigger is CODE-ONLY unless an integration
test is named.

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
| Useful tips gate only the seven marketing periodic letters; billing and removal letters ignore it | `common/Tests/ASC.Notify.Tests/Periodic/PeriodicLetterScheduleTests.cs:556-578` (`OnlyTheMarketingLettersHonourTheUnsubscribe`); `Actions.cs:2146, 2273, 2314, 2354, 2397, 2438, 2524, 2570` | TEST-CONFIRMED |
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
| E-mail + Telegram: subscription method seed `email.sender|telegram.sender` for `send_whats_new` | `common/ASC.Core.Common/EF/Model/Core/DbSubscriptionMethod.cs:66`; `Actions.cs:2633-2634` | CODE-ONLY |
| **Hourly room activity digest** | | |
| Hourly cron; previous hour; actions list; subject "… for the last hour"; no sender-method row so the engine's default senders (e-mail + Telegram) apply | `StudioNotifyServiceSender.cs:27, 44-47`; `StudioWhatsNewNotify.cs` (`RoomsActivityActions`); `Actions.cs:2596-2603`; `common/ASC.Core.Common/Context/WorkContext.cs:54` (`DefaultClientSenders`) | CODE-ONLY |
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
| Suspend / delete instructions to the owner; auto-renew note on a hosted paid tariff; owner-only demand | `web/ASC.Web.Api/Api/PortalController.cs:1030-1044, 1076-1088, 1292-1302`; `StudioNotifyService.cs:532-552` | CODE-ONLY |
| Deletion success letter with the feedback button | `common/Tests/ASC.Notify.Tests/Account/PortalDeleteSuccessLetterTests.cs:55-73`; `PortalController.cs:1199-1201` | TEST-CONFIRMED (text), CODE-ONLY (trigger) |
| Support copy when hosted, not free and `TariffState >= Paid`; skipped when the installation has no support address | `PortalController.cs:1207-1212`; `StudioNotifyService.cs:562-574`; release notes v3.6.1 line 15 | CODE-ONLY |
| **Unused portal warnings and removal** | | |
| Free portal: warning at 3 months idle on the creation anniversary, at 6 months+ (stays on), removal a week after the anniversary when idle 6 months + 7 days; counting starts at the installation's first run; forbidden domains kept | `common/Tests/ASC.Notify.Tests/Periodic/PeriodicLetterScheduleTests.cs:208-257, 497-517, 597-635, 660-727`; `Actions.cs:1768-1790, 2466-2490`; `web/ASC.Web.Core/Notify/PeriodicLetterContext.cs:98-156`; `StudioPeriodicNotify.cs` (`TryRemoveAbandonedPortalAsync`); release notes v3.1 line 17 | TEST-CONFIRMED |
| Lapsed paid portal: warnings 3 and 6 months after the due date, removal 6 months + 7 days after | `PeriodicLetterScheduleTests.cs:318-339, 728-753`; `Actions.cs:1687-1744` | TEST-CONFIRMED |
| Subject "Your <brand> will be deleted" and wording | `Periodic/InactivityWarnings/*.cs` | TEST-CONFIRMED |
| **After-registration letters** | | |
| Days 2, 3, 4, 7, 10, 14; exactly one per day; subjects; admins + owner (day 14 also users); Enterprise trial day 14 with default branding | `PeriodicLetterScheduleTests.cs:136-206, 342-352, 520-546`; `Periodic/Tips/*.cs` subjects; `Actions.cs:2261-2576`; `StudioPeriodicNotify.cs:60-74`; release notes v2.6 line 31 | TEST-CONFIRMED |
| Honour the tips subscription | `PeriodicLetterScheduleTests.cs:556-578` | TEST-CONFIRMED |
| Cron 5:00 daily (`core:notify:cron`), disabled by `core:notify:tariff=false`, SaaS or Enterprise sender chosen per installation | `StudioNotifyServiceSender.cs:14-24` | CODE-ONLY |
| **Payment warning letters** | | |
| SaaS: -3 days, +1 day (grace), grace last day, grace expired; owner + payer; free and trial ignored | `PeriodicLetterScheduleTests.cs:258-317, 643-657`; `Periodic/PaymentWarnings/Saas/*.cs` subjects; `Actions.cs:2666-2801`; `PeriodicLetterDeliveryTests.cs:85-131` (owner / payer recipients) | TEST-CONFIRMED |
| Enterprise / Developer / Lifetime: -7 days and on the day, grace -7 and on the day; exactly one flavour claims a portal; admins | `PeriodicLetterScheduleTests.cs:354-495`; `Periodic/PaymentWarnings/Enterprise/*.cs`, `Developer/*.cs`; `Actions.cs:2854-3224` | TEST-CONFIRMED |
| Not silenced by the tips subscription | `PeriodicLetterScheduleTests.cs:556-578`; `PeriodicLetterDeliveryTests.cs:181-199` | TEST-CONFIRMED |
| **Wallet letters** | | |
| Low balance: once per threshold crossing (`LowBalanceNotified`), payer + owner, e-mail + Telegram | `web/ASC.Web.Studio/Wallet/TopUpWalletService.cs:240-270`; `StudioNotifyService.cs:890-905`; `common/Tests/ASC.Notify.Tests/Billing/LowWalletBalanceLetterTests.cs:45-69`; release notes v4.0 line 77 | TEST-CONFIRMED (text), CODE-ONLY (trigger) |
| Top-up error, auto top-up unavailable, renew error (e-mail + Telegram, payer + owner); upcoming add-on payment 3 days before (e-mail, owner + payer) | `StudioNotifyService.cs:639-690`; `StudioPeriodicNotify.cs` (`SendUpcomingSubscriptionPaymentAsync`); `Billing/TopUpWalletErrorLetterTests.cs:60`, `RenewSubscriptionErrorLetterTests.cs:67`, `UpcomingSubscriptionPaymentLetterTests.cs:55-67`, `WalletAutoTopUpUnavailableLetterTests.cs:61` | TEST-CONFIRMED (texts), CODE-ONLY (triggers) |
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
| v2.6: "Explore useful integrations … (day 10, for managers)" | Day 10 is now "Connect, integrate, and build with <brand> Docs" to admins and the owner (`Actions.cs:2385-2402`). |
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
- **The Telegram bot service itself** (`common/services/ASC.TelegramService`): command handling and message delivery
  plumbing.
- **Which actions fill the digests for a room member** (`IProduct.GetAuditEventsAsync` in the Files product): the
  per-product collection of activity; the spec names only the action families.
- **Rate-limit headers and 429 rows** of `sendAdminMail`, `testSmtpSettings` and `sendEditorNotify`: generic policy.
- **Enum numbers** (`NotificationType`, `RegStatus`): kept out by convention; the yaml lists them.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| N1 | Room deleted letter | The subscription and the mute list are checked for `userId` — the member who deleted the room — instead of each recipient, so a manager who switched room activity off or muted the room still gets the letter, and nobody gets it when the deleter is unsubscribed. | `NotifyClient.cs:334-342` (`IsSubscribedToNotifyAsync(userId, …)`, `CheckMuteForRoomAsync(folderId, userId)`) |
| N2 | Form letters | `SendFormSubmittedAsync` and `SendFormFillingEvent` send their letters and the submitted push without the rooms-activity subscription or the mute check every other room letter applies, so "room activity off" does not silence them although the setting's description says it stops "the letters a room sends at once". | `NotifyClient.cs:241-308, 513-566`; yaml `setNotificationSettings` |
| N3 | `getNotificationSettings` | An integer `type` outside the defined values but accepted by enum binding (e.g. 7) is answered 500 "Incorrect parameters" instead of the documented 400; `setNotificationSettings` with the same value answers 200 and stores nothing. | `NotificationControllerHelper.cs:69, 76-104`; yaml 400 rows |
| N4 | New items (bug 81713) | A file updated or renamed by somebody else is not reported as new; the .NET tests `News_FileUpdatedByAnotherUser_Appears` and `News_RenamedFile_AppearsWithNewTitle` demand it, the TypeScript suite marks the case `test.fail`. | `RoomNewItemsSemanticsTests.cs:125-190` |
| N5 | Muted rooms | `setRoomsNotificationStatus` stores any id, so a mistyped or deleted room id stays in the list forever; documented, but there is no cleanup. | `RoomsNotificationSettings.cs:68-96`; yaml |
| N6 | SMTP save | `useNtlm` is accepted and dropped, and the controller carries `//TODO: Add validation check`: `host`/`senderAddress` emptiness is enforced only by the DTO attributes, not by the settings object. | `SmtpSettingsController.cs` (`SaveSmtpSettings`, `ToSmtpSettingsConfig`) |
| N7 | Telegram patterns | Several studio actions declare a Telegram text that no call site ever sends (`portal_deactivate`, `portal_delete`, `user_has_join`, `self_profile_updated`, `backup_failed`), while the sharing letters' `subject_*_tg` keys in the Files resx are referenced by nothing; dead translations that every culture still has to maintain. | `Actions.cs:46, 76, 719, 3536`; `StudioNotifyService.cs:532-552`; `FilesPatternResource.resx` `subject_ShareDocument_tg` |
| N8 | Form submitted push | The push goes to the filler and the creator as direct recipients regardless of their device subscription setting for room activity and even when the creator is the actor's own account in a different role; consistent with nothing else in the push catalogue. | `NotifyClient.cs:298-307` |
| N9 | Daily feed on hosted portals | The not-paid skip uses `TariffState.NotPaid <= state`, so a portal inside its grace period (`Delay`) still gets digests while a lapsed one does not — intended — but the hourly digest and the at-once letters have no such check, so a lapsed portal keeps sending room letters. | `StudioWhatsNewNotify.cs` (`SendMsgWhatsNewAsync`, tariff check) |
