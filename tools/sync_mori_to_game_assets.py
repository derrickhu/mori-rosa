#!/usr/bin/env python3
"""
Organizes《末日囤货狂》(mori-rosa) promotional, loading, preview, and game art assets into
/Users/rosa/rosa_games/game_assets/mori-rosa/ following the exact directory conventions of black-rosa & hot-pot:
  - loading/
  - 推广/微信头像/{raw,final,preview}/
  - 推广/宣传图/{raw,final}/
  - 预览/forest_slice_v2/
  - assets/forest_slice/{raw,final}/
Also keeps only the chosen Option B app icon inside Unity Assets/Mori/AppIcon/app_icon_hoarder.png
and removes unused promo icon files from the Unity project repo.
"""
import os
import shutil
from PIL import Image

BRAIN = "/Users/rosa/.gemini/antigravity/brain/1b3cbc05-17b2-44a9-ba22-41684be97e65"
REPO = "/Users/rosa/rosa_games/mori-rosa"
GA_ROOT = "/Users/rosa/rosa_games/game_assets/mori-rosa"


def ensure_dirs(*paths):
    for p in paths:
        os.makedirs(p, exist_ok=True)


def main():
    # 1. Target directories in /Users/rosa/rosa_games/game_assets/mori-rosa
    loading_dir = os.path.join(GA_ROOT, "loading")
    wx_icon_raw = os.path.join(GA_ROOT, "推广/微信头像/raw")
    wx_icon_final = os.path.join(GA_ROOT, "推广/微信头像/final")
    wx_icon_prev = os.path.join(GA_ROOT, "推广/微信头像/preview")
    promo_raw = os.path.join(GA_ROOT, "推广/宣传图/raw")
    promo_final = os.path.join(GA_ROOT, "推广/宣传图/final")
    preview_v2 = os.path.join(GA_ROOT, "预览/forest_slice_v2")
    fs_raw = os.path.join(GA_ROOT, "assets/forest_slice/raw")
    fs_final = os.path.join(GA_ROOT, "assets/forest_slice/final")

    ensure_dirs(
        loading_dir, wx_icon_raw, wx_icon_final, wx_icon_prev,
        promo_raw, promo_final, preview_v2, fs_raw, fs_final
    )

    # 2. Loading assets -> game_assets/mori-rosa/loading/
    loading_src_jpg = os.path.join(BRAIN, "loading_hoarder_9x16_1795345254942.jpg")
    loading_repo_png = os.path.join(REPO, "Assets/Mori/ForestSlice/Resources/ForestArt/loading_hoarder.png")
    baked_cover_png = os.path.join(REPO, "Assets/Mori/WeChatIcons/wechat_loading_cover_750x1334.png")
    loading_ui_prev = os.path.join(BRAIN, "loading_screen_preview.png")

    shutil.copy2(loading_src_jpg, os.path.join(loading_dir, "loading_raw_9x16.jpg"))
    shutil.copy2(loading_repo_png, os.path.join(loading_dir, "loading_bg_900x1600.png"))
    if os.path.exists(baked_cover_png):
        shutil.copy2(baked_cover_png, os.path.join(loading_dir, "wechat_loading_cover_750x1334.png"))
        Image.open(baked_cover_png).convert("RGB").save(
            os.path.join(loading_dir, "wechat_loading_cover_750x1334.jpg"), quality=93
        )
    if os.path.exists(loading_ui_prev):
        shutil.copy2(loading_ui_prev, os.path.join(loading_dir, "loading_ui_preview_750x1334.png"))

    # 3. WeChat Mini-Game Icons -> game_assets/mori-rosa/推广/微信头像/{raw,final,preview}
    icon_defs = [
        ("v1_pickup", os.path.join(BRAIN, "wx_icon_01_pickup_1795345378252.jpg"), False),
        ("v2_hoarder_selected", os.path.join(BRAIN, "wx_icon_02_hoarder_1795345299583.jpg"), True),
        ("v3_contrast", os.path.join(BRAIN, "wx_icon_03_contrast_1795345392557.jpg"), False),
        ("v4_king", os.path.join(BRAIN, "wx_icon_04_king_1795345330277.jpg"), False),
    ]

    app_icon_dir = os.path.join(REPO, "Assets/Mori/AppIcon")
    ensure_dirs(app_icon_dir)

    for tag, src_jpg, is_selected in icon_defs:
        shutil.copy2(src_jpg, os.path.join(wx_icon_raw, f"wechat_icon_{tag}_raw.jpg"))
        im = Image.open(src_jpg).convert("RGB")
        im_512 = im.resize((512, 512), Image.Resampling.LANCZOS)
        im_256 = im.resize((256, 256), Image.Resampling.LANCZOS)
        im_144 = im.resize((144, 144), Image.Resampling.LANCZOS)

        im_512.save(os.path.join(wx_icon_final, f"wechat_icon_{tag}_512.png"), optimize=True)
        im_256.save(os.path.join(wx_icon_final, f"wechat_icon_{tag}_256.png"), optimize=True)
        im_144.save(os.path.join(wx_icon_final, f"wechat_icon_{tag}_144.png"), optimize=True)

        if is_selected:
            # Also place chosen Option B right at top-level convenient alias & in Unity AppIcon/
            im_512.save(os.path.join(wx_icon_final, "selected_icon_512.png"), optimize=True)
            im_144.save(os.path.join(wx_icon_final, "selected_icon_144.png"), optimize=True)
            im_512.save(os.path.join(app_icon_dir, "app_icon_hoarder_512.png"), optimize=True)

    comp_board = os.path.join(BRAIN, "wx_icons_comparison_board.png")
    if os.path.exists(comp_board):
        shutil.copy2(comp_board, os.path.join(wx_icon_prev, "wechat_icons_comparison_board.png"))

    # 4. Vertical 720x1280 store promo images -> game_assets/mori-rosa/推广/宣传图/final/
    promo_sources = [
        ("promo_p1_loading_keyart", loading_ui_prev),
        ("promo_p2_full_camp_v2", os.path.join(BRAIN, "econ_06_full_camp_v2_buildings.png")),
        ("promo_p3_tailor_workshop", os.path.join(BRAIN, "econ_03_tailor_workshop_drawer.png")),
        ("promo_p4_orders_board", os.path.join(BRAIN, "econ_04_multi_orders_board.png")),
        ("promo_p5_city_pickup_drive", os.path.join(BRAIN, "ui_08_city_joystick_drive.png")),
    ]
    for pname, psrc in promo_sources:
        if os.path.exists(psrc):
            shutil.copy2(psrc, os.path.join(promo_raw, f"{pname}_raw.png"))
            pim = Image.open(psrc).convert("RGB").resize((720, 1280), Image.Resampling.LANCZOS)
            pim.save(os.path.join(promo_final, f"{pname}_720x1280.jpg"), quality=92)

    # 5. Latest V2 screenshots -> game_assets/mori-rosa/预览/forest_slice_v2/
    v2_shots = [
        "loading_screen_preview.png",
        "replan_01_spacious_camp_default.png",
        "econ_01_warehouse_build_card_jumps.png",
        "econ_02_source_route_popup.png",
        "econ_03_tailor_workshop_drawer.png",
        "econ_04_multi_orders_board.png",
        "econ_05_caravan_trade_modal.png",
        "econ_06_full_camp_v2_buildings.png",
        "cast_01_all5_lineup.png",
        "ui_07_fishing_pushin.png",
        "ui_08_city_joystick_drive.png",
    ]
    for s in v2_shots:
        sp = os.path.join(BRAIN, s)
        if os.path.exists(sp):
            shutil.copy2(sp, os.path.join(preview_v2, s))

    # 6. Sync ForestArt final sprites & raw V2 sheets -> game_assets/mori-rosa/assets/forest_slice/
    for fname in os.listdir(BRAIN):
        if fname.endswith((".jpg", ".png")) and ("sheet" in fname or "atlas" in fname or "keyart" in fname):
            shutil.copy2(os.path.join(BRAIN, fname), os.path.join(fs_raw, fname))

    forest_art_dir = os.path.join(REPO, "Assets/Mori/ForestSlice/Resources/ForestArt")
    for fname in os.listdir(forest_art_dir):
        if fname.endswith(".png"):
            shutil.copy2(os.path.join(forest_art_dir, fname), os.path.join(fs_final, fname))

    # 7. Clean up temporary promo folder inside Unity Assets/ so WeChat package stays lean
    tmp_wx_in_repo = os.path.join(REPO, "Assets/Mori/WeChatIcons")
    if os.path.exists(tmp_wx_in_repo):
        shutil.rmtree(tmp_wx_in_repo)
    tmp_wx_meta = tmp_wx_in_repo + ".meta"
    if os.path.exists(tmp_wx_meta):
        os.remove(tmp_wx_meta)

    print("SYNC_GAME_ASSETS_COMPLETE")


if __name__ == "__main__":
    main()
