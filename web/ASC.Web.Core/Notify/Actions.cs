// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Core.Common.Notify.Model;

namespace ASC.Web.Studio.Core.Notify;

[Singleton]
public class Actions : INotifyActionList
{
    public readonly INotifyAction AdminNotify;
    public readonly INotifyAction PeriodicNotify;

    public readonly INotifyAction SelfProfileUpdated;
    public readonly INotifyAction UserHasJoin;

    public readonly INotifyAction UserMessageToAdmin;

    public readonly INotifyAction UserMessageToSales;

    public readonly INotifyAction RequestTariff;

    public readonly INotifyAction RequestLicense;

    public readonly INotifyAction YourProfileUpdated;

    public readonly INotifyAction JoinUsers;

    public readonly INotifyAction SendWhatsNew;

    public readonly INotifyAction BackupCreated;
    public readonly INotifyAction BackupFailed;
    public readonly INotifyAction ScheduledBackupFailed;
    public readonly INotifyAction RestoreStarted;
    public readonly INotifyAction RestoreCompletedV115;
    public readonly INotifyAction PortalDeactivate;
    public readonly INotifyAction PortalDelete;

    public readonly INotifyAction ProfileDelete;
    public readonly INotifyAction ProfileHasDeletedItself;
    public readonly INotifyAction ReassignsCompleted;
    public readonly INotifyAction ReassignsFailed;
    public readonly INotifyAction RemoveUserDataCompleted;
    public readonly INotifyAction RemoveUserDataCompletedCustomMode;
    public readonly INotifyAction RemoveUserDataFailed;

    public readonly INotifyAction ConfirmOwnerChange;
    public readonly INotifyAction ActivateEmail;
    public readonly INotifyAction EmailChangeV115;
    public readonly INotifyAction PasswordChangeV115;
    public readonly INotifyAction PasswordChanged;
    public readonly INotifyAction PasswordSet;
    public readonly INotifyAction PhoneChange;
    public readonly INotifyAction TfaChange;
    public readonly INotifyAction MigrationPortalStart;
    public readonly INotifyAction MigrationPortalSuccessV115;
    public readonly INotifyAction MigrationPortalError;
    public readonly INotifyAction MigrationPortalServerFailure;
    public readonly INotifyAction PortalRename;

    public readonly INotifyAction SaasGuestActivationV115;
    public readonly INotifyAction EnterpriseGuestActivationV10;
    public readonly INotifyAction EnterpriseWhitelabelGuestActivationV10;
    public readonly INotifyAction OpensourceGuestActivationV11;

    public readonly INotifyAction SaasGuestWelcomeV1;
    public readonly INotifyAction EnterpriseGuestWelcomeV1;
    public readonly INotifyAction EnterpriseWhitelabelGuestWelcomeV1;
    public readonly INotifyAction OpensourceGuestWelcomeV1;

    public readonly INotifyAction SaasCustomModeRegData;

    public readonly INotifyAction StorageEncryptionStart;
    public readonly INotifyAction StorageEncryptionSuccess;
    public readonly INotifyAction StorageEncryptionError;
    public readonly INotifyAction StorageDecryptionStart;
    public readonly INotifyAction StorageDecryptionSuccess;
    public readonly INotifyAction StorageDecryptionError;

    public readonly INotifyAction SaasRoomInvite;
    public readonly INotifyAction SaasAgentInvite;
    public readonly INotifyAction SaasRoomInviteExistingUser;
    public readonly INotifyAction SaasAgentInviteExistingUser;
    public readonly INotifyAction SaasDocSpaceInvite;
    public readonly INotifyAction SaasDocSpaceRegistration;

    public readonly INotifyAction SaasAdminActivationV1;
    public readonly INotifyAction EnterpriseAdminActivationV1;
    public readonly INotifyAction EnterpriseWhitelabelAdminActivationV1;
    public readonly INotifyAction OpensourceAdminActivationV1;

    public readonly INotifyAction SaasAdminWelcomeV1;
    public readonly INotifyAction EnterpriseAdminWelcomeV1;
    public readonly INotifyAction EnterpriseWhitelabelAdminWelcomeV1;
    public readonly INotifyAction OpensourceAdminWelcomeV1;

    public readonly INotifyAction DocsTips;

    public readonly INotifyAction SaasAdminTrialWarningAfterHalfYearV1;
    public readonly INotifyAction SaasAdminStartupWarningAfterYearV1;

    public readonly INotifyAction PortalDeleteSuccessV1;
    public readonly INotifyAction PortalDeletedToSupport;

    public readonly INotifyAction SaasUserWelcomeV1;
    public readonly INotifyAction EnterpriseUserWelcomeV1;
    public readonly INotifyAction EnterpriseWhitelabelUserWelcomeV1;
    public readonly INotifyAction EnterpriseWhitelabelUserWelcomeCustomModeV1;
    public readonly INotifyAction OpensourceUserWelcomeV1;

    public readonly INotifyAction SaasUserActivationV1;
    public readonly INotifyAction EnterpriseUserActivationV1;
    public readonly INotifyAction EnterpriseWhitelabelUserActivationV1;
    public readonly INotifyAction OpensourceUserActivationV1;

    public readonly INotifyAction SaasAdminModulesV1;

    public readonly INotifyAction SaasAdminUserAppsTipsV1;
    public readonly INotifyAction EnterpriseAdminUserAppsTipsV1;

    public readonly INotifyAction RoomsActivity;

    public readonly INotifyAction SaasOwnerPaymentWarningGracePeriodBeforeActivation;
    public readonly INotifyAction SaasOwnerPaymentWarningGracePeriodActivation;
    public readonly INotifyAction SaasOwnerPaymentWarningGracePeriodLastDay;
    public readonly INotifyAction SaasOwnerPaymentWarningGracePeriodExpired;

    public readonly INotifyAction SaasAdminVideoGuides;
    public readonly INotifyAction SaasAdminIntegrations;

    public readonly INotifyAction ZoomWelcome;

    public readonly INotifyAction MigrationPersonalToDocspace;

    public readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodBeforeActivation;
    public readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodActivation;
    public readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodBeforeExpiration;
    public readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodExpiration;

    public readonly INotifyAction EnterpriseAdminPaymentWarningLifetimeBeforeExpiration;
    public readonly INotifyAction EnterpriseAdminPaymentWarningLifetimeExpiration;

    public readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodBeforeActivation;
    public readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodActivation;
    public readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodBeforeExpiration;
    public readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodExpiration;

    public readonly INotifyAction UserTypeChanged;
    public readonly INotifyAction UserRoleChanged;
    public readonly INotifyAction UserAgentRoleChanged;

    public readonly INotifyAction TopUpWalletError;
    public readonly INotifyAction RenewSubscriptionError;

    public readonly INotifyAction ApiKeyExpired;

    public Actions()
    {
        AdminNotify = new NotifyAction("admin_notify", this)
        {
            Patterns =
            [
                //new EmailPattern("admin_notify", () => WebstudioNotifyPatternResource.subject_admin_notify, () => WebstudioNotifyPatternResource.pattern_admin_notify)
            ]
        };
        PeriodicNotify = new NotifyAction("periodic_notify", this)
        {
            Patterns =
            [
                //new EmailPattern("periodic_notify", () => WebstudioNotifyPatternResource.subject_periodic_notify, () => WebstudioNotifyPatternResource.pattern_periodic_notify)
            ]
        };

        SelfProfileUpdated = new NotifyAction("self_profile_updated", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_self_profile_updated, () => WebstudioNotifyPatternResource.pattern_self_profile_updated),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_self_profile_updated_tg)
            ]
        };
        UserHasJoin = new NotifyAction("user_has_join", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_has_join, () => WebstudioNotifyPatternResource.pattern_user_has_join),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_user_has_join_tg)
            ]
        };


        UserMessageToAdmin = new NotifyAction("for_admin_notify", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_admin_notify, () => WebstudioNotifyPatternResource.pattern_for_admin_notify),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_for_admin_notify_tg)
            ]
        };


        UserMessageToSales = new NotifyAction("for_sales_notify", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_sales_notify, () => WebstudioNotifyPatternResource.pattern_for_sales_notify)
            ]
        };

        RequestTariff = new NotifyAction("request_tariff", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_request_tariff, () => WebstudioNotifyPatternResource.pattern_request_tariff1)
            ]
        };

        RequestLicense = new NotifyAction("request_license", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_request_license, () => WebstudioNotifyPatternResource.pattern_request_license)
            ]
        };

        YourProfileUpdated = new NotifyAction("profile_updated", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_profile_updated, () => WebstudioNotifyPatternResource.pattern_profile_updated),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_profile_updated_tg)
            ]
        };

        JoinUsers = new NotifyAction("join", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_join, () => WebstudioNotifyPatternResource.pattern_join),
                new JabberPattern(() => WebstudioNotifyPatternResource.pattern_join)
            ]
        };

        SendWhatsNew = new NotifyAction("send_whats_new", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_send_whats_new, () => WebstudioNotifyPatternResource.pattern_send_whats_new),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_send_whats_new)
            ]
        };

        BackupCreated = new NotifyAction("backup_created", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_backup_created, () => WebstudioNotifyPatternResource.pattern_backup_created),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_backup_created_tg)
            ]
        };
        BackupFailed = new NotifyAction("backup_failed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_backup_failed, () => WebstudioNotifyPatternResource.pattern_backup_failed),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_backup_failed_tg)
            ]
        };
        ScheduledBackupFailed = new NotifyAction("scheduled_backup_failed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_scheduled_backup_failed, () => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed_tg)
            ]
        };
        RestoreStarted = new NotifyAction("restore_started", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_restore_started, () => WebstudioNotifyPatternResource.pattern_restore_started),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_restore_started)
            ]
        };
        RestoreCompletedV115 = new NotifyAction("restore_completed_v115", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_restore_completed, () => WebstudioNotifyPatternResource.pattern_restore_completed_v115),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_restore_completed_v115)
            ]
        };
        PortalDeactivate = new NotifyAction("portal_deactivate", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_deactivate, () => WebstudioNotifyPatternResource.pattern_portal_deactivate),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_portal_deactivate_tg)
            ]
        };
        PortalDelete = new NotifyAction("portal_delete", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_delete, () => WebstudioNotifyPatternResource.pattern_portal_delete),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_portal_delete_tg)
            ]
        };

        ProfileDelete = new NotifyAction("profile_delete", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_profile_delete, () => WebstudioNotifyPatternResource.pattern_profile_delete)
            ]
        };

        ProfileHasDeletedItself = new NotifyAction("profile_has_deleted_itself", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_profile_has_deleted_itself, () => WebstudioNotifyPatternResource.pattern_profile_has_deleted_itself)
            ]
        };

        ReassignsCompleted = new NotifyAction("reassigns_completed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_reassigns_completed, () => WebstudioNotifyPatternResource.pattern_reassigns_completed)
            ]
        };

        ReassignsFailed = new NotifyAction("reassigns_failed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_reassigns_failed, () => WebstudioNotifyPatternResource.pattern_reassigns_failed)
            ]
        };

        RemoveUserDataCompleted = new NotifyAction("remove_user_data_completed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_remove_user_data_completed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_completed)
            ]
        };

        RemoveUserDataCompletedCustomMode = new NotifyAction("remove_user_data_completed_custom_mode", this)
        {
            Patterns =
            [
                new EmailPattern(() => CustomModeResource.subject_remove_user_data_completed_custom_mode, () => CustomModeResource.pattern_remove_user_data_completed_custom_mode)
            ]
        };

        RemoveUserDataFailed = new NotifyAction("remove_user_data_failed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_remove_user_data_failed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_failed)
            ]
        };

        ConfirmOwnerChange = new NotifyAction("owner_confirm_change", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_confirm_owner_change, () => WebstudioNotifyPatternResource.pattern_confirm_owner_change)
            ]
        };

        ActivateEmail = new NotifyAction("activate_email", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_activate_email, () => WebstudioNotifyPatternResource.pattern_activate_email)
            ]
        };

        EmailChangeV115 = new NotifyAction("change_email_v115", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_email_v115, () => WebstudioNotifyPatternResource.pattern_change_email_v115)
            ]
        };

        PasswordChangeV115 = new NotifyAction("change_password_v115", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_password_v115, () => WebstudioNotifyPatternResource.pattern_change_password_v115)
            ]
        };

        PasswordChanged = new NotifyAction("password_changed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_password_changed, () => WebstudioNotifyPatternResource.pattern_password_changed)
            ]
        };

        PasswordSet = new NotifyAction("set_password", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_set_password, () => WebstudioNotifyPatternResource.pattern_set_password)
            ]
        };

        PhoneChange = new NotifyAction("change_phone", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_phone, () => WebstudioNotifyPatternResource.pattern_change_phone)
            ]
        };

        TfaChange = new NotifyAction("change_tfa", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_tfa, () => WebstudioNotifyPatternResource.pattern_change_tfa)
            ]
        };

        MigrationPortalStart = new NotifyAction("migration_start", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_start, () => WebstudioNotifyPatternResource.pattern_migration_start)
            ]
        };

        MigrationPortalSuccessV115 = new NotifyAction("migration_success_v115", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_success, () => WebstudioNotifyPatternResource.pattern_migration_success_v115)
            ]
        };

        MigrationPortalError = new NotifyAction("migration_error", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_error)
            ]
        };

        MigrationPortalServerFailure = new NotifyAction("migration_server_failure", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_server_failure)
            ]
        };

        PortalRename = new NotifyAction("portal_rename", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_rename, () => WebstudioNotifyPatternResource.pattern_portal_rename)
            ]
        };

        SaasGuestActivationV115 = new NotifyAction("saas_guest_activation_v115", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_guest_activation_v115, () => WebstudioNotifyPatternResource.pattern_saas_guest_activation_v115)
            ]
        };

        EnterpriseGuestActivationV10 = new NotifyAction("enterprise_guest_activation_v10", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_activation_v10)
            ]
        };

        EnterpriseWhitelabelGuestActivationV10 = new NotifyAction("enterprise_whitelabel_guest_activation_v10", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_activation_v10)
            ]
        };

        OpensourceGuestActivationV11 = new NotifyAction("opensource_guest_activation_v11", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_guest_activation_v11, () => WebstudioNotifyPatternResource.pattern_opensource_guest_activation_v11)
            ]
        };

        SaasGuestWelcomeV1 = new NotifyAction("saas_guest_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_guest_welcome_v1)
            ]
        };

        EnterpriseGuestWelcomeV1 = new NotifyAction("enterprise_guest_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_welcome_v1)
            ]
        };

        EnterpriseWhitelabelGuestWelcomeV1 = new NotifyAction("enterprise_whitelabel_guest_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_welcome_v1)
            ]
        };

        OpensourceGuestWelcomeV1 = new NotifyAction("opensource_guest_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_guest_welcome_v1)
            ]
        };

        SaasCustomModeRegData = new NotifyAction("saas_custom_mode_reg_data", this)
        {
            Patterns =
            [
                new EmailPattern(() => CustomModeResource.subject_saas_custom_mode_reg_data, () => CustomModeResource.pattern_saas_custom_mode_reg_data)
            ]
        };

        StorageEncryptionStart = new NotifyAction("storage_encryption_start", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_start, () => WebstudioNotifyPatternResource.pattern_storage_encryption_start)
            ]
        };

        StorageEncryptionSuccess = new NotifyAction("storage_encryption_success", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_success, () => WebstudioNotifyPatternResource.pattern_storage_encryption_success)
            ]
        };

        StorageEncryptionError = new NotifyAction("storage_encryption_error", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_error, () => WebstudioNotifyPatternResource.pattern_storage_encryption_error)
            ]
        };

        StorageDecryptionStart = new NotifyAction("storage_decryption_start", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_start, () => WebstudioNotifyPatternResource.pattern_storage_decryption_start)
            ]
        };

        StorageDecryptionSuccess = new NotifyAction("storage_decryption_success", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_success, () => WebstudioNotifyPatternResource.pattern_storage_decryption_success)
            ]
        };

        StorageDecryptionError = new NotifyAction("storage_decryption_error", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_error, () => WebstudioNotifyPatternResource.pattern_storage_decryption_error)
            ]
        };

        SaasRoomInvite = new NotifyAction("saas_room_invite", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite, () => WebstudioNotifyPatternResource.pattern_saas_room_invite)
            ]
        };

        SaasAgentInvite = new NotifyAction("saas_agent_invite", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite)
            ]
        };

        SaasRoomInviteExistingUser = new NotifyAction("saas_room_invite_existing_user", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_room_invite_existing_user)
            ]
        };

        SaasAgentInviteExistingUser = new NotifyAction("saas_agent_invite_existing_user", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite_existing_user)
            ]
        };

        SaasDocSpaceInvite = new NotifyAction("saas_docspace_invite", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_invite, () => WebstudioNotifyPatternResource.pattern_saas_docspace_invite)
            ]
        };

        SaasDocSpaceRegistration = new NotifyAction("saas_docspace_registration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_registration, () => WebstudioNotifyPatternResource.pattern_saas_docspace_registration)
            ]
        };

        SaasAdminActivationV1 = new NotifyAction("saas_admin_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_activation_v1)
            ]
        };

        EnterpriseAdminActivationV1 = new NotifyAction("enterprise_admin_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_activation_v1)
            ]
        };

        EnterpriseWhitelabelAdminActivationV1 = new NotifyAction("enterprise_whitelabel_admin_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_activation_v1)
            ]
        };

        OpensourceAdminActivationV1 = new NotifyAction("opensource_admin_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_activation_v1)
            ]
        };

        SaasAdminWelcomeV1 = new NotifyAction("saas_admin_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_welcome_v1)
            ]
        };

        EnterpriseAdminWelcomeV1 = new NotifyAction("enterprise_admin_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_welcome_v1)
            ]
        };

        EnterpriseWhitelabelAdminWelcomeV1 = new NotifyAction("enterprise_whitelabel_admin_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_welcome_v1)
            ]
        };

        OpensourceAdminWelcomeV1 = new NotifyAction("opensource_admin_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_welcome_v1)
            ]
        };

        DocsTips = new NotifyAction("docs_tips", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_docs_tips, () => WebstudioNotifyPatternResource.pattern_docs_tips)
            ]
        };

        SaasAdminTrialWarningAfterHalfYearV1 = new NotifyAction("saas_admin_trial_warning_after_half_year_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_trial_warning_after_half_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_trial_warning_after_half_year_v1)
            ]
        };

        SaasAdminStartupWarningAfterYearV1 = new NotifyAction("saas_admin_startup_warning_after_year_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_startup_warning_after_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_startup_warning_after_year_v1)
            ]
        };

        PortalDeleteSuccessV1 = new NotifyAction("portal_delete_success_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_delete_success_v1, () => WebstudioNotifyPatternResource.pattern_portal_delete_success_v1)
            ]
        };

        PortalDeletedToSupport = new NotifyAction("portal_deleted_to_support", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_deleted_to_support, () => WebstudioNotifyPatternResource.pattern_portal_deleted_to_support)
            ]
        };

        SaasUserWelcomeV1 = new NotifyAction("saas_user_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v1)
            ]
        };

        EnterpriseUserWelcomeV1 = new NotifyAction("enterprise_user_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_welcome_v1)
            ]
        };

        EnterpriseWhitelabelUserWelcomeV1 = new NotifyAction("enterprise_whitelabel_user_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_welcome_v1)
            ]
        };

        EnterpriseWhitelabelUserWelcomeCustomModeV1 = new NotifyAction("enterprise_whitelabel_user_welcome_custom_mode_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v3)
            ]
        };

        OpensourceUserWelcomeV1 = new NotifyAction("opensource_user_welcome_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_welcome_v1)
            ]
        };

        SaasUserActivationV1 = new NotifyAction("saas_user_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_activation_v1)
            ]
        };

        EnterpriseUserActivationV1 = new NotifyAction("enterprise_user_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_activation_v1)
            ]
        };

        EnterpriseWhitelabelUserActivationV1 = new NotifyAction("enterprise_whitelabel_user_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_activation_v1)
            ]
        };

        OpensourceUserActivationV1 = new NotifyAction("opensource_user_activation_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_activation_v1)
            ]
        };

        SaasAdminModulesV1 = new NotifyAction("saas_admin_modules_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_modules_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_modules_v1)
            ]
        };

        SaasAdminUserAppsTipsV1 = new NotifyAction("saas_admin_user_apps_tips_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_user_apps_tips_v1)
            ]
        };

        EnterpriseAdminUserAppsTipsV1 = new NotifyAction("enterprise_admin_user_apps_tips_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_user_apps_tips_v1)
            ]
        };

        RoomsActivity = new NotifyAction("rooms_activity", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_rooms_activity, () => WebstudioNotifyPatternResource.pattern_rooms_activity),
                new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_rooms_activity)
            ]
        };

        SaasOwnerPaymentWarningGracePeriodBeforeActivation = new NotifyAction("saas_owner_payment_warning_grace_period_before_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_before_activation)
            ]
        };

        SaasOwnerPaymentWarningGracePeriodActivation = new NotifyAction("saas_owner_payment_warning_grace_period_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_activation)
            ]
        };

        SaasOwnerPaymentWarningGracePeriodLastDay = new NotifyAction("saas_owner_payment_warning_grace_period_last_day", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_last_day, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_last_day)
            ]
        };

        SaasOwnerPaymentWarningGracePeriodExpired = new NotifyAction("saas_owner_payment_warning_grace_period_expired", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_expired, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_expired)
            ]
        };

        SaasAdminVideoGuides = new NotifyAction("saas_video_guides_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_video_guides_v1, () => WebstudioNotifyPatternResource.pattern_saas_video_guides_v1)
            ]
        };

        SaasAdminIntegrations = new NotifyAction("saas_admin_integrations_v1", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_integrations_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_integrations_v1)
            ]
        };

        ZoomWelcome = new NotifyAction("zoom_welcome", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_zoom_welcome, () => WebstudioNotifyPatternResource.pattern_zoom_welcome)
            ]
        };

        MigrationPersonalToDocspace = new NotifyAction("migration_personal_to_docspace", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_personal_to_docspace, () => WebstudioNotifyPatternResource.pattern_migration_personal_to_docspace)
            ]
        };

        EnterpriseAdminPaymentWarningGracePeriodBeforeActivation = new NotifyAction("enterprise_admin_payment_warning_grace_period_before_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_activation)
            ]
        };

        EnterpriseAdminPaymentWarningGracePeriodActivation = new NotifyAction("enterprise_admin_payment_warning_grace_period_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_activation)
            ]
        };

        EnterpriseAdminPaymentWarningGracePeriodBeforeExpiration = new NotifyAction("enterprise_admin_payment_warning_grace_period_before_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_expiration)
            ]
        };

        EnterpriseAdminPaymentWarningGracePeriodExpiration = new NotifyAction("enterprise_admin_payment_warning_grace_period_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_expiration)
            ]
        };

        EnterpriseAdminPaymentWarningLifetimeBeforeExpiration = new NotifyAction("enterprise_admin_payment_warning_lifetime_before_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_before_expiration)
            ]
        };

        EnterpriseAdminPaymentWarningLifetimeExpiration = new NotifyAction("enterprise_admin_payment_warning_lifetime_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_expiration)
            ]
        };

        DeveloperAdminPaymentWarningGracePeriodBeforeActivation = new NotifyAction("developer_admin_payment_warning_grace_period_before_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_activation)
            ]
        };

        DeveloperAdminPaymentWarningGracePeriodActivation = new NotifyAction("developer_admin_payment_warning_grace_period_activation", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_activation)
            ]
        };

        DeveloperAdminPaymentWarningGracePeriodBeforeExpiration = new NotifyAction("developer_admin_payment_warning_grace_period_before_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_expiration)
            ]
        };

        DeveloperAdminPaymentWarningGracePeriodExpiration = new NotifyAction("developer_admin_payment_warning_grace_period_expiration", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_expiration)
            ]
        };

        UserTypeChanged = new NotifyAction("user_type_changed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_type_changed, () => WebstudioNotifyPatternResource.pattern_user_type_changed)
            ]
        };

        UserRoleChanged = new NotifyAction("user_role_changed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_role_changed, () => WebstudioNotifyPatternResource.pattern_user_role_changed)
            ]
        };

        UserAgentRoleChanged = new NotifyAction("user_agent_role_changed", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_agent_role_changed, () => WebstudioNotifyPatternResource.pattern_user_agent_role_changed)
            ]
        };

        TopUpWalletError = new NotifyAction("top_up_wallet_error", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_top_up_wallet_error, () => WebstudioNotifyPatternResource.pattern_top_up_wallet_error)
            ]
        };

        RenewSubscriptionError = new NotifyAction("renew_subscription_error", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_renew_subscription_error, () => WebstudioNotifyPatternResource.pattern_renew_subscription_error)
            ]
        };

        ApiKeyExpired = new NotifyAction("api_key_expired", this)
        {
            Patterns =
            [
                new EmailPattern(() => WebstudioNotifyPatternResource.subject_api_key_expired,
                    () => WebstudioNotifyPatternResource.pattern_api_key_expired)
            ]
        };
    }
}