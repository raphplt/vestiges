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
                    # Acid green eyes (color 2)
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
                        
                        # Palettes for Fading Spitter
                        # The json had #CC5533 (Warm reddish orange / rusty) but we should keep it pale
                        # "Cracheur Pâli" -> Pale, bloated flesh
                        if col == 1: # Pale bloated flesh (Base pale grey/pinkish)
                            if top_lit: c = (210, 200, 205, 255)
                            elif side_lit: c = (180, 160, 170, 255)
                            else: c = (130, 110, 120, 255)
                        elif col == 2: # Acid Green Eyes / Glow #7FFF00
                            if top_lit: c = (180, 255, 100, 255)
                            else: c = (127, 255, 0, 255)
                        elif col == 3: # Rusty crust / mineral plates (#CC5533 related)
                            if top_lit: c = (190, 100, 80, 255)
                            elif side_lit: c = (150, 70, 50, 255)
                            else: c = (100, 40, 30, 255)
                        elif col == 4: # Deep glowing acid inside the sac/mouth
                            if top_lit: c = (140, 255, 50, 255)
                            else: c = (90, 200, 10, 255)
                        elif col == 5: # Void black (for orifices)
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

def build_cracheur_preview(angle_deg):
    vol = create_volume()
    cx, cy, cz = 24, 24, 8 
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

    # Design: "Fading Spitter"
    # A bloated, pale sac-like creature supported by three or four spindly legs.
    # It has a huge gaping orifice on its "face" that glows with inner acid.
    # Rusty/Mineral crusts anchor it.
    
    # Bloated Pale Body
    add_sphere(0, 0, 10, 6.0, 1) # Main sac
    add_sphere(2, 0, 12, 5.0, 1) # Front bulge
    
    # Rusty Crust on the back
    add_sphere(-2, 0, 14, 4.5, 3) 
    add_sphere(-4, 0, 12, 4.0, 3)
    
    # The Spitting "Face"
    # A massive fleshy opening protruding forward
    add_sphere(6, 0, 13, 3.5, 1) 
    # The orifice (Void black and glowing acid inside)
    # To carve an orifice, we just draw the void color on the outer rim and acid inside
    add_sphere(7, 0, 13, 2.5, 1) # Lip
    add_sphere(7.5, 0, 13, 1.8, 5) # Dark mouth
    add_sphere(6.5, 0, 13, 1.0, 4) # Glowing acid core deep inside
    
    # Acid leaking from the mouth
    add_bone(7.5, 0, 11, 8, 0, 5, 0.8, 4)
    
    # Asymmetrical Eyes clustered around the orifice
    add_sphere(5, 3, 15, 1.2, 2)
    add_sphere(5, -2.5, 15, 0.8, 2)
    add_sphere(6, -1.5, 16, 0.6, 2)
    
    # Spindly, insectoid/mineral legs to drag this bloated sac
    # Leg 1 (Front Right)
    add_bone(2, 4, 8, 4, 6, 12, 1.2, 3)
    add_bone(4, 6, 12, 6, 8, 0, 1.0, 3)
    
    # Leg 2 (Front Left)
    add_bone(2, -4, 8, 3, -6, 10, 1.2, 3)
    add_bone(3, -6, 10, 5, -8, 0, 1.0, 3)
    
    # Leg 3 (Back Right)
    add_bone(-3, 3, 8, -5, 5, 10, 1.5, 3)
    add_bone(-5, 5, 10, -7, 6, 0, 1.2, 3)

    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/enemies/cracheur"
    os.makedirs(out_dir, exist_ok=True)
    
    # Just generating SE idle frame 00 for validation
    img = build_cracheur_preview(0) # SE
    img.save(os.path.join(out_dir, "enemy_cracheur_SE_idle_00.png"))
    print("Generated preview at:", os.path.join(out_dir, "enemy_cracheur_SE_idle_00.png"))

if __name__ == "__main__":
    main()
