using System;
using System.Collections.Generic;

namespace ASC.Migrations.Core.Identity;

public partial class IdentityShedlock
{
    public string Name { get; set; } = null!;

    public DateTime LockUntil { get; set; }

    public DateTime LockedAt { get; set; }

    public string LockedBy { get; set; } = null!;
}
