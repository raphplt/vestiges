shader_type canvas_item;

// Sel-out coloré : contour teinté, jamais noir pur.
// La couleur du contour est une version sombre de outline_color.
uniform vec4 outline_color : source_color = vec4(0.12, 0.22, 0.1, 1.0);
uniform float outline_width : hint_range(0.0, 3.0) = 1.0;
uniform bool enabled = true;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);

	if (!enabled || outline_width <= 0.0) {
		COLOR = tex;
		return;
	}

	// Si le pixel courant est transparent, vérifier les voisins
	if (tex.a < 0.1) {
		vec2 size = TEXTURE_PIXEL_SIZE * outline_width;

		// Échantillonner les 4 voisins cardinaux
		float a = texture(TEXTURE, UV + vec2(size.x, 0)).a;
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, 0)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(0, -size.y)).a);

		// Aussi les diagonales pour un contour plus lisse
		a = max(a, texture(TEXTURE, UV + vec2(size.x, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(size.x, -size.y)).a);
		a = max(a, texture(TEXTURE, UV + vec2(-size.x, -size.y)).a);

		if (a > 0.1) {
			COLOR = vec4(outline_color.rgb, outline_color.a * a);
		} else {
			COLOR = tex;
		}
	} else {
		COLOR = tex;
	}
}
