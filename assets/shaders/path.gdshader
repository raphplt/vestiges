shader_type canvas_item;

// Chemins de terre entre les régions (plan 10 T3), posés sur le sol (couche des routes).
// Maillage en ruban construit par PathMeshes : UV.x = distance le long du chemin, UV.y = travers (0 → 1).
// Couleur de sommet : rgb = ton moyen du chemin dans le biome traversé, a = part d'ornières
// (0 : piste pleine ; 1 : deux traces de roues, le sol reste visible au milieu).
// Bord tramé par amas de 2×2 pixels, lisière qui ondule ; l'oubli s'y applique comme au sol.

#include "res://assets/shaders/erasure_memory.gdshaderinc"
#include "res://assets/shaders/ground_forget.gdshaderinc"

varying vec2 world_pos;
varying vec4 style;
varying float vertex_memory;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	style = COLOR;
	vertex_memory = memory_at(world_pos);
}

void fragment() {
	vec2 p = floor(world_pos) + 0.5;
	// Repère au sol : la vue 2:1 écrase la profondeur de moitié à l'écran.
	vec2 ground = vec2(p.x, p.y * 2.0);
	float across = abs(UV.y * 2.0 - 1.0);
	float clump = hash(floor(p / 2.0) + 0.71);

	float edge = 0.9 + (value_noise(ground * 0.05) - 0.5) * 0.24 - clump * 0.16;
	float median = style.a * (0.3 + (value_noise(ground * 0.08 + 7.3) - 0.5) * 0.16 - clump * 0.08);
	if (across > edge || across < median)
		discard;

	// Quatre tons : plaques lentes, grain par pixel, gravillons clairs épars.
	float plates = value_noise(ground / 22.0) + (hash(p) - 0.5) * 0.22;
	vec3 base = style.rgb;
	vec3 color = plates < 0.3 ? base * 0.8 : (plates < 0.66 ? base : base * 1.12);
	// Terre tassée au bord des ornières et sur la lisière : un ton plus sombre.
	if (across - median < 0.1 && style.a > 0.05)
		color = base * 0.72;
	else if (edge - across < 0.07)
		color = base * 0.84;
	if (hash(p * 1.37 + 2.9) < 0.012)
		color = min(base * 1.3, vec3(1.0));

	float memory = vertex_memory;
	if (memory < 0.8)
		color = forget(color, p, memory);
	COLOR = vec4(color, 1.0);
}
