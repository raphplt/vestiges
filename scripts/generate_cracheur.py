import os
import math
import random
from PIL import Image

def create_volume():
    return [[[0]*48 for _ in range(48)] for __ in range(48)]

def draw_sphere(vol, cx, cy, cz, r, color):
    min_x = max(0, int(cx - r))
    max_x = min(47, int(cx + r))
    min_y = max(0, int(cy - r))
    max_y = min(47, int(cy + r))
    min_z = max(0, int(cz - r))
    max_z = min(47, int(cz + r))
    r_sq = r * r
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                if (x - cx)**2 + (y - cy)**2 + (z - cz)**2 <= r_sq:
                    if color == 2:
                        vol[x][y][z] = 2
                    elif vol[x][y][z] not in (2, 4):
                        vol[x][y][z] = color

def draw_bone(vol, x1, y1, z1, x2, y2, z2, r, color):
    steps = max(int(math.hypot(x2-x1, math.hypot(y2-y1, z2-z1)) * 2), 2)
    for j in range(steps):
        p = j / float(steps-1)
        bx = x1 + (x2 - x1) * p
        by = y1 + (y2 - y1) * p
        bz = z1 + (z2 - z1) * p
        draw_sphere(vol, bx, by, bz, r, color)

def render_volume(vol):
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    depth_buf = [[-9999]*64 for _ in range(64)]
    
    for z in range(48):
        for y in range(48):
            for x in range(48):
                col = vol[x][y][z]
                if col == 0: continue
                
                sx = int(32 + x - y)
                sy = int(24 + (x + y) / 2.0 - z) 
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 64 and 0 <= sy < 64:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 47 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        if col == 1: 
                            if top_lit: c = (210, 200, 205, 255)
                            elif side_lit: c = (180, 160, 170, 255)
                            else: c = (130, 110, 120, 255)
                        elif col == 2: 
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                        elif col == 3: 
                            if top_lit: c = (190, 100, 80, 255)
                            elif side_lit: c = (150, 70, 50, 255)
                            else: c = (100, 40, 30, 255)
                        elif col == 4: 
                            if top_lit: c = (140, 255, 50, 255)
                            else: c = (90, 200, 10, 255)
                        elif col == 5: 
                            c = (20, 15, 25, 255)
                        
                        img.putpixel((sx, sy), c)
                        
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
                    outline_color = (int(base[0]*0.2), int(base[1]*0.2), int(base[2]*0.2), 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_cracheur_frame(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 24, 24, 4 
    angle = math.radians(angle_deg)
    
    # Animation modifiers
    sac_scale = 1.0
    sac_z_mod = 0.0
    mouth_open = 0.0
    leg_lift_1 = 0.0
    leg_lift_2 = 0.0
    leg_lift_3 = 0.0
    acid_squirt = 0.0
    death_scale = 1.0

    if anim == "idle":
        sac_scale = 1.0 + math.sin(t * math.pi * 2) * 0.05
    elif anim == "walk":
        sac_z_mod = math.sin(t * math.pi * 4) * 0.4
        leg_lift_1 = max(0, math.sin(t * math.pi * 2)) * 1.0
        leg_lift_2 = max(0, math.sin(t * math.pi * 2 + math.pi)) * 1.0
        leg_lift_3 = max(0, math.sin(t * math.pi * 2 + math.pi/2)) * 1.0
    elif anim == "attack":
        # Suck in, then violently spit
        if t < 0.3:
            sac_scale = 1.0 + (t / 0.3) * 0.3 # Bloats up
            mouth_open = 0.0
        elif t < 0.6:
            p = (t - 0.3) / 0.3
            sac_scale = 1.3 - p * 0.5 # Rapid deflation
            mouth_open = p * 1.0
            acid_squirt = p * 3.0
        else:
            p = (t - 0.6) / 0.4
            sac_scale = 0.8 + p * 0.2
            mouth_open = 1.0 * (1 - p)
    elif anim == "death":
        # Disintegrates into black particles / collapses
        death_scale = max(0.1, 1.0 - t * 1.5)
        sac_z_mod = -t * 2.5
        
    random.seed(42 + int(t * 10))

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            if t > 0.2:
                # Turn into iridescent fluid on death
                color = 4 if color == 4 else 5 # mostly black but glowing green inside
                bx += (random.random() - 0.5) * t * 5
                by += (random.random() - 0.5) * t * 5
                r = max(0.5, r - t * 1.0)
                
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + (bz + sac_z_mod) * death_scale
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone(bx1, by1, bz1, bx2, by2, bz2, r, color):
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + (bz1 + (sac_z_mod if "leg" in str(color) else 0)) * death_scale
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2 * death_scale # feet don't bob
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    # BODY
    add_sphere(0, 0, 5, 3.0 * sac_scale, 1) 
    add_sphere(1, 0, 6, 2.5 * sac_scale, 1) 
    
    # CRUST
    if anim != "death" or t < 0.3:
        add_sphere(-1, 0, 7, 2.25 * sac_scale, 3) 
        add_sphere(-2, 0, 6, 2.0 * sac_scale, 3)
    
    # FACE / MOUTH
    add_sphere(3 + mouth_open*0.5, 0, 6.5, 1.75, 1) 
    add_sphere(3.5 + mouth_open, 0, 6.5, 1.25 + mouth_open*0.5, 1) # Lip flares out
    if anim != "death" or t < 0.3:
        add_sphere(3.75 + mouth_open*1.2, 0, 6.5, 0.9 + mouth_open*0.6, 5) # Dark mouth
        add_sphere(3.25 + mouth_open, 0, 6.5, 0.5 + mouth_open*0.3, 4) # Glowing acid core
    
    # ATTACK PROJECTILE (Acid squirt)
    if acid_squirt > 0:
        add_bone(4, 0, 6.5, 4 + acid_squirt, 0, 6.5, 0.75, 4)
    # Passive leaking
    elif anim != "death" or t < 0.2:
        add_bone(3.75, 0, 5.5, 4, 0, 3.5 - (math.sin(t*10)*0.25), 0.4, 4)
    
    # EYES
    if anim != "death" or t < 0.4:
        add_sphere(2.5, 1.5 + mouth_open*0.2, 7.5, 0.6, 2)
        add_sphere(2.5, -1.25 - mouth_open*0.2, 7.5, 0.4, 2)
        add_sphere(3 + mouth_open*0.5, -0.75 - mouth_open*0.2, 8, 0.3, 2)
    
    # LEGS
    if anim != "death" or t < 0.5:
        # Leg 1 
        add_bone(1, 2, 4, 2, 3, 6 + leg_lift_1, 0.6, 3)
        add_bone(2, 3, 6 + leg_lift_1, 3 + (math.sin(t * math.pi * 2)*0.5 if anim=="walk" else 0), 4, 0, 0.5, 3)
        
        # Leg 2 
        add_bone(1, -2, 4, 1.5, -3, 5 + leg_lift_2, 0.6, 3)
        add_bone(1.5, -3, 5 + leg_lift_2, 2.5 + (math.sin(t * math.pi * 2 + math.pi)*0.5 if anim=="walk" else 0), -4, 0, 0.5, 3)
        
        # Leg 3 
        add_bone(-1.5, 1.5, 4, -2.5, 2.5, 5 + leg_lift_3, 0.75, 3)
        add_bone(-2.5, 2.5, 5 + leg_lift_3, -3.5 + (math.sin(t * math.pi * 2 + math.pi/2)*0.5 if anim=="walk" else 0), 3, 0, 0.6, 3)

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/cracheur"
    os.makedirs(out_dir, exist_ok=True)
    
    dirs = {
        "SE": 0,
        "NE": 270,
        "NW": 180,
        "SW": 90
    }
    
    anims = {
        "idle": 4,   
        "walk": 6,   
        "attack": 6, 
        "death": 5   
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (64 * frames, 64), (0, 0, 0, 0)) # Frames are 64x64
            for f in range(frames):
                t = f / float(frames)
                img = build_cracheur_frame(angle, t, anim)
                
                # Crop to 64x64 safely in case of weird bounding (though img is 64x64)
                if img.size != (64, 64):
                    img = img.crop((0,0,64,64))
                    
                filename = f"enemy_cracheur_{d_name}_{anim}_{f:02d}.png"
                img.save(os.path.join(out_dir, filename))
                
                sheet.paste(img, (f * 64, 0))
                
            sheet_name = f"enemy_cracheur_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
