using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SwaggerCustomFilter;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Enum,
    AllowMultiple = false)]
public class SwaggerSchemaCustomAttribute : SwaggerSchemaAttribute
{
    public SwaggerSchemaCustomAttribute(string example = null)
    {
        Example = example;
    }

    public string Example { get; set; }
    public string Type { get; set; }
}

public class SwaggerSchemaCustomFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if(context.MemberInfo != null)
        {
            var exampleAttribute = context.MemberInfo.GetCustomAttributes<SwaggerSchemaCustomAttribute>().FirstOrDefault();
            ApplySchemaAttribute(schema, exampleAttribute);
        }
    }

    private void ApplySchemaAttribute(OpenApiSchema schema, SwaggerSchemaCustomAttribute schemaAttribute)
    {
        if(schemaAttribute != null)
        {
            if(schemaAttribute.Example != null)
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiString(schemaAttribute.Example);
            }
            else if(schemaAttribute.Type != null)
            {
                schema.Type = schemaAttribute.Type;
            }
        }

    }
}