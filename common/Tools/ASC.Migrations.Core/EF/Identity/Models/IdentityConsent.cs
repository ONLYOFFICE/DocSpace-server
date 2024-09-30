using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityConsent
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public bool? IsInvalidated { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual IdentityClient RegisteredClient { get; set; } = null!;

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}
