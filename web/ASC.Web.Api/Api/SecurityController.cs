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

using ASC.Api.Core.Cors.Enums;

using Microsoft.AspNetCore.Cors;

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Security API.
/// </summary>
/// <name>security</name>
[Scope]
[DefaultRoute]
[ApiController]
public class SecurityController(PermissionContext permissionContext,
        TenantManager tenantManager,
        MessageService messageService,
        LoginEventsRepository loginEventsRepository,
        AuditEventsRepository auditEventsRepository,
        AuditReportCreator auditReportCreator,
        AuditReportUploader auditReportSaver,
        SettingsManager settingsManager,
        AuditActionMapper auditActionMapper,
        CoreBaseSettings coreBaseSettings,
        ApiContext apiContext,
        CspSettingsHelper cspSettingsHelper)
    : ControllerBase
{
    /// <summary>
    /// Returns all the latest user login activity, including successful logins and error logs.
    /// </summary>
    /// <short>
    /// Get login history
    /// </short>
    /// <category>Login history</category>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.LoginEventDto, ASC.Web.Api">List of login events</returns>
    /// <path>api/2.0/security/audit/login/last</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("audit/login/last")]
    public async Task<IEnumerable<LoginEventDto>> GetLastLoginEventsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return (await loginEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20)).Select(x => new LoginEventDto(x));
    }

    /// <summary>
    /// Returns a list of the latest changes (creation, modification, deletion, etc.) made by users to the entities (tasks, opportunities, files, etc.) on the portal.
    /// </summary>
    /// <short>
    /// Get audit trail data
    /// </short>
    /// <category>Audit trail data</category>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuditEventDto, ASC.Web.Api">List of audit trail data</returns>
    /// <path>api/2.0/security/audit/events/last</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("audit/events/last")]
    public async Task<IEnumerable<AuditEventDto>> GetLastAuditEventsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return (await auditEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20)).Select(x => new AuditEventDto(x, auditActionMapper));
    }

    /// <summary>
    /// Returns a list of the login events by the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Get filtered login events
    /// </short>
    /// <category>Login history</category>
    /// <param type="System.Guid, System" name="userId">User ID</param>
    /// <param type="ASC.MessagingSystem.Core.MessageAction, ASC.MessagingSystem.Core" name="action">Action</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="from">Start date</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="to">End date</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.LoginEventDto, ASC.Web.Api">List of filtered login events</returns>
    /// <path>api/2.0/security/audit/login/filter</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("/audit/login/filter")]
    public async Task<IEnumerable<LoginEventDto>> GetLoginEventsByFilterAsync(Guid userId,
    MessageAction action,
    ApiDateTime from,
    ApiDateTime to)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var startIndex = (int)apiContext.StartIndex;
        var limit = (int)apiContext.Count;
        apiContext.SetDataPaginated();

        action = action == 0 ? MessageAction.None : action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToString()))
        {
            return await GetLastLoginEventsAsync();
        }

        await DemandAuditPermissionAsync();

        return (await loginEventsRepository.GetByFilterAsync(userId, action, from, to, startIndex, limit)).Select(x => new LoginEventDto(x));
    }

    /// <summary>
    /// Returns a list of the audit events by the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Get filtered audit trail data
    /// </short>
    /// <category>Audit trail data</category>
    /// <param type="System.Guid, System" name="userId">User ID</param>
    /// <param type="ASC.AuditTrail.Types.ProductType, ASC.AuditTrail.Types" name="productType">Product</param>
    /// <param type="ASC.AuditTrail.Types.ModuleType, ASC.AuditTrail.Types" name="moduleType">Module</param>
    /// <param type="ASC.AuditTrail.Types.ActionType, ASC.AuditTrail.Types" name="actionType">Action type</param>
    /// <param type="ASC.MessagingSystem.Core.MessageAction, ASC.MessagingSystem.Core" name="action">Action</param>
    /// <param type="ASC.AuditTrail.Types.EntryType, ASC.AuditTrail.Types" name="entryType">Entry</param>
    /// <param type="System.String, System" name="target">Target</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="from">Start date</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="to">End date</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuditEventDto, ASC.Web.Api">List of filtered audit trail data</returns>
    /// <path>api/2.0/security/audit/events/filter</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("/audit/events/filter")]
    public async Task<IEnumerable<AuditEventDto>> GetAuditEventsByFilterAsync(Guid userId,
            ProductType productType,
            ModuleType moduleType,
            ActionType actionType,
            MessageAction action,
            EntryType entryType,
            string target,
            ApiDateTime from,
            ApiDateTime to)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var startIndex = (int)apiContext.StartIndex;
        var limit = (int)apiContext.Count;
        apiContext.SetDataPaginated();

        action = action == 0 ? MessageAction.None : action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToString()))
        {
            return await GetLastAuditEventsAsync();
        }

        await DemandAuditPermissionAsync();

        return (await auditEventsRepository.GetByFilterAsync(userId, productType, moduleType, actionType, action, entryType, target, from, to, startIndex, limit)).Select(x => new AuditEventDto(x, auditActionMapper));
    }

    /// <summary>
    /// Returns all the available audit trail types.
    /// </summary>
    /// <short>
    /// Get audit trail types
    /// </short>
    /// <category>Audit trail data</category>
    /// <returns type="System.Object, System">Audit trail types</returns>
    /// <path>api/2.0/security/audit/types</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpGet("audit/types")]
    public object GetTypes()
    {
        return new
        {
            Actions = MessageActionExtensions.GetNames(),
            ActionTypes = ActionTypeExtensions.GetNames(),
            ProductTypes = ProductTypeExtensions.GetNames(),
            ModuleTypes = ModuleTypeExtensions.GetNames(),
            EntryTypes = EntryTypeExtensions.GetNames()
        };
    }

    /// <summary>
    /// Returns the mappers for the audit trail types.
    /// </summary>
    /// <short>
    /// Get audit trail mappers
    /// </short>
    /// <category>Audit trail data</category>
    /// <param type="System.Nullable{ASC.AuditTrail.Types.ProductType}, System" name="productType">Product</param>
    /// <param type="System.Nullable{ASC.AuditTrail.Types.ModuleType}, System" name="moduleType">Module</param>
    /// <returns type="System.Object, System">Audit trail mappers</returns>
    /// <path>api/2.0/security/audit/mappers</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpGet("/audit/mappers")]
    public object GetMappers(ProductType? productType, ModuleType? moduleType)
    {
        return auditActionMapper.Mappers
            .Where(r => !productType.HasValue || r.Product == productType.Value)
            .Select(r => new
            {
                ProductType = r.Product.ToString(),
                Modules = r.Mappers
                .Where(m => !moduleType.HasValue || m.Module == moduleType.Value)
                .Select(x => new
                {
                    ModuleType = x.Module.ToString(),
                    Actions = x.Actions.Select(a => new
                    {
                        MessageAction = a.Key.ToString(),
                        ActionType = a.Value.ActionType.ToString(),
                        Entity = a.Value.EntryType1.ToString()
                    })
                })
            });
    }

    /// <summary>
    /// Generates the login history report.
    /// </summary>
    /// <short>
    /// Generate the login history report
    /// </short>
    /// <category>Login history</category>
    /// <returns type="System.Object, System">URL to the xlsx report file</returns>
    /// <path>api/2.0/security/audit/login/report</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("audit/login/report")]
    public async Task<object> CreateLoginHistoryReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(await tenantManager.GetCurrentTenantIdAsync());

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.LoginHistoryLifeTime));

        var reportName = string.Format(AuditReportResource.LoginHistoryReportName + ".csv", from.ToShortDateString(), to.ToShortDateString());
        var events = await loginEventsRepository.GetByFilterAsync(fromDate: from, to: to);

        await using var stream = auditReportCreator.CreateCsvReport(events);
        var result = await auditReportSaver.UploadCsvReport(stream, reportName);

        await messageService.SendAsync(MessageAction.LoginHistoryReportDownloaded);
        return result;
    }

    /// <summary>
    /// Generates the audit trail report.
    /// </summary>
    /// <short>
    /// Generate the audit trail report
    /// </short>
    /// <category>Audit trail data</category>
    /// <returns type="System.Object, System">URL to the xlsx report file</returns>
    /// <path>api/2.0/security/audit/events/report</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("audit/events/report")]
    public async Task<object> CreateAuditTrailReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantId);

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.AuditTrailLifeTime));

        var reportName = string.Format(AuditReportResource.AuditTrailReportName + ".csv", from.ToString("MM.dd.yyyy", CultureInfo.InvariantCulture), to.ToString("MM.dd.yyyy"));

        var events = await auditEventsRepository.GetByFilterAsync(from: from, to: to);

        await using var stream = auditReportCreator.CreateCsvReport(events);
        var result = await auditReportSaver.UploadCsvReport(stream, reportName);

        await messageService.SendAsync(MessageAction.AuditTrailReportDownloaded);
        return result;
    }

    /// <summary>
    /// Returns the audit trail settings.
    /// </summary>
    /// <short>
    /// Get the audit trail settings
    /// </short>
    /// <category>Audit trail data</category>
    /// <returns type="ASC.Core.Tenants.TenantAuditSettings, ASC.Core.Common">Audit settings</returns>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("audit/settings/lifetime")]
    public async Task<TenantAuditSettings> GetAuditSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return await settingsManager.LoadAsync<TenantAuditSettings>(await tenantManager.GetCurrentTenantIdAsync());
    }

    /// <summary>
    /// Sets the audit trail settings for the current portal.
    /// </summary>
    /// <short>
    /// Set the audit trail settings
    /// </short>
    /// <category>Audit trail data</category>
    /// <param type="ASC.Core.Tenants.TenantAuditSettingsWrapper, ASC.Core.Common" name="inDto">Audit trail settings</param>
    /// <returns type="ASC.Core.Tenants.TenantAuditSettings, ASC.Core.Common">Audit trail settings</returns>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("audit/settings/lifetime")]
    public async Task<TenantAuditSettings> SetAuditSettings(TenantAuditSettingsWrapper inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        if (inDto.Settings.LoginHistoryLifeTime is <= 0 or > TenantAuditSettings.MaxLifeTime)
        {
            throw new ArgumentException("LoginHistoryLifeTime");
        }

        if (inDto.Settings.AuditTrailLifeTime is <= 0 or > TenantAuditSettings.MaxLifeTime)
        {
            throw new ArgumentException("AuditTrailLifeTime");
        }

        await settingsManager.SaveAsync(inDto.Settings, await tenantManager.GetCurrentTenantIdAsync());
        await messageService.SendAsync(MessageAction.AuditSettingsUpdated);

        return inDto.Settings;
    }

    [EnableCors(PolicyName = CorsPoliciesEnums.AllowAllCorsPolicyName )]
    [HttpPost("csp")]
    public async Task<CspDto> Csp(CspRequestsDto request)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        ArgumentNullException.ThrowIfNull(request);

        if (request.Domains != null)
        {
            foreach (var domain in request.Domains)
            {
                var uriString = domain.Replace($"{Uri.SchemeDelimiter}*.", Uri.SchemeDelimiter);

                if (uriString.StartsWith("*."))
                {
                    uriString = uriString.Replace("*.", "");
                }
                
                if (!uriString.Contains(Uri.SchemeDelimiter))
                {
                    uriString = string.Concat(Uri.UriSchemeHttp, Uri.SchemeDelimiter, uriString);
                }

                if (!Uri.TryCreate(uriString, UriKind.Absolute, out _) || (Encoding.UTF8.GetByteCount(domain) != domain.Length))
                {
                    throw new ArgumentException(domain, nameof(request.Domains));
                }
            }
        }

        var header = await cspSettingsHelper.SaveAsync(request.Domains);

        return new CspDto { Domains = request.Domains, Header = header };
    }

    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [EnableCors(PolicyName = CorsPoliciesEnums.AllowAllCorsPolicyName)]
    [HttpGet("csp")]
    public async Task<CspDto> Csp()
    {
        //await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await cspSettingsHelper.LoadAsync();
        return new CspDto
        {
            Domains = settings.Domains,
            Header = await cspSettingsHelper.CreateHeaderAsync(settings.Domains)
        };
    }

    private async Task DemandAuditPermissionAsync()
    {
        if (!coreBaseSettings.Standalone
            && (!SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToString())
                || !(await tenantManager.GetCurrentTenantQuotaAsync()).Audit))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Audit");
        }
    }

    private void DemandBaseAuditPermission()
    {
        if (!coreBaseSettings.Standalone
            && !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToString()))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Audit");
        }
    }
}
