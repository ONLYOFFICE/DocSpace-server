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

namespace Textile;

/// <summary>
/// Interface through which the HTML formatted text
/// will be sent.
/// </summary>
/// Clients of the TextileFormatter class will have to provide
/// an outputter that implements this interface. Most of the
/// time, it'll be the WebForm itself.
public interface IOutputter
{
    /// <summary>
    /// Method called just before the formatted text
    /// is sent to the outputter.
    /// </summary>
    void Begin();

    /// <summary>
    /// Metohd called whenever the TextileFormatter wants to
    /// print some text.
    /// </summary>
    /// <param name="text">The formatted HTML text.</param>
    void Write(string text);
    /// <summary>
    /// Metohd called whenever the TextileFormatter wants to
    /// print some text. This should automatically print an
    /// additionnal end of line character.
    /// </summary>
    /// <param name="line">The formatted HTML text.</param>
    void WriteLine(string line);

    /// <summary>
    /// Method called at the end of the formatting.
    /// </summary>
    void End();
}