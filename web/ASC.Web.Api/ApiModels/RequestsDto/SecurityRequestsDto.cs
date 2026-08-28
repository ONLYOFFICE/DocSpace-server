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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for managing user security and access permissions.
/// </summary>
/// <example>
/// {
///   "administrator": true
/// }
/// </example>
public class SecurityRequestsDto
{
    /// <summary>
    /// The product ID for which permissions are being set.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid ProductId { get; set; }

    /// <summary>
    /// The ID of the user whose permissions are being configured.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Specifies whether the user has administrative privileges.
    /// </summary>
    /// <example>true</example>
    public bool Administrator { get; set; }
}

/// <summary>
/// The request parameters for retrieving the security settings across the multiple modules.
/// </summary>
public class SecuritySettingsRequestDto : IValidatableObject
{
    /// <summary>
    /// The list of module identifiers for which to retrieve the security settings.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    [FromQuery(Name = "ids")]
    public IEnumerable<string> Ids { get; set; }

    public IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        return WebItemIdValidator.Validate(Ids, nameof(Ids));
    }
}

/// <summary>
/// Rejects a module identifier that is not a GUID before it reaches the security store, which
/// parses it without guarding and would otherwise fail the request with an unhandled error.
/// </summary>
public static class WebItemIdValidator
{
    public static IEnumerable<DataAnnotationsValidationResult> Validate(IEnumerable<string> ids, string memberName)
    {
        if (ids == null)
        {
            yield break;
        }

        foreach (var id in ids.Where(id => !Guid.TryParse(id, out _)))
        {
            yield return new DataAnnotationsValidationResult($"The module ID \"{id}\" is not a valid identifier.", [memberName]);
        }
    }
}