import os
import math
import random
from PIL import Image

def create_volume():
    return [[[0]*32 for _ in range(32)] for __ in range(32)]

def draw_sphere(vol, cx, cy, cz, r, color):
    # bounding box
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
                    if color == 2:
                        vol[x][y][z] = 2
                    elif vol[x][y][z] not in (2, 4): # don't overwrite eye or fluid
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
                        
                        # Palettes
                        if col == 1:
                            if top_lit: c = (85, 136, 68, 255) # Light Moss
                            elif front_x and front_y: c = (52, 76, 37, 255) # Base Body
                            else: c = (27, 38, 20, 255) # Shadow
                        elif col == 2:
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255) # Acid Green
                        elif col == 3:
                            if top_lit: c = (138, 146, 130, 255) # Bone lit
                            else: c = (75, 82, 70, 255) # Bone shadow
                        elif col == 4:
                            c = (45, 27, 61, 255) # Iridescent liquid
                        
                        # Apply outline by checking neighbors in 2D
                        # We will do outline as a post-process
                        img.putpixel((sx, sy), c)
                        
    # Post-process outline (sel-out)
    out_img = img.copy()
    w, h = img.size
    for py in range(h):
        for px in range(w):
            _, _, _, a = img.getpixel((px, py))
            if a == 0:
                # check neighbors
                neighbors = []
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    nx, ny = px + dx, py + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        nc = img.getpixel((nx, ny))
                        if nc[3] > 0:
                            neighbors.append(nc)
                if neighbors:
                    # found a solid pixel nearby, create a dark outline based on the neighbor color
                    base = neighbors[0]
                    # darken it significantly, but not pure black
                    outline_color = (int(base[0]*0.4), int(base[1]*0.4), int(base[2]*0.4), 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_frame(t, anim, angle_deg):
    vol = create_volume()
    cx, cy, cz = 16, 16, 8
    angle = math.radians(angle_deg)
    
    body_dz = 0
    body_dx = 0
    legs_dz = [0]*6
    legs_dx = [0]*6
    death_scale = 1.0
    random.seed(42 + int(t * 10)) # consistent randomness for particles
    
    if anim == "idle":
        body_dz = math.sin(t * math.pi * 2) * 0.5
    elif anim == "walk":
        body_dz = math.sin(t * math.pi * 4) * 0.5
        phases = [0.0, 0.5, 0.33, 0.83, 0.66, 0.16]
        for i in range(6):
            p = (t + phases[i]) % 1.0
            legs_dx[i] = math.sin(p * math.pi * 2) * 2.5
            legs_dz[i] = max(0, math.sin(p * math.pi * 2)) * 2.5
    elif anim == "attack":
        if t <= 0.25:
            body_dx = -2.5 * (t / 0.25)
            body_dz = 1.5 * (t / 0.25)
        elif t <= 0.5:
            p = (t - 0.25) / 0.25
            body_dx = -2.5 + 6.5 * p
            body_dz = 1.5 - 2.5 * p
        else:
            p = (t - 0.5) / 0.5
            body_dx = 4.0 * (1 - p)
            body_dz = -1.0 * (1 - p)
    elif anim == "death":
        death_scale = max(0.1, 1.0 - t * 0.9)
        body_dz = -6 * t

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            color = 4 
            if t > 0.2:
                bx += (random.random() - 0.5) * t * 12
                by += (random.random() - 0.5) * t * 12
                r = max(1, r - t * 2)
        
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz * death_scale
        draw_sphere(vol, nx, ny, nz, r, color)

    # Body parts
    add_sphere(0 + body_dx, 0, 0 + body_dz, 4, 1)
    add_sphere(-3 + body_dx, -1, -1 + body_dz, 3.2, 1)
    add_sphere(-2 + body_dx, 1, 0 + body_dz, 2.8, 1)
    add_sphere(3 + body_dx, 0, 0 + body_dz, 3.5, 1)
    add_sphere(1.5 + body_dx, 2, -1 + body_dz, 2.5, 1)
    
    # Eyes
    eye_size = 0.9
    if anim == "attack" and 0.25 < t < 0.6:
        eye_size = 1.6
    if anim != "death" or t < 0.3:
        add_sphere(4 + body_dx, 1.5, 1 + body_dz, eye_size, 2)
        add_sphere(4.5 + body_dx, -1, 1 + body_dz, eye_size, 2)
        add_sphere(3 + body_dx, 0, 2 + body_dz, eye_size, 2)
        
    # Legs
    roots = [(2, 3, 0), (2, -3, 0), (0, 4, 0), (0, -4, 0), (-3, 3, -1), (-3, -3, -1)]
    feet = [(3, 4, -8), (3, -4, -8), (0, 5, -8), (0, -5, -8), (-4, 4, -8), (-4, -4, -8)]
    
    for i in range(6):
        rx, ry, rz = roots[i]
        fx, fy, fz = feet[i]
        
        rx += body_dx
        rz += body_dz
        fx += legs_dx[i]
        fz += legs_dz[i]
        
        kx = (rx + fx) / 2
        ky = (ry + fy) / 2 + (2.5 if ry > 0 else -2.5) 
        kz = rz + 1.5 
        
        def draw_bone(x1, y1, z1, x2, y2, z2, r):
            steps = max(int(math.hypot(x2-x1, math.hypot(y2-y1, z2-z1)) * 2), 2)
            for j in range(steps):
                p = j / float(steps-1)
                bx = x1 + (x2 - x1) * p
                by = y1 + (y2 - y1) * p
                bz = z1 + (z2 - z1) * p
                add_sphere(bx, by, bz, r, 3)
                
        if anim != "death" or t < 0.4:
            draw_bone(rx, ry, rz, kx, ky, kz, 1.2)
            draw_bone(kx, ky, kz, fx, fy, fz, 1.0)
            
    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/charognard"
    os.makedirs(out_dir, exist_ok=True)
    
    # Angles mapping based on isometric 2:1 projection where +X is SE
    dirs = {
        "SE": 0,
        "NE": 270,
        "NW": 180,
        "SW": 90
    }
    
    anims = {
        "idle": 3,
        "walk": 4,
        "attack": 4,
        "death": 4
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (32 * frames, 32), (0, 0, 0, 0))
            for f in range(frames):
                t = f / float(frames)
                img = build_frame(t, anim, angle)
                
                # Save individual frame
                filename = f"enemy_charognard_{d_name}_{anim}_{f:02d}.png"
                img.save(os.path.join(out_dir, filename))
                
                # Paste into sheet
                sheet.paste(img, (f * 32, 0))
                
            # Save sheet
            sheet_name = f"enemy_charognard_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
