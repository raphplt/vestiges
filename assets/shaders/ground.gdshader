shader_type canvas_item;

// Sol de la run (couches Ground et RoadOverlay).
// 1. Jonctions : près d'une frontière de biomes, une partie des pixels prend la matière du biome voisin.
//    Plaques organiques au pixel double, lisière perturbée par un bruit lent ; le gameplay ne change pas.
// 2. Oubli : la mémoire des zones (ErasureManager) délave le sol, le blanchit par tramage, le fissure
//    de lumière, jusqu'au Néant blanc animé. Phases de la Stratégie V2 §8.

// R : identifiant de mélange (biome × 8, plus la matière si le biome fond ses matières ; 255 = eau, bord dissous) ;
// G + 256 × B : tuile dans l'atlas ;
// A : 1 si un autre biome est à moins de deux cases (seules ces cellules cherchent la frontière).
uniform sampler2D cell_map : filter_nearest, repeat_disable;
uniform sampler2D tile_atlas : filter_nearest, repeat_disable;
uniform bool blend_enabled = true;
uniform ivec2 map_radius = ivec2(200, 200);
uniform int atlas_columns = 16;
// Portée du mélange de part et d'autre de la frontière, en pixels au sol.
uniform float band_px = 34.0;
// Amplitude du bruit qui fait onduler la lisière (0 = bande régulière).
uniform float edge_noise = 0.35;
uniform float noise_scale = 0.045;
// Part du grain fin qui ronge le bord des plaques (0 = bord lisse).
uniform float grain = 0.3;

#include "res://assets/shaders/erasure_memory.gdshaderinc"
#include "res://assets/shaders/ground_forget.gdshaderinc"
#include "res://assets/shaders/world_grade.gdshaderinc"

const vec2 TILE = vec2(64.0, 32.0);
const int NONE = 255;
// Anneaux de sondage de la frontière : assez pour que la lisière ne marche pas par paliers (plan 37 M2).
const int RINGS = 6;

varying vec2 world_pos;
varying vec4 vertex_color;
// Mémoire lue aux sommets : une zone fait 128 px, l'interpolation sur une tuile de 64×32 suffit.
varying float vertex_memory;

// Grille isométrique « stacked » : centre de (x, y) = (64x + 32·(y impair) + 32, 16y + 16).
vec2 cell_center(ivec2 c) {
	float offset = (c.y & 1) != 0 ? 32.0 : 0.0;
	return vec2(float(c.x) * 64.0 + offset + 32.0, float(c.y) * 16.0 + 16.0);
}

// Le losange le plus proche est dans l'un des deux rangs qui encadrent le point.
ivec2 cell_of(vec2 p) {
	int row = int(floor((p.y - 16.0) / 16.0));
	ivec2 best = ivec2(0);
	float best_distance = 1e9;
	for (int k = 0; k < 2; k++) {
		int r = row + k;
		float offset = (r & 1) != 0 ? 32.0 : 0.0;
		ivec2 c = ivec2(int(round((p.x - 32.0 - offset) / 64.0)), r);
		vec2 d = abs(p - cell_center(c));
		float distance = d.x / 32.0 + d.y / 16.0;
		if (distance < best_distance) {
			best_distance = distance;
			best = c;
		}
	}
	return best;
}

vec4 cell_data(ivec2 c) {
	ivec2 texel = c + map_radius;
	ivec2 size = map_radius * 2 + ivec2(1);
	if (texel.x < 0 || texel.y < 0 || texel.x >= size.x || texel.y >= size.y)
		return vec4(1.0, 0.0, 0.0, 0.0);
	return texelFetch(cell_map, texel, 0);
}

int biome_of(vec4 data) {
	return int(round(data.r * 255.0));
}

vec4 tile_texel(vec4 data, vec2 local) {
	int index = int(round(data.g * 255.0)) + int(round(data.b * 255.0)) * 256;
	ivec2 origin = ivec2(index % atlas_columns, index / atlas_columns) * ivec2(TILE);
	ivec2 texel = clamp(ivec2(floor(local + TILE * 0.5)), ivec2(0), ivec2(TILE) - 1);
	return texelFetch(tile_atlas, origin + texel, 0);
}

// Texel de la tuile d'une case ; sur l'arête vide d'un losange, repris un cran vers l'intérieur.
vec4 cell_texel(vec4 data, vec2 local) {
	vec4 texel = tile_texel(data, local);
	if (texel.a < 0.5)
		texel = tile_texel(data, local - sign(local) * vec2(2.0, 1.0));
	return texel;
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	vertex_color = COLOR;
	vertex_memory = memory_at(world_pos);
}

void fragment() {
	vec2 p = floor(world_pos) + 0.5;

	if (blend_enabled) {
		ivec2 cell = cell_of(p);
		vec4 own_data = cell_data(cell);
		int own = biome_of(own_data);
		vec2 local = p - cell_center(cell);
		// Sur l'arête d'un losange, la tuile laisse un pixel sur deux transparent et la tuile voisine, d'une autre
		// matière, se voyait au travers : une ligne pointillée le long de chaque arête. Le pixel reprend la matière de
		// la case qui le contient.
		if (own != NONE) {
			vec4 base = cell_texel(own_data, local);
			if (base.a > 0.5)
				COLOR = vec4(base.rgb, 1.0) * vertex_color;
		}
		if (own != NONE && own_data.a > 0.5) {
			float nearest = 1e9;
			ivec2 other_cell = ivec2(0);
			int other_biome = own;
			// Anneau extérieur d'abord : loin de toute frontière (cas courant), 8 lectures suffisent.
			// Point sondé déformé par un bruit : la distance à la frontière ne suit plus les losanges de la grille.
			vec2 warped = p + (vec2(value_noise(p * 0.07 + 5.1), value_noise(p * 0.07 + 41.7)) - 0.5) * 20.0;
			for (int ring = RINGS; ring >= 1; ring--) {
				float radius = band_px * float(ring) / float(RINGS);
				bool found = false;
				for (int i = 0; i < 8; i++) {
					float angle = float(i) * 0.7853982;
					ivec2 c = cell_of(warped + vec2(cos(angle), sin(angle) * 0.5) * radius);
					int b = biome_of(cell_data(c));
					if (b != NONE && b != own) {
						nearest = radius;
						other_cell = c;
						other_biome = b;
						found = true;
						break;
					}
				}
				if (!found)
					break;
			}
			if (nearest < 1e8) {
				// Lisière organique (plan 37 M2) : un bruit déplace la frontière vers l'un ou l'autre biome, d'une
				// bande au plus. Le signe dépend de l'ordre des deux biomes, si bien que les deux côtés voient la même
				// frontière déplacée : de grandes plaques avancent sur le voisin, rongées d'un grain plus fin, au pixel
				// double. Un seuil tiré pixel par pixel donnait des pixels épars, un seuil commun aux deux côtés gardait
				// le bord droit des losanges en échangeant seulement les matières.
				vec2 grid = floor(p / 2.0) * 2.0;
				float shape = mix(value_noise(grid * 0.06 + 17.3), value_noise(grid * 0.22 + 3.1), grain);
				shape += (value_noise(p * noise_scale) - 0.5) * edge_noise;
				float side = own < other_biome ? 1.0 : -1.0;
				float advance = side * (shape - 0.5) * 2.0 * band_px;
				if (advance > nearest - band_px / float(RINGS) * 0.5) {
					vec4 other = cell_texel(cell_data(other_cell), local);
					if (other.a > 0.5)
						COLOR = vec4(other.rgb, 1.0) * vertex_color;
				}
			}
		}
	}

	// Étalonné et ombragé avant l'oubli : le blanc de l'Effacement reste le même dans tous les biomes.
	COLOR.rgb = world_grime_at(cloud_shade(world_grade(COLOR.rgb, 1.0), p, 1.0), p);
	float memory = vertex_memory;
	if (memory < 0.8)
		COLOR.rgb = forget(COLOR.rgb, p, memory);
}
