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

using ASC.Api.Core.Cors.Enums;
using ASC.Core.Common.Identity;
using ASC.Files.Core.ApiModels.ResponseDto;
using ASC.Files.Core.IntegrationEvents.Events;
using ASC.Files.Core.Services.DocumentBuilderService;

using Microsoft.AspNetCore.Cors;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal security: the audit trail and the login history the portal records, the reports built out of them and the
/// lifetimes that bound the reports and the recent events, the Content Security Policy the portal serves to browsers,
/// and the JWT that identifies the caller to the identity service. Every audit operation needs the portal-settings right of a DocSpace
/// administrator, and in a cloud installation the matching section has to be enabled for the portal as well: the two
/// operations ending in `last` need nothing more, while filtering, reports and lifetime changes also need the audit
/// option of the portal's pricing plan, and the filtering operations quietly fall back to the twenty most recent
/// events when that option is missing. Reports are built in the background, one per caller and kind, and land in the
/// caller's My documents section. The connections a user currently has open live under
/// `api/2.0/security/activeconnections`, and every other portal setting under `api/2.0/settings`.
/// </remarks>
/// <name>security</name>
[Scope]
[ApiEndpoint("security")]
public class SecurityController(
    PermissionContext permissionContext,
        TenantManager tenantManager,
        MessageService messageService,
        LoginEventsRepository loginEventsRepository,
        AuditEventsRepository auditEventsRepository,
        SettingsManager settingsManager,
        AuditActionMapper auditActionMapper,
        CoreBaseSettings coreBaseSettings,
        CspSettingsHelper cspSettingsHelper,
        ApiDateTimeHelper apiDateTimeHelper,
        IdentityClient identityClient,
        SecurityContext securityContext,
        CommonLinkUtility commonLinkUtility,
        IEventBus eventBus,
        DocumentBuilderTaskManager<AuditReportTask, int, AuditReportTaskData> documentBuilderTaskManager,
        IServiceProvider serviceProvider)
    : ControllerBase
{
    /// <remarks>
    /// Returns the 20 most recent login events of the portal, newest first. The list includes successful sign-ins,
    /// sign-outs, and failed attempts. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. In a cloud installation, the call returns 402 if the login
    /// history and audit trail section is turned off. The call is read-only. In `actionText`, the call cuts every
    /// inserted value to 50 characters. An empty list means the portal has recorded no login events. To filter by
    /// user, action, or period, or to page through the history, use `GET api/2.0/security/audit/login/filter`.
    /// </remarks>
    /// <summary>
    /// Get recent login events
    /// </summary>
    /// <path>api/2.0/security/audit/login/last</path>
    /// <collection>list</collection>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "The twenty most recent login events of the portal, newest first", typeof(IEnumerable<LoginEventDto>))]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/login/last")]
    public async Task<IEnumerable<LoginEventDto>> GetLastLoginEvents()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return (await loginEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20, limitedActionText: true))
            .Select(x => new LoginEventDto(x, apiDateTimeHelper));
    }

    /// <remarks>
    /// Returns the 20 most recent audit events of the portal, newest first. Audit events record what members created,
    /// changed, deleted, shared, or set. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. In a cloud installation, the call returns 402 if the login
    /// history and audit trail section is turned off. The call is read-only. It looks back
    /// only as far as the audit trail lifetime of `GET api/2.0/security/audit/settings/lifetime`. In `actionText`,
    /// the call cuts every inserted value to 50 characters. An empty list means the portal recorded no audit events
    /// in that period. To filter by user, module, action, or period, use `GET api/2.0/security/audit/events/filter`.
    /// </remarks>
    /// <summary>
    /// Get recent audit events
    /// </summary>
    /// <path>api/2.0/security/audit/events/last</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The twenty most recent audit events of the portal, newest first", typeof(IEnumerable<AuditEventDto>))]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/events/last")]
    public async Task<IEnumerable<AuditEventDto>> GetLastAuditEvents()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>();

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.AuditTrailLifeTime));

        return (await auditEventsRepository.GetByFilterAsync(startIndex: 0, limit: 20, from: from, to: to, limitedActionText: true))
            .Select(x => new AuditEventDto(x, auditActionMapper, apiDateTimeHelper));
    }

    /// <remarks>
    /// Returns the login events of the portal that match the query filters, newest first. You can filter by user,
    /// login action, and period. An omitted filter matches everything. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. Without the audit option of the
    /// pricing plan, the call ignores the filters. It then returns the 20 most recent events. In a cloud installation,
    /// the call returns 402 if the login history and audit trail section is turned off. The call is read-only. It applies the filters first and the
    /// page window of `startIndex` and `count` after them. So a full page means more matching events may exist, and
    /// there is no total count. To get the values of `action`, use `GET api/2.0/security/audit/types`. The call
    /// returns 400 for a parameter of the wrong type, a `count` out of range, or an invalid date.
    /// </remarks>
    /// <summary>
    /// Get filtered login events
    /// </summary>
    /// <path>api/2.0/security/audit/login/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "Login events matching the filters, newest first, or the twenty most recent events when the portal has no audit option", typeof(IEnumerable<LoginEventDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or `from` or `to` is not a date and time ending in `Z` or a UTC offset")]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/login/filter")]
    public async Task<IEnumerable<LoginEventDto>> GetLoginEventsByFilter(LoginEventRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.Action = inDto.Action == 0 ? MessageAction.None : inDto.Action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast()))
        {
            return await GetLastLoginEvents();
        }

        await DemandAuditPermissionAsync();

        return (await loginEventsRepository.GetByFilterAsync(inDto.UserId, inDto.Action, inDto.From, inDto.To, inDto.StartIndex, inDto.Count)).Select(x => new LoginEventDto(x, apiDateTimeHelper));
    }

    /// <remarks>
    /// Returns the audit events of the portal that match the query filters, newest first. You can filter by user,
    /// module, action, action type, entry type, target, and period. An omitted filter matches everything. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// Without the audit option of the pricing plan, the call ignores the filters. It then returns the 20 most
    /// recent events. In a cloud installation, the call returns 402 if the login history and audit trail section is turned
    /// off. The call is read-only. To get the filter values, use `GET api/2.0/security/audit/types` and
    /// `GET api/2.0/security/audit/mappers`. If `action` is set, the call matches only that action and ignores
    /// `moduleType` and `actionType`. With `action`, the call matches `target` exactly. With `entryType`, it keeps
    /// the events whose `target` contains the value. It ignores `entryType` without `target`, and `target` without
    /// `action` or `entryType`. The call applies the filters first and the page
    /// window after them. So a full page means more matching events may exist, and there is no total count. The audit
    /// trail lifetime does not limit this call. Without `from`, it returns events back to the oldest one the portal
    /// stores. The call returns 400 for a parameter of the wrong type, a `count` out of range, or an invalid date.
    /// </remarks>
    /// <summary>
    /// Get filtered audit events
    /// </summary>
    /// <path>api/2.0/security/audit/events/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit events matching the filters, newest first, or the twenty most recent events when the portal has no audit option", typeof(IEnumerable<AuditEventDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or `from` or `to` is not a date and time ending in `Z` or a UTC offset")]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/events/filter")]
    public async Task<IEnumerable<AuditEventDto>> GetAuditEventsByFilter(AuditEventRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.Action = inDto.Action == 0 ? MessageAction.None : inDto.Action;

        if (!(await tenantManager.GetCurrentTenantQuotaAsync()).Audit || !SetupInfo.IsVisibleSettings(ManagementType.LoginHistory.ToStringFast()))
        {
            return await GetLastAuditEvents();
        }

        await DemandAuditPermissionAsync();

        return (await auditEventsRepository.GetByFilterAsync(inDto.UserId, inDto.LocationType, inDto.ActionType, inDto.Action, inDto.EntryType, inDto.Target, inDto.From, inDto.To, inDto.StartIndex, inDto.Count)).Select(x => new AuditEventDto(x, auditActionMapper, apiDateTimeHelper));
    }

    /// <remarks>
    /// Returns the names that the audit filters accept, as five flat lists. The lists are `actions`, `actionTypes`,
    /// `productTypes`, `moduleTypes`, and `entryTypes`. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get 403. The call needs neither the audit option of the pricing
    /// plan nor the login history and audit trail section. The call is read-only and takes no parameters. The names
    /// change only with the portal version, so you can read them once and reuse them. Send them as the filter values
    /// of `GET api/2.0/security/audit/events/filter` and `GET api/2.0/security/audit/login/filter`. To see which
    /// module records which action, use `GET api/2.0/security/audit/mappers`.
    /// </remarks>
    /// <summary>
    /// Get audit trail types
    /// </summary>
    /// <path>api/2.0/security/audit/types</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The action, action type, product, module and entry type names accepted by the audit filters", typeof(AuditTrailTypesDto))]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/types")]
    public async Task<AuditTrailTypesDto> GetAuditTrailTypes()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return new AuditTrailTypesDto
        {
            Actions = MessageActionExtensions.GetNames(),
            ActionTypes = ActionTypeExtensions.GetNames(),
            ProductTypes = ProductTypeExtensions.GetNames(),
            ModuleTypes = LocationTypeExtensions.GetNames(),
            EntryTypes = EntryTypeExtensions.GetNames()
        };
    }

    /// <remarks>
    /// Returns the audit actions as a tree of products, their modules, and the actions each module records. Pass
    /// `productType` to keep one product, and `moduleType` to keep one module in each product. Omit both to get the
    /// whole tree. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. The call needs neither the audit option of the pricing plan nor the login history and audit
    /// trail section. The call is read-only. Each action has `messageAction`, the value of the `action` filter of
    /// `GET api/2.0/security/audit/events/filter`. Its `actionType` and `entity` are the values of the `actionType`
    /// and `entryType` filters there. If no product matches `productType`, the call returns an empty list, not an
    /// error. If no module matches `moduleType`, the call returns the products with empty module lists. For flat
    /// lists of the same names, use `GET api/2.0/security/audit/types`.
    /// </remarks>
    /// <summary>
    /// Get audit trail mappers
    /// </summary>
    /// <path>api/2.0/security/audit/mappers</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The products with their modules and the actions each module can record", typeof(IEnumerable<AuditTrailProductDto>))]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/mappers")]
    public async Task<IEnumerable<AuditTrailProductDto>> GetAuditTrailMappers(AuditTrailTypesRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return auditActionMapper.Mappers
            .Where(r => !inDto.ProductType.HasValue || r.Product == inDto.ProductType.Value)
            .Select(r => new AuditTrailProductDto
            {
                ProductType = r.Product.ToStringFast(),
                Modules = r.Mappers
                .Where(m => !inDto.LocationType.HasValue || m.Location == inDto.LocationType.Value)
                .Select(x => new AuditTrailModuleDto
                {
                    ModuleType = x.Location.ToStringFast(),
                    Actions = x.Actions.Select(a => new AuditTrailActionDto
                    {
                        MessageAction = a.Key.ToString(),
                        ActionType = a.Value.ActionType.ToStringFast(),
                        Entity = a.Value.EntryType1.ToStringFast()
                    })
                })
            });
    }

    /// <remarks>
    /// Starts a background job that builds a login history report of the portal, and returns the job state. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. In
    /// a cloud installation, the call returns 402 if the pricing plan has no audit option. It also returns 402 if the
    /// login history and audit trail section is turned off. Without `from` and `to`, the report covers the login
    /// history lifetime of `GET api/2.0/security/audit/settings/lifetime`, up to now.
    /// The call moves an earlier `from` to the start of the lifetime and a future `to` to now. It ignores the other
    /// filters of `GET api/2.0/security/audit/login/filter`. The call queues work. Repeat
    /// `GET api/2.0/security/audit/login/report` until `isCompleted` is true. The portal saves the file in the
    /// caller's My documents section. An XLSX report keeps at most 200,000 of the most recent events by default, and
    /// fewer for long events. Its header says how many events it left out. A CSV report has every event of the
    /// period. If a CSV file is too large for the editor, `resultFileUrl` downloads it instead of opening it. Each
    /// caller has one job per report kind. While the job runs, a repeated call returns that job and starts no second
    /// one. After the job ends, a repeated call builds a new file. To cancel the job, use
    /// `DELETE api/2.0/security/audit/login/report`. The call returns 400 if the period ends before it starts or lies
    /// entirely outside the lifetime.
    /// </remarks>
    /// <summary>
    /// Start login history report
    /// </summary>
    /// <path>api/2.0/security/audit/login/report</path>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "The state of the queued job that builds the login history report", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the requested period ends before it starts or lies entirely outside the login history lifetime")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpPost("audit/login/report")]
    public async Task<DocumentBuilderTaskDto> CreateLoginHistoryReport(AuditReportRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        inDto ??= new AuditReportRequestDto();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        var (from, to) = ResolveReportPeriod(inDto.From, inDto.To, settings.LoginHistoryLifeTime);

        return await StartAuditReportAsync(AuditReportKind.LoginHistory, inDto.Format, from, to);
    }

    /// <remarks>
    /// Returns the state of the caller's login history report job. This is the call to poll after
    /// `POST api/2.0/security/audit/login/report`. Repeat it until `isCompleted` is true. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. In a
    /// cloud installation, the call returns 402 if the pricing plan has no audit option. It also returns 402 if the
    /// login history and audit trail section is turned off. The call is read-only and shows only the caller's own
    /// job, never the report of another admin. The audit trail report has its own status call,
    /// `GET api/2.0/security/audit/events/report`. An empty answer means the caller has no login history report job.
    /// After a failed build, `error` holds the failure message. After a successful build, `resultFileId`,
    /// `resultFileName`, and `resultFileUrl` describe the file. The portal saves the file in the caller's My
    /// documents section. After the next report of this kind starts, the call no longer shows the finished job.
    /// </remarks>
    /// <summary>
    /// Get login history report status
    /// </summary>
    /// <path>api/2.0/security/audit/login/report</path>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "The state of the caller's login history report, or an empty answer when none is known", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/login/report")]
    public async Task<DocumentBuilderTaskDto> GetLoginHistoryReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        return await GetAuditReportStatusAsync(AuditReportKind.LoginHistory);
    }

    /// <remarks>
    /// Cancels the caller's login history report job and removes it from the queue. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. In a cloud installation,
    /// the call returns 402 if the pricing plan has no audit option. It also returns 402 if the login history and
    /// audit trail section is turned off. The call queues the cancellation for the service that builds reports. So a
    /// 200 response means that the service got the request, not that the job has stopped. To check, repeat
    /// `GET api/2.0/security/audit/login/report` until it returns an empty answer. The call affects only the caller's
    /// own login history report. To cancel the audit trail report, use `DELETE api/2.0/security/audit/events/report`.
    /// You can repeat the call, and a call with no running job is not an error. A job cancelled before it saved the
    /// file leaves nothing in My documents. To build the report again, use
    /// `POST api/2.0/security/audit/login/report`.
    /// </remarks>
    /// <summary>
    /// Terminate login history report
    /// </summary>
    /// <path>api/2.0/security/audit/login/report</path>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "The cancellation of the caller's login history report has been accepted")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpDelete("audit/login/report")]
    public async Task TerminateLoginHistoryReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        await TerminateAuditReportAsync(AuditReportKind.LoginHistory);
    }

    /// <remarks>
    /// Starts a background job that builds an audit trail report of the portal, and returns the job state. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. In
    /// a cloud installation, the call returns 402 if the pricing plan has no audit option. It also returns 402 if the
    /// login history and audit trail section is turned off. Without `from` and `to`, the report covers the audit trail
    /// lifetime of `GET api/2.0/security/audit/settings/lifetime`, up to now. The
    /// call moves an earlier `from` to the start of the lifetime and a future `to` to now. It ignores the other
    /// filters of `GET api/2.0/security/audit/events/filter`. The call queues work. Repeat
    /// `GET api/2.0/security/audit/events/report` until `isCompleted` is true. The portal saves the file in the
    /// caller's My documents section. An XLSX report keeps at most 200,000 of the most recent events by default, and
    /// fewer for long events. Its header says how many events it left out. A CSV report has every event of the
    /// period. If a CSV file is too large for the editor, `resultFileUrl` downloads it instead of opening it. Each
    /// caller has one job per report kind. While the job runs, a repeated call returns that job and starts no second
    /// one. After the job ends, a repeated call builds a new file. To cancel the job, use
    /// `DELETE api/2.0/security/audit/events/report`. The call returns 400 if the period ends before it starts or
    /// lies entirely outside the lifetime.
    /// </remarks>
    /// <summary>
    /// Start audit trail report
    /// </summary>
    /// <path>api/2.0/security/audit/events/report</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The state of the queued job that builds the audit trail report", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the requested period ends before it starts or lies entirely outside the audit trail lifetime")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpPost("audit/events/report")]
    public async Task<DocumentBuilderTaskDto> CreateAuditTrailReport(AuditReportRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        inDto ??= new AuditReportRequestDto();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        var (from, to) = ResolveReportPeriod(inDto.From, inDto.To, settings.AuditTrailLifeTime);

        return await StartAuditReportAsync(AuditReportKind.AuditTrail, inDto.Format, from, to);
    }

    /// <remarks>
    /// Returns the state of the caller's audit trail report job. This is the call to poll after
    /// `POST api/2.0/security/audit/events/report`. Repeat it until `isCompleted` is true. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. In a
    /// cloud installation, the call returns 402 if the pricing plan has no audit option. It also returns 402 if the
    /// login history and audit trail section is turned off. The call is read-only and shows only the caller's own
    /// job, never the report of another admin. The login history report has its own status call,
    /// `GET api/2.0/security/audit/login/report`. An empty answer means the caller has no audit trail report job.
    /// After a failed build, `error` holds the failure message. After a successful build, `resultFileId`,
    /// `resultFileName`, and `resultFileUrl` describe the file. The portal saves the file in the caller's My
    /// documents section. After the next report of this kind starts, the call no longer shows the finished job.
    /// </remarks>
    /// <summary>
    /// Get audit trail report status
    /// </summary>
    /// <path>api/2.0/security/audit/events/report</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The state of the caller's audit trail report, or an empty answer when none is known", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/events/report")]
    public async Task<DocumentBuilderTaskDto> GetAuditTrailReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        return await GetAuditReportStatusAsync(AuditReportKind.AuditTrail);
    }

    /// <remarks>
    /// Cancels the caller's audit trail report job and removes it from the queue. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. In a cloud installation,
    /// the call returns 402 if the pricing plan has no audit option. It also returns 402 if the login history and
    /// audit trail section is turned off. The call queues the cancellation for the service that builds reports. So a
    /// 200 response means that the service got the request, not that the job has stopped. To check, repeat
    /// `GET api/2.0/security/audit/events/report` until it returns an empty answer. The call affects only the
    /// caller's own audit trail report. To cancel the login history report, use
    /// `DELETE api/2.0/security/audit/login/report`. You can repeat the call, and a call with no running job is not
    /// an error. A job cancelled before it saved the file leaves nothing in My documents. To build the report again,
    /// use `POST api/2.0/security/audit/events/report`.
    /// </remarks>
    /// <summary>
    /// Terminate audit trail report
    /// </summary>
    /// <path>api/2.0/security/audit/events/report</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The cancellation of the caller's audit trail report has been accepted")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpDelete("audit/events/report")]
    public async Task TerminateAuditTrailReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        await TerminateAuditReportAsync(AuditReportKind.AuditTrail);
    }

    // A report never reaches past the lifetime of its log. Login events older than that are purged anyway; audit
    // events are not, they stay as long as the installation's retention keeps them. For both, the window is what
    // keeps an unbounded request from exporting everything the portal still holds.
    private static (DateTime From, DateTime To) ResolveReportPeriod(DateTime? from, DateTime? to, int lifeTimeDays)
    {
        var now = DateTime.UtcNow;
        var lowerBound = now.Subtract(TimeSpan.FromDays(lifeTimeDays));

        var resolvedTo = to.HasValue && to.Value < now ? to.Value : now;
        var resolvedFrom = from.HasValue && from.Value > lowerBound ? from.Value : lowerBound;

        if (resolvedFrom > resolvedTo)
        {
            throw new ArgumentException("The report period ends before it starts or lies outside the lifetime of the log");
        }

        return (resolvedFrom, resolvedTo);
    }

    private async Task<DocumentBuilderTaskDto> StartAuditReportAsync(AuditReportKind kind, AuditReportFormat format, DateTime from, DateTime to)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<AuditReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, AuditReportTask.GetTaskDiscriminator(kind)));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new AuditReportIntegrationEvent(userId, tenantId, baseUri, kind, format, from, to, headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    private async Task<DocumentBuilderTaskDto> GetAuditReportStatusAsync(AuditReportKind kind)
    {
        var task = await documentBuilderTaskManager.GetTask(tenantManager.GetCurrentTenantId(), securityContext.CurrentAccount.ID, AuditReportTask.GetTaskDiscriminator(kind));

        return DocumentBuilderTaskDto.Get(task);
    }

    private async Task TerminateAuditReportAsync(AuditReportKind kind)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = securityContext.CurrentAccount.ID;

        var evt = new AuditReportIntegrationEvent(userId, tenantId, null, kind, AuditReportFormat.Xlsx, default, default, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Returns the lifetimes of the login history and the audit trail, in days. The portal deletes login events older
    /// than the login history lifetime. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. In a cloud installation, the call returns 402 if the login history
    /// and audit trail section is turned off. The call does not need the audit option of the pricing plan. The call
    /// is read-only. The audit trail lifetime sets how far `GET api/2.0/security/audit/events/last` and the audit
    /// trail report look back. The login history lifetime does the same for the login history report. The audit
    /// trail lifetime deletes no events and does not limit `GET api/2.0/security/audit/events/filter`. The
    /// installation decides how long it stores audit events. To change the lifetimes, use
    /// `POST api/2.0/security/audit/settings/lifetime`.
    /// </remarks>
    /// <summary>
    /// Get audit lifetime settings
    /// </summary>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The login history and audit trail lifetimes of the portal, in days", typeof(TenantAuditSettingsDto))]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/settings/lifetime")]
    public async Task<TenantAuditSettingsDto> GetAuditSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        return settings.Map();
    }

    /// <remarks>
    /// Sets the lifetimes of the login history and the audit trail, in days, and returns the stored values.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. In a cloud installation, the call returns 402 if the pricing plan has no audit option. It also
    /// returns 402 if the login history and audit trail section is turned off. The call changes data and replaces
    /// both values. So send both in `settings`, with the current value for the one that stays. To read the current
    /// values, use `GET api/2.0/security/audit/settings/lifetime`. You can repeat the call with the same body. The
    /// change takes effect immediately for `GET api/2.0/security/audit/events/last` and for both reports. A shorter
    /// login history lifetime lets the daily cleanup permanently delete older login events. A shorter audit trail
    /// lifetime deletes nothing. Older audit events stay in `GET api/2.0/security/audit/events/filter`. They return
    /// to the recent events and the report when the lifetime grows. The call
    /// returns 400 if the body has no `settings` or a lifetime is outside 1 to 180 days. It then saves neither value.
    /// </remarks>
    /// <summary>
    /// Set audit lifetime settings
    /// </summary>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The login history and audit trail lifetimes as they were stored", typeof(TenantAuditSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `settings`, or a lifetime is outside the allowed range of 1 to 180 days")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpPost("audit/settings/lifetime")]
    public async Task<TenantAuditSettingsDto> SetAuditSettings(TenantAuditSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        if (inDto?.Settings?.LoginHistoryLifeTime is not (> 0 and <= TenantAuditSettings.MaxLifeTime))
        {
            throw new ArgumentException("LoginHistoryLifeTime");
        }

        if (inDto.Settings.AuditTrailLifeTime is <= 0 or > TenantAuditSettings.MaxLifeTime)
        {
            throw new ArgumentException("AuditTrailLifeTime");
        }

        var settings = new TenantAuditSettings
        {
            LoginHistoryLifeTime = inDto.Settings.LoginHistoryLifeTime,
            AuditTrailLifeTime = inDto.Settings.AuditTrailLifeTime
        };

        await settingsManager.SaveAsync(settings, tenantManager.GetCurrentTenantId());
        messageService.Send(MessageAction.AuditSettingsUpdated);

        return (await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId())).Map();
    }

    /// <remarks>
    /// Replaces the list of external domains that the Content Security Policy of the portal trusts. It returns the
    /// stored list and the policy header that browsers now get. Send the complete list in `domains`. An empty or
    /// omitted list restores the built-in policy of the portal. Every domain becomes an allowed source for scripts,
    /// styles, images, fonts, frames, media, and connections. Requires the EditPortalSettings permission, which the
    /// portal owner and DocSpace admins have. Other callers get 403. The call changes data for the whole portal, and
    /// you can repeat it with the same body. The call returns 400 for an unreadable body, an invalid address, or an
    /// entry with non-ASCII characters. It then saves nothing. It also returns 403 if the built header is larger than
    /// the installation allows, 15 KB by default. To read the current policy, use `GET api/2.0/security/csp`.
    /// </remarks>
    /// <summary>
    /// Configure CSP settings
    /// </summary>
    /// <path>api/2.0/security/csp</path>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "The stored domains and the policy header the portal now serves", typeof(CspDto))]
    [SwaggerResponse(400, "The request body cannot be read, or an entry of `domains` is not a valid address or holds non-ASCII characters")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator, or the built policy header exceeds the size allowed for the installation")]
    [EnableCors(PolicyName = CorsPoliciesEnums.AllowAllCorsPolicyName)]
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

                if (!Uri.TryCreate(uriString, UriKind.Absolute, out _) || Encoding.UTF8.GetByteCount(domain) != domain.Length)
                {
                    throw new ArgumentException(domain, nameof(request.Domains));
                }
            }
        }

        var header = await cspSettingsHelper.SaveAsync(request.Domains);

        return new CspDto { Domains = request.Domains, Header = header };
    }

    /// <remarks>
    /// Returns the Content Security Policy of the portal, with the allowed external domains and the full header
    /// value. Anyone can call it without signing in, from any origin. The call is read-only. To skip an unchanged
    /// policy, send the `Last-Modified` value of an earlier answer in `If-Modified-Since`. Send the value exactly as
    /// you got it. If the policy has not changed since then, the call returns 304 with an empty body. To change the
    /// allowed domains, use `POST api/2.0/security/csp`, which requires the EditPortalSettings permission.
    /// </remarks>
    /// <summary>
    /// Get CSP settings
    /// </summary>
    /// <path>api/2.0/security/csp</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "The allowed domains and the full policy header the portal serves", typeof(CspDto))]
    [SwaggerResponse(304, "The CSP settings have not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty")]
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

        if (!await cspSettingsHelper.ExistsInCacheAsync())
        {
            await cspSettingsHelper.SaveAsync(settings.Domains, false);
        }

        return new CspDto
        {
            Domains = settings.Domains ?? [],
            Header = await cspSettingsHelper.CreateHeaderAsync(settings.Domains)
        };
    }

    /// <remarks>
    /// Issues a JWT that identifies the caller to the identity service. That service stores the OAuth2 apps of the
    /// installation. Any signed-in user can call it, including a guest. The token always describes the caller, never
    /// another user. The installation signs it with its own key, and it expires after five minutes. The token
    /// carries the user ID, name, and email, and the portal ID and address. It also says if the caller is a DocSpace
    /// admin or a guest. Last, it says if the developer tools setting opens OAuth2 apps to all users. Send the token
    /// to the identity service in the `x-signature` header. This API does not accept it instead of the token of
    /// `POST api/2.0/authentication`. The call is read-only and returns the token as a plain string. Request a new
    /// token before each exchange instead of storing it.
    /// </remarks>
    /// <summary>
    /// Generate JWT token
    /// </summary>
    /// <path>api/2.0/security/oauth2/token</path>
    [Tags("Security / OAuth2")]
    [HttpGet("oauth2/token")]
    [SwaggerResponse(200, "The signed JWT identifying the caller, valid for five minutes", typeof(string))]
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
