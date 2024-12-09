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

using Bogus;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

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
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Enum is { Count: > 0 } && context.Type is { IsEnum: true })
        {
            UpdateSchema(context.Type, schema);
            return;
        }
        
        if (context.MemberInfo is not PropertyInfo propertyInfo)
        {
            return;
        }

        UpdateSchema(propertyInfo.PropertyType, schema);
        
        var swaggerSchemaCustomAttribute = propertyInfo.GetCustomAttributes(true).OfType<SwaggerSchemaCustomAttribute>().FirstOrDefault();

        if (swaggerSchemaCustomAttribute != null)
        {
            if (swaggerSchemaCustomAttribute.Example != null)
            {
                schema.Example = GetExample(swaggerSchemaCustomAttribute.Example);
            }
        }
        else
        {
            var example = GenerateFakeData(propertyInfo);
            if(example != null)
            {
                schema.Example = example;
            }
        }
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
            result.Example = new OpenApiInteger(SwaggerSchemaCustomAttribute.DefaultIntExample);
        } 
        else if (checkType == typeof(long) || checkType == typeof(ulong))
        {
            result.Example = new OpenApiLong(1234);
        }
        else if (checkType == typeof(string))
        {
            result.Example = new OpenApiString(SwaggerSchemaCustomAttribute.DefaultStringExample);
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
        else if(checkType.IsClosedTypeOf(typeof(IDictionary<,>)))
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
            else if(checkType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
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

            if(arraySchema.OneOf.Count != 0)
            {
                result.Items = new OpenApiSchema { AnyOf = arraySchema.OneOf };
            }
            else if(checkType == typeof(object))
            {
                result.Items = new OpenApiSchema { Type = "object" };
            }

        }
        else if(checkType == typeof(JsonElement))
        {
            var oneOfSchema = new List<OpenApiSchema>
            {
                new()
                {
                    Type = "integer",
                    Example = new OpenApiInteger(SwaggerSchemaCustomAttribute.DefaultIntExample)
                },
                new()
                {
                    Type = "string",
                    Example = new OpenApiString(SwaggerSchemaCustomAttribute.DefaultStringExample)
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
                var enumAttribute = value.GetCustomAttributes<SwaggerEnumAttribute>().FirstOrDefault();
                if (enumAttribute is { Ignore: false })
                {
                    enumDataString.Add(new OpenApiString(enumValue.ToString()));
                    enumDataInt.Add(new OpenApiInteger(Convert.ToInt32(enumValue)));
                    enumDescriptionString.Add($"{enumValue} - {enumAttribute.Description}");
                    enumDescriptionInt.Add($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}");
                }
            }

            if (enumDataString.Count > 0)
            {
                result.OneOf = new List<OpenApiSchema>
                {
                    new()
                    {
                        Enum = enumDataString,
                        Type = "string",
                        Description = $"[{string.Join(", ", enumDescriptionString)}]",
                        Example = enumDataString[0]
                    },
                    new()
                    {
                        Enum = enumDataInt,
                        Type = "integer",
                        Description = $"[{string.Join(", ", enumDescriptionInt)}]",
                        Example = enumDataInt[0]
                    }
                };
                result.Enum = null;
                result.Type = null;
                result.Format = null;
            }
        }
        else if(checkType == typeof(object))
        {
            result.Example = new OpenApiObject
            {
                ["int"] = new OpenApiInteger(SwaggerSchemaCustomAttribute.DefaultIntExample),
                ["string"] = new OpenApiString(SwaggerSchemaCustomAttribute.DefaultStringExample),
                ["boolean"] = new OpenApiBoolean(true)
            };
        }
        else if(checkType == typeof(TimeSpan))
        {
            var timeSpan = TimeSpan.Zero.ToString();
            result.Example = new OpenApiString(timeSpan);
        }
        else
        {
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
    private IOpenApiAny GenerateFakeData(PropertyInfo propertyInfo)
    {
        var faker = new Faker();
        var fileExtension = ".txt";
        switch (propertyInfo.Name)
        {
            case "Name":
                return new OpenApiString(faker.Name.FullName());
            case "Email":
                return new OpenApiString(faker.Internet.Email());
            case "FirstName":
                return new OpenApiString(faker.Name.FirstName());
            case "LastName":
                return new OpenApiString(faker.Name.LastName());
            case "Location":
                return new OpenApiString(faker.Address.FullAddress());
            case "Password":
                    return new OpenApiString(faker.Internet.Password());
            case "Extension":
            case "Ext":
            case "FileExtension":
                return new OpenApiString(fileExtension);
            case "Title":
                var fileName = faker.System.FileName();
                return new OpenApiString(fileName.Substring(0, fileName.LastIndexOf('.')));
            case "Id":
            case "FileId":
            case "FolderId":
            case "RoomId":
            case "InstanceId":
            case "UserId":
            case "ProductId":
                if(propertyInfo.PropertyType == typeof(string))
                {
                    return new OpenApiString(faker.Random.Int(1, 10000).ToString());
                }
                else if(propertyInfo.PropertyType == typeof(int))
                {
                    return new OpenApiInteger(faker.Random.Int(1, 10000));
                }
                else
                {
                    return new OpenApiString(Guid.NewGuid().ToString());
                }
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
