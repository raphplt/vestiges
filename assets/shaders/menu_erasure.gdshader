shader_type canvas_item;

uniform vec4 dust_color : source_color = vec4(0.9, 0.78, 0.39, 1.0);

varying vec4 tint;
void vertex() {
    tint = COLOR;
}

// La matière reste fixe ; seuls quelques éclats aux bords cèdent à l'Effacement.
float grain(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 cell = floor(UV * vec2(480.0, 270.0));
    float edge = max(abs(UV.x - 0.5) * 2.0, abs(UV.y - 0.5) * 2.0);
    float noise = grain(cell);
    float pulse = 0.5 + 0.5 * sin(TIME * 0.65 + noise * 40.0);
    float erased = step(0.84 + pulse * 0.12, edge) * step(0.55, noise);
    vec3 stone = texture(TEXTURE, UV).rgb * 0.72;
    stone = mix(stone, vec3(0.025, 0.025, 0.05), erased * 0.65);
    float dust = step(0.998, noise) * step(0.68, edge) * pulse;
    COLOR = tint * vec4(stone + dust_color.rgb * dust * 0.4, 1.0);
}
