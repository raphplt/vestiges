shader_type canvas_item;

// Voile d'écran de l'oubli (plan 16 O4) : les bords se couvrent d'une trame blanc-bleuté, de plus en plus dense
// de l'Effilochée au Néant. Le tramage est l'effacement (charte §5) : pas de fondu flou.

uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform vec4 veil_color : source_color = vec4(0.961, 0.941, 0.922, 1.0);
// Taille d'un point de trame, en pixels d'écran.
uniform float dot_px = 3.0;

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void fragment() {
	vec2 centered = (SCREEN_UV - 0.5) * vec2(1.0, 0.8);
	float edge = smoothstep(0.32, 0.62, length(centered));
	ivec2 cell = ivec2(floor(FRAGCOORD.xy / dot_px));
	float breathe = 0.9 + 0.1 * sin(TIME * 1.7);
	float amount = edge * intensity * breathe;
	COLOR = amount > bayer4(cell) ? vec4(veil_color.rgb, 0.8) : vec4(0.0);
}
