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

using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Web.Core.Files;

public static class JsonWebToken
{
    public static string Encode(object payload, string key)
    {
        var (serializer, algorithm, urlEncoder) = GetSettings();
        var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

        return encoder.Encode(payload, key);
    }

    public static string Decode(string token, string key, bool verify = true)
    {
        var decoder = CreateDecoder();

        return decoder.Decode(token, key, verify);
    }

    public static string Decode(string token)
    {
        var decoder = CreateDecoder();

        return decoder.Decode(token);
    }

    private static JwtDecoder CreateDecoder()
    {
        var (serializer, algorithm, urlEncoder) = GetSettings();

        var provider = new UtcDateTimeProvider();
        IJwtValidator validator = new JwtValidator(serializer, provider);

        return new JwtDecoder(serializer, validator, urlEncoder, algorithm);
    }

    private static (IJsonSerializer, IJwtAlgorithm, IBase64UrlEncoder) GetSettings()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return (new JwtSerializer(), new HMACSHA256Algorithm(), new JwtBase64UrlEncoder());
#pragma warning restore CS0618 // Type or member is obsolete
    }
}

public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var dictionary = new Dictionary<string, object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();

            dictionary.Add(propertyName, ExtractValue(ref reader, options));
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var date))
                {
                    return date;
                }
                return reader.GetString();
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var result))
                {
                    return result;
                }
                return reader.GetDecimal();
            case JsonTokenType.StartObject:
                return Read(ref reader, null, options);
            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ExtractValue(ref reader, options));
                }
                return list;
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}


public class JwtSerializer : IJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    public JwtSerializer()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _options.Converters.Add(new DictionaryStringObjectJsonConverter());
    }

    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public object Deserialize(Type type, string json)
    {
        return JsonSerializer.Deserialize(json, type, _options);
    }
}