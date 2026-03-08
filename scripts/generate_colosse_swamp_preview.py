import os
import math
import random
from PIL import Image

def create_volume():
    # 64x64x64 volume for 128x128 sprite
    return [[[0]*64 for _ in range(64)] for __ in range(64)]

def draw_sphere(vol, cx, cy, cz, r, color):
    min_x = max(0, int(cx - r))
    max_x = min(63, int(cx + r))
    min_y = max(0, int(cy - r))
    max_y = min(63, int(cy + r))
    min_z = max(0, int(cz - r))
    max_z = min(63, int(cz + r))
    r_sq = r * r
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                if (x - cx)**2 + (y - cy)**2 + (z - cz)**2 <= r_sq:
                    # Acid green glowing eyes/core overwrite priority
                    if vol[x][y][z] == 0 or color == 5:
                        vol[x][y][z] = color

def draw_bone(vol, x1, y1, z1, x2, y2, z2, r, color):
    dist = math.hypot(x2-x1, math.hypot(y2-y1, z2-z1))
    if dist == 0:
        draw_sphere(vol, x1, y1, z1, r, color)
        return
    steps = max(int(dist * 2), 2)
    for j in range(steps):
        p = j / float(steps-1)
        bx = x1 + (x2 - x1) * p
        by = y1 + (y2 - y1) * p
        bz = z1 + (z2 - z1) * p
        draw_sphere(vol, bx, by, bz, r, color)

def render_volume(vol):
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    depth_buf = [[-9999]*128 for _ in range(128)]
    
    for z in range(64):
        for y in range(64):
            for x in range(64):
                col = vol[x][y][z]
                if col == 0: continue
                
                # Strict pure Pixel-Art Isometric projection 2:1
                sx = int(64 + x - y)
                sy = int(80 + (x + y) / 2.0 - z) 
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 128 and 0 <= sy < 128:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 63 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        c = (255, 0, 255, 255) # Magenta error
                        
                        # Palettes for Colosse Swamp
                        if col == 1: # Dark Swamp Mud / Peat (Greens/Browns)
                            if top_lit: c = (65, 95, 80, 255) # #415F50
                            elif side_lit: c = (58, 90, 74, 255) # #3A5A4A Base
                            else: c = (35, 60, 50, 255)
                        elif col == 2: # Darker wet mud (Shadows)
                            if top_lit: c = (50, 75, 60, 255)
                            elif side_lit: c = (40, 65, 50, 255)
                            else: c = (25, 40, 30, 255)
                        elif col == 3: # Rotting Wood (Brown/Gray)
                            if top_lit: c = (120, 90, 60, 255)
                            elif side_lit: c = (90, 60, 40, 255)
                            else: c = (50, 35, 25, 255)
                        elif col == 4: # Old Bones (Pale Yellowish White)
                            if top_lit: c = (220, 210, 180, 255)
                            elif side_lit: c = (180, 170, 140, 255)
                            else: c = (120, 110, 90, 255)
                        elif col == 5: # Acid Green Glow / Core (#7FFF00)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                        
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
                    # Very dark muddy outline
                    outline_color = (15, 25, 20, 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_swamp_colosse(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 32, 32, 16 
    angle = math.radians(angle_deg)
    
    # Anim mods
    bob = 0.0
    torso_rot = 0.0
    arm_r_swing = 0.0
    arm_l_swing = 0.0
    arm_r_lift = 0.0
    arm_l_lift = 0.0
    leg_r_y = 0.0
    leg_l_y = 0.0
    leg_r_z = 0.0
    leg_l_z = 0.0
    death_crumble = 1.0

    if anim == "idle":
        bob = math.sin(t * math.pi * 2) * 1.5
        torso_rot = math.sin(t * math.pi) * 0.1
        arm_r_swing = math.sin(t * math.pi * 2) * 1.5
        arm_l_swing = math.cos(t * math.pi * 2) * 1.5
        
    elif anim == "walk":
        bob = abs(math.sin(t * math.pi * 2)) * 3.0
        torso_rot = math.cos(t * math.pi * 2) * 0.2
        
        leg_phase = t * math.pi * 2
        leg_r_y = math.sin(leg_phase) * 6
        leg_l_y = math.sin(leg_phase + math.pi) * 6
        
        leg_r_z = max(0, -math.cos(leg_phase)) * 4
        leg_l_z = max(0, -math.cos(leg_phase + math.pi)) * 4
        
        arm_r_swing = -leg_r_y * 1.5
        arm_l_swing = -leg_l_y * 1.5
        arm_r_lift = leg_r_z * 0.5
        arm_l_lift = leg_l_z * 0.5
        
    elif anim == "attack":
        # Smashing down via heavy right arm (mast)
        if t < 0.3:
            p = t / 0.3
            torso_rot = p * 0.5 
            bob = p * 2.0
            arm_r_swing = -p * 12.0 
            arm_r_lift = p * 15.0 
            arm_l_swing = p * 6.0
        elif t < 0.6:
            p = (t - 0.3) / 0.3
            torso_rot = 0.5 - p * 1.0
            bob = 2.0 - p * 5.0 
            arm_r_swing = -12.0 + p * 24.0 
            arm_r_lift = 15.0 - p * 20.0 
            arm_l_swing = 6.0 - p * 8.0
        else:
            p = (t - 0.6) / 0.4
            torso_rot = -0.5 + p * 0.5 
            bob = -3.0 + p * 3.0
            arm_r_swing = 12.0 - p * 12.0
            arm_r_lift = -5.0 + p * 5.0
            arm_l_swing = -2.0 + p * 2.0
            
    elif anim == "death":
        death_crumble = max(0.1, 1.0 - t*1.5)
        bob = -t * 20

    random.seed(42 + int(t * 10))

    def rot_pts(bx, by, rot):
        nx = bx * math.cos(rot) - by * math.sin(rot)
        ny = bx * math.sin(rot) + by * math.cos(rot)
        return nx, ny

    def add_sphere_rot(bx, by, bz, r, color):
        if anim == "death" and color != 5:
            bx += (random.random() - 0.5) * t * 20
            by += (random.random() - 0.5) * t * 20
            bz += (random.random() - 0.5) * t * 8
            r *= death_crumble
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone_rot(bx1, by1, bz1, bx2, by2, bz2, r, color):
        if anim == "death": return
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    # 1. BASE / LEGS (Thick sludge mounds)
    if anim != "death":
        # Right leg mass
        add_sphere_rot(-2, -6 + leg_r_y*0.5, -4 + bob, 5.0, 1)
        add_sphere_rot(-1, -7 + leg_r_y, -10 + leg_r_z, 6.0, 2)
        # Roots wrapping the leg
        add_bone_rot(2, -4 + leg_r_y*0.5, -2 + bob, -3, -9 + leg_r_y, -12 + leg_r_z, 1.5, 3)
        add_bone_rot(-4, -4 + leg_r_y*0.5, -2 + bob, -1, -5 + leg_r_y, -12 + leg_r_z, 1.2, 3)

        # Left leg mass
        add_sphere_rot(-1, 7 + leg_l_y*0.5, -3 + bob, 4.5, 1)
        add_sphere_rot(0, 8 + leg_l_y, -9 + leg_l_z, 5.5, 2)
        # Bones sticking out of mud
        add_bone_rot(-2, 10 + leg_l_y*0.5, -5 + bob, 4, 12 + leg_l_y, -2 + leg_l_z, 1.0, 4)

    # 2. BELLY / TORSO (Hulking drooping mass of swamp matter)
    tx, ty = rot_pts(0, 0, torso_rot)
    # Pelvis
    add_sphere_rot(tx, ty, 5 + bob, 8.0, 2)
    # Belly (Drooping forward)
    tx_f, ty_f = rot_pts(4, 0, torso_rot)
    tx_f2, ty_f2 = rot_pts(6, 0, torso_rot)
    add_sphere_rot(tx_f, ty_f, 12 + bob, 9.0, 1)
    add_sphere_rot(tx_f2, ty_f2, 10 + bob, 7.0, 1)
    # Chest overhang
    tx_c, ty_c = rot_pts(5, 0, torso_rot)
    tx_r, ty_r = rot_pts(4, -4, torso_rot)
    tx_l, ty_l = rot_pts(4, 4, torso_rot)
    add_sphere_rot(tx_c, ty_c, 20 + bob, 10.0, 1)
    add_sphere_rot(tx_r, ty_r, 22 + bob, 8.0, 2)
    add_sphere_rot(tx_l, ty_l, 22 + bob, 8.0, 2)
    
    # Internal core glowing through mud holes
    if anim != "death" or t < 0.6:
        tx_e1, ty_e1 = rot_pts(14, 0, torso_rot)
        tx_e2, ty_e2 = rot_pts(12, -3, torso_rot)
        add_sphere_rot(tx_e1, ty_e1, 18 + bob, 2.5, 5) # Central eye/core
        add_sphere_rot(tx_e2, ty_e2, 15 + bob, 1.5, 5)
    
    # Ribcage bones protruding from chest
    b1_x1, b1_y1 = rot_pts(10, 4, torso_rot)
    b1_x2, b1_y2 = rot_pts(14, 8, torso_rot)
    add_bone_rot(b1_x1, b1_y1, 19 + bob, b1_x2, b1_y2, 14 + bob, 1.0, 4)
    b2_x1, b2_y1 = rot_pts(12, 2, torso_rot)
    b2_x2, b2_y2 = rot_pts(16, 5, torso_rot)
    add_bone_rot(b2_x1, b2_y1, 22 + bob, b2_x2, b2_y2, 17 + bob, 1.0, 4)
    b3_x1, b3_y1 = rot_pts(10, -4, torso_rot)
    b3_x2, b3_y2 = rot_pts(14, -8, torso_rot)
    add_bone_rot(b3_x1, b3_y1, 19 + bob, b3_x2, b3_y2, 14 + bob, 1.0, 4)
    b4_x1, b4_y1 = rot_pts(12, -2, torso_rot)
    b4_x2, b4_y2 = rot_pts(16, -5, torso_rot)
    add_bone_rot(b4_x1, b4_y1, 22 + bob, b4_x2, b4_y2, 17 + bob, 1.0, 4)

    # 3. BACK STRUCTURE (A whole rotting dead tree growing out of its back)
    tr_b1, tr_b2 = rot_pts(-4, 0, torso_rot)
    tr_t1, tr_t2 = rot_pts(-10, 0, torso_rot)
    add_bone_rot(tr_b1, tr_b2, 15 + bob, tr_t1, tr_t2, 32 + bob, 3.5, 3) # Trunk
    tr_l1, tr_l2 = rot_pts(-14, -6, torso_rot)
    add_bone_rot(tr_t1, tr_t2, 32 + bob, tr_l1, tr_l2, 42 + bob, 2.0, 3) # Branch left
    tr_r1, tr_r2 = rot_pts(-12, 8, torso_rot)
    add_bone_rot(tr_t1, tr_t2, 32 + bob, tr_r1, tr_r2, 45 + bob, 1.5, 3) # Branch right
    tr_f1, tr_f2 = rot_pts(-6, 0, torso_rot)
    tr_f3, tr_f4 = rot_pts(-2, 0, torso_rot)
    add_bone_rot(tr_f1, tr_f2, 25 + bob, tr_f3, tr_f4, 35 + bob, 1.5, 3) # Forward branch
    
    # Vines hanging from branches
    add_bone_rot(tr_l1, tr_l2, 42 + bob, tr_l1, tr_l2, 20 + bob, 0.8, 1)
    add_bone_rot(tr_r1, tr_r2, 45 + bob, tr_r1, tr_r2, 18 + bob, 0.8, 2)

    # 4. HEAD (Sunken skull mask embedded in mud)
    hx, hy = rot_pts(6, 0, torso_rot)
    # Neck mass
    add_sphere_rot(hx, hy, 30 + bob, 6.0, 2) 
    # Sludge cowl
    hx2, hy2 = rot_pts(8, 0, torso_rot)
    add_sphere_rot(hx2, hy2, 33 + bob, 5.0, 1)
    # Skull mask (Animalistic skull, made of bones)
    hx3, hy3 = rot_pts(11, 0, torso_rot)
    add_sphere_rot(hx3, hy3, 32 + bob, 3.0, 4) # Cranium
    sn1, sn2 = rot_pts(11, -1.5, torso_rot)
    sn3, sn4 = rot_pts(16, -2, torso_rot)
    add_bone_rot(sn1, sn2, 32 + bob, sn3, sn4, 28 + bob, 1.5, 4) # Snout right
    sn5, sn6 = rot_pts(11, 1.5, torso_rot)
    sn7, sn8 = rot_pts(16, 2, torso_rot)
    add_bone_rot(sn5, sn6, 32 + bob, sn7, sn8, 28 + bob, 1.5, 4) # Snout left
    # Glowing eyes in the skull
    ey1, ey2 = rot_pts(14, -1.5, torso_rot)
    ey3, ey4 = rot_pts(14, 1.5, torso_rot)
    add_sphere_rot(ey1, ey2, 33 + bob, 1.0, 5)
    add_sphere_rot(ey3, ey4, 33 + bob, 1.0, 5)

    # 5. ARMS
    if anim != "death":
        # Right Arm (Massive mud mound dragging a sunken boat mast)
        ar_sh_x, ar_sh_y = rot_pts(2, -10, torso_rot)
        ar_el_x, ar_el_y = rot_pts(4 + arm_r_swing*0.5, -14, torso_rot)
        ar_ha_x, ar_ha_y = rot_pts(8 + arm_r_swing, -16, torso_rot)
        
        add_sphere_rot(ar_sh_x, ar_sh_y, 24 + bob, 4.5, 2) # Shoulder
        add_bone_rot(ar_sh_x, ar_sh_y, 24 + bob, ar_el_x, ar_el_y, 12 + bob + arm_r_lift*0.5, 4.0, 1) # Bicep
        add_bone_rot(ar_el_x, ar_el_y, 12 + bob + arm_r_lift*0.5, ar_ha_x, ar_ha_y, 0 + bob + arm_r_lift, 5.0, 2) # Sludge forearm
        # Huge rotting wooden mast embedded in forearm
        m1_x, m1_y = rot_pts(2 + arm_r_swing, -12, torso_rot)
        m2_x, m2_y = rot_pts(18 + arm_r_swing, -20, torso_rot)
        add_bone_rot(m1_x, m1_y, 8 + bob + arm_r_lift, m2_x, m2_y, -10 + bob + arm_r_lift, 2.5, 3)
        cb1_x, cb1_y = rot_pts(10 + arm_r_swing, -16, torso_rot)
        cb2_x, cb2_y = rot_pts(16 + arm_r_swing, -12, torso_rot)
        add_bone_rot(cb1_x, cb1_y, -2 + bob + arm_r_lift, cb2_x, cb2_y, -4 + bob + arm_r_lift, 1.5, 3) # Cross beam

        # Left Arm (Tangled roots and vines)
        al_sh_x, al_sh_y = rot_pts(2, 10, torso_rot)
        al_el_x, al_el_y = rot_pts(4 + arm_l_swing*0.5, 14, torso_rot)
        al_ha_x, al_ha_y = rot_pts(10 + arm_l_swing, 12, torso_rot)
        
        add_sphere_rot(al_sh_x, al_sh_y, 24 + bob, 4.0, 2) # Shoulder
        add_bone_rot(al_sh_x, al_sh_y, 24 + bob, al_el_x, al_el_y, 14 + bob + arm_l_lift*0.5, 2.5, 3) # Root arm
        add_bone_rot(al_el_x, al_el_y, 14 + bob + arm_l_lift*0.5, al_ha_x, al_ha_y, 4 + bob + arm_l_lift, 2.0, 3) # Root forearm
        # Hanging sludge from left arm
        ls_x, ls_y = rot_pts(6 + arm_l_swing*0.7, 13, torso_rot)
        add_bone_rot(ls_x, ls_y, 10 + bob + arm_l_lift*0.7, ls_x, ls_y, 0 + bob + arm_l_lift*0.7, 1.5, 1)

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/colosse_swamp"
    os.makedirs(out_dir, exist_ok=True)
    
    dirs = {
        "SE": 0,
        "SW": 90,
        "NW": 180,
        "NE": 270
    }
    
    anims = {
        "idle": 6,   
        "walk": 8,   
        "attack": 8, 
        "death": 6   
    }
    
    print("Generating Colosse Swamp animations (128x128 Massive Voxel Golem)...")
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (128 * frames, 128), (0, 0, 0, 0)) 
            for f in range(frames):
                try:
                    t = f / float(frames)
                    img = build_swamp_colosse(angle, t, anim)
                    
                    filename = f"enemy_colosse_swamp_{d_name}_{anim}_{f:02d}.png"
                    img.save(os.path.join(out_dir, filename))
                    
                    sheet.paste(img, (f * 128, 0))
                except Exception as e:
                    print(f"Error drawing frame {f} for {d_name} {anim}: {e}")
                
            sheet_name = f"enemy_colosse_swamp_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            
    print("Colosse Swamp generation complete.")

if __name__ == "__main__":
    main()
