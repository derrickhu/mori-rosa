#!/usr/bin/env python3
"""
将 ForestArt/tree_0..4.png 打包成单张 2048x1024 图集 trees_atlas.png（零额外生图额度）：
- 上排 (Unity y=512..992, cell 320x480)：tree_0..4 林缘向阳树（树根底对齐到 cell 底部 +8px）
- 下排 (Unity y=0..480,   cell 320x480)：tree_deep_0..4 林内纵深树（树干下半压暗融入林下阴影，树冠保持暖色）
- 右下角 (Unity x=1632, y=64, 256x128)：tree_shadow 树根落地接触阴影椭圆
单张图集让整片森林几千棵 Y-Sort 树合批为 1 个 DrawCall。
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART_DIR = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHECK_DIR = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65/scratch/fix_trees")

ATLAS_W, ATLAS_H = 2048, 1024
CELL_W, CELL_H = 320, 480
ROOT_PAD_BOTTOM = 8  # 最低树根像素距离 cell 底边的像素数


def make_deep_variant(im: Image.Image, is_pine: bool) -> Image.Image:
    """将树干区域压暗为林下暖深褐阴影，树冠保持阳光暖色。"""
    arr = np.asarray(im).astype(np.float32).copy()
    h, w, _ = arr.shape
    # y=0 是顶部树冠，y=h-1 是底部树根
    y_norm = np.linspace(0.0, 1.0, h, dtype=np.float32)[:, None]
    # 阔叶树树冠约占上方 60%，松树树冠更低占上方 76%
    t0, t1 = (0.64, 0.84) if is_pine else (0.50, 0.70)
    trunk_mask = np.clip((y_norm - t0) / (t1 - t0), 0.0, 1.0)
    # 树冠轻微压暗 0.90 区分前后层次，树干压暗到 ~0.48 暖深林阴色
    r_mul = (1.0 - trunk_mask) * 0.92 + trunk_mask * 0.46
    g_mul = (1.0 - trunk_mask) * 0.91 + trunk_mask * 0.44
    b_mul = (1.0 - trunk_mask) * 0.88 + trunk_mask * 0.38
    arr[:, :, 0] *= r_mul
    arr[:, :, 1] *= g_mul
    arr[:, :, 2] *= b_mul
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")


def make_shadow_patch(w: int = 256, h: int = 128) -> Image.Image:
    """手绘感双层落地接触阴影：树根正下方深色接触核 + 向右下微偏的柔和投射影。"""
    ss = 2
    im = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # 外圈投射椭圆（略偏右下）
    d.ellipse((34 * ss, 28 * ss, 232 * ss, 102 * ss), fill=(42, 31, 18, 82))
    # 树根正下方紧贴地面的深色接触影
    d.ellipse((58 * ss, 38 * ss, 174 * ss, 88 * ss), fill=(32, 23, 13, 125))
    im = im.filter(ImageFilter.GaussianBlur(radius=4 * ss))
    return im.resize((w, h), Image.LANCZOS)


def main():
    atlas = Image.new("RGBA", (ATLAS_W, ATLAS_H), (0, 0, 0, 0))

    for i in range(5):
        src = Image.open(ART_DIR / f"tree_{i}.png").convert("RGBA")
        a = np.asarray(src)[:, :, 3]
        ys, xs = np.where(a > 20)
        crop = src.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
        cw, ch = crop.size
        assert cw <= CELL_W - 8 and ch <= CELL_H - 16, f"tree_{i} too big: {crop.size}"

        ox = i * CELL_W + (CELL_W - cw) // 2
        # Unity y=512..992 对应 PIL y = ATLAS_H - 512 - CELL_H = 32 .. 512
        top_cell_bottom_pil = ATLAS_H - 512
        oy_lit = top_cell_bottom_pil - ROOT_PAD_BOTTOM - ch
        atlas.paste(crop, (ox, oy_lit), crop)

        # 下排 Unity y=0..480 对应 PIL y = ATLAS_H - CELL_H = 544 .. 1024
        deep = make_deep_variant(crop, is_pine=(i == 3))
        bot_cell_bottom_pil = ATLAS_H
        oy_deep = bot_cell_bottom_pil - ROOT_PAD_BOTTOM - ch
        atlas.paste(deep, (ox, oy_deep), deep)
        print(f"packed tree_{i}: tight={cw}x{ch}, ox={ox}")

    sh = make_shadow_patch(256, 128)
    # Unity Rect(1632, 64, 256, 128) -> PIL y = 1024 - 64 - 128 = 832
    atlas.paste(sh, (1632, ATLAS_H - 64 - 128), sh)

    out_path = ART_DIR / "trees_atlas.png"
    atlas.save(out_path, "PNG")
    CHECK_DIR.mkdir(parents=True, exist_ok=True)
    bg = Image.new("RGBA", atlas.size, (218, 182, 118, 255))
    bg.alpha_composite(atlas)
    bg.convert("RGB").resize((1024, 512), Image.LANCZOS).save(CHECK_DIR / "trees_atlas_preview.jpg", quality=90)
    print("saved ->", out_path)


if __name__ == "__main__":
    main()
