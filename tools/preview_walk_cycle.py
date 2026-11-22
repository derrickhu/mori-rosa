#!/usr/bin/env python3
"""渲染阿澈与小棠的 6 帧自然步态周期校验条（Walk ±17° 与 Run ±24° 落地防浮空），确认脚步贴地自然不劈叉！"""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw
from build_hero_puppet_atlas import BRAIN, OUT, CHK, CELL, PPU, place_in_cell_with_pivot, composite_part, matte_crop, matte_crop_rgba
from build_xiaotang_puppet_atlas import trim_torso_outer_sleeves

def load_parts_from_atlas(atlas_path: Path):
    im = Image.open(atlas_path).convert("RGBA")
    parts = {}
    for col, row, name in [
        (0, 2, "head_open"), (1, 2, "head_effort"), (2, 2, "acc"),
        (0, 1, "torso"),     (1, 1, "arm_near"),    (2, 1, "arm_far"),
        (0, 0, "leg_near"),  (1, 0, "leg_far"),     (2, 0, "tool"),
    ]:
        px = col * CELL
        py = 1024 - (row + 1) * CELL
        parts[name] = im.crop((px, py, px + CELL, py + CELL))
    return parts

def render_cycle(parts, profile_type: str, max_leg_deg: float, lean_deg: float, bounce_amp: float, out_path: Path):
    uv_head  = (0.48, 0.10)
    uv_acc   = (0.88, 0.58)
    uv_torso = (0.52, 0.12)
    uv_arm   = (0.50, 0.84)
    uv_leg   = (0.50, 0.86)
    uv_tool  = (0.46, 0.16)

    if profile_type == "ache":
        pelvis_base = (0.0, 0.33)
        leg_near_off = (-0.042, 0.01)
        leg_far_off  = ( 0.042, 0.01)
        torso_off    = ( 0.00, -0.02)
        head_off     = ( 0.015, 0.24)
        acc_off      = (-0.06,  0.14)
        arm_near_off = (-0.065, 0.21)
        arm_far_off  = ( 0.075, 0.20)
        tool_grip    = ( 0.01, -0.22)
    else:
        pelvis_base = (0.0, 0.30)
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

    board = Image.new("RGBA", (8 * 190, 290), (222, 186, 122, 255))
    draw = ImageDraw.Draw(board)
    for x in range(0, 8 * 190, 190):
        draw.line([(x, 0), (x, 290)], fill=(185, 150, 95, 255), width=2)
    ground_y = 248
    draw.line([(0, ground_y), (8 * 190, ground_y)], fill=(140, 105, 60, 255), width=2)

    for idx in range(8):
        w = idx * (2 * math.pi / 8.0)
        s = math.sin(w)
        c = math.cos(w)

        # 重心高度在单腿支撑中段(s=0)略微抬升，双脚跨步落地(|s|=1)时略微下沉贴地（完全符合真实行走倒立摆，绝不浮空！）
        # 小幅弹性 bounce = (1 - |s|) * bounce_amp - |s| * 0.012
        bob = (1.0 - abs(s)) * bounce_amp - abs(s) * 0.010
        pelvis = (pelvis_base[0], pelvis_base[1] + bob)

        lnA = s * max_leg_deg
        lfA = -s * max_leg_deg
        # 摆动向前抬脚（cos > 0 对 leg_near 是往前摆；cos < 0 对 leg_far 是往前摆）略微抬腿避免擦地
        ln_lift = max(0.0, c) * 0.018
        lf_lift = max(0.0, -c) * 0.018

        tA = lean_deg + c * 1.6
        hA = -lean_deg * 0.65 - c * 1.8
        bA = 10.0 + max_leg_deg * 0.4 + math.sin(w * 1.5) * 6.0
        anA = -s * (max_leg_deg * 0.95) + 6.0
        afA =  s * (max_leg_deg * 0.95)
        axA = -78.0 + s * 8.0

        rx = idx * 190 + 95
        p_leg_f = add2(pelvis, (leg_far_off[0], leg_far_off[1] + lf_lift))
        p_leg_n = add2(pelvis, (leg_near_off[0], leg_near_off[1] + ln_lift))
        p_torso = add2(pelvis, torso_off)
        p_arm_f = add2(p_torso, rot2d(arm_far_off, tA))
        p_head  = add2(p_torso, rot2d(head_off, tA))
        tot_hA  = tA + hA
        p_acc   = add2(p_head, rot2d(acc_off, tot_hA))
        p_arm_n = add2(p_torso, rot2d(arm_near_off, tA))
        tot_anA = tA + anA
        p_tool  = add2(p_arm_n, rot2d(tool_grip, tot_anA))

        composite_part(board, rx, ground_y, p_leg_f, parts["leg_far"], uv_leg, lfA)
        composite_part(board, rx, ground_y, p_arm_f, parts["arm_far"], uv_arm, tA + afA)
        composite_part(board, rx, ground_y, p_leg_n, parts["leg_near"], uv_leg, lnA)
        composite_part(board, rx, ground_y, p_torso, parts["torso"], uv_torso, tA)
        composite_part(board, rx, ground_y, p_acc,   parts["acc"], uv_acc, tot_hA + bA)
        composite_part(board, rx, ground_y, p_head,  parts["head_open"], uv_head, tot_hA)
        composite_part(board, rx, ground_y, p_tool,  parts["tool"], uv_tool, tot_anA + axA)
        composite_part(board, rx, ground_y, p_arm_n, parts["arm_near"], uv_arm, tot_anA)

    board.convert("RGB").save(out_path, quality=92)
    print("Saved:", out_path)

if __name__ == "__main__":
    ache = load_parts_from_atlas(OUT / "hero_puppet_atlas.png")
    xt   = load_parts_from_atlas(OUT / "xiaotang_puppet_atlas.png")
    render_cycle(ache, "ache", 17.0, -3.5, 0.014, CHK / "walk_ache_natural.jpg")
    render_cycle(ache, "ache", 24.0, -7.0, 0.022, CHK / "run_ache_natural.jpg")
    render_cycle(xt,   "xt",   17.0, -3.0, 0.012, CHK / "walk_xt_natural.jpg")
    render_cycle(xt,   "xt",   24.0, -6.5, 0.020, CHK / "run_xt_natural.jpg")
