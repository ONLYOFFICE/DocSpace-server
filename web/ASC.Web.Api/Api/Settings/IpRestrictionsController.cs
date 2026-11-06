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
    /// Returns the IP restriction list of the current portal, the addresses allowed to reach it. Each entry has its `id`
    /// and the `forAdmin` flag. An entry with `forAdmin` admits only DocSpace admins. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only and
    /// honours `If-None-Match`. The list has no defined order. On a portal without restrictions, the list is empty, and
    /// an empty list blocks nobody. The portal owner and the installation's own networks are never blocked. If it holds
    /// the `ETag` of an earlier answer, unchanged addresses return 304 with an empty body. The `ETag` covers only the
    /// addresses, so a change of `forAdmin` alone also returns 304. To check whether the restrictions are enforced, use
    /// `GET api/2.0/settings/iprestrictions/settings`. To replace the whole list, use
    /// `PUT api/2.0/settings/iprestrictions`. It takes addresses, not the IDs returned here. You cannot add or delete a
    /// single entry.
    /// </remarks>
    /// <summary>Get IP restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    /// <collection>list</collection>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The IP addresses allowed to reach the portal, each with its ID and administrators-only flag; an empty list when the portal has no restrictions", typeof(IEnumerable<IpRestrictionDto>))]
    [SwaggerResponse(304, "The addresses have not changed since the `ETag` sent back in `If-None-Match`, which ignores the administrators-only flags; the body is empty")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("")]
    public async Task<IEnumerable<IpRestrictionDto>> GetIpRestrictions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var etagFromRequest = HttpContext.Request.Headers.IfNoneMatch;
        var result = await iPRestrictionsService.GetAsync(tenant.Id, etagFromRequest);

        return HttpContext.TryGetFromCache(await HttpContextExtension.CalculateEtagAsync(result.Select(r => r.Ip))) ? null : result.Select(r => r.Map());
    }

    /// <remarks>
    /// Replaces the whole IP restriction list of the current portal and stores the enforcement flag. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// changes data, and you can repeat it with the same body. It deletes every stored entry that the body does not
    /// repeat. The new list applies to new requests immediately. An omitted `enable` is on for a list with addresses and
    /// off for an empty one. The call returns 400 if the body has no `ipRestrictions` or `enable` is `true` with an empty
    /// list. It also returns 400 if an entry is not a single IPv4 or IPv6 address. Ranges and CIDR blocks also
    /// return 400. The portal owner and the installation's own networks are never blocked. So a DocSpace admin who leaves
    /// out their own address locks themselves out. The answer echoes the request, so it has no entry IDs, and an omitted
    /// `enable` stays empty. To read the stored list, use `GET api/2.0/settings/iprestrictions`.
    /// </remarks>
    /// <summary>Save IP restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The saved addresses and enforcement flag echoed back exactly as sent, without the IDs of the stored entries", typeof(IpRestrictionsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `ipRestrictions`, an entry is not a single IPv4 or IPv6 address, or `enable` is `true` with an empty list")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPut("")]
    public async Task<IpRestrictionsDto> SaveIpRestrictions(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionEntryDto>();
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
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions.Select(r => r.Map()), tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }

    /// <remarks>
    /// Returns whether the IP restrictions of the current portal are enforced, and when the setting last changed.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call is read-only and honours `If-Modified-Since`. If it holds the `Last-Modified` of an earlier
    /// answer, an unchanged setting returns 304 with an empty body. On a portal nobody has configured, `enable` is
    /// `false`. A `true` flag blocks nothing while the address list is empty. To read the addresses, use
    /// `GET api/2.0/settings/iprestrictions`. The installation configuration can also turn enforcement off for all its
    /// portals. The portal owner and the installation's own networks are never blocked. To change the flag, use
    /// `PUT api/2.0/settings/iprestrictions/settings`. It also replaces the address list, so resend the addresses in
    /// force.
    /// </remarks>
    /// <summary>Get IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The enforcement flag of the IP restrictions and the date the setting was last modified", typeof(IpRestrictionsSettingsDto))]
    [SwaggerResponse(304, "The enforcement flag has not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("settings")]
    public async Task<IpRestrictionsSettingsDto> ReadIpRestrictionsSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<IPRestrictionsSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings.Map();
    }

    /// <remarks>
    /// Stores the enforcement flag of the IP restrictions of the current portal, together with the whole address list. It
    /// works exactly like `PUT api/2.0/settings/iprestrictions`. So to change only the flag, resend the addresses in
    /// force. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call changes data, and you can repeat it with the same body. It deletes every stored entry that the
    /// body does not repeat. The new list applies to new requests immediately. An omitted `enable` is on for a list with
    /// addresses and off for an empty one. The call returns 400 if the body has no `ipRestrictions` or `enable` is `true`
    /// with an empty list. It also returns 400 if an entry is not a single IPv4 or IPv6 address. Ranges and CIDR blocks
    /// also return 400. The portal owner and the installation's own networks are never blocked. So a DocSpace admin who
    /// leaves out their own address locks themselves out. The answer echoes the request, so it has no entry IDs, and an
    /// omitted `enable` stays empty. To read the stored list, use `GET api/2.0/settings/iprestrictions`.
    /// </remarks>
    /// <summary>Update IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "The stored enforcement flag and addresses echoed back exactly as sent, without the IDs of the stored entries", typeof(IpRestrictionsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `ipRestrictions`, an entry is not a single IPv4 or IPv6 address, or `enable` is `true` with an empty list")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPut("settings")]
    public async Task<IpRestrictionsDto> UpdateIpRestrictionsSettings(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionEntryDto>();
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
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions.Select(r => r.Map()), tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }
}