import os
import random
from PIL import Image, ImageOps

palette = {
    ' ': (0, 0, 0, 0),         
    'O': (26, 26, 46, 255),    
    'o': (30, 58, 26, 255),    
    'G': (58, 90, 48, 255),    
    'g': (74, 140, 63, 255),   
    'H': (123, 197, 88, 255),  
    'B': (196, 180, 144, 255), 
    'b': (232, 224, 212, 255), 
    'S': (166, 139, 107, 255), 
    'd': (58, 53, 53, 255),    
    'D': (107, 97, 97, 255),   
    'W': (122, 92, 66, 255),   
    'w': (74, 55, 40, 255),    
    'c': (94, 196, 196, 255),  
    'r': (196, 67, 43, 255),   
    '_': (245, 240, 235, 255)  
}

def a2i(lines):
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    p = img.load()
    for y, line in enumerate(lines[:32]):
        line = line.ljust(32, ' ')
        for x in range(32):
            if line[x] in palette:
                p[x, y] = palette[line[x]]
    return img

SE_base = [
"                                ",
"              o                 ",
"             oGo                ",
"            oGgGo       OO      ",
"           oGgggGo     OwOO     ",
"           oGgggGdo    OwOO     ",
"          oGgGddGdo   OwOO      ",
"          oGgddcdgo   OWO       ",
"          oGgddddgo OOWO        ",
"           oGggGgoOOWWO         ",
"          oBboGgOOWWWO          ",
"          oBBbbOWWWOo           ",
"          oSBbOWWOgGo           ",
"          OSBOWOgGgGo           ",
"         OdoBBdgGggGo           ",
"        OddoBdgGgGGgo           ",
"        OddoddGgGGodO           ",
"         OO OddgggOO            ",
"            OddgggdO            ",
"            OddgggdO            ",
"            OddgggdO            ",
"            OBBBBBBo            ",
"           oBSBBBBBo            ",
"           oBSOBSBBo            ",
"           oBSOBSBBo            ",
"           oBSOBSBBo            ",
"           OOS OOS O            ",
"           OdD OdD O            ",
"           OdD OdD O            ",
"          OddDOddD O            ",
"          OOOOOOOOOO            ",
"                                "
]

legs_SE_w1 = ["                                "] * 21 + [
"            OBBBBBBo            ",
"           oBSBSBBBBo           ",
"           oBSO OBSBBo          ",
"          oBSO   OBSBBo         ",
"          OOSO   OOSBBo         ",
"          OdDO    OOS O         ",
"          OdDO    OdD O         ",
"         OddDO    OdD O         ",
"         OOOOO   OddD O         ",
"                 OOOOOO         ",
"                                "
]

legs_SE_w2 = ["                                "] * 21 + [
"            OBBBBBBo            ",
"           oBSBBBBBo            ",
"           oBSBBBSBo            ",
"           oBSOBSBBo            ",
"           oBSOBSBBo            ",
"           OOS OOS O            ",
"           OdDOOdD O            ",
"           OdDOOdD O            ",
"          OddDOOddDO            ",
"          OOOOOOOOOO            ",
"                                "
]

legs_SE_w3 = ["                                "] * 21 + [
"            OBBBBBBo            ",
"           oBBBBSBSBo           ",
"          oBBSBO OBSBo          ",
"         oBBSBO   OBSBo         ",
"         oBBSOO   OSOO          ",
"         O SOO    ODdO          ",
"         O DdO    ODdO          ",
"         O DdO    ODddO         ",
"         O DddO   OOOOO         ",
"         OOOOOO                 ",
"                                "
]

NE_base = [
"                                ",
"              o                 ",
"             oGo                ",
"            oGgGo               ",
"           oGgggGo       OO     ",
"           oGgggGo      OwOO    ",
"          oGgggGgo      OwOO    ",
"          oGgGddGgo    OwOO     ",
"          oGgddddgo    OWO      ",
"           oGggggOgo OOWO       ",
"          oGgggOOoOOWWWO        ",
"          oGggOWWOoogGGo        ",
"          oGOWWWOgggGgGo        ",
"          OWWOogGgggGGgo        ",
"         OWWOoggGggGGgo         ",
"        OOWOoggGggGGgo          ",
"       OOWO ogGgddGodO          ",
"       OO   oggGddOO            ",
"            ogggddO             ",
"            ogggddO             ",
"            ogggddO             ",
"            oBBBBBBo            ",
"           oBBBBBSBo            ",
"           oBBBSOBSBo           ",
"           oBBSO OBSBo          ",
"           oBBSO OBSBo          ",
"           O SO   OSO           ",
"           ODO    ODO           ",
"           ODdo   ODdo          ",
"          ODddO  ODddO          ",
"          OOOOO  OOOOO          ",
"                                "
]

legs_NE_w1 = ["                                "] * 21 + [
"            oBBBBBBo            ",
"          oBBBBBSBSBo           ",
"         oBBSBO OBSBo           ",
"        oBBSBO   OBSBo          ",
"        oBBSOO   OSOO           ",
"         O SOO    ODO           ",
"         O DdO    ODdo          ",
"         O Ddo    ODddO         ",
"         O DddO   OOOOO         ",
"         OOOOOO                 ",
"                                "
]

legs_NE_w2 = ["                                "] * 21 + [
"            oBBBBBBo            ",
"           oBBBBBSBo            ",
"           oBSBBBSBo            ",
"           oBBSOBSBo            ",
"           oBBSOBSBo            ",
"           O SO OSO             ",
"           ODOo ODO             ",
"           ODdo ODdo            ",
"          ODddOODddO            ",
"          OOOOOOOOOO            ",
"                                "
]

legs_NE_w3 = ["                                "] * 21 + [
"            oBBBBBBo            ",
"           oBSBSBBBBBo          ",
"           oBSO OBSBBo          ",
"          oBSO   OBSBBo         ",
"          OOSO   OOSBBo         ",
"          ODO    OOS O          ",
"          ODdo    ODO           ",
"         ODddO    ODdo          ",
"         OOOOO   ODddO          ",
"                 OOOOO          ",
"                                "
]

SE_dash_1 = [
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                   o            ",
"                  oGgOO         ",
"                 oGggOwOO       ",
"                oGgGdwOO        ",
"                oGgdcOWO        ",
"                 oGgwOWO        ",
"                oBboOOWWO       ",
"                oBBOOWWWO       ",
"               oSOOWgGgGo       ",
"              OdOBdgGggGo       ",
"             OdoBdgGgGGgo       ",
"            OddodGgGodO         ",
"             OO dggdO           ",
"               OdggdO           ",
"      OOOO     OddgdO           ",
"     O   OO   OBSBBBBo          ",
"     OdD O OBSBSBBBBo           ",
"    OddDO  OOSOBSBBo            ",
"    OOOOO   OOSBBo              ",
"           OdD O                ",
"          OddD O                ",
"          OOOOOO                ",
"                                ",
"                                "
]

SE_dash_2 = [
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                     o          ",
"                    oGgOO       ",
"                   oGggOwOO     ",
"                  oGgGdwOO      ",
"                  oGgdcOWO      ",
"                   oGgwOWO      ",
"                  oBboOOWWO     ",
"                  oBBOOWWWO     ",
"                 oSOWgGgGo      ",
"                OdOBgGggGo      ",
"               OdoBgGgGGgo      ",
"              OddodgGodO        ",
"               OO dgdO          ",
"      OOOO        OgdO          ",
"     O   OO      OBSBBo         ",
"     OdD O     OBSBSBBo         ",
"    OddDO    OOSOBSBo           ",
"    OOOOO     OOSBo             ",
"           OdD O                ",
"          OddD O                ",
"          OOOOOO                ",
"                                "
]

NE_dash_1 = [
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                   o            ",
"                  oGgOO         ",
"                 oGggOwOO       ",
"                oGgGdwOO        ",
"                oGgdOWO         ",
"                 oGggOWO        ",
"                oGggOOWWO       ",
"                oGgOOWWWO       ",
"               oGOWgGgGo        ",
"              OWOoggGggGo       ",
"             OOWOgGgGGgo        ",
"            OOWGggGodO          ",
"             OO ggdO            ",
"               OggdO            ",
"      OOOO     OggdO            ",
"     O   OO   oBBBBSBo          ",
"     ODO O   oBBSBSBBo          ",
"    ODdoO    OSOBSBBo           ",
"    ODddO     OSBBo             ",
"    OOOOO  ODO O                ",
"          ODdo O                ",
"          ODddOO                ",
"          OOOOOO                ",
"                                "
]

NE_dash_2 = [
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                                ",
"                     o          ",
"                    oGgOO       ",
"                   oGggOwOO     ",
"                  oGgGdwOO      ",
"                  oGgdOWO       ",
"                   oGggOWO      ",
"                  oGgOOWWO      ",
"                  oGOOWWWO      ",
"                 oOWgGgGo       ",
"                OWOogGggGo      ",
"               OOWOgGgGGgo      ",
"              OOWGggGodO        ",
"               OO ggdO          ",
"      OOOO        OgdO          ",
"     O   OO      oBBSBo         ",
"     ODO O     oBBSBSBo         ",
"    ODdoO    OSOBSBo            ",
"    ODddO     OSBo              ",
"    OOOOO  ODO O                ",
"          ODdo O                ",
"          ODddOO                ",
"          OOOOOO                ",
"                                "
]

def make_hurt(base_img):
    f1 = base_img.copy()
    p = f1.load()
    for y in range(32):
        for x in range(32):
            if p[x,y][3] > 0:
                p[x,y] = palette['_']
    
    f2 = Image.new('RGBA', (32, 32), (0,0,0,0))
    f2.paste(base_img, (-2, -1))
    return [f1, f2]

def make_death(base_img):
    frames = []
    # F1: Recoil
    f1 = Image.new('RGBA', (32, 32), (0,0,0,0))
    f1.paste(base_img, (-2, -1))
    frames.append(f1)
    
    # F2: Fall to knees
    f2 = Image.new('RGBA', (32, 32), (0,0,0,0))
    sq = base_img.resize((32, 16), Image.NEAREST)
    f2.paste(sq, (0, 16))
    frames.append(f2)
    
    # F3: Flat
    f3 = Image.new('RGBA', (32, 32), (0,0,0,0))
    flat = base_img.resize((32, 6), Image.NEAREST)
    f3.paste(flat, (0, 26))
    frames.append(f3)
    
    # F4: Particles
    f4 = Image.new('RGBA', (32, 32), (0,0,0,0))
    p3 = f3.load()
    p4 = f4.load()
    random.seed(42)
    for y in range(32):
        for x in range(32):
            if p3[x,y][3] > 0:
                nx = x + random.randint(-4, 4)
                ny = y + random.randint(-6, 0)
                if 0 <= nx < 32 and 0 <= ny < 32:
                    if random.random() > 0.4:
                        p4[nx, ny] = palette['O']
                    else:
                        p4[nx, ny] = p3[x,y]
    frames.append(f4)
    return frames

animations = {'SE': {}, 'NE': {}, 'SW': {}, 'NW': {}}

# --- SE ---
se_base_img = a2i(SE_base)
se_body = se_base_img.crop((0, 0, 32, 21))
se_legs_idle = se_base_img.crop((0, 21, 32, 32))

se_idle = []
f1 = Image.new('RGBA', (32, 32))
f1.paste(se_body, (0, 0))
f1.paste(se_legs_idle, (0, 21))
se_idle.append(f1)
f2 = Image.new('RGBA', (32, 32))
f2.paste(se_body, (0, 1))
f2.paste(se_legs_idle, (0, 21))
se_idle.append(f2)
se_idle.append(f2.copy())
se_idle.append(f1.copy())

animations['SE']['idle'] = se_idle

se_walk = []
w1 = Image.new('RGBA', (32, 32))
w1.paste(se_body, (0, 1))
w1.paste(a2i(legs_SE_w1).crop((0,21,32,32)), (0, 21))
se_walk.append(w1)
w2 = Image.new('RGBA', (32, 32))
w2.paste(se_body, (0, 0))
w2.paste(a2i(legs_SE_w2).crop((0,21,32,32)), (0, 21))
se_walk.append(w2)
w3 = Image.new('RGBA', (32, 32))
w3.paste(se_body, (0, 1))
w3.paste(a2i(legs_SE_w3).crop((0,21,32,32)), (0, 21))
se_walk.append(w3)
se_walk.append(f1.copy())

animations['SE']['walk'] = se_walk
animations['SE']['hurt'] = make_hurt(se_base_img)
animations['SE']['dash'] = [a2i(SE_dash_1), a2i(SE_dash_2), se_base_img.copy()]
animations['SE']['death'] = make_death(se_base_img)

# --- NE ---
ne_base_img = a2i(NE_base)
ne_body = ne_base_img.crop((0, 0, 32, 21))
ne_legs_idle = ne_base_img.crop((0, 21, 32, 32))

ne_idle = []
n1 = Image.new('RGBA', (32, 32))
n1.paste(ne_body, (0, 0))
n1.paste(ne_legs_idle, (0, 21))
ne_idle.append(n1)
n2 = Image.new('RGBA', (32, 32))
n2.paste(ne_body, (0, 1))
n2.paste(ne_legs_idle, (0, 21))
ne_idle.append(n2)
ne_idle.append(n2.copy())
ne_idle.append(n1.copy())

animations['NE']['idle'] = ne_idle

ne_walk = []
nw1 = Image.new('RGBA', (32, 32))
nw1.paste(ne_body, (0, 1))
nw1.paste(a2i(legs_NE_w1).crop((0,21,32,32)), (0, 21))
ne_walk.append(nw1)
nw2 = Image.new('RGBA', (32, 32))
nw2.paste(ne_body, (0, 0))
nw2.paste(a2i(legs_NE_w2).crop((0,21,32,32)), (0, 21))
ne_walk.append(nw2)
nw3 = Image.new('RGBA', (32, 32))
nw3.paste(ne_body, (0, 1))
nw3.paste(a2i(legs_NE_w3).crop((0,21,32,32)), (0, 21))
ne_walk.append(nw3)
ne_walk.append(n1.copy())

animations['NE']['walk'] = ne_walk
animations['NE']['hurt'] = make_hurt(ne_base_img)
animations['NE']['dash'] = [a2i(NE_dash_1), a2i(NE_dash_2), ne_base_img.copy()]
animations['NE']['death'] = make_death(ne_base_img)

# --- SW & NW ---
for anim in ['idle', 'walk', 'hurt', 'dash', 'death']:
    animations['SW'][anim] = [img.transpose(Image.FLIP_LEFT_RIGHT) for img in animations['SE'][anim]]
    animations['NW'][anim] = [img.transpose(Image.FLIP_LEFT_RIGHT) for img in animations['NE'][anim]]

out_dir = "assets/characters/traqueur"
os.makedirs(out_dir, exist_ok=True)

TARGET_W = 22
TARGET_H = 22

for d, anims in animations.items():
    for anim_name, frames in anims.items():
        # Resize to roughly 2/3 of 32x32
        resized_frames = [frame.resize((TARGET_W, TARGET_H), Image.NEAREST) for frame in frames]

        for i, frame in enumerate(resized_frames):
            path = os.path.join(out_dir, f"char_traqueur_{d}_{anim_name}_{i+1:02d}.png")
            frame.save(path)
        
        sheet_w = TARGET_W * len(resized_frames)
        sheet = Image.new('RGBA', (sheet_w, TARGET_H), (0,0,0,0))
        for i, frame in enumerate(resized_frames):
            sheet.paste(frame, (i * TARGET_W, 0))
        
        sheet_path = os.path.join(out_dir, f"char_traqueur_{d}_{anim_name}_sheet.png")
        sheet.save(sheet_path)

print(f"SUCCESS: 72 sprites generated and resized to {TARGET_W}x{TARGET_H}!")
