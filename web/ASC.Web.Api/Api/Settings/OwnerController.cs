﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("owner")]
public class OwnerController(
    MessageService messageService,
    CommonLinkUtility commonLinkUtility,
    StudioNotifyService studioNotifyService,
    ApiContext apiContext,
    UserManager userManager,
    TenantManager tenantManager,
    AuthContext authContext,
    PermissionContext permissionContext,
    WebItemManager webItemManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IMemoryCache memoryCache,
    IHttpContextAccessor httpContextAccessor,
    IUrlShortener urlShortener,
    UserManagerWrapper userManagerWrapper)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Sends the instructions to change the DocSpace owner.
    /// </summary>
    /// <short>
    /// Send the owner change instructions
    /// </short>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "Message about changing the portal owner", typeof(object))]
    [SwaggerResponse(403, "Collaborator can not be an owner")]
    [HttpPost("")]
    public async Task<object> SendOwnerChangeInstructionsAsync(OwnerIdSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var curTenant = tenantManager.GetCurrentTenant();
        var owner = await userManager.GetUsersAsync(curTenant.OwnerId);
        var newOwner = await userManager.GetUsersAsync(inDto.OwnerId);

        if (await userManager.IsGuestAsync(newOwner))
        {
            throw new SecurityException("Collaborator can not be an owner");
        }

        if (!owner.Id.Equals(authContext.CurrentAccount.ID) || 
            Guid.Empty.Equals(newOwner.Id) || 
            newOwner.Status != EmployeeStatus.Active)
        {
            return new { Status = 0, Message = Resource.ErrorAccessDenied };
        }

        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalOwnerChange, newOwner.Id, newOwner.Id);
        await studioNotifyService.SendMsgConfirmChangeOwnerAsync(owner, newOwner, await urlShortener.GetShortenLinkAsync(confirmLink));

        messageService.Send(MessageAction.OwnerSentChangeOwnerInstructions, MessageTarget.Create(owner.Id), owner.DisplayUserName(false, displayUserSettingsHelper));

        var emailLink = $"<a href=\"mailto:{owner.Email}\">{owner.Email}</a>";
        return new { Status = 1, Message = Resource.ChangePortalOwnerMsg.Replace(":email", emailLink) };
    }

    /// <summary>
    /// Updates the current portal owner with a new one specified in the request.
    /// </summary>
    /// <short>
    /// Update the portal owner
    /// </short>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(400, "The user could not be found")]
    [SwaggerResponse(409, "")]
    [HttpPut("")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalOwnerChange")]
    public async Task OwnerAsync(OwnerIdSettingsRequestDto inDto)
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
