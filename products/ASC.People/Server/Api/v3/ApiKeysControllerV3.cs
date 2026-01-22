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
// the GNU AGPL at: http://creativecommons.org/licenses/agpl-3.0/html
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
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// RESTful API v3 for managing API keys in the DocSpace system.
/// </summary>
/// <remarks>
/// This controller provides full CRUD operations for API key management,
/// following REST best practices and RFC 7231 standards.
///
/// API Keys Overview:
/// API keys provide programmatic access to DocSpace without user credentials.
/// They enable:
/// - Machine-to-machine authentication
/// - Scoped permissions (read/write/admin)
/// - Expiration dates for security
/// - Per-user or global access
///
/// Security Considerations:
/// - Keys are sensitive credentials and should be stored securely
/// - Keys can have expiration dates
/// - Permissions can be scoped to specific operations
/// - Only admins can view all keys; users see only their own
/// - Guest users cannot create keys
///
/// Permission Scopes:
/// - Global read: Read access to all resources
/// - Global write: Write access to all resources
/// - Module-specific: Scoped to Files, CRM, etc.
/// </remarks>
[ApiController]
[Route("api/3.0/api-keys")]
[Tags("API Keys (v3)")]
[Produces("application/json")]
[Consumes("application/json")]
public class ApiKeysControllerV3 : ApiControllerBaseV3
{
    private readonly ApiKeysController _apiKeysController;
    private readonly ILogger<ApiKeysControllerV3> _logger;

    public ApiKeysControllerV3(
        ApiKeysController apiKeysController,
        ILogger<ApiKeysControllerV3> logger)
    {
        _apiKeysController = apiKeysController;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a list of API keys.
    /// </summary>
    /// <remarks>
    /// Returns API keys visible to the current user.
    ///
    /// Visibility Rules:
    /// - Admins see all API keys across all users
    /// - Regular users see only their own keys
    /// - Guest users see only their own keys (but cannot create)
    ///
    /// Permissions:
    /// Requires authenticated user.
    /// </remarks>
    /// <returns>List of API keys</returns>
    /// <response code="200">API keys retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IAsyncEnumerable<ApiKeyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status401Unauthorized)]
    public IAsyncEnumerable<ApiKeyResponseDto> GetApiKeys()
    {
        try
        {
            return _apiKeysController.GetApiKeys();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            throw;
        }
    }

    /// <summary>
    /// Retrieves information about the current API key.
    /// </summary>
    /// <remarks>
    /// Returns details about the API key used to authenticate the current request.
    ///
    /// Use Case:
    /// Allows an API client to introspect its own key to check:
    /// - Permissions/scopes
    /// - Expiration date
    /// - Active status
    /// - Creation date
    ///
    /// Permissions:
    /// Must be authenticated with an API key (not user session).
    /// </remarks>
    /// <returns>Current API key information</returns>
    /// <response code="200">API key retrieved successfully</response>
    /// <response code="401">Not authenticated with API key</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiKeyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentApiKey()
    {
        try
        {
            var result = await _apiKeysController.GetApiKey();
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error("Unauthorized", ex.Message, 401);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current API key");
            return Error("InternalError", "An error occurred while retrieving the API key", 500);
        }
    }

    /// <summary>
    /// Retrieves available API key permissions.
    /// </summary>
    /// <remarks>
    /// Returns a list of all permission scopes that can be assigned to API keys.
    ///
    /// Permission Types:
    /// - Global scopes: read, write (all modules)
    /// - Module scopes: files:read, files:write, crm:read, etc.
    ///
    /// Use Case:
    /// Use this endpoint to discover available permissions before creating
    /// or updating an API key.
    ///
    /// Permissions:
    /// Requires authenticated user.
    /// </remarks>
    /// <returns>List of available permission strings</returns>
    /// <response code="200">Permissions retrieved successfully</response>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetPermissions()
    {
        try
        {
            var permissions = _apiKeysController.GetAllPermissions();
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key permissions");
            return Error("InternalError", "An error occurred while retrieving permissions", 500);
        }
    }

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <remarks>
    /// Creates an API key with specified name, permissions, and optional expiration.
    ///
    /// Side Effects:
    /// - API key is generated and stored
    /// - Key is returned ONLY in creation response (not retrievable later)
    /// - Audit log entry is created
    ///
    /// Security Notes:
    /// - The full API key is returned only once during creation
    /// - Store the key securely - it cannot be retrieved again
    /// - If lost, you must delete and recreate the key
    ///
    /// Business Rules:
    /// - Guest users cannot create keys
    /// - If LimitedAccessForUsers is enabled, only admins can create keys
    /// - Permissions must be valid scopes from /api/3.0/api-keys/permissions
    /// - ExpiresInDays is optional; omit for non-expiring keys
    ///
    /// Rate Limiting:
    /// This endpoint is rate-limited due to security sensitivity.
    /// </remarks>
    /// <param name="request">API key creation request</param>
    /// <returns>The newly created API key with the secret</returns>
    /// <response code="201">API key created successfully</response>
    /// <response code="400">Invalid permissions or parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Insufficient permissions (guest or limited access)</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequestDto request)
    {
        try
        {
            var result = await _apiKeysController.CreateApiKey(request);
            return CreatedAtAction(nameof(GetApiKeys), result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return Error("InternalError", "An error occurred while creating the API key", 500);
        }
    }

    /// <summary>
    /// Updates an existing API key.
    /// </summary>
    /// <remarks>
    /// Updates the name, permissions, and/or active status of an API key.
    ///
    /// Side Effects:
    /// - Key metadata is updated
    /// - Permissions changes take effect immediately
    /// - If deactivated, key becomes unusable until reactivated
    /// - Audit log entry is created
    ///
    /// Business Rules:
    /// - Only key owner or admin can update
    /// - Expired keys cannot be updated (must recreate)
    /// - Permissions must be valid scopes
    ///
    /// Permissions:
    /// - Admins can update any key
    /// - Users can update only their own keys
    /// </remarks>
    /// <param name="id">API key ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Success status</returns>
    /// <response code="200">API key updated successfully</response>
    /// <response code="400">Invalid permissions or expired key</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">API key not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApiKey(
        [FromRoute] Guid id,
        [FromBody] UpdateApiKeyRequestDto request)
    {
        try
        {
            request.KeyId = id;
            var result = await _apiKeysController.UpdateApiKey(request);

            if (!result)
            {
                return Error("UpdateFailed", "API key update failed. Key may be expired or you lack permissions.", 403);
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return Error("NotFound", $"API key with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key {KeyId}", id);
            return Error("InternalError", "An error occurred while updating the API key", 500);
        }
    }

    /// <summary>
    /// Deletes an API key.
    /// </summary>
    /// <remarks>
    /// Permanently removes an API key from the system.
    ///
    /// Side Effects:
    /// - API key is deleted and becomes unusable immediately
    /// - Any requests using this key will fail with 401 Unauthorized
    /// - Audit log entry is created
    ///
    /// Business Rules:
    /// - Only key owner or admin can delete
    /// - Deletion is permanent and cannot be undone
    ///
    /// Permissions:
    /// - Admins can delete any key
    /// - Users can delete only their own keys
    /// </remarks>
    /// <param name="id">API key ID to delete</param>
    /// <returns>Success status</returns>
    /// <response code="200">API key deleted successfully (returns true)</response>
    /// <response code="403">Insufficient permissions (returns false)</response>
    /// <response code="404">API key not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApiKey([FromRoute] Guid id)
    {
        try
        {
            var result = await _apiKeysController.DeleteApiKey(id);

            if (!result)
            {
                return Error("DeleteFailed", "API key deletion failed. You may lack permissions to delete this key.", 403);
            }

            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return Error("NotFound", $"API key with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key {KeyId}", id);
            return Error("InternalError", "An error occurred while deleting the API key", 500);
        }
    }
}
