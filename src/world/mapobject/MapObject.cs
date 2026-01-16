using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 交互结果
    /// </summary>
    public class InteractionResult
    {
        public bool Success;
        public string Message;
        public ResourceBundle ResourcesGained;
        public bool ShouldRemove;

        public static InteractionResult Fail(string msg) =>
            new() { Success = false, Message = msg };

        public static InteractionResult Ok(string msg = null, bool remove = false) =>
            new() { Success = true, Message = msg, ShouldRemove = remove };
    }

    /// <summary>
    /// 地图物件基类
    /// 通过继承实现不同类型：ResourcePile, Mine, Monster 等
    /// </summary>
    public abstract class MapObject : GameBehaviour
    {
        public Vector3Int CellPosition { get; private set; }
        public virtual int OwnerId => -1;
        public virtual bool IsCollected => false;

        /// <summary>
        /// 交互后是否应该移除物件
        /// </summary>
        public virtual bool RemoveAfterInteract => false;

        System.Func<Vector3, Vector3Int> worldToCell;

        protected override void Awake()
        {
            base.Awake();
            UpdateCellPosition();
        }

        public void SetCoordinateConverter(System.Func<Vector3, Vector3Int> converter)
        {
            worldToCell = converter;
            UpdateCellPosition();
        }

        void UpdateCellPosition()
        {
            if (worldToCell != null)
                CellPosition = worldToCell(transform.position);
            else
                CellPosition = Vector3Int.FloorToInt(transform.position);
        }

        /// <summary>
        /// 英雄与物件交互
        /// </summary>
        public abstract InteractionResult Interact(Hero hero, SessionContext session);

        /// <summary>
        /// 是否可以交互
        /// </summary>
        public virtual bool CanInteract(Hero hero) => !IsCollected;
    }
}
