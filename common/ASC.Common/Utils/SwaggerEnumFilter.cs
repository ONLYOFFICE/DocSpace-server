using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.All)]
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