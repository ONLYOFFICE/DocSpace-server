using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityClientAuthenticationMethod
{
    public string ClientId { get; set; } = null!;

    public string AuthenticationMethod { get; set; } = null!;

    public virtual IdentityClient Client { get; set; } = null!;
}
