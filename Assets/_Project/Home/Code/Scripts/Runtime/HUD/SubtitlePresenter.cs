using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// HUD_Subtitles: sottotitoli in basso nella vista (-20°…-28°, 1 m) con lazy follow (<see cref="LazyFollow"/>), sfondo scuro
    /// opaco, nome di chi parla (colore subtitle.speaker) e al massimo 32 caratteri x 2 righe: i testi lunghi vengono divisi in
    /// pagine mostrate in sequenza (<see cref="Paginate"/>). Eventi sonori "[Allarme antincendio]" con icona (colore subtitle.event);
    /// quelli critici si vedono anche con i sottotitoli disattivati. Rispetta le impostazioni subtitles.enabled / size / background_opacity
    /// (lette da UIServices.Settings e rilette al cambio). Più sottotitoli si accodano (max 4, il più vecchio cade).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Subtitle Presenter")]
    public class SubtitlePresenter : MonoBehaviour
    {
        /// <summary>Quanti sottotitoli al massimo restano in coda.</summary>
        private const int MAX_QUEUE = 4;
        /// <summary>Margine interno orizzontale (Figma 16).</summary>
        private const float PADDING_X = 16f;
        /// <summary>Margine interno verticale (Figma 10).</summary>
        private const float PADDING_Y = 10f;
        /// <summary>Lato dell'icona dell'evento sonoro (Figma 24).</summary>
        private const float ICON_SIZE = 24f;

        /// <summary>Un sottotitolo in coda o in corso, già diviso in pagine.</summary>
        private class Entry
        {
            /// <summary>Nome di chi parla (può essere vuoto).</summary>
            public string Speaker;
            /// <summary>Pagine di testo, ciascuna entro 32 caratteri x 2 righe.</summary>
            public List<string> Pages = new List<string>();
            /// <summary>Secondi di permanenza di ogni pagina.</summary>
            public float SecondsPerPage;
            /// <summary>Vero se il sottotitolo nasce da un evento e non dal copione.</summary>
            public bool IsEvent;
        }

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private LazyFollow follow;
        [SerializeField] private GameObject content;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RoundedPanelGraphic background;
        [SerializeField] private Image eventIcon;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [Header("Tempi di lettura automatici (durata = 0)")]
        [Tooltip("Caratteri letti al secondo.")]
        [SerializeField] private float readingCharsPerSecond = 14f;
        [Tooltip("Durata minima di una pagina in secondi.")]
        [SerializeField] private float minPageSeconds = 1.5f;

        // Coda e stato di riproduzione.
        private readonly List<Entry> queue = new List<Entry>();
        private Entry current;
        private int pageIndex;
        private float pageEnd;
        private bool shown;
        private Coroutine fadeRoutine;
        private readonly StringBuilder builder = new StringBuilder(80);

        /// <summary>true se un sottotitolo è visibile o in coda.</summary>
        public bool IsBusy => current != null || queue.Count > 0;

        /// <summary>
        /// Spegne il Canvas all'avvio.
        /// </summary>
        private void Awake()
        {
            if (content != null) content.SetActive(false);
            if (follow != null) follow.enabled = false;
        }

        /// <summary>
        /// Ascolta i cambi di impostazione.
        /// </summary>
        private void OnEnable()
        {
            HUDService.SettingsChanged += HandleSettingChanged;
        }

        /// <summary>
        /// Smette di ascoltare.
        /// </summary>
        private void OnDisable()
        {
            HUDService.SettingsChanged -= HandleSettingChanged;
        }

        /// <summary>
        /// Imposta la testa per il lazy follow.
        /// </summary>
        /// <param name="head">Testa (camera) del giocatore.</param>
        public void SetHead(Transform head)
        {
            if (follow != null) follow.SetHead(head);
        }

        // =====================================================================
        // API
        // =====================================================================

        /// <summary>
        /// Accoda un sottotitolo di dialogo o di evento.
        /// </summary>
        /// <param name="speaker">Chi parla (vuoto = nessuno).</param>
        /// <param name="text">Testo.</param>
        /// <param name="seconds">Durata totale; 0 = automatica.</param>
        /// <param name="isEvent">true per un evento sonoro "[testo]".</param>
        /// <param name="critical">true = mostra anche con i sottotitoli disattivati.</param>
        public void Show(string speaker, string text, float seconds, bool isEvent, bool critical)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!critical && !SubtitlesEnabled()) return;

            var entry = new Entry { Speaker = isEvent ? string.Empty : speaker, IsEvent = isEvent };
            int maxChars = theme.Hud.SubtitleMaxChars;
            if (isEvent)
            {
                string line = strings.Format("hud.subtitles.sound_event", text);
                entry.Pages.Add(line.Length > maxChars ? line.Substring(0, maxChars - 2) + ".." : line);
            }
            else
            {
                Paginate(text, maxChars, theme.Hud.SubtitleMaxLines, entry.Pages, builder);
            }
            if (entry.Pages.Count == 0) return;

            if (seconds > 0f) entry.SecondsPerPage = seconds / entry.Pages.Count;
            else entry.SecondsPerPage = -1f;

            if (queue.Count >= MAX_QUEUE) queue.RemoveAt(0);
            queue.Add(entry);
            if (current == null) Next();
        }

        /// <summary>
        /// Toglie subito il sottotitolo in corso e svuota la coda.
        /// </summary>
        public void Clear()
        {
            queue.Clear();
            current = null;
            Hide(true);
        }

        /// <summary>
        /// Avanza la pagina o il sottotitolo quando scade il tempo.
        /// </summary>
        private void Update()
        {
            if (current == null || Time.unscaledTime < pageEnd) return;
            pageIndex++;
            if (pageIndex < current.Pages.Count) ShowPage();
            else Next();
        }

        /// <summary>
        /// Passa al sottotitolo successivo in coda, o nasconde il pannello.
        /// </summary>
        private void Next()
        {
            if (queue.Count == 0)
            {
                current = null;
                Hide(false);
                return;
            }
            current = queue[0];
            queue.RemoveAt(0);
            pageIndex = 0;
            ShowPage();
        }

        // =====================================================================
        // Impaginazione e layout
        // =====================================================================

        /// <summary>
        /// Divide un testo in pagine di al massimo <paramref name="maxLines"/> righe da <paramref name="maxChars"/> caratteri,
        /// andando a capo sulle parole (le parole più lunghe di una riga si spezzano). Ogni pagina ha le righe separate da '\n'.
        /// </summary>
        /// <param name="text">Testo da dividere.</param>
        /// <param name="maxChars">Caratteri massimi per riga.</param>
        /// <param name="maxLines">Righe massime per pagina.</param>
        /// <param name="pages">Pagine risultanti (viene svuotata prima).</param>
        /// <param name="scratch">StringBuilder di lavoro (evita allocazioni).</param>
        public static void Paginate(string text, int maxChars, int maxLines, List<string> pages, StringBuilder scratch)
        {
            pages.Clear();
            if (string.IsNullOrWhiteSpace(text) || maxChars < 2 || maxLines < 1) return;
            scratch.Length = 0;
            int lines = 0;
            int lineLength = 0;
            string[] words = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w];
                while (word.Length > 0)
                {
                    // Parola più lunga di una riga: ne prende quanto entra.
                    string piece = word.Length > maxChars ? word.Substring(0, maxChars) : word;
                    word = word.Substring(piece.Length);
                    int needed = lineLength == 0 ? piece.Length : lineLength + 1 + piece.Length;
                    if (needed > maxChars)
                    {
                        // Nuova riga (o nuova pagina se la pagina è piena).
                        lines++;
                        if (lines >= maxLines)
                        {
                            pages.Add(scratch.ToString());
                            scratch.Length = 0;
                            lines = 0;
                        }
                        else scratch.Append('\n');
                        lineLength = 0;
                    }
                    if (lineLength > 0) { scratch.Append(' '); lineLength++; }
                    scratch.Append(piece);
                    lineLength += piece.Length;
                }
            }
            if (scratch.Length > 0) pages.Add(scratch.ToString());
        }

        /// <summary>
        /// Mostra la pagina corrente: testo, misure, tempo e comparsa del pannello.
        /// </summary>
        private void ShowPage()
        {
            string page = current.Pages[pageIndex];
            float seconds = current.SecondsPerPage > 0f ? current.SecondsPerPage : Mathf.Max(minPageSeconds, page.Length / Mathf.Max(1f, readingCharsPerSecond));
            pageEnd = Time.unscaledTime + seconds;
            ApplyPage(page);
            Reveal();
        }

        /// <summary>
        /// Stile (dimensione e opacità dalle impostazioni) e dimensioni del pannello per una pagina.
        /// </summary>
        /// <param name="page">Testo della pagina (righe separate da '\n').</param>
        private void ApplyPage(string page)
        {
            float scale = Mathf.Max(0.5f, GetSizePercent() / 100f);
            bool hasSpeaker = !string.IsNullOrEmpty(current.Speaker);
            bool isEvent = current.IsEvent;

            Color bg = theme.SubtitleBackground;
            if (UIServices.Settings != null) bg.a = Mathf.Clamp01(UIServices.Settings.GetFloat(SettingsCatalog.SUBTITLES_BACKGROUND_OPACITY) / 100f);
            LabelStyling.SetColor(background, bg);

            Style(bodyText, theme.Subtitle, isEvent ? theme.GetColor(ColorTokens.SUBTITLE_EVENT) : theme.GetColor(ColorTokens.SUBTITLE_TEXT), scale);
            bodyText.text = page;
            Vector2 bodySize = bodyText.GetPreferredValues(page);
            float lineHeight = Mathf.Ceil(bodyText.GetPreferredValues("Ag").y);
            float bodyHeight = Mathf.Max(lineHeight, Mathf.Ceil(bodySize.y));

            float speakerHeight = 0f;
            float speakerWidth = 0f;
            if (hasSpeaker)
            {
                Style(speakerText, theme.SubtitleBold, theme.GetColor(ColorTokens.SUBTITLE_SPEAKER), scale);
                string label = strings.Format("hud.subtitles.speaker_line", current.Speaker, string.Empty).TrimEnd();
                speakerText.text = label;
                Vector2 speakerSize = speakerText.GetPreferredValues(label);
                speakerHeight = Mathf.Ceil(speakerSize.y);
                speakerWidth = Mathf.Ceil(speakerSize.x);
            }
            speakerText.gameObject.SetActive(hasSpeaker);

            float iconWidth = isEvent ? ICON_SIZE * scale + 8f : 0f;
            float width = PADDING_X * 2f + iconWidth + Mathf.Max(Mathf.Ceil(bodySize.x), speakerWidth);
            float height = PADDING_Y * 2f + speakerHeight + bodyHeight;
            PanelLayout.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-width * 0.5f, -height * 0.5f), new Vector2(width * 0.5f, height * 0.5f));

            if (eventIcon != null)
            {
                eventIcon.gameObject.SetActive(isEvent);
                LabelStyling.SetColor(eventIcon, theme.GetColor(ColorTokens.SUBTITLE_EVENT));
                LabelStyling.Place(eventIcon.rectTransform, PADDING_X, PADDING_Y + (bodyHeight - ICON_SIZE * scale) * 0.5f, ICON_SIZE * scale, ICON_SIZE * scale);
            }
            if (hasSpeaker) LabelStyling.Place(speakerText.rectTransform, PADDING_X, PADDING_Y, width - PADDING_X * 2f, speakerHeight);
            LabelStyling.Place(bodyText.rectTransform, PADDING_X + iconWidth, PADDING_Y + speakerHeight, width - PADDING_X * 2f - iconWidth, bodyHeight);
        }

        /// <summary>
        /// Stile del tema con dimensione scalata dall'impostazione.
        /// </summary>
        /// <param name="text">Testo TMP.</param>
        /// <param name="style">Stile del tema.</param>
        /// <param name="color">Colore.</param>
        /// <param name="scale">Fattore di dimensione (impostazione subtitles.size / 100).</param>
        private void Style(TMP_Text text, UITheme.TextStyle style, Color color, float scale)
        {
            LabelStyling.StyleText(text, theme, style.Name, color);
            text.fontSize = style.Size * scale;
            text.lineSpacing = style.LineSpacing;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>
        /// Mostra il Canvas (se nascosto) con dissolvenza e riparte dallo sguardo.
        /// </summary>
        private void Reveal()
        {
            if (shown) return;
            shown = true;
            if (content != null) content.SetActive(true);
            if (follow != null)
            {
                follow.enabled = true;
                follow.SnapToGaze();
            }
            group.alpha = 0f;
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 1f, theme.Motion.FadeDuration));
            if (fadeRoutine == null) group.alpha = 1f;
        }

        /// <summary>
        /// Nasconde il Canvas con dissolvenza (o subito).
        /// </summary>
        /// <param name="immediate">true = subito.</param>
        private void Hide(bool immediate)
        {
            if (!shown) return;
            shown = false;
            if (immediate)
            {
                if (content != null) content.SetActive(false);
                if (follow != null) follow.enabled = false;
                return;
            }
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 0f, theme.Motion.FadeDuration, () =>
            {
                if (shown) return;
                if (content != null) content.SetActive(false);
                if (follow != null) follow.enabled = false;
            }));
            if (fadeRoutine == null)
            {
                if (content != null) content.SetActive(false);
                if (follow != null) follow.enabled = false;
            }
        }

        // =====================================================================
        // Impostazioni
        // =====================================================================

        /// <summary>
        /// true se i sottotitoli sono attivi nelle impostazioni (predefinito true se non c'è lo store).
        /// </summary>
        /// <returns>Stato dell'impostazione.</returns>
        private static bool SubtitlesEnabled()
        {
            return UIServices.Settings == null || UIServices.Settings.GetBool(SettingsCatalog.SUBTITLES_ENABLED);
        }

        /// <summary>
        /// Dimensione dei sottotitoli in percentuale (predefinito 100).
        /// </summary>
        /// <returns>Percentuale.</returns>
        private static float GetSizePercent()
        {
            if (UIServices.Settings == null) return 100f;
            int percent = UIServices.Settings.GetInt(SettingsCatalog.SUBTITLES_SIZE);
            return percent > 0 ? percent : 100f;
        }

        /// <summary>
        /// Rilegge dimensione e opacità se cambiano; spegne i sottotitoli non critici se vengono disattivati.
        /// </summary>
        /// <param name="id">Id dell'impostazione (null/vuoto = qualsiasi).</param>
        private void HandleSettingChanged(string id)
        {
            if (current == null || theme == null) return;
            if (!string.IsNullOrEmpty(id) && !id.StartsWith("subtitles.", StringComparison.Ordinal)) return;
            if (!current.IsEvent && !SubtitlesEnabled()) { Clear(); return; }
            ApplyPage(current.Pages[Mathf.Min(pageIndex, current.Pages.Count - 1)]);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, LazyFollow newFollow, GameObject newContent, CanvasGroup newGroup,
            RectTransform newPanel, RoundedPanelGraphic newBackground, Image newEventIcon, TMP_Text newSpeaker, TMP_Text newBody)
        {
            theme = newTheme; strings = newStrings; follow = newFollow; content = newContent; group = newGroup;
            panel = newPanel; background = newBackground; eventIcon = newEventIcon; speakerText = newSpeaker; bodyText = newBody;
        }
    }
}
