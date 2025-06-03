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

using ASC.Api.Core.Cors.Enums;
using ASC.Core.Common.Identity;
using ASC.Files.Core.Utils;

using Microsoft.AspNetCore.Cors;

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Security API.
/// </summary>
/// <name>security</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("security")]
public class SecurityController(PermissionContext permissionContext,
        TenantManager tenantManager,
        MessageService messageService,
        LoginEventsRepository loginEventsRepository,
        AuditEventsRepository auditEventsRepository,
        CsvFileHelper csvFileHelper,
        CsvFileUploader csvFileUploader,
        SettingsManager settingsManager,
        AuditActionMapper auditActionMapper,
        CoreBaseSettings coreBaseSettings,
        ApiContext apiContext,
        CspSettingsHelper cspSettingsHelper, 
        ApiDateTimeHelper apiDateTimeHelper,
        IdentityClient identityClient)
    : ControllerBase
{
    /// <summary>
    /// Returns all the latest user login activity, including successful logins and error logs.
    /// </summary>
    /// <short>
    /// Get login history
    /// </short>
    /// <path>api/2.0/security/audit/login/last</path>
    /// <collection>list</collection>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "List of login events", typeof(IEnumerable<LoginEventDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("audit/login/last")]
    public async Task<IEnumerable<LoginEventDto>> GetLastLoginEvents()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return (await loginEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20)).Select(x => new LoginEventDto(x, apiDateTimeHelper));
    }

    /// <summary>
    /// Returns a list of the latest changes (creation, modification, deletion, etc.) made by users to the entities on the portal.
    /// </summary>
    /// <short>
    /// Get audit trail data
    /// </short>
    /// <path>api/2.0/security/audit/events/last</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "List of audit trail data", typeof(IEnumerable<AuditEventDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("audit/events/last")]
    public async Task<IEnumerable<AuditEventDto>> GetLastAuditEvents()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>();

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.AuditTrailLifeTime));

        return (await auditEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20, from: from, to: to))
            .Select(x => new AuditEventDto(x, auditActionMapper, apiDateTimeHelper));
    }

    /// <summary>
    /// Returns a list of the login events by the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Get filtered login events
    /// </short>
    /// <path>api/2.0/security/audit/login/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "List of filtered login events", typeof(IEnumerable<LoginEventDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("audit/login/filter")]
    public async Task<IEnumerable<LoginEventDto>> GetLoginEventsByFilter(LoginEventRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var startIndex = (int)apiContext.StartIndex;
        var limit = (int)apiContext.Count;
        apiContext.SetDataPaginated();

        inDto.Action = inDto.Action == 0 ? MessageAction.None : inDto.Action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast()))
        {
            return await GetLastLoginEvents();
        }

        await DemandAuditPermissionAsync();

        return (await loginEventsRepository.GetByFilterAsync(inDto.UserId, inDto.Action, inDto.From, inDto.To, startIndex, limit)).Select(x => new LoginEventDto(x, apiDateTimeHelper));
    }

    /// <summary>
    /// Returns a list of the audit events by the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Get filtered audit trail data
    /// </short>
    /// <path>api/2.0/security/audit/events/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "List of filtered audit trail data", typeof(IEnumerable<AuditEventDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("audit/events/filter")]
    public async Task<IEnumerable<AuditEventDto>> GetAuditEventsByFilter(AuditEventRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var startIndex = (int)apiContext.StartIndex;
        var limit = (int)apiContext.Count;
        apiContext.SetDataPaginated();

        inDto.Action = inDto.Action == 0 ? MessageAction.None : inDto.Action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast()))
        {
            return await GetLastAuditEvents();
        }

        await DemandAuditPermissionAsync();

        return (await auditEventsRepository.GetByFilterAsync(inDto.UserId, inDto.ProductType, inDto.ModuleType, inDto.ActionType, inDto.Action, inDto.EntryType, inDto.Target, inDto.From, inDto.To, startIndex, limit)).Select(x => new AuditEventDto(x, auditActionMapper, apiDateTimeHelper));
    }

    /// <summary>
    /// Returns all the available audit trail types.
    /// </summary>
    /// <short>
    /// Get audit trail types
    /// </short>
    /// <path>api/2.0/security/audit/types</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit trail types", typeof(object))]
    [AllowAnonymous]
    [HttpGet("audit/types")]
    public object GetAuditTrailTypes()
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
    /// <path>api/2.0/security/audit/mappers</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit trail mappers", typeof(object))]
    [AllowAnonymous]
    [HttpGet("audit/mappers")]
    public object GetAuditTrailMappers(AuditTrailTypesRequestDto inDto)
    {
        return auditActionMapper.Mappers
            .Where(r => !inDto.ProductType.HasValue || r.Product == inDto.ProductType.Value)
            .Select(r => new
            {
                ProductType = r.Product.ToStringFast(),
                Modules = r.Mappers
                .Where(m => !inDto.ModuleType.HasValue || m.Module == inDto.ModuleType.Value)
                .Select(x => new
                {
                    ModuleType = x.Module.ToStringFast(),
                    Actions = x.Actions.Select(a => new
                    {
                        MessageAction = a.Key.ToString(),
                        ActionType = a.Value.ActionType.ToStringFast(),
                        Entity = a.Value.EntryType1.ToStringFast()
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
    /// <path>api/2.0/security/audit/login/report</path>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "URL to the xlsx report file", typeof(string))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("audit/login/report")]
    public async Task<string> CreateLoginHistoryReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.LoginHistoryLifeTime));

        var reportName = string.Format(AuditReportResource.LoginHistoryReportName + ".csv", from.ToShortDateString(), to.ToShortDateString());
        var events = await loginEventsRepository.GetByFilterAsync(fromDate: from, to: to);

        await using var stream = csvFileHelper.CreateFile(events, new BaseEventMap<LoginEvent>());
        var result = await csvFileUploader.UploadFile(stream, reportName);

        messageService.Send(MessageAction.LoginHistoryReportDownloaded);
        return result;
    }

    /// <summary>
    /// Generates the audit trail report.
    /// </summary>
    /// <short>
    /// Generate the audit trail report
    /// </short>
    /// <path>api/2.0/security/audit/events/report</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "URL to the xlsx report file", typeof(string))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("audit/events/report")]
    public async Task<string> CreateAuditTrailReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantId);

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.AuditTrailLifeTime));

        var reportName = string.Format(AuditReportResource.AuditTrailReportName + ".csv", from.ToString("MM.dd.yyyy", CultureInfo.InvariantCulture), to.ToString("MM.dd.yyyy"));

        var events = await auditEventsRepository.GetByFilterAsync(from: from, to: to);

        await using var stream = csvFileHelper.CreateFile(events, new BaseEventMap<AuditEvent>());
        var result = await csvFileUploader.UploadFile(stream, reportName);

        messageService.Send(MessageAction.AuditTrailReportDownloaded);
        return result;
    }

    /// <summary>
    /// Returns the audit trail settings.
    /// </summary>
    /// <short>
    /// Get the audit trail settings
    /// </short>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit settings", typeof(TenantAuditSettings))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("audit/settings/lifetime")]
    public async Task<TenantAuditSettings> GetAuditSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());
    }

    /// <summary>
    /// Sets the audit trail settings for the current portal.
    /// </summary>
    /// <short>
    /// Set the audit trail settings
    /// </short>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit trail settings", typeof(TenantAuditSettings))]
    [SwaggerResponse(400, "Exception in LoginHistoryLifeTime or AuditTrailLifeTime")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
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

        await settingsManager.SaveAsync(inDto.Settings, tenantManager.GetCurrentTenantId());
        messageService.Send(MessageAction.AuditSettingsUpdated);

        return inDto.Settings;
    }

    /// <summary>
    /// Configures the CSP (Content Security Policy) settings for the current portal.
    /// </summary>
    /// <short>
    /// Configure CSP settings
    /// </short>
    /// <path>api/2.0/security/csp</path>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "Ok", typeof(CspDto))]
    [SwaggerResponse(400, "Exception in Domains")]
    [EnableCors(PolicyName = CorsPoliciesEnums.AllowAllCorsPolicyName )]
    [HttpPost("csp")]
    public async Task<CspDto> ConfigureCsp(CspRequestsDto request)
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

    /// <summary>
    /// Returns the CSP (Content Security Policy) settings for the current portal.
    /// </summary>
    /// <short>
    /// Get CSP settings
    /// </short>
    /// <path>api/2.0/security/csp</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "Ok", typeof(CspDto))]
    [AllowAnonymous]
    [EnableCors(PolicyName = CorsPoliciesEnums.AllowAllCorsPolicyName)]
    [HttpGet("csp")]
    public async Task<CspDto> GetCspSettings()
    {
        //await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        
        var settings = await cspSettingsHelper.LoadAsync(HttpContext.GetIfModifiedSince());

        if (HttpContext.TryGetFromCache(settings.LastModified))
        {
            return null;
        }
        
        return new CspDto
        {
            Domains = settings.Domains ?? [],
            Header = await cspSettingsHelper.CreateHeaderAsync(settings.Domains)
        };
    }

    /// <summary>
    /// Generates a JWT token for communication between login (client) and identity services.
    /// </summary>
    /// <short>
    /// Generate JWT token
    /// </short>
    /// <path>api/2.0/security/oauth2/token</path>
    [Tags("Security / OAuth2")]
    [HttpGet("oauth2/token")]
    [SwaggerResponse(200, "Jwt Token", typeof(string))]
    public async Task<string> GenerateJwtToken()
    {
        return await identityClient.GenerateJwtTokenAsync();
    }
    

    private async Task DemandAuditPermissionAsync()
    {
        if (!coreBaseSettings.Standalone
            && (!SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast())
                || !(await tenantManager.GetCurrentTenantQuotaAsync()).Audit))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    private void DemandBaseAuditPermission()
    {
        if (!coreBaseSettings.Standalone
            && !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast()))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }
}
