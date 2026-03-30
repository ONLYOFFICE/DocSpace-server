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

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerSchemaCustomAttribute : SwaggerSchemaAttribute
{
    internal static readonly string DefaultStringExample = "some text";
    internal static readonly int DefaultIntExample = 1234;
    public SwaggerSchemaCustomAttribute(string description = null)
    {
        Description = description;
    }

    public object Example { get; set; }
}

public class SwaggerSchemaCustomFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var openApiSchema = schema as OpenApiSchema;
        if (openApiSchema == null)
        {
            return;
        }

        if (schema.Enum is { Count: > 0 } && context.Type is { IsEnum: true })
        {
            UpdateSchema(context.Type, openApiSchema);
            return;
        }

        if (context.MemberInfo is not PropertyInfo propertyInfo)
        {
            return;
        }

        UpdateSchema(propertyInfo.PropertyType, openApiSchema);

    }

    private IOpenApiSchema UpdateSchema(Type checkType, OpenApiSchema result)
    {
        var nullableType = Nullable.GetUnderlyingType(checkType);
        if (nullableType != null)
        {
            checkType = nullableType;
        }

        if (typeof(IEnumerable).IsAssignableFrom(checkType))
        {
            var array = new JsonArray();

            if (checkType.IsArray)
            {
                checkType = checkType.GetElementType();
            }
            else if (checkType.IsGenericType && checkType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && IsSimpleType(checkType.GetGenericArguments().FirstOrDefault()))
            {
                checkType = checkType.GetGenericArguments().FirstOrDefault();
            }
            else if (checkType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                checkType = checkType.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()
                    .FirstOrDefault() ?? typeof(object);
            }
            else
            {
                return result;
            }

            var arraySchema = UpdateSchema(checkType, new OpenApiSchema());

            if (arraySchema?.Example != null)
            {
                array.Add(arraySchema.Example);
                result.Example = array;
            }

            if (arraySchema?.OneOf != null && arraySchema.OneOf?.Count != 0)
            {
                result.Items = new OpenApiSchema { AnyOf = arraySchema.OneOf };
            }
            else if (checkType == typeof(object))
            {
                result.Items = new OpenApiSchema { Type = JsonSchemaType.Object };
            }

        }
        else if (checkType == typeof(JsonElement))
        {
            var oneOfSchema = new List<IOpenApiSchema>
            {
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Example = SwaggerSchemaCustomAttribute.DefaultIntExample
                },
                new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Example = SwaggerSchemaCustomAttribute.DefaultStringExample
                }
            };

            result.OneOf = oneOfSchema;
        }
        else if (checkType.IsEnum)
        {
            var enumDataString = new List<JsonNode>();
            var enumDescriptionString = new List<string>();
            var enumDescriptionDataString = new JsonArray();
            var enumVarNames = new JsonArray();
            var enumDataInt = new List<JsonNode>();
            var enumDataLong = new List<JsonNode>();
            var enumDescriptionInt = new List<string>();
            var enumDescriptionLong = new List<string>();
            var enumType = "integer";

            var jsonConverterAttr = checkType.GetCustomAttributesData() .FirstOrDefault(a => a.AttributeType == typeof(JsonConverterAttribute));
            if (jsonConverterAttr != null)
            {
                if (jsonConverterAttr.ConstructorArguments.Count > 0 &&
                    jsonConverterAttr.ConstructorArguments[0].Value is Type { IsGenericType: true } converterType &&
                    converterType.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>))
                {
                    enumType = "string";
                }
            }


            foreach (var enumValue in Enum.GetValues(checkType))
            {
                var value = checkType.GetMember(enumValue.ToString())[0];
                var enumAttribute = value.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();

                enumVarNames.Add(enumValue.ToString());

                enumDataString.Add(enumValue.ToString());

                try
                {
                    enumDataInt.Add(Convert.ToInt32(enumValue));
                }
                catch (OverflowException)
                {
                    enumDataLong.Add(Convert.ToInt64(enumValue));
                }


                if (enumAttribute != null)
                {
                    enumDescriptionDataString.Add(enumAttribute.Description);
                    enumDescriptionString.Add($"{enumValue} - {enumAttribute.Description}");

                    try
                    {
                        enumDescriptionInt.Add($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}");
                    }
                    catch (OverflowException)
                    {
                        enumDescriptionLong.Add($"{Convert.ToInt64(enumValue)} - {enumAttribute.Description}");
                    }

                }
            }

            if (enumDataString.Count > 0)
            {
                var oneOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchema
                    {
                        Enum = enumDataString,
                        Type = JsonSchemaType.String,
                        Description = $"[{string.Join(", ", enumDescriptionString)}]",
                        Example = enumDataString[0]
                    }
                };

                if (enumDataInt.Count > 0)
                {
                    oneOf.Add(new OpenApiSchema
                    {
                        Enum = enumDataInt,
                        Type = JsonSchemaType.Integer,
                        Description = $"[{string.Join(", ", enumDescriptionInt)}]",
                        Example = enumDataInt[0],
                        Extensions = new Dictionary<string, IOpenApiExtension>
                        {
                            ["x-enum-varnames"] = new JsonNodeExtension(enumVarNames)
                        }
                    });
                }
                else
                {
                    oneOf.Add(new OpenApiSchema
                    {
                        Enum = enumDataLong,
                        Type = JsonSchemaType.Integer,
                        Description = $"[{string.Join(", ", enumDescriptionLong)}]",
                        Example = enumDescriptionLong[0],
                        Extensions = new Dictionary<string, IOpenApiExtension>
                        {
                            ["x-enum-varnames"] = new JsonNodeExtension(enumVarNames)
                        }
                    });
                }

                result.OneOf = oneOf;
                result.Enum = null;
                result.Type = null;
                result.Format = null;
                result.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                result.Extensions["x-enum-type"] = new JsonNodeExtension(enumType);
            }
        }
        else if (checkType == typeof(TimeSpan))
        {
            var timeSpan = TimeSpan.Zero.ToString();
            result.Example = timeSpan;
        }

        return result;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               type == typeof(JsonElement);
    }
}
