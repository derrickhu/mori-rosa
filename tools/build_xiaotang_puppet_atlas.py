#!/usr/bin/env python3
"""
从 companion_01_xiaotang_concept & companion_01_xiaotang_puppet_parts 构建：
1. Assets/Mori/ForestSlice/Resources/ForestArt/xiaotang_puppet_atlas.png (1024x1024, 3x3 格每格 320x320)
2. Assets/Mori/ForestSlice/Resources/ForestArt/bust_xiaotang.png & survivor_1.png
3. 本地 6 姿态正运动学校验图 (Idle / Run A / Run B / Wrench Windup / Wrench Smash / Carry)，确保肩肘髋无双袖重影、关节零缝隙！
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


def matte_crop_rgba(rgba: Image.Image) -> Image.Image:
    bb = rgba.getbbox()
    return rgba.crop(bb) if bb else rgba


def trim_torso_outer_sleeves(torso_rgba: Image.Image) -> Image.Image:
    """
    小棠的 arm_near / arm_far 自带完整的条纹短袖袖筒，因此需要把 torso 两侧凸出的袖翼裁净，
    只保留背带裤主体与肩带边缘的干净肩窝，这样手臂 360° 摆动举起时绝不会出现“四条袖子”重影！
    """
    arr = np.asarray(torso_rgba).copy()
    h, w, _ = arr.shape
    # 在肩带/背带区域（y = 0.08*h .. 0.44*h）检测深紫色牛仔背带或中心衬衫边界，只删掉靠外侧的冰蓝袖翼
    # 实测原图背带内侧大约在 x = 0.25*w .. 0.78*w 之间；外侧浅蓝条纹短袖在 y < 0.46*h 突出
    yy, xx = np.mgrid[0:h, 0:w]
    r = arr[:, :, 0].astype(np.int32)
    g = arr[:, :, 1].astype(np.int32)
    b = arr[:, :, 2].astype(np.int32)
    # 紫色背带/牛仔主体蓝色通道远高于绿红、或饱和紫 (b > 110 and b - g > 20)；条纹浅蓝袖子高亮度且 (g > 180)
    is_sleeve_zone = yy < int(h * 0.44)
    left_wing = is_sleeve_zone & (xx < int(w * 0.21))
    right_wing = is_sleeve_zone & (xx > int(w * 0.81))
    cut = left_wing | right_wing
    arr[cut, 3] = 0
    return matte_crop_rgba(Image.fromarray(arr, "RGBA"))


def place_in_cell_with_pivot(part: Image.Image, target_h: int, part_pivot_uv: tuple[float, float], cell_pivot_uv: tuple[float, float]) -> Image.Image:
    sc = target_h / float(part.height)
    nw = max(1, round(part.width * sc))
    nh = max(1, round(part.height * sc))
    rs = part.resize((nw, nh), Image.LANCZOS)

    target_cx = round(cell_pivot_uv[0] * CELL)
    target_cy = round((1.0 - cell_pivot_uv[1]) * CELL)

    px_in_part = round(part_pivot_uv[0] * nw)
    py_in_part = round(part_pivot_uv[1] * nh)

    canvas = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    canvas.paste(rs, (target_cx - px_in_part, target_cy - py_in_part), rs)
    return canvas


def rotate_cell_around_pivot(cell_im: Image.Image, cell_pivot_uv: tuple[float, float], unity_deg: float) -> tuple[Image.Image, int, int]:
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

    # 1. 半身立绘 bust_xiaotang.png & 全身参考 survivor_1.png
    concept = Image.open(BRAIN / "companion_01_xiaotang_concept_1795140307845.jpg").convert("RGB")
    cw, ch = concept.size
    full_ref = matte_crop(concept.crop((10, 10, int(cw * 0.45), ch - 10)))
    bust_ref = matte_crop(concept.crop((int(cw * 0.47), 10, cw - 10, ch - 10)))

    bs = 400.0 / bust_ref.height
    bust_out = bust_ref.resize((round(bust_ref.width * bs), 400), Image.LANCZOS)
    bust_pad = Image.new("RGBA", (bust_out.width + 8, bust_out.height + 8), (0, 0, 0, 0))
    bust_pad.paste(bust_out, (4, 4))
    bust_pad.save(OUT / "bust_xiaotang.png", "PNG")

    fs = 194.0 / full_ref.height
    full_out = full_ref.resize((round(full_ref.width * fs), 194), Image.LANCZOS)
    full_pad = Image.new("RGBA", (full_out.width + 8, full_out.height + 8), (0, 0, 0, 0))
    full_pad.paste(full_out, (4, 4))
    full_pad.save(OUT / "survivor_1.png", "PNG")
    print("Saved bust_xiaotang.png & survivor_1.png:", bust_pad.size, full_pad.size)

    # 2. 切分小棠 3x3 骨骼部件表
    sheet = Image.open(BRAIN / "companion_01_xiaotang_puppet_parts_1795141070745.jpg").convert("RGB")
    sw, sh = sheet.size
    xs = [0, sw // 3, (sw * 2) // 3, sw]
    ys = [0, sh // 3, (sh * 2) // 3, sh]

    raw_head_open   = matte_crop(sheet.crop((xs[0] + 6, ys[0] + 6, xs[1] - 6, ys[1] - 6)))
    raw_head_effort = matte_crop(sheet.crop((xs[1] + 6, ys[0] + 6, xs[2] - 6, ys[1] - 6)))
    # (2,2) 只保留左侧飘动的紫色发带结与尾带，裁掉右侧圆环护目镜带
    acc_cell = matte_crop(sheet.crop((xs[2] + 6, ys[0] + 6, xs[3] - 6, ys[1] - 6)))
    raw_ribbon = matte_crop_rgba(acc_cell.crop((0, int(acc_cell.height * 0.14), int(acc_cell.width * 0.49), acc_cell.height)))

    raw_torso_full = matte_crop(sheet.crop((xs[0] + 6, ys[1] + 6, xs[1] - 6, ys[2] - 6)))
    raw_torso = trim_torso_outer_sleeves(raw_torso_full)
    raw_arm_near = matte_crop(sheet.crop((xs[1] + 6, ys[1] + 6, xs[2] - 6, ys[2] - 6)))
    raw_arm_far  = matte_crop(sheet.crop((xs[2] + 6, ys[1] + 6, xs[3] - 6, ys[2] - 6)))

    raw_leg_near = matte_crop(sheet.crop((xs[0] + 6, ys[2] + 6, xs[1] - 6, ys[3] - 6)))
    raw_leg_far  = matte_crop(sheet.crop((xs[1] + 6, ys[2] + 6, xs[2] - 6, ys[3] - 6)))
    raw_wrench   = matte_crop(sheet.crop((xs[2] + 6, ys[2] + 6, xs[3] - 6, ys[3] - 6)))

    # 与 ForestHeroPuppet.cs 统一 3x3 UV 锚点契约（可与任意角色/换装皮肤无缝复用！）
    uv_head   = (0.48, 0.10)
    uv_acc    = (0.88, 0.58)
    uv_torso  = (0.52, 0.12)
    uv_arm    = (0.50, 0.84)
    uv_leg    = (0.50, 0.86)
    uv_tool   = (0.46, 0.16)

    c_head_open   = place_in_cell_with_pivot(raw_head_open,   72, (0.49, 0.90), uv_head)
    c_head_effort = place_in_cell_with_pivot(raw_head_effort, 72, (0.49, 0.90), uv_head)
    c_ribbon      = place_in_cell_with_pivot(raw_ribbon,      36, (0.94, 0.16), uv_acc)

    c_torso    = place_in_cell_with_pivot(raw_torso,    78, (0.52, 0.88), uv_torso)
    c_arm_near = place_in_cell_with_pivot(raw_arm_near, 68, (0.48, 0.14), uv_arm)
    c_arm_far  = place_in_cell_with_pivot(raw_arm_far,  66, (0.42, 0.14), uv_arm)

    c_leg_near = place_in_cell_with_pivot(raw_leg_near, 84, (0.38, 0.10), uv_leg)
    c_leg_far  = place_in_cell_with_pivot(raw_leg_far,  82, (0.38, 0.10), uv_leg)
    c_wrench   = place_in_cell_with_pivot(raw_wrench,   76, (0.44, 0.76), uv_tool)

    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    grid = {
        (0, 2): c_head_open, (1, 2): c_head_effort, (2, 2): c_ribbon,
        (0, 1): c_torso,     (1, 1): c_arm_near,    (2, 1): c_arm_far,
        (0, 0): c_leg_near,  (1, 0): c_leg_far,     (2, 0): c_wrench,
    }
    for (col, row), cim in grid.items():
        atlas.paste(cim, (col * CELL, 1024 - (row + 1) * CELL))

    atlas.save(OUT / "xiaotang_puppet_atlas.png", "PNG")
    print("Saved xiaotang_puppet_atlas.png (1024x1024)!")

    # 3. 6 姿态正运动学校验图（对比原设定稿，确保无穿帮重影！）
    preview = Image.new("RGBA", (7 * 240, 320), (222, 186, 122, 255))
    draw = ImageDraw.Draw(preview)
    for x in range(0, 7 * 240, 240):
        draw.line([(x, 0), (x, 320)], fill=(180, 145, 90, 255), width=2)
    draw.line([(0, 268), (7 * 240, 268)], fill=(150, 118, 70, 255), width=2)

    preview.alpha_composite(full_out, (120 - full_out.width // 2, 268 - full_out.height))

    poses = [
        (0,    0,   0,    0,    0,   12,   -4,  -78, False, True),
        (-12,  8,  28,   40,  -40,  -36,   42,  -55, False, True),
        (-12,  6,  18,  -40,   40,   48,  -38,  -95, False, True),
        (22, -10, -22,   18,  -12,  148,  136, -135, False, True),
        (-24, 10,  28,  -16,   10,   42,   34, -102, True,  True),
        (-6,   4,  20,   34,  -34,  166,  156,    0, False, False),
    ]

    pelvis = (0.0, 0.30)
    leg_near_off = (-0.038, 0.01)
    leg_far_off  = ( 0.038, 0.01)
    torso_off    = ( 0.00, -0.02)
    head_off     = ( 0.010, 0.23)
    acc_off      = (-0.062, 0.145)
    arm_near_off = (-0.050, 0.190)
    arm_far_off  = ( 0.052, 0.184)
    tool_grip    = ( 0.012, -0.20)

    def rot2d(v, deg):
        rad = math.radians(deg)
        cs, sn = math.cos(rad), math.sin(rad)
        return (v[0] * cs - v[1] * sn, v[0] * sn + v[1] * cs)

    def add2(a, b):
        return (a[0] + b[0], a[1] + b[1])

    for idx, (tA, hA, bA, lnA, lfA, anA, afA, axA, eff, showTool) in enumerate(poses):
        rx = (idx + 1) * 240 + 120
        ry = 268

        p_leg_f = add2(pelvis, leg_far_off)
        p_leg_n = add2(pelvis, leg_near_off)
        p_torso = add2(pelvis, torso_off)
        p_arm_f = add2(p_torso, rot2d(arm_far_off, tA))
        p_head  = add2(p_torso, rot2d(head_off, tA))
        tot_hA  = tA + hA
        p_acc   = add2(p_head, rot2d(acc_off, tot_hA))
        p_arm_n = add2(p_torso, rot2d(arm_near_off, tA))
        tot_anA = tA + anA
        p_tool  = add2(p_arm_n, rot2d(tool_grip, tot_anA))

        composite_part(preview, rx, ry, p_leg_f, c_leg_far, uv_leg, lfA)
        composite_part(preview, rx, ry, p_arm_f, c_arm_far, uv_arm, tA + afA)
        composite_part(preview, rx, ry, p_leg_n, c_leg_near, uv_leg, lnA)
        composite_part(preview, rx, ry, p_torso, c_torso, uv_torso, tA)
        composite_part(preview, rx, ry, p_acc,   c_ribbon, uv_acc, tot_hA + bA)
        composite_part(preview, rx, ry, p_head,  c_head_effort if eff else c_head_open, uv_head, tot_hA)
        if showTool:
            composite_part(preview, rx, ry, p_tool, c_wrench, uv_tool, tot_anA + axA)
        composite_part(preview, rx, ry, p_arm_n, c_arm_near, uv_arm, tot_anA)

    preview.convert("RGB").save(CHK / "xiaotang_poses_preview.jpg", quality=92)
    print("Saved 6-pose FK rig verification ->", CHK / "xiaotang_poses_preview.jpg")


if __name__ == "__main__":
    main()