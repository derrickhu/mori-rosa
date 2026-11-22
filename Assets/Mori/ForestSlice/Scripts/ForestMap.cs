using UnityEngine;

namespace Mori.ForestSlice
{
    /// <summary>
    /// 林地逻辑网格：每格一个“树林度”(1 = 密林, 0 = 已开垦)。
    /// MaskTex 双通道：
    ///   R = 原始林地遮罩（驱动荒地边缘柔和投射阴影）
    ///   G = 向内腐蚀 1 格的深林遮罩（只在第 2 排树及更深处变暗为林下腐殖土，确保第 1 排所有树根 100% 踩在实地上，绝不悬空在黑坑里）
    /// </summary>
    public sealed class ForestMap
    {
        public const int N = 32;          // 可开垦区域 N×N 格（拓宽山谷盆地，容纳开阔主营地广场）
        public const int Margin = 14;     // 外圈永久密林
        public const int Domain = N + Margin * 2;
        public const int PxPerCell = 4;   // 遮罩分辨率
        public const int ChunkSize = 3;   // 一次砍一块 3×3

        public readonly float[,] forest = new float[N, N];
        public readonly float[,] target = new float[N, N];
        readonly float[,] _delay = new float[N, N];
        readonly float[,] _eroded = new float[Domain, Domain];

        public Texture2D MaskTex { get; private set; }
        public Texture2D RiverTex { get; private set; }

        float[] _bufR, _bufG, _tmp;
        Color32[] _px;

        public ForestMap()
        {
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    forest[x, y] = target[x, y] = 1f;

            int R = Domain * PxPerCell;
            MaskTex = new Texture2D(R, R, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "ForestMask",
            };
            RiverTex = new Texture2D(R, R, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "RiverMask",
            };
            _bufR = new float[R * R];
            _bufG = new float[R * R];
            _tmp = new float[R * R];
            _px = new Color32[R * R];
            BuildRiver();
            RebuildMask();
        }

        // ---------------- 河道（固定地形） ----------------
        public static float RiverDistance(float gx, float gy)
        {
            float c = 17f + 1.5f * Mathf.Sin(0.4f * gx);
            return Mathf.Abs(gx + gy - c) * 0.7071f;
        }

        public static bool IsRiverCell(int x, int y) => RiverDistance(x + 0.5f, y + 0.5f) < 1.0f;
        public static bool IsNearRiver(int x, int y) => RiverDistance(x + 0.5f, y + 0.5f) < 1.9f;

        void BuildRiver()
        {
            int R = Domain * PxPerCell;
            var px = new Color32[R * R];
            for (int j = 0; j < R; j++)
            {
                for (int i = 0; i < R; i++)
                {
                    float gx = (i + 0.5f) / PxPerCell - Margin;
                    float gy = (j + 0.5f) / PxPerCell - Margin;
                    float d = RiverDistance(gx, gy);
                    float v = Mathf.Clamp01(1f - d / 1.6f);
                    byte b = (byte)(v * 255f);
                    px[j * R + i] = new Color32(b, b, b, 255);
                }
            }
            RiverTex.SetPixels32(px);
            RiverTex.Apply();
        }

        // ---------------- 开垦 ----------------
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;

        public float At(int x, int y) => InBounds(x, y) ? forest[x, y] : 1f;

        public bool IsCleared(int x, int y) => InBounds(x, y) && target[x, y] < 0.5f;

        float ErodedAt(int x, int y)
        {
            int ix = Mathf.Clamp(x + Margin, 0, Domain - 1);
            int iy = Mathf.Clamp(y + Margin, 0, Domain - 1);
            return _eroded[ix, iy];
        }

        public void ClearRectInstant(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (InBounds(x, y)) forest[x, y] = target[x, y] = 0f;
        }

        public float GetChopDelay(int x, int y) => InBounds(x, y) ? _delay[x, y] : 0f;

        /// <summary>砍掉一块 3×3：要求与已开垦区相邻。返回是否开始动画。</summary>
        public bool TryClearChunk(int cx, int cy, System.Random rng)
        {
            int x0 = cx * ChunkSize, y0 = cy * ChunkSize;
            bool anyForest = false, touches = false;
            for (int x = x0; x < x0 + ChunkSize; x++)
            {
                for (int y = y0; y < y0 + ChunkSize; y++)
                {
                    if (!InBounds(x, y)) continue;
                    if (target[x, y] > 0.5f) anyForest = true;
                    if (IsCleared(x - 1, y) || IsCleared(x + 1, y) || IsCleared(x, y - 1) || IsCleared(x, y + 1))
                        touches = true;
                }
            }
            if (!anyForest || !touches) return false;
            for (int x = x0; x < x0 + ChunkSize; x++)
            {
                for (int y = y0; y < y0 + ChunkSize; y++)
                {
                    if (!InBounds(x, y) || target[x, y] < 0.5f) continue;
                    target[x, y] = 0f;
                    _delay[x, y] = (float)rng.NextDouble() * 0.38f;
                }
            }
            return true;
        }

        public bool Tick(float dt)
        {
            bool changed = false;
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    if (Mathf.Approximately(forest[x, y], target[x, y])) continue;
                    if (_delay[x, y] > 0f) { _delay[x, y] -= dt; changed = true; continue; }
                    forest[x, y] = Mathf.MoveTowards(forest[x, y], target[x, y], dt * 2.4f);
                    changed = true;
                }
            }
            if (changed) RebuildMask();
            return changed;
        }

        public void RebuildMask()
        {
            for (int ix = 0; ix < Domain; ix++)
            {
                int gx = ix - Margin;
                for (int iy = 0; iy < Domain; iy++)
                {
                    int gy = iy - Margin;
                    float m = 1f;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            m = Mathf.Min(m, At(gx + dx, gy + dy));
                    _eroded[ix, iy] = m;
                }
            }

            int R = Domain * PxPerCell;
            for (int j = 0; j < R; j++)
            {
                float gy = (j + 0.5f) / PxPerCell - Margin - 0.5f;
                int y0 = Mathf.FloorToInt(gy);
                float fy = gy - y0;
                for (int i = 0; i < R; i++)
                {
                    float gx = (i + 0.5f) / PxPerCell - Margin - 0.5f;
                    int x0 = Mathf.FloorToInt(gx);
                    float fx = gx - x0;

                    float ra = At(x0, y0), rb = At(x0 + 1, y0), rc = At(x0, y0 + 1), rd = At(x0 + 1, y0 + 1);
                    _bufR[j * R + i] = Mathf.Lerp(Mathf.Lerp(ra, rb, fx), Mathf.Lerp(rc, rd, fx), fy);

                    float ga = ErodedAt(x0, y0), gb = ErodedAt(x0 + 1, y0), gc = ErodedAt(x0, y0 + 1), gd = ErodedAt(x0 + 1, y0 + 1);
                    _bufG[j * R + i] = Mathf.Lerp(Mathf.Lerp(ga, gb, fx), Mathf.Lerp(gc, gd, fx), fy);
                }
            }
            Blur(_bufR, R, 2);
            Blur(_bufR, R, 2);
            Blur(_bufG, R, 2);
            Blur(_bufG, R, 2);
            for (int k = 0; k < _bufR.Length; k++)
            {
                byte vr = (byte)(Mathf.Clamp01(_bufR[k]) * 255f);
                byte vg = (byte)(Mathf.Clamp01(_bufG[k]) * 255f);
                _px[k] = new Color32(vr, vg, 0, 255);
            }
            MaskTex.SetPixels32(_px);
            MaskTex.Apply();
        }

        void Blur(float[] buf, int R, int rad)
        {
            float inv = 1f / (rad * 2 + 1);
            for (int j = 0; j < R; j++)
            {
                for (int i = 0; i < R; i++)
                {
                    float s = 0f;
                    for (int k = -rad; k <= rad; k++) s += buf[j * R + Mathf.Clamp(i + k, 0, R - 1)];
                    _tmp[j * R + i] = s * inv;
                }
            }
            for (int j = 0; j < R; j++)
            {
                for (int i = 0; i < R; i++)
                {
                    float s = 0f;
                    for (int k = -rad; k <= rad; k++) s += _tmp[Mathf.Clamp(j + k, 0, R - 1) * R + i];
                    buf[j * R + i] = s * inv;
                }
            }
        }
    }
}
