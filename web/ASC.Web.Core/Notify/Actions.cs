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

namespace ASC.Web.Studio.Core.Notify;

public static class Actions
{
    public static readonly INotifyAction AdminNotify = new NotifyAction("admin_notify")
    {
        Patterns = [
            //new EmailPattern("admin_notify", () => WebstudioNotifyPatternResource.subject_admin_notify, () => WebstudioNotifyPatternResource.pattern_admin_notify)
        ]
    };
    public static readonly INotifyAction PeriodicNotify = new NotifyAction("periodic_notify")
    {
        Patterns = [
            //new EmailPattern("periodic_notify", () => WebstudioNotifyPatternResource.subject_periodic_notify, () => WebstudioNotifyPatternResource.pattern_periodic_notify)
        ]
    };

    public static readonly INotifyAction SelfProfileUpdated = new NotifyAction("self_profile_updated")
    {
        Patterns = [
            new EmailPattern("self_profile_updated", () => WebstudioNotifyPatternResource.subject_self_profile_updated, () => WebstudioNotifyPatternResource.pattern_self_profile_updated),
            new TelegramPattern("self_profile_updated", () => WebstudioNotifyPatternResource.pattern_self_profile_updated_tg)
        ]
    };
    public static readonly INotifyAction UserHasJoin = new NotifyAction("user_has_join")
    {
        Patterns = [
            new EmailPattern("user_has_join", () => WebstudioNotifyPatternResource.subject_user_has_join, () => WebstudioNotifyPatternResource.pattern_user_has_join),
            new TelegramPattern("user_has_join", () => WebstudioNotifyPatternResource.pattern_user_has_join_tg)
        ]
    };
    
    public static readonly INotifyAction UserMessageToAdmin = new NotifyAction("for_admin_notify")
    {
        Patterns = [
            new EmailPattern("for_admin_notify", () => WebstudioNotifyPatternResource.subject_for_admin_notify, () => WebstudioNotifyPatternResource.pattern_for_admin_notify),
            new TelegramPattern("for_admin_notify", () => WebstudioNotifyPatternResource.pattern_for_admin_notify_tg)
        ]
    };
    
    public static readonly INotifyAction UserMessageToSales = new NotifyAction("for_sales_notify")
    {
        Patterns = [
            new EmailPattern("for_sales_notify", () => WebstudioNotifyPatternResource.subject_for_sales_notify, () => WebstudioNotifyPatternResource.pattern_for_sales_notify)
        ]
    };
    
    public static readonly INotifyAction RequestTariff = new NotifyAction("request_tariff")
    {
        Patterns = [
            new EmailPattern("request_tariff", () => WebstudioNotifyPatternResource.subject_request_tariff, () => WebstudioNotifyPatternResource.pattern_request_tariff1)
        ]
    };
    
    public static readonly INotifyAction RequestLicense = new NotifyAction("request_license")
    {
        Patterns = [
            new EmailPattern("request_license", () => WebstudioNotifyPatternResource.subject_request_license, () => WebstudioNotifyPatternResource.pattern_request_license)
        ]
    };

    public static readonly INotifyAction YourProfileUpdated = new NotifyAction("profile_updated")
    {
        Patterns = [
            new EmailPattern("profile_updated", () => WebstudioNotifyPatternResource.subject_profile_updated, () => WebstudioNotifyPatternResource.pattern_profile_updated),
            new TelegramPattern("profile_updated", () => WebstudioNotifyPatternResource.pattern_profile_updated_tg)
        ]
    };
    
    public static readonly INotifyAction JoinUsers = new NotifyAction("join")
    {
        Patterns = [
            new EmailPattern("join", () => WebstudioNotifyPatternResource.subject_join, () => WebstudioNotifyPatternResource.pattern_join),
            new JabberPattern("join", () => WebstudioNotifyPatternResource.pattern_join)
        ]
    };
    
    public static readonly INotifyAction SendWhatsNew = new NotifyAction("send_whats_new")
    {
        Patterns = [
            new EmailPattern("send_whats_new", () => WebstudioNotifyPatternResource.subject_send_whats_new, () => WebstudioNotifyPatternResource.pattern_send_whats_new),
            new TelegramPattern("send_whats_new", () => WebstudioNotifyPatternResource.pattern_send_whats_new)
        ]
    };
    
    public static readonly INotifyAction BackupCreated = new NotifyAction("backup_created")
    {
        Patterns = [
            new EmailPattern("backup_created", () => WebstudioNotifyPatternResource.subject_backup_created, () => WebstudioNotifyPatternResource.pattern_backup_created),
            new TelegramPattern("backup_created", () => WebstudioNotifyPatternResource.pattern_backup_created_tg)
        ]
    };
    public static readonly INotifyAction BackupFailed = new NotifyAction("backup_failed")
    {
        Patterns = [
            new EmailPattern("backup_failed", () => WebstudioNotifyPatternResource.subject_backup_failed, () => WebstudioNotifyPatternResource.pattern_backup_failed),
            new TelegramPattern("backup_failed", () => WebstudioNotifyPatternResource.pattern_backup_failed_tg)
        ]
    };
    public static readonly INotifyAction ScheduledBackupFailed = new NotifyAction("scheduled_backup_failed")
    {
        Patterns = [
            new EmailPattern("scheduled_backup_failed", () => WebstudioNotifyPatternResource.subject_scheduled_backup_failed, () => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed),
            new TelegramPattern("scheduled_backup_failed", () => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed_tg)
        ]
    };
    public static readonly INotifyAction RestoreStarted = new NotifyAction("restore_started")
    {
        Patterns = [
            new EmailPattern("restore_started", () => WebstudioNotifyPatternResource.subject_restore_started, () => WebstudioNotifyPatternResource.pattern_restore_started),
            new TelegramPattern("restore_started", () => WebstudioNotifyPatternResource.pattern_restore_started)
        ]
    };
    public static readonly INotifyAction RestoreCompletedV115 = new NotifyAction("restore_completed_v115")
    {
        Patterns = [
            new EmailPattern("restore_completed_v115", () => WebstudioNotifyPatternResource.subject_restore_completed, () => WebstudioNotifyPatternResource.pattern_restore_completed_v115),
            new TelegramPattern("restore_completed_v115", () => WebstudioNotifyPatternResource.pattern_restore_completed_v115)
        ]
    };
    public static readonly INotifyAction PortalDeactivate = new NotifyAction("portal_deactivate")
    {
        Patterns = [
            new EmailPattern("portal_deactivate", () => WebstudioNotifyPatternResource.subject_portal_deactivate, () => WebstudioNotifyPatternResource.pattern_portal_deactivate),
            new TelegramPattern("portal_deactivate", () => WebstudioNotifyPatternResource.pattern_portal_deactivate_tg)
        ]
    };
    public static readonly INotifyAction PortalDelete = new NotifyAction("portal_delete")
    {
        Patterns = [
            new EmailPattern("portal_delete", () => WebstudioNotifyPatternResource.subject_portal_delete, () => WebstudioNotifyPatternResource.pattern_portal_delete),
            new TelegramPattern("portal_delete", () => WebstudioNotifyPatternResource.pattern_portal_delete_tg)
        ]
    };

    public static readonly INotifyAction ProfileDelete = new NotifyAction("profile_delete")
    {
        Patterns = [
            new EmailPattern("profile_delete", () => WebstudioNotifyPatternResource.subject_profile_delete, () => WebstudioNotifyPatternResource.pattern_profile_delete)
        ]
    };
    public static readonly INotifyAction ProfileHasDeletedItself = new NotifyAction("profile_has_deleted_itself")
    {
        Patterns = [
            new EmailPattern("profile_has_deleted_itself", () => WebstudioNotifyPatternResource.subject_profile_has_deleted_itself, () => WebstudioNotifyPatternResource.pattern_profile_has_deleted_itself)
        ]
    };
    public static readonly INotifyAction ReassignsCompleted = new NotifyAction("reassigns_completed")
    {
        Patterns = [
            new EmailPattern("reassigns_completed", () => WebstudioNotifyPatternResource.subject_reassigns_completed, () => WebstudioNotifyPatternResource.pattern_reassigns_completed)
        ]
    };
    public static readonly INotifyAction ReassignsFailed = new NotifyAction("reassigns_failed")
    {
        Patterns = [
            new EmailPattern("reassigns_failed", () => WebstudioNotifyPatternResource.subject_reassigns_failed, () => WebstudioNotifyPatternResource.pattern_reassigns_failed)
        ]
    };
    public static readonly INotifyAction RemoveUserDataCompleted = new NotifyAction("remove_user_data_completed")
    {
        Patterns = [
            new EmailPattern("remove_user_data_completed", () => WebstudioNotifyPatternResource.subject_remove_user_data_completed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_completed)
        ]
    };
    public static readonly INotifyAction RemoveUserDataCompletedCustomMode = new NotifyAction("remove_user_data_completed_custom_mode")
    {
        Patterns = [
            new EmailPattern("remove_user_data_completed_custom_mode", () => CustomModeResource.subject_remove_user_data_completed_custom_mode, () => CustomModeResource.pattern_remove_user_data_completed_custom_mode)
        ]
    };
    public static readonly INotifyAction RemoveUserDataFailed = new NotifyAction("remove_user_data_failed")
    {
        Patterns = [
            new EmailPattern("remove_user_data_failed", () => WebstudioNotifyPatternResource.subject_remove_user_data_failed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_failed)
        ]
    };

    public static readonly INotifyAction ConfirmOwnerChange = new NotifyAction("owner_confirm_change")
    {
        Patterns = [
            new EmailPattern("owner_confirm_change", () => WebstudioNotifyPatternResource.subject_confirm_owner_change, () => WebstudioNotifyPatternResource.pattern_confirm_owner_change)
        ]
    };
    public static readonly INotifyAction ActivateEmail = new NotifyAction("activate_email")
    {
        Patterns = [
            new EmailPattern("activate_email", () => WebstudioNotifyPatternResource.subject_activate_email, () => WebstudioNotifyPatternResource.pattern_activate_email)
        ]
    };
    public static readonly INotifyAction EmailChangeV115 = new NotifyAction("change_email_v115")
    {
        Patterns = [
            new EmailPattern("change_email_v115", () => WebstudioNotifyPatternResource.subject_change_email_v115, () => WebstudioNotifyPatternResource.pattern_change_email_v115)
        ]
    };
    public static readonly INotifyAction PasswordChangeV115 = new NotifyAction("change_password_v115")
    {
        Patterns = [
            new EmailPattern("change_password_v115", () => WebstudioNotifyPatternResource.subject_change_password_v115, () => WebstudioNotifyPatternResource.pattern_change_password_v115)
        ]
    };
    public static readonly INotifyAction PasswordChanged = new NotifyAction("password_changed")
    {
        Patterns = [
            new EmailPattern("password_changed", () => WebstudioNotifyPatternResource.subject_password_changed, () => WebstudioNotifyPatternResource.pattern_password_changed)
        ]
    };
    public static readonly INotifyAction PasswordSet = new NotifyAction("set_password")
    {
        Patterns = [
            new EmailPattern("set_password", () => WebstudioNotifyPatternResource.subject_set_password, () => WebstudioNotifyPatternResource.pattern_set_password)
        ]
    };
    public static readonly INotifyAction PhoneChange = new NotifyAction("change_phone")
    {
        Patterns = [
            new EmailPattern("change_phone", () => WebstudioNotifyPatternResource.subject_change_phone, () => WebstudioNotifyPatternResource.pattern_change_phone)
        ]
    };
    public static readonly INotifyAction TfaChange = new NotifyAction("change_tfa")
    {
        Patterns = [
            new EmailPattern("change_tfa", () => WebstudioNotifyPatternResource.subject_change_tfa, () => WebstudioNotifyPatternResource.pattern_change_tfa)
        ]
    };
    public static readonly INotifyAction MigrationPortalStart = new NotifyAction("migration_start")
    {
        Patterns = [
            new EmailPattern("migration_start", () => WebstudioNotifyPatternResource.subject_migration_start, () => WebstudioNotifyPatternResource.pattern_migration_start)
        ]
    };
    public static readonly INotifyAction MigrationPortalSuccessV115 = new NotifyAction("migration_success_v115")
    {
        Patterns = [
            new EmailPattern("migration_success_v115", () => WebstudioNotifyPatternResource.subject_migration_success, () => WebstudioNotifyPatternResource.pattern_migration_success_v115)
        ]
    };
    public static readonly INotifyAction MigrationPortalError = new NotifyAction("migration_error")
    {
        Patterns = [
            new EmailPattern("migration_error", () => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_error)
        ]
    };
    public static readonly INotifyAction MigrationPortalServerFailure = new NotifyAction("migration_server_failure")
    {
        Patterns = [
            new EmailPattern("migration_server_failure", () => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_server_failure)
        ]
    };
    public static readonly INotifyAction PortalRename = new NotifyAction("portal_rename")
    {
        Patterns = [
            new EmailPattern("portal_rename", () => WebstudioNotifyPatternResource.subject_portal_rename, () => WebstudioNotifyPatternResource.pattern_portal_rename)
        ]
    };

    public static readonly INotifyAction SaasGuestActivationV115 = new NotifyAction("saas_guest_activation_v115")
    {
        Patterns = [
            new EmailPattern("saas_guest_activation_v115", () => WebstudioNotifyPatternResource.subject_saas_guest_activation_v115, () => WebstudioNotifyPatternResource.pattern_saas_guest_activation_v115)
        ]
    };
    public static readonly INotifyAction EnterpriseGuestActivationV10 = new NotifyAction("enterprise_guest_activation_v10")
    {
        Patterns = [
            new EmailPattern("enterprise_guest_activation_v10", () => WebstudioNotifyPatternResource.subject_enterprise_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_activation_v10)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelGuestActivationV10 = new NotifyAction("enterprise_whitelabel_guest_activation_v10")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_guest_activation_v10", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_activation_v10)
        ]
    };
    public static readonly INotifyAction OpensourceGuestActivationV11 = new NotifyAction("opensource_guest_activation_v11")
    {
        Patterns = [
            new EmailPattern("opensource_guest_activation_v11", () => WebstudioNotifyPatternResource.subject_opensource_guest_activation_v11, () => WebstudioNotifyPatternResource.pattern_opensource_guest_activation_v11)
        ]
    };

    public static readonly INotifyAction SaasGuestWelcomeV1 = new NotifyAction("saas_guest_welcome_v1")
    {
        Patterns = [
            new EmailPattern("saas_guest_welcome_v1", () => WebstudioNotifyPatternResource.subject_saas_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_guest_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseGuestWelcomeV1 = new NotifyAction("enterprise_guest_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_guest_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelGuestWelcomeV1 = new NotifyAction("enterprise_whitelabel_guest_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_guest_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_welcome_v1)
        ]
    };
    public static readonly INotifyAction OpensourceGuestWelcomeV1 = new NotifyAction("opensource_guest_welcome_v1")
    {
        Patterns = [
            new EmailPattern("opensource_guest_welcome_v1", () => WebstudioNotifyPatternResource.subject_opensource_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_guest_welcome_v1)
        ]
    };

    public static readonly INotifyAction SaasCustomModeRegData = new NotifyAction("saas_custom_mode_reg_data")
    {
        Patterns = [
            new EmailPattern("saas_custom_mode_reg_data", () => CustomModeResource.subject_saas_custom_mode_reg_data, () => CustomModeResource.pattern_saas_custom_mode_reg_data)
        ]
    };

    public static readonly INotifyAction StorageEncryptionStart = new NotifyAction("storage_encryption_start")
    {
        Patterns = [
            new EmailPattern("storage_encryption_start", () => WebstudioNotifyPatternResource.subject_storage_encryption_start, () => WebstudioNotifyPatternResource.pattern_storage_encryption_start)
        ]
    };
    public static readonly INotifyAction StorageEncryptionSuccess = new NotifyAction("storage_encryption_success")
    {
        Patterns = [
            new EmailPattern("storage_encryption_success", () => WebstudioNotifyPatternResource.subject_storage_encryption_success, () => WebstudioNotifyPatternResource.pattern_storage_encryption_success)
        ]
    };
    public static readonly INotifyAction StorageEncryptionError = new NotifyAction("storage_encryption_error")
    {
        Patterns = [
            new EmailPattern("storage_encryption_error", () => WebstudioNotifyPatternResource.subject_storage_encryption_error, () => WebstudioNotifyPatternResource.pattern_storage_encryption_error)
        ]
    };
    public static readonly INotifyAction StorageDecryptionStart = new NotifyAction("storage_decryption_start")
    {
        Patterns = [
            new EmailPattern("storage_decryption_start", () => WebstudioNotifyPatternResource.subject_storage_decryption_start, () => WebstudioNotifyPatternResource.pattern_storage_decryption_start)
        ]
    };
    public static readonly INotifyAction StorageDecryptionSuccess = new NotifyAction("storage_decryption_success")
    {
        Patterns = [
            new EmailPattern("storage_decryption_success", () => WebstudioNotifyPatternResource.subject_storage_decryption_success, () => WebstudioNotifyPatternResource.pattern_storage_decryption_success)
        ]
    };
    public static readonly INotifyAction StorageDecryptionError = new NotifyAction("storage_decryption_error")
    {
        Patterns = [
            new EmailPattern("storage_decryption_error", () => WebstudioNotifyPatternResource.subject_storage_decryption_error, () => WebstudioNotifyPatternResource.pattern_storage_decryption_error)
        ]
    };

    public static readonly INotifyAction SaasRoomInvite = new NotifyAction("saas_room_invite")
    {
        Patterns = [
            new EmailPattern("saas_room_invite", () => WebstudioNotifyPatternResource.subject_saas_room_invite, () => WebstudioNotifyPatternResource.pattern_saas_room_invite)
        ]
    };
    public static readonly INotifyAction SaasAgentInvite = new NotifyAction("saas_agent_invite")
    {
        Patterns = [
            new EmailPattern("saas_agent_invite", () => WebstudioNotifyPatternResource.subject_saas_agent_invite, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite)
        ]
    };
    public static readonly INotifyAction SaasRoomInviteExistingUser = new NotifyAction("saas_room_invite_existing_user")
    {
        Patterns = [
            new EmailPattern("saas_room_invite_existing_user", () => WebstudioNotifyPatternResource.subject_saas_room_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_room_invite_existing_user)
        ]
    };
    public static readonly INotifyAction SaasAgentInviteExistingUser = new NotifyAction("saas_agent_invite_existing_user")
    {
        Patterns = [
            new EmailPattern("saas_agent_invite_existing_user", () => WebstudioNotifyPatternResource.subject_saas_agent_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite_existing_user)
        ]
    };
    public static readonly INotifyAction SaasDocSpaceInvite = new NotifyAction("saas_docspace_invite")
    {
        Patterns = [
            new EmailPattern("saas_docspace_invite", () => WebstudioNotifyPatternResource.subject_saas_docspace_invite, () => WebstudioNotifyPatternResource.pattern_saas_docspace_invite)
        ]
    };
    public static readonly INotifyAction SaasDocSpaceRegistration = new NotifyAction("saas_docspace_registration")
    {
        Patterns = [
            new EmailPattern("saas_docspace_registration", () => WebstudioNotifyPatternResource.subject_saas_docspace_registration, () => WebstudioNotifyPatternResource.pattern_saas_docspace_registration)
        ]
    };

    public static readonly INotifyAction SaasAdminActivationV1 = new NotifyAction("saas_admin_activation_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_activation_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_activation_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminActivationV1 = new NotifyAction("enterprise_admin_activation_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_activation_v1", () => WebstudioNotifyPatternResource.subject_enterprise_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_activation_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelAdminActivationV1 = new NotifyAction("enterprise_whitelabel_admin_activation_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_admin_activation_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_activation_v1)
        ]
    };
    public static readonly INotifyAction OpensourceAdminActivationV1 = new NotifyAction("opensource_admin_activation_v1")
    {
        Patterns = [
            new EmailPattern("opensource_admin_activation_v1", () => WebstudioNotifyPatternResource.subject_opensource_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_activation_v1)
        ]
    };

    public static readonly INotifyAction SaasAdminWelcomeV1 = new NotifyAction("saas_admin_welcome_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_welcome_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminWelcomeV1 = new NotifyAction("enterprise_admin_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelAdminWelcomeV1 = new NotifyAction("enterprise_whitelabel_admin_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_admin_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_welcome_v1)
        ]
    };
    public static readonly INotifyAction OpensourceAdminWelcomeV1 = new NotifyAction("opensource_admin_welcome_v1")
    {
        Patterns = [
            new EmailPattern("opensource_admin_welcome_v1", () => WebstudioNotifyPatternResource.subject_opensource_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_welcome_v1)
        ]
    };

    public static readonly INotifyAction DocsTips = new NotifyAction("docs_tips")
    {
        Patterns = [
            new EmailPattern("docs_tips", () => WebstudioNotifyPatternResource.subject_docs_tips, () => WebstudioNotifyPatternResource.pattern_docs_tips)
        ]
    };

    public static readonly INotifyAction SaasAdminTrialWarningAfterHalfYearV1 = new NotifyAction("saas_admin_trial_warning_after_half_year_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_trial_warning_after_half_year_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_trial_warning_after_half_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_trial_warning_after_half_year_v1)
        ]
    };
    public static readonly INotifyAction SaasAdminStartupWarningAfterYearV1 = new NotifyAction("saas_admin_startup_warning_after_year_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_startup_warning_after_year_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_startup_warning_after_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_startup_warning_after_year_v1)
        ]
    };

    public static readonly INotifyAction PortalDeleteSuccessV1 = new NotifyAction("portal_delete_success_v1")
    {
        Patterns = [
            new EmailPattern("portal_delete_success_v1", () => WebstudioNotifyPatternResource.subject_portal_delete_success_v1, () => WebstudioNotifyPatternResource.pattern_portal_delete_success_v1)
        ]
    };
    public static readonly INotifyAction PortalDeletedToSupport = new NotifyAction("portal_deleted_to_support")
    {
        Patterns = [
            new EmailPattern("portal_deleted_to_support", () => WebstudioNotifyPatternResource.subject_portal_deleted_to_support, () => WebstudioNotifyPatternResource.pattern_portal_deleted_to_support)
        ]
    };

    public static readonly INotifyAction SaasUserWelcomeV1 = new NotifyAction("saas_user_welcome_v1")
    {
        Patterns = [
            new EmailPattern("saas_user_welcome_v1", () => WebstudioNotifyPatternResource.subject_saas_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseUserWelcomeV1 = new NotifyAction("enterprise_user_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_user_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelUserWelcomeV1 = new NotifyAction("enterprise_whitelabel_user_welcome_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_user_welcome_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_welcome_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelUserWelcomeCustomModeV1 = new NotifyAction("enterprise_whitelabel_user_welcome_custom_mode_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_user_welcome_custom_mode_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v3)
        ]
    };
    public static readonly INotifyAction OpensourceUserWelcomeV1 = new NotifyAction("opensource_user_welcome_v1")
    {
        Patterns = [
            new EmailPattern("opensource_user_welcome_v1", () => WebstudioNotifyPatternResource.subject_opensource_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_welcome_v1)
        ]
    };

    public static readonly INotifyAction SaasUserActivationV1 = new NotifyAction("saas_user_activation_v1")
    {
        Patterns = [
            new EmailPattern("saas_user_activation_v1", () => WebstudioNotifyPatternResource.subject_saas_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_activation_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseUserActivationV1 = new NotifyAction("enterprise_user_activation_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_user_activation_v1", () => WebstudioNotifyPatternResource.subject_enterprise_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_activation_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseWhitelabelUserActivationV1 = new NotifyAction("enterprise_whitelabel_user_activation_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_whitelabel_user_activation_v1", () => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_activation_v1)
        ]
    };
    public static readonly INotifyAction OpensourceUserActivationV1 = new NotifyAction("opensource_user_activation_v1")
    {
        Patterns = [
            new EmailPattern("opensource_user_activation_v1", () => WebstudioNotifyPatternResource.subject_opensource_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_activation_v1)
        ]
    };

    public static readonly INotifyAction SaasAdminModulesV1 = new NotifyAction("saas_admin_modules_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_modules_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_modules_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_modules_v1)
        ]
    };

    public static readonly INotifyAction SaasAdminUserAppsTipsV1 = new NotifyAction("saas_admin_user_apps_tips_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_user_apps_tips_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_user_apps_tips_v1)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminUserAppsTipsV1 = new NotifyAction("enterprise_admin_user_apps_tips_v1")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_user_apps_tips_v1", () => WebstudioNotifyPatternResource.subject_enterprise_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_user_apps_tips_v1)
        ]
    };

    public static readonly INotifyAction RoomsActivity = new NotifyAction("rooms_activity")
    {
        Patterns = [
            new EmailPattern("rooms_activity", () => WebstudioNotifyPatternResource.subject_rooms_activity, () => WebstudioNotifyPatternResource.pattern_rooms_activity),
            new TelegramPattern("rooms_activity", () => WebstudioNotifyPatternResource.pattern_rooms_activity)
        ]
    };

    public static readonly INotifyAction SaasOwnerPaymentWarningGracePeriodBeforeActivation = new NotifyAction("saas_owner_payment_warning_grace_period_before_activation")
    {
        Patterns = [
            new EmailPattern("saas_owner_payment_warning_grace_period_before_activation", () => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_before_activation)
        ]
    };
    public static readonly INotifyAction SaasOwnerPaymentWarningGracePeriodActivation = new NotifyAction("saas_owner_payment_warning_grace_period_activation")
    {
        Patterns = [
            new EmailPattern("saas_owner_payment_warning_grace_period_activation", () => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_activation)
        ]
    };
    public static readonly INotifyAction SaasOwnerPaymentWarningGracePeriodLastDay = new NotifyAction("saas_owner_payment_warning_grace_period_last_day")
    {
        Patterns = [
            new EmailPattern("saas_owner_payment_warning_grace_period_last_day", () => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_last_day, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_last_day)
        ]
    };
    public static readonly INotifyAction SaasOwnerPaymentWarningGracePeriodExpired = new NotifyAction("saas_owner_payment_warning_grace_period_expired")
    {
        Patterns = [
            new EmailPattern("saas_owner_payment_warning_grace_period_expired", () => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_expired, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_expired)
        ]
    };

    public static readonly INotifyAction SaasAdminVideoGuides = new NotifyAction("saas_video_guides_v1")
    {
        Patterns = [
            new EmailPattern("saas_video_guides_v1", () => WebstudioNotifyPatternResource.subject_saas_video_guides_v1, () => WebstudioNotifyPatternResource.pattern_saas_video_guides_v1)
        ]
    };
    public static readonly INotifyAction SaasAdminIntegrations = new NotifyAction("saas_admin_integrations_v1")
    {
        Patterns = [
            new EmailPattern("saas_admin_integrations_v1", () => WebstudioNotifyPatternResource.subject_saas_admin_integrations_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_integrations_v1)
        ]
    };

    public static readonly INotifyAction ZoomWelcome = new NotifyAction("zoom_welcome")
    {
        Patterns = [
            new EmailPattern("zoom_welcome", () => WebstudioNotifyPatternResource.subject_zoom_welcome, () => WebstudioNotifyPatternResource.pattern_zoom_welcome)
        ]
    };

    public static readonly INotifyAction MigrationPersonalToDocspace = new NotifyAction("migration_personal_to_docspace")
    {
        Patterns = [
            new EmailPattern("migration_personal_to_docspace", () => WebstudioNotifyPatternResource.subject_migration_personal_to_docspace, () => WebstudioNotifyPatternResource.pattern_migration_personal_to_docspace)
        ]
    };

    public static readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodBeforeActivation = new NotifyAction("enterprise_admin_payment_warning_grace_period_before_activation")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_grace_period_before_activation", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_activation)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodActivation = new NotifyAction("enterprise_admin_payment_warning_grace_period_activation")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_grace_period_activation", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_activation)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodBeforeExpiration = new NotifyAction("enterprise_admin_payment_warning_grace_period_before_expiration")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_grace_period_before_expiration", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_expiration)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminPaymentWarningGracePeriodExpiration = new NotifyAction("enterprise_admin_payment_warning_grace_period_expiration")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_grace_period_expiration", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_expiration)
        ]
    };

    public static readonly INotifyAction EnterpriseAdminPaymentWarningLifetimeBeforeExpiration = new NotifyAction("enterprise_admin_payment_warning_lifetime_before_expiration")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_lifetime_before_expiration", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_before_expiration)
        ]
    };
    public static readonly INotifyAction EnterpriseAdminPaymentWarningLifetimeExpiration = new NotifyAction("enterprise_admin_payment_warning_lifetime_expiration")
    {
        Patterns = [
            new EmailPattern("enterprise_admin_payment_warning_lifetime_expiration", () => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_expiration)
        ]
    };

    public static readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodBeforeActivation = new NotifyAction("developer_admin_payment_warning_grace_period_before_activation")
    {
        Patterns = [
            new EmailPattern("developer_admin_payment_warning_grace_period_before_activation", () => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_activation)
        ]
    };
    public static readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodActivation = new NotifyAction("developer_admin_payment_warning_grace_period_activation")
    {
        Patterns = [
            new EmailPattern("developer_admin_payment_warning_grace_period_activation", () => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_activation)
        ]
    };
    public static readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodBeforeExpiration = new NotifyAction("developer_admin_payment_warning_grace_period_before_expiration")
    {
        Patterns = [
            new EmailPattern("developer_admin_payment_warning_grace_period_before_expiration", () => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_expiration)
        ]
    };
    public static readonly INotifyAction DeveloperAdminPaymentWarningGracePeriodExpiration = new NotifyAction("developer_admin_payment_warning_grace_period_expiration")
    {
        Patterns = [
            new EmailPattern("developer_admin_payment_warning_grace_period_expiration", () => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_expiration)
        ]
    };

    public static readonly INotifyAction UserTypeChanged = new NotifyAction("user_type_changed")
    {
        Patterns = [
            new EmailPattern("user_type_changed", () => WebstudioNotifyPatternResource.subject_user_type_changed, () => WebstudioNotifyPatternResource.pattern_user_type_changed)
        ]
    };
    public static readonly INotifyAction UserRoleChanged = new NotifyAction("user_role_changed")
    {
        Patterns = [
            new EmailPattern("user_role_changed", () => WebstudioNotifyPatternResource.subject_user_role_changed, () => WebstudioNotifyPatternResource.pattern_user_role_changed)
        ]
    };
    public static readonly INotifyAction UserAgentRoleChanged = new NotifyAction("user_agent_role_changed")
    {
        Patterns = [
            new EmailPattern("user_agent_role_changed", () => WebstudioNotifyPatternResource.subject_user_agent_role_changed, () => WebstudioNotifyPatternResource.pattern_user_agent_role_changed)
        ]
    };

    public static readonly INotifyAction TopUpWalletError = new NotifyAction("top_up_wallet_error")
    {
        Patterns = [
            new EmailPattern("top_up_wallet_error", () => WebstudioNotifyPatternResource.subject_top_up_wallet_error, () => WebstudioNotifyPatternResource.pattern_top_up_wallet_error)
        ]
    };
    public static readonly INotifyAction RenewSubscriptionError = new NotifyAction("renew_subscription_error")
    {
        Patterns = [
            new EmailPattern("renew_subscription_error", () => WebstudioNotifyPatternResource.subject_renew_subscription_error, () => WebstudioNotifyPatternResource.pattern_renew_subscription_error)
        ]
    };

    public static readonly INotifyAction ApiKeyExpired = new NotifyAction("api_key_expired")
    {
        Patterns = [ 
            new EmailPattern("api_key_expired", 
            () => WebstudioNotifyPatternResource.subject_api_key_expired, 
            () => WebstudioNotifyPatternResource.pattern_api_key_expired)
        ]
    };
}