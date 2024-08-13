using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityClientScope
{
    public string ClientId { get; set; } = null!;

    public string ScopeName { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;

    public virtual IdentityScope ScopeNameNavigation { get; set; } = null!;
}
