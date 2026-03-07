import os
from PIL import Image, ImageDraw

def generate_preview():
    # We will draw a very clean, front-facing (de face) humanoid sprite.
    # Dimensions: 32x32 native, upscaled to 128x128 for preview.
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Palette
    outline = (20, 25, 20, 255) # Dark sel-out
    cloak_main = (58, 90, 48, 255) # #3A5A30 Vert Forêt
    cloak_dark = (40, 60, 35, 255)
    cloak_light = (80, 120, 70, 255)
    
    skin_main = (196, 180, 144, 255) # #C4B490 Beige Clair
    skin_dark = (150, 130, 100, 255)
    
    leather = (90, 60, 40, 255) # Boots/straps
    leather_light = (120, 80, 50, 255)
    
    wood = (100, 70, 40, 255) # Bow
    
    # Center coordinates
    cx = 16
    
    # Let's build a tall, slender humanoid facing directly forward
    
    # 1. Back Layer (Bow)
    # A large bow carried across the back diagonally
    d.arc([(cx-10, 2), (cx+12, 28)], start=130, end=320, fill=wood, width=2)
    # Bow string
    d.line([(cx-8, 10), (cx+10, 24)], fill=(200, 200, 200, 150), width=1)
    
    # 2. Legs (Tall and slender)
    # Right leg (from viewer perspective)
    d.rectangle([(cx-4, 20), (cx-2, 28)], fill=cloak_main)
    d.rectangle([(cx-4, 20), (cx-3, 28)], fill=cloak_dark)
    d.rectangle([(cx-5, 28), (cx-2, 30)], fill=leather) # Boot
    
    # Left leg
    d.rectangle([(cx+1, 20), (cx+3, 28)], fill=cloak_main)
    d.rectangle([(cx+1, 20), (cx+2, 28)], fill=cloak_dark)
    d.rectangle([(cx+1, 28), (cx+4, 30)], fill=leather) # Boot
    
    # 3. Torso (Slender, wrapped in a tunic/cloak)
    # Belt/Waist
    d.rectangle([(cx-4, 17), (cx+3, 19)], fill=leather)
    d.rectangle([(cx-3, 17), (cx+2, 18)], fill=leather_light)
    d.rectangle([(cx-1, 17), (cx, 19)], fill=(200, 180, 100, 255)) # Buckle
    
    # Chest
    d.polygon([(cx-5, 10), (cx+4, 10), (cx+3, 17), (cx-4, 17)], fill=cloak_main)
    # Cloak folds/shading
    d.polygon([(cx-5, 10), (cx-2, 10), (cx-1, 17), (cx-4, 17)], fill=cloak_dark)
    d.line([(cx, 10), (cx+1, 17)], fill=cloak_light, width=1)
    
    # 4. Cape dropping down from shoulders
    d.polygon([(cx-7, 10), (cx-5, 10), (cx-6, 22), (cx-8, 24)], fill=cloak_main) # Right drape
    d.polygon([(cx+6, 10), (cx+4, 10), (cx+5, 22), (cx+7, 24)], fill=cloak_main) # Left drape
    d.polygon([(cx-7, 10), (cx-6, 10), (cx-7, 22), (cx-8, 24)], fill=cloak_dark)
    
    # Cross strap on chest
    d.line([(cx-4, 11), (cx+3, 16)], fill=leather_light, width=1)
    
    # 5. Arms
    # Right arm resting down
    d.polygon([(cx-7, 11), (cx-5, 11), (cx-6, 18), (cx-8, 18)], fill=cloak_main)
    d.rectangle([(cx-8, 18), (cx-6, 20)], fill=skin_dark) # Hand
    
    # Left arm holding a dagger or just resting
    d.polygon([(cx+6, 11), (cx+4, 11), (cx+5, 18), (cx+7, 18)], fill=cloak_main)
    d.rectangle([(cx+5, 18), (cx+7, 20)], fill=skin_dark) # Hand
    
    # 6. Head & Pointed Hood
    # Neck/Shadow
    d.rectangle([(cx-2, 8), (cx+1, 10)], fill=skin_dark)
    
    # Face (Looking straight forward)
    d.rectangle([(cx-3, 5), (cx+2, 8)], fill=skin_main)
    # Eyes (Intense, piercing, slightly shadowed by hood)
    d.point((cx-2, 6), fill=(255,255,255,255))
    d.point((cx+1, 6), fill=(255,255,255,255))
    # Shadow under hood
    d.line([(cx-3, 5), (cx+2, 5)], fill=skin_dark, width=1)
    
    # The Pointed Hood
    # Front opening
    d.line([(cx-4, 5), (cx-4, 9)], fill=cloak_main, width=1)
    d.line([(cx+3, 5), (cx+3, 9)], fill=cloak_main, width=1)
    # Top of hood wrapping around head
    d.polygon([(cx-4, 4), (cx+3, 4), (cx+4, 1), (cx-5, 1)], fill=cloak_main)
    
    # The point towering up (since he's facing front, the point goes straight up or slightly to the side)
    # Let's make it tall and slightly crumpled to the side
    d.polygon([(cx-2, 1), (cx+1, 1), (cx+4, -3), (cx+2, -3)], fill=cloak_main)
    d.polygon([(cx, 1), (cx+1, 1), (cx+4, -3), (cx+3, -3)], fill=cloak_dark)
    
    # Highlights on hood
    d.line([(cx-3, 3), (cx, 1)], fill=cloak_light, width=1)

    # 7. Outline processing (Sel-out)
    outline_img = Image.new("RGBA", (32, 32), (0,0,0,0))
    for x in range(32):
        for y in range(32):
            if img.getpixel((x,y))[3] > 0:
                is_edge = False
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    nx, ny = x+dx, y+dy
                    if 0 <= nx < 32 and 0 <= ny < 32:
                        if img.getpixel((nx,ny))[3] == 0:
                            is_edge = True
                            break
                    else:
                        is_edge = True
                
                if is_edge:
                    outline_img.putpixel((x,y), outline)
                else:
                    outline_img.putpixel((x,y), img.getpixel((x,y)))
                    
    # Scale up exactly 4x for preview
    final = outline_img.resize((128, 128), Image.NEAREST)
    
    out_dir = "c:/Users/rapha/Documents/Travail/Projets/vestiges/assets/characters/traqueur"
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, "preview_traqueur_front.png")
    final.save(out_path)
    print(f"Traqueur preview saved to {out_path}")

if __name__ == "__main__":
    generate_preview()
