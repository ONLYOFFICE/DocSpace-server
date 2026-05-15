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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for handling the authorization service.
/// </summary>
/// <example>
/// {
///   "name": "google",
///   "title": "Google",
///   "description": "Google OAuth authentication",
///   "instruction": "Configure your Google OAuth credentials",
///   "canSet": true,
///   "paid": false,
///   "props": []
/// }
/// </example>
public class AuthServiceRequestsDto
{
    /// <summary>
    /// The name of the authorization service.
    /// </summary>
    /// <example>google</example>
    public string Name { get; set; }

    /// <summary>
    /// The user-friendly display title of the authorization service.
    /// </summary>
    /// <example>Google</example>
    public string Title { get; set; }

    /// <summary>
    /// The brief description of the authorization service.
    /// </summary>
    /// <example>Google OAuth authentication</example>
    public string Description { get; set; }

    /// <summary>
    /// The detailed instructions for configuring or using the authorization service.
    /// </summary>
    /// <example>Configure your Google OAuth credentials</example>
    public string Instruction { get; set; }

    /// <summary>
    /// Specifies whether the authorization service can be configured by the user.
    /// </summary>
    /// <example>true</example>
    public bool CanSet { get; set; }

    /// <summary>
    /// Specifies whether the authorization service is paid or not.
    /// </summary>
    /// <example>false</example>
    public bool Paid { get; set; }

    /// <summary>
    /// The collection of authorization keys associated with the authorization service.
    /// </summary>
    /// <example>[{"name": "key", "value": "value"}]</example>
    public List<AuthKey> Props { get; set; }

    public static async Task<AuthServiceRequestsDto> From(Consumer consumer, string logoText)
    {
        var authService = await AuthService.From(consumer, logoText);
        var result = new AuthServiceRequestsDto
        {
            Name = authService.Name,
            Title = authService.Title,
            Description = authService.Description,
            Instruction = authService.Instruction,
            CanSet = authService.CanSet,
            Paid = authService.Paid
        };

        if (consumer.CanSet)
        {
            result.Props = authService.Props;
            result.CanSet = authService.CanSet;
        }

        return result;
    }
}