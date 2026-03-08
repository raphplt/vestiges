import os
import math
import random
from PIL import Image

def create_volume():
    # Scale up! Colosse is huge. 64x64x64 volume
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
                    # Acid green glowing eyes/core overwrite EVERYTHING
                    if vol[x][y][z] == 0 or color == 4:
                        vol[x][y][z] = color

def draw_box(vol, cx, cy, cz, wx, wy, wz, color, rot_z=0.0):
    """ Draw an angled rectangular concrete slab """
    min_x = max(0, int(cx - wx*2.0 - 2))
    max_x = min(63, int(cx + wx*2.0 + 2))
    min_y = max(0, int(cy - wy*2.0 - 2))
    max_y = min(63, int(cy + wy*2.0 + 2))
    min_z = max(0, int(cz - wz - 2))
    max_z = min(63, int(cz + wz + 2))
    
    cos_r = math.cos(-rot_z)
    sin_r = math.sin(-rot_z)
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                # Apply reverse rotation to test box inclusion
                dx = x - cx
                dy = y - cy
                rx = dx * cos_r - dy * sin_r
                ry = dx * sin_r + dy * cos_r
                
                if abs(rx) <= wx and abs(ry) <= wy and abs(z - cz) <= wz:
                    if vol[x][y][z] == 0:
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

def render_volume(vol, anim="idle", t=0):
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
                        
                        # Palettes for the Colosse Urban
                        if col == 1: # Brutalist Concrete (Light Gray/Warm)
                            if top_lit: c = (160, 155, 145, 255)
                            elif side_lit: c = (110, 105, 100, 255)
                            else: c = (70, 65, 65, 255)
                        elif col == 2: # Dark Concrete / Foundation (Dark Gray/Cool)
                            if top_lit: c = (100, 100, 105, 255)
                            elif side_lit: c = (65, 65, 75, 255)
                            else: c = (40, 40, 50, 255)
                        elif col == 3: # Rusted Steel Rebar (Dark Red/Brown)
                            if top_lit: c = (80, 50, 40, 255)
                            elif side_lit: c = (50, 30, 25, 255)
                            else: c = (25, 15, 15, 255)
                        elif col == 4: # Acid Green Glow / Core (#7FFF00)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                            if anim == "death":
                                c = (c[0], c[1], c[2], int(255 * max(0, 1.0 - t*2.0)))
                        elif col == 5: # Broken Glass (Teal/Cyan)
                            if top_lit: c = (150, 220, 230, 240)
                            elif side_lit: c = (90, 160, 170, 200)
                            else: c = (40, 80, 90, 180)
                        
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
                    # Very dark gray/greenish outline
                    outline_color = (15, 20, 15, 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_colosse_frame(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 32, 32, 20 
    angle = math.radians(angle_deg)
    
    # Anim mods
    bob = 0.0
    sway = 0.0
    leg_r_y = 0.0
    leg_l_y = 0.0
    leg_r_z = 0.0
    leg_l_z = 0.0
    arm_r_swing = 0.0
    arm_l_swing = 0.0
    arm_r_lift = 0.0
    arm_l_lift = 0.0
    death_crumble = 1.0
    torso_rot = 0.0

    if anim == "idle":
        # Slow heavy breathing
        bob = math.sin(t * math.pi * 2) * 1.5
        torso_rot = math.sin(t * math.pi) * 0.1
        arm_r_swing = math.sin(t * math.pi * 2) * 2.0
        arm_l_swing = math.cos(t * math.pi * 2) * 2.0
        
    elif anim == "walk":
        # Heavy, earth-shaking lumber
        bob = abs(math.sin(t * math.pi * 2)) * 3.0
        torso_rot = math.cos(t * math.pi * 2) * 0.2
        
        leg_phase = t * math.pi * 2
        leg_r_y = math.sin(leg_phase) * 6
        leg_l_y = math.sin(leg_phase + math.pi) * 6
        
        leg_r_z = max(0, -math.cos(leg_phase)) * 4
        leg_l_z = max(0, -math.cos(leg_phase + math.pi)) * 4
        
        # Massive asymmetrical arms dragging as counter-weights
        arm_r_swing = -leg_r_y * 1.5
        arm_l_swing = -leg_l_y * 1.5
        arm_r_lift = leg_r_z * 0.5
        arm_l_lift = leg_l_z * 0.5
        
    elif anim == "attack":
        # Sweeping smash with right arm
        if t < 0.3:
            p = t / 0.3
            torso_rot = p * 0.5 # Wind up turn
            bob = p * 2.0
            arm_r_swing = -p * 12.0 # Pull back
            arm_r_lift = p * 15.0 # Lift high
            arm_l_swing = p * 6.0
        elif t < 0.6:
            p = (t - 0.3) / 0.3
            torso_rot = 0.5 - p * 1.0 # Twist forward into smash
            bob = 2.0 - p * 5.0 # Drop weight
            arm_r_swing = -12.0 + p * 24.0 # Smash forward
            arm_r_lift = 15.0 - p * 20.0 # Slam down below floor
            arm_l_swing = 6.0 - p * 8.0
        else:
            p = (t - 0.6) / 0.4
            torso_rot = -0.5 + p * 0.5 # Recover
            bob = -3.0 + p * 3.0
            arm_r_swing = 12.0 - p * 12.0
            arm_r_lift = -5.0 + p * 5.0
            arm_l_swing = -2.0 + p * 2.0
            
    elif anim == "death":
        # Completely falls apart into rubble
        death_crumble = max(0.1, 1.0 - t*1.5)
        bob = -t * 20 # Collapses

    random.seed(42 + int(t * 10))

    def add_sphere(bx, by, bz, r, color):
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_sphere(vol, nx, ny, nz, r, color)

    def add_box(bx, by, bz, wx, wy, wz, color, l_rot=0.0):
        """ Place a box respecting standard death crumble physics and base rotation """
        if anim == "death" and color != 4:
            bx += (random.random() - 0.5) * t * 20
            by += (random.random() - 0.5) * t * 20
            bz += (random.random() - 0.5) * t * 8
            wx *= death_crumble
            wy *= death_crumble
            wz *= death_crumble
            l_rot += (random.random() - 0.5) * t * math.pi
            
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_box(vol, nx, ny, nz, wx, wy, wz, color, rot_z=(angle + l_rot))
        
    def add_bone_c(bx1, by1, bz1, bx2, by2, bz2, r, color):
        """ Twisted rebar """
        if anim == "death": return # Rebars snap and disappear
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    def rot_pts(bx, by, rot):
        nx = bx * math.cos(rot) - by * math.sin(rot)
        ny = bx * math.sin(rot) + by * math.cos(rot)
        return nx, ny

    # 1. LEGS (Thick concrete pillars crossed with rebar)
    if anim != "death":
        # Right Leg (Bulky pillar)
        add_box(0, -6 + leg_r_y*0.5, -4 + bob, 2.5, 3.5, 5.0, 1) # Thigh
        add_box(0, -6 + leg_r_y, -14 + leg_r_z, 3.5, 4.0, 5.0, 2) # Foot
        add_bone_c(0, -6 + leg_r_y*0.5, -9 + bob, 0, -6 + leg_r_y, -10 + leg_r_z, 1.5, 3) # Knee rebar
        
        # Left Leg (Thinner, more exposed rebar)
        add_box(0, 6 + leg_l_y*0.5, -4 + bob, 2.0, 2.5, 4.0, 2) # Thigh
        add_bone_c(-1, 6 + leg_l_y*0.5, -8 + bob, -1, 6 + leg_l_y, -14 + leg_l_z, 0.8, 3) # Rebar shin
        add_bone_c(1, 6 + leg_l_y*0.5, -8 + bob, 1, 6 + leg_l_y, -14 + leg_l_z, 0.8, 3) # Rebar shin
        add_box(0, 6 + leg_l_y, -16 + leg_l_z, 4.0, 3.0, 2.0, 1) # Concrete block foot

    # 2. TORSO (Massive Jenga-like stack of brutalist slabs)
    tx, ty = rot_pts(0, 0, torso_rot)
    
    # Pelvis chunk
    add_box(tx, ty, 6 + bob, 7.0, 5.0, 4.0, 2, l_rot=torso_rot)
    
    # Mid-body slab (Angled)
    tx_mid, ty_mid = rot_pts(1, 0, torso_rot)
    add_box(tx_mid, ty_mid, 14 + bob, 6.0, 6.0, 5.0, 1, l_rot=torso_rot + 0.2)
    
    # Huge chest slab (Wide shoulder block)
    tx_top, ty_top = rot_pts(2, 0, torso_rot)
    add_box(tx_top, ty_top, 24 + bob, 5.0, 10.0, 6.0, 1, l_rot=torso_rot - 0.1)
    
    # Rebar spine connecting them
    add_bone_c(tx, ty, 10 + bob, tx_mid, ty_mid, 14 + bob, 1.2, 3)
    add_bone_c(tx_mid, ty_mid, 18 + bob, tx_top, ty_top, 24 + bob, 1.5, 3)
    
    # 3. CORE (Glowing through cracks)
    # The heart is a cluster of glowing acid green orbs embedded in the chest
    if anim != "death" or t < 0.6:
        core_x, core_y = rot_pts(6, 0, torso_rot)
        add_sphere(cx + core_x*math.cos(angle) - core_y*math.sin(angle), 
                   cy + core_x*math.sin(angle) + core_y*math.cos(angle), 
                   cz + 20 + bob, 2.5, 4)
        core_x, core_y = rot_pts(5, 2, torso_rot)
        add_sphere(cx + core_x*math.cos(angle) - core_y*math.sin(angle), 
                   cy + core_x*math.sin(angle) + core_y*math.cos(angle), 
                   cz + 22 + bob, 1.8, 4)

    # 4. HEAD (A broken pane of glass embedded in a dark concrete block)
    hx, hy = rot_pts(4, 0, torso_rot)
    add_box(hx, hy, 34 + bob, 3.5, 3.5, 4.0, 2, l_rot=torso_rot + 0.4) 
    # Glowing eye slit in the head
    eye_x, eye_y = rot_pts(7.5, 0, torso_rot)
    add_sphere(cx + eye_x*math.cos(angle) - eye_y*math.sin(angle), 
               cy + eye_x*math.sin(angle) + eye_y*math.cos(angle), 
               cz + 35 + bob, 1.5, 4)
    
    # 5. ARMS
    if anim != "death":
        # Right Arm (MASSIVE concrete pillar used as a sledgehammer)
        ar_sh_x, ar_sh_y = rot_pts(2, -10, torso_rot) # Shoulder
        ar_el_x, ar_el_y = rot_pts(1 + arm_r_swing*0.5, -12, torso_rot) # Elbow
        ar_ha_x, ar_ha_y = rot_pts(1 + arm_r_swing, -14, torso_rot) # Hand
        
        # Shoulder joint
        add_box(ar_sh_x, ar_sh_y, 24 + bob, 3.0, 3.0, 3.0, 2, l_rot=torso_rot)
        # Upper Arm (Rebar cluster)
        add_bone_c(ar_sh_x, ar_sh_y, 24+bob, ar_el_x, ar_el_y, 14+bob+arm_r_lift*0.5, 2.0, 3)
        # Lower Arm / Club (Giant block of concrete/glass)
        add_box(ar_ha_x, ar_ha_y, 4+bob+arm_r_lift, 4.0, 4.0, 10.0, 1, l_rot=torso_rot + (arm_r_swing*0.1))
        # Spikes extending from the club
        add_bone_c(ar_ha_x, ar_ha_y, 4+bob+arm_r_lift, ar_ha_x+4, ar_ha_y, -6+bob+arm_r_lift, 1.0, 3)
        
        # Left Arm (Shattered, asymmetrical, made of glass panes and steel)
        al_sh_x, al_sh_y = rot_pts(2, 10, torso_rot)
        al_el_x, al_el_y = rot_pts(1 + arm_l_swing*0.5, 12, torso_rot)
        al_ha_x, al_ha_y = rot_pts(1 + arm_l_swing, 14, torso_rot)
        
        # Shoulder joint
        add_box(al_sh_x, al_sh_y, 24 + bob, 2.5, 2.5, 2.5, 2, l_rot=torso_rot)
        # Upper Arm
        add_bone_c(al_sh_x, al_sh_y, 24+bob, al_el_x, al_el_y, 16+bob+arm_l_lift*0.5, 1.2, 3)
        # Forearm is a pane of shattered teal glass
        add_box(al_ha_x, al_ha_y, 10+bob+arm_l_lift, 1.0, 4.0, 6.0, 5, l_rot=torso_rot - 0.2)

    return render_volume(vol, anim, t)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/colosse_urban"
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
    
    print("Generating Colosse Urban animations (128x128 Massive Voxel Golem)...")
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (128 * frames, 128), (0, 0, 0, 0)) 
            for f in range(frames):
                try:
                    t = f / float(frames)
                    img = build_colosse_frame(angle, t, anim)
                    
                    filename = f"enemy_colosse_urban_{d_name}_{anim}_{f:02d}.png"
                    img.save(os.path.join(out_dir, filename))
                    
                    sheet.paste(img, (f * 128, 0))
                except Exception as e:
                    print(f"Error drawing frame {f} for {d_name} {anim}: {e}")
                
            sheet_name = f"enemy_colosse_urban_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            
    print("Colosse Urban generation complete.")

if __name__ == "__main__":
    main()
