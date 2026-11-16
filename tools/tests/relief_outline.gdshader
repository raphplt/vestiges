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

// Pli de la profondeur autour de uv, mesuré à `reach` pixels de part et d'autre, en fraction du seuil.
float crease(vec2 uv, vec2 reach, float c, mat4 inv_projection, bool orthographic) {
	float l = planar(uv - vec2(reach.x, 0.0), inv_projection, orthographic);
	float r = planar(uv + vec2(reach.x, 0.0), inv_projection, orthographic);
	float u = planar(uv - vec2(0.0, reach.y), inv_projection, orthographic);
	float d = planar(uv + vec2(0.0, reach.y), inv_projection, orthographic);
	// Le trait se pose du côté le plus proche de la caméra : sur la silhouette, pas sur le fond derrière elle.
	float nearer = orthographic ? -1.0 : 1.0;
	float bend = max(nearer * (2.0 * c - l - r), nearer * (2.0 * c - u - d));
	return bend / (orthographic ? ortho_sensitivity : sensitivity * abs(c));
}

void fragment() {
	bool orthographic = PROJECTION_MATRIX[3][3] > 0.5;
	vec2 px = thickness / VIEWPORT_SIZE;
	float c = planar(SCREEN_UV, INV_PROJECTION_MATRIX, orthographic);
	// Anticrénelage (§238) : le test se fait à quatre portées, et le pixel prend la part de celles qui le voient sur le
	// trait. Le cœur du trait est plein, ses bords s'estompent : le trait n'a plus de marches d'un pixel franc. Le seuil
	// lui-même est adouci, pour que les plis à peine marqués ne clignotent pas.
	float edge = 0.0;
	for (int k = 0; k < 4; k++) {
		float reach = 0.4 + 0.3 * float(k);
		edge += smoothstep(0.7, 1.4, crease(SCREEN_UV, px * reach, c, INV_PROJECTION_MATRIX, orthographic));
	}
	ALBEDO = line_color.rgb;
	ALPHA = edge * 0.25 * line_color.a;
}
