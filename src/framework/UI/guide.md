# UI 框架指南

## 概述

UI 框架基于 `GameBehaviour`，提供响应式数据绑定、窗口动画、通用列表组件。

---

## 类层级

```
MonoBehaviour
└── GameBehaviour (框架基类)
    └── UIBehaviour (UI 基类)
        ├── UIWindowBehaviour (窗口，带动画)
        └── ScrollViewUI<TData, TItem> (滚动列表)
            └── ScrollViewItem<TData> (列表项)
```

---

## UIBehaviour

UI 组件基类，继承自 `GameBehaviour`。

```csharp
public class MyUI : UIBehaviour
{
    protected override void Start()
    {
        base.Start();  // 必须调用

        // 监听 Reactive<T>
        Listen(someReactive, value => UpdateDisplay(value));
        ListenImmediate(someReactive, value => UpdateDisplay(value));

        // 监听 ReactiveList<T>
        Listen(someList, evt => OnListChanged(evt));
        ListenCount(someList, count => UpdateCount(count));
        ListenCountImmediate(someList, count => UpdateCount(count));
    }
}
```

**便捷方法**:
- `Show()` / `Hide()` - 显示/隐藏
- `SetInteractable(bool)` - 设置可交互性
- `RectTransform` - 快捷访问

---

## UIWindowBehaviour

带动画的窗口组件，支持 DOTween 或 Animator。

```csharp
public class MyWindow : UIWindowBehaviour
{
    // 在 Inspector 中配置:
    // - Animation Type: None / DOTween / Animator
    // - Animation Duration: 0.3
    // - Show Ease / Hide Ease
    // - Hidden Scale

    public void OpenWindow()
    {
        Show();  // 自动播放显示动画
    }

    public void CloseWindow()
    {
        Hide();  // 自动播放隐藏动画
    }
}
```

**事件回调**:
```csharp
OnShowStart?.Invoke();
OnShowComplete?.Invoke();
OnHideStart?.Invoke();
OnHideComplete?.Invoke();
```

---

## ScrollViewUI

通用滚动列表，支持两种数据源模式。

### 定义列表项

```csharp
public class EnemyItemUI : ScrollViewItem<EnemyData>
{
    [SerializeField] TextMeshProUGUI nameText;

    protected override void OnSetup()
    {
        nameText.text = data.Name;  // data 是基类字段
    }
}
```

### 定义列表

```csharp
public class EnemyListUI : ScrollViewUI<EnemyData, EnemyItemUI>
{
    protected override void OnItemCreated(EnemyItemUI item)
    {
        // 项创建后的回调，可添加事件监听
        item.OnClicked = HandleItemClick;
    }
}
```

### 使用方式

**绑定 ReactiveList（自动响应变化）**:
```csharp
enemyList.Bind(context.Enemies);  // Add/Remove/Clear 自动更新 UI
```

**手动刷新**:
```csharp
enemyList.Refresh(enemyDataArray);
```

---

## 资源显示组件

### ResourceDisplayUI

单个资源显示：

```csharp
// 自动绑定到 SessionContext.Resources
[SerializeField] ResourceDisplayUI goldDisplay;

// 或手动绑定
goldDisplay.Bind(resources.Gold);
goldDisplay.SetResourceType(ResourceType.Gold, resources);
```

### ResourceBarUI

资源条（所有资源）：

```csharp
[SerializeField] ResourceBarUI resourceBar;
resourceBar.BindToResources(session.Resources);
```

### ResourceCostUI

成本显示（带颜色变化）：

```csharp
costUI.Setup(ResourceType.Gold, 500, playerResources);
// 自动变红如果资源不足
```

---

## Modern UI Pack 集成

### ModalWindowManager

```csharp
[SerializeField] ModalWindowManager modal;

modal.Open();   // 打开
modal.Close();  // 关闭

modal.onConfirm.AddListener(() => { });
modal.onCancel.AddListener(() => { });
```

### ButtonManager

```csharp
[SerializeField] ButtonManager button;

button.onClick.AddListener(OnClick);
button.Interactable(false);  // 禁用
```

### WindowDragger

添加到窗口标题栏，使窗口可拖动：
1. 添加 `WindowDragger` 组件
2. 设置 `Drag Object` = 窗口 RectTransform
3. 设置 `Drag Area` = Canvas RectTransform（可选）
