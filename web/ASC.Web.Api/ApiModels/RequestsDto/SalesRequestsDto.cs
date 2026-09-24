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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// Who is writing to the ONLYOFFICE sales team, and what about.
/// </summary>
/// <example>
/// {
///   "userName": "John Doe"
/// }
/// </example>
public class SalesRequestsDto
{
    /// <summary>
    /// The name the sales team should address the reply to. It is sent as written and is not matched against any
    /// portal account; an empty value fails the request with 400.
    /// </summary>
    /// <example>John Doe</example>
    [MaxLength(255)]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource),
        ErrorMessageResourceName = nameof(Resource.ErrorIncorrectUserName))]
    public required string UserName { get; set; }

    /// <summary>
    /// The address the answer is sent to. It has to be a well-formed email address and need not be the caller portal
    /// address; an empty or malformed value fails the request with 400.
    /// </summary>
    /// <example>user@example.com</example>
    [MaxLength(64)]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource),
        ErrorMessageResourceName = nameof(Resource.ErrorNotCorrectEmail))]
    public required string Email { get; set; }

    /// <summary>
    /// What is being asked of the sales team - a quote, an invoice, or a plan that cannot be bought online. An empty
    /// value fails the request with 400.
    /// </summary>
    /// <example>I would like to inquire about pricing</example>
    [MaxLength(255)]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource),
        ErrorMessageResourceName = nameof(Resource.ErrorEmptyMessage))]
    public required string Message { get; set; }
}
