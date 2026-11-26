#!/usr/bin/python3
"""
tools/slice_phase2_heroes_art.py

Phase-2 9-Hero Matrix & Water-World Full-Body Art Slicer, Bust/Jigsaw Composer &
1024x1024 9-Part Skeletal Puppet Atlas Builder.

Produces 18 PNG + collision-free `.png.meta` assets in `Assets/Mori/ForestSlice/Resources/ForestArt/`:
  1. 9 Full-Body Standing Hero Sprites (`fullbody_<id>.png`, target_h=560, pad=8 -> H=576):
     `fullbody_dachui`, `fullbody_xiaotang`, `fullbody_leimeng`,
     `fullbody_linyan`, `fullbody_tieshu`, `fullbody_laochentou`,
     `fullbody_asa`, `fullbody_qinghe`, `fullbody_zhoudr`
  2. 3 New Hero Close-Up Busts (`bust_asa.png`, `bust_qinghe.png`, `bust_zhoudr.png`, target_h=400, pad=4)
  3. 3 New Hero Jigsaw Shards (`ico_shard_asa.png`, `ico_shard_qinghe.png`, `ico_shard_zhoudr.png`, 192x192)
  4. 3 New 1024x1024 (3x3x320) Skeletal Puppet Atlases:
     `asa_puppet_atlas.png`, `qinghe_puppet_atlas.png`, `zhoudr_puppet_atlas.png`

STRICT RULE:
- Processes ONLY genuine built-in `image-generator` raw sheets (`FB1..FB3`, `P1..P3`).
- NEVER calls external APIs (no Vertex AI) and NEVER draws fake procedural characters.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Set, Tuple

USER_SITE = "/Users/rosa/Library/Python/3.9/lib/python/site-packages"
if sys.version_info[:2] != (3, 9) and os.path.isfile("/usr/bin/python3"):
    os.execv("/usr/bin/python3", ["/usr/bin/python3", __file__, *sys.argv[1:]])
if os.path.isdir(USER_SITE) and USER_SITE not in sys.path:
    sys.path.append(USER_SITE)

import numpy as np
from PIL import Image, ImageDraw, ImageOps
from scipy import ndimage

REPO_ROOT = Path(__file__).resolve().parents[1]
ASSETS_ROOT = REPO_ROOT / "Assets"
OUT_DIR = ASSETS_ROOT / "Mori" / "ForestSlice" / "Resources" / "ForestArt"
PUPPET_CS_PATH = ASSETS_ROOT / "Mori" / "ForestSlice" / "Scripts" / "ForestHeroPuppet.cs"
DEFAULT_SCRATCH_DIR = Path(
    "/Users/rosa/.gemini/antigravity/brain/a09919f1-b326-400a-a1c1-804dcabb2118/scratch"
)

CELL_PUPPET = 320
PPU_PUPPET = 256.0
ICON_SIZE = 192

# Exact Unity Slice UV pivots from ForestHeroPuppet.LoadOrGetAtlas (x: left->right, y: bottom->top)
UV_HEAD = (0.48, 0.10)
UV_ACC = (0.88, 0.58)
UV_TORSO = (0.52, 0.12)
UV_ARM = (0.50, 0.84)
UV_LEG = (0.50, 0.86)
UV_TOOL = (0.46, 0.16)

FB1_HEROES = ("dachui", "xiaotang", "leimeng")
FB2_HEROES = ("linyan", "tieshu", "laochentou")
FB3_HEROES = ("asa", "qinghe", "zhoudr")
ALL_9_HEROES = FB1_HEROES + FB2_HEROES + FB3_HEROES


# ---------------------------------------------------------------------------
# 1. Collision-Free Unity `.png.meta` Management (ASTC_4x4 WeixinMiniGame)
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


def read_existing_hex_guid(meta_path: Path) -> Optional[str]:
    if not meta_path.is_file():
        return None
    try:
        txt = meta_path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return None
    m = re.search(r"^guid:\s*(\S+)", txt, re.MULTILINE)
    if not m:
        return None
    g = m.group(1).strip()
    if len(g) == 32 and all(ch in "0123456789abcdef" for ch in g):
        return g
    return None


def make_unique_hex_guid(key: str, used: Set[str]) -> str:
    salt = 0
    while True:
        seed = f"mori-rosa:phase2-heroes-art:{key}:{salt}".encode("utf-8")
        cand = hashlib.md5(seed).hexdigest().lower()
        if len(cand) == 32 and cand not in used:
            used.add(cand)
            return cand
        salt += 1


def write_unity_png_meta(
    png_path: Path,
    used_guids: Set[str],
    asset_kind: str,
) -> str:
    """
    Writes a Unity TextureImporter `.png.meta` with a collision-free 32-hex lowercase GUID
    and WeixinMiniGame ASTC_4x4 (`textureFormat: 48`) override:
      - `fullbody`: Sprite (textureType=8), pivot=(0.5, 0.5), PPU=256, wx_max=1024, ASTC_4x4
      - `bust`:     Sprite (textureType=8), pivot=(0.5, 0.5), PPU=256, wx_max=512,  ASTC_4x4
      - `shard`:    Sprite (textureType=8), pivot=(0.5, 0.5), PPU=100, wx_max=512,  ASTC_4x4
      - `puppet_atlas`: Default (textureType=0), PPU=100,              wx_max=1024, ASTC_4x4
    """
    meta_path = Path(str(png_path) + ".meta")
    existing = read_existing_hex_guid(meta_path)
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
        default_max = 2048
        wx_max = 1024
    elif asset_kind == "fullbody":
        tex_type = 8
        sprite_mesh_type = 0
        alignment = 9
        px, py = 0.5, 0.5
        meta_ppu = 256
        default_max = 2048
        wx_max = 1024
    elif asset_kind == "bust":
        tex_type = 8
        sprite_mesh_type = 0
        alignment = 9
        px, py = 0.5, 0.5
        meta_ppu = 256
        default_max = 2048
        wx_max = 512
    elif asset_kind == "shard":
        tex_type = 8
        sprite_mesh_type = 0
        alignment = 0
        px, py = 0.5, 0.5
        meta_ppu = 100
        default_max = 512
        wx_max = 512
    else:
        raise ValueError(f"Unknown asset_kind: {asset_kind}")

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
  maxTextureSize: {default_max}
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
    maxTextureSize: {default_max}
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
    maxTextureSize: {default_max}
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
    maxTextureSize: {default_max}
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
# 2. Exterior White Cutout, Surgical Background Hole Punching & Ink Defringing
# ---------------------------------------------------------------------------

def tight_crop_rgba(im: Image.Image, alpha_thresh: int = 16) -> Image.Image:
    arr = np.asarray(im)
    ys, xs = np.where(arr[:, :, 3] > alpha_thresh)
    if len(xs) == 0:
        return im
    return im.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def find_vertical_gutter_splits(sheet_rgb: np.ndarray, cols: int = 3, search_ratio: float = 0.09) -> List[int]:
    """Finds pure-white vertical column split lines across a `1 x cols` full-body sheet."""
    h, w, _ = sheet_rgb.shape
    min_c = sheet_rgb.min(axis=2)
    sat = sheet_rgb.max(axis=2).astype(np.int16) - min_c.astype(np.int16)
    non_white = (min_c < 238) | (sat > 20)
    proj = non_white.sum(axis=0).astype(np.float64)
    kernel = np.array([0.12, 0.20, 0.36, 0.20, 0.12])
    smooth = np.convolve(proj, kernel, mode="same")

    splits = [0]
    win = max(10, int(round(w * search_ratio)))
    for k in range(1, cols):
        ideal = int(round(k * w / float(cols)))
        lo = max(1, ideal - win)
        hi = min(w - 1, ideal + win)
        seg = smooth[lo : hi + 1]
        min_v = seg.min()
        cands = np.where(seg <= min_v + 1.5)[0] + lo
        best = int(cands[np.argmin(np.abs(cands - ideal))])
        splits.append(best)
    splits.append(w)
    return splits


@dataclass
class HolePunchRule:
    """Controls enclosed pure-white background loop keying while protecting interior comic whites."""
    min_area_px: int = 18
    y_min_ratio: float = 0.0   # Use precise `protect_uv_boxes` for face eyes/teeth so shoulder-weapon gaps punch cleanly!
    white_min: float = 245.0
    sat_max: float = 12.0
    max_std: float = 6.0
    # Protected normalized boxes `(x0, y0, x1, y1)` in `[0..1]` of tight bbox (face eyes/teeth/bandages, chest sign arrow, lab coat)
    protect_uv_boxes: List[Tuple[float, float, float, float]] = field(default_factory=list)
    # Optional explicit normalized seed points `(ux, uy)` in `[0..1]` of tight bbox to force-punch enclosed bg loops
    force_seed_uvs: List[Tuple[float, float]] = field(default_factory=list)


DEFAULT_HERO_HOLE_RULES: Dict[str, HolePunchRule] = {
    # Protect Dachui's face (forehead X-bandage, eyes, teeth) + chest road-sign white arrow; punch crowbar/arm/leg holes!
    "dachui": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.30, 0.02, 0.67, 0.22), (0.43, 0.29, 0.68, 0.43)],
    ),
    # Protect Xiaotang's full head/eyes/snaggletooth across full width; punch neck-hair & arm-wrench & leg holes!
    "xiaotang": HolePunchRule(
        min_area_px=16,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.29)],
    ),
    # Protect Leimeng's eyes, nose band-aid & grin; punch both under-axe shoulder/neck gaps & leg holes!
    "leimeng": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.32, 0.06, 0.65, 0.24)],
    ),
    # Protect Linyan's full head (glasses/eyes/band-aid); punch crossbow stirrup/string & arm/leg loops!
    "linyan": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.26)],
    ),
    # Protect Tieshu's full head (eyes & toothpick); punch trigger guard & leg loops!
    "tieshu": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.25)],
    ),
    # Protect Laochentou's full head (headband & both eyes) + pale tank-top collar; punch teapot handle & arm-hip & leg loops!
    "laochentou": HolePunchRule(
        min_area_px=12,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.25), (0.30, 0.24, 0.72, 0.42)],
        force_seed_uvs=[(0.885, 0.50)],
    ),
    "asa": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.26)],
    ),
    "qinghe": HolePunchRule(
        min_area_px=18,
        protect_uv_boxes=[(0.15, 0.02, 0.92, 0.26)],
    ),
    # Dr. Zhou: protect head/glasses/hair (`ux >= 0.35`) + torso coat panels while punching monocle-cord crescent & arm/leg/cable loops
    "zhoudr": HolePunchRule(
        min_area_px=16,
        white_min=248.0,
        sat_max=8.5,
        max_std=4.0,
        protect_uv_boxes=[(0.35, 0.02, 0.92, 0.26)],
        force_seed_uvs=[(0.295, 0.235)],
    ),
}


def cutout_comic_rgba_array(
    sub_rgb: np.ndarray,
    white_min: float = 236.0,
    sat_max: float = 22.0,
    hole_rule: Optional[HolePunchRule] = None,
    min_island_px: int = 24,
    keep_only_largest: bool = False,
) -> Tuple[np.ndarray, np.ndarray]:
    """
    8-connected exterior pure-white flood-fill + smart enclosed background loop punching
    + dark comic ink outline halo decontamination.
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
    s_lum = sub_f.mean(axis=2)

    fg_init = ~bg_mask
    fys, fxs = np.where(fg_init)
    if hole_rule is not None and len(fxs) > 0:
        bx0, bx1 = int(fxs.min()), int(fxs.max()) + 1
        by0, by1 = int(fys.min()), int(fys.max()) + 1
        bw = max(1, bx1 - bx0)
        bh = max(1, by1 - by0)
        y_gate = by0 + int(round(bh * hole_rule.y_min_ratio))

        pure_white = (s_min >= hole_rule.white_min) & (s_sat <= hole_rule.sat_max) & (~bg_mask)
        hl_lbl, n_hl = ndimage.label(pure_white)

        for lid in range(1, n_hl + 1):
            reg = (hl_lbl == lid)
            ys, xs = np.where(reg)
            if len(ys) < hole_rule.min_area_px:
                continue
            if int(ys.min()) < y_gate:
                continue

            # Check normalized center against protected boxes
            ux = (float(xs.mean()) - bx0) / float(bw)
            uy = (float(ys.mean()) - by0) / float(bh)
            in_protected = any(
                (px0 <= ux <= px1 and py0 <= uy <= py1)
                for (px0, py0, px1, py1) in hole_rule.protect_uv_boxes
            )
            if in_protected:
                continue

            # Verify flat pure-white background paper (low luminance std) AND dark comic ink boundary
            lum_std = float(np.std(s_lum[reg]))
            if lum_std > hole_rule.max_std:
                continue

            dil = ndimage.binary_dilation(reg, iterations=2) & (~reg)
            if dil.sum() > 0:
                border_dark_frac = float((s_min[dil] < 135.0).mean())
                if border_dark_frac < 0.45:
                    continue

            expanded = ndimage.binary_dilation(reg, iterations=1) & (s_min >= 212.0) & (s_sat <= 24.0)
            bg_mask |= expanded

        # Optional explicit seed points for tricky slightly-shaded enclosed background gaps
        if hole_rule.force_seed_uvs:
            seed_cand = (s_min >= 234.0) & (s_sat <= 22.0) & (~bg_mask)
            sc_lbl, _ = ndimage.label(seed_cand)
            for sux, suy in hole_rule.force_seed_uvs:
                sx = min(w - 1, max(0, int(round(bx0 + sux * bw))))
                sy = min(h - 1, max(0, int(round(by0 + suy * bh))))
                slid = int(sc_lbl[sy, sx])
                if slid > 0:
                    bg_mask |= (sc_lbl == slid)

    fg = ~bg_mask
    comp, n_comp = ndimage.label(fg, structure=struct8)
    if n_comp > 0:
        sizes = ndimage.sum(fg, comp, range(1, n_comp + 1))
        max_sz = float(max(sizes)) if len(sizes) > 0 else 0.0
        if keep_only_largest:
            best_cid = int(np.argmax(sizes)) + 1
            fg = (comp == best_cid)
        else:
            for cid, sz in enumerate(sizes, start=1):
                if sz < min_island_px:
                    fg[comp == cid] = False
                    continue
                # Drop tiny neighbor-cell bleed touching the crop boundary
                if sz < max_sz * 0.025:
                    yc, xc = np.where(comp == cid)
                    if yc.min() <= 1 or yc.max() >= h - 2 or xc.min() <= 1 or xc.max() >= w - 2:
                        fg[comp == cid] = False

    dist_in = ndimage.distance_transform_edt(fg)
    alpha = np.where(fg, 1.0, 0.0).astype(np.float32)

    # Anti-aliased comic ink edge + white halo decontamination
    fringe = fg & (dist_in <= 2.2) & (s_min > 192.0) & (s_sat < 28.0)
    whiteness = np.clip((s_min - 174.0) / 76.0, 0.0, 1.0)
    alpha[fringe] = np.minimum(alpha[fringe], 1.0 - whiteness[fringe] * 0.92)

    edge_ring = fg & (dist_in <= 1.7) & (s_min > 176.0) & (s_sat < 32.0)
    sub_f[edge_ring, 0] = sub_f[edge_ring, 0] * 0.26 + 22.0 * 0.74
    sub_f[edge_ring, 1] = sub_f[edge_ring, 1] * 0.26 + 24.0 * 0.74
    sub_f[edge_ring, 2] = sub_f[edge_ring, 2] * 0.26 + 34.0 * 0.74

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.clip(sub_f, 0, 255).astype(np.uint8)
    rgba[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    return rgba, fg


def cutout_comic_sprite(
    cell_rgb: np.ndarray,
    hole_rule: Optional[HolePunchRule] = None,
    keep_only_largest: bool = False,
    min_island_px: int = 24,
    pad_out: int = 2,
) -> Image.Image:
    rgba, _ = cutout_comic_rgba_array(
        cell_rgb,
        hole_rule=hole_rule,
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


def extract_fullbody_trio(
    sheet_path: Path,
    hero_ids: Tuple[str, str, str],
    hole_rules: Optional[Dict[str, HolePunchRule]] = None,
    custom_x_splits: Optional[List[int]] = None,
) -> Dict[str, Image.Image]:
    """
    Slices a 3-hero horizontal full-body sheet (`FB1`, `FB2`, or `FB3`) cleanly:
    First attempts unclipped 3-major connected-component extraction across the full sheet;
    if two adjacent heroes' weapons touch/bridge, falls back cleanly to pure-white vertical
    gutter splits!
    """
    if not sheet_path.is_file():
        raise FileNotFoundError(f"Full-body sheet not found: {sheet_path}")

    sheet_rgb = np.array(Image.open(sheet_path).convert("RGB"))
    h, w, _ = sheet_rgb.shape
    rules = hole_rules or DEFAULT_HERO_HOLE_RULES

    if custom_x_splits is not None:
        x_splits = custom_x_splits
    else:
        # Check if full-sheet foreground separates cleanly into 3 distinct columns
        _, fg_all = cutout_comic_rgba_array(sheet_rgb, hole_rule=None, min_island_px=30, keep_only_largest=False)
        clbl, n_comp = ndimage.label(fg_all, structure=np.ones((3, 3), dtype=np.int32))
        comps = []
        for cid in range(1, n_comp + 1):
            ys, xs = np.where(clbl == cid)
            if len(xs) >= 30:
                comps.append((len(xs), cid, int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1, float(xs.mean())))
        comps.sort(key=lambda t: t[0], reverse=True)

        # Verify top 3 components each span < 48% of sheet width (meaning no two heroes merged!)
        if len(comps) >= 3 and all((c[4] - c[2]) < w * 0.48 for c in comps[:3]):
            majors = sorted(comps[:3], key=lambda t: t[6])
            extras = comps[3:]
            owned: Dict[int, List[int]] = {i: [majors[i][1]] for i in range(3)}
            for ex in extras:
                _, ecid, ex0, ey0, ex1, ey1, _ = ex
                # Attach detached spark/weapon-tip islands to nearest hero bbox
                def _bdist(idx: int) -> float:
                    _, _, mx0, my0, mx1, my1, _ = majors[idx]
                    return math.hypot(max(0, ex0 - mx1, mx0 - ex1), max(0, ey0 - my1, my0 - ey1))
                best_i = min((0, 1, 2), key=_bdist)
                if _bdist(best_i) < w * 0.08:
                    owned[best_i].append(ecid)

            out_map: Dict[str, Image.Image] = {}
            for idx, hid in enumerate(hero_ids):
                mask = np.isin(clbl, owned[idx])
                ys, xs = np.where(mask)
                x0, x1 = max(0, int(xs.min()) - 6), min(w, int(xs.max()) + 7)
                y0, y1 = max(0, int(ys.min()) - 6), min(h, int(ys.max()) + 7)
                # Build isolated pure-white sub-canvas containing ONLY this hero's components, then run per-hero hole rule!
                isolated_rgb = np.full((y1 - y0, x1 - x0, 3), 255, dtype=np.uint8)
                sub_src = sheet_rgb[y0:y1, x0:x1]
                # Dilate mask by 3px so original anti-aliased edge pixels enter cutout_comic_sprite naturally
                sub_mask_dil = ndimage.binary_dilation(mask[y0:y1, x0:x1], iterations=3)
                isolated_rgb[sub_mask_dil] = sub_src[sub_mask_dil]
                out_map[hid] = cutout_comic_sprite(isolated_rgb, hole_rule=rules.get(hid), min_island_px=24)
            return out_map

        x_splits = find_vertical_gutter_splits(sheet_rgb, cols=3)

    out_map = {}
    for idx, hid in enumerate(hero_ids):
        cell = sheet_rgb[:, x_splits[idx] : x_splits[idx + 1]].copy()
        out_map[hid] = cutout_comic_sprite(cell, hole_rule=rules.get(hid), min_island_px=24)
    return out_map


def save_fullbody_sprite(
    raw_rgba: Image.Image,
    hero_id: str,
    used_guids: Set[str],
    target_h: int = 560,
    pad_px: int = 8,
) -> Image.Image:
    """Scales tight-cropped full-body hero to `target_h = 560` with `pad_px = 8` transparent margin."""
    im = tight_crop_rgba(raw_rgba, alpha_thresh=16)
    scale = float(target_h) / max(1, im.height)
    nw = max(8, int(round(im.width * scale)))
    resized = im.resize((nw, target_h), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (nw + pad_px * 2, target_h + pad_px * 2), (0, 0, 0, 0))
    canvas.alpha_composite(resized, (pad_px, pad_px))

    out_path = OUT_DIR / f"fullbody_{hero_id}.png"
    canvas.save(out_path, format="PNG", optimize=True)
    write_unity_png_meta(out_path, used_guids, asset_kind="fullbody")
    print(f"[FullBody] Saved {out_path.name} -> {canvas.size}")
    return canvas


# ---------------------------------------------------------------------------
# 3. Close-Up Bust Composer (`bust_asa`, `bust_qinghe`, `bust_zhoudr`) & Shards
# ---------------------------------------------------------------------------

def crop_bust_from_fullbody(
    fullbody_rgba: Image.Image,
    crop_top_ratio: float = 0.0,
    crop_bottom_ratio: float = 0.55,
    crop_x0_ratio: float = 0.0,
    crop_x1_ratio: float = 1.0,
    chest_arc_rise_ratio: float = 0.065,
) -> Image.Image:
    """
    Crops the upper head-and-shoulders (`top ~52-58%`) of a full-body hero sprite and finishes
    the lower chest edge with a subtle convex comic bust curve + 2.5px dark comic ink contour
    matching `bust_linyan.png` & `bust_leimeng.png`!
    """
    tight = tight_crop_rgba(fullbody_rgba, alpha_thresh=16)
    tw, th = tight.size
    x0 = max(0, int(round(tw * crop_x0_ratio)))
    x1 = min(tw, int(round(tw * crop_x1_ratio)))
    y0 = max(0, int(round(th * crop_top_ratio)))
    y1 = min(th, int(round(th * crop_bottom_ratio)))

    sub = np.array(tight.crop((x0, y0, x1, y1))).copy()
    sh, sw, _ = sub.shape

    # Find horizontal bounds of opaque body near chest cut (`bottom 25%` of crop)
    lower_band = sub[int(sh * 0.72) :, :, 3] > 32
    _, lxs = np.where(lower_band)
    if len(lxs) > 10:
        cx = 0.5 * (float(lxs.min()) + float(lxs.max()))
        half_w = max(12.0, 0.52 * (float(lxs.max()) - float(lxs.min())))
    else:
        cx = sw * 0.5
        half_w = max(12.0, sw * 0.45)

    rise_px = sh * chest_arc_rise_ratio
    ink_rgb = np.array([23.0, 26.0, 38.0], dtype=np.float32)

    for x in range(sw):
        nx = (x - cx) / half_w
        y_cut = (sh - 2.0) - rise_px * min(1.35, nx * nx)
        y_cut_i = int(round(y_cut))
        if y_cut_i < sh:
            # Clear below curve with 1.5px anti-aliased feather + 2.5px comic ink bottom seal
            for y in range(max(0, y_cut_i - 3), sh):
                a_orig = float(sub[y, x, 3])
                if a_orig <= 4.0:
                    continue
                d = y - y_cut  # <= 0 inside, > 0 outside
                if d >= 1.2:
                    sub[y, x, 3] = 0
                elif d > -0.4:
                    fade = max(0.0, 1.0 - (d + 0.4) / 1.6)
                    sub[y, x, :3] = np.clip(ink_rgb, 0, 255).astype(np.uint8)
                    sub[y, x, 3] = int(round(a_orig * fade))
                elif d >= -2.8:
                    # Subtle comic ink rim along the cut line
                    blend = max(0.0, 1.0 - (-0.4 - d) / 2.4) * 0.85
                    cur = sub[y, x, :3].astype(np.float32)
                    sub[y, x, :3] = np.clip(cur * (1.0 - blend) + ink_rgb * blend, 0, 255).astype(np.uint8)

    # Drop any tiny severed weapon-shaft islands left stranded at the bottom corners after chest cut
    fg = sub[:, :, 3] > 16
    clbl, n_comp = ndimage.label(fg, structure=np.ones((3, 3), dtype=np.int32))
    if n_comp > 1:
        sizes = ndimage.sum(fg, clbl, range(1, n_comp + 1))
        main_cid = int(np.argmax(sizes)) + 1
        for cid, sz in enumerate(sizes, start=1):
            if cid == main_cid:
                continue
            ys_c, _ = np.where(clbl == cid)
            # If small island sits near the bottom cut (`y_mean > 0.65 * sh`), drop it cleanly
            if sz < max(sizes) * 0.08 and float(ys_c.mean()) > sh * 0.60:
                sub[clbl == cid, 3] = 0

    return tight_crop_rgba(Image.fromarray(sub, "RGBA"), alpha_thresh=16)


def save_bust_portrait(
    raw_im: Image.Image,
    out_key: str,
    used_guids: Set[str],
    target_h: int = 400,
    pad_px: int = 4,
) -> Image.Image:
    im = tight_crop_rgba(raw_im, alpha_thresh=16)
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


def _build_interlocking_jigsaw_mask(ss_size: int, tilt_deg: float = -14.0) -> np.ndarray:
    """Exact 1:1 interlocking -14 deg 3-tab / 1-socket jigsaw puzzle mask from `slice_hero_economy_art.py`."""
    yy, xx = np.mgrid[0:ss_size, 0:ss_size].astype(np.float32)
    scale = ss_size / 192.0
    cx = ss_size * 0.5 - 3.5 * scale
    cy = ss_size * 0.5 + 2.0 * scale

    dx = xx - cx
    dy = yy - cy
    theta = math.radians(-tilt_deg)
    cos_t = math.cos(theta)
    sin_t = math.sin(theta)
    lx = dx * cos_t - dy * sin_t
    ly = dx * sin_t + dy * cos_t

    half = 47.0 * scale
    corner_r = 7.5 * scale

    qx = np.abs(lx) - (half - corner_r)
    qy = np.abs(ly) - (half - corner_r)
    outside_d = np.hypot(np.maximum(qx, 0.0), np.maximum(qy, 0.0))
    inside_d = np.minimum(np.maximum(qx, qy), 0.0)
    sd_box = outside_d + inside_d - corner_r
    mask = sd_box <= 0.0

    knob_r = 14.2 * scale
    knob_offset = 10.8 * scale
    neck_half_w = 8.8 * scale

    def add_outward_tab(tx: np.ndarray, ty: np.ndarray) -> np.ndarray:
        head = (tx ** 2 + (ty - (half + knob_offset)) ** 2) <= (knob_r ** 2)
        neck = (np.abs(tx) <= neck_half_w) & (ty >= half - 4.0 * scale) & (ty <= half + knob_offset)
        return head | neck

    def make_inward_socket(tx: np.ndarray, ty: np.ndarray) -> np.ndarray:
        head = (tx ** 2 + (ty - (half - knob_offset * 0.92)) ** 2) <= ((knob_r * 0.92) ** 2)
        neck = (np.abs(tx) <= (neck_half_w * 0.90)) & (ty >= half - knob_offset * 0.92) & (ty <= half + 4.0 * scale)
        return head | neck

    mask |= add_outward_tab(lx, -ly)
    mask |= add_outward_tab(ly, lx)
    mask |= add_outward_tab(lx, ly)
    socket_left = make_inward_socket(ly, -lx)
    mask &= ~socket_left

    smooth_r = max(2, int(round(3.0 * scale)))
    yy_k, xx_k = np.ogrid[-smooth_r : smooth_r + 1, -smooth_r : smooth_r + 1]
    disk = (xx_k ** 2 + yy_k ** 2) <= (smooth_r ** 2)
    mask = ndimage.binary_closing(mask, structure=disk)
    mask = ndimage.binary_opening(mask, structure=disk)
    return mask


def auto_head_crop_box_for_shard(bust_im: Image.Image, side_px: int = 196, y_top: int = 8) -> Tuple[int, int, int, int]:
    """Finds a square crop box centered horizontally on the upper head mass (`y = 10..200`) of a `target_h=400` bust."""
    arr = np.asarray(bust_im)
    h, w, _ = arr.shape
    y_hi = min(h, y_top + side_px)
    head_fg = arr[y_top:y_hi, :, 3] > 32
    _, xs = np.where(head_fg)
    cx = float(np.median(xs)) if len(xs) > 20 else w * 0.5
    x0 = int(round(cx - side_px * 0.5))
    return (x0, y_top, x0 + side_px, y_top + side_px)


def compose_hero_shard_from_bust(
    bust_im: Image.Image,
    hero_id: str,
    used_guids: Set[str],
    crop_box: Optional[Tuple[int, int, int, int]] = None,
    backdrop_rgb: Tuple[int, int, int] = (225, 233, 250),
) -> Image.Image:
    """Exact 1:1 Water-World Jigsaw Hero Shard (`192x192` RGBA) matching `slice_hero_economy_art.py`."""
    ss = 4
    ss_size = ICON_SIZE * ss
    inner_mask = _build_interlocking_jigsaw_mask(ss_size, tilt_deg=-14.0)

    dist_out = ndimage.distance_transform_edt(~inner_mask)
    dist_in = ndimage.distance_transform_edt(inner_mask)

    white_px = 5.0 * ss
    ink_px = 4.0 * ss
    inner_line_px = 1.4 * ss

    white_zone = (~inner_mask) & (dist_out <= white_px)
    ink_main = (~inner_mask) & (dist_out <= (white_px + ink_px))
    extrude_base = ndimage.shift(ink_main.astype(np.uint8), shift=(6, -5), order=0) > 0
    full_silhouette = ink_main | extrude_base

    cx0, cy0, cx1, cy1 = crop_box if crop_box is not None else auto_head_crop_box_for_shard(bust_im)
    bw, bh = bust_im.size
    crop_w, crop_h = cx1 - cx0, cy1 - cy0
    crop_canvas = Image.new("RGBA", (crop_w, crop_h), (0, 0, 0, 0))
    sx0, sy0 = max(0, cx0), max(0, cy0)
    sx1, sy1 = min(bw, cx1), min(bh, cy1)
    if sx1 > sx0 and sy1 > sy0:
        crop_canvas.paste(bust_im.crop((sx0, sy0, sx1, sy1)), (sx0 - cx0, sy0 - cy0))

    target_port_sz = int(round(138 * ss))
    port_resized = crop_canvas.resize((target_port_sz, target_port_sz), Image.Resampling.LANCZOS)
    port_layer = Image.new("RGBA", (ss_size, ss_size), (0, 0, 0, 0))
    pox = (ss_size - target_port_sz) // 2 - int(2 * ss)
    poy = (ss_size - target_port_sz) // 2 + int(2 * ss)
    port_layer.alpha_composite(port_resized, (pox, poy))
    port_arr = np.array(port_layer, dtype=np.float32)

    canvas = np.zeros((ss_size, ss_size, 4), dtype=np.uint8)
    canvas[full_silhouette] = (23, 27, 46, 255)
    canvas[white_zone] = (255, 255, 255, 255)

    yy, xx = np.mgrid[0:ss_size, 0:ss_size].astype(np.float32)
    sheen = ((xx * 0.75 + yy * 0.65) < ss_size * 0.66)
    bg_r, bg_g, bg_b = backdrop_rgb
    tile_rgb = np.empty((ss_size, ss_size, 3), dtype=np.float32)
    tile_rgb[:, :, 0] = np.where(sheen, min(255, bg_r + 12), bg_r)
    tile_rgb[:, :, 1] = np.where(sheen, min(255, bg_g + 11), bg_g)
    tile_rgb[:, :, 2] = np.where(sheen, min(255, bg_b + 6), bg_b)

    pa = port_arr[:, :, 3:4] / 255.0
    comp_rgb = port_arr[:, :, :3] * pa + tile_rgb * (1.0 - pa)

    inner_hairline = inner_mask & (dist_in <= inner_line_px)
    comp_rgb[inner_hairline] = comp_rgb[inner_hairline] * 0.28 + np.array([23.0, 27.0, 46.0]) * 0.72

    canvas[inner_mask, :3] = np.clip(comp_rgb[inner_mask], 0, 255).astype(np.uint8)
    canvas[inner_mask, 3] = 255

    out_im = Image.fromarray(canvas, "RGBA").resize((ICON_SIZE, ICON_SIZE), Image.Resampling.LANCZOS)
    out_path = OUT_DIR / f"ico_shard_{hero_id}.png"
    out_im.save(out_path, format="PNG", optimize=True)
    write_unity_png_meta(out_path, used_guids, asset_kind="shard")
    print(f"[HeroShard] Saved {out_path.name} -> {out_im.size}")
    return out_im


# ---------------------------------------------------------------------------
# 4. 1024x1024 9-Part Skeletal Puppet Atlas Builder & 6-Pose FK Verifier
# ---------------------------------------------------------------------------

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
    """Softens sharp waist-cut corners on detached legs and seals with a 3px comic ink hip cap."""
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


def split_paired_legs_if_needed(leg_rgba: Image.Image) -> Image.Image:
    """
    If `image-generator` drew a 2-legged pair of pants in a leg cell (wide aspect ratio or
    two distinct lower-ankle columns), cleanly isolates a single detached leg + rounded hip cap!
    """
    im = tight_crop_rgba(leg_rgba, alpha_thresh=16)
    arr = np.asarray(im)
    h, w, _ = arr.shape
    if w < h * 0.62:
        return im

    # Check lower 45% (shins/boots) for two separate horizontal leg columns
    lower_fg = arr[int(h * 0.55) :, :, 3] > 32
    col_proj = lower_fg.sum(axis=0)
    mid_lo, mid_hi = int(w * 0.32), int(w * 0.68)
    if mid_hi > mid_lo and col_proj[mid_lo:mid_hi].min() <= 2:
        # Split at the deepest valley between the two legs and keep the cleaner right/front leg
        valley = mid_lo + int(np.argmin(col_proj[mid_lo:mid_hi]))
        right_half = arr[:, valley:].copy()
        left_half = arr[:, :valley].copy()
        pick = right_half if (right_half[:, :, 3] > 32).sum() >= (left_half[:, :, 3] > 32).sum() * 0.85 else left_half
        return round_top_hip_cap(tight_crop_rgba(Image.fromarray(pick, "RGBA")))
    return im


def place_part_in_cell(
    raw_part: Image.Image,
    target_size_px: int,
    part_pivot_uv: Tuple[float, float],
    cell_pivot_uv: Tuple[float, float],
    pre_rotate_deg: float = 0.0,
    flip_x: bool = False,
) -> Image.Image:
    im = tight_crop_rgba(raw_part, alpha_thresh=16)
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


def parse_rig_offsets_from_cs(hero_id: str, fallback: Dict[str, Tuple[float, float]]) -> Dict[str, Tuple[float, float]]:
    """Reads exact `PuppetRegistry` Vector3 bone offsets from `ForestHeroPuppet.cs` if present."""
    if not PUPPET_CS_PATH.is_file():
        return fallback
    try:
        txt = PUPPET_CS_PATH.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return fallback

    m_block = re.search(rf'\["{re.escape(hero_id)}"\]\s*=\s*new\s+PuppetRigProfile\s*\{{(.*?)\n\s*\}},', txt, re.DOTALL)
    if not m_block:
        return fallback
    body = m_block.group(1)
    field_map = {
        "pelvis": "pelvisBase",
        "leg_near": "legNearBase",
        "leg_far": "legFarBase",
        "torso": "torsoBase",
        "head": "headBase",
        "acc": "accBase",
        "arm_near": "armNearBase",
        "arm_far": "armFarBase",
        "tool": "toolGrip",
    }
    parsed = dict(fallback)
    for k, cs_name in field_map.items():
        mv = re.search(
            rf"{cs_name}\s*=\s*new\s+Vector3\(\s*([+-]?[0-9.]+)f?\s*,\s*([+-]?[0-9.]+)f?",
            body,
        )
        if mv:
            parsed[k] = (float(mv.group(1)), float(mv.group(2)))
    return parsed


DEFAULT_NEW_RIG_OFFSETS: Dict[str, Dict[str, Tuple[float, float]]] = {
    "asa": {
        "pelvis": (0.0, 0.335),
        "leg_near": (-0.026, 0.01),
        "leg_far": (0.028, 0.01),
        "torso": (0.0, -0.02),
        "head": (0.012, 0.250),
        "acc": (-0.062, 0.155),
        "arm_near": (-0.062, 0.205),
        "arm_far": (0.068, 0.198),
        "tool": (0.012, -0.215),
    },
    "qinghe": {
        "pelvis": (0.0, 0.315),
        "leg_near": (-0.025, 0.01),
        "leg_far": (0.027, 0.01),
        "torso": (0.0, -0.02),
        "head": (0.012, 0.240),
        "acc": (-0.062, 0.150),
        "arm_near": (-0.060, 0.200),
        "arm_far": (0.066, 0.194),
        "tool": (0.014, -0.205),
    },
    "zhoudr": {
        "pelvis": (0.0, 0.335),
        "leg_near": (-0.026, 0.01),
        "leg_far": (0.028, 0.01),
        "torso": (0.0, -0.02),
        "head": (0.012, 0.255),
        "acc": (-0.062, 0.150),
        "arm_near": (-0.064, 0.210),
        "arm_far": (0.070, 0.202),
        "tool": (0.012, -0.220),
    },
}


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
    bust_ref: Optional[Image.Image] = None,
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

    # Render 6-pose FK rig preview replicating exact ForestHeroPuppet.cs Tick() angles
    tile_w, tile_h = 240, 360
    cols_n = 7
    preview = Image.new("RGBA", (cols_n * tile_w, tile_h), (222, 186, 122, 255))
    draw = ImageDraw.Draw(preview)
    for x in range(0, cols_n * tile_w, tile_w):
        draw.line([(x, 0), (x, tile_h)], fill=(180, 145, 90, 255), width=2)
    ground_y = 306
    draw.line([(0, ground_y), (cols_n * tile_w, ground_y)], fill=(150, 118, 70, 255), width=2)

    if bust_ref is not None:
        sc = min(210.0 / max(1, bust_ref.width), 265.0 / max(1, bust_ref.height))
        th = bust_ref.resize(
            (max(1, int(round(bust_ref.width * sc))), max(1, int(round(bust_ref.height * sc)))),
            Image.Resampling.LANCZOS,
        )
        preview.alpha_composite(th, (tile_w // 2 - th.width // 2, ground_y - th.height))
        draw.text((12, 8), f"{atlas_key} Ref", fill=(45, 32, 20, 255))

    if is_shotgun:
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
            ("4. Windup",       22,  -8, -24,  18, -11, 148, 136, -135, False, True),
            ("5. Strike!",     -24,   8,  26, -16,  10,  42,  34, -102, True,  True),
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


@dataclass
class PuppetPartBoxSpec:
    """Per-character bounding-box / pivot tuning for assembling a 9-part puppet sheet."""
    # Optional explicit `(y0, y1, x0, x1)` crop regions on the raw puppet sheet; if None, auto-grid/component extraction is used
    boxes: Dict[str, Tuple[int, int, int, int]] = field(default_factory=dict)
    head_h: int = 80
    head_open_piv: Tuple[float, float] = (0.48, 0.93)
    head_effort_piv: Tuple[float, float] = (0.48, 0.93)
    acc_h: int = 38
    acc_piv: Tuple[float, float] = (0.88, 0.25)
    acc_rot: float = 0.0
    acc_flip_x: bool = False
    torso_h: int = 94
    torso_piv: Tuple[float, float] = (0.52, 0.86)
    arm_near_h: int = 80
    arm_near_piv: Tuple[float, float] = (0.50, 0.14)
    arm_far_h: int = 78
    arm_far_piv: Tuple[float, float] = (0.50, 0.14)
    leg_near_h: int = 96
    leg_near_piv: Tuple[float, float] = (0.48, 0.12)
    leg_far_h: int = 95
    leg_far_piv: Tuple[float, float] = (0.48, 0.12)
    use_single_leg_mirror: bool = False
    tool_h: int = 112
    tool_piv: Tuple[float, float] = (0.46, 0.78)
    tool_rot: float = 0.0
    tool_flip_x: bool = False
    tool_hole_min_px: int = 30
    is_shotgun: bool = False


DEFAULT_PUPPET_SPECS: Dict[str, PuppetPartBoxSpec] = {
    "asa": PuppetPartBoxSpec(
        boxes={
            "head_open":   (30,  282,  94, 340),
            "head_effort": (30,  282, 410, 658),
            "acc":         (50,  304, 724, 896),
            "torso":       (282, 565, 110, 294),
            "arm_near":    (288, 714, 404, 552),
            "arm_far":     (320, 642, 810, 926),
            "leg_near":    (562, 976, 206, 362),
            "leg_far":     (608, 980, 534, 690),
            "tool":        (668, 978, 802, 934),
        },
        head_h=80,
        head_open_piv=(0.43, 0.93),
        head_effort_piv=(0.44, 0.93),
        acc_h=48,
        acc_piv=(0.90, 0.14),
        torso_h=90,
        torso_piv=(0.50, 0.88),
        arm_near_h=102,
        arm_near_piv=(0.58, 0.18),
        arm_far_h=76,
        arm_far_piv=(0.45, 0.12),
        leg_near_h=96,
        leg_near_piv=(0.42, 0.12),
        leg_far_h=94,
        leg_far_piv=(0.44, 0.10),
        tool_h=98,
        tool_piv=(0.44, 0.80),
        tool_hole_min_px=14,
        is_shotgun=False,
    ),
    "qinghe": PuppetPartBoxSpec(
        boxes={
            "head_open":   (30,  286,  70, 286),
            "head_effort": (30,  286, 416, 628),
            "acc":         (30,  314, 780, 928),
            "torso":       (292, 676,  56, 306),
            "arm_near":    (336, 668, 404, 532),
            "arm_far":     (336, 668, 800, 928),
            "leg_near":    (678, 980, 206, 358),
            "leg_far":     (676, 974, 524, 676),
            "tool":        (674, 994, 730, 990),
        },
        head_h=79,
        head_open_piv=(0.43, 0.93),
        head_effort_piv=(0.43, 0.93),
        acc_h=52,
        acc_piv=(0.86, 0.12),
        acc_flip_x=True,
        torso_h=96,
        torso_piv=(0.54, 0.86),
        arm_near_h=78,
        arm_near_piv=(0.52, 0.14),
        arm_far_h=76,
        arm_far_piv=(0.48, 0.14),
        leg_near_h=92,
        leg_near_piv=(0.40, 0.12),
        leg_far_h=91,
        leg_far_piv=(0.40, 0.12),
        tool_h=96,
        tool_piv=(0.50, 0.80),
        tool_hole_min_px=14,
        is_shotgun=True,
    ),
    "zhoudr": PuppetPartBoxSpec(
        boxes={
            "head_open":   (20,  328,  56, 330),
            "head_effort": (20,  328, 384, 672),
            "acc":         (36,  322, 740, 948),
            "torso":       (350, 654,  72, 294),
            "arm_near":    (368, 698, 372, 500),
            "arm_far":     (368, 690, 580, 700),
            "leg_near":    (708, 976, 204, 362),
            "leg_far":     (714, 980, 476, 628),
            "tool":        (356, 686, 828, 914),
        },
        head_h=84,
        head_open_piv=(0.462, 0.94),
        head_effort_piv=(0.404, 0.94),
        acc_h=64,
        acc_piv=(0.76, 0.24),
        torso_h=96,
        torso_piv=(0.50, 0.88),
        arm_near_h=80,
        arm_near_piv=(0.60, 0.14),
        arm_far_h=78,
        arm_far_piv=(0.38, 0.14),
        leg_near_h=92,
        leg_near_piv=(0.40, 0.12),
        leg_far_h=91,
        leg_far_piv=(0.40, 0.12),
        tool_h=112,
        tool_piv=(0.48, 0.80),
        tool_hole_min_px=14,
        is_shotgun=False,
    ),
}


def extract_9_puppet_parts_auto_or_boxed(
    sheet_rgb: np.ndarray,
    spec: PuppetPartBoxSpec,
) -> Dict[str, Image.Image]:
    """
    Extracts the 9 isolated skeletal puppet parts (`head_open`, `head_effort`, `acc`,
    `torso`, `arm_near`, `arm_far`, `leg_near`, `leg_far`, `tool`) either via explicit
    bboxes in `spec.boxes` or via smart 3x3 connected-component / gutter grid assignment!
    """
    h, w, _ = sheet_rgb.shape
    slot_names_grid = [
        ("head_open", 0, 0),
        ("head_effort", 0, 1),
        ("acc", 0, 2),
        ("torso", 1, 0),
        ("arm_near", 1, 1),
        ("arm_far", 1, 2),
        ("leg_near", 2, 0),
        ("leg_far", 2, 1),
        ("tool", 2, 2),
    ]

    # First compute clean gutter splits as fallback for any slot not explicitly boxed
    y_splits = [0, h // 3, (2 * h) // 3, h]
    x_splits = [0, w // 3, (2 * w) // 3, w]
    try:
        # Refine splits along white gutters
        min_c = sheet_rgb.min(axis=2)
        sat = sheet_rgb.max(axis=2).astype(np.int16) - min_c.astype(np.int16)
        non_white = (min_c < 238) | (sat > 20)
        for axis, count, out_sp in [(0, 3, y_splits), (1, 3, x_splits)]:
            length = sheet_rgb.shape[axis]
            proj = non_white.sum(axis=(1 if axis == 0 else 0)).astype(np.float64)
            smooth = np.convolve(proj, np.array([0.15, 0.2, 0.3, 0.2, 0.15]), mode="same")
            win = max(10, int(round(length * 0.09)))
            for k in range(1, count):
                ideal = int(round(k * length / float(count)))
                lo, hi = max(1, ideal - win), min(length - 1, ideal + win)
                seg = smooth[lo : hi + 1]
                cands = np.where(seg <= seg.min() + 1.5)[0] + lo
                out_sp[k] = int(cands[np.argmin(np.abs(cands - ideal))])
    except Exception:
        pass

    # Also test full-sheet connected-component extraction (great when tall weapons cross the 2/3 line!)
    rgba_full, fg_full = cutout_comic_rgba_array(sheet_rgb, hole_rule=None, min_island_px=20, keep_only_largest=False)
    clbl, n_comp = ndimage.label(fg_full, structure=np.ones((3, 3), dtype=np.int32))
    comps = []
    for cid in range(1, n_comp + 1):
        ys, xs = np.where(clbl == cid)
        if len(xs) >= 20:
            comps.append((len(xs), cid, int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1, float(xs.mean()), float(ys.mean())))
    comps.sort(key=lambda t: t[0], reverse=True)

    parts: Dict[str, Image.Image] = {}
    for sname, r, c in slot_names_grid:
        if sname in ("head_open", "head_effort"):
            # Protect hair/eyes/teeth (`ux <= 0.70`), punch enclosed monocle-cord background loop (`ux > 0.70, uy > 0.58`) if present!
            t_rule = HolePunchRule(
                min_area_px=16,
                y_min_ratio=0.56,
                white_min=247.0,
                sat_max=10.0,
                protect_uv_boxes=[(0.0, 0.0, 0.68, 1.0)],
            )
        elif sname == "torso":
            t_rule = HolePunchRule(min_area_px=spec.tool_hole_min_px, y_min_ratio=0.0, white_min=249.0, sat_max=8.0, max_std=3.5)
        elif sname in ("tool", "acc", "arm_near"):
            t_rule = HolePunchRule(min_area_px=spec.tool_hole_min_px, y_min_ratio=0.0, white_min=247.0, sat_max=10.0)
        else:
            t_rule = None

        if sname in spec.boxes:
            by0, by1, bx0, bx1 = spec.boxes[sname]
            # Isolate all complete connected components whose centroid lies inside `spec.boxes[sname]` (including detached weapon sparks!)
            sub_cids = [
                cid for (_, cid, x0, y0, x1, y1, mx, my) in comps
                if bx0 <= mx <= bx1 and by0 <= my <= by1
            ]
            if sub_cids:
                mask = np.isin(clbl, sub_cids)
                ys, xs = np.where(mask)
                x0, x1 = max(0, int(xs.min()) - 5), min(w, int(xs.max()) + 6)
                y0, y1 = max(0, int(ys.min()) - 5), min(h, int(ys.max()) + 6)
                iso_rgb = np.full((y1 - y0, x1 - x0, 3), 255, dtype=np.uint8)
                dil = ndimage.binary_dilation(mask[y0:y1, x0:x1], iterations=3)
                iso_rgb[dil] = sheet_rgb[y0:y1, x0:x1][dil]
                parts[sname] = cutout_comic_sprite(iso_rgb, hole_rule=t_rule, keep_only_largest=False)
            else:
                parts[sname] = cutout_comic_sprite(sheet_rgb[by0:by1, bx0:bx1], hole_rule=t_rule, keep_only_largest=True)
            continue

        # Find main component whose centroid lies inside cell `(r, c)` (allowing tall weapon in `(2, 2)` to rise into `(1, 2)` right half)
        cy0, cy1 = y_splits[r], y_splits[r + 1]
        cx0, cx1 = x_splits[c], x_splits[c + 1]
        matched_cid = None
        for area, cid, x0, y0, x1, y1, mx, my in comps:
            if sname == "tool":
                if mx >= w * 0.68 and y1 >= h * 0.62 and my >= h * 0.42:
                    matched_cid = cid
                    break
            else:
                if cx0 - 16 <= mx <= cx1 + 16 and cy0 - 16 <= my <= cy1 + 16:
                    matched_cid = cid
                    break

        if matched_cid is not None and (comps[0][4] - comps[0][2]) < w * 0.55:
            mask = (clbl == matched_cid)
            ys, xs = np.where(mask)
            x0, x1 = max(0, int(xs.min()) - 5), min(w, int(xs.max()) + 6)
            y0, y1 = max(0, int(ys.min()) - 5), min(h, int(ys.max()) + 6)
            iso_rgb = np.full((y1 - y0, x1 - x0, 3), 255, dtype=np.uint8)
            dil = ndimage.binary_dilation(mask[y0:y1, x0:x1], iterations=3)
            iso_rgb[dil] = sheet_rgb[y0:y1, x0:x1][dil]
            parts[sname] = cutout_comic_sprite(iso_rgb, hole_rule=t_rule, keep_only_largest=True)
        else:
            sub = sheet_rgb[cy0:cy1, cx0:cx1].copy()
            parts[sname] = cutout_comic_sprite(sub, hole_rule=t_rule, keep_only_largest=True)

    return parts


def build_new_hero_puppet(
    hero_id: str,
    sheet_path: Path,
    used_guids: Set[str],
    qa_dir: Path,
    bust_ref: Optional[Image.Image] = None,
    spec_override: Optional[PuppetPartBoxSpec] = None,
) -> Image.Image:
    if not sheet_path.is_file():
        raise FileNotFoundError(f"Puppet sheet for {hero_id} not found: {sheet_path}")

    spec = spec_override or DEFAULT_PUPPET_SPECS[hero_id]
    sheet_rgb = np.array(Image.open(sheet_path).convert("RGB"))
    parts = extract_9_puppet_parts_auto_or_boxed(sheet_rgb, spec)

    raw_head_open = parts["head_open"]
    raw_head_effort = parts["head_effort"]
    raw_acc = parts["acc"]
    raw_torso = parts["torso"]
    raw_arm_near = parts["arm_near"]
    raw_arm_far = darken_back_limb(parts["arm_far"], 0.90)

    leg_n_clean = round_top_hip_cap(split_paired_legs_if_needed(parts["leg_near"]))
    if spec.use_single_leg_mirror:
        raw_leg_near = leg_n_clean
        raw_leg_far = darken_back_limb(leg_n_clean, 0.86)
    else:
        leg_f_cand = split_paired_legs_if_needed(parts["leg_far"])
        # If `(2, 0)` was paired pants and `(2, 1)` was a clean single leg (like Leimeng earlier), prefer the cleaner narrower single leg!
        if parts["leg_near"].width > parts["leg_near"].height * 0.62 and parts["leg_far"].width <= parts["leg_far"].height * 0.58:
            single = round_top_hip_cap(tight_crop_rgba(parts["leg_far"]))
            raw_leg_near = single
            raw_leg_far = darken_back_limb(single, 0.86)
        else:
            raw_leg_near = leg_n_clean
            raw_leg_far = darken_back_limb(round_top_hip_cap(leg_f_cand), 0.86)

    raw_tool = parts["tool"]

    rig = parse_rig_offsets_from_cs(hero_id, DEFAULT_NEW_RIG_OFFSETS[hero_id])
    cells = {
        (0, 2): place_part_in_cell(raw_head_open,   spec.head_h,     spec.head_open_piv,   UV_HEAD),
        (1, 2): place_part_in_cell(raw_head_effort, spec.head_h,     spec.head_effort_piv, UV_HEAD),
        (2, 2): place_part_in_cell(raw_acc,         spec.acc_h,      spec.acc_piv,         UV_ACC, pre_rotate_deg=spec.acc_rot, flip_x=spec.acc_flip_x),
        (0, 1): place_part_in_cell(raw_torso,       spec.torso_h,    spec.torso_piv,       UV_TORSO),
        (1, 1): place_part_in_cell(raw_arm_near,    spec.arm_near_h, spec.arm_near_piv,    UV_ARM),
        (2, 1): place_part_in_cell(raw_arm_far,     spec.arm_far_h,  spec.arm_far_piv,     UV_ARM),
        (0, 0): place_part_in_cell(raw_leg_near,    spec.leg_near_h, spec.leg_near_piv,    UV_LEG),
        (1, 0): place_part_in_cell(raw_leg_far,     spec.leg_far_h,  spec.leg_far_piv,     UV_LEG),
        (2, 0): place_part_in_cell(raw_tool,        spec.tool_h,     spec.tool_piv,        UV_TOOL, pre_rotate_deg=spec.tool_rot, flip_x=spec.tool_flip_x),
    }
    return pack_puppet_atlas_and_verify(
        atlas_key=f"{hero_id}_puppet_atlas",
        cells=cells,
        rig_offsets=rig,
        used_guids=used_guids,
        preview_out=qa_dir / f"preview_rig_{hero_id}.jpg",
        is_shotgun=spec.is_shotgun,
        bust_ref=bust_ref,
    )


# ---------------------------------------------------------------------------
# 5. Dual-Background QA Contact Sheets (`preview_fullbody_9.jpg`, Busts/Shards)
# ---------------------------------------------------------------------------

def render_fullbody_9_qa_sheet(
    fullbodies: Sequence[Tuple[str, Image.Image]],
    out_path: Path,
) -> None:
    """
    Renders a crisp 3x3 dual-background QA contact sheet (`preview_fullbody_9.jpg`):
    Left half of each tile = Warm Cabin Wood & Rug (`#3D2A1F` + `#D8B782` rug + dark oval foot shadow);
    Right half of each tile = Tactical Chalkboard Dark-Teal (`#163748` + yellow cell floor frame `#E7C84B`).
    """
    if not fullbodies:
        return
    cols = 3
    rows = (len(fullbodies) + cols - 1) // cols
    tile_w, tile_h = 380, 380
    board = Image.new("RGBA", (cols * tile_w, rows * tile_h), (20, 26, 36, 255))
    draw = ImageDraw.Draw(board)

    for idx, (hid, im) in enumerate(fullbodies):
        r, c = divmod(idx, cols)
        ox, oy = c * tile_w, r * tile_h
        mid_x = ox + tile_w // 2
        foot_y = oy + tile_h - 38

        # Left half: Hero Cabin warm wood wall + sunlit woven rug
        draw.rectangle([ox + 4, oy + 4, mid_x, oy + tile_h - 4], fill=(64, 46, 36, 255))
        draw.rectangle([ox + 16, foot_y - 24, mid_x, foot_y + 12], fill=(208, 174, 118, 255))

        # Right half: Tactical Chalkboard dark-teal (#163748) + yellow cell frame
        draw.rectangle([mid_x, oy + 4, ox + tile_w - 4, oy + tile_h - 4], fill=(22, 55, 72, 255))
        draw.rectangle(
            [mid_x - 54, foot_y - 46, ox + tile_w - 34, foot_y + 12],
            outline=(234, 198, 68, 255),
            width=3,
        )

        # Ground shadow oval under feet
        draw.ellipse(
            [mid_x - 68, foot_y - 12, mid_x + 68, foot_y + 10],
            fill=(12, 16, 22, 130),
        )

        # Scale fullbody (`H=576`) cleanly to `316px` display height so head-to-boots + alpha halo are 100% visible
        sc = min((tile_w - 36) / float(max(1, im.width)), 316.0 / float(max(1, im.height)))
        dw = max(1, int(round(im.width * sc)))
        dh = max(1, int(round(im.height * sc)))
        thumb = im.resize((dw, dh), Image.Resampling.LANCZOS)
        board.alpha_composite(thumb, (mid_x - dw // 2, foot_y - dh + 4))

        draw.rectangle([ox + 4, oy + 4, ox + tile_w - 4, oy + tile_h - 4], outline=(86, 102, 126, 255), width=2)
        draw.rectangle([ox + 6, oy + tile_h - 28, ox + tile_w - 6, oy + tile_h - 6], fill=(14, 18, 28, 220))
        draw.text(
            (ox + 12, oy + tile_h - 24),
            f"fullbody_{hid}.png ({im.width}x{im.height}) [Cabin | Chalkboard]",
            fill=(238, 245, 255, 255),
        )

    out_path.parent.mkdir(parents=True, exist_ok=True)
    board.convert("RGB").save(out_path, format="JPEG", quality=94)
    print(f"[QA] Saved 9-hero dual-bg fullbody preview -> {out_path}")


def render_busts_and_shards_qa_sheet(
    items: Sequence[Tuple[str, Image.Image, Image.Image]],
    out_path: Path,
) -> None:
    if not items:
        return
    col_w, row_h = 340, 290
    board = Image.new("RGBA", (len(items) * col_w, row_h), (24, 30, 44, 255))
    draw = ImageDraw.Draw(board)
    for idx, (hid, bust_im, shard_im) in enumerate(items):
        ox = idx * col_w
        draw.rectangle([ox + 6, 6, ox + 178, row_h - 32], fill=(22, 55, 72, 255), outline=(88, 114, 140, 255), width=2)
        draw.rectangle([ox + 184, 6, ox + col_w - 6, row_h - 32], fill=(236, 232, 220, 255), outline=(88, 114, 140, 255), width=2)

        sc = min(156.0 / max(1, bust_im.width), 236.0 / max(1, bust_im.height))
        bw, bh = int(round(bust_im.width * sc)), int(round(bust_im.height * sc))
        b_th = bust_im.resize((bw, bh), Image.Resampling.LANCZOS)
        board.alpha_composite(b_th, (ox + 92 - bw // 2, row_h - 36 - bh))

        s_th = shard_im.resize((140, 140), Image.Resampling.LANCZOS)
        board.alpha_composite(s_th, (ox + 184 + (col_w - 190 - 140) // 2, 60))
        draw.text((ox + 12, row_h - 24), f"bust_{hid} ({bust_im.width}x{bust_im.height}) + ico_shard_{hid}", fill=(240, 245, 255, 255))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    board.convert("RGB").save(out_path, format="JPEG", quality=94)
    print(f"[QA] Saved busts & shards preview -> {out_path}")


# ---------------------------------------------------------------------------
# 6. CLI Entrypoint (Supports Incremental & Full 6-Sheet Execution)
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(description="Slice & package Phase-2 9-Hero Full-Body, Bust, Shard & Puppet Atlas assets.")
    parser.add_argument("--fb1", type=str, default="", help="Path to FB1 sheet (dachui, xiaotang, leimeng)")
    parser.add_argument("--fb2", type=str, default="", help="Path to FB2 sheet (linyan, tieshu, laochentou)")
    parser.add_argument("--fb3", type=str, default="", help="Path to FB3 sheet (asa, qinghe, zhoudr)")
    parser.add_argument("--p1", type=str, default="", help="Path to P1 puppet sheet (asa)")
    parser.add_argument("--p2", type=str, default="", help="Path to P2 puppet sheet (qinghe)")
    parser.add_argument("--p3", type=str, default="", help="Path to P3 puppet sheet (zhoudr)")
    parser.add_argument("--qa-dir", type=Path, default=DEFAULT_SCRATCH_DIR, help="Scratch QA preview output folder")
    args = parser.parse_args()

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    used_guids = collect_existing_guids(ASSETS_ROOT)

    ran_any = False
    if args.fb1:
        ran_any = True
        trio1 = extract_fullbody_trio(Path(args.fb1), FB1_HEROES)
        for hid in FB1_HEROES:
            save_fullbody_sprite(trio1[hid], hid, used_guids)

    if args.fb2:
        ran_any = True
        trio2 = extract_fullbody_trio(Path(args.fb2), FB2_HEROES)
        for hid in FB2_HEROES:
            save_fullbody_sprite(trio2[hid], hid, used_guids)

    new_busts: Dict[str, Image.Image] = {}
    if args.fb3:
        ran_any = True
        trio3 = extract_fullbody_trio(Path(args.fb3), FB3_HEROES)
        bust_shards_qa: List[Tuple[str, Image.Image, Image.Image]] = []
        bust_crop_ratios = {
            "asa":    (0.0, 0.52),
            "qinghe": (0.0, 0.54),
            "zhoudr": (0.0, 0.52),
        }
        shard_backdrops = {
            "asa":    (228, 230, 248),
            "qinghe": (222, 238, 248),
            "zhoudr": (225, 230, 252),
        }
        shard_crop_offsets = {
            "asa":    (-4, 10, 192),
            "qinghe": (-8, 10, 194),
            "zhoudr": (-28, 6, 206),
        }
        for hid in FB3_HEROES:
            fb_saved = save_fullbody_sprite(trio3[hid], hid, used_guids)
            top_r, bot_r = bust_crop_ratios[hid]
            raw_bust = crop_bust_from_fullbody(fb_saved, crop_top_ratio=top_r, crop_bottom_ratio=bot_r)
            bust_saved = save_bust_portrait(raw_bust, f"bust_{hid}", used_guids, target_h=400, pad_px=4)
            new_busts[hid] = bust_saved
            dx_sh, y_sh, sz_sh = shard_crop_offsets[hid]
            ax0, ay0, ax1, ay1 = auto_head_crop_box_for_shard(bust_saved, side_px=sz_sh, y_top=y_sh)
            shard_saved = compose_hero_shard_from_bust(
                bust_saved,
                hid,
                used_guids,
                crop_box=(ax0 + dx_sh, ay0, ax1 + dx_sh, ay1),
                backdrop_rgb=shard_backdrops[hid],
            )
            bust_shards_qa.append((hid, bust_saved, shard_saved))
        render_busts_and_shards_qa_sheet(bust_shards_qa, args.qa_dir / "preview_busts_and_shards_3.jpg")

    # Render `preview_fullbody_9.jpg` from any saved `fullbody_*.png` files on disk
    fb_on_disk: List[Tuple[str, Image.Image]] = []
    for hid in ALL_9_HEROES:
        p = OUT_DIR / f"fullbody_{hid}.png"
        if p.is_file():
            fb_on_disk.append((hid, Image.open(p).convert("RGBA")))
    if fb_on_disk and (args.fb1 or args.fb2 or args.fb3):
        render_fullbody_9_qa_sheet(fb_on_disk, args.qa_dir / "preview_fullbody_9.jpg")

    for p_arg, hid in [(args.p1, "asa"), (args.p2, "qinghe"), (args.p3, "zhoudr")]:
        if p_arg:
            ran_any = True
            b_ref = new_busts.get(hid)
            if b_ref is None and (OUT_DIR / f"bust_{hid}.png").is_file():
                b_ref = Image.open(OUT_DIR / f"bust_{hid}.png").convert("RGBA")
            build_new_hero_puppet(hid, Path(p_arg), used_guids, args.qa_dir, bust_ref=b_ref)

    if not ran_any:
        parser.print_help()
        return 0
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
