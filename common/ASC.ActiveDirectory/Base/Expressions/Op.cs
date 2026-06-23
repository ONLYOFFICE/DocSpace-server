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

namespace ASC.ActiveDirectory.Base.Expressions;
/// <summary>
/// Operations
/// </summary>
/// <remarks>
/// [1 - negation][1 - binary][number]
/// </remarks>
public enum Op
{
    //------------  UNARY -------------
    /// <summary>Attribute exists</summary>
    Exists = 0x000001,
    /// <summary>Attribute does not exist</summary>
    NotExists = 0x010002,

    //------------  BINARY -------------
    /// <summary>Equal</summary>
    Equal = 0x000103,
    /// <summary>Not equal</summary>
    NotEqual = 0x010104,
    /// <summary>Strong less</summary>
    Less = 0x000105,
    /// <summary>Less or equal</summary>
    LessOrEqual = 0x000106,
    /// <summary>Strong greater</summary>
    Greater = 0x000107,
    /// <summary>Greater or equal</summary>
    GreaterOrEqual = 0x000108
}