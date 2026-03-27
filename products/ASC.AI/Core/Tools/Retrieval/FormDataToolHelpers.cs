// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Core.Tools.Retrieval;

/// <summary>
/// Tolerates weak models that pass a bare string instead of a one-element array
/// for IEnumerable&lt;string&gt; parameters — e.g. "col = 1" instead of ["col = 1"].
/// </summary>
internal sealed class FlexibleStringArrayJsonConverter : JsonConverter<IEnumerable<string>>
{
    public static readonly FlexibleStringArrayJsonConverter Instance = new();

    public override IEnumerable<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return [reader.GetString()!];
        }

        var list = new List<string>();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return list;
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                list.Add(reader.GetString()!);
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var s in value)
        {
            writer.WriteStringValue(s);
        }
        writer.WriteEndArray();
    }
}

/// <summary>
/// Shared filter-normalization helpers for form-data tools.
/// Centralises logic that corrects common mistakes made by weak AI models.
/// </summary>
internal static class FormDataToolHelpers
{
    internal static readonly HashSet<string> ValidDateParts =
        new(["YEAR", "MONTH", "WEEK", "DAYOFYEAR", "QUARTER"], StringComparer.OrdinalIgnoreCase);

    internal static readonly JsonSerializerOptions FlexibleJsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        Converters = { FlexibleStringArrayJsonConverter.Instance }
    };

    /// <summary>
    /// Normalises raw <c>filters</c> strings, routing misplaced date-part and date-diff expressions
    /// to their correct parameters so weak models can still get results despite formatting mistakes.
    /// </summary>
    /// <returns>
    /// <c>normal</c> — plain column/value filters;<br/>
    /// <c>datePart</c> — date-part filter strings (e.g. "col MONTH = 6");<br/>
    /// <c>autoDiff</c> — first detected date-diff expression, or <c>null</c>.
    /// </returns>
    internal static (IEnumerable<string> normal, IEnumerable<string> datePart, string? autoDiff)
        ExtractDatePartFilters(IEnumerable<string>? filters)
    {
        if (filters == null)
        {
            return ([], [], null);
        }

        var normal = new List<string>();
        var datePart = new List<string>();
        string? autoDiff = null;

        foreach (var f in filters)
        {
            if (string.IsNullOrWhiteSpace(f))
            {
                continue;
            }

            var parts = f.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Skip "paramName: value" — model put parameter name as filter entry
            if (parts[0].EndsWith(':'))
            {
                continue;
            }

            // "DATEDIFF(col_a, col_b) OP number" → dateDiffFilter "col_a col_b OP number"
            if (f.TrimStart().StartsWith("DATEDIFF(", StringComparison.OrdinalIgnoreCase))
            {
                var openIdx = f.IndexOf('(');
                var closeIdx = f.IndexOf(')');
                if (openIdx >= 0 && closeIdx > openIdx)
                {
                    var colNames = f[(openIdx + 1)..closeIdx].Split(',', 2);
                    var afterParen = f[(closeIdx + 1)..].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (colNames.Length == 2 && afterParen.Length == 2)
                    {
                        autoDiff ??= $"{colNames[0].Trim()} {colNames[1].Trim()} {afterParen[0]} {afterParen[1]}";
                        continue;
                    }
                }
            }

            // "col MONTH = 6" or "col YEAR = 2025" — date part keyword as second token
            if (parts.Length >= 4 && ValidDateParts.Contains(parts[1]))
            {
                datePart.Add(f);
                continue;
            }

            // "col_a - col_b OP num" → arithmetic diff
            if (parts.Length >= 4 && parts[1] == "-")
            {
                autoDiff ??= $"{parts[0]} {string.Join(" ", parts[2..])}";
                continue;
            }

            // "col_a col_b OP num" → date-diff filter (model put dateDiffFilter format into filters array)
            if (parts.Length == 4 && !ValidDateParts.Contains(parts[1]) && int.TryParse(parts[3], out _))
            {
                autoDiff ??= $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
                continue;
            }

            normal.Add(f);
        }

        return (normal, datePart, autoDiff);
    }

}
