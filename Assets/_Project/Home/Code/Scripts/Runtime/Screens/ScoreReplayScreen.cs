using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_ScoreReplay (Figma "Score &amp; Replay"): "SPI N/100" con stelle (= rank), i badge appena sbloccati,
    /// le cinque dimensioni (LBL_DimensionBar Dark) con la differenza rispetto al tentativo precedente, e tre azioni:
    /// Home (torna alla Home), Cambia livello (Mission Brief con lo scenario scelto) e Riprova (stesso scenario e livello).
    /// Dati da <see cref="MissionSession.LastResult"/> e <see cref="UIServices.Progress"/>.
    /// Uso: istanza di PANEL_ScoreReplay sotto PANEL_Screens (UIScreen id ScoreReplay); ci si arriva da PANEL_Debrief ("Avanti").
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Score Replay Screen")]
    public class ScoreReplayScreen : MonoBehaviour
    {
        /// <summary>Numero di dimensioni del punteggio sicurezza.</summary>
        private const int DIMENSION_COUNT = 5;

        [Header("Dati")]
        [SerializeField] private UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;

        [Header("Intestazione")]
        [SerializeField] private ResultHeader header;

        [Header("Punteggio")]
        [Tooltip("Il numero grande (Display Bold).")]
        [SerializeField] private TMP_Text scoreNumber;
        [Tooltip("Stelle del rank.")]
        [SerializeField] private StarRating stars;

        [Header("Nuovi badge")]
        [Tooltip("Blocco \"Nuovi badge\" (nascosto se non ce ne sono).")]
        [SerializeField] private GameObject newBadgesRoot;
        [SerializeField] private BadgeButton newBadgeButton;
        [SerializeField] private TMP_Text newBadgeName;

        [Header("Dimensioni")]
        [Tooltip("Le 5 barre nell'ordine di DimensionKind.")]
        [SerializeField] private DimensionBar[] bars = new DimensionBar[DIMENSION_COUNT];
        [Tooltip("Le 5 differenze rispetto al tentativo precedente (Body Bold).")]
        [SerializeField] private TMP_Text[] deltas = new TMP_Text[DIMENSION_COUNT];

        [Header("Azioni")]
        [SerializeField] private UIButton homeButton;
        [SerializeField] private UIButton changeLevelButton;
        [SerializeField] private UIButton retryButton;

        // Elenco riutilizzato dei badge nuovi.
        private readonly List<BadgeInfo> newBadges = new List<BadgeInfo>();

        /// <summary>
        /// Collega i pulsanti.
        /// </summary>
        private void Awake()
        {
            if (homeButton != null) homeButton.Clicked += HandleHome;
            if (changeLevelButton != null) changeLevelButton.Clicked += HandleChangeLevel;
            if (retryButton != null) retryButton.Clicked += HandleRetry;
        }

        /// <summary>
        /// Scollega i pulsanti.
        /// </summary>
        private void OnDestroy()
        {
            if (homeButton != null) homeButton.Clicked -= HandleHome;
            if (changeLevelButton != null) changeLevelButton.Clicked -= HandleChangeLevel;
            if (retryButton != null) retryButton.Clicked -= HandleRetry;
        }

        /// <summary>
        /// Alla comparsa aggiorna la schermata.
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// Riempie punteggio, stelle, badge nuovi e dimensioni dal risultato corrente.
        /// </summary>
        public void Refresh()
        {
            MissionResult result = DebriefScreen.ResolveResult(out MissionDefinition mission);
            if (header != null) header.Show(result, mission);
            // "78" grande + "/100" piccolo e secondario nello stesso testo (rich text), così il suffisso segue il numero.
            if (scoreNumber != null)
            {
                string suffix = strings.Get("ui.score.out_of_100");
                string color = ColorUtility.ToHtmlStringRGB(theme.TextSecondary);
                SetText(scoreNumber, Mathf.Clamp(result.SafetyScore, 0, 100) + "<size=24><color=#" + color + ">" + suffix + "</color></size>");
            }
            if (stars != null) stars.SetStars(result.Stars);
            RefreshBadges();
            RefreshDimensions(result);
        }

        /// <summary>
        /// "Nuovi badge": primo badge sbloccato durante la missione (il blocco sparisce se non ce ne sono).
        /// </summary>
        private void RefreshBadges()
        {
            MissionFlow.GetNewBadges(newBadges);
            bool any = newBadges.Count > 0;
            if (newBadgesRoot != null) PanelLayout.SetActive(newBadgesRoot, any);
            if (!any) return;
            BadgeInfo badge = newBadges[0];
            if (newBadgeButton != null) newBadgeButton.SetData(true, badge.Icon);
            if (newBadgeName != null) SetText(newBadgeName, badge.Title);
        }

        /// <summary>
        /// Barre delle cinque dimensioni e differenze rispetto al tentativo precedente dello stesso scenario (vuote se non c'è).
        /// </summary>
        private void RefreshDimensions(MissionResult result)
        {
            MissionResult previous = FindPrevious(result);
            for (int i = 0; i < DIMENSION_COUNT; i++)
            {
                int value = result.Dimensions != null && i < result.Dimensions.Length ? result.Dimensions[i] : 0;
                if (i < bars.Length && bars[i] != null) bars[i].SetValue(value);
                if (i >= deltas.Length || deltas[i] == null) continue;
                if (previous == null || previous.Dimensions == null || i >= previous.Dimensions.Length)
                {
                    SetText(deltas[i], string.Empty);
                    continue;
                }
                int delta = value - previous.Dimensions[i];
                if (delta > 0)
                {
                    deltas[i].color = theme.BrandLight;
                    SetText(deltas[i], strings.Format("ui.format.delta_positive", delta));
                }
                else if (delta < 0)
                {
                    deltas[i].color = theme.TextOnGreen;
                    SetText(deltas[i], strings.Format("ui.format.delta_negative", -delta));
                }
                else
                {
                    deltas[i].color = theme.TextOnGreen;
                    SetText(deltas[i], "0");
                }
            }
        }

        /// <summary>
        /// Tentativo precedente dello stesso scenario: il risultato salvato prima di quello corrente (null se è il primo).
        /// </summary>
        private static MissionResult FindPrevious(MissionResult current)
        {
            if (UIServices.Progress == null || string.IsNullOrEmpty(current.MissionId)) return null;
            IReadOnlyList<MissionResult> list = UIServices.Progress.GetResults(current.MissionId);
            if (list == null || list.Count == 0) return null;
            MissionResult last = list[list.Count - 1];
            // L'ultimo salvato è quello corrente (stesso istante) oppure, se non è ancora salvato, è il precedente.
            if (last.TimestampUtc == current.TimestampUtc) return list.Count >= 2 ? list[list.Count - 2] : null;
            return last;
        }

        /// <summary>
        /// "Home": torna al menu e va a Home (scarica lo scenario).
        /// </summary>
        private void HandleHome()
        {
            MissionFlow.ExitToHome();
        }

        /// <summary>
        /// "Cambia livello": apre Mission Brief con lo scenario appena giocato selezionato (← porta a Home).
        /// </summary>
        private void HandleChangeLevel()
        {
            UIScreenManager manager = UIScreenManager.Instance;
            MissionDefinition mission = MissionSession.Current;
            if (manager == null || mission == null) return;
            UINavigationContext.SelectedMission = mission;
            UINavigationContext.SelectedLevel = null;
            // Chiude la sessione (barra in basso di nuovo visibile); lo scenario caricato lo sostituisce il lanciatore al prossimo avvio.
            MissionSession.Clear();
            manager.GoHome();
            manager.Show(UIScreenId.MissionBrief);
        }

        /// <summary>
        /// "Riprova": rilancia lo stesso scenario allo stesso livello.
        /// </summary>
        private void HandleRetry()
        {
            MissionFlow.RestartMission();
        }

        /// <summary>
        /// Scrive un testo solo se è cambiato.
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target.text != value) target.text = value;
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, ResultHeader newHeader, TMP_Text newScoreNumber, StarRating newStars,
            GameObject newBadgesBlock, BadgeButton newBadgeBtn, TMP_Text newBadgeLabel, DimensionBar[] newBars, TMP_Text[] newDeltas,
            UIButton newHome, UIButton newChangeLevel, UIButton newRetry)
        {
            theme = newTheme;
            strings = newStrings;
            header = newHeader;
            scoreNumber = newScoreNumber;
            stars = newStars;
            newBadgesRoot = newBadgesBlock;
            newBadgeButton = newBadgeBtn;
            newBadgeName = newBadgeLabel;
            bars = newBars;
            deltas = newDeltas;
            homeButton = newHome;
            changeLevelButton = newChangeLevel;
            retryButton = newRetry;
        }
    }
}
