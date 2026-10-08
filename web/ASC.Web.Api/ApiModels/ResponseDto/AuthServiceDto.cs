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
public class AuthServiceDto
{
    /// <summary>
    /// The internal key of the provider, such as `google` or `box`. It is the `name` that
    /// `POST api/2.0/settings/authservice` takes to select the provider.
    /// </summary>
    /// <example>google</example>
    public string Name { get; set; }

    /// <summary>
    /// The provider name as it is shown in the interface.
    /// </summary>
    /// <example>Google</example>
    public string Title { get; set; }

    /// <summary>
    /// A sentence about what connecting the provider gives the portal, shown next to it in the interface.
    /// </summary>
    /// <example>Google OAuth authentication</example>
    public string Description { get; set; }

    /// <summary>
    /// The steps an administrator has to take on the provider side to obtain the keys, shown in the interface.
    /// </summary>
    /// <example>Configure your Google OAuth credentials</example>
    public string Instruction { get; set; }

    /// <summary>
    /// Whether this provider accepts keys through the API at all. A provider whose keys are fixed by the
    /// installation reports `false`, and saving keys for it is refused.
    /// </summary>
    /// <example>true</example>
    public bool CanSet { get; set; }

    /// <summary>
    /// Whether the provider is a paid option. A paid one can only be connected while the portal plan includes
    /// third-party storage or the installation is licensed as self-hosted.
    /// </summary>
    /// <example>false</example>
    public bool Paid { get; set; }

    /// <summary>
    /// The keys the provider defines, with the values last saved and how the settings form shows each of them.
    /// It is `null` for a provider that forbids changes (`canSet` is `false`): its keys are not read at all.
    /// </summary>
    /// <example>[{"name": "googleClientId", "value": "1234567890-abc.apps.googleusercontent.com", "title": "Client ID", "type": "text"}]</example>
    public List<AuthKeyDto> Props { get; set; }

    public static async Task<AuthServiceDto> From(Consumer consumer, string logoText)
    {
        var result = new AuthServiceDto
        {
            Name = consumer.Name,
            Title = ConsumerExtension.GetResourceString(consumer.Name) ?? consumer.Name,
            Description = ConsumerExtension.GetResourceString(consumer.Name + "Description")?.Replace("{LogoText}", logoText),
            Instruction = ConsumerExtension.GetResourceString(consumer.Name + "Instruction")?.Replace("{LogoText}", logoText),
            CanSet = consumer.CanSet,
            Paid = consumer.Paid
        };

        if (!consumer.CanSet)
        {
            return result;
        }

        var metadataProvider = consumer as IConsumerKeyMetadataProvider;
        var keys = metadataProvider != null
            ? consumer.ManagedKeys.OrderBy(k => metadataProvider.GetKeyMetadata(k).Order)
            : consumer.ManagedKeys;

        result.Props = [];

        foreach (var item in keys)
        {
            var meta = metadataProvider?.GetKeyMetadata(item);

            result.Props.Add(new AuthKeyDto
            {
                Name = item,
                Value = await consumer.GetAsync(item),
                Title = ConsumerExtension.GetResourceString(item) ?? item,
                Type = meta != null ? meta.Type : "text",
                Options = meta?.Options,
                DependsOn = meta?.DependsOn,
                DependsOnValue = meta?.DependsOnValue,
                DependsOnValues = meta?.DependsOnValues
            });
        }

        return result;
    }
}
