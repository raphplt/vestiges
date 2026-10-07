shader_type canvas_item;

// === Outline (sel-out coloré, jamais noir pur) ===
uniform vec4 outline_color : source_color = vec4(0.12, 0.22, 0.1, 1.0);
uniform float outline_width : hint_range(0.0, 3.0) = 1.0;
uniform bool outline_enabled = false;

// === Hit flash ===
uniform float flash_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// === Dissolve ===
uniform float dissolve_amount : hint_range(0.0, 1.0) = 0.0;
uniform float edge_width : hint_range(0.0, 0.1) = 0.04;
uniform vec4 edge_color : source_color = vec4(0.176, 0.106, 0.239, 1.0);
uniform vec4 edge_highlight : source_color = vec4(0.353, 0.227, 0.478, 1.0);
uniform float noise_scale : hint_range(1.0, 40.0) = 12.0;
uniform float vertical_bias : hint_range(0.0, 1.0) = 0.35;
// Sens du dernier coup, à l'écran (plan 02 J2) : le côté frappé se défait d'abord. Nul : de haut en bas.
uniform vec2 dissolve_direction = vec2(0.0);

// === Aberration tint ===
uniform float aberration_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 aberration_color : source_color = vec4(0.4, 0.15, 0.5, 1.0);

// === Statuts (plan 27 V1a, Combat/EnemyStatusVisual) : écrits au changement d'état seulement ===
// Teinte de priorité, sa force dans l'alpha.
uniform vec4 status_tint : source_color = vec4(0.0);
// Givre (figé) : bords hauts du sprite, sur frost_depth pixels, tramés en damier passé le premier.
uniform vec4 frost_color : source_color = vec4(1.0);
uniform int frost_depth = 0;
// Fêlures (Fragile) : traits diagonaux courts, un par cellule de crack_spacing pixels.
uniform vec4 crack_color : source_color = vec4(1.0);
uniform int crack_spacing = 0;
// Bord chaud (brûlure) : bords bas du sprite, sur heat_depth pixels, en damier.
uniform vec4 heat_color : source_color = vec4(1.0);
uniform int heat_depth = 0;

// Yeux des créatures (#7FFF00, charte) : jamais recouverts par une marque.
const vec3 EYE_COLOR = vec3(0.498, 1.0, 0.0);
// Plafond des boucles de bord (StatusVisualConfig.MaxEdgeDepthPx) : le compilateur les déroule, une borne haute
// alourdirait le shader de toutes les créatures, même sans statut.
const int MAX_EDGE_DEPTH = 4;

// Modulate et SelfModulate seuls. En fragment, COLOR vaut déjà texture × Modulate : le multiplier par la texture
// l'élèverait au carré (sprites assombris), l'ignorer perd le flash des coups, les fondus et les teintes d'annonce.
varying vec4 modulate;

// Hash noise pour la dissolution
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
		mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x),
		f.y
	);
}

void vertex() {
	modulate = COLOR;
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec4 final_color = tex;
	bool done = false;

	// --- Outline pass : si pixel transparent, vérifier les voisins ---
	if (outline_enabled && outline_width > 0.0 && tex.a < 0.1) {
		vec2 size = TEXTURE_PIXEL_SIZE * outline_width;
		float a = texture(TEXTURE, UV + vec2(size.x, 0)).a;
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, 0)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, -size.y)).a);

		if (a > 0.1) {
			final_color = vec4(outline_color.rgb, outline_color.a * a);
		} else {
			final_color = vec4(0.0);
		}
		done = true;
	}

	// --- Dissolve pass ---
	if (!done && dissolve_amount > 0.001) {
		float n = noise(UV * noise_scale);
		float sweep = 1.0 - UV.y;
		if (length(dissolve_direction) > 0.01) {
			sweep = dot(UV - vec2(0.5), normalize(dissolve_direction)) + 0.5;
		}
		float vertical = mix(n, sweep, vertical_bias);
		float diff = vertical - dissolve_amount;

		if (diff < 0.0) {
			final_color = vec4(tex.rgb, 0.0);
			done = true;
		} else if (diff < edge_width) {
			float edge_t = 1.0 - (diff / edge_width);
			vec4 edge = mix(edge_color, edge_highlight, edge_t * edge_t);
			final_color = vec4(mix(tex.rgb, edge.rgb, edge_t * 0.8), tex.a);
			done = true;
		}
	}

	// --- Normal rendering (aberration + flash) ---
	if (!done) {
		vec3 result = tex.rgb;
		if (aberration_amount > 0.0) {
			result = mix(result, aberration_color.rgb, aberration_amount * 0.3);
		}
		// Uniformes seuls dans la garde : sans statut, une créature ne paie pas le bloc.
		bool has_status = status_tint.a > 0.0 || frost_depth > 0 || crack_spacing > 0 || heat_depth > 0;
		if (has_status && tex.a > 0.1 && distance(tex.rgb, EYE_COLOR) > 0.2) {
			vec2 px = floor(UV / TEXTURE_PIXEL_SIZE);
			bool checker = mod(px.x + px.y, 2.0) < 1.0;
			result = mix(result, status_tint.rgb, status_tint.a);

			if (crack_spacing > 0) {
				float spacing = float(crack_spacing);
				vec2 cell = floor(px / spacing);
				vec2 local = px - cell * spacing;
				float h = hash(cell + vec2(17.0, 3.0));
				// Un trait sur deux cellules environ, en diagonale montante ou descendante, coupé avant le bord.
				float along = h < 0.5 ? local.x - local.y : local.x + local.y - (spacing - 1.0);
				if (h > 0.2 && h < 0.8 && abs(along) < 0.5 && local.y < spacing - 1.0) {
					result = mix(result, crack_color.rgb, 0.85);
				}
			}

			// Distance au bord haut (givre) et au bord bas (chaleur), en pixels du sprite.
			if (frost_depth > 0) {
				for (int i = 1; i <= MAX_EDGE_DEPTH; i++) {
					if (i > frost_depth) {
						break;
					}
					if (texture(TEXTURE, UV - vec2(0.0, TEXTURE_PIXEL_SIZE.y * float(i))).a < 0.1) {
						if (i == 1 || checker) {
							result = mix(result, frost_color.rgb, i == 1 ? 0.9 : 0.6);
						}
						break;
					}
				}
			}
			if (heat_depth > 0) {
				for (int i = 1; i <= MAX_EDGE_DEPTH; i++) {
					if (i > heat_depth) {
						break;
					}
					if (texture(TEXTURE, UV + vec2(0.0, TEXTURE_PIXEL_SIZE.y * float(i))).a < 0.1) {
						if (checker) {
							result = mix(result, heat_color.rgb, 0.75);
						}
						break;
					}
				}
			}
		}
		result = mix(result, flash_color.rgb, flash_amount);
		final_color = vec4(result, tex.a);
	}

	COLOR = final_color * modulate;
}
