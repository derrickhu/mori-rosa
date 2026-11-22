#!/usr/bin/env python3
"""
Slice & clean V2 camp buildings (6) + caravan radio post (1) + V2 item icons (15),
matching existing ForestArt pixel scales (PPU=256) and removing both outer white
background and enclosed pure-white structural gaps without touching intentional highlights.
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

BRAIN = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65"
BLD_SHEET = os.path.join(BRAIN, "v2_buildings_sheet_1795331751404.jpg")
ICO_SHEET = os.path.join(BRAIN, "v2_icons_and_radio_sheet_1795331763847.jpg")
OUT_DIR = "/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt"
CHECK_OUT = os.path.join(BRAIN, "scratch", "v2_assets_cutout_check.jpg")


def clean_cutout(cell_rgb: np.ndarray, hole_mode: str = "all") -> Image.Image:
    """
    hole_mode:
      - 'all': remove border white + all enclosed pure-white holes (buildings, woodbed, nails, tea)
      - 'top30': remove border white + enclosed pure-white holes only in top 32% (medkit handle, pipe wheel)
      - 'border': remove border-connected white only
    """
    h, w, _ = cell_rgb.shape
    rgb = cell_rgb.astype(np.float32)
    min_c = rgb.min(axis=2)
    max_c = rgb.max(axis=2)
    sat = max_c - min_c

    # Border flood-fill candidate: bright & low saturation
    bg_cand = (min_c > 234.0) & (sat < 18.0)
    labeled, _ = ndimage.label(bg_cand)
    border_labels = set()
    border_labels.update(labeled[0, :].tolist())
    border_labels.update(labeled[-1, :].tolist())
    border_labels.update(labeled[:, 0].tolist())
    border_labels.update(labeled[:, -1].tolist())
    border_labels.discard(0)
    bg_mask = np.isin(labeled, list(border_labels))

    if hole_mode in ("all", "top30"):
        # Enclosed pure-white structural gaps (between wooden posts, sewing frame, handles)
        pure_white = (min_c > 243.0) & (sat < 12.0) & (~bg_mask)
        hl_lbl, n_hl = ndimage.label(pure_white)
        for lid in range(1, n_hl + 1):
            ys, xs = np.where(hl_lbl == lid)
            if len(ys) < 10:
                continue
            if hole_mode == "top30" and ys.max() > int(h * 0.34):
                continue
            bg_mask |= (hl_lbl == lid)

    # Anti-aliased soft edge transition & white-fringe decontamination
    fg_mask = ~bg_mask
    dist_to_bg = ndimage.distance_transform_edt(fg_mask)
    alpha = np.ones((h, w), dtype=np.float32)
    alpha[bg_mask] = 0.0

    # 1.8px fringe around background: convert residual JPEG white blend into clean alpha
    fringe = fg_mask & (dist_to_bg <= 2.2) & (min_c > 195.0) & (sat < 26.0)
    whiteness = np.clip((min_c - 175.0) / 75.0, 0.0, 1.0)
    alpha[fringe] = np.minimum(alpha[fringe], 1.0 - whiteness[fringe] * 0.88)

    # Darken fringe RGB slightly toward dark ink so no white halo appears over warm dirt/trees
    edge_ring = fg_mask & (dist_to_bg <= 1.6) & (min_c > 180.0)
    rgb[edge_ring] = rgb[edge_ring] * 0.68

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    rgba[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    # Tight crop to non-transparent bounding box + 4px padding
    ys, xs = np.where(rgba[:, :, 3] > 18)
    if len(ys) == 0:
        return Image.fromarray(rgba)
    pad = 4
    y0, y1 = max(0, ys.min() - pad), min(h, ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(w, xs.max() + 1 + pad)
    return Image.fromarray(rgba[y0:y1, x0:x1])


def resize_fit(im: Image.Image, max_w: int, max_h: int) -> Image.Image:
    w, h = im.size
    scale = min(max_w / float(w), max_h / float(h))
    nw, nh = max(1, int(round(w * scale))), max(1, int(round(h * scale)))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(CHECK_OUT), exist_ok=True)

    saved_previews = []

    # 1. Process 3x2 Buildings Sheet
    bld_im = np.array(Image.open(BLD_SHEET).convert("RGB"))
    bh, bw, _ = bld_im.shape
    cw, ch = bw // 3, bh // 2

    bld_specs = [
        (0, 0, "heating_tower", 270, 340, "all"),
        (0, 1, "camp_warehouse", 390, 350, "all"),
        (0, 2, "refugee_cabin", 385, 345, "all"),
        (1, 0, "scrap_smithy", 395, 355, "all"),
        (1, 1, "tailor_shop", 395, 355, "all"),
        (1, 2, "herb_hut", 390, 350, "all"),
    ]

    for r, c, key, mw, mh, hmode in bld_specs:
        cell = bld_im[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw]
        cut = clean_cutout(cell, hole_mode=hmode)
        out = resize_fit(cut, mw, mh)
        out_path = os.path.join(OUT_DIR, f"{key}.png")
        out.save(out_path)
        print(f"Saved building {key}.png -> {out.size}")
        saved_previews.append((key, out))

    # 2. Process 4x4 Icons + Radio Post Sheet
    ico_im = np.array(Image.open(ICO_SHEET).convert("RGB"))
    ih, iw, _ = ico_im.shape
    icw, ich = iw // 4, ih // 4

    ico_specs = [
        (0, 0, "ico_blueprint", 132, 132, "border"),
        (0, 1, "ico_nails", 132, 132, "all"),
        (0, 2, "ico_ingot", 132, 132, "border"),
        (0, 3, "ico_pipe", 132, 132, "top30"),
        (1, 0, "ico_cloth", 132, 132, "border"),
        (1, 1, "ico_pelt", 132, 132, "border"),
        (1, 2, "ico_shirt", 132, 132, "border"),
        (1, 3, "ico_boots", 132, 132, "all"),
        (2, 0, "ico_coat", 132, 132, "border"),
        (2, 1, "ico_armor", 132, 132, "all"),
        (2, 2, "ico_woodbed", 136, 132, "all"),
        (2, 3, "ico_herb", 132, 132, "border"),
        (3, 0, "ico_poultice", 132, 132, "border"),
        (3, 1, "ico_tea", 132, 132, "all"),
        (3, 2, "ico_medkit", 132, 132, "top30"),
        (3, 3, "radio_post", 340, 320, "all"),
    ]

    for r, c, key, mw, mh, hmode in ico_specs:
        if key == "radio_post":
            # Extend upward to capture the full tall antenna mast rising above row 3 boundary
            y_top = int(2.72 * ich)
            cell = ico_im[y_top:ih, c * icw:(c + 1) * icw].copy()
            cut_raw = np.array(clean_cutout(cell, hole_mode=hmode))
            # Keep only foreground components that belong to row 3 (dismiss any herb leaf in upper-left)
            fg = cut_raw[:, :, 3] > 18
            lbl, n_l = ndimage.label(fg)
            if n_l > 1:
                sizes = [(lid, int((lbl == lid).sum())) for lid in range(1, n_l + 1)]
                main_lid = max(sizes, key=lambda t: t[1])[0]
                cut_raw[lbl != main_lid, 3] = 0
                ys, xs = np.where(cut_raw[:, :, 3] > 18)
                cut = Image.fromarray(cut_raw[max(0, ys.min()-4):min(cut_raw.shape[0], ys.max()+5),
                                              max(0, xs.min()-4):min(cut_raw.shape[1], xs.max()+5)])
            else:
                cut = Image.fromarray(cut_raw)
        elif key == "ico_herb":
            cell = ico_im[r * ich:(r + 1) * ich, c * icw:(c + 1) * icw].copy()
            cut_raw = np.array(clean_cutout(cell, hole_mode=hmode))
            fg = cut_raw[:, :, 3] > 18
            lbl, n_l = ndimage.label(fg)
            if n_l > 1:
                sizes = [(lid, int((lbl == lid).sum())) for lid in range(1, n_l + 1)]
                main_lid = max(sizes, key=lambda t: t[1])[0]
                cut_raw[lbl != main_lid, 3] = 0
                ys, xs = np.where(cut_raw[:, :, 3] > 18)
                cut = Image.fromarray(cut_raw[max(0, ys.min()-4):min(cut_raw.shape[0], ys.max()+5),
                                              max(0, xs.min()-4):min(cut_raw.shape[1], xs.max()+5)])
            else:
                cut = Image.fromarray(cut_raw)
        else:
            cell = ico_im[r * ich:(r + 1) * ich, c * icw:(c + 1) * icw]
            cut = clean_cutout(cell, hole_mode=hmode)
        out = resize_fit(cut, mw, mh)
        out_path = os.path.join(OUT_DIR, f"{key}.png")
        out.save(out_path)
        print(f"Saved asset {key}.png -> {out.size}")
        saved_previews.append((key, out))

    # 3. Render QA contact sheet over camp dirt tan (205, 168, 108) to verify zero white residue!
    cols = 6
    rows = (len(saved_previews) + cols - 1) // cols
    tile = 240
    sheet = Image.new("RGBA", (cols * tile, rows * tile), (198, 162, 104, 255))
    for idx, (key, sp) in enumerate(saved_previews):
        rr, cc = divmod(idx, cols)
        thumb = resize_fit(sp, tile - 24, tile - 24)
        ox = cc * tile + (tile - thumb.width) // 2
        oy = rr * tile + (tile - thumb.height) // 2
        sheet.alpha_composite(thumb, (ox, oy))
    sheet.convert("RGB").save(CHECK_OUT, quality=92)
    print("Saved QA cutout sheet ->", CHECK_OUT)


if __name__ == "__main__":
    main()
