using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Ce que Forward+ ouvre au bac à sable du relief (plan 60 §26, lot 60.0f), chaque effet basculé seul : ombres de contact
/// (SSAO), brume volumétrique, et lumières (une par projectile, une par lueur des décors, avec un halo). Le Compatibility
/// n'a aucun des trois : sous lui, rien ne s'allume et l'affichage le dit.
/// </summary>
public sealed class ReliefAtmosphere
{
    private readonly ReliefConfig.AtmosphereStyle _style;
    private readonly OmniLight3D[] _projectileLights;
    private readonly List<OmniLight3D> _glowLights = new();

    public bool Supported { get; }
    public bool Ssao;
    public bool Fog;
    public bool Lights;

    public ReliefAtmosphere(ReliefConfig.AtmosphereStyle style, Node3D world, int projectiles,
        IEnumerable<(Vector3 At, Color Color)> glows)
    {
        _style = style;
        Supported = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
        Ssao = style.StartSsao;
        Fog = style.StartFog;
        Lights = style.StartLights;
        _projectileLights = new OmniLight3D[Supported ? projectiles : 0];
        if (!Supported)
            return;
        Node3D container = new() { Name = "AtmosphereLights" };
        world.AddChild(container);
        for (int p = 0; p < _projectileLights.Length; p++)
        {
            _projectileLights[p] = NewLight($"BoltLight{p}", style.ProjectileColor, style.ProjectileEnergy, style.ProjectileRange);
            _projectileLights[p].Visible = false;
            container.AddChild(_projectileLights[p]);
        }
        foreach ((Vector3 at, Color color) in glows)
        {
            if (_glowLights.Count >= style.MaxGlows)
                break;
            OmniLight3D light = NewLight($"GlowLight{_glowLights.Count}", color, style.GlowEnergy, style.GlowRange);
            light.Position = at;
            container.AddChild(light);
            _glowLights.Add(light);
        }
        GD.Print($"[ReliefAtmosphere] Forward+ : {_projectileLights.Length} lumières de projectile, {_glowLights.Count} lueurs de décor");
    }

    private static OmniLight3D NewLight(string name, Color color, float energy, float range) => new()
    {
        Name = name,
        LightColor = color,
        LightEnergy = energy,
        OmniRange = range,
        OmniAttenuation = 1.6f,
        // Des dizaines de petites lumières : sans ombre, le coût reste celui de l'éclairage par grappes.
        ShadowEnabled = false,
    };

    /// <summary>
    /// Règle l'environnement selon les effets choisis ; à rappeler après tout réglage de la vue. La brume ne vaut qu'en
    /// caméra libre (la vue relief 2D est orthographique) et y remplace le brouillard de profondeur.
    /// </summary>
    public void Apply(Environment env, bool is3D)
    {
        bool ssao = Supported && Ssao;
        env.SsaoEnabled = ssao;
        if (ssao)
        {
            env.SsaoRadius = _style.SsaoRadius;
            env.SsaoIntensity = _style.SsaoIntensity;
            env.SsaoPower = _style.SsaoPower;
            env.SsaoDetail = _style.SsaoDetail;
        }
        bool fog = Supported && Fog && is3D;
        env.VolumetricFogEnabled = fog;
        if (fog)
            env.FogEnabled = false;
        if (fog)
        {
            env.VolumetricFogDensity = _style.FogDensity;
            env.VolumetricFogAlbedo = _style.FogAlbedo;
            env.VolumetricFogEmission = _style.FogEmission;
            env.VolumetricFogAnisotropy = _style.FogAnisotropy;
            env.VolumetricFogLength = _style.FogLength;
            env.VolumetricFogAmbientInject = _style.FogAmbientInject;
        }
        bool lights = Supported && Lights;
        env.GlowEnabled = lights;
        if (lights)
        {
            env.GlowIntensity = _style.Bloom;
            env.GlowHdrThreshold = _style.BloomThreshold;
            env.GlowBlendMode = Environment.GlowBlendModeEnum.Additive;
        }
        foreach (OmniLight3D light in _glowLights)
            light.Visible = lights;
        if (!lights)
        {
            foreach (OmniLight3D light in _projectileLights)
                light.Visible = false;
        }
    }

    /// <summary>Les lumières suivent les projectiles en vol ; positions déjà en coordonnées du monde.</summary>
    public void Update(ReliefSim sim, System.Func<Vector3, Vector3> toWorld)
    {
        if (!Supported || !Lights)
            return;
        for (int p = 0; p < _projectileLights.Length; p++)
        {
            bool active = sim.ProjectileActive[p];
            _projectileLights[p].Visible = active;
            if (active)
                _projectileLights[p].Position = toWorld(sim.ProjectilePos[p]);
        }
    }

    public string Describe() => Supported
        ? $"F5 ombres de contact : {(Ssao ? "oui" : "non")} · F6 brume : {(Fog ? "oui" : "non")} · F7 lumières : {(Lights ? "oui" : "non")}"
        : "F5 à F7 : effets de Forward+, absents en Compatibility";
}
