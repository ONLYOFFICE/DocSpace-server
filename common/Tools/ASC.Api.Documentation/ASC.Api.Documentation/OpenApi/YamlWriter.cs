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


namespace ASC.Api.Documentation;

/// <summary>
/// Writes a joined openapi document as YAML, which is the form the documentation site consumes.
/// </summary>
internal static class YamlWriter
{
    private static readonly ISerializer _serializer = new SerializerBuilder()
        // Sequences indented under their key, matching the document the site already carries -
        // the whole file is rewritten on every run, so an unrelated reindent would bury the
        // actual change in a diff nobody can read.
        .WithIndentedSequences()
        // Without this the string "null" is written as a bare `null`, which YAML reads back as the
        // null scalar - and `type: [string, "null"]`, the 3.1 spelling of a nullable, silently
        // stops being a list of type names. Same trap for "true", "yes" and anything numeric.
        .WithQuotingNecessaryStrings()
        .Build();

    public static string Write(JsonObject document)
    {
        // LF regardless of the platform this runs on: the document is consumed by a repository that
        // stores it with LF, and CRLF would show up there as every single line having changed.
        return _serializer.Serialize(Convert(document)).Replace("\r\n", "\n");
    }

    /// <summary>
    /// YamlDotNet serializes an object graph, not a <see cref="JsonNode"/> tree, so the tree is
    /// converted. Numbers keep the distinction the document draws between whole and fractional
    /// values: emitting 1 as 1.0 would change what the schema states.
    /// </summary>
    private static object? Convert(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;

            case JsonObject obj:
                var map = new Dictionary<string, object?>(obj.Count, StringComparer.Ordinal);
                foreach (var property in obj)
                {
                    map[property.Key] = Convert(property.Value);
                }

                return map;

            case JsonArray array:
                var items = new List<object?>(array.Count);
                foreach (var item in array)
                {
                    items.Add(Convert(item));
                }

                return items;

            case JsonValue value:
                return ConvertValue(value);

            default:
                return node.ToJsonString();
        }
    }

    private static object? ConvertValue(JsonValue value)
    {
        if (value.TryGetValue<bool>(out var flag))
        {
            return flag;
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        var element = value.GetValue<JsonElement>();

        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt64(out var whole) ? whole : element.GetDouble();
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
