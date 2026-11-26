using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mori.ForestSlice
{
    public struct ArenaHeroHudState
    {
        public int heroIdx;
        public int slot0to8;
        public string name;
        public int star;
        public string skillName;
        public bool alive;
        public float hp;
        public float maxHp;
        public float shield;
        public float rage01;
        public bool ultReady;
        public float totalDamage;
        public float totalHeal;
    }

    public struct ArenaResultSummary
    {
        public bool finished;
        public bool victory;
        public int stage;
        public string stageTitle;
        public int mvpHeroIdx;
        public string mvpName;
        public int mvpDamage;
        public string rewardSummary;
        public float durationSec;
    }

    /// <summary>
    /// 《末日囤货狂》独立零树木遮挡策略战斗舞台（对标《疯狂水世界》3×3 九宫格布阵动态冲锋迎击战）：
    /// - 独立世界坐标舞台 (220, 220, 0) + 竖屏正交相机锁定 (220, 220, -10), orthographicSize = 6.4 (1u = 100px)，100% 零树冠遮挡；
    /// - 0.0~0.55s 九宫格战术落位亮相 -> 地面粉笔阵位标线柔和淡化至 25% -> 近战冲锋劈砍嘲讽、远程动态走位风筝与特色弹道火海；
    /// - 多波次丧尸进场（普通游尸、安全帽重甲尸、疾跑尸、红圈预警抛物线切后排【跳尸突袭者】、1.45x 巨型光环【变异尸王】）；
    /// - 支持 1x/2x 倍速、自动/手动大招、以及 5 秒后一键确定性内存秒算结算 SkipToBattleResolution()。
    /// </summary>
    public sealed class ForestTacticalArena
    {
        public static readonly Vector3 ArenaCenter = new Vector3(220f, 220f, 0f);
        public const float ArenaOrthoSize = 6.4f;
        public const float StageDurationLimit = 90f;
        public const float ClashIntroSec = 0.55f;

        // 九宫格布阵本地坐标（slot = row * 3 + col; row 0..2 上中下, col 0..2 后中前）
        static readonly float[] RowLocalY = { 1.15f, -0.35f, -1.85f };
        static readonly float[] ColLocalX = { -2.85f, -1.85f, -0.85f };

        enum EnemyKind
        {
            Shambler = 0,
            HardhatArmored = 1,
            FastRunner = 2,
            BacklineJumper = 3,
            GiantBoss = 4,
        }

        enum ProjectileStyle
        {
            Bolt = 0,
            Buckshot = 1,
            MolotovArc = 2,
            HealHerb = 3,
            RainArrow = 4,
            CryoFlaskArc = 5,
            ElectricBolt = 6,
            GrappleClawBolt = 7,
        }

        enum DamageType
        {
            Physical = 0,
            Elemental = 1,
        }

        struct QueuedEnemySpec
        {
            public EnemyKind kind;
            public int subWave;
            public float spawnTime;
            public int laneRow;
            public float maxHp;
            public float atk;
            public float speed;
            public float armor;
            public float resistance;
            public bool isElementalMutant;
        }

        sealed class OverheadBarRig
        {
            public Transform root;
            public SpriteRenderer hpFill;
            public SpriteRenderer shieldFill;
            public SpriteRenderer rageFill;
            public float innerWidth;
        }

        sealed class HeroUnit
        {
            public int heroIdx;
            public int slot0to8;
            public int row;
            public int col;
            public int roleId; // 0:Vanguard, 1:Guardian, 2:Demolition, 3:Ranger, 4:Gunsmith, 5:Medic
            public HeroFaction faction;
            public string heroId;
            public string name;
            public int star;
            public int skillLv0 = 1;
            public int skillLv1;
            public int skillLv2;
            public int skillLv3;
            public string skillName;
            public bool isMelee;

            public Vector2 formPos;
            public Vector2 pos;
            public Vector2 lungeOffset;
            public Vector2 knockbackVel;
            public bool facingRight = true;

            public bool alive = true;
            public float hp;
            public float maxHp;
            public float shield;
            public float baseAtk;
            public float atk;
            public float skillPower;
            public int range = 1;
            public float armor;
            public float resistance;
            public float dmgReduction;
            public float atkSpeedMul = 1f;
            public float critChance = 0.10f;
            public float critMult = 1.55f;
            public float rageGainMul = 1f;
            public float rage01;
            public float thornsPct;
            public float lifestealPct;
            public float healShieldBonus;
            public float activeSkillMult = 2.20f;
            public float elementalDmgMul = 1f;
            public float frontlineRegenPct;
            public float aggroWeight = 1f;

            // 徽章与 2★/3★ 专属战斗状态追踪
            public bool hasBloodFuryBadge;
            public bool hasEagleScopeBadge;
            public bool hasMedicCrossBadge;
            public bool dachuiUltShieldActive;
            public float dachuiShieldTimer;
            public float dachuiBurstCooldown;
            public float dachuiTauntTimer;
            public int leimengUltSpdStacks;
            public int linyanUltStacks;
            public int doubleBarrelPending;
            public float doubleBarrelTimer;
            public EnemyUnit doubleBarrelTarget;
            public float medicSpiceTimer = 1.2f;
            public float medicAtkSpdBuffTimer;
            public float soupResistTimer;
            public float soupResistBonusMul;

            public float atkCooldown;
            public int swingCount;
            public float hitFlashTimer;
            public float teamSpdBuffTimer;
            public float gunSalvoTimer;
            public int gunSalvoRemaining;

            public float totalDamage;
            public float totalHeal;

            public ForestHeroPuppet puppet;
            public Transform shadowTr;
            public OverheadBarRig bars;
            public SpriteRenderer[] tintRenderers;
            public Color[] baseColors;
        }

        sealed class EnemyUnit
        {
            public EnemyKind kind;
            public int laneRow;
            public Vector2 pos;
            public Vector2 lungeOffset;
            public Vector2 knockbackVel;
            public float baseScale = 0.88f;

            public bool alive = true;
            public float hp;
            public float maxHp;
            public float atk;
            public float speed;
            public float armor;
            public float resistance;
            public bool isElementalMutant;
            public float defenseShredTimer;
            public float asaArmorShredTimer;
            public HeroUnit asaLastHitter;
            public float asaLastHitTimer;
            public float chillTimer;
            public float cryoFreezeTimer;
            public Vector2 grappleVisualFrom;
            public float grappleVisualTimer;
            public float grappleVisualDur;
            public float bleedTimer;
            public float bleedTickTimer;
            public float bleedDps;
            public HeroUnit bleedOwner;
            public float atkRange = 0.66f;
            public float atkCooldown;

            public float phase;
            public float elapsedEntry;
            public float hitFlashTimer;
            public float stunTimer;

            // 跳尸突袭者状态机：0=进场寻位, 1=红圈预警蓄力, 2=高抛腾空跃后排, 3=落地切后排
            public int jumpPhase;
            public float jumpTimer;
            public Vector2 jumpStart;
            public Vector2 jumpTarget;
            public Transform warningRingTr;
            public SpriteRenderer warningRingSr;

            public Transform tr;
            public SpriteRenderer sr;
            public Transform shadowTr;
            public Transform bossAuraTr;
            public OverheadBarRig bars;
            public ForestSpriteFlipbook flipbook;
            public bool isMoving;
            public float attackAnimTimer;
            public float barYOffset = 0.94f;
        }

        sealed class ArenaProjectile
        {
            public ProjectileStyle style;
            public DamageType dmgType;
            public HeroUnit sourceHero;
            public EnemyUnit targetEnemy;
            public HeroUnit targetAlly;
            public Vector2 fromLocal;
            public Vector2 toLocal;
            public Vector2 curLocal;
            public Vector2 dirNorm;
            public float arcHeight;
            public float elapsed;
            public float duration;
            public float amount;
            public float splashRadius;
            public float firePoolDps;
            public float firePoolLife;
            public float chillDuration;
            public float knockback;
            public bool isCrit;
            public bool piercing;
            public int maxPierceCount; // 0 = unlimited piercing, >0 = stop after N hits (e.g. Lin Yan 2★ crit bolt = 2)
            public readonly HashSet<EnemyUnit> piercedSet = new HashSet<EnemyUnit>();
            public Transform tr;
            public SpriteRenderer sr;
        }

        sealed class ArenaFirePool
        {
            public HeroUnit owner;
            public Vector2 localPos;
            public float radius;
            public float dps;
            public float life;
            public float maxLife;
            public float tickTimer;
            public bool shredsDefenses;
            public bool detonateOnExpire;
            public Transform tr;
            public SpriteRenderer sr;
        }

        sealed class ArenaFrostZone
        {
            public HeroUnit owner;
            public Vector2 localPos;
            public float radius;
            public float life;
            public float maxLife;
            public Transform tr;
            public SpriteRenderer sr;
        }

        sealed class ActiveChainStorm
        {
            public HeroUnit caster;
            public float baseBounceDmg;
            public int bouncesDone;
            public int maxBounces;
            public int extraBouncesGranted;
            public float timer;
            public EnemyUnit lastHitEnemy;
            public Vector2 lastAnchorPos;
            public readonly Dictionary<EnemyUnit, int> hitCounts = new Dictionary<EnemyUnit, int>(12);
        }

        sealed class ArenaSegmentFx
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Transform tipTr;
            public SpriteRenderer tipSr;
            public HeroUnit anchorHero;
            public EnemyUnit targetEnemy;
            public Vector2 fixedFrom;
            public Vector2 fixedTo;
            public bool trackUnits;
            public float thickness;
            public Color color;
            public float elapsed;
            public float duration;
        }

        sealed class WorldTextPopup
        {
            public RectTransform rt;
            public Text textComp;
            public Vector2 startLocal;
            public Color baseColor;
            public float baseScale;
            public float elapsed;
            public float duration;
        }

        sealed class TransientFx
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 startLocal;
            public Vector2 endLocal;
            public float startScale;
            public float endScale;
            public Color color;
            public float elapsed;
            public float duration;
        }

        sealed class WorldNumberPopup
        {
            public Transform root;
            public readonly List<SpriteRenderer> glyphRenderers = new List<SpriteRenderer>(8);
            public Vector2 startLocal;
            public float elapsed;
            public float duration;
            public Color baseColor;
        }

        readonly Camera _cam;
        readonly ForestSurvivalSim _sim;
        readonly ForestSurvivalWorld _world;

        GameObject _stageRoot;
        Transform _bgRoot;
        Transform _decalRoot;
        Transform _hazardRoot;
        Transform _shadowRoot;
        Transform _unitRoot;
        Transform _projRoot;
        Transform _fxRoot;
        Canvas _worldTextCanvas;
        RectTransform _worldTextCanvasRt;

        SpriteRenderer _fallbackBgSr;
        SpriteRenderer _artBgSr;
        readonly SpriteRenderer[] _slotDecalSrs = new SpriteRenderer[9];
        readonly bool[] _slotDecalHighlighted = new bool[9];
        SpriteRenderer _scrimmageLineSr;

        readonly List<HeroUnit> _heroes = new List<HeroUnit>(9);
        readonly List<ArenaHeroHudState> _heroStates = new List<ArenaHeroHudState>(9);
        readonly List<QueuedEnemySpec> _spawnQueue = new List<QueuedEnemySpec>(40);
        readonly List<EnemyUnit> _aliveEnemies = new List<EnemyUnit>(32);
        readonly List<ArenaProjectile> _projectiles = new List<ArenaProjectile>(48);
        readonly List<ArenaFirePool> _firePools = new List<ArenaFirePool>(12);
        readonly List<ArenaFrostZone> _frostZones = new List<ArenaFrostZone>(8);
        readonly List<ActiveChainStorm> _chainStorms = new List<ActiveChainStorm>(6);
        readonly List<ArenaSegmentFx> _segmentFxs = new List<ArenaSegmentFx>(32);
        readonly List<TransientFx> _transientFxs = new List<TransientFx>(32);
        readonly List<WorldNumberPopup> _numberPopups = new List<WorldNumberPopup>(24);
        readonly List<WorldTextPopup> _textPopups = new List<WorldTextPopup>(16);

        System.Random _battleRng = new System.Random(2026);
        bool _isFastSimulating;
        bool _hasSavedCam;
        Vector3 _savedCamPos;
        float _savedCamOrtho;
        Color _savedCamBg;
        float _camShakeTimer;
        float _camShakeMag;
        float _queuedHpSum;

        // 共享程序化精灵缓存（零额外 IO，确保特效/血条/跳尸预警圈/治疗数字 100% 清晰锐利）
        static Sprite _spWhiteQuad;
        static Sprite _spWhiteLeftBar;
        static Sprite _spShadowOval;
        static Sprite _spGridBracket;
        static Sprite _spWarningRing;
        static Sprite _spShockRing;
        static Sprite _spCleaveWave;
        static Sprite _spBossCrown;
        static Sprite _spFallbackArenaBg;
        static Sprite _spChainSegment;
        static Sprite _spHookClaw;
        static Sprite _spFrostCircle;
        static Sprite _spLightningSeg;
        static readonly Dictionary<char, Sprite> _glyphSprites = new Dictionary<char, Sprite>(16);

        public bool IsBattleActive { get; private set; }
        public bool IsResultPending { get; private set; }
        public int ActiveStage { get; private set; }
        public string ActiveStageTitle { get; private set; } = "";
        public float StageTimerRemaining { get; private set; } = StageDurationLimit;
        public float ElapsedBattleSec { get; private set; }
        public bool CanSkipBattle => IsBattleActive && !IsResultPending && ElapsedBattleSec >= 5f;
        public int SquadPower { get; private set; }
        public int EnemyPower { get; private set; }
        public float TeamTotalHp { get; private set; }
        public float TeamTotalMaxHp { get; private set; }
        public float EnemyTotalHp { get; private set; }
        public float EnemyTotalMaxHp { get; private set; }
        public int EnemiesAliveAndQueued { get; private set; }
        public int EnemiesTotal { get; private set; }
        public bool AutoCastUlt { get; set; } = true;
        public int SpeedMultiplier { get; private set; } = 1;
        public IReadOnlyList<ArenaHeroHudState> HeroStates => _heroStates;
        public ArenaResultSummary LastResult { get; private set; }

        public ForestTacticalArena(Camera cam, ForestSurvivalSim sim, ForestSurvivalWorld world)
        {
            _cam = cam;
            _sim = sim;
            _world = world;
            EnsureSharedSprites();
        }

        public void ToggleSpeed()
        {
            SpeedMultiplier = SpeedMultiplier == 1 ? 2 : 1;
        }

        public bool StartStageBattle(int stage1Based, out string failReason)
        {
            failReason = "";
            if (_sim == null)
            {
                failReason = "战斗数据尚未初始化";
                return false;
            }

            int clampedStage = Mathf.Clamp(stage1Based, 1, ForestSurvivalSim.MaxBattleStages);
            if (_sim.GetDeployedHeroCount() <= 0)
                _sim.AutoDeployRecommendedFormation();

            if (_sim.GetDeployedHeroCount() <= 0)
            {
                failReason = "请至少部署 1 名上阵骨干！";
                return false;
            }

            Camera cam = ResolveCamera();
            if (cam != null && !_hasSavedCam)
            {
                _savedCamPos = cam.transform.position;
                _savedCamOrtho = cam.orthographicSize;
                _savedCamBg = cam.backgroundColor;
                _hasSavedCam = true;
            }

            EnsureStageHierarchy();
            ClearCombatEntities();
            _stageRoot.SetActive(true);

            var spec = _sim.GetBattleStageSpec(clampedStage);
            ActiveStage = clampedStage;
            ActiveStageTitle = !string.IsNullOrEmpty(spec.title) ? spec.title : $"第 {clampedStage} 关";
            StageTimerRemaining = StageDurationLimit;
            ElapsedBattleSec = 0f;
            IsBattleActive = true;
            IsResultPending = false;
            LastResult = default;
            _camShakeTimer = 0f;
            _camShakeMag = 0f;
            _battleRng = new System.Random(10007 + clampedStage * 7919 + _sim.CalcDeployedSquadPower() * 13);

            RefreshBackdropAndGridDecals();
            SpawnDeployedHeroes();
            BuildDeterministicStageWaves(
                clampedStage,
                spec.enemyTotalCount,
                spec.recommendedPower,
                spec.isBossStage,
                spec.hasBacklineFlanker);

            SquadPower = Mathf.Max(1, _sim.CalcDeployedSquadPower());
            EnemyPower = Mathf.Max(1, spec.recommendedPower);

            // 立即刷出首波先遣尸群于右侧桥面入口，让 0.0~0.55s 阵型亮相期即可看清敌我态势！
            FlushDueEnemySpawns(forceInitialWave: true);
            RefreshAggregatedStatsAndHudStates();
            LockCameraToArena(cam);

            return true;
        }

        public bool TryCastHeroUlt(int heroIdx)
        {
            if (!IsBattleActive || IsResultPending) return false;
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (h.heroIdx == heroIdx)
                {
                    if (!h.alive || h.rage01 < 0.999f) return false;
                    ExecuteHeroUltimate(h);
                    RefreshAggregatedStatsAndHudStates();
                    return true;
                }
            }
            return false;
        }

        public void SkipToBattleResolution()
        {
            if (!IsBattleActive || IsResultPending) return;

            _isFastSimulating = true;
            try
            {
                for (int step = 0; step < 900 && IsBattleActive && !IsResultPending; step++)
                    StepArena(0.1f);

                if (IsBattleActive && !IsResultPending)
                {
                    bool won = _aliveEnemies.Count == 0 && _spawnQueue.Count == 0;
                    ResolveBattleOutcome(won);
                }
            }
            finally
            {
                _isFastSimulating = false;
            }

            ClearTransientVisualsAfterSkip();
            SyncAllUnitVisuals(0.016f, 0.016f);
            RefreshAggregatedStatsAndHudStates();
        }

        public void ExitArenaToCamp()
        {
            ClearCombatEntities();
            if (_stageRoot != null)
                _stageRoot.SetActive(false);

            bool wasActive = IsBattleActive;
            IsBattleActive = false;
            IsResultPending = false;
            _camShakeTimer = 0f;

            Camera cam = ResolveCamera();
            if (cam != null && _hasSavedCam)
            {
                cam.transform.position = _savedCamPos;
                cam.orthographicSize = _savedCamOrtho;
                cam.backgroundColor = _savedCamBg;
                _hasSavedCam = false;
                if (_world != null && _world.View != null)
                    _world.View.SyncCameraOverlays(force: true);
            }

            if (wasActive && _world != null)
                _world.NotifyArenaExitedToCamp(LastResult.victory);
        }

        public void Tick(float dt)
        {
            if (!IsBattleActive) return;

            Camera cam = ResolveCamera();
            RefreshBackdropAspectScale(cam);

            if (IsResultPending)
            {
                TickPostBattleIdle(dt);
                LockCameraToArena(cam);
                return;
            }

            float scaledDt = Mathf.Clamp(dt, 0f, 0.1f) * SpeedMultiplier;
            // 大步长拆分为 <= 0.05s 子步，保证 2x 倍速下弹道与碰撞同样丝滑精准
            int subSteps = scaledDt > 0.055f ? 2 : 1;
            float stepDt = scaledDt / subSteps;
            for (int s = 0; s < subSteps && IsBattleActive && !IsResultPending; s++)
                StepArena(stepDt);

            SyncAllUnitVisuals(scaledDt, Mathf.Clamp(dt, 0f, 0.1f));
            TickVisualOnlyPopupsAndFx(dt);
            LockCameraToArena(cam);
        }

        // =====================================================================
        // 核心确定性战斗步进（实机帧循环与 SkipToBattleResolution 共用同一套逻辑！）
        // =====================================================================
        public void StepArena(float dt)
        {
            if (!IsBattleActive || IsResultPending || dt <= 0f) return;

            ElapsedBattleSec += dt;
            StageTimerRemaining = Mathf.Max(0f, StageTimerRemaining - dt);

            // 1. 地面九宫格战术粉笔框在 0.55s 开战冲锋后柔和淡化至 25% 透明度
            UpdateGridDecalFade();

            // 2. 按波次节奏刷出后续丧尸子波
            FlushDueEnemySpawns(forceInitialWave: false);

            // 3. 推进地面燃烧瓶火海与寒霜冻结法阵
            StepFirePools(dt);
            StepFrostZones(dt);
            if (CheckBattleEndConditions()) return;

            // 3b. 推进周博士超载电磁风暴连环闪电弹射
            StepChainStorms(dt);
            if (CheckBattleEndConditions()) return;

            // 4. 推进我方骨干动态走位、普攻、治疗与自动大招
            StepHeroes(dt);
            if (CheckBattleEndConditions()) return;

            // 5. 推进飞行弹道（弩箭、霰弹、高抛燃烧瓶、草药飞镖、穿透箭雨）
            StepProjectiles(dt);
            if (CheckBattleEndConditions()) return;

            // 6. 推进丧尸军团（普通丧尸前排集火、跳尸红圈预警飞跃切后排、尸王范围重砸）
            StepEnemies(dt);
            if (CheckBattleEndConditions()) return;

            // 7. 柔和单位防重叠微分离 & 汇总实时血量/怒气状态
            ResolveSoftUnitSeparation();
            RefreshAggregatedStatsAndHudStates();
        }

        bool CheckBattleEndConditions()
        {
            if (!IsBattleActive || IsResultPending) return true;

            if (_aliveEnemies.Count == 0 && _spawnQueue.Count == 0)
            {
                ResolveBattleOutcome(victory: true);
                return true;
            }

            bool anyHeroAlive = false;
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].alive && _heroes[i].hp > 0f)
                {
                    anyHeroAlive = true;
                    break;
                }
            }

            if (!anyHeroAlive || StageTimerRemaining <= 0f)
            {
                ResolveBattleOutcome(victory: false);
                return true;
            }
            return false;
        }

        void ResolveBattleOutcome(bool victory)
        {
            if (IsResultPending) return;
            IsResultPending = true;

            if (victory)
            {
                for (int i = 0; i < _heroes.Count; i++)
                {
                    if (_heroes[i].alive && _heroes[i].puppet != null)
                        _heroes[i].puppet.SetMode(HeroAnimMode.Cheer);
                }
            }

            int mvpHeroIdx = _heroes.Count > 0 ? _heroes[0].heroIdx : 0;
            string mvpName = _heroes.Count > 0 ? _heroes[0].name : "大锤";
            float bestScore = -1f;
            int mvpContribution = 0;

            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                float score = h.totalDamage + h.totalHeal;
                if (score > bestScore)
                {
                    bestScore = score;
                    mvpHeroIdx = h.heroIdx;
                    mvpName = h.name;
                    mvpContribution = Mathf.Max(1, Mathf.RoundToInt(h.totalDamage + h.totalHeal));
                }
            }

            string rewardText = victory
                ? (_sim.GrantBattleStageVictory(ActiveStage) ?? "")
                : "防线失守，营地物资零损失！可调整九宫格站位或强化骨干后再战。";

            LastResult = new ArenaResultSummary
            {
                finished = true,
                victory = victory,
                stage = ActiveStage,
                stageTitle = ActiveStageTitle,
                mvpHeroIdx = mvpHeroIdx,
                mvpName = mvpName,
                mvpDamage = mvpContribution,
                rewardSummary = rewardText,
                durationSec = ElapsedBattleSec,
            };

            RefreshAggregatedStatsAndHudStates();
            _world?.NotifyArenaWaveSettled(victory);
        }

        // =====================================================================
        // 英雄生成与动态战术 AI（六维属性 + 射程分级走位 + 2★/3★质变被动 + 战术徽章）
        // =====================================================================
        void SpawnDeployedHeroes()
        {
            var syn = _sim.GetCombinedSynergyTotals();
            bool isIronBastion = _sim.SelectedFormation == TacticalFormationId.IronBastion;

            for (int slot = 0; slot < 9 && slot < _sim.FormationSlots.Length; slot++)
            {
                int heroIdx = _sim.FormationSlots[slot];
                if (heroIdx < 0 || heroIdx >= _sim.Heroes.Count) continue;

                HeroSpec spec = _sim.Heroes[heroIdx];
                if (spec == null || spec.id == "ache") continue;

                int row = slot / 3;
                int col = slot % 3;
                Vector2 formLocal = new Vector2(ColLocalX[col], RowLocalY[row]);

                HeroComputedStats fullStats = _sim.CalcHeroFullStats(heroIdx, slot, includeSquadSynergy: true);
                var slotBuff = _sim.GetFormationSlotBuff(_sim.SelectedFormation, slot);
                int roleId = (int)_sim.GetHeroCombatRole(heroIdx);

                HeroSkillDef sk0 = _sim.GetHeroSkillDef(heroIdx, 0);
                HeroSkillDef sk1 = _sim.GetHeroSkillDef(heroIdx, 1);
                HeroSkillDef sk2 = _sim.GetHeroSkillDef(heroIdx, 2);
                HeroSkillDef sk3 = _sim.GetHeroSkillDef(heroIdx, 3);

                int lv0 = sk0.currentLv > 0 ? sk0.currentLv : ((spec.skillLevels != null && spec.skillLevels.Length > 0) ? Mathf.Max(1, spec.skillLevels[0]) : 1);
                int lv1 = sk1.isUnlocked ? Mathf.Max(1, sk1.currentLv) : ((spec.skillLevels != null && spec.skillLevels.Length > 1 && spec.star >= 1) ? Mathf.Max(1, spec.skillLevels[1]) : 0);
                int lv2 = sk2.isUnlocked ? Mathf.Max(1, sk2.currentLv) : ((spec.skillLevels != null && spec.skillLevels.Length > 2 && spec.star >= 2) ? Mathf.Max(1, spec.skillLevels[2]) : 0);
                int lv3 = sk3.isUnlocked ? Mathf.Max(1, sk3.currentLv) : ((spec.skillLevels != null && spec.skillLevels.Length > 3 && spec.star >= 3) ? Mathf.Max(1, spec.skillLevels[3]) : 0);

                string skillTitle = !string.IsNullOrEmpty(sk0.name) ? sk0.name : _sim.GetHeroSkillTitle(heroIdx);
                if (string.IsNullOrEmpty(skillTitle)) skillTitle = spec.skillName;

                string hid = !string.IsNullOrEmpty(spec.id) ? spec.id : spec.characterId;
                string cid = !string.IsNullOrEmpty(spec.characterId) ? spec.characterId : hid;
                if (hid == "leimeng" || cid == "hunter")
                    cid = "leimeng";
                else if (hid == "tieshu")
                    cid = "tieshu";

                int rangeCells = Mathf.Clamp(fullStats.range > 0 ? fullStats.range : (spec.rangeCells > 0 ? spec.rangeCells : (_sim.IsHeroMelee(heroIdx) ? 1 : 4)), 1, 5);
                if (hid == "asa") rangeCells = 2;
                else if (hid == "qinghe") rangeCells = 4;
                else if (hid == "zhoudr") rangeCells = 5;
                bool melee = rangeCells <= 1;

                float maxHp = Mathf.Max(80f, fullStats.maxHp);
                float atk = Mathf.Max(12f, fullStats.atk);
                float skillPower = Mathf.Max(12f, fullStats.skillPower);
                float armor = Mathf.Max(0f, fullStats.armor);
                float resistance = Mathf.Max(0f, fullStats.resistance);
                float critChance = Mathf.Clamp(fullStats.critRate, 0.04f, 0.90f);
                float critMult = Mathf.Max(1.25f, fullStats.critMult);
                float startRage01 = Mathf.Clamp01(fullStats.startRage / 100f);
                float thornsPct = Mathf.Max(0f, fullStats.thornsPct);
                float lifestealPct = Mathf.Max(0f, fullStats.lifestealPct);
                float healShieldBonus = Mathf.Max(0f, fullStats.healShieldBonus);
                float activeSkillMult = fullStats.activeSkillMult > 0.05f ? fullStats.activeSkillMult : 2.20f;

                float dmgRed = Mathf.Clamp(slotBuff.dmgReduction + syn.dmgRedBonus + (col == 2 ? 0.04f : 0f), 0f, 0.65f);
                bool isLinYan = hid == "linyan" || (roleId == 3 && hid != "qinghe" && hid != "zhoudr" && hid != "asa");
                float linyanPassiveSpd = (isLinYan && lv1 > 0) ? (0.08f + 0.04f * lv1) : 0f;
                float atkSpd = Mathf.Max(0.5f, 1f + slotBuff.atkSpeedBonus + syn.atkSpdBonus + linyanPassiveSpd);
                float rageMul = Mathf.Max(0.6f, 1f + syn.rageGainBonus);

                bool hasBloodFury = false;
                bool hasEagle = false;
                bool hasPyro = false;
                bool hasMedic = false;
                if (spec.equippedBadges != null)
                {
                    for (int b = 0; b < spec.equippedBadges.Length; b++)
                    {
                        switch (spec.equippedBadges[b])
                        {
                            case TacticalBadgeId.BloodFury: hasBloodFury = true; break;
                            case TacticalBadgeId.EagleScope: hasEagle = true; break;
                            case TacticalBadgeId.PyroPowder: hasPyro = true; break;
                            case TacticalBadgeId.MedicCross: hasMedic = true; break;
                        }
                    }
                }

                PuppetRigProfile rigProf = PuppetRegistry.GetProfile(cid);
                if (rigProf == null || rigProf.characterId == "ache")
                    rigProf = ForestSurvivalWorld.ResolveCompanionRigProfile(hid, cid);
                var puppet = new ForestHeroPuppet(_unitRoot, rigProf, $"ArenaHero_{hid}_s{slot}");
                puppet.SetSkinTier(spec.skinTier, autoFallbackToBase: true);
                puppet.SetFacing(true);
                puppet.SetMode(HeroAnimMode.Idle);

                Transform shadowTr = CreateRoadShadow($"HeroShadow_{hid}", formLocal, melee ? 0.66f : 0.58f, 0.24f);
                OverheadBarRig bars = CreateOverheadBars(puppet.Root, isHero: true, width: 0.74f, yOffset: 0.94f);

                // 收集身体部件渲染器以支持受击闪白（排除脚下光环、刀光与头顶血条）
                var allSrs = puppet.Root.GetComponentsInChildren<SpriteRenderer>(true);
                var bodySrs = new List<SpriteRenderer>(12);
                var baseCols = new List<Color>(12);
                for (int r = 0; r < allSrs.Length; r++)
                {
                    SpriteRenderer sr = allSrs[r];
                    if (sr == null) continue;
                    if (sr.transform.IsChildOf(bars.root)) continue;
                    string n = sr.gameObject.name;
                    if (n == "HeroGroundRing" || n == "SlashArc") continue;
                    bodySrs.Add(sr);
                    baseCols.Add(sr.color);
                }

                float aggro = col == 2 ? 1.85f : (col == 1 ? 1.15f : 0.72f);
                if (hid == "dachui" || (roleId == 1 && hid != "asa")) aggro += 1.65f; // 大锤天生嘲讽吸怪

                float openingShield = (hid == "dachui" || (roleId == 1 && hid != "asa"))
                    ? maxHp * 0.12f * (1f + healShieldBonus)
                    : 0f;

                var unit = new HeroUnit
                {
                    heroIdx = heroIdx,
                    slot0to8 = slot,
                    row = row,
                    col = col,
                    roleId = roleId,
                    faction = spec.faction,
                    heroId = hid,
                    name = spec.name,
                    star = Mathf.Clamp(spec.star, 0, 5),
                    skillLv0 = lv0,
                    skillLv1 = lv1,
                    skillLv2 = lv2,
                    skillLv3 = lv3,
                    skillName = skillTitle,
                    isMelee = melee,
                    formPos = formLocal,
                    pos = formLocal,
                    facingRight = true,
                    alive = true,
                    hp = maxHp,
                    maxHp = maxHp,
                    shield = openingShield,
                    baseAtk = atk,
                    atk = atk,
                    skillPower = skillPower,
                    range = rangeCells,
                    armor = armor,
                    resistance = resistance,
                    dmgReduction = dmgRed,
                    atkSpeedMul = atkSpd,
                    critChance = critChance,
                    critMult = critMult,
                    rageGainMul = rageMul,
                    rage01 = startRage01,
                    thornsPct = thornsPct,
                    lifestealPct = lifestealPct,
                    healShieldBonus = healShieldBonus,
                    activeSkillMult = activeSkillMult,
                    elementalDmgMul = hasPyro ? 1.18f : 1f,
                    frontlineRegenPct = (isIronBastion && col == 2) ? 0.015f : 0f,
                    aggroWeight = aggro,
                    hasBloodFuryBadge = hasBloodFury,
                    hasEagleScopeBadge = hasEagle,
                    hasMedicCrossBadge = hasMedic,
                    medicSpiceTimer = 1.2f,
                    atkCooldown = 0.12f + col * 0.06f,
                    puppet = puppet,
                    shadowTr = shadowTr,
                    bars = bars,
                    tintRenderers = bodySrs.ToArray(),
                    baseColors = baseCols.ToArray(),
                };
                unit.pos = ClampHeroPos(unit, unit.pos);
                unit.formPos = unit.pos;

                SyncHeroVisualImmediate(unit);
                _heroes.Add(unit);
            }

            // 周博士 2★ 被动【静电场域】：开场若已部署 2★+ 周博士，为全体上阵【废土科工】(周博士/小棠/铁叔) 提供开场电磁护盾！
            HeroUnit zhou2Star = null;
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].heroId == "zhoudr" && _heroes[i].star >= 2)
                {
                    zhou2Star = _heroes[i];
                    break;
                }
            }
            if (zhou2Star != null)
            {
                float shieldPct = (0.15f + 0.03f * Mathf.Max(0, zhou2Star.skillLv2 - 1)) * (1f + zhou2Star.healShieldBonus);
                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive) continue;
                    bool isWasteTech = ally.faction == HeroFaction.WasteTech
                        || ally.heroId == "zhoudr" || ally.heroId == "xiaotang" || ally.heroId == "tieshu";
                    if (!isWasteTech) continue;

                    float emShield = ally.maxHp * shieldPct;
                    ally.shield = Mathf.Min(ally.maxHp * 1.35f, ally.shield + emShield);
                    zhou2Star.totalHeal += emShield * 0.5f;
                    SyncHeroVisualImmediate(ally);
                    SpawnTransientFx(_spShockRing, ally.pos + new Vector2(0f, 0.14f), ally.pos + new Vector2(0f, 0.14f),
                        0.24f, 1.05f, new Color(0.48f, 0.82f, 1f, 0.88f), 0.42f, sortOrder: 2460);
                }
            }
        }

        void StepHeroes(float dt)
        {
            bool inIntro = ElapsedBattleSec < ClashIntroSec;

            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (!h.alive) continue;

                if (h.hitFlashTimer > 0f) h.hitFlashTimer = Mathf.Max(0f, h.hitFlashTimer - dt);
                if (h.teamSpdBuffTimer > 0f) h.teamSpdBuffTimer = Mathf.Max(0f, h.teamSpdBuffTimer - dt);
                if (h.medicAtkSpdBuffTimer > 0f) h.medicAtkSpdBuffTimer = Mathf.Max(0f, h.medicAtkSpdBuffTimer - dt);
                if (h.soupResistTimer > 0f) h.soupResistTimer = Mathf.Max(0f, h.soupResistTimer - dt);
                if (h.dachuiTauntTimer > 0f) h.dachuiTauntTimer = Mathf.Max(0f, h.dachuiTauntTimer - dt);
                if (h.dachuiBurstCooldown > 0f) h.dachuiBurstCooldown = Mathf.Max(0f, h.dachuiBurstCooldown - dt);
                h.lungeOffset = Vector2.MoveTowards(h.lungeOffset, Vector2.zero, dt * 1.8f);

                if (h.knockbackVel.sqrMagnitude > 0.0004f)
                {
                    h.pos = ClampHeroPos(h, h.pos + h.knockbackVel * dt);
                    h.knockbackVel = Vector2.MoveTowards(h.knockbackVel, Vector2.zero, dt * 7.5f);
                }

                // 铁桶护甲阵前排回血续航 (1.5% MaxHP/s)
                if (!inIntro && h.frontlineRegenPct > 0f && h.hp < h.maxHp)
                {
                    float reg = h.maxHp * h.frontlineRegenPct * dt * (1f + h.healShieldBonus);
                    h.hp = Mathf.Min(h.maxHp, h.hp + reg);
                    h.totalHeal += reg;
                }

                // 大锤 3★ 觉醒：主动护盾自然到期结束时，同样引爆二次重盾震波眩晕周围丧尸 1.5s！
                if (!inIntro && h.dachuiUltShieldActive)
                {
                    h.dachuiShieldTimer -= dt;
                    if (h.dachuiShieldTimer <= 0f)
                    {
                        h.dachuiUltShieldActive = false;
                        h.dachuiShieldTimer = 0f;
                        if (h.star >= 3)
                            TriggerDachuiSecondaryShockwave(h);
                    }
                }

                // 老陈头 2★ 被动【振奋浓香】：每 9s 为攻击力最高的存活友军提升 +18% 攻速
                bool isMedic = h.heroId == "laochentou" || (h.roleId == 5 && h.heroId != "qinghe" && h.heroId != "zhoudr" && h.heroId != "asa");
                if (!inIntro && isMedic && h.star >= 2)
                {
                    h.medicSpiceTimer -= dt;
                    if (h.medicSpiceTimer <= 0f)
                    {
                        h.medicSpiceTimer = 9.0f;
                        ApplyMedicSpiceBuffToTopAtkAlly(h);
                    }
                }

                // 铁叔 2★ 被动【压制装填】：第 4 次普攻触发的第二发双管速射跟进
                if (h.doubleBarrelPending > 0)
                {
                    h.doubleBarrelTimer -= dt;
                    if (h.doubleBarrelTimer <= 0f)
                    {
                        h.doubleBarrelPending = 0;
                        EnemyUnit dbTarget = IsEnemyTargetable(h.doubleBarrelTarget) ? h.doubleBarrelTarget : PickTargetForHero(h);
                        if (dbTarget != null)
                            FireTieShuBuckshotSpread(h, dbTarget, isDoubleBarrelBonus: true);
                    }
                }

                // 铁叔大招连发弹幕计时
                if (h.gunSalvoRemaining > 0)
                {
                    h.gunSalvoTimer -= dt;
                    if (h.gunSalvoTimer <= 0f)
                    {
                        h.gunSalvoTimer = 0.11f;
                        h.gunSalvoRemaining--;
                        FireGunsmithSalvoBurst(h);
                    }
                }

                // 0.0 ~ 0.55s 战术阵型展示：全员稳立九宫格阵位蓄势待发
                if (inIntro)
                {
                    h.facingRight = true;
                    h.puppet?.SetMode(HeroAnimMode.Idle);
                    continue;
                }

                // 自动绝技释放（仅当已有丧尸冲入可见索敌区 x <= 2.25f 时释放，绝不对屏外空放！）
                if (AutoCastUlt && h.rage01 >= 0.999f && HasAnyTargetableEnemy())
                {
                    ExecuteHeroUltimate(h);
                    if (IsResultPending) return;
                }

                bool bloodFuryActive = (h.hasBloodFuryBadge || h.lifestealPct > 0.001f) && (h.hp < h.maxHp * 0.50f);
                float effAtkSpd = h.atkSpeedMul
                    * (h.teamSpdBuffTimer > 0f ? 1.24f : 1f)
                    * (h.medicAtkSpdBuffTimer > 0f ? 1.18f : 1f)
                    * (1f + h.leimengUltSpdStacks * 0.06f)
                    * (bloodFuryActive ? 1.30f : 1f);

                if (h.atkCooldown > 0f)
                    h.atkCooldown -= dt * effAtkSpd;

                // 战地医官（老陈头）：优先治疗受伤队友，满血时投掷草药毒镖助战
                if (isMedic)
                {
                    StepMedicHero(h, dt);
                    continue;
                }

                EnemyUnit target = PickTargetForHero(h);
                if (target == null)
                {
                    h.facingRight = true;
                    h.puppet?.SetMode(HeroAnimMode.Idle);
                    continue;
                }

                if (h.range <= 1)
                {
                    StepMeleeHero(h, target, dt);
                }
                else
                {
                    StepRangedHero(h, target, dt);
                }
            }
        }

        void ApplyMedicSpiceBuffToTopAtkAlly(HeroUnit medic)
        {
            HeroUnit bestAlly = null;
            float topAtk = -1f;
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit ally = _heroes[i];
                if (!ally.alive) continue;
                float score = ally.atk + (ally == medic ? -5f : 0f);
                if (score > topAtk)
                {
                    topAtk = score;
                    bestAlly = ally;
                }
            }
            if (bestAlly == null) return;

            bestAlly.medicAtkSpdBuffTimer = Mathf.Max(bestAlly.medicAtkSpdBuffTimer, 6.5f);
            SpawnTransientFx(_spShockRing, bestAlly.pos + new Vector2(0f, 0.12f), bestAlly.pos + new Vector2(0f, 0.12f),
                0.20f, 0.85f, new Color(0.55f, 1f, 0.48f, 0.85f), 0.25f, sortOrder: 2460);
        }

        void TriggerDachuiSecondaryShockwave(HeroUnit h)
        {
            if (h == null || !h.alive || h.dachuiBurstCooldown > 0f) return;
            h.dachuiBurstCooldown = 1.25f;

            float stunSec = 1.50f + 0.08f * Mathf.Max(0, h.skillLv3 - 1);
            float burstDmg = h.skillPower * (1.10f + 0.12f * Mathf.Max(0, h.skillLv3 - 1));
            for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
            {
                EnemyUnit z = _aliveEnemies[e];
                if (!IsEnemyTargetable(z)) continue;
                if (Vector2.Distance(h.pos, z.pos) <= 2.85f)
                {
                    z.stunTimer = Mathf.Max(z.stunTimer, stunSec);
                    DamageEnemy(z, burstDmg, h, isCrit: true, knockbackX: 0.30f, DamageType.Physical, allowLifesteal: false);
                }
            }

            TriggerMicroShake(0.16f, 0.06f);
            Vector2 ringPt = ClampFireAndAoePos(h.pos + new Vector2(0.18f, 0.12f));
            SpawnTransientFx(_spShockRing, ringPt, ringPt,
                0.35f, 2.55f, new Color(0.35f, 0.92f, 1f, 0.92f), 0.34f, sortOrder: 2560);
            SpawnTransientFx(_spShockRing, ringPt, ringPt,
                0.20f, 1.65f, new Color(1f, 0.88f, 0.36f, 0.88f), 0.26f, sortOrder: 2565);
        }

        void StepMeleeHero(HeroUnit h, EnemyUnit target, float dt)
        {
            float stopX = target.pos.x - 0.62f;
            Vector2 desiredSpot = ClampHeroPos(h, new Vector2(stopX, Mathf.Lerp(h.pos.y, target.pos.y, 0.85f)));
            float distToEnemy = Vector2.Distance(h.pos, target.pos);
            float meleeReach = target.kind == EnemyKind.GiantBoss ? 1.05f : 0.82f;
            float moveSpd = (h.roleId == 1 || h.heroId == "dachui") ? 2.82f : 2.62f;

            if (distToEnemy > meleeReach)
            {
                Vector2 prev = h.pos;
                Vector2 next = ClampHeroPos(h, Vector2.MoveTowards(h.pos, desiredSpot, moveSpd * dt));
                Vector2 delta = next - prev;
                h.pos = next;
                if (Mathf.Abs(delta.x) > 0.002f)
                    h.facingRight = delta.x >= 0f;
                else
                    h.facingRight = target.pos.x >= h.pos.x;
                h.puppet?.SetMode(delta.sqrMagnitude > 0.00001f ? HeroAnimMode.Run : HeroAnimMode.Idle);
            }
            else
            {
                h.facingRight = target.pos.x >= h.pos.x;
                if (h.atkCooldown <= 0f)
                {
                    bool isGuardian = h.roleId == 1 || h.heroId == "dachui";
                    h.atkCooldown = isGuardian ? 0.94f : 0.80f;
                    h.swingCount++;
                    h.lungeOffset = new Vector2(h.facingRight ? 0.14f : -0.14f, 0f);
                    h.puppet?.SetMode(HeroAnimMode.Chop);
                    AddHeroRage(h, 0.16f);

                    bool isCrit = RollCrit(h.critChance);
                    float dmg = h.atk * (isGuardian ? 1.05f : 1.12f) * (isCrit ? h.critMult : 1f);

                    if (isGuardian)
                    {
                        // 大锤：物理重锤猛击 + 每 2 击刷新一层小战地护盾 + 击退震慑
                        DamageEnemy(target, dmg, h, isCrit, knockbackX: 0.18f, DamageType.Physical);
                        if (h.swingCount % 2 == 0)
                        {
                            float sh = h.skillPower * 0.40f * (1f + h.healShieldBonus);
                            h.shield = Mathf.Min(h.maxHp * 1.15f, h.shield + sh);
                            h.totalHeal += sh;
                        }
                        Vector2 fxPt = ClampFireAndAoePos(target.pos + new Vector2(0f, 0.12f));
                        SpawnTransientFx(_spShockRing, fxPt, fxPt,
                            0.25f, 0.85f, new Color(0.45f, 0.82f, 1f, 0.75f), 0.22f, sortOrder: 2400);
                    }
                    else
                    {
                        // 雷猛：重斧顺劈；2★ 解锁【撕裂伤口】：每第 3 击宽幅重劈并附加持续物理流血！
                        bool wideBleedSwing = h.star >= 2 && (h.swingCount % 3 == 0);
                        float cleaveRadius = wideBleedSwing ? 1.48f : 0.92f;
                        float cleaveRatio = wideBleedSwing ? 0.82f : 0.55f;
                        float primaryDmg = wideBleedSwing ? dmg * 1.22f : dmg;

                        DamageEnemy(target, primaryDmg, h, isCrit, knockbackX: wideBleedSwing ? 0.20f : 0.12f, DamageType.Physical);
                        if (wideBleedSwing)
                            ApplyEnemyBleed(target, h);

                        for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                        {
                            EnemyUnit other = _aliveEnemies[e];
                            if (other == target || !IsEnemyTargetable(other)) continue;
                            if (Vector2.Distance(other.pos, target.pos) <= cleaveRadius)
                            {
                                DamageEnemy(other, primaryDmg * cleaveRatio, h, isCrit: false, knockbackX: wideBleedSwing ? 0.14f : 0.08f, DamageType.Physical);
                                if (wideBleedSwing)
                                    ApplyEnemyBleed(other, h);
                            }
                        }

                        Vector2 slashPos = ClampFireAndAoePos(h.pos + new Vector2(h.facingRight ? 0.42f : -0.42f, 0.34f));
                        Vector2 slashEnd = ClampFireAndAoePos(slashPos + new Vector2(h.facingRight ? (wideBleedSwing ? 0.36f : 0.22f) : (wideBleedSwing ? -0.36f : -0.22f), 0f));
                        Color slashCol = wideBleedSwing
                            ? new Color(1f, 0.42f, 0.32f, 0.94f)
                            : new Color(1f, 0.82f, 0.42f, 0.88f);
                        SpawnTransientFx(_spCleaveWave, slashPos, slashEnd,
                            wideBleedSwing ? 0.72f : 0.55f,
                            wideBleedSwing ? 1.42f : 1.05f,
                            slashCol, 0.22f, sortOrder: 2600);
                    }
                }
                else if (h.atkCooldown < 0.32f)
                {
                    h.puppet?.SetMode(HeroAnimMode.Idle);
                }
            }
        }

        static void ApplyEnemyBleed(EnemyUnit z, HeroUnit attacker)
        {
            if (z == null || !z.alive || attacker == null) return;
            float dps = attacker.atk * (0.34f + 0.05f * Mathf.Max(0, attacker.skillLv2 - 1));
            z.bleedDps = Mathf.Max(z.bleedDps, dps);
            z.bleedTimer = 3.0f;
            if (z.bleedTickTimer <= 0f)
                z.bleedTickTimer = 0.50f;
            z.bleedOwner = attacker;
        }

        void StepRangedHero(HeroUnit h, EnemyUnit target, float dt)
        {
            // 按真实射程 (range 2..5) 分级走位压制与风筝：
            // - range 2 (阿飒飞爪突袭): 游击距离 2.0f..2.8f，x <= +0.55f（拉拽目标至身前 +0.58f 时稳立近身重劈！）
            // - range 3 (铁叔霰弹压制): 推进至 2.4f..3.3f 中近距离，x <= +0.25f
            // - range 4 (小棠 / 青禾): 稳守 3.6f..4.35f 安全中后排，x <= -0.48f
            // - range 5 (林砚 / 周博士): 稳守 4.2f..5.2f 后排核心位，x <= -0.48f
            float minIdeal;
            float maxIdeal;
            float preferredStandDist;
            float maxAdvanceX;

            if (h.heroId == "asa" || h.range == 2)
            {
                minIdeal = 0.48f;
                maxIdeal = 2.80f;
                preferredStandDist = 2.35f;
                maxAdvanceX = 0.55f;
            }
            else if (h.range == 3)
            {
                minIdeal = 0.92f;
                maxIdeal = 3.30f;
                preferredStandDist = 2.65f;
                maxAdvanceX = 0.22f;
            }
            else if (h.range == 4)
            {
                minIdeal = 1.25f;
                maxIdeal = 4.35f;
                preferredStandDist = 3.80f;
                maxAdvanceX = -0.48f;
            }
            else
            {
                minIdeal = 1.35f;
                maxIdeal = 5.20f;
                preferredStandDist = 4.45f;
                maxAdvanceX = -0.48f;
            }

            EnemyUnit closestThreat = FindClosestEnemyTo(h.pos);
            float threatDist = closestThreat != null ? Vector2.Distance(h.pos, closestThreat.pos) : 99f;
            float targetDist = Vector2.Distance(h.pos, target.pos);

            bool needKiteBack = closestThreat != null && threatDist < minIdeal && h.pos.x > -2.92f && h.atkCooldown > 0.12f;
            bool needStepForward = targetDist > maxIdeal && h.pos.x < maxAdvanceX;

            if (needKiteBack)
            {
                Vector2 away = (h.pos - closestThreat.pos);
                if (away.sqrMagnitude < 0.001f) away = Vector2.left;
                away.x = Mathf.Min(-0.55f, away.x);
                Vector2 next = ClampHeroPos(h, h.pos + away.normalized * (1.72f * dt));
                h.pos = next;
                h.facingRight = target.pos.x >= h.pos.x;
                h.puppet?.SetMode(HeroAnimMode.Run);
            }
            else if (needStepForward)
            {
                Vector2 goal = ClampHeroPos(h, new Vector2(target.pos.x - preferredStandDist, Mathf.Lerp(h.pos.y, h.formPos.y, 0.5f)));
                Vector2 prev = h.pos;
                h.pos = ClampHeroPos(h, Vector2.MoveTowards(h.pos, goal, 2.05f * dt));
                h.facingRight = target.pos.x >= h.pos.x;
                h.puppet?.SetMode((h.pos - prev).sqrMagnitude > 0.00001f ? HeroAnimMode.Run : HeroAnimMode.Idle);
            }
            else
            {
                h.facingRight = target.pos.x >= h.pos.x;
                if (h.atkCooldown <= 0f)
                {
                    FireRangedBasicAttack(h, target);
                }
                else if (h.atkCooldown < 0.35f)
                {
                    h.puppet?.SetMode(HeroAnimMode.Idle);
                }
            }
        }

        void StepMedicHero(HeroUnit h, float dt)
        {
            EnemyUnit closestThreat = FindClosestEnemyTo(h.pos);
            if (closestThreat != null && Vector2.Distance(h.pos, closestThreat.pos) < 1.25f && h.pos.x > -2.92f && h.atkCooldown > 0.15f)
            {
                Vector2 kite = ClampHeroPos(h, h.pos + new Vector2(-1.65f * dt, (h.formPos.y - h.pos.y) * dt));
                h.pos = kite;
                h.facingRight = true;
                h.puppet?.SetMode(HeroAnimMode.Run);
                return;
            }

            // 寻找血量百分比最低的受伤友军
            HeroUnit hurtAlly = null;
            float lowestRatio = 0.97f;
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit ally = _heroes[i];
                if (!ally.alive) continue;
                float r = ally.hp / Mathf.Max(1f, ally.maxHp);
                if (r < lowestRatio)
                {
                    lowestRatio = r;
                    hurtAlly = ally;
                }
            }

            if (h.atkCooldown <= 0f)
            {
                h.atkCooldown = 0.92f;
                h.swingCount++;
                h.puppet?.SetMode(HeroAnimMode.Chop);
                AddHeroRage(h, 0.16f);

                if (hurtAlly != null)
                {
                    h.facingRight = hurtAlly.pos.x >= h.pos.x;
                    h.lungeOffset = new Vector2(h.facingRight ? 0.08f : -0.08f, 0f);
                    float healAmt = (h.skillPower * 0.85f + hurtAlly.maxHp * 0.035f) * (1f + h.healShieldBonus);
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.HealHerb,
                        dmgType = DamageType.Elemental,
                        sourceHero = h,
                        targetAlly = hurtAlly,
                        fromLocal = h.pos + new Vector2(h.facingRight ? 0.22f : -0.22f, 0.42f),
                        toLocal = hurtAlly.pos + new Vector2(0f, 0.38f),
                        arcHeight = 0.45f,
                        duration = Mathf.Clamp(Vector2.Distance(h.pos, hurtAlly.pos) / 7.5f, 0.16f, 0.36f),
                        amount = healAmt,
                    }, "ico_herb", new Color(0.35f, 1f, 0.58f, 1f), 0.52f);
                }
                else
                {
                    EnemyUnit foe = PickTargetForHero(h);
                    if (foe != null)
                    {
                        h.facingRight = foe.pos.x >= h.pos.x;
                        h.lungeOffset = new Vector2(h.facingRight ? 0.08f : -0.08f, 0f);
                        bool isCrit = RollCrit(h.critChance);
                        SpawnArenaProjectile(new ArenaProjectile
                        {
                            style = ProjectileStyle.Bolt,
                            dmgType = DamageType.Elemental,
                            sourceHero = h,
                            targetEnemy = foe,
                            fromLocal = h.pos + new Vector2(h.facingRight ? 0.24f : -0.24f, 0.40f),
                            toLocal = foe.pos + new Vector2(0f, 0.34f),
                            arcHeight = 0.18f,
                            duration = Mathf.Clamp(Vector2.Distance(h.pos, foe.pos) / 8.5f, 0.15f, 0.38f),
                            amount = h.atk * 0.90f * (isCrit ? h.critMult : 1f),
                            isCrit = isCrit,
                        }, "ico_herb", new Color(0.52f, 1f, 0.65f, 1f), 0.48f);
                    }
                }
            }
            else if (h.atkCooldown < 0.38f)
            {
                h.puppet?.SetMode(HeroAnimMode.Idle);
            }
        }

        void FireRangedBasicAttack(HeroUnit h, EnemyUnit target)
        {
            h.puppet?.SetMode(HeroAnimMode.Chop);
            h.swingCount++;
            Vector2 muzzle = h.pos + new Vector2(h.facingRight ? 0.28f : -0.28f, 0.40f);
            bool isCrit = RollCrit(h.critChance);

            if (h.heroId == "asa")
            {
                // 阿飒（射程 2 · 飞爪猎袭）：中近程链刃突刺劈砍（命中标记 1.2s，2★ 孤狼猎手击败回怒 +35！）
                h.atkCooldown = 0.74f;
                h.lungeOffset = new Vector2(h.facingRight ? 0.14f : -0.14f, 0f);
                AddHeroRage(h, 0.16f);

                Vector2 aimPt = new Vector2(Mathf.Min(2.20f, target.pos.x), target.pos.y + 0.34f);
                float dist = Vector2.Distance(muzzle, aimPt);
                if (dist <= 1.15f)
                {
                    DamageEnemy(target, h.atk * 1.10f * (isCrit ? h.critMult : 1f), h, isCrit, knockbackX: 0.10f, DamageType.Physical);
                    Vector2 slashPos = ClampFireAndAoePos(h.pos + new Vector2(h.facingRight ? 0.38f : -0.38f, 0.34f));
                    SpawnTransientFx(_spCleaveWave, slashPos, slashPos + new Vector2(h.facingRight ? 0.22f : -0.22f, 0f),
                        0.60f, 1.12f, new Color(1f, 0.36f, 0.28f, 0.92f), 0.18f, sortOrder: 2620);
                }
                else
                {
                    SpawnSegmentLineFx(_spChainSegment, muzzle, aimPt, new Color(0.86f, 0.92f, 0.98f, 0.82f), 0.55f, 0.14f, sortOrder: 2790);
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.GrappleClawBolt,
                        dmgType = DamageType.Physical,
                        sourceHero = h,
                        targetEnemy = target,
                        fromLocal = muzzle,
                        toLocal = aimPt,
                        dirNorm = (aimPt - muzzle).normalized,
                        arcHeight = 0f,
                        duration = Mathf.Clamp(dist / 13.5f, 0.08f, 0.20f),
                        amount = h.atk * 1.08f * (isCrit ? h.critMult : 1f),
                        knockback = 0.06f,
                        isCrit = isCrit,
                    }, "__hook_claw", new Color(1f, 0.44f, 0.32f, 1f), 0.72f);
                }
            }
            else if (h.heroId == "qinghe")
            {
                // 青禾（射程 4 · 寒霜猎手）：发射冰青色霜冻弩矢；2★ 解锁【霜冻迟缓】减速减攻速 25% 持续 2.8s！
                h.atkCooldown = 0.80f;
                h.lungeOffset = new Vector2(h.facingRight ? -0.05f : 0.05f, 0f);
                AddHeroRage(h, 0.16f);

                Vector2 aimPt = new Vector2(Mathf.Min(2.20f, target.pos.x), target.pos.y + 0.34f);
                Vector2 dir = (aimPt - muzzle);
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                dir.Normalize();

                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Bolt,
                    dmgType = DamageType.Elemental,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = muzzle,
                    toLocal = aimPt,
                    dirNorm = dir,
                    arcHeight = 0.06f,
                    duration = Mathf.Clamp(Vector2.Distance(muzzle, aimPt) / 10.6f, 0.13f, 0.36f),
                    amount = h.atk * 0.98f * h.elementalDmgMul * (isCrit ? h.critMult : 1f),
                    chillDuration = h.star >= 2 ? 2.8f : 0f,
                    isCrit = isCrit,
                }, "proj_arrow", new Color(0.45f, 0.92f, 1f, 1f), 0.80f);
            }
            else if (h.heroId == "zhoudr")
            {
                // 周博士（射程 5 · 电磁学者）：发射高速噼啪电弧光弹，造成元素伤害！
                h.atkCooldown = 0.78f;
                h.lungeOffset = new Vector2(h.facingRight ? -0.04f : 0.04f, 0f);
                AddHeroRage(h, 0.16f);

                Vector2 elecMuzzle = h.pos + new Vector2(h.facingRight ? 0.30f : -0.30f, 0.46f);
                Vector2 aimPt = new Vector2(Mathf.Min(2.20f, target.pos.x), target.pos.y + 0.36f);
                Vector2 dir = (aimPt - elecMuzzle);
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                dir.Normalize();

                SpawnTransientFx(_spShockRing, elecMuzzle, elecMuzzle,
                    0.14f, 0.46f, new Color(0.56f, 0.88f, 1f, 0.88f), 0.14f, sortOrder: 2660);

                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.ElectricBolt,
                    dmgType = DamageType.Elemental,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = elecMuzzle,
                    toLocal = aimPt,
                    dirNorm = dir,
                    arcHeight = 0f,
                    duration = Mathf.Clamp(Vector2.Distance(elecMuzzle, aimPt) / 13.8f, 0.09f, 0.26f),
                    amount = Mathf.Max(h.atk, h.atk * 0.65f + h.skillPower * 0.42f) * h.elementalDmgMul * (isCrit ? h.critMult : 1f),
                    isCrit = isCrit,
                }, "__lightning_seg", new Color(0.52f, 0.88f, 1f, 1f), 0.74f);
            }
            else if (h.heroId == "xiaotang" || h.roleId == 2)
            {
                // 小棠：高抛燃烧瓶（普攻伤害挂钩 atk，地面灼烧火海挂钩 skillPower，走元素抗性减伤；重甲尸克星！）
                h.atkCooldown = 1.08f;
                h.lungeOffset = new Vector2(h.facingRight ? 0.10f : -0.10f, 0.03f);
                AddHeroRage(h, 0.18f);
                Vector2 landTarget = ClampFireAndAoePos(target.pos);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.MolotovArc,
                    dmgType = DamageType.Elemental,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = muzzle,
                    toLocal = landTarget,
                    arcHeight = 1.10f,
                    duration = Mathf.Clamp(Vector2.Distance(h.pos, landTarget) / 6.2f, 0.28f, 0.52f),
                    amount = h.atk * 0.92f * h.elementalDmgMul * (isCrit ? h.critMult : 1f),
                    splashRadius = 0.88f,
                    firePoolDps = h.skillPower * 0.38f * h.elementalDmgMul,
                    firePoolLife = 2.9f,
                    isCrit = isCrit,
                }, "proj_wrench", new Color(1f, 0.64f, 0.22f, 1f), 0.68f);
            }
            else if (h.roleId == 4 || h.heroId == "tieshu")
            {
                // 铁叔：重型霰弹扇形喷射；2★ 解锁【压制装填】每第 4 次普攻触发双管连发速射 + 强力击退！
                h.atkCooldown = 0.96f;
                AddHeroRage(h, 0.17f);
                bool isDoubleBlast = h.star >= 2 && (h.swingCount % 4 == 0);
                FireTieShuBuckshotSpread(h, target, isDoubleBarrelBonus: isDoubleBlast);
                if (isDoubleBlast)
                {
                    h.doubleBarrelPending = 1;
                    h.doubleBarrelTimer = 0.14f;
                    h.doubleBarrelTarget = target;
                }
            }
            else
            {
                // 林砚：高速青金穿甲连弩；2★ 解锁【弱点洞悉】暴击弩箭直线穿透命中后方第 2 名丧尸！
                h.atkCooldown = 0.62f;
                h.lungeOffset = new Vector2(h.facingRight ? -0.05f : 0.05f, 0f);
                AddHeroRage(h, 0.14f);

                bool critPierce = isCrit && h.star >= 2;
                Vector2 aimPt = new Vector2(Mathf.Min(2.20f, target.pos.x), target.pos.y + 0.34f);
                Vector2 dir = (aimPt - muzzle);
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                dir.Normalize();

                if (critPierce)
                {
                    float rayLen = Mathf.Max(Vector2.Distance(muzzle, aimPt) + 1.45f, 3.2f);
                    aimPt = muzzle + dir * rayLen;
                    aimPt.x = Mathf.Clamp(aimPt.x, -2.90f, 2.24f);
                    aimPt.y = Mathf.Clamp(aimPt.y, -2.20f, 1.55f);
                }

                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Bolt,
                    dmgType = DamageType.Physical,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = muzzle,
                    toLocal = aimPt,
                    dirNorm = dir,
                    arcHeight = critPierce ? 0f : 0.08f,
                    duration = Mathf.Clamp(Vector2.Distance(muzzle, aimPt) / 11.2f, 0.12f, 0.34f),
                    amount = h.atk * 0.98f * (isCrit ? h.critMult : 1f),
                    isCrit = isCrit,
                    piercing = critPierce,
                    maxPierceCount = critPierce ? 2 : 0,
                }, "proj_arrow", critPierce ? new Color(1f, 0.94f, 0.42f, 1f) : new Color(0.55f, 0.98f, 1f, 1f), critPierce ? 0.88f : 0.76f);
            }
        }

        void FireTieShuBuckshotSpread(HeroUnit h, EnemyUnit target, bool isDoubleBarrelBonus)
        {
            Vector2 muzzle = h.pos + new Vector2(h.facingRight ? 0.28f : -0.28f, 0.40f);
            h.lungeOffset = new Vector2(h.facingRight ? (isDoubleBarrelBonus ? -0.14f : -0.09f) : (isDoubleBarrelBonus ? 0.14f : 0.09f), 0f);

            Vector2 baseDir = (target.pos - h.pos);
            if (baseDir.sqrMagnitude < 0.01f) baseDir = Vector2.right;
            float dist = Mathf.Clamp(baseDir.magnitude, 1.2f, 3.65f);
            baseDir.Normalize();

            bool isCrit = RollCrit(h.critChance);
            float kb = isDoubleBarrelBonus ? 0.42f : 0.24f;
            float dmgMul = isDoubleBarrelBonus ? 0.56f : 0.48f;

            float[] spreadAngles = { -9f, 0f, 9f };
            for (int p = 0; p < spreadAngles.Length; p++)
            {
                Vector2 d = Rotate2D(baseDir, spreadAngles[p]);
                Vector2 endPt = h.pos + d * dist + new Vector2(0f, 0.30f);
                endPt.x = Mathf.Clamp(endPt.x, -2.95f, 2.25f);
                bool pelletCrit = isCrit && (p == 1 || isDoubleBarrelBonus);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Buckshot,
                    dmgType = DamageType.Physical,
                    sourceHero = h,
                    targetEnemy = p == 1 ? target : null,
                    fromLocal = muzzle,
                    toLocal = endPt,
                    dirNorm = d,
                    arcHeight = 0f,
                    duration = Mathf.Clamp(dist / 11.8f, 0.11f, 0.26f),
                    amount = h.atk * dmgMul * (pelletCrit ? h.critMult : 1f),
                    knockback = kb,
                    isCrit = pelletCrit,
                }, "proj_buckshot", isDoubleBarrelBonus ? new Color(1f, 0.65f, 0.24f, 1f) : new Color(1f, 0.88f, 0.42f, 1f), isDoubleBarrelBonus ? 0.76f : 0.66f);
            }

            SpawnTransientFx(_spShockRing, muzzle, muzzle + baseDir * 0.30f,
                0.22f, isDoubleBarrelBonus ? 0.78f : 0.58f,
                new Color(1f, 0.82f, 0.32f, 0.88f), 0.15f, sortOrder: 2650);
        }

        void ExecuteHeroUltimate(HeroUnit h)
        {
            h.rage01 = 0f;
            h.puppet?.SetMode(HeroAnimMode.Chop);
            TriggerMicroShake(0.18f, 0.07f);

            // 全部主动绝技、火海、治疗与护盾均按 skillPower * activeSkillMult 核心公式结算！
            float ultPower = h.skillPower * h.activeSkillMult;

            // 脚下金色大招爆发波纹
            SpawnTransientFx(_spShockRing, h.pos + new Vector2(0f, 0.08f), h.pos + new Vector2(0f, 0.08f),
                0.35f, 1.45f, new Color(1f, 0.84f, 0.24f, 0.90f), 0.28f, sortOrder: 2350);

            if (h.heroId == "asa")
            {
                // 阿飒绝技【飞爪夺命钩】：锁定敌方最远右侧后排活体目标，发射可见精钢飞爪铁链将其强行拉至阿飒身前 (pos.x + 0.58f) 重劈！
                // 3★ 觉醒【夺命钩增强】：被钩目标破甲 -35% 持续 5s，并眩晕 2.2s！
                EnemyUnit hookTarget = PickFurthestRightEnemyForHook(h);
                if (hookTarget != null)
                {
                    h.facingRight = true;
                    h.lungeOffset = new Vector2(0.16f, 0f);
                    Vector2 grabOrigin = hookTarget.pos;

                    // 若抓中正在蓄力预警起跳的跳尸，直接打断其跳跃并拽回身前处决！
                    if (hookTarget.kind == EnemyKind.BacklineJumper && hookTarget.jumpPhase < 3)
                    {
                        hookTarget.jumpPhase = 3;
                        if (hookTarget.warningRingTr != null)
                        {
                            DestroyObj(hookTarget.warningRingTr.gameObject);
                            hookTarget.warningRingTr = null;
                            hookTarget.warningRingSr = null;
                        }
                    }

                    Vector2 pulledPos = ClampEnemyUnitPos(hookTarget, new Vector2(
                        h.pos.x + 0.58f,
                        Mathf.Clamp(Mathf.Lerp(h.pos.y, hookTarget.pos.y, 0.18f), -2.15f, 1.45f)));

                    hookTarget.pos = pulledPos;
                    hookTarget.knockbackVel = Vector2.zero;
                    hookTarget.grappleVisualFrom = grabOrigin;
                    hookTarget.grappleVisualTimer = 0.16f;
                    hookTarget.grappleVisualDur = 0.16f;

                    SpawnGrappleChainFx(h, hookTarget, grabOrigin, duration: 0.40f);
                    SpawnWorldFloatingText(pulledPos + new Vector2(0.36f, 1.16f), "飞爪拉拽!", new Color(1f, 0.50f, 0.26f, 1f), 1.08f);

                    if (h.star >= 3)
                    {
                        hookTarget.asaArmorShredTimer = Mathf.Max(hookTarget.asaArmorShredTimer, 5.0f);
                        hookTarget.stunTimer = Mathf.Max(hookTarget.stunTimer, 2.2f);
                    }
                    else
                    {
                        hookTarget.stunTimer = Mathf.Max(hookTarget.stunTimer, 0.45f);
                    }

                    DamageEnemy(hookTarget, ultPower, h, isCrit: true, knockbackX: 0f, DamageType.Physical);

                    Vector2 slamFx = ClampFireAndAoePos(pulledPos + new Vector2(0f, 0.18f));
                    SpawnTransientFx(_spShockRing, slamFx, slamFx,
                        0.30f, 1.65f, new Color(1f, 0.36f, 0.24f, 0.92f), 0.30f, sortOrder: 2620);
                    SpawnTransientFx(_spCleaveWave, h.pos + new Vector2(0.38f, 0.36f), pulledPos + new Vector2(0.18f, 0.36f),
                        0.78f, 1.48f, new Color(1f, 0.42f, 0.30f, 0.95f), 0.24f, sortOrder: 2650);
                }
            }
            else if (h.heroId == "qinghe")
            {
                // 青禾绝技【霜寒捕兽夹阵】：向尸群最密集处高抛液氮寒霜瓶，落地铺设 2.5s 冰霜法阵，造成元素伤害并冰封半径 1.65f 内全部敌人 2.0s！
                Vector2 clusterCenter = FindDensestEnemyClusterPos(1.65f);
                h.facingRight = clusterCenter.x >= h.pos.x;
                h.lungeOffset = new Vector2(h.facingRight ? 0.10f : -0.10f, 0.03f);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.CryoFlaskArc,
                    dmgType = DamageType.Elemental,
                    sourceHero = h,
                    fromLocal = h.pos + new Vector2(h.facingRight ? 0.26f : -0.26f, 0.46f),
                    toLocal = clusterCenter,
                    arcHeight = 1.18f,
                    duration = 0.22f,
                    amount = ultPower,
                    splashRadius = 1.65f,
                    firePoolLife = 2.5f,
                    isCrit = true,
                }, "ico_water", new Color(0.45f, 0.95f, 1f, 1f), 0.78f);
            }
            else if (h.heroId == "zhoudr")
            {
                // 周博士绝技【超载电磁风暴】：召唤青紫高压闪电链在活体敌群间弹射最多 8 次（3★ 同目标递增 +15%，击杀额外 +2 次最多 +6 次）！
                h.facingRight = true;
                h.lungeOffset = new Vector2(-0.06f, 0.03f);
                Vector2 staffTip = h.pos + new Vector2(0.30f, 0.50f);
                SpawnWorldFloatingText(h.pos + new Vector2(0f, 1.06f), "超载电弧!", new Color(0.56f, 0.88f, 1f, 1f), 1.06f);
                SpawnTransientFx(_spShockRing, staffTip, staffTip,
                    0.25f, 1.35f, new Color(0.58f, 0.46f, 1f, 0.92f), 0.28f, sortOrder: 2660);

                var storm = new ActiveChainStorm
                {
                    caster = h,
                    baseBounceDmg = ultPower,
                    bouncesDone = 0,
                    maxBounces = 8,
                    extraBouncesGranted = 0,
                    timer = 0.055f,
                    lastHitEnemy = null,
                    lastAnchorPos = staffTip,
                };
                if (ExecuteNextChainBounce(storm) && storm.bouncesDone < storm.maxBounces && !IsResultPending)
                    _chainStorms.Add(storm);
            }
            else if (h.heroId == "dachui" || h.roleId == 1)
            {
                // 大锤绝技【路牌重盾震击】：展开 skillPower * activeSkillMult * (1 + healShieldBonus) 巨盾 + 强嘲讽 + 范围物理震击！
                float selfSh = ultPower * (1f + h.healShieldBonus);
                h.shield = Mathf.Min(h.maxHp * 1.35f, h.shield + selfSh);
                h.dachuiUltShieldActive = true;
                h.dachuiShieldTimer = 5.5f;
                h.dachuiTauntTimer = 5.5f;
                h.totalHeal += selfSh;
                SpawnWorldNumber(h.pos + new Vector2(0f, 0.95f), Mathf.Max(1, Mathf.RoundToInt(selfSh)), isHeal: true, isCrit: false);

                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive || ally == h) continue;
                    float aSh = selfSh * 0.26f;
                    ally.shield = Mathf.Min(ally.maxHp * 0.85f, ally.shield + aSh);
                    h.totalHeal += aSh;
                }

                float slamDmg = ultPower * 0.88f;
                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(h.pos, z.pos) <= 2.65f)
                    {
                        z.stunTimer = Mathf.Max(z.stunTimer, 1.25f);
                        DamageEnemy(z, slamDmg, h, isCrit: true, knockbackX: 0.32f, DamageType.Physical);
                    }
                }
                Vector2 ringCenter = ClampFireAndAoePos(h.pos + new Vector2(0.25f, 0.12f));
                SpawnTransientFx(_spShockRing, ringCenter, ringCenter,
                    0.45f, 2.35f, new Color(0.38f, 0.84f, 1f, 0.88f), 0.34f, sortOrder: 2500);
            }
            else if (h.roleId == 2 || h.heroId == "xiaotang")
            {
                // 小棠绝技【高爆燃烧瓶】：三连高爆燃烧瓶铺设 3.5s 烈焰火海（元素伤害，2★减双防 20%，3★结束二次爆轰 1.2*skillPower）！
                var targetables = new List<EnemyUnit>(8);
                for (int e = 0; e < _aliveEnemies.Count; e++)
                    if (IsEnemyTargetable(_aliveEnemies[e])) targetables.Add(_aliveEnemies[e]);

                int count = 3;
                float impactPerBottle = ultPower * 0.45f * h.elementalDmgMul;
                float poolDpsPerBottle = ultPower * 0.26f * h.elementalDmgMul;
                for (int k = 0; k < count; k++)
                {
                    Vector2 basePt = targetables.Count > 0
                        ? targetables[(k * 2) % targetables.Count].pos
                        : new Vector2(0.85f, RowLocalY[k % 3]);
                    Vector2 land = ClampFireAndAoePos(basePt + new Vector2((k - 1) * 0.38f, (k % 2 == 0 ? 0.12f : -0.12f)));
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.MolotovArc,
                        dmgType = DamageType.Elemental,
                        sourceHero = h,
                        fromLocal = h.pos + new Vector2(0.25f, 0.48f),
                        toLocal = land,
                        arcHeight = 1.45f + k * 0.22f,
                        duration = 0.34f + k * 0.07f,
                        amount = impactPerBottle,
                        splashRadius = 1.20f,
                        firePoolDps = poolDpsPerBottle,
                        firePoolLife = 3.5f,
                        isCrit = true,
                    }, "proj_wrench", new Color(1f, 0.48f, 0.14f, 1f), 0.82f);
                }
            }
            else if (h.roleId == 3 || h.heroId == "linyan")
            {
                // 林砚绝技【贯日散射箭雨】：3★ 觉醒每次释放绝技永久叠加 +5% 暴击率与 +6% 攻击力！
                if (h.star >= 3)
                {
                    h.linyanUltStacks++;
                    h.critChance = Mathf.Min(0.95f, h.critChance + 0.05f);
                    h.atk += h.baseAtk * 0.06f;
                    SpawnTransientFx(_spShockRing, h.pos + new Vector2(0f, 0.16f), h.pos + new Vector2(0f, 0.16f),
                        0.22f, 1.15f, new Color(0.40f, 1f, 0.90f, 0.92f), 0.26f, sortOrder: 2480);
                }

                float perWaveArrowDmg = ultPower * 0.44f;
                for (int k = 0; k < 7; k++)
                {
                    float laneY = Mathf.Lerp(-2.05f, 1.35f, k / 6f);
                    Vector2 start = new Vector2(h.pos.x + 0.35f, laneY + 1.35f);
                    Vector2 end = new Vector2(2.15f, laneY);
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.RainArrow,
                        dmgType = DamageType.Physical,
                        sourceHero = h,
                        fromLocal = start,
                        toLocal = end,
                        dirNorm = (end - start).normalized,
                        arcHeight = 0f,
                        duration = 0.32f + (k % 3) * 0.04f,
                        amount = perWaveArrowDmg,
                        piercing = true,
                        maxPierceCount = 0,
                        isCrit = true,
                    }, "proj_arrow", new Color(0.42f, 1f, 0.92f, 1f), 0.86f);
                }
            }
            else if (h.roleId == 4 || h.heroId == "tieshu")
            {
                // 铁叔绝技【双管高爆霰弹】：4 轮扇形高爆铁砂弹幕倾泻（挂钩 skillPower * activeSkillMult）！
                FireGunsmithSalvoBurst(h);
                h.gunSalvoRemaining = 3;
                h.gunSalvoTimer = 0.10f;
            }
            else if (h.roleId == 5 || h.heroId == "laochentou")
            {
                // 老陈头绝技【战地急救热汤】：按 skillPower * activeSkillMult 恢复全队生命并施加草本护盾；
                // 3★ 觉醒：同时为全体存活友军恢复 +20 怒气 (+0.20f rage01) 并提升 +15% 元素抗性！
                HeroUnit lowestAlly = null;
                float lowestRatio = 2f;
                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive) continue;
                    float ratio = ally.hp / Mathf.Max(1f, ally.maxHp);
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        lowestAlly = ally;
                    }
                }

                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive) continue;
                    float heal = (ultPower * 0.92f + ally.maxHp * 0.08f) * (1f + h.healShieldBonus);
                    float shieldAdd = (ultPower * 0.42f + ally.maxHp * 0.06f) * (1f + h.healShieldBonus);
                    if (h.hasMedicCrossBadge && ally == lowestAlly)
                        shieldAdd += ally.maxHp * 0.12f * (1f + h.healShieldBonus);

                    float actualHeal = Mathf.Min(ally.maxHp - ally.hp, heal);
                    ally.hp = Mathf.Min(ally.maxHp, ally.hp + heal);
                    ally.shield = Mathf.Min(ally.maxHp * 0.95f, ally.shield + shieldAdd);
                    ally.teamSpdBuffTimer = 4.5f;

                    if (h.star >= 3)
                    {
                        ally.rage01 = Mathf.Clamp01(ally.rage01 + 0.20f);
                        ally.soupResistBonusMul = 0.15f;
                        ally.soupResistTimer = 8.5f;
                    }

                    float credit = actualHeal + shieldAdd * 0.85f + heal * 0.25f;
                    h.totalHeal += credit;
                    SpawnWorldNumber(ally.pos + new Vector2(0f, 0.88f), Mathf.Max(1, Mathf.RoundToInt(heal)), isHeal: true, isCrit: true);
                    SpawnTransientFx(_spShockRing, ally.pos + new Vector2(0f, 0.10f), ally.pos + new Vector2(0f, 0.10f),
                        0.22f, 0.95f, new Color(0.32f, 1f, 0.56f, 0.85f), 0.30f, sortOrder: 2450);
                }
                SpawnTransientFx(_spShockRing, new Vector2(-1.6f, -0.35f), new Vector2(-1.6f, -0.35f),
                    0.6f, 3.2f, new Color(0.36f, 1f, 0.62f, 0.72f), 0.38f, sortOrder: 2300);
            }
            else
            {
                // 雷猛绝技【狂风破障斩】：按 skillPower * activeSkillMult 造成大范围物理横扫；
                // 3★ 觉醒：每命中 1 名敌人恢复自身 8% 已损生命，并叠加 +6% 攻击速度（最多 5 层）！
                h.lungeOffset = new Vector2(0.20f, 0f);
                int hitCount = 0;
                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(h.pos, z.pos) <= 2.75f || (z.pos.x >= h.pos.x - 0.5f && z.pos.x <= 2.25f))
                    {
                        hitCount++;
                        DamageEnemy(z, ultPower, h, isCrit: true, knockbackX: 0.36f, DamageType.Physical);
                    }
                }

                if (h.star >= 3 && hitCount > 0 && h.alive)
                {
                    float totalRecovered = 0f;
                    for (int c = 0; c < hitCount; c++)
                    {
                        float missing = Mathf.Max(0f, h.maxHp - h.hp);
                        float heal = missing * 0.08f * (1f + h.healShieldBonus);
                        h.hp = Mathf.Min(h.maxHp, h.hp + heal);
                        totalRecovered += heal;
                        h.leimengUltSpdStacks = Mathf.Min(5, h.leimengUltSpdStacks + 1);
                    }
                    h.totalHeal += totalRecovered;
                    if (totalRecovered >= 1f)
                        SpawnWorldNumber(h.pos + new Vector2(0f, 0.90f), Mathf.RoundToInt(totalRecovered), isHeal: true, isCrit: false);
                }

                Vector2 waveStart = ClampFireAndAoePos(h.pos + new Vector2(0.40f, 0.36f));
                Vector2 waveEnd = ClampFireAndAoePos(waveStart + new Vector2(1.15f, 0f));
                SpawnTransientFx(_spCleaveWave, waveStart, waveEnd,
                    0.95f, 1.75f, new Color(1f, 0.84f, 0.32f, 0.95f), 0.28f, sortOrder: 2680);
            }
        }

        void FireGunsmithSalvoBurst(HeroUnit h)
        {
            if (!h.alive || !HasAnyTargetableEnemy()) return;
            EnemyUnit target = PickTargetForHero(h);
            Vector2 baseDir = target != null ? (target.pos - h.pos) : Vector2.right;
            if (baseDir.sqrMagnitude < 0.01f) baseDir = Vector2.right;
            baseDir.Normalize();
            Vector2 muzzle = h.pos + new Vector2(h.facingRight ? 0.30f : -0.30f, 0.40f);
            h.lungeOffset = new Vector2(h.facingRight ? -0.07f : 0.07f, 0f);

            float pelletUltDmg = h.skillPower * h.activeSkillMult * 0.26f;
            float[] fan = { -15f, -5f, 5f, 15f };
            for (int k = 0; k < fan.Length; k++)
            {
                Vector2 d = Rotate2D(baseDir, fan[k]);
                Vector2 end = h.pos + d * 3.4f + new Vector2(0f, 0.30f);
                end.x = Mathf.Clamp(end.x, -2.95f, 2.25f);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Buckshot,
                    dmgType = DamageType.Physical,
                    sourceHero = h,
                    fromLocal = muzzle,
                    toLocal = end,
                    dirNorm = d,
                    duration = 0.22f,
                    amount = pelletUltDmg,
                    knockback = 0.34f,
                    piercing = true,
                    maxPierceCount = 0,
                    isCrit = true,
                }, "proj_buckshot", new Color(1f, 0.78f, 0.25f, 1f), 0.74f);
            }
        }

        EnemyUnit PickFurthestRightEnemyForHook(HeroUnit asa)
        {
            EnemyUnit best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!IsEnemyTargetable(z)) continue;
                float laneTie = -Mathf.Abs(z.pos.y - asa.pos.y) * 0.08f;
                float score = z.pos.x + laneTie;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = z;
                }
            }
            if (best != null) return best;

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (z == null || !z.alive || z.jumpPhase == 2) continue;
                if (best == null || z.pos.x > best.pos.x)
                    best = z;
            }
            return best;
        }

        Vector2 FindDensestEnemyClusterPos(float clusterRadius)
        {
            EnemyUnit bestCenter = null;
            float bestWeight = -1f;
            Vector2 bestCentroid = new Vector2(0.85f, -0.35f);

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit c = _aliveEnemies[i];
                if (!IsEnemyTargetable(c)) continue;

                float weight = 0f;
                Vector2 sumPos = Vector2.zero;
                int count = 0;
                for (int j = 0; j < _aliveEnemies.Count; j++)
                {
                    EnemyUnit o = _aliveEnemies[j];
                    if (!IsEnemyTargetable(o)) continue;
                    if (Vector2.Distance(c.pos, o.pos) <= clusterRadius)
                    {
                        float w = o.kind == EnemyKind.GiantBoss ? 1.6f : 1.0f;
                        weight += w;
                        sumPos += o.pos;
                        count++;
                    }
                }

                if (weight > bestWeight && count > 0)
                {
                    bestWeight = weight;
                    bestCenter = c;
                    bestCentroid = sumPos / count;
                }
            }

            if (bestCenter == null)
                return new Vector2(0.85f, -0.35f);

            return ClampFireAndAoePos(Vector2.Lerp(bestCenter.pos, bestCentroid, 0.55f));
        }

        void StepChainStorms(float dt)
        {
            for (int i = _chainStorms.Count - 1; i >= 0; i--)
            {
                ActiveChainStorm st = _chainStorms[i];
                st.timer -= dt;
                bool keep = true;

                while (st.timer <= 0f && st.bouncesDone < st.maxBounces)
                {
                    if (!ExecuteNextChainBounce(st) || IsResultPending)
                    {
                        keep = false;
                        break;
                    }
                    st.timer += 0.055f;
                }

                if (!keep || st.bouncesDone >= st.maxBounces)
                    _chainStorms.RemoveAt(i);
            }
        }

        bool ExecuteNextChainBounce(ActiveChainStorm st)
        {
            if (st == null || st.bouncesDone >= st.maxBounces) return false;

            EnemyUnit next = PickNextChainLightningTarget(st.lastAnchorPos, st.lastHitEnemy);
            if (next == null) return false;

            st.hitCounts.TryGetValue(next, out int priorHits);
            st.hitCounts[next] = priorHits + 1;

            // 周博士 3★ 觉醒【电磁风暴增强】：同一次绝技内重复弹射命中同一敌人，每次递增 +15% 伤害 (1.0x, 1.15x, 1.30x...)！
            bool is3Star = st.caster != null && st.caster.star >= 3;
            float repeatMul = is3Star ? (1f + 0.15f * priorHits) : 1f;
            float bounceDmg = st.baseBounceDmg * repeatMul;

            Vector2 toChest = next.pos + new Vector2(0f, next.kind == EnemyKind.GiantBoss ? 0.56f : 0.36f);
            Vector2 fromPt = st.lastAnchorPos;
            if (next == st.lastHitEnemy)
                fromPt = toChest + new Vector2(st.bouncesDone % 2 == 0 ? -0.52f : 0.52f, 1.38f);

            SpawnChainLightningArcFx(fromPt, toChest);
            SpawnTransientFx(_spShockRing, toChest, toChest,
                0.18f, 0.82f, new Color(0.56f, 0.86f, 1f, 0.90f), 0.20f, sortOrder: 2665);

            // 0.30s 高压麻痹微打断！
            next.stunTimer = Mathf.Max(next.stunTimer, 0.30f);
            DamageEnemy(next, bounceDmg, st.caster, isCrit: true, knockbackX: 0.05f, DamageType.Elemental, allowLifesteal: false);

            // 周博士 3★ 觉醒：任意一次电弧弹射击败目标，立即追加 +2 次弹射（单次绝技最多追加 +6 次）！
            if (!next.alive && is3Star && st.extraBouncesGranted < 6)
            {
                int add = Mathf.Min(2, 6 - st.extraBouncesGranted);
                st.extraBouncesGranted += add;
                st.maxBounces += add;
            }

            st.lastHitEnemy = next;
            st.lastAnchorPos = toChest;
            st.bouncesDone++;
            return true;
        }

        EnemyUnit PickNextChainLightningTarget(Vector2 fromPt, EnemyUnit lastHit)
        {
            EnemyUnit bestOther = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!IsEnemyTargetable(z) || z == lastHit) continue;
                float d = Vector2.Distance(fromPt, z.pos);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestOther = z;
                }
            }
            if (bestOther != null) return bestOther;

            if (IsEnemyTargetable(lastHit))
                return lastHit;

            return null;
        }

        // =====================================================================
        // 丧尸军团波次生成与战术索敌（含双防成长、8关+变异酸液元素尸、跳尸 & 巨型首领）
        // =====================================================================
        void BuildDeterministicStageWaves(int stage, int rawTotalCount, int recPower, bool isBoss, bool hasFlanker)
        {
            _spawnQueue.Clear();
            int total = Mathf.Clamp(rawTotalCount > 0 ? rawTotalCount : (6 + stage / 2), 5, 34);
            EnemiesTotal = total;

            // 锚定六维战力尺度的整关敌人总血量与攻击力（计入护甲/抗性真实减伤，确保战力达标稳定通关！）
            float hpScale = isBoss ? 2.15f : (stage >= 4 ? 1.58f : 1.32f);
            float totalHpBudget = Mathf.Max(360f, recPower * hpScale);
            float baseUnitAtk = Mathf.Max(11f, 8f + recPower * 0.0138f);

            int flankerCount = (hasFlanker || (isBoss && stage >= 5)) ? (stage >= 14 ? 2 : 1) : 0;
            int bossCount = isBoss ? 1 : 0;
            int normalCount = Mathf.Max(2, total - flankerCount - bossCount);

            var rawKinds = new List<EnemyKind>(total);
            for (int i = 0; i < normalCount; i++)
            {
                if (stage >= 3 && i % 4 == 2)
                    rawKinds.Add(EnemyKind.HardhatArmored);
                else if (stage >= 2 && i % 3 == 1)
                    rawKinds.Add(EnemyKind.FastRunner);
                else
                    rawKinds.Add(EnemyKind.Shambler);
            }
            for (int i = 0; i < flankerCount; i++)
                rawKinds.Add(EnemyKind.BacklineJumper);
            if (isBoss)
                rawKinds.Add(EnemyKind.GiantBoss);

            // 计算权重总和以精确分配总血量池
            float weightSum = 0f;
            for (int i = 0; i < rawKinds.Count; i++)
                weightSum += GetKindHpWeight(rawKinds[i]);
            float hpPerWeight = totalHpBudget / Mathf.Max(1f, weightSum);

            int w0End = Mathf.Max(2, Mathf.RoundToInt(rawKinds.Count * 0.44f));
            int w1End = Mathf.Max(w0End + 1, Mathf.RoundToInt(rawKinds.Count * 0.78f));

            _queuedHpSum = 0f;
            for (int i = 0; i < rawKinds.Count; i++)
            {
                EnemyKind kind = rawKinds[i];
                int subWave;
                float spawnT;

                if (kind == EnemyKind.GiantBoss)
                {
                    subWave = 1;
                    spawnT = 1.45f;
                }
                else if (kind == EnemyKind.BacklineJumper)
                {
                    subWave = 1;
                    spawnT = 2.8f + (i % 2) * 0.9f;
                }
                else if (i < w0End)
                {
                    subWave = 0;
                    spawnT = 0.04f * i;
                }
                else if (i < w1End)
                {
                    subWave = 1;
                    spawnT = 3.2f + (i - w0End) * 0.24f;
                }
                else
                {
                    subWave = 2;
                    spawnT = 6.2f + (i - w1End) * 0.24f;
                }

                float hp = Mathf.Max(35f, Mathf.Round(hpPerWeight * GetKindHpWeight(kind)));
                float atk = baseUnitAtk * GetKindAtkWeight(kind);
                float spd = GetKindSpeed(kind);
                GetKindDefenses(kind, stage, out float zArmor, out float zResist);

                bool isElemMutant = (kind == EnemyKind.GiantBoss && stage >= 8)
                    || (kind == EnemyKind.Shambler && stage >= 8 && (i % 2 == 0))
                    || (kind == EnemyKind.BacklineJumper && stage >= 10);

                _spawnQueue.Add(new QueuedEnemySpec
                {
                    kind = kind,
                    subWave = subWave,
                    spawnTime = spawnT,
                    laneRow = kind == EnemyKind.GiantBoss ? 1 : (i % 3),
                    maxHp = hp,
                    atk = atk,
                    speed = spd,
                    armor = zArmor,
                    resistance = zResist,
                    isElementalMutant = isElemMutant,
                });
                _queuedHpSum += hp;
            }

            _spawnQueue.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
            EnemyTotalMaxHp = Mathf.Max(1f, _queuedHpSum);
            EnemyTotalHp = _queuedHpSum;
        }

        static void GetKindDefenses(EnemyKind k, int stage, out float armor, out float resistance)
        {
            switch (k)
            {
                case EnemyKind.HardhatArmored:
                    // 安全帽重甲尸：超高物理护甲 (140+)，但元素抗性偏低（被小棠高爆火海完美克制！）
                    armor = 140f + stage * 9.5f;
                    resistance = 20f + stage * 3.2f;
                    break;
                case EnemyKind.FastRunner:
                    armor = 16f + stage * 3.5f;
                    resistance = 16f + stage * 3.5f;
                    break;
                case EnemyKind.BacklineJumper:
                    armor = 26f + stage * 4.8f;
                    resistance = 30f + stage * 4.8f;
                    break;
                case EnemyKind.GiantBoss:
                    armor = 85f + stage * 8.0f;
                    resistance = 72f + stage * 7.2f;
                    break;
                default:
                    armor = 24f + stage * 4.5f;
                    resistance = 22f + stage * 4.2f;
                    break;
            }
        }

        static float GetKindHpWeight(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.HardhatArmored: return 1.55f;
                case EnemyKind.FastRunner: return 0.74f;
                case EnemyKind.BacklineJumper: return 0.95f;
                case EnemyKind.GiantBoss: return 4.50f;
                default: return 1.00f;
            }
        }

        static float GetKindAtkWeight(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.HardhatArmored: return 1.05f;
                case EnemyKind.FastRunner: return 0.92f;
                case EnemyKind.BacklineJumper: return 1.12f;
                case EnemyKind.GiantBoss: return 2.15f;
                default: return 1.00f;
            }
        }

        static float GetKindSpeed(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.HardhatArmored: return 0.84f;
                case EnemyKind.FastRunner: return 1.82f;
                case EnemyKind.BacklineJumper: return 1.55f;
                case EnemyKind.GiantBoss: return 0.78f;
                default: return 1.08f;
            }
        }

        void FlushDueEnemySpawns(bool forceInitialWave)
        {
            if (_spawnQueue.Count == 0) return;

            // 若场上已杀空，提前释放下一子波，绝不让玩家干等空气！
            if (!forceInitialWave && _aliveEnemies.Count == 0 && ElapsedBattleSec >= ClashIntroSec)
            {
                float nextWaveT = _spawnQueue[0].spawnTime;
                if (nextWaveT > ElapsedBattleSec)
                {
                    float shift = nextWaveT - ElapsedBattleSec;
                    for (int i = 0; i < _spawnQueue.Count; i++)
                    {
                        QueuedEnemySpec q = _spawnQueue[i];
                        q.spawnTime = Mathf.Max(ElapsedBattleSec, q.spawnTime - shift);
                        _spawnQueue[i] = q;
                    }
                }
            }

            while (_spawnQueue.Count > 0)
            {
                QueuedEnemySpec next = _spawnQueue[0];
                bool due = forceInitialWave ? (next.subWave == 0) : (ElapsedBattleSec >= next.spawnTime);
                if (!due) break;

                _spawnQueue.RemoveAt(0);
                _queuedHpSum = Mathf.Max(0f, _queuedHpSum - next.maxHp);
                SpawnEnemyInstance(next);
            }
        }

        static float ResolveEnemyVisualScale(EnemyKind kind, ForestSpriteFlipbook fb)
        {
            bool hasStrip = fb != null && fb.IsLoaded;
            switch (kind)
            {
                case EnemyKind.GiantBoss:
                    return hasStrip ? 1.78f : 1.32f;
                case EnemyKind.HardhatArmored:
                    return hasStrip ? 1.14f : 1.10f;
                case EnemyKind.BacklineJumper:
                    return hasStrip ? 1.04f : 0.94f;
                case EnemyKind.FastRunner:
                    return hasStrip ? 1.02f : 0.92f;
                default:
                    return hasStrip ? 1.06f : 1.04f;
            }
        }

        void SpawnEnemyInstance(QueuedEnemySpec q)
        {
            bool isBoss = q.kind == EnemyKind.GiantBoss;
            float jitterY = ((float)_battleRng.NextDouble() - 0.5f) * 0.36f;
            float inwardOffset = (float)_battleRng.NextDouble() * 0.10f;
            float startX = isBoss ? 2.95f : (3.05f - inwardOffset);
            float startY = Mathf.Clamp(RowLocalY[Mathf.Clamp(q.laneRow, 0, 2)] + jitterY, -2.15f, 1.48f);

            string fallbackKey;
            string stripKey;
            float stripFps;
            float stripPpu = 256f;

            switch (q.kind)
            {
                case EnemyKind.GiantBoss:
                    fallbackKey = "zombie_boss_brute";
                    stripKey = "anim_zom_boss";
                    stripFps = 5.8f;
                    stripPpu = 384f;
                    break;
                case EnemyKind.FastRunner:
                    fallbackKey = "zombie_runner";
                    stripKey = "anim_zom_runner";
                    stripFps = 9.5f;
                    break;
                case EnemyKind.BacklineJumper:
                    fallbackKey = "zombie_runner";
                    stripKey = "anim_zom_runner";
                    stripFps = 9.5f;
                    break;
                case EnemyKind.HardhatArmored:
                    fallbackKey = "zombie_1";
                    stripKey = "anim_zom_armored";
                    stripFps = 6.0f;
                    break;
                default:
                    fallbackKey = (_battleRng.NextDouble() < 0.5) ? "zombie_0" : "zombie_1";
                    stripKey = "anim_zom_shambler";
                    stripFps = 6.5f;
                    break;
            }

            float phaseOff = (float)_battleRng.NextDouble() * 6.28f;
            Sprite fallbackSp = ForestArt.Get(fallbackKey, () => ForestArt.Get("zombie_0", () => ForestPlaceholderArt.Survivor(8)));
            var flipbook = new ForestSpriteFlipbook(
                stripKey,
                frameCount: 4,
                mode: FlipbookPlayMode.SplitMoveAttack,
                fps: stripFps,
                fallbackSprite: fallbackSp,
                ppu: stripPpu,
                pivotY: 0.08f,
                phaseOffset: phaseOff);

            float scale = ResolveEnemyVisualScale(q.kind, flipbook);
            float barY = isBoss ? 1.44f : (q.kind == EnemyKind.HardhatArmored ? 1.00f : 0.92f);

            var go = new GameObject($"ArenaEnemy_{q.kind}");
            go.transform.SetParent(_unitRoot, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            flipbook.ApplyTo(sr, 0f, moving: true, attacking: false);
            sr.flipX = false; // 资源默认朝左迎向我方阵营

            Transform shadowTr = CreateRoadShadow($"EnemyShadow_{q.kind}", new Vector2(startX, startY), isBoss ? 1.08f : 0.58f, isBoss ? 0.36f : 0.22f);
            OverheadBarRig bars = CreateOverheadBars(go.transform, isHero: false,
                width: isBoss ? 1.15f : 0.62f,
                yOffset: barY);

            Transform bossAuraTr = null;
            if (isBoss)
            {
                // 首领脚下暗红威压光环 + 头顶首领皇冠徽记
                var auraGo = new GameObject("BossGroundAura");
                auraGo.transform.SetParent(_decalRoot, false);
                var auraSr = auraGo.AddComponent<SpriteRenderer>();
                auraSr.sprite = _spWarningRing;
                auraSr.color = new Color(1f, 0.26f, 0.18f, 0.68f);
                auraSr.sortingOrder = -410;
                auraGo.transform.localScale = new Vector3(1.45f, 0.68f, 1f);
                bossAuraTr = auraGo.transform;

                var crownGo = new GameObject("BossCrown");
                crownGo.transform.SetParent(bars.root, false);
                crownGo.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                crownGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                var crownSr = crownGo.AddComponent<SpriteRenderer>();
                crownSr.sprite = _spBossCrown;
                crownSr.sortingOrder = 3205;

                TriggerMicroShake(0.22f, 0.08f);
            }
            else if (q.kind == EnemyKind.HardhatArmored)
            {
                // 重甲丧尸头顶小钢盾标识
                var badgeGo = new GameObject("ArmorBadge");
                badgeGo.transform.SetParent(bars.root, false);
                badgeGo.transform.localPosition = new Vector3(-0.36f, 0f, 0f);
                badgeGo.transform.localScale = new Vector3(0.14f, 0.14f, 1f);
                var bSr = badgeGo.AddComponent<SpriteRenderer>();
                bSr.sprite = _spWhiteQuad;
                bSr.color = new Color(0.75f, 0.88f, 1f, 0.95f);
                bSr.sortingOrder = 3205;
            }

            var enemy = new EnemyUnit
            {
                kind = q.kind,
                laneRow = q.laneRow,
                pos = new Vector2(startX, startY),
                baseScale = scale,
                alive = true,
                hp = q.maxHp,
                maxHp = q.maxHp,
                atk = q.atk,
                speed = q.speed,
                armor = q.armor,
                resistance = q.resistance,
                isElementalMutant = q.isElementalMutant,
                atkRange = isBoss ? 0.78f : (q.isElementalMutant && q.kind == EnemyKind.Shambler ? 0.96f : 0.64f),
                atkCooldown = 0.25f + (float)_battleRng.NextDouble() * 0.25f,
                phase = phaseOff,
                elapsedEntry = 0f,
                tr = go.transform,
                sr = sr,
                shadowTr = shadowTr,
                bossAuraTr = bossAuraTr,
                bars = bars,
                flipbook = flipbook,
                isMoving = true,
                attackAnimTimer = 0f,
                barYOffset = barY,
            };

            SyncEnemyVisualImmediate(enemy, 0f);
            _aliveEnemies.Add(enemy);
        }

        void StepEnemies(float dt)
        {
            bool inIntro = ElapsedBattleSec < ClashIntroSec;

            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!z.alive) continue;

                Vector2 prevStepPos = z.pos;
                z.phase += dt;
                z.elapsedEntry += dt;
                if (z.hitFlashTimer > 0f) z.hitFlashTimer = Mathf.Max(0f, z.hitFlashTimer - dt);
                if (z.attackAnimTimer > 0f) z.attackAnimTimer = Mathf.Max(0f, z.attackAnimTimer - dt);
                if (z.defenseShredTimer > 0f) z.defenseShredTimer = Mathf.Max(0f, z.defenseShredTimer - dt);
                if (z.asaArmorShredTimer > 0f) z.asaArmorShredTimer = Mathf.Max(0f, z.asaArmorShredTimer - dt);
                if (z.asaLastHitTimer > 0f)
                {
                    z.asaLastHitTimer = Mathf.Max(0f, z.asaLastHitTimer - dt);
                    if (z.asaLastHitTimer <= 0f) z.asaLastHitter = null;
                }
                if (z.chillTimer > 0f) z.chillTimer = Mathf.Max(0f, z.chillTimer - dt);
                if (z.cryoFreezeTimer > 0f) z.cryoFreezeTimer = Mathf.Max(0f, z.cryoFreezeTimer - dt);
                z.lungeOffset = Vector2.MoveTowards(z.lungeOffset, Vector2.zero, dt * 1.9f);

                // 雷猛 2★ 流血持续物理跳伤
                if (!inIntro && z.bleedTimer > 0f)
                {
                    z.bleedTimer -= dt;
                    z.bleedTickTimer -= dt;
                    if (z.bleedTickTimer <= 0f)
                    {
                        z.bleedTickTimer = 0.50f;
                        DamageEnemy(z, z.bleedDps * 0.50f, z.bleedOwner, isCrit: false, knockbackX: 0f, DamageType.Physical, allowLifesteal: false);
                        if (!z.alive) continue;
                    }
                }

                bool isBoss = z.kind == EnemyKind.GiantBoss;
                float rushTargetX = isBoss ? 1.85f : 2.15f;
                float entryRushSpd = Mathf.Max(z.speed * 2.2f, isBoss ? 2.75f : 2.25f);

                if (z.knockbackVel.sqrMagnitude > 0.0004f && z.jumpPhase != 2)
                {
                    z.pos = ClampEnemyUnitPos(z, z.pos + z.knockbackVel * dt);
                    z.knockbackVel = Vector2.MoveTowards(z.knockbackVel, Vector2.zero, dt * 8.0f);
                }

                // 0.0~0.55s 开场亮相期：先遣丧尸快速冲入桥面可见视野 (x <= 2.15 / Boss <= 1.85)！
                if (inIntro)
                {
                    if (z.pos.x > rushTargetX)
                        z.pos.x = Mathf.MoveTowards(z.pos.x, rushTargetX, entryRushSpd * dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    z.isMoving = Vector2.Distance(prevStepPos, z.pos) > 0.0003f;
                    continue;
                }

                // 新刷出的后续波次丧尸先全速冲入屏内可见区 (x <= 2.15 / Boss <= 1.85) 再转入常规战斗！
                if (z.pos.x > rushTargetX && z.jumpPhase == 0)
                {
                    z.pos.x = Mathf.MoveTowards(z.pos.x, rushTargetX, entryRushSpd * dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    z.isMoving = Vector2.Distance(prevStepPos, z.pos) > 0.0003f;
                    if (z.pos.x > rushTargetX + 0.01f)
                        continue;
                }

                if (z.stunTimer > 0f)
                {
                    z.stunTimer = Mathf.Max(0f, z.stunTimer - dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    z.isMoving = false;
                    z.attackAnimTimer = 0f;
                    continue;
                }

                // 跳尸突袭者专属红圈预警 -> 高抛飞跃切后排状态机！
                if (z.kind == EnemyKind.BacklineJumper && z.jumpPhase < 3)
                {
                    StepBacklineJumperSpecial(z, dt, prevStepPos);
                    continue;
                }

                HeroUnit target = PickTargetForEnemy(z);
                if (target == null)
                {
                    z.isMoving = false;
                    continue;
                }

                float chillSpdMul = z.chillTimer > 0f ? 0.75f : 1f;
                if (z.atkCooldown > 0f)
                    z.atkCooldown -= dt * chillSpdMul;

                float dist = Vector2.Distance(z.pos, target.pos);
                if (dist > z.atkRange)
                {
                    float standX = target.pos.x + (target.pos.x <= z.pos.x ? (z.atkRange * 0.80f) : (-z.atkRange * 0.80f));
                    if (target.isMelee)
                        standX = Mathf.Clamp(standX, -0.60f, isBoss ? 1.72f : 1.65f);
                    Vector2 standGoal = new Vector2(standX, target.pos.y);
                    float marchSpd = (z.pos.x > rushTargetX ? entryRushSpd : z.speed) * chillSpdMul;
                    z.pos = ClampEnemyUnitPos(z, Vector2.MoveTowards(z.pos, standGoal, marchSpd * dt));
                    z.isMoving = Vector2.Distance(prevStepPos, z.pos) > 0.0003f;
                }
                else if (z.atkCooldown <= 0f)
                {
                    z.atkCooldown = isBoss ? 1.25f : (z.kind == EnemyKind.FastRunner ? 0.76f : 0.98f);
                    z.isMoving = false;
                    z.attackAnimTimer = 0.38f;
                    float dirSign = target.pos.x <= z.pos.x ? -1f : 1f;
                    z.lungeOffset = new Vector2(dirSign * 0.15f, 0f);

                    if (isBoss)
                    {
                        // 巨型首领重击 + 元素震波冲击（8关+首领全额变异元素冲击，首领范围震波统一受英雄抗性减免！）
                        DamageType mainSlamType = z.isElementalMutant ? DamageType.Elemental : DamageType.Physical;
                        DamageHero(target, z.atk, knockbackX: -0.18f, attackerEnemy: z, dmgType: mainSlamType, isMeleeHit: true);
                        for (int h = 0; h < _heroes.Count; h++)
                        {
                            HeroUnit other = _heroes[h];
                            if (!other.alive || other == target) continue;
                            if (Vector2.Distance(other.pos, target.pos) <= 1.15f)
                                DamageHero(other, z.atk * 0.58f, knockbackX: -0.12f, attackerEnemy: z, dmgType: DamageType.Elemental, isMeleeHit: false);
                        }
                        TriggerMicroShake(0.14f, 0.05f);
                        Vector2 fxPt = ClampFireAndAoePos(target.pos + new Vector2(0f, 0.10f));
                        Color shockCol = z.isElementalMutant
                            ? new Color(0.55f, 1f, 0.34f, 0.85f)
                            : new Color(1f, 0.32f, 0.22f, 0.80f);
                        SpawnTransientFx(_spShockRing, fxPt, fxPt,
                            0.35f, 1.35f, shockCol, 0.24f, sortOrder: 2450);
                    }
                    else if (z.isElementalMutant)
                    {
                        // 第 8 关+ 酸液/毒雾变异尸：元素腐蚀打击（抗性增强阵 +120 抗性直接大幅减免！）
                        bool rangedAcid = z.kind == EnemyKind.Shambler && dist > 0.72f;
                        DamageHero(target, z.atk, knockbackX: -0.05f, attackerEnemy: z, dmgType: DamageType.Elemental, isMeleeHit: !rangedAcid);
                        Vector2 acidPt = ClampFireAndAoePos(target.pos + new Vector2(0f, 0.16f));
                        SpawnTransientFx(_spShockRing, acidPt, acidPt,
                            0.18f, 0.62f, new Color(0.46f, 1f, 0.32f, 0.78f), 0.18f, sortOrder: 2430);
                    }
                    else
                    {
                        DamageHero(target, z.atk, knockbackX: -0.06f, attackerEnemy: z, dmgType: DamageType.Physical, isMeleeHit: true);
                    }
                }
                else
                {
                    z.isMoving = false;
                }
                z.pos = ClampEnemyUnitPos(z, z.pos);
            }
        }

        void StepBacklineJumperSpecial(EnemyUnit z, float dt, Vector2 prevStepPos)
        {
            if (z.jumpPhase == 0)
            {
                // 先疾冲入桥面清晰可见区域 x <= 2.05f，再下蹲拉响后排红色预警圈！
                if (z.pos.x > 2.05f)
                {
                    z.pos.x = Mathf.MoveTowards(z.pos.x, 2.02f, z.speed * 2.2f * dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    z.isMoving = Vector2.Distance(prevStepPos, z.pos) > 0.0003f;
                    return;
                }

                Vector2 backSpot = ChooseBacklineJumpLandingSpot(z.laneRow);
                z.jumpStart = z.pos;
                z.jumpTarget = backSpot;
                z.jumpPhase = 1;
                z.jumpTimer = 0.65f; // 0.65s 醒目红圈地面预警！
                z.isMoving = false;
                z.attackAnimTimer = 0f;

                var ringGo = new GameObject("FlankerWarningRing");
                ringGo.transform.SetParent(_decalRoot, false);
                ringGo.transform.localPosition = new Vector3(backSpot.x, backSpot.y, 0f);
                var rSr = ringGo.AddComponent<SpriteRenderer>();
                rSr.sprite = _spWarningRing;
                rSr.color = new Color(1f, 0.20f, 0.18f, 0.90f);
                rSr.sortingOrder = -380;
                z.warningRingTr = ringGo.transform;
                z.warningRingSr = rSr;
            }
            else if (z.jumpPhase == 1)
            {
                z.jumpTimer -= dt;
                z.isMoving = false;
                if (z.warningRingTr != null)
                {
                    float pulse = 0.82f + Mathf.Sin(ElapsedBattleSec * 22f) * 0.16f;
                    z.warningRingTr.localScale = new Vector3(pulse * 1.15f, pulse * 0.62f, 1f);
                }
                if (z.jumpTimer <= 0f)
                {
                    z.jumpPhase = 2;
                    z.jumpTimer = 0f;
                    z.attackAnimTimer = 0.38f;
                }
            }
            else if (z.jumpPhase == 2)
            {
                const float leapDur = 0.62f;
                z.jumpTimer += dt;
                float u = Mathf.Clamp01(z.jumpTimer / leapDur);
                z.pos = Vector2.Lerp(z.jumpStart, z.jumpTarget, u);
                z.isMoving = true;
                z.attackAnimTimer = 0.38f;

                if (u >= 1f)
                {
                    z.jumpPhase = 3;
                    z.pos = ClampEnemyUnitPos(z, z.jumpTarget);
                    if (z.warningRingTr != null)
                    {
                        DestroyObj(z.warningRingTr.gameObject);
                        z.warningRingTr = null;
                        z.warningRingSr = null;
                    }
                    // 落地冲击红环！
                    Vector2 landFx = ClampFireAndAoePos(z.pos + new Vector2(0f, 0.06f));
                    SpawnTransientFx(_spShockRing, landFx, landFx,
                        0.28f, 1.05f, new Color(1f, 0.28f, 0.24f, 0.85f), 0.24f, sortOrder: 2420);
                }
            }
        }

        Vector2 ChooseBacklineJumpLandingSpot(int preferredRow)
        {
            // 优先跳向 col=0 后排有远程英雄驻守的路，直接检验玩家后排保护与布阵深度！
            HeroUnit bestBackHero = null;
            float leftmostX = 99f;
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (!h.alive) continue;
                float scoreX = h.pos.x + (h.range <= 1 ? 1.5f : (h.range <= 3 ? 0.65f : 0f));
                if (scoreX < leftmostX)
                {
                    leftmostX = scoreX;
                    bestBackHero = h;
                }
            }

            if (bestBackHero != null)
            {
                float landX = Mathf.Clamp(bestBackHero.pos.x - 0.25f, -2.85f, -1.85f);
                float landY = Mathf.Clamp(bestBackHero.pos.y + 0.12f, -2.15f, 1.45f);
                return new Vector2(landX, landY);
            }

            return new Vector2(ColLocalX[0], RowLocalY[Mathf.Clamp(preferredRow, 0, 2)]);
        }

        // =====================================================================
        // 弹道、地面火海与伤害/护盾/怒气结算
        // =====================================================================
        void SpawnArenaProjectile(ArenaProjectile p, string spriteKey, Color tint, float scale)
        {
            p.curLocal = p.fromLocal;
            if (!_isFastSimulating)
            {
                var go = new GameObject("ArenaProj_" + p.style);
                go.transform.SetParent(_projRoot, false);
                go.transform.localPosition = new Vector3(p.fromLocal.x, p.fromLocal.y, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (spriteKey == "__hook_claw")
                    sr.sprite = _spHookClaw != null ? _spHookClaw : _spWhiteQuad;
                else if (spriteKey == "__lightning_seg")
                    sr.sprite = _spLightningSeg != null ? _spLightningSeg : _spWhiteQuad;
                else
                    sr.sprite = ForestArt.Get(spriteKey, () => _spWhiteQuad);
                sr.color = tint;
                sr.sortingOrder = 2800;

                Vector2 dir = p.toLocal - p.fromLocal;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    go.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                }
                p.tr = go.transform;
                p.sr = sr;
            }
            _projectiles.Add(p);
        }

        void StepProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ArenaProjectile p = _projectiles[i];
                p.elapsed += dt;
                float u = Mathf.Clamp01(p.elapsed / Mathf.Max(0.05f, p.duration));

                if (!p.piercing && p.style != ProjectileStyle.MolotovArc && p.style != ProjectileStyle.CryoFlaskArc)
                {
                    if (IsEnemyTargetable(p.targetEnemy))
                        p.toLocal = new Vector2(Mathf.Min(2.20f, p.targetEnemy.pos.x), p.targetEnemy.pos.y + 0.34f);
                    else if (p.targetAlly != null && p.targetAlly.alive)
                        p.toLocal = p.targetAlly.pos + new Vector2(0f, 0.38f);
                }

                Vector2 basePt = Vector2.Lerp(p.fromLocal, p.toLocal, u);
                float parabola = p.arcHeight > 0.01f ? Mathf.Sin(u * Mathf.PI) * p.arcHeight : 0f;
                Vector2 prevLocal = p.curLocal;
                p.curLocal = basePt + new Vector2(0f, parabola);

                if (p.tr != null)
                {
                    p.tr.localPosition = new Vector3(p.curLocal.x, p.curLocal.y, 0f);
                    if (p.style == ProjectileStyle.MolotovArc || p.style == ProjectileStyle.CryoFlaskArc)
                    {
                        p.tr.Rotate(0f, 0f, -540f * dt);
                    }
                    else
                    {
                        Vector2 vel = p.curLocal - prevLocal;
                        if (vel.sqrMagnitude > 0.00001f)
                        {
                            float ang = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                            p.tr.localRotation = Quaternion.Euler(0f, 0f, ang);
                        }
                    }
                }

                // 穿透类弹道（林砚贯日箭雨 / 林砚2★暴击穿透弩 / 铁叔重装霰弹弹幕）沿途实时命中
                if (p.piercing)
                {
                    bool hitCapReached = false;
                    for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                    {
                        EnemyUnit z = _aliveEnemies[e];
                        if (!IsEnemyTargetable(z) || p.piercedSet.Contains(z)) continue;
                        if (Vector2.Distance(p.curLocal, z.pos + new Vector2(0f, 0.32f)) <= 0.52f)
                        {
                            p.piercedSet.Add(z);
                            DamageEnemy(z, p.amount, p.sourceHero, p.isCrit, p.knockback, p.dmgType);
                            if (p.maxPierceCount > 0 && p.piercedSet.Count >= p.maxPierceCount)
                            {
                                hitCapReached = true;
                                break;
                            }
                        }
                    }

                    if (hitCapReached)
                    {
                        if (p.tr != null) DestroyObj(p.tr.gameObject);
                        _projectiles.RemoveAt(i);
                        continue;
                    }
                }

                if (u >= 1f)
                {
                    ResolveProjectileArrival(p);
                    if (p.tr != null) DestroyObj(p.tr.gameObject);
                    _projectiles.RemoveAt(i);
                }
            }
        }

        void ResolveProjectileArrival(ArenaProjectile p)
        {
            if (p.style == ProjectileStyle.HealHerb)
            {
                HeroUnit ally = p.targetAlly;
                if (ally != null && ally.alive)
                {
                    float missing = Mathf.Max(0f, ally.maxHp - ally.hp);
                    float actual = Mathf.Min(missing, p.amount);
                    ally.hp = Mathf.Min(ally.maxHp, ally.hp + p.amount);
                    if (p.sourceHero != null)
                        p.sourceHero.totalHeal += actual + p.amount * 0.25f;

                    SpawnWorldNumber(ally.pos + new Vector2(0f, 0.85f), Mathf.Max(1, Mathf.RoundToInt(p.amount)), isHeal: true, isCrit: false);
                    SpawnTransientFx(_spShockRing, ally.pos + new Vector2(0f, 0.10f), ally.pos + new Vector2(0f, 0.10f),
                        0.18f, 0.72f, new Color(0.32f, 1f, 0.58f, 0.82f), 0.22f, sortOrder: 2400);
                }
                return;
            }

            if (p.style == ProjectileStyle.MolotovArc)
            {
                Vector2 hitGround = ClampFireAndAoePos(p.toLocal);
                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(z.pos, hitGround) <= p.splashRadius)
                        DamageEnemy(z, p.amount, p.sourceHero, p.isCrit, knockbackX: 0.10f, DamageType.Elemental, allowLifesteal: false);
                }
                SpawnFirePool(p.sourceHero, hitGround, p.splashRadius * 0.95f, p.firePoolDps, p.firePoolLife);
                SpawnTransientFx(_spShockRing, hitGround, hitGround,
                    0.25f, p.splashRadius * 1.65f, new Color(1f, 0.55f, 0.15f, 0.88f), 0.24f, sortOrder: 2480);
                return;
            }

            if (p.style == ProjectileStyle.CryoFlaskArc)
            {
                Vector2 hitGround = ClampFireAndAoePos(p.toLocal);
                SpawnFrostZone(p.sourceHero, hitGround, p.splashRadius, p.firePoolLife > 0.1f ? p.firePoolLife : 2.5f);
                SpawnWorldFloatingText(hitGround + new Vector2(0f, 0.96f), "寒霜冻结!", new Color(0.46f, 0.96f, 1f, 1f), 1.08f);
                TriggerMicroShake(0.16f, 0.055f);

                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(z.pos, hitGround) <= p.splashRadius)
                    {
                        z.stunTimer = Mathf.Max(z.stunTimer, 2.0f);
                        z.cryoFreezeTimer = Mathf.Max(z.cryoFreezeTimer, 2.0f);
                        z.chillTimer = Mathf.Max(z.chillTimer, 2.8f);
                        DamageEnemy(z, p.amount, p.sourceHero, isCrit: true, knockbackX: 0f, DamageType.Elemental, allowLifesteal: false);
                    }
                }

                SpawnTransientFx(_spShockRing, hitGround, hitGround,
                    0.32f, p.splashRadius * 1.95f, new Color(0.45f, 0.94f, 1f, 0.92f), 0.32f, sortOrder: 2540);
                SpawnTransientFx(_spShockRing, hitGround, hitGround,
                    0.18f, p.splashRadius * 1.25f, new Color(0.84f, 0.98f, 1f, 0.88f), 0.24f, sortOrder: 2545);
                return;
            }

            if (!p.piercing)
            {
                EnemyUnit hit = IsEnemyTargetable(p.targetEnemy)
                    ? p.targetEnemy
                    : FindClosestEnemyTo(p.toLocal - new Vector2(0f, 0.30f), maxDist: 0.68f);
                if (IsEnemyTargetable(hit))
                {
                    if (p.chillDuration > 0f)
                    {
                        hit.chillTimer = Mathf.Max(hit.chillTimer, p.chillDuration);
                        SpawnTransientFx(_spShockRing, hit.pos + new Vector2(0f, 0.28f), hit.pos + new Vector2(0f, 0.28f),
                            0.14f, 0.56f, new Color(0.45f, 0.92f, 1f, 0.82f), 0.18f, sortOrder: 2490);
                    }
                    else if (p.style == ProjectileStyle.ElectricBolt)
                    {
                        SpawnTransientFx(_spShockRing, hit.pos + new Vector2(0f, 0.32f), hit.pos + new Vector2(0f, 0.32f),
                            0.15f, 0.62f, new Color(0.56f, 0.86f, 1f, 0.88f), 0.16f, sortOrder: 2495);
                    }
                    DamageEnemy(hit, p.amount, p.sourceHero, p.isCrit, p.knockback, p.dmgType);
                }
            }
        }

        void SpawnFirePool(HeroUnit owner, Vector2 localPos, float radius, float dps, float duration)
        {
            if (dps <= 0f || duration <= 0f) return;
            localPos = ClampFireAndAoePos(localPos);
            Transform tr = null;
            SpriteRenderer sr = null;
            if (!_isFastSimulating)
            {
                var go = new GameObject("MolotovRoadFire");
                go.transform.SetParent(_hazardRoot, false);
                go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
                go.transform.localScale = new Vector3(radius * 1.55f, radius * 0.86f, 1f);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ForestArt.Get("proj_fire_pool", () => _spShockRing);
                sr.color = new Color(1f, 0.68f, 0.22f, 0.88f);
                sr.sortingOrder = -350;
                tr = go.transform;
            }

            _firePools.Add(new ArenaFirePool
            {
                owner = owner,
                localPos = localPos,
                radius = radius,
                dps = dps,
                life = duration,
                maxLife = duration,
                tickTimer = 0.22f,
                shredsDefenses = owner != null && owner.star >= 2,
                detonateOnExpire = owner != null && owner.star >= 3,
                tr = tr,
                sr = sr,
            });
        }

        void StepFirePools(float dt)
        {
            for (int i = _firePools.Count - 1; i >= 0; i--)
            {
                ArenaFirePool fp = _firePools[i];
                fp.life -= dt;
                fp.tickTimer -= dt;

                if (fp.tr != null && fp.sr != null)
                {
                    float pulse = 1f + Mathf.Sin((ElapsedBattleSec + i) * 15f) * 0.07f;
                    fp.tr.localScale = new Vector3(fp.radius * 1.55f * pulse, fp.radius * 0.86f * pulse, 1f);
                    float fade = Mathf.Clamp01(fp.life / 0.55f);
                    fp.sr.color = new Color(1f, 0.66f, 0.20f, 0.86f * fade);
                }

                // 小棠 2★ 被动【熔甲灼烧】：站在火海内的丧尸实时降低 20% 护甲与抗性！
                if (fp.shredsDefenses)
                {
                    for (int e = 0; e < _aliveEnemies.Count; e++)
                    {
                        EnemyUnit z = _aliveEnemies[e];
                        if (!IsEnemyTargetable(z)) continue;
                        if (Vector2.Distance(z.pos, fp.localPos) <= fp.radius + 0.20f)
                            z.defenseShredTimer = Mathf.Max(z.defenseShredTimer, 0.35f);
                    }
                }

                if (fp.tickTimer <= 0f)
                {
                    fp.tickTimer = 0.50f;
                    float tickDmg = fp.dps * 0.50f;
                    for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                    {
                        EnemyUnit z = _aliveEnemies[e];
                        if (!IsEnemyTargetable(z)) continue;
                        if (Vector2.Distance(z.pos, fp.localPos) <= fp.radius + 0.18f)
                            DamageEnemy(z, tickDmg, fp.owner, isCrit: false, knockbackX: 0f, DamageType.Elemental, allowLifesteal: false);
                    }
                }

                if (fp.life <= 0f)
                {
                    // 小棠 3★ 觉醒【燃烧瓶增强】：火海结束时触发二次爆轰，造成 1.2f * skillPower 范围元素伤害！
                    if (fp.detonateOnExpire && fp.owner != null && fp.owner.alive)
                    {
                        float detonateDmg = 1.20f * fp.owner.skillPower * fp.owner.elementalDmgMul;
                        float detonateRad = fp.radius * 1.25f;
                        for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                        {
                            EnemyUnit z = _aliveEnemies[e];
                            if (!IsEnemyTargetable(z)) continue;
                            if (Vector2.Distance(z.pos, fp.localPos) <= detonateRad)
                                DamageEnemy(z, detonateDmg, fp.owner, isCrit: true, knockbackX: 0.16f, DamageType.Elemental, allowLifesteal: false);
                        }
                        TriggerMicroShake(0.12f, 0.045f);
                        SpawnTransientFx(_spShockRing, fp.localPos, fp.localPos,
                            0.30f, detonateRad * 1.85f, new Color(1f, 0.38f, 0.12f, 0.92f), 0.28f, sortOrder: 2520);
                    }

                    if (fp.tr != null) DestroyObj(fp.tr.gameObject);
                    _firePools.RemoveAt(i);
                }
            }
        }

        void SpawnFrostZone(HeroUnit owner, Vector2 localPos, float radius, float duration)
        {
            if (duration <= 0f) return;
            localPos = ClampFireAndAoePos(localPos);
            Transform tr = null;
            SpriteRenderer sr = null;
            if (!_isFastSimulating)
            {
                var go = new GameObject("CryoFrostCircle");
                go.transform.SetParent(_hazardRoot, false);
                go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
                go.transform.localScale = new Vector3(radius * 1.92f, radius * 1.06f, 1f);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spFrostCircle != null ? _spFrostCircle : _spShockRing;
                sr.color = new Color(0.45f, 0.94f, 1f, 0.88f);
                sr.sortingOrder = -348;
                tr = go.transform;
            }

            _frostZones.Add(new ArenaFrostZone
            {
                owner = owner,
                localPos = localPos,
                radius = radius,
                life = duration,
                maxLife = duration,
                tr = tr,
                sr = sr,
            });
        }

        void StepFrostZones(float dt)
        {
            for (int i = _frostZones.Count - 1; i >= 0; i--)
            {
                ArenaFrostZone fz = _frostZones[i];
                fz.life -= dt;

                if (fz.tr != null && fz.sr != null)
                {
                    float pulse = 1f + Mathf.Sin((ElapsedBattleSec + i) * 11f) * 0.05f;
                    fz.tr.localScale = new Vector3(fz.radius * 1.92f * pulse, fz.radius * 1.06f * pulse, 1f);
                    float fade = Mathf.Clamp01(fz.life / 0.45f);
                    fz.sr.color = new Color(0.45f, 0.94f, 1f, 0.88f * fade);
                }

                for (int e = 0; e < _aliveEnemies.Count; e++)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(z.pos, fz.localPos) <= fz.radius + 0.12f)
                        z.chillTimer = Mathf.Max(z.chillTimer, 0.75f);
                }

                if (fz.life <= 0f)
                {
                    if (fz.tr != null) DestroyObj(fz.tr.gameObject);
                    _frostZones.RemoveAt(i);
                }
            }
        }

        void DamageEnemy(
            EnemyUnit z,
            float rawDmg,
            HeroUnit attacker,
            bool isCrit,
            float knockbackX,
            DamageType dmgType = DamageType.Physical,
            bool allowLifesteal = true)
        {
            if (z == null || !z.alive) return;

            float bonusMul = 1f;
            if (attacker != null)
            {
                if (attacker.heroId == "asa")
                {
                    z.asaLastHitter = attacker;
                    z.asaLastHitTimer = 1.20f;
                }

                // 铁叔 3★ 觉醒【高爆霰弹增强】：对距离 <= 2.3f 贴脸丧尸暴击伤害额外提高 +45%！
                if (isCrit && attacker.heroId == "tieshu" && attacker.star >= 3)
                {
                    if (Vector2.Distance(attacker.pos, z.pos) <= 2.30f)
                        bonusMul *= (attacker.critMult + 0.45f) / Mathf.Max(1f, attacker.critMult);
                }

                // 鹰眼狙击徽章：对中后场远距敌人额外增伤
                if (attacker.hasEagleScopeBadge && z.pos.x > 0.65f)
                    bonusMul *= 1.15f;
            }

            // 青禾 3★ 觉醒【寒霜瓶增强】：处于寒霜冻结 (cryoFreezeTimer > 0 && stunTimer > 0) 或霜冻减速 (chillTimer > 0) 的敌人，受到全体友军普攻与绝技伤害提高 +22%..+34%！
            if ((z.cryoFreezeTimer > 0f && z.stunTimer > 0f) || z.chillTimer > 0f)
            {
                float topCryoAmp = 0f;
                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit qh = _heroes[i];
                    if (!qh.alive || qh.heroId != "qinghe" || qh.star < 3) continue;
                    float amp = 0.22f + 0.03f * Mathf.Max(0, qh.skillLv3 - 1);
                    if (amp > topCryoAmp) topCryoAmp = amp;
                }
                if (topCryoAmp > 0f)
                    bonusMul *= (1f + topCryoAmp);
            }

            // 真实物理护甲 vs 元素抗性减伤公式：mit = 1 - def / (def + 300)
            // （小棠 2★ 熔甲灼烧降低双防 20%；阿飒 3★ 夺命钩降低物理护甲 35%）
            float armorShredMul = (z.defenseShredTimer > 0f ? 0.80f : 1f) * (z.asaArmorShredTimer > 0f ? 0.65f : 1f);
            float resistShredMul = z.defenseShredTimer > 0f ? 0.80f : 1f;
            float defStat = Mathf.Max(0f, dmgType == DamageType.Elemental ? (z.resistance * resistShredMul) : (z.armor * armorShredMul));
            float mit = 1f - defStat / (defStat + 300f);

            float finalDmg = Mathf.Max(1f, rawDmg * bonusMul * mit);
            z.hp = Mathf.Max(0f, z.hp - finalDmg);
            z.hitFlashTimer = 0.11f;

            if (knockbackX > 0.001f)
            {
                float resist = z.kind == EnemyKind.GiantBoss ? 0.22f : (z.kind == EnemyKind.HardhatArmored ? 0.55f : 1f);
                z.knockbackVel.x = Mathf.Clamp(z.knockbackVel.x + knockbackX * 5.2f * resist, -2.5f, 3.2f);
            }

            if (attacker != null)
            {
                attacker.totalDamage += finalDmg;
                AddHeroRage(attacker, 0.045f);

                // 狂战士血怒徽章 / 物理吸血词条：物理命中实时回复自身生命
                if (allowLifesteal && dmgType == DamageType.Physical && attacker.alive && attacker.lifestealPct > 0.001f)
                {
                    float vampHeal = finalDmg * attacker.lifestealPct * (1f + attacker.healShieldBonus);
                    if (vampHeal > 0.25f)
                    {
                        attacker.hp = Mathf.Min(attacker.maxHp, attacker.hp + vampHeal);
                        attacker.totalHeal += vampHeal;
                    }
                }
            }

            if (!_isFastSimulating)
            {
                int intDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg));
                Vector3 worldHit = ArenaCenter + new Vector3(z.pos.x, z.pos.y + (z.kind == EnemyKind.GiantBoss ? 0.85f : 0.56f), 0f);
                if (_world != null && _world.HasDamageNumberListeners)
                    _world.RaiseCombatDamageNumber(worldHit, intDmg, isCrit);
                else
                    SpawnWorldNumber(z.pos + new Vector2(0f, 0.65f), intDmg, isHeal: false, isCrit: isCrit);
            }

            if (z.hp <= 0f)
            {
                z.alive = false;
                if (attacker != null)
                    AddHeroRage(attacker, 0.12f);
                for (int i = 0; i < _heroes.Count; i++)
                    if (_heroes[i].alive) AddHeroRage(_heroes[i], 0.04f);

                // 阿飒 2★ 被动【孤狼猎手】：普攻或绝技击败敌人（或被阿飒命中后 1.2s 内阵亡），立即恢复 +35 怒气 (+0.35f rage01)！
                HeroUnit asaUnit = (attacker != null && attacker.heroId == "asa")
                    ? attacker
                    : (z.asaLastHitTimer > 0f ? z.asaLastHitter : null);
                if (asaUnit != null && asaUnit.alive && asaUnit.star >= 2)
                {
                    asaUnit.rage01 = Mathf.Clamp01(asaUnit.rage01 + 0.35f);
                    SpawnWorldFloatingText(asaUnit.pos + new Vector2(-0.26f, 1.18f), "+35怒气", new Color(1f, 0.86f, 0.25f, 1f), 1.02f);
                    SpawnTransientFx(_spShockRing, asaUnit.pos + new Vector2(0f, 0.12f), asaUnit.pos + new Vector2(0f, 0.12f),
                        0.20f, 0.96f, new Color(1f, 0.62f, 0.24f, 0.88f), 0.24f, sortOrder: 2520);
                }

                if (!_isFastSimulating)
                {
                    SpawnTransientFx(_spShockRing, z.pos + new Vector2(0f, 0.24f), z.pos + new Vector2(0f, 0.24f),
                        0.20f, z.kind == EnemyKind.GiantBoss ? 1.35f : 0.65f,
                        new Color(1f, 0.92f, 0.76f, 0.72f), 0.20f, sortOrder: 2500);
                }
                DestroyEnemyVisuals(z);
                _aliveEnemies.Remove(z);
            }
        }

        void DamageHero(
            HeroUnit h,
            float rawDmg,
            float knockbackX,
            EnemyUnit attackerEnemy = null,
            DamageType dmgType = DamageType.Physical,
            bool isMeleeHit = true)
        {
            if (h == null || !h.alive) return;

            bool hadShieldBefore = h.shield > 0.01f;

            // 真实物理护甲 vs 元素抗性减伤公式：mit = 1 - def / (def + 300)
            float effResist = h.resistance * (1f + (h.soupResistTimer > 0f ? h.soupResistBonusMul : 0f));
            float defStat = Mathf.Max(0f, dmgType == DamageType.Elemental ? effResist : h.armor);
            float statMit = 1f - defStat / (defStat + 300f);

            // 大锤 2★ 被动【焊甲反震】：护盾持续期间额外获得 +20% 伤害减免！
            bool isDachui = h.heroId == "dachui" || (h.roleId == 1 && h.heroId != "asa");
            float dachuiShieldDr = (isDachui && h.star >= 2 && hadShieldBefore)
                ? (0.20f + 0.02f * Mathf.Max(0, h.skillLv2 - 1))
                : 0f;
            float totalDr = Mathf.Clamp(h.dmgReduction + dachuiShieldDr, 0f, 0.75f);
            float reduced = Mathf.Max(1f, rawDmg * statMit * (1f - totalDr));

            if (h.shield > 0f)
            {
                float absorbed = Mathf.Min(h.shield, reduced);
                h.shield -= absorbed;
                reduced -= absorbed;
            }

            if (reduced > 0f)
                h.hp = Mathf.Max(0f, h.hp - reduced);

            h.hitFlashTimer = 0.11f;
            AddHeroRage(h, 0.075f); // 受击回怒机制，让前排坦克更快开出大招护盾！

            if (Mathf.Abs(knockbackX) > 0.001f)
                h.knockbackVel.x = Mathf.Clamp(h.knockbackVel.x + knockbackX * 3.8f, -1.8f, 1.8f);

            // 路障荆棘徽章 (thornsPct > 0) + 大锤 2★ 焊甲反震：将近战啃咬伤害反弹回丧尸！
            if (isMeleeHit && attackerEnemy != null && attackerEnemy.alive)
            {
                float thorns = h.thornsPct + ((isDachui && h.star >= 2 && hadShieldBefore) ? 0.10f : 0f);
                if (thorns > 0.001f)
                {
                    float reflectRaw = Mathf.Max(2f, rawDmg * thorns);
                    DamageEnemy(attackerEnemy, reflectRaw, h, isCrit: false, knockbackX: 0.04f, DamageType.Physical, allowLifesteal: false);
                }
            }

            // 大锤 3★ 觉醒【重盾震击增强】：护盾被击破瞬间触发二次震波，眩晕周围丧尸 1.5s！
            if (h.alive && h.hp > 0f && hadShieldBefore && h.shield <= 0.01f)
            {
                h.shield = 0f;
                bool wasUltShield = h.dachuiUltShieldActive;
                h.dachuiUltShieldActive = false;
                h.dachuiShieldTimer = 0f;
                if (isDachui && h.star >= 3)
                {
                    if (wasUltShield) h.dachuiBurstCooldown = 0f;
                    TriggerDachuiSecondaryShockwave(h);
                }
            }

            if (h.hp <= 0f)
            {
                h.alive = false;
                h.hp = 0f;
                h.shield = 0f;
                h.rage01 = 0f;
                h.dachuiUltShieldActive = false;
                if (h.puppet != null && h.puppet.Root != null)
                    h.puppet.Root.gameObject.SetActive(false);
                if (h.shadowTr != null)
                    h.shadowTr.gameObject.SetActive(false);
            }
        }

        static void AddHeroRage(HeroUnit h, float rawDelta01)
        {
            if (h == null || !h.alive) return;
            h.rage01 = Mathf.Clamp01(h.rage01 + rawDelta01 * h.rageGainMul);
        }

        bool RollCrit(float chance)
        {
            return _battleRng.NextDouble() < chance;
        }

        // =====================================================================
        // 索敌与软分离（前排吸收火力 + 跳尸切后排）
        // =====================================================================
        static bool IsEnemyTargetable(EnemyUnit z)
            => z != null && z.alive && z.jumpPhase != 2 && z.pos.x <= 2.25f;

        bool HasAnyTargetableEnemy()
        {
            for (int i = 0; i < _aliveEnemies.Count; i++)
                if (IsEnemyTargetable(_aliveEnemies[i])) return true;
            return false;
        }

        EnemyUnit PickTargetForHero(HeroUnit h)
        {
            EnemyUnit best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                // 尚未冲入桥面可见区 (x > 2.25f) 或腾空跳跃途中的丧尸绝不可被索敌打击！
                if (!IsEnemyTargetable(z)) continue;

                float dist = Vector2.Distance(h.pos, z.pos);
                float laneDiff = Mathf.Abs(h.pos.y - z.pos.y);
                // 优先打击冲在最前排或已跳入我方后排的近身威胁！
                float frontThreatBias = z.pos.x * 0.45f;
                float score = dist * 0.85f + laneDiff * 0.55f + frontThreatBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = z;
                }
            }
            return best;
        }

        HeroUnit PickTargetForEnemy(EnemyUnit z)
        {
            HeroUnit best = null;
            float bestScore = float.MaxValue;
            bool preferBackline = z.kind == EnemyKind.BacklineJumper && z.jumpPhase >= 3;

            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (!h.alive) continue;

                float dist = Vector2.Distance(z.pos, h.pos);
                float laneDiff = Mathf.Abs(z.pos.y - h.pos.y);

                float score;
                if (preferBackline)
                {
                    // 跳尸落地后专门切后排远程脆皮（优先 range >= 4 的后排核心）！
                    float meleePenalty = h.range <= 1 ? 1.85f : (h.range <= 3 ? 0.75f : 0f);
                    score = dist * 0.9f + meleePenalty + (h.pos.x + 3.0f) * 0.35f;
                }
                else
                {
                    // 普通丧尸优先啃咬顶在最前排 (pos.x 大) 及高嘲讽值（大锤主动嘲讽/前排列 col=2）的肉盾！
                    float frontPriority = -h.pos.x * 0.65f;
                    float effAggro = h.aggroWeight + (h.dachuiTauntTimer > 0f ? 1.65f : 0f);
                    float tauntBias = (effAggro - 1f) * (dist < 2.85f ? 0.90f : 0.35f);
                    score = dist * 0.82f + laneDiff * 0.48f + frontPriority - tauntBias;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = h;
                }
            }
            return best;
        }

        EnemyUnit FindClosestEnemyTo(Vector2 pt, float maxDist = float.MaxValue)
        {
            EnemyUnit best = null;
            float bestD = maxDist;
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!IsEnemyTargetable(z)) continue;
                float d = Vector2.Distance(pt, z.pos);
                if (d < bestD)
                {
                    bestD = d;
                    best = z;
                }
            }
            return best;
        }

        void ResolveSoftUnitSeparation()
        {
            // 英雄之间柔和错位，且严格遵守近战 [-2.95, +1.05] / 远程 [-2.95, -0.45] 边界！
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (!_heroes[i].alive) continue;
                for (int j = i + 1; j < _heroes.Count; j++)
                {
                    if (!_heroes[j].alive) continue;
                    Vector2 d = _heroes[j].pos - _heroes[i].pos;
                    float m = d.magnitude;
                    if (m < 0.44f)
                    {
                        Vector2 push = m > 0.001f ? (d / m) * (0.44f - m) * 0.35f : new Vector2(0f, 0.12f);
                        _heroes[i].pos = ClampHeroPos(_heroes[i], _heroes[i].pos - push);
                        _heroes[j].pos = ClampHeroPos(_heroes[j], _heroes[j].pos + push);
                    }
                }
            }

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit a = _aliveEnemies[i];
                if (!a.alive || a.jumpPhase == 2) continue;
                for (int j = i + 1; j < _aliveEnemies.Count; j++)
                {
                    EnemyUnit b = _aliveEnemies[j];
                    if (!b.alive || b.jumpPhase == 2) continue;
                    Vector2 d = b.pos - a.pos;
                    float minSep = (a.kind == EnemyKind.GiantBoss || b.kind == EnemyKind.GiantBoss) ? 0.62f : 0.38f;
                    float m = d.magnitude;
                    if (m < minSep)
                    {
                        Vector2 push = m > 0.001f ? (d / m) * (minSep - m) * 0.32f : new Vector2(0.05f, 0.10f);
                        a.pos = ClampEnemyUnitPos(a, a.pos - push);
                        b.pos = ClampEnemyUnitPos(b, b.pos + push);
                    }
                }
            }
        }

        static Vector2 ClampHeroPos(HeroUnit h, Vector2 p)
        {
            float maxX = -0.45f;
            if (h != null)
            {
                if (h.range <= 1) maxX = 1.05f;
                else if (h.heroId == "asa" || h.range == 2) maxX = 0.55f;
                else if (h.range <= 3) maxX = 0.25f;
                else maxX = -0.45f;
            }
            p.x = Mathf.Clamp(p.x, -2.95f, maxX);
            p.y = Mathf.Clamp(p.y, -2.25f, 1.55f);
            return p;
        }

        static Vector2 ClampEnemyUnitPos(EnemyUnit z, Vector2 p)
        {
            float maxX = 3.05f;
            if (z != null)
            {
                if (z.kind == EnemyKind.GiantBoss)
                    maxX = z.elapsedEntry > 0.45f ? 1.75f : 2.95f;
                else
                    maxX = z.elapsedEntry > 0.45f ? 2.20f : 3.05f;
            }
            p.x = Mathf.Clamp(p.x, -2.85f, maxX);
            p.y = Mathf.Clamp(p.y, -2.25f, 1.55f);
            return p;
        }

        static Vector2 ClampFireAndAoePos(Vector2 p)
        {
            p.x = Mathf.Clamp(p.x, -2.60f, 1.95f);
            p.y = Mathf.Clamp(p.y, -2.15f, 1.45f);
            return p;
        }

        // =====================================================================
        // 汇总 HUD 数据契约与世界空间血条/护盾/怒气条同步
        // =====================================================================
        void RefreshAggregatedStatsAndHudStates()
        {
            float teamHp = 0f, teamMax = 0f;
            _heroStates.Clear();

            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                teamMax += h.maxHp;
                if (h.alive) teamHp += h.hp;

                _heroStates.Add(new ArenaHeroHudState
                {
                    heroIdx = h.heroIdx,
                    slot0to8 = h.slot0to8,
                    name = h.name,
                    star = h.star,
                    skillName = h.skillName,
                    alive = h.alive,
                    hp = h.alive ? h.hp : 0f,
                    maxHp = h.maxHp,
                    shield = h.alive ? h.shield : 0f,
                    rage01 = h.alive ? h.rage01 : 0f,
                    ultReady = h.alive && h.rage01 >= 0.999f,
                    totalDamage = h.totalDamage,
                    totalHeal = h.totalHeal,
                });
            }

            TeamTotalMaxHp = Mathf.Max(1f, teamMax);
            TeamTotalHp = Mathf.Clamp(teamHp, 0f, TeamTotalMaxHp);

            float aliveEnemyHp = 0f;
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                if (_aliveEnemies[i].alive)
                    aliveEnemyHp += _aliveEnemies[i].hp;
            }
            EnemyTotalHp = Mathf.Clamp(aliveEnemyHp + _queuedHpSum, 0f, Mathf.Max(1f, EnemyTotalMaxHp));
            EnemiesAliveAndQueued = _aliveEnemies.Count + _spawnQueue.Count;
        }

        void SyncAllUnitVisuals(float scaledDt, float rawDt)
        {
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (!h.alive || h.puppet == null) continue;

                h.puppet.SetFacing(h.facingRight);
                Vector3 renderPos = new Vector3(h.pos.x + h.lungeOffset.x, h.pos.y + h.lungeOffset.y, 0f);
                h.puppet.SyncWorldPos(renderPos, null, sortingBias: 500);
                if (h.shadowTr != null)
                    h.shadowTr.localPosition = new Vector3(h.pos.x, h.pos.y - 0.02f, 0f);
                h.puppet.Tick(scaledDt);

                // 受击闪白
                bool flash = h.hitFlashTimer > 0f;
                for (int r = 0; r < h.tintRenderers.Length; r++)
                {
                    SpriteRenderer sr = h.tintRenderers[r];
                    if (sr != null)
                        sr.color = flash ? new Color(1f, 0.58f, 0.52f, 1f) : h.baseColors[r];
                }

                UpdateOverheadBarRig(h.bars, h.hp, h.maxHp, h.shield, h.rage01, showRage: true);
            }

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!z.alive || z.tr == null) continue;
                SyncEnemyVisualImmediate(z, rawDt);
            }
        }

        void SyncHeroVisualImmediate(HeroUnit h)
        {
            if (h.puppet == null) return;
            h.puppet.SetFacing(h.facingRight);
            h.puppet.SyncWorldPos(new Vector3(h.pos.x, h.pos.y, 0f), null, sortingBias: 500);
            if (h.shadowTr != null)
                h.shadowTr.localPosition = new Vector3(h.pos.x, h.pos.y - 0.02f, 0f);
            h.puppet.Tick(0.016f);
            UpdateOverheadBarRig(h.bars, h.hp, h.maxHp, h.shield, h.rage01, showRage: true);
        }

        void SyncEnemyVisualImmediate(EnemyUnit z, float rawDt)
        {
            if (z.tr == null) return;

            bool isCryoHardFrozen = z.cryoFreezeTimer > 0f && z.stunTimer > 0f;
            float animSpdMul = SpeedMultiplier * (isCryoHardFrozen ? 0f : (z.chillTimer > 0f ? 0.75f : 1f));
            bool isAttackingWindow = z.attackAnimTimer > 0f && !isCryoHardFrozen;
            z.flipbook?.ApplyTo(z.sr, rawDt, moving: z.isMoving && !isCryoHardFrozen, attacking: isAttackingWindow, speedMul: animSpdMul);

            bool hasStrip = z.flipbook != null && z.flipbook.IsLoaded;
            z.baseScale = ResolveEnemyVisualScale(z.kind, z.flipbook);

            Vector2 displayPos = z.pos;
            if (z.grappleVisualTimer > 0f)
            {
                z.grappleVisualTimer = Mathf.Max(0f, z.grappleVisualTimer - rawDt * SpeedMultiplier);
                float gu = 1f - Mathf.Clamp01(z.grappleVisualTimer / Mathf.Max(0.04f, z.grappleVisualDur));
                displayPos = Vector2.Lerp(z.grappleVisualFrom, z.pos, gu);
            }

            float hopY = 0f;
            float tiltDeg = 0f;
            if (z.jumpPhase == 2)
            {
                float u = Mathf.Clamp01(z.jumpTimer / 0.62f);
                hopY = Mathf.Sin(u * Mathf.PI) * 2.15f;
                tiltDeg = Mathf.Lerp(18f, -22f, u);
            }
            else if (isCryoHardFrozen)
            {
                tiltDeg = 0f;
            }
            else if (z.stunTimer > 0f)
            {
                tiltDeg = Mathf.Sin(ElapsedBattleSec * 36f) * 11f;
            }
            else if (z.isMoving || isAttackingWindow)
            {
                float w = z.phase * (z.kind == EnemyKind.FastRunner ? 11.5f : 7.2f);
                float damp = hasStrip ? 0.36f : 1f;
                hopY = Mathf.Abs(Mathf.Sin(w)) * (z.kind == EnemyKind.GiantBoss ? 0.055f : 0.042f) * damp;
                tiltDeg = Mathf.Sin(w) * (z.kind == EnemyKind.GiantBoss ? 3.5f : 5.5f) * damp;
            }

            Vector3 bodyLocal = new Vector3(displayPos.x + z.lungeOffset.x, displayPos.y + z.lungeOffset.y + hopY, 0f);
            z.tr.localPosition = bodyLocal;
            z.tr.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
            z.tr.localScale = new Vector3(z.baseScale, z.baseScale, 1f);

            if (z.bars != null && z.bars.root != null)
            {
                float invScale = 1f / Mathf.Max(0.35f, z.baseScale);
                z.bars.root.localPosition = new Vector3(0f, z.barYOffset * invScale, 0f);
                z.bars.root.localRotation = Quaternion.Euler(0f, 0f, -tiltDeg);
                z.bars.root.localScale = new Vector3(invScale, invScale, 1f);
            }

            // 若跳尸跃至英雄左侧，自动翻转朝右扑咬后排！
            HeroUnit lookHero = PickTargetForEnemy(z);
            if (lookHero != null && Mathf.Abs(lookHero.pos.x - z.pos.x) > 0.08f)
                z.sr.flipX = lookHero.pos.x > z.pos.x;

            int order = Mathf.RoundToInt(-displayPos.y * 100f) + 505;
            z.sr.sortingOrder = order;

            Color baseCol = z.kind == EnemyKind.BacklineJumper
                ? new Color(1f, 0.80f, 0.85f, 1f)
                : (z.isElementalMutant && z.kind != EnemyKind.GiantBoss
                    ? new Color(0.76f, 1f, 0.72f, 1f)
                    : (!hasStrip && z.kind == EnemyKind.HardhatArmored ? new Color(0.88f, 0.94f, 1f, 1f) : Color.white));
            if (z.defenseShredTimer > 0f || z.asaArmorShredTimer > 0f)
                baseCol = Color.Lerp(baseCol, new Color(1f, 0.62f, 0.40f, 1f), 0.45f);
            if (isCryoHardFrozen)
                baseCol = Color.Lerp(baseCol, new Color(0.38f, 0.88f, 1f, 1f), 0.68f);
            else if (z.chillTimer > 0f)
                baseCol = Color.Lerp(baseCol, new Color(0.52f, 0.90f, 1f, 1f), 0.42f);
            z.sr.color = z.hitFlashTimer > 0f ? new Color(1f, 0.42f, 0.38f, 1f) : baseCol;

            if (z.shadowTr != null)
            {
                z.shadowTr.localPosition = new Vector3(displayPos.x, displayPos.y - 0.02f, 0f);
                float shScale = z.jumpPhase == 2 ? 0.65f : 1f;
                bool boss = z.kind == EnemyKind.GiantBoss;
                z.shadowTr.localScale = new Vector3((boss ? 1.08f : 0.58f) * shScale, (boss ? 0.36f : 0.22f) * shScale, 1f);
            }

            if (z.bossAuraTr != null)
            {
                z.bossAuraTr.localPosition = new Vector3(z.pos.x, z.pos.y - 0.01f, 0f);
                float pulse = 1.42f + Mathf.Sin(ElapsedBattleSec * 5.5f) * 0.10f;
                z.bossAuraTr.localScale = new Vector3(pulse, pulse * 0.46f, 1f);
            }

            UpdateOverheadBarRig(z.bars, z.hp, z.maxHp, 0f, 0f, showRage: false);
        }

        void TickPostBattleIdle(float dt)
        {
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroUnit h = _heroes[i];
                if (h.alive && h.puppet != null)
                    h.puppet.Tick(dt);
            }
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (z.alive && z.sr != null && z.flipbook != null)
                    z.flipbook.ApplyTo(z.sr, dt, moving: false, attacking: false, speedMul: 1f);
            }
            TickVisualOnlyPopupsAndFx(dt);
        }

        // =====================================================================
        // 舞台场景层级、背景贴图、3×3 九宫格粉笔标线与特效渲染
        // =====================================================================
        void EnsureStageHierarchy()
        {
            EnsureSharedSprites();
            if (_stageRoot != null) return;

            _stageRoot = new GameObject("ForestTacticalArenaStage");
            _stageRoot.transform.position = ArenaCenter;

            _bgRoot = MakeLayer(_stageRoot.transform, "L0_ArenaBackdrop");
            _decalRoot = MakeLayer(_stageRoot.transform, "L1_TacticalGridDecals");
            _hazardRoot = MakeLayer(_stageRoot.transform, "L2_RoadHazards");
            _shadowRoot = MakeLayer(_stageRoot.transform, "L3_UnitShadows");
            _unitRoot = MakeLayer(_stageRoot.transform, "L4_CombatUnits");
            _projRoot = MakeLayer(_stageRoot.transform, "L5_Projectiles");
            _fxRoot = MakeLayer(_stageRoot.transform, "L6_CombatFxAndNumbers");

            var textCanvasGo = new GameObject("L7_WorldTextCanvas", typeof(RectTransform));
            textCanvasGo.transform.SetParent(_fxRoot, false);
            _worldTextCanvas = textCanvasGo.AddComponent<Canvas>();
            _worldTextCanvas.renderMode = RenderMode.WorldSpace;
            _worldTextCanvas.sortingOrder = 3650;
            _worldTextCanvasRt = textCanvasGo.GetComponent<RectTransform>();
            _worldTextCanvasRt.sizeDelta = new Vector2(800f, 1400f);
            _worldTextCanvasRt.localPosition = Vector3.zero;
            _worldTextCanvasRt.localScale = new Vector3(0.01f, 0.01f, 1f);

            // 底层程序化断桥公路防线兜底背景（sortingOrder = -520，防止手绘背景尚未导入时黑屏）
            var fbGo = new GameObject("FallbackRoadBackdrop");
            fbGo.transform.SetParent(_bgRoot, false);
            _fallbackBgSr = fbGo.AddComponent<SpriteRenderer>();
            _fallbackBgSr.sprite = _spFallbackArenaBg;
            _fallbackBgSr.sortingOrder = -520;

            // 正式手绘《疯狂水世界》式断桥公路战场背景（sortingOrder = -500）
            var bgGo = new GameObject("BattleArenaArtBg");
            bgGo.transform.SetParent(_bgRoot, false);
            _artBgSr = bgGo.AddComponent<SpriteRenderer>();
            _artBgSr.sortingOrder = -500;

            // 左侧 3×3 九宫格粉笔战术阵位框（x = -3.30..-0.40, y = -2.50..+1.80）
            for (int slot = 0; slot < 9; slot++)
            {
                int r = slot / 3;
                int c = slot % 3;
                var dGo = new GameObject($"TacticalSlotDecal_{r}_{c}");
                dGo.transform.SetParent(_decalRoot, false);
                dGo.transform.localPosition = new Vector3(ColLocalX[c], RowLocalY[r] + 0.04f, 0f);
                dGo.transform.localScale = new Vector3(0.92f, 1.18f, 1f);
                var dSr = dGo.AddComponent<SpriteRenderer>();
                dSr.sprite = _spGridBracket;
                dSr.sortingOrder = -460;
                _slotDecalSrs[slot] = dSr;
            }

            // 前线接战分界虚线标尺
            var lineGo = new GameObject("ScrimmageChalkLine");
            lineGo.transform.SetParent(_decalRoot, false);
            lineGo.transform.localPosition = new Vector3(-0.28f, -0.35f, 0f);
            lineGo.transform.localScale = new Vector3(0.05f, 4.15f, 1f);
            _scrimmageLineSr = lineGo.AddComponent<SpriteRenderer>();
            _scrimmageLineSr.sprite = _spWhiteQuad;
            _scrimmageLineSr.color = new Color(1f, 0.92f, 0.72f, 0.45f);
            _scrimmageLineSr.sortingOrder = -462;
        }

        void RefreshBackdropAndGridDecals()
        {
            Sprite artSp = LoadArenaBackgroundSprite();
            if (_artBgSr != null)
            {
                _artBgSr.sprite = artSp;
                _artBgSr.enabled = artSp != null;
            }

            RefreshBackdropAspectScale(ResolveCamera());

            for (int slot = 0; slot < 9; slot++)
            {
                var buff = _sim.GetFormationSlotBuff(_sim.SelectedFormation, slot);
                _slotDecalHighlighted[slot] = buff.highlighted;
            }
            UpdateGridDecalFade();
        }

        static float ComputeArenaOrthoSize(Camera cam, out float safeAspect)
        {
            const float targetHalfWidth = 3.6f;
            float rawAspect = (cam != null && cam.aspect > 0.1f) ? cam.aspect : (720f / 1280f);
            safeAspect = Mathf.Max(0.42f, rawAspect);
            return Mathf.Max(ArenaOrthoSize, targetHalfWidth / safeAspect);
        }

        void RefreshBackdropAspectScale(Camera cam)
        {
            float ortho = ComputeArenaOrthoSize(cam, out float aspect);
            float rawAspect = (cam != null && cam.aspect > 0.1f) ? cam.aspect : aspect;
            float coverAspect = Mathf.Max(aspect, rawAspect);
            float viewH = ortho * 2f + 0.28f;
            float viewW = ortho * coverAspect * 2f + 0.28f;

            ScaleSpriteToCover(_fallbackBgSr, viewW, viewH);
            if (_artBgSr != null && _artBgSr.enabled && _artBgSr.sprite != null)
                ScaleSpriteToCover(_artBgSr, viewW, viewH);
        }

        static Sprite _cachedArtArenaBgSp;
        static Texture2D _cachedArtArenaBgTex;

        static void ScaleSpriteToCover(SpriteRenderer sr, float targetW, float targetH)
        {
            if (sr == null || sr.sprite == null) return;
            Vector2 spSize = sr.sprite.bounds.size;
            if (spSize.x < 0.001f || spSize.y < 0.001f) return;
            float sx = targetW / spSize.x;
            float sy = targetH / spSize.y;
            float uniform = Mathf.Max(sx, sy);
            sr.transform.localScale = new Vector3(uniform, uniform, 1f);
            Vector3 bc = sr.sprite.bounds.center;
            sr.transform.localPosition = new Vector3(-bc.x * uniform, -bc.y * uniform, 0f);
        }

        static Sprite LoadArenaBackgroundSprite()
        {
            string[] paths = { "ForestArt/battle_arena_bg", "ForestSlice/battle_arena_bg" };
            for (int i = 0; i < paths.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(paths[i]);
                if (tex != null)
                {
                    if (_cachedArtArenaBgSp != null && _cachedArtArenaBgTex == tex)
                        return _cachedArtArenaBgSp;

                    _cachedArtArenaBgTex = tex;
                    _cachedArtArenaBgSp = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    _cachedArtArenaBgSp.name = "battle_arena_bg_centered";
                    _cachedArtArenaBgSp.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    return _cachedArtArenaBgSp;
                }

                Sprite sp = Resources.Load<Sprite>(paths[i]);
                if (sp != null) return sp;
            }
            return null;
        }

        void UpdateGridDecalFade()
        {
            // 开战前 0.55s 高亮展示九宫格阵位 (82% alpha)，冲锋接战后平滑淡化至 25% alpha！
            float t = Mathf.Clamp01((ElapsedBattleSec - ClashIntroSec) / 0.55f);
            float alpha = Mathf.Lerp(0.82f, 0.25f, t);

            for (int slot = 0; slot < 9; slot++)
            {
                SpriteRenderer sr = _slotDecalSrs[slot];
                if (sr == null) continue;
                Color baseCol = _slotDecalHighlighted[slot]
                    ? new Color(1f, 0.86f, 0.36f, alpha)
                    : new Color(0.92f, 0.94f, 0.96f, alpha * 0.78f);
                sr.color = baseCol;
            }

            if (_scrimmageLineSr != null)
                _scrimmageLineSr.color = new Color(1f, 0.90f, 0.64f, Mathf.Lerp(0.45f, 0.16f, t));
        }

        OverheadBarRig CreateOverheadBars(Transform unitParent, bool isHero, float width, float yOffset)
        {
            var rootGo = new GameObject("OverheadBars");
            rootGo.transform.SetParent(unitParent, false);
            float parentScale = Mathf.Max(0.35f, Mathf.Abs(unitParent.localScale.x));
            float invScale = 1f / parentScale;
            rootGo.transform.localPosition = new Vector3(0f, yOffset * invScale, 0f);
            rootGo.transform.localScale = new Vector3(invScale, invScale, 1f);

            float barH = isHero ? 0.135f : 0.095f;
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(rootGo.transform, false);
            bgGo.transform.localScale = new Vector3(width + 0.04f, barH + 0.035f, 1f);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = _spWhiteQuad;
            bgSr.color = new Color(0.08f, 0.07f, 0.06f, 0.86f);
            bgSr.sortingOrder = 3200;

            float hpH = isHero ? 0.082f : 0.072f;
            float hpY = isHero ? 0.022f : 0f;
            var hpGo = new GameObject("HpFill");
            hpGo.transform.SetParent(rootGo.transform, false);
            hpGo.transform.localPosition = new Vector3(-width * 0.5f, hpY, 0f);
            hpGo.transform.localScale = new Vector3(width, hpH, 1f);
            var hpSr = hpGo.AddComponent<SpriteRenderer>();
            hpSr.sprite = _spWhiteLeftBar;
            hpSr.color = isHero ? new Color(0.30f, 0.90f, 0.38f, 1f) : new Color(0.94f, 0.26f, 0.24f, 1f);
            hpSr.sortingOrder = 3201;

            SpriteRenderer shieldSr = null;
            SpriteRenderer rageSr = null;
            if (isHero)
            {
                var shGo = new GameObject("ShieldFill");
                shGo.transform.SetParent(rootGo.transform, false);
                shGo.transform.localPosition = new Vector3(-width * 0.5f, hpY, 0f);
                shGo.transform.localScale = new Vector3(0f, hpH * 0.92f, 1f);
                shieldSr = shGo.AddComponent<SpriteRenderer>();
                shieldSr.sprite = _spWhiteLeftBar;
                shieldSr.color = new Color(0.94f, 0.98f, 1f, 0.92f);
                shieldSr.sortingOrder = 3202;

                var rgGo = new GameObject("RageFill");
                rgGo.transform.SetParent(rootGo.transform, false);
                rgGo.transform.localPosition = new Vector3(-width * 0.5f, -0.038f, 0f);
                rgGo.transform.localScale = new Vector3(width * 0.3f, 0.042f, 1f);
                rageSr = rgGo.AddComponent<SpriteRenderer>();
                rageSr.sprite = _spWhiteLeftBar;
                rageSr.color = new Color(1f, 0.78f, 0.18f, 1f);
                rageSr.sortingOrder = 3201;
            }

            return new OverheadBarRig
            {
                root = rootGo.transform,
                hpFill = hpSr,
                shieldFill = shieldSr,
                rageFill = rageSr,
                innerWidth = width,
            };
        }

        void UpdateOverheadBarRig(OverheadBarRig rig, float hp, float maxHp, float shield, float rage01, bool showRage)
        {
            if (rig == null || rig.root == null) return;
            float w = rig.innerWidth;
            float hpRatio = Mathf.Clamp01(hp / Mathf.Max(1f, maxHp));
            if (rig.hpFill != null)
            {
                Vector3 sc = rig.hpFill.transform.localScale;
                sc.x = w * hpRatio;
                rig.hpFill.transform.localScale = sc;
            }

            if (rig.shieldFill != null)
            {
                float shRatio = Mathf.Clamp01(shield / Mathf.Max(1f, maxHp));
                rig.shieldFill.enabled = shRatio > 0.005f;
                if (rig.shieldFill.enabled)
                {
                    Vector3 sc = rig.shieldFill.transform.localScale;
                    sc.x = w * shRatio;
                    rig.shieldFill.transform.localScale = sc;
                }
            }

            if (showRage && rig.rageFill != null)
            {
                float rRatio = Mathf.Clamp01(rage01);
                Vector3 sc = rig.rageFill.transform.localScale;
                sc.x = w * rRatio;
                rig.rageFill.transform.localScale = sc;
                rig.rageFill.color = rRatio >= 0.999f
                    ? new Color(1f, 0.95f, 0.48f, 1f)
                    : new Color(1f, 0.76f, 0.16f, 1f);
            }
        }

        Transform CreateRoadShadow(string name, Vector2 localPos, float sx, float sy)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_shadowRoot, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y - 0.02f, 0f);
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spShadowOval;
            sr.color = new Color(0.08f, 0.06f, 0.05f, 0.44f);
            sr.sortingOrder = -420;
            return go.transform;
        }

        void SpawnTransientFx(Sprite sp, Vector2 fromLocal, Vector2 toLocal, float startScale, float endScale, Color col, float dur, int sortOrder)
        {
            if (_isFastSimulating || sp == null || _fxRoot == null) return;
            if (_transientFxs.Count >= 36) return;

            var go = new GameObject("ArenaFx");
            go.transform.SetParent(_fxRoot, false);
            go.transform.localPosition = new Vector3(fromLocal.x, fromLocal.y, 0f);
            go.transform.localScale = new Vector3(startScale, startScale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = col;
            sr.sortingOrder = sortOrder;

            _transientFxs.Add(new TransientFx
            {
                tr = go.transform,
                sr = sr,
                startLocal = fromLocal,
                endLocal = toLocal,
                startScale = startScale,
                endScale = endScale,
                color = col,
                elapsed = 0f,
                duration = Mathf.Max(0.08f, dur),
            });
        }

        void SpawnWorldNumber(Vector2 localPos, int value, bool isHeal, bool isCrit)
        {
            if (_isFastSimulating || _fxRoot == null || _numberPopups.Count >= 22) return;

            string text = (isHeal ? "+" : "-") + Mathf.Max(1, value).ToString();
            Color col = isHeal
                ? new Color(0.32f, 1f, 0.54f, 1f)
                : (isCrit ? new Color(1f, 0.84f, 0.24f, 1f) : new Color(1f, 0.96f, 0.90f, 1f));

            float jitterX = ((float)_battleRng.NextDouble() - 0.5f) * 0.22f;
            Vector2 start = localPos + new Vector2(jitterX, 0f);

            var rootGo = new GameObject("ArenaWorldNum");
            rootGo.transform.SetParent(_fxRoot, false);
            rootGo.transform.localPosition = new Vector3(start.x, start.y, 0f);
            float scale = isCrit ? 1.25f : 1.0f;
            rootGo.transform.localScale = new Vector3(scale, scale, 1f);

            var popup = new WorldNumberPopup
            {
                root = rootGo.transform,
                startLocal = start,
                elapsed = 0f,
                duration = 0.62f,
                baseColor = col,
            };

            const float stepX = 0.14f;
            float totalW = (text.Length - 1) * stepX;
            for (int c = 0; c < text.Length; c++)
            {
                if (!_glyphSprites.TryGetValue(text[c], out Sprite gSp) || gSp == null) continue;
                var gGo = new GameObject("g");
                gGo.transform.SetParent(rootGo.transform, false);
                gGo.transform.localPosition = new Vector3(-totalW * 0.5f + c * stepX, 0f, 0f);
                var gSr = gGo.AddComponent<SpriteRenderer>();
                gSr.sprite = gSp;
                gSr.color = col;
                gSr.sortingOrder = 3600;
                popup.glyphRenderers.Add(gSr);
            }

            _numberPopups.Add(popup);
        }

        void SpawnWorldFloatingText(Vector2 localPos, string content, Color textColor, float scale = 1.0f)
        {
            if (_isFastSimulating || _worldTextCanvasRt == null || string.IsNullOrEmpty(content) || _textPopups.Count >= 16)
                return;

            if (_worldTextCanvas != null && _worldTextCanvas.worldCamera == null)
                _worldTextCanvas.worldCamera = ResolveCamera();

            var go = new GameObject("ArenaFloatText", typeof(RectTransform));
            go.transform.SetParent(_worldTextCanvasRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 58f);
            rt.anchoredPosition = localPos * 100f;
            rt.localScale = new Vector3(scale, scale, 1f);

            var txt = go.AddComponent<Text>();
            txt.font = ForestComicUiSkin.UiFont;
            txt.fontSize = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            txt.text = content;
            txt.color = textColor;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.06f, 0.12f, 0.96f);
            outline.effectDistance = new Vector2(2.2f, -2.2f);

            _textPopups.Add(new WorldTextPopup
            {
                rt = rt,
                textComp = txt,
                startLocal = localPos,
                baseColor = textColor,
                baseScale = scale,
                elapsed = 0f,
                duration = 0.86f,
            });
        }

        void SpawnGrappleChainFx(HeroUnit asa, EnemyUnit target, Vector2 grabOrigin, float duration)
        {
            if (_isFastSimulating || _fxRoot == null || asa == null || target == null) return;
            if (_segmentFxs.Count >= 36) return;

            Vector2 fromPt = asa.pos + new Vector2(0.28f, 0.38f);
            Vector2 initTo = grabOrigin + new Vector2(0f, 0.34f);

            // 留下一道精钢链条拉拽高速残影轨迹
            SpawnSegmentLineFx(_spChainSegment, fromPt, initTo, new Color(1f, 0.56f, 0.36f, 0.65f), 0.72f, 0.18f, sortOrder: 2885);

            var chainGo = new GameObject("AsaGrappleChain");
            chainGo.transform.SetParent(_fxRoot, false);
            var chainSr = chainGo.AddComponent<SpriteRenderer>();
            chainSr.sprite = _spChainSegment != null ? _spChainSegment : _spWhiteLeftBar;
            chainSr.color = new Color(0.92f, 0.96f, 1f, 0.98f);
            chainSr.sortingOrder = 2890;

            var clawGo = new GameObject("AsaGrappleClawHead");
            clawGo.transform.SetParent(_fxRoot, false);
            clawGo.transform.localScale = new Vector3(0.84f, 0.84f, 1f);
            var clawSr = clawGo.AddComponent<SpriteRenderer>();
            clawSr.sprite = _spHookClaw != null ? _spHookClaw : _spWhiteQuad;
            clawSr.color = new Color(1f, 0.48f, 0.34f, 1f);
            clawSr.sortingOrder = 2895;

            var seg = new ArenaSegmentFx
            {
                tr = chainGo.transform,
                sr = chainSr,
                tipTr = clawGo.transform,
                tipSr = clawSr,
                anchorHero = asa,
                targetEnemy = target,
                fixedFrom = fromPt,
                fixedTo = initTo,
                trackUnits = true,
                thickness = 0.92f,
                color = new Color(0.92f, 0.96f, 1f, 0.98f),
                elapsed = 0f,
                duration = Mathf.Max(0.16f, duration),
            };
            UpdateSegmentTransform(seg, fromPt, initTo);
            _segmentFxs.Add(seg);
        }

        void SpawnChainLightningArcFx(Vector2 fromLocal, Vector2 toLocal)
        {
            SpawnSegmentLineFx(_spLightningSeg, fromLocal, toLocal, new Color(0.62f, 0.38f, 1f, 0.90f), 1.38f, 0.26f, sortOrder: 2910);
            SpawnSegmentLineFx(_spLightningSeg, fromLocal, toLocal, new Color(0.62f, 0.96f, 1f, 1f), 0.82f, 0.24f, sortOrder: 2915);
        }

        void SpawnSegmentLineFx(Sprite sp, Vector2 fromLocal, Vector2 toLocal, Color col, float thickness, float dur, int sortOrder)
        {
            if (_isFastSimulating || _fxRoot == null || _segmentFxs.Count >= 40) return;

            var go = new GameObject("ArenaSegFx");
            go.transform.SetParent(_fxRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp != null ? sp : _spWhiteLeftBar;
            sr.color = col;
            sr.sortingOrder = sortOrder;

            var seg = new ArenaSegmentFx
            {
                tr = go.transform,
                sr = sr,
                fixedFrom = fromLocal,
                fixedTo = toLocal,
                trackUnits = false,
                thickness = thickness,
                color = col,
                elapsed = 0f,
                duration = Mathf.Max(0.08f, dur),
            };
            UpdateSegmentTransform(seg, fromLocal, toLocal);
            _segmentFxs.Add(seg);
        }

        static void UpdateSegmentTransform(ArenaSegmentFx seg, Vector2 fromPt, Vector2 toPt)
        {
            if (seg == null || seg.tr == null || seg.sr == null || seg.sr.sprite == null) return;
            Vector2 delta = toPt - fromPt;
            float dist = Mathf.Max(0.05f, delta.magnitude);
            float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float spriteW = Mathf.Max(0.01f, seg.sr.sprite.bounds.size.x);

            seg.tr.localPosition = new Vector3(fromPt.x, fromPt.y, 0f);
            seg.tr.localRotation = Quaternion.Euler(0f, 0f, ang);
            seg.tr.localScale = new Vector3(dist / spriteW, seg.thickness, 1f);

            if (seg.tipTr != null)
            {
                seg.tipTr.localPosition = new Vector3(toPt.x, toPt.y, 0f);
                seg.tipTr.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        void TickVisualOnlyPopupsAndFx(float dt)
        {
            for (int i = _transientFxs.Count - 1; i >= 0; i--)
            {
                TransientFx fx = _transientFxs[i];
                fx.elapsed += dt;
                float u = Mathf.Clamp01(fx.elapsed / fx.duration);
                if (fx.tr != null && fx.sr != null)
                {
                    Vector2 p = Vector2.Lerp(fx.startLocal, fx.endLocal, u);
                    float s = Mathf.Lerp(fx.startScale, fx.endScale, u);
                    fx.tr.localPosition = new Vector3(p.x, p.y, 0f);
                    fx.tr.localScale = new Vector3(s, s, 1f);
                    Color c = fx.color;
                    c.a *= (1f - u);
                    fx.sr.color = c;
                }
                if (u >= 1f)
                {
                    if (fx.tr != null) DestroyObj(fx.tr.gameObject);
                    _transientFxs.RemoveAt(i);
                }
            }

            for (int i = _numberPopups.Count - 1; i >= 0; i--)
            {
                WorldNumberPopup np = _numberPopups[i];
                np.elapsed += dt;
                float u = Mathf.Clamp01(np.elapsed / np.duration);
                if (np.root != null)
                {
                    Vector2 p = np.startLocal + new Vector2(0f, u * 0.48f);
                    np.root.localPosition = new Vector3(p.x, p.y, 0f);
                    float alpha = 1f - Mathf.Clamp01((u - 0.55f) / 0.45f);
                    for (int g = 0; g < np.glyphRenderers.Count; g++)
                    {
                        if (np.glyphRenderers[g] != null)
                        {
                            Color c = np.baseColor;
                            c.a = alpha;
                            np.glyphRenderers[g].color = c;
                        }
                    }
                }
                if (u >= 1f)
                {
                    if (np.root != null) DestroyObj(np.root.gameObject);
                    _numberPopups.RemoveAt(i);
                }
            }

            for (int i = _segmentFxs.Count - 1; i >= 0; i--)
            {
                ArenaSegmentFx seg = _segmentFxs[i];
                seg.elapsed += dt;
                float u = Mathf.Clamp01(seg.elapsed / seg.duration);
                if (seg.tr != null && seg.sr != null)
                {
                    Vector2 fromPt = seg.fixedFrom;
                    Vector2 toPt = seg.fixedTo;
                    if (seg.trackUnits)
                    {
                        if (seg.anchorHero != null && seg.anchorHero.alive)
                            fromPt = seg.anchorHero.pos + new Vector2(0.28f, 0.38f);
                        if (seg.targetEnemy != null && seg.targetEnemy.alive && seg.targetEnemy.tr != null)
                            toPt = (Vector2)seg.targetEnemy.tr.localPosition + new Vector2(0f, 0.34f);
                        else if (seg.targetEnemy != null)
                            toPt = seg.targetEnemy.pos + new Vector2(0f, 0.34f);
                        UpdateSegmentTransform(seg, fromPt, toPt);
                    }

                    float fade = 1f - Mathf.Clamp01((u - 0.35f) / 0.65f);
                    Color c = seg.color;
                    c.a *= fade;
                    seg.sr.color = c;
                    if (seg.tipSr != null)
                    {
                        Color tc = seg.tipSr.color;
                        tc.a = fade;
                        seg.tipSr.color = tc;
                    }
                }
                if (u >= 1f)
                {
                    if (seg.tipTr != null) DestroyObj(seg.tipTr.gameObject);
                    if (seg.tr != null) DestroyObj(seg.tr.gameObject);
                    _segmentFxs.RemoveAt(i);
                }
            }

            for (int i = _textPopups.Count - 1; i >= 0; i--)
            {
                WorldTextPopup tp = _textPopups[i];
                tp.elapsed += dt;
                float u = Mathf.Clamp01(tp.elapsed / tp.duration);
                if (tp.rt != null && tp.textComp != null)
                {
                    Vector2 curLocal = tp.startLocal + new Vector2(0f, u * 0.54f);
                    tp.rt.anchoredPosition = curLocal * 100f;
                    float pop = u < 0.14f ? Mathf.Lerp(1.24f, 1.0f, u / 0.14f) : 1.0f;
                    tp.rt.localScale = Vector3.one * (tp.baseScale * pop);
                    float alpha = 1f - Mathf.Clamp01((u - 0.58f) / 0.42f);
                    Color c = tp.baseColor;
                    c.a = alpha;
                    tp.textComp.color = c;
                }
                if (u >= 1f)
                {
                    if (tp.rt != null) DestroyObj(tp.rt.gameObject);
                    _textPopups.RemoveAt(i);
                }
            }

            if (_camShakeTimer > 0f)
                _camShakeTimer = Mathf.Max(0f, _camShakeTimer - dt);
        }

        void TriggerMicroShake(float dur, float mag)
        {
            if (_isFastSimulating) return;
            _camShakeTimer = Mathf.Max(_camShakeTimer, dur);
            _camShakeMag = Mathf.Max(_camShakeMag, mag);
        }

        void LockCameraToArena(Camera cam)
        {
            if (cam == null || !IsBattleActive) return;
            Vector3 shake = Vector3.zero;
            if (_camShakeTimer > 0f && !IsResultPending)
            {
                float s = _camShakeMag * (_camShakeTimer / 0.22f);
                shake = new Vector3(
                    Mathf.Sin(ElapsedBattleSec * 65f) * s,
                    Mathf.Cos(ElapsedBattleSec * 53f) * s,
                    0f);
            }
            float ortho = ComputeArenaOrthoSize(cam, out _);
            cam.orthographicSize = ortho;
            cam.backgroundColor = new Color32(34, 30, 26, 255);
            cam.transform.position = new Vector3(ArenaCenter.x + shake.x, ArenaCenter.y + shake.y, -10f);
            if (_world != null && _world.View != null)
                _world.View.SyncCameraOverlays();
        }

        Camera ResolveCamera()
        {
            if (_cam != null) return _cam;
            return Camera.main;
        }

        void ClearTransientVisualsAfterSkip()
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
                if (_projectiles[i].tr != null) DestroyObj(_projectiles[i].tr.gameObject);
            _projectiles.Clear();

            for (int i = _firePools.Count - 1; i >= 0; i--)
                if (_firePools[i].tr != null) DestroyObj(_firePools[i].tr.gameObject);
            _firePools.Clear();

            for (int i = _frostZones.Count - 1; i >= 0; i--)
                if (_frostZones[i].tr != null) DestroyObj(_frostZones[i].tr.gameObject);
            _frostZones.Clear();

            _chainStorms.Clear();

            for (int i = _segmentFxs.Count - 1; i >= 0; i--)
            {
                if (_segmentFxs[i].tipTr != null) DestroyObj(_segmentFxs[i].tipTr.gameObject);
                if (_segmentFxs[i].tr != null) DestroyObj(_segmentFxs[i].tr.gameObject);
            }
            _segmentFxs.Clear();

            for (int i = _textPopups.Count - 1; i >= 0; i--)
                if (_textPopups[i].rt != null) DestroyObj(_textPopups[i].rt.gameObject);
            _textPopups.Clear();

            for (int i = _transientFxs.Count - 1; i >= 0; i--)
                if (_transientFxs[i].tr != null) DestroyObj(_transientFxs[i].tr.gameObject);
            _transientFxs.Clear();
        }

        void DestroyEnemyVisuals(EnemyUnit z)
        {
            if (z.warningRingTr != null) DestroyObj(z.warningRingTr.gameObject);
            if (z.bossAuraTr != null) DestroyObj(z.bossAuraTr.gameObject);
            if (z.shadowTr != null) DestroyObj(z.shadowTr.gameObject);
            if (z.tr != null) DestroyObj(z.tr.gameObject);
            z.warningRingTr = null;
            z.bossAuraTr = null;
            z.shadowTr = null;
            z.tr = null;
        }

        void ClearCombatEntities()
        {
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].shadowTr != null) DestroyObj(_heroes[i].shadowTr.gameObject);
                if (_heroes[i].puppet != null && _heroes[i].puppet.Root != null)
                    DestroyObj(_heroes[i].puppet.Root.gameObject);
            }
            _heroes.Clear();
            _heroStates.Clear();

            for (int i = 0; i < _aliveEnemies.Count; i++)
                DestroyEnemyVisuals(_aliveEnemies[i]);
            _aliveEnemies.Clear();
            _spawnQueue.Clear();
            _queuedHpSum = 0f;

            ClearTransientVisualsAfterSkip();

            for (int i = _numberPopups.Count - 1; i >= 0; i--)
                if (_numberPopups[i].root != null) DestroyObj(_numberPopups[i].root.gameObject);
            _numberPopups.Clear();
        }

        static Transform MakeLayer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Vector2 Rotate2D(Vector2 v, float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static void DestroyObj(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        // =====================================================================
        // 程序化高质感辅助图元（血条/阴影/九宫格粉笔角标/红圈预警/剑气/数字/兜底公路舞台）
        // =====================================================================
        static void EnsureSharedSprites()
        {
            if (_spWhiteQuad == null) _spWhiteQuad = BuildSolidSprite(new Vector2(0.5f, 0.5f), "ArenaWhiteCenter");
            if (_spWhiteLeftBar == null) _spWhiteLeftBar = BuildSolidSprite(new Vector2(0f, 0.5f), "ArenaWhiteLeftBar");
            if (_spShadowOval == null) _spShadowOval = BuildSoftOvalSprite(64);
            if (_spGridBracket == null) _spGridBracket = BuildTacticalBracketSprite(96);
            if (_spWarningRing == null) _spWarningRing = BuildWarningRingSprite(96);
            if (_spShockRing == null) _spShockRing = BuildShockRingSprite(96);
            if (_spCleaveWave == null) _spCleaveWave = BuildCleaveCrescentSprite(96);
            if (_spBossCrown == null) _spBossCrown = BuildBossCrownSprite(48);
            if (_spFallbackArenaBg == null) _spFallbackArenaBg = BuildFallbackRoadArenaSprite(180, 320);
            if (_spChainSegment == null) _spChainSegment = BuildChainSegmentSprite(96, 20);
            if (_spHookClaw == null) _spHookClaw = BuildHookClawSprite(48);
            if (_spFrostCircle == null) _spFrostCircle = BuildFrostCircleSprite(128);
            if (_spLightningSeg == null) _spLightningSeg = BuildLightningSegmentSprite(96, 28);
            if (_glyphSprites.Count == 0) BuildBitmapNumberGlyphs();
        }

        static Sprite BuildChainSegmentSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y + 0.5f - cy);
                for (int x = 0; x < w; x++)
                {
                    int linkPhase = (x / 8) % 2;
                    int lx = x % 8;
                    bool isOpenLink = linkPhase == 0;
                    if (isOpenLink)
                    {
                        bool inRing = dy <= 5.8f && (dy >= 2.2f || lx <= 1 || lx >= 6);
                        if (!inRing) continue;
                        bool edge = dy >= 4.6f || lx == 0 || lx == 7;
                        px[y * w + x] = edge ? new Color32(28, 32, 42, 250) : new Color32(228, 238, 250, 255);
                    }
                    else
                    {
                        if (dy > 3.2f) continue;
                        bool edge = dy >= 2.2f;
                        px[y * w + x] = edge ? new Color32(28, 32, 42, 250) : new Color32(255, 198, 176, 255);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildHookClawSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - h, y + 0.5f - h) / h;
                    float spine = (p.x >= -0.75f && p.x <= 0.55f && Mathf.Abs(p.y) <= 0.16f) ? 1f : 0f;
                    float upperProng = (p.x >= -0.15f && p.x <= 0.78f && Mathf.Abs(p.y - (0.52f - Mathf.Abs(p.x - 0.30f) * 0.85f)) <= 0.15f) ? 1f : 0f;
                    float lowerProng = (p.x >= -0.15f && p.x <= 0.78f && Mathf.Abs(p.y + (0.52f - Mathf.Abs(p.x - 0.30f) * 0.85f)) <= 0.15f) ? 1f : 0f;
                    float spearTip = (p.x >= 0.25f && p.x <= 0.90f && Mathf.Abs(p.y) <= (0.92f - p.x) * 0.42f) ? 1f : 0f;
                    if (spine + upperProng + lowerProng + spearTip > 0.5f)
                    {
                        bool rim = Mathf.Abs(p.y) > 0.42f || p.x > 0.76f || p.x < -0.66f;
                        px[y * size + x] = rim ? new Color32(34, 24, 28, 255) : new Color32(250, 244, 240, 255);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildFrostCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 d = new Vector2(x + 0.5f - h, y + 0.5f - h) / h;
                    float r = d.magnitude;
                    if (r > 1f) continue;

                    float ang = Mathf.Atan2(d.y, d.x);
                    float star6 = Mathf.Abs(Mathf.Cos(ang * 3f));
                    float outerCrystal = Mathf.Clamp01(1f - Mathf.Abs(r - (0.82f + star6 * 0.10f)) / 0.08f);
                    float innerRing = Mathf.Clamp01(1f - Mathf.Abs(r - 0.46f) / 0.055f) * 0.85f;
                    float spoke = (star6 > 0.94f && r < 0.88f) ? 0.68f : 0f;
                    float fill = Mathf.Clamp01(1f - r * 1.05f) * 0.34f;
                    float a = Mathf.Clamp01(Mathf.Max(outerCrystal, Mathf.Max(innerRing, spoke)) + fill);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildLightningSegmentSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float cy = h * 0.5f;
            float[] knots = { 0f, 0.28f, -0.34f, 0.38f, -0.26f, 0.30f, -0.18f, 0f };
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float kf = u * (knots.Length - 1);
                int k0 = Mathf.Clamp(Mathf.FloorToInt(kf), 0, knots.Length - 2);
                float kt = kf - k0;
                float offsetNorm = Mathf.Lerp(knots[k0], knots[k0 + 1], kt);
                float boltY = cy + offsetNorm * (h * 0.36f);

                for (int y = 0; y < h; y++)
                {
                    float dist = Mathf.Abs(y + 0.5f - boltY);
                    if (dist > 6.5f) continue;
                    float core = Mathf.Clamp01(1f - dist / 2.2f);
                    float halo = Mathf.Clamp01(1f - dist / 6.5f) * 0.58f;
                    float a = Mathf.Clamp01(core + halo);
                    byte rgb = (byte)Mathf.RoundToInt(Mathf.Lerp(205f, 255f, core));
                    px[y * w + x] = new Color32(rgb, 245, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildSolidSprite(Vector2 pivot, string name)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, 4, 4), pivot, 4f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildSoftOvalSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + 0.5f - h, y + 0.5f - h).magnitude / h;
                    float a = Mathf.Clamp01(1f - r * r);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildTacticalBracketSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            int pad = 6;
            int arm = 18;
            int thick = 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inBox = x >= pad && x < size - pad && y >= pad && y < size - pad;
                    if (!inBox) continue;

                    int dx = Mathf.Min(x - pad, (size - 1 - pad) - x);
                    int dy = Mathf.Min(y - pad, (size - 1 - pad) - y);
                    bool corner = (dx < arm && dy < thick) || (dy < arm && dx < thick);
                    if (corner)
                        px[y * size + x] = new Color32(255, 255, 255, 235);
                    else
                        px[y * size + x] = new Color32(255, 255, 255, 28);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildWarningRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 d = new Vector2(x + 0.5f - h, y + 0.5f - h) / h;
                    float r = d.magnitude;
                    if (r > 1f) continue;
                    float outer = Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.08f);
                    float inner = Mathf.Clamp01(1f - Mathf.Abs(r - 0.48f) / 0.06f) * 0.75f;
                    float cross = (Mathf.Abs(d.x) < 0.045f || Mathf.Abs(d.y) < 0.045f) && r < 0.82f ? 0.55f : 0f;
                    float fill = (1f - r) * 0.22f;
                    float a = Mathf.Clamp01(Mathf.Max(outer, Mathf.Max(inner, cross)) + fill);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildShockRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + 0.5f - h, y + 0.5f - h).magnitude / h;
                    if (r > 1f) continue;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
                    float glow = Mathf.Clamp01(1f - r) * 0.18f;
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring + glow));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildCleaveCrescentSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - h, y + 0.5f - h) / h;
                    float r = p.magnitude;
                    float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (r > 0.36f && r < 0.96f && Mathf.Abs(ang) < 68f)
                    {
                        float aAng = 1f - Mathf.Abs(ang) / 68f;
                        float aRad = 1f - Mathf.Abs(r - 0.76f) / 0.22f;
                        float a = Mathf.Clamp01(aAng * 1.25f) * Mathf.Clamp01(aRad);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildBossCrownSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 6; y < size - 8; y++)
            {
                float v = (y - 6f) / (size - 14f);
                for (int x = 6; x < size - 6; x++)
                {
                    float u = (x - 6f) / (size - 12f);
                    float peakL = 1f - Mathf.Abs(u - 0.15f) * 3.8f;
                    float peakM = 1f - Mathf.Abs(u - 0.50f) * 3.2f;
                    float peakR = 1f - Mathf.Abs(u - 0.85f) * 3.8f;
                    float topH = Mathf.Max(0.32f, Mathf.Max(peakL, Mathf.Max(peakM, peakR)));
                    if (v <= topH)
                    {
                        bool border = v < 0.08f || v > topH - 0.09f || u < 0.06f || u > 0.94f;
                        px[y * size + x] = border ? new Color32(42, 24, 14, 255) : new Color32(255, 204, 48, 255);
                    }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static Sprite BuildFallbackRoadArenaSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];

            // y=0 为底边，y=h-1 为顶边：底部 20% 路缘石护栏、中部 56% 干净断桥柏油路面、顶部 24% 山口沙袋拒马远景
            int botCurbEnd = Mathf.RoundToInt(h * 0.20f);
            int roadEnd = Mathf.RoundToInt(h * 0.76f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 col;
                    if (y < botCurbEnd)
                    {
                        bool stripe = y >= botCurbEnd - 9 && y < botCurbEnd - 2 && ((x + y) / 8) % 2 == 0;
                        col = stripe ? new Color32(196, 148, 56, 255) : new Color32(48, 43, 38, 255);
                    }
                    else if (y < roadEnd)
                    {
                        float t = (y - botCurbEnd) / Mathf.Max(1f, roadEnd - botCurbEnd);
                        byte r = (byte)Mathf.Lerp(122f, 138f, Mathf.Sin(t * Mathf.PI));
                        byte g = (byte)Mathf.Lerp(112f, 126f, Mathf.Sin(t * Mathf.PI));
                        byte b = (byte)Mathf.Lerp(98f, 108f, Mathf.Sin(t * Mathf.PI));
                        // 公路中央斑驳黄色双虚线
                        bool centerDash = Mathf.Abs(y - (botCurbEnd + roadEnd) / 2) <= 2 && (x / 14) % 2 == 0;
                        col = centerDash ? new Color32(198, 168, 92, 255) : new Color32(r, g, b, 255);
                    }
                    else
                    {
                        bool parapet = y < roadEnd + 14;
                        col = parapet ? new Color32(82, 72, 60, 255) : new Color32(54, 48, 42, 255);
                    }
                    px[y * w + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 25f, 0, SpriteMeshType.FullRect);
            sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sp;
        }

        static void BuildBitmapNumberGlyphs()
        {
            string[] patterns =
            {
                // +
                "00000|00100|01110|00100|00000|00000|00000",
                // -
                "00000|00000|01110|00000|00000|00000|00000",
                // 0..9
                "01110|10001|10011|10101|11001|10001|01110",
                "00100|01100|00100|00100|00100|00100|01110",
                "01110|10001|00001|00110|01000|10000|11111",
                "11110|00001|00001|01110|00001|00001|11110",
                "00010|00110|01010|10010|11111|00010|00010",
                "11111|10000|11110|00001|00001|10001|01110",
                "01110|10000|11110|10001|10001|10001|01110",
                "11111|00001|00010|00100|01000|01000|01000",
                "01110|10001|10001|01110|10001|10001|01110",
                "01110|10001|10001|01111|00001|00001|01110",
            };
            char[] keys = { '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            const int gw = 5, gh = 7, scale = 3, border = 2;
            int texW = gw * scale + border * 2;
            int texH = gh * scale + border * 2;

            for (int k = 0; k < keys.Length; k++)
            {
                string[] rows = patterns[k].Split('|');
                var mask = new bool[texW, texH];

                for (int r = 0; r < gh; r++)
                {
                    int pyBase = (gh - 1 - r) * scale + border;
                    for (int c = 0; c < gw; c++)
                    {
                        if (rows[r][c] != '1') continue;
                        int pxBase = c * scale + border;
                        for (int dy = 0; dy < scale; dy++)
                            for (int dx = 0; dx < scale; dx++)
                                mask[pxBase + dx, pyBase + dy] = true;
                    }
                }

                var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[texW * texH];
                for (int y = 0; y < texH; y++)
                {
                    for (int x = 0; x < texW; x++)
                    {
                        if (mask[x, y])
                        {
                            px[y * texW + x] = new Color32(255, 255, 255, 255);
                            continue;
                        }
                        bool near = false;
                        for (int oy = -2; oy <= 2 && !near; oy++)
                        {
                            for (int ox = -2; ox <= 2; ox++)
                            {
                                int nx = x + ox, ny = y + oy;
                                if (nx >= 0 && nx < texW && ny >= 0 && ny < texH && mask[nx, ny])
                                {
                                    near = true;
                                    break;
                                }
                            }
                        }
                        if (near) px[y * texW + x] = new Color32(24, 18, 14, 240);
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                var sp = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 96f, 0, SpriteMeshType.FullRect);
                sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
                _glyphSprites[keys[k]] = sp;
            }
        }
    }
}
