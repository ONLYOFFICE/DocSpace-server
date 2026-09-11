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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Whether one user administers one portal module, echoing back the pair that was asked about.
/// </summary>
public class ProductAdministratorDto
{
    /// <summary>
    /// The module the verdict is about, echoed from the request. The all-zero GUID stands for the portal as a
    /// whole rather than for any single module.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid ProductId { get; set; }

    /// <summary>
    /// The user the verdict is about, echoed from the request unchanged - it is not checked for existing.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Whether that user administers that module. It is `true` for a DocSpace administrator whatever the module,
    /// since the portal-wide role covers every one of them. A `false` can also mean the identifiers name no user
    /// or no module at all, so it is not proof that the user exists, and it says nothing about whether the module
    /// is enabled for the portal - `GET api/2.0/settings/security/{id}` reports that.
    /// </summary>
    /// <example>true</example>
    public required bool Administrator { get; set; }
}