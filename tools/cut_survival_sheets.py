#!/usr/bin/env python3
"""
将 4 张末日生存扩展合图按网格白缝切格 + birefnet-general 抠图 + 统一 PPU=256 比例缩放，
输出到 Assets/Mori/ForestSlice/Resources/ForestArt/。
"""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage
from rembg import new_session, remove

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")
OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHK = BRAIN / "scratch" / "surv_cut_check"


def gutter_cuts(gray: np.ndarray, parts: int, axis: int) -> list[int]:
    length = gray.shape[axis]
    non_white = (gray < 240).astype(np.float32)
    prof = np.convolve(non_white.sum(axis=1 - axis), np.ones(9) / 9.0, mode="same")
    cuts = [0]
    win = max(16, int(length * 0.12))
    for k in range(1, parts):
        t = int(round(length * k / parts))
        lo, hi = max(1, t - win), min(length - 1, t + win)
        cuts.append(lo + int(np.argmin(prof[lo:hi])))
    cuts.append(length)
    return cuts


def clean_and_scale(rgba: Image.Image, target_px: int, axis: str, pad: int = 4) -> Image.Image:
    arr = np.asarray(rgba).copy()
    al = arr[:, :, 3]
    al = np.where(al < 22, 0, al).astype(np.uint8)
    mask = al > 0
    lab, n = ndimage.label(mask)
    if n > 1:
        cnt = ndimage.sum(mask, lab, range(1, n + 1))
        mx = float(cnt.max())
        keep = np.zeros(n + 1, bool)
        for i, c in enumerate(cnt, 1):
            # 保留同一格内的多部件（如熏肉架+石灶+吊锅、路障+路牌+岗亭），仅去极小噪点
            if c >= max(30.0, mx * 0.004):
                keep[i] = True
        al = np.where(keep[lab], al, 0).astype(np.uint8)
    arr[:, :, 3] = al
    im = Image.fromarray(arr, "RGBA")
    bb = im.getbbox()
    if bb:
        im = im.crop(bb)
    w, h = im.size
    s = target_px / float(h if axis == "h" else w)
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
    for r in range(rows):
        for c in range(cols):
            name, tgt, axis = specs[idx]
            idx += 1
            cell = im.crop((xs[c] + 2, ys[r] + 2, xs[c + 1] - 2, ys[r + 1] - 2))
            matted = remove(cell, session=sess)
            final = clean_and_scale(matted, tgt, axis)
            final.save(OUT / f"{name}.png", "PNG")
            chk = Image.new("RGBA", final.size, (255, 0, 255, 255))
            chk.alpha_composite(final)
            chk.convert("RGB").save(CHK / f"{name}.jpg", quality=88)
            print(f"  [ok] {name}.png -> {final.size}", flush=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    CHK.mkdir(parents=True, exist_ok=True)
    sess = new_session("birefnet-general", providers=["CPUExecutionProvider"])

    process_grid(
        sess,
        BRAIN / "forest_buildings_tier_sheet_1795051472417.jpg",
        2, 2,
        [
            ("shelter_lv2", 480, "w"),
            ("shelter_lv3", 530, "w"),
            ("sawmill_shed", 410, "w"),
            ("smokehouse_kitchen", 390, "w"),
        ],
    )

    process_grid(
        sess,
        BRAIN / "forest_relics_repair_sheet_1795051507388.jpg",
        3, 2,
        [
            ("ruin_generator", 300, "w"),
            ("ruin_truck", 390, "w"),
            ("ruin_well", 310, "w"),
            ("generator_active", 320, "w"),
            ("truck_active", 410, "w"),
            ("hunter_tower", 460, "h"),
        ],
    )

    process_grid(
        sess,
        BRAIN / "forest_road_wildlife_sheet_1795051551915.jpg",
        3, 2,
        [
            ("road_gate", 450, "w"),
            ("deer", 165, "h"),
            ("boar", 150, "w"),
            ("zombie_0", 156, "h"),
            ("zombie_1", 160, "h"),
            ("loot_pile", 260, "w"),
        ],
    )

    process_grid(
        sess,
        BRAIN / "forest_city_sites_sheet_1795051570568.jpg",
        2, 2,
        [
            ("city_market", 360, "w"),
            ("city_pharmacy", 360, "w"),
            ("city_garage", 360, "w"),
            ("city_depot", 360, "w"),
        ],
    )
    print("ALL 20 SURVIVAL ASSETS READY!", flush=True)


if __name__ == "__main__":
    main()
