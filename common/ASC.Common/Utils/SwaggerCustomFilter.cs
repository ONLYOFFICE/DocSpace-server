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

using Bogus;

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

        var swaggerSchemaCustomAttribute = propertyInfo.GetCustomAttributes(true).OfType<SwaggerSchemaCustomAttribute>().FirstOrDefault();

        if (swaggerSchemaCustomAttribute != null)
        {
            if (swaggerSchemaCustomAttribute.Example != null)
            {
                openApiSchema.Example = GetExample(swaggerSchemaCustomAttribute.Example);
            }
        }
        else
        {
            var example = GenerateFakeData(propertyInfo);
            if (example != null)
            {
                openApiSchema.Example = example;
            }
        }
    }

    private IOpenApiSchema UpdateSchema(Type checkType, OpenApiSchema result)
    {
        var nullableType = Nullable.GetUnderlyingType(checkType);
        if (nullableType != null)
        {
            checkType = nullableType;
        }

        if (checkType == typeof(int))
        {
            result.Example = SwaggerSchemaCustomAttribute.DefaultIntExample;
        }
        else if (checkType == typeof(long) || checkType == typeof(ulong))
        {
            result.Example = 1234;
        }
        else if (checkType == typeof(string))
        {
            result.Example = SwaggerSchemaCustomAttribute.DefaultStringExample;
        }
        else if (checkType == typeof(bool))
        {
            result.Example = true;
        }
        else if (checkType == typeof(double))
        {
            result.Example = -8.5;
        }
        else if (checkType == typeof(DateTime))
        {
            result.Example = new DateTimeOffset(2008, 4, 10, 06, 30, 00, TimeSpan.FromHours(4));
        }
        else if (checkType == typeof(Guid))
        {
            result.Example = "75a5f745-f697-4418-b38d-0fe0d277e258";
        }
        else if (checkType.IsClosedTypeOf(typeof(IDictionary<,>)))
        {
            var array = new JsonArray();
            if (checkType.IsGenericType)
            {
                var dictSchema = new JsonObject();
                for (var index = 0; index < checkType.GenericTypeArguments.Length; index++)
                {
                    var t = checkType.GenericTypeArguments[index];
                    var arraySchema = UpdateSchema(t, new OpenApiSchema());
                    if (arraySchema is { Example: not null })
                    {
                        dictSchema.Add(index == 0 ? "key" : "value", arraySchema.Example);
                    }
                }

                array.Add(dictSchema);
            }
            result.Example = array;
        }
        else if (typeof(IEnumerable).IsAssignableFrom(checkType))
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
                new OpenApiSchema()
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
            var enumDescriptionInt = new List<string>();
            var enumType = "integer";

            var jsonConverterAttr = checkType.GetCustomAttributesData() .FirstOrDefault(a => a.AttributeType == typeof(JsonConverterAttribute));
            if (jsonConverterAttr != null)
            {
                if (jsonConverterAttr.ConstructorArguments.Count > 0 &&
                    jsonConverterAttr.ConstructorArguments[0].Value is Type converterType &&
                    converterType.IsGenericType &&
                    converterType.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>))
                {
                    enumType = "string";
                }
            }


            foreach (var enumValue in Enum.GetValues(checkType))
            {
                var value = checkType.GetMember(enumValue.ToString())[0];
                var enumAttribute = value.GetCustomAttributes<SwaggerEnumAttribute>().FirstOrDefault();
                if (enumAttribute is { Ignore: false })
                {
                    enumDataString.Add(enumValue.ToString());
                    enumVarNames.Add(enumValue.ToString());
                    enumDescriptionDataString.Add(enumAttribute.Description);
                    enumDataInt.Add(Convert.ToInt32(enumValue));
                    enumDescriptionString.Add($"{enumValue} - {enumAttribute.Description}");
                    enumDescriptionInt.Add($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}");
                }
            }

            if (enumDataString.Count > 0)
            {
                result.OneOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchema
                    {
                        Enum = enumDataString,
                        Type = JsonSchemaType.String,
                        Description = $"[{string.Join(", ", enumDescriptionString)}]",
                        Example = enumDataString[0]
                    },
                    new OpenApiSchema
                    {
                        Enum = enumDataInt,
                        Type = JsonSchemaType.Integer,
                        Description = $"[{string.Join(", ", enumDescriptionInt)}]",
                        Example = enumDataInt[0],
                        Extensions = new Dictionary<string, IOpenApiExtension>
                        {
                            ["x-enum-varnames"] = new JsonNodeExtension(enumVarNames),
                            ["x-enum-descriptions"] = new JsonNodeExtension(enumDescriptionDataString)
                        }
                    }
                };
                result.Enum = null;
                result.Type = null;
                result.Format = null;
                result.Extensions = result.Extensions ?? new Dictionary<string, IOpenApiExtension>();
                result.Extensions["x-enum-type"] = new JsonNodeExtension(enumType);
            }
        }
        else if (checkType == typeof(object))
        {
            // result.Example = new JsonArray
            // {
            //     ["int"] = SwaggerSchemaCustomAttribute.DefaultIntExample,
            //     ["string"] = SwaggerSchemaCustomAttribute.DefaultStringExample,
            //     ["boolean"] = true
            // };
        }
        else if (checkType == typeof(TimeSpan))
        {
            var timeSpan = TimeSpan.Zero.ToString();
            result.Example = timeSpan;
        }

        return result;
    }
    private JsonNode GetExample(object exampleValue)
    {
        return exampleValue switch
        {
            string str => str,
            int _int => _int,
            long _long => _long,
            bool _bool => _bool,
            double _double => _double,
            DateTime _dateTime => _dateTime,
            Guid _guid => _guid,
            _ => null
        };
    }
    private JsonNode GenerateFakeData(PropertyInfo propertyInfo)
    {
        var faker = new Faker();
        Randomizer.Seed = new Random(123);
        var fileExtension = ".txt";
        switch (propertyInfo.Name)
        {
            case "Name":
                return faker.Name.FullName();
            case "Email":
                return faker.Internet.Email();
            case "FirstName":
                return faker.Name.FirstName();
            case "LastName":
                return faker.Name.LastName();
            case "Location":
                return faker.Address.FullAddress();
            case "Password":
                return faker.Internet.Password();
            case "Extension":
            case "Ext":
            case "FileExtension":
                return fileExtension;
            case "Title":
                var fileName = faker.System.FileName();
                return fileName.Substring(0, fileName.LastIndexOf('.'));
            case "Id":
            case "FileId":
            case "FolderId":
            case "RoomId":
            case "InstanceId":
            case "UserId":
            case "ProductId":
                if (propertyInfo.PropertyType == typeof(string))
                {
                    return faker.Random.Int(1, 10000).ToString();
                }

                if (propertyInfo.PropertyType == typeof(int))
                {
                    return faker.Random.Int(1, 10000);
                }

                return faker.Random.Guid().ToString();
            default:
                return null;
        }
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