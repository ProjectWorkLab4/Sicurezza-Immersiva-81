using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_Pause (Figma "Pausa" + "conferma Esci"): titolo "01 · Fire Starter · Pausa", badge del livello, pillola "Fase: ...",
    /// obiettivi della missione (fatto / corrente / da fare; oppure checklist dettagliata scorrevole quando lo scenario
    /// imposta <see cref="MissionFlow.PauseChecklist"/>) e quattro pulsanti: Riprendi, Ricomincia (conferma), Impostazioni rapide
    /// (apre PANEL_Settings, ← torna alla Pausa) ed Esci alla Home (bianco, conferma con il pulsante Danger nel dialogo; non registra nulla).
    /// Le azioni stanno in <see cref="MissionFlow"/>. La scena è oscurata da IMG_Dim (nero 40%, token del velo dei dialoghi), grande
    /// quanto basta a coprire la vista. Alla comparsa il dialogo si chiude e il contenuto si aggiorna dalla sessione.
    /// Uso: istanza di PANEL_Pause sotto PANEL_Screens (UIScreen id Pause); la apre MissionFlow.Pause (tasto menu sinistro / P).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Pause Screen")]
    public class PauseScreen : MonoBehaviour
    {
        /// <summary>Numero massimo di obiettivi standard mostrati (MissionDefinition.MAX_OBJECTIVES).</summary>
        private const int MAX_OBJECTIVES = MissionDefinition.MAX_OBJECTIVES;
        /// <summary>Id del Blackout: i suoi obiettivi sono provvisori e restano solo in Guided.</summary>
        private const string ID_BLACKOUT = "02";
        private const float SCROLLBAR_WIDTH = 10f;
        private const float SCROLLBAR_GUTTER = 18f;
        private const float FALLBACK_VIEWPORT_HEIGHT = 200f;
        private static readonly Color ScrollbarTrackColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color ScrollbarHandleColor = new Color(0.29f, 0.87f, 0.50f, 0.90f);

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

        // Scroll view runtime costruita una sola volta dentro objectivesRoot quando serve la checklist dettagliata.
        private readonly List<ObjectiveItem> pooledItems = new List<ObjectiveItem>();
        private ScrollRect checklistScrollRect;
        private RectTransform checklistViewport;
        private RectTransform checklistContent;
        private Scrollbar checklistScrollbar;
        private bool scrollViewReady;

        /// <summary>ScrollRect runtime creato per la checklist dettagliata, o null se non inizializzato.</summary>
        public ScrollRect ChecklistScrollRect => checklistScrollRect;
        /// <summary>Scrollbar verticale runtime della checklist dettagliata, o null se non inizializzato.</summary>
        public Scrollbar ChecklistScrollbar => checklistScrollbar;
        /// <summary>Numero di voci obiettivo attualmente visibili nella schermata di Pausa.</summary>
        public int VisibleObjectiveCount { get; private set; }

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
        /// Obiettivi impilati dall'alto: se lo scenario ha fornito <see cref="MissionFlow.PauseChecklist"/> mostra
        /// l'elenco dettagliato in una lista scorrevole con barra verticale; altrimenti usa i 4 obiettivi sintetici di
        /// <see cref="MissionDefinition.Objectives"/>.
        /// </summary>
        /// <param name="mission">Scenario (null = nessun obiettivo).</param>
        private void RefreshObjectives(MissionDefinition mission)
        {
            bool mostra = mission == null || mission.Id != ID_BLACKOUT || MissionSession.Level == DifficultyLevel.Guided;
            if (objectiveTitle != null) PanelLayout.SetActive(objectiveTitle, mostra);

            IReadOnlyList<PauseChecklistEntry> checklist = mostra ? MissionFlow.PauseChecklist : null;
            bool useChecklist = checklist != null && checklist.Count > 0;
            bool hasStandard = mostra && mission != null && mission.Objectives != null && mission.Objectives.Count > 0;
            bool hasAny = useChecklist || hasStandard;

            if (objectivesRoot != null) PanelLayout.SetActive(objectivesRoot.gameObject, hasAny);
            if (!hasAny)
            {
                VisibleObjectiveCount = 0;
                for (int i = 0; i < objectiveItems.Length; i++)
                {
                    if (objectiveItems[i] != null) PanelLayout.SetActive(objectiveItems[i].gameObject, false);
                }
                HideExtraPooledItems(0);
                return;
            }

            if (!useChecklist)
            {
                RefreshStandardObjectives(mission);
                return;
            }

            RefreshScrollableChecklist(checklist);
        }

        private void RefreshStandardObjectives(MissionDefinition mission)
        {
            float y = 0f;
            int current = Mathf.Max(0, MissionFlow.ObjectiveIndex);
            float width = objectivesWidth;
            int visibleCount = 0;

            if (checklistScrollbar != null) PanelLayout.SetActive(checklistScrollbar.gameObject, false);
            if (checklistViewport != null) checklistViewport.offsetMax = Vector2.zero;

            for (int i = 0; i < objectiveItems.Length; i++)
            {
                ObjectiveItem item = objectiveItems[i];
                if (item == null) continue;
                bool visible = i < mission.Objectives.Count;
                PanelLayout.SetActive(item.gameObject, visible);
                if (!visible) continue;
                visibleCount++;
                var rect = (RectTransform)item.transform;
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -y - rect.rect.height), new Vector2(width, -y));
                ObjectiveState state = i < current ? ObjectiveState.Done : (i == current ? ObjectiveState.Current : ObjectiveState.Todo);
                item.SetObjective(mission.Objectives[i], state);
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -y - rect.rect.height), new Vector2(width, -y));
                y += rect.rect.height + objectivesGap;
            }

            HideExtraPooledItems(objectiveItems.Length);
            if (checklistContent != null)
            {
                float viewportH = GetViewportHeight();
                checklistContent.sizeDelta = new Vector2(0f, Mathf.Max(viewportH, y));
                if (checklistScrollRect != null)
                {
                    checklistScrollRect.verticalNormalizedPosition = 1f;
                    checklistScrollRect.enabled = false;
                }
            }
            VisibleObjectiveCount = visibleCount;
        }

        private void RefreshScrollableChecklist(IReadOnlyList<PauseChecklistEntry> checklist)
        {
            EnsureChecklistScrollView();
            if (!scrollViewReady)
            {
                // Fallback sicuro se il prefab non ha objectivesRoot o template ObjectiveItem.
                VisibleObjectiveCount = 0;
                return;
            }

            EnsurePooledItemCount(checklist.Count);

            float viewportHeight = GetViewportHeight();
            float itemWidth = Mathf.Max(120f, objectivesWidth - SCROLLBAR_GUTTER);
            if (checklistViewport != null) checklistViewport.offsetMax = new Vector2(-SCROLLBAR_GUTTER, 0f);

            float y = 0f;
            float firstCurrentY = -1f;
            for (int i = 0; i < pooledItems.Count; i++)
            {
                ObjectiveItem item = pooledItems[i];
                if (item == null) continue;
                bool visible = i < checklist.Count;
                PanelLayout.SetActive(item.gameObject, visible);
                if (!visible) continue;

                PauseChecklistEntry entry = checklist[i];
                var rect = (RectTransform)item.transform;
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -y - rect.rect.height), new Vector2(itemWidth, -y));
                item.SetObjective(entry.Text, entry.State);
                PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -y - rect.rect.height), new Vector2(itemWidth, -y));

                if (firstCurrentY < 0f && entry.State == ObjectiveState.Current) firstCurrentY = y;
                y += rect.rect.height + objectivesGap;
            }

            float totalHeight = Mathf.Max(0f, y - objectivesGap);
            checklistContent.sizeDelta = new Vector2(0f, Mathf.Max(viewportHeight, totalHeight));

            bool needsScroll = totalHeight > viewportHeight + 1f;
            if (checklistScrollbar != null)
            {
                PanelLayout.SetActive(checklistScrollbar.gameObject, needsScroll);
                if (needsScroll)
                {
                    checklistScrollbar.size = Mathf.Clamp(viewportHeight / totalHeight, 0.14f, 0.95f);
                }
            }

            if (checklistScrollRect != null)
            {
                checklistScrollRect.enabled = needsScroll;
                if (needsScroll && firstCurrentY > 0f)
                {
                    float maxScroll = Mathf.Max(1f, totalHeight - viewportHeight);
                    float targetY = Mathf.Clamp(firstCurrentY - 24f, 0f, maxScroll);
                    checklistScrollRect.verticalNormalizedPosition = 1f - (targetY / maxScroll);
                }
                else
                {
                    checklistScrollRect.verticalNormalizedPosition = 1f;
                }
            }

            VisibleObjectiveCount = checklist.Count;
        }

        private void EnsureChecklistScrollView()
        {
            if (scrollViewReady || objectivesRoot == null) return;

            ObjectiveItem template = FindTemplateObjectiveItem();
            if (template == null) return;

            GameObject viewportGo = new GameObject("VIEWPORT_Objectives", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            checklistViewport = (RectTransform)viewportGo.transform;
            checklistViewport.SetParent(objectivesRoot, false);
            checklistViewport.anchorMin = Vector2.zero;
            checklistViewport.anchorMax = Vector2.one;
            checklistViewport.pivot = new Vector2(0.5f, 0.5f);
            checklistViewport.offsetMin = Vector2.zero;
            checklistViewport.offsetMax = new Vector2(-SCROLLBAR_GUTTER, 0f);
            Image viewportRay = viewportGo.GetComponent<Image>();
            viewportRay.color = new Color(0f, 0f, 0f, 0f);
            viewportRay.raycastTarget = true;

            GameObject contentGo = new GameObject("CONTENT_Objectives", typeof(RectTransform));
            checklistContent = (RectTransform)contentGo.transform;
            checklistContent.SetParent(checklistViewport, false);
            checklistContent.anchorMin = new Vector2(0f, 1f);
            checklistContent.anchorMax = new Vector2(1f, 1f);
            checklistContent.pivot = new Vector2(0f, 1f);
            checklistContent.anchoredPosition = Vector2.zero;
            checklistContent.sizeDelta = new Vector2(0f, GetViewportHeight());

            pooledItems.Clear();
            for (int i = 0; i < objectiveItems.Length; i++)
            {
                ObjectiveItem existing = objectiveItems[i];
                if (existing == null) continue;
                existing.transform.SetParent(checklistContent, false);
                pooledItems.Add(existing);
            }

            GameObject scrollbarGo = new GameObject("SCROLLBAR_Objectives", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var sbRect = (RectTransform)scrollbarGo.transform;
            sbRect.SetParent(objectivesRoot, false);
            sbRect.anchorMin = new Vector2(1f, 0f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 0.5f);
            sbRect.sizeDelta = new Vector2(SCROLLBAR_WIDTH, 0f);
            sbRect.anchoredPosition = new Vector2(-2f, 0f);
            Image trackImg = scrollbarGo.GetComponent<Image>();
            trackImg.color = ScrollbarTrackColor;
            trackImg.raycastTarget = true;

            GameObject slidingGo = new GameObject("SlidingArea", typeof(RectTransform));
            var slidingRect = (RectTransform)slidingGo.transform;
            slidingRect.SetParent(sbRect, false);
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(1f, 2f);
            slidingRect.offsetMax = new Vector2(-1f, -2f);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRect = (RectTransform)handleGo.transform;
            handleRect.SetParent(slidingRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            Image handleImg = handleGo.GetComponent<Image>();
            handleImg.color = ScrollbarHandleColor;
            handleImg.raycastTarget = true;

            checklistScrollbar = scrollbarGo.GetComponent<Scrollbar>();
            checklistScrollbar.direction = Scrollbar.Direction.BottomToTop;
            checklistScrollbar.handleRect = handleRect;
            checklistScrollbar.targetGraphic = handleImg;
            checklistScrollbar.interactable = true;

            checklistScrollRect = objectivesRoot.GetComponent<ScrollRect>();
            if (checklistScrollRect == null) checklistScrollRect = objectivesRoot.gameObject.AddComponent<ScrollRect>();
            checklistScrollRect.viewport = checklistViewport;
            checklistScrollRect.content = checklistContent;
            checklistScrollRect.horizontal = false;
            checklistScrollRect.vertical = true;
            checklistScrollRect.movementType = ScrollRect.MovementType.Clamped;
            checklistScrollRect.inertia = true;
            checklistScrollRect.decelerationRate = 0.12f;
            checklistScrollRect.scrollSensitivity = 32f;
            checklistScrollRect.verticalScrollbar = checklistScrollbar;
            checklistScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            scrollViewReady = true;
        }

        private void EnsurePooledItemCount(int needed)
        {
            ObjectiveItem template = FindTemplateObjectiveItem();
            if (template == null || checklistContent == null) return;
            while (pooledItems.Count < needed)
            {
                ObjectiveItem clone = Instantiate(template, checklistContent, false);
                clone.name = "LBL_ObjectiveItem" + (pooledItems.Count + 1);
                pooledItems.Add(clone);
            }
        }

        private ObjectiveItem FindTemplateObjectiveItem()
        {
            for (int i = 0; i < objectiveItems.Length; i++)
                if (objectiveItems[i] != null) return objectiveItems[i];
            return pooledItems.Count > 0 ? pooledItems[0] : null;
        }

        private void HideExtraPooledItems(int startIndex)
        {
            for (int i = startIndex; i < pooledItems.Count; i++)
                if (pooledItems[i] != null) PanelLayout.SetActive(pooledItems[i].gameObject, false);
        }

        private float GetViewportHeight()
        {
            return objectivesRoot != null && objectivesRoot.rect.height > 1f
                ? objectivesRoot.rect.height
                : FALLBACK_VIEWPORT_HEIGHT;
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
