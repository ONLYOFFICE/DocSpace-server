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
/// The colour themes the portal offers, which of them is applied, and how many the plan allows.
/// </summary>
/// <example>
/// {
///   "themes": [{"id": 1, "name": "Custom Theme"}],
///   "selected": 1,
///   "limit": 1
/// }
/// </example>
public class CustomColorThemesSettingsDto
{
    /// <summary>
    /// Every theme the portal can apply, ordered by ID, with the built-in ones first because they were created
    /// first. It is never empty - the built-in themes cannot be deleted - and a custom theme is one whose ID is
    /// higher than the built-in ones.
    /// </summary>
    /// <example>[{"id": 1, "name": "Custom Theme"}]</example>
    public IEnumerable<CustomColorThemesSettingsItem> Themes { get; set; }

    /// <summary>
    /// The ID of the theme in `themes` that is currently applied to the whole portal. Deleting the applied theme
    /// moves it to the lowest remaining ID, so it can change without anyone having chosen a new one.
    /// </summary>
    /// <example>1</example>
    public int Selected { get; set; }

    /// <summary>
    /// How many entries `themes` may hold in total, built-in ones included; `0` means the plan caps nothing. Once
    /// the cap is reached `PUT api/2.0/settings/colortheme` drops a new theme silently instead of failing, so
    /// compare this with the length of `themes` to tell whether a save took effect.
    /// </summary>
    /// <example>1</example>
    public int Limit { get; set; }

    public CustomColorThemesSettingsDto(CustomColorThemesSettings customColorThemesSettings, int limit)
    {
        Themes = customColorThemesSettings.Themes.OrderBy(r => r.Id);
        Selected = customColorThemesSettings.Selected;
        Limit = limit;
    }
}