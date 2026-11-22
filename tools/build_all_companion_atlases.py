#!/usr/bin/env python3
"""
一次性构建剩余 3 位伙伴（林砚 / 老陈头 / 大锤）的：
1. 半身立绘 bust_*.png & 静态全身参考 survivor_2/3/4.png
2. 标准 1024x1024 (3x3x320) 骨骼图集 *_puppet_atlas.png
3. 本地 6 姿态正运动学校验图（Idle / Run A / Run B / Action Windup / Action Impact / Carry），确保全员关节零穿帮！
"""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw, ImageOps
from scipy import ndimage
from build_hero_puppet_atlas import (
    BRAIN, OUT, CHK, CELL, PPU,
    matte_crop, matte_crop_rgba, place_in_cell_with_pivot, composite_part
)


def keep_largest_component(rgba: Image.Image) -> Image.Image:
    """只保留最大连通前景块，彻底清除跨行/跨格渗入的零碎杂点（如箭羽尖、裤腰边角）。"""
    arr = np.asarray(rgba).copy()
    fg = arr[:, :, 3] > 20
    lab, n = ndimage.label(fg)
    if n > 1:
        cnt = ndimage.sum(fg, lab, range(1, n + 1))
        best = int(np.argmax(cnt)) + 1
        arr[lab != best, 3] = 0
    return matte_crop_rgba(Image.fromarray(arr, "RGBA"))


def remove_enclosed_white_holes(rgba: Image.Image, min_area: int = 90, y_start_ratio: float = 0.0) -> Image.Image:
    """移除封闭线条内部的纯白背景空洞（例如林砚弓弦与PVC弓身之间的封闭白块）。"""
    arr = np.asarray(rgba).copy()
    h, w, _ = arr.shape
    r = arr[:, :, 0].astype(np.int32)
    g = arr[:, :, 1].astype(np.int32)
    b = arr[:, :, 2].astype(np.int32)
    a = arr[:, :, 3]
    sat = np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b)
    lum = (r + g + b) / 3.0
    yy = np.mgrid[0:h, 0:w][0]
    white_mask = (a > 128) & (lum > 236) & (sat < 20) & (yy >= int(h * y_start_ratio))

    lab, n = ndimage.label(white_mask)
    if n > 0:
        cnt = ndimage.sum(white_mask, lab, range(1, n + 1))
        for idx, area in enumerate(cnt, 1):
            if area >= min_area:
                arr[lab == idx, 3] = 0
    return Image.fromarray(arr, "RGBA")


def close_cut_right_edge(rgba: Image.Image) -> Image.Image:
    """为大锤左腿右侧平切口补上圆润髋角与 3px 深墨褐漫画描边，消除切断感。"""
    arr = np.asarray(rgba).copy()
    h, w, _ = arr.shape
    # 在上半截平切边缘（右边界附近）向内轻微倒角并绘制 3px 深墨褐描边
    ink = (26, 20, 18, 255)
    for y in range(h):
        row_fg = np.where(arr[y, :, 3] > 128)[0]
        if len(row_fg) == 0:
            continue
        rx = int(row_fg[-1])
        # 仅对上 55% 处紧贴最右缘的平切断面补描边（下方靴子本来就有完整描边）
        if y < int(h * 0.56) and rx >= w - 5:
            if y < 14:
                cut_w = max(0, 14 - y)
                arr[y, max(0, rx - cut_w):, 3] = 0
                rx = max(0, rx - cut_w - 1)
            for k in range(3):
                if rx - k >= 0 and arr[y, rx - k, 3] > 60:
                    arr[y, rx - k] = ink
    return Image.fromarray(arr, "RGBA")

UV_HEAD  = (0.48, 0.10)
UV_ACC   = (0.88, 0.58)
UV_TORSO = (0.52, 0.12)
UV_ARM   = (0.50, 0.84)
UV_LEG   = (0.50, 0.86)
UV_TOOL  = (0.46, 0.16)


def save_concept_bust_and_full(concept_name: str, bust_key: str, surv_key: str, full_h: int):
    concept = Image.open(BRAIN / concept_name).convert("RGB")
    cw, ch = concept.size
    full_ref = matte_crop(concept.crop((10, 10, int(cw * 0.46), ch - 10)))
    bust_ref = matte_crop(concept.crop((int(cw * 0.47), 10, cw - 10, ch - 10)))

    bs = 400.0 / bust_ref.height
    bust_out = bust_ref.resize((round(bust_ref.width * bs), 400), Image.LANCZOS)
    bust_pad = Image.new("RGBA", (bust_out.width + 8, bust_out.height + 8), (0, 0, 0, 0))
    bust_pad.paste(bust_out, (4, 4))
    bust_pad.save(OUT / f"{bust_key}.png", "PNG")

    fs = float(full_h) / full_ref.height
    full_out = full_ref.resize((round(full_ref.width * fs), full_h), Image.LANCZOS)
    if surv_key == "survivor_2":
        full_out = remove_enclosed_white_holes(full_out, min_area=35, y_start_ratio=0.38)
    full_pad = Image.new("RGBA", (full_out.width + 8, full_out.height + 8), (0, 0, 0, 0))
    full_pad.paste(full_out, (4, 4))
    full_pad.save(OUT / f"{surv_key}.png", "PNG")
    print(f"Saved {bust_key}.png {bust_pad.size} & {surv_key}.png {full_pad.size}")
    return full_pad


def pack_atlas_and_preview(atlas_key: str, full_ref_im: Image.Image, cells: dict, rig_offsets: dict, preview_name: str):
    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    for (col, row), cim in cells.items():
        atlas.paste(cim, (col * CELL, 1024 - (row + 1) * CELL))
    atlas.save(OUT / f"{atlas_key}.png", "PNG")
    print(f"Saved {atlas_key}.png (1024x1024)")

    preview = Image.new("RGBA", (7 * 240, 340), (222, 186, 122, 255))
    draw = ImageDraw.Draw(preview)
    for x in range(0, 7 * 240, 240):
        draw.line([(x, 0), (x, 340)], fill=(180, 145, 90, 255), width=2)
    ground_y = 286
    draw.line([(0, ground_y), (7 * 240, ground_y)], fill=(150, 118, 70, 255), width=2)
    preview.alpha_composite(full_ref_im, (120 - full_ref_im.width // 2, ground_y - full_ref_im.height))

    poses = [
        (0,    0,   0,    0,    0,   10,   -4,  -78, False, True),
        (-4,   2,  10,   20,  -20,  -18,   18,  -70, False, True),
        (-4,   2,  10,  -20,   20,   22,  -18,  -85, False, True),
        (22, -10, -22,   16,  -12,  148,  136, -135, False, True),
        (-24, 10,  28,  -16,   10,   42,   34, -102, True,  True),
        (-5,   3,  14,   19,  -19,  166,  156,    0, False, False),
    ]

    pelvis = rig_offsets["pelvis"]
    leg_n  = rig_offsets["leg_near"]
    leg_f  = rig_offsets["leg_far"]
    torso  = rig_offsets["torso"]
    head   = rig_offsets["head"]
    acc    = rig_offsets["acc"]
    arm_n  = rig_offsets["arm_near"]
    arm_f  = rig_offsets["arm_far"]
    tool_g = rig_offsets["tool"]

    def rot2d(v, deg):
        rad = math.radians(deg)
        cs, sn = math.cos(rad), math.sin(rad)
        return (v[0] * cs - v[1] * sn, v[0] * sn + v[1] * cs)

    def add2(a, b):
        return (a[0] + b[0], a[1] + b[1])

    for idx, (tA, hA, bA, lnA, lfA, anA, afA, axA, eff, showTool) in enumerate(poses):
        rx = (idx + 1) * 240 + 120
        p_leg_f = add2(pelvis, leg_f)
        p_leg_n = add2(pelvis, leg_n)
        p_torso = add2(pelvis, torso)
        p_arm_f = add2(p_torso, rot2d(arm_f, tA))
        p_head  = add2(p_torso, rot2d(head, tA))
        tot_hA  = tA + hA
        p_acc   = add2(p_head, rot2d(acc, tot_hA))
        p_arm_n = add2(p_torso, rot2d(arm_n, tA))
        tot_anA = tA + anA
        p_tool  = add2(p_arm_n, rot2d(tool_g, tot_anA))

        composite_part(preview, rx, ground_y, p_leg_f, cells[(1, 0)], UV_LEG, lfA)
        composite_part(preview, rx, ground_y, p_arm_f, cells[(2, 1)], UV_ARM, tA + afA)
        composite_part(preview, rx, ground_y, p_leg_n, cells[(0, 0)], UV_LEG, lnA)
        composite_part(preview, rx, ground_y, p_torso, cells[(0, 1)], UV_TORSO, tA)
        composite_part(preview, rx, ground_y, p_acc,   cells[(2, 2)], UV_ACC, tot_hA + bA)
        composite_part(preview, rx, ground_y, p_head,  cells[(1, 2)] if eff else cells[(0, 2)], UV_HEAD, tot_hA)
        if showTool:
            composite_part(preview, rx, ground_y, p_tool, cells[(2, 0)], UV_TOOL, tot_anA + axA)
        composite_part(preview, rx, ground_y, p_arm_n, cells[(1, 1)], UV_ARM, tot_anA)

    preview.convert("RGB").save(CHK / preview_name, quality=92)
    print("Saved preview ->", CHK / preview_name)


def build_linyan():
    full_ref = save_concept_bust_and_full("companion_02_linyan_concept_1795140333198.jpg", "bust_linyan", "survivor_2", 224)
    sheet = Image.open(BRAIN / "companion_02_linyan_puppet_parts_1795155389110.jpg").convert("RGB")
    sw, sh = sheet.size
    xs = [0, sw // 3, (sw * 2) // 3, sw]
    ys = [0, sh // 3, (sh * 2) // 3, sh]

    raw_head_open   = keep_largest_component(matte_crop(sheet.crop((xs[0] + 6, ys[0] + 6, xs[1] - 6, ys[1] - 6))))
    raw_head_effort = keep_largest_component(matte_crop(sheet.crop((xs[1] + 6, ys[0] + 6, xs[2] - 6, ys[1] - 6))))
    raw_ponytail    = keep_largest_component(matte_crop(sheet.crop((xs[2] + 6, ys[0] + 6, xs[3] - 6, ys[1] - 6))))

    raw_torso    = keep_largest_component(matte_crop(sheet.crop((xs[0] + 6, ys[1] + 6, int(sw * 0.31), ys[2] - 6))))
    # 中排中间同时竖排了两条手臂：左半是带补丁的近臂，右半是远臂
    raw_arm_near = keep_largest_component(matte_crop(sheet.crop((int(sw * 0.34), ys[1] + 6, int(sw * 0.49), ys[2] - 6))))
    raw_arm_far  = keep_largest_component(matte_crop(sheet.crop((int(sw * 0.50), ys[1] + 6, int(sw * 0.66), ys[2] - 6))))

    raw_leg_near = keep_largest_component(matte_crop(sheet.crop((xs[0] + 6, ys[2] + 6, xs[1] - 6, ys[3] - 6))))
    raw_leg_far  = keep_largest_component(matte_crop(sheet.crop((xs[1] + 6, ys[2] + 6, xs[2] - 6, ys[3] - 6))))
    # 林砚的弓箭上弓梢向上伸到中排下方 (y ≈ 0.45*sh)，不能用 ys[2] 硬切，必须从 0.44*sh 起裁取完整整把弓！
    raw_bow      = remove_enclosed_white_holes(keep_largest_component(matte_crop(sheet.crop((xs[2] + 6, int(sh * 0.44), xs[3] - 6, ys[3] - 6)))), min_area=80, y_start_ratio=0.0)

    cells = {
        (0, 2): place_in_cell_with_pivot(raw_head_open,   74, (0.48, 0.90), UV_HEAD),
        (1, 2): place_in_cell_with_pivot(raw_head_effort, 74, (0.48, 0.90), UV_HEAD),
        (2, 2): place_in_cell_with_pivot(raw_ponytail,    46, (0.88, 0.36), UV_ACC),
        (0, 1): place_in_cell_with_pivot(raw_torso,       90, (0.60, 0.88), UV_TORSO),
        (1, 1): place_in_cell_with_pivot(raw_arm_near,    76, (0.55, 0.12), UV_ARM),
        (2, 1): place_in_cell_with_pivot(raw_arm_far,     74, (0.45, 0.12), UV_ARM),
        (0, 0): place_in_cell_with_pivot(raw_leg_near,    96, (0.55, 0.12), UV_LEG),
        (1, 0): place_in_cell_with_pivot(raw_leg_far,     94, (0.38, 0.12), UV_LEG),
        # 弓高 108px、握把在 0.56 处，下方 0.44*108=47.5px < 51px (0.16*320)，上下两端 100% 完整不被裁切！
        (2, 0): place_in_cell_with_pivot(raw_bow,        108, (0.72, 0.56), UV_TOOL),
    }
    rig = {
        "pelvis": (0.0, 0.345), "leg_near": (-0.025, 0.01), "leg_far": (0.026, 0.01),
        "torso": (0.0, -0.02), "head": (0.012, 0.255), "acc": (-0.055, 0.165),
        "arm_near": (-0.058, 0.205), "arm_far": (0.062, 0.200), "tool": (0.010, -0.22),
    }
    pack_atlas_and_preview("linyan_puppet_atlas", full_ref, cells, rig, "linyan_poses_preview.jpg")


def build_laochentou():
    full_ref = save_concept_bust_and_full("companion_03_laochentou_concept_1795140360693.jpg", "bust_laochentou", "survivor_3", 206)
    sheet = Image.open(BRAIN / "companion_03_laochentou_puppet_parts_1795155483670.jpg").convert("RGB")
    sw, sh = sheet.size
    xs = [0, sw // 3, (sw * 2) // 3, sw]
    ys = [0, sh // 3, (sh * 2) // 3, sh]

    raw_head_open   = matte_crop(sheet.crop((xs[0] + 6, ys[0] + 6, xs[1] - 6, ys[1] - 6)))
    raw_head_effort = matte_crop(sheet.crop((xs[1] + 6, ys[0] + 6, xs[2] - 6, ys[1] - 6)))
    raw_towel_raw   = matte_crop(sheet.crop((xs[2] + 6, ys[0] + 6, xs[3] - 6, ys[1] - 6)))
    # 老陈头头巾结原本在左侧，水平镜像并取头巾尾带让结在右侧、尾带向左飘！
    raw_towel = ImageOps.mirror(matte_crop_rgba(raw_towel_raw.crop((0, 0, int(raw_towel_raw.width * 0.42), raw_towel_raw.height))))

    raw_torso    = matte_crop(sheet.crop((xs[0] + 6, ys[1] + 6, xs[1] - 6, ys[2] - 6)))
    raw_arm_near = matte_crop(sheet.crop((xs[1] + 6, ys[1] + 6, xs[2] - 6, ys[2] - 6)))
    raw_arm_far  = matte_crop(sheet.crop((xs[2] + 6, ys[1] + 6, xs[3] - 6, ys[2] - 6)))

    raw_leg_near = matte_crop(sheet.crop((xs[0] + 6, ys[2] + 6, xs[1] - 6, ys[3] - 6)))
    raw_leg_far  = matte_crop(sheet.crop((xs[1] + 6, ys[2] + 6, xs[2] - 6, ys[3] - 6)))
    raw_ladle    = matte_crop(sheet.crop((xs[2] + 6, ys[2] + 6, xs[3] - 6, ys[3] - 6)))

    cells = {
        (0, 2): place_in_cell_with_pivot(raw_head_open,   72, (0.48, 0.90), UV_HEAD),
        (1, 2): place_in_cell_with_pivot(raw_head_effort, 72, (0.48, 0.90), UV_HEAD),
        (2, 2): place_in_cell_with_pivot(raw_towel,       28, (0.90, 0.35), UV_ACC),
        (0, 1): place_in_cell_with_pivot(raw_torso,       94, (0.50, 0.86), UV_TORSO),
        (1, 1): place_in_cell_with_pivot(raw_arm_near,    74, (0.56, 0.14), UV_ARM),
        (2, 1): place_in_cell_with_pivot(raw_arm_far,     72, (0.44, 0.14), UV_ARM),
        (0, 0): place_in_cell_with_pivot(raw_leg_near,    82, (0.50, 0.12), UV_LEG),
        (1, 0): place_in_cell_with_pivot(raw_leg_far,     82, (0.40, 0.12), UV_LEG),
        (2, 0): place_in_cell_with_pivot(raw_ladle,       82, (0.52, 0.78), UV_TOOL),
    }
    rig = {
        "pelvis": (0.0, 0.295), "leg_near": (-0.032, 0.01), "leg_far": (0.034, 0.01),
        "torso": (0.0, -0.02), "head": (0.010, 0.265), "acc": (-0.068, 0.165),
        "arm_near": (-0.078, 0.230), "arm_far": (0.088, 0.220), "tool": (0.012, -0.21),
    }
    pack_atlas_and_preview("laochentou_puppet_atlas", full_ref, cells, rig, "laochentou_poses_preview.jpg")


def build_dachui():
    full_ref = save_concept_bust_and_full("companion_04_dachui_concept_1795140425976.jpg", "bust_dachui", "survivor_4", 232)
    sheet = Image.open(BRAIN / "companion_04_dachui_puppet_parts_1795155563047.jpg").convert("RGB")
    sw, sh = sheet.size
    xs = [0, sw // 3, (sw * 2) // 3, sw]
    ys = [0, sh // 3, (sh * 2) // 3, sh]

    raw_head_open   = matte_crop(sheet.crop((xs[0] + 6, ys[0] + 6, xs[1] - 6, ys[1] - 6)))
    raw_head_effort = matte_crop(sheet.crop((xs[1] + 6, ys[0] + 6, xs[2] - 6, ys[1] - 6)))
    raw_strap       = matte_crop(sheet.crop((xs[2] + 6, ys[0] + 6, xs[3] - 6, ys[1] - 6)))

    raw_torso    = matte_crop(sheet.crop((xs[0] + 6, ys[1] + 6, xs[1] - 6, ys[2] - 6)))
    raw_arm_near = matte_crop(sheet.crop((xs[1] + 6, ys[1] + 6, xs[2] - 6, ys[2] - 6)))
    raw_arm_far  = matte_crop(sheet.crop((xs[2] + 6, ys[1] + 6, xs[3] - 6, ys[2] - 6)))

    raw_leg_near = close_cut_right_edge(keep_largest_component(matte_crop(sheet.crop((xs[0] + 6, ys[2] + 6, xs[1] - 6, ys[3] - 6)))))
    raw_leg_far  = keep_largest_component(matte_crop(sheet.crop((xs[1] + 6, ys[2] + 6, xs[2] - 6, ys[3] - 6))))
    raw_crowbar  = keep_largest_component(matte_crop(sheet.crop((xs[2] + 6, ys[2] + 6, xs[3] - 6, ys[3] - 6))))

    cells = {
        (0, 2): place_in_cell_with_pivot(raw_head_open,   76, (0.48, 0.90), UV_HEAD),
        (1, 2): place_in_cell_with_pivot(raw_head_effort, 76, (0.48, 0.90), UV_HEAD),
        (2, 2): place_in_cell_with_pivot(raw_strap,       40, (0.90, 0.14), UV_ACC),
        (0, 1): place_in_cell_with_pivot(raw_torso,      102, (0.50, 0.86), UV_TORSO),
        (1, 1): place_in_cell_with_pivot(raw_arm_near,    86, (0.54, 0.15), UV_ARM),
        (2, 1): place_in_cell_with_pivot(raw_arm_far,     84, (0.48, 0.15), UV_ARM),
        (0, 0): place_in_cell_with_pivot(raw_leg_near,   102, (0.58, 0.12), UV_LEG),
        (1, 0): place_in_cell_with_pivot(raw_leg_far,    102, (0.44, 0.12), UV_LEG),
        (2, 0): place_in_cell_with_pivot(raw_crowbar,    102, (0.48, 0.72), UV_TOOL),
    }
    rig = {
        "pelvis": (0.0, 0.345), "leg_near": (-0.036, 0.01), "leg_far": (0.036, 0.01),
        "torso": (0.0, -0.02), "head": (0.012, 0.295), "acc": (-0.075, 0.02),
        "arm_near": (-0.095, 0.240), "arm_far": (0.105, 0.220), "tool": (0.014, -0.24),
    }
    pack_atlas_and_preview("dachui_puppet_atlas", full_ref, cells, rig, "dachui_poses_preview.jpg")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    CHK.mkdir(parents=True, exist_ok=True)
    build_linyan()
    build_laochentou()
    build_dachui()
