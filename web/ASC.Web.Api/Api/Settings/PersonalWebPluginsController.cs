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

[ApiEndpoint(Template = "personalwebplugins")]
public class PersonalWebPluginsController(
    IFusionCache fusionCache,
    WebItemManager webItemManager,
    PermissionContext permissionContext,
    WebPluginManager webPluginManager,
    WebPluginConfigSettings webPluginConfigSettings,
    MessageService messageService,
    CspSettingsHelper cspSettingsHelper,
    TenantManager tenantManager,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    WebPluginMapper mapper,
    ILogger<PersonalWebPluginsController> logger)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the personal web plugins that the members of the portal installed for themselves, for an
    /// administrator to review. Each entry has the member in `createBy`, the member's type in `createByType` and
    /// `createByIsOwner`, the upload time in `createOn`, the domains in `cspDomains` and whether the plugin runs for the
    /// member in `enabled`. The `settings` field is always empty,
    /// because a member's plugin settings stay private. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get 403. The call also returns 403 while the installation turns
    /// off web plugins. The call is read-only. It lists the plugins whatever the personal plugins switch says, the
    /// ones a portal plugin hides included, and the plugins of deleted members too. The list has no paging; a member
    /// holds at most 10 personal plugins by default. To switch a plugin off for its member, use
    /// `PUT api/2.0/settings/personalwebplugins/plugins/{userId}/{name}`.
    /// </remarks>
    /// <summary>Get the personal web plugins of members</summary>
    /// <path>api/2.0/settings/personalwebplugins/plugins</path>
    /// <collection>list</collection>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The personal web plugins of all members, each with its member in `createBy`", typeof(IEnumerable<MemberWebPluginDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins")]
    [HttpGet("plugins")]
    public async Task<IEnumerable<MemberWebPluginDto>> GetMembersWebPlugins()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webPlugins = await webPluginManager.GetMembersWebPluginsAsync(tenantManager.GetCurrentTenantId());

        Dictionary<Guid, EmployeeType> memberTypes = [];
        List<MemberWebPluginDto> outDto = [];
        foreach (var webPlugin in webPlugins)
        {
            outDto.Add(await ToMemberDtoAsync(webPlugin, memberTypes));
        }

        return outDto;
    }

    /// <remarks>
    /// Switches a personal web plugin of a member on or off for that member. Take the member from the `createBy` field
    /// and the name from the `name` field of `GET api/2.0/settings/personalwebplugins/plugins`. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// also returns 403 while the installation turns off web plugins. The call changes data, and you can repeat it with
    /// the same body. It keeps the settings the member saved for the plugin, and the member can switch the plugin
    /// again. The open clients of the member get the `s:change-personal-web-plugin` socket event. The call returns 404
    /// if the member has no plugin with this name. To remove the plugin, use
    /// `DELETE api/2.0/settings/personalwebplugins/plugins/{userId}/{name}`.
    /// </remarks>
    /// <summary>Switch a personal web plugin of a member</summary>
    /// <path>api/2.0/settings/personalwebplugins/plugins/{userId}/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The personal web plugin of the member after the change", typeof(MemberWebPluginDto))]
    [SwaggerResponse(400, "The request body cannot be read")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins")]
    [SwaggerResponse(404, "The member has no personal web plugin with this manifest name")]
    [HttpPut("plugins/{userId}/{name}")]
    public async Task<MemberWebPluginDto> SetMemberWebPlugin(MemberWebPluginStateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webPlugin = await webPluginManager.SetMemberWebPluginEnabledAsync(tenantManager.GetCurrentTenantId(), inDto.UserId, inDto.Name, inDto.State.Enabled);

        await SendMemberWebPluginAuditAsync(MessageAction.PersonalWebPluginUpdated, inDto.UserId, webPlugin);

        await ResetPersonalCspAsync();

        return await ToMemberDtoAsync(webPlugin, []);
    }

    private async Task<MemberWebPluginDto> ToMemberDtoAsync(WebPlugin webPlugin, Dictionary<Guid, EmployeeType> memberTypes)
    {
        if (!memberTypes.TryGetValue(webPlugin.CreateBy, out var memberType))
        {
            memberType = await userManager.GetUserTypeAsync(await userManager.GetUsersAsync(webPlugin.CreateBy));
            memberTypes.Add(webPlugin.CreateBy, memberType);
        }

        var isOwner = webPlugin.CreateBy == tenantManager.GetCurrentTenant().OwnerId;

        return await mapper.ToMemberDtoManual(webPlugin, memberType, isOwner);
    }

    /// <remarks>
    /// Deletes a personal web plugin of a member together with the files of its package. Take the member from the
    /// `createBy` field and the name from the `name` field of `GET api/2.0/settings/personalwebplugins/plugins`.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get
    /// 403. The call also returns 403 while the installation turns off web plugins. The call cannot be undone. It
    /// also deletes the state and the settings the member stored for the plugin. The open clients of the member get
    /// the `s:change-personal-web-plugin` socket event. The call returns 404 if the member has no plugin with this
    /// name, so a repeated call returns 404 too.
    /// </remarks>
    /// <summary>Delete a personal web plugin of a member</summary>
    /// <path>api/2.0/settings/personalwebplugins/plugins/{userId}/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The personal web plugin of the member and the files of its package are removed")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins")]
    [SwaggerResponse(404, "The member has no personal web plugin with this manifest name")]
    [HttpDelete("plugins/{userId}/{name}")]
    public async Task DeleteMemberWebPlugin(MemberWebPluginRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webPlugin = await webPluginManager.DeleteMemberWebPluginAsync(tenantManager.GetCurrentTenantId(), inDto.UserId, inDto.Name);

        await SendMemberWebPluginAuditAsync(MessageAction.PersonalWebPluginDeleted, inDto.UserId, webPlugin);

        await ResetPersonalCspAsync();
    }

    private async Task SendMemberWebPluginAuditAsync(MessageAction action, Guid userId, WebPlugin webPlugin)
    {
        var user = await userManager.GetUsersAsync(userId);

        messageService.Send(action, MessageTarget.Create(userId), webPlugin.Name, user.DisplayUserName(false, displayUserSettingsHelper));
    }

    private async Task ResetPersonalCspAsync()
    {
        try
        {
            await cspSettingsHelper.ResetPersonalAsync();
        }
        catch (Exception e)
        {
            logger.WarningPersonalCspNotUpdated(e);
        }
    }

    /// <remarks>
    /// Switches personal web plugins on or off for the portal. While they are on, every member other than a guest may
    /// install web plugins for themselves through `POST api/2.0/settings/webplugins?personal=true`. A new portal has
    /// them off. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. The call also returns 403 while the installation turns off web plugins or personal plugins.
    /// The call changes data, and you can repeat it with the same body. Switching them off hides the personal plugins
    /// of all members at once: they are neither listed nor loaded, their domains leave the Content Security Policy,
    /// and the calls with `personal=true` return 403. Their files and states stay, so switching them on again brings
    /// them back. Open clients of the portal get the `s:change-personal-web-plugins` socket event. The call returns
    /// the plugin switches as `GET api/2.0/settings` reports them afterwards.
    /// </remarks>
    /// <summary>Switch personal web plugins</summary>
    /// <path>api/2.0/settings/personalwebplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The plugin switches of the portal after the change", typeof(PluginsDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins or personal plugins")]
    [HttpPost("")]
    public async Task<PluginsDto> SetPersonalWebPlugins(PersonalWebPluginsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await webPluginManager.SetPersonalWebPluginsEnabledAsync(inDto.Enabled);

        messageService.Send(inDto.Enabled ? MessageAction.PersonalWebPluginsEnabled : MessageAction.PersonalWebPluginsDisabled);

        await ResetPersonalCspAsync();

        var outDto = webPluginConfigSettings.Map();
        outDto.Personal = await webPluginManager.IsPersonalWebPluginsEnabledAsync();

        return outDto;
    }
}
