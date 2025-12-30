# TH7 项目简介

> **项目代号**: TH7 (Tellanos: Heroes of Seven Realms)
> **中文名称**: 泰拉诺斯大陆：七境英雄
> **引擎**: Unity 6 (URP 2D)
> **类型**: 策略RPG / 文明建设

---

## 技术栈

| 类别 | 库 | 用途 |
|------|-----|------|
| 动画 | DOTween | 补间动画、UI 动效 |
| UI | Modern UI Pack | 现代风格 UI 组件 |
| 存档 | Easy Save 3 | 数据序列化、存档管理 |
| 相机 | Cinemachine | 相机控制 |

---

## 核心概念

七大文明并立的奇幻世界，玩家选择文明、招募英雄、训练军队、研究魔法，收集"神之守护碎片"决定大陆命运。

**七大文明**: 阿拉伯、埃及、印度、希腊、汉唐、蒙古、南岛

---

## 项目结构

```
Assets/                     # 项目根目录
  src/                      # 源代码
    framework/              # 框架层
      Core/                 # 核心入口
        GameEntry.cs        # 游戏入口单例，系统管理器
        IGameSystem.cs      # 系统接口
        GameBehaviour.cs    # MonoBehaviour扩展，自动订阅
      Event/                # 事件系统
        EventSystem.cs      # 发布订阅
        AutoSubscribeAttribute.cs
        AutoSubscribeProcessor.cs
      Context/              # 上下文 + 响应式
        GameContext.cs      # 上下文基类
        ContextSystem.cs    # 上下文管理
        Reactive.cs         # Reactive<T> + ReactiveList<T>
        Subscription.cs     # 统一订阅句柄
      UI/                   # UI 框架
        UIBehaviour.cs      # UI 基类，继承 GameBehaviour
        UIWindowBehaviour.cs # 窗口基类，DOTween/Animator 动画
        ScrollViewUI.cs     # 通用滚动列表
    context/                # 游戏上下文实现
      SessionContext.cs     # 存档会话上下文 + SessionData
      WorldContext.cs       # 探索上下文
      BattleContext.cs      # 战斗上下文
    scene/                  # 场景控制器
      BootController.cs     # 启动场景，初始化系统
      MainMenuController.cs # 主菜单，存档管理
      WorldSceneController.cs # 世界场景，创建 WorldContext
    ui/                     # UI 实现
      MainMenu/             # 主菜单 UI
        MainMenuUI.cs       # 主菜单界面
        SaveSlotPanel.cs    # 存档选择窗口
    map/                    # 地图系统
      Tile.cs               # 逻辑地块结构
      GameTile.cs           # Unity TileBase
      MapData.cs            # 运行时地图数据
      MapManager.cs         # 地图管理器
      TerrainConfig.cs      # 地形配置
    GameSystem.cs           # 调试入口
    GameEnums.cs            # 统一枚举定义
  docs/                     # 设计文档
  scenes/                   # 场景文件
  settings/                 # URP渲染设置
```

## 上下文架构

采用分层上下文管理游戏生命周期，支持嵌套、并行、暂停/恢复：

```
RootContext (全局)
└── SessionContext (存档会话: 玩家数据、天数、资源)
    ├── WorldContext (探索: 地图、英雄移动) ←→ 可暂停
    └── BattleContext (战斗: 战场、回合、单位) ←→ 与World并列
```

**核心API**:
- `CreateChild<T>()` - 创建子上下文
- `DisposeChild<T>()` - 销毁子上下文
- `Pause()` / `Resume()` - 暂停/恢复
- `GetParent<T>()` - 向上查找父上下文

## 启动流程

```
BootScene                           MainMenuScene                    WorldScene
    |                                    |                               |
BootController                    MainMenuController             WorldSceneController
    |                                    |                               |
初始化 GameEntry                  StartNewGame()                  获取 SessionContext
注册 EventSystem                       |                               |
注册 ContextSystem              创建 SessionContext             创建 WorldContext
    |                            (挂载到 Root)                         |
LoadScene(MainMenu)                    |                        Setup(MapManager)
                                LoadScene(World)                       |
                                                                 探索阶段开始
```

**调试模式**: 直接运行 WorldScene 时，GameSystem 会自动初始化系统（需手动处理 Context）

## 地图系统

采用 Unity Tilemap 可视化编辑 + 运行时逻辑数据分离：

```
设计时: Unity Tilemap Editor + GameTile 绘制地图
    ↓
运行时: MapManager.InitializeMap() 从 Tilemap 读取到 MapData
    ↓
逻辑层: MapData 用于寻路、移动力计算等
```

**地块分层**:
- `GroundType` - 基础地表 (Land/Water/DeepWater/Void)
- `SurfaceType` - 地表覆盖 (None/Road/Forest/Mountain/...)
- `BiomeType` - 视觉风格 (Neutral/Arabian/Egyptian/...)

## 命名规范

### 文明代号

| 文明 | 代号 |
|------|------|
| 阿拉伯 | `arb` |
| 埃及 | `egy` |
| 印度 | `ind` |
| 希腊 | `grk` |
| 汉唐 | `han` |
| 蒙古 | `mng` |
| 南岛 | `isl` |

### 代码规范 (C#)

| 类型 | 风格 | 示例 |
|------|------|------|
| 命名空间 | PascalCase | `GameFramework` |
| 类/接口 | PascalCase | `GameEntry`, `IGameSystem` |
| 方法 | PascalCase | `OnInit`, `GetSystem` |
| 私有字段 | camelCase | `currentProcedure`, `eventTable` |
| 参数 | camelCase | `deltaTime`, `eventData` |
| 常量 | ALL_CAPS | `MAX_UNIT_LEVEL` |
| 枚举 | PascalCase | `AutoSubscribeTime { Awake, Start }` |

### 资源命名

**美术**: `th7_[模块]_[文明]_[描述]_vXX.扩展名`
```
th7_unit_arb_cavalry_v01.png
th7_hero_han_strategist_v02.psd
```

**数据表**: `th7_[类型]_[文明]_design.xlsx`
```
th7_units_arb_design.xlsx
th7_spells_all.xlsx
```

---

## 编程偏好

1. **禁用 emoji** - 代码和注释中不使用表情符号
2. **少用 try-catch** - 优先通过逻辑判断避免异常
3. **少用 switch-case** - 优先使用多态、字典映射或状态模式
4. **代码在精不在多** - 简洁优先，避免过度封装
5. **避免重复造轮子** - 优先使用 Unity 内置功能和成熟方案
6. **枚举统一管理** - 所有枚举定义在 `src/GameEnums.cs` 中
7. **精简注释** - 只在声明/定义处写必要注释，简单的流程注释即可，避免过度注释
8. **优先新特性** - 优先使用 C# 和 Unity 的新语法特性（如模式匹配、表达式体成员、null 合并等）
9. **Unity 6 + 高级库** - 使用 Unity 6000，优先采用 Unity 6 新特性和成熟的第三方库（如 DOTween、UniTask、Addressables 等）
10. **模块文档** - 每个独立模块目录下维护 `guide.md` 文件，说明模块用途和使用方法

---

## 开发任务

### 当前里程碑: 城镇界面 MVP

目标：实现英雄无敌风格的城镇系统，包含资源管理、建筑树、招募功能。

### 模块完成状态

| 模块 | 状态 | 说明 |
|------|------|------|
| framework/ | Done | GameEntry、EventSystem、ContextSystem、GameBehaviour |
| framework/UI | Done | UIBehaviour、UIWindowBehaviour、ScrollViewUI |
| context/ | Done | SessionContext + ES3 存档、WorldContext、BattleContext(框架) |
| scene/ | Done | Boot、MainMenu、World 场景控制器 |
| ui/MainMenu | Done | MainMenuUI、SaveSlotPanel |
| ui/Common | Done | ResourceDisplayUI、ResourceBarUI、ResourceCostUI |
| ui/Town | Done | TownPanelUI、BuildingGridUI、BuildingInfoPanelUI、RecruitPanelUI |
| map/ | Done | Tilemap + 逻辑层分离，地形配置 |
| resource/ | Done | ResourceBundle、PlayerResources |
| town/ | Done | TownData、TownContext、BuildingConfig、TownConfigDatabase |
| unit/ | Done | UnitConfig、UnitConfigDatabase |

### 任务列表

#### P0 - 资源系统 (src/resource/) - Done
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 ResourceType 枚举 | Done | Gold/Wood/Ore/Crystal/Gem/Sulfur/Mercury |
| 创建 ResourceBundle | Done | 资源数据容器，支持运算 |
| 创建 PlayerResources | Done | 响应式资源，支持 UI 绑定 |
| 扩展 SessionContext | Done | 持有玩家资源数据 |

#### P0 - 城镇系统 (src/town/) - Done
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 BuildingType 枚举 | Done | TownHall/Tavern/Dwelling1-7 等 |
| 创建 BuildingConfig (SO) | Done | 建筑配置：成本、前置条件、产出 |
| 创建 TownData | Done | 城镇数据：建筑、驻军、可招募数量 |
| 创建 TownContext | Done | 城镇上下文，建造/招募逻辑 |
| 创建 TownConfigDatabase | Done | 建筑配置数据库 |

#### P0 - 兵种系统 (src/unit/) - Done
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 UnitTier 枚举 | Done | Tier1-7 兵种等级 |
| 创建 UnitConfig (SO) | Done | 兵种配置：属性、招募成本 |
| 创建 UnitStack 结构 | Done | 兵种堆叠：UnitId + Count |
| 创建 UnitConfigDatabase | Done | 兵种配置数据库 |
| 实现招募逻辑 | Done | TownContext.Recruit() |

#### P1 - UI 框架 (src/ui/) - Done
| 任务 | 状态 | 说明 |
|------|------|------|
| UIBehaviour 基类 | Done | 继承 GameBehaviour，Listen ReactiveList |
| UIWindowBehaviour | Done | 窗口动画（DOTween/Animator） |
| ScrollViewUI | Done | 通用滚动列表，支持 ReactiveList 绑定 |
| ResourceDisplayUI | Done | 单个资源显示 |
| ResourceBarUI | Done | 顶部资源显示条 |
| TownPanelUI | Done | 城镇主界面 |
| BuildingGridUI | Done | 建筑网格 |
| BuildingInfoPanelUI | Done | 建筑详情弹窗 |
| RecruitPanelUI | Done | 招募界面 |

#### P2 - 数据持久化 (Easy Save 3) - Done
| 任务 | 状态 | 说明 |
|------|------|------|
| Reactive<T> ES3 集成 | Done | SavedValue 属性，不存储监听器 |
| ReactiveList<T> ES3 集成 | Done | SavedItems 属性 |
| SessionData 存档结构 | Done | PlayerName、Day、Resources、Towns |
| SaveSlotPanel | Done | 存档选择窗口 |
| MainMenu 读档功能 | Done | 加载存档恢复游戏状态 |

### 上下文架构 (更新)

```
RootContext (全局)
└── SessionContext (存档会话: 玩家数据、天数、资源)
    ├── WorldContext (探索: 地图、英雄移动)
    │   └── TownContext (城镇: 建筑、招募) ←→ UI 覆盖层
    └── BattleContext (战斗: 战场、回合、单位)
```

### 场景流程 (更新)

城镇界面采用 UI 覆盖层方式，不再使用独立场景：

```
WorldScene (单一场景)
    │
    ├── WorldMap (Tilemap, 英雄, 城镇图标)
    ├── WorldUI (Canvas: 资源条, 小地图, 按钮)
    └── TownPanel (Canvas: 全屏城镇界面, 默认隐藏)

流程:
1. 点击城镇图标 → WorldContext.Pause()
2. 创建 TownContext → TownPanel.Bind(context)
3. 建造/招募/查看
4. 点击离开 → TownPanel.Hide() → DisposeChild<TownContext>()
5. WorldContext.Resume() → 继续探索
```

---

## 下一里程碑: 战斗系统 MVP

目标：实现英雄无敌风格的回合制战斗系统，包含战场、单位、回合管理、伤害计算。

### 战斗系统概述

```
触发战斗                              战斗流程
    │                                    │
WorldContext.TriggerBattle()        BattleContext
    │                                    │
暂停探索 ──────────────────────> 初始化战场
创建 BattleContext                  部署阶段（可选）
    │                                    │
    │                               战斗循环:
    │                               ├── 确定行动顺序（按速度）
    │                               ├── 当前单位行动
    │                               │   ├── 移动
    │                               │   ├── 攻击
    │                               │   ├── 等待
    │                               │   └── 防御
    │                               └── 检查胜负
    │                                    │
恢复 WorldContext <───────────── 战斗结束
销毁 BattleContext                  结算（经验、战利品）
```

### 模块规划

| 模块 | 状态 | 说明 |
|------|------|------|
| battle/ | Pending | 战斗核心逻辑 |
| battle/data | Pending | 战斗数据结构 |
| ui/Battle | Pending | 战斗 UI 界面 |
| context/BattleContext | Pending | 战斗上下文（已有框架） |

### 任务列表

#### P0 - 战场数据 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 BattleConfig | Pending | 战场配置（尺寸、地形） |
| 创建 BattleField | Pending | 战场数据（六边形网格） |
| 创建 BattleUnit | Pending | 战斗单位（UnitStack + 运行时状态） |
| 创建 BattleArmy | Pending | 军队数据（7 个槽位） |
| 定义 BattleAction | Pending | 行动类型枚举（Move/Attack/Wait/Defend） |

#### P0 - 回合管理 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 TurnManager | Pending | 回合管理器，按速度排序 |
| 创建 ActionResolver | Pending | 行动执行器 |
| 实现移动逻辑 | Pending | 六边形寻路 + 移动力消耗 |
| 实现攻击逻辑 | Pending | 近战/远程判定 |
| 实现等待/防御 | Pending | 行动顺序调整 |

#### P0 - 伤害系统 (src/battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 DamageCalculator | Pending | 伤害计算公式 |
| 实现攻防修正 | Pending | 攻击>防御: +5%/点, 防御>攻击: -2.5%/点 |
| 实现反击机制 | Pending | 首次被攻击时反击 |
| 实现堆叠损失 | Pending | 伤害分摊到堆叠单位 |

#### P1 - 战斗 UI (src/ui/Battle/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 BattleSceneUI | Pending | 战斗场景主 UI |
| 创建 BattleFieldView | Pending | 战场六边形网格显示 |
| 创建 BattleUnitView | Pending | 单位显示（精灵 + 数量） |
| 创建 ActionBarUI | Pending | 行动按钮栏 |
| 创建 TurnOrderUI | Pending | 行动顺序预览 |
| 创建 BattleResultUI | Pending | 战斗结果窗口 |

#### P1 - BattleContext 扩展 (src/context/)

| 任务 | 状态 | 说明 |
|------|------|------|
| 扩展 BattleContext | Pending | 战斗状态管理 |
| 实现 StartBattle() | Pending | 初始化战场、部署单位 |
| 实现 ProcessTurn() | Pending | 处理当前单位行动 |
| 实现 EndBattle() | Pending | 结算、返回探索 |
| AI 基础行为 | Pending | 简单 AI（攻击最近敌人） |

#### P2 - 高级功能（可选）

| 任务 | 状态 | 说明 |
|------|------|------|
| 士气系统 | Pending | 正士气额外行动，负士气恐慌 |
| 幸运系统 | Pending | 幸运暴击双倍伤害 |
| 远程攻击 | Pending | 弹药、近身惩罚 |
| 战斗动画 | Pending | DOTween 移动/攻击动画 |
| 战斗音效 | Pending | 攻击、受伤、死亡音效 |

### 战斗流程详解

```
1. 触发战斗
   WorldContext.TriggerBattle(enemyArmy)
   ├── 暂停 WorldContext
   └── 创建 BattleContext

2. 初始化
   BattleContext.StartBattle(playerArmy, enemyArmy)
   ├── 创建 BattleField（15x11 六边形）
   ├── 部署玩家单位（左侧）
   └── 部署敌方单位（右侧）

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
基础伤害 = Random(minDamage, maxDamage) × 单位数量

攻防修正:
- 攻击 > 防御: +5% × (攻击 - 防御), 最高 +300%
- 防御 > 攻击: -2.5% × (防御 - 攻击), 最高 -70%

最终伤害 = 基础伤害 × (1 + 攻防修正) × 其他修正

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
邻居计算: 6 个方向（偶数行/奇数行不同偏移）
```

---

## 相关文档

- [docs/TH7_GameDesignDocument.md](docs/TH7_GameDesignDocument.md) - 完整游戏设计文档
- [docs/HoMM3_GameMechanics.md](docs/HoMM3_GameMechanics.md) - 英雄无敌3机制详解（设计参考）

## 模块指南

| 模块 | 指南 | 说明 |
|------|------|------|
| 框架层 | [src/framework/guide.md](src/framework/guide.md) | 系统管理、事件、上下文、响应式数据、ES3 集成 |
| UI 框架 | [src/framework/UI/guide.md](src/framework/UI/guide.md) | UIBehaviour、窗口、滚动列表 |
| 上下文 | [src/context/guide.md](src/context/guide.md) | Session、World、Battle 上下文 |
| 场景 | [src/scene/guide.md](src/scene/guide.md) | 场景控制器、启动流程 |
| 地图系统 | [src/map/guide.md](src/map/guide.md) | Tilemap 绘制 + 逻辑数据分离 |
| Unity 操作 | [docs/UnitySetup.md](docs/UnitySetup.md) | 场景配置、预制体创建、SO 设置 |
