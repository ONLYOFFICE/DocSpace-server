using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityCert
{
    public string Id { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public sbyte PairType { get; set; }

    public string PrivateKey { get; set; } = null!;

    public string PublicKey { get; set; } = null!;
}
