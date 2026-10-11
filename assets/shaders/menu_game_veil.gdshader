shader_type canvas_item;

// Fond des menus ouverts en run (plan 37 R1) : le jeu reste visible derrière, flou, assombri et un peu éteint. Le flou se
// lit sur la grille de 480 × 270 du pixel art, pour rester dans la matière du jeu ; les bords cèdent à l'Effacement.
uniform sampler2D screen_texture : hint_screen_texture, filter_linear, repeat_disable;
uniform vec4 dust_color : source_color = vec4(0.9, 0.78, 0.39, 1.0);
uniform float brightness = 0.55;
uniform float saturation = 0.6;
uniform float blur_cells = 1.1;

varying vec4 tint;
void vertex() {
    tint = COLOR;
}

const vec2 GRID = vec2(480.0, 270.0);

float grain(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 cell = floor(SCREEN_UV * GRID);
    vec2 center = (cell + 0.5) / GRID;
    vec2 step_uv = blur_cells / GRID;
    // Deux anneaux de six échantillons autour du centre : un flou doux, constant d'une résolution à l'autre.
    vec3 sum = texture(screen_texture, center).rgb * 2.0;
    float weight = 2.0;
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * 1.0471976;
        vec2 dir = vec2(cos(angle), sin(angle));
        sum += texture(screen_texture, center + dir * step_uv).rgb * 1.5;
        sum += texture(screen_texture, center + dir.yx * vec2(1.0, -1.0) * step_uv * 2.2).rgb;
        weight += 2.5;
    }
    vec3 game = sum / weight;
    float luma = dot(game, vec3(0.299, 0.587, 0.114));
    game = mix(vec3(luma), game, saturation) * brightness;

    // Vignette et éclats rongés aux bords, comme la pierre des menus.
    float edge = max(abs(SCREEN_UV.x - 0.5) * 2.0, abs(SCREEN_UV.y - 0.5) * 2.0);
    float noise = grain(cell);
    float pulse = 0.5 + 0.5 * sin(TIME * 0.65 + noise * 40.0);
    float erased = step(0.86 + pulse * 0.1, edge) * step(0.5, noise);
    game *= 1.0 - smoothstep(0.55, 1.0, edge) * 0.45;
    game = mix(game, vec3(0.025, 0.025, 0.05), erased * 0.8);
    float dust = step(0.998, noise) * step(0.7, edge) * pulse;
    COLOR = tint * vec4(game + dust_color.rgb * dust * 0.4, 1.0);
}
