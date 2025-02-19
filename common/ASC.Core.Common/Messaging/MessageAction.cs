// (c) Copyright Ascensio System SIA 2009-2024
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

[EnumExtensions]
public enum MessageAction
{
    [OpenApiEnum("None")]
    None = -1,

    #region Login
    [OpenApiEnum("Login success")]
    LoginSuccess = 1000,

    [OpenApiEnum("Login success via social account")]
    LoginSuccessViaSocialAccount = 1001,

    [OpenApiEnum("Login success via sms")]
    LoginSuccessViaSms = 1007,

    [OpenApiEnum("Login success via api")]
    LoginSuccessViaApi = 1010,

    [OpenApiEnum("Login success via social app")]
    LoginSuccessViaSocialApp = 1011,

    [OpenApiEnum("Login success via api sms")]
    LoginSuccessViaApiSms = 1012,

    [OpenApiEnum("Login success via api tfa")]
    LoginSuccessViaApiTfa = 1024,

    [OpenApiEnum("Login success via api social account")]
    LoginSuccessViaApiSocialAccount = 1019,

    [OpenApiEnum("Login success via SSO")]
    LoginSuccessViaSSO = 1015,

    [OpenApiEnum("Login succes via tfa app")]
    LoginSuccesViaTfaApp = 1021,

    [OpenApiEnum("Login fail via SSO")]
    LoginFailViaSSO = 1018,

    [OpenApiEnum("Login fail invalid combination")]
    LoginFailInvalidCombination = 1002,

    [OpenApiEnum("Login fail social account not found")]
    LoginFailSocialAccountNotFound = 1003,

    [OpenApiEnum("Login fail disabled profile")]
    LoginFailDisabledProfile = 1004,

    [OpenApiEnum("Login fail")]
    LoginFail = 1005,

    [OpenApiEnum("Login fail via sms")]
    LoginFailViaSms = 1008,

    [OpenApiEnum("Login fail via api")]
    LoginFailViaApi = 1013,

    [OpenApiEnum("Login fail via api sms")]
    LoginFailViaApiSms = 1014,

    [OpenApiEnum("Login fail via api tfa")]
    LoginFailViaApiTfa = 1025,

    [OpenApiEnum("Login fail via api social account")]
    LoginFailViaApiSocialAccount = 1020,

    [OpenApiEnum("Login fail via Tfa app")]
    LoginFailViaTfaApp = 1022,

    [OpenApiEnum("Login fail ip security")]
    LoginFailIpSecurity = 1009,

    [OpenApiEnum("Login fail brute force")]
    LoginFailBruteForce = 1023,

    [OpenApiEnum("Login fail recaptcha")]
    LoginFailRecaptcha = 1026,  // last login

    [OpenApiEnum("Logout")]
    Logout = 1006,

    [OpenApiEnum("Session started")]
    SessionStarted = 1016,

    [OpenApiEnum("Session completed")]
    SessionCompleted = 1017,

    #endregion

    #region People

    [OpenApiEnum("User created")]
    UserCreated = 4000,

    [OpenApiEnum("Guest created")]
    GuestCreated = 4001,

    [OpenApiEnum("User created via invite")]
    UserCreatedViaInvite = 4002,

    [OpenApiEnum("Guest created via invite")]
    GuestCreatedViaInvite = 4003,

    [OpenApiEnum("Send join invite")]
    SendJoinInvite = 4037,

    [OpenApiEnum("User activated")]
    UserActivated = 4004,

    [OpenApiEnum("Guest activated")]
    GuestActivated = 4005,

    [OpenApiEnum("User updated")]
    UserUpdated = 4006,

    [OpenApiEnum("User updated mobile number")]
    UserUpdatedMobileNumber = 4029,

    [OpenApiEnum("User updated language")]
    UserUpdatedLanguage = 4007,

    [OpenApiEnum("User added avatar")]
    UserAddedAvatar = 4008,

    [OpenApiEnum("User deleted avatar")]
    UserDeletedAvatar = 4009,

    [OpenApiEnum("User updated avatar thumbnails")]
    UserUpdatedAvatarThumbnails = 4010,

    [OpenApiEnum("User linked social account")]
    UserLinkedSocialAccount = 4011,

    [OpenApiEnum("User unlinked social account")]
    UserUnlinkedSocialAccount = 4012,

    [OpenApiEnum("User connected tfa app")]
    UserConnectedTfaApp = 4032,

    [OpenApiEnum("User disconnected tfa app")]
    UserDisconnectedTfaApp = 4033,

    [OpenApiEnum("User sent activation instructions")]
    UserSentActivationInstructions = 4013,

    [OpenApiEnum("User sent email change instructions")]
    UserSentEmailChangeInstructions = 4014,

    [OpenApiEnum("User sent password change instructions")]
    UserSentPasswordChangeInstructions = 4015,

    [OpenApiEnum("User sent delete instructions")]
    UserSentDeleteInstructions = 4016,

    [OpenApiEnum("User updated email")]
    UserUpdatedEmail = 5047,

    [OpenApiEnum("User updated password")]
    UserUpdatedPassword = 4017,

    [OpenApiEnum("User deleted")]
    UserDeleted = 4018,

    [OpenApiEnum("Users updated type")]
    UsersUpdatedType = 4019,

    [OpenApiEnum("Users updated status")]
    UsersUpdatedStatus = 4020,

    [OpenApiEnum("Users sent activation instructions")]
    UsersSentActivationInstructions = 4021,

    [OpenApiEnum("Users deleted")]
    UsersDeleted = 4022,

    [OpenApiEnum("Sent invite instructions")]
    SentInviteInstructions = 4023,

    [OpenApiEnum("User imported")]
    UserImported = 4024,

    [OpenApiEnum("Guest imported")]
    GuestImported = 4025,

    [OpenApiEnum("Group created")]
    GroupCreated = 4026,

    [OpenApiEnum("Group updated")]
    GroupUpdated = 4027,

    [OpenApiEnum("Group deleted")]
    GroupDeleted = 4028,

    [OpenApiEnum("User data reassigns")]
    UserDataReassigns = 4030,

    [OpenApiEnum("User data removing")]
    UserDataRemoving = 4031,

    [OpenApiEnum("User logout active connections")]
    UserLogoutActiveConnections = 4034,

    [OpenApiEnum("User logout active connection")]
    UserLogoutActiveConnection = 4035,

    [OpenApiEnum("User logout active connections for user")]
    UserLogoutActiveConnectionsForUser = 4036,

    #endregion

    #region Documents

    [OpenApiEnum("File created")]
    FileCreated = 5000,

    [OpenApiEnum("File renamed")]
    FileRenamed = 5001,

    [OpenApiEnum("File updated")]
    FileUpdated = 5002,

    [OpenApiEnum("User file updated")]
    UserFileUpdated = 5034,

    [OpenApiEnum("File created version")]
    FileCreatedVersion = 5003,

    [OpenApiEnum("File deleted version")]
    FileDeletedVersion = 5004,

    [OpenApiEnum("File restore version")]
    FileRestoreVersion = 5044,

    [OpenApiEnum("File updated revision comment")]
    FileUpdatedRevisionComment = 5005,

    [OpenApiEnum("File locked")]
    FileLocked = 5006,

    [OpenApiEnum("File unlocked")]
    FileUnlocked = 5007,

    [OpenApiEnum("File updated access")]
    FileUpdatedAccess = 5008,

    [OpenApiEnum("File updated access for")]
    FileUpdatedAccessFor = 5068,

    [OpenApiEnum("File send access link")]
    FileSendAccessLink = 5036, // not used

    [OpenApiEnum("File opened for change")]
    FileOpenedForChange = 5054,

    [OpenApiEnum("File removed from list")]
    FileRemovedFromList = 5058,

    [OpenApiEnum("File external link access updated")]
    FileExternalLinkAccessUpdated = 5060,

    [OpenApiEnum("File downloaded")]
    FileDownloaded = 5009,

    [OpenApiEnum("File downloaded as")]
    FileDownloadedAs = 5010,

    [OpenApiEnum("File revision downloaded")]
    FileRevisionDownloaded = 5062,

    [OpenApiEnum("File uploaded")]
    FileUploaded = 5011,

    [OpenApiEnum("File imported")]
    FileImported = 5012,

    [OpenApiEnum("File uploaded with overwriting")]
    FileUploadedWithOverwriting = 5099,

    [OpenApiEnum("File copied")]
    FileCopied = 5013,

    [OpenApiEnum("File copied with overwriting")]
    FileCopiedWithOverwriting = 5014,

    [OpenApiEnum("File moved")]
    FileMoved = 5015,

    [OpenApiEnum("File moved with overwriting")]
    FileMovedWithOverwriting = 5016,

    [OpenApiEnum("File moved to trash")]
    FileMovedToTrash = 5017,

    [OpenApiEnum("File deleted")]
    FileDeleted = 5018,
    
    [OpenApiEnum("File version deleted")]
    FileVersionRemoved = 5119,

    [OpenApiEnum("File index changed")]
    FileIndexChanged = 5111,

    [OpenApiEnum("Folder created")]
    FolderCreated = 5019,

    [OpenApiEnum("Folder renamed")]
    FolderRenamed = 5020,

    [OpenApiEnum("Folder updated access")]
    FolderUpdatedAccess = 5021,

    [OpenApiEnum("Folder updated access for")]
    FolderUpdatedAccessFor = 5066,

    [OpenApiEnum("Folder copied")]
    FolderCopied = 5022,

    [OpenApiEnum("Folder copied with overwriting")]
    FolderCopiedWithOverwriting = 5023,

    [OpenApiEnum("Folder moved")]
    FolderMoved = 5024,

    [OpenApiEnum("Folder moved with overwriting")]
    FolderMovedWithOverwriting = 5025,

    [OpenApiEnum("Folder moved to trash")]
    FolderMovedToTrash = 5026,

    [OpenApiEnum("Folder deleted")]
    FolderDeleted = 5027,

    [OpenApiEnum("Folder removed from list")]
    FolderRemovedFromList = 5059,

    [OpenApiEnum("Folder index changed")]
    FolderIndexChanged = 5107,

    [OpenApiEnum("Folder index reordered")]
    FolderIndexReordered = 5108,

    [OpenApiEnum("Folder downloaded")]
    FolderDownloaded = 5057,

    [OpenApiEnum("Form submit")]
    FormSubmit = 6046,

    [OpenApiEnum("Form opened for filling")]
    FormOpenedForFilling = 6047,

    [OpenApiEnum("ThirdParty created")]
    ThirdPartyCreated = 5028,

    [OpenApiEnum("ThirdParty updated")]
    ThirdPartyUpdated = 5029,

    [OpenApiEnum("ThirdParty deleted")]
    ThirdPartyDeleted = 5030,

    [OpenApiEnum("Documents ThirdParty settings updated")]
    DocumentsThirdPartySettingsUpdated = 5031,

    [OpenApiEnum("Documents overwriting settings updated")]
    DocumentsOverwritingSettingsUpdated = 5032,

    [OpenApiEnum("Documents forcesave")]
    DocumentsForcesave = 5049,

    [OpenApiEnum("Documents store forcesave")]
    DocumentsStoreForcesave = 5048,

    [OpenApiEnum("Documents uploading formats settings updated")]
    DocumentsUploadingFormatsSettingsUpdated = 5033,

    [OpenApiEnum("Documents external share settings updated")]
    DocumentsExternalShareSettingsUpdated = 5069,

    [OpenApiEnum("Documents keep new file name settings updated")]
    DocumentsKeepNewFileNameSettingsUpdated = 5083,

    [OpenApiEnum("Documents display file extension updated")]
    DocumentsDisplayFileExtensionUpdated = 5101,

    [OpenApiEnum("File converted")]
    FileConverted = 5035,

    [OpenApiEnum("File change owner")]
    FileChangeOwner = 5043,

    [OpenApiEnum("Document sign complete")]
    DocumentSignComplete = 5046,

    [OpenApiEnum("Document send to sign")]
    DocumentSendToSign = 5045,

    [OpenApiEnum("File marked as favorite")]
    FileMarkedAsFavorite = 5055,

    [OpenApiEnum("File removed from favorite")]
    FileRemovedFromFavorite = 5056,

    [OpenApiEnum("File marked as read")]
    FileMarkedAsRead = 5063,

    [OpenApiEnum("File readed")]
    FileReaded = 5064,

    [OpenApiEnum("Trash emptied")]
    TrashEmptied = 5061,

    [OpenApiEnum("Folder marked as read")]
    FolderMarkedAsRead = 5065,

    [OpenApiEnum("Room created")]
    RoomCreated = 5070,

    [OpenApiEnum("Room renamed")]
    RoomRenamed = 5071,

    [OpenApiEnum("Room archived")]
    RoomArchived = 5072,

    [OpenApiEnum("Room unarchived")]
    RoomUnarchived = 5073,

    [OpenApiEnum("Room deleted")]
    RoomDeleted = 5074,

    [OpenApiEnum("Room copied")]
    RoomCopied = 5100,

    [OpenApiEnum("Room update access for user")]
    RoomUpdateAccessForUser = 5075,

    [OpenApiEnum("Room remove user")]
    RoomRemoveUser = 5084,

    [OpenApiEnum("Room create user")]
    RoomCreateUser = 5085,

    [OpenApiEnum("Room invitation link updated")]
    RoomInvitationLinkUpdated = 5082,

    [OpenApiEnum("Room invitation link created")]
    RoomInvitationLinkCreated = 5086,

    [OpenApiEnum("Room invitation link deleted")]
    RoomInvitationLinkDeleted = 5087,

    [OpenApiEnum("Room group added")]
    RoomGroupAdded = 5094,

    [OpenApiEnum("Room update access for group")]
    RoomUpdateAccessForGroup = 5095,

    [OpenApiEnum("Room group remove")]
    RoomGroupRemove = 5096,

    [OpenApiEnum("Tag created")]
    TagCreated = 5076,

    [OpenApiEnum("Tags deleted")]
    TagsDeleted = 5077,

    [OpenApiEnum("Added room tags")]
    AddedRoomTags = 5078,

    [OpenApiEnum("Deleted room tags")]
    DeletedRoomTags = 5079,

    [OpenApiEnum("Room logo created")]
    RoomLogoCreated = 5080,

    [OpenApiEnum("Room logo deleted")]
    RoomLogoDeleted = 5081,

    [OpenApiEnum("Room color changed")]
    RoomColorChanged = 5102,

    [OpenApiEnum("Room cover changed")]
    RoomCoverChanged = 5103,

    [OpenApiEnum("Room indexing changed")]
    RoomIndexingChanged = 5104,

    [OpenApiEnum("Room deny download changed")]
    RoomDenyDownloadChanged = 5105,

    [OpenApiEnum("Room external link created")]
    RoomExternalLinkCreated = 5088,

    [OpenApiEnum("Room external link updated")]
    RoomExternalLinkUpdated = 5089,

    [OpenApiEnum("Room external link deleted")]
    RoomExternalLinkDeleted = 5090,

    [OpenApiEnum("Room external link revoked")]
    RoomExternalLinkRevoked = 5097,

    [OpenApiEnum("Room external link renamed")]
    RoomExternalLinkRenamed = 5098,

    [OpenApiEnum("File external link created")]
    FileExternalLinkCreated = 5091,

    [OpenApiEnum("File external link updated")]
    FileExternalLinkUpdated = 5092,

    [OpenApiEnum("File external link deleted")]
    FileExternalLinkDeleted = 5093,

    [OpenApiEnum("Room index export saved")]
    RoomIndexingEnabled = 5114,

    [OpenApiEnum("Room indexing disabled")]
    RoomIndexingDisabled = 5115,

    [OpenApiEnum("Room life time set")]
    RoomLifeTimeSet = 5116,

    [OpenApiEnum("Room life time disabled")]
    RoomLifeTimeDisabled = 5117,

    [OpenApiEnum("Room deny download enabled")]
    RoomDenyDownloadEnabled = 5109,

    [OpenApiEnum("Room deny download disabled")]
    RoomDenyDownloadDisabled = 5110,

    [OpenApiEnum("Room watermark set")]
    RoomWatermarkSet = 5112,

    [OpenApiEnum("Room watermark disabled")]
    RoomWatermarkDisabled = 5113,

    [OpenApiEnum("Room invite resend")]
    RoomInviteResend = 5118, 

    [OpenApiEnum("Room index export saved")]
    RoomIndexExportSaved = 5106,

    #endregion

    #region Settings

    [OpenApiEnum("Language settings updated")]
    LanguageSettingsUpdated = 6000,

    [OpenApiEnum("Time zone settings updated")]
    TimeZoneSettingsUpdated = 6001,

    [OpenApiEnum("Dns settings updated")]
    DnsSettingsUpdated = 6002,

    [OpenApiEnum("Trusted mail domain settings updated")]
    TrustedMailDomainSettingsUpdated = 6003,

    [OpenApiEnum("Password strength settings updated")]
    PasswordStrengthSettingsUpdated = 6004,

    [OpenApiEnum("Two factor authentication settings updated")]
    TwoFactorAuthenticationSettingsUpdated = 6005, // deprecated - use 6036-6038 instead

    [OpenApiEnum("Administrator message settings updated")]
    AdministratorMessageSettingsUpdated = 6006,

    [OpenApiEnum("Default start page settings updated")]
    DefaultStartPageSettingsUpdated = 6007,

    [OpenApiEnum("Products list updated")]
    ProductsListUpdated = 6008,

    [OpenApiEnum("Administrator added")]
    AdministratorAdded = 6009,

    [OpenApiEnum("Administrator opened full access")]
    AdministratorOpenedFullAccess = 6010,

    [OpenApiEnum("Administrator deleted")]
    AdministratorDeleted = 6011,

    [OpenApiEnum("Users opened product access")]
    UsersOpenedProductAccess = 6012,

    [OpenApiEnum("Groups opened product access")]
    GroupsOpenedProductAccess = 6013,

    [OpenApiEnum("Product access opened")]
    ProductAccessOpened = 6014,

    [OpenApiEnum("Product access restricted")]
    ProductAccessRestricted = 6015, // not used

    [OpenApiEnum("Product added administrator")]
    ProductAddedAdministrator = 6016,

    [OpenApiEnum("Product deleted administrator")]
    ProductDeletedAdministrator = 6017,

    [OpenApiEnum("Greeting settings updated")]
    GreetingSettingsUpdated = 6018,

    [OpenApiEnum("Team template changed")]
    TeamTemplateChanged = 6019,

    [OpenApiEnum("Color theme changed")]
    ColorThemeChanged = 6020,

    [OpenApiEnum("Owner sent change owner instructions")]
    OwnerSentChangeOwnerInstructions = 6021,

    [OpenApiEnum("Owner updated")]
    OwnerUpdated = 6022,

    [OpenApiEnum("Owner sent portal deactivation instructions")]
    OwnerSentPortalDeactivationInstructions = 6023,

    [OpenApiEnum("Owner sent portal delete instructions")]
    OwnerSentPortalDeleteInstructions = 6024,

    [OpenApiEnum("Portal deactivated")]
    PortalDeactivated = 6025,

    [OpenApiEnum("Portal deleted")]
    PortalDeleted = 6026,

    [OpenApiEnum("Login history report downloaded")]
    LoginHistoryReportDownloaded = 6027,

    [OpenApiEnum("Audit trail report downloaded")]
    AuditTrailReportDownloaded = 6028,

    [OpenApiEnum("SSO enabled")]
    SSOEnabled = 6029,

    [OpenApiEnum("SSO disabled")]
    SSODisabled = 6030,

    [OpenApiEnum("Portal access settings updated")]
    PortalAccessSettingsUpdated = 6031,

    [OpenApiEnum("Cookie settings updated")]
    CookieSettingsUpdated = 6032,

    [OpenApiEnum("Mail service settings updated")]
    MailServiceSettingsUpdated = 6033,

    [OpenApiEnum("Custom navigation settings updated")]
    CustomNavigationSettingsUpdated = 6034,

    [OpenApiEnum("Audit settings updated")]
    AuditSettingsUpdated = 6035,

    [OpenApiEnum("Two factor authentication disabled")]
    TwoFactorAuthenticationDisabled = 6036,

    [OpenApiEnum("Two factor authentication enabled by sms")]
    TwoFactorAuthenticationEnabledBySms = 6037,

    [OpenApiEnum("Two factor authentication enabled by tfa app")]
    TwoFactorAuthenticationEnabledByTfaApp = 6038,

    [OpenApiEnum("Portal renamed")]
    PortalRenamed = 6039,

    [OpenApiEnum("Quota per room changed")]
    QuotaPerRoomChanged = 6040,

    [OpenApiEnum("Quota per room disabled")]
    QuotaPerRoomDisabled = 6041,

    [OpenApiEnum("Quota per user changed")]
    QuotaPerUserChanged = 6042,

    [OpenApiEnum("Quota per user disabled")]
    QuotaPerUserDisabled = 6043,

    [OpenApiEnum("Quota per portal changed")]
    QuotaPerPortalChanged = 6044,

    [OpenApiEnum("Quota per portal disabled")]
    QuotaPerPortalDisabled = 6045,

    [OpenApiEnum("Custom quota per room default")]
    CustomQuotaPerRoomDefault = 6048,

    [OpenApiEnum("Custom quota per room changed")]
    CustomQuotaPerRoomChanged = 6049,

    [OpenApiEnum("Custom quota per room disabled")]
    CustomQuotaPerRoomDisabled = 6050,

    [OpenApiEnum("Custom quota per user default")]
    CustomQuotaPerUserDefault = 6051,

    [OpenApiEnum("Custom quota per user changed")]
    CustomQuotaPerUserChanged = 6052,

    [OpenApiEnum("Custom quota per user disabled")]
    CustomQuotaPerUserDisabled = 6053,

    [OpenApiEnum("Document service location setting")]
    DocumentServiceLocationSetting = 5037,

    [OpenApiEnum("Authorization keys setting")]
    AuthorizationKeysSetting = 5038,

    [OpenApiEnum("Full text search setting")]
    FullTextSearchSetting = 5039,

    [OpenApiEnum("Start transfer setting")]
    StartTransferSetting = 5040,

    [OpenApiEnum("Start backup setting")]
    StartBackupSetting = 5041,

    [OpenApiEnum("License key uploaded")]
    LicenseKeyUploaded = 5042,

    [OpenApiEnum("Start storage encryption")]
    StartStorageEncryption = 5050,

    [OpenApiEnum("Privacy room enable")]
    PrivacyRoomEnable = 5051,

    [OpenApiEnum("Privacy room disable")]
    PrivacyRoomDisable = 5052,

    [OpenApiEnum("Start storage decryption")]
    StartStorageDecryption = 5053,

    #endregion

    #region others
    [OpenApiEnum("Contact admin mail sent")]
    ContactAdminMailSent = 7000,

    [OpenApiEnum("Room invite link used")]
    RoomInviteLinkUsed = 7001,

    [OpenApiEnum("User created and added to room")]
    UserCreatedAndAddedToRoom = 7002,
    
    [OpenApiEnum("Guest created and added to room")]
    GuestCreatedAndAddedToRoom = 7003,
    
    [OpenApiEnum("Contact sales mail sent")]
    ContactSalesMailSent = 7004,

    #endregion
    
    #region Oauth
    
    [OpenApiEnum("Create client")]
    CreateClient = 9901,
    
    [OpenApiEnum("Update client")]
    UpdateClient = 9902,
    
    [OpenApiEnum("Regenerate secret")]
    RegenerateSecret = 9903,
    
    [OpenApiEnum("Delete client")]
    DeleteClient = 9904,
    
    [OpenApiEnum("Change client activation")]
    ChangeClientActivation = 9905,
    
    [OpenApiEnum("Change client visibility")]
    ChangeClientVisibility = 9906,
    
    [OpenApiEnum("Revoke user client")]
    RevokeUserClient = 9907,
    
    [OpenApiEnum("Generate authorization code token")]
    GenerateAuthorizationCodeToken = 9908,
    
    [OpenApiEnum("Generate personal access token")]
    GeneratePersonalAccessToken = 9909,
    
    #endregion
}
