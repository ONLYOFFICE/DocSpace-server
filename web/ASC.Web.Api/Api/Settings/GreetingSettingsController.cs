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

[ApiEndpoint(Template = "greetingsettings")]
public class GreetingSettingsController(
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    MessageService messageService,
    TenantManager tenantManager,
    PermissionContext permissionContext,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    CoreBaseSettings coreBaseSettings)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the greeting title of the current portal - the caption shown as the welcome heading on the sign-in
    /// page, kept as the portal name. Any authenticated user may call it and no administrative right is needed; the
    /// call is read-only. The title comes back as a bare string and is never empty: when the portal has no title of
    /// its own, the built-in default caption is returned instead, localized to the caller's language. Because of that
    /// fallback this operation cannot tell a saved title from the default one - call
    /// `GET api/2.0/settings/greetingsettings/isdefault` when that distinction matters. The same string is part of
    /// the portal settings answer as the `greetingSettings` field of `GET api/2.0/settings`, so a client that already
    /// reads the settings needs no separate call. The value is a caption only: it is neither the portal address nor
    /// the white-label logo text of the header, which is returned by `GET api/2.0/settings/whitelabel/logotext`.
    /// </remarks>
    /// <summary>Get greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "The greeting title of the portal, or the localized default caption when the portal has no title of its own", typeof(string))]
    [HttpGet("")]
    public string GetGreetingSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }

    /// <remarks>
    /// Reports whether the current portal still shows the built-in greeting caption instead of a title of its own.
    /// The check is read-only and open to any authenticated user, with no administrative right required. It answers
    /// `true` while no title is stored for the portal - the state after
    /// `POST api/2.0/settings/greetingsettings/restore` on an installation that configures no portal name, and also
    /// after saving an empty `title` - and `false` as soon as a non-empty title has been saved. Use it together with
    /// `GET api/2.0/settings/greetingsettings`: that operation substitutes the localized default caption for a
    /// missing title, so only these two calls together separate a default greeting from a custom one that happens to
    /// repeat the default wording. The answer covers the greeting title alone; whether the white-label logos and logo
    /// text are still the default ones is reported by `GET api/2.0/settings/whitelabel/logos/isdefault` and
    /// `GET api/2.0/settings/whitelabel/logotext/isdefault`.
    /// </remarks>
    /// <summary>Check the default greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings/isdefault</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Boolean value: true if the portal has no greeting title of its own and the built-in default caption is shown", typeof(bool))]
    [HttpGet("isdefault")]
    public bool GetIsDefaultGreetingSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "";
    }

    /// <remarks>
    /// Replaces the greeting title of the current portal with the `title` from the request, storing it as the portal
    /// name. The caller needs the portal-settings right of a DocSpace administrator, otherwise the call is refused.
    /// The new caption takes effect at once for every user of the portal and the change is written to the audit
    /// trail; repeating the call with the same title leaves the portal in the same state. A missing `title` or one
    /// longer than 255 characters is rejected as an invalid request before the handler runs. On a cloud portal with a
    /// free or trial plan the title is also matched against the character rule configured for the installation and a
    /// title that breaks it is refused, while a paid cloud plan and a server installation apply no character check.
    /// An empty `title` clears the greeting: the portal falls back to the built-in default caption and
    /// `GET api/2.0/settings/greetingsettings/isdefault` starts answering `true`. What comes back is a localized
    /// confirmation message, not the stored title - read the title with `GET api/2.0/settings/greetingsettings`.
    /// </remarks>
    /// <summary>Save the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "A localized message confirming that the greeting title has been saved", typeof(string))]
    [HttpPost("")]
    public async Task<string> SaveGreetingSettings(GreetingSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        if (!coreBaseSettings.Standalone)
        {
            var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
            if (quota.Free || quota.Trial)
            {
                try
                {
                    tenantManager.ValidateTenantName(inDto.Title);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message, nameof(inDto.Title));
                }
            }
        }

        tenant.Name = inDto.Title;
        await tenantManager.SaveTenantAsync(tenant);

        messageService.Send(MessageAction.GreetingSettingsUpdated);

        return Resource.SuccessfullySaveGreetingSettingsMessage;
    }

    /// <remarks>
    /// Drops the custom greeting title of the current portal and puts back the title configured for the installation,
    /// which is an empty value unless the installation defines a portal name of its own. The caller needs the
    /// portal-settings right of a DocSpace administrator, otherwise the call is refused. The change is immediate for
    /// every user of the portal and a second call changes nothing, so a retry after a failed attempt is safe. The
    /// answer is the greeting in force afterwards: the configured title when there is one, and the localized default
    /// caption when the stored title ends up empty - in that case `GET api/2.0/settings/greetingsettings/isdefault`
    /// starts answering `true`. Only the caption is touched: the portal logos and the white-label logo text keep
    /// their values and are reset separately by `PUT api/2.0/settings/whitelabel/logos/restore` and
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`. To set a title instead of the default one use
    /// `POST api/2.0/settings/greetingsettings`.
    /// </remarks>
    /// <summary>Restore the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings/restore</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "The greeting title in force after the restore, or the localized default caption when the installation configures none", typeof(string))]
    [HttpPost("restore")]
    public async Task<string> RestoreGreetingSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantInfoSettingsHelper.RestoreDefaultTenantNameAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }
}