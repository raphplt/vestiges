import os
import math
import random
from PIL import Image

def build_shadow_crawler_preview(angle_deg, t=0.0):
    # Size 32x32 for a small crawler
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    
    # Shadow crawler is a fluid, moving puddle of darkness.
    # We will compute a 2D heightmap/density map using Perlin noise.
    
    cx, cy = 16, 20 # Center of the puddle (low on the sprite)
    radius_base = 10.0
    
    # Dark palette
    col_core = (15, 10, 20, 255) # Intense dark void
    col_edge = (35, 20, 45, 180) # Semi-transparent dark purple edge
    col_mid = (25, 15, 30, 230)
    
    # Acid Green Eyes
    eye_core = (180, 255, 100, 255)
    eye_glow = (127, 255, 0, 200)

    # Angle based offset for eyes to match direction
    dx = math.cos(math.radians(angle_deg))
    dy = -math.sin(math.radians(angle_deg)) # Y goes up in math, down in image
    
    # Projection factor for isometric (flattened puddle)
    iso_scale_y = 0.5 

    for y in range(32):
        for x in range(32):
            # Distance from center with isometric squash
            dist_x = x - cx
            dist_y = (y - cy) / iso_scale_y
            dist = math.hypot(dist_x, dist_y)
            
            # Fluid shape using math sin/cos waves
            nx = dist_x * 0.15
            ny = dist_y * 0.15
            nz = t * math.pi * 2.0
            
            # Pseudo-noise
            n = math.sin(nx + nz) * math.cos(ny - nz) + math.sin(nx*2 - ny*1.5 + nz*0.5) * 0.5
            
            # The radius of the puddle varies based on noise
            r = radius_base + n * 3.0
            
            if dist <= r:
                # Blend colors from core to edge based on distance
                t_col = dist / r
                if t_col < 0.5:
                    c = col_core
                elif t_col < 0.8:
                    c = col_mid
                else:
                    c = col_edge
                    
                # A bit of noise in the alpha at the very edge to make it look like dissipating smoke
                if t_col > 0.85:
                    n_edge = math.sin(x*0.4 + nz) * math.cos(y*0.4 - nz)
                    if n_edge > 0.0:
                        c = (c[0], c[1], c[2], 0) # Cut out
                        
                if c[3] > 0:
                    img.putpixel((x, y), c)

    # The only SOLID elements in the "Ombre" are its eyes.
    # So we draw them sharp and glowing above the fluid surface.
    
    # Position eyes based on angle (assuming SE is dx=1, dy=-1)
    # Using isometric projection: +X is right, +Y is down
    # SE: (+,-), SW: (-,-), NE: (+,+), NW: (-,+)
    # However we just use a small rotation circle
    
    eye_spread = 2.5
    eye_height = -2 # Slightly above the center of the puddle
    
    # Convert angle to 2D screen offset
    # 0 = SE, 90 = SW, 180 = NW, 270 = NE
    eye_center_x = cx + math.cos(math.radians(angle_deg - 45)) * 4
    eye_center_y = cy + eye_height + math.sin(math.radians(angle_deg - 45)) * 2 * iso_scale_y
    
    def draw_glowing_eye(ex, ey, size):
        for y in range(int(ey-size-2), int(ey+size+2)):
            for x in range(int(ex-size-2), int(ex+size+2)):
                if 0 <= x < 32 and 0 <= y < 32:
                    d = math.hypot(x-ex, y-ey)
                    if d <= size:
                        img.putpixel((x, y), eye_core)
                    elif d <= size + 1.2:
                        # Blend glow over existing pixel
                        cur = img.getpixel((x, y))
                        if cur[3] == 0:
                            img.putpixel((x, y), eye_glow)
                        else:
                            # Simple additive blend for glow
                            r = min(255, cur[0] + int(eye_glow[0]*0.8))
                            g = min(255, cur[1] + int(eye_glow[1]*0.8))
                            b = min(255, cur[2] + int(eye_glow[2]*0.8))
                            img.putpixel((x, y), (r, g, b, 255))
    
    # Draw a cluster of eyes (asymmetrical, usually 2 or 3)
    draw_glowing_eye(eye_center_x - 1, eye_center_y, 0.8)
    draw_glowing_eye(eye_center_x + 1.5, eye_center_y + 0.5, 1.0)
    # Third tiny eye
    draw_glowing_eye(eye_center_x + 0.5, eye_center_y - 1.5, 0.5)

    # Outline pass (dark sel-out)
    out_img = img.copy()
    w, h = img.size
    for py in range(h):
        for px in range(w):
            _, _, _, a = img.getpixel((px, py))
            if a == 0:
                has_neighbor = False
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    nx, ny = px + dx, py + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        nc = img.getpixel((nx, ny))
                        if nc[3] > 200: # Only outline solid parts
                            has_neighbor = True
                            break
                if has_neighbor:
                    out_img.putpixel((px, py), (10, 5, 15, 255))
                    
    return out_img

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/shadow_crawler"
    os.makedirs(out_dir, exist_ok=True)
    
    # Just generating SE idle frame 00 for validation
    img = build_shadow_crawler_preview(0) # SE
    img.save(os.path.join(out_dir, "enemy_shadow_crawler_SE_idle_00.png"))
    print("Generated preview at:", os.path.join(out_dir, "enemy_shadow_crawler_SE_idle_00.png"))

if __name__ == "__main__":
    main()
