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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The person a saved revision of a file, or one single change in it, is attributed to.
/// </summary>
public class EditHistoryAuthorDto
{
    /// <summary>
    /// The account the revision or the change is attributed to, as the editing service stored it. It is normally the
    /// identifier of a portal account; the empty identifier stands for a change nobody could be named for.
    /// </summary>
    /// <example>9924256b-447c-4f19-9dbd-8ad8c39e8ff5</example>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of that account as the portal spells it now, which need not be the name that was stored with
    /// the revision. An account that cannot be resolved - one removed from the portal, or a change made through an
    /// anonymous link - is reported as a guest.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; init; }
}
