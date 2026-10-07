using System;

namespace Vestiges.Combat;

/// <summary>États d'une créature qui se lisent sur son sprite (plan 27 V1).</summary>
[Flags]
public enum StatusMarks
{
    None = 0,
    Frozen = 1,
    Slowed = 2,
    Burning = 4,
    Bleeding = 8,
    Disoriented = 16,
    Fragile = 32,
}
