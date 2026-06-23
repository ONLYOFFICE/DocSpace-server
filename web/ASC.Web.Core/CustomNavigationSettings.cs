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

namespace ASC.Web.Studio.Core;

public class CustomNavigationSettings : ISettings<CustomNavigationSettings>
{
    public List<CustomNavigationItem> Items { get; init; }

    public static Guid ID => new("{32E02E4C-925D-4391-BAA4-3B5D223A2104}");

    public CustomNavigationSettings GetDefault()
    {
        return new CustomNavigationSettings { Items = [] };
    }

    public DateTime LastModified { get; set; }
}

/// <summary>
/// The custom navigation item parameters.
/// </summary>
public class CustomNavigationItem
{
    /// <summary>
    /// The ID of the custom navigation item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The label of the custom navigation item.
    /// </summary>
    /// <example>example value</example>
    public string Label { get; set; }

    /// <summary>
    /// The URL of the custom navigation item.
    /// </summary>
    /// <example>example value</example>
    public string Url { get; set; }

    /// <summary>
    /// The big image of the custom navigation item.
    /// </summary>
    /// <example>data:image\\/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAABkAgMAAAANjH3HAAAADFBMVEUAAADJycnJycnJycmiuNtHAAAAA3RSTlMAf4C\\/aSLHAAAAyElEQVR4Xu3NsQ3CMBSE4YubFB4ilHQegdGSjWACvEpGoEyBYiL05AdnXUGHolx10lf82MmOpfLeo5UoJUhBlpKkRCnhUy7b9XCWkqQMUkYlXVHSf8kTvkHKqKQrSnopg5SRxTMklLmS1MwaSWpmCSQ1MyOzWGZCYrEMEFksA4QqlAFuJJYBcCKxjM3FMySeIfEMC2dMOONCGZZgmdr1ly3TSrJMK9EyJBaaGrHQikYstAiJZRYSyiQEdyg5S8Evckih\\/YPscsdej0H6dc0TYw4AAAAASUVORK5CYII=</example>
    public string BigImg { get; set; }

    /// <summary>
    /// The small image of the custom navigation item.
    /// </summary>
    /// <example>data:image\\/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8\\/9hAAAAUUlEQVR4AWMY\\/KC5o\\/cAEP9HxxgKcSpCGELYADyu2E6mAQjNxBlAWPNxkHdwGkBIM3KYYDUAr2ZCAE+oH8eujrAXDsA0k2EAAtDXAGLx4MpsADUgvkRKUlqfAAAAAElFTkSuQmCC</example>
    public string SmallImg { get; set; }

    /// <summary>
    /// Specifies whether to show the custom navigation item in menu or not.
    /// </summary>
    public bool ShowInMenu { get; set; }

    /// <summary>
    /// Specifies whether to show the custom navigation item on home page or not.
    /// </summary>
    public bool ShowOnHomePage { get; set; }

    private static string GetDefaultBigImg()
    {
        return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAABkAgMAAAANjH3HAAAADFBMVEUAAADJycnJycnJycmiuNtHAAAAA3RSTlMAf4C/aSLHAAAAyElEQVR4Xu3NsQ3CMBSE4YubFB4ilHQegdGSjWACvEpGoEyBYiL05AdnXUGHolx10lf82MmOpfLeo5UoJUhBlpKkRCnhUy7b9XCWkqQMUkYlXVHSf8kTvkHKqKQrSnopg5SRxTMklLmS1MwaSWpmCSQ1MyOzWGZCYrEMEFksA4QqlAFuJJYBcCKxjM3FMySeIfEMC2dMOONCGZZgmdr1ly3TSrJMK9EyJBaaGrHQikYstAiJZRYSyiQEdyg5S8Evckih/YPscsdej0H6dc0TYw4AAAAASUVORK5CYII=";
    }

    private static string GetDefaultSmallImg()
    {
        return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAUUlEQVR4AWMY/KC5o/cAEP9HxxgKcSpCGELYADyu2E6mAQjNxBlAWPNxkHdwGkBIM3KYYDUAr2ZCAE+oH8eujrAXDsA0k2EAAtDXAGLx4MpsADUgvkRKUlqfAAAAAElFTkSuQmCC";
    }

    public static CustomNavigationItem GetSample()
    {
        return new CustomNavigationItem
        {
            Id = Guid.Empty,
            ShowInMenu = true,
            ShowOnHomePage = true,
            BigImg = GetDefaultBigImg(),
            SmallImg = GetDefaultSmallImg()
        };
    }
}