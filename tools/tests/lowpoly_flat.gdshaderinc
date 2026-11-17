// Décor qui cache le joueur (see_through, décors seulement) : ses fragments pris dans un cylindre de la caméra au
// joueur s'effacent en trame, du bord vers l'axe. Le cylindre suit la vue, pas la lumière : à la passe d'ombre (sa
// caméra n'est pas celle de la vue), le décor reste plein et son ombre ne se troue pas.
uniform bool see_through = false;
uniform vec3 view_camera;
uniform vec3 view_target;
uniform float see_radius = 1.4;
uniform float see_strength = 0.8;

float bayer4(vec2 pixel) {
	ivec2 p = ivec2(mod(pixel, 4.0));
	int index = p.x + p.y * 4;
	int pattern[16] = int[](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(pattern[index]) + 0.5) / 16.0;
}

void fragment() {
	// Le sens du produit dépend de la convention d'axes de l'image : on tourne la face vers la caméra.
	vec3 face = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
	NORMAL = dot(face, VERTEX) > 0.0 ? -face : face;
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 1.0;
	if (see_through && distance(CAMERA_POSITION_WORLD, view_camera) < 0.05) {
		vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
		vec3 axis = view_target - view_camera;
		float span = length(axis);
		float along = dot(world - view_camera, axis) / max(span * span, 1e-4);
		float off_axis = length(world - (view_camera + axis * along));
		// Seulement devant le joueur, et plein effacement à l'intérieur du cylindre, adouci sur son dernier tiers.
		float fade = (1.0 - smoothstep(see_radius * 0.65, see_radius, off_axis)) * step(along, 1.0 - 0.6 / max(span, 0.6));
		if (bayer4(FRAGCOORD.xy * 0.5) < fade * see_strength) {
			discard;
		}
	}
}
