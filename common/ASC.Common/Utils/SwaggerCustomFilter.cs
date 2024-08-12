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
        Example = "some text";
    }
}

public class SwaggerSchemaCustomBooleanAttribute : SwaggerSchemaCustomAttribute<bool>
{
    internal static readonly bool DefaultExample = true;
    public SwaggerSchemaCustomBooleanAttribute(string description = null)
    {
        Description = description;
        Example = true;
    }
}

public class SwaggerSchemaCustomIntAttribute : SwaggerSchemaCustomAttribute<int>
{
    internal static readonly int DefaultExample = 1234;
    public SwaggerSchemaCustomIntAttribute(string description = null)
    {
        Description = description;
        Example = 1234;
    }
}

public class SwaggerSchemaCustomLongAttribute : SwaggerSchemaCustomAttribute<long>
{
    internal static readonly long DefaultExample = 1234;
    public SwaggerSchemaCustomLongAttribute(string description = null)
    {
        Description = description;
        Example = 1234;
    }
}

public class SwaggerSchemaCustomDoubleAttribute : SwaggerSchemaCustomAttribute<double>
{
    internal static readonly double DefaultExample = -8.5;
    public SwaggerSchemaCustomDoubleAttribute(string description = null)
    {
        Description = description;
        Example = -8.5;
    }
}

public class SwaggerSchemaCustomGuidAttribute : SwaggerSchemaCustomAttribute<string>
{    
    internal static Guid DefaultExample = new("{75A5F745-F697-4418-B38D-0FE0D277E258}");
    public SwaggerSchemaCustomGuidAttribute(string description = null)
    {
        Description = description;
        Example = DefaultExample.ToString();
    }
}

public class SwaggerSchemaCustomDateTimeAttribute : SwaggerSchemaCustomAttribute<DateTime>
{
    internal static DateTime DefaultExample = new(2008, 4, 10, 06, 30, 00);
    public SwaggerSchemaCustomDateTimeAttribute(string description = null)
    {
        Description = description;
        Example = DefaultExample;
    }
}

public class SwaggerSchemaCustomFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo is not PropertyInfo propertyInfo)
        {
            return;
        }

        var swaggerSchemaCustomAttribute = propertyInfo.GetCustomAttributes(true).OfType<SwaggerSchemaCustomAttribute>().FirstOrDefault();

        if (swaggerSchemaCustomAttribute != null)
        {
            var (example, nullable, format) = GetExample(propertyInfo.PropertyType);
            schema.Example = example;
            schema.Nullable = nullable;
            if (!string.IsNullOrEmpty(format))
            {
                schema.Format = format;
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
            ApplySchemaAttribute(schema, exampleValue);
        }
    }

    private object GetExampleValue(object attribute)
    {
        var attributeType = attribute.GetType();
        var exampleProperty = attributeType.GetProperty("Example");
        return exampleProperty == null ? null : exampleProperty.GetValue(attribute);
    }

    private (IOpenApiAny, bool, string) GetExample(Type checkType)
    {
        IOpenApiAny example = null;
        var nullable = false;
        string format = null;
        
        var nullableType = Nullable.GetUnderlyingType(checkType);
        if (nullableType != null)
        {
            nullable = true;
            checkType = nullableType;
        }
            
        if (checkType == typeof(int))
        {
            example = new OpenApiInteger(SwaggerSchemaCustomIntAttribute.DefaultExample);
            format = "int32";
        } 
        else if (checkType == typeof(long) || checkType == typeof(ulong))
        {
            example = new OpenApiLong(SwaggerSchemaCustomLongAttribute.DefaultExample);
            format = "int64";
        }
        else if (checkType == typeof(string))
        {
            example = new OpenApiString(SwaggerSchemaCustomStringAttribute.DefaultExample);
        }
        else if (checkType == typeof(bool))
        {
            example = new OpenApiBoolean(SwaggerSchemaCustomBooleanAttribute.DefaultExample);
        }
        else if (checkType == typeof(double))
        {
            example = new OpenApiDouble(SwaggerSchemaCustomDoubleAttribute.DefaultExample);
            format = "double";
        }
        else if (checkType == typeof(DateTime))
        {
            example = new OpenApiDateTime(SwaggerSchemaCustomDateTimeAttribute.DefaultExample);
        }
        else if (checkType == typeof(Guid))
        {
            example = new OpenApiString(SwaggerSchemaCustomGuidAttribute.DefaultExample.ToString());
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
            
            var (arrayExample, _, _) = GetExample(checkType);
            array.Add(arrayExample);
            example = array;
        }
        else
        {
            var a = 0;
        }

        return (example, nullable, format);
    } 
    private void ApplySchemaAttribute(OpenApiSchema schema, object exampleValue)
    {
        schema.Example = exampleValue switch
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