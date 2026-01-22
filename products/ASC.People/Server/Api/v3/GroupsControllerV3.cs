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

using ASC.People.ApiModels.V3.ResponseDto.Groups;
using ASC.People.ApiModels.V3.ResponseDto.Common;
using ASC.People.ApiModels.V3.RequestDto.Groups;
using ASC.People.ApiModels.RequestDto;
using ASC.People.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// RESTful API v3 for managing groups (departments) in the DocSpace system.
/// </summary>
/// <remarks>
/// This controller provides full CRUD operations for group management,
/// following REST best practices and RFC 7231 standards.
///
/// Groups (Departments) Overview:
/// Groups are organizational units used to structure users within a tenant.
/// They enable:
/// - Hierarchical organization of users
/// - Group-based access control
/// - Simplified permissions management
/// - Team collaboration features
///
/// Each group can have:
/// - A unique name
/// - One manager (optional)
/// - Multiple members
/// - Associated permissions and access rights
/// </remarks>
[ApiController]
[Route("api/3.0/groups")]
[Tags("Groups (v3)")]
[Produces("application/json")]
[Consumes("application/json")]
public class GroupsControllerV3 : ApiControllerBaseV3
{
    private readonly GroupDtoMapperV3 _mapper;
    private readonly GroupController _groupController;
    private readonly ILogger<GroupsControllerV3> _logger;

    public GroupsControllerV3(
        GroupDtoMapperV3 mapper,
        GroupController groupController,
        ILogger<GroupsControllerV3> logger)
    {
        _mapper = mapper;
        _groupController = groupController;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of groups.
    /// </summary>
    /// <remarks>
    /// Returns all groups the current user has permission to view.
    ///
    /// Filtering Options:
    /// - By user membership (userId parameter)
    /// - By search query (q parameter - matches group name)
    ///
    /// Permissions:
    /// Requires Action_ReadGroups permission.
    /// </remarks>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (1-100)</param>
    /// <param name="q">Search query to match group names</param>
    /// <returns>Paginated list of groups</returns>
    /// <response code="200">Groups retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="403">Insufficient permissions</response>
    [HttpGet]
    [ProducesResponseType(typeof(GroupListResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string q = null)
    {
        try
        {
            if (page < 1)
            {
                return Error("InvalidPage", "Page number must be at least 1", 400);
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Error("InvalidPageSize", "Page size must be between 1 and 100", 400);
            }

            var v2Request = new GeneralInformationRequestDto
            {
                StartIndex = (page - 1) * pageSize,
                Count = pageSize,
                Text = q
            };

            var groupsAsync = _groupController.GetGroups(v2Request);
            var groupsList = new List<GroupDto>();

            await foreach (var group in groupsAsync)
            {
                groupsList.Add(group);
            }

            var totalCount = groupsList.Count;
            var response = _mapper.FromDomainList(groupsList, totalCount, page, pageSize);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups list");
            return Error("InternalError", "An error occurred while retrieving groups", 500);
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific group.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <returns>Group details including manager and members</returns>
    /// <response code="200">Group found</response>
    /// <response code="404">Group not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup([FromRoute] Guid id)
    {
        try
        {
            var v2Request = new DetailedInformationRequestDto { Id = id };
            var group = await _groupController.GetGroup(v2Request);

            if (group == null)
            {
                return Error("NotFound", $"Group with ID {id} not found", 404);
            }

            var response = _mapper.FromDomain(group);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group {GroupId}", id);
            return Error("InternalError", "An error occurred while retrieving the group", 500);
        }
    }

    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <remarks>
    /// Creates a group with specified name, manager, and initial members.
    ///
    /// Side Effects:
    /// - Group is created in the system
    /// - Manager is assigned and added as member
    /// - Members are added to the group
    /// - Access control lists are updated
    /// - Webhook GroupCreated event is fired
    /// - Audit log entry is created
    /// </remarks>
    /// <param name="request">Group creation request</param>
    /// <returns>The newly created group</returns>
    /// <response code="201">Group created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="409">Group name already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupResponseDtoV3), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestDtoV3 request)
    {
        try
        {
            var v2Request = _mapper.ToV2CreateRequest(request);
            var createdGroup = await _groupController.AddGroup(v2Request);

            var response = _mapper.FromDomain(createdGroup);

            return CreatedAtAction(nameof(GetGroup), new { id = createdGroup.Id }, response);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
        {
            return Error("GroupNameExists", "A group with this name already exists", 409);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return Error("InternalError", "An error occurred while creating the group", 500);
        }
    }

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    /// <remarks>
    /// Updates group name, manager, and/or membership.
    ///
    /// Side Effects:
    /// - Group information is updated
    /// - Manager changes trigger permission updates
    /// - Member additions/removals update access control
    /// - Webhook GroupUpdated event is fired
    /// </remarks>
    /// <param name="id">Group ID to update</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated group information</returns>
    /// <response code="200">Group updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">Group not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GroupResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroup(
        [FromRoute] Guid id,
        [FromBody] UpdateGroupRequestDtoV3 request)
    {
        try
        {
            var v2Request = new UpdateGroupRequestDto
            {
                Id = id,
                Update = _mapper.ToV2UpdateRequest(request)
            };

            var updatedGroup = await _groupController.UpdateGroup(v2Request);
            var response = _mapper.FromDomain(updatedGroup);

            return Ok(response);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return Error("NotFound", $"Group with ID {id} not found", 404);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return Error("InternalError", "An error occurred while updating the group", 500);
        }
    }

    /// <summary>
    /// Partially updates a group.
    /// </summary>
    /// <remarks>
    /// Only updates the provided fields.
    /// </remarks>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(GroupResponseDtoV3), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchGroup(
        [FromRoute] Guid id,
        [FromBody] UpdateGroupRequestDtoV3 request)
    {
        return await UpdateGroup(id, request);
    }

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <remarks>
    /// Permanently removes the group from the system.
    ///
    /// Side Effects:
    /// - Group is deleted
    /// - Members lose group-based permissions
    /// - Access control lists are updated
    /// - Webhook GroupDeleted event is fired
    ///
    /// Note: This does not delete the users, only the group structure.
    /// </remarks>
    /// <param name="id">Group ID to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Group deleted successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">Group not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid id)
    {
        try
        {
            var v2Request = new GetGroupByIdRequestDto { Id = id };
            await _groupController.DeleteGroup(v2Request);

            return NoContent();
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return Error("NotFound", $"Group with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return Error("InternalError", "An error occurred while deleting the group", 500);
        }
    }
}
