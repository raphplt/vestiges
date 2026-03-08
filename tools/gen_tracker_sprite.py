import os
from PIL import Image

palette = {
    ' ': (0, 0, 0, 0),
    'O': (26, 26, 46, 255),    # Contour absolu / Arc outline
    'o': (30, 58, 26, 255),    # Contour tenue
    'h': (123, 197, 88, 255),  # Vert clair (Highlight)
    'g': (58, 90, 48, 255),    # Vert base (Forester Green)
    'G': (45, 90, 39, 255),    # Vert sombre (Canopée)
    'l': (232, 224, 212, 255), # Beige tres clair (Highlight)
    'b': (196, 180, 144, 255), # Beige base
    'd': (166, 139, 107, 255), # Beige sombre (Ombre)
    'v': (58, 53, 53, 255),    # Masque / Visage sombre
    's': (122, 92, 66, 255),   # Peau (Skin)
    'W': (122, 92, 66, 255),   # Bois clair (Arc/Bow)
    'w': (74, 55, 40, 255),    # Bois sombre (Arc shadow)
    'C': (107, 97, 97, 255),   # Gris clair (Ceinture/Metal)
    'D': (58, 53, 53, 255),    # Gris sombre (Bottes)
}

lines = [
"                        ",
"             oo         ",
"            ohgo    O   ",
"           ohhggo  wO   ",
"          ohhggGgo WO   ",
"          ohggvGgooWO   ",
"          ohgssvvgoWO   ",
"          ohgvsvvgoWO   ",
"           ogvvvGgowO   ",
"           oggggGgowO   ",
"         oollbbbloowO   ",
"        ohhllbbll oDWO  ",
"       ohh llbbbG oDWO  ",
"       oGg oGbbGo o WO  ",
"       oDo oggGgo  o WO ",
"        o  oggggo  o WO ",
"           oggggo    WO ",
"           oCCCgo    WO ",
"          obdCCgo   WO  ",
"           oGggGo   wO  ",
"           oGggGo   wO  ",
"          oGg  oGgo wO  ",
"         oGg   oGgowO   ",
"         oDDo  oDDowO   ",
"        oCDDo  oCDDoO   ",
"        oDDDo  oDDDo    ",
"        oDDDo  oDDDo    ",
"       oCDDDo  oCDDo    ",
"       OOOOOO  OOOOO    ",
"                        ",
"                        ",
"                        ",
]

# Ensure exactly 24x32
assert len(lines) == 32
for line in lines:
    assert len(line) == 24

img = Image.new('RGBA', (24, 32), (0, 0, 0, 0))
pixels = img.load()

for y, line in enumerate(lines):
    for x, char in enumerate(line):
        if char in palette:
            pixels[x, y] = palette[char]
        else:
            print(f"Unknown char '{char}' at {x},{y}")

out_dir = "assets/sprites"
os.makedirs(out_dir, exist_ok=True)
out_path = os.path.join(out_dir, "char_traqueur_SE_idle_01.png")
img.save(out_path)

# Save a 4x scaled preview for easy viewing
img_preview = img.resize((24*4, 32*4), Image.NEAREST)
preview_path = os.path.join(out_dir, "char_traqueur_SE_idle_01_preview.png")
img_preview.save(preview_path)

print(f"Saved sprite to {out_path} and preview to {preview_path}")
