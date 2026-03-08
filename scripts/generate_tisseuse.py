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
                    if vol[x][y][z] == 0 or color in (2, 4): # Overwrite priority for eyes/acid & joints
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
                        
                        c = (255, 0, 255, 255) # Magenta error
                        
                        # Palettes for the Tisseuse
                        if col == 1: # Dark Fleshy Body
                            if top_lit: c = (60, 40, 50, 255)
                            elif side_lit: c = (40, 25, 35, 255)
                            else: c = (25, 15, 20, 255)
                        elif col == 2: # Acid Green Core / Eyes (#7FFF00)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                            if anim == "death":
                                c = (c[0], c[1], c[2], int(255 * max(0, 1.0 - t*2.0)))
                        elif col == 3: # Pale Stringy Legs (Bone/Web)
                            if top_lit: c = (200, 190, 180, 255)
                            elif side_lit: c = (160, 150, 140, 255)
                            else: c = (110, 100, 95, 255)
                        elif col == 4: # Dark Joints / Connecting points
                            if top_lit: c = (45, 40, 45, 255)
                            elif side_lit: c = (25, 20, 25, 255)
                            else: c = (15, 10, 15, 255)
                        elif col == 5: # Webbing string (Attack)
                            c = (127, 255, 0, 200) # Semi-transp Green
                        
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
                    # Dark outline matching the vibe
                    outline_color = (10, 5, 10, 255)
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

def build_tisseuse_frame(angle_deg, t=0.0, anim="idle"):
    vol = create_volume()
    cx, cy, cz = 16, 16, 12 # Higher default Z, suspends itself on long legs
    angle = math.radians(angle_deg)
    
    # Anim mods
    pulse = 0.0
    core_z = 0.0
    leg_spread = 0.0
    death_crumble = 1.0
    attack_web_z = 0.0
    attack_web_spread = 0.0
    
    # 8 Legs tracking
    legs_phase = [0, 0.5, 0.25, 0.75, 0.1, 0.6, 0.35, 0.85] # Asynchronous movement

    if anim == "idle":
        pulse = math.sin(t * math.pi * 4) * 0.5
        core_z = math.sin(t * math.pi * 2) * 1.5
        
    elif anim == "walk":
        pulse = abs(math.sin(t * math.pi * 8)) * 1.0
        core_z = math.sin(t * math.pi * 4) * 1.0 - 2.0 # Slightly lower when crawling
        
    elif anim == "attack":
        # Rears up, front legs flail, weaves a glowing web in front
        if t < 0.4:
            p = t / 0.4
            core_z = p * 6.0
            pulse = p * 2.0
            leg_spread = -p * 2.0
        elif t < 0.7:
            p = (t - 0.4) / 0.3
            core_z = 6.0
            pulse = 2.0 + math.sin(p * math.pi * 10) # Heavy throb
            leg_spread = -2.0
            attack_web_z = p * 8.0
            attack_web_spread = p * 6.0
        else:
            p = (t - 0.7) / 0.3
            core_z = 6.0 - p * 6.0
            pulse = 2.0 - p * 2.0
            leg_spread = -2.0 + p * 2.0
            attack_web_z = 8.0 - p * 8.0 # Throws it down/forward
            attack_web_spread = 6.0 + p * 8.0 # Web expands massively

    elif anim == "death":
        death_crumble = max(0.1, 1.0 - t*2.0)
        core_z = -t * 10 # Drops to the floor
        pulse = -t * 2 # Deflates
        leg_spread = t * 6 # Legs collapse outward

    random.seed(42 + int(t * 10))

    def rot_z(bx, by, rot):
        nx = bx * math.cos(rot) - by * math.sin(rot)
        ny = bx * math.sin(rot) + by * math.cos(rot)
        return nx, ny

    def add_sphere(bx, by, bz, r, color):
        if anim == "death" and color != 2:
            bx += (random.random() - 0.5) * t * 15
            by += (random.random() - 0.5) * t * 15
            bz += (random.random() - 0.5) * t * 5
            r *= death_crumble
            
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone(bx1, by1, bz1, bx2, by2, bz2, r, color):
        if anim == "death": return # Legs snap/dissolve
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    # 1. CORE BODY (Small, pulsating)
    body_r = 3.5 + pulse
    add_sphere(0, 0, core_z, body_r, 1)
    
    # Tiny glowing acid core/eyes randomly dotting the surface
    add_sphere(1.5, -1.0, core_z + 1.0 + pulse, 1.0, 2)
    add_sphere(2.5, 0.5, core_z - 0.5 + pulse, 1.2, 2)
    add_sphere(-1.0, 2.0, core_z + 2.0 + pulse, 0.8, 2)
    add_sphere(-2.0, -1.5, core_z - 1.0 + pulse, 0.6, 2)
    add_sphere(0.0, 0.0, core_z - 3.0, 1.5, 2) # Underside spinneret

    # 2. LONG SPRAWLING LEGS (8 Legs)
    # The Tisseuse doesn't have a rigid structure, its legs attach irregularly
    leg_bases = [
        ( 2.5, -2.5), ( 3.0,  0.0), ( 2.5,  2.5), ( 0.0,  3.0),
        (-2.5,  2.5), (-3.0,  0.0), (-2.5, -2.5), ( 0.0, -3.0)
    ]
    
    for i, (bx, by) in enumerate(leg_bases):
        # Base Phase for animation
        phase = (t + legs_phase[i]) * math.pi * 2
        
        # Calculate joint position (High up)
        # Legs sprawl far out (radius 8 to 12)
        spread_dist = 9.0 + leg_spread
        if i % 2 == 0: spread_dist += 3.0 # Alternate lengths
        
        # Angle from center
        leg_ang = math.atan2(by, bx)
        
        # Walk mechanic: Lift and reach
        lift = 0
        reach = 0
        if anim == "walk":
            lift = math.sin(phase) * 4.0
            reach = math.cos(phase) * 3.0
            if lift < 0: lift = 0 # Only lift up
            
        elif anim == "attack":
            # Front legs (closest to angle 0, i.e. facing direction)
            # In local coords, facing is +X (angle 0)
            if i in [0, 1, 2, 7]: # Front-ish legs
                if t >= 0.4:
                    lift = 6.0 + math.sin(t * math.pi * 15 + i) * 2.0 # Flail aggressively
                    reach = 4.0
                else:
                    lift = (t/0.4) * 6.0
                    reach = (t/0.4) * 4.0
            else:
                # Back legs anchor harder
                lift = 0.0
                reach = -2.0
        
        # Knee position (High arch)
        kx = math.cos(leg_ang) * (spread_dist * 0.5 + reach)
        ky = math.sin(leg_ang) * (spread_dist * 0.5 + reach)
        kz = core_z + 6.0 + lift + (pulse * 0.5)
        
        # Foot position (Far out, on the ground z = -12)
        fx = math.cos(leg_ang) * (spread_dist + reach * 1.5)
        fy = math.sin(leg_ang) * (spread_dist + reach * 1.5)
        fz = -12.0 + lift
        if fz < -12.0: fz = -12.0 # Floor clip
        
        if anim != "death":
            # Joint at body
            add_sphere(bx, by, core_z, 1.2, 4)
            # Thigh (Stringy)
            add_bone(bx, by, core_z, kx, ky, kz, 0.7, 3)
            # Knee Joint
            add_sphere(kx, ky, kz, 1.0, 4)
            # Calf (Very thin)
            add_bone(kx, ky, kz, fx, fy, fz, 0.4, 3)
            # Foot spike
            add_sphere(fx, fy, fz, 0.8, 1)

    # 3. ATTACK WEBBING (Volumetric thread emission)
    if anim == "attack" and t >= 0.4:
        # Weaves an erratic bright green web in front (+X direction)
        web_center_x = 8.0 + attack_web_spread
        web_center_y = 0.0
        web_center_z = core_z - 2.0 - attack_web_z
        
        # Draw erratic web lines
        for i in range(5):
            wx1 = web_center_x + (random.random() - 0.5) * attack_web_spread * 1.5
            wy1 = web_center_y + (random.random() - 0.5) * attack_web_spread * 2.0
            wz1 = web_center_z + (random.random() - 0.5) * attack_web_spread * 2.0
            
            wx2 = web_center_x + (random.random() - 0.5) * attack_web_spread * 1.5
            wy2 = web_center_y + (random.random() - 0.5) * attack_web_spread * 2.0
            wz2 = web_center_z + (random.random() - 0.5) * attack_web_spread * 2.0
            
            # Connect to spinneret occasionally
            if random.random() > 0.5:
                wx1, wy1, wz1 = 0.0, 0.0, core_z - 3.0
                
            add_bone(wx1, wy1, wz1, wx2, wy2, wz2, 0.8, 5)
            # Nodules
            add_sphere(wx2, wy2, wz2, 1.2, 2)

    return render_volume(vol, anim, t)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/tisseuse"
    os.makedirs(out_dir, exist_ok=True)
    
    # Godot isometric logic
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
    
    print("Generating Tisseuse animations (Voxel Formless Arachnid)...")
    
    for d_name, angle in dirs.items():
        for anim, frames in anims.items():
            sheet = Image.new("RGBA", (64 * frames, 64), (0, 0, 0, 0)) 
            for f in range(frames):
                try:
                    t = f / float(frames)
                    img = build_tisseuse_frame(angle, t, anim)
                    
                    filename = f"enemy_tisseuse_{d_name}_{anim}_{f:02d}.png"
                    img.save(os.path.join(out_dir, filename))
                    
                    sheet.paste(img, (f * 64, 0))
                except Exception as e:
                    print(f"Error drawing frame {f} for {d_name} {anim}: {e}")
                
            sheet_name = f"enemy_tisseuse_{d_name}_{anim}_sheet.png"
            sheet.save(os.path.join(out_dir, sheet_name))
            
    print("Tisseuse generation complete.")

if __name__ == "__main__":
    main()
