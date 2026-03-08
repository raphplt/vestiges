import os
import math
import random
from PIL import Image

def create_volume():
    # 32x32 footprint specifically to fit perfectly in 64x64 isometric tile (32+32=64 width)
    # Z can be up to 48 for height
    return [[[0]*48 for _ in range(32)] for __ in range(32)]

def draw_sphere(vol, cx, cy, cz, r, color):
    min_x = max(0, int(cx - r))
    max_x = min(31, int(cx + r))
    min_y = max(0, int(cy - r))
    max_y = min(31, int(cy + r))
    min_z = max(0, int(cz - r))
    max_z = min(47, int(cz + r))
    r_sq = r * r
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                if (x - cx)**2 + (y - cy)**2 + (z - cz)**2 <= r_sq:
                    if vol[x][y][z] == 0 or color in (10, 11, 12):
                        vol[x][y][z] = color

def draw_box(vol, cx, cy, cz, wx, wy, wz, color):
    min_x = max(0, int(cx - wx))
    max_x = min(31, int(cx + wx))
    min_y = max(0, int(cy - wy))
    max_y = min(31, int(cy + wy))
    min_z = max(0, int(cz - wz))
    max_z = min(47, int(cz + wz))
    
    for x in range(min_x, max_x + 1):
        for y in range(min_y, max_y + 1):
            for z in range(min_z, max_z + 1):
                if vol[x][y][z] == 0 or color in (10, 11, 12):
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

def render_volume(vol, outline_color=(20, 25, 20, 255)):
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    depth_buf = [[-9999]*64 for _ in range(64)]
    
    for z in range(48):
        for y in range(32):
            for x in range(32):
                col = vol[x][y][z]
                if col == 0: continue
                
                # Isometric 2:1 projection
                # 32x32 footprint -> dx = 31...-31 -> width ~ 63
                sx = int(31 + x - y)
                sy = int(32 + (x + y) / 2.0 - z) 
                
                depth = -x - y + 1.5 * z
                
                if 0 <= sx < 64 and 0 <= sy < 64:
                    if depth > depth_buf[sy][sx]:
                        depth_buf[sy][sx] = depth
                        
                        top_lit = (z == 47 or vol[x][y][z+1] == 0)
                        front_x = (x == 0 or vol[x-1][y][z] == 0)
                        front_y = (y == 0 or vol[x][y-1][z] == 0)
                        side_lit = (front_x or front_y)
                        
                        c = (255, 0, 255, 255) # Magenta error
                        
                        if col == 1: # Wood logs (Brown/Beige)
                            if top_lit: c = (120, 90, 60, 255)
                            elif side_lit: c = (90, 60, 40, 255)
                            else: c = (60, 40, 25, 255)
                        elif col == 2: # Dark Wood
                            if top_lit: c = (90, 60, 40, 255)
                            elif side_lit: c = (60, 40, 25, 255)
                            else: c = (40, 25, 15, 255)
                        elif col == 3: # Stone / Cobble (Gray)
                            if top_lit: c = (140, 140, 145, 255)
                            elif side_lit: c = (100, 100, 105, 255)
                            else: c = (65, 65, 75, 255)
                        elif col == 4: # Dark Stone
                            if top_lit: c = (100, 100, 105, 255)
                            elif side_lit: c = (65, 65, 75, 255)
                            else: c = (40, 40, 50, 255)
                        elif col == 5: # Metal (Dark teal/Blue-grey steel)
                            if top_lit: c = (120, 130, 140, 255)
                            elif side_lit: c = (80, 90, 100, 255)
                            else: c = (40, 50, 60, 255)
                        elif col == 6: # Rusted Metal / Rivets
                            if top_lit: c = (80, 50, 40, 255)
                            elif side_lit: c = (50, 30, 25, 255)
                            else: c = (25, 15, 15, 255)
                        elif col == 7: # Foliage / Moss (Forest Green)
                            if top_lit: c = (80, 115, 65, 255)
                            elif side_lit: c = (58, 90, 48, 255) 
                            else: c = (40, 60, 35, 255)
                        elif col == 10: # Fire Core (White/Yellow)
                            c = (255, 255, 200, 255)
                        elif col == 11: # Fire Mid (Orange)
                            c = (255, 150, 50, 230)
                        elif col == 12: # Fire Edge (Red)
                            c = (200, 50, 20, 180)
                            
                        if col in (3, 4) and top_lit and side_lit:
                            c = (min(255, c[0]+20), min(255, c[1]+20), min(255, c[2]+20), 255) 
                            
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
                    out_img.putpixel((px, py), outline_color)
                        
    return out_img

# --- Bitmask Wall Generators (16 Frames) ---
# Godot 4-way bitmask: 1=TopRight(NE), 2=BottomRight(SE), 4=BottomLeft(SW), 8=TopLeft(NW)
# In our Voxel grid:
# +X is SE (Bottom-Right on screen)
# +Y is SW (Bottom-Left on screen)
# -X is NW (Top-Left on screen)
# -Y is NE (Top-Right on screen)

def build_wall_framework(vol, bitmask, cx=15, cy=15, thickness=2, draw_segment_func=None):
    # Core pillar logic
    if draw_segment_func:
        # bit 1 (NE, -Y)
        if bitmask & 1: draw_segment_func(vol, cx, cy - 8, thickness, 8, True)
        # bit 2 (SE, +X)
        if bitmask & 2: draw_segment_func(vol, cx + 8, cy, 8, thickness, False)
        # bit 4 (SW, +Y)
        if bitmask & 4: draw_segment_func(vol, cx, cy + 8, thickness, 8, True)
        # bit 8 (NW, -X)
        if bitmask & 8: draw_segment_func(vol, cx - 8, cy, 8, thickness, False)
        # Always draw center core
        draw_segment_func(vol, cx, cy, thickness, thickness, None)

def gen_mur_bois_frame(bitmask):
    vol = create_volume()
    def seg(v, x, y, wx, wy, is_y):
        # Draw horizontal logs layered up
        for z in range(0, 16, 4):
            draw_box(v, x, y, z, wx, wy, 1.5, 1)
            draw_box(v, x, y, z, wx-0.5, wy-0.5, 2.0, 2)
    build_wall_framework(vol, bitmask, thickness=3, draw_segment_func=seg)
    return render_volume(vol)

def gen_mur_pierre_frame(bitmask):
    vol = create_volume()
    def seg(v, x, y, wx, wy, is_y):
        draw_box(v, x, y, 7, wx, wy, 7, 3) # Core
        draw_box(v, x, y, 15, wx, wy, 1, 4) # Top lip
        # Brick details logic simplified for autotiles
        if is_y is not None:
            if is_y: # Y axis
                draw_box(v, x, y, 4, wx+0.5, wy, 1.5, 4)
                draw_box(v, x, y, 10, wx+0.5, wy, 1.5, 4)
            else: # X axis
                draw_box(v, x, y, 4, wx, wy+0.5, 1.5, 4)
                draw_box(v, x, y, 10, wx, wy+0.5, 1.5, 4)
        else: # Center
            draw_box(v, x, y, 4, wx+0.5, wy+0.5, 1.5, 4)
            draw_box(v, x, y, 10, wx+0.5, wy+0.5, 1.5, 4)
    build_wall_framework(vol, bitmask, thickness=3, draw_segment_func=seg)
    return render_volume(vol)

def gen_mur_metal_frame(bitmask):
    vol = create_volume()
    def seg(v, x, y, wx, wy, is_y):
        draw_box(v, x, y, 8, wx, wy, 8, 5) # Core Steel
        # Rivet borders
        if is_y is not None:
            if is_y: 
                draw_box(v, x, y, 8, 1, wy, 0.5, 6) # horizontal rusty line
                draw_box(v, x, y, 16, wx, wy, 0.5, 6)
            else:
                draw_box(v, x, y, 8, wx, 1, 0.5, 6)
                draw_box(v, x, y, 16, wx, wy, 0.5, 6)
        else:
            draw_box(v, x, y, 16, wx, wy, 0.5, 5) # Top plate
    build_wall_framework(vol, bitmask, thickness=2.5, draw_segment_func=seg)
    return render_volume(vol)

def apply_tiling(func_name, generator):
    sheet = Image.new("RGBA", (64 * 4, 64 * 4), (0, 0, 0, 0)) 
    for bitmask in range(16):
        # 4x4 Grid
        grid_x = bitmask % 4
        grid_y = bitmask // 4
        img = generator(bitmask)
        sheet.paste(img, (grid_x * 64, grid_y * 64))
    return sheet

# --- Doors (2 Orientations: SE/NW and SW/NE) ---
# Orientation 0: Along X axis (SE / NW) -> connects bit 2 and 8
# Orientation 1: Along Y axis (NE / SW) -> connects bit 1 and 4

def gen_porte_bois_frame(orient):
    vol = create_volume()
    cx, cy = 15, 15
    if orient == 0:
        draw_box(vol, cx-8, cy, 8, 2, 3, 8, 2) # frame left
        draw_box(vol, cx+8, cy, 8, 2, 3, 8, 2) # frame right
        draw_box(vol, cx, cy, 16, 10, 3, 2, 2) # lintel
        draw_box(vol, cx, cy, 7, 6, 1, 7, 1) # Door slightly open (offset y)
    else:
        draw_box(vol, cx, cy-8, 8, 3, 2, 8, 2) 
        draw_box(vol, cx, cy+8, 8, 3, 2, 8, 2) 
        draw_box(vol, cx, cy, 16, 3, 10, 2, 2) 
        draw_box(vol, cx, cy, 7, 1, 6, 7, 1) 
    return render_volume(vol)

def gen_porte_pierre_frame(orient):
    vol = create_volume()
    cx, cy = 15, 15
    if orient == 0:
        draw_box(vol, cx-8, cy, 8, 4, 3, 8, 4) 
        draw_box(vol, cx+8, cy, 8, 4, 3, 8, 4) 
        draw_box(vol, cx, cy, 16, 10, 4, 2, 3) 
        for x in range(-6, 7, 3): draw_box(vol, cx+x, cy, 6, 0.5, 0.5, 8, 5)
        for z in range(0, 14, 4): draw_box(vol, cx, cy, z, 8, 0.5, 0.5, 5)
    else:
        draw_box(vol, cx, cy-8, 8, 3, 4, 8, 4) 
        draw_box(vol, cx, cy+8, 8, 3, 4, 8, 4) 
        draw_box(vol, cx, cy, 16, 4, 10, 2, 3) 
        for y in range(-6, 7, 3): draw_box(vol, cx, cy+y, 6, 0.5, 0.5, 8, 5)
        for z in range(0, 14, 4): draw_box(vol, cx, cy, z, 0.5, 8, 0.5, 5)
    return render_volume(vol)

def gen_porte_metal_frame(orient):
    vol = create_volume()
    cx, cy = 15, 15
    if orient == 0:
        draw_box(vol, cx-10, cy, 10, 2, 4, 10, 5)
        draw_box(vol, cx+10, cy, 10, 2, 4, 10, 5)
        draw_box(vol, cx, cy, 20, 12, 4, 2, 5)
        draw_box(vol, cx, cy+2, 10, 8, 2, 10, 5)
        draw_sphere(vol, cx, cy-1, 10, 4, 6)
    else:
        draw_box(vol, cx, cy-10, 10, 4, 2, 10, 5)
        draw_box(vol, cx, cy+10, 10, 4, 2, 10, 5)
        draw_box(vol, cx, cy, 20, 4, 12, 2, 5)
        draw_box(vol, cx+2, cy, 10, 2, 8, 10, 5)
        draw_sphere(vol, cx-1, cy, 10, 4, 6)
    return render_volume(vol)

# --- Props --- (Using smaller footprint and strictly centered)
def gen_props(logic_func):
    vol = create_volume()
    cx, cy, cz = 15, 15, 0 
    logic_func(vol, cx, cy, cz)
    return render_volume(vol)

def logic_barricade(vol, cx, cy, cz):
    draw_bone(vol, cx-6, cy-6, cz, cx+6, cy+6, cz+12, 1.5, 1)
    draw_bone(vol, cx-3, cy-9, cz, cx+9, cy+3, cz+10, 1.5, 1)
    draw_bone(vol, cx-9, cy-3, cz, cx+3, cy+9, cz+14, 1.5, 1)
    draw_bone(vol, cx-8, cy+8, cz+6, cx+8, cy-8, cz+6, 2.0, 2)
    draw_sphere(vol, cx, cy, cz+6, 2.5, 6)

def logic_piques(vol, cx, cy, cz):
    for x in [-6, 0, 6]:
        for y in [-6, 0, 6]:
            dx = (random.random() - 0.5) * 8
            dy = (random.random() - 0.5) * 8
            h = 6 + random.random()*6
            draw_bone(vol, cx+x, cy+y, cz, cx+x+dx, cy+y+dy, cz+h, 1.2, 1)
            draw_sphere(vol, cx+x+dx, cy+y+dy, cz+h, 1.0, 5)

def logic_feu_camp(vol, cx, cy, cz):
    for angle in range(0, 360, 45):
        rad = math.radians(angle)
        draw_sphere(vol, cx + math.cos(rad)*6, cy + math.sin(rad)*6, cz+1, 2.0, 3)
    draw_bone(vol, cx-4, cy-4, cz+1, cx+4, cy+4, cz+3, 1.2, 1)
    draw_bone(vol, cx-4, cy+4, cz+1, cx+4, cy-4, cz+3, 1.2, 2)
    draw_sphere(vol, cx, cy, cz+4, 2, 11)
    draw_sphere(vol, cx, cy, cz+3, 1.5, 10)
    draw_bone(vol, cx, cy, cz+4, cx, cy, cz+8, 1.5, 12)

def logic_four(vol, cx, cy, cz):
    draw_box(vol, cx, cy, cz+6, 8, 8, 6, 4) 
    draw_box(vol, cx, cy, cz+16, 6, 6, 4, 3) 
    draw_box(vol, cx, cy, cz+24, 3, 3, 4, 4)
    draw_box(vol, cx-8, cy, cz+6, 2, 4, 4, 0) 
    draw_box(vol, cx-5, cy, cz+4, 2, 3, 2, 11)
    draw_box(vol, cx-5, cy, cz+3, 1, 1, 1, 10)

def logic_torche(vol, cx, cy, cz):
    draw_bone(vol, cx, cy, cz, cx, cy, cz+12, 1.2, 1)
    draw_sphere(vol, cx, cy, cz+13, 2.0, 6)
    draw_sphere(vol, cx, cy, cz+16, 2, 11)
    draw_sphere(vol, cx, cy, cz+15, 1.5, 10)
    draw_bone(vol, cx, cy, cz+16, cx, cy, cz+20, 1.0, 12)

def logic_station_craft(vol, cx, cy, cz):
    draw_box(vol, cx, cy, cz+8, 10, 7, 1.5, 1) 
    draw_box(vol, cx-8, cy-5, cz+4, 1, 1, 4, 2)
    draw_box(vol, cx+8, cy-5, cz+4, 1, 1, 4, 2)
    draw_box(vol, cx-8, cy+5, cz+4, 1, 1, 4, 2)
    draw_box(vol, cx+8, cy+5, cz+4, 1, 1, 4, 2)
    draw_box(vol, cx+4, cy, cz+11, 3, 2, 1.5, 5) 

def gen_lanterne(frame2=False):
    vol = create_volume()
    cx, cy, cz = 15, 15, 0
    draw_box(vol, cx, cy, cz+2, 3, 3, 2, 5)
    draw_box(vol, cx, cy, cz+7, 2, 2, 3, 4) 
    draw_bone(vol, cx-3, cy-3, cz+4, cx-3, cy-3, cz+10, 0.5, 6)
    draw_bone(vol, cx+3, cy-3, cz+4, cx+3, cy-3, cz+10, 0.5, 6)
    draw_bone(vol, cx-3, cy+3, cz+4, cx-3, cy+3, cz+10, 0.5, 6)
    draw_bone(vol, cx+3, cy+3, cz+4, cx+3, cy+3, cz+10, 0.5, 6)
    draw_box(vol, cx, cy, cz+11, 4, 4, 1, 5)
    draw_box(vol, cx, cy, cz+13, 2, 2, 1, 5)
    if frame2: draw_sphere(vol, cx, cy, cz+7, 2.0, 10)
    else: draw_sphere(vol, cx, cy, cz+7, 1.5, 11)
    return render_volume(vol)

def main():
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/sprites/structures"
    os.makedirs(out_dir, exist_ok=True)
    random.seed(42)
    
    print("Generating Isometric Structures (64x64) with Autotile Bitmask Support...")
    
    walls = {
        "structure_mur_bois_sheet.png": gen_mur_bois_frame,
        "structure_mur_pierre_sheet.png": gen_mur_pierre_frame,
        "structure_mur_metal_sheet.png": gen_mur_metal_frame
    }
    for name, gen_func in walls.items():
        print(f"Generating Autotile Sheet: {name}")
        sheet = apply_tiling(name, gen_func)
        sheet.save(os.path.join(out_dir, name))
        
    print("Generating Doors...")
    doors = {
        "structure_porte_bois_{0}.png": gen_porte_bois_frame,
        "structure_porte_pierre_{0}.png": gen_porte_pierre_frame,
        "structure_porte_metal_{0}.png": gen_porte_metal_frame
    }
    for name_fmt, gen_func in doors.items():
        for orient in [0, 1]:
            img = gen_func(orient)
            img.save(os.path.join(out_dir, name_fmt.format(orient)))
        
    print("Generating Props...")
    props = {
        "structure_barricade.png": logic_barricade,
        "structure_piques.png": logic_piques,
        "structure_feu_camp.png": logic_feu_camp,
        "structure_four.png": logic_four,
        "structure_torche.png": logic_torche,
        "structure_station_craft.png": logic_station_craft,
    }
    for name, logic in props.items():
        img = gen_props(logic)
        img.save(os.path.join(out_dir, name))
        
    print("Generating structure_lanterne...")
    img_l1 = gen_lanterne(False)
    img_l1.save(os.path.join(out_dir, "structure_lanterne_f1.png"))
    img_l2 = gen_lanterne(True)
    img_l2.save(os.path.join(out_dir, "structure_lanterne_f2.png"))
        
    print("Done!")

if __name__ == "__main__":
    main()
