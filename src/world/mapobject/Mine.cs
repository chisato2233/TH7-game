using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 矿场 - 占领后每日产出
    /// </summary>
    public class Mine : MapObject
    {
        [Header("Mine")]
        [SerializeField] ResourceType resourceType;
        [SerializeField] int dailyOutput = 2;

        [Header("Visual")]
        [SerializeField] SpriteRenderer flagRenderer;
        [SerializeField] Color neutralColor = Color.gray;
        [SerializeField] Color[] playerColors = { Color.blue, Color.red, Color.green, Color.yellow };

        readonly Reactive<int> ownerId = new(-1);

        public ResourceType ResourceType => resourceType;
        public int DailyOutput => dailyOutput;
        public override int OwnerId => ownerId.Value;
        public Reactive<int> OwnerIdReactive => ownerId;

        protected override void Start()
        {
            base.Start();
            ownerId.Watch(UpdateFlag);
            UpdateFlag(ownerId.Value);
        }

        void UpdateFlag(int owner)
        {
            if (flagRenderer == null) return;
            flagRenderer.color = owner >= 0 && owner < playerColors.Length
                ? playerColors[owner]
                : neutralColor;
        }

        public override bool CanInteract(Hero hero) => ownerId.Value != hero.OwnerPlayerId;

        public override InteractionResult Interact(Hero hero, SessionContext session)
        {
            int previousOwner = ownerId.Value;
            ownerId.Value = hero.OwnerPlayerId;

            string msg = previousOwner < 0
                ? $"Captured {resourceType} mine"
                : $"Seized {resourceType} mine";

            Debug.Log($"[Mine] {hero.HeroName} {msg}");

            return InteractionResult.Ok(msg);
        }

        /// <summary>
        /// 每日产出
        /// </summary>
        public void ProduceDailyOutput(SessionContext session)
        {
            if (ownerId.Value < 0) return;
            session.Resources.Add(resourceType, dailyOutput);
            Debug.Log($"[Mine] {resourceType} mine produced {dailyOutput}");
        }
    }
}
