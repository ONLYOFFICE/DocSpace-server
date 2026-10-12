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

using System;
using System.Linq;
using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Users;
using ASC.MessagingSystem;
using ASC.Web.Api.Routing;
using ASC.Web.Core.PublicResources;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Utility;
using Newtonsoft.Json;

namespace ASC.Api.Settings
{
    public partial class SettingsApi
    {
        /// <summary>
        /// Returns the current portal SSO settings.
        /// </summary>
        /// <short>
        /// Get SSO settings
        /// </short>
        /// <returns>SsoSettingsV2 object</returns>
        [Read("ssov2")]
        public SsoSettingsV2 GetSsoSettingsV2()
        {
            CheckSsoPermissions();

            var settings = SsoSettingsV2.Load();

            if(string.IsNullOrEmpty(settings.SpLoginLabel))
                settings.SpLoginLabel = SsoSettingsV2.SSO_SP_LOGIN_LABEL;

            return settings;
        }

        /// <summary>
        /// Returns the default portal SSO settings.
        /// </summary>
        /// <short>
        /// Get default SSO settings
        /// </short>
        /// <returns>SsoSettingsV2 object</returns>
        [Read("ssov2/default")]
        public SsoSettingsV2 GetDefaultSsoSettingsV2()
        {
            CheckSsoPermissions();

            return new SsoSettingsV2().GetDefault() as SsoSettingsV2;
        }

        /// <summary>
        /// Returns the SSO settings constants.
        /// </summary>
        /// <short>
        /// Get SSO settings constants
        /// </short>
        /// <returns>object</returns>
        [Read("ssov2/constants")]
        public object GetSsoSettingsV2Constants()
        {
            return new
                {
                    SsoNameIdFormatType = new SsoNameIdFormatType(),
                    SsoBindingType = new SsoBindingType(),
                    SsoSigningAlgorithmType = new SsoSigningAlgorithmType(),
                    SsoEncryptAlgorithmType = new SsoEncryptAlgorithmType(),
                    SsoSpCertificateActionType = new SsoSpCertificateActionType(),
                    SsoIdpCertificateActionType = new SsoIdpCertificateActionType()
                };
        }

        /// <summary>
        /// Save the SSO settings for current portal.
        /// </summary>
        /// <short>
        /// Save SSO settings
        /// </short>
        /// <param name="serializeSettings">serialized SsoSettingsV2 object</param>
        /// <returns>SsoSettingsV2 object</returns>
        [Create("ssov2")]
        public SsoSettingsV2 SaveSsoSettingsV2(string serializeSettings)
        {
            CheckSsoPermissions();

            if (string.IsNullOrEmpty(serializeSettings))
                throw new ArgumentException(Resource.SsoSettingsCouldNotBeNull);

            var settings = JsonConvert.DeserializeObject<SsoSettingsV2>(serializeSettings);

            if (settings == null)
                throw new ArgumentException(Resource.SsoSettingsCouldNotBeNull);

            if (string.IsNullOrWhiteSpace(settings.IdpSettings.EntityId))
                throw new Exception(Resource.SsoSettingsInvalidEntityId);

            if (string.IsNullOrWhiteSpace(settings.IdpSettings.SsoUrl) ||
                !CheckUri(settings.IdpSettings.SsoUrl))
                throw new Exception(string.Format(Resource.SsoSettingsInvalidBinding, "SSO " + settings.IdpSettings.SsoBinding));

            if (!string.IsNullOrWhiteSpace(settings.IdpSettings.SloUrl) &&
                !CheckUri(settings.IdpSettings.SloUrl))
                throw new Exception(string.Format(Resource.SsoSettingsInvalidBinding, "SLO " + settings.IdpSettings.SloBinding));

            if (string.IsNullOrWhiteSpace(settings.FieldMapping.FirstName) ||
                string.IsNullOrWhiteSpace(settings.FieldMapping.LastName) ||
                string.IsNullOrWhiteSpace(settings.FieldMapping.Email))
                throw new Exception(Resource.SsoSettingsInvalidMapping);

            if (string.IsNullOrEmpty(settings.SpLoginLabel))
            {
                settings.SpLoginLabel = SsoSettingsV2.SSO_SP_LOGIN_LABEL;
            }
            else if (settings.SpLoginLabel.Length > 100)
            {
                settings.SpLoginLabel = settings.SpLoginLabel.Substring(0, 100);
            }

            if (!settings.Save())
                throw new Exception(Resource.SsoSettingsCantSaveSettings);

            if(!settings.EnableSso)
                ConverSsoUsersToOrdinary();

            var messageAction = settings.EnableSso ? MessageAction.SSOEnabled : MessageAction.SSODisabled;

            MessageService.Send(HttpContext.Current.Request, messageAction);

            return settings;
        }

        /// <summary>
        /// Reset the SSO settings for current portal.
        /// </summary>
        /// <short>
        /// Reset SSO settings
        /// </short>
        /// <returns>SsoSettingsV2 object</returns>
        [Delete("ssov2")]
        public SsoSettingsV2 ResetSsoSettingsV2()
        {
            CheckSsoPermissions();

            var defaultSettings = new SsoSettingsV2().GetDefault() as SsoSettingsV2;

            if (defaultSettings != null && !defaultSettings.Save())
                throw new Exception(Resource.SsoSettingsCantSaveSettings);

            ConverSsoUsersToOrdinary();

            MessageService.Send(HttpContext.Current.Request, MessageAction.SSODisabled);

            return defaultSettings;
        }

        private static void ConverSsoUsersToOrdinary()
        {
            var ssoUsers = CoreContext.UserManager.GetUsers().Where(u => u.IsSSO()).ToList();

            if(!ssoUsers.Any())
                return;

            foreach (var existingSsoUser in ssoUsers)
            {
                existingSsoUser.SsoNameId = null;
                existingSsoUser.SsoSessionId = null;

                existingSsoUser.ConvertExternalContactsToOrdinary();

                CoreContext.UserManager.SaveUserInfo(existingSsoUser);
            }
        }

        private static bool CheckUri(string uriName)
        {
            Uri uriResult;
            return Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static void CheckSsoPermissions()
        {
            SecurityContext.DemandPermissions(SecutiryConstants.EditPortalSettings);

            if (!SetupInfo.IsVisibleSettings(ManagementType.SingleSignOnSettings.ToString()) ||
                (CoreContext.Configuration.Standalone &&
                 !CoreContext.TenantManager.GetTenantQuota(TenantProvider.CurrentTenantID).Sso))
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "Sso");
            }
        }
    }
}
