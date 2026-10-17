shader_type canvas_item;

// Traînée du dash (plan 54 G3) : la frame exacte du personnage en silhouette pleine, à sa couleur d'accent.
// Modulate porte la couleur et l'effacement : l'alpha ne rend pas la silhouette translucide, il la trame au pixel
// (Bayer 4×4), comme les fondus de la charte.

varying vec4 tint;

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void vertex() {
	// Modulate seul : dans fragment(), COLOR vaut déjà texture × Modulate.
	tint = COLOR;
}

void fragment() {
	ivec2 texel = ivec2(floor(UV / TEXTURE_PIXEL_SIZE));
	float coverage = texture(TEXTURE, UV).a;
	float kept = step(bayer4(texel), tint.a) * step(0.5, coverage);
	COLOR = vec4(tint.rgb, kept);
}
