# TH7 战斗系统指南

## 概述

战斗系统实现了回合制战斗，支持从世界地图进入战斗并返回的完整游戏循环。

## 架构

```
SessionContext (存档)
├── WorldContext (探索) [战斗时 Pause]
└── BattleContext (战斗) [战斗时创建]
```

## 核心组件

### 1. BattleContext
战斗上下文，管理战斗状态、单位和地图。

### 2. BattleMap
15x11 格子战场，支持 A* 寻路。

### 3. BattleUnit
继承 `GameStateMachineBehaviour`，所有属性来自 `UnitConfig`。

### 4. BattleTurnManager
回合管理器，状态：
- Idle → RoundStart → SelectUnit → WaitingForAction → ExecutingAction → RoundEnd

### 5. InitiativeQueue
基于速度的行动顺序队列。

### 6. BattleActionExecutor
执行移动、攻击、技能等行动。

## 使用流程

### 进入战斗
```csharp
// WorldSceneController 中
[AutoSubscribe]
void OnBattleRequested(BattleRequestedEvent e)
{
    StartCoroutine(EnterBattle(e.Hero, e.Enemy));
}
```

### 退出战斗
```csharp
// BattleSceneController 中
public void ExitBattle()
{
    sessionContext?.DisposeChild<BattleContext>();
    SceneManager.UnloadSceneAsync("BattleScene");
}
```

## 文件结构

```
src/battle/
├── core/
│   └── BattleData.cs       # 数据结构
├── map/
│   ├── BattleMap.cs        # 地图管理
│   └── BattleTile.cs       # 格子
├── unit/
│   ├── BattleUnit.cs       # 单位核心
│   ├── BattleUnitStateMachine.cs
│   └── BattleUnitFactory.cs
├── turn/
│   ├── BattleTurnManager.cs
│   ├── BattleTurnStateMachine.cs
│   └── InitiativeQueue.cs
├── action/
│   ├── BattleAction.cs     # 行动类
│   └── BattleActionExecutor.cs
├── input/
│   ├── IBattleActionProvider.cs
│   ├── BattleActionProvider.cs
│   └── BattleInputController.cs
├── ai/
│   └── BattleAIProvider.cs
├── view/
│   └── BattleUnitView.cs
├── ui/
│   └── BattleHUD.cs
└── BattleEvents.cs
```

## 关键设计

1. **属性来自配置**: BattleUnit 所有属性取决于 UnitConfig
2. **Additive 场景加载**: WorldScene 保持加载，战斗结束后无需重新初始化
3. **复用 GAS 技能系统**: 通过 AbilitySystemComponent 管理属性和效果
4. **状态机驱动**: 单位和回合都使用 GameStateMachineBehaviour
5. **事件解耦**: 通过 EventSystem 通信

## 待实现

1. 创建 BattleScene.unity 场景
2. 配置 UnitConfigDatabase 中的测试单位
3. 添加战斗结果 UI
4. 实现伤害数字弹出效果
5. 添加更多 AI 策略
