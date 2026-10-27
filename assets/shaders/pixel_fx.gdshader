shader_type canvas_item;
render_mode unshaded;

// Effets d'attaque en pixel art (plan 08, effets d'attaque).
// La forme est calculée dans le repère des texels du monde : toute direction est possible sans
// tourner le quad, donc sans pixels de biais. Trois tons de palette, contour teinté d'un texel,
// animation par poses (progress arrive déjà quantifié) et disparition par tramage ordonné.

// 0 arc (traînée de lame), 1 poussée, 2 anneau, 3 zone au sol, 4 cône, 5 rayon brisé, 6 étoile d'impact,
// 7 couloir annoncé (trajectoire d'un bond), 8 câble de la Rallonge
uniform int shape = 0;
uniform vec2 size_px = vec2(64.0);
uniform float angle = 0.0;
uniform float radius = 24.0;
uniform float thickness = 6.0;
uniform float arc_half = 1.0;
// Zone au sol (forme 3) : demi-ouverture d'une brèche centrée sur angle, au sol ; 0 pour un disque plein (fauchage
// de l'Épouvantail, plan 57 G2).
uniform float gap_half = 0.0;
// Compression verticale : 2 pour une forme posée au sol en vue iso.
uniform float squash = 1.0;
uniform float progress = 0.0;
uniform float fade = 0.0;
uniform float fill_density = 0.35;
uniform bool progress_fill = true;
uniform float seed = 0.0;
// Matière d'une zone au sol (forme 3) : 0 annonce, 1 liquide, 2 feu, 3 cadran, 4 éclat (plan 25 S10).
uniform int matter = 0;
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

// Un fil matériel relié au buste : courbe en S en retard sur la prise qui fait un tour.
// Son épaisseur reste fixe quand l'allonge augmente, pour ne pas redevenir une traînée de lame.
float cable_tone(vec2 p) {
	vec2 q = to_local(p);
	float head = -arc_half + 2.0 * arc_half * min(progress / 0.85, 1.0);
	float c = cos(head);
	float s = sin(head);
	vec2 v = vec2(q.x * c + q.y * s, -q.x * s + q.y * c);
	float reach = radius * (0.4 + 0.6 * min(progress / 0.2, 1.0));
	// Prise moulée et deux broches, dont l'extrémité marque exactement l'allonge.
	float tip = reach - v.x;
	float side = abs(v.y);
	if (tip >= 2.5 && tip <= 8.0 && side <= 3.5)
		return side > 2.0 || tip > 6.5 ? 0.0 : 1.0;
	if (tip >= 0.0 && tip < 2.5 && abs(side - 1.8) < 0.65)
		return 1.0;
	float length = max(reach - 7.0, 1.0);
	if (v.x < 3.0 || v.x > length)
		return -1.0;
	float u = v.x / length;
	float bend = sin(u * TAU) * sin(u * PI) * reach * 0.16;
	bend *= sin(progress * PI + 0.5);
	float d = v.y - bend;
	float half_width = thickness * 0.5;
	if (abs(d) > half_width)
		return -1.0;
	return d < -0.3 ? 1.0 : (d < half_width - 0.5 ? 0.5 : 0.0);
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
	// Onde au bord légèrement irrégulier, comme les zones qu'elle accompagne (plan 25 S10).
	float theta = atan(q.y, q.x);
	float wobble = 1.0 + 0.04 * sin(3.0 * theta + seed * 1.7) + 0.03 * sin(5.0 * theta + seed * 3.9);
	float ring_radius = radius * (0.2 + 0.8 * grow) * wobble;
	float th = max(1.0, thickness * (1.0 - 0.75 * progress));
	float d = length(q) - ring_radius;
	if (abs(d) > th * 0.5)
		return -1.0;
	float v = (d + th * 0.5) / th;
	return v > 0.6 ? 1.0 : (v > 0.3 ? 0.5 : 0.0);
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
	// Ondes en plus (plan 21 G6g) : pour le cône, thickness compte des fronts qui parcourent la portée à chaque
	// cycle ; un front est un trait clair épais bordé de sombre, et les bandes passent alors au ton sombre pour
	// qu'on compte les fronts.
	int fronts = int(thickness);
	for (int k = 0; k < fronts; k++) {
		float gap = abs(r - fract(progress + float(k) / float(fronts)) * radius);
		if (gap < 2.5)
			return 1.0;
		if (gap < 4.0)
			return 0.0;
	}
	// Bandes qui s'éloignent de la source.
	float band = fract(r / 10.0 - progress * 3.0);
	if (band < 0.22)
		return fronts > 0 ? 0.0 : 1.0;
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

// --- Zones au sol (formes 3 et 7, plan 25 S10) ------------------------------------------------------------------
// Une matière par zone. Le bord est un cercle au sol (plan 25 S12) au grain d'un texel tiré de la graine : chaque zone
// est unique sans être une patate. Il garde une épaisseur d'un texel à l'écran, pôles compris, car il se lit sur les
// texels voisins. L'animation avance à 8 poses par seconde pendant toute la vie de la zone.


float cell_hash(vec2 c) {
	return fract(sin(dot(c, vec2(127.1, 311.7)) + seed * 74.7) * 43758.5453);
}

float value_noise(vec2 x) {
	vec2 i = floor(x);
	vec2 f = fract(x);
	f = f * f * (3.0 - 2.0 * f);
	float a = cell_hash(i);
	float b = cell_hash(i + vec2(1.0, 0.0));
	float c = cell_hash(i + vec2(0.0, 1.0));
	float d = cell_hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

vec2 ground(vec2 p) {
	return vec2(p.x, p.y * squash);
}

// Rayon du bord dans la direction de q : cercle exact, grain d'au plus `rough` unités au sol, continu le long du bord.
float rim_radius(vec2 q, float scale, float rough) {
	if (rough <= 0.0)
		return radius * scale;
	vec2 dir = q / max(length(q), 1e-4);
	return radius * scale + rough * (value_noise(dir * radius * 0.45 + 7.0) - 0.5) * 2.0;
}

bool in_zone(vec2 p, float scale, float rough) {
	vec2 q = ground(p);
	if (gap_half > 0.0 && abs(mod(atan(q.y, q.x) - angle + PI, TAU) - PI) < gap_half)
		return false;
	return length(q) <= rim_radius(q, scale, rough);
}

// Texel d'un anneau de rayon r au sol, épais d'un texel à l'écran : un pas de texel déplace q d'une unité selon x et
// de squash unités selon y.
bool on_ring(vec2 q, float r) {
	float d = max(length(q), 1e-4);
	vec2 step_px = abs(vec2(q.x, q.y * squash)) / d;
	return abs(d - r) <= 0.5 * max(step_px.x, step_px.y);
}

// Fissure : crête d'un bruit, assez fine pour rester un trait d'un texel.
bool crack(vec2 q) {
	return abs(value_noise(q * 0.16 + 3.0) - 0.5) < 0.03 || abs(value_noise(q * 0.31 + 11.0) - 0.5) < 0.018;
}

// Couloir au sol depuis l'origine, longueur radius × len_scale, largeur thickness : bords droits au grain d'un texel.
bool in_lane(vec2 p, float len_scale) {
	vec2 q = to_local(p);
	float half_width = thickness * 0.5 + 0.9 * (value_noise(vec2(q.x * 0.45, q.y > 0.0 ? 3.0 : 9.0)) - 0.5);
	float len = radius * len_scale;
	if (q.x < 0.0)
		return length(q) <= half_width;
	if (q.x <= len)
		return abs(q.y) <= half_width;
	return length(q - vec2(len, 0.0)) <= half_width;
}

bool in_area(vec2 p, float scale, float amp) {
	return shape == 7 ? in_lane(p, scale) : in_zone(p, scale, amp);
}

// 0 dedans, 1 bord intérieur, 2 contour extérieur, 3 dehors (voisins à un texel d'écran).
int edge_class(vec2 p, float scale, float amp) {
	// Hors de la bande du bord, la réponse est connue sans bruit : le grain borne le bord à ± amp, et un voisin à un
	// texel d'écran déplace q d'au plus squash.
	if (shape != 7) {
		float l = length(ground(p));
		if (l - squash > radius * scale + amp)
			return 3;
		// Une brèche coupe l'intérieur : seul le dehors se conclut sans tester les voisins.
		if (l + squash < radius * scale - amp && gap_half <= 0.0)
			return 0;
	}
	bool r = in_area(p + vec2(1.0, 0.0), scale, amp);
	bool l = in_area(p - vec2(1.0, 0.0), scale, amp);
	bool d = in_area(p + vec2(0.0, 1.0), scale, amp);
	bool u = in_area(p - vec2(0.0, 1.0), scale, amp);
	if (in_area(p, scale, amp))
		return (r && l && d && u) ? 0 : 1;
	return (r || l || d || u) ? 2 : 3;
}

vec4 shade(vec4 c, float a) {
	return vec4(c.rgb, a);
}

// Bord usé : contour sombre ébréché par endroits, liseré clair ou moyen selon un bruit le long du cercle.
vec4 worn_outline(vec2 p) {
	return shade(c_outline, cell_hash(floor(p) + 41.0) > 0.82 ? 0.5 : 0.92);
}

vec4 worn_rim(vec2 q) {
	return value_noise(q * 0.5 + 19.0) > 0.62 ? c_light : c_mid;
}

// Annonce d'un coup : contour sombre, liseré intérieur en tirets qui tournent, sol assombri et grenu derrière un front
// clair doublé d'une frange tramée, fissures qui gagnent avec le front, ondes qui convergent du bord vers le front.
// Un remplissage complet (fill_density 1) est l'éclair d'impact ; une densité nulle, une simple limite (portée).
vec4 mark_color(vec2 p, float t) {
	const float rough = 1.4;
	int e = edge_class(p, 1.0, rough);
	if (e == 3)
		return vec4(0.0);
	vec2 q = ground(p);
	if (e == 1)
		return worn_rim(q);
	if (e == 2)
		return worn_outline(p);
	bool dash = fract(atan(q.y, q.x) / TAU * 16.0 + t * 0.25) < 0.5;
	if (dash && on_ring(q, radius - 6.0))
		return fill_density <= 0.0 ? shade(c_mid, 0.75) : c_light;
	if (fill_density >= 0.99)
		return (crack(q) || value_noise(q / 2.0) > 0.72) ? c_mid : c_light;
	if (fill_density <= 0.0)
		return vec4(0.0);
	float d = length(q);
	float front = radius * progress;
	int f = edge_class(p, progress, 0.6);
	if (progress > 0.12 && f == 1)
		return c_light;
	if (f == 0) {
		if (front - d < 2.6 && bayer4(p) < 0.5)
			return shade(c_mid, 0.8);
		if (crack(q))
			return shade(c_outline, 0.85);
		return shade(c_dark, value_noise(q / 2.5) > 0.6 ? 0.66 : 0.52);
	}
	for (int k = 0; k < 2; k++) {
		float r = mix(radius - 4.0, max(front, 3.0), fract(t * 1.25 + float(k) * 0.5));
		if (on_ring(q, r))
			return shade(c_mid, 0.5);
	}
	if (crack(q) && bayer4(p) < 0.5)
		return shade(c_outline, 0.5);
	return shade(c_outline, value_noise(q / 2.0) > 0.68 ? 0.42 : 0.26);
}

// Couloir annoncé (bond, charge, tir visé) : bords droits, front qui avance, chevrons qui filent dans le sens du coup.
vec4 lane_color(vec2 p, float t) {
	int e = edge_class(p, 1.0, 0.0);
	if (e == 3)
		return vec4(0.0);
	vec2 q = to_local(p);
	if (e == 2)
		return worn_outline(p);
	if (e == 1)
		return worn_rim(q);
	if (fill_density >= 0.99)
		return c_light;
	if (fill_density <= 0.0)
		return vec4(0.0);
	int f = edge_class(p, progress, 0.0);
	if (progress > 0.05 && f == 1)
		return c_light;
	if (f == 0)
		return shade(c_dark, value_noise(q / 2.5) > 0.6 ? 0.66 : 0.52);
	if (fract((q.x - abs(q.y) * 0.9) / 12.0 - t * 1.5) < 0.2)
		return shade(c_mid, 0.6);
	return shade(c_outline, value_noise(q / 2.0) > 0.68 ? 0.42 : 0.26);
}

// Flaque : bord rond au grain d'un texel, volume par la lumière haut-gauche, reflet, ondes concentriques qui
// s'élargissent, taches qui dérivent, bulles qui éclatent, gouttes autour.
// fill_density règle son opacité : épaisse pour la bile, translucide pour une tache de gomme.
vec4 liquid_color(vec2 p, float t) {
	const float scale = 0.92;
	const float amp = 1.2;
	int e = edge_class(p, scale, amp);
	vec2 q = ground(p);
	if (e >= 2) {
		bool rim = e == 2;
		if (length(q) > radius * 1.24 + 2.4 + 2.0 * squash)
			return vec4(0.0);
		for (int k = 0; k < 5; k++) {
			float a = hash(float(k) * 3.1) * TAU;
			vec2 c = vec2(cos(a), sin(a)) * radius * (1.04 + 0.2 * hash(float(k) * 5.7));
			float drop = 1.2 + 1.2 * hash(float(k) * 9.3);
			float dd = length(q - c);
			if (dd < drop)
				return c_mid;
			rim = rim || dd < drop + 1.5;
		}
		return rim ? shade(c_outline, 0.75) : vec4(0.0);
	}
	if (e == 1)
		return p.x + q.y < 0.0 ? c_light : c_dark;
	if (!in_zone(p + vec2(1.0, 2.0), scale, amp))
		return c_dark;
	vec2 h = vec2(p.x + radius * 0.32, q.y + radius * 0.08);
	if ((abs(h.y) < 1.6 && abs(h.x) < radius * 0.16) || (abs(h.x - radius * 0.28) < 1.0 && abs(h.y) < 1.6))
		return c_light;
	for (int k = 0; k < 2; k++) {
		float wave = fract(t * 0.45 + float(k) * 0.5 + hash(17.0));
		if (wave < 0.85 && on_ring(q, radius * scale * 0.9 * wave) && (wave < 0.5 || bayer4(p) < 0.5))
			return c_light;
	}
	for (int k = 0; k < 3; k++) {
		float a = hash(float(k) * 7.7 + 1.0) * TAU;
		vec2 c = vec2(cos(a), sin(a) / squash) * radius * (0.15 + 0.4 * hash(float(k) * 4.3 + 2.0));
		float phase = fract(t * 0.7 + float(k) / 3.0);
		float ring = abs(length((p - c) * vec2(1.0, 1.5)) - (0.6 + phase * 2.0));
		if (ring < 0.6 && phase < 0.8)
			return phase < 0.5 ? c_light : c_dark;
	}
	return value_noise(q / 3.5 + vec2(t * 0.35, t * 0.2)) > 0.76 ? c_dark : c_mid;
}

float fire_heat(vec2 p, float t) {
	vec2 q = ground(p);
	float rel = length(q) / (radius * 0.95);
	float n = value_noise(vec2(p.x / 1.6, (p.y + t * 10.0) / 4.0)) * 0.6
		+ value_noise(vec2(p.x / 4.0, (p.y + t * 6.0) / 7.0) + 17.0) * 0.4;
	return n * (1.25 - rel * 0.7);
}

// Feu : sol roussi rond, couronne de braises qui scintille, anneau calciné, fissures rougeoyantes, flammèches qui
// montent, langues qui dépassent vers le haut de l'écran.
vec4 fire_color(vec2 p, float t) {
	const float scale = 0.95;
	const float amp = 1.5;
	int e = edge_class(p, scale, amp);
	if (e == 1) {
		float ember = cell_hash(floor(p) + vec2(0.0, floor(t * 8.0)) * 3.0);
		return ember > 0.62 ? c_light : (ember > 0.3 ? c_mid : c_dark);
	}
	if (e == 0) {
		if (cell_hash(floor(p) + vec2(0.0, floor(t * 8.0))) > 0.975)
			return c_light;
		vec2 q = ground(p);
		float rel = length(q) / (radius * scale);
		float heat = fire_heat(p, t);
		if (rel > 0.8 && heat < 0.7)
			return crack(q) ? c_dark : vec4(c_outline.rgb * 0.5, 0.8);
		if (heat > 0.70)
			return c_light;
		if (heat > 0.55)
			return c_mid;
		if (heat > 0.42 || crack(q))
			return c_dark;
		return vec4(c_outline.rgb * 0.6, bayer4(p) < 0.5 ? 0.75 : 0.5);
	}
	if (length(ground(p)) - 6.0 * squash > radius * scale + amp)
		return vec4(0.0);
	for (int lift = 1; lift <= 6; lift++) {
		vec2 below = p + vec2(0.0, float(lift));
		if (in_zone(below, scale, amp) && fire_heat(below, t) - float(lift) * 0.05 > 0.5
				&& value_noise(vec2(p.x / 1.5, (p.y + t * 14.0) / 2.5) + 31.0) > 0.45)
			return lift < 4 ? c_mid : c_dark;
	}
	return e == 2 ? shade(c_outline, 0.6) : vec4(0.0);
}

// Cadran couché : douze graduations, cercle intérieur tramé, aiguille qui fait un tour pendant la vie du champ.
vec4 dial_color(vec2 p) {
	int e = edge_class(p, 1.0, 0.0);
	if (e == 3)
		return vec4(0.0);
	if (e == 2)
		return shade(c_outline, 0.6);
	if (e == 1)
		return c_mid;
	vec2 q = ground(p);
	float d = length(q);
	float theta = atan(q.y, q.x);
	if (d > radius - 9.0 && abs(mod(theta + TAU / 24.0, TAU / 12.0) - TAU / 24.0) < 0.04)
		return c_light;
	if (edge_class(p, 0.62, 0.0) == 1 && bayer4(p) < 0.5)
		return shade(c_mid, 0.8);
	float hand = -TAU * 0.25 + progress * TAU;
	vec2 dir = vec2(cos(hand), sin(hand));
	float along = dot(q, dir);
	if (along > 0.0 && along < radius * 0.85 && abs(q.x * dir.y - q.y * dir.x) < 1.4)
		return c_light;
	float behind = mod(hand - theta, TAU);
	if (behind < 0.6 && bayer4(p) < 0.3)
		return shade(c_mid, 0.8);
	return shade(c_dark, 0.12);
}

// Éclat : onde de choc ronde qui s'élargit, seconde onde tramée, rayons clairs depuis le centre, débris projetés hors
// de la zone, sol roussi et fissuré.
vec4 burst_color(vec2 p) {
	float grow = 0.55 + 0.45 * min(1.0, progress * 2.5);
	float reach = radius * (0.3 + 0.95 * progress) * 1.1 + 2.0 * squash;
	for (int k = 0; k < 8 && length(ground(p)) < reach; k++) {
		float a = hash(float(k) * 2.3 + 0.5) * TAU;
		float dist = radius * (0.3 + 0.95 * progress) * (0.75 + 0.35 * hash(float(k) * 6.1));
		vec2 o = abs(p - vec2(cos(a), sin(a) / squash) * dist);
		if (max(o.x, o.y) < 1.0)
			return k % 2 == 0 ? c_light : c_mid;
	}
	int e = edge_class(p, grow, 1.0);
	if (e == 3)
		return vec4(0.0);
	if (e == 2)
		return shade(c_outline, 0.55);
	if (e == 1)
		return progress < 0.5 ? c_light : c_mid;
	vec2 q = ground(p);
	float d = length(q);
	if (on_ring(q, radius * grow * 0.78) && bayer4(p) < 0.5)
		return shade(c_mid, 0.8);
	float ray = abs(fract(atan(q.y, q.x) * 7.0 / TAU + seed / TAU) - 0.5);
	if (ray < 0.06 && d < radius * grow * 0.85)
		return d < radius * 0.35 ? c_light : c_mid;
	if (d < radius * 0.22 * (1.0 - progress))
		return c_light;
	if (crack(q))
		return shade(c_outline, 0.7);
	return bayer4(p) < 0.5 ? vec4(c_outline.rgb * 0.7, 0.4) : vec4(0.0);
}

vec4 ground_color(vec2 p, float t) {
	if (shape == 7) return lane_color(p, t);
	if (matter == 0) return mark_color(p, t);
	if (matter == 1) {
		vec4 liquid = liquid_color(p, t);
		return vec4(liquid.rgb, liquid.a * fill_density);
	}
	if (matter == 2) return fire_color(p, t);
	if (matter == 3) return dial_color(p);
	return burst_color(p);
}

float tone_at(vec2 p) {
	if (shape == 0) return arc_tone(p);
	if (shape == 1) return thrust_tone(p);
	if (shape == 2) return ring_tone(p);
	if (shape == 4) return cone_tone(p);
	if (shape == 5) return beam_tone(p);
	if (shape == 6) return star_tone(p);
	if (shape == 8) return cable_tone(p);
	return -1.0;
}

void fragment() {
	float alpha = COLOR.a;
	vec2 p = floor(UV * size_px) - floor(size_px * 0.5) + 0.5;
	float tone = tone_at(p);
	vec4 col = vec4(0.0);
	if (shape == 3 || shape == 7) {
		col = ground_color(p, floor(TIME * 8.0) / 8.0);
	} else if (tone >= 0.0) {
		col = tone > 0.75 ? c_light : (tone > 0.25 ? c_mid : c_dark);
	} else if (shape != 4 && (tone_at(p + vec2(1.0, 0.0)) >= 0.0 || tone_at(p - vec2(1.0, 0.0)) >= 0.0
			|| tone_at(p + vec2(0.0, 1.0)) >= 0.0 || tone_at(p - vec2(0.0, 1.0)) >= 0.0)) {
		col = c_outline;
	}
	if (col.a > 0.0 && bayer4(p) < fade)
		col.a = 0.0;
	COLOR = vec4(col.rgb, col.a * alpha);
}
