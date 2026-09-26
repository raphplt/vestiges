shader_type canvas_item;

// Décors dans les zones oubliées (plan 16 O2) : délavés, puis leurs bords s'effritent, puis il n'en reste que des
// fragments au Néant. Un seul matériau partagé par tous les décors ; la mémoire est lue au point au sol du décor.

#include "res://assets/shaders/erasure_memory.gdshaderinc"

varying float prop_memory;

void vertex() {
	prop_memory = memory_at((MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy);
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
