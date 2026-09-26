shader_type canvas_item;

// Écho de l'oubli (plan 16 O6) : silhouette d'un habitant, blanc-bleuté, tramée comme un souvenir incomplet.
// presence : 0 → 1 à l'apparition, 1 → 0 quand il s'efface ; dissolve : 0 → 1 quand le joueur approche,
// les pixels s'envolent vers le haut.

uniform float presence : hint_range(0.0, 1.0) = 1.0;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;
uniform vec4 light_color : source_color = vec4(0.961, 0.941, 0.922, 1.0);   // Blanc effacement #F5F0EB
uniform vec4 shade_color : source_color = vec4(0.62, 0.71, 0.82, 1.0);      // bleu pâle des souvenirs

float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float bayer4(ivec2 p) {
	int x = p.x & 3;
	int y = p.y & 3;
	int m[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);
	return (float(m[y * 4 + x]) + 0.5) / 16.0;
}

void fragment() {
	vec2 size = 1.0 / TEXTURE_PIXEL_SIZE;
	vec2 texel = floor(UV * size);
	// En se dissolvant, chaque pixel monte d'une hauteur qui lui est propre.
	float rise = dissolve * dissolve * (6.0 + hash(texel) * 14.0);
	vec2 source = texel + vec2(0.0, rise);
	vec4 tex = texture(TEXTURE, (source + 0.5) / size);

	float luminance = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
	vec3 color = luminance > 0.33 ? light_color.rgb : shade_color.rgb;
	// Densité tramée : jamais pleine, les pieds s'estompent, des lignes vacillent.
	float density = presence * (0.72 - (UV.y > 0.7 ? (UV.y - 0.7) * 1.6 : 0.0));
	density *= 1.0 - dissolve;
	float flicker = step(hash(vec2(texel.y, floor(TIME * 7.0))), 0.08);
	bool shown = tex.a > 0.5 && bayer4(ivec2(texel)) < density && flicker < 0.5;
	COLOR = vec4(color, shown ? 0.85 : 0.0);
}
