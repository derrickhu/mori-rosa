#!/usr/bin/env python3
"""
Builds the V4 Ecological Farm & Ranch art pack into Assets/Mori/ForestSlice/Resources/ForestArt/:
  - beast_ranch.png       (360px isometric woodland animal corral + timber lean-to + boar & trough)
  - ico_seed_veggie.png   (136px burlap seed pouch + pumpkin/sprout badge)
  - ico_seed_flax.png     (136px linen seed pouch + teal flax & fiber badge)
  - ico_seed_herb.png     (136px herbalist seed pouch + glowing medicinal herb badge)
  - ico_seed_golden.png   (136px golden mutant miracle seed pouch + starlight glow)
  - ico_compost.png       (136px rustic wooden bucket + rich dark humus & green sprout)

Follows <RULE[user_global]>: tries Vertex AI (with fast 14s timeout) after built-in subagent 503,
and falls back to high-detail local PIL composition from authentic project hand-drawn sprites if Vertex is unreachable/503.
"""
import base64
import json
import math
import os
import subprocess
import urllib.request
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter
from scipy import ndimage

REPO = Path(__file__).resolve().parents[1]
RES_ART = REPO / "Assets" / "Mori" / "ForestSlice" / "Resources" / "ForestArt"
BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")
PROMPT_FILE = BRAIN / "scratch" / "ranch_seeds_prompt.txt"
SHEET_OUT = BRAIN / "ranch_and_seeds_sheet.png"
PREVIEW_OUT = BRAIN / "ranch_and_seeds_cutout_preview.png"


def cutout_cell(rgb_arr: np.ndarray) -> Image.Image:
    h, w, _ = rgb_arr.shape
    pad = np.full((h + 8, w + 8, 3), 255, dtype=np.uint8)
    pad[4:4 + h, 4:4 + w] = rgb_arr
    lum = pad.mean(axis=2)
    chroma = pad.max(axis=2).astype(np.int16) - pad.min(axis=2).astype(np.int16)
    near_white = (lum > 238.0) & (chroma < 22)
    labeled, _ = ndimage.label(near_white)
    ext_bg = (labeled == labeled[0, 0])[4:4 + h, 4:4 + w]

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
    comp, ncomp = ndimage.label(fg)
    if ncomp > 0:
        sizes = ndimage.sum(fg, comp, range(1, ncomp + 1))
        for cid, sz in enumerate(sizes, start=1):
            if sz < 20:
                fg[comp == cid] = False

    dist_in = ndimage.distance_transform_edt(fg)
    alpha = np.clip(dist_in * 210.0, 0, 255).astype(np.uint8)
    outer_ring = fg & (dist_in <= 1.6)
    lum_orig = rgb_arr.mean(axis=2)
    fringe = outer_ring & (lum_orig > 215.0)
    alpha[fringe] = (alpha[fringe] * 0.2).astype(np.uint8)
    out_rgb = rgb_arr.copy()
    bright_edge = outer_ring & (lum_orig > 180.0)
    out_rgb[bright_edge] = (out_rgb[bright_edge] * 0.35).astype(np.uint8)

    ys, xs = np.where(alpha > 12)
    if len(xs) == 0:
        return Image.fromarray(np.dstack([out_rgb, alpha]), "RGBA")
    x0, x1 = max(0, int(xs.min()) - 4), min(w, int(xs.max()) + 5)
    y0, y1 = max(0, int(ys.min()) - 4), min(h, int(ys.max()) + 5)
    return Image.fromarray(np.dstack([out_rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1]]), "RGBA")


def resize_fit(im: Image.Image, target_max: int) -> Image.Image:
    w, h = im.size
    m = max(w, h)
    if m <= 0:
        return im
    sc = target_max / float(m)
    return im.resize((max(8, round(w * sc)), max(8, round(h * sc))), Image.Resampling.LANCZOS)


def try_vertex_sheet() -> bool:
    cfg_path = Path.home() / ".config" / "vertex" / "config.json"
    if not cfg_path.is_file() or not PROMPT_FILE.is_file():
        return False
    try:
        cfg = json.loads(cfg_path.read_text(encoding="utf-8"))
        proj = cfg.get("project_id")
        if not proj:
            return False
        tok = subprocess.check_output(
            ["gcloud", "auth", "application-default", "print-access-token"],
            timeout=6, stderr=subprocess.DEVNULL
        ).decode().strip()
        if not tok:
            return False

        prompt_txt = PROMPT_FILE.read_text(encoding="utf-8")
        url = f"https://us-central1-aiplatform.googleapis.com/v1/projects/{proj}/locations/us-central1/publishers/google/models/gemini-2.5-flash-image:generateContent"
        body = json.dumps({
            "contents": [{"role": "user", "parts": [{"text": prompt_txt}]}],
            "generationConfig": {"responseModalities": ["IMAGE"], "imageConfig": {"aspectRatio": "4:3"}},
        }).encode("utf-8")

        for proxy_url in ("http://127.0.0.1:7897", None):
            try:
                handlers = [urllib.request.ProxyHandler({"http": proxy_url, "https": proxy_url})] if proxy_url else [urllib.request.ProxyHandler({})]
                opener = urllib.request.build_opener(*handlers)
                req = urllib.request.Request(url, data=body, headers={
                    "Authorization": f"Bearer {tok}",
                    "Content-Type": "application/json",
                })
                with opener.open(req, timeout=14) as resp:
                    data = json.loads(resp.read().decode("utf-8"))
                for cand in data.get("candidates", []):
                    for part in cand.get("content", {}).get("parts", []):
                        inl = part.get("inlineData")
                        if inl and inl.get("data"):
                            raw = base64.b64decode(inl["data"])
                            SHEET_OUT.write_bytes(raw)
                            print(f"[Vertex] Generated sheet via proxy={proxy_url} -> {SHEET_OUT}")
                            return True
            except Exception as e:
                print(f"[Vertex] Attempt proxy={proxy_url} skipped: {e}")
    except Exception as e:
        print(f"[Vertex] Token/config skipped: {e}")
    return False


def slice_from_vertex_sheet() -> dict:
    im = Image.open(SHEET_OUT).convert("RGB")
    w, h = im.size
    cw, ch = w // 3, h // 2
    specs = [
        (0, 0, "beast_ranch", 356),
        (0, 1, "ico_seed_veggie", 136),
        (0, 2, "ico_seed_flax", 136),
        (1, 0, "ico_seed_herb", 136),
        (1, 1, "ico_seed_golden", 136),
        (1, 2, "ico_compost", 136),
    ]
    out = {}
    for r, c, name, tmax in specs:
        cell = np.array(im.crop((c * cw + 6, r * ch + 6, (c + 1) * cw - 6, (r + 1) * ch - 6)))
        cut = resize_fit(cutout_cell(cell), tmax)
        out[name] = cut
    return out


def tint_rgba(im: Image.Image, r_mul: float, g_mul: float, b_mul: float, sat: float = 1.0) -> Image.Image:
    im = im.convert("RGBA")
    if sat != 1.0:
        rgb = ImageEnhance.Color(im.convert("RGB")).enhance(sat)
        im = Image.merge("RGBA", (*rgb.split(), im.split()[3]))
    arr = np.array(im, dtype=np.float32)
    arr[..., 0] = np.clip(arr[..., 0] * r_mul, 0, 255)
    arr[..., 1] = np.clip(arr[..., 1] * g_mul, 0, 255)
    arr[..., 2] = np.clip(arr[..., 2] * b_mul, 0, 255)
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def add_comic_outline(im: Image.Image, radius: int = 3, color=(28, 20, 14, 255)) -> Image.Image:
    im = im.convert("RGBA")
    pad = radius + 4
    canvas = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    canvas.paste(im, (pad, pad), im)
    alpha = np.array(canvas)[..., 3] > 28
    dilated = ndimage.binary_dilation(alpha, iterations=radius)
    outline = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ol_arr = np.zeros((canvas.height, canvas.width, 4), dtype=np.uint8)
    ol_arr[dilated] = color
    outline = Image.fromarray(ol_arr, "RGBA")
    outline.alpha_composite(canvas)
    bbox = outline.getbbox()
    return outline.crop(bbox) if bbox else outline


def build_local_composite_pack() -> dict:
    """Composites crisp, 100% style-matched comic ink-line Ranch + Seed/Compost icons from existing ForestArt PNGs."""
    hut = Image.open(RES_ART / "herb_hut.png").convert("RGBA")
    fence = Image.open(RES_ART / "fence.png").convert("RGBA")
    boar = Image.open(RES_ART / "boar.png").convert("RGBA")
    farm2 = Image.open(RES_ART / "farm_2.png").convert("RGBA")
    logs = Image.open(RES_ART / "logs.png").convert("RGBA")
    bag = Image.open(RES_ART / "dock_bag.png").convert("RGBA")
    veggie = Image.open(RES_ART / "ico_veggie.png").convert("RGBA")
    cloth = Image.open(RES_ART / "ico_cloth.png").convert("RGBA")
    herb = Image.open(RES_ART / "ico_herb.png").convert("RGBA")
    star = Image.open(RES_ART / "ico_xp_star.png").convert("RGBA")
    tuft = Image.open(RES_ART / "tuft_0.png").convert("RGBA")
    water = Image.open(RES_ART / "ico_water.png").convert("RGBA")

    # 1. beast_ranch.png (isometric fenced corral + warm timber roof shed + hay/soil floor + boar + veggie/water trough)
    rc = Image.new("RGBA", (420, 390), (0, 0, 0, 0))
    dr = ImageDraw.Draw(rc)
    # Isometric dirt & straw corral diamond pad
    diamond = [(210, 150), (388, 242), (210, 344), (32, 242)]
    dr.polygon(diamond, fill=(156, 122, 78, 255), outline=(34, 24, 16, 255))
    inner_d = [(210, 162), (370, 242), (210, 330), (50, 242)]
    dr.polygon(inner_d, fill=(182, 145, 88, 255))
    # Straw patches
    for sx, sy, sw, sh in [(145, 226, 68, 28), (225, 252, 74, 30), (175, 272, 60, 24)]:
        dr.ellipse((sx, sy, sx + sw, sy + sh), fill=(218, 178, 92, 220), outline=(92, 68, 34, 190), width=2)

    # Back-left timber shelter roof cropped/scaled from herb_hut
    shed = resize_fit(tint_rgba(hut, 1.04, 0.96, 0.88), 235)
    rc.alpha_composite(shed, (68, 36))

    # Back-right fence rail
    f_back = resize_fit(fence, 155)
    rc.alpha_composite(f_back, (228, 124))

    # Cute chunky wild boar + small white snow-rabbit companion inside corral
    b_main = resize_fit(boar, 128)
    rc.alpha_composite(b_main, (168, 172))
    # Fluffy white rabbit (drawn in crisp comic ink style)
    dr.ellipse((112, 228, 154, 260), fill=(245, 240, 230, 255), outline=(32, 22, 16, 255), width=3)
    dr.ellipse((136, 214, 164, 242), fill=(248, 244, 236, 255), outline=(32, 22, 16, 255), width=3)
    dr.ellipse((138, 194, 148, 220), fill=(246, 238, 228, 255), outline=(32, 22, 16, 255), width=3)
    dr.ellipse((150, 196, 160, 220), fill=(246, 238, 228, 255), outline=(32, 22, 16, 255), width=3)
    dr.ellipse((153, 224, 158, 229), fill=(36, 24, 20, 255))

    # Wooden feeding trough with veggies & water
    trough_poly = [(236, 254), (318, 214), (334, 234), (252, 276)]
    dr.polygon(trough_poly, fill=(122, 82, 48, 255), outline=(30, 20, 14, 255))
    v_small = resize_fit(veggie, 48)
    rc.alpha_composite(v_small, (254, 222))
    w_small = resize_fit(water, 38)
    rc.alpha_composite(w_small, (288, 212))

    # Front-left & front-right isometric wooden corral fences
    f_fl = resize_fit(fence.transpose(Image.Transpose.FLIP_LEFT_RIGHT), 162)
    f_fr = resize_fit(fence, 162)
    rc.alpha_composite(f_fl, (36, 202))
    rc.alpha_composite(f_fr, (212, 206))
    beast_ranch = resize_fit(add_comic_outline(rc, radius=2), 356)

    def make_seed_pouch(sack_tint, badge_img, ribbon_col, sparkle_gold=False) -> Image.Image:
        ic = Image.new("RGBA", (180, 180), (0, 0, 0, 0))
        d = ImageDraw.Draw(ic)
        if sparkle_gold:
            # Radiant amber halo behind golden seed pouch
            for rad, alpha in [(74, 55), (60, 95), (46, 140)]:
                d.ellipse((90 - rad, 94 - rad, 90 + rad, 94 + rad), fill=(255, 206, 68, alpha))

        # Draw chunky hand-crafted burlap seed pouch with tied top ruffle & cord
        # Top ruffle flare
        d.polygon([(58, 42), (44, 20), (74, 26), (90, 14), (106, 26), (136, 20), (122, 42)],
                  fill=sack_tint, outline=(30, 20, 14, 255))
        # Main plump seed sack body
        d.rounded_rectangle((34, 42, 146, 158), radius=42, fill=sack_tint, outline=(30, 20, 14, 255), width=5)
        # Inner warm highlight & bottom shadow fold
        hi = tuple(min(255, int(c * 1.14)) for c in sack_tint[:3]) + (190,)
        sh = tuple(max(0, int(c * 0.76)) for c in sack_tint[:3]) + (210,)
        d.ellipse((48, 54, 124, 112), fill=hi)
        d.arc((42, 92, 138, 150), start=15, end=165, fill=sh, width=8)
        # Tied hemp/colored cord at neck
        d.rounded_rectangle((54, 38, 126, 52), radius=6, fill=ribbon_col, outline=(30, 20, 14, 255), width=4)

        # Cream emblem patch on front of seed sack + crisp crop icon
        d.ellipse((54, 66, 126, 138), fill=(248, 241, 224, 245), outline=(48, 34, 22, 255), width=3)
        b_fit = resize_fit(badge_img, 64)
        ic.alpha_composite(b_fit, (90 - b_fit.width // 2, 102 - b_fit.height // 2))

        if sparkle_gold:
            st_s = resize_fit(star, 40)
            ic.alpha_composite(st_s, (118, 24))
        return resize_fit(add_comic_outline(ic, radius=2), 136)

    ico_seed_veggie = make_seed_pouch((198, 154, 98, 255), veggie, (96, 168, 64, 255))
    ico_seed_flax = make_seed_pouch((176, 162, 134, 255), cloth, (68, 146, 188, 255))
    ico_seed_herb = make_seed_pouch((142, 168, 118, 255), herb, (206, 68, 58, 255))
    gold_herb = tint_rgba(veggie, 1.25, 1.08, 0.45, sat=1.35)
    ico_seed_golden = make_seed_pouch((118, 78, 44, 255), gold_herb, (246, 188, 42, 255), sparkle_gold=True)

    # 6. ico_compost.png: rustic wooden bucket overflowing with rich dark humus soil + sprout
    cp = Image.new("RGBA", (180, 180), (0, 0, 0, 0))
    dc = ImageDraw.Draw(cp)
    # Wooden bucket trapezoid
    dc.polygon([(36, 74), (144, 74), (130, 158), (50, 158)], fill=(148, 96, 54, 255), outline=(30, 20, 14, 255))
    for lx in (66, 90, 114):
        dc.line([(lx, 78), (lx - (90 - lx) // 5, 154)], fill=(78, 48, 26, 220), width=3)
    # Iron hoops
    dc.rectangle((38, 92, 142, 102), fill=(76, 80, 86, 255), outline=(28, 20, 14, 255), width=3)
    dc.rectangle((46, 132, 134, 142), fill=(76, 80, 86, 255), outline=(28, 20, 14, 255), width=3)
    # Rich dark compost soil mound on top
    dc.ellipse((32, 46, 148, 92), fill=(68, 46, 30, 255), outline=(28, 18, 12, 255), width=4)
    dc.ellipse((48, 38, 132, 78), fill=(86, 58, 38, 255), outline=(28, 18, 12, 255), width=3)
    # Green sprout emerging from fertile compost
    spr = resize_fit(tint_rgba(herb, 0.95, 1.18, 0.85, sat=1.25), 66)
    cp.alpha_composite(spr, (90 - spr.width // 2, 10))
    ico_compost = resize_fit(add_comic_outline(cp, radius=2), 136)

    return {
        "beast_ranch": beast_ranch,
        "ico_seed_veggie": ico_seed_veggie,
        "ico_seed_flax": ico_seed_flax,
        "ico_seed_herb": ico_seed_herb,
        "ico_seed_golden": ico_seed_golden,
        "ico_compost": ico_compost,
    }


def main():
    RES_ART.mkdir(parents=True, exist_ok=True)
    used_vertex = try_vertex_sheet()
    pack = slice_from_vertex_sheet() if used_vertex else build_local_composite_pack()

    for name, img in pack.items():
        out_p = RES_ART / f"{name}.png"
        img.save(out_p, "PNG")
        print(f"Saved {out_p.name}: {img.size} (vertex={used_vertex})")

    # Render dark-checker inspection sheet
    prev = Image.new("RGBA", (920, 420), (42, 36, 30, 255))
    pd = ImageDraw.Draw(prev)
    x = 24
    for name, img in pack.items():
        disp = resize_fit(img, 240 if name == "beast_ranch" else 124)
        prev.alpha_composite(disp, (x, 200 - disp.height // 2))
        x += disp.width + 22
    prev.save(PREVIEW_OUT, "PNG")
    print(f"Preview -> {PREVIEW_OUT}")


if __name__ == "__main__":
    main()
