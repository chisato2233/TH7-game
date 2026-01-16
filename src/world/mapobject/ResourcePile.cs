using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 资源堆 - 一次性拾取
    /// </summary>
    public class ResourcePile : MapObject
    {
        [Header("Resource")]
        [SerializeField] ResourceType resourceType;
        [SerializeField] int amount = 5;

        bool collected;

        public ResourceType ResourceType => resourceType;
        public int Amount => amount;
        public override bool IsCollected => collected;
        public override bool RemoveAfterInteract => true;

        public override bool CanInteract(Hero hero) => !collected;

        public override InteractionResult Interact(Hero hero, SessionContext session)
        {
            if (collected)
                return InteractionResult.Fail("Already collected");

            session.Resources.Add(resourceType, amount);
            collected = true;

            Debug.Log($"[ResourcePile] {hero.HeroName} picked up {amount} {resourceType}");

            return new InteractionResult
            {
                Success = true,
                Message = $"+{amount} {resourceType}",
                ResourcesGained = new ResourceBundle { [resourceType] = amount },
                ShouldRemove = true
            };
        }
    }
}
