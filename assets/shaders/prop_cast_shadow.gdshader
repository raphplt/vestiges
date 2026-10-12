shader_type canvas_item;

// Ombre portée d'un grand décor (plan 39 A3) : la texture du décor, retournée et cisaillée au sol par la
// transformation du nœud (PropShadow.CreateCast), remplie d'une couleur d'ombre unie. Un seul matériau partagé.
// L'ombre s'efface sous un nuage (lumière diffuse) et avec l'oubli de la zone, lus au pied du décor.

#include "res://assets/shaders/erasure_memory.gdshaderinc"
#include "res://assets/shaders/world_grade.gdshaderinc"

// rgb : couleur de l'ombre ; a : opacité du biome où se trouve le joueur (0 : aucune ombre).
global uniform vec4 world_cast_shadow;

varying float shadow_alpha;

void vertex() {
	vec2 foot = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy;
	float sun = 1.0;
	if (world_clouds.w > 0.0)
		sun = 1.0 - cloud_shadow(foot) / world_clouds.w;
	shadow_alpha = world_cast_shadow.a * sun * smoothstep(0.3, 0.8, memory_at(foot));
}

void fragment() {
	COLOR = vec4(world_cast_shadow.rgb, step(0.5, texture(TEXTURE, UV).a) * shadow_alpha);
}
