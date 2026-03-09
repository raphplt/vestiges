shader_type canvas_item;

// Shader atmosphérique Marécages — post-process pixel-art
// Effets lore : eau laiteuse, réalité fragile, reflets spectraux,
// brume permanente, distorsion subtile du monde.
// S'applique sur un ColorRect plein écran via screen_texture (Godot 4).

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

// === Contrôle global ===
uniform float intensity : hint_range(0.0, 1.0) = 1.0;
uniform float local_moisture : hint_range(0.0, 1.0) = 0.0;

// === Brume laiteuse (l'eau absorbe la lumière) ===
uniform vec4 fog_color : source_color = vec4(0.48, 0.54, 0.60, 1.0);
uniform float fog_amount : hint_range(0.0, 0.5) = 0.10;
uniform float desaturation : hint_range(0.0, 1.0) = 0.22;

// === Distorsion de réalité (le monde tremble) ===
uniform float distortion_strength : hint_range(0.0, 4.0) = 0.85;
uniform float distortion_speed : hint_range(0.1, 3.0) = 0.6;
uniform float distortion_scale : hint_range(2.0, 30.0) = 8.0;

// === Reflets spectraux (choses qui n'existent plus) ===
uniform float ghost_strength : hint_range(0.0, 0.3) = 0.04;
uniform float ghost_speed : hint_range(0.1, 2.0) = 0.3;
uniform vec4 ghost_tint : source_color = vec4(0.75, 0.80, 0.82, 1.0);

// === Vignette (visibilité réduite, brume dense aux bords) ===
uniform float vignette_strength : hint_range(0.0, 1.5) = 0.45;
uniform float vignette_softness : hint_range(0.1, 1.0) = 0.52;

// === Pixel grid (respect du pixel art) ===
uniform vec2 pixel_size = vec2(480.0, 270.0);

// Hash noise
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
		mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x),
		f.y
	);
}

// Bruit multi-octave pour la brume organique
float fbm(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	for (int i = 0; i < 3; i++) {
		v += a * noise(p);
		p *= 2.1;
		a *= 0.5;
	}
	return v;
}

void fragment() {
	// Snap UV au pixel grid pour garder le style pixel-art
	vec2 snapped_uv = floor(SCREEN_UV * pixel_size) / pixel_size;
	float t = TIME;
	float moisture = clamp(local_moisture, 0.0, 1.0);

	// --- 1. Distorsion de réalité ---
	// Le monde tremble, la réalité est fragile ici
	vec2 distort_uv = snapped_uv * distortion_scale;
	float dx = noise(distort_uv + vec2(t * distortion_speed, 0.0)) - 0.5;
	float dy = noise(distort_uv + vec2(0.0, t * distortion_speed * 0.7)) - 0.5;
	// Snap la distorsion au pixel grid
	float distortion_mix = intensity * (0.45 + moisture * 0.55);
	vec2 offset = vec2(dx, dy) * distortion_strength * distortion_mix / pixel_size;
	offset = floor(offset * pixel_size) / pixel_size;
	vec2 final_uv = snapped_uv + offset;

	// Lire la scène
	vec4 scene = texture(screen_texture, final_uv);

	// --- 2. Reflets spectraux ---
	// Fantômes de ce qui n'existe plus : image décalée verticalement, teintée
	float ghost_phase = sin(t * ghost_speed) * 0.5 + 0.5;
	vec2 ghost_uv = final_uv;
	// Miroir vertical subtil + décalage ondulant
	ghost_uv.y = 1.0 - ghost_uv.y;
	float ghost_wave = noise(vec2(snapped_uv.x * 6.0, t * 0.4)) * 2.0 / pixel_size.y;
	ghost_uv.y += ghost_wave;
	ghost_uv = clamp(ghost_uv, vec2(0.0), vec2(1.0));
	vec4 ghost_sample = texture(screen_texture, ghost_uv);
	// Teinter en spectral (bleu-gris délavé, comme les reflets dans l'eau laiteuse)
	ghost_sample.rgb = mix(ghost_sample.rgb, ghost_tint.rgb, 0.6);
	// Pulse d'apparition/disparition
	float ghost_alpha = ghost_strength * intensity * (0.4 + 0.6 * moisture) * (0.6 + 0.4 * ghost_phase);
	// Plus fort dans la moitié inférieure (au sol / eau)
	ghost_alpha *= smoothstep(0.3, 0.7, snapped_uv.y);
	scene.rgb = mix(scene.rgb, ghost_sample.rgb, ghost_alpha);

	// --- 3. Brume laiteuse + désaturation ---
	// L'eau absorbe la lumière, tout paraît étouffé
	float lum = dot(scene.rgb, vec3(0.299, 0.587, 0.114));
	vec3 gray = vec3(lum);
	scene.rgb = mix(scene.rgb, gray, desaturation * intensity);

	// Brume non-uniforme via fbm (nappes de brouillard)
	float fog_pattern = fbm(snapped_uv * 4.0 + vec2(t * 0.08, t * 0.05));
	float low_fog_band = 1.0 - smoothstep(0.34, 0.86, snapped_uv.y);
	float low_fog_noise = fbm(snapped_uv * vec2(5.6, 8.0) + vec2(t * 0.03, -t * 0.04));
	float low_fog = low_fog_band * low_fog_noise * (0.12 + moisture * 0.22);
	float fog_factor = fog_amount * intensity * (0.58 + 0.42 * fog_pattern);
	fog_factor += low_fog * intensity;
	scene.rgb = mix(scene.rgb, fog_color.rgb, fog_factor);

	// --- 4. Vignette (visibilité réduite aux bords) ---
	// On ne voit pas loin dans le marécage
	vec2 vig_uv = snapped_uv - 0.5;
	float vig_dist = length(vig_uv);
	float vig = smoothstep(vignette_softness, vignette_softness + 0.3, vig_dist);
	scene.rgb = mix(scene.rgb, fog_color.rgb, vig * vignette_strength * intensity * (0.8 + moisture * 0.2));

	COLOR = vec4(scene.rgb, 1.0);
}
