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

using System;
using System.Diagnostics;
using System.Linq;
using ASC.Common.Threading;
using ASC.Core;
using ASC.Core.Billing;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Utility;
using Newtonsoft.Json;
using ASC.Notify.Cron;
using ASC.Web.Api.Routing;
using ASC.Web.Core.PublicResources;

namespace ASC.Api.Settings
{
    public partial class SettingsApi
    {
        /// <summary>
        /// Returns current portal LDAP settings
        /// </summary>
        /// <short>
        /// Get LDAP settings
        /// </short>
        /// <returns>LDAPSupportSettings object</returns>
        [Read("ldap")]
        [EndpointName("getLdapSettings")]
        [EndpointSummary("Get LDAP settings")]
        [EndpointDescription("Returns current portal LDAP settings")]
        [OpenApiResponse(200, "LDAP settings returned successfully")]
        public LdapSettings GetLdapSettings()
        {
            CheckLdapPermissions();

            var settings = LdapSettings.Load();

            settings = settings.Clone() as LdapSettings; // clone LdapSettings object for clear password (potencial AscCache.Memory issue)

            if (settings == null)
                return new LdapSettings().GetDefault() as LdapSettings;

            settings.Password = null;
            settings.PasswordBytes = null;

            if (settings.IsDefault)
                return settings;

            var defaultSettings = settings.GetDefault();

            if (settings.Equals(defaultSettings))
                settings.IsDefault = true;

            return settings;
        }

        /// <summary>
        /// Returns current portal LDAP AutoSync cron expression if any
        /// </summary>
        /// <short>
        /// Get LDAP AutoSync Cron expression
        /// </short>
        /// <returns>string or null</returns>
        [Read("ldap/cron")]
        [EndpointName("getLdapCronSettings")]
        [EndpointSummary("Get LDAP AutoSync Cron expression")]
        [EndpointDescription("Returns current portal LDAP AutoSync cron expression if any")]
        [OpenApiResponse(200, "LDAP AutoSync Cron expression returned successfully")]
        public string GetLdapCronSettings()
        {
            CheckLdapPermissions();

            var settings = LdapCronSettings.Load();

            if (settings == null)
                settings = new LdapCronSettings().GetDefault() as LdapCronSettings;

            if (string.IsNullOrEmpty(settings.Cron))
                return null;

            return settings.Cron;
        }

        /// <summary>
        /// Sets current portal LDAP AutoSync cron expression
        /// </summary>
        /// <short>
        /// Sets LDAP AutoSync Cron expression
        /// </short>
        [Create("ldap/cron")]
        [EndpointName("setLdapCronSettings")]
        [EndpointSummary("Sets LDAP AutoSync Cron expression")]
        [EndpointDescription("Sets current portal LDAP AutoSync cron expression")]
        [OpenApiResponse(200, "LDAP AutoSync Cron expression set successfully")]
        public void SetLdapCronSettings(string cron)
        {
            CheckLdapPermissions();

            if (!string.IsNullOrEmpty(cron))
            {
                new CronExpression(cron); // validate

                if (!LdapSettings.Load().EnableLdapAuthentication)
                {
                    throw new Exception(Resource.LdapSettingsErrorCantSaveLdapSettings);
                }
            }

            var settings = LdapCronSettings.Load();

            if (settings == null)
                settings = new LdapCronSettings();

            settings.Cron = cron;
            settings.Save();

            var t = CoreContext.TenantManager.GetCurrentTenant();
            if (!string.IsNullOrEmpty(cron))
            {
                LdapNotifyHelper.UnregisterAutoSync(t);
                LdapNotifyHelper.RegisterAutoSync(t, cron);
            }
            else
            {
                LdapNotifyHelper.UnregisterAutoSync(t);
            }
        }

        /// <summary>
        /// Start sync users and groups process by LDAP
        /// </summary>
        /// <short>
        /// Sync LDAP
        /// </short>
        [Read("ldap/sync")]
        [EndpointName("syncLdap")]
        [EndpointSummary("Sync LDAP")]
        [EndpointDescription("Start sync users and groups process by LDAP")]
        [OpenApiResponse(200, "LDAP sync started successfully")]
        public LdapOperationStatus SyncLdap()
        {
            CheckLdapPermissions();

            var operations = LDAPTasks.GetTasks()
                .Where(t => t.GetProperty<int>(LdapOperation.OWNER) == TenantProvider.CurrentTenantID)
                .ToList();

            var hasStarted = operations.Any(o =>
            {
                var opType = o.GetProperty<LdapOperationType>(LdapOperation.OPERATION_TYPE);

                return o.Status <= DistributedTaskStatus.Running &&
                       (opType == LdapOperationType.Sync || opType == LdapOperationType.Save);
            });

            if (hasStarted)
            {
                return GetLdapOperationStatus();
            }

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                return GetStartProcessError();
            }

            var ldapSettings = LdapSettings.Load();

            var ldapLocalization = new LdapLocalization(Resource.ResourceManager);

            var tenant = CoreContext.TenantManager.GetCurrentTenant();

            var op = new LdapSaveSyncOperation(ldapSettings, tenant, LdapOperationType.Sync, ldapLocalization);

            return QueueTask(op);
        }

        /// <summary>
        /// Starts the process of collecting preliminary changes on the portal according to the selected LDAP settings
        /// </summary>
        /// <short>
        /// Sync LDAP
        /// </short>
        [Read("ldap/sync/test")]
        [EndpointName("testLdapSync")]
        [EndpointSummary("Sync LDAP")]
        [EndpointDescription("Starts the process of collecting preliminary changes on the portal according to the selected LDAP settings")]
        [OpenApiResponse(200, "LDAP sync test started successfully")]
        public LdapOperationStatus TestLdapSync()
        {
            CheckLdapPermissions();

            var operations = LDAPTasks.GetTasks()
                .Where(t => t.GetProperty<int>(LdapOperation.OWNER) == TenantProvider.CurrentTenantID)
                .ToList();

            var hasStarted = operations.Any(o =>
            {
                var opType = o.GetProperty<LdapOperationType>(LdapOperation.OPERATION_TYPE);

                return o.Status <= DistributedTaskStatus.Running &&
                       (opType == LdapOperationType.SyncTest || opType == LdapOperationType.SaveTest);
            });

            if (hasStarted)
            {
                return GetLdapOperationStatus();
            }

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                return GetStartProcessError();
            }

            var ldapSettings = LdapSettings.Load();

            var ldapLocalization = new LdapLocalization(Resource.ResourceManager);

            var tenant = CoreContext.TenantManager.GetCurrentTenant();

            var op = new LdapSaveSyncOperation(ldapSettings, tenant, LdapOperationType.SyncTest, ldapLocalization);

            return QueueTask(op);
        }

        /// <summary>
        /// Save LDAP settings and start import/sync users and groups process by LDAP
        /// </summary>
        /// <short>
        /// Save LDAP settings
        /// </short>
        /// <param name="settings">LDAPSupportSettings serialized string</param>
        /// <param name="acceptCertificate">Flag permits errors of checking certificates</param>
        [Create("ldap")]
        [EndpointName("saveLdapSettings")]
        [EndpointSummary("Save LDAP settings")]
        [EndpointDescription("Save LDAP settings and start import/sync users and groups process by LDAP")]
        [OpenApiResponse(200, "LDAP settings saved successfully")]
        public LdapOperationStatus SaveLdapSettings(string settings, bool acceptCertificate)
        {
            CheckLdapPermissions();

            var operations = LDAPTasks.GetTasks()
                .Where(t => t.GetProperty<int>(LdapOperation.OWNER) == TenantProvider.CurrentTenantID).ToList();

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                return GetStartProcessError();
            }

            var ldapSettings = JsonConvert.DeserializeObject<LdapSettings>(settings);

            ldapSettings.AcceptCertificate = acceptCertificate;

            if (!ldapSettings.EnableLdapAuthentication)
            {
                SetLdapCronSettings(null);
            }

            //ToDo
            ldapSettings.AccessRights.Clear();

            var ldapLocalization = new LdapLocalization(Resource.ResourceManager);

            var tenant = CoreContext.TenantManager.GetCurrentTenant();

            var op = new LdapSaveSyncOperation(ldapSettings, tenant, LdapOperationType.Save, ldapLocalization, CurrentUser.ToString());

            return QueueTask(op);
        }

        /// <summary>
        /// Starts the process of collecting preliminary changes on the portal according to the LDAP settings
        /// </summary>
        /// <short>
        /// Save LDAP settings
        /// </short>
        [Create("ldap/save/test")]
        [EndpointName("testLdapSave")]
        [EndpointSummary("Save LDAP settings")]
        [EndpointDescription("Starts the process of collecting preliminary changes on the portal according to the LDAP settings")]
        [OpenApiResponse(200, "LDAP settings saved successfully")]
        public LdapOperationStatus TestLdapSave(string settings, bool acceptCertificate)
        {
            CheckLdapPermissions();

            var operations = LDAPTasks.GetTasks()
                .Where(t => t.GetProperty<int>(LdapOperation.OWNER) == TenantProvider.CurrentTenantID)
                .ToList();

            var hasStarted = operations.Any(o =>
            {
                var opType = o.GetProperty<LdapOperationType>(LdapOperation.OPERATION_TYPE);

                return o.Status <= DistributedTaskStatus.Running &&
                       (opType == LdapOperationType.SyncTest || opType == LdapOperationType.SaveTest);
            });

            if (hasStarted)
            {
                return GetLdapOperationStatus();
            }

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                return GetStartProcessError();
            }

            var ldapSettings = JsonConvert.DeserializeObject<LdapSettings>(settings);

            ldapSettings.AcceptCertificate = acceptCertificate;

            var ldapLocalization = new LdapLocalization(Resource.ResourceManager);

            var tenant = CoreContext.TenantManager.GetCurrentTenant();

            var op = new LdapSaveSyncOperation(ldapSettings, tenant, LdapOperationType.SaveTest, ldapLocalization, CurrentUser.ToString());

            return QueueTask(op);
        }

        /// <summary>
        /// Returns LDAP sync process status
        /// </summary>
        /// <short>
        /// Get LDAP sync process status
        /// </short>
        /// <returns>LDAPSupportSettingsResult object</returns>
        [Read("ldap/status")]
        [EndpointName("getLdapOperationStatus")]
        [EndpointSummary("Get LDAP sync process status")]
        [EndpointDescription("Returns LDAP sync process status")]
        [OpenApiResponse(typeof(LdapOperationStatus), 200, "LDAP sync process status")]
        public LdapOperationStatus GetLdapOperationStatus()
        {
            CheckLdapPermissions();

            return ToLdapOperationStatus();
        }

        /// <summary>
        /// Returns LDAP default settings
        /// </summary>
        /// <short>
        /// Get LDAP default settings
        /// </short>
        /// <returns>LDAPSupportSettings object</returns>
        [Read("ldap/default")]
        [EndpointName("getDefaultLdapSettings")]
        [EndpointSummary("Get LDAP default settings")]
        [EndpointDescription("Returns LDAP default settings")]
        [OpenApiResponse(typeof(LdapSettings), 200, "LDAP default settings")]
        public LdapSettings GetDefaultLdapSettings()
        {
            CheckLdapPermissions();

            return new LdapSettings().GetDefault() as LdapSettings;
        }

        private static LdapOperationStatus ToLdapOperationStatus()
        {
            var operations = LDAPTasks.GetTasks().ToList();

            foreach (var o in operations)
            {
                if (!string.IsNullOrEmpty(o.InstanseId) &&
                    Process.GetProcesses().Any(p => p.Id == int.Parse(o.InstanseId)))
                    continue;

                o.SetProperty(LdapOperation.PROGRESS, 100);
                LDAPTasks.RemoveTask(o.Id);
            }

            var operation =
                operations
                    .FirstOrDefault(t => t.GetProperty<int>(LdapOperation.OWNER) == TenantProvider.CurrentTenantID);

            if (operation == null)
            {
                return null;
            }

            if (DistributedTaskStatus.Running < operation.Status)
            {
                operation.SetProperty(LdapOperation.PROGRESS, 100);
                LDAPTasks.RemoveTask(operation.Id);
            }

            var certificateConfirmRequest = operation.GetProperty<LdapCertificateConfirmRequest>(LdapOperation.CERT_REQUEST);

            var result = new LdapOperationStatus
            {
                Id = operation.Id,
                Completed = operation.GetProperty<bool>(LdapOperation.FINISHED),
                Percents = operation.GetProperty<int>(LdapOperation.PROGRESS),
                Status = operation.GetProperty<string>(LdapOperation.RESULT),
                Error = operation.GetProperty<string>(LdapOperation.ERROR),
                CertificateConfirmRequest = certificateConfirmRequest,
                Source = operation.GetProperty<string>(LdapOperation.SOURCE),
                OperationType = Enum.GetName(typeof(LdapOperationType),
                    (LdapOperationType)Convert.ToInt32(operation.GetProperty<string>(LdapOperation.OPERATION_TYPE))),
                Warning = operation.GetProperty<string>(LdapOperation.WARNING)
            };

            if (!(string.IsNullOrEmpty(result.Warning))) {
                operation.SetProperty(LdapOperation.WARNING, ""); // "mark" as read
            }

            return result;
        }

        private static void CheckLdapPermissions()
        {
            SecurityContext.DemandPermissions(SecutiryConstants.EditPortalSettings);

            if (!SetupInfo.IsVisibleSettings(ManagementType.LdapSettings.ToString()) ||
                (CoreContext.Configuration.Standalone &&
                 !CoreContext.TenantManager.GetTenantQuota(TenantProvider.CurrentTenantID).Ldap))
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "Ldap");
            }
        }

        private LdapOperationStatus QueueTask(LdapOperation op)
        {
            LDAPTasks.QueueTask(op.RunJob, op.GetDistributedTask());
            return ToLdapOperationStatus();
        }

        private LdapOperationStatus GetStartProcessError()
        {
            var result = new LdapOperationStatus
            {
                Id = null,
                Completed = true,
                Percents = 0,
                Status = "",
                Error = Resource.LdapSettingsTooManyOperations,
                CertificateConfirmRequest = null,
                Source = ""
            };

            return result;
        }
    }
}
