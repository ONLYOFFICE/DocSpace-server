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
    /// Starts moving the portal to another owner. The portal sends a confirmation letter to the portal owner's email
    /// address. Ownership moves only after `PUT api/2.0/settings/owner` with the link from this letter. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. A DocSpace
    /// admin who is not the portal owner gets 200 with `status` 0 and a refusal text. The call also returns `status` 0
    /// for an unknown or inactive new owner. So read `status`, not the HTTP code. On success, `status` is 1. The
    /// `message` then shows the owner's address as an HTML `mailto:` link. Do not repeat the call. Each successful call
    /// sends a new letter with a new link. Earlier links keep working until ownership moves. A link works for seven days
    /// by default. The call returns 400 if `ownerId` is missing or the owner's email address is not confirmed. To check
    /// the address, read `activationStatus` from `GET api/2.0/people/@self`. It also returns 403 if the new owner is a
    /// guest. To change a user type instead, use `PUT api/2.0/people/type/{type}`.
    /// </remarks>
    /// <summary>
    /// Start the portal owner change
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "The outcome of the request: `status` 1 with the address the instructions were sent to, or `status` 0 with a localized refusal when the transfer cannot be started", typeof(OwnerChangeInstructionsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `ownerId`, or the portal owner's own email address has not been confirmed yet, so no instructions can be sent")]
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
    /// Completes the portal owner change that `POST api/2.0/settings/owner` started. The user in `ownerId` becomes the
    /// portal owner. Sign the request with the confirmation link from that letter, not with a session. A signed-in
    /// caller without this link gets 403, even the portal owner. A caller with neither a session nor this link gets
    /// 401. Pass the link's `type`, `key`, `uid`, and `encemail` parameters in the `confirm` header. An invalid or
    /// expired link returns 401. A link expires after seven days by default. It also becomes invalid when its address
    /// no longer belongs to the portal owner. A deleted or disabled `uid` account has the same effect. To check that
    /// the link still works, use `POST api/2.0/authentication/confirm`. The call changes data, and a repeated call
    /// with the same link returns 401. If the new owner is not a DocSpace admin yet, the call makes them one first.
    /// The previous owner keeps their account and role but loses the owner's rights. The call returns no body. To read
    /// the new `ownerId`, use `GET api/2.0/settings` as a signed-in user. After this call, a new transfer needs the
    /// new owner. The new owner must be an active account of the portal, not a guest. If promoting the new owner
    /// needs a paid seat that the portal lacks, the call returns 402. Then ownership does not move. Without `ownerId`,
    /// the call returns 400.
    /// </remarks>
    /// <summary>
    /// Confirm the portal owner change
    /// </summary>
    /// <path>api/2.0/settings/owner</path>
    [Tags("Settings / Owner")]
    [SwaggerResponse(200, "The portal owner has been changed to the user named in the request")]
    [SwaggerResponse(400, "The request body cannot be read or has no `ownerId`")]
    [SwaggerResponse(401, "The caller is not signed in and sent no owner change link, or the link is invalid or expired, its address no longer belongs to the portal owner, or its `uid` account is deleted or disabled")]
    [SwaggerResponse(402, "The user named as the new owner is a regular user, and promoting them to DocSpace administrator needs a paid seat the portal has run out of")]
    [SwaggerResponse(403, "The caller is signed in but sent no owner change link, whatever their role, or sent a valid confirmation link of another type")]
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
