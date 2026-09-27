shader_type canvas_item;

// Présage d'une Résurgence (V2 §8, plan 03 lot C) : les bords de l'écran perdent leurs couleurs à mesure que la crise
// approche. Tramé comme l'effacement (charte §5) : un pixel est désaturé ou ne l'est pas, jamais de fondu flou.

uniform sampler2D screen_tex : hint_screen_texture, filter_nearest;
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
// Assombrissement des pixels désaturés : la lumière baisse avec la couleur.
uniform float dim : hint_range(0.0, 1.0) = 0.3;
uniform float dot_px = 3.0;

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void fragment() {
	vec3 color = texture(screen_tex, SCREEN_UV).rgb;
	vec2 centered = (SCREEN_UV - 0.5) * vec2(1.0, 0.8);
	float edge = smoothstep(0.2, 0.6, length(centered));
	ivec2 cell = ivec2(floor(FRAGCOORD.xy / dot_px));
	float amount = edge * intensity;
	float gray = dot(color, vec3(0.299, 0.587, 0.114));
	vec3 drained = vec3(gray) * (1.0 - dim);
	COLOR = vec4(amount > bayer4(cell) ? drained : color, 1.0);
}
