shader_type canvas_item;

// Seconde vague de l'Indicible (plan 07 B5d), dessinée au pixel. Deux usages, selon « breach_light » :
// - le front (0) : une crête d'écume déchirée qui roule, des embruns devant, une masse d'eau sombre qui traîne derrière
//   et s'efface en trame ; les brèches sont des trous dans le mur, bordés d'écume qui s'enroule ;
// - un couloir de brèche (1) : une lumière chaude, tramée, qui vacille comme une lanterne, plus vive au milieu.
// Coordonnées : « along » le long du sens de la vague (0 au front), « across » en travers (0 au point d'ancrage).
// Les brèches sont données en travers ; le couloir n'éclaire que leur largeur. Le fondu passe par « fade » (COLOR est
// réécrit, la Modulate du nœud n'y serait pas).

uniform float breach_light = 0.0;
uniform vec2 origin;
uniform vec2 direction = vec2(0.0, -1.0);
uniform vec4 breaches = vec4(0.0);
uniform int breach_count = 0;
uniform float breach_width = 80.0;
uniform float crest_width = 26.0;
uniform float trail_length = 170.0;
uniform float fade : hint_range(0.0, 1.0) = 1.0;
// Lueur propre (au-delà de 1) : la nuit du CanvasModulate l'assombrit ensuite, elle reste lisible.
uniform float glow = 1.0;
uniform vec4 foam_color : source_color = vec4(0.72, 0.82, 0.92, 1.0);
uniform vec4 water_color : source_color = vec4(0.06, 0.12, 0.24, 1.0);
uniform vec4 light_color : source_color = vec4(0.95, 0.85, 0.45, 1.0);

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

float breach_gap(float across) {
	float gap = 100000.0;
	for (int i = 0; i < 4; i++) {
		if (i < breach_count)
			gap = min(gap, abs(across - breaches[i]) - breach_width * 0.5);
	}
	return gap;
}

void fragment() {
	vec2 p = floor(world_pos);
	float dither = bayer4(ivec2(p));
	// Même travers que Vector2.Orthogonal() côté jeu : (y, −x).
	vec2 side = vec2(direction.y, -direction.x);
	float along = dot(p - origin, direction);
	float across = dot(p - origin, side);
	vec4 color = vec4(0.0);
	if (breach_light > 0.5) {
		float gap_in = breach_gap(across);
		float centre = 1.0 - clamp(abs(gap_in + breach_width * 0.5) / (breach_width * 0.5), 0.0, 1.0);
		float flicker = 0.75 + 0.15 * sin(TIME * 7.0) + 0.1 * sin(TIME * 13.0 + along * 0.02);
		float rays = 0.6 + 0.4 * value_noise(vec2(across * 0.12, along * 0.01 - TIME * 0.8));
		if (gap_in < 0.0 && flicker * rays * (0.35 + 0.65 * centre) > dither)
			color = vec4(light_color.rgb, 0.55);
	} else {
		float gap = breach_gap(across);
		float lead = 5.0 + value_noise(vec2(across * 0.06, TIME * 1.8)) * 12.0;
		float crest = crest_width + value_noise(vec2(across * 0.09, TIME * 0.9)) * 12.0;
		// Au bord d'une brèche, le mur s'enroule : l'écume y est plus large.
		float curl = clamp(1.0 - gap / 14.0, 0.0, 1.0);
		if (gap > 0.0) {
			if (along > lead) {
				float spray = hash21(vec2(floor(across / 2.0), floor((along - TIME * 160.0) / 2.0)));
				if (along < lead + 22.0 && spray > 0.9 + (along - lead) / 220.0)
					color = vec4(foam_color.rgb, 0.85);
			} else if (along > lead - crest - curl * 10.0) {
				float tumble = sin((along + TIME * 90.0) * 0.32 + value_noise(vec2(across * 0.1, along * 0.1)) * 4.0);
				vec3 foam = tumble > 0.2 ? foam_color.rgb : mix(foam_color.rgb, water_color.rgb, 0.45);
				color = vec4(foam, 0.92);
			} else if (along > -trail_length) {
				float behind = (lead - crest - along) / (trail_length + lead - crest);
				float streak = sin(across * 0.05 + value_noise(vec2(across * 0.04, along * 0.05)) * 3.0 + along * 0.01);
				vec3 water = streak > 0.92 ? mix(water_color.rgb, foam_color.rgb, 0.3) : water_color.rgb;
				if (1.0 - behind > dither)
					color = vec4(water, 0.8);
			}
		} else if (gap > -3.0 && along <= lead && along > lead - crest) {
			color = vec4(foam_color.rgb, 0.7);
		}
	}
	color.rgb *= glow;
	color.a *= fade;
	COLOR = color;
}
