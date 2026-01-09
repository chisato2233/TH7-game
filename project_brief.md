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
      Ability/              # 技能系统 (GAS) - GameFramework 命名空间
        Tag/                # 标签系统
          GameplayTag.cs    # 层级标签
          GameplayTagContainer.cs # 标签容器
        Attribute/          # 属性系统
          AttributeType.cs  # 属性定义 + 定义数据库
          AttributeModifier.cs # 属性修改器
          ReactiveAttribute.cs # 响应式属性
          AttributeSet.cs   # 属性集合
        Effect/             # 效果系统
          GameplayEffect.cs # 效果配置 (SO)
          EffectInstance.cs # 效果实例
        Ability/            # 技能系统
          GameplayAbility.cs # 技能配置 (SO)
          AbilityInstance.cs # 技能实例
        Core/               # 核心组件
          AbilitySystemComponent.cs # 技能系统组件
          AbilityDatabase.cs # 技能数据库
          AbilityEvents.cs  # 事件定义
        Editor/             # 编辑器扩展
          GameplayEffectEditor.cs
          GameplayAbilityEditor.cs
          AbilityDatabaseEditor.cs
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
    world/                  # 世界探索系统
      hero/                 # 英雄模块
        Hero.cs             # 英雄组件（Data+Controller+View）
        HeroConfig.cs       # 英雄配置 (SO)
        HeroConfigDatabase.cs # 英雄配置数据库 (SO)
        HeroSaveData.cs     # 英雄存档数据
      action/               # 行动系统
        HeroAction.cs       # 行动基类和具体行动
        ActionResult.cs     # 行动结果
        ActionExecutor.cs   # 行动执行器
      provider/             # 行动提供者
        IActionProvider.cs  # 行动提供者接口
        PlayerActionProvider.cs # 玩家输入提供者
        IPathfinder.cs      # 寻路接口
      input/                # 输入系统
        WorldInputController.cs # 输入控制器
        CinemachineCameraController.cs # 相机控制
      WorldTurnManager.cs   # 回合管理器
      WorldEvents.cs        # 世界事件定义
    town/                   # 城镇系统
      TownData.cs           # 城镇数据
      TownContext.cs        # 城镇上下文
      BuildingConfig.cs     # 建筑配置 (SO)
      TownConfigDatabase.cs # 城镇配置数据库 (SO)
    unit/                   # 兵种系统
      UnitConfig.cs         # 兵种配置 (SO)
      UnitConfigDatabase.cs # 兵种配置数据库 (SO)
      UnitStack.cs          # 兵种堆叠
    resource/               # 资源系统
      ResourceBundle.cs     # 资源包
      PlayerResources.cs    # 玩家资源（响应式）
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



## ScriptableObject 配置系统

项目使用 ScriptableObject 进行数据驱动配置，分为 **单项配置** 和 **数据库** 两类：

### 配置文件列表

| 类型 | 文件 | 菜单路径 | 用途 |
|------|------|----------|------|
| **城镇配置** ||||
| Database | TownConfigDatabase | TH7/Town Config Database | 建筑配置数据库 |
| Config | BuildingConfig | TH7/Building Config | 单个建筑配置 |
| **兵种配置** ||||
| Database | UnitConfigDatabase | TH7/Unit Config Database | 兵种配置数据库 |
| Config | UnitConfig | TH7/Unit Config | 单个兵种配置 |
| **地形配置** ||||
| Config | TerrainConfig | TH7/Terrain Config | 地形移动力/通行配置 |
| **英雄配置** ||||
| Database | HeroConfigDatabase | TH7/Hero Config Database | 英雄配置数据库 |
| Config | HeroConfig | TH7/Hero Config | 单个英雄配置 |
| **技能配置** ||||
| Database | AbilityDatabase | TH7/Ability/Ability Database | 技能和效果数据库 |
| Config | GameplayAbility | TH7/Ability/Gameplay Ability | 单个技能配置 |
| Config | GameplayEffect | TH7/Ability/Gameplay Effect | 单个效果配置 |
| Config | AttributeDefinitionDatabase | TH7/Ability/Attribute Definition Database | 属性定义数据库 |

### 配置结构

```
Assets/
  data/                           # 配置资源目录（建议）
    config/
      TownConfigDatabase.asset    # 城镇配置数据库
      UnitConfigDatabase.asset    # 兵种配置数据库
      TerrainConfig.asset         # 地形配置
      AbilityDatabase.asset       # 技能数据库
    buildings/                    # 建筑配置
      Building_TownHall.asset
      Building_Tavern.asset
      ...
    units/                        # 兵种配置
      Unit_grk_hoplite.asset
      Unit_grk_pegasus.asset
      ...
    abilities/                    # 技能配置
      Ability_Fireball.asset
      Effect_Burn.asset
      ...
    heroes/                       # 英雄配置
      Hero_grk_achilles.asset
      Hero_arb_saladin.asset
      ...
```

### 运行时引用

配置在 WorldSceneController 中通过 SerializeField 引用：

```csharp
[Header("Config")]
[SerializeField] TownConfigDatabase townConfigDatabase;
[SerializeField] UnitConfigDatabase unitConfigDatabase;
[SerializeField] HeroConfigDatabase heroConfigDatabase;
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
| 技能系统 | [src/framework/Ability/guide.md](src/framework/Ability/guide.md) | GAS: 标签、响应式属性、效果、技能、ASC |
| 上下文 | [src/context/guide.md](src/context/guide.md) | Session、World、Battle 上下文 |
| 场景 | [src/scene/guide.md](src/scene/guide.md) | 场景控制器、启动流程 |
| 地图系统 | [src/map/guide.md](src/map/guide.md) | Tilemap 绘制 + 逻辑数据分离 |
| Unity 操作 | [docs/UnitySetup.md](docs/UnitySetup.md) | 场景配置、预制体创建、SO 设置 |
