// Bruits du sol et oubli du sol (plan 16 O1), partagés par le sol et les chemins de terre.
// Inclure après erasure_memory.gdshaderinc.

const vec3 CRACK_LIGHT = vec3(0.749, 0.910, 1.0);       // lueur des fissures

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

vec2 hash2(vec2 p) {
	return vec2(hash(p), hash(p + vec2(17.1, 31.7)));
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

// Distance au bord de cellule de Voronoï (F2 − F1) : faible sur les lignes de fissure.
float crack_distance(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float f1 = 8.0;
	float f2 = 8.0;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 g = vec2(float(x), float(y));
			vec2 o = hash2(i + g);
			float d = length(g + o - f);
			if (d < f1) {
				f2 = f1;
				f1 = d;
			} else if (d < f2) {
				f2 = d;
			}
		}
	}
	return f2 - f1;
}

vec3 forget(vec3 color, vec2 p, float memory) {
	ivec2 pixel = ivec2(floor(p));
	float threshold = bayer4(pixel);
	// Repère au sol : la vue 2:1 écrase la profondeur de moitié à l'écran.
	vec2 ground = vec2(p.x, p.y * 2.0);
	float wander = value_noise(ground * 0.02);

	// Fragile → Effacée : les couleurs se délavent vers un gris froid et pâle.
	color = erasure_wash(color, memory);

	// Effilochée → Effacée : des pixels passent au blanc, en plaques qui ondulent.
	float bleach = smoothstep(0.55, 0.05, memory) * (0.55 + 0.45 * wander);
	if (bleach > threshold)
		color = hash(vec2(pixel)) > 0.5 ? ERASURE_WHITE : ERASURE_PALE;

	// Effilochée → Effacée : le sol se fissure de lumière, les fissures s'élargissent et respirent.
	float cracking = smoothstep(0.5, 0.12, memory);
	// Seule une partie du réseau apparaît, par veines qui gagnent du terrain quand la mémoire baisse.
	float veins = value_noise(ground / 150.0);
	if (cracking > 0.0 && veins > mix(0.72, 0.4, cracking)) {
		float width = mix(0.012, 0.045, cracking);
		float crack = crack_distance(ground / 80.0);
		if (crack < width) {
			float pulse = 0.75 + 0.25 * sin(TIME * 2.2 + wander * 6.2831);
			color = mix(color, CRACK_LIGHT, pulse * min(1.0, cracking * 1.6));
		}
	}

	// Frontière de l'Effacé (mémoire 25 %) : une lisière blanche qui scintille, lisible sans regarder le HUD.
	float front = 1.0 - abs(memory - 0.25) / 0.035;
	if (front > 0.0) {
		float shimmer = hash(vec2(pixel) + floor(TIME * 8.0));
		if (shimmer < front * 0.75)
			color = mix(ERASURE_WHITE, CRACK_LIGHT, step(0.5, hash(vec2(pixel) * 1.7 + floor(TIME * 3.0))));
	}

	// Néant : le sol disparaît dans un blanc qui grésille.
	float nothing = smoothstep(0.06, 0.0, memory);
	if (nothing > 0.0) {
		float flicker = hash(vec2(pixel) + floor(TIME * 6.0));
		color = mix(color, flicker > 0.5 ? ERASURE_WHITE : ERASURE_PALE, nothing);
	}
	return color;
}
