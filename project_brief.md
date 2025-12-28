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
        Reactive.cs         # 响应式数据
        Subscription.cs     # 统一订阅句柄
    context/                # 游戏上下文实现
      SessionContext.cs     # 存档会话上下文
      WorldContext.cs       # 探索上下文
      BattleContext.cs      # 战斗上下文
    scene/                  # 场景控制器
      BootController.cs     # 启动场景，初始化系统
      MainMenuController.cs # 主菜单，创建 SessionContext
      WorldSceneController.cs # 世界场景，创建 WorldContext
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
| context/ | Done | SessionContext、WorldContext、BattleContext(框架) |
| scene/ | Done | Boot、MainMenu、World 场景控制器 |
| map/ | Done | Tilemap + 逻辑层分离，地形配置 |
| resource/ | TODO | 资源系统 |
| town/ | TODO | 城镇系统 |
| unit/ | TODO | 兵种系统 |
| ui/ | TODO | UI 框架 |

### 任务列表

#### P0 - 资源系统 (src/resource/)
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 ResourceType 枚举 | TODO | Gold/Wood/Ore/Crystal/Gem/Sulfur/Mercury |
| 创建 ResourceData 结构 | TODO | 资源数据容器 |
| 创建 ResourceManager | TODO | 资源增减、查询、事件通知 |
| 扩展 SessionContext | TODO | 持有玩家资源数据 |

#### P0 - 城镇系统 (src/town/)
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 BuildingType 枚举 | TODO | 城堡/酒馆/市场/兵营/魔法塔等 |
| 创建 BuildingConfig (SO) | TODO | 建筑配置：名称、图标、建造成本、前置条件、产出 |
| 创建 TownData | TODO | 城镇运行时数据：已建建筑、建造队列 |
| 创建 TownContext | TODO | 城镇上下文，作为 SessionContext 子节点 |
| 实现建筑树逻辑 | TODO | 前置条件检查、解锁判断 |
| 创建 TownScene | TODO | 城镇场景 |
| 创建 TownSceneController | TODO | 城镇场景控制器 |

#### P0 - 兵种系统 (src/unit/)
| 任务 | 状态 | 说明 |
|------|------|------|
| 定义 UnitType 枚举 | TODO | 各文明基础兵种标识 |
| 创建 UnitConfig (SO) | TODO | 兵种配置：属性、招募成本、所需建筑 |
| 创建 UnitStack 结构 | TODO | 兵种堆叠：类型+数量 |
| 实现招募逻辑 | TODO | 消耗资源、检查建筑、加入军队 |

#### P1 - UI 框架 (src/ui/)
| 任务 | 状态 | 说明 |
|------|------|------|
| 创建 UIManager | TODO | UI 层级管理、面板栈 |
| 创建 ResourcePanel | TODO | 顶部资源显示条 |
| 创建 TownPanel | TODO | 城镇主界面：建筑网格、信息区 |
| 创建 BuildingInfoPanel | TODO | 建筑详情弹窗 |
| 创建 RecruitPanel | TODO | 招募界面 |

#### P2 - 数据持久化 (Easy Save 3)
| 任务 | 状态 | 说明 |
|------|------|------|
| 设计存档数据结构 | TODO | SessionSaveData、TownSaveData |
| 实现 SaveManager | TODO | 使用 ES3 存档管理 |
| MainMenu 读档功能 | TODO | 加载存档恢复游戏状态 |

### 上下文架构 (更新)

```
RootContext (全局)
└── SessionContext (存档会话: 玩家数据、天数、资源)
    ├── WorldContext (探索: 地图、英雄移动)
    ├── TownContext (城镇: 建筑、招募) ←→ 新增
    └── BattleContext (战斗: 战场、回合、单位)
```

### 场景流程 (更新)

```
WorldScene                          TownScene
    |                                   |
点击城镇图标                      TownSceneController
    |                                   |
暂停 WorldContext               创建 TownContext
LoadScene(Town)                  显示城镇界面
    |                                   |
    |                            建造/招募/离开
    |                                   |
    ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
恢复 WorldContext               销毁 TownContext
                                返回 WorldScene
```

---

## 相关文档

- [docs/TH7_GameDesignDocument.md](docs/TH7_GameDesignDocument.md) - 完整游戏设计文档
- [docs/HoMM3_GameMechanics.md](docs/HoMM3_GameMechanics.md) - 英雄无敌3机制详解（设计参考）

## 模块指南

| 模块 | 指南 | 说明 |
|------|------|------|
| 框架层 | [src/framework/guide.md](src/framework/guide.md) | 系统管理、事件、上下文、响应式数据 |
| 上下文 | [src/context/guide.md](src/context/guide.md) | Session、World、Battle 上下文 |
| 场景 | [src/scene/guide.md](src/scene/guide.md) | 场景控制器、启动流程 |
| 地图系统 | [src/map/guide.md](src/map/guide.md) | Tilemap 绘制 + 逻辑数据分离 |
