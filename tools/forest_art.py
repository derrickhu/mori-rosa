#!/usr/bin/env python3
"""
深山开荒正式素材后处理（本地 PIL/rembg，不调用任何付费 API）。
原图来自 Antigravity 内置生图，放在 RAW 目录；输出到 Unity Resources/ForestArt。

用法：/usr/bin/python3 tools/forest_art.py <step> ...
  tile   <raw> <name> [--height]      生成四方连续平铺图（--height: A 通道写入树冠高度）
  sheet  <raw> <name1,name2,...> <targetPx1,targetPx2,...> [--axis=h|w]
                                      白底合图 → rembg → 连通域拆分（按行优先排序）→ 按目标像素尺寸缩放
  single <raw> <name> <targetPx> [--axis=h|w]
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHECK = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65/scratch/forest_art_check")


# ---------------------------------------------------------------- 平铺图
def make_seamless(im: Image.Image, size: int) -> Image.Image:
    """取中心正方形 → 缩放 → 与自身半周期偏移版本按十字羽化遮罩混合，消除接缝。"""
    w, h = im.size
    s = min(w, h)
    sq = im.crop(((w - s) // 2, (h - s) // 2, (w + s) // 2, (h + s) // 2)).resize((size, size), Image.LANCZOS)
    a = np.asarray(sq).astype(np.float32)
    # 分轴处理：先左右连续，再上下连续。偏移版的接缝正好落在遮罩 = 1 的中线上，被原图盖住。
    t = np.linspace(0, 1, size, dtype=np.float32)
    edge = np.clip(np.minimum(t, 1 - t) * 2 * 2.2, 0, 1)  # 中心 1，边缘 0
    shape = (1, size) + (1,) * (a.ndim - 2)
    mx = edge.reshape(shape)
    a = a * mx + np.roll(a, size // 2, axis=1) * (1 - mx)
    my = edge.reshape((size, 1) + (1,) * (a.ndim - 2))
    out = a * my + np.roll(a, size // 2, axis=0) * (1 - my)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), sq.mode)


def step_tile(raw: str, name: str, height: bool, size: int = 512, crop=None):
    im = Image.open(raw).convert("RGB")
    if crop:
        im = im.crop(crop)
    sm = make_seamless(im, size)
    arr = np.asarray(sm).astype(np.float32)
    lum = arr @ np.array([0.3, 0.55, 0.15], dtype=np.float32)
    if height:
        # 树冠高度：亮=高、暗缝=低；平滑后拉伸到 0..1
        hmap = ndimage.gaussian_filter(lum, 3, mode="wrap")
        lo, hi = np.percentile(hmap, 4), np.percentile(hmap, 96)
        hmap = np.clip((hmap - lo) / max(1, hi - lo), 0, 1)
    else:
        hmap = ndimage.gaussian_filter(lum, 6, mode="wrap")
        lo, hi = hmap.min(), hmap.max()
        hmap = (hmap - lo) / max(1, hi - lo)
    rgba = np.dstack([arr, hmap * 255]).astype(np.uint8)
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(OUT / f"{name}.png")
    # 检查图：2×2 平铺看接缝
    chk = Image.new("RGB", (size * 2, size * 2))
    for dx in (0, size):
        for dy in (0, size):
            chk.paste(sm, (dx, dy))
    CHECK.mkdir(parents=True, exist_ok=True)
    chk.save(CHECK / f"{name}_tiled.jpg", quality=85)
    print("tile ->", name)


# ---------------------------------------------------------------- 抠图拆分
_session = None


def matte(im: Image.Image) -> Image.Image:
    global _session
    from rembg import new_session, remove
    if _session is None:
        _session = new_session("birefnet-general", providers=["CPUExecutionProvider"])
    return remove(im, session=_session)


def components(rgba: Image.Image, min_ratio: float = 0.02, expected: int = 0):
    a = np.asarray(rgba)[:, :, 3]
    mask = a > 40
    # 稍微膨胀，把同一个物件里断开的小块（烟、水桶）连起来
    grown = ndimage.binary_dilation(mask, iterations=6)
    lab, n = ndimage.label(grown)
    if n == 0:
        return []
    sizes = ndimage.sum(mask, lab, range(1, n + 1))
    keep = [i + 1 for i, s in enumerate(sizes) if s >= sizes.max() * min_ratio]
    if expected and len(keep) > expected:
        keep = sorted(keep, key=lambda i: -sizes[i - 1])[:expected]
    boxes = []
    for i in keep:
        ys, xs = np.where(lab == i)
        boxes.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1, i))
    # 行优先：先按中心 y 分行（行高容差 = 平均高度的一半），行内按 x
    hs = np.mean([b[3] - b[1] for b in boxes])
    boxes.sort(key=lambda b: (b[1] + b[3]) / 2)
    rows, cur = [], [boxes[0]]
    for b in boxes[1:]:
        if abs((b[1] + b[3]) / 2 - (cur[-1][1] + cur[-1][3]) / 2) < hs * 0.5:
            cur.append(b)
        else:
            rows.append(cur)
            cur = [b]
    rows.append(cur)
    ordered = [b for r in rows for b in sorted(r, key=lambda b: b[0])]
    return [(b, lab == b[4]) for b in ordered]


def finish(rgba: Image.Image, target: int, axis: str) -> Image.Image:
    a = np.asarray(rgba).copy()
    al = a[:, :, 3].astype(np.float32)
    al[al < 20] = 0
    a[:, :, 3] = al.astype(np.uint8)
    im = Image.fromarray(a, "RGBA")
    bb = im.getbbox()
    im = im.crop(bb)
    w, h = im.size
    s = target / (h if axis == "h" else w)
    im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
    pad = 4
    out = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    return out


def save_check(name: str, im: Image.Image):
    CHECK.mkdir(parents=True, exist_ok=True)
    bg = Image.new("RGBA", im.size, (255, 0, 255, 255))
    bg.alpha_composite(im)
    bg.convert("RGB").save(CHECK / f"{name}.jpg", quality=85)


def step_sheet(raw: str, names: list[str], targets: list[int], axis: str):
    src = Image.open(raw).convert("RGB")
    rgba = matte(src)
    comps = components(rgba, expected=len(names))
    print(f"found {len(comps)} components, expected {len(names)}")
    arr = np.asarray(rgba)
    OUT.mkdir(parents=True, exist_ok=True)
    for (box, m), name, tgt in zip(comps, names, targets):
        x0, y0, x1, y1, _ = box
        piece = arr[y0:y1, x0:x1].copy()
        piece[:, :, 3] = np.where(m[y0:y1, x0:x1], piece[:, :, 3], 0)
        out = finish(Image.fromarray(piece, "RGBA"), tgt, axis)
        out.save(OUT / f"{name}.png")
        save_check(name, out)
        print(f"  {name}: {out.size}")


def step_single(raw: str, name: str, target: int, axis: str):
    rgba = matte(Image.open(raw).convert("RGB"))
    comps = components(rgba, 0.05)
    arr = np.asarray(rgba).copy()
    if comps:
        keep = np.zeros(arr.shape[:2], bool)
        for _, m in comps:
            keep |= m
        arr[:, :, 3] = np.where(keep, arr[:, :, 3], 0)
    out = finish(Image.fromarray(arr, "RGBA"), target, axis)
    OUT.mkdir(parents=True, exist_ok=True)
    out.save(OUT / f"{name}.png")
    save_check(name, out)
    print(f"  {name}: {out.size}")


def main():
    args = sys.argv[1:]
    axis = "h"
    for a in list(args):
        if a.startswith("--axis="):
            axis = a.split("=")[1]
            args.remove(a)
    height = "--height" in args
    args = [a for a in args if a != "--height"]
    step = args[0]
    if step == "tile":
        crop = None
        for x in args[3:]:
            if x.startswith("crop="):
                crop = tuple(int(v) for v in x[5:].split(","))
        step_tile(args[1], args[2], height, crop=crop)
    elif step == "sheet":
        step_sheet(args[1], args[2].split(","), [int(x) for x in args[3].split(",")], axis)
    elif step == "single":
        step_single(args[1], args[2], int(args[3]), axis)


if __name__ == "__main__":
    main()
