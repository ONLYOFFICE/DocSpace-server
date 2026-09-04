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

using System.Collections;
using System.Collections.Concurrent;

namespace ASC.Api.Core.Core;

/// <summary>
/// Serializes <see cref="SuccessApiResponse"/> honouring the <c>fields</c> query parameter (a comma-separated list of
/// dotted property paths that limits what nested DTOs emit). Fully synchronous: reflection and the per-path
/// <see cref="JsonSerializerOptions"/> are built once and cached, values go straight into the outer writer, and a
/// request without <c>fields</c> is handed to the serializer as a plain object. The previous version re-created
/// <see cref="JsonSerializerOptions"/> (and with them the serializer's type metadata) for every nested type on every
/// request and blocked a thread-pool thread with <c>GetAwaiter().GetResult()</c> to stream each property through a
/// <see cref="MemoryStream"/> — 15x slower than plain serialization with <c>fields</c>, 2.3x without.
/// <see cref="IAsyncEnumerable{T}"/> results are materialized by <see cref="CustomResponseFilterAttribute"/> before
/// they reach this converter, so a synchronous serializer is enough.
/// </summary>
public sealed class DynamicIgnoreConverter<T>(IHttpContextAccessor httpContextAccessor, int depth = 0, string fullPropertyName = "") : JsonConverter<T>
{
    private static readonly PropertyInfo[] _properties = typeof(T).GetProperties();
    private static readonly string[] _rawNames = _properties.Select(p => p.Name).ToArray();
    private static readonly ConcurrentDictionary<JsonNamingPolicy, string[]> _namesByPolicy = new();

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var fields = httpContextAccessor.HttpContext?.Request.Query.GetRequestArray("fields");

        if (depth == 0 && fields is not { Length: > 0 })
        {
            // Nothing to filter: the envelope is a plain object, let the serializer write it in one pass.
            JsonSerializer.Serialize(writer, value, DynamicIgnoreOptionsCache.WithoutDynamicIgnore(options));
            return;
        }

        var names = options.PropertyNamingPolicy is { } policy
            ? _namesByPolicy.GetOrAdd(policy, static (p, raw) => raw.Select(p.ConvertName).ToArray(), _rawNames)
            : _rawNames;

        writer.WriteStartObject();

        for (var i = 0; i < _properties.Length; i++)
        {
            var property = _properties[i];
            var propertyValue = property.GetValue(value);
            var propertyName = names[i];

            if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propertyValue == null)
            {
                continue;
            }

            var shouldSerialize = true;
            var newOptions = options;
            var newFullPropertyName = fullPropertyName;

            if (propertyValue != null && fields is { Length: > 0 })
            {
                if (depth > 0)
                {
                    shouldSerialize = MatchFields(fields, propertyName, ref newFullPropertyName);
                }

                var responsePropertyName = propertyName.Equals(nameof(SuccessApiResponse.Response), StringComparison.InvariantCultureIgnoreCase);
                if (shouldSerialize && (depth == 0 && responsePropertyName || depth > 0))
                {
                    newOptions = DynamicIgnoreOptionsCache.ForValue(propertyValue, httpContextAccessor, depth + 1, newFullPropertyName, newOptions);
                }
            }

            if (shouldSerialize)
            {
                writer.WritePropertyName(propertyName);
                JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, newOptions);
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// A field matches a property when it names it at this depth (<c>depth == 1</c>: the property itself, deeper: the
    /// accumulated path), names the current path, or is a dotted path starting at this property. The first matching
    /// field also decides the path handed to the nested converter — first match wins, as in the original <c>Any</c>.
    /// </summary>
    private bool MatchFields(string[] fields, string propertyName, ref string newFullPropertyName)
    {
        foreach (var r in fields)
        {
            if (r.Contains('.'))
            {
                var checkName = depth == 1 ? propertyName : $"{fullPropertyName}.{propertyName}";
                if (r == checkName || r == fullPropertyName || r.StartsWith(checkName + "."))
                {
                    newFullPropertyName = checkName;
                    return true;
                }

                continue;
            }

            if (r == (depth == 1 ? propertyName : fullPropertyName))
            {
                if (depth == 1)
                {
                    newFullPropertyName = propertyName;
                }

                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Caches the <see cref="JsonSerializerOptions"/> variants the converter needs. Every distinct options instance owns
/// its own serializer metadata cache, so creating one per request (as before) meant rebuilding that metadata per
/// request; the set of variants is bounded by (parent options, DTO type, depth, field path) and is request-independent
/// because <c>fields</c> are read at write time, not when the options are built.
/// </summary>
internal static class DynamicIgnoreOptionsCache
{
    private static readonly ConcurrentDictionary<JsonSerializerOptions, JsonSerializerOptions> _plain = new(ReferenceEqualityComparer.Instance);
    private static readonly ConcurrentDictionary<NestedKey, JsonSerializerOptions> _nested = new();
    private static readonly ConcurrentDictionary<Type, (Type AsyncElement, bool IsEnumerable)> _shapes = new();

    public static JsonSerializerOptions WithoutDynamicIgnore(JsonSerializerOptions options)
    {
        return _plain.GetOrAdd(options, static o =>
        {
            var copy = new JsonSerializerOptions(o);
            for (var i = copy.Converters.Count - 1; i >= 0; i--)
            {
                var t = copy.Converters[i].GetType();
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DynamicIgnoreConverter<>))
                {
                    copy.Converters.RemoveAt(i);
                }
            }

            return copy;
        });
    }

    public static JsonSerializerOptions ForValue(object propertyValue, IHttpContextAccessor accessor, int depth, string path, JsonSerializerOptions options)
    {
        var propType = propertyValue.GetType();
        if (!propType.IsClass || propType == typeof(string))
        {
            return options;
        }

        var (asyncElement, isEnumerable) = _shapes.GetOrAdd(propType, static t =>
        {
            var interfaces = t.GetInterfaces();
            var asyncEnumerable = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
            var enumerable = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return (asyncEnumerable?.GetGenericArguments()[0], enumerable != null);
        });

        if (isEnumerable)
        {
            // One nested converter per distinct runtime element type, in first-seen order.
            HashSet<Type> seen = null;
            foreach (var item in (IEnumerable)propertyValue)
            {
                var itemType = item.GetType();
                seen ??= [];
                if (seen.Add(itemType))
                {
                    options = Nested(itemType, accessor, depth, path, options);
                }
            }

            return options;
        }

        return Nested(asyncElement ?? propType, accessor, depth, path, options);
    }

    private static JsonSerializerOptions Nested(Type type, IHttpContextAccessor accessor, int depth, string path, JsonSerializerOptions parent)
    {
        if (!type.IsClass || type == typeof(string))
        {
            return parent;
        }

        return _nested.GetOrAdd(new NestedKey(parent, type, depth, path), static (key, accessor) =>
        {
            var converter = (JsonConverter)Activator.CreateInstance(typeof(DynamicIgnoreConverter<>).MakeGenericType(key.Type), accessor, key.Depth, key.Path);
            var options = new JsonSerializerOptions(key.Parent);
            options.Converters.Add(converter);
            return options;
        }, accessor);
    }

    private readonly record struct NestedKey(JsonSerializerOptions Parent, Type Type, int Depth, string Path)
    {
        public bool Equals(NestedKey other)
        {
            return ReferenceEquals(Parent, other.Parent) && Type == other.Type && Depth == other.Depth && string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Parent), Type, Depth, Path);
        }
    }
}

public class ConfigureJsonOptions(IHttpContextAccessor httpContextAccessor) : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.Converters.Add(new DynamicIgnoreConverter<SuccessApiResponse>(httpContextAccessor));
        options.JsonSerializerOptions.Converters.Add(new ApiDateTimeConverter());
    }
}
