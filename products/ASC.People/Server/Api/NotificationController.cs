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

namespace ASC.People.Api;

///<remarks>
/// Notification API.
///</remarks>
public class NotificationController : ApiControllerBase
// UserManager userManager,
//     SecurityContext securityContext,
//     AuthContext authContext,
//     PermissionContext permissionContext,
//     CommonLinkUtility commonLinkUtility,
//     StudioNotifyService studioNotifyService

{
    /// <remarks>
    /// Sends a notification to the user with the ID specified in the request to change their phone number.
    /// </remarks>
    /// <summary>
    /// Send a notification to change a phone
    /// </summary>
    /// <path>api/2.0/people/phone</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Notification", typeof(string))]
    [SwaggerResponse(501, "Not Implemented")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("phone")]
    public Task<string> SendNotificationToChange(UpdateMemberSimpleRequestDto inDto)
    {
        throw new NotImplementedException();
        // var user = await userManager.GetUsersAsync(string.IsNullOrEmpty(inDto.UserId)
        //     ? securityContext.CurrentAccount.ID : new Guid(inDto.UserId));
        //
        // var canChange = user.IsMe(authContext) || await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
        //
        // if (!canChange)
        // {
        //     throw new SecurityAccessDeniedException(Resource.ErrorAccessDenied);
        // }
        //
        // user.MobilePhoneActivationStatus = MobilePhoneActivationStatus.NotActivated;
        //
        // await userManager.UpdateUserInfoAsync(user);
        //
        // if (user.IsMe(authContext))
        // {
        //     return commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.PhoneActivation);
        // }
        //
        // await studioNotifyService.SendMsgMobilePhoneChangeAsync(user);
        //
        // return string.Empty;
    }
}