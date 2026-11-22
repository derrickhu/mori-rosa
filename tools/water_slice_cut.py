#!/usr/bin/env python3
"""从风格主视觉里按手工多边形 + 水色抠除，拆出独立模块（仅用于验证，正式素材应单独生成）。"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65/scratch/gen")
im = Image.open(BRAIN / "keyart_v1.jpg").convert("RGB")
A = np.array(im).astype(np.float32)
CUT = BRAIN / "cut"
CUT.mkdir(exist_ok=True)

POLYS = {
    "plaza": [(402, 530), (628, 660), (628, 706), (510, 772), (508, 826), (452, 878), (342, 876), (292, 862),
              (236, 862), (160, 822), (148, 770), (126, 712), (118, 676), (128, 660), (190, 636), (300, 584)],
    "pad_corner": [(424, 444), (462, 466), (462, 482), (426, 498), (390, 482), (390, 466)],
    # 一段直栈道（右上方向的环边上，两节：浅木 + 青色）
    "walk_straight": [(474, 472), (562, 520), (562, 548), (548, 554), (462, 506), (462, 486)],
    "raft_fishing": [(560, 850), (610, 868), (640, 900), (668, 930), (640, 958), (600, 962), (560, 945),
                     (515, 920), (505, 900), (530, 876)],
    "raft_purifier": [(260, 942), (300, 948), (330, 980), (372, 1012), (378, 1036), (330, 1078), (290, 1078),
                      (220, 1046), (212, 1028), (240, 1000)],
    "raft_drying": [(400, 1000), (450, 990), (470, 1010), (500, 1048), (548, 1090), (540, 1112),
                    (472, 1162), (440, 1162), (378, 1112), (390, 1090), (396, 1040)],
}

# 漂浮物（小矩形，水色抠除）
BOXES = {
    "deco_buoy": (8, 352, 46, 384),
    "deco_crate": (676, 1132, 728, 1170),
    "deco_barrel": (52, 856, 80, 884),
    "deco_bottle": (90, 872, 118, 912),
    "deco_can": (14, 872, 44, 902),
}


def water_mask(sub: np.ndarray) -> np.ndarray:
    # 局部水色：取块四周边缘像素中位数
    edge = np.concatenate([sub[0], sub[-1], sub[:, 0], sub[:, -1]])
    ref = np.median(edge, axis=0)
    d = np.sqrt(((sub - ref) ** 2).sum(-1))
    return d < 26


def cut(name, box_or_poly, upscale=2):
    if isinstance(box_or_poly, tuple):
        x0, y0, x1, y1 = box_or_poly
        poly = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    else:
        poly = box_or_poly
    xs, ys = [p[0] for p in poly], [p[1] for p in poly]
    x0, y0, x1, y1 = min(xs) - 4, min(ys) - 4, max(xs) + 4, max(ys) + 4
    sub = A[y0:y1, x0:x1]
    m = Image.new("L", (x1 - x0, y1 - y0), 0)
    ImageDraw.Draw(m).polygon([(x - x0, y - y0) for x, y in poly], fill=255)
    pm = np.array(m) > 0
    wm = water_mask(sub)
    wm = ndimage.binary_opening(wm, iterations=1)
    keep = pm & ~wm
    keep = ndimage.binary_closing(keep, iterations=2)
    keep = ndimage.binary_fill_holes(keep) & pm
    lab, n = ndimage.label(keep)
    if n > 1:
        cnt = ndimage.sum(keep, lab, range(1, n + 1))
        big = np.zeros(n + 1, bool)
        for i, c in enumerate(cnt, 1):
            big[i] = c >= cnt.max() * 0.05
        keep = big[lab]
    alpha = Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))
    rgba = Image.fromarray(sub.astype(np.uint8), "RGB").convert("RGBA")
    rgba.putalpha(alpha)
    rgba = rgba.resize((rgba.width * upscale, rgba.height * upscale), Image.LANCZOS)
    rgba.save(CUT / f"{name}.png")
    chk = Image.new("RGBA", rgba.size, (255, 0, 255, 255))
    chk.alpha_composite(rgba)
    chk.convert("RGB").save(CUT / f"{name}_chk.jpg")


for k, v in POLYS.items():
    cut(k, v)
for k, v in BOXES.items():
    cut(k, v)

names = list(POLYS) + list(BOXES)
ims = [Image.open(CUT / f"{n}_chk.jpg") for n in names]
W = sum(i.width for i in ims) + 10 * len(ims)
H = max(i.height for i in ims)
c = Image.new("RGB", (W, H), (40, 40, 40))
x = 0
for i in ims:
    c.paste(i, (x, 0))
    x += i.width + 10
c.save(CUT / "contact.jpg")
print("ok")
