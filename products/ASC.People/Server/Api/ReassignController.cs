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

[DefaultRoute("reassign")]
public class ReassignController(
    PermissionContext permissionContext,
    QueueWorkerReassign queueWorkerReassign,
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    SecurityContext securityContext,
    WebItemSecurity webItemSecurity)
    : ApiControllerBase
    {
    /// <summary>
    /// Returns the progress of the started data reassignment for the user with the ID specified in the request.
    /// </summary>
    /// <short>Get the reassignment progress</short>
    /// <path>api/2.0/people/reassign/progress/{userid}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [HttpGet("progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetReassignProgressAsync(ProgressRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var progressItem = await queueWorkerReassign.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <summary>
    /// Starts the data reassignment for the user with the ID specified in the request.
    /// </summary>
    /// <short>Start the data reassignment</short>
    /// <path>api/2.0/people/reassign/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "Can not reassign data to user or from user")]
    [HttpPost("start")]
    public async Task<TaskProgressResponseDto> StartReassignAsync(StartReassignRequestDto inDto)
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
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
        if (userManager.IsSystemUser(fromUser.Id) || 
            fromUser.IsOwner(tenant) || 
            fromUser.IsMe(authContext) || 
            await userManager.IsUserAsync(toUser) || 
            fromUser.Status != EmployeeStatus.Terminated || 
            ((await userManager.IsDocSpaceAdminAsync(inDto.FromUserId) || await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, inDto.FromUserId)) && tenant.OwnerId != authContext.CurrentAccount.ID))
        {
            throw new ArgumentException("Can not reassign data from user with id = " + fromUser.Id);
        }

        var progressItem = await queueWorkerReassign.StartAsync(tenant.Id, fromUser.Id, toUser.Id, securityContext.CurrentAccount.ID, true, inDto.DeleteProfile);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <summary>
    /// Terminates the data reassignment for the user with the ID specified in the request.
    /// </summary>
    /// <short>Terminate the data reassignment</short>
    /// <path>api/2.0/people/reassign/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "Reassignment progress", typeof(TaskProgressResponseDto))]
    [HttpPut("terminate")]
    public async Task<TaskProgressResponseDto> TerminateReassignAsync(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var progressItem = await queueWorkerReassign.GetProgressItemStatus(tenant.Id, inDto.UserId);

        if (progressItem != null)
        {
            await queueWorkerReassign.Terminate(tenant.Id, inDto.UserId);

            progressItem.Status = DistributedTaskStatus.Canceled;
            progressItem.IsCompleted = true;
        }

        return TaskProgressResponseDto.Get(progressItem);
    }
}
