using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityClient
{
    public string ClientId { get; set; } = null!;

    public int TenantId { get; set; }

    public string? ClientSecret { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Logo { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? TermsUrl { get; set; }

    public string? PolicyUrl { get; set; }

    public string? LogoutRedirectUri { get; set; }

    public bool? IsPublic { get; set; }

    public bool? IsEnabled { get; set; }

    public bool? IsInvalidated { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public virtual ICollection<IdentityAuthorization> IdentityAuthorizations { get; set; } = new List<IdentityAuthorization>();

    public virtual ICollection<IdentityConsent> IdentityConsents { get; set; } = new List<IdentityConsent>();

    public DbTenant Tenant { get; set; }
}
