// Étalonnage du biome où se trouve le joueur (plan 39 A1), publié par BiomeAtmosphere en uniformes globaux et fondu
// au changement de biome. Partagé par le sol, les routes, les décors, les canopées et, en partie, les entités.
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
