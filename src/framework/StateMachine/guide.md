# StateMachine - 通用状态机

基于泛型和反射的通用状态机系统，支持自动状态注册、条件转换、层级状态机。

---

## 核心类

| 类 | 说明 |
|---|---|
| `StateMachine<TState, TOwner>` | 核心状态机，管理状态和转换 |
| `IState<TOwner>` | 状态接口 |
| `StateBase<TOwner>` | 状态基类，提供默认空实现 |
| `StateTransition` | 状态转换规则 |
| `StateBindingAttribute` | 状态绑定特性，支持自动注册 |
| `StateMachineBehaviour` | MonoBehaviour 封装 |
| `HierarchicalStateMachine` | 层级状态机（支持子状态机） |

---

## 基础用法

### 1. 定义状态枚举

```csharp
public enum PlayerState
{
    Idle,
    Walking,
    Running,
    Jumping,
    Falling
}
```

### 2. 定义上下文

上下文是状态之间共享的数据容器：

```csharp
public class PlayerOwner
{
    public float MoveSpeed;
    public bool IsGrounded;
    public Vector3 Velocity;
    public Animator Animator;
    public CharacterController Controller;
}
```

### 3. 实现状态

**方式 A: 继承 StateBase（推荐）**

```csharp
public class IdleState : StateBase<PlayerOwner>
{
    public override void OnEnter(PlayerOwner owner)
    {
        owner.Animator.Play("Idle");
    }

    public override void OnUpdate(PlayerOwner owner, float deltaTime)
    {
        // 在 Update 中检测输入等
    }
}
```

**方式 B: 使用特性自动绑定**

```csharp
[StateBinding(typeof(PlayerState), PlayerState.Idle)]
public class IdleState : StateBase<PlayerOwner>
{
    // ...
}
```

### 4. 创建状态机

```csharp
var stateMachine = new StateMachine<PlayerState, PlayerOwner>();

// 手动注册状态
stateMachine.RegisterState(PlayerState.Idle, new IdleState());
stateMachine.RegisterState(PlayerState.Walking, new WalkingState());

// 或使用泛型注册
stateMachine.RegisterState<JumpingState>(PlayerState.Jumping);

// 初始化（autoRegisterStates=true 会自动注册带 StateBinding 特性的状态）
stateMachine.Initialize(playerOwner, PlayerState.Idle, autoRegisterStates: true);
```

### 5. 添加转换规则

```csharp
// 从 Idle 到 Walking：当速度 > 0
stateMachine.AddTransition(
    PlayerState.Idle,
    PlayerState.Walking,
    owner => owner.Velocity.magnitude > 0.1f
);

// 任意状态到 Falling：当不在地面
stateMachine.AddAnyTransition(
    PlayerState.Falling,
    owner => !owner.IsGrounded,
    priority: 100  // 高优先级
);
```

### 6. 更新状态机

```csharp
void Update()
{
    stateMachine.Update(Time.deltaTime);
}

void FixedUpdate()
{
    stateMachine.FixedUpdate(Time.fixedDeltaTime);
}
```

---

## StateMachineBehaviour 用法

对于需要挂载到 GameObject 的场景：

```csharp
public class PlayerStateMachine : StateMachineBehaviour<PlayerState, PlayerOwner>
{
    PlayerOwner owner;

    protected override PlayerOwner GetOwner() => owner;
    protected override PlayerState GetInitialState() => PlayerState.Idle;

    protected override void Awake()
    {
        base.Awake();
        owner = new PlayerOwner
        {
            Animator = GetComponent<Animator>(),
            Controller = GetComponent<CharacterController>()
        };
    }

    protected override void ConfigureStateMachine()
    {
        // 注册状态
        StateMachine.RegisterState<IdleState>(PlayerState.Idle);
        StateMachine.RegisterState<WalkingState>(PlayerState.Walking);

        // 添加转换
        StateMachine.AddTransition(PlayerState.Idle, PlayerState.Walking,
            owner => owner.Velocity.magnitude > 0.1f);
    }
}
```

---

## 层级状态机

支持状态内嵌套子状态机：

```csharp
// 主状态
public enum CombatState { Idle, InCombat, Dead }

// 战斗子状态
public enum CombatSubState { Attacking, Defending, Casting }

// 创建层级状态机
var hsm = new HierarchicalStateMachine<CombatState, CombatOwner>();

// 创建子状态机
var combatSub = new StateMachine<CombatSubState, CombatOwner>();
combatSub.RegisterState<AttackingState>(CombatSubState.Attacking);

// 注册子状态机到父状态
hsm.RegisterSubStateMachine(CombatState.InCombat, combatSub);
```

---

## 事件监听

```csharp
stateMachine.OnStateChanged += (from, to) =>
{
    Debug.Log($"State changed: {from} -> {to}");
};

stateMachine.OnStateEntered += state =>
{
    Debug.Log($"Entered: {state}");
};

stateMachine.OnStateExited += state =>
{
    Debug.Log($"Exited: {state}");
};
```

---

## API 参考

### StateMachine<TState, TOwner>

| 方法 | 说明 |
|---|---|
| `Initialize(owner, initialState, autoRegister)` | 初始化状态机 |
| `RegisterState(state, instance)` | 注册状态实例 |
| `RegisterState<T>(state)` | 注册状态类型 |
| `AddTransition(from, to, condition, priority)` | 添加转换规则 |
| `AddAnyTransition(to, condition, priority)` | 添加任意状态转换 |
| `Update(deltaTime)` | 每帧更新 |
| `FixedUpdate(fixedDeltaTime)` | 固定更新 |
| `ChangeState(newState)` | 强制切换状态 |
| `IsInState(state)` | 检查当前状态 |
| `Stop()` | 停止状态机 |
| `Resume()` | 恢复状态机 |

| 属性 | 说明 |
|---|---|
| `CurrentState` | 当前状态 |
| `PreviousState` | 上一个状态 |
| `StateTime` | 当前状态持续时间 |
| `IsRunning` | 是否运行中 |
| `Owner` | 上下文对象 |

### IState<TOwner>

| 方法 | 说明 |
|---|---|
| `OnEnter(owner)` | 进入状态 |
| `OnUpdate(owner, deltaTime)` | 每帧更新 |
| `OnFixedUpdate(owner, fixedDeltaTime)` | 固定更新 |
| `OnExit(owner)` | 退出状态 |

---

## 最佳实践

1. **上下文设计** - 将所有共享数据放入 Owner，避免状态间直接引用
2. **转换优先级** - AnyTransition 默认优先级 100，普通转换默认 0，优先级高的先检查
3. **状态职责单一** - 每个状态只负责一件事，复杂逻辑用子状态机
4. **使用特性绑定** - 大型项目推荐使用 `StateBindingAttribute` 自动注册
5. **调试模式** - `StateMachineBehaviour` 支持 Inspector 显示当前状态

---

## 示例：回合制战斗

```csharp
public enum BattlePhase { Init, PlayerTurn, EnemyTurn, Victory, Defeat }

public class BattleOwner
{
    public List<Unit> PlayerUnits;
    public List<Unit> EnemyUnits;
    public Unit CurrentUnit;
    public bool AllEnemiesDead => EnemyUnits.All(u => u.IsDead);
    public bool AllPlayersDead => PlayerUnits.All(u => u.IsDead);
}

[StateBinding(typeof(BattlePhase), BattlePhase.PlayerTurn)]
public class PlayerTurnState : StateBase<BattleOwner>
{
    public override void OnEnter(BattleOwner owner)
    {
        // 显示 UI，等待玩家输入
    }
}

// 使用
var battle = new StateMachine<BattlePhase, BattleOwner>();
battle.AddTransition(BattlePhase.PlayerTurn, BattlePhase.EnemyTurn,
    owner => owner.CurrentUnit == null);  // 玩家行动结束
battle.AddAnyTransition(BattlePhase.Victory, owner => owner.AllEnemiesDead);
battle.AddAnyTransition(BattlePhase.Defeat, owner => owner.AllPlayersDead);
```
