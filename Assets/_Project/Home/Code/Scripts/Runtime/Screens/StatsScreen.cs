using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Logica di PANEL_Stats (Figma "Statistiche A/B"): a sinistra l'elenco degli scenari giocabili (una BTN_ListRow per scenario del
    /// catalogo), a destra la scheda bianca con il RIEPILOGO (4 PANEL_StatTile + riga della media) oppure, se una riga è selezionata,
    /// il DETTAGLIO dello scenario (miglior "SPI N/100" + stelle, ultimo tentativo con esito e le cinque
    /// LBL_DimensionBar, numero di tentativi e livello massimo). Il riepilogo e il dettaglio si alternano: BTN_Back nella scheda
    /// torna al riepilogo. La selezione è tenuta in <see cref="UINavigationContext.StatsSelectedIndex"/> (-1 = riepilogo).
    /// I dati vengono da <see cref="UIServices.Catalog"/> e <see cref="UIServices.Progress"/> (mock o PlayerPrefs); la schermata si
    /// aggiorna quando compare (OnEnable / UIScreen.OnShown) e a ogni <see cref="IProgressService.Changed"/>.
    /// Il punteggio è sempre ui.format.safety_score ("SPI N/100"). Nessun Layout Group: le righe sono posizionate da codice.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Stats Screen")]
    public class StatsScreen : MonoBehaviour
    {
        /// <summary>Numero di dimensioni del punteggio sicurezza (brief p. 3).</summary>
        private const int DIMENSION_COUNT = 5;

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme (misure della riga: size.list-row.*).")]
        [SerializeField] private UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;

        [Header("Elenco scenari")]
        [Tooltip("Prefab BTN_ListRow, istanziato una volta per scenario giocabile del catalogo.")]
        [SerializeField] private ListRowButton rowPrefab;
        [Tooltip("Contenitore delle righe (PANEL_List, 440 × altezza contenuti).")]
        [SerializeField] private RectTransform listRoot;
        [Tooltip("Spazio verticale tra due righe (Figma: 6). Se le righe non ci stanno si riduce, fino a 0.")]
        [SerializeField] private float rowGap = 6f;

        [Header("Riepilogo (A)")]
        [Tooltip("Gruppo del riepilogo (titolo, tessere, media).")]
        [SerializeField] private GameObject summaryGroup;
        [Tooltip("Le 4 tessere nell'ordine Positive, Partial, Negative, Neutral.")]
        [SerializeField] private StatTile[] tiles = new StatTile[4];
        [Tooltip("Riga della media: \"SPI N/100 di media · X scenari giocati su Y\".")]
        [SerializeField] private TMP_Text averageText;

        [Header("Dettaglio scenario (B)")]
        [Tooltip("Gruppo del dettaglio (si mostra quando una riga è selezionata).")]
        [SerializeField] private GameObject missionGroup;
        [Tooltip("BTN_Back nella scheda: torna al riepilogo.")]
        [SerializeField] private UIButton detailBack;
        [Tooltip("Titolo dello scenario selezionato (\"01 · Fire Starter\").")]
        [SerializeField] private TMP_Text missionTitle;
        [Tooltip("Scritta \"Non giocato\" per uno scenario senza risultati.")]
        [SerializeField] private GameObject notPlayedGroup;
        [Tooltip("Gruppo del miglior risultato (punteggio + stelle).")]
        [SerializeField] private GameObject bestGroup;
        [Tooltip("Testo \"SPI N/100\" del miglior risultato.")]
        [SerializeField] private TMP_Text bestScoreText;
        [Tooltip("Stelle del miglior risultato.")]
        [SerializeField] private StarRating bestStars;
        [Tooltip("Gruppo dell'ultimo tentativo (esito, dimensioni, tentativi, livello massimo).")]
        [SerializeField] private GameObject lastAttemptGroup;
        [Tooltip("Esito dell'ultimo tentativo (LBL_Outcome su card bianca).")]
        [SerializeField] private OutcomeLabel lastOutcome;
        [Tooltip("Le 5 barre delle dimensioni, nell'ordine di DimensionKind.")]
        [SerializeField] private DimensionBar[] dimensionBars = new DimensionBar[DIMENSION_COUNT];
        [Tooltip("Testo \"N tentativi · livello max:\".")]
        [SerializeField] private TMP_Text attemptsText;
        [Tooltip("Badge del livello massimo giocato.")]
        [SerializeField] private LevelBadge maxLevelBadge;

        // Righe già istanziate (pool: quelle in più vengono disattivate, non distrutte).
        private readonly List<ListRowButton> rows = new List<ListRowButton>();
        // Scenari giocabili del catalogo, nello stesso ordine delle righe.
        private readonly List<MissionDefinition> missions = new List<MissionDefinition>();
        // Indice selezionato (-1 = riepilogo).
        private int selected = -1;
        // Servizio a cui siamo iscritti a Changed (per annullare l'iscrizione).
        private IProgressService subscribedProgress;
        // true se siamo in attesa di UIServices.Ready.
        private bool waitingForServices;
        // true dopo il primo avviso di righe che non entrano nell'altezza.
        private bool warnedOverflow;

        /// <summary>
        /// Alla comparsa: se i servizi sono pronti si iscrive agli aggiornamenti e ridisegna, altrimenti attende UIServices.Ready.
        /// </summary>
        private void OnEnable()
        {
            if (detailBack != null) detailBack.Clicked += ShowSummary;
            if (!UIServices.IsReady)
            {
                waitingForServices = true;
                UIServices.Ready += HandleServicesReady;
                return;
            }
            Subscribe();
            Refresh();
        }

        /// <summary>
        /// Alla scomparsa rilascia tutte le iscrizioni.
        /// </summary>
        private void OnDisable()
        {
            if (detailBack != null) detailBack.Clicked -= ShowSummary;
            if (waitingForServices)
            {
                UIServices.Ready -= HandleServicesReady;
                waitingForServices = false;
            }
            Unsubscribe();
        }

        /// <summary>
        /// I servizi sono diventati pronti mentre la schermata era già attiva: iscrizione e primo disegno.
        /// </summary>
        private void HandleServicesReady()
        {
            UIServices.Ready -= HandleServicesReady;
            waitingForServices = false;
            Subscribe();
            Refresh();
        }

        /// <summary>
        /// Si iscrive a <see cref="IProgressService.Changed"/> (una sola volta).
        /// </summary>
        private void Subscribe()
        {
            if (subscribedProgress == UIServices.Progress || UIServices.Progress == null) return;
            Unsubscribe();
            subscribedProgress = UIServices.Progress;
            subscribedProgress.Changed += Refresh;
        }

        /// <summary>
        /// Annulla l'iscrizione a <see cref="IProgressService.Changed"/>.
        /// </summary>
        private void Unsubscribe()
        {
            if (subscribedProgress == null) return;
            subscribedProgress.Changed -= Refresh;
            subscribedProgress = null;
        }

        /// <summary>
        /// Rilegge catalogo e progressi e ridisegna elenco, riepilogo e dettaglio. Pubblico: usabile anche come evento di
        /// UIScreen.OnShown (collegato dal builder).
        /// </summary>
        public void Refresh()
        {
            if (!UIServices.IsReady || UIServices.Catalog == null || UIServices.Progress == null) return;
            missions.Clear();
            missions.AddRange(UIServices.Catalog.Playable);

            // La selezione viene dal contesto di navigazione (sopravvive al cambio schermata); fuori scala = riepilogo.
            selected = UINavigationContext.StatsSelectedIndex;
            if (selected < 0 || selected >= missions.Count) selected = -1;

            BuildRows();
            RefreshSummary();
            ApplySelection();
        }

        /// <summary>
        /// Crea (se mancano) le righe dell'elenco, le posiziona e scrive i dati di ogni scenario.
        /// </summary>
        private void BuildRows()
        {
            if (rowPrefab == null || listRoot == null) return;
            while (rows.Count < missions.Count) rows.Add(CreateRow(rows.Count));

            float rowWidth = theme != null ? theme.GetNumber(ButtonTokens.ROW_WIDTH) : 440f;
            float rowHeight = theme != null ? theme.GetNumber(ButtonTokens.ROW_HEIGHT) : 40f;
            float gap = rowGap;
            int count = missions.Count;
            float available = listRoot.rect.height;
            // Se non entrano si stringe lo spazio tra le righe; oltre non si può (scorrimento non progettato: punto aperto).
            if (available > 0f && count > 1 && count * rowHeight + (count - 1) * gap > available)
            {
                gap = Mathf.Max(0f, (available - count * rowHeight) / (count - 1));
                if (count * rowHeight > available && !warnedOverflow)
                {
                    warnedOverflow = true;
                    Debug.LogWarning($"[SI81 UI] PANEL_Stats: {count} scenari non entrano nell'elenco (altezza {available}). Lo scorrimento non è progettato.");
                }
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ListRowButton row = rows[i];
                bool active = i < count;
                if (row.gameObject.activeSelf != active) row.gameObject.SetActive(active);
                if (!active) continue;
                PanelLayout.PlaceTopLeftCentered((RectTransform)row.transform, 0f, i * (rowHeight + gap), rowWidth, rowHeight);
                WriteRow(row, missions[i]);
            }
        }

        /// <summary>
        /// Istanzia una riga e collega il suo clic alla selezione dell'indice dato.
        /// </summary>
        /// <param name="index">Indice della riga (fisso per tutta la vita della riga).</param>
        /// <returns>La riga creata.</returns>
        private ListRowButton CreateRow(int index)
        {
            ListRowButton row = Instantiate(rowPrefab, listRoot);
            row.name = "BTN_ListRow" + index.ToString("00");
            row.Clicked += () => Select(index);
            return row;
        }

        /// <summary>
        /// Scrive titolo, stelle e punteggio di una riga: "Non giocato" se lo scenario non ha ancora un risultato.
        /// </summary>
        /// <param name="row">Riga da aggiornare.</param>
        /// <param name="mission">Scenario della riga.</param>
        private void WriteRow(ListRowButton row, MissionDefinition mission)
        {
            string title = ScenarioTitle(mission);
            MissionBest best = UIServices.Progress.GetBest(mission.Id);
            if (best != null && best.BestStars.HasValue && best.BestScore.HasValue)
                row.SetData(title, best.BestStars.Value, best.BestScore.Value);
            else
                row.SetNotPlayed(title);
        }

        /// <summary>
        /// Titolo composto "01 · Fire Starter" (ui.format.scenario_title).
        /// </summary>
        /// <param name="mission">Scenario.</param>
        /// <returns>Testo del titolo.</returns>
        private string ScenarioTitle(MissionDefinition mission)
        {
            return strings != null ? strings.Format("ui.format.scenario_title", mission.Id, mission.Title) : mission.Title;
        }

        /// <summary>
        /// Selezione di una riga: salva l'indice nel contesto e mostra il dettaglio.
        /// </summary>
        /// <param name="index">Indice dello scenario.</param>
        private void Select(int index)
        {
            if (index == selected) return;
            selected = index;
            UINavigationContext.StatsSelectedIndex = index;
            ApplySelection();
        }

        /// <summary>
        /// BTN_Back nella scheda: torna al riepilogo (nessuna riga selezionata).
        /// </summary>
        public void ShowSummary()
        {
            selected = -1;
            UINavigationContext.StatsSelectedIndex = -1;
            ApplySelection();
        }

        /// <summary>
        /// Evidenzia la riga selezionata e alterna riepilogo / dettaglio.
        /// </summary>
        private void ApplySelection()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].Selected = i == selected;
            bool showDetail = selected >= 0 && selected < missions.Count;
            if (summaryGroup != null) PanelLayout.SetActive(summaryGroup, !showDetail);
            if (missionGroup != null) PanelLayout.SetActive(missionGroup, showDetail);
            if (showDetail) RefreshDetail(missions[selected]);
        }

        /// <summary>
        /// Riepilogo (A): conteggi per esito, sessioni e riga della media.
        /// </summary>
        private void RefreshSummary()
        {
            ProgressTotals totals = UIServices.Progress.GetTotals(UIServices.Catalog);
            if (totals == null) return;
            SetTile(0, StatAccent.Positive, totals.Positive);
            SetTile(1, StatAccent.Partial, totals.Partial);
            SetTile(2, StatAccent.Negative, totals.Negative);
            SetTile(3, StatAccent.Neutral, totals.Sessions);
            if (averageText != null && strings != null)
            {
                string score = strings.Format("ui.format.safety_score", totals.AverageScore);
                // Singolare/plurale: "1 scenario giocato" ha una chiave propria.
                averageText.text = totals.ScenariosPlayed == 1
                    ? strings.Format("ui.stats.average_line_one", score, totals.ScenariosTotal)
                    : strings.Format("ui.stats.average_line", score, totals.ScenariosPlayed, totals.ScenariosTotal);
            }
        }

        /// <summary>
        /// Scrive una tessera del riepilogo.
        /// </summary>
        /// <param name="index">Posizione nell'array delle tessere.</param>
        /// <param name="accent">Accento della tessera.</param>
        /// <param name="value">Conteggio.</param>
        private void SetTile(int index, StatAccent accent, int value)
        {
            if (tiles != null && index < tiles.Length && tiles[index] != null) tiles[index].SetStat(accent, value);
        }

        /// <summary>
        /// Dettaglio (B) dello scenario: miglior risultato e ultimo tentativo. Senza risultati mostra solo "Non giocato".
        /// </summary>
        /// <param name="mission">Scenario selezionato.</param>
        private void RefreshDetail(MissionDefinition mission)
        {
            if (missionTitle != null) missionTitle.text = ScenarioTitle(mission);

            MissionBest best = UIServices.Progress.GetBest(mission.Id);
            bool played = best != null && best.BestStars.HasValue && best.BestScore.HasValue;
            if (notPlayedGroup != null) PanelLayout.SetActive(notPlayedGroup, !played);
            if (bestGroup != null) PanelLayout.SetActive(bestGroup, played);
            if (played)
            {
                if (bestScoreText != null && strings != null) bestScoreText.text = strings.Format("ui.format.safety_score", best.BestScore.Value);
                if (bestStars != null) bestStars.SetStars(best.BestStars.Value);
            }

            // Ultimo tentativo = risultato con il timestamp più recente.
            IReadOnlyList<MissionResult> results = UIServices.Progress.GetResults(mission.Id);
            MissionResult last = null;
            if (results != null)
                for (int i = 0; i < results.Count; i++)
                    if (last == null || results[i].TimestampUtc >= last.TimestampUtc) last = results[i];
            bool hasLast = played && last != null;
            if (lastAttemptGroup != null) PanelLayout.SetActive(lastAttemptGroup, hasLast);
            if (!hasLast) return;

            if (lastOutcome != null) lastOutcome.SetOutcome(last.Outcome);
            for (int i = 0; i < dimensionBars.Length; i++)
            {
                if (dimensionBars[i] == null) continue;
                int value = last.Dimensions != null && i < last.Dimensions.Length ? last.Dimensions[i] : 0;
                dimensionBars[i].SetDimension((DimensionKind)i, value);
            }
            if (attemptsText != null && strings != null) attemptsText.text = results.Count == 1
                    ? strings.Get("ui.stats.attempts_max_level_one")
                    : strings.Format("ui.stats.attempts_max_level", results.Count);
            if (maxLevelBadge != null) maxLevelBadge.SetLevel(MaxLevel(results));
        }

        /// <summary>
        /// Livello più alto tra i tentativi.
        /// </summary>
        /// <param name="results">Risultati dello scenario (non vuoto).</param>
        /// <returns>Il livello massimo giocato.</returns>
        private static DifficultyLevel MaxLevel(IReadOnlyList<MissionResult> results)
        {
            DifficultyLevel max = DifficultyLevel.Guided;
            for (int i = 0; i < results.Count; i++)
                if ((int)results[i].Level > (int)max) max = results[i].Level;
            return max;
        }

        /// <summary>
        /// Imposta tutti i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, ListRowButton newRowPrefab, RectTransform newListRoot,
            GameObject newSummaryGroup, StatTile[] newTiles, TMP_Text newAverageText, GameObject newMissionGroup, UIButton newDetailBack,
            TMP_Text newMissionTitle, GameObject newNotPlayedGroup, GameObject newBestGroup, TMP_Text newBestScoreText,
            StarRating newBestStars, GameObject newLastAttemptGroup, OutcomeLabel newLastOutcome, DimensionBar[] newBars,
            TMP_Text newAttemptsText, LevelBadge newMaxLevelBadge)
        {
            theme = newTheme;
            strings = newStrings;
            rowPrefab = newRowPrefab;
            listRoot = newListRoot;
            summaryGroup = newSummaryGroup;
            tiles = newTiles;
            averageText = newAverageText;
            missionGroup = newMissionGroup;
            detailBack = newDetailBack;
            missionTitle = newMissionTitle;
            notPlayedGroup = newNotPlayedGroup;
            bestGroup = newBestGroup;
            bestScoreText = newBestScoreText;
            bestStars = newBestStars;
            lastAttemptGroup = newLastAttemptGroup;
            lastOutcome = newLastOutcome;
            dimensionBars = newBars;
            attemptsText = newAttemptsText;
            maxLevelBadge = newMaxLevelBadge;
        }
    }
}
