// Étalonnage du biome où se trouve le joueur (plan 39 A1) et ombres de nuages (A2), publiés par BiomeAtmosphere en
// uniformes globaux et fondus au changement de biome. Partagés par le sol, les routes, les décors, les canopées et, en
// partie, les entités.
// Neutre par défaut (project.godot) : le Hub et les écrans hors run n'en voient rien.

// rgb : teinte déjà normalisée en luminance et multipliée par l'exposition.
global uniform vec4 world_grade_tint;
// x : saturation ; y : contraste ; z : part de l'étalonnage reçue par les entités (joueur, créatures).
global uniform vec4 world_grade_tone;

const vec3 GRADE_LUMA = vec3(0.299, 0.587, 0.114);
// Pivot du contraste : un peu sous le gris moyen, les sols du jeu sont sombres.
const float GRADE_PIVOT = 0.4;

vec3 world_grade(vec3 color, float amount) {
	vec3 graded = color * world_grade_tint.rgb;
	graded = mix(vec3(dot(graded, GRADE_LUMA)), graded, world_grade_tone.x);
	graded = (graded - GRADE_PIVOT) * world_grade_tone.y + GRADE_PIVOT;
	return mix(color, clamp(graded, 0.0, 1.0), amount);
}

// Ombres de nuages (plan 39 A2), même source et mêmes fondus que l'étalonnage.
// xy : décalage du motif (défilement au vent) ; z : part du ciel couverte ; w : noirceur de l'ombre (0 : aucun nuage).
global uniform vec4 world_clouds;
// 1 / taille moyenne d'un nuage au sol, en px.
global uniform float world_cloud_scale;

// L'ombre garde un peu du bleu du ciel : le rouge et le vert baissent plus que le bleu.
const vec3 CLOUD_SHADOW_TINT = vec3(1.0, 0.94, 0.8);

float cloud_hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float cloud_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(cloud_hash(i), cloud_hash(i + vec2(1.0, 0.0)), f.x),
		mix(cloud_hash(i + vec2(0.0, 1.0)), cloud_hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

// Ombre au pixel monde p, de 0 (plein soleil) à la noirceur w. Repère au sol : la vue 2:1 écrase la profondeur de
// moitié, un nuage rond fait une ombre deux fois plus large que haute. Quatre paliers tramés au pixel (Bayer 2×2) :
// la lisière reste du pixel art, pas un dégradé lisse.
float cloud_shadow(vec2 p) {
	vec2 q = (p + world_clouds.xy) * vec2(1.0, 2.0) * world_cloud_scale;
	float n = cloud_noise(q) * 0.65 + cloud_noise(q * 2.7 + 13.1) * 0.35;
	float cover = smoothstep(1.0 - world_clouds.z, 1.0 - world_clouds.z + 0.12, n);
	vec2 cell = mod(floor(p), 2.0);
	float dither = mod(cell.x * 2.0 + cell.y * 3.0, 4.0) * 0.25 + 0.125;
	return clamp(floor(cover * 3.0 + dither), 0.0, 3.0) / 3.0 * world_clouds.w;
}

vec3 cloud_shade(vec3 color, vec2 p, float amount) {
	if (world_clouds.w <= 0.0 || amount <= 0.0)
		return color;
	return color * (vec3(1.0) - cloud_shadow(p) * amount * CLOUD_SHADOW_TINT);
}

// Direction crade (plan 40) : palette salie comme un vieux papier imprimé, puis grain au pixel du monde. Même source et
// mêmes fondus que l'étalonnage ; appliqué après lui et avant l'oubli, si bien que le Néant reste d'un blanc pur.
// x : dosage du biome (0 : aucun effet) ; y : grain fin au pixel ; z : fibre lente du papier ; w : taches (G2).
global uniform vec4 world_grime;
// rgb : jaunissement (multiplie) ; a : saturation gardée.
global uniform vec4 world_grime_paper;
// rgb : noir relevé vers un brun-olive ; a : part des ombres tirée vers le vert-gris.
global uniform vec4 world_grime_ink;

const vec3 GRIME_SHADOW = vec3(0.2, 0.22, 0.17);

// Palette seule : c'est tout ce que reçoivent les entités, sur qui un grain accroché au monde glisserait.
vec3 grime_palette(vec3 color, float amount) {
	if (amount <= 0.0)
		return color;
	float luma = dot(color, GRADE_LUMA);
	vec3 dirty = mix(vec3(luma), color, world_grime_paper.a) * world_grime_paper.rgb;
	float shade = 1.0 - clamp(luma, 0.0, 1.0);
	dirty = mix(dirty, GRIME_SHADOW * luma, shade * shade * world_grime_ink.a);
	dirty = world_grime_ink.rgb + dirty * (1.0 - world_grime_ink.rgb);
	return mix(color, dirty, amount);
}

// Sol, routes et décors : palette, puis grain lu au pixel monde p (fixe quand la caméra bouge).
vec3 world_grime_at(vec3 color, vec2 p) {
	float amount = world_grime.x;
	if (amount <= 0.0)
		return color;
	vec2 cell = floor(p);
	// Nom local distinct de l'uniforme « grain » du sol, qui inclut ce fichier.
	float speckle = (cloud_hash(cell + 71.3) - 0.5) * 2.0 * world_grime.y
		+ (cloud_noise(cell / 6.0 + 23.0) - 0.5) * 2.0 * world_grime.z;
	return grime_palette(color, amount) * (1.0 + speckle * amount);
}
