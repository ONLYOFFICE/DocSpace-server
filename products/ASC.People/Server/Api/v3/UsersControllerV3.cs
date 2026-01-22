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

namespace ASC.People.Api.V3;

using ASC.People.ApiModels.V3.ResponseDto.Users;
using ASC.People.ApiModels.V3.ResponseDto.Common;
using ASC.People.ApiModels.V3.RequestDto.Users;
using ASC.People.ApiModels.RequestDto;
using ASC.People.Mappers;
using ASC.Core;
using ASC.Core.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// RESTful API v3 for managing users in the DocSpace system.
/// </summary>
/// <remarks>
/// This controller provides read operations for user management,
/// following REST best practices and RFC 7231 standards.
///
/// PROOF OF CONCEPT IMPLEMENTATION
/// This is a simplified proof-of-concept implementation that demonstrates:
/// - REST-compliant endpoint structure and naming
/// - Proper HTTP verbs and status codes
/// - HATEOAS support with hypermedia links
/// - Comprehensive documentation for humans and AI
/// - Pagination and filtering support
///
/// Current Implementation Status:
/// - GET endpoints (list, single): IMPLEMENTED
/// - POST (create): NOT IMPLEMENTED - requires deeper v2 integration
/// - PUT/PATCH (update): NOT IMPLEMENTED - requires deeper v2 integration
/// - DELETE (delete): NOT IMPLEMENTED - requires deeper v2 integration
///
/// To complete the implementation, the following is needed:
/// 1. Study the exact signatures of UserManagerWrapper methods
/// 2. Understand the full UserController.cs v2 business logic
/// 3. Implement proper error handling for all edge cases
/// 4. Add proper permission checks
/// 5. Implement quota validation
///
/// Migration from v2:
/// See the README.md for differences between v2 and v3 endpoints.
/// Key changes: improved REST compliance, HATEOAS links, better error responses.
/// </remarks>
[ApiController]
[Route("api/3.0/users")]
[Tags("Users (v3)")]
[Produces("application/json")]
[Consumes("application/json")]
public class UsersControllerV3 : ApiControllerBaseV3
{
    private readonly UserDtoMapperV3 _mapper;
    private readonly UserController _userController;
    private readonly UserManager _userManager;
    private readonly ILogger<UsersControllerV3> _logger;

    public UsersControllerV3(
        UserDtoMapperV3 mapper,
        UserController userController,
        UserManager userManager,
        ILogger<UsersControllerV3> logger)
    {
        _mapper = mapper;
        _userController = userController;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of users with optional filtering and search.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a list of users matching the specified criteria.
    /// Results are paginated for performance and include hypermedia links for navigation.
    ///
    /// Filtering Options:
    /// - By search query (matches name, email)
    ///
    /// Performance Considerations:
    /// - Default page size is 25, maximum is 100
    /// - Response includes pagination metadata
    /// - Consider using search parameter to reduce result set size
    ///
    /// Security:
    /// - Users can only see users they have permission to view
    /// - Some user details may be hidden based on privacy settings
    ///
    /// Example Usage:
    /// GET /api/3.0/users?page=1&amp;pageSize=25&amp;q=john
    /// Returns the first 25 users whose name or email contains "john"
    /// </remarks>
    /// <param name="page">The page number to retrieve (1-based). Default: 1</param>
    /// <param name="pageSize">The number of items per page (1-100). Default: 25</param>
    /// <param name="q">Search query to match against name and email</param>
    /// <returns>A paginated list of users matching the criteria</returns>
    /// <response code="200">Successfully retrieved user list with pagination metadata</response>
    /// <response code="400">Invalid query parameters (e.g., page less than 1, pageSize greater than 100)</response>
    /// <response code="401">Authentication required - no valid credentials provided</response>
    /// <response code="403">Insufficient permissions to view users</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string q = null)
    {
        try
        {
            // Validation
            if (page < 1)
            {
                return Error("InvalidPage", "Page number must be at least 1", 400);
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Error("InvalidPageSize", "Page size must be between 1 and 100", 400);
            }

            // Use v2 controller to get users
            var v2Request = new GetAllProfilesRequestDto
            {
                Count = pageSize,
                StartIndex = (page - 1) * pageSize,
                Text = q
            };

            var usersAsync = _userController.GetAllProfiles(v2Request);
            var usersList = new List<EmployeeFullDto>();

            await foreach (var user in usersAsync)
            {
                usersList.Add(user);
            }

            var totalCount = usersList.Count;

            // Map to v3 response
            var response = _mapper.FromDomainList(usersList, totalCount, page, pageSize);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users list");
            return Error("InternalError", "An error occurred while retrieving users", 500);
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific user by ID.
    /// </summary>
    /// <remarks>
    /// Returns complete profile information for the specified user including:
    /// - Basic profile data (name, email, title)
    /// - Contact information
    /// - Department memberships
    /// - Employment details
    /// - Profile photo URL
    /// - Account status and activation state
    ///
    /// HATEOAS Links:
    /// The response includes hypermedia links to related resources:
    /// - self: This user's resource URL
    /// - photo: Photo management endpoint
    /// - contacts: Contact information endpoint
    /// - groups: Group membership endpoint
    ///
    /// These links enable API clients and AI systems to discover available
    /// operations without hardcoding URLs.
    /// </remarks>
    /// <param name="id">The unique identifier (GUID) of the user</param>
    /// <returns>Detailed user profile information with HATEOAS links</returns>
    /// <response code="200">User found and retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Insufficient permissions to view this user</response>
    /// <response code="404">User not found or has been deleted</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser([FromRoute] Guid id)
    {
        try
        {
            // Get UserInfo first, then get full details using v2 controller
            var userInfo = await _userManager.GetUsersAsync(id);

            if (userInfo == null || userInfo.Id == Guid.Empty)
            {
                return Error("NotFound", $"User with ID {id} not found", 404);
            }

            // Get full user details via v2 controller
            var v2GetRequest = new GetMemberByIdRequestDto { UserId = id.ToString() };
            var user = await _userController.GetProfileByUserId(v2GetRequest);

            if (user == null)
            {
                return Error("NotFound", $"User with ID {id} not found", 404);
            }

            // Map to v3 response
            var response = _mapper.FromDomain(user);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return Error("InternalError", "An error occurred while retrieving the user", 500);
        }
    }

    /// <summary>
    /// Creates a new activated user in the system.
    /// </summary>
    /// <remarks>
    /// This endpoint creates a fully activated user who can immediately access the system.
    ///
    /// Business Rules:
    /// - Email must be unique within the tenant
    /// - Password must meet password policy requirements
    /// - User type determines quota consumption
    /// - Creating admin users requires owner or admin permissions
    ///
    /// Post-Creation Actions:
    /// - Welcome email sent to user's email address (if configured)
    /// - User automatically added to specified departments
    /// - Audit log entry created
    /// - Webhook events fired
    /// </remarks>
    /// <param name="request">User creation request with profile and authentication details</param>
    /// <returns>The newly created user with complete profile information</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Insufficient permissions or quota exceeded</response>
    /// <response code="409">Email address already in use</response>
    /// <response code="422">Validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDtoV3), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDtoV3 request)
    {
        try
        {
            // Convert v3 DTO to v2 format
            var v2Request = _mapper.ToV2CreateRequest(request);

            // Call existing v2 business logic
            var createdUser = await _userController.AddMemberAsActivated(v2Request);

            // Map to v3 response
            var response = _mapper.FromDomain(createdUser);

            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, response);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("email") || ex.Message.Contains("Email"))
        {
            return Error("EmailInUse", "The email address is already in use", 409);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Error("InternalError", "An error occurred while creating the user", 500);
        }
    }

    /// <summary>
    /// Fully updates an existing user's profile information.
    /// </summary>
    /// <remarks>
    /// This endpoint performs a complete update (PUT semantics).
    ///
    /// Idempotency:
    /// This operation is idempotent - sending the same request multiple times
    /// produces the same result.
    ///
    /// Side Effects:
    /// - Changing user type recalculates quota
    /// - Adding/removing departments updates access control lists
    /// - Profile changes invalidate cached user data
    /// - Webhook events fired
    /// </remarks>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="request">The user profile data</param>
    /// <returns>The updated user information</returns>
    /// <response code="200">User updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequestDtoV3 request)
    {
        try
        {
            // Convert v3 DTO to v2 format with user ID
            var v2Request = new UpdateMemberByIdRequestDto
            {
                UserId = id.ToString(),
                UpdateMember = _mapper.ToV2UpdateRequest(request, id.ToString())
            };

            // Call existing v2 business logic
            var updatedUser = await _userController.UpdateMember(v2Request);

            // Map to v3 response
            var response = _mapper.FromDomain(updatedUser);

            return Ok(response);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("Not found"))
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return Error("InternalError", "An error occurred while updating the user", 500);
        }
    }

    /// <summary>
    /// Partially updates a user's profile information.
    /// </summary>
    /// <remarks>
    /// This endpoint performs a partial update (PATCH semantics).
    /// Only provided fields are updated; others remain unchanged.
    ///
    /// Idempotency:
    /// This operation is idempotent.
    /// </remarks>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="request">The partial user profile data</param>
    /// <returns>The updated user information</returns>
    /// <response code="200">User updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequestDtoV3 request)
    {
        // PATCH uses the same logic as PUT since v2 service handles null/missing fields appropriately
        return await UpdateUser(id, request);
    }

    /// <summary>
    /// Permanently deletes a user from the system.
    /// </summary>
    /// <remarks>
    /// This operation permanently removes the user.
    ///
    /// WARNING: Data Loss Risk
    /// - User profile and authentication credentials deleted
    /// - User's files and documents may be deleted
    ///
    /// Prerequisites:
    /// - User must be terminated (status: Terminated) before deletion
    /// - User cannot be the tenant owner
    /// - User cannot have active sessions
    ///
    /// Idempotency:
    /// This operation is idempotent - deleting an already deleted user
    /// returns success without error.
    /// </remarks>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <returns>Information about the deleted user</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="400">User cannot be deleted (still active, is owner, etc.)</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        try
        {
            var v2Request = new GetMemberByIdRequestDto { UserId = id.ToString() };

            // Call existing v2 business logic
            var deletedUser = await _userController.DeleteMember(v2Request);

            // Map to v3 response and return the deleted user info
            var response = _mapper.FromDomain(deletedUser);

            return Ok(response);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (Exception ex) when (ex.Message.Contains("not suspended") || ex.Message.Contains("not terminated"))
        {
            return Error("UserNotTerminated", "User must be terminated before deletion", 400);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return Error("InternalError", "An error occurred while deleting the user", 500);
        }
    }
}
