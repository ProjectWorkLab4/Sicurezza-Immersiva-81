using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Schermata PANEL_Home ("Scenari"): striscia PANEL_TrainingCard in alto e griglia 4 x 2 di BTN_ScenarioCard con gli scenari
    /// giocabili di SO_MissionCatalog (<see cref="UIServices.Catalog"/>), più le stelle migliori, la complessità e i livelli
    /// completati di <see cref="UIServices.Progress"/>. Mostra SOLO le stelle, mai il punteggio 0–100 (CLAUDE.md 4.6).
    /// Clic su una card: salva scenario e livello in <see cref="UINavigationContext"/> (livello = ultimo giocato, altrimenti
    /// Guided) e apre PANEL_MissionBrief. Clic sul Training: apre PANEL_TrainingBrief.
    /// Le 8 card sono istanze già presenti nel prefab (nessuna istanza a runtime); gli scenari oltre l'ottavo vanno in altre
    /// pagine: due frecce 40 x 40 e "N/M" nel posto a destra dell'intestazione di PANEL_Menu, visibili solo con più di una pagina
    /// (la fine del giro riparte dalla prima). Uno scenario senza scena (o, in una build, non presente nelle Build Settings)
    /// usa la card Disabled ("Non disponibile").
    /// Si aggiorna quando la schermata compare e quando cambia <see cref="IProgressService.Changed"/>.
    /// Uso: istanziare il prefab sotto PANEL_Screens di MNG_UI; nessun parametro da impostare, tutto arriva dai dati.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIScreen))]
    [AddComponentMenu("SI81/UI/Home Screen")]
    public class HomeScreen : MonoBehaviour
    {
        /// <summary>Chiave del numero di pagina ("{0}/{1}").</summary>
        public const string PAGE_KEY = "ui.home.page";

        [Header("Riferimenti (assegnati dal builder)")]
        [Tooltip("Tabella testi SO_UIStrings_It (titolo scenario, numero di pagina).")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Radice della schermata (UIScreen): la sua comparsa riempie le card.")]
        [SerializeField] private UIScreen screen;
        [Tooltip("Striscia del Training (PANEL_TrainingCard).")]
        [SerializeField] private TrainingCard trainingCard;
        [Tooltip("Le card della griglia (8, in ordine di lettura: da sinistra a destra, dall'alto in basso).")]
        [SerializeField] private ScenarioCard[] cards = new ScenarioCard[0];
        [Tooltip("Contenitore delle frecce di pagina (nascosto con una sola pagina).")]
        [SerializeField] private GameObject pager;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [Tooltip("Testo \"N/M\" tra le due frecce.")]
        [SerializeField] private TMP_Text pageLabel;

        // Scenari giocabili in elenco (riempito a ogni aggiornamento, senza allocare di nuovo).
        private readonly List<MissionDefinition> missions = new List<MissionDefinition>();
        // Pagina mostrata (0 = prima).
        private int pageIndex;
        // Servizio dei progressi a cui siamo iscritti (per togliere l'iscrizione).
        private IProgressService subscribedProgress;

        /// <summary>Numero di pagine della griglia (almeno 1).</summary>
        public int PageCount
        {
            get
            {
                int perPage = cards != null ? cards.Length : 0;
                return perPage <= 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(missions.Count / (float)perPage));
            }
        }

        /// <summary>Pagina mostrata (0 = prima).</summary>
        public int PageIndex => pageIndex;

        /// <summary>
        /// Si iscrive a clic, comparsa, servizi e progressi, poi riempie la griglia.
        /// </summary>
        private void OnEnable()
        {
            if (screen == null) screen = GetComponent<UIScreen>();
            if (screen != null) screen.OnShown.AddListener(Refresh);
            if (trainingCard != null) trainingCard.Clicked += OnTrainingClicked;
            if (cards != null)
                foreach (ScenarioCard card in cards)
                    if (card != null) card.Clicked += OnCardClicked;
            if (previousPageButton != null) previousPageButton.onClick.AddListener(ShowPreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(ShowNextPage);
            UIServices.Ready += Refresh;
            Refresh();
        }

        /// <summary>
        /// Toglie tutte le iscrizioni.
        /// </summary>
        private void OnDisable()
        {
            if (screen != null) screen.OnShown.RemoveListener(Refresh);
            if (trainingCard != null) trainingCard.Clicked -= OnTrainingClicked;
            if (cards != null)
                foreach (ScenarioCard card in cards)
                    if (card != null) card.Clicked -= OnCardClicked;
            if (previousPageButton != null) previousPageButton.onClick.RemoveListener(ShowPreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.RemoveListener(ShowNextPage);
            UIServices.Ready -= Refresh;
            UnsubscribeProgress();
        }

        /// <summary>
        /// Riempie le card della pagina corrente con gli scenari del catalogo e i progressi del giocatore, e aggiorna le frecce.
        /// Senza servizi pronti la griglia resta vuota (le card sono nascoste).
        /// </summary>
        public void Refresh()
        {
            SubscribeProgress();
            MissionCatalog catalog = UIServices.Catalog;
            missions.Clear();
            if (catalog != null)
                foreach (MissionDefinition mission in catalog.Playable)
                    if (mission != null) missions.Add(mission);

            int pages = PageCount;
            pageIndex = Mathf.Clamp(pageIndex, 0, pages - 1);

            int perPage = cards != null ? cards.Length : 0;
            for (int i = 0; i < perPage; i++)
            {
                ScenarioCard card = cards[i];
                if (card == null) continue;
                int index = pageIndex * perPage + i;
                bool used = index < missions.Count;
                PanelLayout.SetActive(card.gameObject, used);
                if (used) card.Set(BuildCardData(missions[index]));
            }

            // Frecce: solo con più di una pagina.
            if (pager != null) PanelLayout.SetActive(pager, pages > 1);
            if (pageLabel != null && strings != null) pageLabel.text = strings.Format(PAGE_KEY, pageIndex + 1, pages);
        }

        /// <summary>
        /// Passa alla pagina precedente (dalla prima si torna all'ultima).
        /// </summary>
        public void ShowPreviousPage()
        {
            int pages = PageCount;
            pageIndex = (pageIndex - 1 + pages) % pages;
            Refresh();
        }

        /// <summary>
        /// Passa alla pagina successiva (dall'ultima si torna alla prima).
        /// </summary>
        public void ShowNextPage()
        {
            pageIndex = (pageIndex + 1) % PageCount;
            Refresh();
        }

        /// <summary>
        /// Compone i dati di una card: titolo "NN · Titolo", stelle migliori (null = "Nuovo"), complessità, livelli completati.
        /// Mai il punteggio 0–100.
        /// </summary>
        /// <param name="mission">Scenario da mostrare.</param>
        /// <returns>Dati per <see cref="ScenarioCard.Set"/>.</returns>
        private ScenarioCardData BuildCardData(MissionDefinition mission)
        {
            IProgressService progress = UIServices.Progress;
            MissionBest best = progress != null ? progress.GetBest(mission.Id) : null;
            return new ScenarioCardData
            {
                Id = mission.Id,
                Title = strings != null ? strings.Format("ui.format.scenario_title", mission.Id, mission.Title) : mission.Title,
                Icon = mission.Icon,
                Image = mission.Image,
                GradientStart = mission.GradientTop,
                GradientEnd = mission.GradientBottom,
                BestStars = best != null ? best.BestStars : null,
                Complexity = mission.Complexity,
                LevelsCompleted = best != null ? best.LevelsCompleted : 0,
                Unlocked = IsAvailable(mission)
            };
        }

        /// <summary>
        /// Indica se lo scenario si può avviare: lo decide il lanciatore (scena presente nella build; sempre sì con il lanciatore finto).
        /// </summary>
        /// <param name="mission">Scenario da controllare.</param>
        /// <returns>true se la card è cliccabile.</returns>
        private static bool IsAvailable(MissionDefinition mission)
        {
            return UIServices.Launcher != null && UIServices.Launcher.CanLaunch(mission);
        }

        /// <summary>
        /// Clic su una card: ricorda scenario e livello (ultimo giocato, altrimenti Guided) e apre PANEL_MissionBrief.
        /// </summary>
        /// <param name="missionId">Id dello scenario cliccato.</param>
        private void OnCardClicked(string missionId)
        {
            MissionCatalog catalog = UIServices.Catalog;
            MissionDefinition mission = catalog != null ? catalog.GetById(missionId) : null;
            if (mission == null) return;
            IProgressService progress = UIServices.Progress;
            MissionBest best = progress != null ? progress.GetBest(missionId) : null;
            UINavigationContext.SelectedMission = mission;
            UINavigationContext.SelectedLevel = best != null && best.LastPlayedLevel.HasValue ? best.LastPlayedLevel.Value : DifficultyLevel.Guided;
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(UIScreenId.MissionBrief);
        }

        /// <summary>
        /// Clic sulla striscia del Training: apre PANEL_TrainingBrief.
        /// </summary>
        private void OnTrainingClicked()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(UIScreenId.TrainingBrief);
        }

        /// <summary>
        /// Si iscrive a Changed del servizio dei progressi corrente (una sola volta; cambia se il servizio cambia).
        /// </summary>
        private void SubscribeProgress()
        {
            IProgressService current = UIServices.Progress;
            if (current == subscribedProgress) return;
            UnsubscribeProgress();
            subscribedProgress = current;
            if (subscribedProgress != null) subscribedProgress.Changed += Refresh;
        }

        /// <summary>
        /// Toglie l'iscrizione a Changed.
        /// </summary>
        private void UnsubscribeProgress()
        {
            if (subscribedProgress != null) subscribedProgress.Changed -= Refresh;
            subscribedProgress = null;
        }

        /// <summary>
        /// Collega i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIStrings newStrings, UIScreen newScreen, TrainingCard newTrainingCard, ScenarioCard[] newCards,
            GameObject newPager, Button newPrevious, Button newNext, TMP_Text newPageLabel)
        {
            strings = newStrings;
            screen = newScreen;
            trainingCard = newTrainingCard;
            cards = newCards;
            pager = newPager;
            previousPageButton = newPrevious;
            nextPageButton = newNext;
            pageLabel = newPageLabel;
        }
    }
}
