shader_type canvas_item;

// Mort du joueur (plan 02 M1) : l'Effacement part du joueur et s'étend en cercle irrégulier. Derrière son front,
// le monde perd ses couleurs et pâlit vers le blanc d'effacement ; le front lui-même est une trame violette qui
// s'émiette (charte §5 : le tramage est l'effacement, pas de fondu flou). Tout est calculé en pixels du monde :
// la trame ne glisse pas quand la caméra zoome.

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;
// Coin haut-gauche de l'écran et taille de l'écran, en coordonnées du monde.
uniform vec2 view_origin;
uniform vec2 view_size = vec2(960.0, 540.0);
uniform vec2 origin;
uniform float radius = 0.0;
uniform float seed = 0.0;
uniform float front_width = 14.0;
uniform float ragged = 18.0;
uniform vec4 wash_color : source_color = vec4(0.961, 0.941, 0.922, 1.0);
uniform vec4 front_color : source_color = vec4(0.290, 0.188, 0.400, 1.0);
uniform float wash_amount : hint_range(0.0, 1.0) = 0.45;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233)) + seed) * 43758.5453);
}

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

// Contour irrégulier : bruit de valeur sur l'angle, bouclé sur le tour complet.
float lobes(vec2 d) {
	const float count = 11.0;
	float t = (atan(d.y, d.x) / 6.2831853 + 0.5) * count;
	float i = floor(t);
	float a = hash(vec2(mod(i, count), 3.0));
	float b = hash(vec2(mod(i + 1.0, count), 3.0));
	return mix(a, b, smoothstep(0.0, 1.0, fract(t))) - 0.5;
}

void fragment() {
	vec4 screen = texture(screen_texture, SCREEN_UV);
	vec2 world = view_origin + SCREEN_UV * view_size;
	vec2 cell = floor(world);
	ivec2 grid = ivec2(cell);
	vec2 d = cell + 0.5 - origin;
	float dist = length(d) + lobes(d) * ragged;

	// 1 loin derrière le front, 0 devant ; la bande intermédiaire est tramée.
	float depth = clamp((radius - dist) / front_width, 0.0, 1.0);
	float threshold = bayer4(grid);
	float erased = step(threshold, depth) * step(0.001, depth);
	float front = erased * (1.0 - step(0.999, depth)) * step(0.5, hash(cell));

	float luma = dot(screen.rgb, vec3(0.299, 0.587, 0.114));
	vec3 faded = mix(vec3(luma), wash_color.rgb, wash_amount);
	vec3 color = mix(screen.rgb, faded, erased);
	color = mix(color, front_color.rgb, front);
	COLOR = vec4(color, 1.0);
}
