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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("owner")]
public class OwnerController(
    MessageService messageService,
    CommonLinkUtility commonLinkUtility,
    StudioNotifyService studioNotifyService,
    UserManager userManager,
    TenantManager tenantManager,
    AuthContext authContext,
    PermissionContext permissionContext,
    WebItemManager webItemManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IFusionCache fusionCache,
    IUrlShortener urlShortener,
    UserManagerWrapper userManagerWrapper)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Sends the instructions to change the DocSpace owner.
    /// </remarks>
    /// <summary>
    /// Send the owner change instructions
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "Message about changing the portal owner", typeof(OwnerChangeInstructionsDto))]
    [SwaggerResponse(400, "Owner's email is not activated")]
    [SwaggerResponse(403, "Collaborator can not be an owner")]
    [HttpPost("")]
    public async Task<OwnerChangeInstructionsDto> SendOwnerChangeInstructions(OwnerIdSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var curTenant = tenantManager.GetCurrentTenant();
        var owner = await userManager.GetUsersAsync(curTenant.OwnerId);
        var newOwner = await userManager.GetUsersAsync(inDto.OwnerId);

        if (owner.ActivationStatus != EmployeeActivationStatus.Activated)
        {
            throw new ArgumentException("Owner's email is not activated");
        }

        if (await userManager.IsGuestAsync(newOwner))
        {
            throw new SecurityException("Collaborator can not be an owner");
        }

        if (!owner.Id.Equals(authContext.CurrentAccount.ID) ||
            Guid.Empty.Equals(newOwner.Id) ||
            newOwner.Status != EmployeeStatus.Active)
        {
            return new OwnerChangeInstructionsDto { Status = 0, Message = Resource.ErrorAccessDenied };
        }

        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalOwnerChange, newOwner.Id, newOwner.Id);
        await studioNotifyService.SendMsgConfirmChangeOwnerAsync(owner, newOwner, await urlShortener.GetShortenLinkAsync(confirmLink));

        messageService.Send(MessageAction.OwnerSentChangeOwnerInstructions, MessageTarget.Create(owner.Id), owner.DisplayUserName(false, displayUserSettingsHelper));

        var emailLink = $"<a href=\"mailto:{owner.Email}\">{owner.Email}</a>";
        return new OwnerChangeInstructionsDto { Status = 1, Message = Resource.ChangePortalOwnerMsg.Replace(":email", emailLink) };
    }

    /// <remarks>
    /// Updates the current portal owner with a new one specified in the request.
    /// </remarks>
    /// <summary>
    /// Update the portal owner
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(400, "The user could not be found")]
    [SwaggerResponse(409, "")]
    [HttpPut("")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalOwnerChange")]
    public async Task UpdatePortalOwner(OwnerIdSettingsRequestDto inDto)
    {
        var newOwner = Constants.LostUser;
        try
        {
            newOwner = await userManager.GetUsersAsync(inDto.OwnerId);
        }
        catch
        {
            // ignored
        }

        if (Constants.LostUser.Equals(newOwner))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (await userManager.IsUserInGroupAsync(newOwner.Id, Constants.GroupGuest.ID))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (newOwner.Status != EmployeeStatus.Active)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var newOwnerType = await userManager.GetUserTypeAsync(newOwner);
        if (newOwnerType != EmployeeType.DocSpaceAdmin)
        {
            if (!await userManagerWrapper.UpdateUserTypeAsync(newOwner, EmployeeType.DocSpaceAdmin))
            {
                throw new InvalidOperationException();
            }
        }

        var curTenant = tenantManager.GetCurrentTenant();
        curTenant.OwnerId = newOwner.Id;
        await tenantManager.SaveTenantAsync(curTenant);

        messageService.Send(MessageAction.OwnerUpdated, newOwner.DisplayUserName(false, displayUserSettingsHelper));
    }
}