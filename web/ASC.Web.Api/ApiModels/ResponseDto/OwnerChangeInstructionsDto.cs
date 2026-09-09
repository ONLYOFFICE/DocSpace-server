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
/// The outcome of asking for the portal-ownership transfer letter to be sent.
/// </summary>
public class OwnerChangeInstructionsDto
{
    /// <summary>
    /// Whether the letter was sent: `1` that it was, `0` that the request was turned down. A refusal comes back
    /// with HTTP 200, so this field and not the status code is what says whether anything happened - the request
    /// is turned down when the caller is not the portal owner and when the named member is unknown or inactive.
    /// </summary>
    /// <example>1</example>
    public int Status { get; set; }

    /// <summary>
    /// The outcome spelled out in the portal language. On success it names the address the letter went to, and it
    /// carries an HTML `mailto:` anchor rather than plain text, so it has to be rendered as markup or stripped;
    /// on a refusal it is the localised reason.
    /// </summary>
    /// <example>Ownership transferred successfully</example>
    public string Message { get; set; }
}