using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityAuthorization
{
    public string? Id { get; set; }

    public string RegisteredClientId { get; set; } = null!;

    public string PrincipalId { get; set; } = null!;

    public int TenantId { get; set; }

    public string? State { get; set; }

    public string? Attributes { get; set; }

    public string? AuthorizationGrantType { get; set; }

    public string? AuthorizedScopes { get; set; }

    public string? AuthorizationCodeValue { get; set; }

    public string? AuthorizationCodeMetadata { get; set; }

    public DateTime? AuthorizationCodeIssuedAt { get; set; }

    public DateTime? AuthorizationCodeExpiresAt { get; set; }

    public string? AccessTokenType { get; set; }

    public string? AccessTokenValue { get; set; }

    public string? AccessTokenHash { get; set; }

    public string? AccessTokenScopes { get; set; }

    public string? AccessTokenMetadata { get; set; }

    public DateTime? AccessTokenIssuedAt { get; set; }

    public DateTime? AccessTokenExpiresAt { get; set; }

    public string? RefreshTokenValue { get; set; }

    public string? RefreshTokenHash { get; set; }

    public string? RefreshTokenMetadata { get; set; }

    public DateTime? RefreshTokenIssuedAt { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool? IsInvalidated { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual IdentityClient RegisteredClient { get; set; } = null!;

    public DbTenant Tenant { get; set; }
}
