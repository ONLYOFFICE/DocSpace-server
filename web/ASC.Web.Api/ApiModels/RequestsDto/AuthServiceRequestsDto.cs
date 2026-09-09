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
/// One third-party authorization or storage provider and the keys the portal connects to it with.
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
    /// The provider being configured, by its internal key such as `google` or `box`. Take it from the `name` of
    /// `GET api/2.0/settings/authservice`; it is the only field that selects the provider, and a key this
    /// installation does not know is refused the same way a provider that forbids changes is.
    /// </summary>
    /// <example>google</example>
    public string Name { get; set; }

    /// <summary>
    /// The provider name as it is shown in the interface. It is filled in by the portal when the providers are
    /// listed and is ignored when keys are saved.
    /// </summary>
    /// <example>Google</example>
    public string Title { get; set; }

    /// <summary>
    /// A sentence about what connecting the provider gives the portal, shown next to it in the interface. It is
    /// filled in by the portal and ignored when keys are saved.
    /// </summary>
    /// <example>Google OAuth authentication</example>
    public string Description { get; set; }

    /// <summary>
    /// The steps an administrator has to take on the provider side to obtain the keys, shown in the interface. It is
    /// filled in by the portal and ignored when keys are saved.
    /// </summary>
    /// <example>Configure your Google OAuth credentials</example>
    public string Instruction { get; set; }

    /// <summary>
    /// Whether this provider accepts keys through the API at all. A provider whose keys are fixed by the
    /// installation reports `false`, and saving keys for it is refused; the field is reported by the portal and
    /// ignored on the way in.
    /// </summary>
    /// <example>true</example>
    public bool CanSet { get; set; }

    /// <summary>
    /// Whether the provider is a paid option. A paid one can only be connected while the portal plan includes
    /// third-party storage or the installation is licensed as self-hosted; the field is reported by the portal and
    /// ignored on the way in.
    /// </summary>
    /// <example>false</example>
    public bool Paid { get; set; }

    /// <summary>
    /// The credentials the portal authenticates to the provider with, as the name and value pairs the provider
    /// defines. Send the whole set the provider expects: leaving every value empty disconnects it, and a set that
    /// fails the provider validation is cleared rather than stored half-applied. The listing operation reports the
    /// values last saved, and a provider that forbids changes reports none at all.
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
