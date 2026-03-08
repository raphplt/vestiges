import os
import random
from PIL import Image

def get_base_sprite(path):
    return Image.open(path).convert("RGBA")

def is_skin_or_scarf(r, g, b):
    colors = [
        (122, 92, 66),
        (232, 224, 212),
        (196, 180, 144),
        (166, 139, 107)
    ]
    for c in colors:
        if abs(r - c[0]) < 10 and abs(g - c[1]) < 10 and abs(b - c[2]) < 10:
            return True
    return False

def make_ne_base(front_se):
    # Rotate logically : SE faces front-right. Flip it -> faces front-left (SW).
    # We want NE (back-right). 
    # Front-left has the bow on the left side of the screen.
    # Back-right (NE) should have the bow on the left side of the screen too.
    # So we flip SE to get the base for NE, then recolor the face.
    
    back = front_se.transpose(Image.FLIP_LEFT_RIGHT)
    bp = back.load()
    w, h = back.size
    
    for y in range(h):
        for x in range(w):
            if bp[x,y][3] == 0: continue
            r, g, b, a = bp[x,y]
            
            is_front_feature = is_skin_or_scarf(r, g, b)
            
            # Eyes/dark face details
            if r == 58 and g == 53 and b == 53 and y < 24 and 10 < x < 38:
                is_front_feature = True
                
            # Protect the bow (which is now mostly on x < 24)
            is_bow = False
            if x < 24 and (r == 122 and g == 92 and b == 66):
                is_bow = True
                
            if is_front_feature and not is_bow:
                if y < 24:
                    bp[x,y] = (45, 90, 39, 255) # Hood back
                else:
                    bp[x,y] = (58, 90, 48, 255) # Cloak back
    return back

def generate_idle(base):
    # Slice character: body (0-44 Y), legs (44-64 Y)
    body = base.crop((0, 0, 48, 44))
    legs = base.crop((0, 44, 48, 64))
    
    frames = []
    # 4 frames: neutral, down, neutral, up
    for dy in [0, 1, 0, -1]:
        f = Image.new("RGBA", (48, 64), (0,0,0,0))
        f.paste(legs, (0, 44))
        f.paste(body, (0, dy), mask=body)
        frames.append(f)
    return frames

def generate_walk(base):
    # Proper 6-frame walk cycle isolating legs
    body = base.crop((0, 0, 48, 44))
    leg1 = base.crop((0, 44, 24, 64))
    leg2 = base.crop((24, 44, 48, 64))
    
    frames = []
    
    # Offsets: (body_y, leg1_x, leg1_y, leg2_x, leg2_y)
    cycle = [
        ( 0,  2, 0, -2, 0),  # Contact
        ( 1,  1, 0, -1,-1),  # Down
        (-1, -1,-1,  1, 0),  # Up / Pass
        ( 0, -2, 0,  2, 0),  # Contact swapped
        ( 1, -1,-1,  1, 0),  # Down swapped
        (-1,  1, 0, -1,-1),  # Up / Pass swapped
    ]
    
    for (by, l1x, l1y, l2x, l2y) in cycle:
        f = Image.new("RGBA", (48, 64), (0,0,0,0))
        f.paste(leg1, (l1x, 44 + l1y))
        f.paste(leg2, (24 + l2x, 44 + l2y))
        f.paste(body, (0, by), mask=body)
        frames.append(f)
    return frames

def generate_dash(base):
    body = base.crop((0, 0, 48, 44))
    legs = base.crop((0, 44, 48, 64))
    
    # 1: squash
    f1_body = body.resize((52, 40), Image.Resampling.NEAREST)
    f1 = Image.new("RGBA", (48, 64), (0,0,0,0))
    f1.paste(legs, (0, 44))
    f1.paste(f1_body, (-2, 4), mask=f1_body)
    
    # 2: stretch
    f2_body = body.resize((56, 42), Image.Resampling.NEAREST)
    f2 = Image.new("RGBA", (48, 64), (0,0,0,0))
    f2.paste(legs, (4, 44))
    f2.paste(f2_body, (4, 2), mask=f2_body)
    
    return [f1, f2, base]

def generate_hurt(base_img):
    f1 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f1p = f1.load()
    bp = base_img.load()
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                f1p[x,y] = (255, 255, 255, 255)
                
    f2 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f2.paste(base_img, (-2, -1), mask=base_img)
    return [f1, f2]

def generate_death(base_img):
    frames = []
    bp = base_img.load()
    for frame_idx in range(4):
        f = Image.new("RGBA", base_img.size, (0,0,0,0))
        fp = f.load()
        prob_keep = 1.0 - (frame_idx * 0.3)
        for y in range(base_img.height):
            for x in range(base_img.width):
                if bp[x,y][3] > 0:
                    if random.random() < prob_keep:
                        r, g, b, a = bp[x,y]
                        dark = 1.0 - (frame_idx * 0.2)
                        fp[x,y] = (int(r*dark), int(g*dark), int(b*dark), a)
        frames.append(f)
    return frames

def make_spritesheet(frames, filename):
    w, h = 48, 64
    count = len(frames)
    sheet = Image.new("RGBA", (w * count, h), (0,0,0,0))
    for i, frame in enumerate(frames):
        sheet.paste(frame, (i * w, 0))
    
    preview = sheet.resize((w * count * 4, h * 4), Image.Resampling.NEAREST)
    sheet.save(filename)
    preview.save(filename.replace(".png", "_preview.png"))

def flip_frames(frames):
    return [f.transpose(Image.FLIP_LEFT_RIGHT) for f in frames]

def generate_all_for_base(base_img, suffix):
    animations = {
        'idle': generate_idle(base_img),
        'walk': generate_walk(base_img),
        'dash': generate_dash(base_img),
        'hurt': generate_hurt(base_img),
        'death': generate_death(base_img),
    }
    
    out_dir = "assets/characters/traqueur/sprites"
    os.makedirs(out_dir, exist_ok=True)
    
    for name, frames in animations.items():
        make_spritesheet(frames, f"{out_dir}/char_traqueur_{suffix}_{name}_sheet.png")

if __name__ == "__main__":
    os.makedirs("assets/characters/traqueur", exist_ok=True)
    # Copier base if missing
    base_file = "assets/sprites/char_traqueur_SE_idle_01.png"
    new_base_file = "assets/characters/traqueur/base_tracker.png"
    
    if os.path.exists(base_file):
        import shutil
        shutil.copy(base_file, new_base_file)
        
    if not os.path.exists(new_base_file):
        print(f"Error: Base file {new_base_file} not found.")
        exit(1)
        
    base_se = get_base_sprite(new_base_file)
    
    # SE
    generate_all_for_base(base_se, "SE")
    
    # SW
    base_sw = base_se.transpose(Image.FLIP_LEFT_RIGHT)
    generate_all_for_base(base_sw, "SW")
    
    # NE (Back right)
    base_ne = make_ne_base(base_se)
    generate_all_for_base(base_ne, "NE")
    
    # NW (Back left)
    base_nw = base_ne.transpose(Image.FLIP_LEFT_RIGHT)
    generate_all_for_base(base_nw, "NW")
    
    print("Generated ALL animations in assets/characters/traqueur/sprites/")
