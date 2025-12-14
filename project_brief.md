# TH7 项目简介

> **项目代号**: TH7 (Tellanos: Heroes of Seven Realms)
> **中文名称**: 泰拉诺斯大陆：七境英雄
> **引擎**: Unity (URP 2D)
> **类型**: 策略RPG / 文明建设

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
      GameEntry.cs          # 游戏入口单例，系统管理器
      IGameSystem.cs        # 系统接口 (Priority/Init/Update/Shutdown)
      EventSystem.cs        # 事件系统 (发布订阅)
      GameContext.cs        # 上下文基类 (分层生命周期管理)
      ContextSystem.cs      # 上下文系统 (管理游戏上下文树)
      GameBehaviour.cs      # MonoBehaviour扩展，自动事件订阅
    context/                # 游戏上下文实现
      SessionContext.cs     # 存档会话上下文
      WorldContext.cs       # 探索上下文 (持有 MapManager 引用)
      BattleContext.cs      # 战斗上下文
    scene/                  # 场景控制器
      BootController.cs     # 启动场景，初始化系统
      MainMenuController.cs # 主菜单，创建 SessionContext
      WorldSceneController.cs # 世界场景，创建 WorldContext 并关联 MapManager
    map/                    # 地图系统
      Tile.cs               # 逻辑地块结构
      GameTile.cs           # Unity TileBase，用于 Tilemap 绘制
      MapData.cs            # 运行时地图数据，从 Tilemap 读取
      MapManager.cs         # 地图管理器
      TerrainConfig.cs      # 地形配置 (移动力、可通行性)
    GameSystem.cs           # 调试入口，非 Boot 场景直接运行时初始化
    GameEnums.cs            # 统一枚举定义文件
  docs/                     # 设计文档
    source/                 # 原始设计文档
    TH7_GameDesignDocument.md
  scenes/                   # 场景文件
    BootScene               # 启动场景 (Build Settings 第一个)
    MainMenuScene           # 主菜单场景
    WorldScene              # 世界地图场景
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

---

## 相关文档

- [docs/TH7_GameDesignDocument.md](docs/TH7_GameDesignDocument.md) - 完整游戏设计文档
