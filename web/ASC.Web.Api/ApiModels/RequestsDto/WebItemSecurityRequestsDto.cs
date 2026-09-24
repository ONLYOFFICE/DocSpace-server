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
/// The access rule stored for one portal module: whether it may be opened, and by whom.
/// </summary>
/// <example>
/// {
///   "enabled": true,
///   "subjects": []
/// }
/// </example>
public class WebItemSecurityRequestsDto : IValidatableObject
{
    /// <summary>
    /// The module the rule applies to, given as a GUID. A value that is not a GUID fails the request as invalid.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required string Id { get; set; }

    /// <summary>
    /// Whether the module may be opened. It decides the outcome only while `subjects` names somebody: an empty
    /// `subjects` array is stored as access for everyone whatever this flag says.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The users and groups the rule is stored for, given by their IDs. This is the whole allow-list that is to hold
    /// afterwards and not a list of additions - what was stored before is dropped. Leaving it out applies `enabled`
    /// to everyone and skips the audit trail entry, while sending it empty stores access for everyone.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public IEnumerable<Guid> Subjects { get; set; }

    public IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        return WebItemIdValidator.Validate([Id], nameof(Id));
    }
}

/// <summary>
/// The modules switched on or off together, one entry per module.
/// </summary>
public class WebItemsSecurityRequestsDto : IValidatableObject
{
    /// <summary>
    /// The modules to switch, each entry pairing a module GUID as its `key` with the new enabled flag as its
    /// `value`. A key that is not a GUID fails the whole request as invalid, and a module listed twice is applied
    /// once, from its first entry. No allow-list travels here: switching a product module on restores the users and
    /// groups it was last restricted to, and everything else is stored as a plain allow or deny for everyone.
    /// </summary>
    /// <example>[{"key":"00000000-0000-0000-0000-000000000000","value":true}]</example>
    public IEnumerable<ItemKeyValuePair<string, bool>> Items { get; set; }

    public IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        return WebItemIdValidator.Validate(Items?.Select(i => i.Key), nameof(Items));
    }
}
