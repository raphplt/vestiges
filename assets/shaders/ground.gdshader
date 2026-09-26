shader_type canvas_item;

// Sol de la run (couches Ground et RoadOverlay).
// 1. Jonctions : près d'une frontière de biomes, une partie des pixels prend la matière du biome voisin.
//    Tramage net (Bayer en pixels monde), lisière perturbée par un bruit lent ; le gameplay ne change pas.
// 2. Oubli : la mémoire des zones (ErasureManager) délave le sol, le blanchit par tramage, le fissure
//    de lumière, jusqu'au Néant blanc animé. Phases de la Stratégie V2 §8.

// R : identifiant de mélange (biome × 8, plus la matière si le biome fond ses matières ; 255 = eau, bord dissous) ;
// G + 256 × B : tuile dans l'atlas ;
// A : 1 si un autre biome est à moins de deux cases (seules ces cellules cherchent la frontière).
uniform sampler2D cell_map : filter_nearest, repeat_disable;
uniform sampler2D tile_atlas : filter_nearest, repeat_disable;
uniform bool blend_enabled = true;
uniform int map_radius = 200;
uniform int atlas_columns = 16;
// Portée du mélange de part et d'autre de la frontière, en pixels au sol.
uniform float band_px = 34.0;
// Amplitude du bruit qui fait onduler la lisière (0 = bande régulière).
uniform float edge_noise = 0.35;
uniform float noise_scale = 0.045;

// Mémoire des zones autour du joueur (R = 0..1, une texel par zone), écrite par ErasureManager.
global uniform sampler2D erasure_memory;
// xy : coin monde de la fenêtre ; zw : taille monde de la fenêtre.
global uniform vec4 erasure_window;
// Mémoire hors de la fenêtre suivie.
global uniform float erasure_far_memory;

const vec2 TILE = vec2(64.0, 32.0);
const int NONE = 255;
const vec3 ERASURE_WHITE = vec3(0.961, 0.941, 0.922);   // Blanc effacement #F5F0EB
const vec3 ERASURE_PALE = vec3(0.847, 0.878, 0.918);    // blanc-bleuté
const vec3 CRACK_LIGHT = vec3(0.749, 0.910, 1.0);       // lueur des fissures

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
	ivec2 texel = c + ivec2(map_radius);
	int size = map_radius * 2 + 1;
	if (texel.x < 0 || texel.y < 0 || texel.x >= size || texel.y >= size)
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

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

vec2 hash2(vec2 p) {
	return vec2(hash(p), hash(p + vec2(17.1, 31.7)));
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

// Distance au bord de cellule de Voronoï (F2 − F1) : faible sur les lignes de fissure.
float crack_distance(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float f1 = 8.0;
	float f2 = 8.0;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 g = vec2(float(x), float(y));
			vec2 o = hash2(i + g);
			float d = length(g + o - f);
			if (d < f1) {
				f2 = f1;
				f1 = d;
			} else if (d < f2) {
				f2 = d;
			}
		}
	}
	return f2 - f1;
}

// Filtrage bilinéaire à la main : une zone fait 128 px, la transition entre zones reste douce.
float memory_at(vec2 p) {
	vec2 size = vec2(textureSize(erasure_memory, 0));
	if (size.x < 2.0 || erasure_window.z <= 0.0)
		return erasure_far_memory;
	vec2 uv = (p - erasure_window.xy) / erasure_window.zw;
	if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0)
		return erasure_far_memory;
	vec2 t = uv * size - 0.5;
	ivec2 i = ivec2(floor(t));
	vec2 f = fract(t);
	ivec2 hi = ivec2(size) - 1;
	float a = texelFetch(erasure_memory, clamp(i, ivec2(0), hi), 0).r;
	float b = texelFetch(erasure_memory, clamp(i + ivec2(1, 0), ivec2(0), hi), 0).r;
	float c = texelFetch(erasure_memory, clamp(i + ivec2(0, 1), ivec2(0), hi), 0).r;
	float d = texelFetch(erasure_memory, clamp(i + ivec2(1, 1), ivec2(0), hi), 0).r;
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	vertex_color = COLOR;
	vertex_memory = memory_at(world_pos);
}

vec3 forget(vec3 color, vec2 p, float memory) {
	ivec2 pixel = ivec2(floor(p));
	float threshold = bayer4(pixel);
	// Repère au sol : la vue 2:1 écrase la profondeur de moitié à l'écran.
	vec2 ground = vec2(p.x, p.y * 2.0);
	float wander = value_noise(ground * 0.02);

	// Fragile → Effacée : les couleurs se délavent vers un gris froid et pâle.
	float fade = smoothstep(0.8, 0.2, memory);
	float luminance = dot(color, vec3(0.299, 0.587, 0.114));
	vec3 washed = mix(vec3(luminance), ERASURE_PALE, 0.35);
	color = mix(color, washed, fade * 0.85);

	// Effilochée → Effacée : des pixels passent au blanc, en plaques qui ondulent.
	float bleach = smoothstep(0.55, 0.05, memory) * (0.55 + 0.45 * wander);
	if (bleach > threshold)
		color = hash(vec2(pixel)) > 0.5 ? ERASURE_WHITE : ERASURE_PALE;

	// Effilochée → Effacée : le sol se fissure de lumière, les fissures s'élargissent et respirent.
	float cracking = smoothstep(0.5, 0.12, memory);
	// Seule une partie du réseau apparaît, par veines qui gagnent du terrain quand la mémoire baisse.
	float veins = value_noise(ground / 150.0);
	if (cracking > 0.0 && veins > mix(0.72, 0.4, cracking)) {
		float width = mix(0.012, 0.045, cracking);
		float crack = crack_distance(ground / 80.0);
		if (crack < width) {
			float pulse = 0.75 + 0.25 * sin(TIME * 2.2 + wander * 6.2831);
			color = mix(color, CRACK_LIGHT, pulse * min(1.0, cracking * 1.6));
		}
	}

	// Néant : le sol disparaît dans un blanc qui grésille.
	float nothing = smoothstep(0.06, 0.0, memory);
	if (nothing > 0.0) {
		float flicker = hash(vec2(pixel) + floor(TIME * 6.0));
		color = mix(color, flicker > 0.5 ? ERASURE_WHITE : ERASURE_PALE, nothing);
	}
	return color;
}

void fragment() {
	vec2 p = floor(world_pos) + 0.5;

	if (blend_enabled) {
		ivec2 cell = cell_of(p);
		vec4 own_data = cell_data(cell);
		int own = biome_of(own_data);
		if (own != NONE && own_data.a > 0.5) {
			float nearest = 1e9;
			ivec2 other_cell = ivec2(0);
			// Anneau extérieur d'abord : loin de toute frontière (cas courant), 8 lectures suffisent.
			for (int ring = 3; ring >= 1; ring--) {
				float radius = band_px * float(ring) / 3.0;
				bool found = false;
				for (int i = 0; i < 8; i++) {
					float angle = float(i) * 0.7853982;
					ivec2 c = cell_of(p + vec2(cos(angle), sin(angle) * 0.5) * radius);
					int b = biome_of(cell_data(c));
					if (b != NONE && b != own) {
						nearest = radius;
						other_cell = c;
						found = true;
						break;
					}
				}
				if (!found)
					break;
			}
			if (nearest < 1e8) {
				float closeness = 1.0 - nearest / (band_px + band_px / 3.0);
				float wander = (value_noise(p * noise_scale) - 0.5) * 2.0 * edge_noise;
				float chance = 0.5 * closeness + wander * closeness;
				// Seuil par amas de 2×2 pixels : touffes et gravillons plutôt qu'une trame régulière.
				if (chance > hash(floor(p / 2.0) + 0.37)) {
					vec4 other = tile_texel(cell_data(other_cell), p - cell_center(cell));
					if (other.a > 0.5)
						COLOR = vec4(other.rgb, 1.0) * vertex_color;
				}
			}
		}
	}

	float memory = vertex_memory;
	if (memory < 0.8)
		COLOR.rgb = forget(COLOR.rgb, p, memory);
}
