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
    public SwaggerSchemaCustomStringAttribute(string description = null)
    {
        Description = description;
        Example = "some text";
    }
}

public class SwaggerSchemaCustomBooleanAttribute : SwaggerSchemaCustomAttribute<bool>
{
    public SwaggerSchemaCustomBooleanAttribute(string description = null)
    {
        Description = description;
        Example = true;
    }
}

public class SwaggerSchemaCustomIntAttribute : SwaggerSchemaCustomAttribute<int>
{
    public SwaggerSchemaCustomIntAttribute(string description = null)
    {
        Description = description;
        Example = 1234;
    }
}

public class SwaggerSchemaCustomLongAttribute : SwaggerSchemaCustomAttribute<long>
{
    public SwaggerSchemaCustomLongAttribute(string description = null)
    {
        Description = description;
        Example = 1234;
    }
}

public class SwaggerSchemaCustomDoubleAttribute : SwaggerSchemaCustomAttribute<double>
{
    public SwaggerSchemaCustomDoubleAttribute(string description = null)
    {
        Description = description;
        Example = -8.5;
    }
}

public class SwaggerSchemaCustomGuidAttribute : SwaggerSchemaCustomAttribute<string>
{
    public SwaggerSchemaCustomGuidAttribute(string description = null)
    {
        Description = description;
        Example = new Guid("{75A5F745-F697-4418-B38D-0FE0D277E258}").ToString();
    }
}

public class SwaggerSchemaCustomDateTimeAttribute : SwaggerSchemaCustomAttribute<DateTime>
{
    public SwaggerSchemaCustomDateTimeAttribute(string description = null)
    {
        Description = description;
        Example = new DateTime(2008, 4, 10, 06, 30, 00);
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
            if (propertyInfo.PropertyType == typeof(int))
            {
                schema.Example = new OpenApiInteger(1234);
            } 
            else if (propertyInfo.PropertyType == typeof(long))
            {
                schema.Example = new OpenApiLong(1234);
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                schema.Example = new OpenApiString("some text");
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                schema.Example = new OpenApiBoolean(true);
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                schema.Example = new OpenApiDouble(-8.5);
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                schema.Example = new OpenApiDateTime(new DateTime(2008, 4, 10, 06, 30, 00));
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