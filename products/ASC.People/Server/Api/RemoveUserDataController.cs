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
/// Remove user data API.
///</remarks>
public class RemoveUserDataController(PermissionContext permissionContext,
        UserManager userManager,
        QueueWorkerRemove queueWorkerRemove,
        QueueDeletePersonalFolder queueDeletePersonalFolder,
        SecurityContext securityContext,
        StudioNotifyService studioNotifyService,
        MessageService messageService,
        AuthContext authContext,
        TenantManager tenantManager)
    : ApiControllerBase
{
    /// <remarks>
    /// Returns the progress of the started data deletion for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Get the deletion progress</summary>
    /// <path>api/2.0/people/remove/progress/{userid}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Deletion progress", typeof(TaskProgressResponseDto))]
    [HttpGet("remove/progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetRemoveProgress(UserIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerRemove.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Sends the instructions for deleting a user profile.
    /// </remarks>
    /// <summary>
    /// Send the deletion instructions
    /// </summary>
    /// <path>api/2.0/people/self/delete</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Information message", typeof(string))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("self/delete")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendInstructionsToDelete()
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var tenant = tenantManager.GetCurrentTenant();

        if (user.IsLDAP() || user.IsOwner(tenant))
        {
            throw new SecurityException();
        }

        await studioNotifyService.SendMsgProfileDeletionAsync(user);
        messageService.Send(MessageAction.UserSentDeleteInstructions);

        return string.Format(Resource.SuccessfullySentNotificationDeleteUserInfoMessage, "<b>" + user.Email + "</b>");
    }

    /// <remarks>
    /// Starts the data deletion for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Start the data deletion</summary>
    /// <path>api/2.0/people/remove/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Deletion progress", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "User exception")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [HttpPost("remove/start")]
    public async Task<TaskProgressResponseDto> StartRemove(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var user = await userManager.GetUsersAsync(inDto.UserId);

        if (user == null || user.Id == Constants.LostUser.Id)
        {
            throw new ArgumentException("User with id = " + inDto.UserId + " not found");
        }

        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await userManager.GetUserTypeAsync(currentUser.Id);

        var tenant = tenantManager.GetCurrentTenant();
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

        var isGuest = await userManager.IsGuestAsync(user.Id);

        var progressItem = await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, true, true, isGuest);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Terminates the data deletion for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Terminate the data deletion</summary>
    /// <path>api/2.0/people/remove/terminate</path>
    [Tags("People / User data")]
    [HttpPut("remove/terminate")]
    public async Task TerminateRemove(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        await queueWorkerRemove.Terminate(tenant.Id, inDto.UserId);
    }

    /// <remarks>
    /// Starts deleting the personal folder.
    /// </remarks>
    /// <summary>Delete the personal folder</summary>
    /// <path>api/2.0/people/delete/personal/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "delete personal progress", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "Access denied")]
    [HttpPost("delete/personal/start")]
    public async Task<TaskProgressResponseDto> StartDeletePersonalFolder()
    {
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var userType = await userManager.GetUserTypeAsync(currentUser);

        var tenantId = tenantManager.GetCurrentTenantId();

        if (userType != EmployeeType.Guest)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var progressItem = await queueDeletePersonalFolder.StartAsync(tenantId, securityContext.CurrentAccount.ID);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Returns the progress of deleting the personal folder.
    /// </remarks>
    /// <summary>Get the progress of deleting the personal folder</summary>
    /// <path>api/2.0/people/delete/personal/progress</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Deletion progress", typeof(TaskProgressResponseDto))]
    [HttpGet("delete/personal/progress")]
    public async Task<TaskProgressResponseDto> GetDeletePersonalFolderProgress()
    {
        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueDeletePersonalFolder.GetProgressItemStatus(tenant.Id, securityContext.CurrentAccount.ID);

        return TaskProgressResponseDto.Get(progressItem);
    }
}