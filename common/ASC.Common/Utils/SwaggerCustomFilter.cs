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
        Example = "some text";
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
            var (defaultExample, nullable, format) = GetExample(propertyInfo.PropertyType);
            if (swaggerSchemaCustomAttribute.Example != null)
            {
                schema.Example = GetExample(swaggerSchemaCustomAttribute.Example) ?? defaultExample;
            }
            else
            {
                schema.Example = defaultExample;
            }

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
            schema.Example = GetExample(exampleValue);
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
            example = new OpenApiLong(1234);
            format = "int64";
        }
        else if (checkType == typeof(string))
        {
            example = new OpenApiString(SwaggerSchemaCustomStringAttribute.DefaultExample);
        }
        else if (checkType == typeof(bool))
        {
            example = new OpenApiBoolean(true);
        }
        else if (checkType == typeof(double))
        {
            example = new OpenApiDouble(-8.5);
            format = "double";
        }
        else if (checkType == typeof(DateTime))
        {
            example = new OpenApiDateTime(new DateTime(2008, 4, 10, 06, 30, 00));
        }
        else if (checkType == typeof(Guid))
        {
            example = new OpenApiString(new Guid("{75A5F745-F697-4418-B38D-0FE0D277E258}").ToString());
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

        return (example, nullable, format);
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