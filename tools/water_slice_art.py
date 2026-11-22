#!/usr/bin/env python3
"""
水上验证样板素材处理（本地 PIL/rembg，不调用任何付费生图 API）。
输入：Antigravity 内置生图得到的风格主视觉 keyart_v1.jpg
输出：Assets/Mori/WaterSlice/Resources/WaterArt/*.png
用法：/usr/bin/python3 tools/water_slice_art.py
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65/scratch/gen")
KEYART = BRAIN / "keyart_v1.jpg"
CUT = BRAIN / "cut"
OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/WaterSlice/Resources/WaterArt")

WATER = np.array([76, 108, 140], dtype=np.float32)
UPSCALE = 2  # 主视觉 768 宽，放大 2 倍后按统一 PPU 入库


def tileable_noise(n: int, sigma: float, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    z = rng.standard_normal((n, n)).astype(np.float32)
    z = ndimage.gaussian_filter(z, sigma, mode="wrap")
    z -= z.min()
    z /= max(1e-6, z.max())
    return z


def make_water_tile():
    n = 512
    low = tileable_noise(n, 40, 1) - 0.5
    mid = tileable_noise(n, 8, 2) - 0.5
    rgb = WATER[None, None, :] + low[..., None] * np.array([10, 12, 14]) + mid[..., None] * np.array([4, 5, 6])
    im = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    # 稀疏的深色横向涂鸦笔触（四方连续：画在 3x3 平铺上再取中间）
    big = Image.new("RGBA", (n * 3, n * 3), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    rng = np.random.default_rng(3)
    for _ in range(26):
        x, y = rng.uniform(0, n), rng.uniform(0, n)
        L = rng.uniform(30, 90)
        for ox in (0, n, 2 * n):
            for oy in (0, n, 2 * n):
                pts = []
                for k in range(8):
                    t = k / 7
                    pts.append((ox + x + t * L, oy + y + np.sin(t * 3.0 + x) * 2.5))
                d.line(pts, fill=(40, 62, 92, 70), width=2)
    layer = big.crop((n, n, 2 * n, 2 * n))
    im = Image.alpha_composite(im, layer)
    im.save(OUT / "water_tile.png")


def chevron(w: int, h: int, seed: int, name: str):
    s = 4
    im = Image.new("RGBA", (w * s, h * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = np.random.default_rng(seed)
    cx, cy = w * s / 2, h * s * 0.35
    left = (w * s * 0.06, h * s * 0.85)
    right = (w * s * 0.94, h * s * 0.85)
    # 两笔略带弯曲的白色“人字”波纹，中间粗两头细
    for a, b in ((left, (cx, cy)), ((cx, cy), right)):
        for k in range(12):
            t0, t1 = k / 12, (k + 1) / 12
            p0 = (a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0 + np.sin(t0 * np.pi) * -3 * s)
            p1 = (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1 + np.sin(t1 * np.pi) * -3 * s)
            mid = 1 - abs((t0 if a == left else 1 - t0) - 1.0)
            width = int(s * (1.2 + 2.6 * mid))
            d.line([p0, p1], fill=(232, 240, 248, 235), width=max(1, width))
    im = im.resize((w, h), Image.LANCZOS)
    im.save(OUT / f"{name}.png")


def fish(name: str, seed: int, scale: float):
    s = 4
    w, h = int(120 * scale), int(48 * scale)
    im = Image.new("RGBA", (w * s, h * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    col = (22, 40, 62, 120)
    d.ellipse((w * s * 0.12, h * s * 0.25, w * s * 0.78, h * s * 0.75), fill=col)
    d.polygon([(w * s * 0.72, h * s * 0.5), (w * s * 0.98, h * s * 0.15), (w * s * 0.92, h * s * 0.5), (w * s * 0.98, h * s * 0.85)], fill=col)
    im = im.resize((w, h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.2))
    im.save(OUT / f"{name}.png")


def clean_alpha(rgba: Image.Image, pad: int = 6) -> Image.Image:
    a = np.array(rgba)
    alpha = a[:, :, 3]
    alpha = np.where(alpha < 24, 0, alpha).astype(np.uint8)
    mask = alpha > 0
    lab, num = ndimage.label(mask)
    if num > 1:
        counts = ndimage.sum(mask, lab, range(1, num + 1))
        keep = np.zeros(num + 1, bool)
        mx = counts.max()
        for i, c in enumerate(counts, 1):
            keep[i] = c >= mx * 0.02
        alpha = np.where(keep[lab], alpha, 0).astype(np.uint8)
    a[:, :, 3] = alpha
    im = Image.fromarray(a, "RGBA")
    bb = im.getbbox()
    if not bb:
        return im
    t = im.crop(bb)
    out = Image.new("RGBA", (t.width + pad * 2, t.height + pad * 2), (0, 0, 0, 0))
    out.paste(t, (pad, pad))
    return out


def ruins_background():
    im = Image.open(KEYART).convert("RGB")
    top = im.crop((0, 0, 768, 340))
    top = top.resize((top.width * UPSCALE, top.height * UPSCALE), Image.LANCZOS)
    a = np.array(top).astype(np.float32)
    h = a.shape[0]
    # 下沿 45% 渐隐进水面；整体再往水色靠一点，避免和水面接不上
    fade = np.clip((h - np.arange(h)) / (h * 0.45), 0, 1)[:, None]
    alpha = (fade * 255).astype(np.uint8)
    rgba = np.dstack([a.astype(np.uint8), np.broadcast_to(alpha, a.shape[:2])])
    Image.fromarray(rgba, "RGBA").save(OUT / "bg_ruins.png")


def export_cut(name: str, out_name: str | None = None):
    src = CUT / f"{name}.png"
    if not src.exists():
        print("missing cut", src)
        return
    im = clean_alpha(Image.open(src).convert("RGBA"))
    im.save(OUT / f"{out_name or name}.png")
    print("  cut ->", out_name or name, im.size)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    make_water_tile()
    chevron(70, 26, 1, "fx_ripple_a")
    chevron(48, 18, 2, "fx_ripple_b")
    fish("fx_fish_a", 1, 1.0)
    fish("fx_fish_b", 2, 0.7)
    ruins_background()
    for n in sys.argv[1:] or ["plaza", "raft_fishing", "raft_purifier", "raft_drying", "pad_corner", "walk_straight",
                              "deco_bottle", "deco_barrel", "deco_buoy", "deco_crate", "deco_can"]:
        export_cut(n)
    print("done")


if __name__ == "__main__":
    main()
