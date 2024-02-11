using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ASC.Api.Core.Cors.Accessors;
internal interface ICorsPolicyAccessor
{
    CorsPolicy GetPolicy();
    CorsPolicy GetPolicy(string name);
}