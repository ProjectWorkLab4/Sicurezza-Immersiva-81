using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Tema comune della UI (asset SO_UITheme): colori, stili di testo, ombre, misure, animazioni e
    /// costanti di posizionamento. I componenti leggono i valori SOLO da qui, mai valori scritti a mano.
    /// L'asset è generato dal menu "SI81/UI/Build Theme" a partire da Editor/Source/tokens_figma.json, che è l'UNICA
    /// fonte di verità: ogni esecuzione riscrive tutti i valori, quindi le modifiche fatte nell'Inspector vengono perse.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Theme", fileName = "SO_UITheme")]
    public class UITheme : ScriptableObject
    {
        // =====================================================================
        // Costanti di scala e distanza (CLAUDE.md §3, §4.11)
        // =====================================================================

        /// <summary>Scala del Canvas World Space per ogni metro di distanza: scala = 0,00126 × distanza (m).</summary>
        public const float CANVAS_SCALE_PER_METER = 0.00126f;
        /// <summary>Distanza di HUD_DecisionPanel, in metri (davanti al giocatore).</summary>
        public const float DECISION_DISTANCE_M = 1.2f;
        /// <summary>Distanza di HUD_OutcomePanel e PANEL_Pause, in metri (al centro).</summary>
        public const float OUTCOME_DISTANCE_M = 1.5f;
        /// <summary>Distanza di HUD_Subtitles, in metri (Lazy Follow).</summary>
        public const float SUBTITLE_DISTANCE_M = 1.0f;
        /// <summary>
        /// Colore "dato mancante": magenta pieno, volutamente brutto e ben visibile. È l'UNICO colore scritto nel codice:
        /// lo restituiscono le ricerche di token inesistenti ed è il valore iniziale di ogni campo colore, così un dato
        /// non generato dal builder (SI81/UI/Build Theme, da tokens_figma.json) si vede subito a schermo.
        /// </summary>
        public static readonly Color MISSING_COLOR = new Color(1f, 0f, 1f, 1f);

        /// <summary>
        /// Calcola la scala del Canvas World Space per una distanza data.
        /// </summary>
        /// <param name="distanceMeters">Distanza dagli occhi in metri.</param>
        /// <returns>Scala uniforme da assegnare al RectTransform del Canvas.</returns>
        public static float GetCanvasScale(float distanceMeters)
        {
            return CANVAS_SCALE_PER_METER * distanceMeters;
        }

        // =====================================================================
        // Tipi serializzati
        // =====================================================================

        /// <summary>Un token colore Figma: nome puntato, valore e descrizione.</summary>
        [Serializable]
        public class ColorToken
        {
            /// <summary>Nome puntato del token, es. "color.brand.dark".</summary>
            public string Token;
            /// <summary>Valore RGBA (alpha incluso, es. vetro all'85%). Iniziale = MISSING_COLOR finché il builder non lo scrive.</summary>
            public Color Value = MISSING_COLOR;
            /// <summary>Descrizione dalla variabile Figma (uso e contrasto).</summary>
            [TextArea(1, 3)] public string Description;
        }

        /// <summary>Un token numerico (unità Canvas).</summary>
        [Serializable]
        public class NumberToken
        {
            /// <summary>Nome puntato del token, es. "radius.card".</summary>
            public string Token;
            /// <summary>Valore in unità Canvas (1 px Figma = 1 unità).</summary>
            public float Value;
        }

        /// <summary>Uno stile di testo Jaldi: font TMP, dimensione, interlinea, peso.</summary>
        [Serializable]
        public class TextStyle
        {
            /// <summary>Identificativo dello stile (vedi <see cref="TextStyleIds"/>).</summary>
            public string Name;
            /// <summary>Font asset TMP (FNT_JaldiRegular_SDF o FNT_JaldiBold_SDF). Usare il suo fontSharedMaterial.</summary>
            public TMP_FontAsset Font;
            /// <summary>Dimensione in unità Canvas.</summary>
            public float Size = 18f;
            /// <summary>Interlinea Figma in percentuale della dimensione (125%).</summary>
            public float LineHeightPercent = 125f;
            /// <summary>Valore da assegnare a TMP_Text.lineSpacing per ottenere l'interlinea Figma con questo font.</summary>
            public float LineSpacing;
            /// <summary>Spaziatura caratteri (TMP characterSpacing). Figma: 0%.</summary>
            public float CharacterSpacing;
            /// <summary>Peso del testo (informativo: il grassetto si ottiene col font Bold, non con lo stile sintetico).</summary>
            public FontWeight Weight = FontWeight.Regular;

            /// <summary>
            /// Applica lo stile a un testo TMP usando il materiale condiviso del font (nessun materiale extra).
            /// </summary>
            /// <param name="text">Testo TMP da configurare.</param>
            public void ApplyTo(TMP_Text text)
            {
                if (text == null) return;
                if (Font != null)
                {
                    text.font = Font;
                    text.fontSharedMaterial = Font.material;
                }
                text.fontSize = Size;
                text.lineSpacing = LineSpacing;
                text.characterSpacing = CharacterSpacing;
                text.fontWeight = FontWeight.Regular;
            }
        }

        /// <summary>Un'ombra o un glow (effetto Figma DROP_SHADOW).</summary>
        [Serializable]
        public class ShadowToken
        {
            /// <summary>Ruolo dell'effetto (vedi <see cref="ShadowRoles"/>).</summary>
            public string Role;
            /// <summary>Spostamento in unità Canvas, asse Y verso l'alto (Figma y=8 diventa -8).</summary>
            public Vector2 Offset;
            /// <summary>Raggio di sfocatura in unità Canvas.</summary>
            public float Blur;
            /// <summary>Espansione in unità Canvas.</summary>
            public float Spread;
            /// <summary>Colore con alpha. Iniziale = MISSING_COLOR finché il builder non lo scrive.</summary>
            public Color Color = MISSING_COLOR;
        }

        /// <summary>Riflesso diagonale dei pannelli vetro (gradiente lineare Figma "sheen").</summary>
        [Serializable]
        public class GlassSheen
        {
            /// <summary>Colore all'inizio del gradiente (da Figma "glassPanel.sheen").</summary>
            public Color StartColor = MISSING_COLOR;
            /// <summary>Colore alla fine del gradiente (da Figma "glassPanel.sheen").</summary>
            public Color EndColor = MISSING_COLOR;
            /// <summary>Posizione (0–1) dove il riflesso diventa trasparente.</summary>
            public float EndPosition;
            /// <summary>
            /// Maniglia iniziale del gradiente sulla diagonale normalizzata del pannello, d = (u+v)/2 (u da sinistra,
            /// v dall'alto). Da tokens_figma.json "project.glassSheen" (Figma: 1/7).
            /// </summary>
            public float HandleStart;
            /// <summary>Maniglia finale del gradiente sulla stessa diagonale (Figma: 6/7).</summary>
            public float HandleEnd;

            /// <summary>
            /// Colore del riflesso nel punto normalizzato (u, v) del pannello: u da sinistra (0) a destra (1),
            /// v dall'alto (0) al basso (1). Il gradiente si allunga con il pannello (spazio UV, come in Figma).
            /// </summary>
            /// <param name="u">Coordinata orizzontale normalizzata.</param>
            /// <param name="v">Coordinata verticale normalizzata (dall'alto).</param>
            /// <returns>Colore con alpha del riflesso in quel punto.</returns>
            public Color Evaluate(float u, float v)
            {
                float d = (u + v) * 0.5f;
                float span = HandleEnd - HandleStart;
                float t = span > 0f ? (d - HandleStart) / span : 0f;
                float k = EndPosition > 0f ? Mathf.Clamp01(t / EndPosition) : (t > 0f ? 1f : 0f);
                return Color.Lerp(StartColor, EndColor, k);
            }
        }

        /// <summary>Valori di animazione e interazione. Nessun valore nel codice: arrivano da tokens_figma.json "project.motion" (PROVVISORI).</summary>
        [Serializable]
        public class MotionSettings
        {
            /// <summary>Scala allo stato hover (Figma: varianti con scala 1,04).</summary>
            public float HoverScale;
            /// <summary>Scala allo stato pressed (Figma: varianti con scala 0,98).</summary>
            public float PressedScale;
            /// <summary>Durata transizione hover in secondi. PROVVISORIO (guideline §3.7 non disponibile).</summary>
            public float HoverDuration;
            /// <summary>Durata transizione pressed in secondi. PROVVISORIO.</summary>
            public float PressDuration;
            /// <summary>Durata transizione selezione in secondi. PROVVISORIO.</summary>
            public float SelectDuration;
            /// <summary>Durata dissolvenza pannelli in secondi. PROVVISORIO.</summary>
            public float FadeDuration;
            /// <summary>Durata cambio schermata in secondi. PROVVISORIO.</summary>
            public float ScreenTransitionDuration;
        }

        /// <summary>Regole della HUD in scenario (docs/Guida_HUD_comune_SI81.pdf, CLAUDE.md §4.11). Nessun valore nel codice: arrivano da tokens_figma.json "project.hud".</summary>
        [Serializable]
        public class HudSettings
        {
            /// <summary>Durata di un avviso breve (HUD_EventAlert / HUD_ActionFeedback) in secondi.</summary>
            public float AlertDuration;
            /// <summary>Angolo verticale degli avvisi brevi, in gradi (in basso nella vista).</summary>
            public float AlertPitch;
            /// <summary>Angolo verticale minimo dei sottotitoli, in gradi.</summary>
            public float SubtitlePitchMin;
            /// <summary>Angolo verticale massimo dei sottotitoli, in gradi.</summary>
            public float SubtitlePitchMax;
            /// <summary>Nessun elemento sopra questo angolo, in gradi.</summary>
            public float MaxPitch;
            /// <summary>Caratteri massimi per riga dei sottotitoli.</summary>
            public int SubtitleMaxChars;
            /// <summary>Righe massime dei sottotitoli.</summary>
            public int SubtitleMaxLines;
            /// <summary>Caratteri massimi per riga dell'obiettivo sul polso (circa).</summary>
            public int WristObjectiveMaxChars;
            /// <summary>Opacità del pannello polso quando non è rivolto agli occhi.</summary>
            public float WristIdleAlpha;
            /// <summary>Distanza (m) entro cui il pannello polso, rivolto agli occhi, va al 100%.</summary>
            public float WristFocusDistance;
            /// <summary>Dimensioni del pannello polso in metri (circa 12 × 7 cm).</summary>
            public Vector2 WristPanelSizeMeters;
            /// <summary>Primo avviso timer (secondi rimanenti) con impulso aptico.</summary>
            public float TimerWarningFirst;
            /// <summary>Secondo avviso timer (secondi rimanenti) con impulso aptico.</summary>
            public float TimerWarningSecond;
            /// <summary>Distanza minima delle etichette ancorate agli oggetti, in metri.</summary>
            public float WorldLabelMinDistance;
            /// <summary>Distanza massima delle etichette ancorate agli oggetti, in metri.</summary>
            public float WorldLabelMaxDistance;
            /// <summary>Numero massimo di HUD_RiskLabel visibili insieme.</summary>
            public int MaxRiskLabels;
        }

        /// <summary>Misure del raggio di puntamento UI (UIPointer). Nessun valore nel codice: arrivano da tokens_figma.json "project.pointer" (PROVVISORI).</summary>
        [Serializable]
        public class PointerSettings
        {
            /// <summary>Spessore del raggio in metri.</summary>
            public float RayWidth;
            /// <summary>Lunghezza massima del raggio in metri (oltre, nessun puntamento visibile).</summary>
            public float RayMaxLength;
            /// <summary>Lunghezza del raggio quando non punta la UI, in metri (il raggio resta sempre visibile).</summary>
            public float IdleRayLength;
            /// <summary>Diametro del reticolo nel punto colpito, in metri.</summary>
            public float ReticleDiameter;
            /// <summary>Distacco del reticolo dalla superficie colpita verso il controller, in metri (evita lo z-fighting).</summary>
            public float ReticleSurfaceOffset;
        }

        // =====================================================================
        // Dati serializzati
        // =====================================================================

        [Header("Colori (token Figma + derivati)")]
        [SerializeField] private List<ColorToken> colors = new List<ColorToken>();

        [Header("Stili di testo (Jaldi)")]
        [SerializeField] private List<TextStyle> textStyles = new List<TextStyle>();

        [Header("Ombre e glow")]
        [SerializeField] private List<ShadowToken> shadows = new List<ShadowToken>();
        [SerializeField] private GlassSheen glassSheen = new GlassSheen();

        [Header("Misure (unità Canvas)")]
        [SerializeField] private List<NumberToken> numbers = new List<NumberToken>();

        [Header("Animazioni e HUD (da tokens_figma.json \"project\": modifiche qui perse a ogni Build Theme)")]
        [SerializeField] private MotionSettings motion = new MotionSettings();
        [SerializeField] private HudSettings hud = new HudSettings();
        [SerializeField] private PointerSettings pointer = new PointerSettings();

        // Cache di ricerca per nome, ricostruite quando l'asset cambia.
        [NonSerialized] private Dictionary<string, ColorToken> colorLookup;
        [NonSerialized] private Dictionary<string, NumberToken> numberLookup;
        [NonSerialized] private Dictionary<string, TextStyle> textStyleLookup;
        [NonSerialized] private Dictionary<string, ShadowToken> shadowLookup;
        [NonSerialized] private HashSet<string> warnedTokens;

        // =====================================================================
        // Accesso pubblico in sola lettura
        // =====================================================================

        /// <summary>Tutti i token colore (lettura).</summary>
        public IReadOnlyList<ColorToken> Colors => colors;
        /// <summary>Tutti gli stili di testo (lettura).</summary>
        public IReadOnlyList<TextStyle> TextStyles => textStyles;
        /// <summary>Tutte le ombre e i glow (lettura).</summary>
        public IReadOnlyList<ShadowToken> Shadows => shadows;
        /// <summary>Tutti i token numerici (lettura).</summary>
        public IReadOnlyList<NumberToken> Numbers => numbers;
        /// <summary>Riflesso diagonale dei pannelli vetro.</summary>
        public GlassSheen Sheen => glassSheen;
        /// <summary>Valori di animazione (scale hover/pressed, durate).</summary>
        public MotionSettings Motion => motion;
        /// <summary>Regole della HUD in scenario.</summary>
        public HudSettings Hud => hud;
        /// <summary>Misure del raggio di puntamento UI (UIPointer).</summary>
        public PointerSettings Pointer => pointer;

        // --- Gruppi tipizzati di colori ---

        /// <summary>Verde scuro del brand (#1B5E48).</summary>
        public Color BrandDark => GetColor(ColorTokens.BRAND_DARK);
        /// <summary>Verde chiaro del brand (glow di selezione, evidenza su scuro).</summary>
        public Color BrandLight => GetColor(ColorTokens.BRAND_LIGHT);
        /// <summary>Colore di conferma / esito positivo.</summary>
        public Color Confirm => GetColor(ColorTokens.CONFIRM);
        /// <summary>Colore di errore / rischio.</summary>
        public Color Deny => GetColor(ColorTokens.DENY);
        /// <summary>Verde attivo #4ADE80 (pulsante primario, voce attiva).</summary>
        public Color StateActive => GetColor(ColorTokens.STATE_ACTIVE);
        /// <summary>Superficie card bianca.</summary>
        public Color SurfaceCard => GetColor(ColorTokens.SURFACE_CARD);
        /// <summary>Pannello opaco per la UI in scenario.</summary>
        public Color SurfacePanel => GetColor(ColorTokens.SURFACE_PANEL);
        /// <summary>Vetro dei menu (brand.dark 85%).</summary>
        public Color SurfaceGlass => GetColor(ColorTokens.SURFACE_GLASS);
        /// <summary>Vetro chiaro della bottom bar (bianco 78%).</summary>
        public Color SurfaceGlassLight => GetColor(ColorTokens.SURFACE_GLASS_LIGHT);
        /// <summary>Testo nero su fondi chiari.</summary>
        public Color TextPrimary => GetColor(ColorTokens.TEXT_PRIMARY);
        /// <summary>Testo bianco su fondi scuri.</summary>
        public Color TextOnDark => GetColor(ColorTokens.TEXT_ON_DARK);
        /// <summary>Testo secondario su brand.dark.</summary>
        public Color TextOnGreen => GetColor(ColorTokens.TEXT_ON_GREEN);
        /// <summary>Testo secondario su bianco.</summary>
        public Color TextSecondary => GetColor(ColorTokens.TEXT_SECONDARY);
        /// <summary>Tile neutra.</summary>
        public Color TileNeutral => GetColor(ColorTokens.TILE_NEUTRAL);
        /// <summary>Riempimento pulsante disabilitato (#ECECEC 40%).</summary>
        public Color ButtonDisabled => GetColor(ColorTokens.BUTTON_DISABLED);
        /// <summary>Testo pulsante disabilitato (nero).</summary>
        public Color ButtonDisabledText => GetColor(ColorTokens.BUTTON_DISABLED_TEXT);
        /// <summary>Sfondo sottotitoli.</summary>
        public Color SubtitleBackground => GetColor(ColorTokens.SUBTITLE_BG);
        /// <summary>Esito positivo.</summary>
        public Color OutcomePositive => GetColor(ColorTokens.OUTCOME_POSITIVE);
        /// <summary>Esito parziale.</summary>
        public Color OutcomePartial => GetColor(ColorTokens.OUTCOME_PARTIAL);
        /// <summary>Esito negativo.</summary>
        public Color OutcomeNegative => GetColor(ColorTokens.OUTCOME_NEGATIVE);
        /// <summary>Puntatore UI su un elemento interattivo.</summary>
        public Color PointerValid => GetColor(ColorTokens.POINTER_VALID);
        /// <summary>Puntatore UI su un elemento non interattivo.</summary>
        public Color PointerInvalid => GetColor(ColorTokens.POINTER_INVALID);

        // --- Stili di testo tipizzati ---

        /// <summary>Stile Display (Bold 64).</summary>
        public TextStyle Display => GetTextStyle(TextStyleIds.DISPLAY);
        /// <summary>Stile Title (Bold 32).</summary>
        public TextStyle Title => GetTextStyle(TextStyleIds.TITLE);
        /// <summary>Stile Subtitle (Regular 24).</summary>
        public TextStyle Subtitle => GetTextStyle(TextStyleIds.SUBTITLE);
        /// <summary>Stile Subtitle Bold (Bold 24).</summary>
        public TextStyle SubtitleBold => GetTextStyle(TextStyleIds.SUBTITLE_BOLD);
        /// <summary>Stile HUD min (Regular 21).</summary>
        public TextStyle HudMin => GetTextStyle(TextStyleIds.HUD_MIN);
        /// <summary>Stile Body (Regular 18).</summary>
        public TextStyle Body => GetTextStyle(TextStyleIds.BODY);
        /// <summary>Stile Body Bold (Bold 18).</summary>
        public TextStyle BodyBold => GetTextStyle(TextStyleIds.BODY_BOLD);

        // --- Misure tipizzate ---

        /// <summary>Raggio angoli pannello.</summary>
        public float RadiusPanel => GetNumber(NumberTokens.RADIUS_PANEL);
        /// <summary>Raggio angoli card.</summary>
        public float RadiusCard => GetNumber(NumberTokens.RADIUS_CARD);
        /// <summary>Raggio angoli pulsante.</summary>
        public float RadiusButton => GetNumber(NumberTokens.RADIUS_BUTTON);
        /// <summary>Raggio pillola.</summary>
        public float RadiusPill => GetNumber(NumberTokens.RADIUS_PILL);
        /// <summary>Padding pannello.</summary>
        public float PaddingPanel => GetNumber(NumberTokens.PADDING_PANEL);
        /// <summary>Spazio tra celle di griglia.</summary>
        public float GapGrid => GetNumber(NumberTokens.GAP_GRID);
        /// <summary>Dimensione minima di un bersaglio interattivo.</summary>
        public float TargetMinSize => GetNumber(NumberTokens.SIZE_TARGET_MIN);
        /// <summary>Margine del frame schermata attorno ai pannelli menu (40).</summary>
        public float MarginScreen => GetNumber(NumberTokens.MARGIN_SCREEN);
        /// <summary>Dimensione del Canvas di CANVAS_Menu (frame Figma 840 × 627), in unità Canvas.</summary>
        public Vector2 MenuCanvasSize => new Vector2(GetNumber(NumberTokens.MENU_CANVAS_WIDTH), GetNumber(NumberTokens.MENU_CANVAS_HEIGHT));
        /// <summary>Distanza dei menu dagli occhi, in metri (scala risultante 0,00189).</summary>
        public float MenuDistance => GetNumber(NumberTokens.MENU_DISTANCE);
        /// <summary>Altezza da terra del centro del menu, in metri (poco sotto gli occhi).</summary>
        public float MenuCenterHeight => GetNumber(NumberTokens.MENU_CENTER_HEIGHT);
        /// <summary>Altezza degli occhi in piedi, in metri (camera della sandbox sul desktop).</summary>
        public float EyeHeight => GetNumber(NumberTokens.EYE_HEIGHT);

        // =====================================================================
        // Ricerca per nome
        // =====================================================================

        /// <summary>
        /// Restituisce il colore di un token per nome puntato (es. "color.brand.dark").
        /// Se il token non esiste restituisce magenta e scrive UN avviso per token.
        /// </summary>
        /// <param name="token">Nome puntato del token (vedi <see cref="ColorTokens"/>).</param>
        /// <returns>Il colore del token, o <see cref="MISSING_COLOR"/>.</returns>
        public Color GetColor(string token)
        {
            EnsureCache();
            if (token != null && colorLookup.TryGetValue(token, out ColorToken entry)) return entry.Value;
            WarnOnce("colore", token);
            return MISSING_COLOR;
        }

        /// <summary>
        /// Prova a leggere il colore di un token senza scrivere avvisi.
        /// </summary>
        /// <param name="token">Nome puntato del token.</param>
        /// <param name="color">Colore trovato.</param>
        /// <returns>true se il token esiste.</returns>
        public bool TryGetColor(string token, out Color color)
        {
            EnsureCache();
            if (token != null && colorLookup.TryGetValue(token, out ColorToken entry))
            {
                color = entry.Value;
                return true;
            }
            color = MISSING_COLOR;
            return false;
        }

        /// <summary>
        /// Restituisce un valore numerico per nome puntato (es. "radius.card"). 0 e un avviso se manca.
        /// </summary>
        /// <param name="token">Nome puntato del token (vedi <see cref="NumberTokens"/>).</param>
        /// <returns>Valore in unità Canvas.</returns>
        public float GetNumber(string token)
        {
            EnsureCache();
            if (token != null && numberLookup.TryGetValue(token, out NumberToken entry)) return entry.Value;
            WarnOnce("numero", token);
            return 0f;
        }

        /// <summary>
        /// Restituisce uno stile di testo per nome (es. "Body Bold"). Null e un avviso se manca.
        /// </summary>
        /// <param name="styleName">Nome dello stile (vedi <see cref="TextStyleIds"/>).</param>
        /// <returns>Lo stile, o null.</returns>
        public TextStyle GetTextStyle(string styleName)
        {
            EnsureCache();
            if (styleName != null && textStyleLookup.TryGetValue(styleName, out TextStyle entry)) return entry;
            WarnOnce("stile di testo", styleName);
            return null;
        }

        /// <summary>
        /// Restituisce un'ombra o un glow per ruolo (es. "selected.lift"). Null e un avviso se manca.
        /// </summary>
        /// <param name="role">Ruolo dell'effetto (vedi <see cref="ShadowRoles"/>).</param>
        /// <returns>L'ombra, o null.</returns>
        public ShadowToken GetShadow(string role)
        {
            EnsureCache();
            if (role != null && shadowLookup.TryGetValue(role, out ShadowToken entry)) return entry;
            WarnOnce("ombra", role);
            return null;
        }

        // =====================================================================
        // Scrittura dati (solo per il builder Editor, assembly SI81.UI.Editor)
        // =====================================================================

        /// <summary>
        /// Sostituisce tutti i dati del tema. Usato solo dal builder "SI81/UI/Build Theme".
        /// </summary>
        internal void SetData(List<ColorToken> newColors, List<TextStyle> newTextStyles, List<ShadowToken> newShadows,
            GlassSheen newSheen, List<NumberToken> newNumbers, MotionSettings newMotion, HudSettings newHud, PointerSettings newPointer)
        {
            colors = newColors;
            textStyles = newTextStyles;
            shadows = newShadows;
            glassSheen = newSheen;
            numbers = newNumbers;
            motion = newMotion;
            hud = newHud;
            pointer = newPointer;
            InvalidateCache();
        }

        // =====================================================================
        // Ciclo di vita e cache
        // =====================================================================

        /// <summary>
        /// Svuota le cache quando l'asset viene caricato.
        /// </summary>
        private void OnEnable()
        {
            InvalidateCache();
        }

        /// <summary>
        /// Svuota le cache quando l'asset viene modificato dall'Inspector.
        /// </summary>
        private void OnValidate()
        {
            InvalidateCache();
        }

        /// <summary>
        /// Invalida tutte le cache di ricerca: verranno ricostruite al prossimo accesso.
        /// </summary>
        private void InvalidateCache()
        {
            colorLookup = null;
            numberLookup = null;
            textStyleLookup = null;
            shadowLookup = null;
        }

        /// <summary>
        /// Costruisce le cache di ricerca per nome se non esistono ancora.
        /// </summary>
        private void EnsureCache()
        {
            if (colorLookup != null) return;

            colorLookup = new Dictionary<string, ColorToken>(StringComparer.Ordinal);
            foreach (ColorToken c in colors)
                if (c != null && !string.IsNullOrEmpty(c.Token)) colorLookup[c.Token] = c;

            numberLookup = new Dictionary<string, NumberToken>(StringComparer.Ordinal);
            foreach (NumberToken n in numbers)
                if (n != null && !string.IsNullOrEmpty(n.Token)) numberLookup[n.Token] = n;

            textStyleLookup = new Dictionary<string, TextStyle>(StringComparer.Ordinal);
            foreach (TextStyle t in textStyles)
                if (t != null && !string.IsNullOrEmpty(t.Name)) textStyleLookup[t.Name] = t;

            shadowLookup = new Dictionary<string, ShadowToken>(StringComparer.Ordinal);
            foreach (ShadowToken s in shadows)
                if (s != null && !string.IsNullOrEmpty(s.Role)) shadowLookup[s.Role] = s;
        }

        /// <summary>
        /// Scrive un solo avviso per ogni token mancante, per non riempire la Console.
        /// </summary>
        /// <param name="kind">Tipo di token (colore, numero...).</param>
        /// <param name="token">Nome cercato.</param>
        private void WarnOnce(string kind, string token)
        {
            if (warnedTokens == null) warnedTokens = new HashSet<string>();
            string id = kind + ":" + token;
            if (!warnedTokens.Add(id)) return;
            Debug.LogWarning($"[SO_UITheme] Token {kind} mancante: \"{token}\". Rigenerare con SI81/UI/Build Theme.", this);
        }
    }
}
