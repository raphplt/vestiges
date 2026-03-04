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

// === Aberration tint ===
uniform float aberration_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 aberration_color : source_color = vec4(0.4, 0.15, 0.5, 1.0);

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

void fragment() {
	vec4 tex = texture(TEXTURE, UV);

	// --- Outline pass : si pixel transparent, vérifier les voisins ---
	if (outline_enabled && outline_width > 0.0 && tex.a < 0.1) {
		vec2 size = TEXTURE_PIXEL_SIZE * outline_width;
		float a = 0.0;
		// 8 voisins (cardinaux + diagonales)
		a = max(a, texture(TEXTURE, UV + vec2(size.x, 0)).a);
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, 0)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, -size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(size.x, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(size.x, -size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, -size.y)).a);

		if (a > 0.1) {
			COLOR = vec4(outline_color.rgb, outline_color.a * a);
		} else {
			COLOR = vec4(0.0);
		}
		return;
	}

	// --- Dissolve pass ---
	if (dissolve_amount > 0.001) {
		float n = noise(UV * noise_scale);
		float vertical = mix(n, 1.0 - UV.y, vertical_bias);
		float diff = vertical - dissolve_amount;

		if (diff < 0.0) {
			COLOR.a = 0.0;
			return;
		} else if (diff < edge_width) {
			float edge_t = 1.0 - (diff / edge_width);
			vec4 edge = mix(edge_color, edge_highlight, edge_t * edge_t);
			COLOR = vec4(mix(tex.rgb, edge.rgb, edge_t * 0.8), tex.a);
			return;
		}
	}

	// --- Aberration tint ---
	vec3 result = tex.rgb;
	if (aberration_amount > 0.0) {
		result = mix(result, aberration_color.rgb, aberration_amount * 0.3);
	}

	// --- Hit flash (appliqué en dernier, par-dessus tout) ---
	result = mix(result, flash_color.rgb, flash_amount);

	// Intègre SelfModulate / Modulate du node (COLOR vertex = modulate combiné)
	COLOR = vec4(result * COLOR.rgb, tex.a * COLOR.a);
}
