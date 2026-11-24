#!/usr/bin/env python3
"""为《末日囤货狂》构建微信小游戏内嵌中文字体子集 (MoriBold.ttf)。

参考 black-rosa 方案：从 NotoSansSC[wght].ttf (OFL 1.1 免费商用) 按 Assets/Mori/ 下
所有 C# 脚本字符串字面量切出轻量级子集 (~300KB)，放入 Resources/Fonts/MoriBold.ttf，
解决微信小游戏模拟器及真机上无系统回落字体导致的中文不显示问题。
"""
import pathlib
import unicodedata
from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

REPO = pathlib.Path(__file__).resolve().parents[1]
SCRIPTS = REPO / "Assets" / "Mori"
OUT_FONT = REPO / "Assets" / "Mori" / "ForestSlice" / "Resources" / "Fonts" / "MoriBold.ttf"
SRC_FONT = pathlib.Path("/Users/rosa/rosa_games/game_assets/black-rosa/字体/NotoSansSC[wght].ttf")

EXTRAS = (
    "，。！？：；、…—·「」『』（）《》【】％×÷≤≥°＋－＝／★☆✔✕✓✗"
    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"
)


def is_cjk_or_symbol(ch: str) -> bool:
    if ord(ch) < 0x80:
        return False
    return unicodedata.category(ch) not in ("Cc", "Cf", "Cs", "Co", "Cn", "Zl", "Zp")


def scan_cs_strings(src: str) -> str:
    out = []
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        if c == "/" and i + 1 < n and src[i + 1] == "/":
            i = src.find("\n", i)
            if i < 0:
                break
        elif c == "/" and i + 1 < n and src[i + 1] == "*":
            i = src.find("*/", i + 2)
            if i < 0:
                break
            i += 2
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                i += 2 if src[i] == "\\" else 1
            i += 1
        elif c == "@" and i + 1 < n and src[i + 1] == '"':
            i += 2
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i + 1] == '"':
                        out.append('"')
                        i += 2
                        continue
                    i += 1
                    break
                out.append(src[i])
                i += 1
        elif c == '"':
            i += 1
            while i < n and src[i] != '"':
                if src[i] == "\\":
                    i += 2
                    continue
                if src[i] == "\n":
                    break
                out.append(src[i])
                i += 1
            i += 1
        else:
            i += 1
    return "".join(out)


def main():
    if not SRC_FONT.is_file():
        raise SystemExit(f"找不到母版字体：{SRC_FONT}")

    chars = set(EXTRAS)
    cs_files = sorted(SCRIPTS.rglob("*.cs"))
    for p in cs_files:
        text = scan_cs_strings(p.read_text(encoding="utf-8", errors="replace"))
        for ch in text:
            if is_cjk_or_symbol(ch):
                chars.add(ch)

    sorted_text = "".join(sorted(chars))
    print(f"扫描 {len(cs_files)} 个脚本，共收集 {len(chars)} 个唯一字符")

    opts = subset.Options()
    opts.drop_tables += ["BASE", "DSIG", "vhea", "vmtx", "VORG"]
    opts.recalc_bounds = True
    opts.notdef_outline = True
    opts.name_IDs = ["*"]
    opts.name_legacy = True

    font = TTFont(str(SRC_FONT), fontNumber=0)
    subsetter = subset.Subsetter(options=opts)
    subsetter.populate(text=sorted_text)
    subsetter.subset(font)

    instancer.instantiateVariableFont(font, {"wght": 700}, inplace=True, updateFontNames=False)

    family = "Mori Sans SC"
    style = "Bold"
    names = {
        1: family,
        2: style,
        4: f"{family} {style}",
        6: "MoriSansSC-Bold",
        16: family,
        17: style,
    }
    for rec in font["name"].names:
        if rec.nameID in names:
            rec.string = names[rec.nameID]

    OUT_FONT.parent.mkdir(parents=True, exist_ok=True)
    font.save(str(OUT_FONT))
    font.close()
    print(f"✔ 生成微信小游戏内嵌字体：{OUT_FONT} ({OUT_FONT.stat().st_size / 1024:.1f} KB)")


if __name__ == "__main__":
    main()
