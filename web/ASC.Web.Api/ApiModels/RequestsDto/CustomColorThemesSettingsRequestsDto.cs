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
/// The custom colour theme being saved, the theme being selected, or both.
/// </summary>
public class CustomColorThemesSettingsRequestsDto
{
    /// <summary>
    /// The theme to store, with its accent and button colours for the interface and for the text on it. An `id` that
    /// matches a stored custom theme replaces it, an unknown `id` appends a new one, and an `id` belonging to a
    /// built-in theme is treated as a request for a new custom theme rather than overwriting the built-in one. Once
    /// the plan limit on custom themes is reached a new theme is silently not added, so compare the returned themes
    /// against `limit` instead of assuming it was saved. Leave it out to change only the selection.
    /// </summary>
    /// <example>
    /// {
    ///   "id": 1,
    ///   "main": {
    ///     "accent": "#4781D1",
    ///     "buttons": "#5299E0"
    ///   },
    ///   "text": {
    ///     "accent": "#FFFFFF",
    ///     "buttons": "#FFFFFF"
    ///   }
    /// }
    /// </example>
    public CustomColorThemeRequestDto Theme { get; set; }

    /// <summary>
    /// The theme the whole portal switches to, by theme ID. An ID matching no stored theme is ignored rather than
    /// refused, and leaving it out keeps the selection as it is.
    /// </summary>
    /// <example>1</example>
    public int? Selected { get; set; }
}

/// <summary>
/// A colour theme to store.
/// </summary>
public class CustomColorThemeRequestDto
{
    /// <summary>
    /// The id of the custom theme to replace, or an id no stored theme has to add a new one.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// The accent and button colours of the interface. Left out, a stored theme keeps its own.
    /// </summary>
    public ColorThemeColorsRequestDto Main { get; set; }

    /// <summary>
    /// The colours of the text shown on the accent and on the buttons. Left out, a stored theme keeps its own.
    /// </summary>
    public ColorThemeColorsRequestDto Text { get; set; }
}

/// <summary>
/// A pair of colours of a theme.
/// </summary>
public class ColorThemeColorsRequestDto
{
    /// <summary>
    /// The accent colour, as a hex code.
    /// </summary>
    /// <example>#4781D1</example>
    public string Accent { get; set; }

    /// <summary>
    /// The button colour, as a hex code.
    /// </summary>
    /// <example>#5299E0</example>
    public string Buttons { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class CustomColorThemeRequestDtoMapper
{
    public static partial CustomColorThemesSettingsItem Map(this CustomColorThemeRequestDto source);
}
