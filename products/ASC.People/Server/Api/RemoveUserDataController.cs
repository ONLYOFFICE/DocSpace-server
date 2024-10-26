// (c) Copyright Ascensio System SIA 2009-2024
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

public class RemoveUserDataController(PermissionContext permissionContext,
        UserManager userManager,
        QueueWorkerRemove queueWorkerRemove,
        SecurityContext securityContext,
        StudioNotifyService studioNotifyService,
        MessageService messageService,
        AuthContext authContext,
        TenantManager tenantManager)
    : ApiControllerBase
    {
    /// <summary>
    /// Returns the progress of the started data deletion for the user with the ID specified in the request.
    /// </summary>
    /// <short>Get the deletion progress</short>
    /// <param type="System.Guid, System" name="userId">User ID</param>
    /// <category>User data</category>
    /// <returns type="ASC.People.ApiModels.ResponseDto.TaskProgressResponseDto, ASC.People">Deletion progress</returns>
    /// <path>api/2.0/people/remove/progress/{userid}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("remove/progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetRemoveProgressAsync(Guid userId)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var progressItem = await queueWorkerRemove.GetProgressItemStatus(tenant.Id, userId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <summary>
    /// Sends instructions for deleting a user profile.
    /// </summary>
    /// <short>
    /// Send the deletion instructions
    /// </short>
    /// <category>Profiles</category>
    /// <returns type="System.Object, System">Information message</returns>
    /// <path>api/2.0/people/self/delete</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("self/delete")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<object> SendInstructionsToDeleteAsync()
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
        if (user.IsLDAP() || user.IsOwner(tenant))
        {
            throw new SecurityException();
        }

        await studioNotifyService.SendMsgProfileDeletionAsync(user);
        await messageService.SendAsync(MessageAction.UserSentDeleteInstructions);

        return string.Format(Resource.SuccessfullySentNotificationDeleteUserInfoMessage, "<b>" + user.Email + "</b>");
    }

    /// <summary>
    /// Starts the data deletion for the user with the ID specified in the request.
    /// </summary>
    /// <short>Start the data deletion</short>
    /// <param type="ASC.People.ApiModels.RequestDto.TerminateRequestDto, ASC.People" name="inDto">Request parameters for starting the deletion process</param>
    /// <category>User data</category>
    /// <returns type="ASC.People.ApiModels.ResponseDto.TaskProgressResponseDto, ASC.People">Deletion progress</returns>
    /// <path>api/2.0/people/remove/start</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("remove/start")]
    public async Task<TaskProgressResponseDto> StartRemoveAsync(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var user = await userManager.GetUsersAsync(inDto.UserId);

        if (user == null || user.Id == Constants.LostUser.Id)
        {
            throw new ArgumentException("User with id = " + inDto.UserId + " not found");
        }

        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await userManager.GetUserTypeAsync(currentUser.Id); 
        
        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (user.IsOwner(tenant) || user.IsMe(authContext) || user.Status != EmployeeStatus.Terminated)
        {
            throw new ArgumentException("Can not delete user with id = " + inDto.UserId);
        }
        
        var userType = await userManager.GetUserTypeAsync(user); 

        switch (userType)
        {
            case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
            case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                throw new SecurityException(Resource.ErrorAccessDenied);
        }
        
        var isGuest = await userManager.IsGuestAsync(user);

        var progressItem = await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, true, true, isGuest);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <summary>
    /// Terminates the data deletion for the user with the ID specified in the request.
    /// </summary>
    /// <short>Terminate the data deletion</short>
    /// <param type="ASC.People.ApiModels.RequestDto.TerminateRequestDto, ASC.People" name="inDto">Request parameters for terminating the deletion process</param>
    /// <category>User data</category>
    /// <path>api/2.0/people/remove/terminate</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns></returns>
    [HttpPut("remove/terminate")]
    public async Task TerminateRemoveAsync(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        await queueWorkerRemove.Terminate(tenant.Id, inDto.UserId);
    }
}
