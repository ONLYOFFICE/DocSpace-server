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

namespace ASC.Core.Tenants;

/// <summary>
/// The deep link settings.
/// </summary>
[Serializable]
public class TenantDeepLinkSettings : ISettings<TenantDeepLinkSettings>
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    public static Guid ID => new("{926A6850-7C19-4744-B4AD-813DE3CD55B1}");

    /// <summary>
    /// The deep link handling mode.
    /// </summary>
    /// <example>ProvideChoice</example>
    public DeepLinkHandlingMode HandlingMode { get; set; }

    public TenantDeepLinkSettings GetDefault()
    {
        return new TenantDeepLinkSettings();
    }
    
    /// <summary>
    /// The timestamp indicating when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }
}

/// <summary>
/// The deep link handling mode.
/// </summary>
public enum DeepLinkHandlingMode
{
    [Description("Provide choice")]
    ProvideChoice,

    [Description("Web")]
    Web,

    [Description("App")]
    App
}