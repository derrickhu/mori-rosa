using System.Collections.Generic;

namespace Mori
{
    public enum GroundKind
    {
        Dirt = 0,
        Straw = 1,
    }

    /// <summary>
    /// 首页这块地的格子数据。地面是可重复地块，物体单独摆，远处高架只占地图上沿。
    /// 往外加格子或把废墟格清成干草地，地图就能继续扩。
    /// </summary>
    public static class CampMapLayout
    {
        public const int Cols = 20;
        public const int Rows = 16;
        public const float Cell = 1.32f;

        public static GroundKind GroundAt(int x, int y)
        {
            // 中央已清空的营地
            if (x >= 8 && x <= 12 && y >= 7 && y <= 10) return GroundKind.Straw;
            // 右下两块待建空地
            if (x >= 14 && x <= 16 && y >= 4 && y <= 5) return GroundKind.Straw;
            return GroundKind.Dirt;
        }

        /// <summary>河从左上往右下走，格子上只标河心，渲染时铺横向河段。</summary>
        public static bool IsRiver(int x, int y)
        {
            int spine = 6 - x / 5;
            return y == spine;
        }

        public static List<MapProp> BuildProps()
        {
            float c = Cell;
            var list = new List<MapProp>
            {
                P("bus", "p_bus", 9.2f, 8.7f, 1.85f, "command_bus"),
                P("shack", "p_shack", 11.3f, 8.9f, 1.7f, "smokehouse"),
                P("tanks", "p_tanks", 12.6f, 8.6f, 1.45f, "water_still"),
                P("people", "p_survivors", 9.6f, 7.7f, 1.15f, null),
                P("pad", "p_foundation", 10.6f, 7.15f, 1.15f, null),
                P("pad_b", "p_foundation", 15.2f, 4.55f, 1.05f, null),
                P("pad_c", "p_foundation", 16.5f, 4.15f, 0.95f, null),

                P("tower", "p_tower", 1.6f, 10.4f, 2.5f, "hunt_tower"),
                P("logs", "p_logs", 2.7f, 9.6f, 0.85f, null),
                P("berries", "p_berries", 4.4f, 9.3f, 1.05f, null),
                P("garden", "p_garden", 2.2f, 6.4f, 1.05f, null),

                P("pine_a", "p_pine", 1.1f, 12.2f, 2.35f, null),
                P("pine_b", "p_pine", 2.5f, 12.6f, 2.6f, null),
                P("pine_c", "p_pine", 3.8f, 12.0f, 2.2f, null),
                P("leaf_a", "p_broadleaf", 1.8f, 11.2f, 2.15f, null),
                P("leaf_b", "p_broadleaf", 4.6f, 11.5f, 2.05f, null),
                P("leaf_c", "p_broadleaf", 3.2f, 10.6f, 1.9f, null),

                P("dead_a", "p_dead", 6.2f, 12.4f, 2.1f, null),
                P("dead_b", "p_dead", 8.4f, 13.1f, 1.9f, null),
                P("dead_c", "p_dead", 11.5f, 13.3f, 1.85f, null),
                P("dead_d", "p_dead", 14.2f, 13.0f, 1.7f, null),

                P("rubble_a", "p_rubble", 2.4f, 4.2f, 1.25f, null, true),
                P("rubble_b", "p_rubble", 4.8f, 2.6f, 1.15f, null, true),
                P("rubble_c", "p_rubble", 15.6f, 10.6f, 1.45f, null, true),
                P("rubble_d", "p_rubble", 17.2f, 8.8f, 1.15f, null, true),
                P("car_a", "p_car", 16.4f, 6.6f, 1.35f, null, true),
                P("car_b", "p_car", 14.8f, 11.6f, 1.45f, null),
                P("buckets", "p_buckets", 11.2f, 5.35f, 0.7f, null),
                P("weir", "p_weir", 8.6f, 5.55f, 1.05f, null),
            };

            for (int i = 0; i < list.Count; i++)
            {
                list[i].x *= c;
                list[i].y *= c;
            }
            return list;
        }

        static MapProp P(string id, string key, float x, float y, float height, string buildingId, bool clearable = false)
        {
            return new MapProp
            {
                id = id,
                spriteKey = key,
                x = x,
                y = y,
                height = height,
                buildingId = buildingId,
                clearable = clearable,
            };
        }
    }

    public sealed class MapProp
    {
        public string id;
        public string spriteKey;
        public float x;
        public float y;
        public float height;
        public string buildingId;
        public bool clearable;
    }
}
