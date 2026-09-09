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
/// Whether one branding slot still holds the built-in image or wordmark.
/// </summary>
public class IsDefaultWhiteLabelLogosDto
{
    /// <summary>
    /// The stable name of the slot, matching the `name` of the same slot in
    /// `GET api/2.0/settings/whitelabel/logos` - `LightSmall`, `LoginPage`, `Favicon`, `DocsEditor` and the rest,
    /// plus `Notification`, which that list leaves out. The wordmark check reports the fixed name `logotext`
    /// instead of a slot.
    /// </summary>
    /// <example>LightSmall</example>
    public required string Name { get; set; }

    /// <summary>
    /// Whether the slot has never been written for this portal, in which case the built-in image is what gets
    /// rendered. It turns `false` once an image has been stored, for either the light or the dark theme, and back
    /// to `true` after the matching restore operation. For `logotext` it stays `true` when the built-in wordmark
    /// itself is saved, because saving that value counts as clearing the setting.
    /// </summary>
    /// <example>true</example>
    public required bool Default { get; set; }
}