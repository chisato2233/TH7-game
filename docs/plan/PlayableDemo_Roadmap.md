# TH7 可玩 Demo 开发路线图

> 目标：快速完成一个可玩的 Demo，展示核心游戏循环
> 创建日期：2026-01-13

---

## 当前状态概览

### 已完成模块 (90%+)

| 模块 | 状态 | 说明 |
|------|------|------|
| 框架层 | ✅ 完成 | GameEntry, EventSystem, ContextSystem, Reactive |
| 能力系统 | ✅ 完成 | GAS 框架：属性、效果、技能、标签 |
| 上下文 | ✅ 完成 | Session, World, Town, Battle(骨架) |
| 英雄系统 | ✅ 完成 | 移动、状态机、军队管理 |
| 回合系统 | ✅ 完成 | 玩家行动、回合流转、日/周/月 |
| 城镇系统 | ✅ 完成 | 建筑建造、兵种招募 |
| 资源系统 | ✅ 完成 | 7种资源、产出、消耗 |
| 存档系统 | ✅ 完成 | ES3 序列化 |
| UI 系统 | ✅ 完成 | 资源条、日期、回合、城镇面板 |

### 缺失功能 (影响可玩性)

| 功能 | 优先级 | 说明 |
|------|--------|------|
| 战斗系统 | P0 | 核心玩法，必须实现 |
| 地图物件 | P1 | 资源堆、矿场、野怪 |
| AI 系统 | P2 | 敌方英雄决策 |
| 英雄招募 | P2 | 酒馆招募英雄 |

---

## Demo 目标定义

### 最小可玩产品 (MVP)

玩家能够完成以下游戏循环：

```
开始新游戏 → 控制英雄探索 → 拾取资源 → 占领矿场 →
回城建造 → 招募兵种 → 遭遇野怪 → 进入战斗 →
胜利获得奖励 → 继续探索
```

### Demo 包含内容

1. **单张测试地图** (20x20 格)
2. **1个可玩文明** (希腊 Greek)
3. **1座玩家城镇**
4. **1位玩家英雄**
5. **3-5种地图物件** (资源堆、矿场、野怪)
6. **简化战斗系统** (自动战斗 + 基础手动)
7. **3种可招募兵种** (1/3/5级)

---

## 开发阶段规划

### 第一阶段：地图物件系统

> 让世界地图变得可交互

#### 1.1 地图物件基础架构

**目标文件**: `src/world/mapobject/`

```
MapObject (基类)
├── ResourcePile (资源堆) - 一次性拾取
├── Mine (矿场) - 占领后每日产出
├── Monster (野怪) - 战斗遭遇
└── Town (城镇入口) - 已实现
```

**数据结构**:
```csharp
public class MapObjectData
{
    public string ObjectId;
    public MapObjectType Type;
    public Vector3Int Position;
    public int OwnerId; // -1 = 中立
    public bool IsCollected;
}
```

**任务清单**:
- [ ] 创建 `MapObject` 基类和生命周期
- [ ] 实现 `ResourcePile` (木材堆、金币袋等)
- [ ] 实现 `Mine` (矿场占领逻辑)
- [ ] 在 `WorldContext` 中管理地图物件列表
- [ ] 英雄踩到物件时触发交互

#### 1.2 资源产出系统

**集成点**: `WorldTurnManager.ProcessDayEnd()`

```csharp
// 每日结算时
foreach (var mine in worldContext.OwnedMines)
{
    sessionContext.Resources.Add(mine.ResourceType, mine.DailyOutput);
}
```

**任务清单**:
- [ ] 在 `SessionContext` 中存储已占领矿场
- [ ] 在日结束时计算资源产出
- [ ] 显示资源变化通知 UI

---

### 第二阶段：战斗系统 (简化版)

> 实现可玩的回合制战斗

#### 2.1 战场方案：Isometric Z as Y

采用 Unity Tilemap 的 **Isometric Z as Y** 模式：

**优势**:
- 逻辑层保持矩形坐标 (x, y)，算法简单
- 视觉上有策略游戏的立体感
- Y 坐标自动决定渲染顺序
- 与世界地图共用坐标系统
- Unity 原生支持，无需额外工作

**战场设置**:
```
BattleField (Isometric Tilemap)
├── Grid: 12x8 逻辑格子
├── 左侧: 攻方部署区 (列 0-2)
├── 右侧: 守方部署区 (列 9-11)
└── 渲染: Z as Y 自动排序
```

#### 2.2 战斗数据结构

**目标文件**: `src/battle/`

```
BattleField (战场)
├── Grid (矩形格子 12x8, Isometric 渲染)
├── Units (战斗单位列表)
└── Obstacles (障碍物)

BattleUnit (战斗单位)
├── SourceStack (来源军队)
├── CurrentHP / MaxHP
├── Position (战场坐标)
└── HasActed (本回合是否行动)
```

**任务清单**:
- [ ] 设计 `BattleField` 数据结构
- [ ] 设计 `BattleUnit` 运行时数据
- [ ] 实现单位从 `UnitStack` 到 `BattleUnit` 的转换

#### 2.2 战斗流程

**状态流转**:
```
Init → RoundStart → UnitSelect → ActionSelect →
ActionExecute → (循环直到一方全灭) → BattleEnd
```

**回合内流程**:
1. 按速度排序所有单位
2. 当前单位选择：移动 / 攻击 / 等待 / 防御
3. 执行行动
4. 检查胜负
5. 下一单位

**任务清单**:
- [ ] 实现 `BattleContext` 完整状态机
- [ ] 实现单位行动顺序计算
- [ ] 实现单位移动 (六边形寻路)
- [ ] 实现单位攻击 (伤害计算)
- [ ] 实现胜负判定

#### 2.3 伤害公式 (简化版)

```csharp
// 简化的英雄无敌伤害公式
int CalculateDamage(BattleUnit attacker, BattleUnit defender)
{
    int baseDamage = Random.Range(attacker.MinDamage, attacker.MaxDamage + 1);
    int totalDamage = baseDamage * attacker.Count;

    int atkDef = attacker.Attack - defender.Defense;
    float modifier = atkDef > 0
        ? 1f + atkDef * 0.05f  // +5% per point
        : 1f + atkDef * 0.025f; // -2.5% per point

    modifier = Mathf.Clamp(modifier, 0.3f, 4f);
    return Mathf.RoundToInt(totalDamage * modifier);
}
```

#### 2.4 战斗 UI

**目标文件**: `src/ui/Battle/`

```
BattleScene
├── BattleFieldView (战场可视化)
│   ├── Tilemap (Isometric 网格)
│   └── UnitViews (单位显示)
├── ActionBar (行动选项)
├── UnitInfoPanel (单位属性)
└── BattleResultPanel (战斗结果)
```

**任务清单**:
- [ ] 创建战斗场景 `BattleScene`
- [ ] 配置 Isometric Tilemap (Z as Y)
- [ ] 实现单位点击选择
- [ ] 实现移动范围高亮
- [ ] 实现攻击范围高亮
- [ ] 实现战斗结果面板

#### 2.5 自动战斗 (快速实现)

为了快速验证，先实现自动战斗：

```csharp
BattleResult AutoBattle(List<UnitStack> army1, List<UnitStack> army2)
{
    // 简单的战力比较
    int power1 = CalculateArmyPower(army1);
    int power2 = CalculateArmyPower(army2);

    // 概率 + 损失计算
    // 返回胜负和剩余兵力
}
```

---

### 第三阶段：野怪遭遇

> 连接探索与战斗

#### 3.1 野怪配置

**数据结构**:
```csharp
[CreateAssetMenu]
public class MonsterConfig : ScriptableObject
{
    public string MonsterId;
    public UnitConfig UnitType;
    public int MinCount;
    public int MaxCount;
    public int ThreatLevel; // 1-10 难度等级
    public ResourceBundle Reward;
}
```

#### 3.2 遭遇流程

```
英雄移动到野怪格子 → 弹出选项 (战斗/撤退) →
选择战斗 → 进入 BattleScene → 战斗结束 →
胜利: 获得奖励，野怪消失
失败: 英雄返回，军队损失
```

**任务清单**:
- [ ] 创建 `MonsterConfig` 数据
- [ ] 实现 `Monster` 地图物件
- [ ] 实现遭遇对话框
- [ ] 连接战斗系统
- [ ] 实现战后奖励

---

### 第四阶段：打磨与平衡

#### 4.1 数据配置

- [ ] 配置希腊文明的 3 种兵种 (Hoplite, Centaur, Minotaur)
- [ ] 配置 5-10 个野怪组合
- [ ] 配置建筑和升级成本
- [ ] 配置测试地图的物件分布

#### 4.2 UI/UX 优化

- [ ] 添加教程提示
- [ ] 添加战斗动画
- [ ] 添加音效反馈
- [ ] 优化移动流畅度

#### 4.3 测试与调试

- [ ] 完整流程测试
- [ ] 数值平衡调整
- [ ] Bug 修复

---

## 技术实现建议

### 战斗系统架构

建议采用 MVC 分离：

```
BattleModel (数据层)
├── BattleField
├── BattleUnit
└── BattleState

BattleController (逻辑层)
├── TurnManager
├── ActionExecutor
└── DamageCalculator

BattleView (视图层)
├── BattleFieldView
├── UnitView
└── UI Components
```

### 场景切换

```
WorldScene ←→ BattleScene (异步加载)
    ↓
TownScene (叠加面板方式，已实现)
```

战斗使用独立场景，便于管理资源和状态。

### 代码复用

- 复用 `Reactive<T>` 做战斗数据绑定
- 复用 `EventSystem` 做战斗事件
- 复用 GAS 系统做 Buff/Debuff
- 复用 `UIBehaviour` 基类

---

## 里程碑检查点

### M1: 地图可交互
- [ ] 能拾取资源堆
- [ ] 能占领矿场
- [ ] 每日资源产出正常

### M2: 战斗可运行
- [ ] 能进入战斗场景
- [ ] 单位能移动和攻击
- [ ] 能判定胜负并退出

### M3: 循环闭环
- [ ] 完整游戏循环可运行
- [ ] 城镇建造 → 招募 → 战斗 → 资源获取

### M4: Demo 完成
- [ ] 有清晰的目标 (如：击败 Boss 野怪)
- [ ] 游戏节奏合理
- [ ] 无明显 Bug

---

## 风险与应对

| 风险 | 影响 | 应对策略 |
|------|------|----------|
| 战斗系统复杂度高 | 开发时间超预期 | 先做自动战斗，再迭代手动战斗 |
| 六边形战场实现困难 | 美术和逻辑复杂 | 可考虑先用矩形格子验证 |
| 数值平衡困难 | 游戏体验差 | 提供作弊码调试，后期再平衡 |

---

## 附录

### 参考资源

- [docs/HoMM3_GameMechanics.md](../HoMM3_GameMechanics.md) - 英雄无敌3机制详解
- [src/world/guide.md](../../src/world/guide.md) - 世界系统开发指南
- [src/town/guide.md](../../src/town/guide.md) - 城镇系统开发指南

### 优先级说明

- **P0**: 必须实现，否则 Demo 不可玩
- **P1**: 重要功能，显著提升体验
- **P2**: 可选功能，时间允许再做
