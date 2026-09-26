shader_type canvas_item;

// Personnage verrouillé du Hub : silhouette blanchie par l'Effacement, pixels qui manquent et reviennent.
uniform vec4 erasure_color : source_color = vec4(0.961, 0.941, 0.922, 1.0); // #F5F0EB blanc effacement
uniform float missing_ratio : hint_range(0.0, 1.0) = 0.35;
uniform float flicker_rate = 3.0;
uniform float opacity : hint_range(0.0, 1.0) = 0.7;

float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec2 cell = floor(UV / TEXTURE_PIXEL_SIZE);
	float epoch = floor(TIME * flicker_rate + hash(cell) * 4.0);
	float gone = step(hash(cell + epoch * 7.13), missing_ratio + (1.0 - UV.y) * 0.2);
	float luminance = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
	COLOR = vec4(mix(erasure_color.rgb * 0.82, erasure_color.rgb, luminance), tex.a * (1.0 - gone) * opacity);
}
