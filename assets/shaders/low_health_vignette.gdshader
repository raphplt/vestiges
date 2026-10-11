shader_type canvas_item;
render_mode unshaded;

uniform vec4 tint : source_color = vec4(0.77, 0.26, 0.17, 0.56);
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float thickness : hint_range(0.0, 0.5) = 0.22;
// Horloge de jeu : les fragments se figent avec la run pendant les menus.
uniform float elapsed = 0.0;

float grain(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	vec2 centered = (SCREEN_UV - vec2(0.5)) * 2.0;
	float radius = length(centered);
	float inner = 1.0 - thickness * 2.0 * (0.8 + 0.2 * intensity);
	// Un halo elliptique continu : pas de bandes ni de coins rectangulaires.
	float veil = smoothstep(inner, 1.38, radius);
	// Grille à hauteur native constante, quelle que soit la résolution de la fenêtre.
	vec2 pixel = floor(SCREEN_UV / SCREEN_PIXEL_SIZE / max(1.0, 1.0 / SCREEN_PIXEL_SIZE.y / 270.0));
	vec2 cluster = floor(pixel / 3.0);
	float patch = grain(cluster);
	float speck = grain(pixel);
	// Les amas s'effacent lentement, chacun à sa phase ; les pixels ne sont pas retirés au hasard à chaque image.
	float breath = 0.5 + 0.5 * sin(elapsed * 0.8 + patch * 6.283185);
	float erosion = smoothstep(0.35, 0.85, patch * 0.65 + speck * 0.35 + breath * 0.25);
	float dissolving = 1.0 - smoothstep(inner + 0.12, 1.25, radius);
	float alpha = veil * (1.0 - erosion * dissolving * 0.85);
	// Quelques restes de matière au bord intérieur du voile, eux aussi transparents et déclinants.
	float fringe = smoothstep(inner - 0.08, inner + 0.10, radius)
		* (1.0 - smoothstep(inner + 0.10, inner + 0.30, radius));
	alpha += fringe * step(0.82, patch) * step(0.5, speck) * breath * 0.035;
	vec3 blood = mix(tint.rgb * 0.55, tint.rgb, 1.0 - smoothstep(0.85, 1.5, radius));
	COLOR = vec4(blood, alpha * intensity * tint.a);
}
