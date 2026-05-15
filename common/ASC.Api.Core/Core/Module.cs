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
/// The module information.
/// </summary>
public class Module(Product product)
{
    /// <summary>
    /// The module ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; } = product.ProductID;

    /// <summary>
    /// The module product class name.
    /// </summary>
    /// <example>files</example>
    public string AppName { get; set; } = product.ProductClassName;

    /// <summary>
    /// The module product class name.
    /// </summary>
    /// <example>Documents</example>
    public string Title { get; set; } = product.Name;

    /// <summary>
    /// The URL to the module start page.
    /// </summary>
    /// <example>https://example.com</example>
    public string Link { get; set; } = product.StartURL;

    /// <summary>
    /// The module icon URL.
    /// </summary>
    /// <example>https://example.com/icon.svg</example>
    public string IconUrl { get; set; } = product.Context.IconFileName;

    /// <summary>
    /// The module large image URL.
    /// </summary>
    /// <example>https://example.com/image.png</example>
    public string ImageUrl { get; set; } = product.Context.LargeIconFileName;

    /// <summary>
    /// The module help URL.
    /// </summary>
    /// <example>https://example.com/help</example>
    public string HelpUrl { get; set; } = product.HelpURL;

    /// <summary>
    /// The module description.
    /// </summary>
    /// <example>File management</example>
    public string Description { get; set; } = product.Description;

    /// <summary>
    /// Specifies if the module is primary or not.
    /// </summary>
    /// <example>true</example>
    public bool IsPrimary { get; set; } = product.IsPrimary;
}