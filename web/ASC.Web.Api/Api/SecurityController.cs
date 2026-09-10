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
/// retention that bounds both, the Content Security Policy the portal serves to browsers, and the JWT that identifies
/// the caller to the identity service. Every audit operation needs the portal-settings right of a DocSpace
/// administrator, and in a cloud installation the matching section has to be enabled for the portal as well: the two
/// operations ending in `last` need nothing more, while filtering, reports and retention changes also need the audit
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
    /// Returns the twenty most recent login events of the whole portal - successful sign-ins, sign-outs and failed
    /// attempts alike - as the short summary a settings page shows before anyone asks for the full history. The
    /// caller needs the portal-settings right of a DocSpace administrator, and in a cloud installation the login
    /// history and audit trail section must be enabled for the portal, otherwise the call is answered with 402. The
    /// operation is read-only and takes no parameters: the number of events is fixed at twenty, nothing can be
    /// filtered, and events are ordered newest first. `date` is given in the portal time zone, `actionText` is the
    /// readable sentence describing the event with every substituted value shortened to fifty characters here, and
    /// `country` and `city` are resolved from the IP address and stay empty when it cannot be located. An empty list
    /// means the portal has recorded no login events yet. Use `GET api/2.0/security/audit/login/filter` to filter by
    /// user, action or period and to page through the whole history.
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
    /// Returns the twenty most recent audit events of the portal - the creations, changes, deletions, sharing and
    /// settings updates its members made - as the short summary a settings page shows before anyone asks for the full
    /// trail. The caller needs the portal-settings right of a DocSpace administrator, and in a cloud installation the
    /// login history and audit trail section must be enabled for the portal, otherwise the call is answered with 402.
    /// The operation is read-only and takes no parameters: it looks back exactly as far as the audit trail lifetime
    /// that `GET api/2.0/security/audit/settings/lifetime` reports, returns at most twenty events ordered newest
    /// first, and cannot be filtered. `date` is given in the portal time zone, `actionText` is the readable sentence
    /// describing the event with every substituted value shortened to fifty characters here, and `target` names the
    /// entity the action was applied to. An empty list means nothing was recorded inside that period. Use
    /// `GET api/2.0/security/audit/events/filter` to filter by user, module, action or period.
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
    /// Returns the portal's login events that match the filters in the query - by user, by login action and by period
    /// - and is the operation behind the login history page. The caller needs the portal-settings right of a DocSpace
    /// administrator plus the audit option of the portal's pricing plan; when that option is missing the filters are
    /// silently ignored and the answer is the same twenty most recent events that
    /// `GET api/2.0/security/audit/login/last` returns, and when the login history and audit trail section is
    /// disabled altogether the call is answered with 402. Omit a filter to match everything. `from` and `to` are read
    /// as UTC instants while `date` comes back in the portal time zone, `count` defaults to 100 and cannot exceed it,
    /// `startIndex` skips events from the newest end, and the page window is applied to the log before the filters,
    /// so a page can hold fewer items than `count` while older matches still exist. The operation is read-only; take
    /// the values accepted by `action` from `GET api/2.0/security/audit/types`.
    /// </remarks>
    /// <summary>
    /// Get filtered login events
    /// </summary>
    /// <path>api/2.0/security/audit/login/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "Login events matching the filters, newest first, or the twenty most recent events when the portal has no audit option", typeof(IEnumerable<LoginEventDto>))]
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
    /// Returns the portal's audit events that match the filters in the query - by the user who acted, the module the
    /// action belongs to, the action and its type, the entity type and target, and the period - and is the operation
    /// behind the audit trail page. The caller needs the portal-settings right of a DocSpace administrator plus the
    /// audit option of the portal's pricing plan; when that option is missing the filters are silently ignored and
    /// the answer is the same twenty most recent events that `GET api/2.0/security/audit/events/last` returns, and
    /// when the login history and audit trail section is disabled altogether the call is answered with 402. Take the
    /// values accepted by `action`, `actionType`, `moduleType` and `entryType` from
    /// `GET api/2.0/security/audit/types`, and the tree they belong to from `GET api/2.0/security/audit/mappers`. A
    /// non-default `action` matches only that action and, combined with `target`, only its exact value; it also
    /// stops `moduleType` and `actionType` from narrowing the result, so combine `target` with `entryType` instead of
    /// `action` when filtering by target without pinning a single action. `from` and `to` are read as UTC instants
    /// while `date` comes back in the portal time zone, `count` defaults to 100 and cannot exceed it, and the filters
    /// are applied before the page window, so a full page means there may be more matching events beyond it. The
    /// operation is read-only.
    /// </remarks>
    /// <summary>
    /// Get filtered audit events
    /// </summary>
    /// <path>api/2.0/security/audit/events/filter</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "Audit events matching the filters, newest first, or the twenty most recent events when the portal has no audit option", typeof(IEnumerable<AuditEventDto>))]
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
    /// Returns the vocabularies the audit filters are built from: `actions` lists every action the portal can record,
    /// `actionTypes` the kinds of change they stand for, `productTypes` the products they belong to, `moduleTypes`
    /// the locations inside those products, and `entryTypes` the kinds of entity an action can be applied to. The
    /// caller needs the portal-settings right of a DocSpace administrator; the audit option of the pricing plan is
    /// not required, so the lists can be read on any portal. The operation is read-only, takes no parameters and
    /// depends on nothing else. Every value is the name to send in the matching query parameter of
    /// `GET api/2.0/security/audit/events/filter` or `GET api/2.0/security/audit/login/filter`, so read this
    /// operation once and reuse the answer instead of guessing spellings. The response is an untyped object holding
    /// those five arrays of names, and it changes only with the portal version. Use
    /// `GET api/2.0/security/audit/mappers` when the relations between products, modules and actions are needed
    /// rather than the flat lists.
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
    /// Returns the audit vocabulary as the tree it really is: every product, the modules inside it, and for each
    /// module the actions it can record together with the type of change and the entity each of them applies to. Pass
    /// `productType` to keep a single product and `moduleType` to keep a single module inside the products that
    /// remain; omit both to get the whole tree. The caller needs the portal-settings right of a DocSpace
    /// administrator; the audit option of the pricing plan is not required, and the call is read-only and safe to
    /// repeat. Each action carries `messageAction`, the name to send as the `action` filter of
    /// `GET api/2.0/security/audit/events/filter`, next to `actionType` and `entity`, the values its `actionType` and
    /// `entryType` filters accept - this is where a caller learns which action belongs to which module instead of
    /// guessing. A filter that matches nothing yields an empty list rather than an error. Use
    /// `GET api/2.0/security/audit/types` for the flat lists of the same names.
    /// </remarks>
    /// <summary>
    /// Get audit trail mappers
    /// </summary>
    /// <path>api/2.0/security/audit/mappers</path>
    /// <collection>list</collection>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The products with their modules and the actions each module can record", typeof(IEnumerable<AuditTrailProductMapperDto>))]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/mappers")]
    public async Task<IEnumerable<AuditTrailProductMapperDto>> GetAuditTrailMappers(AuditTrailTypesRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return auditActionMapper.Mappers
            .Where(r => !inDto.ProductType.HasValue || r.Product == inDto.ProductType.Value)
            .Select(r => new AuditTrailProductMapperDto
            {
                ProductType = r.Product.ToStringFast(),
                Modules = r.Mappers
                .Where(m => !inDto.LocationType.HasValue || m.Location == inDto.LocationType.Value)
                .Select(x => new AuditTrailModuleMapperDto
                {
                    ModuleType = x.Location.ToStringFast(),
                    Actions = x.Actions.Select(a => new AuditTrailActionMapperDto
                    {
                        MessageAction = a.Key.ToString(),
                        ActionType = a.Value.ActionType.ToStringFast(),
                        Entity = a.Value.EntryType1.ToStringFast()
                    })
                })
            });
    }

    /// <remarks>
    /// Queues a report of the portal's login history and returns the state of the background job that builds it. The
    /// report covers the period reaching from now back by the login history lifetime that
    /// `GET api/2.0/security/audit/settings/lifetime` reports and is never filtered: the query parameters of
    /// `GET api/2.0/security/audit/login/filter` do not apply here. The caller needs the portal-settings right of a
    /// DocSpace administrator plus the audit option of the portal's pricing plan, otherwise the call is answered with
    /// 402. The file is not ready when the response arrives - poll `GET api/2.0/security/audit/login/report` until
    /// `isCompleted` is true, then take `resultFileUrl`, and treat a non-empty `error` as a failed build. The
    /// finished file is saved to the caller's My documents section, as an XLSX workbook by default or as CSV when
    /// `format=Csv`, in which case `resultFileId` stays empty and only the name and the URL identify it. One job runs
    /// per caller and kind: calling again while the previous one is still building returns that job instead of
    /// starting a second, and `DELETE api/2.0/security/audit/login/report` cancels it.
    /// </remarks>
    /// <summary>
    /// Start login history report
    /// </summary>
    /// <path>api/2.0/security/audit/login/report</path>
    [Tags("Security / Login history")]
    [SwaggerResponse(200, "The state of the queued job that builds the login history report", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpPost("audit/login/report")]
    public async Task<DocumentBuilderTaskDto> CreateLoginHistoryReport(AuditReportRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.LoginHistoryLifeTime));

        return await StartAuditReportAsync(AuditReportKind.LoginHistory, (inDto ?? new AuditReportRequestDto()).Format, from, to);
    }

    /// <remarks>
    /// Returns the state of the login history report the calling user has started, and is the operation to poll after
    /// `POST api/2.0/security/audit/login/report`. The caller needs the portal-settings right of a DocSpace
    /// administrator plus the audit option of the portal's pricing plan, otherwise the call is answered with 402.
    /// Jobs are kept per user and per report kind: this operation never shows another administrator's report, nor the
    /// audit trail report, which has its own status at `GET api/2.0/security/audit/events/report`. The answer is
    /// empty when no report of this kind is known for the caller; otherwise `percentage` grows towards 100,
    /// `isCompleted` turns true when the build has ended, `error` carries the failure message when it ended badly,
    /// and `resultFileName` and `resultFileUrl` point at the file saved to the caller's My documents section, while
    /// `resultFileId` is filled for an XLSX report only. The operation is read-only and safe to poll every few
    /// seconds; a finished job is dropped as soon as the next report of this kind is started.
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
    /// Cancels the login history report the calling user has running and drops it from the build queue. The caller
    /// needs the portal-settings right of a DocSpace administrator plus the audit option of the portal's pricing
    /// plan, otherwise the call is answered with 402. Cancellation is handed to the same background service that
    /// builds the report, so a successful answer means the request was accepted rather than that the job has already
    /// stopped: poll `GET api/2.0/security/audit/login/report` to watch it disappear. The operation returns no
    /// content and touches only the caller's own login history report - the audit trail report is cancelled by
    /// `DELETE api/2.0/security/audit/events/report`, and no report of another user can be reached from here. It is
    /// idempotent: cancelling when nothing is running is not an error. A job stopped before it finished writing
    /// leaves nothing in My documents, and a report cancelled by mistake has to be built again with
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
    /// Queues a report of the portal's audit trail and returns the state of the background job that builds it. The
    /// report covers the period reaching from now back by the audit trail lifetime that
    /// `GET api/2.0/security/audit/settings/lifetime` reports and is never filtered: the query parameters of
    /// `GET api/2.0/security/audit/events/filter` do not apply here. The caller needs the portal-settings right of a
    /// DocSpace administrator plus the audit option of the portal's pricing plan, otherwise the call is answered with
    /// 402. The file is not ready when the response arrives - poll `GET api/2.0/security/audit/events/report` until
    /// `isCompleted` is true, then take `resultFileUrl`, and treat a non-empty `error` as a failed build. The
    /// finished file is saved to the caller's My documents section, as an XLSX workbook by default or as CSV when
    /// `format=Csv`, in which case `resultFileId` stays empty and only the name and the URL identify it. One job runs
    /// per caller and kind: calling again while the previous one is still building returns that job instead of
    /// starting a second, and `DELETE api/2.0/security/audit/events/report` cancels it.
    /// </remarks>
    /// <summary>
    /// Start audit trail report
    /// </summary>
    /// <path>api/2.0/security/audit/events/report</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The state of the queued job that builds the audit trail report", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpPost("audit/events/report")]
    public async Task<DocumentBuilderTaskDto> CreateAuditTrailReport(AuditReportRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandAuditPermissionAsync();

        var settings = await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());

        var to = DateTime.UtcNow;
        var from = to.Subtract(TimeSpan.FromDays(settings.AuditTrailLifeTime));

        return await StartAuditReportAsync(AuditReportKind.AuditTrail, (inDto ?? new AuditReportRequestDto()).Format, from, to);
    }

    /// <remarks>
    /// Returns the state of the audit trail report the calling user has started, and is the operation to poll after
    /// `POST api/2.0/security/audit/events/report`. The caller needs the portal-settings right of a DocSpace
    /// administrator plus the audit option of the portal's pricing plan, otherwise the call is answered with 402.
    /// Jobs are kept per user and per report kind: this operation never shows another administrator's report, nor the
    /// login history report, which has its own status at `GET api/2.0/security/audit/login/report`. The answer is
    /// empty when no report of this kind is known for the caller; otherwise `percentage` grows towards 100,
    /// `isCompleted` turns true when the build has ended, `error` carries the failure message when it ended badly,
    /// and `resultFileName` and `resultFileUrl` point at the file saved to the caller's My documents section, while
    /// `resultFileId` is filled for an XLSX report only. The operation is read-only and safe to poll every few
    /// seconds; a finished job is dropped as soon as the next report of this kind is started.
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
    /// Cancels the audit trail report the calling user has running and drops it from the build queue. The caller
    /// needs the portal-settings right of a DocSpace administrator plus the audit option of the portal's pricing
    /// plan, otherwise the call is answered with 402. Cancellation is handed to the same background service that
    /// builds the report, so a successful answer means the request was accepted rather than that the job has already
    /// stopped: poll `GET api/2.0/security/audit/events/report` to watch it disappear. The operation returns no
    /// content and touches only the caller's own audit trail report - the login history report is cancelled by
    /// `DELETE api/2.0/security/audit/login/report`, and no report of another user can be reached from here. It is
    /// idempotent: cancelling when nothing is running is not an error. A job stopped before it finished writing
    /// leaves nothing in My documents, and a report cancelled by mistake has to be built again with
    /// `POST api/2.0/security/audit/events/report`.
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
    /// Returns how long this portal keeps its two security logs: `loginHistoryLifeTime` for login events and
    /// `auditTrailLifeTime` for audit events, both counted in days, together with `lastModified`, the moment the pair
    /// was last saved. The caller needs the portal-settings right of a DocSpace administrator, and in a cloud
    /// installation the login history and audit trail section must be enabled for the portal, otherwise the call is
    /// answered with 402; the audit option of the pricing plan is not required to read the values. Both numbers lie
    /// between 1 and 180 days, and a portal that never changed them reports the default of 180. They define the
    /// window the rest of the audit operations work in: `GET api/2.0/security/audit/events/last` looks exactly this
    /// far back, and the reports started by `POST api/2.0/security/audit/login/report` and
    /// `POST api/2.0/security/audit/events/report` cover exactly this period. The operation is read-only; change the
    /// values with `POST api/2.0/security/audit/settings/lifetime`.
    /// </remarks>
    /// <summary>
    /// Get audit lifetime settings
    /// </summary>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The login history and audit trail lifetimes of the portal, in days", typeof(TenantAuditSettings))]
    [SwaggerResponse(402, "The login history and audit trail section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
    [HttpGet("audit/settings/lifetime")]
    public async Task<TenantAuditSettings> GetAuditSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        DemandBaseAuditPermission();

        return await settingsManager.LoadAsync<TenantAuditSettings>(tenantManager.GetCurrentTenantId());
    }

    /// <remarks>
    /// Sets how long this portal keeps its login history and its audit trail, in days, and returns the pair as it was
    /// stored. The caller needs the portal-settings right of a DocSpace administrator plus the audit option of the
    /// portal's pricing plan, otherwise the call is answered with 402. Send both numbers inside `settings`: each has
    /// to be between 1 and 180 days, and a value outside that range is refused with 400 without either number being
    /// saved, so read the current pair from `GET api/2.0/security/audit/settings/lifetime` and resend the one that
    /// should stay as it is. The call replaces the stored settings rather than merging them, is idempotent, and takes
    /// effect at once: the period covered by `GET api/2.0/security/audit/events/last` and by both audit reports
    /// shrinks or grows with it, and events older than the new lifetime stop being reported. The change is itself
    /// recorded in the audit trail.
    /// </remarks>
    /// <summary>
    /// Set audit lifetime settings
    /// </summary>
    /// <path>api/2.0/security/audit/settings/lifetime</path>
    [Tags("Security / Audit trail data")]
    [SwaggerResponse(200, "The login history and audit trail lifetimes as they were stored", typeof(TenantAuditSettings))]
    [SwaggerResponse(400, "A lifetime is outside the allowed range of 1 to 180 days")]
    [SwaggerResponse(402, "The portal's pricing plan has no audit option, or the login history and audit trail section is not enabled")]
    [SwaggerResponse(403, "The caller does not have the portal-settings right of a DocSpace administrator")]
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

    /// <remarks>
    /// Replaces the list of external domains the portal's Content Security Policy trusts and returns the policy
    /// header the portal serves to browsers from that moment on. The list in `domains` replaces the stored one, so an
    /// omitted or empty list falls back to the portal's built-in policy, and every entry that is sent becomes an
    /// allowed source for scripts, styles, images, fonts, frames, media and connections at once. An entry may be a
    /// host, a host with a scheme, or a wildcard host such as `*.example.com`; it has to form a valid absolute
    /// address and may contain ASCII characters only, and an entry that does not is refused with 400 before anything
    /// is saved. The caller needs the portal-settings right of a DocSpace administrator, and the request is also
    /// refused with 403 when the header built from the list grows past the size configured for the installation, 15
    /// KB by default. The change applies to the whole portal at once and is idempotent. Read the current state with
    /// `GET api/2.0/security/csp`.
    /// </remarks>
    /// <summary>
    /// Configure CSP settings
    /// </summary>
    /// <path>api/2.0/security/csp</path>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "The stored domains and the policy header the portal now serves", typeof(CspDto))]
    [SwaggerResponse(400, "An entry of `domains` is not a valid address or holds non-ASCII characters")]
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
    /// Returns the Content Security Policy this portal serves: `domains`, the external hosts an administrator has
    /// allowed, and `header`, the whole policy value built from them together with the portal's own defaults and the
    /// integrations it has switched on. The operation is anonymous and reachable cross-origin - no token is needed -
    /// because the login and editor front-ends read it before anyone has signed in. It is read-only for the caller,
    /// but it does repair the portal's cached policy when the cache has lost it, so a call can rebuild the header
    /// instead of only reading it. The answer honours `If-Modified-Since`: send back the `Last-Modified` value of an
    /// earlier answer and an unchanged policy comes back as an empty not-modified response rather than a body.
    /// `domains` is an empty list on a portal nobody has configured, while `header` is filled from the defaults even
    /// then. Change the allowed domains with `POST api/2.0/security/csp`, which does need a DocSpace administrator.
    /// </remarks>
    /// <summary>
    /// Get CSP settings
    /// </summary>
    /// <path>api/2.0/security/csp</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Security / CSP")]
    [SwaggerResponse(200, "The allowed domains and the full policy header the portal serves", typeof(CspDto))]
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
    /// Issues a short-lived JWT that identifies the calling user to the identity service, the component that stores
    /// the OAuth2 applications of this installation and their consents. Any signed-in user may call it, nothing has
    /// to be prepared first, and the token always describes the caller - it cannot be issued on behalf of somebody
    /// else. The token is signed with the installation's own key and carries the user ID, name and e-mail, the portal
    /// ID and address, whether the caller is an administrator or a guest, and whether the portal's developer tools
    /// setting leaves OAuth2 applications open to ordinary users. It expires five minutes after it was issued and is
    /// meant to be presented to the identity service in the `x-signature` header, not to this API: requests to the
    /// portal are authorized with the token that `POST api/2.0/authentication` returns, and this JWT is not accepted
    /// in its place. The call is read-only and gives the token back as a plain string; ask for a fresh one per
    /// exchange instead of storing it.
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
