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
/// What the notification letters of the installation print below their body.
/// </summary>
public class MailWhiteLabelSettingsDto
{
    ///<summary>
    /// Whether the letters carry a footer at all. While it is `false` neither the vendor block nor the social
    /// media links are printed, whatever `footerSocialEnabled` says. It starts out `true`.
    ///</summary>
    /// <example>true</example>
    public bool FooterEnabled { get; set; }

    ///<summary>
    /// Whether the footer includes the vendor's social media links. It starts out `true` and has no effect while
    /// `footerEnabled` is `false`.
    ///</summary>
    /// <example>true</example>
    public bool FooterSocialEnabled { get; set; }

    ///<summary>
    /// Whether both flags are still switched on as they ship. It turns `false` as soon as either of them is saved
    /// off, and the values are installation-wide, so every portal of the installation reports the same ones.
    ///</summary>
    /// <example>false</example>
    public bool IsDefault { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class MailWhiteLabelSettingsDtoMapper
{
    [MapPropertyFromSource(nameof(MailWhiteLabelSettingsDto.IsDefault), Use = nameof(MapManual))]
    public static partial MailWhiteLabelSettingsDto MapToDto(this MailWhiteLabelSettings source);

    private static bool MapManual(MailWhiteLabelSettings source)
    {
        return source.IsDefault();
    }
}