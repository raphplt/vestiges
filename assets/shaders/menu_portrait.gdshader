shader_type canvas_item;
render_mode unshaded;
// Le portrait rejoint la matière du menu par une tranche de pixels irrégulière.
varying vec4 tint;
void vertex() {
    tint = COLOR;
}
void fragment() {
    vec2 grid = floor(UV * 180.0);
    vec2 uv = (floor(UV * 360.0) + 0.5) / 360.0;
    vec4 art = texture(TEXTURE, uv);
    float grain = fract(sin(dot(grid, vec2(17.37, 41.71))) * 12437.51);
    float edge = min(min(UV.x, 1.0 - UV.x), min(UV.y, 1.0 - UV.y));
    float coverage = smoothstep(0.005, 0.09, edge);
    art.a *= step(grain, coverage);
    COLOR = tint * art;
}
