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

[ApiEndpoint(Template = "owner")]
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
    UserManagerWrapper userManagerWrapper,
    EmailValidationKeyModelHelper emailValidationKeyModelHelper,
    SecurityContext securityContext)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Starts handing this portal over to another of its members: the confirmation letter goes to the current owner's
    /// address, and nothing changes until the link in it is used. The owner's own email address has to be confirmed
    /// first, otherwise the call is answered with 400; `GET api/2.0/people/@self` reports it as `activationStatus`.
    /// The caller needs the portal-settings right of a DocSpace administrator, so a room administrator, an ordinary
    /// member or a guest is refused with 403, as is naming a guest in `ownerId`. Only the portal owner can actually
    /// start a transfer: an administrator who is not the owner, or a named user who is inactive or unknown here, gets
    /// 200 with `status` 0 and a localized refusal instead of an error, so read `status` and not the HTTP code. A
    /// started transfer answers `status` 1 and a `message` carrying the owner's address inside an HTML `mailto:`
    /// anchor rather than as plain text. Ownership itself does not move here; every call issues a fresh link usable
    /// for a limited period, seven days by default, and the attempt is recorded in the audit trail. Complete the
    /// transfer with `PUT api/2.0/settings/owner`; changing what a member may do is `PUT api/2.0/people/type/{type}`.
    /// </remarks>
    /// <summary>
    /// Start the portal owner change
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "The outcome of the request: `status` 1 with the address the instructions were sent to, or `status` 0 with a localized refusal when the transfer cannot be started", typeof(OwnerChangeInstructionsDto))]
    [SwaggerResponse(400, "The portal owner's own email address has not been confirmed yet, so no instructions can be sent")]
    [SwaggerResponse(403, "The caller does not hold the portal-settings right of a DocSpace administrator, or the user named as the new owner is a guest")]
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
    /// Completes the portal owner change that `POST api/2.0/settings/owner` started, making the user named in
    /// `ownerId` the owner of this portal. Authorization comes from the confirmation link in that letter, not from an
    /// ordinary session: pass the link's `type`, `key`, `uid` and `encemail` parameters in the `confirm` request
    /// header, and check with `POST api/2.0/authentication/confirm` that it is still usable, because it expires after
    /// a limited period, seven days by default. A caller without such a link is refused whatever role it holds, and
    /// so is a link whose address is no longer the owner's, which is what replaying a used link looks like. The named
    /// user has to be an active member of the portal and must not be a guest. The call is mutating: a named user who
    /// is not a DocSpace administrator yet is promoted to one first, and a promotion needing a paid seat the portal
    /// lacks is refused before ownership moves. The previous owner keeps their account and role but loses the owner's
    /// rights, and the change reaches the audit trail. The answer carries no payload: read the new `ownerId` from
    /// `GET api/2.0/settings`, which needs no token. Only the new owner can start another transfer.
    /// </remarks>
    /// <summary>
    /// Confirm the portal owner change
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "The portal owner has been changed to the user named in the request")]
    [SwaggerResponse(400, "The user named as the new owner cannot be found in this portal, is a guest, or is not active")]
    [SwaggerResponse(409, "The new owner could not be given DocSpace administrator rights, so the transfer was not applied")]
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

        var curTenant = tenantManager.GetCurrentTenant();

        var confirmModel = emailValidationKeyModelHelper.GetModel();
        var currentOwnerEmail = emailValidationKeyModelHelper.DecryptEmail(confirmModel.EncEmail);
        var currentOwner = await userManager.GetUserByEmailAsync(currentOwnerEmail);

        if (Constants.LostUser.Equals(currentOwner) || !currentOwner.Id.Equals(curTenant.OwnerId))
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(currentOwner.Id);

        var newOwnerType = await userManager.GetUserTypeAsync(newOwner);
        if (newOwnerType != EmployeeType.DocSpaceAdmin)
        {
            if (!await userManagerWrapper.UpdateUserTypeAsync(newOwner, EmployeeType.DocSpaceAdmin))
            {
                throw new InvalidOperationException();
            }
        }

        curTenant.OwnerId = newOwner.Id;
        await tenantManager.SaveTenantAsync(curTenant);

        messageService.Send(MessageAction.OwnerUpdated, newOwner.DisplayUserName(false, displayUserSettingsHelper));
    }
}
