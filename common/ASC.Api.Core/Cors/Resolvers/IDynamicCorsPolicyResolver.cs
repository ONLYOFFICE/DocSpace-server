namespace ASC.Api.Core.Cors.Resolvers;
public interface IDynamicCorsPolicyResolver
{
    Task<bool> ResolveForOrigin(string origin);
}