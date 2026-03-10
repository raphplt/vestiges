shader_type canvas_item;

// Mode : 0 = off, 1 = protanopia, 2 = deuteranopia, 3 = tritanopia
uniform int mode : hint_range(0, 3) = 0;
// Intensité du filtre (0 = normal, 1 = simulation complète)
uniform float intensity : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec4 c = texture(TEXTURE, UV);

	if (mode == 0) {
		COLOR = c;
		return;
	}

	// Matrices de simulation daltonisme (Machado 2009)
	mat3 transform;

	if (mode == 1) {
		// Protanopia (rouge)
		transform = mat3(
			vec3(0.152286, 0.114503, -0.003882),
			vec3(1.052583, 0.786281, -0.048116),
			vec3(-0.204868, 0.099216, 1.051998)
		);
	} else if (mode == 2) {
		// Deuteranopia (vert)
		transform = mat3(
			vec3(0.367322, 0.280085, -0.011820),
			vec3(0.860646, 0.672501, 0.042940),
			vec3(-0.227968, 0.047413, 0.968881)
		);
	} else {
		// Tritanopia (bleu)
		transform = mat3(
			vec3(1.255528, -0.078411, 0.004733),
			vec3(-0.076749, 0.930809, 0.691367),
			vec3(-0.178779, 0.147602, 0.303900)
		);
	}

	vec3 cb = transform * c.rgb;
	COLOR = vec4(mix(c.rgb, cb, intensity), c.a);
}
