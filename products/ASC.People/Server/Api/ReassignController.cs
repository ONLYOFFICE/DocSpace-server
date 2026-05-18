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
/// Reassign API.
///</remarks>
[DefaultRoute("reassign")]
public class ReassignController(
    PermissionContext permissionContext,
    QueueWorkerReassign queueWorkerReassign,
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    SecurityContext securityContext,
    WebItemSecurity webItemSecurity,
    FileStorageService fileStorageService)
    : ApiControllerBase
{
    /// <remarks>
    /// Returns the progress of the started data reassignment for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Get the reassignment progress</summary>
    /// <path>api/2.0/people/reassign/progress/{userid}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [HttpGet("progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetReassignProgress(UserIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var userType = await userManager.GetUserTypeAsync(inDto.UserId);

        if (userType is EmployeeType.DocSpaceAdmin && !securityContext.CurrentAccount.ID.Equals(tenant.OwnerId))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var progressItem = await queueWorkerReassign.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Starts the data reassignment for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Start the data reassignment</summary>
    /// <path>api/2.0/people/reassign/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "Can not reassign data to user or from user")]
    [HttpPost("start")]
    public async Task<TaskProgressResponseDto> StartReassign(StartReassignRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var toUser = await userManager.GetUsersAsync(inDto.ToUserId);

        var toUserType = await userManager.GetUserTypeAsync(toUser);
        var toUserIsAdmin = toUserType is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin;

        if (userManager.IsSystemUser(toUser.Id) ||
            !toUserIsAdmin ||
            toUser.Status == EmployeeStatus.Terminated)
        {
            throw new ArgumentException("Can not reassign data to user with id = " + toUser.Id);
        }

        var fromUser = await userManager.GetUsersAsync(inDto.FromUserId);
        var tenant = tenantManager.GetCurrentTenant();

        if (userManager.IsSystemUser(fromUser.Id) ||
            fromUser.IsOwner(tenant) ||
            fromUser.IsMe(authContext) ||
            await userManager.IsGuestAsync(toUser) ||
            fromUser.Status != EmployeeStatus.Terminated ||
            ((await userManager.IsDocSpaceAdminAsync(inDto.FromUserId) || await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, inDto.FromUserId)) && tenant.OwnerId != authContext.CurrentAccount.ID))
        {
            throw new ArgumentException("Can not reassign data from user with id = " + fromUser.Id);
        }

        var progressItem = await queueWorkerReassign.StartAsync(tenant.Id, fromUser.Id, toUser.Id, securityContext.CurrentAccount.ID, true, inDto.DeleteProfile);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Terminates the data reassignment for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>Terminate the data reassignment</summary>
    /// <path>api/2.0/people/reassign/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [HttpPut("terminate")]
    public async Task<TaskProgressResponseDto> TerminateReassign(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var userType = await userManager.GetUserTypeAsync(inDto.UserId);

        if (userType is EmployeeType.DocSpaceAdmin && !securityContext.CurrentAccount.ID.Equals(tenant.OwnerId))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var progressItem = await queueWorkerReassign.GetProgressItemStatus(tenant.Id, inDto.UserId);

        if (progressItem != null)
        {
            await queueWorkerReassign.Terminate(tenant.Id, inDto.UserId);

            progressItem.Status = DistributedTaskStatus.Canceled;
            progressItem.IsCompleted = true;
        }

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Checks whether the reassignment of rooms and shared files is required.
    /// </remarks>
    /// <summary>Check data for reassignment need</summary>
    /// <path>api/2.0/people/reassign/necessary</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Boolean value: true if neccessary reassign", typeof(bool))]
    [HttpGet("necessary")]
    public async Task<bool> NecessaryReassign([FromQuery] NecessaryReassignDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.Type), Constants.Action_AddRemoveUser);

        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var userType = await userManager.GetUserTypeAsync(inDto.UserId);
        var tenant = tenantManager.GetCurrentTenant();

        if (!currentUser.IsOwner(tenant) && userType is EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var result = await fileStorageService.AnyRoomsAsync(inDto.UserId);

        if (inDto.Type is EmployeeType.Guest && !result)
        {
            result = await fileStorageService.GetSharedEntriesCountAsync(inDto.UserId) > 0;
        }

        return result;
    }
}