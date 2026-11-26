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
    /// Returns the personal web plugins that the members of the portal installed for themselves. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// also returns 403 while the installation turns off web plugins. The call is read-only. The list includes
    /// plugins hidden by the personal plugins switch or by a portal plugin. It also includes the plugins of deleted
    /// members. The plugins come without the `settings` field. The `enabled` field is the state stored for the member.
    /// A plugin hidden by a portal plugin keeps that state but does not run. The list has no paging. A member holds at
    /// most 10 personal plugins by default. To switch a plugin off for its member, use
    /// `PUT api/2.0/settings/personalwebplugins/plugins/{userId}/{name}`.
    /// </remarks>
    /// <summary>List members' web plugins</summary>
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

        Dictionary<Guid, (UserInfo User, EmployeeType Type)> members = [];
        List<MemberWebPluginDto> outDto = [];
        foreach (var webPlugin in webPlugins)
        {
            outDto.Add(await ToMemberDtoAsync(webPlugin, members));
        }

        return outDto;
    }

    /// <remarks>
    /// Switches a personal web plugin of a member on or off for that member. Take the member from `createBy` and the
    /// name from `name` of `GET api/2.0/settings/personalwebplugins/plugins`. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call also returns 403
    /// while the installation turns off web plugins. It works on the plugins of every member, the owner included. The
    /// call changes data, and you can repeat it with the same body. It keeps the settings the member saved, and the
    /// member can switch the plugin again. The call returns 400 if `userId` is not a valid GUID or the body cannot be
    /// read. It returns 404 if the member has no plugin with this name. To remove the plugin, use
    /// `DELETE api/2.0/settings/personalwebplugins/plugins/{userId}/{name}`.
    /// </remarks>
    /// <summary>Switch a member's web plugin</summary>
    /// <path>api/2.0/settings/personalwebplugins/plugins/{userId}/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The personal web plugin of the member after the change", typeof(MemberWebPluginDto))]
    [SwaggerResponse(400, "The member identifier is not a valid GUID, or the request body cannot be read")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins")]
    [SwaggerResponse(404, "The member has no personal web plugin with this manifest name")]
    [HttpPut("plugins/{userId}/{name}")]
    public async Task<MemberWebPluginDto> SetMemberWebPlugin(SetMemberWebPluginRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webPlugin = await webPluginManager.SetMemberWebPluginEnabledAsync(tenantManager.GetCurrentTenantId(), inDto.UserId, inDto.Name, inDto.State.Enabled);

        await ResetPersonalCspAsync(webPlugin);

        await SendMemberWebPluginAuditAsync(MessageAction.PersonalWebPluginUpdated, inDto.UserId, webPlugin);

        return await ToMemberDtoAsync(webPlugin, []);
    }

    private async Task<MemberWebPluginDto> ToMemberDtoAsync(WebPlugin webPlugin, Dictionary<Guid, (UserInfo User, EmployeeType Type)> members)
    {
        if (!members.TryGetValue(webPlugin.CreateBy, out var member))
        {
            var user = await userManager.GetUsersAsync(webPlugin.CreateBy, false);
            member = (user, await userManager.GetUserTypeAsync(user.Removed ? ASC.Core.Users.Constants.LostUser : user));
            members.Add(webPlugin.CreateBy, member);
        }

        var isOwner = webPlugin.CreateBy == tenantManager.GetCurrentTenant().OwnerId;

        return await mapper.ToMemberDtoManual(webPlugin, member.User, member.Type, isOwner);
    }

    /// <remarks>
    /// Deletes a personal web plugin of a member together with the files of its package. Take the member from
    /// `createBy` and the name from `name` of `GET api/2.0/settings/personalwebplugins/plugins`. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// also returns 403 while the installation turns off web plugins. It works on the plugins of every member, the
    /// owner included. It also works while the installation forbids deleting the portal's own plugins. The call cannot be
    /// undone. It also deletes the state and the settings the member stored for the plugin. The call returns 400 if
    /// `userId` is not a valid GUID. It returns 404 if the member has no plugin with this name. A repeated call
    /// returns 404 too.
    /// </remarks>
    /// <summary>Delete a member's web plugin</summary>
    /// <path>api/2.0/settings/personalwebplugins/plugins/{userId}/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The personal web plugin of the member and the files of its package are removed")]
    [SwaggerResponse(400, "The member identifier is not a valid GUID")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins")]
    [SwaggerResponse(404, "The member has no personal web plugin with this manifest name")]
    [HttpDelete("plugins/{userId}/{name}")]
    public async Task DeleteMemberWebPlugin(DeleteMemberWebPluginRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webPlugin = await webPluginManager.DeleteMemberWebPluginAsync(tenantManager.GetCurrentTenantId(), inDto.UserId, inDto.Name);

        await ResetPersonalCspAsync(webPlugin);

        await SendMemberWebPluginAuditAsync(MessageAction.PersonalWebPluginDeleted, inDto.UserId, webPlugin);
    }

    private async Task SendMemberWebPluginAuditAsync(MessageAction action, Guid userId, WebPlugin webPlugin)
    {
        try
        {
            var user = await userManager.GetUsersAsync(userId, false);

            messageService.Send(action, MessageTarget.Create(userId), webPlugin.Name, user.DisplayUserName(false, displayUserSettingsHelper));
        }
        catch (Exception e)
        {
            logger.WarningMemberWebPluginAuditNotSent(e);
        }
    }

    private async Task ResetPersonalCspAsync(WebPlugin webPlugin)
    {
        if (!string.IsNullOrWhiteSpace(webPlugin.CspDomains))
        {
            await ResetPersonalCspAsync();
        }
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
    /// install web plugins through `POST api/2.0/settings/webplugins` with `personal`. The installation has to allow
    /// uploading plugins for that. A new portal has them off.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get
    /// 403. The call also returns 403 while the installation turns off web plugins or personal plugins. The call
    /// changes data, and you can repeat it with the same body. Switching them off hides the personal plugins from
    /// their members. They are neither listed nor loaded, and their domains leave the Content Security Policy. The
    /// calls with `personal=true` then return 403. Their files and states stay, so switching them on again brings
    /// them back. The call returns 400 if the body cannot be read. It returns the plugin switches after the change, as
    /// `GET api/2.0/settings` reports them.
    /// </remarks>
    /// <summary>Switch personal web plugins</summary>
    /// <path>api/2.0/settings/personalwebplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The plugin switches of the portal after the change", typeof(PluginsDto))]
    [SwaggerResponse(400, "The request body cannot be read")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the installation turns off web plugins or personal plugins")]
    [HttpPost("")]
    public async Task<PluginsDto> SetPersonalWebPlugins(SetPersonalWebPluginsRequestDto inDto)
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
