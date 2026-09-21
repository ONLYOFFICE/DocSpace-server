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

namespace ASC.Api.Collections;

/// <summary>
/// One entry of a keyed collection, carried as an explicit pair of `key` and `value` fields instead of as a member
/// of a JSON object, so that the key is not restricted to a string and the entries keep the order they are sent in.
/// </summary>
public class ItemKeyValuePair<TKey, TValue>
{
    /// <summary>
    /// The left half of the pair. Where the pair configures something, this is the identifier the value belongs to -
    /// a setting name, a module id, a logo slot; where the pair reports the result of a call, this is the result
    /// itself, such as the flag telling whether the call succeeded. Which of the two it is, and which keys are
    /// accepted, is stated by the operation that sends or returns the pair.
    /// </summary>
    public TKey Key { get; init; }

    /// <summary>
    /// The right half of the pair: what is assigned to the key next to it, or what is reported for it. Its meaning
    /// and its accepted values follow from the key, so read them from the operation that sends or returns the pair.
    /// </summary>
    public TValue Value { get; init; }
}