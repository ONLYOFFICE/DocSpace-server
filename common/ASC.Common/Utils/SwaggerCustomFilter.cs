using Microsoft.OpenApi;
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


[AttributeUsage(AttributeTargets.Property)]
public class SwaggerSchemaCustomAttribute<T> : SwaggerSchemaAttribute
{
    public SwaggerSchemaCustomAttribute(string description = null)
    {
        Description = description;
    }

    public T Example { get; set; }
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
            return;
        }
            
        var attribute = context.MemberInfo.GetCustomAttributes(true).FirstOrDefault(attr =>
        {
            var baseType = attr.GetType().BaseType;
            if (baseType == null)
            {
                return false;
            }

            return baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SwaggerSchemaCustomAttribute<>);
        });

        if (attribute == null)
        {
            return;
        }

        var exampleValue = GetExampleValue(attribute);

        if (exampleValue != null)
        {
            schema.Example = GetExample(exampleValue);
        }
    }

    private object GetExampleValue(object attribute)
    {
        var attributeType = attribute.GetType();
        var exampleProperty = attributeType.GetProperty("Example");
        return exampleProperty == null ? null : exampleProperty.GetValue(attribute);
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
            else
            {
                checkType = checkType.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()
                    .FirstOrDefault() ?? typeof(object);
            }

            var arraySchema = UpdateSchema(checkType, new OpenApiSchema());
            if (arraySchema?.Example != null)
            {
                array.Add(arraySchema.Example);
            }
            result.Example = array;
            if(arraySchema.OneOf != null)
            {
                result.Items = new OpenApiSchema { AnyOf = arraySchema.OneOf };
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
            var enumData = new List<IOpenApiAny>();
            var enumDescription = new List<string>();
            
            foreach(var enumValue in Enum.GetValues(checkType))
            {
                var value = checkType.GetMember(enumValue.ToString())[0];
                var enumAttribute = value.GetCustomAttributes<SwaggerEnumAttribute>().FirstOrDefault();
                if (enumAttribute is { Ignore: false })
                {
                    enumData.Add(new OpenApiString(enumValue.ToString()));
                    enumDescription.Add($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}");
                }
            }

            if(enumData.Count > 0)
            {
                result.Format = null;
                result.Type = "string";
                result.Enum = enumData;
                result.Description = $"[{string.Join(", ", enumDescription)}]";
                result.Example = enumData[0];
            }
        }
        else if(checkType == typeof(object))
        {
            var openApiSchema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { 
                        "key1", new ()
                        {
                            Type = "integer",
                            Example = new OpenApiInteger(SwaggerSchemaCustomAttribute.DefaultIntExample)
                        }
                    },
                    { 
                        "key2", new ()
                        {
                            Type = "string",
                            Example = new OpenApiString(SwaggerSchemaCustomAttribute.DefaultStringExample)
                        }
                    },
                    { 
                        "key3", new ()
                        {
                            Type = "boolean",
                            Example = new OpenApiBoolean(false)
                        }
                    }
                }
            };
            result.Properties = openApiSchema.Properties;
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

    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               type == typeof(JsonElement);
    }
}

public class HideRouteDocumentFilter : IDocumentFilter
{
    private readonly string _routeToHide;

    public HideRouteDocumentFilter(string RouteToHide)
    {
        _routeToHide = RouteToHide;
    }

    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        if (document.Paths.ContainsKey(_routeToHide))
        {
            document.Paths.Remove(_routeToHide);
        }
    }
}