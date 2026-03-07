import os
import math
import random
from PIL import Image

def create_volume():
    return [[[0]*32 for _ in range(32)] for __ in range(32)]

def draw_sphere(vol, cx, cy, cz, r, color):
    min_x = max(0, int(cx - r))
    max_x = min(31, int(cx + r))
    min_y = max(0, int(cy - r))
    max_y = min(31, int(cy + r))
    min_z = max(0, int(cz - r))
    max_z = min(31, int(cz + r))
    r_sq = r * r
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                if (x - cx)**2 + (y - cy)**2 + (z - cz)**2 <= r_sq:
                    # Don't overwrite eyes (2) or iridescent fluid (4) unless necessary
                    if color == 2:
                        vol[x][y][z] = 2
                    elif vol[x][y][z] not in (2, 4):
                        vol[x][y][z] = color

def render_volume(vol):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    depth_buf = [[-9999]*32 for _ in range(32)]
    
    for z in range(32):
        for y in range(32):
            for x in range(32):
                col = vol[x][y][z]
                if col == 0: continue
                
                sx = int(16 + x - y)
                sy = int(6 + (x + y) / 2.0 - z)
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 32 and 0 <= sy < 32:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 31 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        # Palettes
                        if col == 1: # Flesh (Base #5A4A3A)
                            if top_lit: c = (110, 90, 75, 255)
                            elif side_lit: c = (90, 74, 58, 255)
                            else: c = (50, 40, 30, 255) 
                        elif col == 2: # Eyes (Acid Green #7FFF00)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                        elif col == 3: # Stone / Armor plates
                            if top_lit: c = (120, 120, 125, 255)
                            elif side_lit: c = (80, 80, 85, 255)
                            else: c = (40, 40, 45, 255)
                        elif col == 4: # Iridescent fluid / Death
                            c = (45, 27, 61, 255) # #2D1B3D
                        
                        img.putpixel((sx, sy), c)
                        
    # Post-process outline (sel-out)
    out_img = img.copy()
    w, h = img.size
    for py in range(h):
        for px in range(w):
            _, _, _, a = img.getpixel((px, py))
            if a == 0:
                neighbors = []
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    nx, ny = px + dx, py + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        nc = img.getpixel((nx, ny))
                        if nc[3] > 0:
                            neighbors.append(nc)
                if neighbors:
                    base = neighbors[0]
                    outline_color = (int(base[0]*0.3), int(base[1]*0.3), int(base[2]*0.3), 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_frame(t, anim, angle_deg):
    vol = create_volume()
    cx, cy, cz = 16, 16, 8
    angle = math.radians(angle_deg)
    
    death_scale = 1.0
    random.seed(42 + int(t * 10))
    
    # Rampant is a segmented slithering creature (like a centipede/worm)
    segments = 5
    seg_pos = [(0,0,0)] * segments
    
    rear_up = 0.0 # For attack
    
    if anim == "idle":
        # Low breathing
        for i in range(segments):
            breath = math.sin(t * math.pi * 2 - i * 0.5) * 0.3
            seg_pos[i] = (i * -2.5, 0, breath)
    elif anim == "walk":
        # Slithering undulation
        for i in range(segments):
            wave = math.sin(t * math.pi * 4 - i * 1.2) * 1.5
            lat_wave = math.cos(t * math.pi * 4 - i * 1.2) * 1.0
            seg_pos[i] = (i * -2.5 + math.cos(t * math.pi * 4)*0.5, lat_wave, max(0, wave))
    elif anim == "attack":
        # Rearing up
        if t <= 0.2:
            rear_up = (t / 0.2)
        elif t <= 0.5:
            rear_up = 1.0
        else:
            p = (t - 0.5) / 0.5
            rear_up = 1.0 * (1 - p)
            
        for i in range(segments):
            if i == 0:
                seg_pos[i] = (0, 0, rear_up * 5)
            elif i == 1:
                seg_pos[i] = (-2, 0, rear_up * 2.5)
            else:
                seg_pos[i] = (i * -2.5 + rear_up * 1, 0, 0)
    elif anim == "death":
        death_scale = max(0.1, 1.0 - t)
        for i in range(segments):
            seg_pos[i] = (i * -2.5, 0, -5 * t)

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            color = 4 
            if t > 0.1:
                bx += (random.random() - 0.5) * t * 15
                by += (random.random() - 0.5) * t * 15
                r = max(1, r - t * 3)
        
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz * death_scale
        draw_sphere(vol, nx, ny, nz, r, color)

    # Draw segments
    for i in range(segments):
        bx, by, bz = seg_pos[i]
        size = 3.5 - (i * 0.4) # Tapers off
        
        # Flesh body
        add_sphere(bx, by, bz, size, 1)
        
        # Stone plates on top (Armor)
        if anim != "death" or t < 0.2:
            add_sphere(bx - 0.5, by, bz + size * 0.6, size * 0.8, 3)
            
        # Tiny legs on the sides
        if anim != "death" or t < 0.4:
            leg_phase = (t * math.pi * 8 + i * 1.5)
            leg_lift = max(0, math.sin(leg_phase)) * 1.5
            leg_swing = math.cos(leg_phase) * 1.0
            if anim == "idle" or (anim == "attack" and i < 2): 
                leg_lift = 0
                leg_swing = 0
                
            # Right leg
            add_sphere(bx + leg_swing, by + size, bz - 1 + leg_lift - (rear_up * 2 if i<2 else 0), 1.0, 3)
            # Left leg
            add_sphere(bx - leg_swing, by - size, bz - 1 + leg_lift - (rear_up * 2 if i<2 else 0), 1.0, 3)

    # Head and Eyes (Asymmetric)
    hx, hy, hz = seg_pos[0]
    
    if anim != "death" or t < 0.3:
        # Multiple glowing eyes, mostly clustered on the front/sides
        # Acid green glowing eyes
        eye_base_z = hz + 1 + rear_up * 0.5
        add_sphere(hx + 2.5, hy + 1.2, eye_base_z + 0.5, 0.8, 2)
        add_sphere(hx + 3.0, hy - 0.8, eye_base_z, 1.0, 2)
        add_sphere(hx + 1.5, hy - 1.5, eye_base_z + 1.0, 0.7, 2)
        add_sphere(hx + 2.0, hy + 2.0, eye_base_z - 0.5, 0.6, 2)
        
        # Hideous mandibles (stone colored)
        if anim == "attack" and 0.2 < t < 0.6:
            add_sphere(hx + 4, hy + 1.5, hz - 0.5, 1.2, 3)
            add_sphere(hx + 4, hy - 1.5, hz - 0.5, 1.2, 3)
        else:
            add_sphere(hx + 3, hy + 1.0, hz - 1.0, 0.8, 3)
            add_sphere(hx + 3, hy - 1.0, hz - 1.0, 0.8, 3)

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/rampant"
    os.makedirs(out_dir, exist_ok=True)
    
    dirs = {
        "SE": 0,
        "NE": 270,
        "NW": 180,
        "SW": 90
    }
    
    anims = {
        "idle": 4,
        "walk": 6,   # Needs to be smooth slithering
        "attack": 5, 
        "death": 5
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (32 * frames, 32), (0, 0, 0, 0))
            for f in range(frames):
                # Using fractional t
                t = f / float(frames)
                img = build_frame(t, anim, angle)
                
                filename = f"enemy_rampant_{d_name}_{anim}_{f:02d}.png"
                img.save(os.path.join(out_dir, filename))
                
                sheet.paste(img, (f * 32, 0))
                
            sheet_name = f"enemy_rampant_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
