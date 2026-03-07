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
                    # Color priorities
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
                sy = int(24 + (x + y) / 2.0 - z) 
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 64 and 0 <= sy < 64:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 31 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        # Palettes for the Rodeur
                        if col == 1: # Torso/Limbs: Dark purplish-gray fleshy decay
                            if top_lit: c = (120, 100, 115, 255)
                            elif side_lit: c = (90, 70, 85, 255)
                            else: c = (60, 45, 55, 255)
                        elif col == 2: # Acid green eyes (VERY PROMINENT)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                            if anim == "death":
                                c = (c[0], c[1], c[2], int(255 * max(0, 1.0 - t*2.0)))
                        elif col == 3: # Pale distorted Head/Face
                            if top_lit: c = (200, 190, 195, 255)
                            elif side_lit: c = (160, 150, 155, 255)
                            else: c = (120, 110, 115, 255)
                        elif col == 4: # Darkest shadows / holes / dragging hands
                            if top_lit: c = (40, 30, 45, 255)
                            elif side_lit: c = (25, 15, 25, 255)
                            else: c = (15, 10, 15, 255)
                        elif col == 5: # Cloth rags / tattered pants
                            if top_lit: c = (80, 85, 90, 255)
                            elif side_lit: c = (55, 60, 65, 255)
                            else: c = (35, 40, 45, 255)
                        
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

def build_rodeur_frame(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 16, 16, 8 
    angle = math.radians(angle_deg)
    
    # Anim mods
    bob = 0.0
    hunch_forward = 0.0
    sway = 0.0
    leg_r_y = 0.0
    leg_l_y = 0.0
    leg_r_z = 0.0
    leg_l_z = 0.0
    arm_r_swing = 0.0
    arm_l_swing = 0.0
    arm_r_lift = 0.0
    arm_l_lift = 0.0
    head_turn = 0.0
    death_crumble = 1.0

    if anim == "idle":
        bob = math.sin(t * math.pi * 2) * 0.5
        arm_r_swing = math.sin(t * math.pi * 2) * 1.0
        arm_l_swing = math.cos(t * math.pi * 2) * 1.0
        head_turn = math.sin(t * math.pi) * 1.5
        
    elif anim == "walk":
        bob = math.sin(t * math.pi * 4) * 1.0
        hunch_forward = 1.5
        
        # Heavy dragging steps (z lifts briefly, y moves forward/back)
        leg_phase = t * math.pi * 2
        leg_r_y = math.sin(leg_phase) * 3
        leg_l_y = math.sin(leg_phase + math.pi) * 3
        
        leg_r_z = max(0, -math.cos(leg_phase)) * 2
        leg_l_z = max(0, -math.cos(leg_phase + math.pi)) * 2
        
        # Arms drag far behind and swing heavily to counterbalance
        arm_r_swing = -leg_r_y * 1.5
        arm_l_swing = -leg_l_y * 1.5
        
    elif anim == "attack":
        # Raises massive arms up high, then slams them down
        if t < 0.3:
            p = t / 0.3
            hunch_forward = -p * 3.0 # Rears back
            arm_r_lift = p * 12.0
            arm_l_lift = p * 12.0
            arm_r_swing = p * 4.0
            arm_l_swing = p * 4.0
            bob = p * 2.0
        elif t < 0.6:
            p = (t - 0.3) / 0.3
            hunch_forward = -3.0 + p * 8.0 # Slams completely forward
            bob = 2.0 - p * 4.0
            arm_r_lift = 12.0 - p * 16.0 # Arms hit the ground hard
            arm_l_lift = 12.0 - p * 16.0
            arm_r_swing = 4.0 - p * 2.0
            arm_l_swing = 4.0 - p * 2.0
        else:
            p = (t - 0.6) / 0.4
            hunch_forward = 5.0 - p * 5.0 # Recovers slowly
            bob = -2.0 + p * 2.0
            arm_r_lift = -4.0 + p * 4.0
            arm_r_swing = 2.0 - p * 2.0
            arm_l_swing = 2.0 - p * 2.0

    elif anim == "death":
        death_crumble = max(0.1, 1.0 - t*1.5)
        hunch_forward = t * 10 # Falls flat on face
        bob = -t * 8

    random.seed(42 + int(t * 10))

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            bx += (random.random() - 0.5) * t * 10
            by += (random.random() - 0.5) * t * 10
            r *= death_crumble
            
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone(bx1, by1, bz1, bx2, by2, bz2, r, color):
        if anim == "death":
            return # Skip bones on death to let it dissolve to spheres
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    # LEGS (Bipedal)
    if anim != "death":
        # Right Leg
        add_bone(0, -2, -2 + bob, leg_r_y, -2, -8 + leg_r_z, 1.2, 5) # Pants
        add_bone(leg_r_y, -2, -8 + leg_r_z, leg_r_y+1, -2, -8 + leg_r_z, 0.8, 4) # Naked Foot
        
        # Left Leg
        add_bone(0, 2, -2 + bob, leg_l_y, 2, -8 + leg_l_z, 1.2, 5)
        add_bone(leg_l_y, 2, -8 + leg_l_z, leg_l_y+1, 2, -8 + leg_l_z, 0.8, 4)
    
    # PELVIS
    add_sphere(0, 0, -1 + bob, 2.5, 5) 

    # TORSO (Severely hunched)
    t_mid_x = 3 + hunch_forward*0.5
    t_top_x = 6 + hunch_forward
    if anim != "death":
        add_bone(0, 0, 0 + bob, t_mid_x, 0, 4 + bob, 3.0, 1) # Lower back
        add_bone(t_mid_x, 0, 4 + bob, t_top_x, 0, 7 + bob, 2.8, 1) # Upper back
    
    add_sphere(t_top_x - 1, 0, 9 + bob, 2.5, 1) # Protruding hunched spine
    
    # OVERSIZED ARMS
    if anim != "death":
        # Right arm
        ar_sh_x, ar_sh_y, ar_sh_z = t_top_x - 1, -3, 8 + bob
        ar_el_x, ar_el_y, ar_el_z = t_top_x - 2 + arm_r_swing, -5, 3 + bob + arm_r_lift*0.5
        ar_wr_x, ar_wr_y, ar_wr_z = t_top_x - 1 + arm_r_swing*1.2, -4, -4 + bob + arm_r_lift
        ar_ha_x, ar_ha_y, ar_ha_z = t_top_x + 1 + arm_r_swing*1.5, -3, -8 + bob + arm_r_lift
        if ar_ha_z < -8: ar_ha_z = -8 # dragging on floor
        
        add_bone(ar_sh_x, ar_sh_y, ar_sh_z, ar_el_x, ar_el_y, ar_el_z, 1.8, 1)
        add_bone(ar_el_x, ar_el_y, ar_el_z, ar_wr_x, ar_wr_y, ar_wr_z, 1.4, 1)
        add_bone(ar_wr_x, ar_wr_y, ar_wr_z, ar_ha_x, ar_ha_y, ar_ha_z, 1.6, 4)
        
        # Left arm
        al_sh_x, al_sh_y, al_sh_z = t_top_x - 1, 3, 8 + bob
        al_el_x, al_el_y, al_el_z = t_top_x - 3 + arm_l_swing, 5, 2 + bob + arm_l_lift*0.5
        al_wr_x, al_wr_y, al_wr_z = t_top_x - 3 + arm_l_swing*1.2, 6, -5 + bob + arm_l_lift
        al_ha_x, al_ha_y, al_ha_z = t_top_x + 0 + arm_l_swing*1.5, 5, -8 + bob + arm_l_lift
        if al_ha_z < -8: al_ha_z = -8
        
        add_bone(al_sh_x, al_sh_y, al_sh_z, al_el_x, al_el_y, al_el_z, 1.8, 1)
        add_bone(al_el_x, al_el_y, al_el_z, al_wr_x, al_wr_y, al_wr_z, 1.4, 1)
        add_bone(al_wr_x, al_wr_y, al_wr_z, al_ha_x, al_ha_y, al_ha_z, 1.6, 4)


    # HEAD & EYES
    h_x = t_top_x + 2 
    h_y = 0 + head_turn
    h_z = 8 + bob
    
    add_sphere(h_x, h_y, h_z, 2.2, 3) # Pale face
    
    # Eyes are explicitly positioned on the +X/+Y facing side of the head sphere
    # They are drawn in the 3D volume, so if the angle rotates, they will naturally be occluded by the head sphere from behind!
    if anim != "death" or t < 0.6:
        # Front-right main eye
        add_sphere(h_x + 1.0, h_y - 1.0, h_z + 1.0, 0.8, 2)
        # Front-left secondary eye
        add_sphere(h_x + 1.5, h_y + 1.2, h_z + 0.5, 0.6, 2)
        # Lower weird eye
        add_sphere(h_x + 0.8, h_y + 0.0, h_z - 0.5, 0.4, 2)

    return render_volume(vol, anim, t)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/rodeur"
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
        "attack": 6, 
        "death": 6   
    }
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (64 * frames, 64), (0, 0, 0, 0)) 
            for f in range(frames):
                try:
                    t = f / float(frames)
                    img = build_rodeur_frame(angle, t, anim)
                    
                    filename = f"enemy_rodeur_{d_name}_{anim}_{f:02d}.png"
                    img.save(os.path.join(out_dir, filename))
                    
                    sheet.paste(img, (f * 64, 0))
                except Exception as e:
                    print(f"Error drawing frame {f} for {d_name} {anim}: {e}")
                
            sheet_name = f"enemy_rodeur_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            print(f"Generated {d_name} {anim}")

if __name__ == "__main__":
    main()
