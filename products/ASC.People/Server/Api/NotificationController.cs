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

namespace ASC.People.Api;

public class NotificationController(UserManager userManager,
        SecurityContext securityContext,
        AuthContext authContext,
        PermissionContext permissionContext,
        CommonLinkUtility commonLinkUtility,
        StudioNotifyService studioNotifyService)
    : ApiControllerBase
{
    /// <summary>
    /// Sends a notification to the user with the ID specified in the request to change their phone number.
    /// </summary>
    /// <short>
    /// Send a notification to change a phone
    /// </short>
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMemberRequestDto, ASC.People" name="inDto">Request parameters for updating user contacts</param>
    /// <returns type="System.Object, System">Notification</returns>
    /// <path>api/2.0/people/phone</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("phone")]
    public async Task<object> SendNotificationToChangeAsync(UpdateMemberRequestDto inDto)
    {
        var user = await userManager.GetUsersAsync(string.IsNullOrEmpty(inDto.UserId)
            ? securityContext.CurrentAccount.ID : new Guid(inDto.UserId));

        var canChange = user.IsMe(authContext) || await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        if (!canChange)
        {
            throw new SecurityAccessDeniedException(Resource.ErrorAccessDenied);
        }

        user.MobilePhoneActivationStatus = MobilePhoneActivationStatus.NotActivated;

        await userManager.UpdateUserInfoAsync(user);

        if (user.IsMe(authContext))
        {
            return await commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.PhoneActivation);
        }

        await studioNotifyService.SendMsgMobilePhoneChangeAsync(user);

        return string.Empty;
    }
}