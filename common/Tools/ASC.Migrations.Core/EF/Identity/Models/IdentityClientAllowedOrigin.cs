using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityClientAllowedOrigin
{
    public string ClientId { get; set; } = null!;

    public string AllowedOrigin { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}
