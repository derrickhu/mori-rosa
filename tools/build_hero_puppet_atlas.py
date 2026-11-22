#!/usr/bin/env python3
"""
从 hero_refugee_puppet_parts 与 hero_refugee_concept_bust 构建：
1. Assets/Mori/ForestSlice/Resources/ForestArt/hero_puppet_atlas.png (1024x1024, 3x3 格每格 320x320，枢轴严格固定在锚点像素上！)
2. Assets/Mori/ForestSlice/Resources/ForestArt/bust_leader.png (半身漫画对话头像) 与 survivor_0.png
3. 本地拼装渲染 6 姿态骨骼校验图 (Idle / Run A / Run B / Chop Windup / Chop Impact / Carry)，确保关节零缝隙！
"""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

BRAIN = Path("/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65")
OUT = Path("/Users/rosa/rosa_games/mori-rosa/Assets/Mori/ForestSlice/Resources/ForestArt")
CHK = BRAIN / "scratch" / "puppet_check"

CELL = 320
PPU = 256.0


def matte_crop(rgb_im: Image.Image) -> Image.Image:
    rgb = np.asarray(rgb_im)
    gray = np.asarray(rgb_im.convert("L"))
    white = gray > 240
    lab, _ = ndimage.label(white)
    borders = set(lab[0, :]) | set(lab[-1, :]) | set(lab[:, 0]) | set(lab[:, -1])
    borders.discard(0)
    bg = np.isin(lab, list(borders))
    fg = ~bg

    # 去极小孤立噪点
    flab, n = ndimage.label(fg)
    if n > 1:
        cnt = ndimage.sum(fg, flab, range(1, n + 1))
        mx = float(cnt.max())
        keep = np.zeros(n + 1, bool)
        for i, c in enumerate(cnt, 1):
            if c >= max(30.0, mx * 0.01):
                keep[i] = True
        fg = keep[flab]

    eroded = ndimage.binary_erosion(fg, iterations=1)
    edge = fg & (~eroded)
    alpha = fg.astype(np.float32) * 255.0
    soft = np.clip((250.0 - gray.astype(np.float32)) / 28.0, 0.15, 1.0) * 255.0
    alpha = np.where(edge, soft, alpha).astype(np.uint8)

    rgba = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
    bb = rgba.getbbox()
    return rgba.crop(bb) if bb else rgba


def place_in_cell_with_pivot(part: Image.Image, target_h: int, part_pivot_uv: tuple[float, float], cell_pivot_uv: tuple[float, float]) -> Image.Image:
    """
    将抠好紧裁的部件按目标像素高度缩放，并使其内部关节锚点 part_pivot_uv (0..1, 0=左/上, 1=右/下 in PIL coords)
    精确落在 320x320 格子的 Unity UV 锚点 cell_pivot_uv (0=左/底, 1=右/顶) 上！
    """
    sc = target_h / float(part.height)
    nw = max(1, round(part.width * sc))
    nh = max(1, round(part.height * sc))
    rs = part.resize((nw, nh), Image.LANCZOS)

    # Unity cell_pivot_uv: x 从左往右，y 从底往上 -> 转成 PIL (x 从左往右, y 从顶往下)
    target_cx = round(cell_pivot_uv[0] * CELL)
    target_cy = round((1.0 - cell_pivot_uv[1]) * CELL)

    px_in_part = round(part_pivot_uv[0] * nw)
    py_in_part = round(part_pivot_uv[1] * nh)

    canvas = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    ox = target_cx - px_in_part
    oy = target_cy - py_in_part
    canvas.paste(rs, (ox, oy), rs)
    return canvas


def rotate_cell_around_pivot(cell_im: Image.Image, cell_pivot_uv: tuple[float, float], unity_deg: float) -> tuple[Image.Image, int, int]:
    """按 Unity 逆时针正角度围绕 cell_pivot_uv 旋转（PIL Image.rotate 默认也是逆时针正角度！）。"""
    cx = cell_pivot_uv[0] * CELL
    cy = (1.0 - cell_pivot_uv[1]) * CELL
    rot = cell_im.rotate(unity_deg, resample=Image.BICUBIC, center=(cx, cy), expand=False)
    return rot, round(cx), round(cy)


def composite_part(board: Image.Image, root_x: int, root_y: int, world_offset: tuple[float, float],
                   cell_im: Image.Image, cell_pivot_uv: tuple[float, float], unity_deg: float):
    rot, cx, cy = rotate_cell_around_pivot(cell_im, cell_pivot_uv, unity_deg)
    wx = round(root_x + world_offset[0] * PPU) - cx
    wy = round(root_y - world_offset[1] * PPU) - cy
    board.alpha_composite(rot, (wx, wy))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    CHK.mkdir(parents=True, exist_ok=True)

    # 1. 处理立绘与全身概念参考图
    concept = Image.open(BRAIN / "hero_refugee_concept_bust_1795088827409.jpg").convert("RGB")
    cw, ch = concept.size
    full_ref = matte_crop(concept.crop((10, 10, int(cw * 0.46), ch - 10)))
    bust_ref = matte_crop(concept.crop((int(cw * 0.47), 10, cw - 10, ch - 10)))

    # 保存 bust_leader.png (高 400px) 与静态兜底 survivor_0.png (高 216px)
    bs = 400.0 / bust_ref.height
    bust_out = bust_ref.resize((round(bust_ref.width * bs), 400), Image.LANCZOS)
    bust_pad = Image.new("RGBA", (bust_out.width + 8, bust_out.height + 8), (0, 0, 0, 0))
    bust_pad.paste(bust_out, (4, 4))
    bust_pad.save(OUT / "bust_leader.png", "PNG")

    fs = 212.0 / full_ref.height
    full_out = full_ref.resize((round(full_ref.width * fs), 212), Image.LANCZOS)
    full_pad = Image.new("RGBA", (full_out.width + 8, full_out.height + 8), (0, 0, 0, 0))
    full_pad.paste(full_out, (4, 4))
    full_pad.save(OUT / "survivor_0.png", "PNG")
    print("Saved bust_leader.png & survivor_0.png:", bust_pad.size, full_pad.size)

    # 2. 切分 3x3 骨骼部件表
    sheet = Image.open(BRAIN / "hero_refugee_puppet_parts_1795089387846.jpg").convert("RGB")
    sw, sh = sheet.size
    # 精确三分格
    xs = [0, sw // 3, (sw * 2) // 3, sw]
    ys = [0, sh // 3, (sh * 2) // 3, sh]

    raw_head_open   = matte_crop(sheet.crop((xs[0] + 6, ys[0] + 6, xs[1] - 6, ys[1] - 6)))
    raw_head_effort = matte_crop(sheet.crop((xs[1] + 6, ys[0] + 6, xs[2] - 6, ys[1] - 6)))
    # 头巾飘带：裁掉绳结右侧多余的一截宽带（只保留绳结到左侧两条飘带，约左 66% 宽度）
    band_cell = matte_crop(sheet.crop((xs[2] + 6, ys[0] + 6, xs[3] - 6, ys[1] - 6)))
    raw_bandana = matte_crop_rgba(band_cell.crop((0, 0, int(band_cell.width * 0.66), band_cell.height)))

    raw_torso    = matte_crop(sheet.crop((xs[0] + 6, ys[1] + 6, xs[1] - 6, ys[2] - 6)))
    raw_arm_near = matte_crop(sheet.crop((xs[1] + 6, ys[1] + 6, xs[2] - 6, ys[2] - 6)))
    raw_arm_far  = matte_crop(sheet.crop((xs[2] + 6, ys[1] + 6, xs[3] - 6, ys[2] - 6)))

    raw_leg_near = matte_crop(sheet.crop((xs[0] + 6, ys[2] + 6, xs[1] - 6, ys[3] - 6)))
    raw_leg_far  = matte_crop(sheet.crop((xs[1] + 6, ys[2] + 6, xs[2] - 6, ys[3] - 6)))
    raw_hatchet  = matte_crop(sheet.crop((xs[2] + 6, ys[2] + 6, xs[3] - 6, ys[3] - 6)))

    # 3. 按全身约 216px (0.84u @ PPU=256) 黄金比例放入各自 320x320 Cell，使关节锚点严格对准 ForestHeroPuppet.cs 的 Slice UV！
    # PIL part_pivot_uv: (0=左..1=右, 0=顶..1=底)
    # Unity cell_pivot_uv: (0=左..1=右, 0=底..1=顶)
    uv_head    = (0.48, 0.10)
    uv_bandana = (0.88, 0.58)
    uv_torso   = (0.52, 0.12)
    uv_arm     = (0.50, 0.84)
    uv_leg     = (0.50, 0.86)
    uv_axe     = (0.46, 0.16)

    c_head_open   = place_in_cell_with_pivot(raw_head_open,   74, (0.36, 0.90), uv_head)
    c_head_effort = place_in_cell_with_pivot(raw_head_effort, 74, (0.36, 0.90), uv_head)
    c_bandana     = place_in_cell_with_pivot(raw_bandana,     36, (0.94, 0.24), uv_bandana)

    c_torso    = place_in_cell_with_pivot(raw_torso,    82, (0.60, 0.88), uv_torso)
    c_arm_near = place_in_cell_with_pivot(raw_arm_near, 72, (0.55, 0.12), uv_arm)
    c_arm_far  = place_in_cell_with_pivot(raw_arm_far,  70, (0.45, 0.12), uv_arm)

    c_leg_near = place_in_cell_with_pivot(raw_leg_near, 92, (0.55, 0.10), uv_leg)
    c_leg_far  = place_in_cell_with_pivot(raw_leg_far,  90, (0.36, 0.10), uv_leg)
    c_hatchet  = place_in_cell_with_pivot(raw_hatchet,  80, (0.28, 0.86), uv_axe)

    # 拼入 1024x1024 图集（注意 Unity y=0 在图片底部，所以 PIL paste y = 1024 - (row+1)*320！）
    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    grid = {
        (0, 2): c_head_open,   (1, 2): c_head_effort, (2, 2): c_bandana,
        (0, 1): c_torso,       (1, 1): c_arm_near,    (2, 1): c_arm_far,
        (0, 0): c_leg_near,    (1, 0): c_leg_far,     (2, 0): c_hatchet,
    }
    for (col, row), cim in grid.items():
        pil_x = col * CELL
        pil_y = 1024 - (row + 1) * CELL
        atlas.paste(cim, (pil_x, pil_y))

    atlas.save(OUT / "hero_puppet_atlas.png", "PNG")
    print("Saved hero_puppet_atlas.png (1024x1024)!")

    # 4. 本地骨骼正运动学 (FK) 拼装预览校验图（100% 复刻 ForestHeroPuppet.cs 坐标计算，肉眼检验所有动作姿态！）
    preview = Image.new("RGBA", (7 * 240, 320), (222, 186, 122, 255)) # 模拟暖黄营地泥土地面色！
    draw = ImageDraw.Draw(preview)
    for x in range(0, 7 * 240, 240):
        draw.line([(x, 0), (x, 320)], fill=(180, 145, 90, 255), width=2)
    draw.line([(0, 268), (7 * 240, 268)], fill=(150, 118, 70, 255), width=2)

    # 第0格：放原始一体概念图做对比标尺
    preview.alpha_composite(full_out, (120 - full_out.width // 2, 268 - full_out.height))

    poses = [
        # (torsoA, headA, bandA, legNearA, legFarA, armNearA, armFarA, axeA, effort, showAxe)
        (0,    0,   0,    0,    0,   12,   -4,  -82, False, True),   # 1. Idle
        (-12,  8,  28,   40,  -40,  -36,   42,  -55, False, True),   # 2. Run Stride A
        (-12,  6,  18,  -40,   40,   48,  -38,  -95, False, True),   # 3. Run Stride B
        (22, -10, -22,   18,  -12,  148,  136, -135, False, True),   # 4. Chop Wind-Up (高举脑后蓄力！)
        (-24, 10,  28,  -16,   10,   42,   34, -102, True,  True),   # 5. Chop Cleave Impact (猛劈向前下！)
        (-6,   4,  20,   34,  -34,  166,  156,    0, False, False),  # 6. Carry Overhead (双手高举头顶！)
    ]

    pelvis = (0.0, 0.33)
    leg_near_off = (-0.045, 0.01)
    leg_far_off  = ( 0.045, 0.01)
    torso_off    = ( 0.00, -0.02)
    head_off     = ( 0.015, 0.24)
    band_off     = (-0.06,  0.14)
    arm_near_off = (-0.065, 0.21)
    arm_far_off  = ( 0.075, 0.20)
    axe_grip     = ( 0.01, -0.22)

    def rot2d(v, deg):
        rad = math.radians(deg)
        cs, sn = math.cos(rad), math.sin(rad)
        return (v[0] * cs - v[1] * sn, v[0] * sn + v[1] * cs)

    def add2(a, b):
        return (a[0] + b[0], a[1] + b[1])

    for idx, (tA, hA, bA, lnA, lfA, anA, afA, axA, eff, showAxe) in enumerate(poses):
        rx = (idx + 1) * 240 + 120
        ry = 268

        p_leg_f = add2(pelvis, leg_far_off)
        p_leg_n = add2(pelvis, leg_near_off)
        p_torso = add2(pelvis, torso_off)
        p_arm_f = add2(p_torso, rot2d(arm_far_off, tA))
        p_head  = add2(p_torso, rot2d(head_off, tA))
        tot_hA  = tA + hA
        p_band  = add2(p_head, rot2d(band_off, tot_hA))
        p_arm_n = add2(p_torso, rot2d(arm_near_off, tA))
        tot_anA = tA + anA
        p_axe   = add2(p_arm_n, rot2d(axe_grip, tot_anA))

        # 按真实遮挡顺序绘制：LegFar -> ArmFar -> LegNear -> Torso -> Bandana -> Head -> Hatchet -> ArmNear
        composite_part(preview, rx, ry, p_leg_f, c_leg_far, uv_leg, lfA)
        composite_part(preview, rx, ry, p_arm_f, c_arm_far, uv_arm, tA + afA)
        composite_part(preview, rx, ry, p_leg_n, c_leg_near, uv_leg, lnA)
        composite_part(preview, rx, ry, p_torso, c_torso, uv_torso, tA)
        composite_part(preview, rx, ry, p_band, c_bandana, uv_bandana, tot_hA + bA)
        composite_part(preview, rx, ry, p_head, c_head_effort if eff else c_head_open, uv_head, tot_hA)
        if showAxe:
            composite_part(preview, rx, ry, p_axe, c_hatchet, uv_axe, tot_anA + axA)
        composite_part(preview, rx, ry, p_arm_n, c_arm_near, uv_arm, tot_anA)

    preview.convert("RGB").save(CHK / "poses_preview.jpg", quality=92)
    print("Saved 6-pose FK rig verification ->", CHK / "poses_preview.jpg")


def matte_crop_rgba(rgba: Image.Image) -> Image.Image:
    bb = rgba.getbbox()
    return rgba.crop(bb) if bb else rgba


if __name__ == "__main__":
    main()
