#!/usr/bin/env python3
"""
Slices v3_villagers_and_tieshu_sheet and v3_combat_zombies_and_fx_sheet (both 3x3 on pure white)
into 18 crisp, exterior-floodfill transparent, defringed PNGs in Resources/ForestArt/,
syncs them to /Users/rosa/rosa_games/game_assets/mori-rosa/assets/forest_slice/,
and renders a dark-background cutout inspection sheet.
"""
import os
import shutil
import numpy as np
from PIL import Image
from scipy import ndimage

BRAIN = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65"
SCRATCH = os.path.join(BRAIN, "scratch")
RES_ART = "/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt"
GA_RAW = "/Users/rosa/rosa_games/game_assets/mori-rosa/assets/forest_slice/raw"
GA_FINAL = "/Users/rosa/rosa_games/game_assets/mori-rosa/assets/forest_slice/final"

SHEET_1 = os.path.join(BRAIN, "v3_villagers_and_tieshu_sheet_1795371263653.jpg")
SHEET_2 = os.path.join(BRAIN, "v3_combat_zombies_and_fx_sheet_1795371273138.jpg")

# (row, col, out_name, target_max_px)
ITEMS_1 = [
    (0, 0, "villager_0", 216),
    (0, 1, "villager_1", 212),
    (0, 2, "villager_2", 214),
    (1, 0, "villager_3", 206),
    (1, 1, "bust_vil_0", 220),
    (1, 2, "bust_vil_1", 220),
    (2, 0, "bust_vil_2", 220),
    (2, 1, "bust_vil_3", 220),
    (2, 2, "survivor_tieshu", 232),
]

ITEMS_2 = [
    (0, 0, "zombie_runner", 216),
    (0, 1, "zombie_boss_brute", 310),
    (0, 2, "ico_hero_medal", 136),
    (1, 0, "proj_arrow", 128),
    (1, 1, "proj_wrench", 136),
    (1, 2, "proj_buckshot", 168),
    (2, 0, "proj_fire_pool", 240),
    (2, 1, "ico_nitro", 136),
    (2, 2, "city_gold_airdrop", 220),
]


def cutout_cell(rgb_arr: np.ndarray) -> Image.Image:
    h, w, _ = rgb_arr.shape
    # Pad 4px white border so border flood-fill reaches all exterior white regions
    pad = np.full((h + 8, w + 8, 3), 255, dtype=np.uint8)
    pad[4:4 + h, 4:4 + w] = rgb_arr

    lum = pad.mean(axis=2)
    chroma = pad.max(axis=2).astype(np.int16) - pad.min(axis=2).astype(np.int16)
    near_white = (lum > 238.0) & (chroma < 22)

    labeled, _ = ndimage.label(near_white)
    bg_label = labeled[0, 0]
    ext_bg = (labeled == bg_label)[4:4 + h, 4:4 + w]

    # Also punch out enclosed pure-white background holes (e.g. between arm & torso or inside swoosh gaps),
    # while sparing tiny eye-white dots (< 45 px)!
    pure_white = (rgb_arr.mean(axis=2) > 243.0) & (
        (rgb_arr.max(axis=2).astype(np.int16) - rgb_arr.min(axis=2).astype(np.int16)) < 14
    ) & (~ext_bg)
    pw_lbl, pw_n = ndimage.label(pure_white)
    if pw_n > 0:
        pw_sizes = ndimage.sum(pure_white, pw_lbl, range(1, pw_n + 1))
        for cid, sz in enumerate(pw_sizes, start=1):
            if sz >= 45:
                ext_bg[pw_lbl == cid] = True

    fg = ~ext_bg
    # Remove tiny isolated dust (<20 px), while keeping multi-part swoosh/pellets intact
    comp, ncomp = ndimage.label(fg)
    if ncomp > 0:
        sizes = ndimage.sum(fg, comp, range(1, ncomp + 1))
        for cid, sz in enumerate(sizes, start=1):
            if sz < 20:
                fg[comp == cid] = False

    # Soft anti-aliased edge + defringe exterior white halo
    dist_in = ndimage.distance_transform_edt(fg)
    alpha = np.clip(dist_in * 210.0, 0, 255).astype(np.uint8)
    # Erode 1px of near-white fringe right on outer boundary
    outer_ring = fg & (dist_in <= 1.6)
    lum_orig = rgb_arr.mean(axis=2)
    fringe = outer_ring & (lum_orig > 215.0)
    alpha[fringe] = (alpha[fringe] * 0.2).astype(np.uint8)

    out_rgb = rgb_arr.copy()
    # Darken very bright outer-ring pixels toward comic ink charcoal so zero white halo appears
    bright_edge = outer_ring & (lum_orig > 180.0)
    out_rgb[bright_edge] = (out_rgb[bright_edge] * 0.35).astype(np.uint8)

    ys, xs = np.where(alpha > 12)
    if len(xs) == 0:
        return Image.fromarray(np.dstack([out_rgb, alpha]), "RGBA")

    x0, x1 = max(0, int(xs.min()) - 4), min(w, int(xs.max()) + 5)
    y0, y1 = max(0, int(ys.min()) - 4), min(h, int(ys.max()) + 5)
    rgba = np.dstack([out_rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1]])
    return Image.fromarray(rgba, "RGBA")


def resize_fit(im: Image.Image, target_max: int) -> Image.Image:
    w, h = im.size
    m = max(w, h)
    if m <= 0:
        return im
    scale = target_max / float(m)
    nw = max(8, int(round(w * scale)))
    nh = max(8, int(round(h * scale)))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def process_sheet(sheet_path: str, specs, results):
    im = Image.open(sheet_path).convert("RGB")
    arr = np.array(im)
    sh, sw, _ = arr.shape
    cw, ch = sw / 3.0, sh / 3.0
    for r, c, name, tmax in specs:
        x0 = int(round(c * cw)) + 6
        x1 = int(round((c + 1) * cw)) - 6
        y0 = int(round(r * ch)) + 6
        y1 = int(round((r + 1) * ch)) - 6
        cut = cutout_cell(arr[y0:y1, x0:x1])
        sized = resize_fit(cut, tmax)
        out_p = os.path.join(RES_ART, f"{name}.png")
        sized.save(out_p, optimize=True)
        if os.path.isdir(GA_FINAL):
            sized.save(os.path.join(GA_FINAL, f"{name}.png"), optimize=True)
        results.append((name, sized))
        print(f"Saved {name}.png -> {sized.size}")


def main():
    os.makedirs(SCRATCH, exist_ok=True)
    os.makedirs(RES_ART, exist_ok=True)
    os.makedirs(GA_RAW, exist_ok=True)
    os.makedirs(GA_FINAL, exist_ok=True)

    shutil.copy2(SHEET_1, os.path.join(GA_RAW, os.path.basename(SHEET_1)))
    shutil.copy2(SHEET_2, os.path.join(GA_RAW, os.path.basename(SHEET_2)))

    results = []
    process_sheet(SHEET_1, ITEMS_1, results)
    process_sheet(SHEET_2, ITEMS_2, results)

    # Build dark background inspection board (6 cols x 3 rows)
    cols, rows = 6, 3
    cell = 260
    board = Image.new("RGBA", (cols * cell, rows * cell), (36, 40, 46, 255))
    for idx, (name, sp) in enumerate(results):
        cx = (idx % cols) * cell + (cell - sp.width) // 2
        cy = (idx // cols) * cell + (cell - sp.height) // 2
        board.alpha_composite(sp, (cx, cy))
    chk_path = os.path.join(SCRATCH, "v3_cutout_check.jpg")
    board.convert("RGB").save(chk_path, quality=94)
    print(f"Inspection board -> {chk_path}")


if __name__ == "__main__":
    main()
