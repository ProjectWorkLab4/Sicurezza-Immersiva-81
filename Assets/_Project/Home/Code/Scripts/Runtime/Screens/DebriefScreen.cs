using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_Debrief (Figma "Debrief"): a sinistra PANEL_Side "Percorso corretto" (gli obiettivi ordinati dello scenario, max 6 passi
    /// numerati), a destra PANEL_Menu con titolo "Debrief · 01 · Fire Starter", livello ed esito su vetro (<see cref="ResultHeader"/>),
    /// la scheda bianca "Decisioni" e la scheda "Errori" e il pulsante "Avanti" verso Score &amp; Replay.
    /// Le righe vengono da <see cref="MissionResult.DebriefLines"/> con i prefissi di <see cref="DebriefLinePrefix"/>:
    /// "+ " = decisione corretta (spunta verde, scheda Decisioni), "- " = errore (croce rossa, scheda Errori),
    /// "! " = errore critico (avviso rosso, "Critico · ...", scheda Errori), nessun prefisso = nota neutra (info, scheda Decisioni).
    /// Altezze calcolate dal testo (nessun Layout Group); le righe che non entrano nel pannello (475) vengono scartate con un avviso.
    /// Uso: istanza di PANEL_Debrief sotto PANEL_Screens (UIScreen id Debrief); la apre MissionFlowDriver a fine missione.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Debrief Screen")]
    public class DebriefScreen : MonoBehaviour
    {
        /// <summary>Id del Blackout: il suo "Percorso corretto" è provvisorio e resta solo in Guided.</summary>
        const string ID_BLACKOUT = "02";

        /// <summary>Tipo di una riga di debrief.</summary>
        private enum LineKind
        {
            /// <summary>Decisione corretta.</summary>
            Good,
            /// <summary>Decisione sbagliata o errore.</summary>
            Bad,
            /// <summary>Errore critico.</summary>
            Critical,
            /// <summary>Nota senza prefisso.</summary>
            Neutral
        }

        /// <summary>Una riga di una scheda: radice, icona e testo.</summary>
        [Serializable]
        public class Row
        {
            /// <summary>Radice della riga.</summary>
            public RectTransform Root;
            /// <summary>Icona (24).</summary>
            public Image Icon;
            /// <summary>Testo a capo.</summary>
            public TMP_Text Text;
        }

        /// <summary>Un passo del percorso corretto: radice, numero e testo.</summary>
        [Serializable]
        public class StepRow
        {
            /// <summary>Radice del passo (riga di PANEL_Side).</summary>
            public RectTransform Root;
            /// <summary>Numero nel cerchio.</summary>
            public TMP_Text Number;
            /// <summary>Testo del passo.</summary>
            public TMP_Text Text;
        }

        [Header("Dati")]
        [SerializeField] private UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Altezza disponibile per le due schede (contenuto − pulsante − spazi), in unità Canvas.")]
        [SerializeField] private float cardsHeight = 315f;
        [Tooltip("Larghezza delle schede (larghezza del contenuto).")]
        [SerializeField] private float cardWidth = 712f;
        [Tooltip("Larghezza del testo dei passi (PANEL_Side meno cerchio e spazio).")]
        [SerializeField] private float stepTextWidth = 234f;

        [Header("Intestazione e pannelli")]
        [SerializeField] private ResultHeader header;
        [SerializeField] private SidePanel side;

        [Header("Percorso corretto")]
        [SerializeField] private StepRow[] steps = new StepRow[6];

        [Header("Schede")]
        [SerializeField] private RectTransform decisionsCard;
        [SerializeField] private RectTransform errorsCard;
        [SerializeField] private Row[] decisionRows = new Row[5];
        [SerializeField] private Row[] errorRows = new Row[4];
        [Tooltip("Icone nell'ordine: corretta, errore, critico, neutra.")]
        [SerializeField] private Sprite[] icons = new Sprite[4];

        [Header("Pulsante")]
        [SerializeField] private UIButton nextButton;

        // Righe di debrief già interpretate (riutilizzate).
        private readonly List<KeyValuePair<LineKind, string>> parsed = new List<KeyValuePair<LineKind, string>>();
        // true dopo il primo avviso di righe in eccesso.
        private bool warnedOverflow;

        /// <summary>
        /// Collega "Avanti".
        /// </summary>
        private void Awake()
        {
            if (nextButton != null) nextButton.Clicked += HandleNext;
        }

        /// <summary>
        /// Scollega "Avanti".
        /// </summary>
        private void OnDestroy()
        {
            if (nextButton != null) nextButton.Clicked -= HandleNext;
        }

        /// <summary>
        /// Alla comparsa riempie la schermata con l'ultimo risultato.
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// Riempie intestazione, percorso corretto e schede dal risultato (ultimo della sessione, o ultimo salvato se manca).
        /// </summary>
        public void Refresh()
        {
            MissionResult result = ResolveResult(out MissionDefinition mission);
            if (header != null) header.Show(result, mission);
            RefreshSteps(mission, result.Level);
            RefreshCards(result);
        }

        /// <summary>
        /// Risultato da mostrare: quello della sessione, altrimenti l'ultimo dei progressi, altrimenti uno vuoto (prove in sandbox).
        /// </summary>
        /// <param name="mission">Scenario corrispondente (può essere null).</param>
        /// <returns>Un risultato mai null.</returns>
        internal static MissionResult ResolveResult(out MissionDefinition mission)
        {
            MissionResult result = MissionSession.LastResult;
            if (result == null && UIServices.Progress != null && UIServices.Progress.AllResults.Count > 0)
                result = UIServices.Progress.AllResults[UIServices.Progress.AllResults.Count - 1];
            if (result == null) result = new MissionResult();
            mission = MissionSession.Current;
            if (mission == null && UIServices.Catalog != null && !string.IsNullOrEmpty(result.MissionId)) mission = UIServices.Catalog.GetById(result.MissionId);
            return result;
        }

        /// <summary>
        /// "Percorso corretto": gli obiettivi ordinati dello scenario, ognuno con la sua altezza dal testo; poi ricalcola PANEL_Side.
        /// Per Blackout l'elenco è provvisorio: solo in Guided, negli altri livelli si nasconde l'intero PANEL_Side.
        /// </summary>
        private void RefreshSteps(MissionDefinition mission, DifficultyLevel level)
        {
            bool mostraPercorso = mission == null || mission.Id != ID_BLACKOUT || level == DifficultyLevel.Guided;
            if (side != null) PanelLayout.SetActive(side.gameObject, mostraPercorso);

            for (int i = 0; i < steps.Length; i++)
            {
                StepRow step = steps[i];
                if (step == null || step.Root == null) continue;
                bool visible = mostraPercorso && mission != null && mission.Objectives != null && i < mission.Objectives.Count && !string.IsNullOrEmpty(mission.Objectives[i]);
                PanelLayout.SetActive(step.Root.gameObject, visible);
                if (!visible) continue;
                SetText(step.Number, (i + 1).ToString());
                SetText(step.Text, mission.Objectives[i]);
                float textHeight = Mathf.Ceil(step.Text.GetPreferredValues(mission.Objectives[i], stepTextWidth, 0f).y);
                step.Root.sizeDelta = new Vector2(step.Root.sizeDelta.x, Mathf.Max(26f, textHeight));
            }
            if (side != null && mostraPercorso) side.RefreshLayout();
        }

        /// <summary>
        /// Schede Decisioni ed Errori: interpreta i prefissi, riempie le righe che entrano e adatta l'altezza delle schede.
        /// </summary>
        private void RefreshCards(MissionResult result)
        {
            parsed.Clear();
            if (result.DebriefLines != null)
                foreach (string line in result.DebriefLines)
                    if (!string.IsNullOrWhiteSpace(line)) parsed.Add(Parse(line));

            float used = 0f;
            float gap = theme != null ? theme.GetNumber(NumberTokens.SPACE_M) : 12f;
            bool overflow = false;
            float decisionsBottom = FillCard(decisionsCard, decisionRows, false, ref overflow, cardsHeight, ref used, 0f);
            float errorsTop = decisionsBottom > 0f ? decisionsBottom + gap : 0f;
            used = errorsTop;
            FillCard(errorsCard, errorRows, true, ref overflow, cardsHeight, ref used, errorsTop);
            if (overflow && !warnedOverflow)
            {
                warnedOverflow = true;
                Debug.LogWarning("[SI81 UI] PANEL_Debrief: troppe righe di debrief per il pannello, quelle in eccesso non vengono mostrate.", this);
            }
        }

        /// <summary>
        /// Riempie una scheda con le righe del suo tipo e la posiziona; nasconde la scheda se non ha righe.
        /// </summary>
        /// <param name="card">Scheda (PANEL_Decisions o PANEL_Errors).</param>
        /// <param name="rows">Pool di righe.</param>
        /// <param name="errors">true per la scheda Errori (righe Bad e Critical), false per Decisioni (Good e Neutral).</param>
        /// <param name="overflow">Diventa true se qualche riga non entra.</param>
        /// <param name="budget">Altezza totale disponibile.</param>
        /// <param name="used">Altezza già occupata sopra la scheda.</param>
        /// <param name="top">Distanza dal bordo superiore del contenuto.</param>
        /// <returns>Bordo inferiore della scheda (0 se nascosta).</returns>
        private float FillCard(RectTransform card, Row[] rows, bool errors, ref bool overflow, float budget, ref float used, float top)
        {
            if (card == null) return 0f;
            float padding = 12f;
            float rowGap = 6f;
            float titleBlock = padding + PanelLayout.LineHeight(theme != null ? theme.Subtitle : null) + rowGap;
            float textWidth = cardWidth - 2f * padding - 32f;
            float y = titleBlock;
            int shown = 0;
            foreach (KeyValuePair<LineKind, string> entry in parsed)
            {
                bool isError = entry.Key == LineKind.Bad || entry.Key == LineKind.Critical;
                if (isError != errors) continue;
                if (shown >= rows.Length) { overflow = true; continue; }
                Row row = rows[shown];
                string text = entry.Key == LineKind.Critical && strings != null ? strings.Format("ui.debrief.critical_prefix", entry.Value) : entry.Value;
                float textHeight = Mathf.Ceil(row.Text.GetPreferredValues(text, textWidth, 0f).y);
                float rowHeight = Mathf.Max(24f, textHeight);
                // Non entra nel pannello: scartata (e le successive di questa scheda).
                if (top + y + rowHeight + padding > budget) { overflow = true; break; }
                SetText(row.Text, text);
                row.Icon.sprite = icons[(int)entry.Key];
                row.Icon.color = IconColor(entry.Key);
                row.Root.gameObject.SetActive(true);
                PanelLayout.SetRect(row.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(padding, -y - rowHeight), new Vector2(cardWidth - padding, -y));
                y += rowHeight + rowGap;
                shown++;
            }
            for (int i = shown; i < rows.Length; i++) rows[i].Root.gameObject.SetActive(false);
            PanelLayout.SetActive(card.gameObject, shown > 0);
            if (shown == 0) return 0f;
            float height = y - rowGap + padding;
            PanelLayout.SetRect(card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -top - height), new Vector2(cardWidth, -top));
            used = top + height;
            return top + height;
        }

        /// <summary>
        /// Interpreta il prefisso di una riga (vedi <see cref="DebriefLinePrefix"/>).
        /// </summary>
        private static KeyValuePair<LineKind, string> Parse(string line)
        {
            if (line.StartsWith(DebriefLinePrefix.GOOD, StringComparison.Ordinal)) return new KeyValuePair<LineKind, string>(LineKind.Good, line.Substring(DebriefLinePrefix.GOOD.Length));
            if (line.StartsWith(DebriefLinePrefix.BAD, StringComparison.Ordinal)) return new KeyValuePair<LineKind, string>(LineKind.Bad, line.Substring(DebriefLinePrefix.BAD.Length));
            if (line.StartsWith(DebriefLinePrefix.CRITICAL, StringComparison.Ordinal)) return new KeyValuePair<LineKind, string>(LineKind.Critical, line.Substring(DebriefLinePrefix.CRITICAL.Length));
            return new KeyValuePair<LineKind, string>(LineKind.Neutral, line);
        }

        /// <summary>
        /// Colore dell'icona: confirm per corretta, deny per errori, brand.dark per la nota neutra.
        /// </summary>
        private Color IconColor(LineKind kind)
        {
            if (theme == null) return Color.white;
            switch (kind)
            {
                case LineKind.Good: return theme.Confirm;
                case LineKind.Bad:
                case LineKind.Critical: return theme.Deny;
                default: return theme.BrandDark;
            }
        }

        /// <summary>
        /// Scrive un testo solo se è cambiato.
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value) target.text = value;
        }

        /// <summary>
        /// "Avanti": apre Score &amp; Replay.
        /// </summary>
        private void HandleNext()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(UIScreenId.ScoreReplay);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, float newCardsHeight, float newCardWidth, float newStepTextWidth, ResultHeader newHeader,
            SidePanel newSide, StepRow[] newSteps, RectTransform newDecisionsCard, RectTransform newErrorsCard, Row[] newDecisionRows, Row[] newErrorRows,
            Sprite[] newIcons, UIButton newNext)
        {
            theme = newTheme;
            strings = newStrings;
            cardsHeight = newCardsHeight;
            cardWidth = newCardWidth;
            stepTextWidth = newStepTextWidth;
            header = newHeader;
            side = newSide;
            steps = newSteps;
            decisionsCard = newDecisionsCard;
            errorsCard = newErrorsCard;
            decisionRows = newDecisionRows;
            errorRows = newErrorRows;
            icons = newIcons;
            nextButton = newNext;
        }
    }
}
