shader_type canvas_item;

// Brouillard épais (plan 39 A5) : la vue se referme autour du joueur. Net jusqu'à clear_px au sol, plein à full_px ;
// la lisière ondule et dérive avec un bruit lent, tramée au pixel du monde comme les ombres de nuages.

uniform vec4 fog_color : source_color = vec4(0.8, 0.82, 0.84, 1.0);
uniform float opacity = 0.0;
uniform float clear_px = 150.0;
uniform float full_px = 300.0;
// Joueur à l'écran (0–1) et taille de la vue en px monde : distances mesurées au sol, pas en pixels d'écran.
uniform vec2 center_uv = vec2(0.5);
uniform vec2 view_world = vec2(960.0, 540.0);
// Coin haut-gauche de la vue en px monde : bruit et trame accrochés au monde, pas à l'écran.
uniform vec2 view_origin = vec2(0.0);

float fog_hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float fog_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(fog_hash(i), fog_hash(i + vec2(1.0, 0.0)), f.x),
		mix(fog_hash(i + vec2(0.0, 1.0)), fog_hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

void fragment() {
	vec2 offset = (SCREEN_UV - center_uv) * view_world;
	// Repère au sol : la vue 2:1 écrase la profondeur de moitié.
	float ground = length(vec2(offset.x, offset.y * 2.0));
	vec2 world = floor(view_origin + SCREEN_UV * view_world);
	float drift = fog_noise(world * vec2(0.012, 0.024) + vec2(TIME * 0.08, TIME * 0.03));
	float edge = ground + (drift - 0.5) * (full_px - clear_px) * 0.8;
	float amount = smoothstep(clear_px, full_px, edge);
	vec2 cell = mod(world, 2.0);
	float dither = mod(cell.x * 2.0 + cell.y * 3.0, 4.0) * 0.25 + 0.125;
	amount = clamp(floor(amount * 4.0 + dither), 0.0, 4.0) / 4.0;
	COLOR = vec4(fog_color.rgb, amount * opacity);
}
