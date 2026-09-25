using Godot;

namespace Vestiges.Combat;

/// <summary>Trois tons et un contour teinté, du plus clair au contour (Charte §5 : jamais de noir pur).</summary>
public readonly record struct FxRamp(Color Light, Color Mid, Color Dark, Color Outline);
