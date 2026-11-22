using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mori.ForestSlice
{
    public enum HeroAnimMode
    {
        Idle = 0,     // 呼吸待机 + 飘带微动 + 眨眼
        Walk = 1,     // 营地自然漫步（±16° 紧凑自然步幅，重心稳重贴地）
        Run = 2,      // 60FPS 敏捷小跑（±21° 紧凑快步 + 轻微前倾）
        Chop = 3,     // 蓄力高举工具 -> 猛力下劈/敲击刀光 + 咬牙表情 + 顿帧震动
        CarryRun = 4, // 双手高举战利品过头顶小跑回营
        Cheer = 5,    // 落成欢呼跃起
    }

    /// <summary>营地升级解锁的幸存者进阶装束档位（为后续服装/护甲换装预留标准分级）。</summary>
    public enum OutfitTier
    {
        Refugee = 0,    // 营地 Lv.1：落魄都市难民初装
        Reinforced = 1, // 木屋 Lv.2：加固防护工装（皮夹克/护膝/工具腰带）
        Armored = 2,    // 木堡 Lv.3：末日重装护甲（焊接铁甲/防毒面具/强化武器）
    }

    public enum ToolAnimStyle
    {
        HatchetCleave = 0, // 斧类/重劈（阿澈、大锤）
        WrenchSmash = 1,   // 扳手/敲击修补（小棠）
    }

    public sealed class OutfitTierDef
    {
        public OutfitTier tier;
        public int requireShelterLv;
        public string tierName;
        public string atlasKey;
        public string bustKey;
        public string armorChestOverlayKey;
        public string headgearOverlayKey;
        public string toolOverrideKey;
    }

    /// <summary>
    /// 通用多角色骨骼档案（支持阿澈 / 小棠及后续林砚、老陈头、大锤）：
    /// 统一采用 1024x1024 (3x3x320) 图集契约与独立槽位覆盖，天然兼容全套或局部护甲换装。
    /// </summary>
    public sealed class PuppetRigProfile
    {
        public string characterId;
        public string displayName;
        public float baseScale = 0.92f;
        public bool showGroundRing;
        public Color ringTint = new Color(0.28f, 0.92f, 1f, 1f);
        public Color slashTint = new Color(0.88f, 0.98f, 1f, 1f);
        public ToolAnimStyle toolStyle = ToolAnimStyle.HatchetCleave;

        public Vector3 pelvisBase = new Vector3(0f, 0.33f, 0f);
        public Vector3 legNearBase = new Vector3(-0.045f, 0.01f, 0f);
        public Vector3 legFarBase = new Vector3(0.045f, 0.01f, 0f);
        public Vector3 torsoBase = new Vector3(0f, -0.02f, 0f);
        public Vector3 headBase = new Vector3(0.015f, 0.24f, 0f);
        public Vector3 accBase = new Vector3(-0.06f, 0.14f, 0f);
        public Vector3 armNearBase = new Vector3(-0.065f, 0.21f, 0f);
        public Vector3 armFarBase = new Vector3(0.075f, 0.20f, 0f);
        public Vector3 toolGrip = new Vector3(0.01f, -0.22f, 0f);
        public Vector3 carryLocalPos = new Vector3(0.02f, 0.62f, 0f);

        public OutfitTierDef[] tiers;

        public OutfitTierDef GetTier(OutfitTier tier)
        {
            if (tiers == null || tiers.Length == 0) return null;
            for (int i = 0; i < tiers.Length; i++)
                if (tiers[i].tier == tier) return tiers[i];
            return tiers[0];
        }
    }

    public static class PuppetRegistry
    {
        static readonly Dictionary<string, PuppetRigProfile> Profiles = new Dictionary<string, PuppetRigProfile>
        {
            ["ache"] = new PuppetRigProfile
            {
                characterId = "ache",
                displayName = "阿澈",
                baseScale = 0.92f,
                showGroundRing = true,
                ringTint = new Color(0.28f, 0.92f, 1f, 1f),
                slashTint = new Color(0.88f, 0.98f, 1f, 1f),
                toolStyle = ToolAnimStyle.HatchetCleave,
                pelvisBase = new Vector3(0f, 0.33f, 0f),
                legNearBase = new Vector3(-0.026f, 0.01f, 0f),
                legFarBase = new Vector3(0.028f, 0.01f, 0f),
                torsoBase = new Vector3(0f, -0.02f, 0f),
                headBase = new Vector3(0.015f, 0.24f, 0f),
                accBase = new Vector3(-0.06f, 0.14f, 0f),
                armNearBase = new Vector3(-0.065f, 0.21f, 0f),
                armFarBase = new Vector3(0.075f, 0.20f, 0f),
                toolGrip = new Vector3(0.01f, -0.22f, 0f),
                carryLocalPos = new Vector3(0.02f, 0.62f, 0f),
                tiers = new[]
                {
                    new OutfitTierDef { tier = OutfitTier.Refugee, requireShelterLv = 1, tierName = "逃难初装", atlasKey = "hero_puppet_atlas", bustKey = "bust_leader" },
                    new OutfitTierDef { tier = OutfitTier.Reinforced, requireShelterLv = 2, tierName = "加固皮衣", atlasKey = "hero_puppet_atlas_t1", bustKey = "bust_leader_t1", armorChestOverlayKey = "gear_ache_vest_t1", headgearOverlayKey = "gear_ache_hood_t1" },
                    new OutfitTierDef { tier = OutfitTier.Armored, requireShelterLv = 3, tierName = "重装铁甲", atlasKey = "hero_puppet_atlas_t2", bustKey = "bust_leader_t2", armorChestOverlayKey = "gear_ache_armor_t2", headgearOverlayKey = "gear_ache_helm_t2" },
                }
            },
            ["xiaotang"] = new PuppetRigProfile
            {
                characterId = "xiaotang",
                displayName = "小棠",
                baseScale = 0.87f,
                showGroundRing = false,
                ringTint = new Color(0.68f, 0.56f, 1f, 0.85f),
                slashTint = new Color(0.84f, 0.72f, 1f, 1f),
                toolStyle = ToolAnimStyle.WrenchSmash,
                pelvisBase = new Vector3(0f, 0.30f, 0f),
                legNearBase = new Vector3(-0.024f, 0.01f, 0f),
                legFarBase = new Vector3(0.026f, 0.01f, 0f),
                torsoBase = new Vector3(0f, -0.02f, 0f),
                headBase = new Vector3(0.010f, 0.23f, 0f),
                accBase = new Vector3(-0.062f, 0.145f, 0f),
                armNearBase = new Vector3(-0.050f, 0.190f, 0f),
                armFarBase = new Vector3(0.052f, 0.184f, 0f),
                toolGrip = new Vector3(0.012f, -0.20f, 0f),
                carryLocalPos = new Vector3(0.02f, 0.58f, 0f),
                tiers = new[]
                {
                    new OutfitTierDef { tier = OutfitTier.Refugee, requireShelterLv = 1, tierName = "逃难初装", atlasKey = "xiaotang_puppet_atlas", bustKey = "bust_xiaotang" },
                    new OutfitTierDef { tier = OutfitTier.Reinforced, requireShelterLv = 2, tierName = "电工护具", atlasKey = "xiaotang_puppet_atlas_t1", bustKey = "bust_xiaotang_t1", armorChestOverlayKey = "gear_xiaotang_vest_t1", headgearOverlayKey = "gear_xiaotang_goggle_t1" },
                    new OutfitTierDef { tier = OutfitTier.Armored, requireShelterLv = 3, tierName = "焊甲技师", atlasKey = "xiaotang_puppet_atlas_t2", bustKey = "bust_xiaotang_t2", armorChestOverlayKey = "gear_xiaotang_armor_t2", headgearOverlayKey = "gear_xiaotang_weld_t2" },
                }
            },
            ["linyan"] = new PuppetRigProfile
            {
                characterId = "linyan",
                displayName = "林砚",
                baseScale = 0.94f,
                showGroundRing = false,
                ringTint = new Color(0.24f, 0.88f, 0.78f, 0.85f),
                slashTint = new Color(0.45f, 0.96f, 0.86f, 1f),
                toolStyle = ToolAnimStyle.HatchetCleave,
                pelvisBase = new Vector3(0f, 0.345f, 0f),
                legNearBase = new Vector3(-0.025f, 0.01f, 0f),
                legFarBase = new Vector3(0.026f, 0.01f, 0f),
                torsoBase = new Vector3(0f, -0.02f, 0f),
                headBase = new Vector3(0.012f, 0.255f, 0f),
                accBase = new Vector3(-0.055f, 0.165f, 0f),
                armNearBase = new Vector3(-0.058f, 0.205f, 0f),
                armFarBase = new Vector3(0.062f, 0.200f, 0f),
                toolGrip = new Vector3(0.010f, -0.22f, 0f),
                carryLocalPos = new Vector3(0.02f, 0.64f, 0f),
                tiers = new[]
                {
                    new OutfitTierDef { tier = OutfitTier.Refugee, requireShelterLv = 1, tierName = "逃难初装", atlasKey = "linyan_puppet_atlas", bustKey = "bust_linyan" },
                    new OutfitTierDef { tier = OutfitTier.Reinforced, requireShelterLv = 2, tierName = "轻装猎手", atlasKey = "linyan_puppet_atlas_t1", bustKey = "bust_linyan_t1" },
                    new OutfitTierDef { tier = OutfitTier.Armored, requireShelterLv = 3, tierName = "游骑风衣", atlasKey = "linyan_puppet_atlas_t2", bustKey = "bust_linyan_t2" },
                }
            },
            ["laochentou"] = new PuppetRigProfile
            {
                characterId = "laochentou",
                displayName = "老陈头",
                baseScale = 0.90f,
                showGroundRing = false,
                ringTint = new Color(0.36f, 0.76f, 1f, 0.85f),
                slashTint = new Color(0.68f, 0.88f, 1f, 1f),
                toolStyle = ToolAnimStyle.WrenchSmash,
                pelvisBase = new Vector3(0f, 0.295f, 0f),
                legNearBase = new Vector3(-0.032f, 0.01f, 0f),
                legFarBase = new Vector3(0.034f, 0.01f, 0f),
                torsoBase = new Vector3(0f, -0.02f, 0f),
                headBase = new Vector3(0.010f, 0.265f, 0f),
                accBase = new Vector3(-0.068f, 0.165f, 0f),
                armNearBase = new Vector3(-0.078f, 0.230f, 0f),
                armFarBase = new Vector3(0.088f, 0.220f, 0f),
                toolGrip = new Vector3(0.012f, -0.21f, 0f),
                carryLocalPos = new Vector3(0.02f, 0.60f, 0f),
                tiers = new[]
                {
                    new OutfitTierDef { tier = OutfitTier.Refugee, requireShelterLv = 1, tierName = "逃难初装", atlasKey = "laochentou_puppet_atlas", bustKey = "bust_laochentou" },
                    new OutfitTierDef { tier = OutfitTier.Reinforced, requireShelterLv = 2, tierName = "厚围裙装", atlasKey = "laochentou_puppet_atlas_t1", bustKey = "bust_laochentou_t1" },
                    new OutfitTierDef { tier = OutfitTier.Armored, requireShelterLv = 3, tierName = "铁锅大厨", atlasKey = "laochentou_puppet_atlas_t2", bustKey = "bust_laochentou_t2" },
                }
            },
            ["dachui"] = new PuppetRigProfile
            {
                characterId = "dachui",
                displayName = "大锤",
                baseScale = 0.98f,
                showGroundRing = false,
                ringTint = new Color(0.20f, 0.64f, 1f, 0.85f),
                slashTint = new Color(0.50f, 0.85f, 1f, 1f),
                toolStyle = ToolAnimStyle.HatchetCleave,
                pelvisBase = new Vector3(0f, 0.345f, 0f),
                legNearBase = new Vector3(-0.036f, 0.01f, 0f),
                legFarBase = new Vector3(0.036f, 0.01f, 0f),
                torsoBase = new Vector3(0f, -0.02f, 0f),
                headBase = new Vector3(0.012f, 0.295f, 0f),
                accBase = new Vector3(-0.075f, 0.02f, 0f),
                armNearBase = new Vector3(-0.095f, 0.240f, 0f),
                armFarBase = new Vector3(0.105f, 0.220f, 0f),
                toolGrip = new Vector3(0.014f, -0.24f, 0f),
                carryLocalPos = new Vector3(0.02f, 0.68f, 0f),
                tiers = new[]
                {
                    new OutfitTierDef { tier = OutfitTier.Refugee, requireShelterLv = 1, tierName = "逃难初装", atlasKey = "dachui_puppet_atlas", bustKey = "bust_dachui" },
                    new OutfitTierDef { tier = OutfitTier.Reinforced, requireShelterLv = 2, tierName = "路牌护甲", atlasKey = "dachui_puppet_atlas_t1", bustKey = "bust_dachui_t1" },
                    new OutfitTierDef { tier = OutfitTier.Armored, requireShelterLv = 3, tierName = "防暴重装", atlasKey = "dachui_puppet_atlas_t2", bustKey = "bust_dachui_t2" },
                }
            },
        };

        public static PuppetRigProfile GetProfile(string characterId)
        {
            if (!string.IsNullOrEmpty(characterId) && Profiles.TryGetValue(characterId, out PuppetRigProfile p))
                return p;
            return Profiles["ache"];
        }

        public static OutfitTier MaxUnlockedTierForShelter(int shelterLv)
        {
            if (shelterLv >= 3) return OutfitTier.Armored;
            if (shelterLv >= 2) return OutfitTier.Reinforced;
            return OutfitTier.Refugee;
        }

        public static string ResolveBustKey(string characterId, OutfitTier tier)
        {
            PuppetRigProfile p = GetProfile(characterId);
            OutfitTierDef t = p.GetTier(tier);
            if (t != null && Resources.Load<Texture2D>("ForestArt/" + t.bustKey) != null)
                return t.bustKey;
            OutfitTierDef baseDef = p.GetTier(OutfitTier.Refugee);
            return baseDef != null ? baseDef.bustKey : "bust_leader";
        }
    }

    /// <summary>
    /// 60-FPS 通用多部件 2D 骨骼动画与换装控制器：
    /// 1. 统一读取 1024x1024 (3x3x320) 角色骨骼图集，驱动待机呼吸眨眼、跨步奔跑、挥砍/扳手敲击、举物搬运与跃起欢呼；
    /// 2. 支持营地升级后一键换装（整套图集切换 SetSkinTier + 局部护胸/头盔/武器槽位覆盖 OverrideGearSlots）。
    /// </summary>
    public sealed class ForestHeroPuppet
    {
        const float PPU = 256f;
        const float Cell = 320f;

        sealed class AtlasPack
        {
            public Texture2D tex;
            public Sprite headOpen, headEffort, accTail, torso, armNear, armFar, legNear, legFar, tool;
        }

        static readonly Dictionary<string, AtlasPack> AtlasCache = new Dictionary<string, AtlasPack>();
        static Sprite _spSlashArc, _spGroundRing;

        public readonly PuppetRigProfile Profile;
        public readonly Transform Root;
        public OutfitTier CurrentTier { get; private set; } = OutfitTier.Refugee;
        public string ActiveAtlasKey { get; private set; }

        readonly SortingGroup _sortGroup;
        readonly Transform _flipRoot;
        readonly Transform _pelvis;
        readonly Transform _torso;
        readonly Transform _head;
        readonly Transform _accTail;
        readonly Transform _armNear;
        readonly Transform _armFar;
        readonly Transform _legNear;
        readonly Transform _legFar;
        readonly Transform _tool;
        readonly Transform _slashTr;
        readonly Transform _carryPivot;

        readonly SpriteRenderer _headSr;
        readonly SpriteRenderer _accSr;
        readonly SpriteRenderer _torsoSr;
        readonly SpriteRenderer _armNearSr;
        readonly SpriteRenderer _armFarSr;
        readonly SpriteRenderer _legNearSr;
        readonly SpriteRenderer _legFarSr;
        readonly SpriteRenderer _toolSr;
        readonly SpriteRenderer _armorChestSr; // 预留：Lv.2/Lv.3 护胸/皮夹克外挂槽位
        readonly SpriteRenderer _headgearSr;   // 预留：Lv.2/Lv.3 防护头盔/焊镜外挂槽位
        readonly SpriteRenderer _slashSr;
        readonly SpriteRenderer _carrySr;
        readonly SpriteRenderer _ringSr;

        AtlasPack _atlas;
        Sprite _headOpenSp;
        Sprite _headEffortSp;

        public HeroAnimMode Mode { get; private set; } = HeroAnimMode.Idle;
        public bool FacingRight { get; private set; } = true;

        float _phase;
        float _chopCycle;
        bool _chopHitFiredThisSwing;
        float _blinkTimer = 2.6f;
        float _blinkHold;
        float _accAngle;
        float _accVel;
        float _slashAlpha;

        public event Action OnChopImpact;
        public event Action OnFootstep;
        int _lastStepHalf = -1;

        public ForestHeroPuppet(Transform parent, string name = "HeroPuppet_Ache")
            : this(parent, PuppetRegistry.GetProfile("ache"), name) { }

        public ForestHeroPuppet(Transform parent, PuppetRigProfile profile, string name = null)
        {
            Profile = profile ?? PuppetRegistry.GetProfile("ache");
            EnsureSharedFx();

            var rootGo = new GameObject(string.IsNullOrEmpty(name) ? ("Puppet_" + Profile.characterId) : name);
            rootGo.transform.SetParent(parent, false);
            Root = rootGo.transform;
            Root.localScale = new Vector3(Profile.baseScale, Profile.baseScale, 1f);

            _sortGroup = rootGo.AddComponent<SortingGroup>();

            // 脚下光圈（主角常显便于一眼识别直控角色，伙伴可选隐藏或低透明度）
            var ringGo = new GameObject("HeroGroundRing");
            ringGo.transform.SetParent(Root, false);
            ringGo.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            ringGo.transform.localScale = new Vector3(0.58f, 0.30f, 1f);
            _ringSr = ringGo.AddComponent<SpriteRenderer>();
            _ringSr.sprite = _spGroundRing;
            _ringSr.color = Profile.ringTint;
            _ringSr.sortingOrder = 0;
            ringGo.SetActive(Profile.showGroundRing);

            _flipRoot = MakeChild(Root, "FlipRoot", Vector3.zero);
            _pelvis = MakeChild(_flipRoot, "Pelvis", Profile.pelvisBase);

            _legFar = MakePart(_pelvis, "LegFar", Profile.legFarBase, null, 2, out _legFarSr);
            _legNear = MakePart(_pelvis, "LegNear", Profile.legNearBase, null, 4, out _legNearSr);

            _torso = MakePart(_pelvis, "Torso", Profile.torsoBase, null, 5, out _torsoSr);
            // 预留：护甲外穿槽位（直接挂载在 Torso 骨骼上跟随呼吸与前倾！）
            MakePart(_torso, "ArmorChestOverlay", Vector3.zero, null, 6, out _armorChestSr);
            _armorChestSr.enabled = false;

            _armFar = MakePart(_torso, "ArmFar", Profile.armFarBase, null, 3, out _armFarSr);

            _head = MakePart(_torso, "Head", Profile.headBase, null, 8, out _headSr);
            _accTail = MakePart(_head, "AccessoryTail", Profile.accBase, null, 7, out _accSr);
            // 预留：头盔/面具外穿槽位（直接挂载在 Head 骨骼上跟随摇头与挥砍！）
            MakePart(_head, "HeadgearOverlay", Vector3.zero, null, 9, out _headgearSr);
            _headgearSr.enabled = false;

            _carryPivot = MakeChild(_torso, "CarryItem", Profile.carryLocalPos);
            _carrySr = _carryPivot.gameObject.AddComponent<SpriteRenderer>();
            _carrySr.sortingOrder = 10;
            _carryPivot.localScale = new Vector3(0.55f, 0.55f, 1f);
            _carryPivot.gameObject.SetActive(false);

            _armNear = MakePart(_torso, "ArmNear", Profile.armNearBase, null, 11, out _armNearSr);
            _tool = MakePart(_armNear, "Tool", Profile.toolGrip, null, 9, out _toolSr);

            _slashTr = MakeChild(_flipRoot, "SlashArc", new Vector3(0.28f, 0.44f, 0f));
            _slashTr.localScale = new Vector3(0.95f, 0.95f, 1f);
            _slashSr = _slashTr.gameObject.AddComponent<SpriteRenderer>();
            _slashSr.sprite = _spSlashArc;
            _slashSr.sortingOrder = 13;
            _slashSr.color = new Color(Profile.slashTint.r, Profile.slashTint.g, Profile.slashTint.b, 0f);

            SetSkinTier(OutfitTier.Refugee, autoFallbackToBase: true);
        }

        static Transform MakeChild(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        static Transform MakePart(Transform parent, string name, Vector3 localPos, Sprite sp, int order, out SpriteRenderer sr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            return go.transform;
        }

        // ---------------- 营地升级皮肤/护甲切换接口 ----------------
        public bool SetSkinTier(OutfitTier tier, bool autoFallbackToBase = true)
        {
            CurrentTier = tier;
            OutfitTierDef t = Profile.GetTier(tier);
            OutfitTierDef baseDef = Profile.GetTier(OutfitTier.Refugee);

            string targetKey = t != null ? t.atlasKey : (baseDef != null ? baseDef.atlasKey : "hero_puppet_atlas");
            AtlasPack pack = LoadOrGetAtlas(targetKey);
            bool usedNativeTierAtlas = pack != null;

            if (pack == null && autoFallbackToBase && baseDef != null)
                pack = LoadOrGetAtlas(baseDef.atlasKey);

            if (pack != null)
            {
                ActiveAtlasKey = usedNativeTierAtlas ? targetKey : (baseDef != null ? baseDef.atlasKey : targetKey);
                ApplyAtlasPack(pack);
            }

            // 若未来仅制作了局部护甲/头盔散件（gear_*），自动叠穿到预留槽位上！
            Sprite chestOv = (t != null && !string.IsNullOrEmpty(t.armorChestOverlayKey))
                ? Resources.Load<Sprite>("ForestArt/" + t.armorChestOverlayKey)
                : null;
            Sprite headOv = (t != null && !string.IsNullOrEmpty(t.headgearOverlayKey))
                ? Resources.Load<Sprite>("ForestArt/" + t.headgearOverlayKey)
                : null;
            Sprite toolOv = (t != null && !string.IsNullOrEmpty(t.toolOverrideKey))
                ? Resources.Load<Sprite>("ForestArt/" + t.toolOverrideKey)
                : null;

            OverrideGearSlots(armorChest: chestOv, headgear: headOv, tool: toolOv);
            return usedNativeTierAtlas || chestOv != null || headOv != null;
        }

        public void OverrideGearSlots(Sprite torso = null, Sprite armNear = null, Sprite armFar = null,
            Sprite legNear = null, Sprite legFar = null, Sprite armorChest = null, Sprite headgear = null, Sprite tool = null)
        {
            if (torso != null) _torsoSr.sprite = torso;
            if (armNear != null) _armNearSr.sprite = armNear;
            if (armFar != null) _armFarSr.sprite = armFar;
            if (legNear != null) _legNearSr.sprite = legNear;
            if (legFar != null) _legFarSr.sprite = legFar;
            if (tool != null) _toolSr.sprite = tool;

            _armorChestSr.sprite = armorChest;
            _armorChestSr.enabled = armorChest != null;

            _headgearSr.sprite = headgear;
            _headgearSr.enabled = headgear != null;
        }

        void ApplyAtlasPack(AtlasPack pack)
        {
            _atlas = pack;
            _headOpenSp = pack.headOpen;
            _headEffortSp = pack.headEffort;
            _headSr.sprite = _headOpenSp;
            _accSr.sprite = pack.accTail;
            _accSr.enabled = pack.accTail != null;
            _torsoSr.sprite = pack.torso;
            _armNearSr.sprite = pack.armNear;
            _armFarSr.sprite = pack.armFar;
            _legNearSr.sprite = pack.legNear;
            _legFarSr.sprite = pack.legFar;
            _toolSr.sprite = pack.tool;
        }

        void EnsureAtlasAlive()
        {
            if (_atlas == null || _atlas.tex == null || _headOpenSp == null || _torsoSr.sprite == null)
                SetSkinTier(CurrentTier, autoFallbackToBase: true);
            if (_spSlashArc == null || _spGroundRing == null)
                EnsureSharedFx();
            if (_slashSr != null && _slashSr.sprite == null) _slashSr.sprite = _spSlashArc;
            if (_ringSr != null && _ringSr.sprite == null) _ringSr.sprite = _spGroundRing;
        }

        // ---------------- 朝向、搬运与位置同步 ----------------
        public void SetFacing(bool faceRight)
        {
            FacingRight = faceRight;
            _flipRoot.localScale = new Vector3(faceRight ? 1f : -1f, 1f, 1f);
            if (_carryPivot != null)
                _carryPivot.localScale = new Vector3(faceRight ? 0.55f : -0.55f, 0.55f, 1f);
        }

        public void SetCarrySprite(Sprite itemSprite)
        {
            if (itemSprite == null)
            {
                _carryPivot.gameObject.SetActive(false);
                return;
            }
            _carrySr.sprite = itemSprite;
            _carryPivot.gameObject.SetActive(true);
        }

        public void SetMode(HeroAnimMode next)
        {
            if (Mode == next) return;
            Mode = next;
            if (next == HeroAnimMode.Chop)
            {
                _chopCycle = 0f;
                _chopHitFiredThisSwing = false;
            }
        }

        public void SyncWorldPos(Vector3 isoWorldPos, Transform shadowTr, int sortingBias = 2)
        {
            Root.localPosition = isoWorldPos;
            _sortGroup.sortingOrder = Mathf.RoundToInt(-isoWorldPos.y * 100f) + sortingBias;
            if (shadowTr != null) shadowTr.localPosition = isoWorldPos;
        }

        // ---------------- 60FPS 骨骼姿态解算 ----------------
        public void Tick(float dt)
        {
            EnsureAtlasAlive();
            _phase += dt;

            if (_ringSr != null && _ringSr.gameObject.activeSelf)
            {
                float rp = 0.56f + Mathf.Sin(_phase * 3.5f) * 0.035f;
                _ringSr.transform.localScale = new Vector3(rp, rp * 0.52f, 1f);
            }

            if (_slashAlpha > 0f)
            {
                _slashAlpha = Mathf.Max(0f, _slashAlpha - dt * 5.8f);
                _slashSr.color = new Color(Profile.slashTint.r, Profile.slashTint.g, Profile.slashTint.b, _slashAlpha);
            }

            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blinkHold = 0.13f;
                _blinkTimer = 2.6f + Mathf.Abs(Mathf.Sin(_phase * 7.3f)) * 2.2f;
            }
            if (_blinkHold > 0f) _blinkHold = Mathf.Max(0f, _blinkHold - dt);

            float targetAcc = 0f;

            switch (Mode)
            {
                case HeroAnimMode.Idle:
                {
                    float b = Mathf.Sin(_phase * 3.0f);
                    _pelvis.localPosition = Profile.pelvisBase + new Vector3(0f, b * 0.010f, 0f);
                    _pelvis.localScale = new Vector3(1f - b * 0.008f, 1f + b * 0.012f, 1f);

                    _torso.localRotation = Quaternion.Euler(0f, 0f, b * 1.6f);
                    _head.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_phase * 1.7f) * 3.0f);
                    _headSr.sprite = _blinkHold > 0f ? _headEffortSp : _headOpenSp;

                    _legNear.localPosition = Profile.legNearBase;
                    _legFar.localPosition = Profile.legFarBase;
                    _legNear.localRotation = Quaternion.identity;
                    _legFar.localRotation = Quaternion.identity;

                    _armNear.localRotation = Quaternion.Euler(0f, 0f, 10f + b * 4.0f);
                    _armFar.localRotation = Quaternion.Euler(0f, 0f, -4f - b * 3.0f);
                    _toolSr.enabled = true;
                    _tool.localRotation = Quaternion.Euler(0f, 0f, -80f + b * 2.5f);

                    targetAcc = Mathf.Sin(_phase * 4.2f) * 7f;
                    break;
                }

                case HeroAnimMode.Walk:
                case HeroAnimMode.Run:
                case HeroAnimMode.CarryRun:
                {
                    bool isWalk = (Mode == HeroAnimMode.Walk);
                    bool carrying = (Mode == HeroAnimMode.CarryRun);

                    // 自然紧凑步幅：营地漫步仅 ±16°，小跑/搬运 ±21°，告别夸张劈叉与浮空！
                    float freq = isWalk ? 7.2f : 9.8f;
                    float legAmp = isWalk ? 16f : (carrying ? 19f : 21f);
                    float armAmp = isWalk ? 14f : 22f;
                    float bobAmp = isWalk ? 0.012f : 0.020f;
                    float leanDeg = isWalk ? -3.2f : (carrying ? -4.5f : -7.0f);

                    float w = _phase * freq;
                    float s = Mathf.Sin(w);
                    float c = Mathf.Cos(w);

                    int half = Mathf.FloorToInt(w / Mathf.PI);
                    if (half != _lastStepHalf)
                    {
                        _lastStepHalf = half;
                        if (!isWalk) OnFootstep?.Invoke();
                    }

                    // 真实倒立摆重心曲线：单腿支撑经过中点(|s|=0)时重心微升，迈步落地(|s|=1)时微沉贴地
                    float bob = (1f - Mathf.Abs(s)) * bobAmp - Mathf.Abs(s) * 0.008f;
                    float squash = isWalk ? 1f : (1f - Mathf.Abs(s) * 0.022f);
                    _pelvis.localPosition = Profile.pelvisBase + new Vector3(0f, bob, 0f);
                    _pelvis.localScale = new Vector3(2f - squash, squash, 1f);

                    _torso.localRotation = Quaternion.Euler(0f, 0f, leanDeg + c * 1.4f);
                    _head.localRotation = Quaternion.Euler(0f, 0f, -leanDeg * 0.65f - c * 1.6f);
                    _headSr.sprite = _blinkHold > 0f ? _headEffortSp : _headOpenSp;

                    // 向前摆动的脚略微抬起避免拖地，支撑蹬地的脚保持贴地
                    float nearLift = Mathf.Max(0f, c) * (isWalk ? 0.014f : 0.022f);
                    float farLift = Mathf.Max(0f, -c) * (isWalk ? 0.014f : 0.022f);
                    _legNear.localPosition = Profile.legNearBase + new Vector3(0f, nearLift, 0f);
                    _legFar.localPosition = Profile.legFarBase + new Vector3(0f, farLift, 0f);
                    _legNear.localRotation = Quaternion.Euler(0f, 0f, s * legAmp);
                    _legFar.localRotation = Quaternion.Euler(0f, 0f, -s * legAmp);

                    if (carrying)
                    {
                        _armNear.localRotation = Quaternion.Euler(0f, 0f, 166f + bob * 40f);
                        _armFar.localRotation = Quaternion.Euler(0f, 0f, 156f + bob * 40f);
                        _toolSr.enabled = false;
                        _carryPivot.localPosition = Profile.carryLocalPos + new Vector3(0f, bob * 0.4f, 0f);
                    }
                    else
                    {
                        _armNear.localRotation = Quaternion.Euler(0f, 0f, -s * armAmp + 6f);
                        _armFar.localRotation = Quaternion.Euler(0f, 0f, s * armAmp);
                        _toolSr.enabled = true;
                        _tool.localRotation = Quaternion.Euler(0f, 0f, -78f + s * (isWalk ? 6f : 10f));
                    }

                    targetAcc = (isWalk ? 10f : 18f) + Mathf.Sin(w * 1.5f) * (isWalk ? 6f : 10f);
                    break;
                }

                case HeroAnimMode.Chop:
                {
                    _toolSr.enabled = true;
                    _chopCycle += dt;
                    float period = Profile.toolStyle == ToolAnimStyle.WrenchSmash ? 0.46f : 0.56f;
                    if (_chopCycle >= period)
                    {
                        _chopCycle -= period;
                        _chopHitFiredThisSwing = false;
                    }

                    float u = _chopCycle / period;
                    float torsoAng, armNearAng, armFarAng, toolAng, legNearAng, pelvisY;
                    bool fierceFace = false;

                    if (u < 0.46f)
                    {
                        float t = Smooth01(u / 0.46f);
                        torsoAng = Mathf.Lerp(-4f, 22f, t);
                        armNearAng = Mathf.Lerp(24f, 148f, t);
                        armFarAng = Mathf.Lerp(14f, 136f, t);
                        toolAng = Mathf.Lerp(-82f, -135f, t);
                        legNearAng = Mathf.Lerp(0f, 18f, t);
                        pelvisY = t * 0.035f;
                    }
                    else if (u < 0.62f)
                    {
                        float t = (u - 0.46f) / 0.16f;
                        t = t * t;
                        torsoAng = Mathf.Lerp(22f, -24f, t);
                        armNearAng = Mathf.Lerp(148f, 42f, t);
                        armFarAng = Mathf.Lerp(136f, 34f, t);
                        toolAng = Mathf.Lerp(-135f, -102f, t);
                        legNearAng = Mathf.Lerp(18f, -16f, t);
                        pelvisY = Mathf.Lerp(0.035f, -0.035f, t);
                        fierceFace = true;

                        if (u >= 0.56f && !_chopHitFiredThisSwing)
                        {
                            _chopHitFiredThisSwing = true;
                            _slashAlpha = 0.95f;
                            OnChopImpact?.Invoke();
                        }
                    }
                    else if (u < 0.76f)
                    {
                        float buzz = Mathf.Sin(_phase * 70f) * 2.2f;
                        torsoAng = -24f + buzz * 0.4f;
                        armNearAng = 42f + buzz;
                        armFarAng = 34f;
                        toolAng = -102f + buzz;
                        legNearAng = -16f;
                        pelvisY = -0.035f;
                        fierceFace = true;
                    }
                    else
                    {
                        float t = Smooth01((u - 0.76f) / 0.24f);
                        torsoAng = Mathf.Lerp(-24f, -4f, t);
                        armNearAng = Mathf.Lerp(42f, 24f, t);
                        armFarAng = Mathf.Lerp(34f, 14f, t);
                        toolAng = Mathf.Lerp(-102f, -82f, t);
                        legNearAng = Mathf.Lerp(-16f, 0f, t);
                        pelvisY = Mathf.Lerp(-0.035f, 0f, t);
                    }

                    _pelvis.localPosition = Profile.pelvisBase + new Vector3(0f, pelvisY, 0f);
                    _pelvis.localScale = Vector3.one;
                    _torso.localRotation = Quaternion.Euler(0f, 0f, torsoAng);
                    _head.localRotation = Quaternion.Euler(0f, 0f, -torsoAng * 0.35f);
                    _headSr.sprite = fierceFace ? _headEffortSp : _headOpenSp;
                    _armNear.localRotation = Quaternion.Euler(0f, 0f, armNearAng);
                    _armFar.localRotation = Quaternion.Euler(0f, 0f, armFarAng);
                    _tool.localRotation = Quaternion.Euler(0f, 0f, toolAng);
                    _legNear.localPosition = Profile.legNearBase;
                    _legFar.localPosition = Profile.legFarBase;
                    _legNear.localRotation = Quaternion.Euler(0f, 0f, legNearAng);
                    _legFar.localRotation = Quaternion.Euler(0f, 0f, -legNearAng * 0.6f);

                    targetAcc = -torsoAng * 1.1f;
                    break;
                }

                case HeroAnimMode.Cheer:
                {
                    float j = Mathf.Abs(Mathf.Sin(_phase * 9f));
                    _pelvis.localPosition = Profile.pelvisBase + new Vector3(0f, j * 0.14f, 0f);
                    _torso.localRotation = Quaternion.Euler(0f, 0f, 6f);
                    _headSr.sprite = _headEffortSp;
                    _armNear.localRotation = Quaternion.Euler(0f, 0f, 150f);
                    _armFar.localRotation = Quaternion.Euler(0f, 0f, -135f);
                    _legNear.localRotation = Quaternion.Euler(0f, 0f, -22f * j);
                    _legFar.localRotation = Quaternion.Euler(0f, 0f, 22f * j);
                    targetAcc = -20f;
                    break;
                }
            }

            float force = (targetAcc - _accAngle) * 85f - _accVel * 9f;
            _accVel += force * dt;
            _accAngle += _accVel * dt;
            _accTail.localRotation = Quaternion.Euler(0f, 0f, _accAngle);
        }

        static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        // ---------------- 图集切分与缓存 ----------------
        public static void EnsureSprites()
        {
            EnsureSharedFx();
            LoadOrGetAtlas("hero_puppet_atlas");
        }

        static AtlasPack LoadOrGetAtlas(string atlasKey)
        {
            if (string.IsNullOrEmpty(atlasKey)) return null;
            var tex = Resources.Load<Texture2D>("ForestArt/" + atlasKey);
            if (tex == null) return null;

            if (AtlasCache.TryGetValue(atlasKey, out AtlasPack cached) && cached != null && cached.tex == tex && cached.headOpen != null)
                return cached;

            var p = new AtlasPack
            {
                tex = tex,
                headOpen   = Slice(tex, 0, 2, new Vector2(0.48f, 0.10f), atlasKey + "_head_open"),
                headEffort = Slice(tex, 1, 2, new Vector2(0.48f, 0.10f), atlasKey + "_head_effort"),
                accTail    = Slice(tex, 2, 2, new Vector2(0.88f, 0.58f), atlasKey + "_acc_tail"),
                torso      = Slice(tex, 0, 1, new Vector2(0.52f, 0.12f), atlasKey + "_torso"),
                armNear    = Slice(tex, 1, 1, new Vector2(0.50f, 0.84f), atlasKey + "_arm_near"),
                armFar     = Slice(tex, 2, 1, new Vector2(0.50f, 0.84f), atlasKey + "_arm_far"),
                legNear    = Slice(tex, 0, 0, new Vector2(0.50f, 0.86f), atlasKey + "_leg_near"),
                legFar     = Slice(tex, 1, 0, new Vector2(0.50f, 0.86f), atlasKey + "_leg_far"),
                tool       = Slice(tex, 2, 0, new Vector2(0.46f, 0.16f), atlasKey + "_tool"),
            };
            AtlasCache[atlasKey] = p;
            return p;
        }

        static Sprite Slice(Texture2D tex, int col, int row, Vector2 pivot, string name)
        {
            var sp = Sprite.Create(tex, new Rect(col * Cell, row * Cell, Cell, Cell), pivot, PPU, 0, SpriteMeshType.FullRect);
            sp.name = name;
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static void EnsureSharedFx()
        {
            if (_spSlashArc == null) _spSlashArc = BuildSlashArcSprite(128);
            if (_spGroundRing == null) _spGroundRing = BuildHeroRingSprite(96);
        }

        static Sprite BuildSlashArcSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x - half, y - half) / half;
                    float r = p.magnitude;
                    float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (r > 0.45f && r < 0.96f && ang > -58f && ang < 72f)
                    {
                        float uAng = 1f - Mathf.Abs(ang - 7f) / 65f;
                        float outerEdge = 1f - Mathf.Abs(r - 0.80f) / 0.18f;
                        float a = Mathf.Clamp01(uAng * 1.3f) * Mathf.Clamp01(outerEdge);
                        Color c = r > 0.82f ? new Color(0.25f, 0.90f, 1f, a) : new Color(1f, 1f, 1f, a * 0.92f);
                        px[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildHeroRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                    if (r > 1f) continue;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.84f) / 0.10f);
                    float glow = Mathf.Clamp01(1f - r) * 0.10f;
                    float a = Mathf.Clamp01(ring * 0.52f + glow);
                    px[y * size + x] = new Color(0.28f, 0.92f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }
    }
}
