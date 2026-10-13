shader_type canvas_item;

// Projectiles du joueur (DECISIONS §53, plan 21 F5) : liseré clair d'un pixel, pour qu'ils se lisent sur tous les sols
// et se distinguent des tirs ennemis, qui restent pleins et colorés. Allégé le 7 octobre 2026, puis halo retiré et
// liseré ramené à 0,25 le 13 octobre (plan 25 S1c) : avec le contour du sprite, il épaississait trop la bordure.
uniform vec4 rim_color : source_color = vec4(0.96, 0.92, 0.84, 0.25);

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
		// Seuls les pixels opaques portent le liseré : la traînée de vitesse, semi-transparente, reste nue.
		// Le compilateur headless de Godot ne résout pas TEXTURE passé comme sampler à une fonction.
		vec2 step = TEXTURE_PIXEL_SIZE;
		float near_alpha = texture(TEXTURE, UV + vec2(step.x, 0.0)).a;
		near_alpha = max(near_alpha, texture(TEXTURE, UV - vec2(step.x, 0.0)).a);
		near_alpha = max(near_alpha, texture(TEXTURE, UV + vec2(0.0, step.y)).a);
		near_alpha = max(near_alpha, texture(TEXTURE, UV - vec2(0.0, step.y)).a);
		if (near_alpha > 0.8) {
			result = rim_color;
		}
	}
	COLOR = result * modulate;
}
