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
    [SwaggerEnum("None")]
    None = -1,

    #region Login
    [SwaggerEnum("Login success")]
    LoginSuccess = 1000,

    [SwaggerEnum("Login success via social account")]
    LoginSuccessViaSocialAccount = 1001,

    [SwaggerEnum("Login success via sms")]
    LoginSuccessViaSms = 1007,

    [SwaggerEnum("Login success via api")]
    LoginSuccessViaApi = 1010,

    [SwaggerEnum("Login success via social app")]
    LoginSuccessViaSocialApp = 1011,

    [SwaggerEnum("Login success via api sms")]
    LoginSuccessViaApiSms = 1012,

    [SwaggerEnum("Login success via api tfa")]
    LoginSuccessViaApiTfa = 1024,

    [SwaggerEnum("Login success via api social account")]
    LoginSuccessViaApiSocialAccount = 1019,

    [SwaggerEnum("Login success via SSO")]
    LoginSuccessViaSSO = 1015,

    [SwaggerEnum("Login succes via tfa app")]
    LoginSuccesViaTfaApp = 1021,

    [SwaggerEnum("Login fail via SSO")]
    LoginFailViaSSO = 1018,

    [SwaggerEnum("Login fail invalid combination")]
    LoginFailInvalidCombination = 1002,

    [SwaggerEnum("Login fail social account not found")]
    LoginFailSocialAccountNotFound = 1003,

    [SwaggerEnum("Login fail disabled profile")]
    LoginFailDisabledProfile = 1004,

    [SwaggerEnum("Login fail")]
    LoginFail = 1005,

    [SwaggerEnum("Login fail via sms")]
    LoginFailViaSms = 1008,

    [SwaggerEnum("Login fail via api")]
    LoginFailViaApi = 1013,

    [SwaggerEnum("Login fail via api sms")]
    LoginFailViaApiSms = 1014,

    [SwaggerEnum("Login fail via api tfa")]
    LoginFailViaApiTfa = 1025,

    [SwaggerEnum("Login fail via api social account")]
    LoginFailViaApiSocialAccount = 1020,

    [SwaggerEnum("Login fail via Tfa app")]
    LoginFailViaTfaApp = 1022,

    [SwaggerEnum("Login fail ip security")]
    LoginFailIpSecurity = 1009,

    [SwaggerEnum("Login fail brute force")]
    LoginFailBruteForce = 1023,

    [SwaggerEnum("Login fail recaptcha")]
    LoginFailRecaptcha = 1026,  // last login

    [SwaggerEnum("Logout")]
    Logout = 1006,

    [SwaggerEnum("Session started")]
    SessionStarted = 1016,

    [SwaggerEnum("Session completed")]
    SessionCompleted = 1017,

    #endregion

    #region People

    [SwaggerEnum("User created")]
    UserCreated = 4000,

    [SwaggerEnum("Guest created")]
    GuestCreated = 4001,

    [SwaggerEnum("User created via invite")]
    UserCreatedViaInvite = 4002,

    [SwaggerEnum("Guest created via invite")]
    GuestCreatedViaInvite = 4003,

    [SwaggerEnum("Send join invite")]
    SendJoinInvite = 4037,

    [SwaggerEnum("User activated")]
    UserActivated = 4004,

    [SwaggerEnum("Guest activated")]
    GuestActivated = 4005,

    [SwaggerEnum("User updated")]
    UserUpdated = 4006,

    [SwaggerEnum("User updated mobile number")]
    UserUpdatedMobileNumber = 4029,

    [SwaggerEnum("User updated language")]
    UserUpdatedLanguage = 4007,

    [SwaggerEnum("User added avatar")]
    UserAddedAvatar = 4008,

    [SwaggerEnum("User deleted avatar")]
    UserDeletedAvatar = 4009,

    [SwaggerEnum("User updated avatar thumbnails")]
    UserUpdatedAvatarThumbnails = 4010,

    [SwaggerEnum("User linked social account")]
    UserLinkedSocialAccount = 4011,

    [SwaggerEnum("User unlinked social account")]
    UserUnlinkedSocialAccount = 4012,

    [SwaggerEnum("User connected tfa app")]
    UserConnectedTfaApp = 4032,

    [SwaggerEnum("User disconnected tfa app")]
    UserDisconnectedTfaApp = 4033,

    [SwaggerEnum("User sent activation instructions")]
    UserSentActivationInstructions = 4013,

    [SwaggerEnum("User sent email change instructions")]
    UserSentEmailChangeInstructions = 4014,

    [SwaggerEnum("User sent password change instructions")]
    UserSentPasswordChangeInstructions = 4015,

    [SwaggerEnum("User sent delete instructions")]
    UserSentDeleteInstructions = 4016,

    [SwaggerEnum("User updated email")]
    UserUpdatedEmail = 5047,

    [SwaggerEnum("User updated password")]
    UserUpdatedPassword = 4017,

    [SwaggerEnum("User reset password")]
    UserResetPassword = 4038,

    [SwaggerEnum("User deleted")]
    UserDeleted = 4018,

    [SwaggerEnum("Users updated type")]
    UsersUpdatedType = 4019,

    [SwaggerEnum("Users updated status")]
    UsersUpdatedStatus = 4020,

    [SwaggerEnum("Users sent activation instructions")]
    UsersSentActivationInstructions = 4021,

    [SwaggerEnum("Users deleted")]
    UsersDeleted = 4022,

    [SwaggerEnum("Sent invite instructions")]
    SentInviteInstructions = 4023,

    [SwaggerEnum("User imported")]
    UserImported = 4024,

    [SwaggerEnum("Guest imported")]
    GuestImported = 4025,

    [SwaggerEnum("Group created")]
    GroupCreated = 4026,

    [SwaggerEnum("Group updated")]
    GroupUpdated = 4027,

    [SwaggerEnum("Group deleted")]
    GroupDeleted = 4028,

    [SwaggerEnum("User data reassigns")]
    UserDataReassigns = 4030,

    [SwaggerEnum("User data removing")]
    UserDataRemoving = 4031,

    [SwaggerEnum("User logout active connections")]
    UserLogoutActiveConnections = 4034,

    [SwaggerEnum("User logout active connection")]
    UserLogoutActiveConnection = 4035,

    [SwaggerEnum("User logout active connections for user")]
    UserLogoutActiveConnectionsForUser = 4036,

    #endregion

    #region Documents

    [SwaggerEnum("File created")]
    FileCreated = 5000,

    [SwaggerEnum("File renamed")]
    FileRenamed = 5001,

    [SwaggerEnum("File updated")]
    FileUpdated = 5002,

    [SwaggerEnum("User file updated")]
    UserFileUpdated = 5034,

    [SwaggerEnum("File created version")]
    FileCreatedVersion = 5003,

    [SwaggerEnum("File deleted version")]
    FileDeletedVersion = 5004,

    [SwaggerEnum("File restore version")]
    FileRestoreVersion = 5044,

    [SwaggerEnum("File updated revision comment")]
    FileUpdatedRevisionComment = 5005,

    [SwaggerEnum("File locked")]
    FileLocked = 5006,

    [SwaggerEnum("File unlocked")]
    FileUnlocked = 5007,

    [SwaggerEnum("File updated access")]
    FileUpdatedAccess = 5008,

    [SwaggerEnum("File updated access for")]
    FileUpdatedAccessFor = 5068,

    [SwaggerEnum("File send access link")]
    FileSendAccessLink = 5036, // not used

    [SwaggerEnum("File opened for change")]
    FileOpenedForChange = 5054,

    [SwaggerEnum("File removed from list")]
    FileRemovedFromList = 5058,

    [SwaggerEnum("File external link access updated")]
    FileExternalLinkAccessUpdated = 5060,

    [SwaggerEnum("File downloaded")]
    FileDownloaded = 5009,

    [SwaggerEnum("File downloaded as")]
    FileDownloadedAs = 5010,

    [SwaggerEnum("File revision downloaded")]
    FileRevisionDownloaded = 5062,

    [SwaggerEnum("File uploaded")]
    FileUploaded = 5011,

    [SwaggerEnum("File imported")]
    FileImported = 5012,

    [SwaggerEnum("File uploaded with overwriting")]
    FileUploadedWithOverwriting = 5099,

    [SwaggerEnum("File copied")]
    FileCopied = 5013,

    [SwaggerEnum("File copied with overwriting")]
    FileCopiedWithOverwriting = 5014,

    [SwaggerEnum("File moved")]
    FileMoved = 5015,

    [SwaggerEnum("File moved with overwriting")]
    FileMovedWithOverwriting = 5016,

    [SwaggerEnum("File moved to trash")]
    FileMovedToTrash = 5017,

    [SwaggerEnum("File deleted")]
    FileDeleted = 5018,

    [SwaggerEnum("File index changed")]
    FileIndexChanged = 5111,

    [SwaggerEnum("Folder created")]
    FolderCreated = 5019,

    [SwaggerEnum("Folder renamed")]
    FolderRenamed = 5020,

    [SwaggerEnum("Folder updated access")]
    FolderUpdatedAccess = 5021,

    [SwaggerEnum("Folder updated access for")]
    FolderUpdatedAccessFor = 5066,

    [SwaggerEnum("Folder copied")]
    FolderCopied = 5022,

    [SwaggerEnum("Folder copied with overwriting")]
    FolderCopiedWithOverwriting = 5023,

    [SwaggerEnum("Folder moved")]
    FolderMoved = 5024,

    [SwaggerEnum("Folder moved with overwriting")]
    FolderMovedWithOverwriting = 5025,

    [SwaggerEnum("Folder moved to trash")]
    FolderMovedToTrash = 5026,

    [SwaggerEnum("Folder deleted")]
    FolderDeleted = 5027,

    [SwaggerEnum("Folder removed from list")]
    FolderRemovedFromList = 5059,

    [SwaggerEnum("Folder index changed")]
    FolderIndexChanged = 5107,

    [SwaggerEnum("Folder index reordered")]
    FolderIndexReordered = 5108,

    [SwaggerEnum("Folder downloaded")]
    FolderDownloaded = 5057,

    [SwaggerEnum("Form submit")]
    FormSubmit = 6046,

    [SwaggerEnum("Form opened for filling")]
    FormOpenedForFilling = 6047,

    [SwaggerEnum("ThirdParty created")]
    ThirdPartyCreated = 5028,

    [SwaggerEnum("ThirdParty updated")]
    ThirdPartyUpdated = 5029,

    [SwaggerEnum("ThirdParty deleted")]
    ThirdPartyDeleted = 5030,

    [SwaggerEnum("Documents ThirdParty settings updated")]
    DocumentsThirdPartySettingsUpdated = 5031,

    [SwaggerEnum("Documents overwriting settings updated")]
    DocumentsOverwritingSettingsUpdated = 5032,

    [SwaggerEnum("Documents forcesave")]
    DocumentsForcesave = 5049,

    [SwaggerEnum("Documents store forcesave")]
    DocumentsStoreForcesave = 5048,

    [SwaggerEnum("Documents uploading formats settings updated")]
    DocumentsUploadingFormatsSettingsUpdated = 5033,

    [SwaggerEnum("Documents external share settings updated")]
    DocumentsExternalShareSettingsUpdated = 5069,

    [SwaggerEnum("Documents keep new file name settings updated")]
    DocumentsKeepNewFileNameSettingsUpdated = 5083,

    [SwaggerEnum("Documents display file extension updated")]
    DocumentsDisplayFileExtensionUpdated = 5101,

    [SwaggerEnum("File converted")]
    FileConverted = 5035,

    [SwaggerEnum("File change owner")]
    FileChangeOwner = 5043,

    [SwaggerEnum("Document sign complete")]
    DocumentSignComplete = 5046,

    [SwaggerEnum("Document send to sign")]
    DocumentSendToSign = 5045,

    [SwaggerEnum("File marked as favorite")]
    FileMarkedAsFavorite = 5055,

    [SwaggerEnum("File removed from favorite")]
    FileRemovedFromFavorite = 5056,

    [SwaggerEnum("File marked as read")]
    FileMarkedAsRead = 5063,

    [SwaggerEnum("File readed")]
    FileReaded = 5064,

    [SwaggerEnum("Trash emptied")]
    TrashEmptied = 5061,

    [SwaggerEnum("Folder marked as read")]
    FolderMarkedAsRead = 5065,

    [SwaggerEnum("Room created")]
    RoomCreated = 5070,

    [SwaggerEnum("Room renamed")]
    RoomRenamed = 5071,

    [SwaggerEnum("Room archived")]
    RoomArchived = 5072,

    [SwaggerEnum("Room unarchived")]
    RoomUnarchived = 5073,

    [SwaggerEnum("Room deleted")]
    RoomDeleted = 5074,

    [SwaggerEnum("Room copied")]
    RoomCopied = 5100,

    [SwaggerEnum("Room update access for user")]
    RoomUpdateAccessForUser = 5075,

    [SwaggerEnum("Room remove user")]
    RoomRemoveUser = 5084,

    [SwaggerEnum("Room create user")]
    RoomCreateUser = 5085,

    [SwaggerEnum("Room invitation link updated")]
    RoomInvitationLinkUpdated = 5082,

    [SwaggerEnum("Room invitation link created")]
    RoomInvitationLinkCreated = 5086,

    [SwaggerEnum("Room invitation link deleted")]
    RoomInvitationLinkDeleted = 5087,

    [SwaggerEnum("Room group added")]
    RoomGroupAdded = 5094,

    [SwaggerEnum("Room update access for group")]
    RoomUpdateAccessForGroup = 5095,

    [SwaggerEnum("Room group remove")]
    RoomGroupRemove = 5096,

    [SwaggerEnum("Tag created")]
    TagCreated = 5076,

    [SwaggerEnum("Tags deleted")]
    TagsDeleted = 5077,

    [SwaggerEnum("Added room tags")]
    AddedRoomTags = 5078,

    [SwaggerEnum("Deleted room tags")]
    DeletedRoomTags = 5079,

    [SwaggerEnum("Room logo created")]
    RoomLogoCreated = 5080,

    [SwaggerEnum("Room logo deleted")]
    RoomLogoDeleted = 5081,

    [SwaggerEnum("Room color changed")]
    RoomColorChanged = 5102,

    [SwaggerEnum("Room cover changed")]
    RoomCoverChanged = 5103,

    [SwaggerEnum("Room indexing changed")]
    RoomIndexingChanged = 5104,

    [SwaggerEnum("Room deny download changed")]
    RoomDenyDownloadChanged = 5105,

    [SwaggerEnum("Room external link created")]
    RoomExternalLinkCreated = 5088,

    [SwaggerEnum("Room external link updated")]
    RoomExternalLinkUpdated = 5089,

    [SwaggerEnum("Room external link deleted")]
    RoomExternalLinkDeleted = 5090,

    [SwaggerEnum("Room external link revoked")]
    RoomExternalLinkRevoked = 5097,

    [SwaggerEnum("Room external link renamed")]
    RoomExternalLinkRenamed = 5098,

    [SwaggerEnum("File external link created")]
    FileExternalLinkCreated = 5091,

    [SwaggerEnum("File external link updated")]
    FileExternalLinkUpdated = 5092,

    [SwaggerEnum("File external link deleted")]
    FileExternalLinkDeleted = 5093,

    [SwaggerEnum("Room index export saved")]
    RoomIndexingEnabled = 5114,

    [SwaggerEnum("Room indexing disabled")]
    RoomIndexingDisabled = 5115,

    [SwaggerEnum("Room life time set")]
    RoomLifeTimeSet = 5116,

    [SwaggerEnum("Room life time disabled")]
    RoomLifeTimeDisabled = 5117,

    [SwaggerEnum("Room deny download enabled")]
    RoomDenyDownloadEnabled = 5109,

    [SwaggerEnum("Room deny download disabled")]
    RoomDenyDownloadDisabled = 5110,

    [SwaggerEnum("Room watermark set")]
    RoomWatermarkSet = 5112,

    [SwaggerEnum("Room watermark disabled")]
    RoomWatermarkDisabled = 5113,

    [SwaggerEnum("Room invite resend")]
    RoomInviteResend = 5118, 

    [SwaggerEnum("Room index export saved")]
    RoomIndexExportSaved = 5106,

    #endregion

    #region Settings

    [SwaggerEnum("Language settings updated")]
    LanguageSettingsUpdated = 6000,

    [SwaggerEnum("Time zone settings updated")]
    TimeZoneSettingsUpdated = 6001,

    [SwaggerEnum("Dns settings updated")]
    DnsSettingsUpdated = 6002,

    [SwaggerEnum("Trusted mail domain settings updated")]
    TrustedMailDomainSettingsUpdated = 6003,

    [SwaggerEnum("Password strength settings updated")]
    PasswordStrengthSettingsUpdated = 6004,

    [SwaggerEnum("Two factor authentication settings updated")]
    TwoFactorAuthenticationSettingsUpdated = 6005, // deprecated - use 6036-6038 instead

    [SwaggerEnum("Administrator message settings updated")]
    AdministratorMessageSettingsUpdated = 6006,

    [SwaggerEnum("Default start page settings updated")]
    DefaultStartPageSettingsUpdated = 6007,

    [SwaggerEnum("Products list updated")]
    ProductsListUpdated = 6008,

    [SwaggerEnum("Administrator added")]
    AdministratorAdded = 6009,

    [SwaggerEnum("Administrator opened full access")]
    AdministratorOpenedFullAccess = 6010,

    [SwaggerEnum("Administrator deleted")]
    AdministratorDeleted = 6011,

    [SwaggerEnum("Users opened product access")]
    UsersOpenedProductAccess = 6012,

    [SwaggerEnum("Groups opened product access")]
    GroupsOpenedProductAccess = 6013,

    [SwaggerEnum("Product access opened")]
    ProductAccessOpened = 6014,

    [SwaggerEnum("Product access restricted")]
    ProductAccessRestricted = 6015, // not used

    [SwaggerEnum("Product added administrator")]
    ProductAddedAdministrator = 6016,

    [SwaggerEnum("Product deleted administrator")]
    ProductDeletedAdministrator = 6017,

    [SwaggerEnum("Greeting settings updated")]
    GreetingSettingsUpdated = 6018,

    [SwaggerEnum("Team template changed")]
    TeamTemplateChanged = 6019,

    [SwaggerEnum("Color theme changed")]
    ColorThemeChanged = 6020,

    [SwaggerEnum("Owner sent change owner instructions")]
    OwnerSentChangeOwnerInstructions = 6021,

    [SwaggerEnum("Owner updated")]
    OwnerUpdated = 6022,

    [SwaggerEnum("Owner sent portal deactivation instructions")]
    OwnerSentPortalDeactivationInstructions = 6023,

    [SwaggerEnum("Owner sent portal delete instructions")]
    OwnerSentPortalDeleteInstructions = 6024,

    [SwaggerEnum("Portal deactivated")]
    PortalDeactivated = 6025,

    [SwaggerEnum("Portal deleted")]
    PortalDeleted = 6026,

    [SwaggerEnum("Login history report downloaded")]
    LoginHistoryReportDownloaded = 6027,

    [SwaggerEnum("Audit trail report downloaded")]
    AuditTrailReportDownloaded = 6028,

    [SwaggerEnum("SSO enabled")]
    SSOEnabled = 6029,

    [SwaggerEnum("SSO disabled")]
    SSODisabled = 6030,

    [SwaggerEnum("Portal access settings updated")]
    PortalAccessSettingsUpdated = 6031,

    [SwaggerEnum("Cookie settings updated")]
    CookieSettingsUpdated = 6032,

    [SwaggerEnum("Mail service settings updated")]
    MailServiceSettingsUpdated = 6033,

    [SwaggerEnum("Custom navigation settings updated")]
    CustomNavigationSettingsUpdated = 6034,

    [SwaggerEnum("Audit settings updated")]
    AuditSettingsUpdated = 6035,

    [SwaggerEnum("Two factor authentication disabled")]
    TwoFactorAuthenticationDisabled = 6036,

    [SwaggerEnum("Two factor authentication enabled by sms")]
    TwoFactorAuthenticationEnabledBySms = 6037,

    [SwaggerEnum("Two factor authentication enabled by tfa app")]
    TwoFactorAuthenticationEnabledByTfaApp = 6038,

    [SwaggerEnum("Portal renamed")]
    PortalRenamed = 6039,

    [SwaggerEnum("Quota per room changed")]
    QuotaPerRoomChanged = 6040,

    [SwaggerEnum("Quota per room disabled")]
    QuotaPerRoomDisabled = 6041,

    [SwaggerEnum("Quota per user changed")]
    QuotaPerUserChanged = 6042,

    [SwaggerEnum("Quota per user disabled")]
    QuotaPerUserDisabled = 6043,

    [SwaggerEnum("Quota per portal changed")]
    QuotaPerPortalChanged = 6044,

    [SwaggerEnum("Quota per portal disabled")]
    QuotaPerPortalDisabled = 6045,

    [SwaggerEnum("Custom quota per room default")]
    CustomQuotaPerRoomDefault = 6048,

    [SwaggerEnum("Custom quota per room changed")]
    CustomQuotaPerRoomChanged = 6049,

    [SwaggerEnum("Custom quota per room disabled")]
    CustomQuotaPerRoomDisabled = 6050,

    [SwaggerEnum("Custom quota per user default")]
    CustomQuotaPerUserDefault = 6051,

    [SwaggerEnum("Custom quota per user changed")]
    CustomQuotaPerUserChanged = 6052,

    [SwaggerEnum("Custom quota per user disabled")]
    CustomQuotaPerUserDisabled = 6053,

    [SwaggerEnum("Document service location setting")]
    DocumentServiceLocationSetting = 5037,

    [SwaggerEnum("Authorization keys setting")]
    AuthorizationKeysSetting = 5038,

    [SwaggerEnum("Full text search setting")]
    FullTextSearchSetting = 5039,

    [SwaggerEnum("Start transfer setting")]
    StartTransferSetting = 5040,

    [SwaggerEnum("Start backup setting")]
    StartBackupSetting = 5041,

    [SwaggerEnum("License key uploaded")]
    LicenseKeyUploaded = 5042,

    [SwaggerEnum("Start storage encryption")]
    StartStorageEncryption = 5050,

    [SwaggerEnum("Privacy room enable")]
    PrivacyRoomEnable = 5051,

    [SwaggerEnum("Privacy room disable")]
    PrivacyRoomDisable = 5052,

    [SwaggerEnum("Start storage decryption")]
    StartStorageDecryption = 5053,

    #endregion

    #region others
    [SwaggerEnum("Contact admin mail sent")]
    ContactAdminMailSent = 7000,

    [SwaggerEnum("Room invite link used")]
    RoomInviteLinkUsed = 7001,

    [SwaggerEnum("User created and added to room")]
    UserCreatedAndAddedToRoom = 7002,
    
    [SwaggerEnum("Guest created and added to room")]
    GuestCreatedAndAddedToRoom = 7003,
    
    [SwaggerEnum("Contact sales mail sent")]
    ContactSalesMailSent = 7004,

    #endregion
    
    #region Oauth
    
    [SwaggerEnum("Create client")]
    CreateClient = 9901,
    
    [SwaggerEnum("Update client")]
    UpdateClient = 9902,
    
    [SwaggerEnum("Regenerate secret")]
    RegenerateSecret = 9903,
    
    [SwaggerEnum("Delete client")]
    DeleteClient = 9904,
    
    [SwaggerEnum("Change client activation")]
    ChangeClientActivation = 9905,
    
    [SwaggerEnum("Change client visibility")]
    ChangeClientVisibility = 9906,
    
    [SwaggerEnum("Revoke user client")]
    RevokeUserClient = 9907,
    
    [SwaggerEnum("Generate authorization code token")]
    GenerateAuthorizationCodeToken = 9908,
    
    [SwaggerEnum("Generate personal access token")]
    GeneratePersonalAccessToken = 9909,
    
    #endregion
}
