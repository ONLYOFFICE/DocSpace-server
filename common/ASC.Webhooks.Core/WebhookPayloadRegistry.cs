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

namespace ASC.Webhooks.Core;

/// <summary>
/// Joins a <see cref="WebhookTrigger"/> to the DTO that models its payload.
/// </summary>
/// <remarks>
/// The trigger declares a <see cref="WebhookPayloadKind"/> and so does the DTO; this walks the assemblies it
/// is handed and pairs them up. It is the one definition of how that join works, shared by the invariant test
/// and by whatever generates the webhook SDK contract. Nothing at runtime needs it - the publisher is handed a
/// built payload and never looks the type up.
/// </remarks>
public static class WebhookPayloadRegistry
{
    /// <summary>
    /// Finds the DTO declared for each payload kind in the given assemblies.
    /// </summary>
    public static Dictionary<WebhookPayloadKind, List<Type>> GetPayloadTypes(params Assembly[] assemblies)
    {
        var result = new Dictionary<WebhookPayloadKind, List<Type>>();

        foreach (var type in assemblies.SelectMany(GetLoadableTypes))
        {
            var attribute = type.GetCustomAttribute<WebhookPayloadAttribute>(false);

            if (attribute == null || attribute.Kind == WebhookPayloadKind.None)
            {
                continue;
            }

            if (!result.TryGetValue(attribute.Kind, out var types))
            {
                types = [];
                result[attribute.Kind] = types;
            }

            types.Add(type);
        }

        return result;
    }

    /// <summary>
    /// Lists every trigger that carries a payload, with the kind it declares.
    /// </summary>
    public static Dictionary<WebhookTrigger, WebhookPayloadKind> GetTriggerKinds()
    {
        return Enum.GetValues<WebhookTrigger>()
            .Select(trigger => (trigger, kind: trigger.GetPayloadKind()))
            .Where(x => x.kind != WebhookPayloadKind.None)
            .ToDictionary(x => x.trigger, x => x.kind);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        // a product assembly can reference something absent from the test host; the types we want still load
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}
