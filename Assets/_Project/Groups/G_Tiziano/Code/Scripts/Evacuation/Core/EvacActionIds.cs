namespace SI81.Tiziano.Evacuation.Core
{
    /// <summary>Stable action log identifiers (LG01-LG11) and derived text keys. Never put user-facing strings here.</summary>
    public static class EvacActionIds
    {
        /// <summary>First plan inspection (Correct, Awareness).</summary>
        public const string Lg01PlanInspected = "evac.lg01";
        /// <summary>Exit without inspecting any plan (Missed, Awareness).</summary>
        public const string Lg02NoPlan = "evac.lg02";
        /// <summary>All NPCs of office 1 collected (Correct, Execution).</summary>
        public const string Lg03Room1Done = "evac.lg03";
        /// <summary>All NPCs of office 2 collected (Correct, Execution).</summary>
        public const string Lg04Room2Done = "evac.lg04";
        /// <summary>Emergency stairs with NPCs missing (Risky, Decision).</summary>
        public const string Lg05StairsMissingNpcs = "evac.lg05";
        /// <summary>Elevator used (Critical, Safety).</summary>
        public const string Lg06Elevator = "evac.lg06";
        /// <summary>Fire contact 1 or 2 (Risky, Safety).</summary>
        public const string Lg07FireContact = "evac.lg07";
        /// <summary>Lethal fire contact (Critical, Safety).</summary>
        public const string Lg08FireDeath = "evac.lg08";
        /// <summary>Emergency stairs with every NPC (Correct, Decision).</summary>
        public const string Lg09StairsAllNpcs = "evac.lg09";
        /// <summary>Timer expired (Missed, Time).</summary>
        public const string Lg10Timeout = "evac.lg10";
        /// <summary>Internal stairs (Risky, Decision).</summary>
        public const string Lg11InternalStairs = "evac.lg11";

        /// <summary>Replay metadata logged at run start: seed and chosen NPC/fire layout (not a debrief LG entry).</summary>
        public const string ReplayStart = "evac.replay.start";

        /// <summary>Returns the consequence/phrase text key for an action id (evac.lgNN -> evac.log.lgNN.phrase).</summary>
        public static string PhraseKey(string actionId)
        {
            return ToLogKey(actionId) + ".phrase";
        }

        /// <summary>Returns the "what you could have done" text key for an action id (evac.lgNN -> evac.log.lgNN.hint).</summary>
        public static string HintKey(string actionId)
        {
            return ToLogKey(actionId) + ".hint";
        }

        /// <summary>Returns the severity label of an action id as defined in the BRIEF table.</summary>
        public static EvacLogSeverity GetSeverity(string actionId)
        {
            switch (actionId)
            {
                case Lg01PlanInspected:
                case Lg03Room1Done:
                case Lg04Room2Done:
                case Lg09StairsAllNpcs:
                case ReplayStart:
                    return EvacLogSeverity.Correct;
                case Lg02NoPlan:
                case Lg10Timeout:
                    return EvacLogSeverity.Missed;
                case Lg06Elevator:
                case Lg08FireDeath:
                    return EvacLogSeverity.Critical;
                default:
                    return EvacLogSeverity.Risky;
            }
        }

        private static string ToLogKey(string actionId)
        {
            const string prefix = "evac.";
            if (actionId != null && actionId.StartsWith(prefix, System.StringComparison.Ordinal))
                return prefix + "log." + actionId.Substring(prefix.Length);
            return "evac.log." + actionId;
        }
    }
}
