#!/usr/bin/env python3
"""
将 5 张 UI 图标、Dock/配方、漫画半身立绘、尸潮危机海报与丧尸城街区物件合图
切格 + birefnet-general 抠图 + 缩放输出到 Assets/Mori/ForestSlice/Resources/ForestArt/。
"""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage
from rembg import new_session, remove

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")
OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHK = BRAIN / "scratch" / "ui_cut_check"


def gutter_cuts(gray: np.ndarray, parts: int, axis: int) -> list[int]:
    length = gray.shape[axis]
    non_white = (gray < 240).astype(np.float32)
    prof = np.convolve(non_white.sum(axis=1 - axis), np.ones(9) / 9.0, mode="same")
    cuts = [0]
    win = max(16, int(length * 0.11))
    for k in range(1, parts):
        t = int(round(length * k / parts))
        lo, hi = max(1, t - win), min(length - 1, t + win)
        cuts.append(lo + int(np.argmin(prof[lo:hi])))
    cuts.append(length)
    return cuts


def clean_and_scale(rgba: Image.Image, target_px: int, axis: str = "max", pad: int = 4) -> Image.Image:
    arr = np.asarray(rgba).copy()
    al = arr[:, :, 3]
    al = np.where(al < 24, 0, al).astype(np.uint8)
    mask = al > 0
    lab, n = ndimage.label(mask)
    if n > 1:
        cnt = ndimage.sum(mask, lab, range(1, n + 1))
        mx = float(cnt.max())
        keep = np.zeros(n + 1, bool)
        for i, c in enumerate(cnt, 1):
            if c >= max(24.0, mx * 0.003):
                keep[i] = True
        al = np.where(keep[lab], al, 0).astype(np.uint8)
    arr[:, :, 3] = al
    im = Image.fromarray(arr, "RGBA")
    bb = im.getbbox()
    if bb:
        im = im.crop(bb)
    w, h = im.size
    denom = float(max(w, h) if axis == "max" else (h if axis == "h" else w))
    s = target_px / max(1.0, denom)
    im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
    out = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    return out


def process_grid(sess, src_path: Path, cols: int, rows: int, specs: list[tuple[str, int, str]]):
    im = Image.open(src_path).convert("RGB")
    gray = np.asarray(im.convert("L"))
    xs = gutter_cuts(gray, cols, axis=1)
    ys = gutter_cuts(gray, rows, axis=0)
    idx = 0
    tiles = []
    for r in range(rows):
        for c in range(cols):
            name, tgt, axis = specs[idx]
            idx += 1
            cell = im.crop((xs[c] + 3, ys[r] + 3, xs[c + 1] - 3, ys[r + 1] - 3))
            matted = remove(cell, session=sess)
            final = clean_and_scale(matted, tgt, axis)
            final.save(OUT / f"{name}.png", "PNG")
            chk = Image.new("RGBA", (tgt + 16, tgt + 16), (48, 42, 38, 255))
            ox = (chk.width - final.width) // 2
            oy = (chk.height - final.height) // 2
            chk.paste(final, (max(0, ox), max(0, oy)), final)
            tiles.append(chk.convert("RGB"))
            print(f"  [ok] {name}.png -> {final.size}", flush=True)
    return tiles


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    CHK.mkdir(parents=True, exist_ok=True)
    sess = new_session("birefnet-general", providers=["CPUExecutionProvider"])

    # 1. 16 个核心资源与交互图标
    t1 = process_grid(
        sess,
        BRAIN / "ui_icons_res_fx_sheet_1795067163846.jpg",
        4, 4,
        [
            ("ico_wood", 128, "max"),
            ("ico_plank", 128, "max"),
            ("ico_water", 128, "max"),
            ("ico_rawfood", 128, "max"),
            ("ico_ration", 128, "max"),
            ("ico_scrap", 128, "max"),
            ("ico_parts", 128, "max"),
            ("ico_xp_star", 128, "max"),
            ("ico_coin", 128, "max"),
            ("ico_glove", 136, "max"),
            ("ico_hammer", 136, "max"),
            ("ico_check", 120, "max"),
            ("ico_dust", 220, "max"),
            ("ico_fish_cast", 136, "max"),
            ("ico_wheel_go", 136, "max"),
            ("ico_clipboard", 136, "max"),
        ],
    )

    # 2. 12 个底部漫画 Dock / 锁 / 返回键 / 工坊配方与溪鱼
    t2 = process_grid(
        sess,
        BRAIN / "ui_dock_and_recipes_sheet_1795067254610.jpg",
        4, 3,
        [
            ("dock_defense", 148, "max"),
            ("dock_build", 148, "max"),
            ("dock_heroes", 148, "max"),
            ("dock_bag", 148, "max"),
            ("dock_radio", 148, "max"),
            ("dock_map", 148, "max"),
            ("ico_lock", 112, "max"),
            ("btn_back_red", 136, "max"),
            ("rec_beam", 128, "max"),
            ("rec_stakes", 128, "max"),
            ("rec_stew", 128, "max"),
            ("fx_fish_big", 160, "max"),
        ],
    )

    # 3. 6 个角色半身漫画立绘
    process_grid(
        sess,
        BRAIN / "comic_busts_portraits_sheet_1795067422105.jpg",
        3, 2,
        [
            ("bust_leader", 380, "h"),
            ("bust_xiaobu", 380, "h"),
            ("bust_hunter", 380, "h"),
            ("bust_cook", 380, "h"),
            ("bust_driver", 380, "h"),
            ("bust_zombie_boss", 380, "h"),
        ],
    )

    # 4. 6 个丧尸城开车搜刮街区等轴距物件与车斗叠货包
    process_grid(
        sess,
        BRAIN / "city_scavenge_props_sheet_1795067682322.jpg",
        3, 2,
        [
            ("city_crate_food", 220, "w"),
            ("city_crate_scrap", 220, "w"),
            ("city_crate_med", 210, "w"),
            ("city_wreck_blocker", 340, "w"),
            ("city_barrel_boom", 135, "h"),
            ("cargo_stack_unit", 170, "w"),
        ],
    )

    # 5. 尸潮危机全屏竖版漫画海报（无需抠图，缩放到 720x1280 适配 UI）
    poster = Image.open(BRAIN / "crisis_poster_horde_9x16_1795067608110.jpg").convert("RGB")
    poster = poster.resize((720, 1280), Image.LANCZOS)
    poster.save(OUT / "crisis_horde_bg.png", "PNG")
    print("  [ok] crisis_horde_bg.png -> (720, 1280)", flush=True)

    # 拼一张快速质检板
    all_icons = t1 + t2
    cols = 7
    rows = (len(all_icons) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 152, rows * 152), (32, 28, 25))
    for i, tile in enumerate(all_icons):
        t_rs = tile.resize((144, 144), Image.LANCZOS)
        sheet.paste(t_rs, ((i % cols) * 152 + 4, (i // cols) * 152 + 4))
    sheet.save(CHK / "ui_icons_contact.jpg", quality=88)
    print("ALL 41 NEW UI & CITY ASSETS READY!", flush=True)


if __name__ == "__main__":
    main()
