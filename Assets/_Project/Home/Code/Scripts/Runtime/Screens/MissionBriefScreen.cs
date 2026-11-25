using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Schermata PANEL_MissionBrief: scheda dello scenario scelto in Home (<see cref="UINavigationContext.SelectedMission"/>).
    /// Intestazione di PANEL_Menu (BTN_Back, percorso, titolo "NN · Titolo", complessità, pillola "Miglior risultato" con le
    /// sole stelle), scheda bianca con contesto e obiettivi, tre BTN_DifficultyCard (Guided, Operational, Crisis) con la
    /// spunta sul livello scelto, BTN_Primary "Avvia scenario" e PANEL_Side "Criteri di completamento" con i criteri dello scenario.
    /// Niente durata, niente punteggio (CLAUDE.md 4.7). Il livello preselezionato è <see cref="UINavigationContext.SelectedLevel"/>
    /// (ultimo giocato, altrimenti Guided, deciso da Home): se manca, nessuna card è selezionata e il pulsante resta disabilitato
    /// con "Scegli un livello" (stato A di Figma). Un livello con <see cref="MissionLevel.UnlockedByDefault"/> false usa la card bloccata.
    /// Avvio: <see cref="IMissionLauncher.Launch"/>. Tutti i contenuti dello scenario (titolo, contesto, obiettivi, criteri) sono
    /// dati di SO_Mission_*, non chiavi di SO_UIStrings_It. Si aggiorna quando la schermata compare e quando cambiano i progressi.
    /// Uso: istanziare il prefab sotto PANEL_Screens di MNG_UI; Home imposta scenario e livello prima di mostrarla.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIScreen))]
    [AddComponentMenu("SI81/UI/Mission Brief Screen")]
    public class MissionBriefScreen : MonoBehaviour
    {
        /// <summary>Numero massimo di obiettivi mostrati (brief: 4).</summary>
        public const int MAX_OBJECTIVES = 4;
        /// <summary>Numero di righe dei criteri (una per esito: Positivo, Parziale, Negativo).</summary>
        public const int CRITERIA_ROWS = 3;
        /// <summary>Chiave dell'obiettivo numerato ("{0}. {1}").</summary>
        public const string OBJECTIVE_KEY = "ui.brief.objective_item";
        /// <summary>Chiave del testo di un criterio ("{0}: {1}").</summary>
        public const string CRITERION_KEY = "ui.brief.criterion";

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private UIScreen screen;
        [Tooltip("PANEL_Menu della schermata (titolo scenario e posto a destra).")]
        [SerializeField] private MenuPanel menuPanel;
        [Tooltip("PANEL_Side con i criteri di completamento.")]
        [SerializeField] private SidePanel sidePanel;

        [Header("Intestazione")]
        [Tooltip("Testo del percorso (serve a mettere la complessità alla sua destra).")]
        [SerializeField] private TMP_Text breadcrumbText;
        [SerializeField] private RectTransform complexityRoot;
        [SerializeField] private CardDots complexityDots;
        [SerializeField] private TMP_Text complexityLabel;
        [SerializeField] private RectTransform bestPill;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private StarRating bestStars;

        [Header("Descrizione")]
        [SerializeField] private TMP_Text contextText;
        [Tooltip("Gli obiettivi (4, disposti su due colonne: 1 e 3 a sinistra, 2 e 4 a destra).")]
        [SerializeField] private TMP_Text[] objectiveTexts = new TMP_Text[MAX_OBJECTIVES];
        [Tooltip("Larghezza di una colonna di obiettivi: oltre, il testo si accorcia con \"…\".")]
        [SerializeField] private float objectiveWidth = 336f;

        [Header("Livelli e avvio")]
        [Tooltip("Le tre card nell'ordine Guided, Operational, Crisis.")]
        [SerializeField] private DifficultyCard[] levelCards = new DifficultyCard[3];
        [SerializeField] private UIButton startButton;

        [Header("Criteri di completamento")]
        [Tooltip("Una riga per esito, nell'ordine di OutcomeType (Positivo, Parziale, Negativo).")]
        [SerializeField] private RectTransform[] criterionRows = new RectTransform[CRITERIA_ROWS];
        [SerializeField] private TMP_Text[] criterionTexts = new TMP_Text[CRITERIA_ROWS];
        [Tooltip("Larghezza del testo di un criterio (riga 272 - icona 24 - gap 6).")]
        [SerializeField] private float criterionWidth = 242f;

        // Scenario mostrato.
        private MissionDefinition mission;
        // Servizio dei progressi a cui siamo iscritti (per togliere l'iscrizione).
        private IProgressService subscribedProgress;

        /// <summary>Scenario mostrato (null prima del primo aggiornamento).</summary>
        public MissionDefinition Mission => mission;

        /// <summary>
        /// Si iscrive a clic, comparsa, servizi e progressi, poi aggiorna la scheda.
        /// </summary>
        private void OnEnable()
        {
            if (screen == null) screen = GetComponent<UIScreen>();
            if (screen != null) screen.OnShown.AddListener(Refresh);
            if (startButton != null) startButton.Clicked += OnStartClicked;
            if (levelCards != null)
                foreach (DifficultyCard card in levelCards)
                    if (card != null) card.Clicked += OnLevelClicked;
            UIServices.Ready += Refresh;
            Refresh();
        }

        /// <summary>
        /// Toglie tutte le iscrizioni.
        /// </summary>
        private void OnDisable()
        {
            if (screen != null) screen.OnShown.RemoveListener(Refresh);
            if (startButton != null) startButton.Clicked -= OnStartClicked;
            if (levelCards != null)
                foreach (DifficultyCard card in levelCards)
                    if (card != null) card.Clicked -= OnLevelClicked;
            UIServices.Ready -= Refresh;
            if (subscribedProgress != null) subscribedProgress.Changed -= Refresh;
            subscribedProgress = null;
        }

        /// <summary>
        /// Riscrive tutta la scheda dallo scenario scelto. Senza scenario scelto (es. schermata aperta dalla sandbox) usa il primo
        /// giocabile del catalogo; senza catalogo non fa nulla.
        /// </summary>
        public void Refresh()
        {
            SubscribeProgress();
            MissionCatalog catalog = UIServices.Catalog;
            if (UINavigationContext.SelectedMission == null && catalog != null) SelectFallbackMission(catalog);
            mission = UINavigationContext.SelectedMission;
            if (mission == null || strings == null || theme == null) return;

            IProgressService progress = UIServices.Progress;
            MissionBest best = progress != null ? progress.GetBest(mission.Id) : null;

            ApplyHeader(best);
            ApplyDescription();
            ApplyLevels();
            ApplyCriteria();
        }

        /// <summary>
        /// Se nessuno scenario è stato scelto, prende il primo giocabile con il suo ultimo livello (o Guided).
        /// </summary>
        private static void SelectFallbackMission(MissionCatalog catalog)
        {
            foreach (MissionDefinition candidate in catalog.Playable)
            {
                if (candidate == null) continue;
                IProgressService progress = UIServices.Progress;
                MissionBest best = progress != null ? progress.GetBest(candidate.Id) : null;
                UINavigationContext.SelectedMission = candidate;
                UINavigationContext.SelectedLevel = best != null && best.LastPlayedLevel.HasValue ? best.LastPlayedLevel.Value : DifficultyLevel.Guided;
                return;
            }
        }

        /// <summary>
        /// Titolo "NN · Titolo", complessità (5 pallini + "N/5") e pillola "Miglior risultato" con le sole stelle
        /// (nascosta se lo scenario non è mai stato giocato).
        /// </summary>
        private void ApplyHeader(MissionBest best)
        {
            if (menuPanel != null) menuPanel.SetTitleText(strings.Format("ui.format.scenario_title", mission.Id, mission.Title));

            // Complessità: a destra del percorso, sulla stessa riga (colonna del titolo, riga alta quanto il percorso).
            int complexity = Mathf.Clamp(mission.Complexity, 1, CardDots.COUNT);
            if (complexityRoot != null && complexityDots != null)
            {
                Color on = theme.GetColor(ColorTokens.LEVEL_BAR_ON_DARK);
                Color off = theme.GetColor(ColorTokens.LEVEL_BAR_OFF_DARK);
                complexityDots.Set(complexity, on, off);
                if (complexityLabel != null) complexityLabel.text = strings.Format(ScenarioCard.COMPLEXITY_KEY, complexity);
                float breadcrumbWidth = breadcrumbText != null ? Mathf.Ceil(CardTextUtil.MeasureWidth(breadcrumbText, breadcrumbText.text)) : 0f;
                float line = PanelLayout.LineHeight(theme.Body);
                CardTextUtil.PlaceTopLeft(complexityRoot, breadcrumbWidth + theme.GetNumber(NumberTokens.SPACE_L), 0f, complexityRoot.sizeDelta.x, line);
            }

            // Pillola: "Miglior risultato" + stelle (padding 12/6, gap 8, raggio card).
            bool hasBest = best != null && best.BestStars.HasValue;
            if (bestPill != null) PanelLayout.SetActive(bestPill.gameObject, hasBest);
            if (!hasBest || bestLabel == null || bestStars == null) return;

            bestLabel.text = strings.Get("ui.brief.best_result");
            bestStars.SetStars(best.BestStars.Value);
            float padX = theme.GetNumber(NumberTokens.SPACE_M);
            float padY = theme.GetNumber(NumberTokens.SPACE_S) * 0.75f;
            float gap = theme.GetNumber(NumberTokens.SPACE_S);
            var starsRect = (RectTransform)bestStars.transform;
            float starsWidth = starsRect.rect.width;
            float starsHeight = starsRect.rect.height;
            float labelWidth = Mathf.Ceil(CardTextUtil.MeasureWidth(bestLabel, bestLabel.text)) + 1f;
            float height = starsHeight + padY * 2f;
            float labelBox = Mathf.Max(height, bestLabel.preferredHeight);
            CardTextUtil.PlaceTopLeft(bestLabel.rectTransform, padX, (height - labelBox) * 0.5f, labelWidth, labelBox);
            CardTextUtil.PlaceTopLeft(starsRect, padX + labelWidth + gap, padY, starsWidth, starsHeight);
            bestPill.anchorMin = bestPill.anchorMax = new Vector2(1f, 0.5f);
            bestPill.pivot = new Vector2(1f, 0.5f);
            bestPill.anchoredPosition = Vector2.zero;
            bestPill.sizeDelta = new Vector2(Mathf.Ceil(padX * 2f + labelWidth + gap + starsWidth), height);
        }

        /// <summary>
        /// Contesto (max 280 caratteri) e obiettivi numerati su una riga ciascuno (accorciati con "…" se troppo lunghi).
        /// </summary>
        private void ApplyDescription()
        {
            if (contextText != null) contextText.text = mission.Context ?? string.Empty;
            for (int i = 0; i < objectiveTexts.Length; i++)
            {
                TMP_Text text = objectiveTexts[i];
                if (text == null) continue;
                bool used = mission.Objectives != null && i < mission.Objectives.Count && !string.IsNullOrWhiteSpace(mission.Objectives[i]);
                PanelLayout.SetActive(text.gameObject, used);
                if (used) text.text = FitSingleLine(text, strings.Format(OBJECTIVE_KEY, i + 1, mission.Objectives[i].Trim()), objectiveWidth);
            }
        }

        /// <summary>
        /// Le tre card dei livelli (bloccate se il livello non è sbloccato), la selezione corrente e lo stato del pulsante.
        /// Un livello scelto ma bloccato viene deselezionato.
        /// </summary>
        private void ApplyLevels()
        {
            DifficultyLevel? selected = UINavigationContext.SelectedLevel;
            if (selected.HasValue && !mission.GetLevel(selected.Value).UnlockedByDefault)
            {
                selected = null;
                UINavigationContext.SelectedLevel = null;
            }
            foreach (DifficultyCard card in levelCards)
            {
                if (card == null) continue;
                bool locked = !mission.GetLevel(card.Level).UnlockedByDefault;
                card.Set(card.Level, null, locked);
                card.Selected = selected.HasValue && selected.Value == card.Level;
            }
            // Stato A: nessun livello scelto, "Scegli un livello" e pulsante disabilitato.
            if (startButton != null) startButton.Interactable = selected.HasValue;
        }

        /// <summary>
        /// Righe dei criteri di PANEL_Side: icona + "Positivo: testo" (testo dai dati dello scenario). Una riga senza testo si
        /// nasconde; il testo lungo va a capo e la riga cresce; poi il pannello ricalcola altezza e posizione.
        /// </summary>
        private void ApplyCriteria()
        {
            if (sidePanel == null) return;
            float minHeight = theme.GetNumber(NumberTokens.SPACE_XL);
            float line = PanelLayout.LineHeight(theme.Body);
            for (int i = 0; i < CRITERIA_ROWS; i++)
            {
                RectTransform row = criterionRows[i];
                TMP_Text text = criterionTexts[i];
                if (row == null || text == null) continue;
                var outcome = (OutcomeType)i;
                string criterion = mission.GetCriterionText(outcome);
                bool used = !string.IsNullOrWhiteSpace(criterion);
                PanelLayout.SetActive(row.gameObject, used);
                if (!used) continue;

                string shortName = strings.Get(GetShortKey(outcome));
                text.text = strings.Format(CRITERION_KEY, shortName, criterion.Trim());
                int lines = CountLines(text, text.text, criterionWidth);
                row.sizeDelta = new Vector2(row.sizeDelta.x, Mathf.Max(minHeight, lines * line));
                text.alignment = lines > 1 ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            }
            sidePanel.RefreshLayout();
        }

        /// <summary>
        /// Chiave breve dell'esito ("Positivo", "Parziale", "Negativo").
        /// </summary>
        /// <param name="outcome">Esito.</param>
        /// <returns>Chiave di SO_UIStrings_It.</returns>
        private static string GetShortKey(OutcomeType outcome)
        {
            switch (outcome)
            {
                case OutcomeType.Positive: return "ui.outcome.positive_short";
                case OutcomeType.Partial: return "ui.outcome.partial_short";
                default: return "ui.outcome.negative_short";
            }
        }

        /// <summary>
        /// Clic su una card di livello: selezione esclusiva, salvata nel contesto di navigazione; abilita il pulsante.
        /// </summary>
        /// <param name="level">Livello cliccato.</param>
        private void OnLevelClicked(DifficultyLevel level)
        {
            UINavigationContext.SelectedLevel = level;
            foreach (DifficultyCard card in levelCards)
                if (card != null) card.Selected = card.Level == level;
            if (startButton != null) startButton.Interactable = true;
        }

        /// <summary>
        /// Clic su "Avvia scenario": avvia lo scenario con il livello scelto.
        /// </summary>
        private void OnStartClicked()
        {
            DifficultyLevel? level = UINavigationContext.SelectedLevel;
            if (mission == null || !level.HasValue) return;
            if (UIServices.Launcher == null)
            {
                Debug.LogWarning("[SI81 UI] Mission Brief: nessun IMissionLauncher in UIServices, scenario non avviato.", this);
                return;
            }
            UIServices.Launcher.Launch(mission, level.Value);
        }

        /// <summary>
        /// Si iscrive a Changed del servizio dei progressi corrente (le stelle migliori cambiano a fine missione).
        /// </summary>
        private void SubscribeProgress()
        {
            IProgressService current = UIServices.Progress;
            if (current == subscribedProgress) return;
            if (subscribedProgress != null) subscribedProgress.Changed -= Refresh;
            subscribedProgress = current;
            if (subscribedProgress != null) subscribedProgress.Changed += Refresh;
        }

        /// <summary>
        /// Accorcia un testo con "…" finché sta in una riga della larghezza data (misura con lo stile già applicato al TMP).
        /// </summary>
        /// <param name="text">TMP che mostrerà il testo (stile già applicato).</param>
        /// <param name="content">Testo completo.</param>
        /// <param name="maxWidth">Larghezza massima in unità Canvas.</param>
        /// <returns>Testo intero o accorciato.</returns>
        private static string FitSingleLine(TMP_Text text, string content, float maxWidth)
        {
            if (string.IsNullOrEmpty(content) || CardTextUtil.MeasureWidth(text, content) <= maxWidth) return content;
            for (int length = content.Length - 1; length > 0; length--)
            {
                string candidate = content.Substring(0, length).TrimEnd() + "…";
                if (CardTextUtil.MeasureWidth(text, candidate) <= maxWidth) return candidate;
            }
            return "…";
        }

        /// <summary>
        /// Conta le righe che un testo occupa alla larghezza data (altezza preferita / altezza di una riga sola).
        /// </summary>
        /// <param name="text">TMP con lo stile già applicato (a capo attivo).</param>
        /// <param name="content">Testo da misurare.</param>
        /// <param name="width">Larghezza disponibile.</param>
        /// <returns>Numero di righe, almeno 1.</returns>
        private static int CountLines(TMP_Text text, string content, float width)
        {
            float singleLine = text.GetPreferredValues("Ag", width, 0f).y;
            if (singleLine <= 0f) return 1;
            float total = text.GetPreferredValues(content, width, 0f).y;
            return Mathf.Max(1, Mathf.RoundToInt(total / singleLine));
        }

        /// <summary>
        /// Collega i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, UIScreen newScreen, MenuPanel newMenu, SidePanel newSide,
            TMP_Text newBreadcrumb, RectTransform newComplexityRoot, CardDots newDots, TMP_Text newComplexityLabel,
            RectTransform newBestPill, TMP_Text newBestLabel, StarRating newBestStars, TMP_Text newContext, TMP_Text[] newObjectives,
            DifficultyCard[] newLevelCards, UIButton newStart, RectTransform[] newCriterionRows, TMP_Text[] newCriterionTexts)
        {
            theme = newTheme;
            strings = newStrings;
            screen = newScreen;
            menuPanel = newMenu;
            sidePanel = newSide;
            breadcrumbText = newBreadcrumb;
            complexityRoot = newComplexityRoot;
            complexityDots = newDots;
            complexityLabel = newComplexityLabel;
            bestPill = newBestPill;
            bestLabel = newBestLabel;
            bestStars = newBestStars;
            contextText = newContext;
            objectiveTexts = newObjectives;
            levelCards = newLevelCards;
            startButton = newStart;
            criterionRows = newCriterionRows;
            criterionTexts = newCriterionTexts;
        }
    }
}
