using GameFramework;

namespace TH7
{
    /// <summary>
    /// 英雄状态枚举
    /// </summary>
    public enum HeroState
    {
        Idle,           // 待机（可以接收行动指令）
        Moving,         // 正在移动
        Interacting,    // 正在交互（城镇、战斗等）
        Disabled        // 禁用（回合结束、被控制等）
    }

    /// <summary>
    /// 英雄待机状态
    /// </summary>
    [StateBinding(typeof(HeroState), HeroState.Idle)]
    public class HeroIdleState : StateBase<Hero>
    {
        public override void OnEnter(Hero hero)
        {
            hero.SetMoving(false);
        }

        public override void OnUpdate(Hero hero, float deltaTime)
        {
            // 待机状态不需要特殊处理
        }
    }

    /// <summary>
    /// 英雄移动状态
    /// </summary>
    [StateBinding(typeof(HeroState), HeroState.Moving)]
    public class HeroMovingState : StateBase<Hero>
    {
        public override void OnEnter(Hero hero)
        {
            hero.SetMoving(true);
        }

        public override void OnExit(Hero hero)
        {
            hero.SetMoving(false);
        }
    }

    /// <summary>
    /// 英雄交互状态（进入城镇、战斗等）
    /// </summary>
    [StateBinding(typeof(HeroState), HeroState.Interacting)]
    public class HeroInteractingState : StateBase<Hero>
    {
        public override void OnEnter(Hero hero)
        {
            hero.SetMoving(false);
        }
    }

    /// <summary>
    /// 英雄禁用状态
    /// </summary>
    [StateBinding(typeof(HeroState), HeroState.Disabled)]
    public class HeroDisabledState : StateBase<Hero>
    {
        public override void OnEnter(Hero hero)
        {
            hero.SetMoving(false);
        }
    }
}
