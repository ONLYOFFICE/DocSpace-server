// (c) Copyright Ascensio System SIA 2009-2026
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
// the GNU AGPL at: http://creativecommons.org/licenses/agpl-3.0.html
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

namespace ASC.People.Api.V3;

using ASC.People.ApiModels.V3.ResponseDto.Common;
using ASC.People.ApiModels.RequestDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

/// <summary>
/// RESTful API v3 for managing user data removal operations.
/// </summary>
/// <remarks>
/// This controller provides operations for permanently deleting user data,
/// following REST best practices and RFC 7231 standards.
///
/// Data Removal Overview:
/// Permanently deletes all user data including:
/// - Files and documents
/// - Shared folders and rooms
/// - Comments and activity history
/// - Personal settings
///
/// WARNING: This is a destructive operation that cannot be undone.
///
/// Workflow:
/// 1. Request self-deletion via POST /users/me/deletion-requests
/// 2. Start admin-initiated deletion via POST /users/data-removals
/// 3. Monitor progress via GET /users/data-removals/{userId}
/// 4. Optionally terminate via DELETE
///
/// Business Rules:
/// - LDAP users cannot self-delete
/// - Owners cannot self-delete
/// - User must be Terminated before admin deletion
/// - Operation is asynchronous and may take hours
///
/// GDPR Compliance:
/// This endpoint supports "right to be forgotten" under GDPR.
/// </remarks>
[ApiController]
[Route("api/3.0/users")]
[Tags("User Data Removal (v3)")]
[Produces("application/json")]
[Consumes("application/json")]
public class UserDataRemovalControllerV3 : ApiControllerBaseV3
{
    private readonly RemoveUserDataController _removeUserDataController;
    private readonly ILogger<UserDataRemovalControllerV3> _logger;

    public UserDataRemovalControllerV3(
        RemoveUserDataController removeUserDataController,
        ILogger<UserDataRemovalControllerV3> logger)
    {
        _removeUserDataController = removeUserDataController;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the progress status of a data removal operation.
    /// </summary>
    /// <remarks>
    /// Returns the current status of an ongoing or completed data deletion.
    ///
    /// Status Values:
    /// - NotStarted: Operation queued but not started
    /// - Running: Currently deleting data
    /// - Completed: All data deleted successfully
    /// - Failed: Error occurred during deletion
    /// - Canceled: Terminated by administrator
    ///
    /// Progress Information:
    /// - Percentage: 0-100% completion
    /// - Status: Current state
    /// - Error message if failed
    /// - Items processed/remaining
    ///
    /// Polling Recommendations:
    /// - Poll every 5-10 seconds while running
    /// - Stop polling when status is Completed/Failed/Canceled
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// </remarks>
    /// <param name="userId">User ID whose data removal to check</param>
    /// <returns>Progress information</returns>
    /// <response code="200">Progress retrieved successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">No data removal operation found</response>
    [HttpGet("data-removals/{userId:guid}")]
    [ProducesResponseType(typeof(TaskProgressResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDataRemovalProgress([FromRoute] Guid userId)
    {
        try
        {
            var v2Request = new UserIdRequestDto { UserId = userId };
            var result = await _removeUserDataController.GetRemoveProgress(v2Request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data removal progress for user {UserId}", userId);
            return Error("InternalError", "An error occurred while retrieving data removal progress", 500);
        }
    }

    /// <summary>
    /// Starts a new data removal operation for a specific user.
    /// </summary>
    /// <remarks>
    /// Initiates an asynchronous operation to permanently delete all user data.
    ///
    /// WARNING: This is a destructive operation that cannot be undone!
    ///
    /// Side Effects:
    /// - Background task is queued
    /// - All user files are permanently deleted
    /// - All shared rooms are removed
    /// - All comments and activity are erased
    /// - Personal settings are cleared
    /// - Audit log entries created
    ///
    /// Business Rules:
    /// - User must be in Terminated status
    /// - User cannot be owner or system user
    /// - User cannot be current authenticated user
    /// - Operation cannot be reversed
    ///
    /// Data Deleted:
    /// - Files in "My Documents"
    /// - Shared files and folders
    /// - Room memberships
    /// - Comments and activity history
    /// - Personal settings and preferences
    ///
    /// Response:
    /// Returns 202 Accepted with task status.
    /// Use GET /api/3.0/users/data-removals/{userId} to monitor progress.
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// </remarks>
    /// <param name="request">Data removal request with user ID</param>
    /// <returns>Initial task status</returns>
    /// <response code="202">Data removal started successfully</response>
    /// <response code="400">User not terminated or other violation</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPost("data-removals")]
    [ProducesResponseType(typeof(TaskProgressResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartDataRemoval([FromBody] TerminateRequestDto request)
    {
        try
        {
            var result = await _removeUserDataController.StartRemove(request);
            return Accepted($"/api/3.0/users/data-removals/{request.UserId}", result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting data removal");
            return Error("InternalError", "An error occurred while starting data removal", 500);
        }
    }

    /// <summary>
    /// Terminates an ongoing data removal operation.
    /// </summary>
    /// <remarks>
    /// Cancels a running data deletion operation.
    ///
    /// Side Effects:
    /// - Background task is stopped
    /// - Partial data may already be deleted (cannot be recovered)
    /// - Operation cannot be resumed - must restart from beginning
    /// - Audit log entry created
    ///
    /// Important Notes:
    /// - Only running operations can be terminated
    /// - Already deleted data CANNOT be recovered
    /// - Partial state: Some data may already be permanently deleted
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// </remarks>
    /// <param name="userId">User ID whose data removal to terminate</param>
    /// <returns>No content</returns>
    /// <response code="204">Data removal terminated successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">No active data removal operation found</response>
    [HttpDelete("data-removals/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateDataRemoval([FromRoute] Guid userId)
    {
        try
        {
            var v2Request = new TerminateRequestDto { UserId = userId };
            await _removeUserDataController.TerminateRemove(v2Request);

            return NoContent();
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating data removal for user {UserId}", userId);
            return Error("InternalError", "An error occurred while terminating data removal", 500);
        }
    }

    /// <summary>
    /// Requests self-deletion instructions for the current user.
    /// </summary>
    /// <remarks>
    /// Sends an email to the current user with instructions for deleting their profile.
    ///
    /// GDPR "Right to be Forgotten":
    /// This endpoint supports GDPR compliance by allowing users to request
    /// deletion of their personal data.
    ///
    /// Side Effects:
    /// - Email sent to user's registered email address
    /// - Email contains confirmation link
    /// - Audit log entry created
    ///
    /// Business Rules:
    /// - LDAP users cannot self-delete (managed externally)
    /// - Owner cannot self-delete (transfer ownership first)
    /// - User must confirm via email link
    ///
    /// Rate Limiting:
    /// This endpoint is rate-limited to prevent abuse.
    ///
    /// Permissions:
    /// User must be authenticated.
    /// </remarks>
    /// <returns>Success message with email address</returns>
    /// <response code="200">Instructions sent successfully</response>
    /// <response code="403">LDAP user or owner cannot self-delete</response>
    [HttpPost("me/deletion-requests")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RequestSelfDeletion()
    {
        try
        {
            var result = await _removeUserDataController.SendInstructionsToDelete();
            return Ok(new { message = result });
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", "LDAP users and owners cannot request self-deletion", 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending self-deletion instructions");
            return Error("InternalError", "An error occurred while sending deletion instructions", 500);
        }
    }
}
