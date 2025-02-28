// (c) Copyright Ascensio System SIA 2009-2024
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

using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

using Bogus;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.Property)]
public class OpenApiDescriptionAttribute : DescriptionAttribute
{
    internal static readonly string DefaultStringExample = "some text";
    internal static readonly int DefaultIntExample = 1234;
    public OpenApiDescriptionAttribute(string description)
    {
        DescriptionValue = description;
    }
    
    public object Example { get; set; }
}

public class OpenApiDescriptionSchemaTransformer : IOpenApiSchemaTransformer, IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Parameters == null)
        {
            return Task.CompletedTask;
        }
        foreach (var parameter in operation.Parameters)
        {
            var parameterDescriptor = context.Description.ParameterDescriptions?.FirstOrDefault(p => p.Name.Equals(parameter.Name));
            if (parameterDescriptor == null)
            {
                continue;
            }
            var propertyInfo = parameterDescriptor?.ModelMetadata;
            var modelType = propertyInfo?.UnderlyingOrModelType;
            if (modelType != null)
            {
                UpdateSchema(modelType, parameter.Schema);
            }
            var propertyName = propertyInfo?.Name;
            var attr = propertyInfo?.ContainerType?.GetMembers()?.FirstOrDefault(m => m.Name.Equals(propertyName))?.GetCustomAttribute<OpenApiDescriptionAttribute>();
            if (attr != null)
            {
                parameter.Description = attr.Description;
                if (attr.Example != null)
                {
                    parameter.Example = GetExample(attr.Example);
                }
            }
            else
            {
                var example = GenerateFakeData(parameter.Name, parameter.GetType());
                if (example != null)
                {
                    parameter.Example = example;
                }
            }
        }
        return Task.CompletedTask;
    }
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema.Enum is { Count: > 0 })
        {
            UpdateSchema(context.JsonTypeInfo.Type, schema);
            return Task.CompletedTask;
        }
        var propertyInfo = context?.JsonPropertyInfo;
        if (propertyInfo == null) 
        {
            return Task.CompletedTask;
        }
        UpdateSchema(propertyInfo.PropertyType, schema);

        var openApiCustomAttribute = context?.JsonPropertyInfo?.AttributeProvider?.GetCustomAttributes(true)?.OfType<OpenApiDescriptionAttribute>().FirstOrDefault();

        if (openApiCustomAttribute != null)
        {
            if (openApiCustomAttribute.Example != null)
            {
                schema.Example = GetExample(openApiCustomAttribute.Example);
            }
        }
        else
        {
            var example = GenerateFakeData(propertyInfo.Name, propertyInfo.PropertyType);
            if (example != null)
            {
                schema.Example = example;
            }
        }

        return Task.CompletedTask;
    }

    private OpenApiSchema UpdateSchema(Type checkType, OpenApiSchema result)
    {
        var nullableType = Nullable.GetUnderlyingType(checkType);
        if (nullableType != null)
        {
            checkType = nullableType;
        }

        if (checkType == typeof(int))
        {
            result.Example = new OpenApiInteger(OpenApiDescriptionAttribute.DefaultIntExample);
        }
        else if (checkType == typeof(long) || checkType == typeof(ulong))
        {
            result.Example = new OpenApiLong(1234);
        }
        else if (checkType == typeof(string))
        {
            result.Example = new OpenApiString(OpenApiDescriptionAttribute.DefaultStringExample);
        }
        else if (checkType == typeof(bool))
        {
            result.Example = new OpenApiBoolean(true);
        }
        else if (checkType == typeof(double))
        {
            result.Example = new OpenApiDouble(-8.5);
        }
        else if (checkType == typeof(DateTime))
        {
            result.Example = new OpenApiDateTime(new DateTime(2008, 4, 10, 06, 30, 00));
        }
        else if (checkType == typeof(Guid))
        {
            result.Example = new OpenApiString(new Guid("{75A5F745-F697-4418-B38D-0FE0D277E258}").ToString());
        }
        else if (checkType.IsClosedTypeOf(typeof(IDictionary<,>)))
        {
            var array = new OpenApiArray();
            if (checkType.IsGenericType)
            {
                var dictSchema = new OpenApiObject();
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
            var array = new OpenApiArray();

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
            else if (checkType.IsGenericType && checkType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && checkType.GetGenericArguments().FirstOrDefault().IsEnum)
            {
                checkType = checkType.GetGenericArguments().FirstOrDefault();
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

            if (arraySchema.OneOf.Count != 0)
            {
                result.Items = new OpenApiSchema { AnyOf = arraySchema.OneOf };
            }
            else if (checkType == typeof(object))
            {
                result.Items = new OpenApiSchema { Type = "object" };
            }
            else if (checkType.IsEnum) 
            {
                result.Items = new OpenApiSchema { Type = arraySchema.Type, Enum = arraySchema.Enum, Description = arraySchema.Description, Example = arraySchema.Example };
            }

        }
        else if (checkType == typeof(JsonElement))
        {
            var oneOfSchema = new List<OpenApiSchema>
            {
                new()
                {
                    Type = "integer",
                    Example = new OpenApiInteger(OpenApiDescriptionAttribute.DefaultIntExample)
                },
                new()
                {
                    Type = "string",
                    Example = new OpenApiString(OpenApiDescriptionAttribute.DefaultStringExample)
                }
            };

            result.OneOf = oneOfSchema;
        }
        else if (checkType.IsEnum)
        {
            var enumDataString = new List<IOpenApiAny>();
            var enumDescriptionString = new List<string>();
            var enumDataInt = new List<IOpenApiAny>();
            var enumDescriptionInt = new List<string>();

            foreach (var enumValue in Enum.GetValues(checkType))
            {
                var value = checkType.GetMember(enumValue.ToString())[0];
                var enumAttribute = value.GetCustomAttributes<OpenApiEnumAttribute>().FirstOrDefault();
                if (enumAttribute is { Ignore: false })
                {
                    enumDataString.Add(new OpenApiString(enumValue.ToString()));
                    enumDataInt.Add(new OpenApiInteger(Convert.ToInt32(enumValue)));
                    enumDescriptionString.Add($"{enumValue} - {enumAttribute.Description}");
                    enumDescriptionInt.Add($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}");
                }
            }
            if(enumDataInt.Count > 0)
            {
                result.Enum = enumDataInt;
                result.Description = $"[{string.Join(", ", enumDescriptionInt)}]";
                result.Type = "integer";
                result.Example = enumDataInt[0];
            }
        }
        else if (checkType == typeof(object))
        {
            result.Example = new OpenApiObject
            {
                ["int"] = new OpenApiInteger(OpenApiDescriptionAttribute.DefaultIntExample),
                ["string"] = new OpenApiString(OpenApiDescriptionAttribute.DefaultStringExample),
                ["boolean"] = new OpenApiBoolean(true)
            };
        }
        else if (checkType == typeof(TimeSpan))
        {
            var timeSpan = TimeSpan.Zero.ToString();
            result.Example = new OpenApiString(timeSpan);
        }
        return result;
    }
    private IOpenApiAny GetExample(object exampleValue)
    {
        return exampleValue switch
        {
            string _str => new OpenApiString(_str),
            int _int => new OpenApiInteger(_int),
            long _long => new OpenApiLong(_long),
            bool _bool => new OpenApiBoolean(_bool),
            double _double => new OpenApiDouble(_double),
            DateTime _dateTime => new OpenApiDateTime(_dateTime),
            _ => null
        };
    }

    private IOpenApiAny GenerateFakeData(string name, Type type)
    {
        var faker = new Faker();
        Randomizer.Seed = new Random(123);
        var fileExtension = ".txt";
        switch (name)
        {
            case "name":
                return new OpenApiString(faker.Name.FullName());
            case "email":
                return new OpenApiString(faker.Internet.Email());
            case "firstName":
                return new OpenApiString(faker.Name.FirstName());
            case "lastName":
                return new OpenApiString(faker.Name.LastName());
            case "location":
                return new OpenApiString(faker.Address.FullAddress());
            case "password":
                return new OpenApiString(faker.Internet.Password());
            case "extension":
            case "ext":
            case "fileExtension":
                return new OpenApiString(fileExtension);
            case "Title":
                var fileName = faker.System.FileName();
                return new OpenApiString(fileName.Substring(0, fileName.LastIndexOf('.')));
            case "id":
            case "fileId":
            case "folderId":
            case "roomId":
            case "instanceId":
            case "userId":
            case "productId":
                if (type == typeof(string))
                {
                    return new OpenApiString(faker.Random.Int(1, 10000).ToString());
                }

                if (type == typeof(int))
                {
                    return new OpenApiInteger(faker.Random.Int(1, 10000));
                }

                return new OpenApiString(faker.Random.Guid().ToString());
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

