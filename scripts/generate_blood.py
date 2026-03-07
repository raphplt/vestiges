import os
import random
import math
from PIL import Image, ImageDraw

def generate_pixel_splatter(filename, width=32, height=32):
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center_x, center_y = width // 2, height // 2
    
    # Palette Vestiges "Mal Organique"
    base_color = (45, 27, 61, 255)      # #2D1B3D Noir Iridescent
    shift_color = (74, 48, 102, 255)    # #4A3066 Violet Profond
    highlight_color = (46, 224, 165, 255) # #2EE0A5 Vert Acide
    
    # Random splatter centers
    blobs = []
    
    # Main body (smaller, more gathered)
    num_main_blobs = random.randint(4, 6)
    for _ in range(num_main_blobs):
        ang = random.uniform(0, math.pi * 2)
        dist = random.uniform(0, 4)
        x = center_x + math.cos(ang) * dist
        y = center_y + math.sin(ang) * dist
        r = random.uniform(5, 8)
        blobs.append((x, y, r))
        
    # Splashes (tighter grouping)
    num_splashes = random.randint(5, 10)
    for _ in range(num_splashes):
        ang = random.uniform(0, math.pi * 2)
        dist = random.uniform(6, 12)
        x = center_x + math.cos(ang) * dist
        y = center_y + math.sin(ang) * dist
        r = random.uniform(1, 2.5)
        blobs.append((x, y, r))

    # Render blobs
    for y in range(height):
        for x in range(width):
            # Check distance to all blobs
            in_blob = False
            min_dist_to_center = 999
            
            for bx, by, br in blobs:
                dist = math.hypot(x - bx, y - by)
                if dist < br:
                    in_blob = True
                    # Add some noise to the edge
                    if dist > br - 1.5 and random.random() > 0.6:
                        in_blob = False
                if in_blob:
                    min_dist_to_center = min(min_dist_to_center, dist)
                    break
            
            if in_blob:
                # Color assignment based on noise/position to simulate iridescence
                noise_val = math.sin((x + y) * 0.2 + random.random() * 0.5) 
                
                if noise_val > 0.7 and random.random() > 0.5:
                    color = highlight_color
                elif noise_val > 0.0:
                    color = shift_color
                else:
                    color = base_color
                    
                img.putpixel((x, y), color)

    # Ensure directory exists
    os.makedirs(os.path.dirname(filename), exist_ok=True)
    img.save(filename)
    print(f"Generated {filename}")

if __name__ == "__main__":
    generate_pixel_splatter("c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/vfx/vfx_blood_splatter.png")
