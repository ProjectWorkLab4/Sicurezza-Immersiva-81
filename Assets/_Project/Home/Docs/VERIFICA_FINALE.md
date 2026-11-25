# VERIFICA FINALE — UI comune SI81 (v0.7.0, 2026-11-18)

Controllo indipendente dei Blocchi A–D. Legenda: **OK** = conforme a CLAUDE.md §2/§9; **corretto** = sistemato in questa verifica; **eccezione** = scostamento accettato e documentato.

## 1. Prefab (45)

| Gruppo | Prefab | Esito |
| --- | --- | --- |
| Base | MNG_UI, PANEL_Menu, PANEL_Side, PANEL_BottomBar, BTN_BottomBarItem | OK |
| Pulsanti / liste | BTN_Primary, BTN_Secondary, BTN_Back, BTN_Badge, BTN_SettingsTab, BTN_ListRow, PANEL_ConfirmDialog | OK |
| Card | BTN_ScenarioCard, PANEL_TrainingCard, BTN_DifficultyCard | OK (PANEL_TrainingCard cliccabile: nome dalla tabella §2, da confermare) |
| Etichette | LBL_LevelBadge, LBL_StarRating, LBL_DimensionBar, LBL_Outcome, LBL_ObjectiveItem, PANEL_StatTile | OK |
| Impostazioni | PANEL_SettingsToggle / Slider / Selector / Action | OK |
| Schermate menu | PANEL_Home, MissionBrief, Stats, Profile, Settings, TrainingBrief | OK |
| Fine / pausa | PANEL_Pause, Debrief, ScoreReplay, TrainingComplete | OK |
| HUD | MNG_HUD, HUD_WristPanel, EventAlert, ActionFeedback, Subtitles, RiskLabel, InteractionPrompt, ContextHelp, DecisionPanel, OutcomePanel | OK (eccezione: valori di design HUD serializzati) |

Controllo programmatico su tutti i prefab: nome radice = nome asset; 0 script mancanti; 0 Animator / Shadow / Outline / TMP_Dropdown; 0 Layout Group attivi nei prefab in scenario; solo Canvas World Space; 0 GraphicRaycaster semplici, 0 TrackedDeviceRaycaster, 0 tipi XRI/OVR.

## 2. Scene e dati

| Asset | Esito |
| --- | --- |
| SCN_Core | OK: 1 EventSystem, 1 InputSystemUIInputModule, solo UIWorldRaycaster, nessun Canvas Screen Space |
| SCN_UISandbox | OK (non in Build Settings) |
| SCN_ScenarioTemplate, SCN_MockScenario | OK: nessun Canvas / EventSystem / camera / rig |
| SO_UITheme, SO_DifficultyStyle, SO_UIStrings_It (174 chiavi, 0 problemi), SO_HUDIcons | OK |
| SO_MissionCatalog, SO_LevelConfig_* | OK (valori provvisori) |
| SO_Mission_* (8 di esempio, Mock, Template) | OK / eccezione: segnaposto "(esempio)" |
| SO_Mission_Training | **corretto**: aggiunti 3 criteri; `SCN_Training` da creare dal team del training |

## 3. Correzioni fatte

- Commenti: 76 `/// <summary>` mancanti aggiunti (campi pubblici delle strutture dei builder, riferimenti di `UIButton` / `UISettingsRow`). Scansione di tutti i .cs: 0 funzioni o membri pubblici senza summary.
- Duplicati: 4 gruppi di helper copiati estratti (`PlaceTopLeftCentered`, `EnsureFolder`, `LoadIcon`, `DefaultLevels`).
- Validatore: missioni di esempio senza scena = avviso; Training senza avvisi sui livelli. Risultato: 0 errori, 9 avvisi.
- Guida (v0.2, §9 nuovo) e CHANGELOG v0.7.0 allineati.

## 4. Requisiti brief / CLAUDE.md

- Scenari solo da dati: `Nuovo scenario...` crea `SO_Mission` + scena, li mette in catalogo e Build Settings; il validatore blocca UI fatta a mano (Canvas, EventSystem, `HUD_*`, `PANEL_*`, `MNG_UI/HUD`). Passi della guida verificati sul codice.
- §8 assente: nessun livello globale, Graphics, Account/Logout, canvas curvi, blur in tempo reale, HUD fisso o centrato, profili multipli, filtro luce blu. "SPI" solo in commenti e nel controllo che lo vieta.
- §3: nessun XRI/OVR nel codice SI81 (`TrackedDeviceRaycaster` compare solo per rilevarlo e rimuoverlo); nessun `TMP_Dropdown`; un solo EventSystem.

## 5. Eccezioni accettate

- Misure di layout dei componenti Label e valori di design HUD (pose del polso, angoli, velocità) nei builder o serializzati.
- Didascalie della galleria (solo sandbox) e anteprime di testo salvate nei prefab: sono il valore risolto da chiavi di `SO_UIStrings_It`, sostituito a runtime.
- Istanze `BTN_Option_01..04` (indice come suffisso) e `BTN_ScenarioCardN`.
- Canvas HUD di sola lettura senza raycaster; ~360 campi privati senza commento (non richiesti da §2).
- Avvisi "Missing script (Unknown)" e "Account API" della Console: vengono da pacchetti Meta / Unity, non da SI81.

## 6. Da tradurre su decisione del team

Parole inglesi prese da Figma: Safety, Awareness, Decision, Execution, Time (dimensioni); Scan (fase); Guided, Operational, Crisis (livelli); Rank; Mission brief.

## 7. Decisioni aperte (team / utente)

SCN_Training e rig reale (VR Interaction System); valori provvisori (soglie stelle 20/40/60/80/90, tempi dei livelli, posa del polso, valori del PDF HUD); PANEL_TrainingCard vs BTN_; bordo di BTN_Primary; suoni SFX_ (solo slot); prove su Quest 3.
