shader_type canvas_item;

// Marée de l'Indicible (plan 07 B5c) : une seule surface d'eau dessinée au pixel, posée au sol. Le front lèche la côte
// (il ondule le long du bord et respire), frangé d'écume ; l'eau peu profonde est claire et ridée de vaguelettes qui
// viennent vers la côte, l'eau profonde est sombre, piquée de reflets. Avant chaque montée, la bande qui va être noyée
// luit d'eau qui affleure. Autour du joueur dans l'eau, des remous. Transitions tramées (charte §5), pas de fondu flou.
// Le fondu de la surface passe par « fade » : COLOR est réécrit, la Modulate du nœud n'y serait pas.

uniform vec2 anchor;
uniform vec2 direction = vec2(0.0, -1.0);
uniform float front = 0.0;
uniform float shallow_width = 60.0;
uniform float band_step = 45.0;
uniform float warning : hint_range(0.0, 1.0) = 0.0;
uniform float fade : hint_range(0.0, 1.0) = 1.0;
uniform vec2 wader;
uniform float wader_wet : hint_range(0.0, 1.0) = 0.0;
uniform vec4 shallow_color : source_color = vec4(0.16, 0.28, 0.42, 1.0);
uniform vec4 deep_color : source_color = vec4(0.06, 0.12, 0.24, 1.0);
uniform vec4 foam_color : source_color = vec4(0.72, 0.82, 0.92, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.72;

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float hash21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x), mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x), f.y);
}

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void fragment() {
	vec2 p = floor(world_pos);
	ivec2 cell = ivec2(p);
	float dither = bayer4(cell);
	vec2 side = vec2(-direction.y, direction.x);
	float along = dot(p - anchor, direction);
	float across = dot(p - anchor, side);
	// Le bord lèche la côte : il ondule le long du front et respire lentement.
	float lap = (value_noise(vec2(across * 0.045, TIME * 0.35)) - 0.5) * 16.0 + sin(TIME * 1.4 + across * 0.021) * 3.0;
	float depth = along - (front + lap);
	vec4 color = vec4(0.0);
	if (depth >= 0.0) {
		float deepness = clamp((depth - shallow_width * 0.4) / shallow_width, 0.0, 1.0);
		vec3 water = deepness > dither ? deep_color.rgb : shallow_color.rgb;
		// Vaguelettes parallèles au front, qui viennent vers la côte ; plus marquées en eau peu profonde.
		float warp = value_noise(vec2(across * 0.035, depth * 0.03 + TIME * 0.1)) * 5.0;
		float ripple = sin(depth * 0.19 + warp + TIME * 2.4);
		float crest = mix(0.86, 0.97, deepness);
		if (ripple > crest)
			water = mix(water, foam_color.rgb, mix(0.45, 0.18, deepness));
		// Reflets épars au large.
		float glint = value_noise(p * vec2(0.11, 0.22) + vec2(TIME * 0.3, TIME * -0.12));
		if (deepness > 0.5 && glint > 0.86)
			water = mix(water, foam_color.rgb, 0.3);
		// Écume du bord, déchirée.
		float foam_width = 3.0 + value_noise(vec2(across * 0.12, TIME * 0.8)) * 5.0;
		float alpha = opacity;
		if (depth < foam_width) {
			water = foam_color.rgb;
			alpha = depth / foam_width > dither * 0.6 ? 0.9 : opacity;
		}
		// Remous autour du joueur dans l'eau : des cercles au sol qui s'élargissent.
		vec2 offset = p - wader;
		float ring = length(vec2(offset.x, offset.y * 2.0));
		if (wader_wet > 0.0 && ring < 46.0 && ring > 8.0 && fract(ring * 0.09 - TIME * 1.3) < 0.12)
			water = mix(water, foam_color.rgb, 0.55 * (1.0 - ring / 46.0) * wader_wet);
		color = vec4(water, alpha);
	} else if (warning > 0.0 && depth > -band_step) {
		// L'eau affleure sur la bande qui va être noyée : un sol mouillé qui luit, tramé, plus dense près du bord.
		float share = 1.0 + depth / band_step;
		float sheen = value_noise(vec2(across * 0.08, along * 0.15 + TIME * 0.6));
		if (share * warning * (0.6 + 0.6 * sheen) > dither)
			color = vec4(mix(shallow_color.rgb, foam_color.rgb, 0.35), opacity * 0.55);
	}
	color.a *= fade;
	COLOR = color;
}
