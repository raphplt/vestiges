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
                    if color == 2:
                        vol[x][y][z] = 2
                    elif vol[x][y][z] not in (2, 5): 
                        vol[x][y][z] = color

def draw_bone(vol, x1, y1, z1, x2, y2, z2, r, color):
    steps = max(int(math.hypot(x2-x1, math.hypot(y2-y1, z2-z1)) * 2), 2)
    for j in range(steps):
        p = j / float(steps-1)
        bx = x1 + (x2 - x1) * p
        by = y1 + (y2 - y1) * p
        bz = z1 + (z2 - z1) * p
        draw_sphere(vol, bx, by, bz, r, color)

def render_volume(vol, anim="idle", t=0):
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0)) 
    depth_buf = [[-9999]*64 for _ in range(64)]
    
    for z in range(32):
        for y in range(32):
            for x in range(32):
                col = vol[x][y][z]
                if col == 0: continue
                
                sx = int(32 + x - y)
                sy = int(30 + (x + y) / 2.0 - z) 
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 64 and 0 <= sy < 64:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 31 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        # Palettes
                        if col == 1: # Dark sinister brown tree bark
                            if top_lit: c = (90, 70, 50, 255)
                            elif side_lit: c = (65, 50, 35, 255)
                            else: c = (40, 30, 20, 255)
                        elif col == 2: # Glow eyes - pure acid
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                            if anim == "death": # Fade glow
                                c = (c[0], c[1], c[2], int(255 * max(0, 1.0 - t*1.5)))
                        elif col == 3: # Swollen Bark (Face features) sickly grayish brown to blend
                            if top_lit: c = (110, 100, 80, 255)
                            elif side_lit: c = (85, 80, 60, 255)
                            else: c = (60, 55, 40, 255)
                        elif col == 4: # Thick dark canopy
                            if top_lit: c = (45, 65, 30, 255)
                            elif side_lit: c = (30, 45, 20, 255)
                            else: c = (15, 25, 10, 255)
                        elif col == 5: # Void Black / Maw
                            c = (15, 10, 10, 255)
                        
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

def build_treant_frame(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 16, 16, 5 
    angle = math.radians(angle_deg)
    
    # Anim mods (scaled down by 2)
    sway = 0.0
    sway_y = 0.0
    walk_bob = 0.0
    lift_fr = 0.0
    lift_fl = 0.0
    lift_br = 0.0
    lift_bl = 0.0
    arm_r_swing = 0.0
    arm_l_swing = 0.0
    smash_z = 0.0
    mouth_open = 0.0
    death_crumble = 1.0
    death_sink = 0.0

    if anim == "idle":
        sway = math.sin(t * math.pi * 2) * 0.75
        sway_y = math.cos(t * math.pi * 2) * 0.25
        mouth_open = (math.sin(t * math.pi * 2) + 1.0) * 0.25
        
    elif anim == "walk":
        sway = math.sin(t * math.pi * 4) * 1.0
        sway_y = abs(math.sin(t * math.pi * 4)) * 0.75
        walk_bob = abs(math.sin(t * math.pi * 4)) * -0.75
        
        lift_fr = max(0, math.sin(t * math.pi * 2)) * 1.5
        lift_bl = max(0, math.sin(t * math.pi * 2)) * 1.0
        
        lift_fl = max(0, math.sin(t * math.pi * 2 + math.pi)) * 1.5
        lift_br = max(0, math.sin(t * math.pi * 2 + math.pi)) * 1.0
        
        arm_r_swing = math.sin(t * math.pi * 2) * 2.0
        arm_l_swing = -math.sin(t * math.pi * 2) * 2.0
        
    elif anim == "attack":
        if t < 0.4:
            p = t / 0.4
            sway = -p * 2.5 
            arm_r_swing = p * 7.5 
            arm_l_swing = p * 7.5
            mouth_open = p * 1.5 
        elif t < 0.6:
            p = (t - 0.4) / 0.2
            sway = -2.5 + p * 6.0 
            arm_r_swing = 7.5 - p * 15.0 
            arm_l_swing = 7.5 - p * 15.0
            smash_z = -p * 7.5 
            mouth_open = 1.5 - p * 1.0
        else:
            p = (t - 0.6) / 0.4    
            sway = 3.5 - p * 3.5               
            arm_r_swing = -7.5 + p * 7.5
            arm_l_swing = -7.5 + p * 7.5
            smash_z = -7.5 + p * 7.5
            mouth_open = 0.5 - p * 0.5

    elif anim == "death":
        death_crumble = max(0.1, 1.0 - t)
        death_sink = t * 7.5
        mouth_open = t * 2.0

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            bx += (random.random() - 0.5) * t * 7.5
            by += (random.random() - 0.5) * t * 7.5
            
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz * death_crumble - death_sink
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone(bx1, by1, bz1, bx2, by2, bz2, r, color):
        if anim == "death": return 
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    random.seed(42 + int(t*100))

    # ROOT LEGS 
    if anim != "death":
        add_bone(0, 0, 1 + walk_bob, 5 + sway, 4 + sway_y, -2 + lift_fr, 1.25, 1) # FR
        add_bone(5 + sway, 4 + sway_y, -2 + lift_fr, 7 + sway, 6 + sway_y, -3.5, 0.75, 1)
        
        add_bone(0, 0, 1 + walk_bob, 5 + sway, -4 + sway_y, -2 + lift_fl, 1.25, 1) # FL
        add_bone(5 + sway, -4 + sway_y, -2 + lift_fl, 7 + sway, -6 + sway_y, -3.5, 0.75, 1)
        
        add_bone(0, 0, 2 + walk_bob, -4 + sway, 3 + sway_y, -1 + lift_br, 1.25, 1) # BR
        add_bone(0, 0, 2 + walk_bob, -4 + sway, -3 + sway_y, -1 + lift_bl, 1.25, 1) # BL
        
    # MAIN TRUNK
    add_bone(0, 0, walk_bob, 1 + sway*0.3, sway_y*0.3, 7.5 + walk_bob, 3.5, 1) 
    add_bone(1 + sway*0.3, sway_y*0.3, 7.5 + walk_bob, 3 + sway*0.6, sway_y*0.6, 15 + walk_bob, 3.0, 1) 
    add_bone(3 + sway*0.6, sway_y*0.6, 15 + walk_bob, 4 + sway, sway_y, 21 + walk_bob, 2.5, 1) 

    # ARMS
    if anim != "death":
        br_x, br_y, br_z = 2.5 + sway*0.7, 3.5 + sway_y*0.7, 14 + walk_bob
        add_bone(br_x, br_y, br_z, br_x+1.5 + arm_r_swing, br_y+4.5, br_z-4 + arm_r_swing + smash_z, 1.75, 1) 
        br_x2, br_y2, br_z2 = br_x+1.5 + arm_r_swing, br_y+4.5, br_z-4 + arm_r_swing + smash_z
        add_bone(br_x2, br_y2, br_z2, br_x2+3.5, br_y2-1, br_z2-4 + smash_z, 1.25, 1) 
        add_bone(br_x2+3.5, br_y2-1, br_z2-4 + smash_z, br_x2+7, br_y2+1, br_z2-7 + smash_z, 0.75, 1) 
        add_bone(br_x2+3.5, br_y2-1, br_z2-4 + smash_z, br_x2+5, br_y2-3, br_z2-8 + smash_z, 0.75, 1) 
        
        bl_x, bl_y, bl_z = 2.5 + sway*0.7, -3.5 + sway_y*0.7, 14 + walk_bob
        add_bone(bl_x, bl_y, bl_z, bl_x+1.5 + arm_l_swing, bl_y-4.5, bl_z-4 + arm_l_swing + smash_z, 1.75, 1)
        bl_x2, bl_y2, bl_z2 = bl_x+1.5 + arm_l_swing, bl_y-4.5, bl_z-4 + arm_l_swing + smash_z
        add_bone(bl_x2, bl_y2, bl_z2, bl_x2+4, bl_y2+2, bl_z2-4 + smash_z, 1.25, 1)
        add_bone(bl_x2+4, bl_y2+2, bl_z2-4 + smash_z, bl_x2+8, bl_y2-2, bl_z2-7.5 + smash_z, 0.75, 1) 
        add_bone(bl_x2+4, bl_y2+2, bl_z2-4 + smash_z, bl_x2+6, bl_y2+2, bl_z2-8.5 + smash_z, 0.75, 1) 

    # THE FACES IN THE BARK
    f_x = 3.5 + sway*0.8
    f_y = -2 + sway_y*0.8
    f_z = 16 + walk_bob
    add_bone(f_x, f_y, f_z, f_x, f_y+4, f_z, 1.0, 3) 
    add_sphere(f_x+0.5, f_y+0.75, f_z-1, 1.0, 5) 
    add_sphere(f_x+1, f_y+0.75, f_z-1, 0.5, 2) 
    
    add_sphere(f_x+0.5, f_y+3.25, f_z-1, 1.0, 5) 
    add_sphere(f_x+1, f_y+3.25, f_z-1, 0.5, 2) 
    
    add_bone(f_x, f_y+2, f_z-1, f_x+1, f_y+2, f_z-5, 0.75, 3)
    
    add_bone(f_x-0.5, f_y, f_z-3.5, f_x+0.5, f_y, f_z-7, 1.0, 3)
    add_bone(f_x-0.5, f_y+4, f_z-3.5, f_x+0.5, f_y+4, f_z-7, 1.0, 3)
    
    m_x = 4 + sway*0.5
    m_y = sway_y*0.5
    m_z = 7 + walk_bob
    
    add_sphere(m_x, m_y, m_z, 2.25 + mouth_open*0.5, 5) 
    add_bone(m_x, m_y-1.5, m_z+2, m_x, m_y+1.5, m_z+2 + mouth_open*0.5, 0.75, 3) # Upper lip
    add_bone(m_x-1, m_y-2, m_z-2, m_x-1, m_y+2, m_z-2 - mouth_open*1.5, 0.75, 3) # Lower lip drops
    
    if anim != "death":
        add_bone(m_x-0.5, m_y-0.5, m_z-2 - mouth_open*1.5, m_x+0.5, m_y-0.5, m_z-4.5 - mouth_open*1.5 - (math.sin(t*10)*0.25), 0.5, 2)
        add_bone(m_x-0.5, m_y+1, m_z-2 - mouth_open*1.5, m_x, m_y+1, m_z-3.5 - mouth_open*1.5 - (math.cos(t*10)*0.25), 0.4, 2)

    # CANOPY
    c_x = 3.5 + sway
    c_y = sway_y
    c_z = 22.5 + walk_bob
    add_sphere(c_x, c_y, c_z, 4.5, 4)
    add_sphere(c_x-1, c_y+3, c_z-1.5, 3.0, 4)
    add_sphere(c_x-1, c_y-3, c_z-1.5, 3.0, 4)
    add_sphere(c_x-2.5, c_y, c_z-2.5, 4.0, 4)
    
    if anim != "death":
        add_bone(c_x-1, c_y+3, c_z-1.5, c_x-2.5, c_y+4.5, c_z-9.5, 1.25, 4)
        add_bone(c_x-1, c_y-3, c_z-1.5, c_x-2.5, c_y-4.5, c_z-9.5, 1.25, 4)
        add_bone(c_x+1, c_y, c_z, c_x+2, c_y, c_z-5.5, 0.75, 4) 

    return render_volume(vol, anim, t)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/treant_corrompu"
    os.makedirs(out_dir, exist_ok=True)
    
    dirs = {
        "SE": 0,
        "NE": 270,
        "NW": 180,
        "SW": 90
    }
    
    anims = {
        "idle": 6,   
        "walk": 8,   
        "attack": 8, 
        "death": 6   
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (64 * frames, 64), (0, 0, 0, 0)) 
            for f in range(frames):
                try:
                    t = f / float(frames)
                    img = build_treant_frame(angle, t, anim)
                    
                    filename = f"enemy_treant_corrompu_{d_name}_{anim}_{f:02d}.png"
                    img.save(os.path.join(out_dir, filename))
                    
                    sheet.paste(img, (f * 64, 0))
                except Exception as e:
                    print(f"Error drawing frame {f} for {d_name} {anim}: {e}")
                
            sheet_name = f"enemy_treant_corrompu_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
