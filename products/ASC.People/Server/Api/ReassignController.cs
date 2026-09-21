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

/// <remarks>
/// Reassign API.
/// </remarks>
[ApiEndpoint(Template = "reassign")]
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
    /// Returns the current state of the data reassignment queued for the user with the ID specified in the request.
    /// A reassignment must have been queued by `POST api/2.0/people/reassign/start` first: when nothing is queued for
    /// that user the operation answers 200 with an empty body.
    /// The caller needs the permission to edit users, and only the portal owner may track a reassignment whose source
    /// user is a DocSpace administrator.
    /// The call is read-only and is the polling operation of the reassignment flow - repeat it until `isCompleted` is
    /// true, reading `percentage` for the 0 to 100 progress and `error` for the message left by a failed job.
    /// Use `PUT api/2.0/people/reassign/terminate` to cancel a job that is still running.
    /// </remarks>
    /// <summary>Get the reassignment progress</summary>
    /// <path>api/2.0/people/reassign/progress/{userid}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued reassignment, or an empty body when nothing is queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Queues an asynchronous job that transfers the rooms and the shared files owned by one portal user to another.
    /// The source user must already have the `Terminated` status - disable the account through
    /// `PUT api/2.0/people/status/{status}` before calling this - and the destination user must be an active room
    /// admin or DocSpace admin, so a guest, a system account or a disabled account is rejected.
    /// The caller needs the permission to edit users, cannot reassign their own data, and must be the portal owner to
    /// reassign the data of another DocSpace administrator or of a People module administrator.
    /// The transfer does not finish within this call: poll `GET api/2.0/people/reassign/progress/{userid}` with the
    /// source user ID until `isCompleted` is true, and cancel it through `PUT api/2.0/people/reassign/terminate`.
    /// Pass `deleteProfile` as true to delete the source profile once the transfer succeeds, otherwise the emptied
    /// profile is kept.
    /// Use `GET api/2.0/people/reassign/necessary` first to find out whether the user owns anything that has to be
    /// reassigned at all.
    /// </remarks>
    /// <summary>Start the data reassignment</summary>
    /// <path>api/2.0/people/reassign/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued reassignment", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The destination user is not an active room or DocSpace admin, or the source user is a system account, the portal owner, the caller, or is not disabled")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Cancels the data reassignment queued for the user with the ID specified in the request.
    /// The caller needs the permission to edit users, and only the portal owner may cancel a reassignment whose
    /// source user is a DocSpace administrator.
    /// The operation is idempotent: when nothing is queued for that user it answers 200 with an empty body, and
    /// repeating it on an already cancelled job changes nothing.
    /// Cancelling removes the job from the queue and does not undo the transfers it has already made, and a cancelled
    /// job cannot be resumed - start a new one through `POST api/2.0/people/reassign/start`.
    /// The returned progress reports `status` as `Canceled` and `isCompleted` as true.
    /// </remarks>
    /// <summary>Terminate the data reassignment</summary>
    /// <path>api/2.0/people/reassign/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the cancelled reassignment, or an empty body when nothing was queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Reports whether the rooms and the shared files of a user have to be reassigned before that user can be removed
    /// or changed to the type passed in `type`.
    /// Call it before `DELETE api/2.0/people/{userid}` or before a type change to find out whether
    /// `POST api/2.0/people/reassign/start` has to run first.
    /// The caller needs the permission to add and remove users of the requested type, and must be the portal owner
    /// when the checked user is a DocSpace administrator.
    /// The call is read-only and answers true when the user owns at least one room, or - when `type` is `Guest` -
    /// when the user still has shared files.
    /// A false answer means the user can be removed or converted without a reassignment.
    /// </remarks>
    /// <summary>Check data for reassignment need</summary>
    /// <path>api/2.0/people/reassign/necessary</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "True if the data of the user has to be reassigned before the removal or the type change", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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