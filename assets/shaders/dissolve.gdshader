shader_type canvas_item;

uniform float dissolve_amount : hint_range(0.0, 1.0) = 0.0;
uniform float edge_width : hint_range(0.0, 0.1) = 0.04;
uniform vec4 edge_color : source_color = vec4(0.176, 0.106, 0.239, 1.0); // #2D1B3D noir iridescent
uniform vec4 edge_highlight : source_color = vec4(0.353, 0.227, 0.478, 1.0); // #5A3A7A violet iridescent
uniform float noise_scale : hint_range(1.0, 40.0) = 12.0;
uniform float vertical_bias : hint_range(0.0, 1.0) = 0.35;

// Simple hash-based noise (pas besoin de texture externe)
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

	// Bruit + biais vertical (dissolution du bas vers le haut)
	float n = noise(UV * noise_scale);
	float vertical = mix(n, 1.0 - UV.y, vertical_bias);
	float threshold = vertical;

	// Dissolution
	float diff = threshold - dissolve_amount;
	if (diff < 0.0) {
		COLOR.a = 0.0;
	} else if (diff < edge_width) {
		// Bordure iridescente
		float edge_t = 1.0 - (diff / edge_width);
		vec4 edge = mix(edge_color, edge_highlight, edge_t * edge_t);
		COLOR = vec4(mix(tex.rgb, edge.rgb, edge_t * 0.8), tex.a);
	} else {
		COLOR = tex;
	}
}
