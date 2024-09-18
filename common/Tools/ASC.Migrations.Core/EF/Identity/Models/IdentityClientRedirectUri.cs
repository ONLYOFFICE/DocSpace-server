using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityClientRedirectUri
{
    public string ClientId { get; set; } = null!;

    public string RedirectUri { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}
