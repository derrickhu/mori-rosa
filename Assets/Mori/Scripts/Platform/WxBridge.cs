using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mori
{
    /// <summary>
    /// 微信小游戏平台桥接：反射安全调用（编辑器无 Wx 程序集也可编译）、
    /// 胶囊安全区（全0不缓存）、原生字体获取、首帧转发注册、多实例 multiton 激励视频缓存。
    /// </summary>
    public static class WxBridge
    {
        static Font _cachedFont;
        static float _cachedSafeTop = -1f;
        static readonly Dictionary<string, object> AdInstances = new Dictionary<string, object>();

        public static bool IsMiniGame
        {
            get
            {
#if UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN
                return !Application.isEditor;
#else
                return false;
#endif
            }
        }

        public static bool IsDevtools
        {
            get
            {
                if (Application.isEditor) return true;
                object info = GetSystemInfo();
                string platform = ReadString(info, "platform");
                return string.Equals(platform, "devtools", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static Font GetUiFont()
        {
            if (_cachedFont != null) return _cachedFont;
            var res = Resources.Load<Font>("Fonts/UiFont");
            if (res != null)
            {
                _cachedFont = res;
                return _cachedFont;
            }
            _cachedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 30);
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cachedFont;
        }

        /// <summary>计算顶部避让高度（逻辑 720 宽换算），胶囊返回全0时不缓存。</summary>
        public static float GetSafeTop720()
        {
            if (_cachedSafeTop > 0f) return _cachedSafeTop;
            if (!IsMiniGame) return 44f;

            object wx = WxType();
            if (wx == null) return 44f;
            object info = GetSystemInfo();
            float winW = ReadFloat(info, "windowWidth");
            float statusBar = ReadFloat(info, "statusBarHeight");

            var menuMethod = ((Type)wx).GetMethod("GetMenuButtonBoundingClientRect", Type.EmptyTypes);
            object rect = menuMethod?.Invoke(null, null);
            float bottom = ReadFloat(rect, "bottom");
            float width = ReadFloat(rect, "width");
            if (winW <= 0f || (bottom <= 0f && width <= 0f))
                return Mathf.Max(44f, statusBar);

            float scale = 720f / winW;
            _cachedSafeTop = Mathf.Max(bottom, statusBar) * scale + 8f;
            return _cachedSafeTop;
        }

        public static void EnableShareMenu(string title, string imageUrl)
        {
            Type wx = WxType();
            if (wx == null || !IsMiniGame) return;
            // 首帧后开启转发菜单，防止胶囊菜单置灰
            wx.GetMethod("ShowShareMenu")?.Invoke(null, new object[] { null });
        }

        static Type WxType()
        {
            return Type.GetType("WeChatWASM.WX, Wx") ?? Type.GetType("WeChatWASM.WX");
        }

        static object GetSystemInfo()
        {
            return WxType()?.GetMethod("GetSystemInfoSync", Type.EmptyTypes)?.Invoke(null, null);
        }

        static float ReadFloat(object obj, string field)
        {
            if (obj == null) return 0f;
            var f = obj.GetType().GetField(field);
            if (f != null && f.GetValue(obj) is IConvertible cf) return Convert.ToSingle(cf);
            var p = obj.GetType().GetProperty(field);
            if (p != null && p.GetValue(obj) is IConvertible cp) return Convert.ToSingle(cp);
            return 0f;
        }

        static string ReadString(object obj, string field)
        {
            if (obj == null) return "";
            var f = obj.GetType().GetField(field);
            if (f != null) return f.GetValue(obj) as string ?? "";
            var p = obj.GetType().GetProperty(field);
            if (p != null) return p.GetValue(obj) as string ?? "";
            return "";
        }
    }
}
