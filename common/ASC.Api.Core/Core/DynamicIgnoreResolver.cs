// (c) Copyright Ascensio System SIA 2009-2025
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

using System.Collections;

namespace ASC.Api.Core.Core;

public class DynamicIgnoreConverter<T>(IHttpContextAccessor httpContextAccessor, int depth = 0, string fullPropertyName = "") : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        WriteAsync(writer, value, options).GetAwaiter().GetResult();
    }

    private async Task WriteAsync(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(value);
            var propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;

            if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propertyValue == null)
            {
                continue;
            }

            var shouldSerialize = true;
            var newOptions = options;
            var newFullPropertyName = fullPropertyName;
            
            if (propertyValue != null)
            {
                var fields = httpContextAccessor.HttpContext?.Request.Query.GetRequestArray("fields");
                if (fields is { Length: > 0 })
                {
                    if (depth > 0)
                    {
                        shouldSerialize = fields.Any(r =>
                        {
                            bool result;
                            if (r.Contains('.'))
                            {
                                var checkName = depth == 1 ? propertyName : $"{fullPropertyName}.{propertyName}";
                                result = r == checkName;
                                if (!result)
                                {
                                    result = r == fullPropertyName;
                                }
                                if (!result)
                                {
                                    result = r.StartsWith(checkName + ".");
                                }

                                if (result)
                                {
                                    newFullPropertyName = checkName;
                                }
                                
                                return result;
                            }

                            result = r == (depth == 1 ? propertyName : fullPropertyName);
                            if (result && depth == 1)
                            {
                                newFullPropertyName = propertyName;
                            }
                            return result;
                        });
                    }

                    var responsePropertyName = propertyName.Equals(nameof(SuccessApiResponse.Response), StringComparison.InvariantCultureIgnoreCase);
                    if (shouldSerialize && (depth == 0 && responsePropertyName || depth > 0))
                    {
                        var propType = propertyValue.GetType();
                        if (propType.IsClass && propType != typeof(string))
                        {
                            var interfaces = propType.GetInterfaces();
                            var asyncEnumerableInterface = interfaces.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
                            var enumerableInterfaces = interfaces.FirstOrDefault(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
                            if (enumerableInterfaces != null)
                            {
                                var types = ((IEnumerable)propertyValue).Cast<object>().Select(r=> r.GetType()).Distinct();
                                foreach (var type in types)
                                {
                                    newOptions = JsonSerializerOptions(type, newFullPropertyName, newOptions);
                                }
                            } else if (asyncEnumerableInterface != null)
                            {   
                                var elementType = asyncEnumerableInterface.GetGenericArguments()[0];
                                newOptions = JsonSerializerOptions(elementType, newFullPropertyName, newOptions);
                            }
                            else
                            {
                                newOptions = JsonSerializerOptions(propType, newFullPropertyName, newOptions);
                            }
                        }
                    }
                }
            }

            if (shouldSerialize)
            {
                writer.WritePropertyName(propertyName);

                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, propertyValue, property.PropertyType, newOptions);

                var jsonBytes = stream.ToArray();
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                writer.WriteRawValue(jsonString);
            }
        }

        writer.WriteEndObject();

        JsonSerializerOptions JsonSerializerOptions(Type propType, string newFullPropertyName, JsonSerializerOptions newOptions)
        {
            if (propType is { IsClass: true } && propType != typeof(string))
            {
                var converterType = typeof(DynamicIgnoreConverter<>).MakeGenericType(propType);

                var converter = Activator.CreateInstance(converterType, httpContextAccessor, depth + 1, newFullPropertyName);

                newOptions = new JsonSerializerOptions(newOptions);
                newOptions.Converters.Add((JsonConverter)converter);
            }

            return newOptions;
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
    }
}