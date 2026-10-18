using Godot;
using Vestiges.World;

namespace Vestiges.Core;

/// <summary>
/// Saut du vide (plan 52 E3) : dès que le rang de la Série donne une allonge, le dash est porté au-dessus du Néant et
/// s'y prolonge. Retomber sur le sol après avoir franchi assez de vide relance la Série ; retomber dans le vide revient à
/// y marcher.
/// Sans allonge (rang 0, Élan éteint), le dash s'arrête au bord comme avant.
/// </summary>
public partial class Player
{
    // Vitesse de montée et de descente de la hauteur visible du saut, en pixels par seconde.
    private const float LeapHeightRate = 200f;
    // Pas d'échantillonnage du vide franchi sur un trajet de dash.
    private const float VoidSampleStep = 4f;

    // Le dash en cours est porté au-dessus du vide : décidé au départ, selon le rang d'alors.
    private bool _dashLeaps;
    private float _leapVoidPixels;
    private float _leapHeight;

    /// <summary>Sauts du vide réussis de la run (affichage de réglage du bac à sable).</summary>
    public int VoidLeaps { get; private set; }

    /// <summary>
    /// Dash porté par l'Élan en cours, au-dessus du vide ou non : tant qu'il dure, le Néant ne prend rien et la phase du
    /// joueur ne change pas.
    /// </summary>
    public bool IsLeaping => _dashLeaps && Mobility.IsDashing;

    /// <summary>Avant le pas de mobilité : allonge du rang actuel, et le joueur est-il au-dessus du vide.</summary>
    private void PrepareVoidLeap()
    {
        Mobility.LeapExtensionPixels = Streak.Enabled ? Streak.Config.VoidLeapExtensionAt(Streak.Rank) : 0f;
        Mobility.OverVoid = _dashLeaps && IsVoidAt(GlobalPosition);
    }

    /// <summary>
    /// Après le pas de mobilité : un dash qui part fixe sa règle pour toute sa durée. Le saut instantané (Plan de métro)
    /// n'est jamais porté : il s'arrête au bord du vide comme avant.
    /// </summary>
    private void LatchVoidLeap()
    {
        if (!Mobility.StartedThisStep)
            return;
        // Un dash qui repart à l'image où le saut précédent finit : la retombée du précédent compte d'abord.
        if (_dashLeaps)
            LandFromLeap();
        _dashLeaps = Mobility.LeapExtensionPixels > 0f && !Mobility.BlinkedThisStep;
        _leapVoidPixels = 0f;
    }

    /// <summary>Après le déplacement : vide franchi, retombée, hauteur visible.</summary>
    private void StepVoidLeap(float delta, Vector2 previousPosition, bool dashMovement)
    {
        if (_dashLeaps && dashMovement)
            _leapVoidPixels += VoidDistanceAlong(previousPosition, GlobalPosition);
        if (_dashLeaps && !Mobility.IsDashing)
            LandFromLeap();
        float target = IsLeaping && IsVoidAt(GlobalPosition) ? Streak.Config.VoidLeapHeightPixels : 0f;
        _leapHeight = Mathf.MoveToward(_leapHeight, target, LeapHeightRate * delta);
    }

    /// <summary>
    /// Fin d'un dash porté. Retombée sur le sol après assez de vide parcouru (un dash parti du Néant qui en sort compte
    /// aussi, le séjour y a été payé) : la fenêtre de la Série repart, avec une onde et un son.
    /// </summary>
    private void LandFromLeap()
    {
        _dashLeaps = false;
        if (_leapVoidPixels < Streak.Config.VoidLeapMinVoidPixels || IsVoidAt(GlobalPosition))
            return;
        VoidLeaps++;
        Streak.Refresh("saut du vide");
        _mobilityFeedback?.VoidLeapLanded(Streak.Config.VoidLeapLandingAudio);
    }

    /// <summary>Pause, menu ou mort : le saut retombe aussitôt à l'écran.</summary>
    private void ResetVoidLeapHeight() => _leapHeight = 0f;

    /// <summary>Une suspension en plein saut au-dessus du vide garde le saut, qui reprend ensuite (pause, montée de niveau).</summary>
    private bool KeepsLeapThroughSuspension => IsLeaping && IsVoidAt(GlobalPosition);

    /// <summary>Phase Néant à cette position (saut du joueur, et créatures quand le vide est une arme).</summary>
    internal bool IsVoidAt(Vector2 position) =>
        _erasureManager?.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void;

    /// <summary>Longueur du trajet au-dessus du Néant, échantillonnée tous les quelques pixels (dash seulement).</summary>
    private float VoidDistanceAlong(Vector2 from, Vector2 to)
    {
        float length = from.DistanceTo(to);
        if (length <= 0f || _erasureManager == null)
            return 0f;
        int samples = Mathf.Max(1, Mathf.CeilToInt(length / VoidSampleStep));
        int overVoid = 0;
        for (int i = 1; i <= samples; i++)
        {
            if (IsVoidAt(from.Lerp(to, (float)i / samples)))
                overVoid++;
        }
        return length * overVoid / samples;
    }
}
