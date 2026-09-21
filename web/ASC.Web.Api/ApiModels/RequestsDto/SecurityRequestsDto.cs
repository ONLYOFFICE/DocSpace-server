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
/// Which member is granted or denied the administrator role of which portal module.
/// </summary>
/// <example>
/// {
///   "administrator": true
/// }
/// </example>
public class SecurityRequestsDto
{
    /// <summary>
    /// The module the role applies to, given by its GUID. The all-zero GUID stands for the portal itself and grants
    /// or revokes the DocSpace administrator role, which covers every module at once; a GUID that names no module
    /// group is stored without effect rather than refused.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid ProductId { get; set; }

    /// <summary>
    /// The portal member the role is given to or taken from, by user ID. The member has to exist already - nobody is
    /// created here - and promoting a guest or a plain member turns them into a paid one.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Which way the role goes: `true` adds the member to the module administrator group, `false` removes them from
    /// it. Taking away the portal-wide role also drops the member from every product group.
    /// </summary>
    /// <example>true</example>
    public bool Administrator { get; set; }
}

/// <summary>
/// Which portal modules the access configuration is read for.
/// </summary>
public class SecuritySettingsRequestDto : IValidatableObject
{
    /// <summary>
    /// The modules to report on, each given as a GUID and sent as a repeated query value. An entry that is not a
    /// GUID fails the whole request as invalid. Leaving the list out asks about every module registered in the
    /// portal, which on a DocSpace installation is none, so the answer is then empty rather than complete.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    [FromQuery(Name = "ids")]
    public IEnumerable<string> Ids { get; set; }

    public IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        return WebItemIdValidator.Validate(Ids, nameof(Ids));
    }
}
