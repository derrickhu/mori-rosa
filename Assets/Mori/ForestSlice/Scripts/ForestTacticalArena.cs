using System;
using System.Collections.Generic;
using UnityEngine;

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
            public float dmgRed;
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
            public string heroId;
            public string name;
            public int star;
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
            public float atk;
            public float dmgReduction;
            public float atkSpeedMul = 1f;
            public float critChance = 0.10f;
            public float critMult = 1.55f;
            public float rageGainMul = 1f;
            public float rage01;
            public float aggroWeight = 1f;

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
            public float dmgReduction;
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
        }

        sealed class ArenaProjectile
        {
            public ProjectileStyle style;
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
            public float knockback;
            public bool isCrit;
            public bool piercing;
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
            public Transform tr;
            public SpriteRenderer sr;
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

        SpriteRenderer _fallbackBgSr;
        SpriteRenderer _artBgSr;
        readonly SpriteRenderer[] _slotDecalSrs = new SpriteRenderer[9];
        readonly bool[] _slotDecalHighlighted = new bool[9];
        SpriteRenderer _scrimmageLineSr;

        readonly List<HeroUnit> _heroes = new List<HeroUnit>(6);
        readonly List<ArenaHeroHudState> _heroStates = new List<ArenaHeroHudState>(6);
        readonly List<QueuedEnemySpec> _spawnQueue = new List<QueuedEnemySpec>(40);
        readonly List<EnemyUnit> _aliveEnemies = new List<EnemyUnit>(32);
        readonly List<ArenaProjectile> _projectiles = new List<ArenaProjectile>(48);
        readonly List<ArenaFirePool> _firePools = new List<ArenaFirePool>(12);
        readonly List<TransientFx> _transientFxs = new List<TransientFx>(32);
        readonly List<WorldNumberPopup> _numberPopups = new List<WorldNumberPopup>(24);

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
            SyncAllUnitVisuals(0.016f);
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

            SyncAllUnitVisuals(scaledDt);
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

            // 3. 推进地面燃烧瓶火海持续灼烧
            StepFirePools(dt);
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
        // 英雄生成与动态战术 AI（冲锋近战 / 风筝射手 / 高抛火海 / 医疗群盾）
        // =====================================================================
        void SpawnDeployedHeroes()
        {
            var syn = _sim.GetCombinedSynergyTotals();

            for (int slot = 0; slot < 9 && slot < _sim.FormationSlots.Length; slot++)
            {
                int heroIdx = _sim.FormationSlots[slot];
                if (heroIdx < 0 || heroIdx >= _sim.Heroes.Count) continue;

                HeroSpec spec = _sim.Heroes[heroIdx];
                if (spec == null || spec.id == "ache") continue;

                int row = slot / 3;
                int col = slot % 3;
                Vector2 formLocal = new Vector2(ColLocalX[col], RowLocalY[row]);

                var slotBuff = _sim.GetFormationSlotBuff(_sim.SelectedFormation, slot);
                int roleId = (int)_sim.GetHeroCombatRole(heroIdx);
                bool melee = _sim.IsHeroMelee(heroIdx);
                string skillTitle = _sim.GetHeroSkillTitle(heroIdx);
                if (string.IsNullOrEmpty(skillTitle)) skillTitle = spec.skillName;

                float hpMul = Mathf.Max(0.5f, 1f + slotBuff.hpBonus + syn.hpMulBonus);
                float atkMul = Mathf.Max(0.5f, 1f + slotBuff.atkBonus + syn.atkMulBonus);
                float maxHp = Mathf.Max(80f, spec.MaxHp * hpMul);
                float atk = Mathf.Max(12f, spec.Attack * atkMul);
                float dmgRed = Mathf.Clamp(slotBuff.dmgReduction + syn.dmgRedBonus + (col == 2 ? 0.05f : 0f), 0f, 0.72f);
                float atkSpd = Mathf.Max(0.5f, 1f + slotBuff.atkSpeedBonus + syn.atkSpdBonus);
                float critChance = Mathf.Clamp(0.10f + slotBuff.critBonus * 0.55f + (roleId == 3 ? 0.12f : 0f), 0.05f, 0.75f);
                float critMult = 1.55f + slotBuff.critBonus;
                float rageMul = Mathf.Max(0.6f, 1f + syn.rageGainBonus);
                float startRage = Mathf.Clamp01(0.28f + slotBuff.startRage);

                string hid = !string.IsNullOrEmpty(spec.id) ? spec.id : spec.characterId;
                string cid = !string.IsNullOrEmpty(spec.characterId) ? spec.characterId : hid;
                PuppetRigProfile rigProf = ForestSurvivalWorld.ResolveCompanionRigProfile(hid, cid);
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

                float aggro = col == 2 ? 1.8f : (col == 1 ? 1.15f : 0.75f);
                if (roleId == 1 || hid == "dachui") aggro += 1.6f; // 大锤天生嘲讽吸怪

                var unit = new HeroUnit
                {
                    heroIdx = heroIdx,
                    slot0to8 = slot,
                    row = row,
                    col = col,
                    roleId = roleId,
                    heroId = hid,
                    name = spec.name,
                    star = Mathf.Max(1, spec.stars),
                    skillName = skillTitle,
                    isMelee = melee,
                    formPos = formLocal,
                    pos = formLocal,
                    facingRight = true,
                    alive = true,
                    hp = maxHp,
                    maxHp = maxHp,
                    shield = (roleId == 1 || hid == "dachui") ? maxHp * 0.12f : 0f,
                    atk = atk,
                    dmgReduction = dmgRed,
                    atkSpeedMul = atkSpd,
                    critChance = critChance,
                    critMult = critMult,
                    rageGainMul = rageMul,
                    rage01 = startRage,
                    aggroWeight = aggro,
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
                h.lungeOffset = Vector2.MoveTowards(h.lungeOffset, Vector2.zero, dt * 1.8f);

                if (h.knockbackVel.sqrMagnitude > 0.0004f)
                {
                    h.pos = ClampHeroPos(h, h.pos + h.knockbackVel * dt);
                    h.knockbackVel = Vector2.MoveTowards(h.knockbackVel, Vector2.zero, dt * 7.5f);
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

                float effAtkSpd = h.atkSpeedMul * (h.teamSpdBuffTimer > 0f ? 1.28f : 1f);
                if (h.atkCooldown > 0f)
                    h.atkCooldown -= dt * effAtkSpd;

                // 战地医官（老陈头）：优先治疗受伤队友，满血时投掷草药毒镖助战
                if (h.roleId == 5 || h.heroId == "laochentou")
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

                if (h.isMelee)
                {
                    StepMeleeHero(h, target, dt);
                }
                else
                {
                    StepRangedHero(h, target, dt);
                }
            }
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
                        // 大锤：重锤猛击 + 每 2 击自生小护盾 + 击退震慑
                        DamageEnemy(target, dmg, h, isCrit, knockbackX: 0.18f);
                        if (h.swingCount % 2 == 0)
                        {
                            float sh = h.maxHp * 0.045f;
                            h.shield = Mathf.Min(h.maxHp * 0.65f, h.shield + sh);
                            h.totalHeal += sh;
                        }
                        Vector2 fxPt = ClampFireAndAoePos(target.pos + new Vector2(0f, 0.12f));
                        SpawnTransientFx(_spShockRing, fxPt, fxPt,
                            0.25f, 0.85f, new Color(0.45f, 0.82f, 1f, 0.75f), 0.22f, sortOrder: 2400);
                    }
                    else
                    {
                        // 雷猛：重装先锋消防破拆重斧宽幅顺劈主目标 + 前方扇区副目标
                        DamageEnemy(target, dmg, h, isCrit, knockbackX: 0.12f);
                        for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                        {
                            EnemyUnit other = _aliveEnemies[e];
                            if (other == target || !IsEnemyTargetable(other)) continue;
                            if (Vector2.Distance(other.pos, target.pos) <= 0.92f)
                                DamageEnemy(other, dmg * 0.55f, h, isCrit: false, knockbackX: 0.08f);
                        }
                        Vector2 slashPos = ClampFireAndAoePos(h.pos + new Vector2(h.facingRight ? 0.42f : -0.42f, 0.34f));
                        Vector2 slashEnd = ClampFireAndAoePos(slashPos + new Vector2(h.facingRight ? 0.22f : -0.22f, 0f));
                        SpawnTransientFx(_spCleaveWave, slashPos, slashEnd,
                            0.55f, 1.05f, new Color(1f, 0.82f, 0.42f, 0.88f), 0.20f, sortOrder: 2600);
                    }
                }
                else if (h.atkCooldown < 0.32f)
                {
                    h.puppet?.SetMode(HeroAnimMode.Idle);
                }
            }
        }

        void StepRangedHero(HeroUnit h, EnemyUnit target, float dt)
        {
            // 各远程职业最佳交火射程：铁叔近中程霰弹、小棠中程投掷、林砚远程连弩
            float minIdeal = 1.12f;
            float maxIdeal = 4.45f;
            if (h.roleId == 4 || h.heroId == "tieshu") { minIdeal = 0.95f; maxIdeal = 3.55f; }
            else if (h.roleId == 2 || h.heroId == "xiaotang") { minIdeal = 1.15f; maxIdeal = 4.25f; }
            else if (h.roleId == 3 || h.heroId == "linyan") { minIdeal = 1.20f; maxIdeal = 4.95f; }

            EnemyUnit closestThreat = FindClosestEnemyTo(h.pos);
            float threatDist = closestThreat != null ? Vector2.Distance(h.pos, closestThreat.pos) : 99f;
            float targetDist = Vector2.Distance(h.pos, target.pos);

            // 远程骨干严格活动在左半桥 [-2.95f, -0.45f] 内风筝与推进！
            bool needKiteBack = closestThreat != null && threatDist < minIdeal && h.pos.x > -2.92f && h.atkCooldown > 0.12f;
            bool needStepForward = targetDist > maxIdeal && h.pos.x < -0.48f;

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
                Vector2 goal = ClampHeroPos(h, new Vector2(target.pos.x - (maxIdeal - 0.35f), Mathf.Lerp(h.pos.y, h.formPos.y, 0.5f)));
                Vector2 prev = h.pos;
                h.pos = ClampHeroPos(h, Vector2.MoveTowards(h.pos, goal, 1.95f * dt));
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
            if (closestThreat != null && Vector2.Distance(h.pos, closestThreat.pos) < 1.15f && h.pos.x > -2.92f && h.atkCooldown > 0.15f)
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
                h.puppet?.SetMode(HeroAnimMode.Chop);
                AddHeroRage(h, 0.16f);

                if (hurtAlly != null)
                {
                    h.facingRight = hurtAlly.pos.x >= h.pos.x;
                    h.lungeOffset = new Vector2(h.facingRight ? 0.08f : -0.08f, 0f);
                    float healAmt = h.atk * 1.05f + hurtAlly.maxHp * 0.04f;
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.HealHerb,
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
                        SpawnArenaProjectile(new ArenaProjectile
                        {
                            style = ProjectileStyle.Bolt,
                            sourceHero = h,
                            targetEnemy = foe,
                            fromLocal = h.pos + new Vector2(h.facingRight ? 0.24f : -0.24f, 0.40f),
                            toLocal = foe.pos + new Vector2(0f, 0.34f),
                            arcHeight = 0.18f,
                            duration = Mathf.Clamp(Vector2.Distance(h.pos, foe.pos) / 8.5f, 0.15f, 0.38f),
                            amount = h.atk * 0.85f,
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
            Vector2 muzzle = h.pos + new Vector2(h.facingRight ? 0.28f : -0.28f, 0.40f);
            bool isCrit = RollCrit(h.critChance);

            if (h.roleId == 2 || h.heroId == "xiaotang")
            {
                // 小棠：高抛弧线燃烧瓶 -> 落地爆裂并在路面留下 3.0s 橘红灼烧火海（落点严格钳制于屏内桥面）！
                h.atkCooldown = 1.08f;
                h.lungeOffset = new Vector2(h.facingRight ? 0.10f : -0.10f, 0.03f);
                AddHeroRage(h, 0.18f);
                Vector2 landTarget = ClampFireAndAoePos(target.pos);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.MolotovArc,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = muzzle,
                    toLocal = landTarget,
                    arcHeight = 1.10f,
                    duration = Mathf.Clamp(Vector2.Distance(h.pos, landTarget) / 6.2f, 0.28f, 0.52f),
                    amount = h.atk * 0.92f * (isCrit ? h.critMult : 1f),
                    splashRadius = 0.88f,
                    firePoolDps = h.atk * 0.42f,
                    firePoolLife = 3.0f,
                    isCrit = isCrit,
                }, "proj_wrench", new Color(1f, 0.64f, 0.22f, 1f), 0.68f);
            }
            else if (h.roleId == 4 || h.heroId == "tieshu")
            {
                // 铁叔：重型霰弹扇形 3 弹丸喷射 + 枪口火光 + 物理击退！
                h.atkCooldown = 0.96f;
                h.lungeOffset = new Vector2(h.facingRight ? -0.09f : 0.09f, 0f); // 后坐力顿感
                AddHeroRage(h, 0.17f);

                Vector2 baseDir = (target.pos - h.pos);
                if (baseDir.sqrMagnitude < 0.01f) baseDir = Vector2.right;
                float dist = Mathf.Clamp(baseDir.magnitude, 1.2f, 3.6f);
                baseDir.Normalize();

                float[] spreadAngles = { -9f, 0f, 9f };
                for (int p = 0; p < spreadAngles.Length; p++)
                {
                    Vector2 d = Rotate2D(baseDir, spreadAngles[p]);
                    Vector2 endPt = h.pos + d * dist + new Vector2(0f, 0.30f);
                    endPt.x = Mathf.Clamp(endPt.x, -2.95f, 2.25f);
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.Buckshot,
                        sourceHero = h,
                        targetEnemy = p == 1 ? target : null,
                        fromLocal = muzzle,
                        toLocal = endPt,
                        dirNorm = d,
                        arcHeight = 0f,
                        duration = Mathf.Clamp(dist / 11.5f, 0.12f, 0.26f),
                        amount = h.atk * 0.48f * (isCrit ? h.critMult : 1f),
                        knockback = 0.24f,
                        isCrit = isCrit && p == 1,
                    }, "proj_buckshot", new Color(1f, 0.88f, 0.42f, 1f), 0.66f);
                }

                SpawnTransientFx(_spShockRing, muzzle, muzzle + baseDir * 0.28f,
                    0.22f, 0.58f, new Color(1f, 0.82f, 0.32f, 0.85f), 0.14f, sortOrder: 2650);
            }
            else
            {
                // 林砚：高速青金穿甲连弩！
                h.atkCooldown = 0.62f;
                h.lungeOffset = new Vector2(h.facingRight ? -0.05f : 0.05f, 0f);
                AddHeroRage(h, 0.14f);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Bolt,
                    sourceHero = h,
                    targetEnemy = target,
                    fromLocal = muzzle,
                    toLocal = new Vector2(Mathf.Min(2.20f, target.pos.x), target.pos.y + 0.34f),
                    arcHeight = 0.08f,
                    duration = Mathf.Clamp(Vector2.Distance(h.pos, target.pos) / 10.8f, 0.12f, 0.32f),
                    amount = h.atk * 0.96f * (isCrit ? h.critMult : 1f),
                    isCrit = isCrit,
                }, "proj_arrow", new Color(0.55f, 0.98f, 1f, 1f), 0.76f);
            }
        }

        void ExecuteHeroUltimate(HeroUnit h)
        {
            h.rage01 = 0f;
            h.puppet?.SetMode(HeroAnimMode.Chop);
            TriggerMicroShake(0.18f, 0.07f);

            // 脚下金色大招爆发波纹
            SpawnTransientFx(_spShockRing, h.pos + new Vector2(0f, 0.08f), h.pos + new Vector2(0f, 0.08f),
                0.35f, 1.45f, new Color(1f, 0.84f, 0.24f, 0.90f), 0.28f, sortOrder: 2350);

            if (h.roleId == 1 || h.heroId == "dachui")
            {
                // 大锤绝技：全队前排护盾 + 自身 34% 巨盾 + 震地冲击波眩晕嘲讽！
                float selfSh = h.maxHp * 0.34f;
                h.shield = Mathf.Min(h.maxHp * 0.85f, h.shield + selfSh);
                h.totalHeal += selfSh;
                SpawnWorldNumber(h.pos + new Vector2(0f, 0.95f), Mathf.RoundToInt(selfSh), isHeal: true, isCrit: false);

                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive || ally == h) continue;
                    float aSh = ally.maxHp * 0.14f;
                    ally.shield = Mathf.Min(ally.maxHp * 0.70f, ally.shield + aSh);
                    h.totalHeal += aSh;
                }

                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(h.pos, z.pos) <= 2.65f)
                    {
                        z.stunTimer = Mathf.Max(z.stunTimer, 1.35f);
                        DamageEnemy(z, h.atk * 2.15f, h, isCrit: true, knockbackX: 0.32f);
                    }
                }
                Vector2 ringCenter = ClampFireAndAoePos(h.pos + new Vector2(0.25f, 0.12f));
                SpawnTransientFx(_spShockRing, ringCenter, ringCenter,
                    0.45f, 2.35f, new Color(0.38f, 0.84f, 1f, 0.88f), 0.34f, sortOrder: 2500);
            }
            else if (h.roleId == 2 || h.heroId == "xiaotang")
            {
                // 小棠绝技：三连高爆燃烧瓶覆盖桥心尸群，燃起成片烈焰火海（3.0s，严格钳制在屏内 [-2.6, +1.95]）！
                var targetables = new List<EnemyUnit>(8);
                for (int e = 0; e < _aliveEnemies.Count; e++)
                    if (IsEnemyTargetable(_aliveEnemies[e])) targetables.Add(_aliveEnemies[e]);

                int count = 3;
                for (int k = 0; k < count; k++)
                {
                    Vector2 basePt = targetables.Count > 0
                        ? targetables[(k * 2) % targetables.Count].pos
                        : new Vector2(0.85f, RowLocalY[k % 3]);
                    Vector2 land = ClampFireAndAoePos(basePt + new Vector2((k - 1) * 0.38f, (k % 2 == 0 ? 0.12f : -0.12f)));
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.MolotovArc,
                        sourceHero = h,
                        fromLocal = h.pos + new Vector2(0.25f, 0.48f),
                        toLocal = land,
                        arcHeight = 1.45f + k * 0.22f,
                        duration = 0.34f + k * 0.07f,
                        amount = h.atk * 1.55f,
                        splashRadius = 1.20f,
                        firePoolDps = h.atk * 0.56f,
                        firePoolLife = 3.0f,
                        isCrit = true,
                    }, "proj_wrench", new Color(1f, 0.48f, 0.14f, 1f), 0.82f);
                }
            }
            else if (h.roleId == 3 || h.heroId == "linyan")
            {
                // 林砚绝技：贯日散射箭雨，7 支青金穿透利箭覆盖桥面中央交战区！
                for (int k = 0; k < 7; k++)
                {
                    float laneY = Mathf.Lerp(-2.05f, 1.35f, k / 6f);
                    Vector2 start = new Vector2(h.pos.x + 0.35f, laneY + 1.35f);
                    Vector2 end = new Vector2(2.15f, laneY);
                    SpawnArenaProjectile(new ArenaProjectile
                    {
                        style = ProjectileStyle.RainArrow,
                        sourceHero = h,
                        fromLocal = start,
                        toLocal = end,
                        dirNorm = (end - start).normalized,
                        arcHeight = 0f,
                        duration = 0.32f + (k % 3) * 0.04f,
                        amount = h.atk * 1.25f,
                        piercing = true,
                        isCrit = true,
                    }, "proj_arrow", new Color(0.42f, 1f, 0.92f, 1f), 0.86f);
                }
            }
            else if (h.roleId == 4 || h.heroId == "tieshu")
            {
                // 铁叔绝技：双管重火力 4 轮连发倾泻，密集铁砂弹幕强力压制尸潮！
                FireGunsmithSalvoBurst(h);
                h.gunSalvoRemaining = 3;
                h.gunSalvoTimer = 0.10f;
            }
            else if (h.roleId == 5 || h.heroId == "laochentou")
            {
                // 老陈头绝技：翡翠战地急救群疗波 + 全队白色护盾 + 攻速沸腾！
                for (int i = 0; i < _heroes.Count; i++)
                {
                    HeroUnit ally = _heroes[i];
                    if (!ally.alive) continue;
                    float heal = h.atk * 1.95f + ally.maxHp * 0.12f;
                    float shieldAdd = ally.maxHp * 0.16f;
                    float actualHeal = Mathf.Min(ally.maxHp - ally.hp, heal);
                    ally.hp = Mathf.Min(ally.maxHp, ally.hp + heal);
                    ally.shield = Mathf.Min(ally.maxHp * 0.75f, ally.shield + shieldAdd);
                    ally.teamSpdBuffTimer = 4.5f;

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
                // 雷猛绝技：狂风破障斩，巨型烈金剑气横扫桥心尸群并强力击退！
                h.lungeOffset = new Vector2(0.20f, 0f);
                for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit z = _aliveEnemies[e];
                    if (!IsEnemyTargetable(z)) continue;
                    if (Vector2.Distance(h.pos, z.pos) <= 2.75f || (z.pos.x >= h.pos.x - 0.5f && z.pos.x <= 2.25f))
                        DamageEnemy(z, h.atk * 2.55f, h, isCrit: true, knockbackX: 0.36f);
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

            float[] fan = { -15f, -5f, 5f, 15f };
            for (int k = 0; k < fan.Length; k++)
            {
                Vector2 d = Rotate2D(baseDir, fan[k]);
                Vector2 end = h.pos + d * 3.4f + new Vector2(0f, 0.30f);
                end.x = Mathf.Clamp(end.x, -2.95f, 2.25f);
                SpawnArenaProjectile(new ArenaProjectile
                {
                    style = ProjectileStyle.Buckshot,
                    sourceHero = h,
                    fromLocal = muzzle,
                    toLocal = end,
                    dirNorm = d,
                    duration = 0.22f,
                    amount = h.atk * 0.62f,
                    knockback = 0.32f,
                    piercing = true,
                    isCrit = true,
                }, "proj_buckshot", new Color(1f, 0.78f, 0.25f, 1f), 0.74f);
            }
        }

        // =====================================================================
        // 丧尸军团波次生成与战术索敌（含红圈预警抛物线跳尸 & 1.45x 巨型首领）
        // =====================================================================
        void BuildDeterministicStageWaves(int stage, int rawTotalCount, int recPower, bool isBoss, bool hasFlanker)
        {
            _spawnQueue.Clear();
            int total = Mathf.Clamp(rawTotalCount > 0 ? rawTotalCount : (6 + stage / 2), 5, 34);
            EnemiesTotal = total;

            // 将整关敌人总血量与攻击锚定在 recommendedPower 上，确保战力达标稳定通关、首领战有充足拉扯感！
            float hpScale = isBoss ? 3.65f : (stage >= 4 ? 2.65f : 2.25f);
            float totalHpBudget = Mathf.Max(420f, recPower * hpScale);
            float baseUnitAtk = Mathf.Max(11f, 9f + recPower * 0.024f);

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
                    spawnT = 3.6f;
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
                float dr = kind == EnemyKind.HardhatArmored ? 0.18f : (kind == EnemyKind.GiantBoss ? 0.12f : 0f);

                _spawnQueue.Add(new QueuedEnemySpec
                {
                    kind = kind,
                    subWave = subWave,
                    spawnTime = spawnT,
                    laneRow = kind == EnemyKind.GiantBoss ? 1 : (i % 3),
                    maxHp = hp,
                    atk = atk,
                    speed = spd,
                    dmgRed = dr,
                });
                _queuedHpSum += hp;
            }

            _spawnQueue.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
            EnemyTotalMaxHp = Mathf.Max(1f, _queuedHpSum);
            EnemyTotalHp = _queuedHpSum;
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

        void SpawnEnemyInstance(QueuedEnemySpec q)
        {
            bool isBoss = q.kind == EnemyKind.GiantBoss;
            float jitterY = ((float)_battleRng.NextDouble() - 0.5f) * 0.36f;
            float inwardOffset = (float)_battleRng.NextDouble() * 0.10f;
            float startX = isBoss ? 2.95f : (3.05f - inwardOffset);
            float startY = Mathf.Clamp(RowLocalY[Mathf.Clamp(q.laneRow, 0, 2)] + jitterY, -2.15f, 1.48f);

            string spriteKey;
            float scale;
            Color baseTint = Color.white;

            switch (q.kind)
            {
                case EnemyKind.GiantBoss:
                    spriteKey = "zombie_boss_brute";
                    scale = 1.45f; // 1.45x 巨型首领压迫感！
                    break;
                case EnemyKind.FastRunner:
                    spriteKey = "zombie_runner";
                    scale = 0.84f;
                    break;
                case EnemyKind.BacklineJumper:
                    spriteKey = "zombie_runner";
                    scale = 0.88f;
                    baseTint = new Color(1f, 0.76f, 0.82f, 1f);
                    break;
                case EnemyKind.HardhatArmored:
                    spriteKey = "zombie_1";
                    scale = 1.02f;
                    baseTint = new Color(0.88f, 0.94f, 1f, 1f);
                    break;
                default:
                    spriteKey = (_battleRng.NextDouble() < 0.5) ? "zombie_0" : "zombie_1";
                    scale = 0.88f;
                    break;
            }

            var go = new GameObject($"ArenaEnemy_{q.kind}");
            go.transform.SetParent(_unitRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestArt.Get(spriteKey, () => ForestArt.Get("zombie_0", () => ForestPlaceholderArt.Survivor(8)));
            sr.color = baseTint;
            sr.flipX = false; // 资源默认朝左迎向我方阵营

            Transform shadowTr = CreateRoadShadow($"EnemyShadow_{q.kind}", new Vector2(startX, startY), isBoss ? 1.08f : 0.58f, isBoss ? 0.36f : 0.22f);
            OverheadBarRig bars = CreateOverheadBars(go.transform, isHero: false,
                width: isBoss ? 1.15f : 0.62f,
                yOffset: isBoss ? 1.12f : 0.82f);

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
                dmgReduction = q.dmgRed,
                atkRange = isBoss ? 0.78f : 0.64f,
                atkCooldown = 0.25f + (float)_battleRng.NextDouble() * 0.25f,
                phase = (float)_battleRng.NextDouble() * 6.28f,
                elapsedEntry = 0f,
                tr = go.transform,
                sr = sr,
                shadowTr = shadowTr,
                bossAuraTr = bossAuraTr,
                bars = bars,
            };

            SyncEnemyVisualImmediate(enemy);
            _aliveEnemies.Add(enemy);
        }

        void StepEnemies(float dt)
        {
            bool inIntro = ElapsedBattleSec < ClashIntroSec;

            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyUnit z = _aliveEnemies[i];
                if (!z.alive) continue;

                z.phase += dt;
                z.elapsedEntry += dt;
                if (z.hitFlashTimer > 0f) z.hitFlashTimer = Mathf.Max(0f, z.hitFlashTimer - dt);
                z.lungeOffset = Vector2.MoveTowards(z.lungeOffset, Vector2.zero, dt * 1.9f);

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
                    continue;
                }

                // 新刷出的后续波次丧尸先全速冲入屏内可见区 (x <= 2.15 / Boss <= 1.85) 再转入常规战斗！
                if (z.pos.x > rushTargetX && z.jumpPhase == 0)
                {
                    z.pos.x = Mathf.MoveTowards(z.pos.x, rushTargetX, entryRushSpd * dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    if (z.pos.x > rushTargetX + 0.01f)
                        continue;
                }

                if (z.stunTimer > 0f)
                {
                    z.stunTimer = Mathf.Max(0f, z.stunTimer - dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    continue;
                }

                // 跳尸突袭者专属红圈预警 -> 高抛飞跃切后排状态机！
                if (z.kind == EnemyKind.BacklineJumper && z.jumpPhase < 3)
                {
                    StepBacklineJumperSpecial(z, dt);
                    continue;
                }

                HeroUnit target = PickTargetForEnemy(z);
                if (target == null) continue;

                if (z.atkCooldown > 0f)
                    z.atkCooldown -= dt;

                float dist = Vector2.Distance(z.pos, target.pos);
                if (dist > z.atkRange)
                {
                    float standX = target.pos.x + (target.pos.x <= z.pos.x ? (z.atkRange * 0.80f) : (-z.atkRange * 0.80f));
                    if (target.isMelee)
                        standX = Mathf.Clamp(standX, -0.60f, isBoss ? 1.72f : 1.65f);
                    Vector2 standGoal = new Vector2(standX, target.pos.y);
                    float marchSpd = z.pos.x > rushTargetX ? entryRushSpd : z.speed;
                    z.pos = ClampEnemyUnitPos(z, Vector2.MoveTowards(z.pos, standGoal, marchSpd * dt));
                }
                else if (z.atkCooldown <= 0f)
                {
                    z.atkCooldown = isBoss ? 1.25f : (z.kind == EnemyKind.FastRunner ? 0.76f : 0.98f);
                    float dirSign = target.pos.x <= z.pos.x ? -1f : 1f;
                    z.lungeOffset = new Vector2(dirSign * 0.15f, 0f);

                    if (isBoss)
                    {
                        // 巨型首领范围横扫重击！
                        DamageHero(target, z.atk, knockbackX: -0.18f);
                        for (int h = 0; h < _heroes.Count; h++)
                        {
                            HeroUnit other = _heroes[h];
                            if (!other.alive || other == target) continue;
                            if (Vector2.Distance(other.pos, target.pos) <= 1.05f)
                                DamageHero(other, z.atk * 0.58f, knockbackX: -0.12f);
                        }
                        TriggerMicroShake(0.14f, 0.05f);
                        Vector2 fxPt = ClampFireAndAoePos(target.pos + new Vector2(0f, 0.10f));
                        SpawnTransientFx(_spShockRing, fxPt, fxPt,
                            0.35f, 1.35f, new Color(1f, 0.32f, 0.22f, 0.80f), 0.24f, sortOrder: 2450);
                    }
                    else
                    {
                        DamageHero(target, z.atk, knockbackX: -0.06f);
                    }
                }
                z.pos = ClampEnemyUnitPos(z, z.pos);
            }
        }

        void StepBacklineJumperSpecial(EnemyUnit z, float dt)
        {
            if (z.jumpPhase == 0)
            {
                // 先疾冲入桥面清晰可见区域 x <= 2.05f，再下蹲拉响后排红色预警圈！
                if (z.pos.x > 2.05f)
                {
                    z.pos.x = Mathf.MoveTowards(z.pos.x, 2.02f, z.speed * 2.2f * dt);
                    z.pos = ClampEnemyUnitPos(z, z.pos);
                    return;
                }

                Vector2 backSpot = ChooseBacklineJumpLandingSpot(z.laneRow);
                z.jumpStart = z.pos;
                z.jumpTarget = backSpot;
                z.jumpPhase = 1;
                z.jumpTimer = 0.65f; // 0.65s 醒目红圈地面预警！

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
                if (z.warningRingTr != null)
                {
                    float pulse = 0.82f + Mathf.Sin(ElapsedBattleSec * 22f) * 0.16f;
                    z.warningRingTr.localScale = new Vector3(pulse * 1.15f, pulse * 0.62f, 1f);
                }
                if (z.jumpTimer <= 0f)
                {
                    z.jumpPhase = 2;
                    z.jumpTimer = 0f;
                }
            }
            else if (z.jumpPhase == 2)
            {
                const float leapDur = 0.62f;
                z.jumpTimer += dt;
                float u = Mathf.Clamp01(z.jumpTimer / leapDur);
                z.pos = Vector2.Lerp(z.jumpStart, z.jumpTarget, u);

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
                float scoreX = h.pos.x + (h.isMelee ? 1.5f : 0f);
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

                if (!p.piercing && p.style != ProjectileStyle.MolotovArc)
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
                    if (p.style == ProjectileStyle.MolotovArc)
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

                // 穿透类弹道（林砚贯日箭雨 / 铁叔重装霰弹弹幕）沿途实时命中（仅命中已冲入可见区 x <= 2.25f 的丧尸）
                if (p.piercing)
                {
                    for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                    {
                        EnemyUnit z = _aliveEnemies[e];
                        if (!IsEnemyTargetable(z) || p.piercedSet.Contains(z)) continue;
                        if (Vector2.Distance(p.curLocal, z.pos + new Vector2(0f, 0.32f)) <= 0.52f)
                        {
                            p.piercedSet.Add(z);
                            DamageEnemy(z, p.amount, p.sourceHero, p.isCrit, p.knockback);
                        }
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
                        DamageEnemy(z, p.amount, p.sourceHero, p.isCrit, knockbackX: 0.10f);
                }
                SpawnFirePool(p.sourceHero, hitGround, p.splashRadius * 0.95f, p.firePoolDps, p.firePoolLife);
                SpawnTransientFx(_spShockRing, hitGround, hitGround,
                    0.25f, p.splashRadius * 1.65f, new Color(1f, 0.55f, 0.15f, 0.88f), 0.24f, sortOrder: 2480);
                return;
            }

            if (!p.piercing)
            {
                EnemyUnit hit = IsEnemyTargetable(p.targetEnemy)
                    ? p.targetEnemy
                    : FindClosestEnemyTo(p.toLocal - new Vector2(0f, 0.30f), maxDist: 0.68f);
                if (IsEnemyTargetable(hit))
                    DamageEnemy(hit, p.amount, p.sourceHero, p.isCrit, p.knockback);
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

                if (fp.tickTimer <= 0f)
                {
                    fp.tickTimer = 0.50f;
                    float tickDmg = fp.dps * 0.50f;
                    for (int e = _aliveEnemies.Count - 1; e >= 0; e--)
                    {
                        EnemyUnit z = _aliveEnemies[e];
                        if (!IsEnemyTargetable(z)) continue;
                        if (Vector2.Distance(z.pos, fp.localPos) <= fp.radius + 0.18f)
                            DamageEnemy(z, tickDmg, fp.owner, isCrit: false, knockbackX: 0f);
                    }
                }

                if (fp.life <= 0f)
                {
                    if (fp.tr != null) DestroyObj(fp.tr.gameObject);
                    _firePools.RemoveAt(i);
                }
            }
        }

        void DamageEnemy(EnemyUnit z, float rawDmg, HeroUnit attacker, bool isCrit, float knockbackX)
        {
            if (z == null || !z.alive) return;
            float finalDmg = Mathf.Max(1f, rawDmg * (1f - z.dmgReduction));
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

        void DamageHero(HeroUnit h, float rawDmg, float knockbackX)
        {
            if (h == null || !h.alive) return;
            float reduced = Mathf.Max(1f, rawDmg * (1f - h.dmgReduction));

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

            if (h.hp <= 0f)
            {
                h.alive = false;
                h.hp = 0f;
                h.shield = 0f;
                h.rage01 = 0f;
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
                    // 跳尸落地后专门切后排远程脆皮！
                    float meleePenalty = h.isMelee ? 1.85f : 0f;
                    score = dist * 0.9f + meleePenalty + (h.pos.x + 3.0f) * 0.35f;
                }
                else
                {
                    // 普通丧尸优先啃咬顶在最前排 (pos.x 大) 及高嘲讽值（大锤/前排列 col=2）的肉盾！
                    float frontPriority = -h.pos.x * 0.65f;
                    float tauntBias = (h.aggroWeight - 1f) * (dist < 2.6f ? 0.85f : 0.30f);
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
            float maxX = (h != null && h.isMelee) ? 1.05f : -0.45f;
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

        void SyncAllUnitVisuals(float dt)
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
                h.puppet.Tick(dt);

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
                SyncEnemyVisualImmediate(z);
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

        void SyncEnemyVisualImmediate(EnemyUnit z)
        {
            if (z.tr == null) return;

            float hopY = 0f;
            float tiltDeg = 0f;
            if (z.jumpPhase == 2)
            {
                float u = Mathf.Clamp01(z.jumpTimer / 0.62f);
                hopY = Mathf.Sin(u * Mathf.PI) * 2.15f;
                tiltDeg = Mathf.Lerp(18f, -22f, u);
            }
            else if (z.stunTimer > 0f)
            {
                tiltDeg = Mathf.Sin(ElapsedBattleSec * 36f) * 11f;
            }
            else
            {
                float w = z.phase * (z.kind == EnemyKind.FastRunner ? 11.5f : 7.2f);
                hopY = Mathf.Abs(Mathf.Sin(w)) * (z.kind == EnemyKind.GiantBoss ? 0.055f : 0.042f);
                tiltDeg = Mathf.Sin(w) * (z.kind == EnemyKind.GiantBoss ? 3.5f : 5.5f);
            }

            Vector3 bodyLocal = new Vector3(z.pos.x + z.lungeOffset.x, z.pos.y + z.lungeOffset.y + hopY, 0f);
            z.tr.localPosition = bodyLocal;
            z.tr.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
            z.tr.localScale = new Vector3(z.baseScale, z.baseScale, 1f);

            // 若跳尸跃至英雄左侧，自动翻转朝右扑咬后排！
            HeroUnit lookHero = PickTargetForEnemy(z);
            if (lookHero != null && Mathf.Abs(lookHero.pos.x - z.pos.x) > 0.08f)
                z.sr.flipX = lookHero.pos.x > z.pos.x;

            int order = Mathf.RoundToInt(-z.pos.y * 100f) + 505;
            z.sr.sortingOrder = order;

            Color baseCol = z.kind == EnemyKind.BacklineJumper
                ? new Color(1f, 0.76f, 0.82f, 1f)
                : (z.kind == EnemyKind.HardhatArmored ? new Color(0.88f, 0.94f, 1f, 1f) : Color.white);
            z.sr.color = z.hitFlashTimer > 0f ? new Color(1f, 0.42f, 0.38f, 1f) : baseCol;

            if (z.shadowTr != null)
            {
                z.shadowTr.localPosition = new Vector3(z.pos.x, z.pos.y - 0.02f, 0f);
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
            if (_glyphSprites.Count == 0) BuildBitmapNumberGlyphs();
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
