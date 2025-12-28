using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 通用滚动列表 UI 组件
    /// 支持两种数据源模式：
    /// 1. 绑定 ReactiveList - 自动响应列表变化
    /// 2. 手动刷新 - 调用 Refresh(items) 更新
    /// </summary>
    public class ScrollViewUI<TData, TItem> : UIBehaviour where TItem : ScrollViewItem<TData>
    {
        [Header("ScrollView")]
        [SerializeField] protected Transform itemContainer;
        [SerializeField] protected GameObject itemPrefab;

        [Header("Empty State")]
        [SerializeField] protected GameObject emptyStateObject;

        protected List<TItem> activeItems = new();
        protected ReactiveList<TData> boundList;

        protected override void Awake()
        {
            base.Awake();
            ValidateReferences();
        }

        void ValidateReferences()
        {
            if (itemContainer == null)
                Debug.LogError($"[{GetType().Name}] itemContainer is not assigned!");
            if (itemPrefab == null)
                Debug.LogError($"[{GetType().Name}] itemPrefab is not assigned!");
        }

        #region Bind ReactiveList

        /// <summary>
        /// 绑定 ReactiveList，自动响应列表变化
        /// </summary>
        public void Bind(ReactiveList<TData> list)
        {
            // 解绑旧的
            Unbind();

            boundList = list;
            if (boundList == null) return;

            // 监听变化
            Listen(boundList, OnListChanged);

            // 初始刷新
            RefreshAll();
        }

        /// <summary>
        /// 解除绑定
        /// </summary>
        public void Unbind()
        {
            boundList = null;
            // Subscriptions 会在 OnDestroy 时自动清理
        }

        void OnListChanged(ReactiveList<TData>.ListChangeEvent evt)
        {
            switch (evt.Type)
            {
                case ReactiveList<TData>.ChangeType.Add:
                    OnItemAdded(evt.Index, evt.NewValue);
                    break;
                case ReactiveList<TData>.ChangeType.Remove:
                    OnItemRemoved(evt.Index);
                    break;
                case ReactiveList<TData>.ChangeType.Replace:
                    OnItemReplaced(evt.Index, evt.NewValue);
                    break;
                case ReactiveList<TData>.ChangeType.Clear:
                case ReactiveList<TData>.ChangeType.Reset:
                    RefreshAll();
                    break;
            }
        }

        void OnItemAdded(int index, TData data)
        {
            var item = CreateItem(data);
            item.transform.SetSiblingIndex(index);
            activeItems.Insert(index, item);
            UpdateEmptyState();
        }

        void OnItemRemoved(int index)
        {
            if (index < 0 || index >= activeItems.Count) return;

            var item = activeItems[index];
            activeItems.RemoveAt(index);
            DestroyItem(item);
            UpdateEmptyState();
        }

        void OnItemReplaced(int index, TData data)
        {
            if (index < 0 || index >= activeItems.Count) return;
            activeItems[index].Setup(data);
        }

        #endregion

        #region Manual Refresh

        /// <summary>
        /// 手动刷新列表（用于非 ReactiveList 数据源）
        /// </summary>
        public void Refresh(IReadOnlyList<TData> items)
        {
            ClearAllItems();

            if (items != null)
            {
                foreach (var data in items)
                {
                    var item = CreateItem(data);
                    activeItems.Add(item);
                }
            }

            UpdateEmptyState();
        }

        /// <summary>
        /// 刷新当前绑定的 ReactiveList
        /// </summary>
        public void RefreshAll()
        {
            if (boundList == null)
            {
                ClearAllItems();
                UpdateEmptyState();
                return;
            }

            Refresh(boundList.AsReadOnly());
        }

        #endregion

        #region Item Management

        protected virtual TItem CreateItem(TData data)
        {
            if (itemPrefab == null || itemContainer == null)
                return null;

            var go = Instantiate(itemPrefab, itemContainer);
            var item = go.GetComponent<TItem>();

            if (item != null)
            {
                item.Setup(data);
                OnItemCreated(item);
            }

            return item;
        }

        protected virtual void DestroyItem(TItem item)
        {
            if (item != null && item.gameObject != null)
            {
                OnItemDestroyed(item);
                Destroy(item.gameObject);
            }
        }

        protected void ClearAllItems()
        {
            foreach (var item in activeItems)
                DestroyItem(item);
            activeItems.Clear();
        }

        void UpdateEmptyState()
        {
            if (emptyStateObject != null)
                emptyStateObject.SetActive(activeItems.Count == 0);
        }

        /// <summary>
        /// Item 创建后的回调（子类可重写以添加事件监听等）
        /// </summary>
        protected virtual void OnItemCreated(TItem item) { }

        /// <summary>
        /// Item 销毁前的回调
        /// </summary>
        protected virtual void OnItemDestroyed(TItem item) { }

        #endregion

        #region Query

        public int ItemCount => activeItems.Count;

        public TItem GetItem(int index)
        {
            if (index < 0 || index >= activeItems.Count) return null;
            return activeItems[index];
        }

        public TItem FindItem(Predicate<TItem> predicate)
        {
            return activeItems.Find(predicate);
        }

        #endregion

        protected override void OnDestroy()
        {
            ClearAllItems();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// 滚动列表项基类
    /// </summary>
    public abstract class ScrollViewItem<TData> : UIBehaviour
    {
        protected TData data;

        public TData Data => data;

        public virtual void Setup(TData itemData)
        {
            data = itemData;
            OnSetup();
        }

        /// <summary>
        /// 数据设置后的回调（子类实现具体 UI 更新）
        /// </summary>
        protected abstract void OnSetup();
    }
}
