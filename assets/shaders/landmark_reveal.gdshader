shader_type canvas_item;

// Réveil d'un lieu (plan 24 B2) : le monde figé se pixelise par gros pixels du monde, puis ses bords perdent leurs
// couleurs autour du lieu ; enfin la trame recule jusqu'aux bords (l'oubli recule), derrière un front à la couleur
// du lieu. Tramage ordonné calculé en pixels du monde (charte §5) : la trame ne glisse pas quand la caméra bouge.

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;
// Coin haut-gauche de l'écran et taille de l'écran, en coordonnées du monde.
uniform vec2 view_origin;
uniform vec2 view_size = vec2(960.0, 540.0);
uniform vec2 origin;
// Côté d'un gros pixel en pixels du monde ; 1 = image intacte.
uniform float block = 1.0;
// Rayon de la zone gardée en couleur autour du lieu, en pixels du monde.
uniform float clear_radius = 80.0;
uniform float front_width = 48.0;
uniform float fade_amount : hint_range(0.0, 1.0) = 0.0;
uniform float front_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 fade_color : source_color = vec4(0.29, 0.31, 0.36, 1.0);
uniform vec4 front_color : source_color = vec4(0.62, 0.9, 0.95, 1.0);

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void fragment() {
	vec2 world = view_origin + SCREEN_UV * view_size;
	vec2 sample_world = world;
	if (block > 1.0)
		sample_world = (floor(world / block) + 0.5) * block;
	vec2 uv = clamp((sample_world - view_origin) / view_size, vec2(0.0), vec2(1.0));
	vec3 screen = texture(screen_texture, uv).rgb;

	// Trame calculée sur la grille des gros pixels : pendant la pixelisation, elle garde leur taille.
	vec2 cell = floor(sample_world / max(block, 1.0));
	float dist = length(sample_world - origin);
	// 0 dans la zone gardée, 1 au-delà de la bande du front.
	float depth = clamp((dist - clear_radius) / front_width, 0.0, 1.0);
	float threshold = bayer4(ivec2(cell));
	float faded = step(threshold, depth * fade_amount) * step(0.001, depth * fade_amount);
	// Le front : un liseré tramé clair au bord intérieur de la bande, seulement quand l'oubli recule. Clairsemé, il
	// dessine le bord sans couvrir le monde.
	float band = clamp(1.0 - abs(depth - 0.2) / 0.2, 0.0, 1.0);
	float front_density = band * band * front_amount * 0.55;
	float front = step(threshold, front_density) * step(0.001, front_density) * step(0.001, fade_amount);

	float luma = dot(screen, vec3(0.299, 0.587, 0.114));
	vec3 grey = mix(vec3(luma) * 0.82, fade_color.rgb, 0.18);
	vec3 color = mix(screen, grey, faded);
	color = mix(color, front_color.rgb, front);
	COLOR = vec4(color, 1.0);
}
