import os
import math
import random
from PIL import Image

def create_volume():
    # Large 64x64 volume to allow grand scale
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
                    if color == 2:
                        vol[x][y][z] = 2
                    elif vol[x][y][z] not in (2, 5): # 5 is void
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
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0)) # Plenty of space
    depth_buf = [[-9999]*128 for _ in range(128)]
    
    for z in range(64):
        for y in range(64):
            for x in range(64):
                col = vol[x][y][z]
                if col == 0: continue
                
                # Isometric projection
                sx = int(64 + x - y)
                sy = int(60 + (x + y) / 2.0 - z) # perfectly centered
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 128 and 0 <= sy < 128:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 63 or vol[x][y][z+1] == 0)
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
                        elif col == 3: # Swollen Bark (Face features) sickly grayish brown to blend
                            if top_lit: c = (110, 100, 80, 255)
                            elif side_lit: c = (85, 80, 60, 255)
                            else: c = (60, 55, 40, 255)
                        elif col == 4: # Thick dark canopy (Prominent but terrifying)
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

def build_treant_preview(angle_deg):
    vol = create_volume()
    cx, cy, cz = 32, 32, 10 # Base center.
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

    # ==== SCARY CORRUPTED TREE DESIGN ====
    # Wide, gnarled base trunk. Leaning forward menacingly like a hunchback.
    
    # TRUNK
    add_bone(0, 0, 0, 2, 0, 15, 7.0, 1) # Lower trunk (thick)
    add_bone(2, 0, 15, 6, 0, 30, 6.0, 1) # Leaning forward
    add_bone(6, 0, 30, 8, 0, 42, 5.0, 1) # Top spine

    # ROOT LEGS (Crawling, scary, spider-like)
    add_bone(0, 0, 2, 10, 8, -4, 2.5, 1) # Front right
    add_bone(10, 8, -4, 14, 12, -7, 1.5, 1)
    
    add_bone(0, 0, 2, 10, -8, -4, 2.5, 1) # Front left
    add_bone(10, -8, -4, 14, -12, -7, 1.5, 1)
    
    add_bone(0, 0, 4, -8, 6, -2, 2.5, 1) # Back right
    add_bone(0, 0, 4, -8, -6, -2, 2.5, 1) # Back left
    
    # HUGE JAGGED CLAW-ARMS (Branches)
    # Right Arm
    add_bone(5, 7, 28, 8, 16, 20, 3.5, 1) # Shoulder
    add_bone(8, 16, 20, 15, 14, 12, 2.5, 1) # Forearm
    add_bone(15, 14, 12, 22, 18, 6, 1.5, 1) # Spike finger 1
    add_bone(15, 14, 12, 18, 10, 4, 1.5, 1) # Spike finger 2
    
    # Left Arm
    add_bone(5, -7, 28, 8, -16, 20, 3.5, 1)
    add_bone(8, -16, 20, 16, -12, 12, 2.5, 1)
    add_bone(16, -12, 12, 24, -16, 5, 1.5, 1) # Spike finger 1
    add_bone(16, -12, 12, 20, -8, 3, 1.5, 1) # Spike finger 2

    # THE FACES IN THE BARK
    # Formed of a slightly paler greenish-brown bark (Color 3) pushing out from the main trunk.
    # A gigantic screaming face stretched down the front of the trunk.
    
    # "Forehead" / Upper brow protruding forward
    add_bone(7, -4, 32, 7, 4, 32, 2.0, 3) 
    # Eyes deeply sunken under the brow
    add_sphere(8, -2.5, 30, 2.0, 5) # Void socket
    add_sphere(8.5, -2.5, 30, 0.8, 2) # Glowing eye
    
    add_sphere(8, 2.5, 30, 2.0, 5) # Void socket
    add_sphere(8.5, 2.5, 30, 0.8, 2) # Glowing eye
    
    # Nose/Bridge
    add_bone(7, 0, 30, 9, 0, 22, 1.5, 3)
    
    # Cheeks
    add_bone(6, -4, 25, 8, -4, 18, 2.0, 3)
    add_bone(6, 4, 25, 8, 4, 18, 2.0, 3)
    
    # Massive gaping screaming mouth (void)
    add_sphere(8, 0, 14, 4.5, 5) # Inner dark void
    add_bone(8, -3, 18, 8, 3, 18, 1.5, 3) # Upper lip
    add_bone(6, -4, 10, 6, 4, 10, 1.5, 3) # Lower lip dropping VERY low, jaw unhinged
    # Drooling acid from the maw
    add_bone(7, -1, 10, 9, -1, 5, 0.8, 2)
    add_bone(7, 2, 10, 8, 2, 3, 0.6, 2)

    # MASSIVE OPPRESSIVE CANOPY (Dark leaves drooping)
    # Top crown
    add_sphere(7, 0, 45, 9.0, 4)
    add_sphere(5, 6, 42, 6.0, 4)
    add_sphere(5, -6, 42, 6.0, 4)
    add_sphere(2, 0, 40, 8.0, 4)
    
    # Drooping moss/vines hanging over the "shoulders"
    add_bone(5, 6, 42, 2, 9, 26, 2.5, 4)
    add_bone(5, -6, 42, 2, -9, 26, 2.5, 4)
    
    add_bone(9, 0, 45, 11, 0, 34, 1.5, 4) # Drops right over the forehead

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/treant_corrompu"
    os.makedirs(out_dir, exist_ok=True)
    
    # Just generating SE idle frame 00 for validation
    img = build_treant_preview(0) # SE
    img.save(os.path.join(out_dir, "enemy_treant_corrompu_SE_idle_00.png"))
    print("Generated preview at:", os.path.join(out_dir, "enemy_treant_corrompu_SE_idle_00.png"))

if __name__ == "__main__":
    main()
