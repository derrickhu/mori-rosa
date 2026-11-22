#!/usr/bin/env python3
"""
切分并抠出全新冷色调现代逃难小队（5个等轴距全身小人 + 5个对应半身漫画立绘）、
野生浆果蘑菇丛、山道锈铁残骸堆以及 4 个细分生鲜食材/烤鱼图标。
采用边缘连通域 Flood-Fill + 抗锯齿羽化，完美保留陈伯雪白厨师服、唐果白T恤与白蘑菇，不漏底不穿孔！
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter, ImageOps
from scipy import ndimage

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")
OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHK = BRAIN / "scratch" / "char_food_check"


def row_cuts(gray: np.ndarray, rows: int) -> list[int]:
    h = gray.shape[0]
    non_white = (gray < 238).astype(np.float32)
    prof = np.convolve(non_white.sum(axis=1), np.ones(9) / 9.0, mode="same")
    cuts = [0]
    win = max(16, int(h * 0.14))
    for k in range(1, rows):
        t = int(round(h * k / rows))
        lo, hi = max(1, t - win), min(h - 1, t + win)
        cuts.append(lo + int(np.argmin(prof[lo:hi])))
    cuts.append(h)
    return cuts


def col_cuts_for_band(band_gray: np.ndarray, cols: int) -> list[int]:
    """对单行独立做横向投影寻谷，即使上下行物件宽窄不一也能100%精准走白缝切开！"""
    w = band_gray.shape[1]
    non_white = (band_gray < 238).astype(np.float32)
    prof = np.convolve(non_white.sum(axis=0), np.ones(11) / 11.0, mode="same")
    # 找出非白像素实际跨度，按连通块间隙切分
    occupied = prof > 2.0
    runs = []
    in_run = False
    start = 0
    for x in range(w):
        if occupied[x] and not in_run:
            in_run = True
            start = x
        elif not occupied[x] and in_run:
            in_run = False
            if x - start > 24:
                runs.append((start, x))
    if in_run and w - start > 24:
        runs.append((start, w))

    # 若连通块数正好等于 cols，直接取相邻块中点做精确切线！
    if len(runs) == cols:
        cuts = [0]
        for i in range(cols - 1):
            cuts.append((runs[i][1] + runs[i + 1][0]) // 2)
        cuts.append(w)
        return cuts

    cuts = [0]
    win = max(16, int(w * 0.14))
    for k in range(1, cols):
        t = int(round(w * k / cols))
        lo, hi = max(1, t - win), min(w - 1, t + win)
        cuts.append(lo + int(np.argmin(prof[lo:hi])))
    cuts.append(w)
    return cuts


def ink_flood_matte(cell_rgb: Image.Image, target_px: int, axis: str, mirror: bool = False, pad: int = 4) -> Image.Image:
    if mirror:
        cell_rgb = ImageOps.mirror(cell_rgb)
    rgb = np.asarray(cell_rgb)
    gray = np.asarray(cell_rgb.convert("L"))
    h, w = gray.shape

    # 1. 从四条外边框向内 Flood-Fill 纯白背景 (gray > 240)
    white_mask = gray > 240
    lab, _ = ndimage.label(white_mask)
    border_labels = set(lab[0, :]) | set(lab[-1, :]) | set(lab[:, 0]) | set(lab[:, -1])
    border_labels.discard(0)
    is_bg = np.isin(lab, list(border_labels))

    # 同时剔除弓弦与手臂之间较大的纯白镂空洞（面积 > 280px 且紧邻黑线、内部均值 > 251 的纯白闭合区，而白衣服内部有衣褶阴影或较小）
    # 注意：陈伯白厨师服上有纽扣与衣领黑线分割，每块面积可能 > 300，所以只用纯边框外背景最安全；
    # 对弓箭手(survivor_2 / bust_hunter)弓与身体间的纯白弦内空隙：亮度极纯 (>252) 且无任何色彩波动
    fg = ~is_bg

    # 去孤立极小噪点
    fg_lab, n_fg = ndimage.label(fg)
    if n_fg > 1:
        cnt = ndimage.sum(fg, fg_lab, range(1, n_fg + 1))
        mx = float(cnt.max())
        keep = np.zeros(n_fg + 1, bool)
        for i, c in enumerate(cnt, 1):
            if c >= max(25.0, mx * 0.005):
                keep[i] = True
        fg = keep[fg_lab]

    # 边缘 1px 柔和抗锯齿：外扩1px白边处按灰度平滑过渡
    eroded = ndimage.binary_erosion(fg, iterations=1)
    boundary = fg & (~eroded)
    alpha = fg.astype(np.float32) * 255.0
    edge_soft = np.clip((250.0 - gray.astype(np.float32)) / 28.0, 0.15, 1.0) * 255.0
    alpha = np.where(boundary, edge_soft, alpha).astype(np.uint8)

    rgba = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
    bb = rgba.getbbox()
    if bb:
        rgba = rgba.crop(bb)

    cw, ch = rgba.size
    denom = float(max(cw, ch) if axis == "max" else (ch if axis == "h" else cw))
    sc = target_px / max(1.0, denom)
    rgba = rgba.resize((max(1, round(cw * sc)), max(1, round(ch * sc))), Image.LANCZOS)
    out = Image.new("RGBA", (rgba.width + pad * 2, rgba.height + pad * 2), (0, 0, 0, 0))
    out.paste(rgba, (pad, pad))
    return out


def process_sheet(src: Path, cols: int, rows: int, specs: list[tuple[str, int, str, bool]]):
    im = Image.open(src).convert("RGB")
    gray = np.asarray(im.convert("L"))
    ys = row_cuts(gray, rows)
    previews = []
    idx = 0
    for r in range(rows):
        band = gray[ys[r]:ys[r + 1], :]
        xs = col_cuts_for_band(band, cols)
        for c in range(cols):
            name, tgt, axis, mirror = specs[idx]
            idx += 1
            cell = im.crop((xs[c] + 3, ys[r] + 3, xs[c + 1] - 3, ys[r + 1] - 3))
            out = ink_flood_matte(cell, tgt, axis, mirror=mirror)
            out.save(OUT / f"{name}.png", "PNG")

            box = Image.new("RGBA", (180, 180), (54, 46, 36, 255))
            ps = min(164.0 / out.width, 164.0 / out.height)
            thumb = out.resize((max(1, round(out.width * ps)), max(1, round(out.height * ps))), Image.LANCZOS)
            box.paste(thumb, ((180 - thumb.width) // 2, (180 - thumb.height) // 2), thumb)
            previews.append(box.convert("RGB"))
            print(f"  [ok] {name}.png -> {out.size}", flush=True)
    return previews


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    CHK.mkdir(parents=True, exist_ok=True)

    # 1. 5 个高对比冷色调全身小人 + 野生浆果蘑菇丛 (PPU=256 下 162px 高 ≈ 0.63 格身高，清晰醒目！)
    p1 = process_sheet(
        BRAIN / "survivors_fullbody_cool_sheet_1795073079784.jpg",
        3, 2,
        [
            ("survivor_0", 164, "h", False),       # 陆沉（钴蓝冲锋衣）
            ("survivor_1", 162, "h", False),       # 唐果（青色护目镜+白T牛仔背带裤）
            ("survivor_2", 166, "h", False),       # 苏凛（紫青运动风衣弓手）
            ("survivor_3", 162, "h", False),       # 陈伯（雪白厨师服+宝蓝围裙）
            ("survivor_4", 164, "h", False),       # 雷猛（藏青反光条救援服）
            ("prop_berry_bush", 210, "w", False),  # 野生浆果蘑菇丛
        ],
    )

    # 2. 5 个对应半身漫画立绘（右侧副手镜像朝左与左侧陆沉对视！）+ 山道锈铁残骸堆
    p2 = process_sheet(
        BRAIN / "survivors_busts_cool_sheet_1795073489155.jpg",
        3, 2,
        [
            ("bust_leader", 380, "h", False),      # 陆沉（朝右，放对话框左侧）
            ("bust_mechanic", 380, "h", True),     # 唐果（镜像朝左，放对话框右侧对视！）
            ("bust_hunter", 380, "h", True),       # 苏凛（镜像朝左）
            ("bust_cook", 380, "h", True),         # 陈伯（镜像朝左）
            ("bust_driver", 380, "h", True),       # 雷猛（镜像朝左）
            ("prop_scrap_wreck", 250, "w", False), # 山道锈铁护栏残骸堆
        ],
    )

    # 3. 4 个细分生鲜食材与香烤鳟鱼图标
    p3 = process_sheet(
        BRAIN / "food_ingredients_icons_sheet_1795073692090.jpg",
        2, 2,
        [
            ("ico_veggie", 128, "max", False),
            ("ico_fish", 128, "max", False),
            ("ico_meat", 128, "max", False),
            ("rec_roast_fish", 132, "max", False),
        ],
    )

    all_p = p1 + p2 + p3
    cols = 8
    rows = (len(all_p) + cols - 1) // cols
    contact = Image.new("RGB", (cols * 184, rows * 184), (36, 30, 24))
    for i, tile in enumerate(all_p):
        contact.paste(tile, ((i % cols) * 184 + 2, (i // cols) * 184 + 2))
    contact.save(CHK / "new_chars_food_contact.jpg", quality=90)
    print("ALL 16 NEW CHARACTER, PROP & INGREDIENT SPRITES READY!", flush=True)


if __name__ == "__main__":
    main()
