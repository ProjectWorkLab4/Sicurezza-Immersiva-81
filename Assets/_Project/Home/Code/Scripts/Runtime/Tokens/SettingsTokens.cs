namespace SI81.UI
{
    /// <summary>
    /// Nomi puntati dei token numerici delle righe di Impostazioni (PANEL_SettingsToggle / Slider / Selector / Action).
    /// Definiti in Editor/Source/tokens_figma.json, sezione "project.numbers", blocco "Righe Impostazioni" (misure da
    /// docs/FIGMA_SPECS.md). Tutti i valori sono in unità Canvas (1 px Figma = 1 unità), tranne l'opacità e il passo del tick.
    /// Usare con <see cref="UITheme.GetNumber(string)"/>.
    /// </summary>
    public static class SettingsTokens
    {
        // --- Riga (comune) ---
        /// <summary>Larghezza della riga = larghezza del contenuto del pannello Impostazioni (516).</summary>
        public const string ROW_WIDTH = "size.settings-row.width";
        /// <summary>Altezza della riga (48).</summary>
        public const string ROW_HEIGHT = "size.settings-row.height";
        /// <summary>Padding sinistro della riga (16).</summary>
        public const string ROW_PADDING_LEFT = "padding.settings-row.left";
        /// <summary>Padding destro della riga (8).</summary>
        public const string ROW_PADDING_RIGHT = "padding.settings-row.right";
        /// <summary>Spazio tra etichetta e controlli (12).</summary>
        public const string ROW_GAP = "gap.settings-row";
        /// <summary>Lato delle icone della riga (24).</summary>
        public const string ROW_ICON = "size.settings-icon";
        /// <summary>Opacità di una riga disabilitata (0,4). Valore puro, non in unità Canvas.</summary>
        public const string ROW_DISABLED_OPACITY = "opacity.settings-disabled";

        // --- Toggle ---
        /// <summary>Larghezza della traccia del toggle (56).</summary>
        public const string TOGGLE_TRACK_WIDTH = "size.settings-toggle.track-width";
        /// <summary>Altezza della traccia del toggle (32).</summary>
        public const string TOGGLE_TRACK_HEIGHT = "size.settings-toggle.track-height";
        /// <summary>Diametro della manopola del toggle (24).</summary>
        public const string TOGGLE_KNOB = "size.settings-toggle.knob";
        /// <summary>Larghezza fissa del testo ON/OFF (32).</summary>
        public const string TOGGLE_STATE_WIDTH = "size.settings-toggle.state-width";

        // --- Slider ---
        /// <summary>Larghezza della traccia dello slider (200).</summary>
        public const string SLIDER_TRACK_WIDTH = "size.settings-slider.track-width";
        /// <summary>Altezza della traccia dello slider (12).</summary>
        public const string SLIDER_TRACK_HEIGHT = "size.settings-slider.track-height";
        /// <summary>Diametro della maniglia a riposo (28).</summary>
        public const string SLIDER_HANDLE = "size.settings-slider.handle";
        /// <summary>Diametro della maniglia in hover o trascinamento (32).</summary>
        public const string SLIDER_HANDLE_HOVER = "size.settings-slider.handle-hover";
        /// <summary>Altezza dell'area di presa (40, bersaglio minimo).</summary>
        public const string SLIDER_GRAB = "size.settings-slider.grab";
        /// <summary>Larghezza fissa del valore numerico (44).</summary>
        public const string SLIDER_VALUE_WIDTH = "size.settings-slider.value-width";
        /// <summary>Passo (in unità del valore) dell'impulso aptico/tick (10). Valore puro.</summary>
        public const string SLIDER_TICK_STEP = "step.settings-slider.tick";

        // --- Selector ---
        /// <summary>Diametro delle frecce del selettore (40).</summary>
        public const string SELECTOR_ARROW = "size.settings-selector.arrow";
        /// <summary>Larghezza del valore centrale del selettore (130).</summary>
        public const string SELECTOR_VALUE_WIDTH = "size.settings-selector.value-width";
        /// <summary>Spazio tra frecce e valore (4).</summary>
        public const string SELECTOR_GAP = "gap.settings-selector";

        // --- Action ---
        /// <summary>Altezza del pulsante azione (40).</summary>
        public const string ACTION_HEIGHT = "size.settings-action.height";
        /// <summary>Padding sinistro del pulsante azione (12).</summary>
        public const string ACTION_PADDING_LEFT = "padding.settings-action.left";
        /// <summary>Padding destro del pulsante azione (16).</summary>
        public const string ACTION_PADDING_RIGHT = "padding.settings-action.right";
        /// <summary>Spazio tra icona e testo del pulsante azione (6).</summary>
        public const string ACTION_GAP = "gap.settings-action";
    }
}
