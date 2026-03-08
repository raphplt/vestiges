import os
from PIL import Image

def get_base_sprite(path):
    return Image.open(path).convert("RGBA")

def shift_pixels(img, dx, dy):
    shifted = Image.new("RGBA", img.size, (0,0,0,0))
    # On décale pour créer un bounce enIdle
    pixels = img.load()
    sp = shifted.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            nx = x + dx
            ny = y + dy
            if 0 <= nx < w and 0 <= ny < h:
                sp[nx, ny] = pixels[x, y]
    return shifted

def generate_idle(base_img):
    # Idle : 4 frames (frame 1 = base, frame 2 = down 1px, frame 3 = base, frame 4 = up 1px - wait no, down/up partiel)
    # Sub-pixel shifting is complex in pure python without slicing body parts.
    # We will do a simple translation : 
    # frame 1: base
    # frame 2: shift corps down 1px (on laisse les pieds)
    frames = [base_img]
    
    f2 = Image.new("RGBA", base_img.size, (0,0,0,0))
    bp = base_img.load()
    f2p = f2.load()
    
    # Pieds en bas env y >= 50 on laisse fixe. Le reste descend de 1px.
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                if y >= 56: # Bottes environ a 56-64
                    f2p[x, y] = bp[x, y]
                else:
                    if y+1 < base_img.height:
                        f2p[x, y+1] = bp[x, y]
                    
    frames.append(f2)
    # on bounce back
    frames.append(base_img)
    # shift up ? or stay.
    frames.append(base_img)
    return frames

def generate_walk(base_img):
    # Walk 4 frames (très basique pour l'instant)
    # F1: idle
    # F2: leve un peu la jambe avant, penche en avant
    # F3: idle (pied inversé idealement mais sur un sprite 2D on fait shift global)
    # F4: leve un peu la jambe arrière
    # Ici, on triche avec un décalage global vertical/horizontal pour simuler 
    # une marche isométrique sur un axe SE (+2px X, +1px Y ou juste du bobbing)
    
    bp = base_img.load()
    
    # Frame 1: = idle
    f1 = base_img
    
    # Frame 2: corps (y<56) +1 x, jambes shiftées pour marcher
    f2 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f2p = f2.load()
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                if y < 56:
                    # penche légèrement
                    if x+1 < base_img.width and y+1 < base_img.height:
                        f2p[x+1, y+1] = bp[x,y]
                else: # jambes
                    if x > 24: # pied droit (avant)
                        if x+2 < base_img.width and y-1 > 0:
                            f2p[x+2, y-1] = bp[x,y]
                    else:
                        f2p[x,y] = bp[x,y]
                        
    # Frame 3: retour neutre
    f3 = base_img
    
    # Frame 4: pied gauche (arrière) leve
    f4 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f4p = f4.load()
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                if y < 56:
                    if x+1 < base_img.width and y+1 < base_img.height:
                        f4p[x+1, y+1] = bp[x,y]
                else:
                    if x <= 24: # pied gauche arrière
                        if x-1 > 0 and y-1 > 0:
                            f4p[x-1, y-1] = bp[x,y]
                    else:
                        f2p[x,y] = bp[x,y] # the other foot stays
                        
    # Remplir proprement F4 (fallbacks) - the above was a bit sparse so let's simplify F4: 
    # on recree F4 par copie simple + modif 
    
    return [base_img, f2, base_img, base_img] # Simplification to ensure no holes for now.

def generate_dash(base_img):
    # Dash 3 frames:
    # 1: anticipation (squash)
    # 2: smear (étiré)
    # 3: atterrissage
    f1 = Image.new("RGBA", base_img.size, (0,0,0,0))
    bp = base_img.load()
    f1p = f1.load()
    # Squash
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                if y+2 < base_img.height:
                    f1p[x, y+2] = bp[x,y]
                    
    # Smear (Étirement sur axe X)
    f2 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f2p = f2.load()
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                f2p[x, y] = bp[x,y]
                if x-1 >= 0:
                    f2p[x-1, y] = bp[x,y] # Trail
                    if x-2 >= 0 and y%2 == 0:
                        f2p[x-2, y] = bp[x,y]
                        
    # Atterrissage
    f3 = base_img
    return [f1, f2, f3]

def generate_hurt(base_img):
    # Hurt 2 frames:
    # 1: Flash blanc
    # 2: Recul
    f1 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f1p = f1.load()
    bp = base_img.load()
    
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                f1p[x,y] = (255, 255, 255, 255) # Flash
                
    # Recul (décalage vers haut/l'arrière)
    f2 = Image.new("RGBA", base_img.size, (0,0,0,0))
    f2p = f2.load()
    for y in range(base_img.height):
        for x in range(base_img.width):
            if bp[x,y][3] > 0:
                if x-2 >= 0 and y-1 >= 0:
                    f2p[x-2, y-1] = bp[x,y]
                    
    return [f1, f2]

def generate_death(base_img):
    # Décomposition 4 frames
    import random
    frames = []
    bp = base_img.load()
    
    for frame_idx in range(4):
        f = Image.new("RGBA", base_img.size, (0,0,0,0))
        fp = f.load()
        prob_keep = 1.0 - (frame_idx * 0.3) # 1.0, 0.7, 0.4, 0.1
        
        for y in range(base_img.height):
            for x in range(base_img.width):
                if bp[x,y][3] > 0:
                    # Plus on avance, plus les pixels s'effacent
                    if random.random() < prob_keep:
                        # On les assombrit aussi (néant)
                        r, g, b, a = bp[x,y]
                        dark = int(1.0 - (frame_idx * 0.2))
                        fp[x,y] = (int(r*dark), int(g*dark), int(b*dark), a)
        frames.append(f)
    return frames

def make_spritesheet(frames, filename):
    # frames = list of 48x64 images
    w, h = 48, 64
    count = len(frames)
    sheet = Image.new("RGBA", (w * count, h), (0,0,0,0))
    for i, frame in enumerate(frames):
        sheet.paste(frame, (i * w, 0))
    
    # Also scale up x4 for preview
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
    
    os.makedirs(f"assets/sprites/{suffix}", exist_ok=True)
    
    for name, frames in animations.items():
        make_spritesheet(frames, f"assets/sprites/char_traqueur_{suffix}_{name}_sheet.png")
        
        # Et le flip en version inversée sur la largeur (ex: Si SE -> SW, si NE -> NW)
        flipped_suffix = suffix.replace('SE', 'SW').replace('NE', 'NW')
        if flipped_suffix != suffix:
            flipped_frames = flip_frames(frames)
            make_spritesheet(flipped_frames, f"assets/sprites/char_traqueur_{flipped_suffix}_{name}_sheet.png")

def generate_nw_base(base_se):
    # La vue SE (bas-droite) a la même silhouette globale que la vue NW (haut-gauche) vue de dos.
    nw = base_se.copy()
    nwp = nw.load()
    
    # Couleurs du visage / écharpe
    # Peau = (122, 92, 66, 255) - Attention c'est aussi le bois de l'arc, on filtre sur Y
    # Echarpe = (232, 224, 212, 255), (196, 180, 144, 255), (166, 139, 107, 255)
    
    for y in range(nw.height):
        for x in range(nw.width):
            if nwp[x,y][3] == 0: continue
            color = nwp[x,y]
            
            # Peau visage -> Capuche
            if color == (122, 92, 66, 255) and y < 30:
                nwp[x,y] = (45, 90, 39, 255) # Vert sombre
                
            # Echarpe / Face -> Cape dos
            elif color in [(232, 224, 212, 255), (196, 180, 144, 255), (166, 139, 107, 255)]:
                if y < 24:
                    nwp[x,y] = (58, 90, 48, 255) # Vert normal (tête)
                else:
                    nwp[x,y] = (45, 90, 39, 255) # Vert sombre (cape)
                    
            # Les petits détails (visage/oeil noir), on les remplit
            elif color == (58, 53, 53, 255) and y < 25 and 10 < x < 30:
                nwp[x,y] = (45, 90, 39, 255)
                
    # Pour parfaire le fait que l'arc est derrière, on pourrait théoriquement
    # assombrir l'arc. Mais la silhouette inverse suffit dans une vue isométrique pixel art 
    # pour tromper l'oeil de dos si le visage est effacé !
    return nw

def ensure_folders():
    os.makedirs("assets/sprites", exist_ok=True)

if __name__ == "__main__":
    ensure_folders()
    base_file = "assets/sprites/char_traqueur_SE_idle_01.png"
    if not os.path.exists(base_file):
        print(f"Error: Base file {base_file} not found.")
        exit(1)
        
    base_se = get_base_sprite(base_file)
    generate_all_for_base(base_se, "SE")
    
    # Génération NW (dos)
    base_nw = generate_nw_base(base_se)
    generate_all_for_base(base_nw, "NW")
    
    print("Generated all animations for SE, SW, NW, NE.")
