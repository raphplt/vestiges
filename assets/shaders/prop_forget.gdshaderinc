// Décors dans les zones oubliées (plan 16 O2) : délavés, puis leurs bords s'effritent, puis il n'en reste que des
// fragments au Néant. Un seul matériau partagé par tous les décors ; la mémoire est lue au point au sol du décor.

#include "res://assets/shaders/erasure_memory.gdshaderinc"

varying float prop_memory;

#ifdef PROP_TRAMPLE
// Herbes qui plient (plan 10 lot E) : les petits décors traversables s'écartent du joueur. Le haut du sprite se cisaille
// à l'opposé de lui, la base reste plantée. Position du joueur en uniforme global : aucune boucle sur les décors.
global uniform vec4 trample_origin;
// Huit créatures les plus proches du joueur, deux par vecteur (xy, zw) ; loin = 1e6.
global uniform vec4 trample_creatures_0;
global uniform vec4 trample_creatures_1;
global uniform vec4 trample_creatures_2;
global uniform vec4 trample_creatures_3;
const float TRAMPLE_RADIUS = 30.0;
const float TRAMPLE_BEND = 5.0;
const float TRAMPLE_SINK = 2.0;
const float TRAMPLE_HEIGHT = 28.0;
#endif

void vertex() {
	vec2 ground = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy;
	prop_memory = memory_at(ground);
#ifdef PROP_TRAMPLE
	// Le passage le plus proche (joueur ou créature) couche la touffe.
	vec2 away = ground - trample_origin.xy;
	vec2 origins[8] = vec2[8](trample_creatures_0.xy, trample_creatures_0.zw, trample_creatures_1.xy, trample_creatures_1.zw,
		trample_creatures_2.xy, trample_creatures_2.zw, trample_creatures_3.xy, trample_creatures_3.zw);
	// Distance au sol : un rang vaut une demi-colonne.
	float distance_on_ground = length(vec2(away.x, away.y * 2.0));
	for (int i = 0; i < 8; i++) {
		vec2 creature_away = ground - origins[i];
		float creature_distance = length(vec2(creature_away.x, creature_away.y * 2.0));
		if (creature_distance < distance_on_ground) {
			distance_on_ground = creature_distance;
			away = creature_away;
		}
	}
	float push = 1.0 - smoothstep(TRAMPLE_RADIUS * 0.35, TRAMPLE_RADIUS, distance_on_ground);
	float side = away.x >= 0.0 ? 1.0 : -1.0;
	float lift = clamp(-VERTEX.y / TRAMPLE_HEIGHT, 0.0, 1.0);
	VERTEX += vec2(side * TRAMPLE_BEND, TRAMPLE_SINK) * push * lift;
#endif
}

void fragment() {
	if (prop_memory < 0.8) {
		ivec2 size = textureSize(TEXTURE, 0);
		ivec2 texel = ivec2(floor(UV * vec2(size)));
		ivec2 hi = size - 1;
		float edge = 0.0;
		edge = max(edge, step(texelFetch(TEXTURE, clamp(texel + ivec2(1, 0), ivec2(0), hi), 0).a, 0.01));
		edge = max(edge, step(texelFetch(TEXTURE, clamp(texel - ivec2(1, 0), ivec2(0), hi), 0).a, 0.01));
		edge = max(edge, step(texelFetch(TEXTURE, clamp(texel + ivec2(0, 1), ivec2(0), hi), 0).a, 0.01));
		edge = max(edge, step(texelFetch(TEXTURE, clamp(texel - ivec2(0, 1), ivec2(0), hi), 0).a, 0.01));
		COLOR = erasure_prop(COLOR, prop_memory, vec2(texel), edge);
	}
}
