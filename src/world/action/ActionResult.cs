namespace TH7
{
    /// <summary>
    /// 行动执行结果
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; }
        public ActionResultType Type { get; }
        public string Message { get; }
        public object Data { get; }

        ActionResult(bool success, ActionResultType type, string message = null, object data = null)
        {
            Success = success;
            Type = type;
            Message = message;
            Data = data;
        }

        public static ActionResult Succeeded(ActionResultType type = ActionResultType.None, object data = null)
            => new(true, type, null, data);

        public static ActionResult Failed(string message)
            => new(false, ActionResultType.None, message);

        public static ActionResult TriggerBattle(object enemyData)
            => new(true, ActionResultType.TriggerBattle, null, enemyData);

        public static ActionResult EnterTown(TownData town)
            => new(true, ActionResultType.EnterTown, null, town);

        public static ActionResult ResourceGained(ResourceBundle resources)
            => new(true, ActionResultType.ResourceGained, null, resources);
    }

    /// <summary>
    /// 行动结果类型
    /// </summary>
    public enum ActionResultType
    {
        None,
        TriggerBattle,      // 触发战斗
        EnterTown,          // 进入城镇
        ResourceGained,     // 获得资源
        ItemGained,         // 获得物品
        HeroMoved,          // 英雄移动完成
        TurnEnded           // 回合结束
    }
}
