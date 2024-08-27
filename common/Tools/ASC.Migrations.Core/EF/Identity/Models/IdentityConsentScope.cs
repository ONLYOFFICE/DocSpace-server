using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityConsentScope
{
    public string PrincipalId { get; set; } = null!;

    public string RegisteredClientId { get; set; } = null!;

    public string ScopeName { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;

    public virtual IdentityConsent Consent { get; set; } = null!;
}
