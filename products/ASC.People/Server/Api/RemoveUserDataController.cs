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
/// Remove user data API.
/// </remarks>
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
    /// Returns the progress of the job that deletes the data of a user.
    /// To start the job, use `POST api/2.0/people/remove/start`. Requires a DocSpace admin. Other callers get 403.
    /// The call is read-only. This is the call to poll. Repeat it until `isCompleted` is true.
    /// If no job exists for the user, the response has no progress object. If the job fails, `error` holds the reason.
    /// To cancel a running job, use `PUT api/2.0/people/remove/terminate`.
    /// </remarks>
    /// <summary>Get the deletion progress</summary>
    /// <path>api/2.0/people/remove/progress/{userId}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued deletion, or an empty body when nothing is queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("remove/progress/{userId:guid}")]
    public async Task<TaskProgressResponseDto> GetRemoveProgress(UserIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerRemove.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Emails the caller a link that confirms the deletion of their own profile. Any signed-in user can call it. The
    /// portal owner and LDAP accounts get 403. The call changes no data and sends an email. Within 10 minutes after the
    /// email, a repeated call sends no new email but returns the same text. A later call emails a new link. Every link
    /// sent stays valid for 7 days. To delete the profile, the app calls `DELETE api/2.0/people/@self` with the
    /// confirmation token from the link. To delete another account, use `DELETE api/2.0/people/{userId}`. The call
    /// returns a text that you can show to the user, with the email address of the link. The address is wrapped in HTML
    /// bold tags. Remove them before you show the text outside a web page.
    /// </remarks>
    /// <summary>
    /// Send the deletion instructions
    /// </summary>
    /// <path>api/2.0/people/self/delete</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The message stating which address the confirmation link was sent to", typeof(string))]
    [SwaggerResponse(403, "The caller is the portal owner or an LDAP account and cannot delete their own profile")]
    [HttpPut("self/delete")]
    public async Task<string> SendInstructionsToDelete()
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var tenant = tenantManager.GetCurrentTenant();

        if (user.IsLDAP() || user.IsOwner(tenant))
        {
            throw new SecurityException();
        }

        if (await studioNotifyService.SendMsgProfileDeletionAsync(user))
        {
            messageService.Send(MessageAction.UserSentDeleteInstructions);
        }

        return string.Format(Resource.SuccessfullySentNotificationDeleteUserInfoMessage, "<b>" + user.Email + "</b>");
    }

    /// <remarks>
    /// Starts a job that deletes the data and the profile of a disabled user.
    /// The job deletes the personal files of the account. It moves the rooms and third-party storage accounts to the
    /// caller. Files and folders that the account shared from the personal section move to a new folder of the caller.
    /// First, disable the account through `PUT api/2.0/people/status/{status}`.
    /// Requires a DocSpace admin. Other callers get 403.
    /// Only the portal owner can delete the data of another DocSpace admin. Other DocSpace admins get 403 for it.
    /// The call queues work, and the deletion cannot be undone.
    /// Repeat `GET api/2.0/people/remove/progress/{userId}` with the same user ID until `isCompleted` is true.
    /// While a job for the account runs, a repeated call returns that job and starts nothing new. After the job
    /// succeeds, a repeated call returns 400. After a failed job, a repeated call starts a new one. The portal emails
    /// the caller when the job succeeds or fails. To cancel a running job, use `PUT api/2.0/people/remove/terminate`.
    /// To move the data to a user you choose, use `POST api/2.0/people/reassign/start` instead. The call returns 400 if
    /// the user is unknown, the portal owner, the caller, or not disabled.
    /// </remarks>
    /// <summary>Start the data deletion</summary>
    /// <path>api/2.0/people/remove/start</path>
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "when the job moves shared files of the account to a new folder of the caller")]
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued deletion", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`, no user has the specified ID, or the account is the portal owner, the caller, or is not disabled")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Cancels the job that deletes the data of a user.
    /// Requires a DocSpace admin. Other callers get 403.
    /// The call changes data. It removes the job from the queue and returns no body.
    /// It does not restore the data that the job already deleted. You cannot resume the job.
    /// To start again, use `POST api/2.0/people/remove/start`.
    /// If no job exists for the user, nothing changes. So you can repeat the call.
    /// To check whether the job still runs, use `GET api/2.0/people/remove/progress/{userId}`.
    /// The call returns 400 if the body has no `userId`.
    /// </remarks>
    /// <summary>Terminate the data deletion</summary>
    /// <path>api/2.0/people/remove/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The queued deletion is cancelled, or there was nothing to cancel. No content is returned")]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("remove/terminate")]
    public async Task TerminateRemove(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        await queueWorkerRemove.Terminate(tenant.Id, inDto.UserId);
    }

    /// <remarks>
    /// Starts a job that deletes the personal folder of the caller.
    /// Only a guest can call it. Other callers get 403. If the guest has no personal folder, the call also returns 403.
    /// Files and folders that the caller shared from the folder move to a new folder of another member.
    /// That member last changed the personal folder. If that member is a guest, the portal owner gets the new folder.
    /// The call queues work, and the deletion cannot be undone. You cannot cancel the job.
    /// Repeat `GET api/2.0/people/delete/personal/progress` until `isCompleted` is true.
    /// While the job runs, a repeated call returns that job. After the job succeeds, a repeated call returns 403. If
    /// the job fails and the folder remains, a repeated call starts a new job.
    /// </remarks>
    /// <summary>Delete the personal folder</summary>
    /// <path>api/2.0/people/delete/personal/start</path>
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "when the job moves shared files of the caller to a new folder of another member")]
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued personal folder deletion", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "The caller is not a guest, or is a guest whose personal folder has not been created")]
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
    /// Returns the progress of the job that deletes the personal folder of the caller.
    /// To start the job, use `POST api/2.0/people/delete/personal/start`. Any signed-in user can call it.
    /// The call is read-only. This is the call to poll. Repeat it until `isCompleted` is true.
    /// If no job exists for the caller, the response has no progress object.
    /// If the job fails, `error` holds the reason. You cannot cancel the job.
    /// </remarks>
    /// <summary>Get the personal folder deletion progress</summary>
    /// <path>api/2.0/people/delete/personal/progress</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued personal folder deletion, or an empty body when nothing is queued for the caller", typeof(TaskProgressResponseDto))]
    [HttpGet("delete/personal/progress")]
    public async Task<TaskProgressResponseDto> GetDeletePersonalFolderProgress()
    {
        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueDeletePersonalFolder.GetProgressItemStatus(tenant.Id, securityContext.CurrentAccount.ID);

        return TaskProgressResponseDto.Get(progressItem);
    }
}