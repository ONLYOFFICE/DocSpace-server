using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityScope
{
    public string Name { get; set; } = null!;

    public string Group { get; set; } = null!;

    public string Type { get; set; } = null!;

    public virtual ICollection<IdentityConsentScope> IdentityConsentScopes { get; set; } = new List<IdentityConsentScope>();
}
