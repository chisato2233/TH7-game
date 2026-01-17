using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 新游戏设置（选择文明和英雄后的配置）
    /// </summary>
    [System.Serializable]
    public class NewGameSettings
    {
        public string PlayerName = "Player";
        public FactionConfig SelectedFaction;
        public HeroConfig SelectedHero;

        /// <summary>
        /// 验证设置是否完整
        /// </summary>
        public bool IsValid => SelectedFaction != null && SelectedHero != null;

        /// <summary>
        /// 获取初始资源
        /// </summary>
        public ResourceBundle GetStartingResources()
        {
            return SelectedFaction?.StartingResources ?? new ResourceBundle();
        }
    }
}
