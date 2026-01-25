using System;

namespace TH7
{
    /// <summary>
    /// 战斗行动提供者接口
    /// 负责为单位生成行动指令
    /// </summary>
    public interface IBattleActionProvider
    {
        /// <summary>
        /// 请求行动
        /// </summary>
        /// <param name="unit">需要行动的单位</param>
        /// <param name="context">战斗上下文</param>
        /// <param name="callback">行动回调</param>
        void RequestAction(BattleUnit unit, BattleContext context, Action<BattleAction> callback);

        /// <summary>
        /// 取消当前请求
        /// </summary>
        void CancelRequest();

        /// <summary>
        /// 启用/禁用提供者
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; }
    }
}
