// Fond animé des écrans de choix (plan 24 B1) : dessiné en gros pixels sur la grille de l'écran, palette réduite et
// tramage ordonné. Rayons en marches qui tournent, halo central, poussière qui monte, bords qui s'effilochent en violet
// comme une zone qui s'oublie. Transparent hors de ses motifs : posé sur le voile sombre de l'écran.
shader_type canvas_item;

uniform vec4 tint : source_color = vec4(0.95, 0.78, 0.35, 1.0);
uniform vec4 fray_color : source_color = vec4(0.42, 0.31, 0.63, 1.0);
uniform float pixel_size = 4.0;
uniform float ray_count = 12.0;
uniform float ray_speed = 0.035;
uniform float intensity : hint_range(0.0, 1.0) = 1.0;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float bayer4(vec2 cell) {
	const float matrix[16] = float[](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
	int x = int(mod(cell.x, 4.0));
	int y = int(mod(cell.y, 4.0));
	return (matrix[y * 4 + x] + 0.5) / 16.0;
}

void fragment() {
	vec2 screen = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 cells = floor(screen / pixel_size);
	vec2 cell = floor(FRAGCOORD.xy / pixel_size);
	vec2 p = (cell - cells * 0.5) / cells.y;
	float radius = length(p);

	// Rayons : bandes franches qui tournent lentement, plus pâles en s'éloignant.
	float angle = atan(p.y, p.x);
	float band = fract(angle / 6.2831853 * ray_count + TIME * ray_speed);
	float ray = step(band, 0.42) * (1.0 - smoothstep(0.05, 0.95, radius));
	float halo = 1.0 - smoothstep(0.0, 0.55, radius);
	float level = (0.55 * ray + 0.45 * halo) * intensity;

	// Trois tons tramés : le dégradé reste en pixels francs.
	float tone = floor(level * 3.0 + bayer4(cell)) / 3.0;

	// Poussière d'oubli : quelques pixels clairs qui montent, chacun dans sa colonne.
	float rise = floor(cell.y + TIME * 6.0);
	float dust = step(0.9965, hash(vec2(cell.x, rise))) * (1.0 - smoothstep(0.2, 0.9, radius)) * intensity;

	// Bords effilochés : près du cadre, des pixels violets qui frémissent.
	vec2 edge2 = abs(cell / cells - 0.5) * 2.0;
	float edge = max(edge2.x, edge2.y);
	float fray = step(0.91 + 0.08 * hash(cell + floor(TIME * 3.0)), edge) * intensity;

	vec3 color = tint.rgb * (0.55 + 0.45 * tone);
	color = mix(color, vec3(1.0, 0.97, 0.88), dust);
	color = mix(color, fray_color.rgb, fray);
	float alpha = clamp(tone * 0.42 + dust + fray * 0.55, 0.0, 0.9);
	COLOR = vec4(color, alpha);
}
