using System;
using TH7;

namespace GameFramework
{
    /// <summary>
    /// 自动订阅特性
    /// 用法: [AutoSubscribe] 或 [AutoSubscribe(typeof(PlayerDiedEvent))]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AutoSubscribeAttribute : Attribute
    {
        public AutoSubscribeTime Time { get; }
        public Type TargetType { get; }

        public AutoSubscribeAttribute(AutoSubscribeTime time = AutoSubscribeTime.Start)
        {
            Time = time;
            TargetType = null;
        }

        public AutoSubscribeAttribute(Type targetType, AutoSubscribeTime time = AutoSubscribeTime.Start)
        {
            Time = time;
            TargetType = targetType;
        }
    }
}
