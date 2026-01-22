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

using ASC.People.ApiModels.V3.ResponseDto.Common;

/// <summary>
/// Base controller for all v3 API endpoints with REST-compliant helper methods.
/// </summary>
/// <remarks>
/// This base controller provides:
/// - Standardized error responses following RFC 7807 principles
/// - Helper methods for REST-compliant status codes (201 Created, 202 Accepted, 204 No Content)
/// - Consistent response formatting across all v3 endpoints
/// - Integration with ASP.NET Core API versioning
///
/// All v3 controllers should inherit from this base class to ensure consistency
/// and proper REST semantics throughout the API.
///
/// Design Principles:
/// - Follow HTTP/1.1 RFC 7231 for status codes and semantics
/// - Provide meaningful error responses with machine-readable codes
/// - Include Location headers for created resources (201 Created)
/// - Support content negotiation (JSON by default)
/// - Enable OpenAPI/Swagger documentation generation
/// </remarks>
[ApiController]
[Route("api/3.0/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class ApiControllerBaseV3 : ControllerBase
{
    /// <summary>
    /// Returns a 201 Created response with a Location header pointing to the new resource.
    /// </summary>
    /// <typeparam name="T">The type of the response body</typeparam>
    /// <param name="actionName">The name of the action to generate the Location URL</param>
    /// <param name="routeValues">Route values for the action (typically includes resource ID)</param>
    /// <param name="value">The created resource to include in the response body</param>
    /// <returns>201 Created response with Location header and resource body</returns>
    /// <remarks>
    /// Use this method when creating new resources via POST requests.
    /// The Location header helps clients discover the URL of the newly created resource.
    ///
    /// Example:
    /// return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    /// </remarks>
    protected IActionResult CreatedAtAction<T>(string actionName, object routeValues, T value)
    {
        var location = Url.Action(actionName, routeValues);
        if (!string.IsNullOrEmpty(location))
        {
            Response.Headers.Append("Location", location);
        }

        return StatusCode(StatusCodes.Status201Created, value);
    }

    /// <summary>
    /// Returns a 202 Accepted response indicating that the request has been accepted for processing.
    /// </summary>
    /// <typeparam name="T">The type of the response body</typeparam>
    /// <param name="location">The URL where the client can check the status of the operation</param>
    /// <param name="value">The response body (typically contains operation ID and status)</param>
    /// <returns>202 Accepted response with Location header</returns>
    /// <remarks>
    /// Use this method for asynchronous operations that will complete later.
    /// The Location header should point to a status endpoint where clients can poll for completion.
    ///
    /// Common use cases:
    /// - Long-running data processing operations
    /// - Batch operations
    /// - User data reassignments
    /// - Quota recalculations
    ///
    /// Example:
    /// return Accepted($"/api/3.0/users/reassignments/{operationId}", statusDto);
    /// </remarks>
    protected IActionResult Accepted<T>(string location, T value)
    {
        if (!string.IsNullOrEmpty(location))
        {
            Response.Headers.Append("Location", location);
        }

        return StatusCode(StatusCodes.Status202Accepted, value);
    }

    /// <summary>
    /// Returns a 204 No Content response indicating successful completion with no response body.
    /// </summary>
    /// <returns>204 No Content response</returns>
    /// <remarks>
    /// Use this method for:
    /// - DELETE operations (successful deletion)
    /// - PUT operations when no response body is needed
    /// - PATCH operations when no response body is needed
    ///
    /// This is the preferred response for mutations that don't need to return data,
    /// as it reduces bandwidth and improves performance.
    ///
    /// Example:
    /// await _userManager.DeleteUserAsync(id);
    /// return NoContent();
    /// </remarks>
    protected IActionResult NoContent()
    {
        return StatusCode(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// Creates a standardized error response with the specified error code and message.
    /// </summary>
    /// <param name="code">Machine-readable error code for programmatic handling</param>
    /// <param name="message">Human-readable error message</param>
    /// <param name="statusCode">HTTP status code (default: 400 Bad Request)</param>
    /// <returns>Error response with standardized format</returns>
    /// <remarks>
    /// This method creates RFC 7807-compliant error responses that include:
    /// - Error code for programmatic handling
    /// - Human-readable message
    /// - Timestamp for troubleshooting
    /// - Request ID for log correlation
    ///
    /// Common error codes:
    /// - ValidationFailed: One or more fields failed validation
    /// - Unauthorized: Authentication required
    /// - Forbidden: Insufficient permissions
    /// - NotFound: Resource doesn't exist
    /// - Conflict: Resource state conflict (e.g., duplicate email)
    /// - QuotaExceeded: Operation would exceed quota limits
    /// - InvalidOperation: Operation not allowed in current state
    ///
    /// Example:
    /// if (user == null)
    ///     return Error("NotFound", $"User with ID {id} not found", 404);
    /// </remarks>
    protected IActionResult Error(string code, string message, int statusCode = 400)
    {
        var error = new ErrorResponseDtoV3
        {
            Error = new ErrorDetailDtoV3
            {
                Code = code,
                Message = message,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };

        return StatusCode(statusCode, error);
    }

    /// <summary>
    /// Creates a validation error response with field-level error details.
    /// </summary>
    /// <param name="code">Machine-readable error code (typically "ValidationFailed")</param>
    /// <param name="message">General error message</param>
    /// <param name="validationErrors">Field-level validation errors</param>
    /// <param name="statusCode">HTTP status code (default: 422 Unprocessable Entity)</param>
    /// <returns>Error response with field-level validation details</returns>
    /// <remarks>
    /// Use this method when multiple fields fail validation.
    /// Returns 422 Unprocessable Entity by default, which indicates that the request
    /// was well-formed but contains semantic errors.
    ///
    /// Example:
    /// var errors = new List<ValidationErrorDtoV3>
    /// {
    ///     new() { Field = "email", Message = "Email is already in use" },
    ///     new() { Field = "password", Message = "Password must be at least 8 characters" }
    /// };
    /// return ValidationError("ValidationFailed", "Validation failed", errors);
    /// </remarks>
    protected IActionResult ValidationError(
        string code,
        string message,
        IEnumerable<ValidationErrorDtoV3> validationErrors,
        int statusCode = 422)
    {
        var error = new ErrorResponseDtoV3
        {
            Error = new ErrorDetailDtoV3
            {
                Code = code,
                Message = message,
                Details = validationErrors,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };

        return StatusCode(statusCode, error);
    }
}
