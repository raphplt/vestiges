shader_type canvas_item;

uniform vec4 ray_color : source_color = vec4(0.941176, 0.784314, 0.360784, 1.0);
uniform float rotation_degrees_per_second = 8.0;

// Angle continu à chaque rendu ; seule la grille spatiale est celle du pixel art.
// La texture native fournit les poussières, le shader fait tourner les rayons.
void fragment() {
    vec3 pixel = texture(TEXTURE, UV).rgb;
    vec2 cell = floor(UV / TEXTURE_PIXEL_SIZE);
    vec2 offset = cell - vec2(0.5, 144.0 / 270.0) / TEXTURE_PIXEL_SIZE;
    float angle = atan(offset.y, offset.x);
    float ray = fract(angle / (3.14159265359 / 4.0) + TIME * rotation_degrees_per_second / 45.0);
    vec3 background = vec3(12.0, 12.0, 21.0) / 255.0;
    if (length(offset) >= 24.0) {
        if (ray < 0.20) {
            background = floor(ray_color.rgb * 255.0 * 0.16 + 16.0) / 255.0;
        } else if (ray < 0.28) {
            background = floor(ray_color.rgb * 255.0 * 0.09 + 13.0) / 255.0;
        }
    }
    float dust = step(0.30, max(pixel.r, max(pixel.g, pixel.b)));
    float phase = dot(cell, vec2(0.71, 1.37));
    float shimmer = 0.55 + 0.20 * sin(TIME * 1.2 + phase);
    COLOR.rgb = mix(background, pixel * shimmer, dust);
}
