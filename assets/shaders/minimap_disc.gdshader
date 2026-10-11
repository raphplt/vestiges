shader_type canvas_item;

// Radar rond de la minimap (plan 37 R4) : terrain et repères n'existent que dans le disque. radius ≤ 0 : carte entière,
// rien n'est découpé. Le test se fait en unités du calque, donc au pixel de la minimap.
uniform vec2 center = vec2(56.0, 56.0);
uniform float radius = 56.0;

varying vec2 local;
void vertex() {
    local = VERTEX;
}

void fragment() {
    float outside = radius > 0.0 ? step(radius, distance(local, center)) : 0.0;
    COLOR.a *= 1.0 - outside;
}
