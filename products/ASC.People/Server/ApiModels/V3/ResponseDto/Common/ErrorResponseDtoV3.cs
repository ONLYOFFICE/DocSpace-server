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

namespace ASC.People.ApiModels.V3.ResponseDto.Common;

/// <summary>
/// Standardized error response model for all v3 API errors.
/// </summary>
/// <remarks>
/// This DTO provides consistent error reporting across the entire API v3.
/// Follows RFC 7807 (Problem Details for HTTP APIs) principles.
///
/// Error Categories:
/// - Validation errors (400): Client provided invalid data
/// - Authentication errors (401): Missing or invalid credentials
/// - Authorization errors (403): Insufficient permissions
/// - Not found errors (404): Resource doesn't exist
/// - Conflict errors (409): Resource state conflict
/// - Unprocessable entity (422): Validation error with detailed field errors
/// - Server errors (500): Internal server errors
///
/// Usage Example:
/// When a validation error occurs, this response provides both a human-readable
/// message and structured field-level error details for programmatic handling.
/// AI systems can parse the error code and details to understand what went wrong
/// and suggest fixes to the user.
/// </remarks>
/// <example>
/// {
///   "error": {
///     "code": "ValidationFailed",
///     "message": "One or more validation errors occurred.",
///     "details": [
///       {
///         "field": "email",
///         "message": "Email address is already in use.",
///         "value": "existing@example.com"
///       }
///     ],
///     "timestamp": "2024-06-15T10:00:00Z",
///     "requestId": "550e8400-e29b-41d4-a716-446655440000"
///   }
/// }
/// </example>
public class ErrorResponseDtoV3
{
    /// <summary>
    /// The error details containing code, message, and additional information.
    /// </summary>
    public ErrorDetailDtoV3 Error { get; set; }
}

/// <summary>
/// Detailed error information including error code, message, and optional validation details.
/// </summary>
/// <remarks>
/// This DTO contains all information needed to understand and handle an error:
/// - A machine-readable error code for programmatic handling
/// - A human-readable error message for display
/// - Optional field-level validation errors
/// - Timestamp for troubleshooting
/// - Request ID for correlation with logs
/// </remarks>
public class ErrorDetailDtoV3
{
    /// <summary>
    /// A machine-readable error code for programmatic error handling.
    /// Common codes: ValidationFailed, Unauthorized, Forbidden, NotFound, Conflict,
    /// EmailInUse, PasswordPolicyViolation, QuotaExceeded, InvalidOperation.
    /// </summary>
    /// <example>ValidationFailed</example>
    public string Code { get; set; }

    /// <summary>
    /// A human-readable error message describing what went wrong.
    /// This message is safe to display to end users.
    /// </summary>
    /// <example>One or more validation errors occurred.</example>
    public string Message { get; set; }

    /// <summary>
    /// Detailed validation errors for specific fields (if applicable).
    /// Only present for validation errors (typically with 400 Bad Request or 422 Unprocessable Entity).
    /// </summary>
    public IEnumerable<ValidationErrorDtoV3> Details { get; set; }

    /// <summary>
    /// The timestamp when the error occurred (UTC).
    /// Useful for troubleshooting and correlating errors across systems.
    /// </summary>
    /// <example>2024-06-15T10:00:00Z</example>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// A unique identifier for this request, used for troubleshooting and log correlation.
    /// Provide this ID when contacting support for faster resolution.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public string RequestId { get; set; }
}
