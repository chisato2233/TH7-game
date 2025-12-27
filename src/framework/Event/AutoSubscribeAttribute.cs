using System;
using TH7;

namespace GameFramework
{
    /// <summary>
    /// 自动订阅特性基类
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

        protected AutoSubscribeAttribute(Type targetType, AutoSubscribeTime time)
        {
            Time = time;
            TargetType = targetType;
        }
    }

    /// <summary>
    /// 类型化自动订阅特性，支持 IDE 搜索引用
    /// 用法: [AutoSubscribe&lt;PlayerDiedEvent&gt;]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AutoSubscribeAttribute<T> : AutoSubscribeAttribute where T : struct
    {
        public AutoSubscribeAttribute(AutoSubscribeTime time = AutoSubscribeTime.Start)
            : base(typeof(T), time) { }
    }
}
