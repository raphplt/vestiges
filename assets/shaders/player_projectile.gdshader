shader_type canvas_item;

// Projectiles du joueur (DECISIONS §53, plan 21 F5) : liseré clair d'un pixel, puis halo léger, pour qu'ils se lisent
// sur tous les sols et se distinguent des tirs ennemis, qui restent pleins et colorés.
uniform vec4 rim_color : source_color = vec4(0.96, 0.92, 0.84, 0.9);
uniform vec4 halo_color : source_color = vec4(0.96, 0.92, 0.84, 0.28);

float neighbour_alpha(sampler2D tex, vec2 uv, vec2 step) {
	float a = texture(tex, uv + vec2(step.x, 0.0)).a;
	a = max(a, texture(tex, uv - vec2(step.x, 0.0)).a);
	a = max(a, texture(tex, uv + vec2(0.0, step.y)).a);
	return max(a, texture(tex, uv - vec2(0.0, step.y)).a);
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec4 result = tex;
	if (tex.a < 0.1) {
		if (neighbour_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE) > 0.1) {
			result = rim_color;
		} else if (neighbour_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE * 2.0) > 0.1) {
			result = halo_color;
		}
	}
	COLOR = result * COLOR;
}
