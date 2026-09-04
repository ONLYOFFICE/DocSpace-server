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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The request parameters for starting the reassignment process.
/// </summary>
public class StartReassignRequestDto
{
    /// <summary>
    /// The ID of the user whose rooms and shared files are transferred away. The account has to have the `Terminated`
    /// status already, and it cannot be a system account, the portal owner or the caller.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid FromUserId { get; set; }

    /// <summary>
    /// The ID of the user who receives the data. The account has to be an active room admin or DocSpace admin, so a
    /// guest, a system account or a disabled account is rejected.
    /// </summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    public required Guid ToUserId { get; set; }

    /// <summary>
    /// Specifies whether to delete the source profile once the transfer succeeds. When false, which is the default,
    /// the emptied profile is kept and can be deleted later through `DELETE api/2.0/people/{userid}`.
    /// </summary>
    /// <example>false</example>
    public bool DeleteProfile { get; set; }
}