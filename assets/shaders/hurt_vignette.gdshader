shader_type canvas_item;

// Vignette de blessure du joueur (plan 27 V3a, Vestiges.UI.HurtVignette) : bords de l'écran tramés en damier,
// plus épais du côté d'où vient le coup. Paramètres écrits par image seulement pendant l'effacement.
uniform vec4 tint : source_color = vec4(0.77, 0.26, 0.17, 1.0);
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float thickness : hint_range(0.0, 0.5) = 0.16;
// Côté du coup, à l'écran (nul : tous les bords pareils) et part de l'épaisseur qui s'y ajoute.
uniform vec2 side = vec2(0.0);
uniform float side_bias : hint_range(0.0, 1.0) = 0.65;
// Taille d'un pixel de trame, en pixels d'écran.
uniform float cell = 3.0;

void fragment() {
	vec2 uv = SCREEN_UV;
	// Distance au bord le plus proche, en part de l'écran.
	float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
	// Le bord tourné vers le coup s'épaissit.
	vec2 centered = uv - vec2(0.5);
	float facing = length(side) > 0.01 ? max(dot(normalize(centered), normalize(side)), 0.0) : 0.0;
	float width = thickness * (1.0 + side_bias * facing * 1.5) * intensity;
	float depth = width > 0.0 ? clamp(1.0 - edge / width, 0.0, 1.0) : 0.0;
	// Trame à trois niveaux : plein contre le bord, un pixel sur deux, puis un sur quatre.
	vec2 px = floor(FRAGCOORD.xy / cell);
	bool checker = mod(px.x + px.y, 2.0) < 1.0;
	bool sparse = mod(px.x, 2.0) < 1.0 && mod(px.y, 2.0) < 1.0;
	float alpha = depth > 0.7 ? 1.0 : (depth > 0.35 ? (checker ? 1.0 : 0.0) : (depth > 0.0 && sparse ? 1.0 : 0.0));
	COLOR = vec4(tint.rgb, alpha * intensity * tint.a);
}
