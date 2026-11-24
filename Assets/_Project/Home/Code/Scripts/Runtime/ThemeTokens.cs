namespace SI81.UI
{
    /// <summary>
    /// Nomi puntati dei token colore, identici alle variabili Figma (collezione "Color", "/" sostituito da ".").
    /// Usare queste costanti con <see cref="UITheme.GetColor(string)"/> invece di scrivere le stringhe a mano.
    /// </summary>
    public static class ColorTokens
    {
        // --- Azioni ed esiti base ---
        /// <summary>Conferma, esito positivo, Guided (#0072B2, testo bianco).</summary>
        public const string CONFIRM = "color.confirm";
        /// <summary>Errore, rischio, Crisis (#D55E00, SOLO testo nero).</summary>
        public const string DENY = "color.deny";

        // --- Brand ---
        /// <summary>Verde scuro del brand (#1B5E48): pannelli, Operational.</summary>
        public const string BRAND_DARK = "color.brand.dark";
        /// <summary>Verde chiaro del brand (#ADDD8E): evidenza su scuro, glow selezione. Solo testo nero.</summary>
        public const string BRAND_LIGHT = "color.brand.light";

        // --- Superfici ---
        /// <summary>Card bianca su pannello.</summary>
        public const string SURFACE_CARD = "color.surface.card";
        /// <summary>Pannello opaco (= brand.dark) per la UI in scenario.</summary>
        public const string SURFACE_PANEL = "color.surface.panel";
        /// <summary>Vetro dei menu: brand.dark all'85% (nessuna sfocatura in tempo reale).</summary>
        public const string SURFACE_GLASS = "color.surface.glass";
        /// <summary>Vetro chiaro della bottom bar: bianco al 78%.</summary>
        public const string SURFACE_GLASS_LIGHT = "color.surface.glass-light";

        // --- Testo ---
        /// <summary>Testo su bianco, brand.light, deny, state.active (#000000).</summary>
        public const string TEXT_PRIMARY = "color.text.primary";
        /// <summary>Testo principale su brand.dark e confirm (#FFFFFF).</summary>
        public const string TEXT_ON_DARK = "color.text.on-dark";
        /// <summary>Testo secondario su brand.dark (#ECECEC).</summary>
        public const string TEXT_ON_GREEN = "color.text.on-green";
        /// <summary>Testo secondario su bianco (#666666).</summary>
        public const string TEXT_SECONDARY = "color.text.secondary";

        // --- Stati, bordi, tile ---
        /// <summary>Verde attivo #4ADE80 (decisione di team, sostituisce l'oliva). Solo testo nero.</summary>
        public const string STATE_ACTIVE = "color.state.active";
        /// <summary>Bordo 2 unità del pulsante primario su fondo scuro.</summary>
        public const string BORDER_ON_DARK = "color.border.on-dark";
        /// <summary>Bordo 2 unità del pulsante primario su fondo chiaro.</summary>
        public const string BORDER_ON_LIGHT = "color.border.on-light";
        /// <summary>Tile neutre e hover della bottom bar (#ECECEC).</summary>
        public const string TILE_NEUTRAL = "color.tile.neutral";

        // --- Livelli di difficoltà ---
        /// <summary>Colore del livello 1 · Guided.</summary>
        public const string LEVEL_GUIDED = "color.level.guided";
        /// <summary>Colore del livello 2 · Operational.</summary>
        public const string LEVEL_OPERATIONAL = "color.level.operational";
        /// <summary>Colore del livello 3 · Crisis.</summary>
        public const string LEVEL_CRISIS = "color.level.crisis";
        /// <summary>Barre piene dell'icona livello su bianco (#168F6B).</summary>
        public const string LEVEL_BAR_ON_LIGHT = "color.level.bar.on-light";
        /// <summary>Barre vuote (solo contorno) su bianco.</summary>
        public const string LEVEL_BAR_OFF_LIGHT = "color.level.bar.off-light";
        /// <summary>Barre piene su fondo scuro (#ADDD8E).</summary>
        public const string LEVEL_BAR_ON_DARK = "color.level.bar.on-dark";
        /// <summary>Barre vuote (solo contorno) su fondo scuro.</summary>
        public const string LEVEL_BAR_OFF_DARK = "color.level.bar.off-dark";

        // --- Esiti ---
        /// <summary>Esito positivo.</summary>
        public const string OUTCOME_POSITIVE = "color.outcome.positive";
        /// <summary>Esito parziale (solo su card bianca).</summary>
        public const string OUTCOME_PARTIAL = "color.outcome.partial";
        /// <summary>Esito negativo (testo nero).</summary>
        public const string OUTCOME_NEGATIVE = "color.outcome.negative";

        // --- Sottotitoli ---
        /// <summary>Sfondo sottotitoli (alpha &gt;= 0,92).</summary>
        public const string SUBTITLE_BG = "color.subtitle.bg";
        /// <summary>Testo sottotitoli.</summary>
        public const string SUBTITLE_TEXT = "color.subtitle.text";
        /// <summary>Riga evento sonoro.</summary>
        public const string SUBTITLE_EVENT = "color.subtitle.event";
        /// <summary>Nome di chi parla.</summary>
        public const string SUBTITLE_SPEAKER = "color.subtitle.speaker";

        // --- Etichette nel mondo e interazione ---
        /// <summary>Cerchio tratteggiato del rischio.</summary>
        public const string RISK_STROKE = "color.risk.stroke";
        /// <summary>Bordo esterno nero del cerchio rischio.</summary>
        public const string RISK_OUTER = "color.risk.outer";
        /// <summary>Contorno grab interno.</summary>
        public const string GRAB_INNER = "color.grab.inner";
        /// <summary>Contorno grab esterno.</summary>
        public const string GRAB_OUTER = "color.grab.outer";
        /// <summary>Puntatore valido (proposta VR Interaction System).</summary>
        public const string POINTER_VALID = "color.pointer.valid";
        /// <summary>Puntatore non valido (proposta VR Interaction System).</summary>
        public const string POINTER_INVALID = "color.pointer.invalid";

        // --- Token derivati (non variabili Figma) ---
        /// <summary>Riempimento pulsante disabilitato: #ECECEC al 40% (Figma, nota 5). Token derivato.</summary>
        public const string BUTTON_DISABLED = "color.button.disabled";
        /// <summary>Testo del pulsante disabilitato: nero (Figma, nota 5). Token derivato.</summary>
        public const string BUTTON_DISABLED_TEXT = "color.button.disabled-text";
        /// <summary>Parte alta dello sfondo segnaposto della scena menu (PROVVISORIO, in attesa del 360° pre-sfocato). Token derivato.</summary>
        public const string BACKDROP_TOP = "color.backdrop.top";
        /// <summary>Parte bassa dello sfondo segnaposto e colore di sfondo della camera (PROVVISORIO). Token derivato.</summary>
        public const string BACKDROP_BOTTOM = "color.backdrop.bottom";
    }

    /// <summary>
    /// Nomi puntati dei token numerici (collezione Figma "Dimension") e dei valori derivati.
    /// Tutti i valori sono in unità Canvas (1 px Figma = 1 unità).
    /// </summary>
    public static class NumberTokens
    {
        /// <summary>Spaziatura XS (4).</summary>
        public const string SPACE_XS = "space.xs";
        /// <summary>Spaziatura S (8).</summary>
        public const string SPACE_S = "space.s";
        /// <summary>Spaziatura M (12).</summary>
        public const string SPACE_M = "space.m";
        /// <summary>Spaziatura L (16).</summary>
        public const string SPACE_L = "space.l";
        /// <summary>Spaziatura XL (24).</summary>
        public const string SPACE_XL = "space.xl";
        /// <summary>Padding interno dei pannelli (24).</summary>
        public const string PADDING_PANEL = "padding.panel";
        /// <summary>Spazio tra le celle delle griglie (16).</summary>
        public const string GAP_GRID = "gap.grid";
        /// <summary>Raggio angoli pannello (24).</summary>
        public const string RADIUS_PANEL = "radius.panel";
        /// <summary>Raggio angoli card (12).</summary>
        public const string RADIUS_CARD = "radius.card";
        /// <summary>Raggio angoli pulsante (10).</summary>
        public const string RADIUS_BUTTON = "radius.button";
        /// <summary>Raggio "pillola" (999 = completamente arrotondato).</summary>
        public const string RADIUS_PILL = "radius.pill";
        /// <summary>Dimensione minima di un bersaglio interattivo (40).</summary>
        public const string SIZE_TARGET_MIN = "size.target-min";

        // --- Derivati (non variabili Figma, ricavati dalle varianti) ---
        /// <summary>Spessore bordo del pulsante primario (2). Punto aperto di team.</summary>
        public const string BORDER_PRIMARY = "border.primary";
        /// <summary>Spessore contorno delle barre livello vuote (1,5).</summary>
        public const string STROKE_LEVEL_BAR = "stroke.level-bar";
        /// <summary>Margine del frame schermata attorno a PANEL_Menu e PANEL_BottomBar (40).</summary>
        public const string MARGIN_SCREEN = "margin.screen";
        /// <summary>Larghezza del Canvas di CANVAS_Menu = frame Figma (840).</summary>
        public const string MENU_CANVAS_WIDTH = "size.menu-canvas.width";
        /// <summary>Altezza del Canvas di CANVAS_Menu = frame Figma (627).</summary>
        public const string MENU_CANVAS_HEIGHT = "size.menu-canvas.height";
        /// <summary>Distanza dei menu dagli occhi, in METRI (1,5). Non in unità Canvas.</summary>
        public const string MENU_DISTANCE = "distance.menu";
        /// <summary>Altezza da terra del centro del menu, in METRI (1,5). Non in unità Canvas.</summary>
        public const string MENU_CENTER_HEIGHT = "height.menu-center";
        /// <summary>Altezza degli occhi in piedi, in METRI (1,6). Non in unità Canvas.</summary>
        public const string EYE_HEIGHT = "height.eye";
        /// <summary>Larghezza fissa di PANEL_Menu (760).</summary>
        public const string MENU_PANEL_WIDTH = "size.menu-panel.width";
        /// <summary>Altezza di default di PANEL_Menu, Home (475).</summary>
        public const string MENU_PANEL_HEIGHT = "size.menu-panel.height";
        /// <summary>Altezza di PANEL_Menu su Mission Brief (535, punto aperto).</summary>
        public const string MENU_PANEL_HEIGHT_BRIEF = "size.menu-panel.height-brief";
        /// <summary>Larghezza fissa di PANEL_Side (320).</summary>
        public const string SIDE_PANEL_WIDTH = "size.side-panel.width";
        /// <summary>Distanza tra PANEL_Side e PANEL_Menu (24).</summary>
        public const string SIDE_PANEL_GAP = "gap.side-panel";
        /// <summary>Spazio tra titolo e righe di PANEL_Side e tra le righe (6).</summary>
        public const string SIDE_PANEL_ROW_GAP = "gap.side-panel.rows";
        /// <summary>Inclinazione di PANEL_Side verso il giocatore, in gradi (20). Unico token in gradi, non in unità Canvas.</summary>
        public const string SIDE_PANEL_TILT = "angle.side-panel.tilt";
        /// <summary>Altezza minima del pannello coperta senza compressione da TEX_PanelShadowCompact (120).</summary>
        public const string SHADOW_COMPACT_MIN_HEIGHT = "size.shadow.compact-min-height";
        /// <summary>Larghezza fissa di PANEL_BottomBar (520).</summary>
        public const string BOTTOM_BAR_WIDTH = "size.bottom-bar.width";
        /// <summary>Altezza di PANEL_BottomBar (56); pillola completa (raggio = metà altezza).</summary>
        public const string BOTTOM_BAR_HEIGHT = "size.bottom-bar.height";
        /// <summary>Padding interno di PANEL_BottomBar (6).</summary>
        public const string BOTTOM_BAR_PADDING = "padding.bottom-bar";
        /// <summary>Distanza tra il bordo inferiore di PANEL_Menu e PANEL_BottomBar (16).</summary>
        public const string BOTTOM_BAR_MENU_GAP = "gap.bottom-bar.menu";
        /// <summary>Altezza di BTN_BottomBarItem (44).</summary>
        public const string BOTTOM_BAR_ITEM_HEIGHT = "size.bottom-bar-item.height";
        /// <summary>Padding orizzontale di BTN_BottomBarItem (10).</summary>
        public const string BOTTOM_BAR_ITEM_PADDING = "padding.bottom-bar-item";
        /// <summary>Lato dell'icona di BTN_BottomBarItem (32).</summary>
        public const string BOTTOM_BAR_ITEM_ICON = "size.bottom-bar-item.icon";
        /// <summary>Spazio tra icona ed etichetta di BTN_BottomBarItem (6).</summary>
        public const string BOTTOM_BAR_ITEM_GAP = "gap.bottom-bar-item";
    }

    /// <summary>
    /// Identificativi degli stili di testo (Figma, famiglia Jaldi).
    /// </summary>
    public static class TextStyleIds
    {
        /// <summary>Display: Bold 64.</summary>
        public const string DISPLAY = "Display";
        /// <summary>Title: Bold 32.</summary>
        public const string TITLE = "Title";
        /// <summary>Subtitle: Regular 24.</summary>
        public const string SUBTITLE = "Subtitle";
        /// <summary>Subtitle Bold: Bold 24.</summary>
        public const string SUBTITLE_BOLD = "Subtitle Bold";
        /// <summary>HUD min: Regular 21 (dimensione minima per la UI in scenario).</summary>
        public const string HUD_MIN = "HUD min";
        /// <summary>Body: Regular 18.</summary>
        public const string BODY = "Body";
        /// <summary>Body Bold: Bold 18.</summary>
        public const string BODY_BOLD = "Body Bold";
    }

    /// <summary>
    /// Ruoli delle ombre e del glow (effetti Figma). In uGUI non si usano componenti Shadow/Outline:
    /// le ombre sono sprite 9-slice pre-sfocati generati da questi valori.
    /// </summary>
    public static class ShadowRoles
    {
        /// <summary>Ombra card a riposo (0/3, blur 8, nero 25%).</summary>
        public const string CARD = "card";
        /// <summary>Sollevamento dello stato selezionato (0/8, blur 20, nero 35%).</summary>
        public const string SELECTED_LIFT = "selected.lift";
        /// <summary>Glow morbido brand.light dello stato selezionato (blur 18).</summary>
        public const string SELECTED_GLOW = "selected.glow";
        /// <summary>Ombra hover (= selected.lift, la scala 1,04 si applica a runtime).</summary>
        public const string HOVER = "hover";
        /// <summary>Ombra pressed (= card, la scala 0,98 si applica a runtime).</summary>
        public const string PRESSED = "pressed";
        /// <summary>Ombra del cerchio bianco con la spunta di selezione.</summary>
        public const string CHECK_BADGE = "check.badge";
        /// <summary>Ombra della bottom bar.</summary>
        public const string BOTTOM_BAR = "bottomBar";
        /// <summary>Ombra del pannello menu (0/20, blur 60, nero 28%).</summary>
        public const string MENU_PANEL = "menuPanel";
        /// <summary>Ombra del pannello laterale (0/20, blur 60, nero 30%).</summary>
        public const string SIDE_PANEL = "sidePanel";
        /// <summary>Ombra di Pausa e dialoghi di conferma (0/20, blur 60, nero 35%).</summary>
        public const string DIALOG = "dialog";
        /// <summary>Ombra delle card HUD (avvisi, feedback, etichette nel mondo).</summary>
        public const string HUD_CARD = "hudCard";
    }
}
