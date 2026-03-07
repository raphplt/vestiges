import os
import math
import random
from PIL import Image

def build_shadow_crawler_frame(angle_deg, t=0.0, anim="idle"):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    
    cx, cy = 16, 20
    radius_base = 10.0
    
    # Shadow palettes
    col_core = (15, 10, 20, 255) 
    col_edge = (35, 20, 45, 180) 
    col_mid = (25, 15, 30, 230)
    
    # Eye palettes
    eye_core = (180, 255, 100, 255)
    eye_glow = (127, 255, 0, 200)

    # Animation modifiers
    anim_scale = 1.0
    anim_offset_y = 0.0
    anim_offset_x = 0.0
    eye_size_mod = 0.0
    
    if anim == "walk":
        # Pulses more aggressively while walking
        radius_base = 10.0 + math.sin(t * math.pi * 2) * 1.5
    elif anim == "attack":
        # Surges forward and expands then contracts quickly
        if t < 0.3:
            surge = (t / 0.3)
            radius_base = 10.0 + surge * 3.0
            anim_offset_x = math.cos(math.radians(angle_deg-45)) * surge * 5
            anim_offset_y = math.sin(math.radians(angle_deg-45)) * surge * 2
            eye_size_mod = surge * 0.5
        else:
            recede = 1.0 - ((t - 0.3) / 0.7)
            radius_base = 10.0 + recede * 3.0
            anim_offset_x = math.cos(math.radians(angle_deg-45)) * recede * 5
            anim_offset_y = math.sin(math.radians(angle_deg-45)) * recede * 2
            eye_size_mod = recede * 0.5
    elif anim == "death":
        # Shrinks and fades into nothing
        anim_scale = max(0.01, 1.0 - t * 1.5)
        radius_base = 10.0 * anim_scale
        eye_size_mod = -t * 2.0

    iso_scale_y = 0.5 

    for y in range(32):
        for x in range(32):
            dist_x = x - (cx + anim_offset_x)
            dist_y = (y - (cy + anim_offset_y)) / iso_scale_y
            dist = math.hypot(dist_x, dist_y)
            
            # Fluid shape using math sin/cos waves
            nx = dist_x * 0.15
            ny = dist_y * 0.15
            # Faster wave if walking or attacking
            nz = t * math.pi * (4.0 if anim in ("walk", "attack") else 2.0)
            
            n = math.sin(nx + nz) * math.cos(ny - nz) + math.sin(nx*2 - ny*1.5 + nz*0.5) * 0.5
            
            r = radius_base + n * 3.0 * anim_scale
            
            if dist <= r and anim_scale > 0:
                t_col = dist / max(0.1, r)
                if t_col < 0.5:
                    c = col_core
                elif t_col < 0.8:
                    c = col_mid
                else:
                    c = col_edge
                    
                if t_col > 0.85:
                    n_edge = math.sin(x*0.4 + nz*2) * math.cos(y*0.4 - nz*2)
                    if n_edge > 0.0:
                        c = (c[0], c[1], c[2], 0) 
                        
                # Fade out on death
                if anim == "death":
                    alpha = int(c[3] * max(0, 1.0 - t))
                    c = (c[0], c[1], c[2], alpha)
                        
                if c[3] > 0:
                    img.putpixel((x, y), c)

    # Eyes
    if anim != "death" or t < 0.5: # Eyes disappear halfway through death
        eye_spread = 2.5
        eye_height = -2 
        
        eye_center_x = cx + anim_offset_x + math.cos(math.radians(angle_deg - 45)) * 4
        eye_center_y = cy + anim_offset_y + eye_height + math.sin(math.radians(angle_deg - 45)) * 2 * iso_scale_y
        
        def draw_glowing_eye(ex, ey, base_size):
            size = max(0.1, base_size + eye_size_mod)
            for y in range(int(ey-size-2), int(ey+size+2)):
                for x in range(int(ex-size-2), int(ex+size+2)):
                    if 0 <= x < 32 and 0 <= y < 32:
                        d = math.hypot(x-ex, y-ey)
                        if d <= size:
                            img.putpixel((x, y), eye_core)
                        elif d <= size + 1.2:
                            cur = img.getpixel((x, y))
                            if cur[3] == 0:
                                img.putpixel((x, y), eye_glow)
                            else:
                                r = min(255, cur[0] + int(eye_glow[0]*0.8))
                                g = min(255, cur[1] + int(eye_glow[1]*0.8))
                                b = min(255, cur[2] + int(eye_glow[2]*0.8))
                                img.putpixel((x, y), (r, g, b, 255))
        
        draw_glowing_eye(eye_center_x - 1, eye_center_y, 0.8)
        draw_glowing_eye(eye_center_x + 1.5, eye_center_y + 0.5, 1.0)
        draw_glowing_eye(eye_center_x + 0.5, eye_center_y - 1.5, 0.5)

    # Outline pass
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
                        if nc[3] > 200: 
                            has_neighbor = True
                            break
                if has_neighbor and (anim != "death" or t < 0.8):
                    out_img.putpixel((px, py), (10, 5, 15, 255))
                    
    return out_img

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/shadow_crawler"
    os.makedirs(out_dir, exist_ok=True)
    
    dirs = {
        "SE": 0,
        "NE": 270,
        "NW": 180,
        "SW": 90
    }
    
    anims = {
        "idle": 6,   # Fluid puddle breathing
        "walk": 6,   # Pulsing/sliding movement
        "attack": 6, # Surging forward
        "death": 6   # Dissipating
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (32 * frames, 32), (0, 0, 0, 0))
            for f in range(frames):
                t = f / float(frames)
                img = build_shadow_crawler_frame(angle, t, anim)
                
                filename = f"enemy_shadow_crawler_{d_name}_{anim}_{f:02d}.png"
                img.save(os.path.join(out_dir, filename))
                
                sheet.paste(img, (f * 32, 0))
                
            sheet_name = f"enemy_shadow_crawler_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
