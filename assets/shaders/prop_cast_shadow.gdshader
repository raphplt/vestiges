shader_type canvas_item;

// Ombre portée d'un grand décor (plan 39 A3) : la texture du décor, retournée et cisaillée au sol par la
// transformation du nœud (PropShadow.CreateCast), remplie d'une couleur d'ombre unie. Un seul matériau partagé.
// L'ombre s'efface sous un nuage (lumière diffuse) et avec l'oubli de la zone, lus au pied du décor.

#include "res://assets/shaders/erasure_memory.gdshaderinc"
#include "res://assets/shaders/world_grade.gdshaderinc"

// rgb : couleur de l'ombre ; a : opacité du biome où se trouve le joueur (0 : aucune ombre).
global uniform vec4 world_cast_shadow;
// Taille des pixels de l'ombre, en px monde (Paramètres › Ombres) ; 0 : ombres désactivées.
global uniform float world_shadow_pixel;

varying float shadow_alpha;
varying vec2 shadow_world;

void vertex() {
	vec2 foot = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy;
	float sun = 1.0;
	if (world_clouds.w > 0.0)
		sun = 1.0 - cloud_shadow(foot) / world_clouds.w;
	shadow_alpha = world_cast_shadow.a * sun * smoothstep(0.3, 0.8, memory_at(foot));
	shadow_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

// Le sprite est cisaillé : lu tel quel, son bord suit les pixels de l'écran et tranche sur le pixel art. On lit donc la
// texture au centre de la case de la grille du monde (world_shadow_pixel) qui contient le fragment : l'ombre est faite
// de gros pixels alignés sur le monde. UV est affine en position monde ; le jacobien vient des dérivées écran.
void fragment() {
	float cell = max(world_shadow_pixel, 1.0);
	vec2 snapped = (floor(shadow_world / cell) + 0.5) * cell;
	mat2 uv_per_screen = mat2(dFdx(UV), dFdy(UV));
	mat2 world_per_screen = mat2(dFdx(shadow_world), dFdy(shadow_world));
	vec2 uv = UV + uv_per_screen * inverse(world_per_screen) * (snapped - shadow_world);
	float enabled = step(0.5, world_shadow_pixel);
	COLOR = vec4(world_cast_shadow.rgb, step(0.5, texture(TEXTURE, uv).a) * shadow_alpha * enabled);
}
