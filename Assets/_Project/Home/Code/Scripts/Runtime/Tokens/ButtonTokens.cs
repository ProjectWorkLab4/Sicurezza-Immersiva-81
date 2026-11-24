namespace SI81.UI
{
    /// <summary>
    /// Nomi dei token (puntati) aggiunti dal Blocco A · Pulsanti in Editor/Source/tokens_figma.json (sezione "project").
    /// Stesso schema di <see cref="NumberTokens"/> / <see cref="ColorTokens"/>, in un file a parte per non toccare ThemeTokens.cs.
    /// Si leggono con <c>theme.GetNumber(ButtonTokens.HEIGHT)</c> o <c>theme.GetColor(ButtonTokens.DIALOG_BACKDROP)</c>.
    /// </summary>
    public static class ButtonTokens
    {
        /// <summary>Altezza di BTN_Primary / BTN_Secondary (48).</summary>
        public const string HEIGHT = "size.btn.height";
        /// <summary>Larghezza fissa di BTN_Primary (200).</summary>
        public const string PRIMARY_WIDTH = "size.btn.primary-width";
        /// <summary>Padding orizzontale di BTN_Primary / BTN_Secondary (16).</summary>
        public const string PADDING_X = "padding.btn.x";
        /// <summary>Spazio tra icona ed etichetta dei pulsanti (8).</summary>
        public const string GAP = "gap.btn";
        /// <summary>Lato dell'icona dei pulsanti (24).</summary>
        public const string ICON = "size.btn.icon";
        /// <summary>Lato di BTN_Back (40).</summary>
        public const string BACK = "size.btn.back";
        /// <summary>Lato di BTN_Badge (64).</summary>
        public const string BADGE = "size.badge";
        /// <summary>Lato dell'icona di BTN_Badge sbloccato (32).</summary>
        public const string BADGE_ICON = "size.badge.icon";
        /// <summary>Larghezza di BTN_SettingsTab (180).</summary>
        public const string TAB_WIDTH = "size.settings-tab.width";
        /// <summary>Altezza di BTN_SettingsTab (48).</summary>
        public const string TAB_HEIGHT = "size.settings-tab.height";
        /// <summary>Padding sinistro di BTN_SettingsTab (12).</summary>
        public const string TAB_PADDING = "padding.settings-tab";
        /// <summary>Spazio icona-etichetta di BTN_SettingsTab (8).</summary>
        public const string TAB_GAP = "gap.settings-tab";
        /// <summary>Larghezza di BTN_ListRow (440).</summary>
        public const string ROW_WIDTH = "size.list-row.width";
        /// <summary>Altezza di BTN_ListRow (40).</summary>
        public const string ROW_HEIGHT = "size.list-row.height";
        /// <summary>Padding orizzontale di BTN_ListRow (12).</summary>
        public const string ROW_PADDING = "padding.list-row";
        /// <summary>Spazio tra le colonne di BTN_ListRow (12).</summary>
        public const string ROW_GAP = "gap.list-row";
        /// <summary>Larghezza del posto delle stelle in BTN_ListRow (153).</summary>
        public const string ROW_STARS_WIDTH = "size.list-row.stars-width";
        /// <summary>Larghezza del punteggio N/100 in BTN_ListRow (56).</summary>
        public const string ROW_SCORE_WIDTH = "size.list-row.score-width";
        /// <summary>Lato del cerchio bianco con la spunta di selezione (30).</summary>
        public const string STATE_CHECK = "size.state-check";
        /// <summary>Sporgenza del cerchio di selezione oltre l'angolo (8).</summary>
        public const string STATE_CHECK_OFFSET = "offset.state-check";
        /// <summary>Lato del cerchio di selezione compatto (22), per componenti bassi.</summary>
        public const string STATE_CHECK_COMPACT = "size.state-check.compact";
        /// <summary>Larghezza di PANEL_ConfirmDialog (480).</summary>
        public const string DIALOG_WIDTH = "size.dialog.width";
        /// <summary>Altezza di PANEL_ConfirmDialog (240).</summary>
        public const string DIALOG_HEIGHT = "size.dialog.height";
        /// <summary>Colore del velo dietro il dialogo modale (nero 45%).</summary>
        public const string DIALOG_BACKDROP = "color.dialog.backdrop";
    }
}
