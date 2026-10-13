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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// One role of a form and the account that fills it.
/// </summary>
public class FormRoleRequest
{
    /// <summary>
    /// The ID of the room the form is in. It is stored with the role as sent, so pass the room the form lives in.
    /// </summary>
    /// <example>1</example>
    public int RoomId { get; set; }

    /// <summary>
    /// The name of a role the form defines, such as the one the form author gave a group of fields.
    /// </summary>
    /// <example>Manager</example>
    public string RoleName { get; set; }

    /// <summary>
    /// The color the editor marks the fields of this role with, as a hex code.
    /// </summary>
    /// <example>#4781D1</example>
    public string RoleColor { get; set; }

    /// <summary>
    /// The account that fills this role. It is notified once filling starts, unless it is the caller.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// Accepted for compatibility and ignored: the position of the role in the list sets the filling order.
    /// </summary>
    /// <example>12</example>
    public int Sequence { get; set; }

    /// <summary>
    /// Whether this role counts as already submitted. It is stored as sent; send false when filling starts.
    /// </summary>
    /// <example>false</example>
    public bool Submitted { get; set; }

    /// <summary>
    /// Accepted for compatibility and ignored: the portal records when the role is opened.
    /// </summary>
    /// <example>2026-01-01T10:00:00Z</example>
    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// Accepted for compatibility and ignored: the portal records when the role is submitted.
    /// </summary>
    /// <example>2026-01-01T10:00:00Z</example>
    public DateTime SubmissionDate { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class FormRoleRequestMapper
{
    public static partial FormRole Map(this FormRoleRequest source);
}
