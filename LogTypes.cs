using System;

namespace BetKookBridge
{
    internal enum LogType { None, ActorDeath, HostilityEvent, Corpse }

    internal sealed class LogMonitorInfo
    {
        public LogType LogType { get; set; } = LogType.None;
        public string Handle { get; set; } = "";
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public DateTime Utc { get; set; } = DateTime.MinValue;

        public override string ToString() => $"{LogType}: {Handle} | {Key} | {Value}";
    }
}
