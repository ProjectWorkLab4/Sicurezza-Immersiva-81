namespace SI81.UI
{
    /// <summary>
    /// Nomi puntati dei token numerici delle card (BTN_ScenarioCard, PANEL_TrainingCard, BTN_DifficultyCard).
    /// I valori stanno in Editor/Source/tokens_figma.json (sezione "project.numbers", blocco "[Blocco A · Card]") e si leggono
    /// con <c>theme.GetNumber(CardsTokens.X)</c>. Tutte le misure sono in unità Canvas (1 px Figma = 1 unità);
    /// le scale e gli alpha sono moltiplicatori.
    /// </summary>
    public static class CardsTokens
    {
        // --- Comuni ---
        /// <summary>Alpha di contenuti e barre di una card non disponibile / livello bloccato (0,4).</summary>
        public const string ALPHA_DISABLED = "alpha.card.disabled";

        // --- BTN_ScenarioCard ---
        /// <summary>Larghezza di BTN_ScenarioCard (166).</summary>
        public const string SCENARIO_WIDTH = "size.scenario-card.width";
        /// <summary>Altezza di BTN_ScenarioCard (140).</summary>
        public const string SCENARIO_HEIGHT = "size.scenario-card.height";
        /// <summary>Altezza dell'immagine IMG_Thumbnail (80).</summary>
        public const string SCENARIO_THUMBNAIL_HEIGHT = "size.scenario-card.thumbnail-height";
        /// <summary>Lato dell'icona dello scenario sull'immagine (44).</summary>
        public const string SCENARIO_ICON = "size.scenario-card.icon";
        /// <summary>Distanza della pillola "miglior risultato" dall'angolo in alto a destra (6).</summary>
        public const string SCENARIO_PILL_INSET = "inset.scenario-card.pill";
        /// <summary>Padding sinistro della pillola (6).</summary>
        public const string SCENARIO_PILL_PAD_LEFT = "padding.scenario-card.pill-left";
        /// <summary>Padding destro della pillola (8).</summary>
        public const string SCENARIO_PILL_PAD_RIGHT = "padding.scenario-card.pill-right";
        /// <summary>Padding verticale della pillola (1).</summary>
        public const string SCENARIO_PILL_PAD_Y = "padding.scenario-card.pill-y";
        /// <summary>Spazio tra icona e testo nella pillola (2).</summary>
        public const string SCENARIO_PILL_GAP = "gap.scenario-card.pill";
        /// <summary>Lato dell'icona nella pillola (20).</summary>
        public const string SCENARIO_PILL_ICON = "size.scenario-card.pill-icon";
        /// <summary>Padding orizzontale del corpo (8).</summary>
        public const string SCENARIO_BODY_PAD_X = "padding.scenario-card.x";
        /// <summary>Padding verticale del corpo (6).</summary>
        public const string SCENARIO_BODY_PAD_Y = "padding.scenario-card.y";
        /// <summary>Spazio tra titolo e riga complessità (2).</summary>
        public const string SCENARIO_BODY_GAP = "gap.scenario-card.body";
        /// <summary>Spazio tra i pallini di complessità e il loro testo (6).</summary>
        public const string SCENARIO_ROW_GAP = "gap.scenario-card.row";
        /// <summary>Diametro di un pallino di complessità (8).</summary>
        public const string SCENARIO_DOT = "size.scenario-card.dot";
        /// <summary>Spazio tra due pallini (3: 5 x 8 + 4 x 3 = 52).</summary>
        public const string SCENARIO_DOT_GAP = "gap.scenario-card.dot";
        /// <summary>Lato dell'icona dei livelli completati (20).</summary>
        public const string SCENARIO_LEVELS_ICON = "size.scenario-card.levels-icon";

        // --- PANEL_TrainingCard ---
        /// <summary>Larghezza della striscia (712).</summary>
        public const string TRAINING_WIDTH = "size.training-card.width";
        /// <summary>Altezza della striscia (56).</summary>
        public const string TRAINING_HEIGHT = "size.training-card.height";
        /// <summary>Padding sinistro (16).</summary>
        public const string TRAINING_PAD_LEFT = "padding.training-card.left";
        /// <summary>Padding destro (4).</summary>
        public const string TRAINING_PAD_RIGHT = "padding.training-card.right";
        /// <summary>Spazio tra gli elementi della riga (12).</summary>
        public const string TRAINING_GAP = "gap.training-card";
        /// <summary>Lato dell'icona ICN_School (32).</summary>
        public const string TRAINING_ICON = "size.training-card.icon";
        /// <summary>Scala hover della striscia (1,02: resta nei 712 di contenuto, Figma 726,24 x 57,12).</summary>
        public const string TRAINING_HOVER_SCALE = "scale.training-card.hover";
        /// <summary>Scala pressed della striscia (0,99, Figma 704,88).</summary>
        public const string TRAINING_PRESSED_SCALE = "scale.training-card.pressed";

        // --- BTN_DifficultyCard ---
        /// <summary>Larghezza di BTN_DifficultyCard (226, CLAUDE.md 4.10).</summary>
        public const string DIFFICULTY_WIDTH = "size.difficulty-card.width";
        /// <summary>Altezza di BTN_DifficultyCard (117, CLAUDE.md 4.10).</summary>
        public const string DIFFICULTY_HEIGHT = "size.difficulty-card.height";
        /// <summary>Padding interno (10).</summary>
        public const string DIFFICULTY_PADDING = "padding.difficulty-card";
        /// <summary>Spazio tra badge livello e descrizione (4).</summary>
        public const string DIFFICULTY_GAP = "gap.difficulty-card";
        /// <summary>Lato del cerchio bianco con la spunta (30).</summary>
        public const string CHECK_BADGE_SIZE = "size.check-badge";
        /// <summary>Sporgenza del cerchio oltre l'angolo in alto a destra (8).</summary>
        public const string CHECK_BADGE_OUTSET = "outset.check-badge";
        /// <summary>Lato dell'icona della spunta (24).</summary>
        public const string CHECK_BADGE_ICON = "size.check-badge.icon";
    }
}
