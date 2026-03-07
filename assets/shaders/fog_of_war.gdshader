shader_type canvas_item;

// === Fog of War / Dithering World ===
// VESTIGES : Le brouillard n'est pas noir, c'est l'Effacement. 
// Il utilise un motif de dithering animé blanc-bleuté.

uniform vec4 fog_color : source_color = vec4(0.96, 0.94, 0.92, 1.0); // #F5F0EB - Blanc effacement
uniform float animation_speed = 2.0;
uniform float dither_size = 2.0;

// Texture du masque (Noir = exploré/transparent, Blanc = inexploré/fog)
uniform sampler2D mask_texture : hint_default_black, filter_nearest;

// Hash basique 
float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void fragment() {
    float mask_value = texture(mask_texture, UV).r;
    
    // Si la zone est totalement explorée, on ne trace rien pour sauver des perfs
    if (mask_value < 0.01) {
        COLOR = vec4(0.0);
    } else {
        // Coordonnées de dithering basées sur la position écran pour que le motif 
        // reste stable selon le monde, modifiées par la taille du dither
        vec2 dither_uv = floor(FRAGCOORD.xy / dither_size);
        
        // Création d'un checkerboard animé par le temps
        float dither_pattern = fract(sin(dot(dither_uv + floor(TIME * animation_speed), vec2(12.9898, 78.233))) * 43758.5453);
        
        // Mélanger le pattern avec le masque (bords en dégradé)
        float threshold = 1.0 - mask_value;
        
        // Si le dithering pattern est au-dessus du seuil du masque, on affiche le brouillard
        if (dither_pattern > threshold) {
            COLOR = fog_color;
        } else {
            COLOR = vec4(0.0);
        }
    }
}
