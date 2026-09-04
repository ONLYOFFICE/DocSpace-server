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
    /// Returns the current state of the data deletion queued for the user with the ID specified in the request.
    /// A deletion must have been queued by `POST api/2.0/people/remove/start` first: when nothing is queued for that
    /// user the operation answers 200 with an empty body.
    /// The caller needs the permission to edit users.
    /// The call is read-only and is the polling operation of the deletion flow - repeat it until `isCompleted` is
    /// true, reading `percentage` for the 0 to 100 progress and `error` for the message left by a failed job.
    /// Use `PUT api/2.0/people/remove/terminate` to cancel a job that is still running.
    /// </remarks>
    /// <summary>Get the deletion progress</summary>
    /// <path>api/2.0/people/remove/progress/{userid}</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued deletion, or an empty body when nothing is queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("remove/progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetRemoveProgress(UserIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerRemove.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Emails the caller a confirmation link that lets them delete their own profile, and is the first step of the
    /// self-service profile removal.
    /// It acts on the authenticated account only and takes no parameters, so it cannot be used to remove somebody
    /// else - an administrator removes another user through `DELETE api/2.0/people/{userid}`.
    /// The caller has to be a regular portal account: the portal owner and an account imported from LDAP are
    /// rejected, because neither can delete itself.
    /// The call sends mail and does not change the profile; the deletion happens later, when the caller follows the
    /// emailed link and the client calls `DELETE api/2.0/people/@self` with the confirmation token from it.
    /// The answer is a ready-to-display message naming the address the link was sent to, and the address is wrapped
    /// in bold HTML markup, so strip the markup before showing it outside a web page.
    /// Repeated calls are throttled, and each one sends a new link.
    /// </remarks>
    /// <summary>
    /// Send the deletion instructions
    /// </summary>
    /// <path>api/2.0/people/self/delete</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The message stating which address the confirmation link was sent to", typeof(string))]
    [SwaggerResponse(403, "The caller is the portal owner or an LDAP account and cannot delete their own profile")]
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
    /// Queues an asynchronous job that erases the data of the user with the ID specified in the request.
    /// The account must already have the `Terminated` status - disable it through
    /// `PUT api/2.0/people/status/{status}` first - and it cannot be the portal owner or the caller.
    /// The caller needs the permission to edit users, has to be a DocSpace admin to erase the data of a room admin,
    /// and has to be the portal owner to erase the data of another DocSpace admin.
    /// The erasure does not finish within this call: poll `GET api/2.0/people/remove/progress/{userid}` with the same
    /// user ID until `isCompleted` is true, and cancel it through `PUT api/2.0/people/remove/terminate`.
    /// This operation destroys the data and cannot be undone; to keep the rooms and the shared files of the account
    /// instead, transfer them first through `POST api/2.0/people/reassign/start`.
    /// An unknown ID and a rejected precondition both answer 400 and name the ID they rejected.
    /// </remarks>
    /// <summary>Start the data deletion</summary>
    /// <path>api/2.0/people/remove/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued deletion", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "No user has the specified ID, or the account is the portal owner, the caller, or is not disabled")]
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
    /// Cancels the data deletion queued for the user with the ID specified in the request.
    /// The caller needs the permission to edit users.
    /// The operation is idempotent and returns no body: it drops the job from the queue, and doing so when nothing is
    /// queued, or when the job has already finished, changes nothing and still answers 200.
    /// Cancelling does not restore the data the job has already erased, and a cancelled job cannot be resumed - start
    /// a new one through `POST api/2.0/people/remove/start`.
    /// To find out whether the job is still running, read
    /// `GET api/2.0/people/remove/progress/{userid}` before and after this call.
    /// </remarks>
    /// <summary>Terminate the data deletion</summary>
    /// <path>api/2.0/people/remove/terminate</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The queued deletion is cancelled, or there was nothing to cancel. No content is returned")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("remove/terminate")]
    public async Task TerminateRemove(TerminateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        await queueWorkerRemove.Terminate(tenant.Id, inDto.UserId);
    }

    /// <remarks>
    /// Queues an asynchronous job that empties the personal folder of the authenticated account.
    /// The operation takes no parameters and always acts on the caller, so it cannot be used to empty the folder of
    /// another user.
    /// Only an account whose type is `Guest` may call it; every other type is rejected, because only a guest has a
    /// personal folder that can be emptied this way.
    /// The job does not finish within this call: poll `GET api/2.0/people/delete/personal/progress` until
    /// `isCompleted` is true.
    /// The job deletes the files permanently and cannot be undone or cancelled - there is no terminate operation for
    /// this flow, unlike the user data deletion.
    /// </remarks>
    /// <summary>Delete the personal folder</summary>
    /// <path>api/2.0/people/delete/personal/start</path>
    [Tags("People / User data")]
    [SwaggerResponse(200, "The state of the queued personal folder deletion", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "The caller is not a guest, so there is no personal folder to empty")]
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
    /// Returns the current state of the personal folder deletion queued for the authenticated account.
    /// The job must have been queued by `POST api/2.0/people/delete/personal/start` first: when nothing is queued for
    /// the caller the operation answers 200 with an empty body.
    /// It takes no parameters and reports on the caller only, so an administrator cannot watch the folder deletion of
    /// another user through it.
    /// The call is read-only and is the polling operation of this flow - repeat it until `isCompleted` is true, and
    /// read `error` for the message left by a failed job.
    /// A queued personal folder deletion cannot be cancelled, so the only outcome to wait for is its completion.
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