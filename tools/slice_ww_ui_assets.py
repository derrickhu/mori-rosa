#!/usr/bin/python3
"""
Slice, background-key, defringe, resize, and write Unity Sprite .png.meta files for
all 29 Water-World style comic UI assets (ww_*.png) produced by the built-in
`image-generator` subagent.

Strictly processes ONLY genuine `image-generator` raw output images passed via CLI
(--call1 .. --call5); never synthesizes fake procedural art.
"""
from __future__ import annotations

import argparse
import hashlib
import os
import re
import sys
from pathlib import Path
from typing import List, Optional, Set, Tuple

USER_SITE = "/Users/rosa/Library/Python/3.9/lib/python/site-packages"
if os.path.isdir(USER_SITE) and USER_SITE not in sys.path:
    sys.path.append(USER_SITE)

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

REPO_ROOT = Path(__file__).resolve().parents[1]
ASSETS_ROOT = REPO_ROOT / "Assets"
OUT_DIR = ASSETS_ROOT / "Mori" / "ForestSlice" / "Resources" / "ForestArt"


def collect_existing_guids(assets_dir: Path) -> Set[str]:
    guids: Set[str] = set()
    guid_re = re.compile(r"^guid:\s*(\S+)", re.MULTILINE)
    for root, _, files in os.walk(assets_dir):
        for fn in files:
            if not fn.endswith(".meta"):
                continue
            p = Path(root) / fn
            try:
                txt = p.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            m = guid_re.search(txt)
            if m:
                guids.add(m.group(1).strip())
    return guids


def read_existing_guid(meta_path: Path) -> Optional[str]:
    if not meta_path.is_file():
        return None
    try:
        txt = meta_path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return None
    m = re.search(r"^guid:\s*(\S+)", txt, re.MULTILINE)
    return m.group(1).strip() if m else None


def make_unique_hex_guid(key: str, used: Set[str]) -> str:
    salt = 0
    while True:
        seed = f"mori-rosa:ww-ui-sprite:{key}:{salt}".encode("utf-8")
        cand = hashlib.md5(seed).hexdigest().lower()
        if len(cand) == 32 and cand not in used:
            used.add(cand)
            return cand
        salt += 1


def write_sprite_meta(png_path: Path, is_opaque: bool, max_tex_size: int, used_guids: Set[str]) -> str:
    meta_path = Path(str(png_path) + ".meta")
    existing = read_existing_guid(meta_path)
    if existing and len(existing) == 32 and all(ch in "0123456789abcdef" for ch in existing):
        guid = existing
        used_guids.add(guid)
    else:
        guid = make_unique_hex_guid(png_path.name, used_guids)

    alpha_trans = 0 if is_opaque else 1
    meta_text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 14
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  webStreaming: 0
  priorityLevel: 0
  uploadedMode: 2
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_tex_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: {alpha_trans}
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_tex_size}
    maxPlaceholderSize: 32
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: {max_tex_size}
    maxPlaceholderSize: 32
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""
    meta_path.write_text(meta_text, encoding="utf-8")
    return guid


def find_gutter_splits(sheet_rgb: np.ndarray, count: int, axis: int) -> List[int]:
    """
    Finds optimal pure-white gutter split coordinates along `axis` (0=rows/y, 1=cols/x)
    within +/- 6% of each ideal 1/count boundary so protruding comic spikes/ribbons/flames
    are never clipped.
    """
    length = sheet_rgb.shape[axis]
    min_c = sheet_rgb.min(axis=2)
    sat = sheet_rgb.max(axis=2).astype(np.int16) - min_c.astype(np.int16)
    non_white = (min_c < 238) | (sat > 20)
    # Sum non-white pixels along the perpendicular axis
    proj = non_white.sum(axis=(1 if axis == 0 else 0)).astype(np.float64)
    # Smooth slightly over 5px window to prefer center of wide white gutters
    kernel = np.array([0.15, 0.2, 0.3, 0.2, 0.15])
    smooth = np.convolve(proj, kernel, mode="same")

    splits = [0]
    win = max(8, int(round(length * 0.065)))
    for k in range(1, count):
        ideal = int(round(k * length / float(count)))
        lo = max(1, ideal - win)
        hi = min(length - 1, ideal + win)
        segment = smooth[lo : hi + 1]
        min_val = segment.min()
        # Pick the candidate with minimum ink closest to ideal
        candidates = np.where(segment <= min_val + 0.5)[0] + lo
        best = int(candidates[np.argmin(np.abs(candidates - ideal))])
        splits.append(best)
    splits.append(length)
    return splits


def slice_grid_smart(sheet_rgb: np.ndarray, rows: int, cols: int, r: int, c: int) -> np.ndarray:
    y_splits = find_gutter_splits(sheet_rgb, rows, axis=0)
    x_splits = find_gutter_splits(sheet_rgb, cols, axis=1)
    return sheet_rgb[y_splits[r] : y_splits[r + 1], x_splits[c] : x_splits[c + 1]].copy()


def cutout_comic_cell(
    cell_rgb: np.ndarray,
    hole_mode: str = "border",
    white_min: float = 236.0,
    sat_max: float = 22.0,
) -> Image.Image:
    """
    Cleanly cuts out a bold black-ink comic UI icon from a solid white grid cell.
    hole_mode:
      - 'border': flood-fill exterior white connected to cell borders ONLY (keeps interior white icons
                  like back arrow, fist, question mark, dust cloud, lettering 100% opaque!).
      - 'top42': exterior white + enclosed pure-white structural loops strictly inside the top 42% of
                 the icon's foreground bbox (e.g. gift bag top handle slot, toolbox handle gap).
      - 'bottom25': exterior white + enclosed pure-white gaps in bottom 25% (e.g. between crossed axe handles).
    """
    sub = cell_rgb.copy()
    h, w, _ = sub.shape

    # Pad 4px pure white around cell so border flood-fill wraps cleanly around all 4 sides
    pad = np.full((h + 8, w + 8, 3), 255, dtype=np.uint8)
    pad[4 : 4 + h, 4 : 4 + w] = sub

    rgb_f = pad.astype(np.float32)
    min_c = rgb_f.min(axis=2)
    max_c = rgb_f.max(axis=2)
    sat = max_c - min_c

    bg_cand = (min_c >= white_min) & (sat <= sat_max)
    labeled, _ = ndimage.label(bg_cand)
    ext_label = labeled[0, 0]
    bg_mask = (labeled == ext_label)[4 : 4 + h, 4 : 4 + w]

    sub_f = sub.astype(np.float32)
    s_min = sub_f.min(axis=2)
    s_sat = sub_f.max(axis=2) - s_min

    fg_initial = ~bg_mask
    fg_ys, _ = np.where(fg_initial)
    if len(fg_ys) > 0 and hole_mode in ("top42", "bottom25"):
        fy0, fy1 = int(fg_ys.min()), int(fg_ys.max())
        fh = max(1, fy1 - fy0 + 1)
        pure_white = (s_min >= 242.0) & (s_sat <= 16.0) & (~bg_mask)
        hl_lbl, n_hl = ndimage.label(pure_white)
        for lid in range(1, n_hl + 1):
            ys, xs = np.where(hl_lbl == lid)
            if len(ys) < 40:
                continue
            if hole_mode == "top42" and ys.max() <= fy0 + int(fh * 0.42):
                bg_mask |= hl_lbl == lid
            elif hole_mode == "bottom25" and ys.min() >= fy0 + int(fh * 0.74):
                bg_mask |= hl_lbl == lid

    fg = ~bg_mask
    # Despeckle ONLY tiny JPEG dust (< 16 px) so floating comic stars & shotgun sparks stay 100% intact
    comp, n_comp = ndimage.label(fg)
    if n_comp > 0:
        sizes = ndimage.sum(fg, comp, range(1, n_comp + 1))
        for cid, sz in enumerate(sizes, start=1):
            if sz < 16:
                fg[comp == cid] = False

    # Anti-aliased comic ink edge + white halo decontamination
    dist_in = ndimage.distance_transform_edt(fg)
    alpha = np.where(fg, 1.0, 0.0).astype(np.float32)

    fringe = fg & (dist_in <= 2.2) & (s_min > 192.0) & (s_sat < 28.0)
    whiteness = np.clip((s_min - 175.0) / 76.0, 0.0, 1.0)
    alpha[fringe] = np.minimum(alpha[fringe], 1.0 - whiteness[fringe] * 0.90)

    # Pull bright outer 1.6px boundary toward dark comic charcoal (#171B2E) so zero white halo appears
    edge_ring = fg & (dist_in <= 1.6) & (s_min > 175.0) & (s_sat < 32.0)
    sub_f[edge_ring, 0] *= 0.35
    sub_f[edge_ring, 1] *= 0.36
    sub_f[edge_ring, 2] *= 0.42

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.clip(sub_f, 0, 255).astype(np.uint8)
    rgba[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)

    ys, xs = np.where(rgba[:, :, 3] > 16)
    if len(xs) == 0:
        return Image.fromarray(rgba, "RGBA")

    pad_px = 4
    x0, x1 = max(0, int(xs.min()) - pad_px), min(w, int(xs.max()) + 1 + pad_px)
    y0, y1 = max(0, int(ys.min()) - pad_px), min(h, int(ys.max()) + 1 + pad_px)
    return Image.fromarray(rgba[y0:y1, x0:x1], "RGBA")


def fit_on_canvas(im: Image.Image, canvas_w: int, canvas_h: int, margin: int = 6) -> Image.Image:
    """Centers the tight-cropped RGBA icon inside an exact (canvas_w x canvas_h) transparent canvas."""
    avail_w = max(8, canvas_w - margin * 2)
    avail_h = max(8, canvas_h - margin * 2)
    w, h = im.size
    scale = min(avail_w / float(w), avail_h / float(h))
    nw = max(1, int(round(w * scale)))
    nh = max(1, int(round(h * scale)))
    resized = im.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    ox = (canvas_w - nw) // 2
    oy = (canvas_h - nh) // 2
    canvas.alpha_composite(resized, (ox, oy))
    return canvas


def resize_proportional(im: Image.Image, max_w: int, max_h: int) -> Image.Image:
    w, h = im.size
    scale = min(max_w / float(w), max_h / float(h))
    nw = max(1, int(round(w * scale)))
    nh = max(1, int(round(h * scale)))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def process_call1_cabin(src_path: Path, used_guids: Set[str], saved: List[Tuple[str, Image.Image]]) -> None:
    im = Image.open(src_path).convert("RGB")
    w, h = im.size
    side = min(w, h)
    x0 = (w - side) // 2
    y0 = (h - side) // 2
    sq = im.crop((x0, y0, x0 + side, y0 + side)).resize((720, 720), Image.Resampling.LANCZOS)
    out_path = OUT_DIR / "ww_hero_cabin_bg.png"
    sq.save(out_path, format="PNG", optimize=True)
    write_sprite_meta(out_path, is_opaque=True, max_tex_size=2048, used_guids=used_guids)
    print(f"[Call 1] Saved {out_path.name} ({sq.size}, RGB opaque)")
    saved.append(("ww_hero_cabin_bg", sq.convert("RGBA")))


def trim_congrats_plaque(im: Image.Image) -> Image.Image:
    """
    Removes the tall outer slate-navy starburst spikes (`#24335A`) behind `恭喜获得` while
    preserving 100% of `image-generator`'s pink-cream 3D block letters, maroon 3D extrusion,
    and thick black comic ink outline (`~2.4:1` wide header aspect with zero clipped edges).
    """
    arr = np.array(im)
    h, w, _ = arr.shape
    r = arr[:, :, 0].astype(np.int16)
    g = arr[:, :, 1].astype(np.int16)
    b = arr[:, :, 2].astype(np.int16)
    a = arr[:, :, 3]

    # Core 3D lettering + maroon 3D block shadow (pink/cream/maroon always has R > B + 20 and R >= 105,
    # whereas the outer slate-navy starburst has B > R + 18!)
    warm_letters = (a > 180) & (r >= 105) & ((r - b) >= 20)
    # Dilate by 8px to keep the genuine thick black comic ink outline around every character
    keep_zone = ndimage.binary_dilation(warm_letters, iterations=8)

    cleaned = arr.copy()
    # Outside the 8px ink border around the 3D letters, drop the protruding navy spikes
    drop = (~keep_zone) & (a > 0)
    cleaned[drop, 3] = 0

    # Soften 1.5px outer edge of the preserved black comic outline
    fg = cleaned[:, :, 3] > 32
    dist = ndimage.distance_transform_edt(fg)
    aa = np.where(fg, np.clip(dist * 215.0, 0, 255), 0).astype(np.uint8)
    cleaned[:, :, 3] = np.minimum(cleaned[:, :, 3], aa)

    ys, xs = np.where(cleaned[:, :, 3] > 16)
    if len(xs) == 0:
        return im
    pad = 4
    y0, y1 = max(0, int(ys.min()) - pad), min(h, int(ys.max()) + 1 + pad)
    x0, x1 = max(0, int(xs.min()) - pad), min(w, int(xs.max()) + 1 + pad)
    return Image.fromarray(cleaned[y0:y1, x0:x1], "RGBA")


def process_call2_banners_badges(src_path: Path, used_guids: Set[str], saved: List[Tuple[str, Image.Image]]) -> None:
    sheet = np.array(Image.open(src_path).convert("RGB"))
    # (row, col, name, hole_mode, fit_mode, w, h)
    specs = [
        (0, 0, "ww_banner_congrats", "border", "prop", 512, 220),
        (0, 1, "ww_banner_levelup", "border", "prop", 512, 224),
        (0, 2, "ww_btn_back_red", "border", "square", 224, 224),
        (1, 0, "ww_badge_power_fist", "border", "square", 224, 224),
        (1, 1, "ww_order_gift_box", "top42", "square", 256, 256),
        (1, 2, "ww_smiley_morale", "border", "square", 192, 192),
        (2, 0, "ww_check_green", "border", "square", 160, 160),
        (2, 1, "ww_cross_red", "border", "square", 160, 160),
        (2, 2, "ww_xp_star", "border", "square", 192, 192),
    ]
    for r, c, name, hmode, fmode, tw, th in specs:
        cell = slice_grid_smart(sheet, 3, 3, r, c)
        cut = cutout_comic_cell(cell, hole_mode=hmode)
        if name == "ww_banner_congrats":
            cut = trim_congrats_plaque(cut)
        out_im = fit_on_canvas(cut, tw, th, margin=6) if fmode == "square" else resize_proportional(cut, tw, th)
        out_path = OUT_DIR / f"{name}.png"
        out_im.save(out_path, format="PNG", optimize=True)
        write_sprite_meta(out_path, is_opaque=False, max_tex_size=512, used_guids=used_guids)
        print(f"[Call 2] Saved {out_path.name} -> {out_im.size}")
        saved.append((name, out_im))


def process_call3_dock(src_path: Path, used_guids: Set[str], saved: List[Tuple[str, Image.Image]]) -> None:
    sheet = np.array(Image.open(src_path).convert("RGB"))
    # Sheet is 3x3 where rows 0 and 1 hold the 6 unique dock icons (row 2 is duplicate of row 1)
    specs = [
        (0, 0, "ww_dock_battle", "bottom25"),
        (0, 1, "ww_dock_heroes", "border"),
        (0, 2, "ww_dock_bag", "border"),
        (1, 0, "ww_dock_build", "top42"),
        (1, 1, "ww_dock_delegate", "border"),
        (1, 2, "ww_dock_citymap", "border"),
    ]
    for r, c, name, hmode in specs:
        cell = slice_grid_smart(sheet, 3, 3, r, c)
        cut = cutout_comic_cell(cell, hole_mode=hmode)
        out_im = fit_on_canvas(cut, 256, 256, margin=6)
        out_path = OUT_DIR / f"{name}.png"
        out_im.save(out_path, format="PNG", optimize=True)
        write_sprite_meta(out_path, is_opaque=False, max_tex_size=512, used_guids=used_guids)
        print(f"[Call 3] Saved {out_path.name} -> {out_im.size}")
        saved.append((name, out_im))


def process_call4_depts(src_path: Path, used_guids: Set[str], saved: List[Tuple[str, Image.Image]]) -> None:
    sheet = np.array(Image.open(src_path).convert("RGB"))
    specs = [
        (0, 0, "ww_dept_workshop", "border"),
        (0, 1, "ww_dept_build", "border"),
        (1, 0, "ww_dept_farm", "border"),
        (1, 1, "ww_dept_trade", "border"),
    ]
    for r, c, name, hmode in specs:
        cell = slice_grid_smart(sheet, 2, 2, r, c)
        cut = cutout_comic_cell(cell, hole_mode=hmode)
        out_im = fit_on_canvas(cut, 256, 256, margin=4)
        out_path = OUT_DIR / f"{name}.png"
        out_im.save(out_path, format="PNG", optimize=True)
        write_sprite_meta(out_path, is_opaque=False, max_tex_size=512, used_guids=used_guids)
        print(f"[Call 4] Saved {out_path.name} -> {out_im.size}")
        saved.append((name, out_im))


def process_call5_skills(src_path: Path, used_guids: Set[str], saved: List[Tuple[str, Image.Image]]) -> None:
    sheet = np.array(Image.open(src_path).convert("RGB"))
    specs = [
        (0, 0, "ww_skill_shield"),
        (0, 1, "ww_skill_fire"),
        (0, 2, "ww_skill_cleave"),
        (1, 0, "ww_skill_arrow"),
        (1, 1, "ww_skill_shotgun"),
        (1, 2, "ww_skill_heal"),
        (2, 0, "ww_passive_sword"),
        (2, 1, "ww_passive_heart"),
        (2, 2, "ww_passive_fury"),
    ]
    for r, c, name in specs:
        cell = slice_grid_smart(sheet, 3, 3, r, c)
        cut = cutout_comic_cell(cell, hole_mode="border")
        out_im = fit_on_canvas(cut, 192, 192, margin=4)
        out_path = OUT_DIR / f"{name}.png"
        out_im.save(out_path, format="PNG", optimize=True)
        write_sprite_meta(out_path, is_opaque=False, max_tex_size=512, used_guids=used_guids)
        print(f"[Call 5] Saved {out_path.name} -> {out_im.size}")
        saved.append((name, out_im))


def build_contact_sheet(saved: List[Tuple[str, Image.Image]], out_path: Path) -> None:
    if not saved:
        return
    cols = 5
    rows = (len(saved) + cols - 1) // cols
    tile_w, tile_h = 340, 204
    sheet = Image.new("RGBA", (cols * tile_w, rows * tile_h), (24, 30, 52, 255))
    draw = ImageDraw.Draw(sheet)

    for idx, (name, im) in enumerate(saved):
        r, c = divmod(idx, cols)
        x0, y0 = c * tile_w, r * tile_h
        # Left half dark royal indigo (#232B52), right half light periwinkle card (#E2E8F8)
        draw.rectangle([x0 + 4, y0 + 4, x0 + tile_w // 2, y0 + tile_h - 26], fill=(35, 43, 82, 255))
        draw.rectangle([x0 + tile_w // 2, y0 + 4, x0 + tile_w - 4, y0 + tile_h - 26], fill=(226, 232, 248, 255))

        thumb = resize_proportional(im, 152, 152)
        sheet.alpha_composite(thumb, (x0 + (tile_w // 2 - thumb.width) // 2, y0 + 8 + (158 - thumb.height) // 2))
        sheet.alpha_composite(
            thumb,
            (x0 + tile_w // 2 + (tile_w // 2 - thumb.width) // 2, y0 + 8 + (158 - thumb.height) // 2),
        )
        draw.text((x0 + 8, y0 + tile_h - 21), f"{name} ({im.width}x{im.height})", fill=(235, 242, 255, 255))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out_path, format="JPEG", quality=93)
    print(f"[QA] Contact sheet -> {out_path}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Slice Water-World UI sprite sheets from image-generator.")
    parser.add_argument("--call1", type=Path, help="Raw image path for Call 1 (Hero Cabin Stage 1:1)")
    parser.add_argument("--call2", type=Path, help="Raw image path for Call 2 (3x3 Banners & Core Badges)")
    parser.add_argument("--call3", type=Path, help="Raw image path for Call 3 (3x3 sheet, top 2 rows = 6 Dock Icons)")
    parser.add_argument("--call4", type=Path, help="Raw image path for Call 4 (2x2 Delegation Dept Vignettes)")
    parser.add_argument("--call5", type=Path, help="Raw image path for Call 5 (3x3 Circular Skill Medallions)")
    parser.add_argument(
        "--contact-out",
        type=Path,
        default=Path("/Users/rosa/.gemini/antigravity/brain/652995c2-60ff-42de-b824-f0ca369f0025/scratch/ww_ui_cutout_qa.jpg"),
        help="Output path for dual-background visual QA contact sheet",
    )
    args = parser.parse_args()

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    used_guids = collect_existing_guids(ASSETS_ROOT)
    saved: List[Tuple[str, Image.Image]] = []

    if args.call1 and args.call1.is_file():
        process_call1_cabin(args.call1, used_guids, saved)
    if args.call2 and args.call2.is_file():
        process_call2_banners_badges(args.call2, used_guids, saved)
    if args.call3 and args.call3.is_file():
        process_call3_dock(args.call3, used_guids, saved)
    if args.call4 and args.call4.is_file():
        process_call4_depts(args.call4, used_guids, saved)
    if args.call5 and args.call5.is_file():
        process_call5_skills(args.call5, used_guids, saved)

    if saved:
        build_contact_sheet(saved, args.contact_out)
    else:
        print("No input sheet files supplied yet; pass --call1 .. --call5 once generated by image-generator.")


if __name__ == "__main__":
    main()
