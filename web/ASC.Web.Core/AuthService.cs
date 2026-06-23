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

namespace ASC.Web.Studio.UserControls.Management;

public class AuthService
{
    public Consumer Consumer { get; set; }

    public string Name => Consumer.Name;

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Instruction { get; private set; }

    public bool CanSet => Consumer.CanSet;

    public int? Order => Consumer.Order;

    public bool Paid => Consumer.Paid;

    public List<AuthKey> Props { get; private set; }

    public static async Task<AuthService> From(Consumer consumer, string logoText)
    {
        var result = new AuthService
        {
            Consumer = consumer,
            Title = ConsumerExtension.GetResourceString(consumer.Name) ?? consumer.Name,
            Description = ConsumerExtension.GetResourceString(consumer.Name + "Description")?.Replace("{LogoText}", logoText),
            Instruction = ConsumerExtension.GetResourceString(consumer.Name + "Instruction")?.Replace("{LogoText}", logoText),
            Props = []
        };

        var metadataProvider = consumer as IConsumerKeyMetadataProvider;
        var keys = metadataProvider != null
            ? consumer.ManagedKeys.OrderBy(k => metadataProvider.GetKeyMetadata(k).Order)
            : consumer.ManagedKeys;

        foreach (var item in keys)
        {
            var authKey = new AuthKey
            {
                Name = item,
                Value = await consumer.GetAsync(item),
                Title = ConsumerExtension.GetResourceString(item) ?? item
            };

            if (metadataProvider != null)
            {
                var meta = metadataProvider.GetKeyMetadata(item);
                authKey.Type = meta.Type;
                authKey.Options = meta.Options;
                authKey.DependsOn = meta.DependsOn;
                authKey.DependsOnValue = meta.DependsOnValue;
            }

            result.Props.Add(authKey);
        }

        return result;
    }
}

public static class ConsumerExtension
{
    public static string GetResourceString(string resourceKey)
    {
        try
        {
            Resource.ResourceManager.IgnoreCase = true;
            return Resource.ResourceManager.GetString("Consumers" + resourceKey);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// The authorization key parameters.
/// </summary>
[DebuggerDisplay("({Name},{Value})")]
public class AuthKey
{
    /// <summary>
    /// The authorization key name.
    /// </summary>
    /// <example>Auth-Key</example>
    public required string Name { get; init; }

    /// <summary>
    /// The authorization key value.
    /// </summary>
    /// <example>abc123xyz456</example>
    [StringLength(4000)]
    public required string Value { get; init; }

    /// <summary>
    /// The authorization key title.
    /// </summary>
    /// <example>API key</example>
    public string Title { get; set; }

    /// <summary>
    /// The field type: "text", "password", "select", "toggle".
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// The list of options for "select" type fields.
    /// </summary>
    public List<string> Options { get; set; }

    /// <summary>
    /// The name of another key this field depends on for visibility.
    /// </summary>
    public string DependsOn { get; set; }

    /// <summary>
    /// The value of <see cref="DependsOn"/> key that makes this field visible.
    /// </summary>
    public string DependsOnValue { get; set; }
}
