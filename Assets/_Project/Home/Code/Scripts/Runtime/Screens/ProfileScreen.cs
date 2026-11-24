using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Logica di PANEL_Profile (Figma "Profilo"): profilo unico (nessun profilo multiplo, nessun account/logout: CLAUDE.md §8).
    /// Colonna sinistra: avatar, nome, "Missioni completate N/M", ultimi 3 risultati e la scheda bianca con il
    /// "SPI N/100" complessivo + stelle (richiesto anche se il Figma non lo mostra).
    /// Colonna destra: le cinque competenze (LBL_DimensionBar Dark, media di tutti i risultati) e la griglia dei badge
    /// (BTN_Badge sbloccato/bloccato, 6 per riga). Un clic su un badge apre una scheda di dettaglio semplice (nome, stato, descrizione:
    /// il dettaglio non è progettato in Figma, punto aperto) che sostituisce la griglia finché non si preme BTN_Back.
    /// Dati da <see cref="UIServices.Catalog"/> e <see cref="UIServices.Progress"/>; si aggiorna a OnEnable / UIScreen.OnShown e a ogni
    /// <see cref="IProgressService.Changed"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Profile Screen")]
    public class ProfileScreen : MonoBehaviour
    {
        /// <summary>Chiave PlayerPrefs del nome del profilo (modificabile in futuro da un'impostazione).</summary>
        public const string PROFILE_NAME_PREFS_KEY = "si81.profile.name";
        /// <summary>Numero di righe "ultimi risultati" (Figma: 3).</summary>
        public const int RECENT_COUNT = 3;
        /// <summary>Numero di dimensioni del punteggio sicurezza.</summary>
        private const int DIMENSION_COUNT = 5;
        /// <summary>Separatore tra stelle e punteggio nella riga dei risultati recenti ("4/5 · 86/100").</summary>
        private const string RESULT_SEPARATOR = " · ";

        /// <summary>Una riga degli ultimi risultati: radice, titolo scenario e valore "N/5 · NN/100".</summary>
        [Serializable]
        public class RecentRow
        {
            /// <summary>Radice della riga (si nasconde se mancano risultati).</summary>
            public GameObject Root;
            /// <summary>Titolo dello scenario (Body on-dark).</summary>
            public TMP_Text Title;
            /// <summary>Stelle e punteggio (Body on-green).</summary>
            public TMP_Text Value;
        }

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme (misura del badge: size.badge).")]
        [SerializeField] private UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Nome mostrato se in PlayerPrefs (\"si81.profile.name\") non ne è salvato uno. Dato provvisorio del Figma, non una chiave di testo.")]
        [SerializeField] private string defaultProfileName = "Operatore_Luca";

        [Header("Identità")]
        [Tooltip("Nome del profilo (Subtitle Bold).")]
        [SerializeField] private TMP_Text nameText;
        [Tooltip("\"Missioni completate N/M\".")]
        [SerializeField] private TMP_Text missionsText;
        [Tooltip("Le 3 righe degli ultimi risultati.")]
        [SerializeField] private RecentRow[] recentRows = new RecentRow[RECENT_COUNT];
        [Tooltip("Scheda bianca: \"SPI N/100\" complessivo.")]
        [SerializeField] private TMP_Text overallScoreText;
        [Tooltip("Stelle del punteggio complessivo (= rank).")]
        [SerializeField] private StarRating overallStars;

        [Header("Competenze")]
        [Tooltip("Le 5 barre nell'ordine di DimensionKind.")]
        [SerializeField] private DimensionBar[] skillBars = new DimensionBar[DIMENSION_COUNT];

        [Header("Badge")]
        [Tooltip("Titolo \"Badge · N su M\".")]
        [SerializeField] private TMP_Text badgesTitleText;
        [Tooltip("Prefab BTN_Badge, istanziato una volta per badge.")]
        [SerializeField] private BadgeButton badgePrefab;
        [Tooltip("Contenitore della griglia dei badge (PANEL_BadgeGrid).")]
        [SerializeField] private RectTransform badgeGrid;
        [Tooltip("Badge per riga (Figma: 6).")]
        [SerializeField] private int badgeColumns = 6;
        [Tooltip("Spazio tra i badge, in orizzontale e in verticale (Figma: 12).")]
        [SerializeField] private float badgeGap = 12f;

        [Header("Dettaglio badge")]
        [Tooltip("Scheda di dettaglio del badge (PANEL_BadgeDetail), nascosta finché non si sceglie un badge.")]
        [SerializeField] private GameObject detailRoot;
        [Tooltip("Nome del badge.")]
        [SerializeField] private TMP_Text detailTitle;
        [Tooltip("Scritta \"Bloccato\" (solo per i badge bloccati).")]
        [SerializeField] private TMP_Text detailState;
        [Tooltip("Descrizione del badge (come si ottiene / che cosa premia).")]
        [SerializeField] private TMP_Text detailDescription;
        [Tooltip("BTN_Back della scheda: la chiude e riporta la griglia.")]
        [SerializeField] private UIButton detailClose;

        // Badge istanziati (pool) e dati attuali, nello stesso ordine.
        private readonly List<BadgeButton> badgeButtons = new List<BadgeButton>();
        private readonly List<BadgeInfo> badges = new List<BadgeInfo>();
        // Risultati ordinati dal più recente (riutilizzato a ogni aggiornamento).
        private readonly List<MissionResult> recent = new List<MissionResult>();
        // Servizio a cui siamo iscritti a Changed.
        private IProgressService subscribedProgress;
        private bool waitingForServices;
        // true dopo il primo avviso di badge che non entrano nelle righe previste.
        private bool warnedOverflow;

        /// <summary>
        /// Alla comparsa: collega la chiusura del dettaglio, poi si iscrive e ridisegna (o attende UIServices.Ready).
        /// </summary>
        private void OnEnable()
        {
            if (detailClose != null) detailClose.Clicked += CloseBadgeDetail;
            ShowDetail(false);
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
        /// Alla scomparsa rilascia le iscrizioni.
        /// </summary>
        private void OnDisable()
        {
            if (detailClose != null) detailClose.Clicked -= CloseBadgeDetail;
            if (waitingForServices)
            {
                UIServices.Ready -= HandleServicesReady;
                waitingForServices = false;
            }
            Unsubscribe();
        }

        /// <summary>
        /// I servizi sono diventati pronti mentre la schermata era già attiva.
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
        /// Rilegge i progressi e ridisegna tutta la schermata. Pubblico: usabile come evento di UIScreen.OnShown.
        /// </summary>
        public void Refresh()
        {
            if (!UIServices.IsReady || UIServices.Catalog == null || UIServices.Progress == null) return;
            if (nameText != null) nameText.text = PlayerPrefs.GetString(PROFILE_NAME_PREFS_KEY, defaultProfileName);
            ProgressTotals totals = UIServices.Progress.GetTotals(UIServices.Catalog);
            RefreshMissions(totals);
            RefreshRecent();
            RefreshOverall(totals);
            RefreshSkills();
            RefreshBadges();
        }

        /// <summary>
        /// "Missioni completate N/M" (scenari giocati su scenari totali, da <see cref="ProgressTotals"/>).
        /// </summary>
        /// <param name="totals">Totali del profilo.</param>
        private void RefreshMissions(ProgressTotals totals)
        {
            if (missionsText == null || strings == null || totals == null) return;
            missionsText.text = strings.Format("ui.profile.missions_completed", totals.ScenariosPlayed, totals.ScenariosTotal);
        }

        /// <summary>
        /// Ultimi 3 risultati (i più recenti per timestamp): "Titolo" a sinistra, "N/5 · NN/100" a destra.
        /// </summary>
        private void RefreshRecent()
        {
            recent.Clear();
            IReadOnlyList<MissionResult> all = UIServices.Progress.AllResults;
            if (all != null) for (int i = 0; i < all.Count; i++) recent.Add(all[i]);
            recent.Sort((a, b) => b.TimestampUtc.CompareTo(a.TimestampUtc));

            for (int i = 0; i < recentRows.Length; i++)
            {
                RecentRow row = recentRows[i];
                if (row == null) continue;
                bool has = i < recent.Count;
                if (row.Root != null) PanelLayout.SetActive(row.Root, has);
                if (!has) continue;
                MissionResult result = recent[i];
                MissionDefinition mission = UIServices.Catalog.GetById(result.MissionId);
                if (row.Title != null && strings != null)
                    row.Title.text = mission != null ? strings.Format("ui.format.scenario_title", mission.Id, mission.Title) : result.MissionId;
                if (row.Value != null && strings != null)
                    row.Value.text = strings.Format("ui.format.stars", result.Stars) + RESULT_SEPARATOR + strings.Format("ui.format.score_of_100", result.SafetyScore);
            }
        }

        /// <summary>
        /// Punteggio sicurezza complessivo (media dei punteggi, ui.format.safety_score) e stelle = media arrotondata delle migliori stelle
        /// degli scenari giocati.
        /// </summary>
        /// <param name="totals">Totali del profilo.</param>
        private void RefreshOverall(ProgressTotals totals)
        {
            if (overallScoreText != null && strings != null && totals != null)
                overallScoreText.text = strings.Format("ui.format.safety_score", totals.AverageScore);
            if (overallStars == null) return;
            int sum = 0;
            int played = 0;
            foreach (MissionDefinition mission in UIServices.Catalog.Playable)
            {
                MissionBest best = UIServices.Progress.GetBest(mission.Id);
                if (best == null || !best.BestStars.HasValue) continue;
                sum += best.BestStars.Value;
                played++;
            }
            overallStars.SetStars(played > 0 ? Mathf.RoundToInt(sum / (float)played) : 0, false);
        }

        /// <summary>
        /// Competenze: media di ciascuna delle cinque dimensioni su tutti i risultati (0 se non ce ne sono).
        /// </summary>
        private void RefreshSkills()
        {
            IReadOnlyList<MissionResult> all = UIServices.Progress.AllResults;
            for (int d = 0; d < skillBars.Length; d++)
            {
                if (skillBars[d] == null) continue;
                int sum = 0;
                int count = 0;
                if (all != null)
                    for (int i = 0; i < all.Count; i++)
                    {
                        int[] dims = all[i].Dimensions;
                        if (dims == null || d >= dims.Length) continue;
                        sum += dims[d];
                        count++;
                    }
                skillBars[d].SetDimension((DimensionKind)d, count > 0 ? Mathf.RoundToInt(sum / (float)count) : 0);
            }
        }

        /// <summary>
        /// Titolo "Badge · N su M" e griglia dei BTN_Badge (sbloccati con icona piena, bloccati con lucchetto).
        /// </summary>
        private void RefreshBadges()
        {
            badges.Clear();
            IReadOnlyList<BadgeInfo> source = UIServices.Progress.GetBadges();
            if (source != null) for (int i = 0; i < source.Count; i++) badges.Add(source[i]);

            int unlocked = 0;
            for (int i = 0; i < badges.Count; i++) if (badges[i].Unlocked) unlocked++;
            if (badgesTitleText != null && strings != null)
                badgesTitleText.text = strings.Format("ui.profile.badges_count", unlocked, badges.Count);

            if (badgePrefab == null || badgeGrid == null) return;
            while (badgeButtons.Count < badges.Count) badgeButtons.Add(CreateBadgeButton(badgeButtons.Count));

            float size = theme != null ? theme.GetNumber(ButtonTokens.BADGE) : 64f;
            int columns = Mathf.Max(1, badgeColumns);
            // Righe previste dal Figma: 2. Oltre l'altezza del contenitore i badge uscirebbero dalla scheda (scorrimento non progettato).
            int rowsNeeded = Mathf.CeilToInt(badges.Count / (float)columns);
            if (!warnedOverflow && rowsNeeded * size + (rowsNeeded - 1) * badgeGap > badgeGrid.rect.height + 0.5f)
            {
                warnedOverflow = true;
                Debug.LogWarning($"[SI81 UI] PANEL_Profile: {badges.Count} badge non entrano in {badgeGrid.rect.height} unità di altezza. Lo scorrimento non è progettato.");
            }

            for (int i = 0; i < badgeButtons.Count; i++)
            {
                BadgeButton button = badgeButtons[i];
                bool active = i < badges.Count;
                if (button.gameObject.activeSelf != active) button.gameObject.SetActive(active);
                if (!active) continue;
                PanelLayout.PlaceTopLeftCentered((RectTransform)button.transform, (i % columns) * (size + badgeGap), (i / columns) * (size + badgeGap), size, size);
                button.SetData(badges[i].Unlocked, badges[i].Icon);
            }
        }

        /// <summary>
        /// Istanzia un BTN_Badge e collega il clic al dettaglio del badge con l'indice dato.
        /// </summary>
        /// <param name="index">Indice del badge (fisso per tutta la vita del pulsante).</param>
        /// <returns>Il pulsante creato.</returns>
        private BadgeButton CreateBadgeButton(int index)
        {
            BadgeButton button = Instantiate(badgePrefab, badgeGrid);
            button.name = "BTN_Badge" + index.ToString("00");
            button.Clicked += () => OpenBadgeDetail(index);
            return button;
        }

        /// <summary>
        /// Apre la scheda di dettaglio del badge: nome, stato (solo se bloccato) e descrizione.
        /// </summary>
        /// <param name="index">Indice del badge nell'elenco.</param>
        private void OpenBadgeDetail(int index)
        {
            if (index < 0 || index >= badges.Count) return;
            BadgeInfo info = badges[index];
            if (detailTitle != null) detailTitle.text = info.Title;
            if (detailDescription != null) detailDescription.text = info.Description;
            if (detailState != null)
            {
                PanelLayout.SetActive(detailState.gameObject, !info.Unlocked);
                if (!info.Unlocked && strings != null) detailState.text = strings.Get("ui.profile.badge_locked");
            }
            ShowDetail(true);
        }

        /// <summary>
        /// Chiude la scheda di dettaglio e mostra di nuovo la griglia.
        /// </summary>
        public void CloseBadgeDetail()
        {
            ShowDetail(false);
        }

        /// <summary>
        /// Alterna griglia dei badge e scheda di dettaglio (mai insieme: il dettaglio occupa lo stesso spazio).
        /// </summary>
        /// <param name="show">true = mostra il dettaglio.</param>
        private void ShowDetail(bool show)
        {
            if (detailRoot != null) PanelLayout.SetActive(detailRoot, show);
            if (badgeGrid != null) PanelLayout.SetActive(badgeGrid.gameObject, !show);
        }

        /// <summary>
        /// Imposta tutti i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, TMP_Text newName, TMP_Text newMissions, RecentRow[] newRecent,
            TMP_Text newOverallScore, StarRating newOverallStars, DimensionBar[] newSkills, TMP_Text newBadgesTitle, BadgeButton newBadgePrefab,
            RectTransform newBadgeGrid, GameObject newDetailRoot, TMP_Text newDetailTitle, TMP_Text newDetailState, TMP_Text newDetailDescription,
            UIButton newDetailClose)
        {
            theme = newTheme;
            strings = newStrings;
            nameText = newName;
            missionsText = newMissions;
            recentRows = newRecent;
            overallScoreText = newOverallScore;
            overallStars = newOverallStars;
            skillBars = newSkills;
            badgesTitleText = newBadgesTitle;
            badgePrefab = newBadgePrefab;
            badgeGrid = newBadgeGrid;
            detailRoot = newDetailRoot;
            detailTitle = newDetailTitle;
            detailState = newDetailState;
            detailDescription = newDetailDescription;
            detailClose = newDetailClose;
        }
    }
}
