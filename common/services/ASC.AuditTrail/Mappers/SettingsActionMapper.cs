// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.AuditTrail.Mappers;

internal class SettingsActionsMapper : IProductActionMapper
{
    public List<ILocationActionMapper> Mappers { get; } =
    [
        new GeneralActionMapper(),
        new ProductsActionMapper(),
        new OAuthActionMapper()
    ];

    public ProductType Product => ProductType.Settings;
}

internal class GeneralActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public GeneralActionMapper()
    {
        Location = LocationType.Settings;

        Actions = new MessageMapsDictionary(ProductType.Settings, Location)
        {
            {
                ActionType.Update, [
                    MessageAction.LanguageSettingsUpdated, MessageAction.TimeZoneSettingsUpdated, MessageAction.DnsSettingsUpdated,
                    MessageAction.TrustedMailDomainSettingsUpdated,MessageAction.PasswordStrengthSettingsUpdated,MessageAction.TwoFactorAuthenticationSettingsUpdated,
                    MessageAction.AdministratorMessageSettingsUpdated,MessageAction.DefaultStartPageSettingsUpdated
                ]
            },
            { ActionType.Send, [MessageAction.ContactAdminMailSent] }
        };

        Actions.Add(MessageAction.TwoFactorAuthenticationDisabled, new MessageMaps(nameof(AuditReportResource.TwoFactorAuthenticationSettingsDisabled), ActionType.Update, ProductType.Settings, Location));
        Actions.Add(MessageAction.TwoFactorAuthenticationEnabledBySms, new MessageMaps(nameof(AuditReportResource.TwoFactorAuthenticationSettingsEnabledBySms), ActionType.Update, ProductType.Settings, Location));
        Actions.Add(MessageAction.TwoFactorAuthenticationEnabledByTfaApp, new MessageMaps(nameof(AuditReportResource.TwoFactorAuthenticationSettingsEnabledByTfaApp), ActionType.Update, ProductType.Settings, Location));
    }
}

internal class ProductsActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public ProductsActionMapper()
    {
        Location = LocationType.Settings;

        Actions = new MessageMapsDictionary(ProductType.Settings, Location)
        {
            {
                ActionType.Update, [
                    MessageAction.ProductsListUpdated,
                    MessageAction.GreetingSettingsUpdated,MessageAction.TeamTemplateChanged,MessageAction.ColorThemeChanged,
                    MessageAction.OwnerSentPortalDeactivationInstructions, MessageAction.PortalDeactivated, MessageAction.PortalRenamed,
                    MessageAction.SSOEnabled,MessageAction.SSODisabled,MessageAction.PortalAccessSettingsUpdated,
                    MessageAction.LdapEnabled,MessageAction.LdapDisabled,MessageAction.LdapSync,
                    MessageAction.DocumentServiceLocationSetting, MessageAction.AuthorizationKeysSetting,
                    MessageAction.FullTextSearchSetting, MessageAction.StartTransferSetting,
                    MessageAction.BackupStarted,MessageAction.BackupCompleted,MessageAction.BackupFailed,MessageAction.BackupCancelled,
                    MessageAction.ScheduledBackupStarted,MessageAction.ScheduledBackupCompleted,MessageAction.ScheduledBackupFailed,
                    MessageAction.RestoreStarted,MessageAction.RestoreCancelled,
                    MessageAction.LicenseKeyUploaded, MessageAction.StartStorageEncryption,
                    MessageAction.StartStorageDecryption, MessageAction.CookieSettingsUpdated,  MessageAction.MailServiceSettingsUpdated,
                    MessageAction.CustomNavigationSettingsUpdated,MessageAction.AuditSettingsUpdated,
                    MessageAction.QuotaPerRoomChanged, MessageAction.QuotaPerRoomDisabled, MessageAction.QuotaPerUserChanged, MessageAction.QuotaPerUserDisabled, MessageAction.QuotaPerPortalChanged, MessageAction.QuotaPerPortalDisabled, MessageAction.QuotaPerAiAgentChanged, MessageAction.QuotaPerAiAgentDisabled,
                    MessageAction.CustomQuotaPerRoomDefault, MessageAction.CustomQuotaPerRoomChanged, MessageAction.CustomQuotaPerRoomDisabled, MessageAction.CustomQuotaPerUserDefault, MessageAction.CustomQuotaPerUserChanged, MessageAction.CustomQuotaPerUserDisabled,
                    MessageAction.CustomQuotaPerAiAgentDefault, MessageAction.CustomQuotaPerAiAgentChanged, MessageAction.CustomQuotaPerAiAgentDisabled,
                    MessageAction.DevToolsAccessSettingsChanged,
                    MessageAction.WebhookUpdated,
                    MessageAction.ApiKeyUpdated,
                    MessageAction.CustomerWalletToppedUp, MessageAction.CustomerWalletTopUpSettingsUpdated, MessageAction.CustomerSubscriptionUpdated,
                    MessageAction.BannerSettingsChanged, MessageAction.CustomerWalletServicesSettingsUpdated,
                    MessageAction.SubscriptionBalanceMovedToWallet,
                    MessageAction.AIProviderUpdated,
                    MessageAction.AIDefaultProviderSet,
                    MessageAction.ServerUpdated,
                    MessageAction.ServerEnabled,
                    MessageAction.ServerDisabled,
                    MessageAction.WebpluginUpdated,
                    MessageAction.WhiteLabelSettingsLogoTextUpdated,MessageAction.WhiteLabelSettingsLogosUpdated,MessageAction.WhiteLabelCompanySettingsUpdated,MessageAction.WhiteLabelAdditionalSettingsUpdated,MessageAction.WhiteLabelMailSettingsUpdated,
                    MessageAction.InvitationSettingsUpdated,MessageAction.IPRestrictionsSettingsUpdated,MessageAction.LoginSettingsUpdated,
                    MessageAction.AIAccessEnabled,
                    MessageAction.AIAccessDisabled,
                    MessageAction.UserUpdatedAiSettings
                ]
            },
            {
                ActionType.Create, [
                    MessageAction.AdministratorAdded,
                    MessageAction.ProductAddedAdministrator,
                    MessageAction.WebhookCreated,
                    MessageAction.ApiKeyCreated,
                    MessageAction.CustomerOperationPerformed,
                    MessageAction.AIProviderCreated,
                    MessageAction.ServerCreated,
                    MessageAction.SetWebSearchSettings,
                    MessageAction.SetVectorizationSettings
                ]
            },
            {
                ActionType.UpdateAccess, [
                    MessageAction.ProductAccessOpened,MessageAction.ProductAccessRestricted,MessageAction.AdministratorDeleted, MessageAction.AdministratorOpenedFullAccess
                ]
            },
            {
                ActionType.Delete, [
                    MessageAction.ProductDeletedAdministrator,
                    MessageAction.PortalDeleted,
                    MessageAction.WebhookDeleted,
                    MessageAction.ApiKeyDeleted,
                    MessageAction.AIProviderDeleted,
                    MessageAction.ServerDeleted,
                    MessageAction.ResetWebSearchSettings,
                    MessageAction.ResetVectorizationSettings,
                    MessageAction.WebpluginDeleted,
                    MessageAction.ScheduledBackupDeleted
                ]
            },
            {
                ActionType.Send, [
                    MessageAction.OwnerSentPortalDeleteInstructions, MessageAction.OwnerSentChangeOwnerInstructions
                ]
            },
            {
                ActionType.Download, [
                    MessageAction.LoginHistoryReportDownloaded, MessageAction.AuditTrailReportDownloaded, MessageAction.CustomerOperationsReportDownloaded
                ]
            },
            {
                ActionType.Upload, [
                    MessageAction.WebpluginUploaded
                ]
            }
        };

        Actions.Add(MessageAction.UsersOpenedProductAccess, new MessageMaps(nameof(AuditReportResource.ProductAccessOpenedForUsers), ActionType.UpdateAccess, ProductType.Settings, Location));
        Actions.Add(MessageAction.GroupsOpenedProductAccess, new MessageMaps(nameof(AuditReportResource.ProductAccessOpenedForGroups), ActionType.UpdateAccess, ProductType.Settings, Location));
        Actions.Add(MessageAction.OwnerUpdated, new MessageMaps(nameof(AuditReportResource.OwnerChanged), ActionType.Update, ProductType.Settings, Location));
    }
}

public class OAuthActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public OAuthActionMapper()
    {
        Location = LocationType.Settings;
        Actions = new MessageMapsDictionary(ProductType.Settings, Location)
        {
            {
                ActionType.Create, [
                    MessageAction.CreateClient
                ]
            },
            {
                ActionType.Update, [
                    MessageAction.UpdateClient,
                    MessageAction.RegenerateSecret,
                    MessageAction.ChangeClientActivation,
                    MessageAction.ChangeClientVisibility,
                    MessageAction.RevokeUserClient,
                    MessageAction.GenerateAuthorizationCodeToken,
                    MessageAction.GeneratePersonalAccessToken
                ]
            },
            {
                ActionType.Delete, [
                    MessageAction.DeleteClient
                ]
            }
        };
    }
}
