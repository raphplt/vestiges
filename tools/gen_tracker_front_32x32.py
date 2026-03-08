import os
from PIL import Image

# Palette strictly taken from the VESTIGES-BIBLE.md & CHARTE-GRAPHIQUE.md
palette = {
    ' ': (0, 0, 0, 0),         # Transparent
    'O': (26, 26, 46, 255),    # Noir profond (Outlines)
    'o': (30, 58, 26, 255),    # Vert/Lierre très sombre (Outlines for green parts)
    'G': (58, 90, 48, 255),    # Vert Forêt (Base cloaque/hood)
    'g': (74, 140, 63, 255),   # Vert mousse (Highlight)
    'H': (123, 197, 88, 255),  # Vert clair (Extreme Highlight)
    'B': (196, 180, 144, 255), # Beige clair (Base scarf/pants)
    'b': (232, 224, 212, 255), # Blanc cassé (Highlight beige)
    'S': (166, 139, 107, 255), # Brun clair (Shadow beige)
    'd': (58, 53, 53, 255),    # Gris chaud foncé (Dark mask/shadows)
    'D': (107, 97, 97, 255),   # Gris chaud (Belt/Metal/Boots)
    'u': (158, 148, 148, 255), # Gris clair (Highlight metal)
    'W': (122, 92, 66, 255),   # Brun terreux (Bow light)
    'w': (74, 55, 40, 255),    # Brun tronc foncé (Bow dark)
    'c': (94, 196, 196, 255),  # Cyan glacé (Small eye reflection / goggle maybe)
}

lines = [
"                                ",#0
"               o                ",#1
"              oGo               ",#2
"             oGgGo              ",#3
"            oGgggGo             ",#4
"           oGgggggGo            ",#5
"           oGgggggGo            ",#6
"          oGgGddGggGo           ",#7
"         oGgGddddGGgGo          ",#8  (Hood opening framing face)
"         oGgdddcdcdgGo          ",#9  (Dark mask with tiny cyan glints)
"        oWgGgddddddgGwO         ",#10 (Bow tips 'W', 'w' showing behind shoulders)
"        oWgGgGggggGgGwO         ",#11
"        oWgGgOooooOgGwO         ",#12
"       oWwgGobbbBBbogGwO        ",#13 (Beige scarf wrapped around neck)
"       oWwGgoBBBBBBoGgwO        ",#14
"       oWgGGoBSBBBSoGgGwO       ",#15
"      OoWGgdooBBBBoodGgWO       ",#16 (Arms in dark sleeves 'd' next to green coat)
"     oOWGdGdo OooO odGgdWO      ",#17
"     oOWGdddo ODDd odddgWO      ",#18 (Belt 'D' around waist)
"     oOWgdddo ODDd odddgWO      ",#19
"      oWGGddo OddO oddGgWO      ",#20
"      oWGGddo OBBo oddGwo       ",#21 (Legs/pants starting)
"       oGGdgoOBSBBoogdwo        ",#22
"        ooo OOBBBBOO oo         ",#23 (Hands/gloves ending)
"           OoBBBBBBSoO          ",#24
"           OoBBSOoBBSoO         ",#25 (Legs splitting)
"           OoBBSOoBBSoO         ",#26
"           OoSbSoOSbSoO         ",#27
"          OoDdDdoOdDdDoO        ",#28 (Boots 'D', 'd')
"          OOddddOOddddOO        ",#29
"         OOddddddOddddddOO      ",#30
"         OOOOOOOOOOOOOOOOO      ",#31
]

assert len(lines) == 32
for idx, line in enumerate(lines):
    assert len(line) == 32, f"Line {idx} length is {len(line)} instead of 32"

img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
pixels = img.load()

for y, line in enumerate(lines):
    for x, char in enumerate(line):
        if char in palette:
            pixels[x, y] = palette[char]
        else:
            print(f"Unknown char '{char}' at {x},{y}")

out_dir = "assets/characters/traqueur"
os.makedirs(out_dir, exist_ok=True)
out_path = os.path.join(out_dir, "char_traqueur_front_idle_01.png")
img.save(out_path)

# Save a 4x scaled preview for easy viewing
img_preview = img.resize((32*4, 32*4), Image.NEAREST)
preview_path = os.path.join(out_dir, "char_traqueur_front_idle_01_preview.png")
img_preview.save(preview_path)

print(f"Saved sprite to {out_path} and preview to {preview_path}")

