using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 文明/阵营配置
    /// </summary>
    [CreateAssetMenu(fileName = "FactionConfig", menuName = "TH7/Faction Config")]
    public class FactionConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public BiomeType FactionType;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Visuals")]
        public Sprite Icon;
        public Sprite Banner;
        public Color PrimaryColor = Color.white;

        [Header("Starting Resources")]
        public ResourceBundle StartingResources = new();

        [Header("Associated Heroes")]
        [Tooltip("该文明可选的英雄列表")]
        public List<HeroConfig> AvailableHeroes = new();

        [Header("Associated Units")]
        [Tooltip("该文明的兵种列表")]
        public List<UnitConfig> FactionUnits = new();
    }
}
