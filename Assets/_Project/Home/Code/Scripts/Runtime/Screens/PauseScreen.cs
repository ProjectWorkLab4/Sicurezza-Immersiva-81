using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_Pause (Figma "Pausa" + "conferma Esci"): titolo "01 · Fire Starter · Pausa", badge del livello, pillola "Fase: ...",
    /// obiettivi della missione (fatto / corrente / da fare) e quattro pulsanti: Riprendi, Ricomincia (conferma), Impostazioni rapide
    /// (apre PANEL_Settings, ← torna alla Pausa) ed Esci alla Home (bianco, conferma con il pulsante Danger nel dialogo; non registra nulla).
    /// Le azioni stanno in <see cref="MissionFlow"/>. La scena è oscurata da IMG_Dim (nero 40%, token del velo dei dialoghi), grande
    /// quanto basta a coprire la vista. Alla comparsa il dialogo si chiude e il contenuto si aggiorna dalla sessione.
    /// Uso: istanza di PANEL_Pause sotto PANEL_Screens (UIScreen id Pause); la apre MissionFlow.Pause (tasto menu sinistro / P).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Pause Screen")]
    public class PauseScreen : MonoBehaviour
    {
        /// <summary>Numero massimo di obiettivi mostrati (MissionDefinition.MAX_OBJECTIVES).</summary>
        private const int MAX_OBJECTIVES = MissionDefinition.MAX_OBJECTIVES;
        /// <summary>Id del Blackout: i suoi obiettivi sono provvisori e restano solo in Guided.</summary>
        const string ID_BLACKOUT = "02";

        [Header("Dati")]
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Larghezza degli obiettivi, in unità Canvas (colonna sinistra del contenuto).")]
        [SerializeField] private float objectivesWidth = 448f;
        [Tooltip("Spazio tra gli obiettivi (Figma: 4).")]
        [SerializeField] private float objectivesGap = 4f;

        [Header("Intestazione")]
        [SerializeField] private MenuPanel menu;
        [SerializeField] private LevelBadge levelBadge;
        [Tooltip("Pillola \"Fase: ...\" (nascosta se la fase è sconosciuta).")]
        [SerializeField] private GameObject phasePill;
        [SerializeField] private TMP_Text phaseText;

        [Header("Obiettivi")]
        [Tooltip("Contenitore degli obiettivi (sotto il titolo \"Obiettivi\").")]
        [SerializeField] private RectTransform objectivesRoot;
        [SerializeField] private ObjectiveItem[] objectiveItems = new ObjectiveItem[MAX_OBJECTIVES];
        // Titolo "Obiettivi", fratello di PANEL_Objectives nel builder: cercato a runtime per nasconderlo insieme alla lista.
        private GameObject objectiveTitle;

        [Header("Pulsanti")]
        [SerializeField] private UIButton resumeButton;
        [SerializeField] private UIButton restartButton;
        [SerializeField] private UIButton quickSettingsButton;
        [SerializeField] private UIButton exitButton;
        [Tooltip("Dialogo di conferma (istanza di PANEL_ConfirmDialog, salvata disattiva).")]
        [SerializeField] private ConfirmDialog confirmDialog;
        [Tooltip("Icona del pulsante di conferma di \"Ricomincia\" (ICN_RestartAlt).")]
        [SerializeField] private Sprite restartIcon;

        /// <summary>
        /// Collega i pulsanti.
        /// </summary>
        private void Awake()
        {
            if (resumeButton != null) resumeButton.Clicked += HandleResume;
            if (restartButton != null) restartButton.Clicked += HandleRestart;
            if (quickSettingsButton != null) quickSettingsButton.Clicked += HandleQuickSettings;
            if (exitButton != null) exitButton.Clicked += HandleExit;
            if (objectivesRoot != null && objectivesRoot.parent != null)
            {
                Transform titolo = objectivesRoot.parent.Find("LBL_ObjTitle");
                if (titolo != null) objectiveTitle = titolo.gameObject;
            }
        }

        /// <summary>
        /// Scollega i pulsanti.
        /// </summary>
        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.Clicked -= HandleResume;
            if (restartButton != null) restartButton.Clicked -= HandleRestart;
            if (quickSettingsButton != null) quickSettingsButton.Clicked -= HandleQuickSettings;
            if (exitButton != null) exitButton.Clicked -= HandleExit;
        }

        /// <summary>
        /// Alla comparsa chiude il dialogo e aggiorna titolo, livello, fase e obiettivi.
        /// </summary>
        private void OnEnable()
        {
            if (confirmDialog != null) confirmDialog.Hide();
            Refresh();
        }

        /// <summary>
        /// Aggiorna il contenuto dalla sessione corrente (<see cref="MissionSession"/> e <see cref="MissionFlow"/>).
        /// </summary>
        public void Refresh()
        {
            MissionDefinition mission = MissionSession.Current;
            if (menu != null && strings != null)
                menu.SetTitleText(strings.Format("ui.pause.title", mission != null ? mission.GetFullTitle() : string.Empty));
            if (levelBadge != null) levelBadge.SetLevel(MissionSession.Level);

            // Fase: pillola solo se la fase è nota.
            string phaseKey = MissionFlow.CurrentPhaseKey;
            bool hasPhase = strings != null && !string.IsNullOrEmpty(phaseKey) && strings.Contains(phaseKey);
            if (phasePill != null) PanelLayout.SetActive(phasePill, hasPhase);
            if (hasPhase && phaseText != null)
            {
                string text = strings.Format("ui.pause.phase", strings.Get(phaseKey));
                if (phaseText.text != text) phaseText.text = text;
            }
            RefreshObjectives(mission);
        }

        /// <summary>
        /// Obiettivi impilati dall'alto: prima del corrente = fatti, il corrente, poi da fare.
        /// Per Blackout l'elenco è provvisorio: solo in Guided, negli altri livelli si nascondono titolo e righe.
        /// </summary>
        /// <param name="mission">Scenario (null = nessun obiettivo).</param>
        private void RefreshObjectives(MissionDefinition mission)
        {
            float y = 0f;
            int current = Mathf.Max(0, MissionFlow.ObjectiveIndex);
            bool mostra = mission == null || mission.Id != ID_BLACKOUT || MissionSession.Level == DifficultyLevel.Guided;
            if (objectiveTitle != null) PanelLayout.SetActive(objectiveTitle, mostra);
            if (objectivesRoot != null) PanelLayout.SetActive(objectivesRoot.gameObject, mostra && mission != null && mission.Objectives != null && mission.Objectives.Count > 0);
            for (int i = 0; i < objectiveItems.Length; i++)
            {
                ObjectiveItem item = objectiveItems[i];
                if (item == null) continue;
                bool visible = mostra && mission != null && mission.Objectives != null && i < mission.Objectives.Count;
                PanelLayout.SetActive(item.gameObject, visible);
                if (!visible) continue;
                var rect = (RectTransform)item.transform;
                // Larghezza prima del testo: ObjectiveItem calcola l'altezza da essa.
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y - rect.rect.height), new Vector2(objectivesWidth, -y));
                ObjectiveState state = i < current ? ObjectiveState.Done : (i == current ? ObjectiveState.Current : ObjectiveState.Todo);
                item.SetObjective(mission.Objectives[i], state);
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y - rect.rect.height), new Vector2(objectivesWidth, -y));
                y += rect.rect.height + objectivesGap;
            }
        }

        /// <summary>
        /// "Riprendi": chiude la pausa.
        /// </summary>
        private void HandleResume()
        {
            MissionFlow.Resume();
        }

        /// <summary>
        /// "Ricomincia": chiede conferma, poi rilancia lo stesso scenario e livello.
        /// </summary>
        private void HandleRestart()
        {
            if (confirmDialog == null)
            {
                MissionFlow.RestartMission();
                return;
            }
            confirmDialog.Show("ui.dialog.restart.title", "ui.dialog.restart.body", "ui.button.restart", "ui.button.cancel", MissionFlow.RestartMission, null, restartIcon);
        }

        /// <summary>
        /// "Impostazioni rapide": apre PANEL_Settings (← riporta alla Pausa).
        /// </summary>
        private void HandleQuickSettings()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(UIScreenId.Settings);
        }

        /// <summary>
        /// "Esci alla Home": chiede conferma (pulsante Danger nel dialogo), poi esce senza registrare nulla.
        /// </summary>
        private void HandleExit()
        {
            if (confirmDialog == null)
            {
                MissionFlow.ExitToHome();
                return;
            }
            confirmDialog.Show("ui.dialog.exit_mission.title", "ui.dialog.exit_mission.body", "ui.button.exit", "ui.button.cancel", MissionFlow.ExitToHome, null);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIStrings newStrings, float newObjectivesWidth, float newObjectivesGap, MenuPanel newMenu, LevelBadge newBadge,
            GameObject newPhasePill, TMP_Text newPhaseText, RectTransform newObjectivesRoot, ObjectiveItem[] newItems,
            UIButton newResume, UIButton newRestart, UIButton newQuick, UIButton newExit, ConfirmDialog newDialog, Sprite newRestartIcon)
        {
            strings = newStrings;
            objectivesWidth = newObjectivesWidth;
            objectivesGap = newObjectivesGap;
            menu = newMenu;
            levelBadge = newBadge;
            phasePill = newPhasePill;
            phaseText = newPhaseText;
            objectivesRoot = newObjectivesRoot;
            objectiveItems = newItems;
            resumeButton = newResume;
            restartButton = newRestart;
            quickSettingsButton = newQuick;
            exitButton = newExit;
            confirmDialog = newDialog;
            restartIcon = newRestartIcon;
        }
    }
}
