shader_type canvas_item;

#include "res://assets/shaders/erasure_memory.gdshaderinc"

// === Organic Vegetation Sway Shader ===
// VESTIGES: Apporte de la vie à la flore tout en restant pixel-perfect.

uniform float speed = 1.0;
uniform float min_strength = 0.05;
uniform float max_strength = 0.01; // Force maximale appliquée au sommet
uniform float strength_scale = 100.0;
uniform float interval = 3.5;
uniform float detail = 1.0;
uniform float distortion : hint_range(0.0, 1.0) = 0.0;
uniform float height_offset = 0.0; // Décale le point de pivot (0 = haut, 1 = bas)

uniform vec2 pixel_size = vec2(1.0, 1.0); // Définir la taille du sprite ou 1.0/TEXTURE_PIXEL_SIZE

varying float prop_memory;

// Hash et Noise pour un vent organique
float get_wind(vec2 vertex, vec2 uv, float timer){
	vec2 pos = mix(vec2(1.0), vertex, distortion).xy;
	float time = timer * speed + pos.x * detail;
	float diff = pow(max_strength - min_strength, 2.0);
	float strength = clamp(min_strength + diff + sin(time / interval) * diff, min_strength, max_strength) * strength_scale;
	float wind = (sin(time) + cos(time * 1.2)) * strength;
	return wind; 
}

void vertex() {
	// UV.y inversé pour appliquer plus de force en haut
	float weight = clamp((1.0 - UV.y) - height_offset, 0.0, 1.0);
	
	float wind = get_wind(VERTEX, UV, TIME);
	
	// Appliquer le décalage, limité au sommet
	vec2 offset = vec2(wind * weight, 0.0);
	
	// Sub-pixel snapping pour le pixel art
	// On décale le Vertex, et ensuite on snap sur une grille théorique pour éviter l'effet jelly
	vec2 snapped_pos = floor((VERTEX + offset) / pixel_size) * pixel_size;
	
	VERTEX = snapped_pos;
	prop_memory = memory_at((MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy);
}

// Canopées dans les zones oubliées : même effritement que les décors (plan 16 O2).
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
