// Mémoire des zones de l'Effacement, publiée par ErasureManager en global shader uniforms (plan 16).
// Partagée par le sol, les décors et les canopées.

global uniform sampler2D erasure_memory;
// xy : coin monde de la fenêtre ; zw : taille monde de la fenêtre.
global uniform vec4 erasure_window;
// Mémoire hors de la fenêtre suivie.
global uniform float erasure_far_memory;

const vec3 ERASURE_WHITE = vec3(0.961, 0.941, 0.922);   // Blanc effacement #F5F0EB
const vec3 ERASURE_PALE = vec3(0.847, 0.878, 0.918);    // blanc-bleuté

// Filtrage bilinéaire à la main : une zone fait 128 px, la transition entre zones reste douce.
float memory_at(vec2 p) {
	vec2 size = vec2(textureSize(erasure_memory, 0));
	if (size.x < 2.0 || erasure_window.z <= 0.0)
		return erasure_far_memory;
	vec2 uv = (p - erasure_window.xy) / erasure_window.zw;
	if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0)
		return erasure_far_memory;
	vec2 t = uv * size - 0.5;
	ivec2 i = ivec2(floor(t));
	vec2 f = fract(t);
	ivec2 hi = ivec2(size) - 1;
	float a = texelFetch(erasure_memory, clamp(i, ivec2(0), hi), 0).r;
	float b = texelFetch(erasure_memory, clamp(i + ivec2(1, 0), ivec2(0), hi), 0).r;
	float c = texelFetch(erasure_memory, clamp(i + ivec2(0, 1), ivec2(0), hi), 0).r;
	float d = texelFetch(erasure_memory, clamp(i + ivec2(1, 1), ivec2(0), hi), 0).r;
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float erasure_hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

// Couleurs qui se délavent (Fragile → Effacée), vers un gris froid et pâle.
vec3 erasure_wash(vec3 color, float memory) {
	float fade = smoothstep(0.8, 0.2, memory);
	float luminance = dot(color, vec3(0.299, 0.587, 0.114));
	return mix(color, mix(vec3(luminance), ERASURE_PALE, 0.35), fade * 0.85);
}

// Décor qui se défait : délavé, bords qui s'effritent en pixels perdus, pixels blanchis ; au Néant, des fragments.
// texel : position du pixel dans la texture (pixels entiers) ; edge : 1 si un voisin est transparent.
vec4 erasure_prop(vec4 color, float memory, vec2 texel, float edge) {
	if (memory >= 0.8 || color.a <= 0.0)
		return color;
	color.rgb = erasure_wash(color.rgb, memory);
	float roll = erasure_hash(texel + 0.37);
	float crumble = smoothstep(0.55, 0.1, memory);
	// Les bords partent d'abord, puis l'intérieur au Néant.
	float loss = edge > 0.5 ? crumble * 0.85 : smoothstep(0.2, 0.0, memory) * 0.7;
	if (roll < loss)
		color.a = 0.0;
	else if (erasure_hash(texel * 1.9 + 3.1) < crumble * 0.35)
		color.rgb = roll > 0.5 ? ERASURE_WHITE : ERASURE_PALE;
	return color;
}
