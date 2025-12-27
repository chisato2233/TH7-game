# Context 模块

> 游戏上下文实现：管理游戏各阶段的状态和生命周期

## 文件说明

| 文件 | 职责 |
|------|------|
| `SessionContext.cs` | 存档会话，管理玩家数据、天数 |
| `WorldContext.cs` | 探索阶段，管理地图、英雄 |
| `BattleContext.cs` | 战斗阶段，管理战场、回合 |

---

## 上下文层级

```
RootContext (全局)
└── SessionContext (存档会话)
    ├── WorldContext (探索) ←→ 可暂停
    ├── TownContext (城镇)  ←→ TODO
    └── BattleContext (战斗) ←→ 与 World 并列
```

---

## SessionContext

存档会话生命周期：加载存档 → 退出存档

```csharp
// 开始新游戏
session.StartNewSession("PlayerName");

// 加载存档
session.LoadSession("save01.json");

// 进入探索
var world = session.StartExploration();

// 推进天数
session.AdvanceDay();
```

| 属性 | 说明 |
|------|------|
| `PlayerName` | 玩家名 |
| `CurrentDay` | 当前天数 |
| `CurrentWeek` | 当前周 |
| `CurrentMonth` | 当前月 |

---

## WorldContext

探索阶段生命周期：进入大地图 → 离开大地图

```csharp
// 设置地图管理器
world.Setup(mapManager);

// 触发战斗（自动暂停 World，创建 Battle）
var battle = world.TriggerBattle();

// 结束回合
world.EndTurn();
```

| 属性 | 说明 |
|------|------|
| `Map` | MapManager 引用 |
| `CurrentHeroIndex` | 当前英雄索引 |
| `IsHeroMoving` | 英雄移动中 |

---

## BattleContext

战斗生命周期：进入战斗 → 战斗结束

```csharp
// 开始新回合
battle.StartNewRound();

// 切换阶段
battle.SetPhase(BattlePhase.ActionSelect);

// 结束战斗
battle.EndBattle(BattleResult.Victory);

// 撤退
battle.Retreat();
```

### 战斗阶段 (BattlePhase)

| 阶段 | 说明 |
|------|------|
| `Init` | 初始化 |
| `RoundStart` | 回合开始 |
| `UnitSelect` | 选择单位 |
| `ActionSelect` | 选择行动 |
| `ActionExecute` | 执行行动 |
| `RoundEnd` | 回合结束 |
| `BattleEnd` | 战斗结束 |

### 战斗结果 (BattleResult)

| 结果 | 说明 |
|------|------|
| `None` | 未结束 |
| `Victory` | 胜利 |
| `Defeat` | 失败 |
| `Retreat` | 撤退 |

---

## 典型流程

```
SessionContext.StartExploration()
    ↓
WorldContext.TriggerBattle()
    ↓ (World 暂停)
BattleContext 处理战斗
    ↓
BattleContext.EndBattle()
    ↓ (World 恢复)
继续探索
```
