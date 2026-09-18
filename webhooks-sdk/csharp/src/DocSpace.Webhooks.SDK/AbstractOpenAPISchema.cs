using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DocSpace.Webhooks.SDK.Model;

/// <summary>
/// Base for the generated <c>oneOf</c> wrappers -- <c>EntryId</c> is the only
/// one in this contract. Its converter discriminates by attempting each branch
/// in turn, which is why <see cref="SerializerSettings"/> must keep
/// <see cref="MissingMemberHandling.Error"/>: that is the signal a branch did
/// not match.
/// </summary>
public abstract partial class AbstractOpenAPISchema
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        MissingMemberHandling = MissingMemberHandling.Error,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy { OverrideSpecifiedNames = false },
        },
    };

    public static readonly JsonSerializerSettings AdditionalPropertiesSerializerSettings = new()
    {
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy { OverrideSpecifiedNames = false },
        },
    };

    /// <summary>The value actually carried, as one of the oneOf branches.</summary>
    public virtual object ActualInstance { get; set; } = default!;

    public bool IsNullable { get; set; }

    public string SchemaType { get; set; } = default!;

    public abstract string ToJson();
}
