using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerSchemaCustomAttribute : SwaggerSchemaAttribute
{
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

public class SwaggerSchemaCustomStringAttribute : SwaggerSchemaCustomAttribute<string>
{
    internal static readonly string DefaultExample = "some text";
    public SwaggerSchemaCustomStringAttribute(string description = null)
    {
        Description = description;
        Example = DefaultExample;
    }
}

public class SwaggerSchemaCustomIntAttribute : SwaggerSchemaCustomAttribute<int>
{
    internal static readonly int DefaultExample = 1234;
    public SwaggerSchemaCustomIntAttribute(string description = null)
    {
        Description = description;
        Example = DefaultExample;
    }
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

        var swaggerSchemaCustomAttribute = propertyInfo.GetCustomAttributes(true).OfType<SwaggerSchemaCustomAttribute>().FirstOrDefault();

        if (swaggerSchemaCustomAttribute != null)
        {
            UpdateSchema(propertyInfo.PropertyType, schema);
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
            result.Example = new OpenApiInteger(SwaggerSchemaCustomIntAttribute.DefaultExample);
        } 
        else if (checkType == typeof(long) || checkType == typeof(ulong))
        {
            result.Example = new OpenApiLong(1234);
        }
        else if (checkType == typeof(string))
        {
            result.Example = new OpenApiString(SwaggerSchemaCustomStringAttribute.DefaultExample);
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
        else if(typeof(IEnumerable).IsAssignableFrom(checkType))
        {
            var array = new OpenApiArray();
            if (checkType.IsGenericType)
            {
                checkType = checkType.GenericTypeArguments.FirstOrDefault();
            }
            else if(checkType.IsArray)
            {
                checkType = checkType.GetElementType();
            }
            
            var arraySchema = UpdateSchema(checkType, new OpenApiSchema());
            if (arraySchema is { Example: not null })
            {
                array.Add(arraySchema.Example);
                result.Example = array;
            }
        }
        else if(checkType == typeof(JsonElement))
        {
            var oneOfSchema = new List<OpenApiSchema>
            {
                new OpenApiSchema
                {
                    Type = "integer",
                    Example = new OpenApiInteger(SwaggerSchemaCustomIntAttribute.DefaultExample)
                },
                new OpenApiSchema
                {
                    Type = "string",
                    Example = new OpenApiString(SwaggerSchemaCustomStringAttribute.DefaultExample)
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
}