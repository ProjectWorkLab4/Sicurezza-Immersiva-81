namespace SI81.UI
{
    /// <summary>
    /// Nomi puntati dei token numerici usati dalle etichette nel mondo, da HUD_DecisionPanel e da HUD_OutcomePanel
    /// (Blocco C · C2). I valori stanno in tokens_figma.json (sezione "numbers") e vengono letti con <see cref="UITheme.GetNumber"/>.
    /// Distanze e angoli della HUD stanno invece in <see cref="UITheme.HudSettings"/>.
    /// </summary>
    public static class WorldTokens
    {
        /// <summary>Intervallo di aggiornamento della scelta delle etichette, in secondi (0,1 = circa 10 Hz).</summary>
        public const string LABEL_TICK = "interval.world-label.tick";
        /// <summary>Scostamento in gradi oltre il quale l'etichetta viene rigirata verso la testa.</summary>
        public const string LABEL_YAW_THRESHOLD = "angle.world-label.yaw-threshold";
        /// <summary>Numero massimo di HUD_InteractionPrompt visibili insieme.</summary>
        public const string MAX_INTERACTION_PROMPTS = "count.world-label.max-interaction";
        /// <summary>Numero massimo di HUD_ContextHelp visibili insieme.</summary>
        public const string MAX_CONTEXT_HELPS = "count.world-label.max-context";
        /// <summary>Durata di HUD_OutcomePanel in secondi (brief HUD: 3-5 s).</summary>
        public const string OUTCOME_DURATION = "duration.outcome";
        /// <summary>Durata minima ammessa di HUD_OutcomePanel in secondi (3).</summary>
        public const string OUTCOME_DURATION_MIN = "duration.outcome.min";
        /// <summary>Durata massima ammessa di HUD_OutcomePanel in secondi (5).</summary>
        public const string OUTCOME_DURATION_MAX = "duration.outcome.max";
        /// <summary>Ampiezza dell'impulso aptico alla scelta di un'opzione di HUD_DecisionPanel (0-1).</summary>
        public const string DECISION_HAPTIC_AMPLITUDE = "haptic.decision.amplitude";
        /// <summary>Durata dell'impulso aptico alla scelta di un'opzione, in secondi.</summary>
        public const string DECISION_HAPTIC_DURATION = "haptic.decision.duration";
        /// <summary>Intervallo di aggiornamento della barra del tempo di HUD_DecisionPanel, in secondi.</summary>
        public const string DECISION_TIMER_TICK = "interval.decision.timer-tick";
        /// <summary>Larghezza di HUD_DecisionPanel (480).</summary>
        public const string DECISION_WIDTH = "size.decision.width";
        /// <summary>Padding orizzontale di HUD_DecisionPanel (24).</summary>
        public const string DECISION_PADDING_X = "padding.decision.x";
        /// <summary>Padding verticale di HUD_DecisionPanel (20).</summary>
        public const string DECISION_PADDING_Y = "padding.decision.y";
        /// <summary>Spazio tra le righe di HUD_DecisionPanel (10).</summary>
        public const string DECISION_GAP = "gap.decision";
        /// <summary>Altezza della barra del tempo di HUD_DecisionPanel (6).</summary>
        public const string DECISION_TIMER_HEIGHT = "size.decision.timer-height";
        /// <summary>Lato dell'icona della domanda di HUD_DecisionPanel (32).</summary>
        public const string DECISION_ICON = "size.decision.icon";
        /// <summary>Larghezza di HUD_OutcomePanel (480).</summary>
        public const string OUTCOME_WIDTH = "size.outcome.width";
        /// <summary>Altezza minima di HUD_OutcomePanel (160).</summary>
        public const string OUTCOME_MIN_HEIGHT = "size.outcome.min-height";
        /// <summary>Padding di HUD_OutcomePanel (24).</summary>
        public const string OUTCOME_PADDING = "padding.outcome";
        /// <summary>Spazio tra pillola esito e motivo in HUD_OutcomePanel (12).</summary>
        public const string OUTCOME_GAP = "gap.outcome";
    }
}
