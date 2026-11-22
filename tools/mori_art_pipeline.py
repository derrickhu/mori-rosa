#!/usr/bin/env python3
"""
mori-rosa 美术资产后处理与 2x 废土彩漫贴纸风 UI Chrome 生成管线
用法（需用带 PIL + rembg 的 /usr/bin/python3 执行）：
  /usr/bin/python3 tools/mori_art_pipeline.py split_and_mat
  /usr/bin/python3 tools/mori_art_pipeline.py ui_chrome
  /usr/bin/python3 tools/mori_art_pipeline.py all
"""
from __future__ import annotations

import argparse
import os
import shutil
import time
from pathlib import Path

os.environ["OMP_NUM_THREADS"] = "8"

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ASSETS_ROOT = Path("/Users/rosa/rosa_games/game_assets/mori-rosa/assets")
RAW_DIR = ASSETS_ROOT / "raw"
SPLIT_DIR = ASSETS_ROOT / "split"
NOBG_DIR = ASSETS_ROOT / "nobg"
FINAL_DIR = ASSETS_ROOT / "final"
UI_DIR = FINAL_DIR / "ui"


# =====================================================================
# 1. 白底宫格智能寻缝切图 + rembg 抠图 + 去杂点裁边
# =====================================================================

def find_gutter_cuts(gray: np.ndarray, parts: int, axis: int) -> list[int]:
    """
    在接近 1/parts, 2/parts ... 的 ±12% 窗口内寻找最白（非白像素最少）的空白缝隙分割线，
    避免等分切图裁掉建筑屋檐或图标边缘。
    axis=1 表示找水平方向切分列（沿 x 轴找垂直白缝），axis=0 表示沿 y 轴找水平白缝。
    """
    length = gray.shape[axis]
    non_white = (gray < 242).astype(np.float32)
    profile = non_white.sum(axis=1 - axis)
    # 平滑 7px 窗口找最宽白谷中心
    kernel = np.ones(7, dtype=np.float32) / 7.0
    smooth = np.convolve(profile, kernel, mode="same")

    cuts = [0]
    window = max(12, int(length * 0.12))
    for k in range(1, parts):
        target = int(round(length * k / parts))
        lo = max(1, target - window)
        hi = min(length - 1, target + window)
        best = lo + int(np.argmin(smooth[lo:hi]))
        cuts.append(best)
    cuts.append(length)
    return cuts


def smart_split_sheet(src_path: Path, cols: int, rows: int, names: list[str], out_dir: Path, pad_inner: int = 2):
    out_dir.mkdir(parents=True, exist_ok=True)
    im = Image.open(src_path).convert("RGB")
    gray = np.array(im.convert("L"))
    x_cuts = find_gutter_cuts(gray, cols, axis=1)
    y_cuts = find_gutter_cuts(gray, rows, axis=0)

    idx = 0
    for r in range(rows):
        for c in range(cols):
            x1 = x_cuts[c] + (pad_inner if c > 0 else 4)
            x2 = x_cuts[c + 1] - (pad_inner if c < cols - 1 else 4)
            y1 = y_cuts[r] + (pad_inner if r > 0 else 4)
            y2 = y_cuts[r + 1] - (pad_inner if r < rows - 1 else 4)
            cell = im.crop((x1, y1, x2, y2))
            out_path = out_dir / f"{names[idx]}.png"
            cell.save(out_path, "PNG")
            print(f"  [split] {names[idx]}.png -> {cell.size[0]}x{cell.size[1]} (box={x1},{y1},{x2},{y2})", flush=True)
            idx += 1


def clean_alpha_and_trim(im: Image.Image, padding: int = 6, min_island_ratio: float = 0.0015) -> Image.Image:
    """清理边缘孤立噪点，确保四角 alpha=0，并按不透明包围盒留白裁切。"""
    rgba = np.array(im.convert("RGBA"))
    alpha = rgba[:, :, 3]
    # 压掉低于 18 的半透明雾边
    alpha = np.where(alpha < 18, 0, alpha).astype(np.uint8)

    mask = alpha > 0
    labeled, num = ndimage.label(mask)
    if num > 1:
        counts = ndimage.sum(mask, labeled, range(1, num + 1))
        max_area = float(np.max(counts)) if len(counts) else 0.0
        keep = np.zeros(num + 1, dtype=bool)
        for i, cnt in enumerate(counts, start=1):
            if cnt >= max(24.0, max_area * min_island_ratio):
                keep[i] = True
        clean_mask = keep[labeled]
        alpha = np.where(clean_mask, alpha, 0).astype(np.uint8)

    rgba[:, :, 3] = alpha
    cleaned = Image.fromarray(rgba, mode="RGBA")
    bbox = cleaned.getbbox()
    if not bbox:
        return cleaned

    # 先裁紧再补纯透明边，确保四角 100% alpha == 0
    tight = cleaned.crop(bbox)
    tw, th = tight.size
    canvas_im = Image.new("RGBA", (tw + padding * 2, th + padding * 2), (0, 0, 0, 0))
    canvas_im.paste(tight, (padding, padding))
    return canvas_im


def run_split_and_mat():
    from rembg import new_session, remove

    SPLIT_DIR.mkdir(parents=True, exist_ok=True)
    NOBG_DIR.mkdir(parents=True, exist_ok=True)
    FINAL_DIR.mkdir(parents=True, exist_ok=True)

    # 1) 地表底图直接转存高清 PNG（不抠图）
    base_src = RAW_DIR / "b0_camp_base_ground.jpg"
    base_im = Image.open(base_src).convert("RGB")
    base_out = FINAL_DIR / "camp_base_s1.png"
    base_im.save(base_out, "PNG")
    print(f"[base] camp_base_s1.png saved ({base_im.size[0]}x{base_im.size[1]})", flush=True)

    # 2) 切分宫格图
    building_names = ["b_command_bus", "b_smokehouse", "b_hunt_tower", "b_water_still"]
    smart_split_sheet(RAW_DIR / "b0_buildings_sheet.jpg", 2, 2, building_names, SPLIT_DIR)

    prop_walker_names = ["b_tent_house", "plot_scaffold", "walker_mechanic", "walker_napper"]
    smart_split_sheet(RAW_DIR / "b0_props_and_walkers_sheet.jpg", 2, 2, prop_walker_names, SPLIT_DIR)

    icon_names = [
        "ico_coin", "ico_gem", "ico_raw_meat",
        "ico_smoked_ribs", "ico_clean_water", "ico_scrap_wood",
        "ico_blueprint", "ico_nav_hero", "ico_nav_explore",
    ]
    smart_split_sheet(RAW_DIR / "b0_icons_sheet.jpg", 3, 3, icon_names, SPLIT_DIR)

    dock_names = ["ico_nav_camp", "ico_orders", "ico_nav_trial", "ico_nav_wasteland"]
    smart_split_sheet(RAW_DIR / "b0_dock_icons_sheet.jpg", 2, 2, dock_names, SPLIT_DIR)

    # 单张立绘复制进 split
    shutil.copy2(RAW_DIR / "b0_hero_ironpot_sam.jpg", SPLIT_DIR / "hero_ironpot_sam.png")

    # 3) 分模型批量 rembg（CPUExecutionProvider）
    general_targets = building_names + prop_walker_names + ["hero_ironpot_sam"]
    icon_targets = icon_names + dock_names

    t0 = time.time()
    print("\nLoading birefnet-general session (CPUExecutionProvider)...", flush=True)
    sess_gen = new_session("birefnet-general", providers=["CPUExecutionProvider"])
    for name in general_targets:
        inp = Image.open(SPLIT_DIR / f"{name}.png")
        out = remove(inp, session=sess_gen)
        out.save(NOBG_DIR / f"{name}.png", "PNG")
        trimmed = clean_alpha_and_trim(out, padding=8)
        trimmed.save(FINAL_DIR / f"{name}.png", "PNG")
        print(f"  [birefnet] {name}.png -> {trimmed.size[0]}x{trimmed.size[1]}", flush=True)

    print("\nLoading isnet-anime session for icons (CPUExecutionProvider)...", flush=True)
    sess_ico = new_session("isnet-anime", providers=["CPUExecutionProvider"])
    for name in icon_targets:
        inp = Image.open(SPLIT_DIR / f"{name}.png")
        out = remove(inp, session=sess_ico)
        out.save(NOBG_DIR / f"{name}.png", "PNG")
        trimmed = clean_alpha_and_trim(out, padding=6)
        trimmed.save(FINAL_DIR / f"{name}.png", "PNG")
        print(f"  [isnet-anime] {name}.png -> {trimmed.size[0]}x{trimmed.size[1]}", flush=True)

    print(f"\nSplit & Matting complete in {time.time() - t0:.1f}s!", flush=True)


# =====================================================================
# 2. 2x 废土彩漫贴纸风 UI Chrome 组件生成（4x 超采样抗锯齿 -> 2x 输出）
# 严格遵循 minigame-ui-library/method.md：
# - 全局统一 2px（2x 图上为 4px）深墨褐外描边 #18120E
# - 顶部 14% 亮面高光 + 底部短硬边厚度阴影
# - 大面积暖沙米底 #F4EAD5 / 旧铁皮深褐 #362E2A，高饱和只留小按钮与品质框
# =====================================================================

SS = 2  # 目标已是 2x 像素，再乘 2 倍（共 4x 逻辑分辨率）做超采样抗锯齿
INK = (24, 18, 14, 255)        # #18120E 统一深墨褐外描边


def new_ss(w2x: int, h2x: int) -> Image.Image:
    return Image.new("RGBA", (w2x * SS, h2x * SS), (0, 0, 0, 0))


def finish_ss(im: Image.Image, out_path: Path):
    out = im.resize((im.width // SS, im.height // SS), Image.LANCZOS)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path, "PNG")
    print(f"  [ui] {out_path.name} ({out.width}x{out.height})", flush=True)


def make_sticker_box(
    w2x: int,
    h2x: int,
    radius2x: int,
    top_rgb: tuple[int, int, int],
    bot_rgb: tuple[int, int, int],
    bevel_rgb: tuple[int, int, int],
    gloss_alpha: int = 95,
    outline2x: int = 4,
    bevel2x: int = 6,
    drop_shadow_alpha: int = 55,
) -> Image.Image:
    """生成带统一 2px 墨线、顶部 14% 高光、底部内厚度边与轻投影的圆角贴纸组件（输出为 2x 尺寸）。"""
    pad_bot = 4 * SS
    W, H = w2x * SS, h2x * SS
    R = radius2x * SS
    L = outline2x * SS
    B = bevel2x * SS

    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body_h = H - pad_bot

    # 底部轻投影（只留在平直底边下）
    if drop_shadow_alpha > 0:
        sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        sd = ImageDraw.Draw(sh)
        sd.rounded_rectangle((R // 2, pad_bot, W - 1 - R // 2, H - 1), R, fill=(18, 12, 8, drop_shadow_alpha))
        sh = sh.filter(ImageFilter.GaussianBlur(radius=2 * SS))
        im = Image.alpha_composite(im, sh)

    # 外圈深墨褐描边
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, W - 1, body_h - 1), R, fill=INK)

    # 内侧面色遮罩
    inner_r = max(2, R - L)
    inner_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(inner_mask).rounded_rectangle((L, L, W - 1 - L, body_h - 1 - L), inner_r, fill=255)

    # 底部立体厚度暗边（铺在内圈下半，随后被主面色盖住上半）
    bevel_layer = Image.new("RGBA", (W, H), bevel_rgb + (255,))
    im.paste(bevel_layer, (0, 0), inner_mask)

    # 主面垂直柔和渐变（留出底边 B 像素露出厚度暗边）
    face_mask = Image.new("L", (W, H), 0)
    face_bot = max(L + 4, body_h - 1 - L - B)
    ImageDraw.Draw(face_mask).rounded_rectangle((L, L, W - 1 - L, face_bot), inner_r, fill=255)

    t = np.linspace(0.0, 1.0, H, dtype=np.float32)[:, None, None]
    c_top = np.array(top_rgb, dtype=np.float32)[None, None, :]
    c_bot = np.array(bot_rgb, dtype=np.float32)[None, None, :]
    grad_rgb = np.clip(c_top * (1.0 - t) + c_bot * t, 0, 255).astype(np.uint8)
    grad_rgb = np.broadcast_to(grad_rgb, (H, W, 3)).copy()
    grad_rgba = np.dstack([grad_rgb, np.full((H, W), 255, dtype=np.uint8)])
    im.paste(Image.fromarray(grad_rgba, "RGBA"), (0, 0), face_mask)

    # 顶部 14% 亮面高光带
    if gloss_alpha > 0:
        gh = max(12 * SS, int((face_bot - L) * 0.18))
        gloss_mask = Image.new("L", (W, H), 0)
        ImageDraw.Draw(gloss_mask).rounded_rectangle(
            (L + 3 * SS, L + 2 * SS, W - 1 - L - 3 * SS, L + gh),
            max(2, inner_r - 2 * SS),
            fill=gloss_alpha,
        )
        # 下半渐隐
        gy = np.arange(H, dtype=np.float32)[:, None]
        fade = np.clip(1.0 - (gy - (L + 2 * SS)) / max(1.0, float(gh)), 0.0, 1.0)
        gm = (np.array(gloss_mask, dtype=np.float32) * fade).astype(np.uint8)
        white_layer = Image.new("RGBA", (W, H), (255, 255, 255, 255))
        im.paste(white_layer, (0, 0), Image.fromarray(gm, "L"))

    return im


def make_slanted_currency_pill(w2x: int = 336, h2x: int = 76) -> Image.Image:
    """
    顶栏货币槽（对标 method.md：左端半圆，右端斜切直边，深暖褐 #362E2A，带细内高光与 2px 墨线）。
    """
    W, H = w2x * SS, h2x * SS
    L = 4 * SS
    slant = int(H * 0.34)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def pill_poly(inset: int) -> list[tuple[int, int]]:
        r = (H - inset * 2) // 2
        cx, cy = inset + r, H // 2
        pts = []
        # 左半圆（90° -> 270°）
        for deg in range(90, 271, 6):
            rad = np.deg2rad(deg)
            pts.append((int(round(cx + r * np.cos(rad))), int(round(cy - r * np.sin(rad)))))
        # 下沿到右下（斜切：上沿比下沿更靠右）
        pts.append((W - 1 - slant - inset, H - 1 - inset))
        pts.append((W - 1 - inset, inset))
        return pts

    d = ImageDraw.Draw(im)
    d.polygon(pill_poly(0), fill=INK)
    d.polygon(pill_poly(L), fill=(54, 46, 42, 242))  # #362E2A
    # 上沿一条柔和暖高光细边
    d.line([(H // 2, L + 2 * SS), (W - L - 4 * SS, L + 2 * SS)], fill=(112, 96, 86, 150), width=2 * SS)
    return im


def make_pointed_button(
    w2x: int,
    h2x: int,
    top_rgb: tuple[int, int, int],
    bot_rgb: tuple[int, int, int],
    bevel_rgb: tuple[int, int, int],
) -> Image.Image:
    """六边形尖角主行动按钮（对标 method.md 尖角主按钮：2px 墨线 + 上沿高光 + 底边厚度）。"""
    W, H = w2x * SS, h2x * SS
    pad_bot = 4 * SS
    bh = H - pad_bot
    L = 4 * SS
    B = 6 * SS
    tip = int(bh * 0.30)

    def hex_pts(inset: int, y_bot_trim: int = 0) -> list[tuple[int, int]]:
        y1 = inset
        y2 = bh - 1 - inset - y_bot_trim
        ym = (y1 + y2) // 2
        x_left = inset
        x_right = W - 1 - inset
        t_in = max(4, tip - inset // 2)
        return [
            (x_left + t_in, y1),
            (x_right - t_in, y1),
            (x_right, ym),
            (x_right - t_in, y2),
            (x_left + t_in, y2),
            (x_left, ym),
        ]

    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # 底边硬投影
    d.rounded_rectangle((tip, bh - 2 * SS, W - tip, H - 1), 4 * SS, fill=(18, 12, 8, 65))
    # 外墨线
    d.polygon(hex_pts(0), fill=INK)
    # 厚度层
    d.polygon(hex_pts(L), fill=bevel_rgb + (255,))
    # 面色渐变层
    face_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(face_mask).polygon(hex_pts(L, B), fill=255)

    t = np.linspace(0.0, 1.0, H, dtype=np.float32)[:, None, None]
    c_top = np.array(top_rgb, dtype=np.float32)[None, None, :]
    c_bot = np.array(bot_rgb, dtype=np.float32)[None, None, :]
    grad = np.clip(c_top * (1.0 - t) + c_bot * t, 0, 255).astype(np.uint8)
    grad = np.broadcast_to(grad, (H, W, 3)).copy()
    im.paste(Image.fromarray(np.dstack([grad, np.full((H, W), 255, dtype=np.uint8)]), "RGBA"), (0, 0), face_mask)

    # 上沿 14% 高光条
    d = ImageDraw.Draw(im)
    d.line(
        [(L + tip, L + 3 * SS), (W - L - tip, L + 3 * SS)],
        fill=(255, 255, 255, 145),
        width=4 * SS,
    )
    return im


def make_modal_shell(w2x: int = 512, h2x: int = 512) -> Image.Image:
    """
    废土彩漫铆钉弹窗九宫格壳子（2x 尺寸 512x512，9-slice border = 140 左右，150 上，90 下）：
    - 顶栏为暖琥珀橙黄头 (#F49D22 -> #E08214) + 四角金属圆铆钉
    - 主体为舒适护眼的暖沙羊皮米色 (#F4EAD5) + 极淡废土细点底纹
    - 全局统一 4px（1x 下 2px）深墨褐外描边 #18120E
    """
    W, H = w2x * SS, h2x * SS
    R = 36 * SS
    L = 4 * SS
    header_h = 108 * SS

    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # 外描边与底部深棕厚度
    d.rounded_rectangle((0, 0, W - 1, H - 1), R, fill=INK)
    d.rounded_rectangle((L, L, W - 1 - L, H - 1 - L), R - L, fill=(182, 162, 134, 255))
    # 暖沙米色内衬主体 #F4EAD5
    d.rounded_rectangle((L, L, W - 1 - L, H - 1 - L - 8 * SS), R - L, fill=(244, 234, 213, 255))

    # 橙黄标题头（盖住上沿圆角，底边一条 2px 墨线分隔）
    head_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(head_mask).rounded_rectangle((L, L, W - 1 - L, header_h), R - L, fill=255)
    ImageDraw.Draw(head_mask).rectangle((L, header_h // 2, W - 1 - L, header_h), fill=255)

    head_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(head_layer)
    for y in range(L, header_h + 1):
        ratio = (y - L) / max(1, header_h - L)
        r = int(250 * (1 - ratio) + 228 * ratio)
        g = int(172 * (1 - ratio) + 132 * ratio)
        b = int(48 * (1 - ratio) + 24 * ratio)
        hd.line([(L, y), (W - 1 - L, y)], fill=(r, g, b, 255))
    # 标题头上沿亮线 + 底部分隔墨线
    hd.line([(L + 24 * SS, L + 4 * SS), (W - L - 24 * SS, L + 4 * SS)], fill=(255, 236, 178, 175), width=4 * SS)
    im.paste(head_layer, (0, 0), head_mask)
    d = ImageDraw.Draw(im)
    d.line([(L, header_h), (W - 1 - L, header_h)], fill=INK, width=L)

    # 标题头左右两颗小金属铆钉（落在 9-slice 左右角安全区内）
    for rx in (32 * SS, W - 32 * SS):
        ry = header_h // 2 + 2 * SS
        rr = 7 * SS
        d.ellipse((rx - rr, ry - rr, rx + rr, ry + rr), fill=INK)
        d.ellipse((rx - rr + 2 * SS, ry - rr + 2 * SS, rx + rr - 2 * SS, ry + rr - 2 * SS), fill=(214, 198, 176, 255))
        d.ellipse((rx - 3 * SS, ry - 4 * SS, rx + 1 * SS, ry), fill=(255, 255, 255, 190))

    return im


def make_dock_bar_and_active() -> tuple[Image.Image, Image.Image]:
    """
    底部主导航 Dock 底板（奶油暖沙 #F2E4CA 直边 + 顶沿 2px 墨线）
    与选中高亮块（亮黄描边 + 暖黄渐变 + 圆点暗纹）。
    """
    # Dock 底条（可横向拉伸或整条铺满 720x132 -> 2x: 1440x264）
    W, H = 512 * SS, 256 * SS
    L = 4 * SS
    dock = Image.new("RGBA", (W, H), (242, 228, 202, 255))  # #F2E4CA
    dd = ImageDraw.Draw(dock)
    # 底部渐暖一档
    for y in range(H):
        t = y / max(1, H - 1)
        r = int(245 * (1 - t) + 226 * t)
        g = int(232 * (1 - t) + 206 * t)
        b = int(206 * (1 - t) + 174 * t)
        dd.line([(0, y), (W - 1, y)], fill=(r, g, b, 255))
    dd.rectangle((0, 0, W - 1, L - 1), fill=INK)
    dd.rectangle((0, L, W - 1, L + 3 * SS), fill=(255, 248, 232, 190))

    # 选中页签高亮块（1x: 136x116 -> 2x: 272x232）
    aw, ah = 272 * SS, 232 * SS
    act = make_sticker_box(
        272, 232, radius2x=28,
        top_rgb=(255, 224, 102),
        bot_rgb=(246, 182, 42),
        bevel_rgb=(202, 132, 20),
        gloss_alpha=115,
        outline2x=4,
        bevel2x=6,
        drop_shadow_alpha=35,
    )
    # 下半圆点暗纹（对标 method.md：选中块里面的圆点暗纹往下更清楚）
    ad = ImageDraw.Draw(act)
    step = 22 * SS
    for py in range(ah // 2, ah - 22 * SS, step):
        alpha = int(35 + 55 * ((py - ah // 2) / max(1, ah // 2)))
        for px in range(26 * SS, aw - 26 * SS, step):
            rr = 4 * SS
            ad.ellipse((px - rr, py - rr, px + rr, py + rr), fill=(212, 136, 18, alpha))

    return dock, act


def make_camp_bubble(w2x: int = 136, h2x: int = 148, rim_rgb: tuple[int, int, int] = (138, 203, 41)) -> Image.Image:
    """建筑头顶悬浮交互气泡（带底尖小三角指针 + 2px 墨线 + 亮色内圈）。"""
    W, H = w2x * SS, h2x * SS
    L = 4 * SS
    box_h = H - 22 * SS
    R = 32 * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    cx = W // 2
    # 底部小尖角外墨线
    d.rounded_rectangle((0, 0, W - 1, box_h - 1), R, fill=INK)
    d.polygon([(cx - 18 * SS, box_h - 4 * SS), (cx + 18 * SS, box_h - 4 * SS), (cx, H - 1)], fill=INK)

    # 彩色内框边
    d.rounded_rectangle((L, L, W - 1 - L, box_h - 1 - L), R - L, fill=rim_rgb + (255,))
    d.polygon([(cx - 13 * SS, box_h - L - 2 * SS), (cx + 13 * SS, box_h - L - 2 * SS), (cx, H - 1 - L - 2 * SS)], fill=rim_rgb + (255,))

    # 暖白内芯
    in2 = L + 5 * SS
    d.rounded_rectangle((in2, in2, W - 1 - in2, box_h - 1 - in2), max(4, R - in2), fill=(255, 250, 238, 255))
    return im


def run_ui_chrome():
    UI_DIR.mkdir(parents=True, exist_ok=True)
    print("Generating 2x Wasteland Comic Sticker UI Chrome...", flush=True)

    # 1) 顶栏斜切货币槽 & 左上玩家等级徽章底板
    finish_ss(make_slanted_currency_pill(336, 76), UI_DIR / "hud_currency_pill.png")
    finish_ss(
        make_sticker_box(136, 136, 34, (252, 197, 74), (236, 148, 30), (178, 98, 16), gloss_alpha=120),
        UI_DIR / "hud_level_shield.png",
    )

    # 2) 左下主线任务引导横条 & 胶囊黑底子卡
    finish_ss(
        make_sticker_box(520, 116, 30, (64, 52, 46), (44, 36, 32), (28, 22, 18), gloss_alpha=45),
        UI_DIR / "hud_quest_bar.png",
    )
    finish_ss(
        make_sticker_box(256, 256, 28, (234, 220, 194), (220, 202, 172), (188, 168, 138), gloss_alpha=40, bevel2x=4, drop_shadow_alpha=0),
        UI_DIR / "card_well_beige.png",
    )
    finish_ss(
        make_sticker_box(256, 256, 28, (255, 251, 242), (245, 236, 218), (206, 190, 162), gloss_alpha=65, bevel2x=5, drop_shadow_alpha=25),
        UI_DIR / "card_row_cream.png",
    )

    # 3) 铆钉暖沙米弹窗九宫格壳子 + 关闭圆钮
    finish_ss(make_modal_shell(512, 512), UI_DIR / "modal_shell.png")
    finish_ss(
        make_sticker_box(84, 88, 42, (236, 92, 74), (204, 58, 44), (148, 34, 24), gloss_alpha=110),
        UI_DIR / "btn_close_red.png",
    )

    # 4) 尖角主按钮（明黄 / 生机绿 / 净水蓝 / 暖灰禁用）
    finish_ss(make_pointed_button(352, 104, (255, 212, 88), (244, 168, 34), (192, 118, 16)), UI_DIR / "btn_hex_gold.png")
    finish_ss(make_pointed_button(352, 104, (158, 222, 64), (122, 188, 34), (82, 136, 18)), UI_DIR / "btn_hex_green.png")
    finish_ss(make_pointed_button(352, 104, (98, 196, 255), (52, 152, 232), (28, 104, 176)), UI_DIR / "btn_hex_blue.png")
    finish_ss(make_pointed_button(352, 104, (196, 192, 184), (168, 164, 156), (128, 124, 118)), UI_DIR / "btn_hex_grey.png")

    # 5) 圆角副按钮（小号列表操作按钮）
    finish_ss(
        make_sticker_box(240, 88, 24, (255, 210, 84), (242, 166, 32), (188, 116, 16), gloss_alpha=110),
        UI_DIR / "btn_round_gold.png",
    )
    finish_ss(
        make_sticker_box(240, 88, 24, (156, 220, 62), (120, 186, 32), (80, 134, 18), gloss_alpha=110),
        UI_DIR / "btn_round_green.png",
    )
    finish_ss(
        make_sticker_box(240, 88, 24, (98, 196, 255), (52, 152, 232), (28, 104, 176), gloss_alpha=110),
        UI_DIR / "btn_round_blue.png",
    )

    # 6) 品质物品方框（对标 method.md：1x 96x96 -> 2x 192x192，绿/蓝/紫/橙/暖灰空槽）
    qualities = {
        "slot_q1_green": ((156, 218, 68), (124, 186, 38), (88, 138, 22)),
        "slot_q2_blue": ((76, 182, 245), (44, 148, 220), (26, 104, 168)),
        "slot_q3_purple": ((184, 104, 232), (148, 68, 204), (104, 42, 152)),
        "slot_q4_orange": ((252, 176, 62), (238, 136, 32), (184, 92, 16)),
        "slot_empty": ((218, 204, 180), (198, 182, 156), (164, 148, 124)),
    }
    for name, (t_rgb, b_rgb, bev_rgb) in qualities.items():
        finish_ss(
            make_sticker_box(192, 192, 32, t_rgb, b_rgb, bev_rgb, gloss_alpha=85, bevel2x=5, drop_shadow_alpha=20),
            UI_DIR / f"{name}.png",
        )

    # 7) 进度条黑槽与绿/金填充条
    finish_ss(
        make_sticker_box(320, 44, 22, (46, 40, 36), (58, 50, 46), (34, 28, 24), gloss_alpha=0, bevel2x=2, drop_shadow_alpha=0),
        UI_DIR / "bar_track_dark.png",
    )
    finish_ss(
        make_sticker_box(312, 36, 18, (148, 222, 68), (108, 186, 36), (78, 142, 22), gloss_alpha=90, outline2x=2, bevel2x=2, drop_shadow_alpha=0),
        UI_DIR / "bar_fill_green.png",
    )
    finish_ss(
        make_sticker_box(312, 36, 18, (255, 214, 84), (242, 166, 30), (192, 120, 16), gloss_alpha=90, outline2x=2, bevel2x=2, drop_shadow_alpha=0),
        UI_DIR / "bar_fill_gold.png",
    )

    # 8) 底部 Dock 栏与选中页签块
    dock, act = make_dock_bar_and_active()
    finish_ss(dock, UI_DIR / "dock_bg.png")
    finish_ss(act, UI_DIR / "dock_tab_active.png")

    # 9) 营地建筑悬浮气泡（绿边=可收获/可竣工，金边=可建造/升级，蓝边=生产中）
    finish_ss(make_camp_bubble(132, 146, (138, 203, 41)), UI_DIR / "bubble_green.png")
    finish_ss(make_camp_bubble(132, 146, (248, 168, 38)), UI_DIR / "bubble_gold.png")
    finish_ss(make_camp_bubble(132, 146, (68, 172, 242)), UI_DIR / "bubble_blue.png")

    # 10) 侧面圆形快捷入口底托（订单、自动生产特权等）
    finish_ss(
        make_sticker_box(124, 128, 36, (255, 246, 224), (236, 218, 184), (186, 162, 126), gloss_alpha=95),
        UI_DIR / "hud_side_btn.png",
    )
    print("UI Chrome generation complete!", flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("cmd", choices=["split_and_mat", "ui_chrome", "all"])
    args = parser.parse_args()
    if args.cmd in ("split_and_mat", "all"):
        run_split_and_mat()
    if args.cmd in ("ui_chrome", "all"):
        run_ui_chrome()


if __name__ == "__main__":
    main()
