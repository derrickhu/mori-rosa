#!/usr/bin/env python3
"""
Exports the finalized《末日囤货狂》9:16 Loading Key Art into Resources/ForestArt/loading_hoarder.png,
exports all 4 WeChat Mini-Game icon candidates at 512x512 and 144x144 into Assets/Mori/WeChatIcons/,
and renders a WeChat real-UI comparison sheet (large rounded + mini pull-down circle preview).
"""
import os
from PIL import Image, ImageDraw, ImageFont

BRAIN = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65"
REPO = "/Users/rosa/rosa_games/mori-rosa"
RES_ART = os.path.join(REPO, "Assets/Mori/ForestSlice/Resources/ForestArt")
WX_DIR = os.path.join(REPO, "Assets/Mori/WeChatIcons")

LOADING_SRC = os.path.join(BRAIN, "loading_hoarder_9x16_1795345254942.jpg")
ICONS = [
    ("wx_icon_01_pickup", "方案 A · 飞车进货", os.path.join(BRAIN, "wx_icon_01_pickup_1795345378252.jpg")),
    ("wx_icon_02_hoarder", "方案 B · 狂喜抱宝", os.path.join(BRAIN, "wx_icon_02_hoarder_1795345299583.jpg")),
    ("wx_icon_03_contrast", "方案 C · 尸外吃肉", os.path.join(BRAIN, "wx_icon_03_contrast_1795345392557.jpg")),
    ("wx_icon_04_king", "方案 D · 物资山王", os.path.join(BRAIN, "wx_icon_04_king_1795345330277.jpg")),
]


def load_cjk_font(size: int):
    candidates = [
        "/System/Library/Fonts/PingFang.ttc",
        "/System/Library/Fonts/STHeiti Medium.ttc",
        "/System/Library/Fonts/Hiragino Sans GB.ttc",
        "/Library/Fonts/Arial Unicode.ttf",
    ]
    for p in candidates:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()


def round_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size * 2, size * 2), 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle((0, 0, size * 2 - 1, size * 2 - 1), radius=radius * 2, fill=255)
    return mask.resize((size, size), Image.Resampling.LANCZOS)


def main():
    os.makedirs(RES_ART, exist_ok=True)
    os.makedirs(WX_DIR, exist_ok=True)

    # 1. Export 9:16 Loading key art (900x1600) into Resources/ForestArt/loading_hoarder.png
    ld = Image.open(LOADING_SRC).convert("RGB")
    ld_900 = ld.resize((900, 1600), Image.Resampling.LANCZOS)
    ld_out = os.path.join(RES_ART, "loading_hoarder.png")
    ld_900.save(ld_out, optimize=True)
    print(f"Saved loading art -> {ld_out} ({ld_900.size})")

    # Also save a standalone baked WeChat native cover (750x1334) with full 健康游戏忠告
    cover = ld.resize((750, 1334), Image.Resampling.LANCZOS).convert("RGBA")
    grad = Image.new("RGBA", (750, 235), (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    for y in range(235):
        alpha = int(min(230, (y / 234.0) ** 1.1 * 230))
        gd.line([(0, y), (750, y)], fill=(18, 13, 9, alpha))
    cover.alpha_composite(grad, (0, 1334 - 235))
    cd = ImageDraw.Draw(cover)
    f_hdr = load_cjk_font(19)
    f_body = load_cjk_font(17)
    lines = [
        "《健康游戏忠告》",
        "抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗上当。",
        "适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。",
    ]
    ys = [1334 - 110, 1334 - 78, 1334 - 50]
    cols = [(236, 206, 152, 255), (226, 218, 206, 250), (226, 218, 206, 250)]
    for txt, yy, col, fnt in zip(lines, ys, cols, [f_hdr, f_body, f_body]):
        bbox = cd.textbbox((0, 0), txt, font=fnt)
        tw = bbox[2] - bbox[0]
        tx = (750 - tw) // 2
        cd.text((tx + 1, yy + 1), txt, font=fnt, fill=(0, 0, 0, 220))
        cd.text((tx, yy), txt, font=fnt, fill=col)
    baked_cover_path = os.path.join(WX_DIR, "wechat_loading_cover_750x1334.png")
    cover.convert("RGB").save(baked_cover_path, quality=95)
    print(f"Saved baked WeChat cover -> {baked_cover_path}")

    # Also render full Loading UI preview (with comic progress bar + 健康游戏忠告) into artifact dir
    ui_prev = cover.copy()
    ud = ImageDraw.Draw(ui_prev)
    bx0, by0, bx1, by1 = 85, 1334 - 168, 665, 1334 - 128
    ud.rounded_rectangle((bx0, by0, bx1, by1), radius=20, fill=(38, 26, 18, 245), outline=(14, 9, 6, 255), width=3)
    fill_w = int((bx1 - bx0 - 8) * 0.72)
    ud.rounded_rectangle((bx0 + 4, by0 + 4, bx0 + 4 + fill_w, by1 - 4), radius=16, fill=(238, 146, 36, 255))
    ud.rounded_rectangle((bx0 + 8, by0 + 6, bx0 + fill_w - 4, by0 + 13), radius=6, fill=(255, 198, 96, 180))
    f_bar = load_cjk_font(17)
    btxt = "正在把整条街物资搬回山寨... 72%"
    bb = ud.textbbox((0, 0), btxt, font=f_bar)
    btw = bb[2] - bb[0]
    btx = (750 - btw) // 2
    ud.text((btx + 1, by0 + 9), btxt, font=f_bar, fill=(20, 12, 6, 230))
    ud.text((btx, by0 + 8), btxt, font=f_bar, fill=(255, 255, 255, 255))
    prev_path = os.path.join(BRAIN, "loading_screen_preview.png")
    ui_prev.convert("RGB").save(prev_path, quality=95)
    print(f"Saved loading UI preview -> {prev_path}")

    # 2. Export 4 WeChat icons (512x512 & 144x144) + build comparison board
    board_w, board_h = 1480, 760
    board = Image.new("RGBA", (board_w, board_h), (30, 28, 34, 255))
    bd = ImageDraw.Draw(board)

    f_title = load_cjk_font(28)
    f_sub = load_cjk_font(22)
    f_wx = load_cjk_font(19)

    bd.text((48, 28), "《末日囤货狂》微信小游戏图标 4 套备选对比（上：高清圆角大图 / 下：微信下拉栏真实圆形小图标效果）",
            font=f_title, fill=(250, 224, 168, 255))

    # Simulated WeChat "最近使用的小游戏" dark panel at bottom
    bd.rounded_rectangle((44, 510, board_w - 44, 726), radius=24, fill=(44, 44, 50, 255))
    bd.text((74, 528), "微信下拉栏「最近使用的小游戏」真实尺寸预览：", font=f_sub, fill=(170, 172, 180, 255))

    sq_mask = round_mask(300, 62)
    circ_mask = round_mask(96, 48)

    for idx, (slug, label, src_path) in enumerate(ICONS):
        im = Image.open(src_path).convert("RGB")
        im_512 = im.resize((512, 512), Image.Resampling.LANCZOS)
        im_144 = im.resize((144, 144), Image.Resampling.LANCZOS)

        p512 = os.path.join(WX_DIR, f"{slug}_512.png")
        p144 = os.path.join(WX_DIR, f"{slug}_144.png")
        im_512.save(p512, optimize=True)
        im_144.save(p144, optimize=True)
        # Also copy clean 512 PNG into artifact dir for direct user viewing
        im_512.save(os.path.join(BRAIN, f"{slug}.png"), optimize=True)
        print(f"Saved icon {slug} -> 512 & 144")

        cx = 215 + idx * 350
        # Top large rounded-square icon
        im_300 = im.resize((300, 300), Image.Resampling.LANCZOS).convert("RGBA")
        card = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
        card.paste(im_300, (0, 0), sq_mask)
        board.alpha_composite(card, (cx - 150, 96))

        bb = bd.textbbox((0, 0), label, font=f_sub)
        lw = bb[2] - bb[0]
        bd.text((cx - lw // 2, 412), label, font=f_sub, fill=(255, 240, 210, 255))

        # Bottom WeChat mini circle icon + 5-char game name
        im_96 = im.resize((96, 96), Image.Resampling.LANCZOS).convert("RGBA")
        mini = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        mini.paste(im_96, (0, 0), circ_mask)
        board.alpha_composite(mini, (cx - 48, 572))

        gname = "末日囤货狂"
        gb = bd.textbbox((0, 0), gname, font=f_wx)
        gw = gb[2] - gb[0]
        bd.text((cx - gw // 2, 680), gname, font=f_wx, fill=(235, 235, 240, 255))

    board_out = os.path.join(BRAIN, "wx_icons_comparison_board.png")
    board.convert("RGB").save(board_out, quality=95)
    print(f"Saved comparison board -> {board_out}")


if __name__ == "__main__":
    main()
