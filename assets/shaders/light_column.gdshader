shader_type canvas_item;
render_mode unshaded;

// Colonne de lumière des coffres et des micro-événements (plan 17, lot 0A).
// Dessinée dans le repère des texels du quad : cœur clair, halo moyen puis sombre tramé, extinction tramée
// vers le haut, poussières qui montent par à-coups et tache au sol aplatie 2:1. Le temps avance par poses.

uniform vec2 size_px = vec2(24.0, 140.0);
uniform float core_width = 1.0;
uniform float ground_radius = 11.0;
uniform float seed = 0.0;
uniform vec4 c_light : source_color = vec4(1.0);
uniform vec4 c_mid : source_color = vec4(0.8, 0.8, 0.8, 1.0);
uniform vec4 c_dark : source_color = vec4(0.4, 0.4, 0.4, 1.0);

int bayer2(int x, int y) {
	return ((x ^ y) << 1) | y;
}

float bayer4(vec2 p) {
	ivec2 q = ivec2(mod(p, 4.0));
	int value = 4 * bayer2(q.x & 1, q.y & 1) + bayer2((q.x >> 1) & 1, (q.y >> 1) & 1);
	return (float(value) + 0.5) / 16.0;
}

float hash(float n) {
	return fract(sin(n * 12.9898 + seed * 78.233) * 43758.5453);
}

void fragment() {
	vec2 p = floor(UV * size_px);
	float half_w = floor(size_px.x * 0.5);
	float dx = abs(p.x - half_w + 0.5);
	float column_h = size_px.y - ground_radius;
	// 0 au pied de la colonne, 1 au sommet.
	float t = clamp((column_h - p.y) / column_h, 0.0, 1.0);
	float step_time = floor(TIME * 8.0) / 8.0;
	float pulse = 0.5 + 0.5 * sin(step_time * 2.4 + seed * 6.0);
	vec4 col = vec4(0.0);

	if (p.y < column_h) {
		// Pleine sur les deux tiers, puis tramée jusqu'au sommet : le haut dépasse du bord de l'écran.
		float keep = (1.0 - t * t) * (0.8 + 0.2 * pulse);
		if (dx <= core_width) {
			if (bayer4(p) < keep * 1.2)
				col = c_light;
		} else if (dx <= core_width + 3.0) {
			if (bayer4(p) < keep)
				col = c_mid;
		} else if (dx <= core_width + 6.0) {
			if (bayer4(p) < keep * 0.5)
				col = c_dark;
		}
	} else {
		// Tache au sol : ellipse aplatie, bord sombre, cœur tramé.
		vec2 q = vec2(p.x - half_w + 0.5, (p.y - column_h - ground_radius * 0.5 + 0.5) * 2.0);
		float d = length(q) / ground_radius;
		if (d < 1.0) {
			if (d > 0.82)
				col = c_dark;
			else if (bayer4(p) < (1.0 - d) * (0.55 + 0.25 * pulse))
				col = c_mid;
		}
	}

	// Poussières de mémoire qui montent le long de la colonne.
	for (int i = 0; i < 5; i++) {
		float fi = float(i);
		float speed = 10.0 + 8.0 * hash(fi + 1.0);
		float y = column_h - mod(step_time * speed + hash(fi + 7.0) * column_h, column_h);
		float x = half_w + floor((hash(fi + 3.0) - 0.5) * (core_width * 2.0 + 12.0));
		if (p.x == x && p.y == floor(y))
			col = t < 0.8 ? c_light : c_mid;
	}

	COLOR = vec4(col.rgb, col.a * COLOR.a);
}
