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

def render_volume(vol):
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
                        # A sickly, dark, decaying humanoid form.
                        if col == 1: # Torso/Limbs: Dark purplish-gray fleshy decay
                            if top_lit: c = (120, 100, 115, 255)
                            elif side_lit: c = (90, 70, 85, 255)
                            else: c = (60, 45, 55, 255)
                        elif col == 2: # Acid green eyes (VERY PROMINENT)
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
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

def build_rodeur_preview(angle_deg):
    vol = create_volume()
    cx, cy, cz = 16, 16, 8 
    angle = math.radians(angle_deg)
    
    def add_sphere(bx, by, bz, r, color):
        nx = cx + (bx * math.cos(angle) - by * math.sin(angle))
        ny = cy + (bx * math.sin(angle) + by * math.cos(angle))
        nz = cz + bz
        draw_sphere(vol, nx, ny, nz, r, color)
        
    def add_bone(bx1, by1, bz1, bx2, by2, bz2, r, color):
        nx1 = cx + (bx1 * math.cos(angle) - by1 * math.sin(angle))
        ny1 = cy + (bx1 * math.sin(angle) + by1 * math.cos(angle))
        nz1 = cz + bz1
        nx2 = cx + (bx2 * math.cos(angle) - by2 * math.sin(angle))
        ny2 = cy + (bx2 * math.sin(angle) + by2 * math.cos(angle))
        nz2 = cz + bz2
        draw_bone(vol, nx1, ny1, nz1, nx2, ny2, nz2, r, color)

    # 1. LEGS & PELVIS (Clear human bipedal stance)
    # Tattered dark pants to make it read as an erased human
    add_bone(0, -2, -2, 0, -2, -8, 1.2, 5) # Right leg
    add_bone(0, -2, -8, 1, -2, -8, 0.8, 4)  # Bare distinct right foot
    
    add_bone(0, 2, -2, 0, 2, -8, 1.2, 5) # Left leg
    add_bone(0, 2, -8, 1, 2, -8, 0.8, 4)  # Bare distinct left foot
    
    # Pelvis
    add_sphere(0, 0, -1, 2.5, 5) 

    # 2. TORSO (Hunched forward aggressively)
    add_bone(0, 0, 0, 3, 0, 4, 3.0, 1) # Lower back leaning forward
    add_bone(3, 0, 4, 6, 0, 7, 2.8, 1) # Upper back, severely hunched
    
    # Protruding spine / hunched shoulders
    add_sphere(5, 0, 9, 2.5, 1)
    
    # 3. OVERSIZED DRAGGING ARMS
    # They attach high up at the hunched shoulders, and drop all the way to the floor
    # Right arm
    add_bone(5, -3, 8, 4, -5, 3, 1.8, 1) # Shoulder to elbow
    add_bone(4, -5, 3, 5, -4, -4, 1.4, 1) # Elbow to wrist
    add_bone(5, -4, -4, 7, -3, -8, 1.5, 4) # HUGE dark hand dragging on the ground
    
    # Left arm (Longer, more twisted)
    add_bone(5, 3, 8, 3, 5, 2, 1.8, 1) # Shoulder to elbow
    add_bone(3, 5, 2, 3, 6, -5, 1.4, 1) # Elbow to wrist
    add_bone(3, 6, -5, 6, 5, -8, 1.5, 4) # HUGE dark hand dragging


    # 4. THE PALE FACE & EYES
    # Very distinct, pale, featureless blob
    add_sphere(8, 0, 8, 2.2, 3) # Head pushing forward from the hunch
    
    # Acid Green Eyes (huge and glowing)
    # The lore says "amas de traits" so we place multiple asymmetrical eyes on the pale face
    add_sphere(9, -1, 9, 0.8, 2) # Large main eye
    add_sphere(9.5, 1.2, 8.5, 0.6, 2) # Secondary eye
    add_sphere(8.8, 0, 7.5, 0.4, 2) # Small weird eye lower down

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/rodeur"
    os.makedirs(out_dir, exist_ok=True)
    
    # Just generating SE idle frame 00 for validation
    img = build_rodeur_preview(0) # SE
    img.save(os.path.join(out_dir, "enemy_rodeur_SE_idle_00.png"))
    print("Generated preview at:", os.path.join(out_dir, "enemy_rodeur_SE_idle_00.png"))

if __name__ == "__main__":
    main()
