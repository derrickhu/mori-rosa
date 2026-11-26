using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mori.ForestSlice
{
    public enum FlipbookPlayMode
    {
        LoopAll = 0,         // 0, 1, ..., N-1, 0...
        PingPongAll = 1,     // 0, 1, 2, 1, 0... (smooth 3-frame work & animal loops)
        SplitMoveAttack = 2, // Move loops frames [0 .. N/2 - 1], Attack plays frames [N/2 .. N - 1]
    }

    /// <summary>
    /// 零 GC 运行时横向序列帧切分与播放器：
    /// 统一按帧数等宽切分 Resources/ForestArt/{stripKey}，支持驻点工匠/动物 PingPong 循环、
    /// 道路居民 LoopAll 行走循环以及丧尸/首领 SplitMoveAttack 移动+攻击双段状态切换。
    /// </summary>
    public sealed class ForestSpriteFlipbook
    {
        readonly struct StripCacheKey : IEquatable<StripCacheKey>
        {
            public readonly string stripKey;
            public readonly int frameCount;
            public readonly int ppuMilli;
            public readonly int pivotMilli;

            public StripCacheKey(string key, int count, float ppu, float pivotY)
            {
                stripKey = key ?? string.Empty;
                frameCount = count;
                ppuMilli = Mathf.RoundToInt(ppu * 100f);
                pivotMilli = Mathf.RoundToInt(pivotY * 10000f);
            }

            public bool Equals(StripCacheKey other)
            {
                return frameCount == other.frameCount &&
                       ppuMilli == other.ppuMilli &&
                       pivotMilli == other.pivotMilli &&
                       string.Equals(stripKey, other.stripKey, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is StripCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = stripKey != null ? StringComparer.Ordinal.GetHashCode(stripKey) : 0;
                    h = (h * 397) ^ frameCount;
                    h = (h * 397) ^ ppuMilli;
                    h = (h * 397) ^ pivotMilli;
                    return h;
                }
            }
        }

        static readonly Dictionary<StripCacheKey, Sprite[]> FrameCache = new Dictionary<StripCacheKey, Sprite[]>(32);

        public string StripKey { get; private set; }
        public int FrameCount { get; private set; }
        public bool IsLoaded { get; private set; }
        public Sprite CurrentSprite { get; private set; }

        readonly FlipbookPlayMode _mode;
        readonly float _fps;
        readonly Sprite _fallbackSprite;
        readonly float _ppu;
        readonly float _pivotY;

        Sprite[] _frames;
        float _phase;
        float _attackPhase;
        bool _wasAttacking;
        float _retryCooldown;

        public static Sprite[] LoadStripFrames(string stripKey, int frameCount, float ppu = 256f, float pivotY = 0.08f)
        {
            if (string.IsNullOrEmpty(stripKey) || frameCount <= 0)
                return null;

            float safePpu = ppu > 1f ? ppu : 256f;
            var cacheKey = new StripCacheKey(stripKey, frameCount, safePpu, pivotY);
            if (FrameCache.TryGetValue(cacheKey, out Sprite[] cached) &&
                cached != null &&
                cached.Length == frameCount &&
                cached[0] != null &&
                cached[0].texture != null)
            {
                return cached;
            }

            string resPath = "ForestArt/" + stripKey;
            Texture2D tex = Resources.Load<Texture2D>(resPath);
            if (tex == null)
            {
                Sprite spAsset = Resources.Load<Sprite>(resPath);
                if (spAsset != null)
                    tex = spAsset.texture;
            }

            if (tex == null || tex.width < frameCount || tex.height <= 0)
                return null;

            int fwInt = tex.width / frameCount;
            if (fwInt <= 0)
                return null;

            float fw = fwInt;
            float fh = tex.height;
            Vector2 pivot = new Vector2(0.5f, pivotY);
            var frames = new Sprite[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                var sp = Sprite.Create(tex, new Rect(i * fw, 0f, fw, fh), pivot, safePpu, 0, SpriteMeshType.FullRect);
                sp.name = stripKey + "_f" + i;
                sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
                frames[i] = sp;
            }

            FrameCache[cacheKey] = frames;
            return frames;
        }

        public ForestSpriteFlipbook(
            string stripKey,
            int frameCount,
            FlipbookPlayMode mode = FlipbookPlayMode.LoopAll,
            float fps = 7f,
            Sprite fallbackSprite = null,
            float ppu = 256f,
            float pivotY = 0.08f,
            float phaseOffset = 0f)
        {
            StripKey = stripKey ?? string.Empty;
            FrameCount = Mathf.Max(1, frameCount);
            _mode = mode;
            _fps = Mathf.Max(0.1f, fps);
            _fallbackSprite = fallbackSprite;
            _ppu = ppu > 1f ? ppu : 256f;
            _pivotY = pivotY;
            _phase = Mathf.Max(0f, phaseOffset);
            _attackPhase = 0f;
            _wasAttacking = false;

            _frames = LoadStripFrames(StripKey, FrameCount, _ppu, _pivotY);
            IsLoaded = _frames != null && _frames.Length == FrameCount && _frames[0] != null;
            _retryCooldown = IsLoaded ? 0f : 1.0f;
            CurrentSprite = IsLoaded ? _frames[0] : _fallbackSprite;
        }

        public Sprite Sample(float dt, bool moving = true, bool attacking = false, float speedMul = 1f)
        {
            float safeDt = dt > 0f ? dt : 0f;

            if (!IsLoaded || _frames == null || _frames.Length == 0 || _frames[0] == null)
            {
                _retryCooldown -= safeDt;
                if (_retryCooldown <= 0f)
                {
                    _retryCooldown = 1.25f;
                    _frames = LoadStripFrames(StripKey, FrameCount, _ppu, _pivotY);
                    IsLoaded = _frames != null && _frames.Length == FrameCount && _frames[0] != null;
                }

                if (!IsLoaded)
                {
                    CurrentSprite = _fallbackSprite;
                    return CurrentSprite;
                }
            }

            int n = _frames.Length;
            if (n <= 1)
            {
                CurrentSprite = _frames[0];
                return CurrentSprite;
            }

            float rate = _fps * (speedMul > 0f ? speedMul : 0f);
            int idx = 0;

            switch (_mode)
            {
                case FlipbookPlayMode.LoopAll:
                {
                    if (!moving && !attacking)
                    {
                        idx = 0;
                    }
                    else
                    {
                        _phase += safeDt * rate;
                        if (_phase > 100000f) _phase -= 100000f;
                        idx = Mathf.FloorToInt(_phase) % n;
                        if (idx < 0) idx += n;
                    }
                    break;
                }

                case FlipbookPlayMode.PingPongAll:
                {
                    if (!moving && !attacking)
                    {
                        idx = 0;
                    }
                    else
                    {
                        _phase += safeDt * rate;
                        if (_phase > 100000f) _phase -= 100000f;
                        if (n == 2)
                        {
                            idx = Mathf.FloorToInt(_phase) & 1;
                        }
                        else
                        {
                            int period = 2 * n - 2; // For n=3: period=4 -> 0, 1, 2, 1, 0...
                            int step = Mathf.FloorToInt(_phase) % period;
                            if (step < 0) step += period;
                            idx = step < n ? step : (period - step);
                        }
                    }
                    break;
                }

                case FlipbookPlayMode.SplitMoveAttack:
                {
                    int half = Mathf.Max(1, n / 2);
                    if (attacking)
                    {
                        if (!_wasAttacking)
                            _attackPhase = 0f;
                        _attackPhase += safeDt * (rate * 1.25f);
                        if (_attackPhase > 100000f) _attackPhase -= 100000f;
                        int atkCount = Mathf.Max(1, n - half);
                        idx = Mathf.Min(n - 1, half + (Mathf.FloorToInt(_attackPhase) % atkCount));
                    }
                    else if (moving)
                    {
                        _phase += safeDt * rate;
                        if (_phase > 100000f) _phase -= 100000f;
                        idx = Mathf.FloorToInt(_phase) % half;
                        if (idx < 0) idx += half;
                    }
                    else
                    {
                        idx = 0;
                    }
                    break;
                }
            }

            _wasAttacking = attacking;
            CurrentSprite = _frames[Mathf.Clamp(idx, 0, n - 1)] ?? _fallbackSprite;
            return CurrentSprite;
        }

        public void ApplyTo(SpriteRenderer sr, float dt, bool moving = true, bool attacking = false, float speedMul = 1f)
        {
            Sprite sp = Sample(dt, moving, attacking, speedMul);
            if (sr != null && sp != null && sr.sprite != sp)
                sr.sprite = sp;
        }
    }
}
