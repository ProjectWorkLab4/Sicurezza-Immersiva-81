namespace SI81.UI
{
    /// <summary>Priorità di un avviso breve: l'avviso evento (HUD_EventAlert) vince sul feedback d'azione (HUD_ActionFeedback).</summary>
    public enum AlertPriority
    {
        /// <summary>Feedback d'azione (HUD_ActionFeedback), priorità bassa.</summary>
        Feedback = 0,
        /// <summary>Avviso evento (HUD_EventAlert), priorità alta.</summary>
        Event = 1
    }

    /// <summary>Tipo di feedback d'azione: decide icona, colore e suono.</summary>
    public enum FeedbackKind
    {
        /// <summary>Azione corretta (spunta verde).</summary>
        Positive,
        /// <summary>Informazione neutra.</summary>
        Neutral,
        /// <summary>Azione sbagliata o rischiosa (croce rossa).</summary>
        Negative
    }

    /// <summary>Mano (controller) a cui riferire pannello da polso e impulsi aptici.</summary>
    public enum HUDHand
    {
        /// <summary>Controller sinistro.</summary>
        Left = 0,
        /// <summary>Controller destro.</summary>
        Right = 1
    }

    /// <summary>Forma di un HUD_EventAlert, scelta dal livello (Guided completo, Operational solo titolo, Crisis solo icona).</summary>
    public enum EventAlertMode
    {
        /// <summary>Icona + titolo + testo (Guided).</summary>
        Full,
        /// <summary>Icona + titolo (Operational).</summary>
        TitleOnly,
        /// <summary>Solo icona, con suono e sottotitolo (Crisis).</summary>
        IconOnly
    }
}
