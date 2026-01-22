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
/// RESTful API v3 for managing user data reassignments.
/// </summary>
/// <remarks>
/// This controller provides operations for reassigning user data (files, rooms, etc.)
/// from one user to another, following REST best practices and RFC 7231 standards.
///
/// Reassignment Overview:
/// When a user leaves an organization, their data (files, rooms, shared documents)
/// needs to be reassigned to another user. This is an asynchronous long-running operation.
///
/// Workflow:
/// 1. Start reassignment via POST
/// 2. Monitor progress via GET
/// 3. Optionally terminate via DELETE
///
/// Business Rules:
/// - Source user must be Terminated status
/// - Target user must be Admin or Room Admin
/// - Cannot reassign from owner or system users
/// - Only owner can reassign admin data
/// - Operation is asynchronous and may take minutes to hours
///
/// Use Cases:
/// - Employee leaves company - reassign their files to manager
/// - User account cleanup - move data before deletion
/// - Organizational restructuring - redistribute ownership
/// </remarks>
[ApiController]
[Route("api/3.0/users/reassignments")]
[Tags("User Reassignments (v3)")]
[Produces("application/json")]
[Consumes("application/json")]
public class UserReassignmentsControllerV3 : ApiControllerBaseV3
{
    private readonly ReassignController _reassignController;
    private readonly ILogger<UserReassignmentsControllerV3> _logger;

    public UserReassignmentsControllerV3(
        ReassignController reassignController,
        ILogger<UserReassignmentsControllerV3> logger)
    {
        _reassignController = reassignController;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the progress status of a data reassignment operation.
    /// </summary>
    /// <remarks>
    /// Returns the current status of an ongoing or completed reassignment.
    ///
    /// Status Values:
    /// - NotStarted: Operation queued but not started
    /// - Running: Currently processing
    /// - Completed: Successfully finished
    /// - Failed: Error occurred
    /// - Canceled: Terminated by user
    ///
    /// Progress Information:
    /// - Percentage: 0-100% completion
    /// - Status: Current state
    /// - Error message if failed
    ///
    /// Polling Recommendations:
    /// - Poll every 5-10 seconds while running
    /// - Stop polling when status is Completed/Failed/Canceled
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// Only owner can check admin reassignments.
    /// </remarks>
    /// <param name="userId">User ID whose reassignment to check</param>
    /// <returns>Progress information</returns>
    /// <response code="200">Progress retrieved successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">No reassignment found for this user</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(TaskProgressResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReassignmentProgress([FromRoute] Guid userId)
    {
        try
        {
            var v2Request = new UserIdRequestDto { UserId = userId };
            var result = await _reassignController.GetReassignProgress(v2Request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reassignment progress for user {UserId}", userId);
            return Error("InternalError", "An error occurred while retrieving reassignment progress", 500);
        }
    }

    /// <summary>
    /// Starts a new data reassignment operation.
    /// </summary>
    /// <remarks>
    /// Initiates an asynchronous operation to reassign data from one user to another.
    ///
    /// Side Effects:
    /// - Background task is queued
    /// - Files, rooms, and shared documents are transferred
    /// - Optionally deletes source user profile after completion
    /// - Audit log entries created
    ///
    /// Business Rules:
    /// - FromUserId must be a Terminated user
    /// - FromUserId cannot be owner, system user, or current user
    /// - ToUserId must be Admin or Room Admin
    /// - ToUserId cannot be Guest or Terminated
    /// - Only owner can reassign from admin users
    ///
    /// DeleteProfile Parameter:
    /// - true: Delete source user after reassignment completes
    /// - false: Keep source user profile (recommended for audit)
    ///
    /// Response:
    /// Returns 202 Accepted with task status.
    /// Use GET /api/3.0/users/reassignments/{userId} to monitor progress.
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// Only owner can reassign admin data.
    /// </remarks>
    /// <param name="request">Reassignment request with source/target users</param>
    /// <returns>Initial task status</returns>
    /// <response code="202">Reassignment started successfully</response>
    /// <response code="400">Invalid users or business rule violation</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskProgressResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartReassignment([FromBody] StartReassignRequestDto request)
    {
        try
        {
            var result = await _reassignController.StartReassign(request);
            return Accepted($"/api/3.0/users/reassignments/{request.FromUserId}", result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Can not reassign"))
        {
            return Error("InvalidUser", ex.Message, 400);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting reassignment");
            return Error("InternalError", "An error occurred while starting reassignment", 500);
        }
    }

    /// <summary>
    /// Terminates an ongoing data reassignment operation.
    /// </summary>
    /// <remarks>
    /// Cancels a running reassignment operation.
    ///
    /// Side Effects:
    /// - Background task is stopped
    /// - Partial data may already be reassigned
    /// - Operation cannot be resumed - must restart from beginning
    /// - Audit log entry created
    ///
    /// Important Notes:
    /// - Only running operations can be terminated
    /// - Already completed reassignments cannot be reversed
    /// - Partial state: Some data may have been transferred
    ///
    /// Permissions:
    /// Requires Action_EditUser permission.
    /// Only owner can terminate admin reassignments.
    /// </remarks>
    /// <param name="userId">User ID whose reassignment to terminate</param>
    /// <returns>Final task status</returns>
    /// <response code="200">Reassignment terminated successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">No active reassignment found</response>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(typeof(TaskProgressResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateReassignment([FromRoute] Guid userId)
    {
        try
        {
            var v2Request = new TerminateRequestDto { UserId = userId };
            var result = await _reassignController.TerminateReassign(v2Request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating reassignment for user {UserId}", userId);
            return Error("InternalError", "An error occurred while terminating reassignment", 500);
        }
    }
}
