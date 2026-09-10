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

namespace ASC.Api.Core;

/// <summary>
/// The descriptor of a portal module: what it is called, where it starts and how it is pictured.
/// </summary>
public class Module(Product product)
{
    /// <summary>
    /// The identifier of the module. It is the same in every portal and in every language, so use it rather than the
    /// title to tell modules apart.
    /// </summary>
    /// <example>e67be73d-f9ae-4ce1-8fec-1880cb518cb4</example>
    public Guid Id { get; set; } = product.ProductID;

    /// <summary>
    /// The short system name of the module, the one that appears in its addresses and in the portal configuration.
    /// Unlike the title it is not translated.
    /// </summary>
    /// <example>files</example>
    public string AppName { get; set; } = product.ProductClassName;

    /// <summary>
    /// The display name of the module, already translated for the calling account, so it changes with the language
    /// and must not be compared against a fixed string.
    /// </summary>
    /// <example>Documents</example>
    public string Title { get; set; } = product.Name;

    /// <summary>
    /// The address of the start page of the module, to be opened in a browser rather than called as an API.
    /// </summary>
    /// <example>https://example.com</example>
    public string Link { get; set; } = product.StartURL;

    /// <summary>
    /// The address of the small icon of the module, meant for a menu entry.
    /// </summary>
    /// <example>https://example.com/icon.svg</example>
    public string IconUrl { get; set; } = product.Context.IconFileName;

    /// <summary>
    /// The address of the large image of the module, meant for a tile or a start screen.
    /// </summary>
    /// <example>https://example.com/image.png</example>
    public string ImageUrl { get; set; } = product.Context.LargeIconFileName;

    /// <summary>
    /// The address of the help section of the module. It is empty when the portal publishes no help for it.
    /// </summary>
    /// <example>https://example.com/help</example>
    public string HelpUrl { get; set; } = product.HelpURL;

    /// <summary>
    /// The one-line description of the module shown next to its title, translated for the calling account.
    /// </summary>
    /// <example>File management</example>
    public string Description { get; set; } = product.Description;

    /// <summary>
    /// Whether the portal opens this module first when no other destination is given.
    /// </summary>
    /// <example>true</example>
    public bool IsPrimary { get; set; } = product.IsPrimary;
}