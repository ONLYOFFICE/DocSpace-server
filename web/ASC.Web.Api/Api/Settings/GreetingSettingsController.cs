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
    /// Returns the greeting title of the current portal as a plain string. The portal shows it as the welcome heading
    /// on the sign-in page and keeps it as the portal name. Any signed-in user can call it, including a guest. The
    /// call is read-only. The answer is never empty. If the portal has no title, the call returns the built-in
    /// default caption in the language of the caller. So the answer cannot tell a saved title from the
    /// default one. To check that, use `GET api/2.0/settings/greetingsettings/isdefault`. The `greetingSettings`
    /// field of `GET api/2.0/settings` has the same string. The title is not the portal address or the white-label
    /// logo text. To read the logo text, use `GET api/2.0/settings/whitelabel/logotext`.
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
    /// Returns whether the current portal shows the built-in greeting caption instead of a title of its own. Any
    /// signed-in user can call it, including a guest. The call is read-only. It returns `true` while the portal
    /// stores no title, and `false` after a non-empty title is saved. The portal stores no title after an empty
    /// `title` is saved. It also stores none after `POST api/2.0/settings/greetingsettings/restore` if the
    /// installation configures no portal name. For a missing title, `GET api/2.0/settings/greetingsettings` returns
    /// the default caption. So only this call tells the default greeting from a custom title with the same words.
    /// The answer covers only the greeting title. To check the white-label logos and logo text, use
    /// `GET api/2.0/settings/whitelabel/logos/isdefault` and `GET api/2.0/settings/whitelabel/logotext/isdefault`.
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
    /// Replaces the greeting title of the current portal with `title` and stores it as the portal name. An empty
    /// `title` clears the title. Then the portal shows the built-in default caption, and
    /// `GET api/2.0/settings/greetingsettings/isdefault` returns `true`. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call changes data, and the new
    /// title applies immediately to every user of the portal. You can repeat it with the same body. On a free or
    /// trial cloud plan, the call checks the title against the character rule of the installation. A paid cloud plan
    /// and a server installation skip this check. The call returns a localized confirmation message, not the stored
    /// title. To read the title, use `GET api/2.0/settings/greetingsettings`. The call returns 400 if `title` is
    /// missing, too long, or breaks the character rule.
    /// </remarks>
    /// <summary>Save the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "A localized message confirming that the greeting title has been saved", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, the `title` is longer than 255 characters, or on a free or trial cloud plan it breaks the character rule of the installation")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Resets the greeting title of the current portal to the portal name configured for the installation. If the
    /// installation configures no name, the stored title becomes empty. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call changes data, and the change
    /// applies immediately to every user of the portal. You can repeat it, and a second call changes nothing. It
    /// returns the title in force after the reset. For an empty title, it returns the localized default caption.
    /// Then `GET api/2.0/settings/greetingsettings/isdefault` returns `true`. The call does not reset the portal
    /// logos or the white-label logo text. To reset them, use `PUT api/2.0/settings/whitelabel/logos/restore` and
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`. To set a custom title, use
    /// `POST api/2.0/settings/greetingsettings`.
    /// </remarks>
    /// <summary>Restore the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings/restore</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "The greeting title in force after the restore, or the localized default caption when the installation configures none", typeof(string))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPost("restore")]
    public async Task<string> RestoreGreetingSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantInfoSettingsHelper.RestoreDefaultTenantNameAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }
}