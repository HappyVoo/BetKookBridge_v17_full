namespace BetKookBridge
{
    internal sealed class Translation
    {
        public LogMonitorText Log_Monitor { get; } = new LogMonitorText();
        internal sealed class LogMonitorText
        {
            public string Title => "日志监听";
            public string Webhook_Actor_Death => "Actor Death 角色死亡";
            public string Webhook_Corpse => "Corpse 角色放尸";
            public string Webhook_Killer => "Killer 击杀者";
            public string Webhook_Hostility_Event => "Hostility Event 危险警告";
            public string Webhook_Hostility_Event_Ship => "Hostility Event Ship 受威胁载具";
            public string Webhook_Hostility_Event_Attacker => "Hostility Event Attacker 攻击者";
            public string Webhook_Using => "Using 使用武器/装备";
            public string Webhook_Damage_Type => "Damage Type 伤害类型";
            public string Webhook_Zone => "Zone 区域/位置";
        }
    }
}
