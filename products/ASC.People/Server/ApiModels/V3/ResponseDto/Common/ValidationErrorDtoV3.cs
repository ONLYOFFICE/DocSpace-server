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
/// Field-level validation error details for form submissions and API requests.
/// </summary>
/// <remarks>
/// This DTO provides specific information about which field failed validation and why.
/// It helps both human users and AI systems understand exactly what needs to be fixed.
///
/// Usage Context:
/// - Returned as part of ErrorResponseDtoV3 when validation fails
/// - Typically associated with 400 Bad Request or 422 Unprocessable Entity responses
/// - Multiple validation errors can be returned for a single request
///
/// AI Integration:
/// AI systems can parse these errors to:
/// - Identify which fields have problems
/// - Understand the validation rules that were violated
/// - Generate corrected requests automatically
/// - Provide helpful suggestions to users
/// </remarks>
/// <example>
/// {
///   "field": "email",
///   "message": "Email address is already in use.",
///   "value": "existing@example.com"
/// }
/// </example>
public class ValidationErrorDtoV3
{
    /// <summary>
    /// The name of the field that failed validation.
    /// Uses camelCase naming convention to match JSON property names.
    /// For nested objects, uses dot notation (e.g., "address.city").
    /// </summary>
    /// <example>email</example>
    public string Field { get; set; }

    /// <summary>
    /// The validation error message for this field.
    /// Explains what validation rule was violated and how to fix it.
    /// </summary>
    /// <example>Email address is already in use.</example>
    public string Message { get; set; }

    /// <summary>
    /// The invalid value that was provided (optional, for debugging).
    /// May be omitted for security-sensitive fields like passwords.
    /// Useful for helping users understand what they entered wrong.
    /// </summary>
    /// <example>invalid@email</example>
    public object Value { get; set; }
}
