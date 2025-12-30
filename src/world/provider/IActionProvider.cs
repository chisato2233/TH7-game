using System;

namespace TH7
{
    /// <summary>
    /// 行动提供者接口
    /// 定义如何获取英雄的下一个行动（玩家输入 / AI 决策 / 网络同步）
    /// </summary>
    public interface IActionProvider
    {
        /// <summary>
        /// 是否需要等待外部输入（人类玩家需要，AI 不需要）
        /// </summary>
        bool RequiresInput { get; }

        /// <summary>
        /// 是否正在等待输入
        /// </summary>
        bool IsWaiting { get; }

        /// <summary>
        /// 开始请求行动（非阻塞）
        /// </summary>
        /// <param name="hero">当前英雄</param>
        /// <param name="context">世界上下文</param>
        /// <param name="onActionReady">行动就绪回调</param>
        void RequestAction(HeroData hero, WorldContext context, Action<HeroAction> onActionReady);

        /// <summary>
        /// 取消当前请求
        /// </summary>
        void CancelRequest();

        /// <summary>
        /// 启用/禁用提供者
        /// </summary>
        void SetEnabled(bool enabled);
    }
}
