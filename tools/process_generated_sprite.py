import sys
import os
from PIL import Image

def color_dist(c1, c2):
    return (c1[0]-c2[0])**2 + (c1[1]-c2[1])**2 + (c1[2]-c2[2])**2

# Palette RGB values selon la charte
palette_colors = [
    (26, 26, 46, 255),    # Noir profond (contour)
    (22, 33, 62, 255),    # Noir bleuté
    (58, 53, 53, 255),    # Gris chaud foncé
    (107, 97, 97, 255),   # Gris chaud
    (158, 148, 148, 255), # Gris clair
    (232, 224, 212, 255), # Blanc cassé
    (212, 168, 67, 255),  # Or Foyer
    (224, 123, 57, 255),  # Orange
    (196, 67, 43, 255),   # Rouge sang
    (58, 90, 48, 255),    # Vert forêt (Traqueur base)
    (196, 180, 144, 255), # Beige clair (Traqueur accent)
    (123, 197, 88, 255),  # Vert clair
    (45, 90, 39, 255),    # Vert canopée
    (122, 92, 66, 255),   # Brun terreux
    (74, 55, 40, 255),    # Bois sombre
]

def map_pixel(pixel):
    if pixel[3] < 128:
        return (0, 0, 0, 0)
    
    # Filter white backgrounds
    if pixel[0] > 240 and pixel[1] > 240 and pixel[2] > 240:
        return (0, 0, 0, 0)

    best_color = None
    min_dist = float('inf')
    for pc in palette_colors:
        dist = color_dist(pixel, pc)
        if dist < min_dist:
            min_dist = dist
            best_color = pc
    return best_color

def process_image(img_path, out_path, prev_path):
    img = Image.open(img_path).convert("RGBA")
    
    pixels = img.load()
    width, height = img.size
    
    # Remplacer les fonds presque blancs par du transparent total + recadrer
    min_x, max_x = width, 0
    min_y, max_y = height, 0
    
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if r > 240 and g > 240 and b > 240:
                pixels[x, y] = (0, 0, 0, 0)
                continue
            if a > 128:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y
                
    if min_x > max_x or min_y > max_y:
        print("Image provides no valid content")
        return
        
    crop_img = img.crop((min_x, min_y, max_x + 1, max_y + 1))
    
    # Redimensionnement propre
    cw, ch = crop_img.size
    scale = min(48.0 / cw, 64.0 / ch)
    new_w = max(1, int(cw * scale))
    new_h = max(1, int(ch * scale))
    
    scaled_img = crop_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
    
    # Augmenter un peu le contraste pour éviter les contours baveux
    from PIL import ImageEnhance
    enhancer = ImageEnhance.Contrast(scaled_img)
    scaled_img = enhancer.enhance(1.5)
    
    # Enforce palette logic on a 48x64 canvas
    final_img = Image.new("RGBA", (48, 64), (0, 0, 0, 0))
    offset_x = (48 - new_w) // 2
    offset_y = 64 - new_h # Align to bottom
    final_img.paste(scaled_img, (offset_x, offset_y))
    
    out_pixels = final_img.load()
    
    # Ajouter le contour "Sel-out" manuellement pour plus de lisibilité
    for y in range(64):
        for x in range(48):
            out_pixels[x, y] = map_pixel(out_pixels[x, y])
            
    # Ajouter un contour foncé si le pixel est adjacent à un bord transparent (optionnel, mais aide à la lisibilité)
    temp_img = final_img.copy()
    temp_pixels = temp_img.load()
    
    for y in range(64):
        for x in range(48):
            if out_pixels[x, y][3] == 0:
                # check neighbors
                neighbors = []
                if x > 0: neighbors.append(temp_pixels[x-1, y])
                if x < 47: neighbors.append(temp_pixels[x+1, y])
                if y > 0: neighbors.append(temp_pixels[x, y-1])
                if y < 63: neighbors.append(temp_pixels[x, y+1])
                
                has_solid_neighbor = any(n[3] > 0 for n in neighbors)
                if has_solid_neighbor:
                    out_pixels[x, y] = (26, 26, 46, 255) # contour noir profond
                    
    # Re-clean any pure-transparent that were wrongly replaced? 
    # Non, le contour outline s'ajoute SUR le transparent if near solid.
            
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    final_img.save(out_path)
    
    preview_img = final_img.resize((48*4, 64*4), Image.Resampling.NEAREST)
    preview_img.save(prev_path)
    print(f"Processed and saved to {out_path} and preview to {prev_path}")

if __name__ == "__main__":
    process_image(sys.argv[1], "assets/sprites/char_traqueur_SE_idle_01.png", "assets/sprites/char_traqueur_SE_idle_01_preview.png")
