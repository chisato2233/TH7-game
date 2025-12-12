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
      ProcedureSystem.cs    # 流程状态机
      BaseProcedure.cs      # 流程基类
      GameBehaviour.cs      # MonoBehaviour扩展，自动事件订阅
    GameSystem.cs           # 游戏启动入口，注册系统
  docs/                     # 设计文档
    source/                 # 原始设计文档
    TH7_GameDesignDocument.md
  scenes/                   # 场景文件
  settings/                 # URP渲染设置
```

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
3. **代码在精不在多** - 简洁优先，避免过度封装
4. **避免重复造轮子** - 优先使用 Unity 内置功能和成熟方案

---

## 相关文档

- [docs/TH7_GameDesignDocument.md](docs/TH7_GameDesignDocument.md) - 完整游戏设计文档
