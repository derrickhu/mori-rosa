#!/usr/bin/env python3
"""
Builds Assets/Mori/ForestSlice/Resources/ForestArt/battle_arena_bg.png (720x1280 opaque RGB PNG):
  【山口断桥公路防线 · 独立策略战斗舞台】
  - Top 24% (y = 0..307 px): Post-apocalyptic autumn mountain pass horizon, billowy twilight comic
    clouds, pine-tooth far ridge, rugged canyon cliffs, distant broken overpass, weathered concrete
    bridge parapet, stacked burlap sandbags, rusted W-beam highway guardrail, concertina barbed wire,
    and spiked wooden barricades. Slightly darker/muted near very top for HUD & VS contrast.
  - Middle 56% (y = 308..1024 px): 100% UNOBSTRUCTED flat weathered asphalt & organic salvaged timber
    bridge repair deck stretching left-to-right. Jagged chipped asphalt break lips, staggered bolted
    oak planks, subtle cracked road markings, faint painted tactical staging brackets on left side,
    flat pebbles/autumn leaves painted into floor, warm buff/ochre-grey value.
    ZERO trees, ZERO tall props, ZERO foreground canopies!
  - Bottom 20% (y = 1025..1280 px): Low broken concrete roadside curb, flat caution stripes, steel
    bridge edge girder, and cross-hatched dark cliff-edge stone & steel under-truss framing the
    bottom skill-card dock.

Follows <RULE[user_global]>: tries local Vertex AI (<= 14s timeout) if reachable, and falls back
to a 2x-supersampled (1440x2560 -> 720x1280 LANCZOS) hand-drawn bold ink-line post-apocalyptic
autumn comic illustration composited with authentic ForestArt elements.
"""
from __future__ import annotations

import base64
import io
import json
import math
import random
import subprocess
import urllib.request
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

REPO = Path(__file__).resolve().parents[1]
FOREST_ART_DIR = REPO / "Assets" / "Mori" / "ForestSlice" / "Resources" / "ForestArt"
OUT_DIR = FOREST_ART_DIR
OUT_PNG = OUT_DIR / "battle_arena_bg.png"

W, H = 720, 1280
SS = 2
W2, H2 = W * SS, H * SS

VERTEX_PROMPT = (
    "2D hand-drawn bold dark ink-line post-apocalyptic autumn comic game battle arena background, "
    "vertical portrait 9:16 (720x1280), elevated 3/4 side-view looking down at a wide horizontal "
    "mountain-pass highway bridge deck stretching cleanly from left edge to right edge. "
    "Warm muted earth tones, cel-shaded comic cross-hatching, matching 'Last Day Hoarder' style. "
    "Top 24% (y=0..307): dark smoky twilight mountain pass sky at very top for UI contrast, distant "
    "autumn pine ridge, weathered concrete bridge parapet wall, stacked sandbags, rusted steel highway "
    "guardrail, barbed wire and spiked wooden barricades strictly above y=307. "
    "Middle 56% (y=308..1024): 100% UNOBSTRUCTED, wide, completely flat weathered warm buff-ochre asphalt "
    "highway & flush reinforced heavy timber plank bridge deck stretching cleanly left-to-right, subtle "
    "cracked road lines, faint painted tactical staging lines on the left half, tiny flat pebbles. "
    "STRICTLY ZERO trees, ZERO tall props, ZERO characters, ZERO foreground objects in the middle 56%! "
    "Bottom 20% (y=1025..1280): low chipped concrete roadside curb with flat yellow-black caution stripes "
    "and dark cliff-edge stone & steel bridge girder shadow framing the bottom UI dock, no tall foliage."
)


def try_vertex_arena() -> Image.Image | None:
    cfg_path = Path.home() / ".config" / "vertex" / "config.json"
    try:
        if not cfg_path.is_file():
            return None
        cfg = json.loads(cfg_path.read_text(encoding="utf-8"))
        proj = cfg.get("project_id")
        if not proj:
            return None
        tok = subprocess.check_output(
            ["gcloud", "auth", "application-default", "print-access-token"],
            timeout=5,
            stderr=subprocess.DEVNULL,
        ).decode().strip()
        if not tok:
            return None

        url = (
            f"https://us-central1-aiplatform.googleapis.com/v1/projects/{proj}"
            "/locations/us-central1/publishers/google/models/gemini-2.5-flash-image:generateContent"
        )
        body = json.dumps({
            "contents": [{"role": "user", "parts": [{"text": VERTEX_PROMPT}]}],
            "generationConfig": {
                "responseModalities": ["IMAGE"],
                "imageConfig": {"aspectRatio": "9:16"},
            },
        }).encode("utf-8")

        for proxy_url in ("http://127.0.0.1:7897", None):
            try:
                handlers = (
                    [urllib.request.ProxyHandler({"http": proxy_url, "https": proxy_url})]
                    if proxy_url
                    else [urllib.request.ProxyHandler({})]
                )
                opener = urllib.request.build_opener(*handlers)
                req = urllib.request.Request(
                    url,
                    data=body,
                    headers={
                        "Authorization": f"Bearer {tok}",
                        "Content-Type": "application/json",
                    },
                )
                with opener.open(req, timeout=14) as resp:
                    data = json.loads(resp.read().decode("utf-8"))
                for cand in data.get("candidates", []):
                    for part in cand.get("content", {}).get("parts", []):
                        inl = part.get("inlineData")
                        if inl and inl.get("data"):
                            raw = base64.b64decode(inl["data"])
                            im = Image.open(io.BytesIO(raw)).convert("RGB")
                            print(f"[Vertex] Generated raw arena {im.size} via proxy={proxy_url}")
                            return fit_portrait(im, W, H)
            except Exception as e:
                print(f"[Vertex] Attempt proxy={proxy_url} skipped: {e}")
    except Exception as e:
        print(f"[Vertex] Unavailable in current environment ({e}); using local comic builder.")
    return None


def fit_portrait(im: Image.Image, tw: int, th: int) -> Image.Image:
    sw, sh = im.size
    target_ratio = tw / float(th)
    src_ratio = sw / float(sh)
    if src_ratio > target_ratio:
        nw = round(sh * target_ratio)
        x0 = (sw - nw) // 2
        im = im.crop((x0, 0, x0 + nw, sh))
    elif src_ratio < target_ratio:
        nh = round(sw / target_ratio)
        y0 = (sh - nh) // 2
        im = im.crop((0, y0, sw, y0 + nh))
    return im.resize((tw, th), Image.Resampling.LANCZOS)


def wobble_pts(pts, rng: random.Random, amp: float = 2.2, subdiv: int = 2):
    out = [pts[0]]
    for i in range(len(pts) - 1):
        x0, y0 = pts[i]
        x1, y1 = pts[i + 1]
        dist = math.hypot(x1 - x0, y1 - y0)
        steps = max(1, min(subdiv, int(dist // 42)))
        for s in range(1, steps):
            t = s / float(steps)
            mx = x0 + (x1 - x0) * t + rng.uniform(-amp, amp)
            my = y0 + (y1 - y0) * t + rng.uniform(-amp, amp)
            out.append((mx, my))
        out.append((x1, y1))
    return out


def draw_hatch_group(draw: ImageDraw.ImageDraw, x0: int, y0: int, count: int, spacing: int, dx: int, dy: int, col, width: int = 2):
    for i in range(count):
        sx = x0 + i * spacing
        draw.line([(sx, y0), (sx + dx, y0 + dy)], fill=col, width=width)


def tint_sprite(im: Image.Image, r_m: float, g_m: float, b_m: float, sat: float = 1.0) -> Image.Image:
    im = im.convert("RGBA")
    if sat != 1.0:
        rgb = ImageEnhance.Color(im.convert("RGB")).enhance(sat)
        im = Image.merge("RGBA", (*rgb.split(), im.split()[3]))
    arr = np.array(im, dtype=np.float32)
    arr[..., 0] = np.clip(arr[..., 0] * r_m, 0, 255)
    arr[..., 1] = np.clip(arr[..., 1] * g_m, 0, 255)
    arr[..., 2] = np.clip(arr[..., 2] * b_m, 0, 255)
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def draw_sandbag(draw: ImageDraw.ImageDraw, cx: int, cy: int, w: int, h: int, base_col, ink, rng: random.Random):
    x0, y0 = cx - w // 2, cy - h // 2
    x1, y1 = cx + w // 2, cy + h // 2
    pts = [
        (x0 + 8, y0 + 6),
        (cx, y0),
        (x1 - 8, y0 + 5),
        (x1, cy),
        (x1 - 6, y1 - 4),
        (cx, y1),
        (x0 + 6, y1 - 4),
        (x0, cy),
    ]
    wpts = wobble_pts(pts + [pts[0]], rng, amp=1.6, subdiv=2)
    draw.polygon(wpts, fill=base_col)
    hi = tuple(min(255, int(c * 1.13)) for c in base_col[:3]) + (255,)
    sh = tuple(max(0, int(c * 0.75)) for c in base_col[:3]) + (255,)
    draw.polygon(
        [(x0 + 12, y0 + 8), (cx, y0 + 4), (x1 - 12, y0 + 8), (x1 - 16, cy - 2), (x0 + 16, cy - 2)],
        fill=hi,
    )
    draw.polygon(
        [(x0 + 10, cy + 4), (x1 - 10, cy + 4), (x1 - 8, y1 - 5), (cx, y1 - 2), (x0 + 8, y1 - 5)],
        fill=sh,
    )
    draw.line([(x0 + 10, cy + 1), (x1 - 10, cy - 1)], fill=(86, 68, 46, 175), width=3)
    for hx in range(x0 + 18, x1 - 16, 16):
        draw.line([(hx, cy - 4), (hx + 4, cy + 5)], fill=(70, 54, 36, 165), width=2)
    draw.line(wpts, fill=ink, width=5, joint="curve")


def draw_pine_silhouette(draw: ImageDraw.ImageDraw, cx: int, base_y: int, h: int, fill_col, shade_col, ink_col):
    w = int(h * 0.48)
    tw = max(6, w // 7)
    draw.rectangle((cx - tw, base_y - h // 5, cx + tw, base_y + 8), fill=(58, 42, 30, 255), outline=ink_col, width=3)
    tiers = 4
    for t in range(tiers):
        frac = t / float(tiers)
        ty0 = int(base_y - h + frac * (h * 0.56))
        ty1 = int(ty0 + h * (0.34 + frac * 0.06))
        hw = int(w * (0.36 + frac * 0.24))
        poly = [
            (cx, ty0),
            (cx + hw, ty1),
            (cx + int(hw * 0.52), ty1 - 6),
            (cx, ty1 + 4),
            (cx - int(hw * 0.52), ty1 - 6),
            (cx - hw, ty1),
        ]
        draw.polygon(poly, fill=fill_col)
        sh_poly = [(cx, ty0), (cx + hw, ty1), (cx + int(hw * 0.52), ty1 - 6), (cx, ty1 + 4)]
        draw.polygon(sh_poly, fill=shade_col)
        draw.line(poly + [poly[0]], fill=ink_col, width=4, joint="curve")


def draw_crack(draw: ImageDraw.ImageDraw, start, length: int, angle_deg: float, rng: random.Random, ink_col, hi_col, width: int = 4):
    x, y = start
    pts = [(x, y)]
    ang = math.radians(angle_deg)
    segs = max(4, length // 24)
    step = length / float(segs)
    for i in range(segs):
        ang += math.radians(rng.uniform(-24, 24))
        x += math.cos(ang) * step
        y += math.sin(ang) * step * 0.65
        pts.append((x, y))
        if i in (segs // 3, (2 * segs) // 3) and rng.random() < 0.78:
            bx, by = x, y
            bang = ang + math.radians(rng.choice([-44, 38, -56, 48]))
            bpts = [(bx, by)]
            for _ in range(rng.randint(2, 4)):
                bang += math.radians(rng.uniform(-18, 18))
                bx += math.cos(bang) * (step * 0.65)
                by += math.sin(bang) * (step * 0.5)
                bpts.append((bx, by))
            draw.line([(px + 2, py + 2) for px, py in bpts], fill=hi_col, width=max(2, width - 2))
            draw.line(bpts, fill=ink_col, width=max(2, width - 1), joint="curve")
    draw.line([(px + 2, py + 3) for px, py in pts], fill=hi_col, width=max(2, width - 1))
    draw.line(pts, fill=ink_col, width=width, joint="curve")


def draw_concertina_wire(draw: ImageDraw.ImageDraw, x0: int, x1: int, base_y: int, ink_col, rng: random.Random):
    # Angled iron stanchions
    for px in range(x0 + 28, x1 - 12, 146):
        draw.line([(px, base_y + 12), (px - 12, base_y - 42)], fill=ink_col, width=7)
        draw.line([(px, base_y + 12), (px - 12, base_y - 42)], fill=(98, 90, 84, 255), width=3)
    # Tensioned horizontal spine wire
    draw.line([(x0, base_y - 20), (x1, base_y - 22)], fill=(54, 46, 42, 210), width=3)
    # Overlapping helical figure-8 coils
    step = 26
    for idx, wx in enumerate(range(x0, x1 - 30, step)):
        y_jit = (idx % 2) * 3 - 2
        # Back strand
        b_pts = [
            (wx, base_y + 2),
            (wx + 10, base_y - 36 + y_jit),
            (wx + 28, base_y - 40 + y_jit),
            (wx + 36, base_y - 6),
        ]
        draw.line(b_pts, fill=(68, 60, 56, 225), width=3, joint="curve")
        # Front bold strand
        f_pts = [
            (wx + 4, base_y - 34 + y_jit),
            (wx + 20, base_y + 4),
            (wx + 34, base_y + 2),
            (wx + 40, base_y - 32 + y_jit),
        ]
        draw.line(f_pts, fill=(38, 30, 26, 245), width=4, joint="curve")
        # Sharp comic X-barbs every other loop
        if idx % 2 == 0:
            bx, by = wx + 16, base_y - 38 + y_jit
            draw.line([(bx - 5, by - 5), (bx + 5, by + 5)], fill=ink_col, width=3)
            draw.line([(bx + 5, by - 5), (bx - 5, by + 5)], fill=ink_col, width=3)


def build_local_comic_arena() -> Image.Image:
    rng = random.Random(20261124)
    INK = (30, 20, 14, 255)
    INK_SOFT = (46, 33, 24, 225)

    xx = np.linspace(0.0, 1.0, W2, dtype=np.float32)[None, :]
    canvas_arr = np.zeros((H2, W2, 4), dtype=np.uint8)
    canvas_arr[..., 3] = 255

    # -------------------------------------------------------------------------
    # 1. SKY & ATMOSPHERIC MOUNTAIN PASS HORIZON (y1x = 0..307 -> y2 = 0..614)
    # -------------------------------------------------------------------------
    sky_h = 616
    sy = np.linspace(0.0, 1.0, sky_h, dtype=np.float32)[:, None]
    r_sky = np.interp(sy, [0.0, 0.36, 0.72, 1.0], [30, 52, 108, 154])
    g_sky = np.interp(sy, [0.0, 0.36, 0.72, 1.0], [29, 44, 83, 118])
    b_sky = np.interp(sy, [0.0, 0.36, 0.72, 1.0], [36, 46, 66, 88])
    glow = np.exp(-(((xx - 0.50) / 0.32) ** 2 + ((sy - 0.70) / 0.26) ** 2))
    canvas_arr[:sky_h, :, 0] = np.clip(r_sky + glow * 32.0, 0, 255).astype(np.uint8)
    canvas_arr[:sky_h, :, 1] = np.clip(g_sky + glow * 20.0, 0, 255).astype(np.uint8)
    canvas_arr[:sky_h, :, 2] = np.clip(b_sky + glow * 8.0, 0, 255).astype(np.uint8)

    # -------------------------------------------------------------------------
    # 2. MIDDLE 56% ROAD BASE (y1x = 308..1024 -> y2 = 616..2048)
    # -------------------------------------------------------------------------
    road_y0, road_y1 = 616, 2048
    rh_len = road_y1 - road_y0
    ry = np.linspace(0.0, 1.0, rh_len, dtype=np.float32)[:, None]
    shoulder_curve = 1.0 - 0.115 * (np.abs(ry - 0.5) * 2.0) ** 2.2
    base_r = 152.0 * shoulder_curve - 4.0 * np.sin(xx * math.pi)
    base_g = 138.0 * shoulder_curve - 3.0 * np.sin(xx * math.pi)
    base_b = 116.0 * shoulder_curve - 2.0 * np.sin(xx * math.pi)

    gt_path = FOREST_ART_DIR / "ground_tile.png"
    if gt_path.is_file():
        gt = Image.open(gt_path).convert("L").resize((512, 512), Image.Resampling.BILINEAR)
        gt_arr = np.array(gt, dtype=np.float32)
        gt_norm = (gt_arr - gt_arr.mean()) / (gt_arr.std() + 1e-5)
        tiled = np.tile(gt_norm, (rh_len // 512 + 2, W2 // 512 + 2))[:rh_len, :W2]
        grain_im = Image.fromarray(np.clip((tiled * 18.0 + 128.0), 0, 255).astype(np.uint8), "L").filter(
            ImageFilter.GaussianBlur(radius=4.5)
        )
        grain = (np.array(grain_im, dtype=np.float32) - 128.0) * 0.38
    else:
        grain = 0.0

    # Very subtle warm wash variation (±3.5 RGB) so asphalt stays clean, smooth & cel-shaded!
    np_rng = np.random.default_rng(42)
    macro_small = np_rng.normal(0.0, 5.5, size=(18, 12)).astype(np.float32)
    macro_im = Image.fromarray(np.clip(macro_small * 1.4 + 128.0, 0, 255).astype(np.uint8), "L").resize(
        (W2, rh_len), Image.Resampling.BICUBIC
    )
    macro = (np.array(macro_im, dtype=np.float32) - 128.0) * 0.45

    speck = np_rng.normal(0.0, 2.8, size=(rh_len // 2, W2 // 2)).astype(np.float32)
    speck_up = np.repeat(np.repeat(speck, 2, axis=0), 2, axis=1)[:rh_len, :W2]

    delta = grain + macro + speck_up
    canvas_arr[road_y0:road_y1, :, 0] = np.clip(base_r + delta, 0, 255).astype(np.uint8)
    canvas_arr[road_y0:road_y1, :, 1] = np.clip(base_g + delta * 0.91, 0, 255).astype(np.uint8)
    canvas_arr[road_y0:road_y1, :, 2] = np.clip(base_b + delta * 0.78, 0, 255).astype(np.uint8)

    # -------------------------------------------------------------------------
    # 3. BOTTOM 20% CLIFF CHASM & DOCK BACKDROP (y1x = 1025..1280 -> y2 = 2048..2560)
    # -------------------------------------------------------------------------
    bot_y0 = 2048
    by = np.linspace(0.0, 1.0, H2 - bot_y0, dtype=np.float32)[:, None]
    canvas_arr[bot_y0:, :, 0] = np.interp(by, [0.0, 0.25, 1.0], [56, 36, 20]).astype(np.uint8)
    canvas_arr[bot_y0:, :, 1] = np.interp(by, [0.0, 0.25, 1.0], [46, 29, 17]).astype(np.uint8)
    canvas_arr[bot_y0:, :, 2] = np.interp(by, [0.0, 0.25, 1.0], [38, 25, 15]).astype(np.uint8)

    im = Image.fromarray(canvas_arr, "RGBA")
    dr = ImageDraw.Draw(im, "RGBA")

    # =========================================================================
    # LAYER A: TOP 24% — BILLOWY COMIC CLOUDS, PINE RIDGE & CANYON GORGE
    # =========================================================================
    # Billowy comic twilight clouds (matching crisis_horde_bg.png style, kept dark for HUD readability!)
    cloud_bands = [
        (0, 74, (38, 35, 44, 185), (22, 20, 26, 175)),
        (46, 138, (48, 42, 48, 165), (26, 22, 28, 160)),
        (108, 196, (66, 54, 52, 135), (34, 26, 28, 145)),
    ]
    for cy_top, cy_bot, c_fill, c_ink in cloud_bands:
        scallops = [(0, cy_top)]
        cx = 0
        while cx < W2:
            span = rng.randint(110, 190)
            mid_x = cx + span // 2
            dip_y = cy_bot + rng.randint(-14, 16)
            cusp_y = cy_bot - rng.randint(12, 26)
            scallops.extend([(mid_x, dip_y), (min(W2, cx + span), cusp_y)])
            # Subtle diagonal cloud-belly pen hatching
            draw_hatch_group(dr, cx + 28, dip_y - 22, 4, 12, -8, 14, (24, 20, 26, 90), width=2)
            cx += span
        dr.polygon(scallops + [(W2, cy_top)], fill=c_fill)
        dr.line(scallops[1:], fill=c_ink, width=3, joint="curve")

    # Far mountain pass silhouette with tiny pine-tree tooth horizon (matching crisis_horde_bg.png!)
    far_ridge_ctrl = [
        (0, 246), (130, 204), (270, 232), (400, 194), (545, 252),
        (685, 314), (775, 318), (905, 262), (1055, 206), (1205, 182),
        (1335, 214), (1440, 190),
    ]
    far_toothed = []
    for i in range(len(far_ridge_ctrl) - 1):
        x0, y0 = far_ridge_ctrl[i]
        x1, y1 = far_ridge_ctrl[i + 1]
        steps = max(3, (x1 - x0) // 18)
        for s in range(steps):
            t = s / float(steps)
            bx = x0 + (x1 - x0) * t
            by_r = y0 + (y1 - y0) * t
            tooth = -rng.randint(8, 18) if (s % 2 == 1 and not (640 <= bx <= 820)) else rng.randint(0, 4)
            far_toothed.append((bx, by_r + tooth))
    far_toothed.append(far_ridge_ctrl[-1])
    dr.polygon(far_toothed + [(1440, 500), (0, 500)], fill=(58, 50, 52, 255))
    dr.line(far_toothed, fill=(32, 24, 24, 225), width=4, joint="curve")

    # Winding misty canyon river / broken road far down in the mountain pass gorge (x2 ~ 635..825, y2 ~ 320..480)
    gorge_road = [
        (718, 318), (744, 318), (776, 368), (748, 418), (796, 476),
        (728, 476), (698, 418), (732, 368),
    ]
    dr.polygon(gorge_road, fill=(122, 102, 82, 185))
    dr.line([(718, 318), (732, 368), (698, 418), (728, 476)], fill=(64, 50, 40, 175), width=3, joint="curve")
    dr.line([(744, 318), (776, 368), (748, 418), (796, 476)], fill=(64, 50, 40, 175), width=3, joint="curve")

    # Distant ruined highway viaduct piers & snapped deck in the gorge (with ragged concrete + rebar!)
    dr.polygon([(648, 392), (660, 294), (694, 296), (704, 396)], fill=(78, 68, 62, 245), outline=INK_SOFT, width=3)
    dr.polygon([(784, 402), (794, 306), (826, 304), (838, 404)], fill=(74, 64, 58, 245), outline=INK_SOFT, width=3)
    # Snapped viaduct deck left & right with protruding rebar filaments
    dr.polygon([(580, 286), (718, 282), (710, 298), (696, 304), (580, 306)], fill=(96, 84, 74, 245), outline=INK_SOFT, width=3)
    dr.polygon([(776, 298), (898, 308), (896, 326), (784, 318), (772, 308)], fill=(90, 78, 68, 245), outline=INK_SOFT, width=3)
    for r_y in (286, 293, 299):
        dr.line([(714, r_y), (736, r_y + 4)], fill=(46, 36, 32, 220), width=2)
        dr.line([(776, r_y + 16), (756, r_y + 12)], fill=(46, 36, 32, 220), width=2)

    # Rugged stepped rocky canyon ridges on left & right (natural jagged cliffs, NO straight vertical drop!)
    left_ridge = [
        (0, 300), (148, 274), (308, 304), (468, 338), (602, 372),
        (648, 404), (632, 434), (676, 468), (698, 516), (0, 516),
    ]
    right_ridge = [
        (772, 516), (794, 466), (836, 434), (818, 402), (868, 368),
        (986, 328), (1140, 290), (1292, 266), (1440, 282), (1440, 516),
    ]
    dr.polygon(left_ridge, fill=(102, 76, 54, 255))
    dr.polygon(right_ridge, fill=(106, 78, 55, 255))
    # Shadowed inner canyon cliff faces
    dr.polygon([(560, 362), (602, 372), (648, 404), (632, 434), (676, 468), (698, 516), (590, 516), (576, 430)], fill=(76, 56, 40, 255))
    dr.polygon([(772, 516), (794, 466), (836, 434), (818, 402), (868, 368), (912, 354), (892, 438), (864, 516)], fill=(78, 58, 42, 255))
    # Rock strata & pen cross-hatching on canyon cliffs
    for rx0, ry0, rx1, ry1 in [
        (48, 324, 144, 348), (196, 310, 294, 342), (350, 340, 448, 372),
        (582, 396, 636, 424), (960, 352, 1068, 324), (1140, 316, 1254, 294), (1304, 304, 1412, 320),
    ]:
        dr.line([(rx0, ry0), (rx1, ry1)], fill=(52, 36, 26, 205), width=3)
        draw_hatch_group(dr, rx0 + 10, ry0 + 6, 5, 12, -6, 16, (56, 38, 28, 155), width=2)
    dr.line(wobble_pts(left_ridge[:9], rng, amp=1.8), fill=INK, width=5, joint="curve")
    dr.line(wobble_pts(right_ridge[:9], rng, amp=1.8), fill=INK, width=5, joint="curve")

    # Distant ForestArt autumn broadleaf trees + crisp pine spires strictly inside y2 = 285..475 (y1x <= 237)
    t0_p = FOREST_ART_DIR / "tree_0.png"
    t2_p = FOREST_ART_DIR / "tree_2.png"
    if t0_p.is_file() and t2_p.is_file():
        t_amber = tint_sprite(Image.open(t0_p), 0.78, 0.72, 0.68, sat=0.74)
        t_olive = tint_sprite(Image.open(t2_p), 0.74, 0.74, 0.68, sat=0.68)
        for tx, ty, sc, use_amber in [
            (22, 290, 118, True), (120, 304, 104, False), (216, 316, 112, True),
            (324, 338, 96, False), (1034, 332, 98, True), (1128, 310, 114, False),
            (1238, 292, 122, True), (1344, 302, 108, False),
        ]:
            src_t = t_amber if use_amber else t_olive
            tw = int(src_t.width * (sc / float(src_t.height)))
            im.alpha_composite(src_t.resize((tw, sc), Image.Resampling.LANCZOS), (tx, ty))

    pine_specs = [
        (84, 438, 118, (66, 72, 52, 255), (46, 52, 38, 255)),
        (184, 444, 108, (76, 78, 54, 255), (52, 56, 40, 255)),
        (292, 452, 98, (112, 78, 46, 255), (82, 54, 34, 255)),
        (414, 462, 88, (68, 72, 52, 255), (48, 52, 38, 255)),
        (512, 468, 76, (98, 74, 48, 255), (70, 52, 34, 255)),
        (922, 466, 80, (70, 74, 54, 255), (50, 54, 40, 255)),
        (1008, 458, 94, (116, 80, 48, 255), (84, 56, 36, 255)),
        (1198, 446, 112, (64, 70, 50, 255), (46, 50, 36, 255)),
        (1312, 440, 120, (82, 80, 54, 255), (56, 56, 38, 255)),
        (1408, 446, 106, (112, 76, 46, 255), (80, 54, 34, 255)),
    ]
    for px, pby, ph, pc1, pc2 in pine_specs:
        draw_pine_silhouette(dr, px, pby, ph, pc1, pc2, INK)

    # Warm canyon mist separating distant horizon from the bridge defense parapet
    mist = Image.new("RGBA", (W2, 164), (0, 0, 0, 0))
    md = ImageDraw.Draw(mist)
    for my in range(164):
        a = int(138 * math.sin((my / 163.0) * math.pi) ** 1.4)
        md.line([(0, my), (W2, my)], fill=(172, 148, 118, a))
    im.alpha_composite(mist, (0, 386))
    dr = ImageDraw.Draw(im, "RGBA")

    # =========================================================================
    # LAYER B: UPPER BRIDGE DEFENSE LINE (y1x = 222..307 -> y2 = 444..614)
    # =========================================================================
    draw_concertina_wire(dr, 20, 650, 488, INK, rng)
    draw_concertina_wire(dr, 1044, 1420, 488, INK, rng)

    concrete_blocks = [
        (0, 228), (234, 464), (470, 694),
        (1042, 1248), (1254, 1440),
    ]
    for bx0, bx1 in concrete_blocks:
        top_y = 486 + rng.randint(-3, 3)
        mid_y = 548
        flare_y = 576
        bot_y = 602
        dr.polygon(
            [(bx0 + 6, top_y), (bx1 - 6, top_y), (bx1 - 2, top_y + 18), (bx0 + 2, top_y + 18)],
            fill=(182, 171, 152, 255),
        )
        dr.polygon(
            [(bx0 + 2, top_y + 18), (bx1 - 2, top_y + 18), (bx1 - 2, mid_y), (bx0 + 2, mid_y)],
            fill=(150, 139, 122, 255),
        )
        dr.polygon(
            [(bx0 + 2, mid_y), (bx1 - 2, mid_y), (bx1 + 4, flare_y), (bx0 - 4, flare_y)],
            fill=(166, 155, 136, 255),
        )
        dr.polygon(
            [(bx0 - 4, flare_y), (bx1 + 4, flare_y), (bx1 + 4, bot_y), (bx0 - 4, bot_y)],
            fill=(116, 106, 92, 255),
        )
        # Pen cross-hatching on lower concrete shadow face (matching road_gate.png!)
        draw_hatch_group(dr, bx0 + 16, flare_y + 4, (bx1 - bx0 - 32) // 18, 18, -5, 18, (68, 56, 44, 135), width=2)
        for sx in (bx0 + 54, bx1 - 78):
            if bx1 - bx0 > 160:
                dr.polygon(
                    [(sx, bot_y), (sx + 8, bot_y - 16), (sx + 34, bot_y - 16), (sx + 42, bot_y)],
                    fill=(46, 36, 28, 255),
                    outline=INK,
                    width=3,
                )
        for _ in range(3):
            rx = rng.randint(bx0 + 24, max(bx0 + 30, bx1 - 38))
            rw = rng.randint(18, 38)
            rh = rng.randint(20, 44)
            dr.polygon(
                [(rx, top_y + 18), (rx + rw, top_y + 18), (rx + rw - 6, top_y + 18 + rh), (rx + 4, top_y + 18 + rh - 6)],
                fill=(142, 90, 54, 155),
            )
        draw_crack(dr, (bx0 + (bx1 - bx0) // 2, top_y + 20), 52, 78, rng, (52, 40, 30, 220), (192, 180, 160, 150), width=3)
        dr.line([(bx0 + 2, top_y + 18), (bx1 - 2, top_y + 18)], fill=(68, 56, 44, 190), width=3)
        dr.line([(bx0 + 2, mid_y), (bx1 - 2, mid_y)], fill=(78, 66, 52, 180), width=3)
        dr.line([(bx0 - 4, flare_y), (bx1 + 4, flare_y)], fill=(58, 48, 38, 210), width=3)
        outline_pts = [
            (bx0 + 6, top_y), (bx1 - 6, top_y), (bx1 - 2, top_y + 18),
            (bx1 - 2, mid_y), (bx1 + 4, flare_y), (bx1 + 4, bot_y),
            (bx0 - 4, bot_y), (bx0 - 4, flare_y), (bx0 + 2, mid_y), (bx0 + 2, top_y + 18),
        ]
        dr.line(wobble_pts(outline_pts + [outline_pts[0]], rng, amp=1.2), fill=INK, width=5, joint="curve")

    # Rusted corrugated steel W-beam highway guardrail across the breached bridge gap (x2 = 668..1068)
    for post_x in (696, 816, 936, 1032):
        dr.rectangle((post_x - 10, 474, post_x + 10, 602), fill=(92, 96, 102, 255), outline=INK, width=5)
        dr.rectangle((post_x - 6, 478, post_x + 2, 598), fill=(122, 128, 134, 255))
    rail_y0, rail_y1 = 502, 566
    dr.polygon([(666, rail_y0), (1066, rail_y0 - 4), (1066, rail_y1 - 4), (666, rail_y1)], fill=(134, 146, 156, 255))
    dr.polygon([(666, rail_y0 + 20), (1066, rail_y0 + 16), (1066, rail_y0 + 40), (666, rail_y0 + 44)], fill=(88, 98, 110, 255))
    dr.polygon([(668, rail_y0 + 4), (1064, rail_y0), (1064, rail_y0 + 14), (668, rail_y0 + 18)], fill=(176, 186, 194, 255))
    for rx, ry, rw, rh in [(710, 506, 56, 26), (842, 524, 72, 34), (968, 502, 52, 28)]:
        dr.ellipse((rx, ry, rx + rw, ry + rh), fill=(144, 86, 50, 225), outline=(74, 42, 24, 180), width=2)
    for post_x in (696, 816, 936, 1032):
        dr.ellipse((post_x - 5, rail_y0 + 24, post_x + 5, rail_y0 + 34), fill=(58, 54, 52, 255), outline=INK, width=3)
    dr.line(
        wobble_pts([(666, rail_y0), (1066, rail_y0 - 4), (1066, rail_y1 - 4), (666, rail_y1), (666, rail_y0)], rng, amp=1.4),
        fill=INK,
        width=5,
        joint="curve",
    )

    # Spiked wooden barricades from ForestArt tucked strictly in the upper defense band (y2 <= 608 -> y1x <= 304)
    bar_a_p = FOREST_ART_DIR / "barricade.png"
    bar_b_p = FOREST_ART_DIR / "barricade_b.png"
    if bar_a_p.is_file() and bar_b_p.is_file():
        ba = Image.open(bar_a_p).convert("RGBA")
        bb = Image.open(bar_b_p).convert("RGBA")
        for src, bx, by, bh in [(ba, 594, 472, 134), (bb, 988, 470, 136)]:
            bw = int(src.width * (bh / float(src.height)))
            b_sm = src.resize((bw, bh), Image.Resampling.LANCZOS)
            dr.ellipse((bx + 8, by + bh - 18, bx + bw - 8, by + bh + 6), fill=(38, 28, 20, 135))
            im.alpha_composite(b_sm, (bx, by))

    sandbag_clusters = [
        [(186, 590), (262, 592), (338, 590), (224, 562), (300, 564)],
        [(746, 592), (824, 594), (902, 592), (784, 564), (862, 564), (824, 538)],
        [(1210, 590), (1288, 592), (1248, 562)],
    ]
    for cluster in sandbag_clusters:
        for sx, sy in cluster:
            scol = (172 + rng.randint(-8, 8), 146 + rng.randint(-8, 8), 104 + rng.randint(-6, 6), 255)
            draw_sandbag(dr, sx, sy, 82, 34, scol, INK, rng)

    for sy_off, alpha in [(0, 145), (6, 95), (12, 55), (18, 25)]:
        dr.line([(0, 604 + sy_off), (W2, 604 + sy_off)], fill=(36, 26, 18, alpha), width=6)

    # =========================================================================
    # LAYER C: MIDDLE 56% — 100% UNOBSTRUCTED HIGHWAY & SALVAGED TIMBER DECK
    #          (y1x = 308..1024 -> y2 = 616..2048)
    # =========================================================================
    # Perspective-angled asphalt slab expansion seams (subtle elevated 3/4 road feel!)
    for top_sx, bot_sx in [(368, 314), (1126, 1184)]:
        spts = wobble_pts(
            [(top_sx, 622), ((top_sx + bot_sx) // 2 + rng.randint(-8, 8), 1330), (bot_sx, 2042)],
            rng,
            amp=3.5,
            subdiv=4,
        )
        dr.line([(px + 3, py) for px, py in spts], fill=(170, 156, 134, 95), width=3)
        dr.line(spts, fill=(80, 68, 54, 135), width=3, joint="curve")

    # Subtle horizontal tire-wear tracks across the 3 tactical lanes
    track_overlay = Image.new("RGBA", (W2, H2), (0, 0, 0, 0))
    td = ImageDraw.Draw(track_overlay)
    for lane_y2 in (1050, 1350, 1650):
        td.line([(0, lane_y2 - 26), (W2, lane_y2 - 22)], fill=(94, 82, 66, 22), width=28)
        td.line([(0, lane_y2 + 28), (W2, lane_y2 + 24)], fill=(94, 82, 66, 22), width=28)
    im.alpha_composite(track_overlay)
    dr = ImageDraw.Draw(im, "RGBA")

    # Highway shoulder solid edge lines (interrupted organically over the broken bridge patch)
    for edge_y2 in (668, 1986):
        for seg_x in range(24, W2 - 20, 142):
            if 690 <= seg_x <= 1040:
                continue
            seg_w = rng.randint(94, 124)
            dr.rounded_rectangle(
                (seg_x, edge_y2 - 7, min(W2 - 16, seg_x + seg_w), edge_y2 + 7),
                radius=3,
                fill=(196, 180, 144, 152),
                outline=(92, 80, 62, 118),
                width=2,
            )

    # Faded highway dashed center divider line along y1x = 675 (y2 = 1350)
    for dash_x in range(44, W2 - 40, 168):
        if 680 <= dash_x <= 1070:
            continue
        dw = 92
        dr.rounded_rectangle(
            (dash_x, 1343, dash_x + dw, 1357),
            radius=3,
            fill=(198, 162, 92, 140),
            outline=(98, 78, 46, 110),
            width=2,
        )

    # Faint painted tactical staging markings on the left deployment half (x2 = 96..636, y2 = 876..1820)
    stag_col = (214, 192, 142, 60)
    stag_edge = (82, 68, 48, 50)
    for sy in range(864, 1824, 72):
        dr.polygon(
            [(628, sy), (642, sy - 8), (642, sy + 30), (628, sy + 38)],
            fill=(206, 170, 96, 68),
            outline=stag_edge,
            width=2,
        )
    for cpts in [
        [(104, 920), (104, 876), (156, 876)],
        [(588, 876), (636, 876), (636, 920)],
        [(104, 1776), (104, 1820), (156, 1820)],
        [(588, 1820), (636, 1820), (636, 1776)],
    ]:
        dr.line(cpts, fill=stag_col, width=8, joint="curve")

    # -------------------------------------------------------------------------
    # ORGANIC BROKEN-BRIDGE CHASM PATCH & SALVAGED HEAVY TIMBER DECK
    # Jagged hand-chipped asphalt break lips + staggered bolted planks (100% flat!)
    # -------------------------------------------------------------------------
    # Subtle perspective widen from top (x2 = 746..1026) to bottom (x2 = 716..1054)
    chasm_left = []
    chasm_right = []
    for cy in range(618, 2050, 36):
        t = (cy - 618) / float(2046 - 618)
        lx = int(746 - t * 28 + rng.randint(-14, 14))
        rx = int(1026 + t * 28 + rng.randint(-14, 14))
        chasm_left.append((lx, cy))
        chasm_right.append((rx, cy))
    # Dark recessed sub-deck shadow glimpsed only in 3px crack gaps under chipped asphalt lip
    dr.polygon(chasm_left + chasm_right[::-1], fill=(64, 48, 36, 255))
    # Chipped asphalt bevel lip along jagged break edges
    dr.line([(x - 6, y) for x, y in chasm_left], fill=(172, 158, 136, 150), width=5, joint="curve")
    dr.line([(x + 6, y) for x, y in chasm_right], fill=(172, 158, 136, 150), width=5, joint="curve")
    dr.line(chasm_left, fill=INK_SOFT, width=5, joint="curve")
    dr.line(chasm_right, fill=INK_SOFT, width=5, joint="curve")

    # Staggered heavy salvaged oak bridge planks bolted flush across the jagged asphalt break!
    py = 620
    plank_idx = 0
    while py < 2044:
        ph = min(56 + (plank_idx % 3) * 6, 2044 - py)
        t = (py - 620) / float(2044 - 620)
        # Staggered plank ends overlapping onto the chipped asphalt on both sides!
        stagger_l = ((plank_idx * 37) % 34) - 14
        stagger_r = ((plank_idx * 53) % 34) - 14
        px0 = int(718 - t * 28 + stagger_l)
        px1 = int(1054 + t * 28 + stagger_r)

        tone_shift = ((plank_idx * 19) % 15) - 7
        p_col = (155 + tone_shift, 123 + tone_shift, 87 + tone_shift // 2, 255)

        # Subtle flat contact shadow at plank ends on asphalt
        dr.rectangle((px0 - 3, py + 3, px1 + 4, py + ph + 2), fill=(52, 38, 26, 95))
        # Main flat plank body
        p_poly = wobble_pts([(px0, py), (px1, py), (px1, py + ph - 2), (px0, py + ph - 2), (px0, py)], rng, amp=1.0)
        dr.polygon(p_poly, fill=p_col)
        # Warm top highlight & dark seam line
        dr.line([(px0 + 6, py + 4), (px1 - 6, py + 4)], fill=(178, 146, 106, 150), width=3)
        dr.line([(px0 + 4, py + ph - 5), (px1 - 4, py + ph - 5)], fill=(116, 86, 56, 155), width=3)
        dr.line(p_poly, fill=INK_SOFT, width=4, joint="curve")

        # Occasional staggered plank butt-joint + iron mending plate
        if plank_idx % 3 == 1 and ph >= 44:
            jx = px0 + 120 + ((plank_idx * 61) % 110)
            dr.line([(jx, py + 2), (jx, py + ph - 3)], fill=INK_SOFT, width=4)
            # Small flat iron splice plate across butt joint
            dr.rectangle((jx - 18, py + ph // 2 - 8, jx + 18, py + ph // 2 + 8), fill=(112, 106, 100, 235), outline=INK_SOFT, width=2)
            dr.ellipse((jx - 12, py + ph // 2 - 3, jx - 6, py + ph // 2 + 3), fill=(62, 56, 52, 240))
            dr.ellipse((jx + 6, py + ph // 2 - 3, jx + 12, py + ph // 2 + 3), fill=(62, 56, 52, 240))

        # Delicate horizontal wood grain & knot lines
        for g_n in range(2):
            gy = py + 15 + g_n * 20 + rng.randint(-3, 3)
            if gy < py + ph - 7:
                gx0 = px0 + rng.randint(24, 80)
                gx1 = px1 - rng.randint(24, 80)
                dr.line([(gx0, gy), (gx1, gy)], fill=(92, 66, 42, 110), width=2)

        # Dual flat anchor bolts fastening each plank end into the bridge stringer
        for bx in (px0 + 18, px1 - 18):
            for by_off in (-10, 10):
                if py + ph // 2 + by_off < py + ph - 6 and py + ph // 2 + by_off > py + 6:
                    dr.ellipse(
                        (bx - 4, py + ph // 2 + by_off - 4, bx + 4, py + ph // 2 + by_off + 4),
                        fill=(78, 70, 64, 235),
                        outline=(40, 30, 22, 200),
                        width=2,
                    )

        py += ph
        plank_idx += 1

    # Two longitudinal flat steel runner straps bolted over the bridge planks (angled subtly with perspective!)
    for top_rx, bot_rx in [(778, 752), (994, 1020)]:
        strap_poly = [(top_rx - 8, 622), (top_rx + 8, 622), (bot_rx + 9, 2042), (bot_rx - 9, 2042)]
        dr.polygon(strap_poly, fill=(112, 106, 100, 210), outline=(48, 38, 30, 205), width=3)
        for s_t in np.linspace(0.04, 0.96, 16):
            sx_c = int(top_rx + (bot_rx - top_rx) * s_t)
            sy_c = int(622 + (2042 - 622) * s_t)
            dr.ellipse((sx_c - 4, sy_c - 4, sx_c + 4, sy_c + 4), fill=(72, 66, 62, 235))

    # -------------------------------------------------------------------------
    # FLAT ROAD CRACKS, SUBTLE SHOULDER HATCHING, PEBBLES & AUTUMN LEAVES
    # -------------------------------------------------------------------------
    # Comic pen cross-hatching along extreme top & bottom asphalt shoulders
    for hx in range(20, W2 - 40, 88):
        draw_hatch_group(dr, hx, 622, 3, 12, -6, 16, (72, 58, 44, 85), width=2)
        draw_hatch_group(dr, hx + 24, 2024, 3, 12, -6, 18, (68, 54, 40, 95), width=2)

    crack_specs = [
        ((164, 712), 136, 28),
        ((486, 760), 158, 152),
        ((232, 1188), 122, -18),
        ((548, 1540), 144, 24),
        ((186, 1884), 150, -26),
        ((1188, 748), 152, 146),
        ((1290, 1210), 138, -162),
        ((1164, 1690), 162, 32),
        ((1310, 1916), 126, -148),
    ]
    for c_start, c_len, c_ang in crack_specs:
        draw_crack(dr, c_start, c_len, c_ang, rng, (64, 52, 40, 200), (174, 160, 138, 130), width=3)

    pebble_zones = (
        [(rng.randint(36, 1404), rng.randint(632, 756)) for _ in range(24)]
        + [(rng.randint(36, 1404), rng.randint(1914, 2032)) for _ in range(24)]
        + [(rng.randint(60, 1380), rng.randint(780, 1890)) for _ in range(14)]
    )
    for px, py in pebble_zones:
        pw = rng.randint(6, 12)
        ph = rng.randint(4, 8)
        pcol = rng.choice([(122, 110, 94, 215), (166, 152, 130, 210), (104, 94, 80, 210)])
        dr.ellipse((px, py, px + pw, py + ph), fill=pcol, outline=(52, 40, 30, 175), width=2)

    leaf_cols = [(186, 104, 48, 225), (202, 138, 54, 225), (158, 76, 42, 220)]
    for _ in range(28):
        lx = rng.randint(32, 1408)
        ly = rng.choice([rng.randint(628, 748), rng.randint(1926, 2034)])
        ang = rng.uniform(0, math.tau)
        lw, lh = rng.randint(12, 18), rng.randint(7, 11)
        dx, dy = math.cos(ang) * lw * 0.5, math.sin(ang) * lw * 0.5
        nx, ny = -math.sin(ang) * lh * 0.5, math.cos(ang) * lh * 0.5
        lpoly = [(lx - dx, ly - dy), (lx + nx, ly + ny), (lx + dx, ly + dy), (lx - nx, ly - ny)]
        dr.polygon(lpoly, fill=rng.choice(leaf_cols), outline=(54, 34, 22, 195), width=2)

    # =========================================================================
    # LAYER D: BOTTOM 20% — BROKEN CONCRETE CURB, CAUTION STRIPES & CLIFF ABYSS
    #          (y1x = 1025..1280 -> y2 = 2050..2560)
    # =========================================================================
    for sy_off, alpha in [(0, 45), (5, 85), (10, 135)]:
        dr.line([(0, 2038 + sy_off), (W2, 2038 + sy_off)], fill=(38, 28, 20, alpha), width=5)

    curb_segs = [(0, 284), (292, 596), (604, 706), (1064, 1244), (1252, 1440)]
    for cx0, cx1 in curb_segs:
        dr.polygon(
            [(cx0, 2050), (cx1, 2050), (cx1 - 4, 2088), (cx0 + 4, 2088)],
            fill=(154, 142, 124, 255),
        )
        dr.polygon(
            [(cx0 + 4, 2088), (cx1 - 4, 2088), (cx1 - 6, 2130), (cx0 + 6, 2130)],
            fill=(106, 96, 82, 255),
        )
        draw_hatch_group(dr, cx0 + 18, 2094, max(2, (cx1 - cx0 - 36) // 20), 20, -6, 28, (58, 46, 36, 145), width=2)
        dr.line([(cx0 + 6, 2055), (cx1 - 6, 2055)], fill=(184, 172, 152, 185), width=4)
        dr.line([(cx0 + 4, 2088), (cx1 - 4, 2088)], fill=(52, 42, 32, 220), width=4)
        draw_crack(dr, ((cx0 + cx1) // 2, 2054), 48, 82, rng, (48, 38, 28, 220), (178, 166, 146, 130), width=3)
        c_out = [(cx0, 2050), (cx1, 2050), (cx1 - 6, 2130), (cx0 + 6, 2130)]
        dr.line(wobble_pts(c_out + [c_out[0]], rng, amp=1.3), fill=INK, width=5, joint="curve")

    # Flat weathered yellow-and-black diagonal caution hazard plates
    for hx0, hx1 in [(108, 268), (710, 1060), (1084, 1228)]:
        hy0, hy1 = 2054, 2106
        plate = Image.new("RGBA", (hx1 - hx0, hy1 - hy0), (46, 40, 36, 255))
        pd = ImageDraw.Draw(plate)
        pw_p, ph_p = plate.size
        for sx in range(-ph_p, pw_p + ph_p, 44):
            pd.polygon(
                [(sx, ph_p), (sx + 22, ph_p), (sx + 22 + ph_p, 0), (sx + ph_p, 0)],
                fill=(196, 150, 56, 240),
            )
        pd.rectangle((0, ph_p - 14, pw_p, ph_p), fill=(36, 28, 22, 105))
        im.alpha_composite(plate, (hx0, hy0))
        dr = ImageDraw.Draw(im, "RGBA")
        dr.rectangle((hx0, hy0, hx1, hy1), outline=INK, width=5)

    # Heavy riveted steel bridge edge I-beam girder (y2 = 2128..2204)
    dr.rectangle((0, 2128, W2, 2204), fill=(68, 66, 68, 255), outline=INK, width=5)
    dr.rectangle((0, 2136, W2, 2150), fill=(94, 90, 90, 255))
    dr.rectangle((0, 2188, W2, 2202), fill=(48, 44, 46, 255))
    for rx in range(36, W2, 72):
        dr.ellipse((rx - 6, 2162, rx + 6, 2174), fill=(92, 86, 82, 255), outline=INK, width=3)
        if rx % 216 == 36:
            dr.polygon([(rx - 5, 2174), (rx + 5, 2174), (rx + 2, 2200), (rx - 3, 2198)], fill=(122, 72, 42, 185))

    # Shadowed steel bridge under-truss X-girders + gusset plates in the chasm (drawn behind cliff rims!)
    truss_beams = [
        ((460, 2206), (740, 2556)),
        ((740, 2206), (460, 2556)),
        ((740, 2206), (1020, 2556)),
        ((1020, 2206), (740, 2556)),
        ((740, 2206), (740, 2560)),
        ((450, 2376), (1030, 2376)),
    ]
    for (tx0, ty0), (tx1, ty1) in truss_beams:
        dr.line([(tx0, ty0), (tx1, ty1)], fill=INK, width=24)
        dr.line([(tx0, ty0), (tx1, ty1)], fill=(44, 40, 42, 255), width=13)
    for gx, gy in [(740, 2222), (600, 2376), (740, 2376), (880, 2376)]:
        dr.polygon([(gx - 22, gy - 16), (gx + 22, gy - 16), (gx + 26, gy + 16), (gx - 26, gy + 16)], fill=(54, 48, 50, 255), outline=INK, width=4)

    # Jagged cel-shaded & cross-hatched cliff-edge stone walls framing left & right bottom dock
    left_cliff = [
        (0, 2204), (496, 2204), (458, 2266), (484, 2328), (434, 2408),
        (472, 2486), (428, 2560), (0, 2560),
    ]
    right_cliff = [
        (984, 2204), (1440, 2204), (1440, 2560), (1016, 2560),
        (978, 2482), (1024, 2404), (968, 2324), (1008, 2262),
    ]
    dr.polygon(left_cliff, fill=(54, 43, 35, 255))
    dr.polygon(right_cliff, fill=(52, 42, 34, 255))
    # Darker lower cliff shadow steps
    dr.polygon([(0, 2350), (464, 2362), (434, 2408), (472, 2486), (428, 2560), (0, 2560)], fill=(38, 30, 25, 255))
    dr.polygon([(990, 2356), (1440, 2348), (1440, 2560), (1016, 2560), (978, 2482), (1024, 2404)], fill=(36, 29, 24, 255))
    for fpts in [
        [(28, 2248), (194, 2268), (344, 2240), (458, 2266)],
        [(54, 2354), (242, 2382), (418, 2352)],
        [(84, 2468), (276, 2492), (432, 2466)],
        [(1008, 2262), (1168, 2238), (1326, 2268), (1420, 2252)],
        [(1012, 2368), (1188, 2348), (1374, 2384)],
        [(1010, 2478), (1204, 2456), (1390, 2488)],
    ]:
        dr.line(fpts, fill=(78, 62, 50, 185), width=4, joint="curve")
        dr.line([(px, py + 6) for px, py in fpts], fill=INK, width=5, joint="curve")
        draw_hatch_group(dr, int(fpts[0][0]) + 18, int(fpts[0][1]) + 10, 6, 22, -8, 24, (26, 20, 16, 175), width=2)

    dr.line(wobble_pts(left_cliff[:7], rng, amp=2.0), fill=INK, width=6, joint="curve")
    dr.line(wobble_pts([right_cliff[0]] + right_cliff[3:][::-1], rng, amp=2.0), fill=INK, width=6, joint="curve")

    # Gentle top & bottom vignette for HUD contrast
    vig = Image.new("RGBA", (W2, H2), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vig)
    for y in range(220):
        a = int(76 * (1.0 - y / 220.0) ** 1.6)
        vd.line([(0, y), (W2, y)], fill=(16, 14, 18, a))
    for y in range(2220, H2):
        t = (y - 2220) / float(H2 - 2220)
        a = int(112 * (t ** 1.4))
        vd.line([(0, y), (W2, y)], fill=(14, 11, 10, a))
    im.alpha_composite(vig)

    return im.convert("RGB").resize((W, H), Image.Resampling.LANCZOS)


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    arena = try_vertex_arena()
    used_vertex = arena is not None
    if arena is None:
        arena = build_local_comic_arena()
    arena = arena.convert("RGB")
    assert arena.size == (W, H), f"Expected {(W, H)}, got {arena.size}"
    arena.save(OUT_PNG, "PNG", optimize=True)
    print(f"Saved {OUT_PNG} ({arena.size}, mode={arena.mode}, vertex={used_vertex})")


if __name__ == "__main__":
    main()
