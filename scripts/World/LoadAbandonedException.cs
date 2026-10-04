using System;

namespace Vestiges.World;

/// <summary>La scène de run a été quittée pendant son chargement : ce n'est pas une erreur.</summary>
public sealed class LoadAbandonedException : Exception
{
    public LoadAbandonedException() : base("scène quittée pendant le chargement") { }
}
