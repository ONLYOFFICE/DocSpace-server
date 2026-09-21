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

namespace ASC.Web.Api.Controllers.Settings;

[ApiEndpoint(Template = "iprestrictions")]
public class IpRestrictionsController(
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    IPRestrictionsService iPRestrictionsService,
    IFusionCache fusionCache,
    MessageService messageService,
    TenantManager tenantManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the IP restriction list of the current portal - the addresses allowed to reach it, each with its `id`
    /// and the `forAdmin` flag that narrows the entry to DocSpace administrators. The caller needs the
    /// portal-settings right of a DocSpace administrator, otherwise the call is refused. The call is read-only and
    /// honours `If-None-Match`: send back the `ETag` of an earlier answer and an unchanged list comes back as an
    /// empty not-modified response rather than a body. The list has no defined order and is empty on a portal where
    /// nobody has configured restrictions - and an empty list blocks nobody, whatever the enforcement flag says.
    /// Whether the restrictions are enforced at all is not part of this answer: read that flag with
    /// `GET api/2.0/settings/iprestrictions/settings`. The entries listed here apply to every user of the portal
    /// except its owner. Replace the whole list with `PUT api/2.0/settings/iprestrictions`; single entries cannot be
    /// added or deleted, and that update takes plain addresses rather than the IDs returned here.
    /// </remarks>
    /// <summary>Get IP restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    /// <collection>list</collection>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The IP addresses allowed to reach the portal, each with its ID and administrators-only flag; an empty list when the portal has no restrictions", typeof(IEnumerable<IPRestriction>))]
    [HttpGet("")]
    public async Task<IEnumerable<IPRestriction>> GetIpRestrictions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var etagFromRequest = HttpContext.Request.Headers.IfNoneMatch;
        var result = await iPRestrictionsService.GetAsync(tenant.Id, etagFromRequest);

        return HttpContext.TryGetFromCache(await HttpContextExtension.CalculateEtagAsync(result.Select(r => r.Ip))) ? null : result;
    }

    /// <remarks>
    /// Replaces the whole IP restriction list of the current portal with the addresses from the request and stores
    /// the enforcement flag in the same call. The caller needs the portal-settings right of a DocSpace administrator,
    /// otherwise the call is refused. Every entry must be a single IPv4 or IPv6 address: `from-to` ranges and CIDR
    /// blocks are matched by the portal but cannot be stored here and are rejected as an invalid request, as is
    /// `enable: true` with an empty list. An omitted `enable` follows the list - on when addresses are sent, off when
    /// the list is empty. The replacement is written in one transaction, applies to new requests without a restart
    /// and is recorded in the audit trail; entries not repeated in the body are deleted, and sending the same body
    /// twice leaves the portal as it is. Enforcement spares the portal owner and the installation's own networks
    /// only, so a list without the caller's own address locks the remaining administrators out. The answer echoes the
    /// request rather than the stored rows - no entry IDs, and `enable` exactly as sent, empty when it was omitted -
    /// so read the result with `GET api/2.0/settings/iprestrictions`.
    /// </remarks>
    /// <summary>Save IP restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The saved addresses and enforcement flag echoed back exactly as sent, without the IDs of the stored entries", typeof(IpRestrictionsDto))]
    [HttpPut("")]
    public async Task<IpRestrictionsDto> SaveIpRestrictions(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionBase>();
        var isEmpty = !inDto.IpRestrictions.Any();

        bool enable;
        if (!inDto.Enable.HasValue)
        {
            enable = !isEmpty;
        }
        else
        {
            enable = inDto.Enable.Value;
        }

        if (enable && isEmpty)
        {
            throw new ArgumentException(Resource.ErrorIpRestriction);
        }

        if (inDto.IpRestrictions.Any(r => !IPAddress.TryParse(r.Ip, out _)))
        {
            throw new ArgumentException(nameof(inDto.IpRestrictions));
        }

        var tenant = tenantManager.GetCurrentTenant();
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions, tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }

    /// <remarks>
    /// Reports whether the IP restrictions of the current portal are enforced, as the `enable` flag together with the
    /// `lastModified` stamp of the setting. The caller needs the portal-settings right of a DocSpace administrator,
    /// otherwise the call is refused. The call is read-only and honours `If-Modified-Since`: send back the
    /// `Last-Modified` value of an earlier answer and an unchanged setting comes back as an empty not-modified
    /// response rather than a body. The flag is `false` on a portal nobody has configured. A `true` flag on its own
    /// blocks nothing: enforcement also needs at least one stored address, which this answer does not carry - read
    /// the addresses with `GET api/2.0/settings/iprestrictions` - and it is skipped entirely on an installation whose
    /// configuration hides the IP security section. Even when enforced, the portal owner and the installation's own
    /// networks are let through. Change the flag with `PUT api/2.0/settings/iprestrictions/settings`, which replaces
    /// the address list in the same call, so resend the addresses in force when all that changes is the flag.
    /// </remarks>
    /// <summary>Get IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The enforcement flag of the IP restrictions and the date the setting was last modified", typeof(IPRestrictionsSettings))]
    [HttpGet("settings")]
    public async Task<IPRestrictionsSettings> ReadIpRestrictionsSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<IPRestrictionsSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings;
    }

    /// <remarks>
    /// Stores the enforcement flag of the IP restrictions of the current portal together with the whole address list,
    /// replacing the addresses saved before; this operation and `PUT api/2.0/settings/iprestrictions` are two routes
    /// to the same handler and behave identically. The caller needs the portal-settings right of a DocSpace
    /// administrator, otherwise the call is refused. Every entry must be a single IPv4 or IPv6 address: `from-to`
    /// ranges and CIDR blocks are matched by the portal but cannot be stored here and are rejected as an invalid
    /// request, as is `enable: true` with an empty list. An omitted `enable` follows the list - on when addresses are
    /// sent, off when the list is empty - so the flag cannot be moved without resending the addresses that stay in
    /// force. The new state applies to new requests without a restart, is recorded in the audit trail, and sending
    /// the same body twice changes nothing further. Enforcement spares the portal owner and the installation's own
    /// networks only, so a list without the caller's own address locks the remaining administrators out. The answer
    /// echoes the request, so read the stored entries and their IDs with `GET api/2.0/settings/iprestrictions`.
    /// </remarks>
    /// <summary>Update IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The stored enforcement flag and addresses echoed back exactly as sent, without the IDs of the stored entries", typeof(IpRestrictionsDto))]
    [HttpPut("settings")]
    public async Task<IpRestrictionsDto> UpdateIpRestrictionsSettings(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionBase>();
        var isEmpty = !inDto.IpRestrictions.Any();

        bool enable;
        if (!inDto.Enable.HasValue)
        {
            enable = !isEmpty;
        }
        else
        {
            enable = inDto.Enable.Value;
        }

        if (enable && isEmpty)
        {
            throw new ArgumentException(Resource.ErrorIpRestriction);
        }

        if (inDto.IpRestrictions.Any(r => !IPAddress.TryParse(r.Ip, out _)))
        {
            throw new ArgumentException(nameof(inDto.IpRestrictions));
        }

        var tenant = tenantManager.GetCurrentTenant();
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions, tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }
}