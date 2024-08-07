using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class SwaggerEnumAttribute : Attribute
{
    public SwaggerEnumAttribute(string description = null)
    {
        Ignore = false;
        Description = description;
    }
    public bool Ignore {  get; set; }
    public string Description { get; set; }
}

public class SwaggerEnumFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            var enumData = new List<IOpenApiAny>();
            var enumDescription = new StringBuilder("[");
            foreach(var enumValue in Enum.GetValues(context.Type))
            {
                var value = context.Type.GetMember(enumValue.ToString())[0];
                var enumAttribute = value.GetCustomAttributes<SwaggerEnumAttribute>().FirstOrDefault();
                if (enumAttribute != null && !enumAttribute.Ignore)
                {
                    enumData.Add(new OpenApiString(enumValue.ToString()));
                    enumDescription.Append($"{Convert.ToInt32(enumValue)} - {enumAttribute.Description}, ");
                }
            }
            if(enumData.Count > 0)
            {
                schema.Enum = enumData;
                schema.Description = enumDescription.ToString().TrimEnd(' ').TrimEnd(',') + "]";
            }
        }
    }

}