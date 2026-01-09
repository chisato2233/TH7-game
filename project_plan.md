# TH7 开发计划

> 项目进度追踪与里程碑规划

---

## 模块完成状态

| 模块 | 状态 | 说明 |
|------|------|------|
| framework/ | Done | GameEntry, EventSystem, ContextSystem, GameBehaviour |
| framework/UI | Done | UIBehaviour, UIWindowBehaviour, ScrollViewUI |
| framework/Ability | Done | GAS: 标签, 属性(响应式), 效果, 技能, ASC + Editor |
| context/ | Done | SessionContext + ES3 存档, WorldContext, BattleContext(框架) |
| scene/ | Done | Boot, MainMenu, World 场景控制器 |
| ui/MainMenu | Done | MainMenuUI, SaveSlotPanel |
| ui/Common | Done | ResourceDisplayUI, ResourceBarUI, ResourceCostUI |
| ui/Town | Done | TownPanelUI, BuildingGridUI, BuildingInfoPanelUI, RecruitPanelUI |
| map/ | Done | Tilemap + 逻辑层分离, 地形配置 |
| resource/ | Done | ResourceBundle, PlayerResources |
| town/ | Done | TownData, TownContext, BuildingConfig, TownConfigDatabase |
| unit/ | Done | UnitConfig, UnitConfigDatabase, UnitStack |
| world/ | Done | Hero, HeroSaveData, HeroAction, WorldTurnManager, ActionExecutor |

---

## 已完成里程碑

### 城镇界面 MVP

目标：实现英雄无敌风格的城镇系统，包含资源管理、建筑树、招募功能。

**完成内容**:
- 资源系统: ResourceType 枚举, ResourceBundle, PlayerResources (响应式)
- 城镇系统: BuildingConfig (SO), TownData, TownContext, TownConfigDatabase
- 兵种系统: UnitConfig (SO), UnitStack, UnitConfigDatabase
- UI 界面: ResourceBarUI, TownPanelUI, BuildingGridUI, RecruitPanelUI
- 数据持久化: ES3 集成, SessionData 存档, SaveSlotPanel

---

## 当前里程碑: 战斗系统 MVP

目标：实现英雄无敌风格的回合制战斗系统，包含战场、单位、回合管理、伤害计算。

### 系统架构

```
触发战斗                              战斗流程
    |                                    |
WorldContext.TriggerBattle()        BattleContext
    |                                    |
暂停探索 ─────────────────────> 初始化战场
创建 BattleContext                  部署阶段(可选)
    |                                    |
    |                               战斗循环:
    |                               ├── 确定行动顺序(按速度)
    |                               ├── 当前单位行动
    |                               │   ├── 移动
    |                               │   ├── 攻击
    |                               │   ├── 等待
    |                               │   └── 防御
    |                               └── 检查胜负
    |                                    |
恢复 WorldContext <──────────── 战斗结束
销毁 BattleContext                  结算(经验、战利品)
```

### 模块规划

| 模块 | 状态 | 说明 |
|------|------|------|
| battle/ | Pending | 战斗核心逻辑 |
| battle/data | Pending | 战斗数据结构 |
| ui/Battle | Pending | 战斗 UI 界面 |
| context/BattleContext | Pending | 战斗上下文(已有框架) |

### P0 - 战场数据 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 BattleConfig | Pending | 战场配置(尺寸、地形) |
| 创建 BattleField | Pending | 战场数据(六边形网格) |
| 创建 BattleUnit | Pending | 战斗单位(UnitStack + 运行时状态) |
| 创建 BattleArmy | Pending | 军队数据(7 个槽位) |
| 定义 BattleAction | Pending | 行动类型枚举(Move/Attack/Wait/Defend) |

### P0 - 回合管理 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 TurnManager | Pending | 回合管理器, 按速度排序 |
| 创建 ActionResolver | Pending | 行动执行器 |
| 实现移动逻辑 | Pending | 六边形寻路 + 移动力消耗 |
| 实现攻击逻辑 | Pending | 近战/远程判定 |
| 实现等待/防御 | Pending | 行动顺序调整 |

### P0 - 伤害系统 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 DamageCalculator | Pending | 伤害计算公式 |
| 实现攻防修正 | Pending | 攻击>防御: +5%/点, 防御>攻击: -2.5%/点 |
| 实现反击机制 | Pending | 首次被攻击时反击 |
| 实现堆叠损失 | Pending | 伤害分摊到堆叠单位 |

### P1 - 战斗 UI (src/ui/Battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 BattleSceneUI | Pending | 战斗场景主 UI |
| 创建 BattleFieldView | Pending | 战场六边形网格显示 |
| 创建 BattleUnitView | Pending | 单位显示(精灵 + 数量) |
| 创建 ActionBarUI | Pending | 行动按钮栏 |
| 创建 TurnOrderUI | Pending | 行动顺序预览 |
| 创建 BattleResultUI | Pending | 战斗结果窗口 |

### P1 - BattleContext 扩展 (src/context/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 扩展 BattleContext | Pending | 战斗状态管理 |
| 实现 StartBattle() | Pending | 初始化战场、部署单位 |
| 实现 ProcessTurn() | Pending | 处理当前单位行动 |
| 实现 EndBattle() | Pending | 结算、返回探索 |
| AI 基础行为 | Pending | 简单 AI(攻击最近敌人) |

### P2 - 高级功能(可选)

| 任务 | 状态 | 说明 |
|------|------|------|
| 士气系统 | Pending | 正士气额外行动, 负士气恐慌 |
| 幸运系统 | Pending | 幸运暴击双倍伤害 |
| 远程攻击 | Pending | 弹药、近身惩罚 |
| 战斗动画 | Pending | DOTween 移动/攻击动画 |
| 战斗音效 | Pending | 攻击、受伤、死亡音效 |

---

## 技术参考

### 战斗流程详解

```
1. 触发战斗
   WorldContext.TriggerBattle(enemyArmy)
   ├── 暂停 WorldContext
   └── 创建 BattleContext

2. 初始化
   BattleContext.StartBattle(playerArmy, enemyArmy)
   ├── 创建 BattleField(15x11 六边形)
   ├── 部署玩家单位(左侧)
   └── 部署敌方单位(右侧)

3. 战斗循环
   while (!IsOver)
   {
       // 确定行动顺序
       TurnManager.CalculateTurnOrder()

       // 处理每个单位的行动
       foreach (unit in turnOrder)
       {
           if (unit.IsPlayerControlled)
               WaitForPlayerInput()  // UI 交互
           else
               AIController.DecideAction()  // AI 决策

           ActionResolver.Execute(action)
           CheckVictoryCondition()
       }
   }

4. 战斗结束
   BattleContext.EndBattle(result)
   ├── 计算经验值
   ├── 分配战利品
   ├── 更新军队状态
   └── 返回 WorldContext
```

### 伤害计算公式

```
基础伤害 = Random(minDamage, maxDamage) * 单位数量

攻防修正:
- 攻击 > 防御: +5% * (攻击 - 防御), 最高 +300%
- 防御 > 攻击: -2.5% * (防御 - 攻击), 最高 -70%

最终伤害 = 基础伤害 * (1 + 攻防修正) * 其他修正

堆叠损失:
- 伤害先分配给已受伤单位
- 剩余伤害击杀完整单位
- 残余伤害计入下一个单位
```

### 六边形网格

```
战场布局 (15x11):

  [0,0] [0,1] [0,2] ... [0,14]     <- 敌方后排
    [1,0] [1,1] [1,2] ...          <- 偏移行
  [2,0] [2,1] [2,2] ...
    ...
  [10,0] [10,1] ...                <- 玩家前排

坐标系统: Offset Coordinates (奇数行偏移)
邻居计算: 6 个方向(偶数行/奇数行不同偏移)
```

### 上下文架构

```
RootContext (全局)
└── SessionContext (存档会话: 玩家数据、天数、资源)
    ├── WorldContext (探索: 地图、英雄移动)
    │   └── TownContext (城镇: 建筑、招募) <-> UI 覆盖层
    └── BattleContext (战斗: 战场、回合、单位)
```

### 场景流程

城镇界面采用 UI 覆盖层方式，不再使用独立场景：

```
WorldScene (单一场景)
    |
    ├── WorldMap (Tilemap, 英雄, 城镇图标)
    ├── WorldUI (Canvas: 资源条, 小地图, 按钮)
    └── TownPanel (Canvas: 全屏城镇界面, 默认隐藏)

流程:
1. 点击城镇图标 -> WorldContext.Pause()
2. 创建 TownContext -> TownPanel.Bind(context)
3. 建造/招募/查看
4. 点击离开 -> TownPanel.Hide() -> DisposeChild<TownContext>()
5. WorldContext.Resume() -> 继续探索
```

---

