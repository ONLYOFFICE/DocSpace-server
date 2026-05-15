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
/// The request parameters for linking accounts.
/// </summary>
public class LinkAccountRequestDto
{
    /// <summary>
    /// The third-party profile in the serialized format.
    /// </summary>
    /// <example>{"provider":"Google","id":"123456"}</example>
    public string SerializedProfile { get; set; }
}

/// <summary>
/// The request parameters for creating a third-party account.
/// </summary>
public class SignupAccountRequestDto
{
    /// <summary>
    /// The user type.
    /// </summary>
    /// <example>1</example>
    public EmployeeType? EmployeeType { get; set; }

    /// <summary>
    /// The user link key.
    /// </summary>
    /// <example>invite_key_123456</example>
    public required string Key { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// The third-party profile in the serialized format
    /// </summary>
    /// <example>{"provider":"Google","id":"123456"}</example>
    public required string SerializedProfile { get; set; }
}