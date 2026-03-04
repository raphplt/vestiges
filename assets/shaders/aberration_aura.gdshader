shader_type canvas_item;

// Aura d'aberration : halo sombre pulsant autour du sprite.
// Appliqué comme second pass via SubViewport ou en tant que shader sur le sprite.
uniform vec4 aura_color : source_color = vec4(0.15, 0.05, 0.2, 0.4);
uniform float pulse_speed : hint_range(0.5, 5.0) = 2.0;
uniform float aura_size : hint_range(1.0, 5.0) = 2.0;
uniform float intensity : hint_range(0.0, 1.0) = 0.6;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec2 size = TEXTURE_PIXEL_SIZE * aura_size;

	// Pulse sinusoïdale
	float pulse = 0.7 + 0.3 * sin(TIME * pulse_speed);
	float effective_size = aura_size * pulse;
	vec2 psize = TEXTURE_PIXEL_SIZE * effective_size;

	if (tex.a < 0.1) {
		// Vérifier les voisins dans un rayon plus large
		float a = 0.0;
		for (float i = -1.0; i <= 1.0; i += 1.0) {
			for (float j = -1.0; j <= 1.0; j += 1.0) {
				if (i == 0.0 && j == 0.0) continue;
				a = max(a, texture(TEXTURE, UV + vec2(i, j) * psize).a);
			}
		}

		if (a > 0.1) {
			float aura_alpha = aura_color.a * intensity * pulse * a;
			COLOR = vec4(aura_color.rgb, aura_alpha);
		} else {
			COLOR = tex;
		}
	} else {
		// Légère teinte sur le sprite lui-même
		float tint = intensity * 0.15 * pulse;
		COLOR = vec4(mix(tex.rgb, aura_color.rgb, tint), tex.a);
	}
}
