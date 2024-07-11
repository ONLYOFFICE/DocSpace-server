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

namespace ASC.AuditTrail.Mappers;

internal class SettingsActionsMapper : IProductActionMapper
{
    public List<IModuleActionMapper> Mappers { get; } =
    [
        new GeneralActionMapper(),
        new ProductsActionMapper()
    ];

    public ProductType Product { get; } = ProductType.Settings;
}

internal class GeneralActionMapper : IModuleActionMapper
{
    public ModuleType Module { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public GeneralActionMapper()
    {
        Module = ModuleType.General;
        const ProductType productType = ProductType.Settings;
        Actions = new MessageMapsDictionary(productType, Module)
        {
            {
                ActionType.Update, new[]
                {
                    MessageAction.LanguageSettingsUpdated, MessageAction.TimeZoneSettingsUpdated, MessageAction.DnsSettingsUpdated,
                    MessageAction.TrustedMailDomainSettingsUpdated,MessageAction.PasswordStrengthSettingsUpdated,MessageAction.TwoFactorAuthenticationSettingsUpdated,
                    MessageAction.AdministratorMessageSettingsUpdated,MessageAction.DefaultStartPageSettingsUpdated
                }
            }
        };

        Actions.Add(MessageAction.TwoFactorAuthenticationDisabled, new MessageMaps("TwoFactorAuthenticationSettingsDisabled", ActionType.Update, productType, Module));
        Actions.Add(MessageAction.TwoFactorAuthenticationEnabledBySms, new MessageMaps("TwoFactorAuthenticationSettingsEnabledBySms", ActionType.Update, productType, Module));
        Actions.Add(MessageAction.TwoFactorAuthenticationEnabledByTfaApp, new MessageMaps("TwoFactorAuthenticationSettingsEnabledByTfaApp", ActionType.Update, productType, Module));
    }
}

internal class ProductsActionMapper : IModuleActionMapper
{
    public ModuleType Module { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public ProductsActionMapper()
    {
        Module = ModuleType.Products;
        const ProductType productType = ProductType.Settings;

        Actions = new MessageMapsDictionary(ProductType.Settings, Module)
        {
            {
                ActionType.Update, new[]
                {
                    MessageAction.ProductsListUpdated,
                    MessageAction.GreetingSettingsUpdated,MessageAction.TeamTemplateChanged,MessageAction.ColorThemeChanged,
                    MessageAction.OwnerSentPortalDeactivationInstructions, MessageAction.PortalDeactivated, MessageAction.PortalRenamed,
                    MessageAction.SSOEnabled,MessageAction.SSODisabled,MessageAction.PortalAccessSettingsUpdated,
                    MessageAction.DocumentServiceLocationSetting, MessageAction.AuthorizationKeysSetting,
                    MessageAction.FullTextSearchSetting, MessageAction.StartTransferSetting,
                    MessageAction.StartBackupSetting,MessageAction.LicenseKeyUploaded, MessageAction.StartStorageEncryption,
                    MessageAction.StartStorageDecryption, MessageAction.CookieSettingsUpdated,  MessageAction.MailServiceSettingsUpdated,
                    MessageAction.CustomNavigationSettingsUpdated,MessageAction.AuditSettingsUpdated,MessageAction.PrivacyRoomEnable,
                    MessageAction.PrivacyRoomDisable, 
                    MessageAction.QuotaPerRoomChanged, MessageAction.QuotaPerRoomDisabled, MessageAction.QuotaPerUserChanged, MessageAction.QuotaPerUserDisabled, MessageAction.QuotaPerPortalChanged, MessageAction.QuotaPerPortalDisabled
                }
            },
            {
                ActionType.Create, new[]
                {
                    MessageAction.AdministratorAdded, MessageAction.ProductAddedAdministrator
                }
            },
            {
                ActionType.UpdateAccess, new[]
                {
                    MessageAction.ProductAccessOpened,MessageAction.ProductAccessRestricted,MessageAction.AdministratorDeleted, MessageAction.AdministratorOpenedFullAccess
                }
            },
            {
                ActionType.Delete, new[]
                {
                    MessageAction.ProductDeletedAdministrator,MessageAction.PortalDeleted
                }
            },
            {
                ActionType.Send, new[]
                {
                    MessageAction.OwnerSentPortalDeleteInstructions, MessageAction.OwnerSentChangeOwnerInstructions
                }
            },
            {
                ActionType.Download, new[]
                {
                    MessageAction.LoginHistoryReportDownloaded, MessageAction.AuditTrailReportDownloaded
                }
            }
        };

        Actions.Add(MessageAction.UsersOpenedProductAccess, new MessageMaps("ProductAccessOpenedForUsers", ActionType.UpdateAccess, productType, Module));
        Actions.Add(MessageAction.GroupsOpenedProductAccess, new MessageMaps("ProductAccessOpenedForGroups", ActionType.UpdateAccess, productType, Module));
        Actions.Add(MessageAction.OwnerUpdated, new MessageMaps("OwnerChanged", ActionType.Update, productType, Module));
    }
}