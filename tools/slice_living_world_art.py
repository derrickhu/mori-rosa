#!/usr/bin/python3
"""
tools/slice_living_world_art.py

Production art slicer, alpha cleaner, 1024x1024 9-part skeletal puppet atlas assembler,
horizontal flipbook strip builder, and collision-free Unity `.png.meta` writer for the
Living World Art Upgrade (20 PNG assets across 4 categories).

STRICT RULE:
- Processes ONLY genuine built-in `image-generator` raw sheets supplied via CLI / JSON spec.
- NEVER calls external APIs (no Vertex AI) and NEVER draws procedural placeholder characters.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence, Set, Tuple

USER_SITE = "/Users/rosa/Library/Python/3.9/lib/python/site-packages"
if os.path.isdir(USER_SITE) and USER_SITE not in sys.path:
    sys.path.append(USER_SITE)

import numpy as np
from PIL import Image, ImageDraw, ImageOps
from scipy import ndimage

REPO_ROOT = Path(__file__).resolve().parents[1]
ASSETS_ROOT = REPO_ROOT / "Assets"
OUT_DIR = ASSETS_ROOT / "Mori" / "ForestSlice" / "Resources" / "ForestArt"
COORDINATOR_BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")

CELL_PUPPET = 320
PPU_PUPPET = 256.0

# Exact Unity Slice UV pivots from ForestHeroPuppet.LoadOrGetAtlas (x: left->right, y: bottom->top)
UV_HEAD = (0.48, 0.10)
UV_ACC = (0.88, 0.58)
UV_TORSO = (0.52, 0.12)
UV_ARM = (0.50, 0.84)
UV_LEG = (0.50, 0.86)
UV_TOOL = (0.46, 0.16)


# ---------------------------------------------------------------------------
# 1. Collision-Free Unity `.png.meta` Management
# ---------------------------------------------------------------------------

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
        seed = f"mori-rosa:living-world-art:{key}:{salt}".encode("utf-8")
        cand = hashlib.md5(seed).hexdigest().lower()
        if len(cand) == 32 and cand not in used:
            used.add(cand)
            return cand
        salt += 1


def write_unity_png_meta(
    png_path: Path,
    used_guids: Set[str],
    asset_kind: str,
    ppu: int = 256,
    pivot: Tuple[float, float] = (0.5, 0.08),
    weixin_max_size: int = 2048,
) -> str:
    """
    Writes a Unity TextureImporter `.png.meta` with a collision-free 32-hex lowercase GUID,
    matching `ForestArtPostprocessor.cs` & `WeChatMiniGameBuilder.cs` ASTC_4x4 settings 1:1.
    """
    meta_path = Path(str(png_path) + ".meta")
    existing = read_existing_guid(meta_path)
    if existing:
        guid = existing
        used_guids.add(guid)
    else:
        guid = make_unique_hex_guid(png_path.name, used_guids)

    if asset_kind == "puppet_atlas":
        tex_type = 0
        sprite_mesh_type = 1
        alignment = 0
        px, py = 0.5, 0.5
        meta_ppu = 100
        wx_max = 1024
    elif asset_kind == "bust":
        tex_type = 8
        sprite_mesh_type = 0
        alignment = 9
        px, py = 0.5, 0.5
        meta_ppu = 256
        wx_max = 512
    else:
        tex_type = 8
        sprite_mesh_type = 0
        alignment = 9
        px, py = pivot
        meta_ppu = ppu
        wx_max = weixin_max_size

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
  maxTextureSize: 2048
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
  spriteMeshType: {sprite_mesh_type}
  alignment: {alignment}
  spritePivot: {{x: {px}, y: {py}}}
  spritePixelsToUnits: {meta_ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: {tex_type}
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
    maxTextureSize: 2048
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
    maxTextureSize: 2048
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
  - serializedVersion: 3
    buildTarget: WebGL
    maxTextureSize: 2048
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
  - serializedVersion: 3
    buildTarget: WeixinMiniGame
    maxTextureSize: {wx_max}
    maxPlaceholderSize: 32
    resizeAlgorithm: 0
    textureFormat: 48
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""
    meta_path.write_text(meta_text, encoding="utf-8")
    return guid


# ---------------------------------------------------------------------------
# 2. Exterior-Flood-Fill Cutout, Defringing & Component-Based Unclipped Slicer
# ---------------------------------------------------------------------------

def find_gutter_splits(sheet_rgb: np.ndarray, count: int, axis: int, search_ratio: float = 0.08) -> List[int]:
    length = sheet_rgb.shape[axis]
    if count <= 1:
        return [0, length]

    min_c = sheet_rgb.min(axis=2)
    sat = sheet_rgb.max(axis=2).astype(np.int16) - min_c.astype(np.int16)
    non_white = (min_c < 238) | (sat > 20)
    proj = non_white.sum(axis=(1 if axis == 0 else 0)).astype(np.float64)
    kernel = np.array([0.12, 0.20, 0.36, 0.20, 0.12])
    smooth = np.convolve(proj, kernel, mode="same")

    splits = [0]
    win = max(8, int(round(length * search_ratio)))
    for k in range(1, count):
        ideal = int(round(k * length / float(count)))
        lo = max(1, ideal - win)
        hi = min(length - 1, ideal + win)
        segment = smooth[lo : hi + 1]
        min_val = segment.min()
        candidates = np.where(segment <= min_val + 1.0)[0] + lo
        best = int(candidates[np.argmin(np.abs(candidates - ideal))])
        splits.append(best)
    splits.append(length)
    return splits


def cutout_comic_rgba_array(
    sub_rgb: np.ndarray,
    white_min: float = 236.0,
    sat_max: float = 22.0,
    punch_enclosed_holes_min_px: int = 0,
    hole_y_min_ratio: float = 0.0,
    min_island_px: int = 20,
    keep_only_largest: bool = False,
) -> Tuple[np.ndarray, np.ndarray]:
    """
    Returns `(rgba_uint8, fg_bool)` using 8-connected exterior pure-white border flood-fill
    + optional enclosed flat-white background loop punching (sparing eye whites, teeth,
    bandages, yellow hardhats, shaded white rabbit fur, and cooking steam puffs) + dark comic
    ink outline halo decontamination.
    """
    sub = sub_rgb.copy()
    h, w, _ = sub.shape

    pad = np.full((h + 8, w + 8, 3), 255, dtype=np.uint8)
    pad[4 : 4 + h, 4 : 4 + w] = sub

    rgb_f = pad.astype(np.float32)
    min_c = rgb_f.min(axis=2)
    max_c = rgb_f.max(axis=2)
    sat = max_c - min_c

    bg_cand = (min_c >= white_min) & (sat <= sat_max)
    struct8 = np.ones((3, 3), dtype=np.int32)
    labeled, _ = ndimage.label(bg_cand, structure=struct8)
    ext_label = labeled[0, 0]
    bg_mask = (labeled == ext_label)[4 : 4 + h, 4 : 4 + w]

    sub_f = sub.astype(np.float32)
    s_min = sub_f.min(axis=2)
    s_sat = sub_f.max(axis=2) - s_min

    if punch_enclosed_holes_min_px > 0:
        y_gate = int(round(h * hole_y_min_ratio))
        pure_white = (s_min >= 247.0) & (s_sat <= 10.0) & (~bg_mask)
        hl_lbl, n_hl = ndimage.label(pure_white)
        for lid in range(1, n_hl + 1):
            ys, _ = np.where(hl_lbl == lid)
            if len(ys) >= punch_enclosed_holes_min_px and int(ys.min()) >= y_gate:
                bg_mask |= (hl_lbl == lid)

    fg = ~bg_mask
    comp, n_comp = ndimage.label(fg)
    if n_comp > 0:
        sizes = ndimage.sum(fg, comp, range(1, n_comp + 1))
        if keep_only_largest:
            best_cid = int(np.argmax(sizes)) + 1
            fg = (comp == best_cid)
        else:
            for cid, sz in enumerate(sizes, start=1):
                if sz < min_island_px:
                    fg[comp == cid] = False

    dist_in = ndimage.distance_transform_edt(fg)
    alpha = np.where(fg, 1.0, 0.0).astype(np.float32)

    fringe = fg & (dist_in <= 2.1) & (s_min > 194.0) & (s_sat < 26.0)
    whiteness = np.clip((s_min - 176.0) / 75.0, 0.0, 1.0)
    alpha[fringe] = np.minimum(alpha[fringe], 1.0 - whiteness[fringe] * 0.90)

    edge_ring = fg & (dist_in <= 1.6) & (s_min > 178.0) & (s_sat < 30.0)
    sub_f[edge_ring, 0] *= 0.30
    sub_f[edge_ring, 1] *= 0.28
    sub_f[edge_ring, 2] *= 0.32

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.clip(sub_f, 0, 255).astype(np.uint8)
    rgba[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    return rgba, fg


def cutout_comic_sprite(
    cell_rgb: np.ndarray,
    punch_enclosed_holes_min_px: int = 0,
    keep_only_largest: bool = False,
    min_island_px: int = 20,
    pad_out: int = 2,
) -> Image.Image:
    rgba, _ = cutout_comic_rgba_array(
        cell_rgb,
        punch_enclosed_holes_min_px=punch_enclosed_holes_min_px,
        min_island_px=min_island_px,
        keep_only_largest=keep_only_largest,
    )
    h, w, _ = rgba.shape
    ys, xs = np.where(rgba[:, :, 3] > 16)
    if len(xs) == 0:
        return Image.fromarray(rgba, "RGBA")
    x0 = max(0, int(xs.min()) - pad_out)
    x1 = min(w, int(xs.max()) + 1 + pad_out)
    y0 = max(0, int(ys.min()) - pad_out)
    y1 = min(h, int(ys.max()) + 1 + pad_out)
    return Image.fromarray(rgba[y0:y1, x0:x1], "RGBA")


def extract_grid_frames_by_components(
    sheet_path: Path,
    rows: int,
    cols: int,
    punch_enclosed_holes_min_px: int = 0,
    hole_y_min_ratio: float = 0.0,
) -> Dict[Tuple[int, int], Image.Image]:
    """
    Extracts `rows x cols` character/animal frames from a pure-white spritesheet with ZERO
    straight-line boundary clipping (e.g. when an armored zombie's raised iron pipe or a
    fisherman's rod extends slightly into an adjacent cell's white margin):
    1. Keys & defringes the full sheet once.
    2. Finds the `rows * cols` largest connected components (the main character bodies) and
       sorts them cleanly into `(r, c)` slots (top-to-bottom in `rows` bands, left-to-right).
    3. Attaches every secondary FX component (`area >= 18px`: steam puffs, embers, sawdust,
       water droplets, motion arcs, ground sparks) to its nearest main character component
       by minimum bbox-to-bbox distance!
    """
    rgb = np.array(Image.open(sheet_path).convert("RGB"))
    rgba, fg = cutout_comic_rgba_array(
        rgb,
        punch_enclosed_holes_min_px=punch_enclosed_holes_min_px,
        hole_y_min_ratio=hole_y_min_ratio,
        min_island_px=18,
        keep_only_largest=False,
    )
    h, w, _ = rgba.shape
    clbl, n_comp = ndimage.label(fg, structure=np.ones((3, 3), dtype=np.int32))

    target_n = rows * cols
    comp_info: List[Tuple[int, int, int, int, int, int, float, float]] = []
    for cid in range(1, n_comp + 1):
        ys, xs = np.where(clbl == cid)
        area = len(xs)
        if area < 18:
            continue
        comp_info.append((
            area,
            cid,
            int(xs.min()),
            int(ys.min()),
            int(xs.max()) + 1,
            int(ys.max()) + 1,
            float(xs.mean()),
            float(ys.mean()),
        ))

    comp_info.sort(key=lambda t: t[0], reverse=True)
    if len(comp_info) < target_n:
        raise RuntimeError(f"{sheet_path.name}: expected >= {target_n} components, found {len(comp_info)}")

    majors = comp_info[:target_n]
    extras = comp_info[target_n:]

    # Sort majors by vertical centroid into `rows` groups of `cols`, then horizontal centroid
    majors_by_y = sorted(majors, key=lambda t: t[7])
    grid_majors: Dict[Tuple[int, int], Tuple[int, int, int, int, int, int, float, float]] = {}
    owner_cids: Dict[Tuple[int, int], List[int]] = {}
    for r in range(rows):
        row_slice = sorted(majors_by_y[r * cols : (r + 1) * cols], key=lambda t: t[6])
        for c, m in enumerate(row_slice):
            grid_majors[(r, c)] = m
            owner_cids[(r, c)] = [m[1]]

    def bbox_dist(a_box: Tuple[int, int, int, int], b_box: Tuple[int, int, int, int]) -> float:
        ax0, ay0, ax1, ay1 = a_box
        bx0, by0, bx1, by1 = b_box
        dx = max(0, ax0 - bx1, bx0 - ax1)
        dy = max(0, ay0 - by1, by0 - ay1)
        return math.hypot(dx, dy)

    for ex in extras:
        _, ecid, ex0, ey0, ex1, ey1, _, _ = ex
        ebox = (ex0, ey0, ex1, ey1)
        best_rc = min(
            grid_majors.keys(),
            key=lambda rc: bbox_dist(ebox, grid_majors[rc][2:6]),
        )
        owner_cids[best_rc].append(ecid)

    frames_out: Dict[Tuple[int, int], Image.Image] = {}
    for rc, cids in owner_cids.items():
        mask = np.isin(clbl, cids)
        ys, xs = np.where(mask)
        x0 = max(0, int(xs.min()) - 2)
        x1 = min(w, int(xs.max()) + 3)
        y0 = max(0, int(ys.min()) - 2)
        y1 = min(h, int(ys.max()) + 3)
        sub_rgba = rgba[y0:y1, x0:x1].copy()
        sub_mask = mask[y0:y1, x0:x1]
        sub_rgba[~sub_mask, 3] = 0
        frames_out[rc] = Image.fromarray(sub_rgba, "RGBA")

    return frames_out


def tight_crop_rgba(im: Image.Image, alpha_thresh: int = 16) -> Image.Image:
    arr = np.asarray(im)
    ys, xs = np.where(arr[:, :, 3] > alpha_thresh)
    if len(xs) == 0:
        return im
    return im.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


# ---------------------------------------------------------------------------
# 3. Character Polish Helpers (Leimeng Nose Touch-Up & Back-Limb Depth Tint)
# ---------------------------------------------------------------------------

def fix_leimeng_nose_triangle(head_rgba: Image.Image) -> Image.Image:
    """
    In `leimeng_puppet_sheet_v3`, directly below Leimeng's nose band-aid (`x_uv ~ 0.61..0.72`,
    `y_uv ~ 0.592..0.682`), `image-generator` shaded the nose tip as a tiny solid black triangle.
    This replaces ONLY the interior of that tiny dark nose-hole triangle with Leimeng's warm
    human nose skin tone + subtle underside comic outline so his face matches `bust_leimeng.png` 100%!
    """
    arr = np.asarray(head_rgba).copy()
    h, w, _ = arr.shape
    x0, x1 = int(round(w * 0.605)), int(round(w * 0.730))
    y0, y1 = int(round(h * 0.592)), int(round(h * 0.684))

    roi = arr[y0:y1, x0:x1]
    lum = roi[:, :, :3].mean(axis=2)
    dark = (roi[:, :, 3] > 180) & (lum < 85.0)
    if dark.sum() < 12:
        return head_rgba

    # Sample Leimeng's actual cheek skin color just to the left of the nose (`x_uv ~ 0.42..0.50, y_uv ~ 0.63..0.68`)
    sx0, sx1 = int(round(w * 0.42)), int(round(w * 0.50))
    sy0, sy1 = int(round(h * 0.63)), int(round(h * 0.68))
    cheek = arr[sy0:sy1, sx0:sx1, :3].astype(np.float32)
    skin_light = np.percentile(cheek.reshape(-1, 3), 75, axis=0)  # ~ (208, 172, 148)
    skin_warm = np.clip(skin_light * np.array([1.02, 0.90, 0.84]), 120, 235)
    skin_shade = np.clip(skin_warm * np.array([0.82, 0.74, 0.70]), 90, 200)

    # Erode `dark` by 2px from bottom and right so a crisp 2px comic nose-tip contour remains!
    dys, dxs = np.where(dark)
    dy_min, dy_max = int(dys.min()), int(dys.max())
    dx_min, dx_max = int(dxs.min()), int(dxs.max())
    rh = max(1, dy_max - dy_min + 1)
    rw = max(1, dx_max - dx_min + 1)

    for ry, rx in zip(dys, dxs):
        u = (rx - dx_min) / float(rw)
        v = (ry - dy_min) / float(rh)
        # Keep a thin 1.8px dark comic contour ONLY along the bottom nostril/tip shadow & right bridge edge
        on_bottom_outline = (ry >= dy_max - 1) and (u >= 0.18)
        on_right_outline = (rx >= dx_max - 1) and (v >= 0.25)
        if on_bottom_outline or on_right_outline:
            roi[ry, rx, :3] = np.array([34, 24, 22], dtype=np.uint8)
        else:
            shade_t = np.clip(u * 0.55 + v * 0.45, 0.0, 1.0)
            col = skin_warm * (1.0 - shade_t) + skin_shade * shade_t
            roi[ry, rx, :3] = np.clip(col, 0, 255).astype(np.uint8)

    arr[y0:y1, x0:x1] = roi
    return Image.fromarray(arr, "RGBA")


def darken_back_limb(rgba: Image.Image, factor: float = 0.86) -> Image.Image:
    """Slightly darkens interior fills of `legFar` / `armFar` while keeping black comic ink crisp."""
    arr = np.asarray(rgba).copy()
    rgb = arr[:, :, :3].astype(np.float32)
    lum = rgb.mean(axis=2)
    interior = (arr[:, :, 3] > 32) & (lum > 42.0)
    rgb[interior] *= factor
    arr[:, :, :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def round_top_hip_cap(leg_rgba: Image.Image) -> Image.Image:
    """
    Softens sharp top-left/top-right waist cut corners on detached pant legs (e.g. Tieshu's legs)
    and adds a 3px dark comic ink cap so +/-21 deg stride rotations never poke sharp corners!
    """
    arr = np.asarray(leg_rgba).copy()
    h, w, _ = arr.shape
    cap_h = max(8, int(round(h * 0.07)))
    ink = np.array([26, 22, 20, 255], dtype=np.uint8)
    for y in range(cap_h):
        xs = np.where(arr[y, :, 3] > 100)[0]
        if len(xs) < 6:
            continue
        t = 1.0 - (y / float(cap_h))
        trim = int(round((len(xs) * 0.22) * (t * t)))
        if trim > 0:
            arr[y, xs[0] : xs[0] + trim, 3] = 0
            arr[y, max(0, xs[-1] - trim + 1) : xs[-1] + 1, 3] = 0
        xs2 = np.where(arr[y, :, 3] > 100)[0]
        if len(xs2) > 0 and y < 3:
            arr[y, xs2] = ink
        elif len(xs2) >= 6:
            arr[y, xs2[:3]] = ink
            arr[y, xs2[-3:]] = ink
    return Image.fromarray(arr, "RGBA")


# ---------------------------------------------------------------------------
# 4. Bust Portrait & 1024x1024 Puppet Atlas Builders
# ---------------------------------------------------------------------------

def save_bust_portrait(
    raw_im: Image.Image,
    out_key: str,
    used_guids: Set[str],
    target_h: int = 400,
    pad_px: int = 4,
) -> Image.Image:
    im = tight_crop_rgba(raw_im)
    scale = float(target_h) / max(1, im.height)
    nw = max(8, int(round(im.width * scale)))
    resized = im.resize((nw, target_h), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (nw + pad_px * 2, target_h + pad_px * 2), (0, 0, 0, 0))
    canvas.alpha_composite(resized, (pad_px, pad_px))

    out_path = OUT_DIR / f"{out_key}.png"
    canvas.save(out_path, format="PNG", optimize=True)
    write_unity_png_meta(out_path, used_guids, asset_kind="bust")
    print(f"[Bust] Saved {out_path.name} -> {canvas.size}")
    return canvas


def place_part_in_cell(
    raw_part: Image.Image,
    target_size_px: int,
    part_pivot_uv: Tuple[float, float],
    cell_pivot_uv: Tuple[float, float],
    pre_rotate_deg: float = 0.0,
    flip_x: bool = False,
) -> Image.Image:
    im = tight_crop_rgba(raw_part)
    if flip_x:
        im = ImageOps.mirror(im)
    if abs(pre_rotate_deg) > 0.01:
        im = tight_crop_rgba(im.rotate(pre_rotate_deg, resample=Image.Resampling.BICUBIC, expand=True))

    sc = float(target_size_px) / max(1.0, float(im.height))
    nw = max(1, int(round(im.width * sc)))
    nh = max(1, int(round(im.height * sc)))
    rs = im.resize((nw, nh), Image.Resampling.LANCZOS)

    target_cx = int(round(cell_pivot_uv[0] * CELL_PUPPET))
    target_cy = int(round((1.0 - cell_pivot_uv[1]) * CELL_PUPPET))

    px_in_part = int(round(part_pivot_uv[0] * nw))
    py_in_part = int(round(part_pivot_uv[1] * nh))

    canvas = Image.new("RGBA", (CELL_PUPPET, CELL_PUPPET), (0, 0, 0, 0))
    ox = target_cx - px_in_part
    oy = target_cy - py_in_part
    canvas.alpha_composite(rs, (ox, oy))
    return canvas


def _rot2d(v: Tuple[float, float], deg: float) -> Tuple[float, float]:
    rad = math.radians(deg)
    cs, sn = math.cos(rad), math.sin(rad)
    return (v[0] * cs - v[1] * sn, v[0] * sn + v[1] * cs)


def _add2(a: Tuple[float, float], b: Tuple[float, float]) -> Tuple[float, float]:
    return (a[0] + b[0], a[1] + b[1])


def _composite_cell_part(
    board: Image.Image,
    root_x: int,
    root_y: int,
    world_offset: Tuple[float, float],
    cell_im: Image.Image,
    cell_pivot_uv: Tuple[float, float],
    unity_deg: float,
) -> None:
    cx = cell_pivot_uv[0] * CELL_PUPPET
    cy = (1.0 - cell_pivot_uv[1]) * CELL_PUPPET
    rot = cell_im.rotate(unity_deg, resample=Image.Resampling.BICUBIC, center=(cx, cy), expand=False)
    wx = int(round(root_x + world_offset[0] * PPU_PUPPET)) - int(round(cx))
    wy = int(round(root_y - world_offset[1] * PPU_PUPPET)) - int(round(cy))
    board.alpha_composite(rot, (wx, wy))


def pack_puppet_atlas_and_verify(
    atlas_key: str,
    cells: Dict[Tuple[int, int], Image.Image],
    rig_offsets: Dict[str, Tuple[float, float]],
    used_guids: Set[str],
    preview_out: Path,
    is_shotgun: bool,
    bust_ref: Optional[Image.Image],
) -> Image.Image:
    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    for (col, row), cim in cells.items():
        pil_x = col * CELL_PUPPET
        pil_y = 1024 - (row + 1) * CELL_PUPPET
        atlas.paste(cim, (pil_x, pil_y))

    out_path = OUT_DIR / f"{atlas_key}.png"
    atlas.save(out_path, format="PNG", optimize=True)
    write_unity_png_meta(out_path, used_guids, asset_kind="puppet_atlas")
    print(f"[PuppetAtlas] Saved {out_path.name} -> (1024, 1024)")

    # Replicate exact ForestHeroPuppet.cs Tick() angles (including ShotgunBlast lines 740-883!)
    tile_w, tile_h = 240, 350
    cols_n = 7
    preview = Image.new("RGBA", (cols_n * tile_w, tile_h), (222, 186, 122, 255))
    draw = ImageDraw.Draw(preview)
    for x in range(0, cols_n * tile_w, tile_w):
        draw.line([(x, 0), (x, tile_h)], fill=(180, 145, 90, 255), width=2)
    ground_y = 296
    draw.line([(0, ground_y), (cols_n * tile_w, ground_y)], fill=(150, 118, 70, 255), width=2)

    if bust_ref is not None:
        sc = min(210.0 / max(1, bust_ref.width), 255.0 / max(1, bust_ref.height))
        th = bust_ref.resize(
            (max(1, int(round(bust_ref.width * sc))), max(1, int(round(bust_ref.height * sc)))),
            Image.Resampling.LANCZOS,
        )
        preview.alpha_composite(th, (tile_w // 2 - th.width // 2, ground_y - th.height))
        draw.text((12, 8), f"{atlas_key} Bust", fill=(45, 32, 20, 255))

    if is_shotgun:
        # Exact ForestHeroPuppet.cs ToolAnimStyle.ShotgunBlast angles!
        poses = [
            ("1. Idle (Port)",   0,  0,   0,   0,   0,  24,  44,  -92, False, True),
            ("2. Walk A",       -3,  2,  10,  16, -16,  23,  44,  -92, False, True),
            ("3. Walk B",       -3,  2,  10, -16,  16,  33,  52, -100, False, True),
            ("4. Aim (-170)",  -10,  4,  11, -14,   8,  82,  76, -170, True,  True),
            ("5. Blast Kick!",  15, -5, -16,   8,  -5, 110, 100, -146, True,  True),
            ("6. Carry",        -5,  3,  14,  19, -19, 166, 156,    0, False, False),
        ]
    else:
        poses = [
            ("1. Idle",          0,   0,   0,   0,   0,  10,  -4,  -80, False, True),
            ("2. Walk A",       -3,   2,  10,  16, -16, -14,  14,  -72, False, True),
            ("3. Walk B",       -3,   2,  10, -16,  16,  20, -14,  -84, False, True),
            ("4. Chop Windup",  22,  -8, -24,  18, -11, 148, 136, -135, False, True),
            ("5. Chop Cleave!",-24,   8,  26, -16,  10,  42,  34, -102, True,  True),
            ("6. Carry",        -5,   3,  14,  19, -19, 166, 156,    0, False, False),
        ]

    pelvis = rig_offsets["pelvis"]
    leg_n = rig_offsets["leg_near"]
    leg_f = rig_offsets["leg_far"]
    torso = rig_offsets["torso"]
    head = rig_offsets["head"]
    acc = rig_offsets["acc"]
    arm_n = rig_offsets["arm_near"]
    arm_f = rig_offsets["arm_far"]
    tool_g = rig_offsets["tool"]

    for idx, (label, tA, hA, bA, lnA, lfA, anA, afA, axA, eff, showTool) in enumerate(poses):
        col_i = idx + 1
        rx = col_i * tile_w + tile_w // 2
        p_leg_f = _add2(pelvis, leg_f)
        p_leg_n = _add2(pelvis, leg_n)
        p_torso = _add2(pelvis, torso)
        p_arm_f = _add2(p_torso, _rot2d(arm_f, tA))
        p_head = _add2(p_torso, _rot2d(head, tA))
        tot_hA = tA + hA
        p_acc = _add2(p_head, _rot2d(acc, tot_hA))
        p_arm_n = _add2(p_torso, _rot2d(arm_n, tA))
        tot_anA = tA + anA
        p_tool = _add2(p_arm_n, _rot2d(tool_g, tot_anA))

        # Exact SyncWorldPos sorting order from ForestHeroPuppet.cs lines 616-628:
        # LegFar(1) -> ArmFar(2) -> LegNear(3) -> Torso(4) -> Acc(6) -> Head(7) -> Tool(8) -> ArmNear(10)
        _composite_cell_part(preview, rx, ground_y, p_leg_f, cells[(1, 0)], UV_LEG, lfA)
        _composite_cell_part(preview, rx, ground_y, p_arm_f, cells[(2, 1)], UV_ARM, tA + afA)
        _composite_cell_part(preview, rx, ground_y, p_leg_n, cells[(0, 0)], UV_LEG, lnA)
        _composite_cell_part(preview, rx, ground_y, p_torso, cells[(0, 1)], UV_TORSO, tA)
        _composite_cell_part(preview, rx, ground_y, p_acc, cells[(2, 2)], UV_ACC, tot_hA + bA)
        _composite_cell_part(preview, rx, ground_y, p_head, cells[(1, 2)] if eff else cells[(0, 2)], UV_HEAD, tot_hA)
        if showTool:
            _composite_cell_part(preview, rx, ground_y, p_tool, cells[(2, 0)], UV_TOOL, tot_anA + axA)
        _composite_cell_part(preview, rx, ground_y, p_arm_n, cells[(1, 1)], UV_ARM, tot_anA)
        draw.text((col_i * tile_w + 10, 8), label, fill=(45, 32, 20, 255))

    preview_out.parent.mkdir(parents=True, exist_ok=True)
    preview.convert("RGB").save(preview_out, format="JPEG", quality=94)
    print(f"[QA] Saved 6-pose FK rig preview -> {preview_out}")
    return atlas


def build_leimeng_puppet(
    sheet_path: Path,
    used_guids: Set[str],
    qa_dir: Path,
    bust_ref: Optional[Image.Image],
) -> None:
    rgb = np.array(Image.open(sheet_path).convert("RGB"))
    # Extract each isolated component cleanly by exact bbox (zero clipping on tall fire-axe!)
    raw_head_open = fix_leimeng_nose_triangle(cutout_comic_sprite(rgb[8:305, 90:310], keep_only_largest=True))
    raw_head_effort = fix_leimeng_nose_triangle(cutout_comic_sprite(rgb[8:305, 440:660], keep_only_largest=True))

    band_full = cutout_comic_sprite(rgb[50:286, 730:985], keep_only_largest=True)
    # Trim the wide right-side headband strap past the knot (keep knot + left fluttering tails, 0..64% width)
    raw_band_tails = tight_crop_rgba(band_full.crop((0, 0, int(band_full.width * 0.64), band_full.height)))

    raw_torso = cutout_comic_sprite(rgb[312:640, 74:336], keep_only_largest=True)
    raw_arm_near = cutout_comic_sprite(rgb[324:668, 434:566], keep_only_largest=True)
    raw_arm_far = darken_back_limb(cutout_comic_sprite(rgb[324:662, 654:778], keep_only_largest=True), 0.90)

    # Use clean single detached side-3/4 leg at (2,1) for legNear AND slightly darkened legFar
    raw_leg_single = cutout_comic_sprite(rgb[668:1016, 422:574], keep_only_largest=True)
    raw_leg_near = raw_leg_single
    raw_leg_far = darken_back_limb(raw_leg_single, 0.85)

    raw_axe = cutout_comic_sprite(rgb[556:1010, 782:994], keep_only_largest=True)

    # Exact PuppetRegistry["leimeng"] offsets from ForestHeroPuppet.cs lines 225-233
    rig = {
        "pelvis": (0.0, 0.34),
        "leg_near": (-0.032, 0.01),
        "leg_far": (0.032, 0.01),
        "torso": (0.0, -0.02),
        "head": (0.014, 0.265),
        "acc": (-0.068, 0.14),
        "arm_near": (-0.078, 0.225),
        "arm_far": (0.086, 0.212),
        "tool": (0.012, -0.23),
    }

    cells = {
        (0, 2): place_part_in_cell(raw_head_open,   80, (0.488, 0.93), UV_HEAD),
        (1, 2): place_part_in_cell(raw_head_effort, 80, (0.485, 0.93), UV_HEAD),
        (2, 2): place_part_in_cell(raw_band_tails,  42, (0.94,  0.22), UV_ACC),
        (0, 1): place_part_in_cell(raw_torso,       96, (0.52,  0.86), UV_TORSO),
        (1, 1): place_part_in_cell(raw_arm_near,    82, (0.54,  0.14), UV_ARM),
        (2, 1): place_part_in_cell(raw_arm_far,     80, (0.46,  0.14), UV_ARM),
        (0, 0): place_part_in_cell(raw_leg_near,    98, (0.48,  0.12), UV_LEG),
        (1, 0): place_part_in_cell(raw_leg_far,     97, (0.46,  0.12), UV_LEG),
        (2, 0): place_part_in_cell(raw_axe,        108, (0.45,  0.76), UV_TOOL),
    }
    pack_puppet_atlas_and_verify(
        atlas_key="leimeng_puppet_atlas",
        cells=cells,
        rig_offsets=rig,
        used_guids=used_guids,
        preview_out=qa_dir / "leimeng_puppet_atlas_fk_preview.jpg",
        is_shotgun=False,
        bust_ref=bust_ref,
    )


def build_tieshu_puppet(
    sheet_path: Path,
    used_guids: Set[str],
    qa_dir: Path,
    bust_ref: Optional[Image.Image],
) -> None:
    rgb = np.array(Image.open(sheet_path).convert("RGB"))
    raw_head_open = cutout_comic_sprite(rgb[22:320, 42:294], keep_only_largest=True)
    raw_head_effort = cutout_comic_sprite(rgb[22:318, 382:618], keep_only_largest=True)
    raw_strap = cutout_comic_sprite(rgb[90:266, 828:890], keep_only_largest=True)

    raw_torso = cutout_comic_sprite(rgb[336:652, 54:294], keep_only_largest=True)
    raw_arm_near = cutout_comic_sprite(rgb[326:694, 470:584], keep_only_largest=True)
    raw_arm_far = darken_back_limb(cutout_comic_sprite(rgb[326:690, 670:780], keep_only_largest=True), 0.90)

    raw_leg_near = round_top_hip_cap(cutout_comic_sprite(rgb[656:1016, 86:238], keep_only_largest=True))
    raw_leg_far = darken_back_limb(
        round_top_hip_cap(cutout_comic_sprite(rgb[656:1012, 340:498], keep_only_largest=True)),
        0.86,
    )
    # Punch enclosed white inside the trigger guard loop (`punch_enclosed_holes_min_px=35`)!
    raw_shotgun = cutout_comic_sprite(rgb[418:930, 818:972], punch_enclosed_holes_min_px=35, keep_only_largest=True)

    # Exact PuppetRegistry["tieshu"] offsets from ForestHeroPuppet.cs lines 251-259
    rig = {
        "pelvis": (0.0, 0.325),
        "leg_near": (-0.028, 0.01),
        "leg_far": (0.030, 0.01),
        "torso": (0.0, -0.02),
        "head": (0.014, 0.250),
        "acc": (-0.064, 0.150),
        "arm_near": (-0.068, 0.212),
        "arm_far": (0.076, 0.204),
        "tool": (0.014, -0.21),
    }

    # Note: `flip_x=True` on raw_shotgun puts the trigger guard & pistol grip curve on `+X` (right side of cell),
    # so when rotated `-92 deg` (Idle) or `-98 deg` (Aim: torso -10 + arm 82 - tool 170 = -98 deg) in Unity,
    # the double barrels sit ON TOP and the trigger guard/stock curve DOWNWARD right into Tieshu's grip!
    cells = {
        (0, 2): place_part_in_cell(raw_head_open,   78, (0.559, 0.93), UV_HEAD),
        (1, 2): place_part_in_cell(raw_head_effort, 78, (0.597, 0.93), UV_HEAD),
        (2, 2): place_part_in_cell(raw_strap,       25, (0.72,  0.28), UV_ACC, pre_rotate_deg=-52.0),
        (0, 1): place_part_in_cell(raw_torso,       92, (0.54,  0.86), UV_TORSO),
        (1, 1): place_part_in_cell(raw_arm_near,    78, (0.50,  0.14), UV_ARM),
        (2, 1): place_part_in_cell(raw_arm_far,     76, (0.48,  0.14), UV_ARM),
        (0, 0): place_part_in_cell(raw_leg_near,    94, (0.46,  0.12), UV_LEG),
        (1, 0): place_part_in_cell(raw_leg_far,     93, (0.48,  0.12), UV_LEG),
        (2, 0): place_part_in_cell(raw_shotgun,    122, (0.48,  0.74), UV_TOOL, flip_x=True),
    }
    pack_puppet_atlas_and_verify(
        atlas_key="tieshu_puppet_atlas",
        cells=cells,
        rig_offsets=rig,
        used_guids=used_guids,
        preview_out=qa_dir / "tieshu_puppet_atlas_fk_preview.jpg",
        is_shotgun=True,
        bust_ref=bust_ref,
    )


# ---------------------------------------------------------------------------
# 5. Baseline-Locked Horizontal Flipbook Strip Builder (`anim_*.png`)
# ---------------------------------------------------------------------------

def build_horizontal_strip(
    raw_frames: Sequence[Image.Image],
    strip_key: str,
    used_guids: Set[str],
    cell_size: int = 256,
    target_ref_height: int = 206,
    foot_bottom_pad: int = 20,
    meta_ppu: int = 256,
    flip_x: bool = False,
) -> Image.Image:
    """
    Packs `N` animation frames left-to-right into `(N * cell_size) x cell_size` RGBA PNG:
    - Computes ONE unified scale factor from the movement/base frames (`min(2, N)`) so that
      raising a hammer, steel I-beam, or iron pipe overhead NEVER shrinks the character's body,
      while clamping safely so the tallest/widest frame fits inside `cell_size` with zero clipping!
    - Locks every frame's bottom foot pixel onto `y = cell_size - foot_bottom_pad` (`236` in
      `256x256`, `354` in `384x384`, matching `pivotY = 0.08f` in `ForestSpriteFlipbook`).
    - Stabilizes horizontal placement by centering the lower-body mass (`lower 70%`) on
      `cell_size // 2` so extended fishing rods, hammers, pipes, or beam slams don't shift torso X!
    """
    trimmed: List[Image.Image] = []
    for f in raw_frames:
        tf = tight_crop_rgba(f, alpha_thresh=24)
        if flip_x:
            tf = ImageOps.mirror(tf)
        trimmed.append(tf)

    max_headroom = cell_size - foot_bottom_pad - 6
    max_width_avail = cell_size - 10

    ref_n = min(2, len(trimmed))
    ref_h = float(np.median([trimmed[i].height for i in range(ref_n)]))
    base_scale = float(target_ref_height) / max(1.0, ref_h)

    tallest_h = max(im.height for im in trimmed)
    widest_w = max(im.width for im in trimmed)
    safe_scale = min(
        base_scale,
        float(max_headroom) / max(1, tallest_h),
        float(max_width_avail) / max(1, widest_w),
    )

    strip = Image.new("RGBA", (len(trimmed) * cell_size, cell_size), (0, 0, 0, 0))
    baseline_y = cell_size - foot_bottom_pad

    for idx, im in enumerate(trimmed):
        nw = max(4, int(round(im.width * safe_scale)))
        nh = max(4, int(round(im.height * safe_scale)))
        rs = im.resize((nw, nh), Image.Resampling.LANCZOS)
        arr = np.asarray(rs)
        fg = arr[:, :, 3] > 28
        ys, xs = np.where(fg)
        if len(xs) == 0:
            continue

        bottom_in_rs = int(ys.max()) + 1
        oy = max(2, baseline_y - bottom_in_rs)

        # Lower-body centroid stabilization
        y_cut = int(ys.min()) + int(round((ys.max() - ys.min() + 1) * 0.30))
        lower_fg = fg & (np.arange(nh)[:, None] >= y_cut)
        _, lxs = np.where(lower_fg)
        cx_in_rs = float(np.mean(lxs)) if len(lxs) > 20 else float(nw) * 0.5

        ox = int(round(cell_size * 0.5 - cx_in_rs))
        ox = max(2, min(cell_size - nw - 2, ox))

        cell_canvas = Image.new("RGBA", (cell_size, cell_size), (0, 0, 0, 0))
        cell_canvas.alpha_composite(rs, (ox, oy))
        strip.paste(cell_canvas, (idx * cell_size, 0))

    out_path = OUT_DIR / f"{strip_key}.png"
    strip.save(out_path, format="PNG", optimize=True)
    pivot_y = round(float(foot_bottom_pad) / float(cell_size), 2)
    write_unity_png_meta(
        out_path,
        used_guids,
        asset_kind="anim_strip",
        ppu=meta_ppu,
        pivot=(0.5, pivot_y),
        weixin_max_size=2048,
    )
    print(f"[AnimStrip] Saved {out_path.name} -> {strip.size} ({len(trimmed)}f, scale={safe_scale:.3f}, foot_pad={foot_bottom_pad}px)")
    return strip


def render_strips_qa_sheet(
    saved_strips: Sequence[Tuple[str, Image.Image, int]],
    out_path: Path,
) -> None:
    if not saved_strips:
        return
    max_w = 1120
    row_heights: List[int] = []
    scaled_rows: List[Tuple[str, Image.Image, int, float]] = []
    for key, strip_im, foot_pad in saved_strips:
        sc = min(1.0, 184.0 / float(strip_im.height), float(max_w - 32) / float(strip_im.width))
        dw = int(round(strip_im.width * sc))
        dh = int(round(strip_im.height * sc))
        thumb = strip_im.resize((dw, dh), Image.Resampling.LANCZOS) if sc < 0.999 else strip_im
        scaled_rows.append((key, thumb, foot_pad, sc))
        row_heights.append(dh + 32)

    board = Image.new("RGBA", (max_w, sum(row_heights) + 16), (26, 30, 38, 255))
    draw = ImageDraw.Draw(board)

    y_cur = 8
    for idx, (key, thumb, foot_pad, sc) in enumerate(scaled_rows):
        dw, dh = thumb.size
        orig_w, orig_h = saved_strips[idx][1].size
        n_frames = max(1, orig_w // orig_h)
        bg_col = (38, 48, 44, 255) if (idx % 2 == 0) else (196, 166, 118, 255)
        draw.rectangle([14, y_cur + 20, 14 + dw, y_cur + 20 + dh], fill=bg_col)

        cell_dw = dw / float(n_frames)
        for fi in range(n_frames + 1):
            gx = int(round(14 + fi * cell_dw))
            draw.line([(gx, y_cur + 20), (gx, y_cur + 20 + dh)], fill=(96, 112, 128, 190), width=1)
        base_dy = y_cur + 20 + int(round((orig_h - foot_pad) * sc))
        draw.line([(14, base_dy), (14 + dw, base_dy)], fill=(255, 72, 72, 225), width=2)

        board.alpha_composite(thumb, (14, y_cur + 20))
        draw.text(
            (16, y_cur + 2),
            f"{key}.png  ({orig_w}x{orig_h}, {n_frames} frames, foot_pad={foot_pad}px)",
            fill=(235, 242, 255, 255),
        )
        y_cur += dh + 32

    out_path.parent.mkdir(parents=True, exist_ok=True)
    board.convert("RGB").save(out_path, format="JPEG", quality=93)
    print(f"[QA] Strip baseline & halo contact sheet -> {out_path}")


# ---------------------------------------------------------------------------
# 6. Full 9-Sheet Production Pipeline
# ---------------------------------------------------------------------------

def run_all_9_sheets(
    sheet_busts: Path,
    sheet_leimeng: Path,
    sheet_tieshu: Path,
    sheet_walkers: Path,
    sheet_work1: Path,
    sheet_work2: Path,
    sheet_zombies: Path,
    sheet_boss: Path,
    sheet_animals: Path,
    qa_dir: Path,
) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    qa_dir.mkdir(parents=True, exist_ok=True)
    used_guids = collect_existing_guids(ASSETS_ROOT)
    saved_strips: List[Tuple[str, Image.Image, int]] = []

    # 1. Call A1 — Hero Bust Portraits (`bust_leimeng.png`, `bust_tieshu.png`)
    # Strictly gate hole punching to y >= 0.42*h so big white comic eyes & teeth in top 40% stay 100% opaque!
    bust_frames = extract_grid_frames_by_components(
        sheet_busts, rows=1, cols=2, punch_enclosed_holes_min_px=80, hole_y_min_ratio=0.42
    )
    bust_leimeng = save_bust_portrait(bust_frames[(0, 0)], "bust_leimeng", used_guids, target_h=400)
    bust_tieshu = save_bust_portrait(bust_frames[(0, 1)], "bust_tieshu", used_guids, target_h=400)

    # 2. Call A2 — `leimeng_puppet_atlas.png` (`1024x1024`)
    build_leimeng_puppet(sheet_leimeng, used_guids, qa_dir, bust_ref=bust_leimeng)

    # 3. Call A3 — `tieshu_puppet_atlas.png` (`1024x1024`)
    build_tieshu_puppet(sheet_tieshu, used_guids, qa_dir, bust_ref=bust_tieshu)

    # 4. Call B1 — Road Walking Civilians (`4 cols x 3 rows`, facing +X right)
    w_frames = extract_grid_frames_by_components(sheet_walkers, rows=3, cols=4, punch_enclosed_holes_min_px=45)
    for r, (key, th) in enumerate([
        ("anim_walker_0", 210),
        ("anim_walker_1", 206),
        ("anim_walker_2", 206),
    ]):
        st = build_horizontal_strip(
            [w_frames[(r, c)] for c in range(4)],
            strip_key=key,
            used_guids=used_guids,
            cell_size=256,
            target_ref_height=th,
            foot_bottom_pad=20,
            flip_x=False,
        )
        saved_strips.append((key, st, 20))

    # 5. Call C1 — Stationed Workers 1 (`3 cols x 3 rows`, facing +X right)
    wk1_frames = extract_grid_frames_by_components(sheet_work1, rows=3, cols=3, punch_enclosed_holes_min_px=80)
    for r, (key, th) in enumerate([
        ("anim_work_fishing", 202),
        ("anim_work_cooking", 206),
        ("anim_work_farming", 206),
    ]):
        st = build_horizontal_strip(
            [wk1_frames[(r, c)] for c in range(3)],
            strip_key=key,
            used_guids=used_guids,
            cell_size=256,
            target_ref_height=th,
            foot_bottom_pad=20,
            flip_x=False,
        )
        saved_strips.append((key, st, 20))

    # 6. Call C2 — Stationed Workers 2 (`3 cols x 3 rows`, facing +X right)
    wk2_frames = extract_grid_frames_by_components(sheet_work2, rows=3, cols=3, punch_enclosed_holes_min_px=65)
    for r, (key, th) in enumerate([
        ("anim_work_sawing",  204),
        ("anim_work_forging", 212),
        ("anim_work_herbs",   202),
    ]):
        st = build_horizontal_strip(
            [wk2_frames[(r, c)] for c in range(3)],
            strip_key=key,
            used_guids=used_guids,
            cell_size=256,
            target_ref_height=th,
            foot_bottom_pad=20,
            flip_x=False,
        )
        saved_strips.append((key, st, 20))

    # 7. Call D1 — Zombies (`4 cols x 3 rows`, mirrored to face -X left toward heroes!)
    z_frames = extract_grid_frames_by_components(sheet_zombies, rows=3, cols=4, punch_enclosed_holes_min_px=75)
    for r, (key, th) in enumerate([
        ("anim_zom_shambler", 198),
        ("anim_zom_armored",  204),
        ("anim_zom_runner",   192),
    ]):
        st = build_horizontal_strip(
            [z_frames[(r, c)] for c in range(4)],
            strip_key=key,
            used_guids=used_guids,
            cell_size=256,
            target_ref_height=th,
            foot_bottom_pad=20,
            flip_x=True,
        )
        saved_strips.append((key, st, 20))

    # 8. Call D2 — Giant Zombie Boss (`2x2` -> `[ (0,0), (0,1), (1,0), (1,1) ]` in `1536x384`, mirrored -X left!)
    boss_frames = extract_grid_frames_by_components(sheet_boss, rows=2, cols=2, punch_enclosed_holes_min_px=90)
    boss_ordered = [boss_frames[(0, 0)], boss_frames[(0, 1)], boss_frames[(1, 0)], boss_frames[(1, 1)]]
    st_boss = build_horizontal_strip(
        boss_ordered,
        strip_key="anim_zom_boss",
        used_guids=used_guids,
        cell_size=384,
        target_ref_height=292,
        foot_bottom_pad=30,
        meta_ppu=256,
        flip_x=True,
    )
    saved_strips.append(("anim_zom_boss", st_boss, 30))

    # 9. Call D3 — Forest & Ranch Animals (`3x3`, mirrored to face -X left matching boar/deer & ForestSliceView!)
    # Note: punch_enclosed_holes_min_px=0 for rabbit/boar preserves 100% of white rabbit fur & boar tusks,
    # while punch=45 on deer row punches enclosed white gaps between thin deer legs!
    ani_no_punch = extract_grid_frames_by_components(sheet_animals, rows=3, cols=3, punch_enclosed_holes_min_px=0)
    ani_leg_punch = extract_grid_frames_by_components(sheet_animals, rows=3, cols=3, punch_enclosed_holes_min_px=50)
    for r, (key, th, src_map) in enumerate([
        ("anim_rabbit", 122, ani_no_punch),
        ("anim_boar",   148, ani_leg_punch),
        ("anim_deer",   192, ani_leg_punch),
    ]):
        st = build_horizontal_strip(
            [src_map[(r, c)] for c in range(3)],
            strip_key=key,
            used_guids=used_guids,
            cell_size=256,
            target_ref_height=th,
            foot_bottom_pad=20,
            flip_x=True,
        )
        saved_strips.append((key, st, 20))

    render_strips_qa_sheet(saved_strips, qa_dir / "living_world_strips_qa.jpg")


def main() -> None:
    parser = argparse.ArgumentParser(description="Slice & assemble all 20 Living World skeletal & flipbook assets.")
    parser.add_argument(
        "--sheet-busts",
        type=Path,
        default=COORDINATOR_BRAIN / "leimeng_tieshu_portrait_sheet_v3_1795563975976.jpg",
    )
    parser.add_argument(
        "--sheet-leimeng",
        type=Path,
        default=COORDINATOR_BRAIN / "leimeng_puppet_sheet_v3_1795564083901.jpg",
    )
    parser.add_argument(
        "--sheet-tieshu",
        type=Path,
        default=COORDINATOR_BRAIN / "tieshu_puppet_sheet_1795564143868.jpg",
    )
    parser.add_argument(
        "--sheet-walkers",
        type=Path,
        default=COORDINATOR_BRAIN / "villagers_walk_cycle_sheet_v3_1795564301267.jpg",
    )
    parser.add_argument(
        "--sheet-work1",
        type=Path,
        default=COORDINATOR_BRAIN / "camp_worker_sprites_3x3_v2_1795564393476.jpg",
    )
    parser.add_argument(
        "--sheet-work2",
        type=Path,
        default=COORDINATOR_BRAIN / "camp_worker_spritesheet_3x3_v2_1795564492555.jpg",
    )
    parser.add_argument(
        "--sheet-zombies",
        type=Path,
        default=COORDINATOR_BRAIN / "zombie_animation_spritesheet_4x3_final_1795564596033.jpg",
    )
    parser.add_argument(
        "--sheet-boss",
        type=Path,
        default=COORDINATOR_BRAIN / "boss_zombie_spritesheet_1795564663851.jpg",
    )
    parser.add_argument(
        "--sheet-animals",
        type=Path,
        default=COORDINATOR_BRAIN / "forest_animals_spritesheet_3x3_1795564754487.jpg",
    )
    parser.add_argument(
        "--qa-dir",
        type=Path,
        default=Path("/Users/rosa/.gemini/antigravity/brain/d62bb038-37cd-4ae2-9d8a-f3241831c87e/scratch"),
    )
    args = parser.parse_args()

    run_all_9_sheets(
        sheet_busts=args.sheet_busts,
        sheet_leimeng=args.sheet_leimeng,
        sheet_tieshu=args.sheet_tieshu,
        sheet_walkers=args.sheet_walkers,
        sheet_work1=args.sheet_work1,
        sheet_work2=args.sheet_work2,
        sheet_zombies=args.sheet_zombies,
        sheet_boss=args.sheet_boss,
        sheet_animals=args.sheet_animals,
        qa_dir=args.qa_dir,
    )


if __name__ == "__main__":
    main()
