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
/// The vocabularies the audit and login-history filters accept, one array of names per dimension of an event.
/// </summary>
public class AuditTrailTypesDto
{
    /// <summary>
    /// Every action name the build can record, spelled as the `action` filter of
    /// `GET api/2.0/security/audit/events/filter` and `GET api/2.0/security/audit/login/filter` expects it. It is
    /// the whole vocabulary, not the actions this portal has recorded, and only a handful of the names are the
    /// sign-in actions the login filter accepts.
    /// </summary>
    /// <example>["FileCreated"]</example>
    public string[] Actions { get; set; }

    /// <summary>
    /// The kinds of change an action can stand for, spelled as the `actionType` filter of
    /// `GET api/2.0/security/audit/events/filter` expects it.
    /// </summary>
    /// <example>["Create"]</example>
    public string[] ActionTypes { get; set; }

    /// <summary>
    /// The products an action can belong to, spelled as the `productType` filter of
    /// `GET api/2.0/security/audit/mappers` expects it. The audit trail itself cannot be filtered by product.
    /// </summary>
    /// <example>["Documents"]</example>
    public string[] ProductTypes { get; set; }

    /// <summary>
    /// The locations inside those products, spelled as the `moduleType` filter of
    /// `GET api/2.0/security/audit/events/filter` and `GET api/2.0/security/audit/mappers` expects it.
    /// </summary>
    /// <example>["Files"]</example>
    public string[] ModuleTypes { get; set; }

    /// <summary>
    /// The kinds of object an action can be applied to, spelled as the `entryType` filter of
    /// `GET api/2.0/security/audit/events/filter` expects it.
    /// </summary>
    /// <example>["File"]</example>
    public string[] EntryTypes { get; set; }
}
