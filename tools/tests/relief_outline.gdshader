// Contours noirs du bac à sable de la verticalité (plan 60, lot 60.0d), en post-traitement sur la profondeur.
// Posé sur un quadrilatère qui couvre l'écran. Un pixel est sur un contour quand la profondeur y fait un saut ou un pli :
// silhouettes, arêtes des falaises et des toits, bord du vide. Pour qu'un plan vu de biais ne se raye pas, le test porte
// sur une grandeur qui varie en ligne droite sur un plan à l'écran : 1/z en perspective, z en orthographique.
shader_type spatial;
render_mode unshaded, depth_draw_never, depth_test_disabled, cull_disabled, fog_disabled, shadows_disabled, skip_vertex_transform;

uniform sampler2D depth_texture : hint_depth_texture, filter_nearest, repeat_disable;
uniform vec4 line_color : source_color = vec4(0.03, 0.02, 0.05, 1.0);
// Pli de la profondeur au-delà duquel un pixel devient un trait : relatif à 1/z en perspective, en mètres du monde
// (cellules) en orthographique, où la caméra se tient loin et où toutes les profondeurs se ressemblent.
uniform float sensitivity = 0.02;
uniform float ortho_sensitivity = 0.12;
uniform float thickness = 1.0;

void vertex() {
	POSITION = vec4(VERTEX.xy, 0.0, 1.0);
}

float linear_depth(vec2 uv, mat4 inv_projection) {
	float raw = texture(depth_texture, uv).r;
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw * 2.0 - 1.0);
#else
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw);
#endif
	vec4 view = inv_projection * vec4(ndc, 1.0);
	return -view.z / view.w;
}

float planar(vec2 uv, mat4 inv_projection, bool orthographic) {
	float z = linear_depth(uv, inv_projection);
	return orthographic ? z : 1.0 / max(z, 0.0001);
}

void fragment() {
	bool orthographic = PROJECTION_MATRIX[3][3] > 0.5;
	vec2 px = thickness / VIEWPORT_SIZE;
	float c = planar(SCREEN_UV, INV_PROJECTION_MATRIX, orthographic);
	float l = planar(SCREEN_UV - vec2(px.x, 0.0), INV_PROJECTION_MATRIX, orthographic);
	float r = planar(SCREEN_UV + vec2(px.x, 0.0), INV_PROJECTION_MATRIX, orthographic);
	float u = planar(SCREEN_UV - vec2(0.0, px.y), INV_PROJECTION_MATRIX, orthographic);
	float d = planar(SCREEN_UV + vec2(0.0, px.y), INV_PROJECTION_MATRIX, orthographic);
	// Le trait se pose du côté le plus proche de la caméra : sur la silhouette, pas sur le fond derrière elle.
	float nearer = orthographic ? -1.0 : 1.0;
	float bend = max(nearer * (2.0 * c - l - r), nearer * (2.0 * c - u - d));
	float edge = step(orthographic ? ortho_sensitivity : sensitivity * abs(c), bend);
	ALBEDO = line_color.rgb;
	ALPHA = edge * line_color.a;
}
