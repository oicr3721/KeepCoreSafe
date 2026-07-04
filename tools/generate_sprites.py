from pathlib import Path
from PIL import Image, ImageDraw
import json

OUT = Path(__file__).resolve().parents[1] / "assets" / "sprites"
OUT.mkdir(parents=True, exist_ok=True)

P = {
    "ink": (13, 43, 69, 255),
    "navy": (32, 60, 86, 255),
    "slate": (84, 78, 104, 255),
    "mauve": (141, 105, 122, 255),
    "rust": (208, 129, 89, 255),
    "amber": (255, 170, 94, 255),
    "cream": (255, 212, 163, 255),
    "white": (255, 236, 214, 255),
}
T = (0, 0, 0, 0)
FRAME = 64
FRAMES = 4


def canvas():
    return Image.new("RGBA", (FRAME, FRAME), T)


def rect(d, xy, c): d.rectangle(xy, fill=P[c])
def poly(d, xy, c): d.polygon(xy, fill=P[c])
def ell(d, xy, c): d.ellipse(xy, fill=P[c])


def machine_base(d, accent="rust", wide=False):
    x0, x1 = (10, 53) if wide else (12, 51)
    rect(d, (x0 + 4, 15, x1 - 4, 51), "ink")
    rect(d, (x0, 21, x1, 47), "navy")
    rect(d, (x0 + 3, 18, x1 - 3, 49), "slate")
    rect(d, (x0 + 7, 21, x1 - 7, 46), "navy")
    # Corner stabilizers and warm team markers.
    for x in (x0, x1 - 7):
        rect(d, (x, 22, x + 7, 31), "ink")
        rect(d, (x + 1, 23, x + 6, 29), accent)
        rect(d, (x, 38, x + 7, 47), "ink")
        rect(d, (x + 1, 40, x + 6, 46), "slate")
    rect(d, (x0 + 9, 48, x1 - 9, 53), "ink")
    rect(d, (x0 + 13, 49, x1 - 13, 51), accent)


def laser(frame, action=False):
    im = canvas(); d = ImageDraw.Draw(im); machine_base(d)
    pulse = [0, 1, 0, -1][frame]
    rect(d, (27, 20 + pulse, 36, 39 + pulse), "ink")
    rect(d, (29, 17 + pulse, 34, 36 + pulse), "mauve")
    rect(d, (30, 12 + pulse, 33, 20 + pulse), "cream")
    rect(d, (29, 10 + pulse, 34, 14 + pulse), "amber")
    ell(d, (24, 31, 39, 46), "ink")
    lamp = ["amber", "cream", "amber", "rust"][frame] if not action else "rust"
    ell(d, (27, 34, 36, 43), lamp)
    if action:
        length = [3, 8, 14, 6][frame]
        rect(d, (31, max(0, 10 - length), 32, 9), "white")
        if frame in (1, 2):
            rect(d, (29, max(0, 10 - length), 34, max(1, 12 - length)), "amber")
    return im


def cannon(frame, action=False):
    im = canvas(); d = ImageDraw.Draw(im); machine_base(d, wide=True)
    recoil = [0, 2, 5, 1][frame] if action else [0, -1, 0, 1][frame]
    ell(d, (20, 27, 43, 49), "ink"); ell(d, (23, 29, 40, 46), "slate")
    rect(d, (26, 10 + recoil, 37, 35 + recoil), "ink")
    rect(d, (28, 12 + recoil, 35, 31 + recoil), "mauve")
    rect(d, (27, 8 + recoil, 36, 14 + recoil), "ink")
    rect(d, (29, 9 + recoil, 34, 11 + recoil), "rust")
    if action and frame == 1:
        poly(d, [(31, 2), (27, 7), (30, 7), (31, 10), (33, 7), (36, 7)], "white")
        rect(d, (29, 4, 34, 7), "amber")
    return im


def hammer(frame, action=False, variant=0):
    im = canvas(); d = ImageDraw.Draw(im); machine_base(d)
    shifts = [0, -3, -7, 2] if action else [0, 1, 0, -1]
    s = shifts[frame]
    if variant == 0:
        rect(d, (29, 24, 34, 43), "mauve")
        rect(d, (30, 13 + s, 33, 30), "cream")
        rect(d, (22, 10 + s, 41, 18 + s), "ink")
        rect(d, (24, 11 + s, 39, 16 + s), "rust")
        rect(d, (27, 9 + s, 36, 12 + s), "amber")
    else:
        # Twin side-impact hammers, visually distinct from the overhead ram.
        rect(d, (29, 23, 34, 43), "mauve")
        spread = [0, 2, 6, 1][frame] if action else [0, 1, 0, 1][frame]
        rect(d, (16 - spread, 27, 29, 31), "cream")
        rect(d, (34, 27, 47 + spread, 31), "cream")
        rect(d, (12 - spread, 23, 19 - spread, 35), "ink")
        rect(d, (13 - spread, 25, 18 - spread, 33), "rust")
        rect(d, (44 + spread, 23, 51 + spread, 35), "ink")
        rect(d, (45 + spread, 25, 50 + spread, 33), "rust")
    return im


def healer(frame, action=False):
    im = canvas(); d = ImageDraw.Draw(im); machine_base(d)
    bob = [0, -1, 0, 1][frame]
    rect(d, (20, 23, 43, 43), "ink"); rect(d, (22, 25, 41, 41), "white")
    cross = "cream" if (not action and frame == 1) else "rust"
    rect(d, (29, 27, 34, 39), cross); rect(d, (25, 31, 38, 35), cross)
    # Repair arms and wrench-like tips.
    rect(d, (15, 29 + bob, 22, 33 + bob), "mauve")
    rect(d, (41, 29 - bob, 48, 33 - bob), "mauve")
    if action:
        r = [4, 7, 10, 6][frame]
        for x, y in ((17, 17), (46, 18), (32, 10)):
            rect(d, (x - 1, y - r // 4, x + 1, y + r // 4), "cream")
            rect(d, (x - r // 4, y - 1, x + r // 4, y + 1), "cream")
    return im


def supporter(frame, action=False):
    im = canvas(); d = ImageDraw.Draw(im); machine_base(d)
    sway = [0, 1, 0, -1][frame]
    rect(d, (29 + sway, 11, 34 + sway, 40), "ink")
    rect(d, (30 + sway, 13, 33 + sway, 38), "mauve")
    rect(d, (17, 16, 46, 19), "ink")
    rect(d, (20, 17, 43, 18), "cream")
    lamp = "cream" if (not action and frame in (1, 3)) else "amber"
    for x in (17, 45): ell(d, (x - 2, 13, x + 2, 20), lamp)
    if action:
        arcs = [0, 2, 4, 1][frame]
        if arcs:
            for side in (-1, 1):
                x = 14 if side < 0 else 49
                poly(d, [(x, 17), (x + side * 4, 13), (x + side * 2, 18), (x + side * 6, 22)], "white")
    return im


def wall(frame):
    im = canvas(); d = ImageDraw.Draw(im)
    # Square tile footprint, matching the other allied blocks.
    rect(d, (10, 10, 53, 53), "ink")
    rect(d, (13, 13, 50, 50), "slate")
    rect(d, (17, 17, 46, 46), "navy")
    for x, y in ((12,12),(45,12),(12,45),(45,45)):
        rect(d, (x, y, x+6, y+6), "ink")
    rect(d, (21, 25, 42, 39), "ink")
    rect(d, (23, 27, 40, 37), "navy")
    light = ["rust", "cream", "amber", "cream"][frame]
    rect(d, (28, 31, 35, 34), light)
    return im


def enemy_melee(frame, mode="idle"):
    im = canvas(); d = ImageDraw.Draw(im)
    move = [0, 2, 0, -2][frame] if mode == "move" else 0
    lunge = [0, -2, -5, 0][frame] if mode == "attack" else 0
    bob = [0, -1, 0, 1][frame] if mode == "idle" else 0
    cx, cy = 32 + move, 33 + lunge + bob
    poly(d, [(cx-12,cy-9),(cx-4,cy-15),(cx+8,cy-12),(cx+14,cy-3),(cx+10,cy+10),(cx,cy+14),(cx-12,cy+8),(cx-16,cy)], "mauve")
    rect(d, (cx-7, cy-6, cx+7, cy+7), "slate")
    ell(d, (cx-3, cy-4, cx+3, cy+2), "amber")
    # Four claw limbs; opposite phases create locomotion without directional facing.
    phase = [0, 3, 0, -3][frame] if mode == "move" else [0, 1, 0, -1][frame]
    for sx, sy in ((-1,-1),(1,-1),(-1,1),(1,1)):
        ox = sx * (14 + (phase if sy > 0 else -phase))
        oy = sy * 12
        rect(d, (cx+ox-2, cy+oy-2, cx+ox+2, cy+oy+5), "ink")
        poly(d, [(cx+ox-2,cy+oy+4),(cx+ox+sx*7,cy+oy+8),(cx+ox+sx*3,cy+oy+1)], "rust")
    if mode == "attack":
        reach = [0, 4, 9, 2][frame]
        poly(d, [(cx-8,cy-10),(cx-13-reach,cy-18-reach),(cx-7,cy-14)], "cream")
        poly(d, [(cx+8,cy-10),(cx+13+reach,cy-18-reach),(cx+7,cy-14)], "cream")
    return im


def enemy_ranged(frame, mode="idle"):
    im = canvas(); d = ImageDraw.Draw(im)
    drift = [0, 2, 0, -2][frame] if mode == "move" else 0
    bob = [0, -1, 0, 1][frame] if mode == "idle" else 0
    cx, cy = 32 + drift, 32 + bob
    ell(d, (cx-13, cy-13, cx+13, cy+13), "ink")
    poly(d, [(cx,cy-15),(cx+12,cy-8),(cx+15,cy+5),(cx+5,cy+14),(cx-8,cy+11),(cx-14,cy-2),(cx-9,cy-11)], "mauve")
    ell(d, (cx-6, cy-6, cx+6, cy+6), "rust"); ell(d, (cx-2, cy-2, cx+2, cy+2), "cream")
    phase = [0, 2, 4, 1][frame] if mode == "attack" else [0,1,0,-1][frame]
    for sx in (-1, 1):
        rect(d, (cx+sx*12-2, cy+7, cx+sx*12+2, cy+18+phase), "navy")
        rect(d, (cx+sx*12-1, cy+16+phase, cx+sx*12+1, cy+20+phase), "amber")
    if mode == "attack":
        r = [1, 3, 5, 2][frame]
        ell(d, (cx-r, cy-17-r, cx+r, cy-17+r), "white")
    return im


def core(frame, broken=False):
    im = canvas(); d = ImageDraw.Draw(im)
    if not broken or frame < 3:
        rect(d, (10, 8, 53, 55), "ink")
        rect(d, (13, 11, 50, 52), "slate")
        rect(d, (17, 13, 46, 50), "amber")
        rect(d, (19, 15, 44, 48), "cream")
        rect(d, (21, 17, 42, 46), "white")
        for x,y in ((11,9),(47,9),(11,49),(47,49)): rect(d,(x,y,x+5,y+5),"navy")
    if not broken:
        bob = [-1, 0, 1, 0][frame]
        hair_sway = [0, 1, 0, -1][frame]
        body_sway = [0, 0, -1, 0][frame]
        # Fully clothed pilot icon: hair and body sway independently in fluid.
        ell(d, (27+hair_sway, 21+bob, 35+hair_sway, 29+bob), "rust")
        poly(d, [(28+hair_sway,25+bob),(25+hair_sway,32+bob),(29+hair_sway,30+bob)], "rust")
        rect(d, (29+body_sway, 28+bob, 34+body_sway, 38+bob), "mauve")
        poly(d, [(29+body_sway,31+bob),(24+body_sway,35+bob),(26+body_sway,37+bob),(31+body_sway,34+bob)], "rust")
        poly(d, [(34+body_sway,32+bob),(39+body_sway,35+bob),(37+body_sway,38+bob),(32+body_sway,35+bob)], "rust")
        poly(d, [(30+body_sway,38+bob),(27+body_sway,43+bob),(30+body_sway,44+bob),(33+body_sway,39+bob)], "slate")
        poly(d, [(33+body_sway,38+bob),(36+body_sway,43+bob),(33+body_sway,44+bob),(31+body_sway,39+bob)], "slate")
        for x,y in ((24,20),(39,28),(25,42)):
            yy=y+[0,-1,0,1][frame]; rect(d,(x,yy,x+1,yy+2),"cream")
    else:
        # Progressive cracks, then outward fragments and liquid burst.
        if frame < 3:
            cracks = [[(31,12),(30,24),(35,30)],[(19,15),(27,25),(24,39),(31,48)],[(42,16),(36,25),(41,35),(34,47)]]
            for pts in cracks[:frame+1]:
                for a,b in zip(pts,pts[1:]): d.line((a,b), fill=P["ink"], width=2)
        burst = [0, 3, 8, 15][frame]
        if burst:
            ell(d, (20-burst, 24-burst//2, 43+burst, 47+burst//2), "amber")
            ell(d, (24-burst//2, 27-burst//3, 39+burst//2, 43+burst//3), "cream")
            for sx,sy in ((-1,-1),(1,-1),(-1,1),(1,1)):
                poly(d, [(32+sx*12,32+sy*12),(32+sx*(15+burst),32+sy*(10+burst)),(32+sx*10,32+sy*(16+burst))], "slate")
        if frame == 3:
            # Small protected silhouette remains readable inside the splash.
            ell(d, (28, 26, 35, 33), "rust"); rect(d, (30, 33, 33, 41), "mauve")
    return im


def simplify_colors(im, group):
    # Four opaque colors per unit group: ~50% fewer value steps than v1.
    if group == "enemy":
        mapping = {P["navy"]: P["ink"], P["slate"]: P["ink"],
                   P["rust"]: P["amber"], P["white"]: P["cream"]}
    else:
        mapping = {P["slate"]: P["navy"], P["mauve"]: P["navy"],
                   P["rust"]: P["amber"], P["white"]: P["cream"]}
    return im.point(lambda v: v).convert("RGBA") if not mapping else Image.frombytes(
        "RGBA", im.size,
        b"".join(bytes(mapping.get(px, px)) for px in im.getdata())
    )


def save_sheet(name, frame_fn):
    sheet = Image.new("RGBA", (FRAME * FRAMES, FRAME), T)
    for i in range(FRAMES): sheet.alpha_composite(frame_fn(i), (i * FRAME, 0))
    group = "enemy" if name.startswith("enemy_") else "ally"
    sheet = simplify_colors(sheet, group)
    path = OUT / f"{name}.png"; sheet.save(path, optimize=False)
    return path


def main():
    paths = []
    for name, fn in (
        ("attack_laser_idle", lambda i: laser(i, False)),
        ("attack_laser_action", lambda i: laser(i, True)),
        ("attack_cannon_idle", lambda i: cannon(i, False)),
        ("attack_cannon_action", lambda i: cannon(i, True)),
        ("attack_hammer_a_idle", lambda i: hammer(i, False, 0)),
        ("attack_hammer_a_action", lambda i: hammer(i, True, 0)),
        ("attack_hammer_b_idle", lambda i: hammer(i, False, 1)),
        ("attack_hammer_b_action", lambda i: hammer(i, True, 1)),
        ("healer_idle", lambda i: healer(i, False)),
        ("healer_action", lambda i: healer(i, True)),
        ("supporter_idle", lambda i: supporter(i, False)),
        ("supporter_action", lambda i: supporter(i, True)),
        ("wall_idle", wall),
        ("enemy_melee_idle", lambda i: enemy_melee(i, "idle")),
        ("enemy_melee_attack", lambda i: enemy_melee(i, "attack")),
        ("enemy_melee_move", lambda i: enemy_melee(i, "move")),
        ("enemy_ranged_idle", lambda i: enemy_ranged(i, "idle")),
        ("enemy_ranged_attack", lambda i: enemy_ranged(i, "attack")),
        ("enemy_ranged_move", lambda i: enemy_ranged(i, "move")),
        ("core_idle", lambda i: core(i, False)),
        ("core_break", lambda i: core(i, True)),
    ):
        paths.append(save_sheet(name, fn))

    manifest = {
        "frame_size": [64, 64], "frames_per_sheet": 4,
        "layout": "horizontal", "background": "transparent",
        "palette": ["#%02X%02X%02X" % c[:3] for c in P.values()],
        "sheets": [p.name for p in paths],
    }
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    # Contact sheet for quick visual review, nearest-neighbor only.
    preview = Image.new("RGBA", (256, 64 * len(paths)), T)
    for row, p in enumerate(paths): preview.alpha_composite(Image.open(p), (0, row * 64))
    preview.save(OUT / "all_sheets_preview.png", optimize=False)

    idle_paths = [p for p in paths if p.stem.endswith("_idle")]
    idle_preview = Image.new("RGBA", (256, 64 * len(idle_paths)), T)
    for row, p in enumerate(idle_paths):
        idle_preview.alpha_composite(Image.open(p), (0, row * 64))
    idle_preview.save(OUT / "idle_sheets_preview.png", optimize=False)


if __name__ == "__main__": main()
