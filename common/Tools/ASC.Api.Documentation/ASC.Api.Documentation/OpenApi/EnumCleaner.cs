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

public class EnumCleaner
{
    public static void Clean(JsonObject root)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        ProcessNode(root);
    }
    private static void ProcessNode(JsonNode node, string? preferredEnumType = null, JsonObject? parentNode = null)
    {
        if (node is not JsonObject obj)
        {
            return;
        }

        var keys = obj.Select(kv => kv.Key).ToList();
        foreach (var key in keys)
        {
            var value = obj[key];

            if (key == "summary" && value is JsonValue sv)
            {
                obj[key] = JsonValue.Create(sv.GetValue<string>().Replace("\"", ""));
            }

            if (key == "description" && value is JsonValue dv)
            {
                obj[key] = JsonValue.Create(dv.GetValue<string>().Replace("\"", ""));
            }

            if ((key == "anyOf" || key == "oneOf") && value is JsonArray arr && IsEnumAnyOf(arr))
            {
                var targetType = preferredEnumType ?? obj["x-enum-type"]?.GetValue<string>() ?? "integer";

                var preferred = arr.OfType<JsonObject>().FirstOrDefault(o => (o["x-enum-type"]?.GetValue<string>() ?? o["type"]?.GetValue<string>()) == targetType);

                if (preferred != null)
                {
                    if (preferred["enum"] != null)
                    {
                        obj["enum"] = preferred["enum"]!.DeepClone();
                    }

                    obj["type"] = targetType;

                    if (preferred["example"] != null)
                    {
                        obj["example"] = preferred["example"]!.DeepClone();
                    }

                    if (preferred["x-enum-varnames"] != null)
                    {
                        obj["x-enum-varnames"] = preferred["x-enum-varnames"]!.DeepClone();
                    }

                    obj["description"] = parentNode?["description"]?.DeepClone() ?? preferred["description"]?.DeepClone();

                    obj.Remove(key);
                    obj.Remove("x-enum-type");
                }

                continue;
            }

            if (value is JsonObject childObj)
            {
                ProcessNode(childObj, preferredEnumType, obj);
            }
            else if (value is JsonArray childArr)
            {
                foreach (var item in childArr)
                {
                    ProcessNode(item, preferredEnumType, obj);
                }
            }
        }
    }

    private static bool IsEnumAnyOf(JsonArray arr)
    {
        return arr.All(item => item is JsonObject o && o["$ref"] == null && (o["type"]?.GetValue<string>() == "string" || o["type"]?.GetValue<string>() == "integer") && o["enum"] is JsonArray);
    }
}
