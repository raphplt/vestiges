shader_type canvas_item;
render_mode unshaded;

// Colonne de lumière des coffres et des lieux (plan 17 lot 0A, reprise au plan 41 R3, choix de Raphaël) : faisceau
// fin qui se défait en pixels vers le haut. Des vagues de lumière le remontent, le cœur respire d'un à deux pixels,
// des éclats quittent le sommet et des poussières montent dedans ; au sol, un anneau en losange part du pied et
// s'efface. Dessinée dans le repère des texels du quad ; le temps avance par poses (8 par seconde).

uniform vec2 size_px = vec2(24.0, 140.0);
uniform float core_width = 1.0;
uniform float ground_radius = 11.0;
uniform float seed = 0.0;
uniform vec4 c_light : source_color = vec4(1.0);
uniform vec4 c_mid : source_color = vec4(0.8, 0.8, 0.8, 1.0);

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
	vec4 col = vec4(0.0);

	if (p.y < column_h) {
		// Vagues de lumière qui remontent le faisceau ; le cœur respire par tronçons de 6 texels.
		float wave = 0.5 + 0.5 * sin((p.y + step_time * 34.0) * 0.21);
		float breathe = step(0.5, fract(step_time * 0.9 + hash(floor(p.y / 6.0)) * 0.7));
		float core = core_width + breathe;
		float w = core + 1.0 + t * 4.0;
		float intensity = pow(1.0 - t, 1.3) * (0.55 + 0.45 * wave);
		// Au-dessus de la moitié, le faisceau se défait par amas de 2 texels qui changent à chaque demi-pose.
		float broken = t > 0.5 ? step(0.5, hash(floor(p.y / 2.0) * 7.0 + floor(p.x / 2.0) + floor(step_time * 4.0))) : 1.0;
		if (dx <= core && bayer4(p) < intensity * 1.3 * broken)
			col = c_light;
		else if (dx <= w && bayer4(p) < intensity * 0.6 * (1.0 - dx / (w + 1.0)) * broken)
			col = c_mid;
	} else {
		// Au sol, losange aplati 2:1 : lueur au pied et anneau qui s'élargit puis s'efface.
		vec2 q = vec2(p.x - half_w + 0.5, (p.y - column_h - ground_radius * 0.5 + 0.5) * 2.0);
		float d = (abs(q.x) + abs(q.y)) / ground_radius;
		float ring = fract(step_time * 0.7);
		if (d < 1.0 && abs(d - ring) < 0.09 && bayer4(p) < 1.0 - ring)
			col = c_light;
		else if (d < 0.45 && bayer4(p) < (0.45 - d) * 1.4)
			col = c_mid;
	}

	// Éclats qui quittent le sommet et poussières qui montent dans le faisceau.
	for (int i = 0; i < 10; i++) {
		float fi = float(i);
		float speed = 14.0 + 16.0 * hash(fi + 1.0);
		float y = column_h - mod(step_time * speed + hash(fi + 7.0) * column_h * 1.3, column_h * 1.3);
		float spread = y < column_h * 0.35 ? 6.0 : 2.0;
		float x = half_w + floor((hash(fi + 3.0) - 0.5) * (core_width * 2.0 + spread * 2.0));
		if (p.x == x && p.y == floor(y) && y > 0.0)
			col = (column_h - y) / column_h < 0.7 ? c_light : c_mid;
	}

	COLOR = vec4(col.rgb, col.a * COLOR.a);
}
