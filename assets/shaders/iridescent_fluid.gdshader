shader_type canvas_item;

// === Iridescent Fluid / Mal Organique ===
// VESTIGES : Le sang noir/violet irisé des Aberrations et créatures.
// Produit un effet "flaque d'huile" en mouvement (chromatic aberration confinée / color shifting).

uniform vec4 base_color : source_color = vec4(0.176, 0.106, 0.239, 1.0); // #2D1B3D Noir Iridescent
uniform vec4 shift_color : source_color = vec4(0.29, 0.18, 0.40, 1.0); // #4A3066 Violet Profond
uniform vec4 highlight_color : source_color = vec4(0.18, 0.88, 0.65, 1.0); // #2EE0A5 Vert-Acide très léger reflet

uniform float speed = 1.0;
uniform float scale_factor = 5.0;
uniform float intensity : hint_range(0.0, 1.0) = 0.8;

// Hash noise basique pour perturber la phase
float hash(vec2 p) {
    p = fract(p * vec2(234.34, 435.345));
    p += dot(p, p + 34.23);
    return fract(p.x * p.y);
}

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // N'affecte que les pixels opaques (pour des flaques ou sprites de sang)
    if (tex_color.a > 0.1) {
        
        // Création d'un pattern "oil slick" avec des ondes sinusoïdales décalées
        vec2 p = UV * scale_factor;
        float time = TIME * speed;
        
        // Ondes horizontales et verticales déphasées
        float wave1 = sin(p.x + time + sin(p.y * 2.1));
        float wave2 = cos(p.y + time * 0.8 + cos(p.x * 1.5));
        
        // Combinaison pour un effet organique (valeur de 0 à 1)
        float value = (wave1 + wave2 + 2.0) * 0.25;
        
        // On module la couleur principale selon la valeur
        vec4 iris_color = mix(base_color, shift_color, value * intensity);
        
        // Ajout d'un point lumineux/highlight acide (le vert des yeux/particules) au centre des ondes
        float highlight_band = smoothstep(0.85, 1.0, value);
        iris_color = mix(iris_color, highlight_color, highlight_band * 0.5 * intensity);
        
        // On mélange avec la luminosité originale du sprite pour préserver du volume
        float lum = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
        iris_color.rgb *= lum * 2.0;
        
        COLOR = vec4(iris_color.rgb, tex_color.a);
    } else {
        COLOR = tex_color;
    }
}
