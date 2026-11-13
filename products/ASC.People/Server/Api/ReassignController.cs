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
    /// Returns the progress of the job that moves the data of a user to another user.
    /// To start the job, use `POST api/2.0/people/reassign/start`. Requires a DocSpace admin. Other callers get 403.
    /// Only the portal owner can read the job of a source user who is a DocSpace admin.
    /// Other DocSpace admins get 403 for it. The call is read-only. This is the call to poll.
    /// Repeat it until `isCompleted` is true. If no job exists for the user, the response has no progress object.
    /// If the job fails, `error` holds the reason.
    /// To cancel a running job, use `PUT api/2.0/people/reassign/terminate`.
    /// </remarks>
    /// <summary>Get the reassignment progress</summary>
    /// <path>api/2.0/people/reassign/progress/{userId}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued reassignment, or an empty body when nothing is queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("progress/{userId:guid}")]
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
    /// Starts a job that moves the data of a disabled user to another user.
    /// The job moves the rooms, third-party storage accounts, and files of the source.
    /// Files and folders that the source shared from the personal section move to a new folder of the recipient.
    /// First, disable the source account through `PUT api/2.0/people/status/{status}`.
    /// To check whether any data needs to move, use `GET api/2.0/people/reassign/necessary`.
    /// Requires a DocSpace admin. Other callers get 403.
    /// Only the portal owner can move the data of a DocSpace admin or an admin of the People section.
    /// Other callers get 400 for such a source. The call queues work.
    /// Repeat `GET api/2.0/people/reassign/progress/{userId}` with the source ID until `isCompleted` is true.
    /// While a job for the source runs, a repeated call returns that job and starts nothing new. After the job ends, a
    /// repeated call starts a new job. The portal then emails the caller again. If the job deleted the source profile,
    /// a repeated call returns 400. If `deleteProfile` is true, the job also deletes the other personal files of the
    /// source, then the profile. This cannot be undone. Otherwise, the source keeps the personal files that it did not
    /// share. The portal emails the caller when the job succeeds or fails. The call returns 400 if the recipient is
    /// unknown or is not an active room admin or DocSpace admin. It also returns 400 if the source is unknown, a system
    /// account, the portal owner, the caller, or not disabled.
    /// </remarks>
    /// <summary>Start the data reassignment</summary>
    /// <path>api/2.0/people/reassign/start</path>
    [RaisesWebhook(WebhookTrigger.UserDeleted, When = "when `deleteProfile` is true and the job succeeds")]
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "when the job creates a folder for shared files, but not `room.updated` for moved rooms")]
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued reassignment", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `fromUserId` or `toUserId`, the destination user does not exist or is not an active room or DocSpace admin, or the source user does not exist, is a system account, the portal owner, the caller, is not disabled, or is a DocSpace or People administrator while the caller is not the portal owner")]
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
    /// Cancels the job that moves the data of a user to another user.
    /// Requires a DocSpace admin. Other callers get 403.
    /// Only the portal owner can cancel the job of a source user who is a DocSpace admin.
    /// Other DocSpace admins get 403 for it. The call changes data. It removes the job from the queue.
    /// It does not undo the moves that the job already made. You cannot resume the job.
    /// To start again, use `POST api/2.0/people/reassign/start`.
    /// The returned progress has `status` `Canceled` and `isCompleted` true, even for a job that already finished.
    /// If no job exists for the user, the response has no progress object. So you can repeat the call.
    /// The call returns 400 if the body has no `userId`.
    /// </remarks>
    /// <summary>Terminate the data reassignment</summary>
    /// <path>api/2.0/people/reassign/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the cancelled reassignment, or an empty body when nothing was queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`")]
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
    /// Returns whether a user's data must move to another user before you delete the user or change its type.
    /// Call it before `DELETE api/2.0/people/{userId}` or a type change.
    /// If it returns true, first move the data through `POST api/2.0/people/reassign/start`.
    /// Requires the right to add and remove users of the type in `type`. Other callers get 403.
    /// Only the portal owner can check a DocSpace admin. Other callers get 403 for one. The call is read-only.
    /// It returns true if the user owns a room. If `type` is `Guest`, it also returns true for shared files.
    /// False means that you can delete the user or change the type without moving data.
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