shader_type canvas_item;
render_mode unshaded;

// Effets d'attaque en pixel art (plan 08, effets d'attaque).
// La forme est calculée dans le repère des texels du monde : toute direction est possible sans
// tourner le quad, donc sans pixels de biais. Trois tons de palette, contour teinté d'un texel,
// animation par poses (progress arrive déjà quantifié) et disparition par tramage ordonné.

// 0 arc (traînée de lame), 1 poussée, 2 anneau, 3 zone au sol, 4 cône, 5 rayon brisé, 6 étoile d'impact,
// 7 couloir annoncé (trajectoire d'un bond)
uniform int shape = 0;
uniform vec2 size_px = vec2(64.0);
uniform float angle = 0.0;
uniform float radius = 24.0;
uniform float thickness = 6.0;
uniform float arc_half = 1.0;
// Compression verticale : 2 pour une forme posée au sol en vue iso.
uniform float squash = 1.0;
uniform float progress = 0.0;
uniform float fade = 0.0;
uniform float fill_density = 0.35;
uniform bool progress_fill = true;
uniform float seed = 0.0;
uniform vec4 c_light : source_color = vec4(0.96, 0.94, 0.92, 1.0);
uniform vec4 c_mid : source_color = vec4(0.91, 0.88, 0.83, 1.0);
uniform vec4 c_dark : source_color = vec4(0.83, 0.66, 0.26, 1.0);
uniform vec4 c_outline : source_color = vec4(0.42, 0.23, 0.14, 1.0);

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

vec2 to_local(vec2 p) {
	vec2 q = vec2(p.x, p.y * squash);
	float c = cos(-angle);
	float s = sin(-angle);
	return vec2(q.x * c - q.y * s, q.x * s + q.y * c);
}

// Renvoie la teinte (1 clair, 0.5 moyen, 0 sombre) ou -1 hors forme.
float arc_tone(vec2 p) {
	vec2 q = to_local(p);
	float r = length(q);
	float a = atan(q.y, q.x);
	// La tête balaie l'ouverture, la queue la rattrape : trame de mouvement qui se referme.
	float head = -arc_half + 2.0 * arc_half * min(1.0, progress / 0.5);
	float tail = -arc_half + 2.0 * arc_half * clamp((progress - 0.2) / 0.8, 0.0, 1.0);
	if (a < tail || a > head || head - tail < 0.02)
		return -1.0;
	float t = (a - tail) / (head - tail);
	float th = thickness * (0.25 + 0.75 * t);
	float depth = radius - r;
	if (depth < 0.0 || depth > th)
		return -1.0;
	// Queue tramée : la traînée se défait en pixels épars.
	if (t < 0.3 && bayer4(p) > t / 0.3)
		return -1.0;
	float v = 0.55 * t + 0.45 * (1.0 - depth / max(th, 1.0));
	return v > 0.62 ? 1.0 : (v > 0.36 ? 0.5 : 0.0);
}

float thrust_tone(vec2 p) {
	vec2 q = to_local(p);
	float head = radius * min(1.0, progress / 0.35);
	float tail = radius * clamp((progress - 0.25) / 0.75, 0.0, 1.0);
	if (q.x < tail || q.x > head || head - tail < 0.5)
		return -1.0;
	float t = (q.x - tail) / (head - tail);
	float half_width = thickness * 0.5 * clamp((head - q.x) / (thickness * 1.6), 0.15, 1.0) * (0.35 + 0.65 * t);
	float y = abs(q.y);
	if (y > half_width + 0.25)
		return -1.0;
	if (t < 0.3 && bayer4(p) > t / 0.3)
		return -1.0;
	float v = 0.5 * t + 0.5 * (1.0 - y / max(half_width, 0.5));
	return v > 0.6 ? 1.0 : (v > 0.35 ? 0.5 : 0.0);
}

float ring_tone(vec2 p) {
	vec2 q = vec2(p.x, p.y * squash);
	float grow = 1.0 - pow(1.0 - progress, 2.0);
	float ring_radius = radius * (0.2 + 0.8 * grow);
	float th = max(1.0, thickness * (1.0 - 0.75 * progress));
	float d = length(q) - ring_radius;
	if (abs(d) > th * 0.5)
		return -1.0;
	float v = (d + th * 0.5) / th;
	return v > 0.6 ? 1.0 : (v > 0.3 ? 0.5 : 0.0);
}

// Zone au sol annoncée : bord plein, intérieur tramé, disque de progression qui se remplit.
float zone_tone(vec2 p) {
	vec2 q = vec2(p.x, p.y * squash);
	float d = length(q);
	if (d > radius)
		return -1.0;
	if (d > radius - max(1.0, thickness))
		return 0.5;
	if (progress_fill && d < radius * progress)
		return bayer4(p) < max(0.5, fill_density) ? 1.0 : -1.0;
	return bayer4(p) < fill_density ? 0.0 : -1.0;
}

float cone_tone(vec2 p) {
	vec2 q = to_local(p);
	float r = length(q);
	float a = abs(atan(q.y, q.x));
	if (r > radius || a > arc_half)
		return -1.0;
	float edge = min(radius - r, (arc_half - a) * r);
	if (edge < 1.0)
		return 0.5;
	// Bandes qui s'éloignent de la source.
	float band = fract(r / 10.0 - progress * 3.0);
	if (band < 0.22)
		return 1.0;
	return bayer4(p) < fill_density * (1.0 - r / radius * 0.6) ? 0.0 : -1.0;
}

// Rayon brisé entre deux points : le quad est centré sur le milieu, radius = demi-longueur.
float beam_tone(vec2 p) {
	vec2 q = to_local(p);
	if (abs(q.x) > radius)
		return -1.0;
	float segment = floor((q.x + radius) / 5.0);
	float f = fract((q.x + radius) / 5.0);
	float y0 = (hash(segment + floor(progress * 4.0) * 17.0) - 0.5) * thickness * 2.0;
	float y1 = (hash(segment + 1.0 + floor(progress * 4.0) * 17.0) - 0.5) * thickness * 2.0;
	float center = mix(y0, y1, f);
	float d = abs(q.y - center);
	if (d < 0.75)
		return 1.0;
	if (d < 1.6)
		return 0.5;
	return -1.0;
}

// Étoile d'impact : croix franche qui s'allonge, diagonales courtes, cœur clair.
float star_tone(vec2 p) {
	vec2 q = abs(p);
	float grow = min(1.0, progress / 0.4);
	float arm = radius * (0.45 + 0.55 * grow);
	float cross_arm = min(q.x, q.y) < 0.6 ? max(q.x, q.y) : 99.0;
	float diagonal = abs(q.x - q.y) < 0.6 ? q.x : 99.0;
	if (cross_arm <= arm)
		return cross_arm < arm * 0.35 ? 1.0 : 0.5;
	if (diagonal <= arm * 0.45)
		return 0.0;
	return -1.0;
}

// Couloir au sol depuis le centre du quad, longueur radius, largeur thickness : bord plein,
// intérieur tramé, remplissage qui avance avec la progression.
float lane_tone(vec2 p) {
	vec2 q = to_local(p);
	float half_width = thickness * 0.5;
	float end_distance = length(q - vec2(radius, 0.0));
	bool inside_rect = q.x >= 0.0 && q.x <= radius && abs(q.y) <= half_width;
	if (!inside_rect && end_distance > half_width)
		return -1.0;
	bool border = (abs(q.y) > half_width - 1.0 && q.x <= radius) || q.x < 1.0
		|| (q.x > radius && end_distance > half_width - 1.0);
	if (border)
		return 0.5;
	if (progress_fill && q.x < radius * progress)
		return bayer4(p) < max(0.5, fill_density) ? 1.0 : -1.0;
	return bayer4(p) < fill_density ? 0.0 : -1.0;
}

float tone_at(vec2 p) {
	if (shape == 0) return arc_tone(p);
	if (shape == 1) return thrust_tone(p);
	if (shape == 2) return ring_tone(p);
	if (shape == 3) return zone_tone(p);
	if (shape == 4) return cone_tone(p);
	if (shape == 5) return beam_tone(p);
	if (shape == 6) return star_tone(p);
	return lane_tone(p);
}

void fragment() {
	float alpha = COLOR.a;
	vec2 p = floor(UV * size_px) - floor(size_px * 0.5) + 0.5;
	float tone = tone_at(p);
	vec4 col = vec4(0.0);
	if (tone >= 0.0) {
		col = tone > 0.75 ? c_light : (tone > 0.25 ? c_mid : c_dark);
	} else if (shape != 3 && shape != 4 && shape != 7 && (tone_at(p + vec2(1.0, 0.0)) >= 0.0 || tone_at(p - vec2(1.0, 0.0)) >= 0.0
			|| tone_at(p + vec2(0.0, 1.0)) >= 0.0 || tone_at(p - vec2(0.0, 1.0)) >= 0.0)) {
		col = c_outline;
	}
	if (col.a > 0.0 && bayer4(p) < fade)
		col.a = 0.0;
	COLOR = vec4(col.rgb, col.a * alpha);
}
