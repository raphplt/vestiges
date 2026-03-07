import os
import math
from PIL import Image, ImageDraw

# --- Configuration & Palette ---
OUT_DIR = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/characters/traqueur"
os.makedirs(OUT_DIR, exist_ok=True)

OUTLINE       = (20, 25, 20, 255)
CLOAK_MAIN    = (58, 90, 48, 255)
CLOAK_DARK    = (40, 60, 35, 255)
CLOAK_LIGHT   = (80, 120, 70, 255)
SKIN_MAIN     = (196, 180, 144, 255)
SKIN_DARK     = (150, 130, 100, 255)
LEATHER       = (90, 60, 40, 255)
LEATHER_LIGHT = (120, 80, 50, 255)
WOOD          = (100, 70, 40, 255)
WHITE         = (255, 255, 255, 255)
FX_COLOR      = (150, 200, 150, 200) # Ghostly trail / fx

DIRECTIONS = ["SE", "SW", "NE", "NW"]
ANIMATIONS = {
    "idle": 4,
    "walk": 4,
    "attack": 4,
    "death": 4
}

def project_iso(x, y, z):
    """Simple pseudo-3D isometric projection"""
    # x points SE, y points SW, z points up
    iso_x = (x - y) * 1.0
    iso_y = (x + y) * 0.5 - z
    return iso_x, iso_y

def get_limb_positions(anim, frame, t, dir_idx):
    """Compute parametric positions for limbs based on animation state"""
    # Base offsets
    bob = 0
    sway = 0
    
    # 0=SE, 1=SW, 2=NE, 3=NW
    is_front = dir_idx < 2
    is_right = dir_idx == 0 or dir_idx == 2
    
    # Base configuration
    leg_l = [0, 0, 0] # Left leg (relative to character)
    leg_r = [0, 0, 0] # Right leg
    arm_l = [0, 0, 0]
    arm_r = [0, 0, 0]
    head  = [0, 0, 16]
    torso = [0, 0, 8]
    bow   = [0, 0, 0] # 0 = on back, 1 = drawn
    arrow_t = 0 # 0 to 1 for firing
    
    dead = False

    if anim == "idle":
        bob = math.sin(t * math.pi * 2) * 1.0 # Gentle breathing
        torso[2] += bob
        head[2] += bob
        arm_l[2] = 8 + bob
        arm_r[2] = 8 + bob
        arm_l[0] = -3
        arm_r[0] = 3
        
        leg_l[0] -= 2
        leg_r[0] += 2
        
    elif anim == "walk":
        stride = math.sin(t * math.pi * 2)
        stride_arms = math.cos(t * math.pi * 2)
        
        if dir_idx == 0 or dir_idx == 3: # Moving along X axis mostly
            leg_l[0] = -stride * 4
            leg_r[0] = stride * 4
            arm_l[0] = stride * 3
            arm_r[0] = -stride * 3
        else: # SW/NE, moving along Y axis
            leg_l[1] = -stride * 4
            leg_r[1] = stride * 4
            arm_l[1] = stride * 3
            arm_r[1] = -stride * 3
            
        bob = abs(math.sin(t * math.pi * 2)) * 2
        torso[2] += bob
        head[2] += bob
        
        # Lift legs
        if stride > 0:
            leg_r[2] = stride * 3
        else:
            leg_l[2] = -stride * 3
            
    elif anim == "attack":
        # Drawing bow
        bow[2] = 1 # Active
        bob = 0
        
        if t < 0.5:
            # Draw string back
            arrow_t = t * 2
        elif t < 0.8:
            # Hold
            arrow_t = 1.0
        else:
            # Fire
            arrow_t = 2.0 # Fired
            
        # Poses
        leg_l[0] -= 3
        leg_r[0] += 3
        torso[2] -= 1 # Brace stance
        head[2] -= 1
        
    elif anim == "death":
        dead = True
        # Crumple to ground
        # t=0 -> standing, t=1 -> flat
        fall = min(1.0, t * 1.5)
        
        torso[2] = 8 * (1 - fall)
        head[2] = 16 * (1 - fall) - (fall * 4) # head hits floor
        
        if dir_idx == 0 or dir_idx == 3:
            head[0] += fall * 8
        else:
            head[1] += fall * 8
            
        arm_l[2] = 8 * (1 - fall)
        arm_r[2] = 4 * (1 - fall)
        
        leg_l[0] -= fall * 4
        leg_r[0] += fall * 4
        leg_l[2] = 0
        leg_r[2] = 0

    return {
        "leg_l": leg_l, "leg_r": leg_r,
        "arm_l": arm_l, "arm_r": arm_r,
        "torso": torso, "head": head,
        "bow_state": bow, "arrow_t": arrow_t,
        "dead": dead
    }

def add_outline(img):
    out = Image.new("RGBA", img.size, (0,0,0,0))
    w, h = img.size
    for x in range(w):
        for y in range(h):
            p = img.getpixel((x,y))
            if p[3] > 0:
                # check neighbors
                edge = False
                for dx, dy in [(0,1),(1,0),(0,-1),(-1,0)]:
                    nx, ny = x+dx, y+dy
                    if 0 <= nx < w and 0 <= ny < h:
                        if img.getpixel((nx,ny))[3] == 0:
                            edge = True
                            break
                    else:
                        edge = True
                
                if edge:
                    out.putpixel((x,y), OUTLINE)
                else:
                    out.putpixel((x,y), p)
    return out

def draw_traqueur_frame(direction, anim, frame, total_frames):
    canvas_w = 48
    canvas_h = 48
    img = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    t = frame / total_frames
    dir_idx = DIRECTIONS.index(direction)
    state = get_limb_positions(anim, frame, t, dir_idx)
    
    cx = canvas_w // 2
    cy = canvas_h // 2 + 10 # Ground level
    
    is_front = dir_idx < 2
    is_right = dir_idx == 0 or dir_idx == 2
    
    def transform(pt):
        # pt is [x, y, z]
        ix, iy = project_iso(pt[0], pt[1], pt[2])
        # Base orientation rotation
        if dir_idx == 0: # SE
            return cx + ix, cy + iy
        elif dir_idx == 1: # SW
            return cx - ix, cy + iy
        elif dir_idx == 2: # NE
            return cx + ix, cy - iy
        elif dir_idx == 3: # NW
            return cx - ix, cy - iy
            
    # Polygons / Shapes
    def draw_limb(p1, p2, color, w=2):
        tp1 = transform(p1)
        tp2 = transform(p2)
        d.line([tp1, tp2], fill=color, width=w)
        
    def draw_rect(center, w, h, color):
        tc = transform(center)
        d.rectangle([tc[0]-w/2, tc[1]-h/2, tc[0]+w/2, tc[1]+h/2], fill=color)

    # Rendering order depends on facing
    # If front facing (SE, SW), back limbs first.
    # If back facing (NE, NW), front limbs first.
    
    render_order = []
    
    # Simple depth sorting purely by transformed Y coordinate
    # We will collect drawing calls and execute them
    calls = []
    
    def add_call(z_index, func, *args):
        calls.append((z_index, func, args))
        
    # Legs
    lt_l_top = [state["leg_l"][0], state["leg_l"][1], 8]
    lt_l_bot = [state["leg_l"][0], state["leg_l"][1], state["leg_l"][2]]
    
    lt_r_top = [state["leg_r"][0], state["leg_r"][1], 8]
    lt_r_bot = [state["leg_r"][0], state["leg_r"][1], state["leg_r"][2]]
    
    add_call(transform(lt_l_bot)[1], draw_limb, lt_l_top, lt_l_bot, CLOAK_DARK, 3)
    add_call(transform(lt_l_bot)[1]+1, draw_limb, [lt_l_bot[0], lt_l_bot[1], lt_l_bot[2]+2], lt_l_bot, LEATHER, 2) # Boot
    
    add_call(transform(lt_r_bot)[1], draw_limb, lt_r_top, lt_r_bot, CLOAK_MAIN, 3)
    add_call(transform(lt_r_bot)[1]+1, draw_limb, [lt_r_bot[0], lt_r_bot[1], lt_r_bot[2]+2], lt_r_bot, LEATHER, 2) # Boot

    # Torso
    t_top = [state["torso"][0], state["torso"][1], state["torso"][2]+10]
    t_bot = [state["torso"][0], state["torso"][1], state["torso"][2]]
    
    def draw_torso():
        tp_top = transform(t_top)
        tp_bot = transform(t_bot)
        # Cloak body
        d.line([tp_top, tp_bot], fill=CLOAK_MAIN, width=5)
        # Leather strap
        d.line([(tp_top[0]-2, tp_top[1]+2), (tp_bot[0]+2, tp_bot[1]-2)], fill=LEATHER_LIGHT, width=1)

    add_call(transform(t_bot)[1], draw_torso)
    
    # Head & Hood
    h_cen = [state["head"][0], state["head"][1], state["head"][2]]
    def draw_head():
        if state["dead"]: return
        hc = transform(h_cen)
        # Face
        d.rectangle([hc[0]-2, hc[1]-2, hc[0]+2, hc[1]+2], fill=SKIN_DARK)
        
        # Eyes
        if is_front:
            d.point((hc[0]-1, hc[1]), fill=WHITE)
            d.point((hc[0]+1, hc[1]), fill=WHITE)
            
        # Pointed hood
        # Base
        d.polygon([(hc[0]-3, hc[1]-3), (hc[0]+3, hc[1]-3), (hc[0]+4, hc[1]+1), (hc[0]-4, hc[1]+1)], fill=CLOAK_MAIN)
        
        # Point
        hx, hy = -4, -6
        if dir_idx == 0: # SE -> point goes NW (Top left)
            hx, hy = -5, -6
        elif dir_idx == 1: # SW -> point goes NE (Top right)
            hx, hy = +5, -6
        elif dir_idx == 2: # NE -> point goes SW (Bottom left)
            hx, hy = -4, +2
        elif dir_idx == 3: # NW -> point goes SE (Bottom right)
            hx, hy = +4, +2
            
        tip = (hc[0]+hx, hc[1]+hy)
        d.polygon([(hc[0]-2, hc[1]-3), (hc[0]+2, hc[1]-3), tip], fill=CLOAK_DARK)
        
    add_call(transform(h_cen)[1], draw_head)

    # Arms
    arm_l_top = [state["torso"][0]-3, state["torso"][1], state["torso"][2]+8]
    arm_l_bot = [state["arm_l"][0], state["arm_l"][1], state["arm_l"][2]]
    
    arm_r_top = [state["torso"][0]+3, state["torso"][1], state["torso"][2]+8]
    arm_r_bot = [state["arm_r"][0], state["arm_r"][1], state["arm_r"][2]]
    
    # If attacking, override arms
    if state["bow_state"][2] == 1: # Active drawing bow
        if is_right:
            # Holding bow with left arm, drawing with right
            arm_l_bot = [state["torso"][0]+5, state["torso"][1]+5, state["torso"][2]+8]
            arm_r_bot = [state["torso"][0]-2, state["torso"][1]-2, state["torso"][2]+8]
            if state["arrow_t"] < 1.0: # Drawing
                arm_r_bot[0] -= state["arrow_t"] * 4
                arm_r_bot[1] -= state["arrow_t"] * 4
        else:
            arm_l_bot = [state["torso"][0]-5, state["torso"][1]+5, state["torso"][2]+8]
            arm_r_bot = [state["torso"][0]+2, state["torso"][1]-2, state["torso"][2]+8]
            if state["arrow_t"] < 1.0: # Drawing
                arm_r_bot[0] += state["arrow_t"] * 4
                arm_r_bot[1] -= state["arrow_t"] * 4

    add_call(transform(arm_l_bot)[1], draw_limb, arm_l_top, arm_l_bot, CLOAK_MAIN, 2)
    add_call(transform(arm_l_bot)[1]+1, draw_limb, arm_l_bot, [arm_l_bot[0], arm_l_bot[1], arm_l_bot[2]-1], SKIN_MAIN, 2) # Hand
    
    add_call(transform(arm_r_bot)[1], draw_limb, arm_r_top, arm_r_bot, CLOAK_DARK, 2)
    add_call(transform(arm_r_bot)[1]+1, draw_limb, arm_r_bot, [arm_r_bot[0], arm_r_bot[1], arm_r_bot[2]-1], SKIN_DARK, 2) # Hand

    # Bow
    def draw_bow():
        if state["dead"]: return
        
        if state["bow_state"][2] == 0:
            # On back
            bc = transform([state["torso"][0], state["torso"][1], state["torso"][2]+5])
            if is_front:
                # Barely visible, maybe just the tips
                d.line([(bc[0]-6, bc[1]-6), (bc[0]+6, bc[1]+6)], fill=WOOD, width=1)
            else:
                d.arc([(bc[0]-10, bc[1]-10), (bc[0]+10, bc[1]+10)], start=30, end=150, fill=WOOD, width=2)
                d.line([(bc[0]-8, bc[1]+4), (bc[0]+8, bc[1]+4)], fill=(150,150,150,200), width=1) # String
        else:
            # Active in hand
            hand_pos = arm_l_bot if is_right else arm_r_bot
            hc = transform(hand_pos)
            
            # Simple bow arc
            if is_right:
                d.arc([(hc[0]-12, hc[1]-12), (hc[0]+12, hc[1]+12)], start=270, end=90, fill=WOOD, width=2)
                
                # Arrow
                if state["arrow_t"] <= 1.0:
                    ax = hc[0] - 6 + (state["arrow_t"] * 6)
                    ay = hc[1]
                    d.line([(ax, ay), (hc[0]+8, ay)], fill=WOOD, width=1) # Shaft
                    d.point((hc[0]+9, ay), fill=WHITE) # Tip
                    
                    # String pulled back
                    d.line([(hc[0], hc[1]-12), (ax, ay)], fill=(200,200,200,200), width=1)
                    d.line([(hc[0], hc[1]+12), (ax, ay)], fill=(200,200,200,200), width=1)
                else:
                    # String relaxed
                    d.line([(hc[0], hc[1]-12), (hc[0], hc[1]+12)], fill=(200,200,200,200), width=1)
            else:
                d.arc([(hc[0]-12, hc[1]-12), (hc[0]+12, hc[1]+12)], start=90, end=270, fill=WOOD, width=2)
                
                # Arrow
                if state["arrow_t"] <= 1.0:
                    ax = hc[0] + 6 - (state["arrow_t"] * 6)
                    ay = hc[1]
                    d.line([(ax, ay), (hc[0]-8, ay)], fill=WOOD, width=1) # Shaft
                    d.point((hc[0]-9, ay), fill=WHITE) # Tip
                    
                    # String pulled back
                    d.line([(hc[0], hc[1]-12), (ax, ay)], fill=(200,200,200,200), width=1)
                    d.line([(hc[0], hc[1]+12), (ax, ay)], fill=(200,200,200,200), width=1)
                else:
                    # String relaxed
                    d.line([(hc[0], hc[1]-12), (hc[0], hc[1]+12)], fill=(200,200,200,200), width=1)
                    
    # The bow needs to render relative to the torso/arms
    bow_z = transform(t_bot)[1] - 5 if not is_front else transform(t_bot)[1] + 5
    if state["bow_state"][2] == 1:
        # In hand, render in front
        bow_z = transform(arm_l_bot)[1] + 2 if is_right else transform(arm_r_bot)[1] + 2
        
    add_call(bow_z, draw_bow)

    # Sort calls by Z-index (Y-coordinate basically)
    calls.sort(key=lambda c: c[0])
    
    # Execute
    for c in calls:
        if c[2]:
            c[1](*c[2])
        else:
            c[1]()

    # Add ghost arrow FX if Fired
    if anim == "attack" and state["arrow_t"] > 1.0:
        hand_pos = arm_l_bot if is_right else arm_r_bot
        hc = transform(hand_pos)
        dist = (state["arrow_t"] - 1.0) * 20
        if is_right:
            d.line([(hc[0]+8+dist, hc[1]), (hc[0]+16+dist, hc[1])], fill=FX_COLOR, width=2)
        else:
            d.line([(hc[0]-8-dist, hc[1]), (hc[0]-16-dist, hc[1])], fill=FX_COLOR, width=2)

    # Dead state pooling blood/essence
    if state["dead"]:
        hc = transform(h_cen)
        rad = int(t * 8)
        d.ellipse([hc[0]-rad, hc[1]-rad//2, hc[0]+rad, hc[1]+rad//2], fill=(20, 10, 10, 150))

    final_img = add_outline(img)
    return final_img

def main():
    print("Generating Traqueur animations...")
    
    for anim, frames in ANIMATIONS.items():
        for d in DIRECTIONS:
            sheet = Image.new("RGBA", (48 * frames, 48), (0, 0, 0, 0))
            
            for f in range(frames):
                img = draw_traqueur_frame(d, anim, f, frames)
                
                # Save individual frame
                filename = f"char_traqueur_{d}_{anim}_{f+1:02d}.png"
                img.save(os.path.join(OUT_DIR, filename))
                
                # Paste into sheet
                sheet.paste(img, (f * 48, 0))
                
            # Save sheet
            sheet_name = f"char_traqueur_{d}_{anim}_sheet.png"
            sheet.save(os.path.join(OUT_DIR, sheet_name))
            
    print("Traqueur generation complete.")

if __name__ == "__main__":
    main()
