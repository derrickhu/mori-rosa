#!/usr/bin/env python3
"""将《末日囤货狂》(mori-rosa) 微信小游戏首资源包上传至 CloudBase COS CDN。

完全沿用 black-rosa 的同款 CloudBase 环境与本机登录态 (~/.config/.cloudbase/auth.json)，
按独立前缀 mori-rosa/StreamingAssets/ 存放哈希首包，不删除、不影响云上任何旧文件或其他项目。
"""

import hashlib
import hmac
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONFIG_ASSET = ROOT / "Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset"
WX_KIT_CONF = ROOT / "wechat-minigame/minigame/wx-game-kit/config.json"
AUTH = Path.home() / ".config/.cloudbase/auth.json"

ENV_ID = "rosa-env-d7grf78r5dbd37323"
REGION = "ap-shanghai"
BUCKET = "726f-rosa-env-d7grf78r5dbd37323-1414200063"
COS_PREFIX = "mori-rosa/StreamingAssets/"

DIRECT_DOMAINS = (
    ".myqcloud.com",
    ".qcloud.la",
    ".qcloud.com",
    ".tencent.com",
    ".tencentcloudapi.com",
)
_DIRECT_OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def open_url(req, timeout):
    host = (urllib.parse.urlparse(req.full_url).hostname or "").lower()
    if any(host == d.lstrip(".") or host.endswith(d) for d in DIRECT_DOMAINS):
        return _DIRECT_OPENER.open(req, timeout=timeout)
    return urllib.request.urlopen(req, timeout=timeout)


def fail(msg):
    print(msg, file=sys.stderr)
    return 1


def resolve_cdn_and_data():
    if not WX_KIT_CONF.is_file():
        raise SystemExit(f"未找到导出配置 {WX_KIT_CONF}")
    kit = json.loads(WX_KIT_CONF.read_text(encoding="utf-8"))
    cdn = (kit.get("network", {}).get("dataFileCDN") or "").strip()
    if not cdn:
        for line in CONFIG_ASSET.read_text(encoding="utf-8").splitlines():
            s = line.strip()
            if s.startswith("CDN:"):
                cdn = s.split(":", 1)[1].strip()
                break
    if not cdn or not cdn.endswith("/"):
        raise SystemExit(f"CDN 地址无效（需以 / 结尾）：{cdn!r}")

    data_info = kit.get("launch", {}).get("data", {})
    md5 = data_info.get("fileMD5")
    is_compressed = bool(data_info.get("isCompressed", True))
    if not md5:
        raise SystemExit("wx-game-kit/config.json 缺少 launch.data.fileMD5")

    webgl_dir = ROOT / "wechat-minigame/webgl"
    src_br = webgl_dir / f"{md5}.data.bin.br"
    src_raw = webgl_dir / f"{md5}.data.bin"
    if is_compressed and src_br.is_file():
        local_file = src_br
        remote_names = [f"{md5}.data.bin.br", f"{md5}.data.bin"]
    elif src_raw.is_file():
        local_file = src_raw
        remote_names = [f"{md5}.data.bin"]
    else:
        raise SystemExit(f"找不到首资源包：{src_br} 或 {src_raw}")

    return cdn, local_file, remote_names


def mac_hash():
    node = uuid.getnode()
    mac = ":".join(f"{(node >> shift) & 0xFF:02x}" for shift in range(40, -1, -8))
    return hashlib.md5(mac.encode()).hexdigest()


def refresh(cred):
    body = {
        "tmpSecretId": cred["tmpSecretId"],
        "tmpSecretKey": cred["tmpSecretKey"],
        "tmpToken": cred["tmpToken"],
        "tmpExpired": cred["tmpExpired"],
        "expired": cred["expired"],
        "authTime": cred["authTime"],
        "refreshToken": cred["refreshToken"],
        "uin": cred["uin"],
        "hash": mac_hash(),
        "envId": ENV_ID,
    }
    req = urllib.request.Request(
        "https://iaas.cloud.tencent.com/tcb_refresh",
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with open_url(req, timeout=20) as resp:
            payload = json.loads(resp.read().decode())
    except urllib.error.HTTPError as err:
        detail = err.read().decode(errors="replace")[:200]
        raise SystemExit(f"云登录刷新失败 {err.code} {detail}") from err
    if payload.get("code") not in (0, None) or not payload.get("data"):
        raise SystemExit("云登录刷新失败，请重新登录 CloudBase 后再上传首包")
    merged = dict(cred)
    merged.update(payload["data"])
    return merged


def try_refresh_via_cloudbase_cli() -> bool:
    import glob
    import subprocess

    cli_bins = sorted(glob.glob(str(Path.home() / ".npm/_npx/*/node_modules/.bin/cloudbase")))
    node_bins = sorted(glob.glob(str(Path.home() / ".nvm/versions/node/*/bin")))
    env = dict(os.environ)
    extra_paths = [p for p in node_bins + ["/opt/homebrew/bin", "/usr/local/bin"] if os.path.isdir(p)]
    if extra_paths:
        env["PATH"] = ":".join(extra_paths) + ":" + env.get("PATH", "")

    for cli in reversed(cli_bins):
        try:
            res = subprocess.run([cli, "env:list"], env=env, capture_output=True, text=True, timeout=25)
            if res.returncode == 0:
                return True
        except Exception:
            pass
    return False


def load_auth():
    if not AUTH.is_file():
        raise SystemExit(f"未找到本机 CloudBase 登录凭证：{AUTH}")
    data = json.loads(AUTH.read_text(encoding="utf-8"))
    cred = data["credential"]
    now = int(time.time() * 1000)
    if cred.get("tmpExpired", 0) - now < 5 * 60 * 1000:
        if try_refresh_via_cloudbase_cli():
            data = json.loads(AUTH.read_text(encoding="utf-8"))
            return data["credential"]
        cred = refresh(cred)
        data["credential"] = cred
        tmp = AUTH.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        os.replace(tmp, AUTH)
    return cred


def sign(secret_id, secret_key, method, uri, headers):
    now = int(time.time())
    key_time = f"{now};{now + 3600}"
    sign_key = hmac.new(secret_key.encode(), key_time.encode(), hashlib.sha1).hexdigest()
    items = []
    for key in sorted(headers):
        val = urllib.parse.quote(str(headers[key]), safe="-_.~")
        items.append(f"{key}={val}")
    header_list = ";".join(sorted(headers))
    http_string = f"{method.lower()}\n{uri}\n\n{'&'.join(items)}\n"
    hashed = hashlib.sha1(http_string.encode()).hexdigest()
    string_to_sign = f"sha1\n{key_time}\n{hashed}\n"
    signature = hmac.new(sign_key.encode(), string_to_sign.encode(), hashlib.sha1).hexdigest()
    return (
        "q-sign-algorithm=sha1"
        f"&q-ak={secret_id}"
        f"&q-sign-time={key_time}"
        f"&q-key-time={key_time}"
        f"&q-header-list={header_list}"
        "&q-url-param-list="
        f"&q-signature={signature}"
    )


def cos(method, key, cred, body=None, content_type=None):
    host = f"{BUCKET}.cos.{REGION}.myqcloud.com"
    uri = "/" + urllib.parse.quote(key, safe="/")
    headers = {
        "host": host,
        "x-cos-security-token": cred["tmpToken"],
    }
    data = None
    if body is not None:
        data = body
        headers["content-length"] = str(len(body))
        headers["content-type"] = content_type or "application/octet-stream"
    auth = sign(cred["tmpSecretId"], cred["tmpSecretKey"], method, uri, headers)
    req_headers = {
        "Host": host,
        "Authorization": auth,
        "x-cos-security-token": cred["tmpToken"],
    }
    if body is not None:
        req_headers["Content-Length"] = headers["content-length"]
        req_headers["Content-Type"] = headers["content-type"]
    req = urllib.request.Request(
        f"https://{host}{uri}",
        data=data,
        headers=req_headers,
        method=method,
    )
    try:
        with open_url(req, timeout=180) as resp:
            resp.read()
            return resp.status, resp.headers
    except urllib.error.HTTPError as err:
        detail = err.read().decode(errors="replace")
        return err.code, detail


def upload_one(cred, local_bytes: bytes, remote_name: str, cdn_base: str) -> int:
    key = COS_PREFIX + remote_name
    public_url = cdn_base + remote_name
    code, headers = cos("HEAD", key, cred)
    if code == 200 and int(headers.get("Content-Length", "0")) == len(local_bytes):
        print(f"首包已在云端，无需重传: {public_url}")
        return 0
    if code not in (200, 404):
        text = headers if isinstance(headers, str) else ""
        return fail(f"检查云端首包失败 {code} {text[:180]}")

    print(f"正在上传首包 {remote_name} ({len(local_bytes) / (1024 * 1024):.2f} MB) -> {key} ...")
    code, detail = cos("PUT", key, cred, local_bytes)
    if code not in (200, 201):
        text = detail if isinstance(detail, str) else ""
        return fail(f"上传首包失败 {code} {text[:180]}")

    code, headers = cos("HEAD", key, cred)
    size = int(headers.get("Content-Length", "0")) if code == 200 else -1
    if code != 200 or size != len(local_bytes):
        return fail(f"上传后校验文件大小不一致: {code} size={size}")

    req = urllib.request.Request(public_url, method="GET")
    req.add_header("Range", "bytes=0-15")
    try:
        with open_url(req, timeout=30) as resp:
            got = resp.read(16)
    except urllib.error.HTTPError as err:
        return fail(f"公网 CDN 地址无法访问 {err.code}: {public_url}")
    if got != local_bytes[:16]:
        return fail(f"公网 CDN 首字节校验不一致: {public_url}")

    print(f"✔ 首包已上传并通过公网校验: {public_url}")
    return 0


def main():
    cdn_base, local_file, remote_names = resolve_cdn_and_data()
    cred = load_auth()
    local_bytes = local_file.read_bytes()
    for rname in remote_names:
        rc = upload_one(cred, local_bytes, rname, cdn_base)
        if rc != 0:
            return rc
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except SystemExit as err:
        if err.code not in (0, None) and not isinstance(err.code, int):
            print(err.code, file=sys.stderr)
            sys.exit(1)
        raise
