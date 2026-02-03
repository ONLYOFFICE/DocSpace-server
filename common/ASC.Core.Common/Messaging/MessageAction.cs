// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.MessagingSystem.Core;

/// <summary>
/// The event action ID.
/// </summary>
[EnumExtensions]
public enum MessageAction
{
    [Description("None")]
    None = -1,

    #region Login
    [Description("Login success")]
    LoginSuccess = 1000,

    [Description("Login success via social account")]
    LoginSuccessViaSocialAccount = 1001,

    [Description("Login success via sms")]
    LoginSuccessViaSms = 1007,

    [Description("Login success via api")]
    LoginSuccessViaApi = 1010,

    [Description("Login success via social app")]
    LoginSuccessViaSocialApp = 1011,

    [Description("Login success via api sms")]
    LoginSuccessViaApiSms = 1012,

    [Description("Login success via api tfa")]
    LoginSuccessViaApiTfa = 1024,

    [Description("Login success via api social account")]
    LoginSuccessViaApiSocialAccount = 1019,

    [Description("Login success via SSO")]
    LoginSuccessViaSSO = 1015,

    [Description("Login succes via tfa app")]
    LoginSuccesViaTfaApp = 1021,

    [Description("Login fail via SSO")]
    LoginFailViaSSO = 1018,

    [Description("Login fail invalid combination")]
    LoginFailInvalidCombination = 1002,

    [Description("Login fail social account not found")]
    LoginFailSocialAccountNotFound = 1003,

    [Description("Login fail disabled profile")]
    LoginFailDisabledProfile = 1004,

    [Description("Login fail")]
    LoginFail = 1005,

    [Description("Login fail via sms")]
    LoginFailViaSms = 1008,

    [Description("Login fail via api")]
    LoginFailViaApi = 1013,

    [Description("Login fail via api sms")]
    LoginFailViaApiSms = 1014,

    [Description("Login fail via api tfa")]
    LoginFailViaApiTfa = 1025,

    [Description("Login fail via api social account")]
    LoginFailViaApiSocialAccount = 1020,

    [Description("Login fail via Tfa app")]
    LoginFailViaTfaApp = 1022,

    [Description("Login fail ip security")]
    LoginFailIpSecurity = 1009,

    [Description("Login fail brute force")]
    LoginFailBruteForce = 1023,

    [Description("Login fail recaptcha")]
    LoginFailRecaptcha = 1026,

    [Description("Logout")]
    Logout = 1006,

    [Description("Session started")]
    SessionStarted = 1016,

    [Description("Session completed")]
    SessionCompleted = 1017,

    [Description("Authorization link activated")]
    AuthLinkActivated = 1027,

    [Description("Login success via OAuth 2.0")]
    LoginSuccessViaOAuth = 1028,

    [Description("Login success via login and password")]
    LoginSuccessViaPassword = 1029,  // last login

    #endregion

    #region People

    [Description("User created")]
    UserCreated = 4000,

    [Description("Guest created")]
    GuestCreated = 4001,

    [Description("User created via invite")]
    UserCreatedViaInvite = 4002,

    [Description("Guest created via invite")]
    GuestCreatedViaInvite = 4003,

    [Description("Send join invite")]
    SendJoinInvite = 4037,

    [Description("User activated")]
    UserActivated = 4004,

    [Description("Guest activated")]
    GuestActivated = 4005,

    [Description("User updated")]
    UserUpdated = 4006,

    [Description("User updated mobile number")]
    UserUpdatedMobileNumber = 4029,

    [Description("User updated language")]
    UserUpdatedLanguage = 4007,

    [Description("User added avatar")]
    UserAddedAvatar = 4008,

    [Description("User deleted avatar")]
    UserDeletedAvatar = 4009,

    [Description("User updated avatar thumbnails")]
    UserUpdatedAvatarThumbnails = 4010,

    [Description("User linked social account")]
    UserLinkedSocialAccount = 4011,

    [Description("User unlinked social account")]
    UserUnlinkedSocialAccount = 4012,

    [Description("User connected tfa app")]
    UserConnectedTfaApp = 4032,

    [Description("User disconnected tfa app")]
    UserDisconnectedTfaApp = 4033,

    [Description("User sent activation instructions")]
    UserSentActivationInstructions = 4013,

    [Description("User sent email change instructions")]
    UserSentEmailChangeInstructions = 4014,

    [Description("User sent password change instructions")]
    UserSentPasswordChangeInstructions = 4015,

    [Description("User sent delete instructions")]
    UserSentDeleteInstructions = 4016,

    [Description("User updated email")]
    UserUpdatedEmail = 5047,

    [Description("User updated password")]
    UserUpdatedPassword = 4017,

    [Description("User deleted")]
    UserDeleted = 4018,

    [Description("Users updated type")]
    UsersUpdatedType = 4019,

    [Description("Users updated status")]
    UsersUpdatedStatus = 4020,

    [Description("Users sent activation instructions")]
    UsersSentActivationInstructions = 4021,

    [Description("Users deleted")]
    UsersDeleted = 4022,

    [Description("Sent invite instructions")]
    SentInviteInstructions = 4023,

    [Description("User imported")]
    UserImported = 4024,

    [Description("Guest imported")]
    GuestImported = 4025,

    [Description("Group created")]
    GroupCreated = 4026,

    [Description("Group updated")]
    GroupUpdated = 4027,

    [Description("Group deleted")]
    GroupDeleted = 4028,

    [Description("User data reassigns")]
    UserDataReassigns = 4030,

    [Description("User data removing")]
    UserDataRemoving = 4031,

    [Description("User logout active connections")]
    UserLogoutActiveConnections = 4034,

    [Description("User logout active connection")]
    UserLogoutActiveConnection = 4035,

    [Description("User logout active connections for user")]
    UserLogoutActiveConnectionsForUser = 4036,

    #endregion

    #region Documents

    [Description("File created")]
    FileCreated = 5000,

    [Description("File renamed")]
    FileRenamed = 5001,

    [Description("File updated")]
    FileUpdated = 5002,

    [Description("User file updated")]
    UserFileUpdated = 5034,

    [Description("File created version")]
    FileCreatedVersion = 5003,

    [Description("File deleted version")]
    FileDeletedVersion = 5004,

    [Description("File restore version")]
    FileRestoreVersion = 5044,

    [Description("File updated revision comment")]
    FileUpdatedRevisionComment = 5005,

    [Description("File locked")]
    FileLocked = 5006,

    [Description("File unlocked")]
    FileUnlocked = 5007,

    [Description("File updated access")]
    FileUpdatedAccess = 5008,

    [Description("File updated access for")]
    FileUpdatedAccessFor = 5068,

    [Description("File send access link")]
    FileSendAccessLink = 5036, // not used

    [Description("File opened for change")]
    FileOpenedForChange = 5054,

    [Description("File removed from list")]
    FileRemovedFromList = 5058,

    [Description("File external link access updated")]
    FileExternalLinkAccessUpdated = 5060,

    [Description("File downloaded")]
    FileDownloaded = 5009,

    [Description("File downloaded as")]
    FileDownloadedAs = 5010,

    [Description("File revision downloaded")]
    FileRevisionDownloaded = 5062,

    [Description("File uploaded")]
    FileUploaded = 5011,

    [Description("File imported")]
    FileImported = 5012,

    [Description("File uploaded with overwriting")]
    FileUploadedWithOverwriting = 5099,

    [Description("File copied")]
    FileCopied = 5013,

    [Description("File copied with overwriting")]
    FileCopiedWithOverwriting = 5014,

    [Description("File moved")]
    FileMoved = 5015,

    [Description("File moved with overwriting")]
    FileMovedWithOverwriting = 5016,

    [Description("File moved to trash")]
    FileMovedToTrash = 5017,

    [Description("File deleted")]
    FileDeleted = 5018,

    [Description("File version deleted")]
    FileVersionRemoved = 5119,

    [Description("File index changed")]
    FileIndexChanged = 5111,

    [Description("File custom filter enabled")]
    FileCustomFilterEnabled = 5120,

    [Description("File custom filter disabled")]
    FileCustomFilterDisabled = 5121,

    [Description("Folder created")]
    FolderCreated = 5019,

    [Description("Folder renamed")]
    FolderRenamed = 5020,

    [Description("Folder updated access")]
    FolderUpdatedAccess = 5021,

    [Description("Folder updated access for")]
    FolderUpdatedAccessFor = 5066,

    [Description("Folder copied")]
    FolderCopied = 5022,

    [Description("Folder copied with overwriting")]
    FolderCopiedWithOverwriting = 5023,

    [Description("Folder moved")]
    FolderMoved = 5024,

    [Description("Folder moved with overwriting")]
    FolderMovedWithOverwriting = 5025,

    [Description("Folder moved to trash")]
    FolderMovedToTrash = 5026,

    [Description("Folder deleted")]
    FolderDeleted = 5027,

    [Description("Folder removed from list")]
    FolderRemovedFromList = 5059,

    [Description("Folder index changed")]
    FolderIndexChanged = 5107,

    [Description("Folder index reordered")]
    FolderIndexReordered = 5108,

    [Description("Folder downloaded")]
    FolderDownloaded = 5057,

    [Description("Form submit")]
    FormSubmit = 6046,

    [Description("Form opened for filling")]
    FormOpenedForFilling = 6047,

    [Description("ThirdParty created")]
    ThirdPartyCreated = 5028,

    [Description("ThirdParty updated")]
    ThirdPartyUpdated = 5029,

    [Description("ThirdParty deleted")]
    ThirdPartyDeleted = 5030,

    [Description("Documents ThirdParty settings updated")]
    DocumentsThirdPartySettingsUpdated = 5031,

    [Description("Documents overwriting settings updated")]
    DocumentsOverwritingSettingsUpdated = 5032,

    [Description("Documents forcesave")]
    DocumentsForcesave = 5049,

    [Description("Documents store forcesave")]
    DocumentsStoreForcesave = 5048,

    [Description("Documents uploading formats settings updated")]
    DocumentsUploadingFormatsSettingsUpdated = 5033,

    [Description("Documents external share settings updated")]
    DocumentsExternalShareSettingsUpdated = 5069,

    [Description("Documents keep new file name settings updated")]
    DocumentsKeepNewFileNameSettingsUpdated = 5083,

    [Description("Documents display file extension updated")]
    DocumentsDisplayFileExtensionUpdated = 5101,

    [Description("File converted")]
    FileConverted = 5035,

    [Description("File change owner")]
    FileChangeOwner = 5043,

    [Description("Document sign complete")]
    DocumentSignComplete = 5046,

    [Description("Document send to sign")]
    DocumentSendToSign = 5045,

    [Description("File marked as favorite")]
    FileMarkedAsFavorite = 5055,

    [Description("File removed from favorite")]
    FileRemovedFromFavorite = 5056,

    [Description("File marked as read")]
    FileMarkedAsRead = 5063,

    [Description("File readed")]
    FileReaded = 5064,

    [Description("Trash emptied")]
    TrashEmptied = 5061,

    [Description("Folder marked as read")]
    FolderMarkedAsRead = 5065,

    [Description("Room created")]
    RoomCreated = 5070,

    [Description("Room renamed")]
    RoomRenamed = 5071,

    [Description("Room archived")]
    RoomArchived = 5072,

    [Description("Room unarchived")]
    RoomUnarchived = 5073,

    [Description("Room deleted")]
    RoomDeleted = 5074,

    [Description("Room copied")]
    RoomCopied = 5100,

    [Description("Room update access for user")]
    RoomUpdateAccessForUser = 5075,

    [Description("Room remove user")]
    RoomRemoveUser = 5084,

    [Description("Room create user")]
    RoomCreateUser = 5085,

    [Description("Room invitation link updated")]
    RoomInvitationLinkUpdated = 5082,

    [Description("Room invitation link created")]
    RoomInvitationLinkCreated = 5086,

    [Description("Room invitation link deleted")]
    RoomInvitationLinkDeleted = 5087,

    [Description("Room group added")]
    RoomGroupAdded = 5094,

    [Description("Room update access for group")]
    RoomUpdateAccessForGroup = 5095,

    [Description("Room group remove")]
    RoomGroupRemove = 5096,

    [Description("Tag created")]
    TagCreated = 5076,

    [Description("Tags deleted")]
    TagsDeleted = 5077,

    [Description("Added room tags")]
    AddedRoomTags = 5078,

    [Description("Deleted room tags")]
    DeletedRoomTags = 5079,

    [Description("Room logo created")]
    RoomLogoCreated = 5080,

    [Description("Room logo deleted")]
    RoomLogoDeleted = 5081,

    [Description("Room color changed")]
    RoomColorChanged = 5102,

    [Description("Room cover changed")]
    RoomCoverChanged = 5103,

    [Description("Room indexing changed")]
    RoomIndexingChanged = 5104,

    [Description("Room deny download changed")]
    RoomDenyDownloadChanged = 5105,

    [Description("Room external link created")]
    RoomExternalLinkCreated = 5088,

    [Description("Room external link updated")]
    RoomExternalLinkUpdated = 5089,

    [Description("Room external link deleted")]
    RoomExternalLinkDeleted = 5090,

    [Description("Room external link revoked")]
    RoomExternalLinkRevoked = 5097,

    [Description("Room external link renamed")]
    RoomExternalLinkRenamed = 5098,

    [Description("File external link created")]
    FileExternalLinkCreated = 5091,

    [Description("File external link updated")]
    FileExternalLinkUpdated = 5092,

    [Description("File external link deleted")]
    FileExternalLinkDeleted = 5093,

    [Description("Folder external link created")]
    FolderExternalLinkCreated = 5122,

    [Description("Folder external link updated")]
    FolderExternalLinkUpdated = 5123,

    [Description("Folder external link deleted")]
    FolderExternalLinkDeleted = 5124,

    [Description("Room index export saved")]
    RoomIndexingEnabled = 5114,

    [Description("Room indexing disabled")]
    RoomIndexingDisabled = 5115,

    [Description("Room life time set")]
    RoomLifeTimeSet = 5116,

    [Description("Room life time disabled")]
    RoomLifeTimeDisabled = 5117,

    [Description("Room deny download enabled")]
    RoomDenyDownloadEnabled = 5109,

    [Description("Room deny download disabled")]
    RoomDenyDownloadDisabled = 5110,

    [Description("Room watermark set")]
    RoomWatermarkSet = 5112,

    [Description("Room watermark disabled")]
    RoomWatermarkDisabled = 5113,

    [Description("Room invite resend")]
    RoomInviteResend = 5118,

    [Description("Room index export saved")]
    RoomIndexExportSaved = 5106,

    [Description("Form started to fill")]
    FormStartedToFill = 5150,

    [Description("Form partially filled")]
    FormPartiallyFilled = 5151,

    [Description("Form completely filled")]
    FormCompletelyFilled = 5152,

    [Description("Form stopped")]
    FormStopped = 5153,
    
    [Description("AI agent created")]
    AgentCreated = 5154,

    [Description("AI agent renamed")]
    AgentRenamed = 5155,
    
    [Description("AI agent deleted")]
    AgentDeleted = 5156,
    
    [Description("MCP server added to AI agent")]
    AddedServerToAgent = 5157,
    
    [Description("MCP server deleted from AI agent")]
    DeletedServerFromAgent = 5158,

    [Description("Room change owner")]
    RoomChangeOwner = 5159,

    #endregion

    #region Settings

    [Description("Language settings updated")]
    LanguageSettingsUpdated = 6000,

    [Description("Time zone settings updated")]
    TimeZoneSettingsUpdated = 6001,

    [Description("Dns settings updated")]
    DnsSettingsUpdated = 6002,

    [Description("Trusted mail domain settings updated")]
    TrustedMailDomainSettingsUpdated = 6003,

    [Description("Password strength settings updated")]
    PasswordStrengthSettingsUpdated = 6004,

    [Description("Two factor authentication settings updated")]
    TwoFactorAuthenticationSettingsUpdated = 6005, // deprecated - use 6036-6038 instead

    [Description("Administrator message settings updated")]
    AdministratorMessageSettingsUpdated = 6006,

    [Description("Default start page settings updated")]
    DefaultStartPageSettingsUpdated = 6007,

    [Description("Products list updated")]
    ProductsListUpdated = 6008,

    [Description("Administrator added")]
    AdministratorAdded = 6009,

    [Description("Administrator opened full access")]
    AdministratorOpenedFullAccess = 6010,

    [Description("Administrator deleted")]
    AdministratorDeleted = 6011,

    [Description("Users opened product access")]
    UsersOpenedProductAccess = 6012,

    [Description("Groups opened product access")]
    GroupsOpenedProductAccess = 6013,

    [Description("Product access opened")]
    ProductAccessOpened = 6014,

    [Description("Product access restricted")]
    ProductAccessRestricted = 6015, // not used

    [Description("Product added administrator")]
    ProductAddedAdministrator = 6016,

    [Description("Product deleted administrator")]
    ProductDeletedAdministrator = 6017,

    [Description("Greeting settings updated")]
    GreetingSettingsUpdated = 6018,

    [Description("Team template changed")]
    TeamTemplateChanged = 6019,

    [Description("Color theme changed")]
    ColorThemeChanged = 6020,

    [Description("Owner sent change owner instructions")]
    OwnerSentChangeOwnerInstructions = 6021,

    [Description("Owner updated")]
    OwnerUpdated = 6022,

    [Description("Owner sent portal deactivation instructions")]
    OwnerSentPortalDeactivationInstructions = 6023,

    [Description("Owner sent portal delete instructions")]
    OwnerSentPortalDeleteInstructions = 6024,

    [Description("Portal deactivated")]
    PortalDeactivated = 6025,

    [Description("Portal deleted")]
    PortalDeleted = 6026,

    [Description("Login history report downloaded")]
    LoginHistoryReportDownloaded = 6027,

    [Description("Audit trail report downloaded")]
    AuditTrailReportDownloaded = 6028,

    [Description("SSO enabled")]
    SSOEnabled = 6029,

    [Description("SSO disabled")]
    SSODisabled = 6030,

    [Description("Portal access settings updated")]
    PortalAccessSettingsUpdated = 6031,

    [Description("Cookie settings updated")]
    CookieSettingsUpdated = 6032,

    [Description("Mail service settings updated")]
    MailServiceSettingsUpdated = 6033,

    [Description("Custom navigation settings updated")]
    CustomNavigationSettingsUpdated = 6034,

    [Description("Audit settings updated")]
    AuditSettingsUpdated = 6035,

    [Description("Two factor authentication disabled")]
    TwoFactorAuthenticationDisabled = 6036,

    [Description("Two factor authentication enabled by sms")]
    TwoFactorAuthenticationEnabledBySms = 6037,

    [Description("Two factor authentication enabled by tfa app")]
    TwoFactorAuthenticationEnabledByTfaApp = 6038,

    [Description("Portal renamed")]
    PortalRenamed = 6039,

    [Description("Quota per room changed")]
    QuotaPerRoomChanged = 6040,

    [Description("Quota per room disabled")]
    QuotaPerRoomDisabled = 6041,

    [Description("Quota per user changed")]
    QuotaPerUserChanged = 6042,

    [Description("Quota per user disabled")]
    QuotaPerUserDisabled = 6043,

    [Description("Quota per portal changed")]
    QuotaPerPortalChanged = 6044,

    [Description("Quota per portal disabled")]
    QuotaPerPortalDisabled = 6045,

    [Description("Custom quota per room default")]
    CustomQuotaPerRoomDefault = 6048,

    [Description("Custom quota per room changed")]
    CustomQuotaPerRoomChanged = 6049,

    [Description("Custom quota per room disabled")]
    CustomQuotaPerRoomDisabled = 6050,

    [Description("Custom quota per user default")]
    CustomQuotaPerUserDefault = 6051,

    [Description("Custom quota per user changed")]
    CustomQuotaPerUserChanged = 6052,

    [Description("Custom quota per user disabled")]
    CustomQuotaPerUserDisabled = 6053,

    [Description("DevTools access settings changed")]
    DevToolsAccessSettingsChanged = 6054,

    [Description("Webhook created")]
    WebhookCreated = 6055,

    [Description("Webhook updated")]
    WebhookUpdated = 6056,

    [Description("Webhook deleted")]
    WebhookDeleted = 6057,

    [Description("Created api key")]
    ApiKeyCreated = 6058,

    [Description("Update api key")]
    ApiKeyUpdated = 6059,

    [Description("Deleted User api key")]
    ApiKeyDeleted = 6060,

    [Description("Document service location setting")]
    DocumentServiceLocationSetting = 5037,

    [Description("Authorization keys setting")]
    AuthorizationKeysSetting = 5038,

    [Description("Full text search setting")]
    FullTextSearchSetting = 5039,

    [Description("Start transfer setting")]
    StartTransferSetting = 5040,

    [Description("Backup started")]
    BackupStarted = 5041,

    [Description("Backup completed")]
    BackupCompleted = 5125,

    [Description("Backup failed")]
    BackupFailed = 5126,

    [Description("Scheduled backup started")]
    ScheduledBackupStarted = 5127,

    [Description("Scheduled backup completed")]
    ScheduledBackupCompleted = 5128,

    [Description("Scheduled backup failed")]
    ScheduledBackupFailed = 5129,

    [Description("License key uploaded")]
    LicenseKeyUploaded = 5042,

    [Description("Start storage encryption")]
    StartStorageEncryption = 5050,

    [Description("Privacy room enable")]
    PrivacyRoomEnable = 5051,

    [Description("Privacy room disable")]
    PrivacyRoomDisable = 5052,

    [Description("Start storage decryption")]
    StartStorageDecryption = 5053,

    [Description("Customer wallet topped up")]
    CustomerWalletToppedUp = 6061,

    [Description("Customer operation performed")]
    CustomerOperationPerformed = 6062,

    [Description("Customer operations report downloaded")]
    CustomerOperationsReportDownloaded = 6063,

    [Description("Customer wallet top up settings updated")]
    CustomerWalletTopUpSettingsUpdated = 6064,

    [Description("Customer subscription updated")]
    CustomerSubscriptionUpdated = 6065,

    [Description("Promotional banners visibility settings changed")]
    BannerSettingsChanged = 6066,

    [Description("Customer wallet services settings updated")]
    CustomerWalletServicesSettingsUpdated = 6067,

    [Description("Quota per AI agent changed")]
    QuotaPerAiAgentChanged = 6068,

    [Description("Quota per AI agent disabled")]
    QuotaPerAiAgentDisabled = 6069,

    [Description("Custom quota per AI agent default")]
    CustomQuotaPerAiAgentDefault = 6070,

    [Description("Custom quota per AI agent changed")]
    CustomQuotaPerAiAgentChanged = 6071,

    [Description("Custom quota per AI agent disabled")]
    CustomQuotaPerAiAgentDisabled = 6072,

    [Description("AI provider created")]
    AIProviderCreated = 6073,

    [Description("AI provider updated")]
    AIProviderUpdated = 6074,

    [Description("AI provider deleted")]
    AIProviderDeleted = 6075,

    [Description("MCP server created")]
    ServerCreated = 6076,
    
    [Description("MCP server updated")]
    ServerUpdated = 6077,
    
    [Description("MCP server enabled")]
    ServerEnabled = 6078,
    
    [Description("MCP server disabled")]
    ServerDisabled = 6079,
    
    [Description("MCP server deleted")]
    ServerDeleted = 6080,
    
    [Description("WebSearch settings configured")]
    SetWebSearchSettings = 6081,
    
    [Description("WebSearch settings reset")]
    ResetWebSearchSettings = 6082,
    
    [Description("Vectorization settings configured")]
    SetVectorizationSettings = 6083,
    
    [Description("Vectorization settings reset")]
    ResetVectorizationSettings = 6084,

    [Description("Webplugin uploaded")]
    WebpluginUploaded = 6085,

    [Description("Webplugin updated")]
    WebpluginUpdated = 6086,

    [Description("Webplugin deleted")]
    WebpluginDeleted = 6087,

    [Description("Whitelabel settings logo text updated")]
    WhiteLabelSettingsLogoTextUpdated = 6088,

    [Description("Whitelabel settings logos updated")]
    WhiteLabelSettingsLogosUpdated = 6089,

    [Description("Whitelabel company settings updated")]
    WhiteLabelCompanySettingsUpdated = 6090,

    [Description("Whitelabel additional settings updated")]
    WhiteLabelAdditionalSettingsUpdated = 6091,

    [Description("Whitelabel mail settings updated")]
    WhiteLabelMailSettingsUpdated = 6092,

    [Description("Invitation settings updated")]
    InvitationSettingsUpdated = 6093,

    [Description("IP restrictions settings updated")]
    IPRestrictionsSettingsUpdated = 6094,

    [Description("Login settings updated")]
    LoginSettingsUpdated = 6095,
    
    [Description("AI default provider set")]
    AIDefaultProviderSet = 6096,

    #endregion

    #region others
    [Description("Contact admin mail sent")]
    ContactAdminMailSent = 7000,

    [Description("Room invite link used")]
    RoomInviteLinkUsed = 7001,

    [Description("User created and added to room")]
    UserCreatedAndAddedToRoom = 7002,

    [Description("Guest created and added to room")]
    GuestCreatedAndAddedToRoom = 7003,

    [Description("Contact sales mail sent")]
    ContactSalesMailSent = 7004,

    #endregion

    #region Oauth

    [Description("Create client")]
    CreateClient = 9901,

    [Description("Update client")]
    UpdateClient = 9902,

    [Description("Regenerate secret")]
    RegenerateSecret = 9903,

    [Description("Delete client")]
    DeleteClient = 9904,

    [Description("Change client activation")]
    ChangeClientActivation = 9905,

    [Description("Change client visibility")]
    ChangeClientVisibility = 9906,

    [Description("Revoke user client")]
    RevokeUserClient = 9907,

    [Description("Generate authorization code token")]
    GenerateAuthorizationCodeToken = 9908,

    [Description("Generate personal access token")]
    GeneratePersonalAccessToken = 9909,

    #endregion

    #region Ldap

    [Description("Ldap enabled")]
    LdapEnabled = 5501,

    [Description("Ldap disabled")]
    LdapDisabled = 5502,

    [Description("LDAP synchronization completed")]
    LdapSync = 5503

    #endregion
}