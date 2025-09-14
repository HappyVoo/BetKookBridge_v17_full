using System;

using System.Text.RegularExpressions;



namespace BetKookBridge

{

    internal static class LogParser

    {

        private static readonly Regex RgxActorDeath = new(

            @"^<(?<Date>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)>\s+\[Notice\]\s+<Actor Death>\s+CActor::Kill:\s+'(?<Handle>[^']+)'\s+\[\d+\]\s+in zone\s+'(?<Zone>[^']+)'\s+killed by\s+'(?<KilledBy>[^']+)'\s+\[\d+\]\s+using\s+'(?<Using>[^']+)'\s+\[Class\s+(?<UsingClass>[^\]]+)\]\s+with damage type\s+'(?<DamageType>[^']+)'",

            RegexOptions.Compiled | RegexOptions.IgnoreCase);



        // Value: Using(+Class) -> Zone -> Damage Type

        private static readonly Regex RgxActorDeathInfo = new(

            @"Using:\s*(?<Using>.+)\r?\nZone:\s*(?<Zone>.+)\r?\nDamage\s*Type:\s*(?<Type>.+)",

            RegexOptions.Compiled | RegexOptions.IgnoreCase);



        private static readonly Regex RgxHostility = new(

            @"^<(?<Date>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)>\s+\[Notice\]\s+<(?:Hostility Event|Debug Hostility Events)>\s+\[OnHandleHit\]\s+(?:Fake hit\s+)?FROM\s+(?<Attacker>\S+)\s+TO\s+(?<Vehicle>\S+)\.\s+(?:Being\s+sent\s+to\s+child\s+(?<Victim>\S+))?",

            RegexOptions.Compiled | RegexOptions.IgnoreCase);



        public static LogMonitorInfo? TryParse(string line)

        {

            if (string.IsNullOrEmpty(line)) return null;



            var md = RgxActorDeath.Match(line);

            if (md.Success)

            {

                var info = new LogMonitorInfo

                {

                    LogType = LogType.ActorDeath,

                    Handle  = md.Groups["Handle"].Value,

                    Key     = md.Groups["KilledBy"].Value,

                    Value   = $"Using: {md.Groups["Using"].Value} (Class {md.Groups["UsingClass"].Value})\r\n" +

                              $"Zone: {md.Groups["Zone"].Value}\r\n" +

                              $"Damage Type: {md.Groups["DamageType"].Value}"

                };

                if (DateTime.TryParse(md.Groups["Date"].Value, out var t))

                    info.Utc = DateTime.SpecifyKind(t, DateTimeKind.Utc);

                return info;

            }



            var mh = RgxHostility.Match(line);

            if (mh.Success)

            {

                var info = new LogMonitorInfo

                {

                    LogType = LogType.HostilityEvent,

                    Handle  = mh.Groups["Victim"].Success ? mh.Groups["Victim"].Value : "Unknown",

                    Key     = mh.Groups["Attacker"].Value,

                    Value   = mh.Groups["Vehicle"].Value

                };

                if (DateTime.TryParse(mh.Groups["Date"].Value, out var t))

                    info.Utc = DateTime.SpecifyKind(t, DateTimeKind.Utc);

                return info;

            }



            return null;

        }



        public static Match MatchActorDeathInfo(string value) => RgxActorDeathInfo.Match(value);

    }

}

