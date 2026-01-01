# Ability System Guide (GAS for Unity)

> 基于 Unreal Engine GameplayAbilitySystem 概念的 Unity 实现
> **完全通用化设计 - 可用于任意 Unity 项目**

## 概述

技能系统提供了一套完整的属性管理、效果系统和技能系统，用于处理游戏中的各种能力和状态效果。

**核心特点：**
- 完全通用 - 不依赖特定项目的枚举或类型
- 字符串属性 - 属性使用字符串标识，可自由定义任意属性
- 响应式属性 - ReactiveAttribute 支持 Watch 监听变化
- 层级标签 - 灵活的标签系统支持层级匹配
- ScriptableObject - 效果和技能配置使用 SO，支持编辑器配置
- 自定义 Editor - 提供友好的 Inspector 界面

```
AbilitySystemComponent (核心组件)
├── AttributeSet (属性集合)
│   └── ReactiveAttribute (响应式属性，支持 Watch)
├── GameplayTagContainer (标签容器)
├── ActiveEffects (活跃效果)
└── GrantedAbilities (已授予技能)
```

## 目录结构

```
src/framework/Ability/
├── Tag/
│   ├── GameplayTag.cs          # 层级标签结构
│   └── GameplayTagContainer.cs # 标签容器
├── Attribute/
│   ├── AttributeType.cs        # 属性定义类 + 定义数据库
│   ├── AttributeModifier.cs    # 属性修改器
│   ├── ReactiveAttribute.cs    # 响应式属性（支持 Watch）
│   └── AttributeSet.cs         # 属性集合
├── Effect/
│   ├── GameplayEffect.cs       # 效果配置 (ScriptableObject)
│   └── EffectInstance.cs       # 效果运行时实例
├── Ability/
│   ├── GameplayAbility.cs      # 技能配置 (ScriptableObject)
│   └── AbilityInstance.cs      # 技能运行时实例
├── Core/
│   ├── AbilitySystemComponent.cs # 核心组件
│   ├── AbilityDatabase.cs        # 技能/效果数据库
│   └── AbilityEvents.cs          # 事件数据结构
└── Editor/
    ├── EffectModifierEntryDrawer.cs   # 修改器单行显示
    ├── AttributeDefinitionDrawer.cs   # 属性定义紧凑显示
    ├── GameplayEffectEditor.cs        # 效果 Inspector
    ├── GameplayAbilityEditor.cs       # 技能 Inspector
    └── AbilityDatabaseEditor.cs       # 数据库 Inspector
```

## 核心概念

### 1. GameplayTag (游戏标签)

层级化字符串标签，用于标记状态和分类：

```csharp
// 创建标签
GameplayTag buffTag = "Status.Buff.Haste";
GameplayTag debuffTag = new GameplayTag("Status.Debuff.Poison");

// 层级匹配
buffTag.HasParent("Status.Buff");  // true
buffTag.HasParent("Status");       // true

// 标签容器
var container = new GameplayTagContainer();
container.AddTag("Unit.Hero");
container.AddTag("Team.Player");

// 查询
container.HasTag("Unit.Hero");           // 精确匹配
container.HasTagOrParent("Unit");        // 层级匹配
container.HasAll(requiredTags);          // 全部匹配
container.HasAny(optionalTags);          // 任一匹配
container.HasNone(blockedTags);          // 无匹配
```

**标签命名规范**（建议，可自定义）:
- `Unit.*` - 单位类型 (Hero, Monster, Building)
- `Team.*` - 阵营 (Player, Enemy, Neutral)
- `Status.Buff.*` - 增益状态
- `Status.Debuff.*` - 减益状态
- `Ability.*` - 技能分类 (Spell.Fire, Skill.Combat)
- `Effect.*` - 效果分类 (Battle, Permanent)

### 2. ReactiveAttribute (响应式属性)

单个属性的响应式封装，支持修改器和变化监听：

```csharp
// 获取响应式属性
var healthAttr = attributes.GetAttribute("Health");

// 设置/获取值
healthAttr.BaseValue = 100;
float current = healthAttr.Value;      // 计算后的当前值
int currentInt = healthAttr.ValueInt;  // 整数值

// 监听变化
healthAttr.Watch(newVal => Debug.Log($"Health: {newVal}"));
healthAttr.WatchImmediate(newVal => UpdateUI(newVal));  // 立即触发一次
healthAttr.WatchOnce(newVal => OnFirstChange(newVal));  // 只触发一次

// 添加修改器
healthAttr.AddModifier(AttributeModifier.Add("Health", 50, source));
healthAttr.AddModifier(AttributeModifier.Multiply("Health", 1.2f, source));
```

### 3. AttributeSet (属性集合)

管理实体的所有属性，**使用字符串作为属性标识**：

```csharp
var attributes = new AttributeSet();

// 设置基础值
attributes.SetBaseValue("MaxHealth", 100);
attributes.SetBaseValue("Attack", 10);

// 获取当前值（基础值 + 修改器）
float currentHealth = attributes.GetCurrentValue("Health");
int attackPower = attributes.GetCurrentValueInt("Attack");

// 添加/移除修改器
attributes.AddModifier(AttributeModifier.Add("Attack", 5, source));
attributes.RemoveModifiersFromSource(source);

// 监听属性变化（推荐方式）
attributes.Watch("Health", newVal => Debug.Log($"Health: {newVal}"));
attributes.WatchImmediate("Attack", UpdateAttackUI);

// 批量初始化
attributes.InitializeAttributes(
    ("MaxHealth", 100f),
    ("Attack", 15f),
    ("Defense", 8f)
);
```

**属性计算顺序**: `BaseValue -> Add -> Multiply -> Override`

### 4. GameBehaviour 集成

在 UI 或 MonoBehaviour 中监听属性变化：

```csharp
public class HeroStatsUI : GameBehaviour
{
    void Start()
    {
        var hero = GetHeroData();

        // 监听 ReactiveAttribute（自动管理订阅生命周期）
        ListenImmediate(hero.AbilitySystem.GetAttribute("Health"), UpdateHealthBar);

        // 或通过 AttributeSet
        ListenImmediate(hero.AbilitySystem.Attributes, "Attack", UpdateAttackText);
    }
}
```

### 5. GameplayEffect (游戏效果)

效果配置，定义如何修改目标的属性和状态：

```csharp
// 在编辑器中创建: Create -> TH7 -> Ability -> Gameplay Effect

// 持续时间类型
EffectDurationType.Instant   // 即时效果（直接修改基础值）
EffectDurationType.Duration  // 持续效果（时间结束后移除）
EffectDurationType.Infinite  // 永久效果（直到被移除）
EffectDurationType.Periodic  // 周期效果（每隔一段时间触发）

// 堆叠策略
EffectStackingPolicy.None     // 不堆叠，忽略新效果
EffectStackingPolicy.Stack    // 叠加层数
EffectStackingPolicy.Refresh  // 刷新持续时间
EffectStackingPolicy.Override // 覆盖旧效果
```

**效果配置示例**:

```
[Haste Effect]
- EffectId: "effect_haste"
- DurationType: Duration
- Duration: 10s
- Modifiers:
  - Attribute: "MoveSpeed", Operation: Multiply, Value: 1.5
- GrantedTags: ["Status.Buff.Haste"]
- BlockedTags: ["Status.Debuff.Slow"]
```

### 6. GameplayAbility (游戏技能)

技能配置，定义技能的类型、消耗和效果：

```csharp
// 在编辑器中创建: Create -> TH7 -> Ability -> Gameplay Ability

// 技能类型
AbilityType.Passive    // 被动技能，始终生效
AbilityType.Active     // 主动技能，需要激活
AbilityType.Triggered  // 触发技能，特定条件下自动触发

// 触发时机
AbilityTrigger.OnTurnStart    // 回合开始
AbilityTrigger.OnAttack       // 攻击时
AbilityTrigger.OnDamageTaken  // 受伤时
AbilityTrigger.OnKill         // 击杀时
```

### 7. AbilitySystemComponent (核心组件)

每个需要技能系统的实体都应该拥有一个：

```csharp
var asc = new AbilitySystemComponent();

// 初始化属性
asc.InitializeFromConfig(
    ("MaxHealth", 100f),
    ("Attack", 10f),
    ("Defense", 5f)
);

// 添加标签
asc.AddTag("Unit.Hero");
asc.AddTag("Team.Player");

// 授予技能
var abilityInstance = asc.GrantAbility(fireballAbility);

// 激活技能
var result = asc.TryActivateAbility("spell_fireball", targetASC);

// 应用效果
var effectInstance = asc.ApplyEffectToSelf(hasteEffect, caster);

// 监听属性变化
asc.WatchAttribute("Health", newVal => {
    if (newVal <= 0) OnDeath();
});

// 生命周期
asc.OnTurnStart();   // 回合开始
asc.Tick(deltaTime); // 每帧更新
asc.OnTurnEnd();     // 回合结束
```

## 定义项目属性（推荐方式）

为了获得类型安全和代码补全，建议为项目定义属性常量类：

```csharp
public static class MyGameAttributes
{
    // 战斗属性
    public const string Health = "Health";
    public const string MaxHealth = "MaxHealth";
    public const string Attack = "Attack";
    public const string Defense = "Defense";
    public const string Speed = "Speed";

    // 资源属性
    public const string Mana = "Mana";
    public const string MaxMana = "MaxMana";
}

// 使用时
asc.Attributes.SetBaseValue(MyGameAttributes.MaxHealth, 100);
asc.WatchAttribute(MyGameAttributes.Health, UpdateHealthUI);
```

## 与游戏系统集成

### 角色数据集成

```csharp
public class CharacterData
{
    [NonSerialized]
    AbilitySystemComponent abilitySystem;

    public AbilitySystemComponent AbilitySystem
    {
        get
        {
            if (abilitySystem == null) InitializeAbilitySystem();
            return abilitySystem;
        }
    }

    void InitializeAbilitySystem()
    {
        abilitySystem = new AbilitySystemComponent();
        abilitySystem.InitializeFromConfig(
            ("MaxHealth", 100f),
            ("Health", 100f),
            ("Attack", 10f)
        );

        // 监听生命值变化
        abilitySystem.WatchAttribute("Health", newVal => {
            if (newVal <= 0) OnDeath();
        });
    }
}
```

### 战斗系统集成

```csharp
public class BattleUnit
{
    public AbilitySystemComponent AbilitySystem { get; }

    public void Attack(BattleUnit target)
    {
        // 触发攻击相关技能
        AbilitySystem.TriggerAbilities(AbilityTrigger.OnAttack, target.AbilitySystem);

        // 计算伤害
        float attack = AbilitySystem.Attributes.GetCurrentValue("Attack");
        float defense = target.AbilitySystem.Attributes.GetCurrentValue("Defense");
        float damage = Mathf.Max(0, attack - defense);

        // 应用伤害
        float currentHp = target.AbilitySystem.Attributes.GetBaseValue("Health");
        target.AbilitySystem.Attributes.SetBaseValue("Health", currentHp - damage);
    }
}
```

## Editor 扩展

系统提供了自定义 Editor 脚本，优化 Inspector 显示：

### EffectModifierEntry
单行显示 `[Attribute] [Operation] [Value]`

### AttributeDefinition
三行布局显示 Name/Display, Default/Min/Max/Int, Category

### GameplayEffect Inspector
- 分组折叠（Basic Info / Duration / Stacking / Modifiers / Tags / Visual）
- 图标预览
- 条件显示（Duration/Period 根据类型显示）
- 触发次数预览

### GameplayAbility Inspector
- 分组折叠
- 图标预览
- 类型条件显示（被动技能隐藏消耗配置）
- 效果统计

### AbilityDatabase Inspector
- 统计信息（X Abilities | Y Effects）
- 滚动列表
- 快速预览（类型/消耗）
- 一键重建缓存

## 事件系统

```csharp
// 订阅效果事件
asc.OnEffectApplied += instance => Debug.Log($"Effect applied: {instance.Effect.DisplayName}");
asc.OnEffectRemoved += instance => Debug.Log($"Effect removed: {instance.Effect.DisplayName}");

// 监听属性变化（使用 Watch API）
asc.WatchAttribute("Health", newVal => {
    if (newVal <= 0) HandleDeath();
});

// 技能事件
asc.OnAbilityActivated += (ability, target) => PlayAbilityAnimation(ability);
asc.OnAbilityEnded += ability => OnAbilityComplete(ability);

// 标签变化事件
asc.OnTagChanged += (tag, added) => Debug.Log($"Tag {tag}: {(added ? "added" : "removed")}");
```

## 性能注意事项

1. **缓存属性值**: ReactiveAttribute 内部缓存计算结果，频繁读取不会重复计算
2. **延迟初始化**: ASC 可以延迟初始化，按需创建
3. **效果清理**: 战斗结束时调用 `OnBattleEnd()` 清理战斗临时效果
4. **修改器追踪**: 效果移除时自动清理关联的修改器
5. **Subscription 管理**: 使用 GameBehaviour.Listen 自动管理订阅生命周期

## 调试

```csharp
// 打印 ASC 状态
Debug.Log(asc.ToString());
// 输出: [ASC: 3 abilities, 2 effects]

// 打印所有活跃效果
foreach (var effect in asc.GetActiveEffects())
{
    Debug.Log(effect.GetDescription());
}

// 打印所有标签
Debug.Log(asc.Tags.ToString());

// 导出所有属性值
var allValues = asc.Attributes.ExportCurrentValues();
foreach (var kvp in allValues)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}");
}

// 打印单个属性详情
var attr = asc.GetAttribute("Health");
Debug.Log(attr.ToString());
// 输出: Health: 85.00 (Base: 100.00, Mods: 2)
```
