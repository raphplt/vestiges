shader_type canvas_item;

// Projectiles du joueur (DECISIONS §53, plan 21 F5) : liseré clair d'un pixel, puis halo à peine visible (allégés le 7 octobre 2026), pour qu'ils se lisent
// sur tous les sols et se distinguent des tirs ennemis, qui restent pleins et colorés.
uniform vec4 rim_color : source_color = vec4(0.96, 0.92, 0.84, 0.45);
uniform vec4 halo_color : source_color = vec4(0.96, 0.92, 0.84, 0.1);

// Modulate seul (opacité réglée par le joueur). En fragment, COLOR vaut déjà texture × Modulate : le liseré, posé
// sur des pixels transparents, y serait multiplié par un alpha nul.
varying vec4 modulate;

void vertex() {
	modulate = COLOR;
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec4 result = tex;
	if (tex.a < 0.1) {
		// Le compilateur headless de Godot ne résout pas TEXTURE passé comme sampler à une fonction.
		vec2 step = TEXTURE_PIXEL_SIZE;
		float near_alpha = texture(TEXTURE, UV + vec2(step.x, 0.0)).a;
		near_alpha = max(near_alpha, texture(TEXTURE, UV - vec2(step.x, 0.0)).a);
		near_alpha = max(near_alpha, texture(TEXTURE, UV + vec2(0.0, step.y)).a);
		near_alpha = max(near_alpha, texture(TEXTURE, UV - vec2(0.0, step.y)).a);
		if (near_alpha > 0.1) {
			result = rim_color;
		} else {
			step *= 2.0;
			float far_alpha = texture(TEXTURE, UV + vec2(step.x, 0.0)).a;
			far_alpha = max(far_alpha, texture(TEXTURE, UV - vec2(step.x, 0.0)).a);
			far_alpha = max(far_alpha, texture(TEXTURE, UV + vec2(0.0, step.y)).a);
			far_alpha = max(far_alpha, texture(TEXTURE, UV - vec2(0.0, step.y)).a);
			if (far_alpha > 0.1) {
				result = halo_color;
			}
		}
	}
	COLOR = result * modulate;
}
