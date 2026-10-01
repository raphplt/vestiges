shader_type canvas_item;

// Les points de poussière sont déjà dessinés dans la texture native.
// Seule leur luminosité varie : aucun déplacement plein écran, aucune pose qui saute.
void fragment() {
    vec3 pixel = texture(TEXTURE, UV).rgb;
    vec2 cell = floor(UV / TEXTURE_PIXEL_SIZE);
    float dust = step(0.30, max(pixel.r, max(pixel.g, pixel.b)));
    float phase = dot(cell, vec2(0.71, 1.37));
    float shimmer = 0.55 + 0.20 * sin(TIME * 1.2 + phase);
    COLOR.rgb *= mix(1.0, shimmer, dust);
}
