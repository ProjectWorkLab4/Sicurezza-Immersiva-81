# CHANGELOG — UI comune SI81 (`Assets/_Project/Home/`)

Proprietario: **Team UI — Romario Gjoka**. Formato: una voce per versione, poi una sezione per componente
(versione + nota d'uso in 3 righe). I valori **provvisori** sono elencati in fondo a ogni versione.

## v0.7.7 — 2026-11-20 — UI spostata nella struttura del README

Proprietario: **Team Ludovica**. Nessun cambiamento di comportamento: solo cartelle.
- Da `Assets/_Common/UI/` ad `Assets/_Project/Home/` (`Code/Scripts`, `UI/Prefabs/{Base,Components,Screens,HUD}`, `UI/Data`, `UI/Fonts`, `Art/Textures/{Icons,UI}`, `Art/Materials`, `Level/Scenes`, `Docs`); modello di scenario in `Assets/_Project/Shared/Level/ScenarioTemplate/`; documenti per i team in `Assets/_Project/Docs/`.
- Spostamento fatto dentro Unity: GUID e riferimenti invariati. Builder e `ScenarioController` puntano ai nuovi percorsi.

## v0.7.6 — 2026-11-19 — Causa trovata: binding dei controller rifiutati sul Quest 3

Proprietario: **Team Ludovica**. Log del Quest con v0.7.5: raggi corti e nessuna pressione del grilletto vista da Unity. Il runtime rifiuta
`/user/detached_controller_meta/left/input/thumbrest/force` sul profilo `touch_controller_plus` (`XR_ERROR_PATH_UNSUPPORTED`), quindi tutti i
binding del profilo Touch Plus (controller del Quest 3) saltano e l'Input System non riceve né posa "aim" né grilletto.
- **OpenXR Android**: spenti i 3 profili "Detached ..." (controller posati con mani tracciate: non usati).
- **ControllersOnlyBuildGuard**: a ogni build Android spegne anche i profili Detached (oltre alle mani). Menu `SI81/Build/Spegni profili mani e controller staccati (Android)`.
- Da verificare sul visore: nel log non deve più comparire `XR_ERROR_PATH_UNSUPPORTED` e devono comparire le righe `[SI81 Input] grilletto ...`.

## v0.7.5 — 2026-11-19 — Diagnosi input sul visore + guardia build "solo controller"

Proprietario: **Team Ludovica**. I log del Quest (19/11) mostravano un APK vecchio e nessuna riga della UI: impossibile capire il clic.
- **UIInputLog** (nuovo, aggiunto a runtime da UIInputSetup): righe `[SI81 Input]` nel log del visore: versione UI, impostazioni del modulo, controller collegati, e a ogni pressione del grilletto controller, posa del raggio, oggetto colpito e schermata.
- **ControllersOnlyBuildGuard** (Editor): un passo della build Android riaccendeva i profili OpenXR delle mani; ora a ogni build Android (ultimo passo) vengono spenti e la Console scrive `[SI81 Build] ...`. Menu `SI81/Build/Spegni profili mani (Android)`.

## v0.7.4 — 2026-11-19 — Controllo bug prima del test sul visore

Proprietario: **Team Ludovica**. Revisione del codice + giro completo in Play Mode guidato solo da un controller simulato (posa aim + grilletto).
- **XRHandDevices** (nuovo): sceglie il controller vero di ogni mano (tracciato, aggiornato più di recente) con cache per frame; usato da `UIPointer` e `HapticsService` (prima la vibrazione poteva andare a un dispositivo senza motore).
- **OpenXR Android**: disattivati `HandInteractionProfile` e `MicrosoftHandInteraction` (progetto solo controller): i dispositivi "mano" diventavano puntatori fermi con hover bloccato.
- **IMissionLauncher.CanLaunch**: una missione o il Training si avviano solo se la scena è nelle Build Settings. Home blocca le card anche in Editor (prima solo in build); "Avvia training" è disattivato con "Non disponibile" finché `SCN_Training` non esiste (prima dava 2 errori e non faceva nulla). `UIButton.SetDisabledLabelKey`.
- **UIScreenManager**: il primo centraggio del menu aspetta la prima posa vera della testa (max 3 s).
- **Validatore**: le camere che disegnano solo su RenderTexture (monitor, specchi) non sono più errore.
- **Sandbox**: il builder usa la posa aim anche per i controller di SCN_UISandbox (vale al prossimo Build Base).
- Prova: Home → Training (bloccato) → pagina 2 → 99 → Avvia → decisione → Debrief → Score → Home, tutto con il grilletto; validatore 0 errori; Console 0 errori.

## v0.7.3 — 2026-11-19 — Controller: interazione con la UI sul visore

Proprietario: **Team Ludovica**. Sul Quest 3 i raggi restavano corti e nessun pulsante reagiva (video del 19/11).
- **UIInputSetup**: `InputSystemUIInputModule.pointerBehavior = AllPointersAsIs`. Con il valore di default un evento di mouse/puntatore di sistema cancellava ogni frame i puntatori dei controller.
- **UIInputActions**: raggio dalla posa "aim" OpenXR (`pointerPosition` / `pointerRotation`) invece della posa "grip" (`devicePosition`), che puntava in alto.
- **UIPointer**: segue il dispositivo che il modulo usa davvero (più profili OpenXR per mano) e disegna il raggio dalla stessa posa "aim".
- Verificato in Editor con un controller simulato (posa aim + rumore del mouse): hover su "Profilo", grilletto → schermata Profilo. Da verificare sul visore.

## v0.7.2 — 2026-11-19 — Punteggio a schermo = "SPI N/100"

Proprietario: **Team Ludovica**. Decisione del team (nota Figma 17): il punteggio si mostra come **"SPI N/100"** + stelle, al posto di "Punteggio sicurezza N/100".
- `ui.format.safety_score` = "SPI {0}/100" (Statistiche, Profilo, Score & Replay). Etichetta di Score & Replay `ui.score.safety_label` = "SPI · Indice di sicurezza".
- Tolti i controlli che vietavano "SPI": `UIStrings` (verifica tabella) e validatore scenari (testi della missione).
- Commenti e tooltip aggiornati; guida di integrazione aggiornata. CLAUDE.md §4.6 e §8 aggiornati.

## v0.7.1 — 2026-11-19 — Rig condiviso OVRCameraRig, modelli dei controller, puntatore sempre visibile

Proprietario: **Team UI — Romario Gjoka**. Correzione di 3 problemi visti sul visore: UI che segue la testa, controller invisibili, nessun puntatore.
- **Causa**: `SCN_MockScenario` conteneva un secondo rig (`OVRCameraRig`) oltre al rig segnaposto di `SCN_Core`: due camere sul visore e UI agganciata al rig sbagliato.
- **SCN_Core**: `MNG_CoreRig` ora contiene `OVRCameraRig` di Meta XR Core SDK (origine `FloorLevel`) al posto del rig segnaposto con TrackedPoseDriver. `CoreRig` consegna `CenterEyeAnchor` / `TrackingSpace` / `Left-RightControllerAnchor` a MNG_UI e MNG_HUD. Modelli dei controller: `MESH_Controller` (OVRControllerPrefab) sotto ogni ancora. Il builder rimuove il vecchio `MNG_RigOffset`.
- **UIPointer**: il raggio è sempre visibile (corto, `pointer.idleRayLength` = 0,6 m, senza reticolo) quando non punta la UI; origine e direzione dalla stessa posa del modulo UI (devicePosition/Rotation nell'origine di tracciamento), così il raggio disegnato coincide con quello degli eventi su ogni rig.
- **Validatore**: una Camera nella scena di uno scenario ora è un **errore** (prima avviso).

### UIPointer — v0.7.1
- Va sull'ancora del controller del rig condiviso (oggi `Left/RightControllerAnchor` di OVRCameraRig).
- Raggio corto a riposo, raggio fino al punto colpito con reticolo sulla UI (colore valid/invalid).
- Misure solo da `SO_UITheme.Pointer` (`tokens_figma.json` → `project.pointer`).

Provvisori: `pointer.idleRayLength` 0,6 m (da confermare con VR Interaction System).

## v0.7.0 — 2026-11-18 — Verifica finale (Blocchi A–D)

Proprietario: **Team UI — Romario Gjoka**. Versione complessiva della UI comune: **0.7.0**. Ricostruzione completa (Build Theme → Build Base → Crea kit scenario): 45 prefab, GUID invariati (395 meta), Build Settings invariate, Console 0 errori. Tabella per prefab in `Docs/VERIFICA_FINALE.md`.
- **Commenti (§9)**: aggiunti 76 `/// <summary>` in italiano sui campi pubblici delle strutture dei builder (`GalleryBuilder`, `ComponentsBuilder.*`, `ScreensBuilder.*`, `BaseBuilder`) e sui riferimenti protetti di `UIButton` / `UISettingsRow`; scansione programmatica di tutti i .cs: 0 funzioni / membri pubblici senza summary.
- **Codice duplicato**: `PlaceTopLeft` di `ProfileScreen` / `StatsScreen` → `PanelLayout.PlaceTopLeftCentered`; 3 `EnsureFolder` privati (Icon/Sprite/ThemeBuilder) → `BaseBuilder.EnsureFolder`; `LoadIcon` di `BottomBarBuilder` / `GalleryBuilder` → `ComponentsBuilder.LoadIcon`; `DefaultLevels` di `ScenarioKitBuilder` → `ScreensBuilder.DefaultLevels`.
- **Training**: `SO_Mission_Training` ora ha i 3 criteri (Positivo / Parziale / Negativo). `SCN_Training` non esiste: la crea il team proprietario del training (vedi guida §9).
- **Validatore**: le missioni di esempio (`(esempio)` in `DesignerNote`) senza scena danno un **avviso**, non un errore; il Training non avvisa più per i livelli Operational / Crisis. `Valida tutti gli scenari` = 0 errori, 9 avvisi (8 scene di esempio + SCN_Training).
- **Smoke test Play Mode (SCN_Core)**: Home → pagina 2 → missione 99 → Guided → Avvia → decisione → Debrief → Score & Replay ("Punteggio sicurezza 92/100") → Home; scena scenario scaricata, sessione chiusa, nessun errore. Chiave `si81.progress` creata dal test e rimossa.

### SO_HUDIcons (`HUDIconLibrary`) — v0.7.0
- Libreria generata da `SI81/UI/Build HUD`: tutte le `ICN_` per nome (con o senza prefisso).
- Serve a `HUDService.ShowEventAlert` e `LabelAnchor` quando l'icona arriva come testo.
- Non si modifica a mano: riesegui Build HUD dopo aver aggiunto icone.

## v0.6.0 — 2026-11-18 — Blocco D: kit scenario, SCN_Core, scenario finto e validatore

Proprietario: **Team UI — Romario Gjoka**. Versione: **0.6.0**. Menu `SI81/Scenari/Crea kit scenario` (rieseguibile, GUID invariati), `Nuovo scenario...`, `Valida scenario`, `Valida tutti gli scenari`. Build Settings: `SCN_Core` all'indice 0, poi `SCN_MockScenario` (voci `SampleScene` mancanti rimosse; reversibile da File > Build Profiles).
- **Rifattorizzazione**: `BaseBuilder.ConfigurePoseDriver` / `HasSingleBinding` sono `internal` e riusati da `ScenarioKitBuilder`; `SceneMissionLauncher.ShowHome` rimette il menu davanti alla testa dopo lo scaricamento dello scenario (l'origine del rig torna alla posa originale).
- **Correzione**: il builder ricarica gli asset dal percorso dopo ogni `OpenScene(Single)` (Unity scarica gli asset non referenziati e la missione risultava vuota nella scena).
- **Validatore**: `MissionFlowDriver` resta tra i tipi "solo SCN_Core"; il `ScenarioController` è riconosciuto anche come sottoclasse; i testi `[DA COMPILARE]` e `SO_Mission_Template` / `SCN_ScenarioTemplate` danno solo avvisi.

### ScenarioController, ScenarioTrigger — v0.6.0
- Su `MNG_Scenario`: erediti da `ScenarioController` e in `OnScenarioStarted()` avvii la logica.
- Metodi: `SetObjective`, `CompleteObjective`, `SetPhase`, `Alert`, `Feedback`, `Subtitle`, `AskDecision`, `EndMission(esito, punteggio 0-100, 5 dimensioni, righe di debrief)`.
- Nessuna UI, camera o rig nella scena; `Play` nella tua scena carica SCN_Core da solo (solo Editor).

### MockScenario (SCN_MockScenario, SO_Mission_MockScenario, Id 99) — v0.6.0
- Scenario finto di ~30 s: sottotitolo, feedback, avviso, decisione a 2 opzioni (Positivo 92 / Parziale 55 / Negativo 20 se scade).
- Serve a provare Home (pagina 2) → Mission Brief → scenario → Debrief → Score & Replay → Home.
- Da togliere dal catalogo alla consegna (`DesignerNote` contiene "(esempio)").

### SCN_Core (+ CoreRig) — v0.6.0
- Scena persistente: rig segnaposto `MNG_CoreRig`, `MNG_UI` con `useMocks = false`, `MNG_HUD`; prima nelle Build Settings.
- `CoreRig` passa testa e mani a `UIScreenManager.SetHead/SetTrackingOrigin` e `HUDRoot.SetRig`: il rig vero di VR Interaction System metterà lo stesso componente sul suo prefab.
- Non rinominarla e non spostarla dall'indice 0.

### SCN_ScenarioTemplate — v0.6.0
- Modello da copiare solo col menu `Nuovo scenario...` (luce, pavimento, `MNG_SpawnPoint`, `MNG_Scenario`, 3 `LabelAnchor`, `MNG_DecisionTrigger`).
- Sostituisci ambiente e oggetti di esempio; non aggiungere Canvas, EventSystem, camera o rig.
- Non è nel catalogo né nelle Build Settings (il validatore dà solo avvisi).

### SO_Mission_Template — v0.6.0
- Missione modello con testi `[DA COMPILARE]` (Id 00, fuori dal catalogo).
- La copia il menu `Nuovo scenario...` con numero, titolo e `SceneName` del tuo team.
- Compila titolo, contesto <= 280, icona, 2-4 obiettivi (~30 caratteri), 3 criteri, complessità.

### Validatore scenari (`ScenarioValidator`, `ValidationWindow`) — v1.0
- `SI81/Scenari/Valida scenario` (scena aperta) o `Valida tutti gli scenari` (catalogo).
- Errori in rosso da correggere prima della consegna; avvisi consigliati; clic su un messaggio seleziona l'oggetto.
- Controlla asset missione, nome e Build Settings della scena, UI fatta a mano, `LabelAnchor`, chiavi di testo.

### Kit scenario (`ScenarioKitBuilder`) — v0.6.0
- `SI81/Scenari/Crea kit scenario`: dopo Build Base e Build HUD; crea SCN_Core, template, scenario finto e Build Settings.
- Rieseguibile: riallinea gli asset, non crea doppioni né cambia i GUID.
- `SI81/Scenari/Nuovo scenario...` crea scena e missione del team in `Assets/_Project/Groups/<gruppo>/Scenari/<Titolo>/`.

## v0.5.1 — 2026-11-18 — Controllo e correzioni di fine Blocco C

Proprietario: **Team UI — Romario Gjoka**. Versione dei prefab HUD / pausa / fine: **0.5.1** (nessun cambio di aspetto). Rieseguiti Build Screens e Build HUD: GUID invariati (378 meta), 45 prefab, nessun doppione.
- **Obiettivi di esempio** (`ScreensBuilder.Data.cs`): 11 righe accorciate a <= 30 caratteri (significato invariato), così il pannello da polso non emette più l'avviso `LBL_ObjectiveItem`.
- **Commenti**: aggiunti i `/// <summary>` mancanti (campi di `AlertPresenter.Pending`, `SubtitlePresenter.Entry`, adattatore di `MissionFlow`; 11 voci).
- **Verifiche senza modifiche**: nomi (prefab, figli, `CANVAS_WristPanel`/`CANVAS_Subtitles`), nessun Animator / Shadow / Outline / Layout Group / GraphicRaycaster nei prefab HUD e di fine; HUD opaca (nessun riferimento al vetro nei builder/runtime HUD; l'intestazione esito su vetro è solo nelle schermate di fine); soglie del tema (-15° / 3 s, -20…-28° / 1 m / 32 × 2, 0,75–4 m, max 3, 1,2 m, 1,5 m, 30% → 100% entro 40 cm) corrette; timer sul polso riscritto solo al cambio del secondo; etichette nel mondo a ~10 Hz; 112 chiavi testo presenti, `UIStrings.Validate()` = 0 problemi, nessun "SPI".
- **Raycaster**: solo `HUD_DecisionPanel` e `HUD_ContextHelp` (pallino cliccabile) hanno `UIWorldRaycaster`; gli altri canvas HUD no (sola lettura).
- **Eccezione accettata (§9)**: soglie angolari del polso (50°/45°/10°), velocità e tolleranze di `LazyFollow`, impulso del timer e misure Figma dei prefab HUD restano serializzate nei componenti / costanti dei builder, non in `SO_UITheme`.
- **Avviso "referenced script (Unknown) is missing"**: nessun riferimento orfano in `Assets/` (45 prefab, tutti gli asset e le scene controllati per GUID). L'origine sono i prefab dei Building Blocks multiplayer del pacchetto Meta XR Core SDK (NGO / Photon Fusion, pacchetti non installati) caricati dal Project Setup Tool a ogni ricarica del dominio. Non è codice SI81: nessuna azione.

## v0.5.0 — 2026-11-18 — Blocco C: HUD in scenario, etichette nel mondo, decisione/esito, pausa e schermate di fine

Proprietario: **Team UI — Romario Gjoka**. Versione dei prefab del blocco: **0.5.0**. Catena: Build Theme → Build Base (Componenti → pannelli → dati → MNG_UI → Build Screens → **Build HUD** → SCN_UISandbox); **SI81/UI/Build HUD** gira anche da solo (ordine: etichette nel mondo, decisione/esito, rig HUD con MNG_HUD, schermate di pausa e fine). Rieseguibile, GUID invariati.
- **Eventi di sessione**: `MissionSession.Started` (lanciato da `Begin`) sostituisce il polling in `HUDRoot` e `MissionFlowDriver`.
- **Lanciatori**: `SceneMissionLauncher` scarica la scena di scenario ancora caricata prima di avviarne una nuova (Ricomincia / Riprova / Cambia livello), la imposta come scena attiva al caricamento e rimette attiva la scena core al ritorno a Home; `MockMissionLauncher` riparte con tempo normale.
- **Sandbox**: `SCN_UISandbox` contiene `MNG_HUD`, le 4 schermate di pausa/fine sotto `PANEL_Screens` e `UISandboxRig` (passa testa e controller a `HUDRoot.SetRig` e allinea `UIScreenManager.SetHead` / `SetTrackingOrigin`). `MNG_UI` ha `MissionFlowDriver`.
- **Token e testi**: 19 token numerici in `project.numbers` (etichette nel mondo, decisione, esito); 3 chiavi (`ui.training.learned.pause`, `ui.score.safety_label`, `ui.score.out_of_100`).
- **Correzione**: Build HUD non rigenera più `TEX_PillRounded` con `radius.pill` 999 (usciva 4096 × 4096 e rompeva l'atlante): lo sprite resta quello di Build Theme.

### HUD_WristPanel, HUD_EventAlert, HUD_ActionFeedback, HUD_Subtitles, MNG_HUD — v0.5.0 (`HUDRoot`, `HUDService`, `WristPanel`, `AlertPresenter`, `SubtitlePresenter`)
- Non si mettono in scena a mano: stanno in `MNG_HUD` (rig condiviso di SCN_Core, o sandbox). `HUDService.SetObjective("...")` / `SetObjectiveIndex(n)`, `SetPhase("hud.phase.action")`, `StartTimer(90)`; obiettivi ~30 caratteri per riga. Parte nascosto e si mostra da solo all'inizio missione.
- `HUDService.ShowEventAlert("Titolo\nDettaglio", "ICN_NotificationsActive_Filled")`, `ShowActionFeedback("Allarme dato", FeedbackKind.Positive)`: uno alla volta, 3 s, evento prima del feedback, regole per livello automatiche, poi l'ultima riga resta sul polso.
- `HUDService.ShowSubtitle("Marco", "testo")` (32 caratteri × 2 righe, pagine automatiche); `ShowSoundEvent("Allarme antincendio")` per i suoni critici. Il polso sta sul controller NON dominante (`controls.dominant_hand`) e va dal 30% al 100% quando è rivolto agli occhi entro ~40 cm.

### LabelAnchor, HUD_RiskLabel, HUD_InteractionPrompt, HUD_ContextHelp (MNG_WorldLabels) — v0.5.0 (`WorldLabelManager`)
- Aggiungi `LabelAnchor` a un oggetto, scegli Risk / Interaction / ContextHelp, scrivi titolo e dettaglio (o chiavi): compare da solo tra 0,75 e 4 m.
- Visibilità dal livello: Risk Guided/Operational (max 3, i più vicini), ContextHelp solo Guided, Interaction sempre. `Suppressed = true` lo nasconde.
- Le etichette non stanno nella scena dello scenario: le istanzia `MNG_WorldLabels` (in `MNG_HUD`).

### HUD_DecisionPanel — v0.5.0 (`DecisionPanel`)
- `decisionPanel.Show(domanda, new[]{...2–4 testi}, i => ..., secondiLimite)`: si apre 1,2 m davanti alla testa, si sceglie col raggio.
- `-1` = tempo scaduto (`DecisionPanel.TIMEOUT_INDEX`); impulso aptico sulla mano dominante alla scelta.
- Domanda e opzioni sono dati dello scenario (non stanno in `SO_UIStrings_It`).

### HUD_OutcomePanel — v0.5.0 (`OutcomePanel`)
- `outcomePanel.Show(risultato, () => ...)`: 3–5 s al centro (1,5 m), poi `onDone`.
- Lo chiama `MissionFlowDriver` a fine missione: gli scenari non lo usano direttamente.
- Motivo: parametro, altrimenti criterio dello scenario, altrimenti prima riga del debrief.

### PANEL_Pause — v0.5.0 (`PauseScreen`)
- Si apre da sola col tasto menu sinistro (P in Editor) a missione in corso: nasconde la HUD e mette `Time.timeScale = 0` (animazioni UI su tempo non scalato).
- Riprendi / Ricomincia (conferma) / Impostazioni rapide / Esci alla Home (conferma, nessun salvataggio).
- Gli scenari non fanno nulla; la HUD scrive `MissionFlow.CurrentPhaseKey` e `ObjectiveIndex` per la pillola Fase e gli obiettivi.

### PANEL_Debrief, PANEL_ScoreReplay, PANEL_TrainingComplete — v0.5.0 (`DebriefScreen`, `ScoreReplayScreen`, `TrainingCompleteScreen`, `ResultHeader`)
- Si aprono da sole dopo `MissionSession.End(result)`: esito 3–5 s, poi Debrief → Score & Replay → Home (Training: PANEL_TrainingComplete dopo 0,5 s, nessun esito).
- Righe debrief con prefissi `DebriefLinePrefix` ("+ ", "- ", "! "); il "percorso corretto" sono gli obiettivi dello scenario.
- "Punteggio sicurezza N/100" + stelle (= rank) + 5 barre delle dimensioni con delta sul tentativo precedente; mai "SPI".

### MissionFlow, MissionFlowDriver, PauseInput — v0.5.0
- Un solo `MissionFlowDriver` su `MNG_UI`; ascolta `MissionSession.Started` / `Ended`.
- Punti d'ingresso comuni: `MissionFlow.Pause/Resume/RestartMission/ExitToHome`; `IOutcomePresenter` per un pannello esito alternativo (HUD_OutcomePanel è già adattato).
- Bottom bar nascosta per tutta la missione; riappare dopo `MissionSession.Clear`.

### Scelte e punti aperti (v0.5.0, PROVVISORI)
- Wrist: scala 340 × ~198 unità = 0,12 m; posa (0, 0,05, −0,04) m e inclinazione 70° da tarare sul visore; soglie angolari 50° / 45°.
- Il HUD non ha raycaster (sola lettura): deroga alla lettera di CLAUDE.md §3 per i canvas non interattivi; solo HUD_ContextHelp e HUD_DecisionPanel hanno `UIWorldRaycaster`.
- Inviti all'azione sempre visibili (Figma: Guided su mira, Operational dopo 0,5 s, Crisis mai): da decidere con VR Interaction System.
- Durata esito = campo `outcomeSeconds` (4) su `MissionFlowDriver`; "percorso corretto" = obiettivi ordinati (nessun campo dedicato); pausa breve = pressione (la Figma cita "tenuto 1 s").
- Pausa/Training completato usano `PANEL_Menu` 760 × 400 (larghezza dal tema), titolo a sinistra; nessun suono (slot `SFX_*` vuoti, null-safe); `…` sostituito da `..` nei sottotitoli evento troppo lunghi.
- Token HUD opzionali proposti da C1 (soglie del polso, aptica, velocità dei sottotitoli) non aggiunti: restano serializzati nei componenti.

## v0.4.1 — 2026-11-18 — Controllo e correzioni di fine Blocco B

Proprietario: **Team UI — Romario Gjoka**. Versione di dati e schermate: **0.4.1** (nessun cambio di aspetto). Rieseguiti Build Theme, Build Base, Build Screens: GUID invariati, nessun doppione.
- **Punteggio da token**: `ScoreRules` legge `score.stars.N.min` e `badge.fast_run.seconds` dal tema (`ScoreRules.UseTheme`, chiamato da `UIServicesBootstrap`, che ha ora il campo `theme`); `StarsMin(n)` e `FastRunSeconds()` sostituiscono le costanti pubbliche `STARS_N_MIN` e `ProgressServiceBase.FAST_RUN_SECONDS` (rimosse). Senza tema (prove in Editor) valgono i valori di riserva, identici ai token.
- **Singolare**: nuove chiavi `ui.stats.attempts_max_level_one` ("1 tentativo · livello max:") e `ui.stats.average_line_one` ("1 scenario giocato su N"); StatsScreen sceglie la chiave dal conteggio.
- **Sessione**: `MissionSession.Clear()` esisteva già e `ReturnToHome` (Scene e Mock) lo chiama; `LastResult` resta valido fino a quel punto (serve a Debrief e Score & Replay). Aggiunta la guardia: una seconda `End(result)` nella stessa sessione è ignorata con un avviso (evita il doppio conteggio).
- **Nomi (§2)**: `PANEL_RecentRow_N` → `PANEL_RecentRowN` (il builder rinomina le vecchie righe, nessun doppione); righe create a runtime `BTN_ListRowNN` / `BTN_BadgeNN`.
- **Documentazione (§9)**: `/// <summary>` in italiano sui campi pubblici di `LevelConfig`, `MissionCatalog`, `MissionDefinition`.
- Verificati senza modifiche: nessun testo italiano fisso nelle schermate (solo `SO_UIStrings_It`; il nome profilo e i contenuti `SO_Mission_*` sono dati), nessuna sigla "SPI", Mission Brief senza durata/punteggio, 6 schede e max 7 righe, niente Layout Group/Animator/Shadow/Outline nei 6 prefab, grafiche decorative con `raycastTarget` spento.
- Prova in Play Mode (sandbox): Home → card → Mission Brief → livello → Avvia (mock) → `End` → `ReturnToHome` → Statistiche/Profilo/Impostazioni dalla barra → Indietro: ok.

## v0.4.0 — 2026-11-18 — Blocco B: dati, servizi e 6 schermate menu

Proprietario: **Team UI — Romario Gjoka**. Versione di dati e schermate: **0.4.0**. Catena: Build Theme → Build Base (esegue Componenti → pannelli → dati → MNG_UI → **Build Screens** → SCN_UISandbox); **SI81/UI/Build Screens** gira anche da solo. Rieseguibile, GUID invariati.
Nuovi `UIScreenId` in fondo (valori stabili): Pause 7, Debrief 8, ScoreReplay 9, TrainingComplete 10 (schermate del Blocco C). `UINavigationContext.StatsSelectedIndex` parte da -1. `BackButton` ha l'opzione `inCardLook` (fondo `tile.neutral` dentro una scheda bianca). `MNG_UI` contiene `UIServicesBootstrap` (catalogo = SO_MissionCatalog): nel prefab `useMocks = false` (PlayerPrefs + caricamento scene), nell'istanza di SCN_UISandbox `useMocks = true` (MockProgressService, MockSettingsStore, MockMissionLauncher). Il tasto N della sandbox apre la schermata reale N-1 (1-6); le cifre senza schermata sono ignorate. Nuove chiavi: `ui.home.page`, `ui.brief.objective_item`; nuovi token numerici `score.stars.N.min`, `badge.fast_run.seconds` (documentano le soglie; le costanti vive restano in `ScoreRules`/`ProgressServiceBase`).

### Dati scenario (`SO_Mission_*`, `SO_MissionCatalog`, `SO_LevelConfig_*`) e servizi (`UIServices`, `UIServicesBootstrap`) — v0.4.0
- Crea `SO_Mission_<Titolo>` (Create > SI81 > UI > Mission Definition: id, titolo, contesto max 280, obiettivi max 4, criteri, `SceneName`), aggiungilo a `SO_MissionCatalog.Missions` e `SCN_<Titolo>` alle Build Settings: Home, Brief, Statistiche e HUD lo usano da soli.
- A fine missione chiama `MissionSession.End(result)` (stelle da `ScoreRules.StarsFromScore`): registra i progressi; non chiamare `Progress.RecordResult` a mano (doppio conteggio). Il Training non viene registrato.
- Le schermate leggono sempre `UIServices.*` (Catalog, Progress, Settings, Launcher) e si aggiornano su `Progress.Changed`; `UINavigationContext` porta scenario/livello scelto. Prove: mock + `MockProgressService.ResetToSample()`, `MockMissionLauncher.FabricateResult(...)`.
- Contenuto dati: 8 scenari di esempio (01,02,03,04,07,08,09,10, testi "(esempio)"), `SO_Mission_Training`, 3 `SO_LevelConfig_*` (valori provvisori: Guided senza timer, Operational 600 s, Crisis 300 s), 12 badge.

### PANEL_Home — v0.4.0 (`HomeScreen`, id Home)
- Griglia di 8 BTN_ScenarioCard da `Catalog.Playable` + PANEL_TrainingCard; oltre 8 scenari compaiono le frecce di pagina (`ui.home.page`).
- Mostra stelle migliori ("Nuovo" se mai giocato), complessità e livelli completati; mai il punteggio.
- Clic card = scenario e livello (ultimo giocato o Guided) in `UINavigationContext` e apertura di Mission Brief; clic sul training = PANEL_TrainingBrief.

### PANEL_MissionBrief — v0.4.0 (`MissionBriefScreen`, id MissionBrief)
- PANEL_Menu 760 x 535 + PANEL_Side "Criteri di completamento"; titolo, contesto, obiettivi, criteri e livelli sbloccati vengono da `SO_Mission_*`; nessuna durata né punteggio.
- Scelta livello con BTN_DifficultyCard (spunta); "Avvia scenario" disabilitato ("Scegli un livello") finché non è scelto.
- Avvia = `UIServices.Launcher.Launch(mission, level)`; BTN_Back torna indietro.

### PANEL_Stats — v0.4.0 (`StatsScreen`, id Stats)
- Elenco BTN_ListRow per scenario (da catalogo) + dettaglio: riepilogo (4 PANEL_StatTile + media) oppure scenario ("Punteggio sicurezza N/100", stelle, ultimo tentativo, 5 LBL_DimensionBar, tentativi e livello max). Selezione in `UINavigationContext.StatsSelectedIndex`.
- Scenario mai giocato = "Non giocato"; si aggiorna a ogni `Progress.Changed`.
- Mai la sigla "SPI"; esito dell'ultimo tentativo = `LBL_Outcome` a colori pieni (card bianca, §4.9).

### PANEL_Profile — v0.4.0 (`ProfileScreen`, id Profile)
- Identità (nome da PlayerPrefs `si81.profile.name`), missioni completate, ultimi 3 risultati, "Punteggio sicurezza N/100" complessivo con stelle, 5 competenze (medie), griglia badge con scheda di dettaglio al clic.
- Dati da `UIServices.Progress` (risultati, totali, `GetBadges()`); profilo unico, niente account/logout (§8).
- Nome profilo provvisorio "Operatore_Luca" (campo `defaultProfileName`).

### PANEL_Settings — v0.4.0 (`SettingsScreen`, `SettingsCatalog`, id Settings)
- 6 schede (Comfort, Audio, Sottotitoli, Controlli, Accessibilità, Generale), max 7 righe per scheda; righe e id da `SettingsCatalog` (nuova impostazione = nuova riga nel catalogo + Build Screens). Mano dominante: `SettingsCatalog.DOMINANT_HAND` (0 destra, 1 sinistra) per HUD_WristPanel.
- Valori solo da `UIServices.Settings`, mai PlayerPrefs diretti; scheda aperta in `UINavigationContext.SettingsTab`.
- "Ripristina impostazioni" apre PANEL_ConfirmDialog (conferma = valori predefiniti). Eventi statici `SettingsScreen.RecalibrateRequested` / `InfoRequested` per rig e contenuto Informazioni.

### PANEL_TrainingBrief — v0.4.0 (`TrainingBriefScreen`, id TrainingBrief)
- Si apre da PANEL_TrainingCard (Home) o da Impostazioni > Rivedi training; BTN_Back torna indietro.
- "Avvia training" = `UIServices.Launcher.LaunchTraining(UIServices.Catalog.Training)`.
- Testi fissi `ui.training.*`; nessuna configurazione a mano.

### Decisioni e punti aperti (v0.4.0)
- Brief di Blocco B vs FIGMA_SPECS: Operational mostra le risk label (da confermare con la Guida HUD). Soglie stelle e badge "rapido" provvisorie.
- Home: frecce di pagina proprie (`BTN_PagePrevious/Next`) invece di BTN_Secondary. Mission Brief: complessità sulla riga del percorso. "Scenario non disponibile" = senza `SceneName` o fuori Build Settings (solo build).
- Stats/Profile: nessuno scorrimento oltre 8 scenari / 12 badge (decisione di design aperta); "1 tentativi" senza plurale; scheda dettaglio badge non progettata in Figma; "Informazioni > Apri" non progettato.
- Impostazioni: slider opacità sottotitoli 92-100 (da rivedere); "Rivedi training > Avvia" apre il brief.
- Il Training non viene registrato nei progressi. Le scene SCN_<Titolo> degli esempi non esistono: con i mock non servono; il validatore (passo 7) le controllerà.

## v0.3.1 — 2026-11-18 — Controllo e correzioni di fine Blocco A

Proprietario: **Team UI — Romario Gjoka**. Nessun cambio di API o di aspetto dei componenti (versione componenti: 0.3.1).
- **Indietro doppio corretto**: `MenuPanel` non collega più BTN_Back (rimossi `WireBackButton` e `OnBackPressed`); il clic è gestito solo da `BackButton` (una sola chiamata a `UIScreenManager.Back`). Il pannello mostra/nasconde soltanto il posto `PANEL_BackSlot`.
- **Nomi (§2) in SCN_UISandbox**: `SandboxBackdrop` → `MESH_SandboxBackdrop`; rig `XROrigin/CameraOffset/CameraHead/LeftController/RightController` → `MNG_SandboxRig/MNG_RigOffset/MNG_RigHead/MNG_RigLeftHand/MNG_RigRightHand`; `Reticle` → `VFX_PointerReticle`; celle galleria `PANEL_GalleryCell0_1` → `PANEL_GalleryCellP0N1` (niente underscore interni). I builder rinominano gli oggetti esistenti (nessun duplicato); le vecchie celle galleria vengono ricreate.
- **Documentazione (§9)**: aggiunti `/// <summary>` in italiano ai membri pubblici di `UIStateVisual` e `SettingsOption`.
- Verificati senza modifiche: nessun Animator, Shadow/Outline, Layout Group/ContentSizeFitter a runtime, Screen Space, TMP_Dropdown, tipi XRI/OVR; un solo EventSystem; solo `UIWorldRaycaster` sui Canvas SI81; nessuna sigla "SPI" a schermo; testi solo da chiavi.

## v0.3.0 — 2026-11-18 — Passo 3 (Blocco A): 20 componenti e galleria nella sandbox

Proprietario: **Team UI — Romario Gjoka**. Versione di tutti i componenti: **0.3.0**. Prefab in `Prefabs/Components/` da **SI81/UI/Build Components** (eseguito anche da **SI81/UI/Build Base**; catena completa = Build Theme → Build Base).
Ordine di `ComponentsBuilder.BuildAll`: Pulsanti → Etichette → BTN_ListRow (annida LBL_StarRating) → Righe impostazioni → Card (annidano BTN_Primary e LBL_LevelBadge). Rieseguibile: GUID invariati, nessun duplicato. Colori, misure, font e durate solo da `SO_UITheme`; testi solo da `SO_UIStrings_It`.
Stati comuni (CLAUDE.md §4.1) in un solo punto, `UIStateVisual`: hover scala 1,04 + ombra, pressed 0,98, selected = ombra più profonda + glow `brand.light` + spunta, nessun bordo. Nuovo `UIStateVisual.ForceState(stato)` mostra uno stato senza puntatore (galleria e prove).

### Galleria componenti (solo SCN_UISandbox)
- Nuova schermata `UIScreenId.ComponentGallery` (valore 6; **tasto 7** o pulsante "Galleria componenti" del canvas di debug): PANEL_Menu con 11 pagine e le 20 istanze dei prefab, ognuna con la didascalia dello stato (Normal / Hover / Pressed / Selected / Disabled dove esiste). Pulsante "Avanti" per cambiare pagina; PANEL_ConfirmDialog si apre dal pulsante "Ricomincia" (pagina 3) e dall'azione "Ripristina" di Impostazioni (pagina 7).
- Dati finti e stati sono applicati a runtime da `UISandboxGallery` (nessun dato finto nei prefab); le righe di Impostazioni sono collegate a un `MockSettingsStore`. Le didascalie degli stati sono testo di debug (eccezione ammessa: solo sandbox, non entrano in SO_UIStrings_It). Nuova chiave `ui.sandbox.gallery`.
- Builder: `Editor/GalleryBuilder.cs`, chiamato da `BaseBuilder.BuildPlaceholderScreen`.

### BTN_Primary — v0.3.0 (`BTN_Primary.prefab`, script `UIButton`)
- Uso: istanza; `SetLabelKey(key)`, `SetIcon(sprite)`, `SetSurface(Dark|Light)` (Light sulle card bianche: bordo `border.on-light`); `Interactable = false` mostra il motivo (`ui.button.choose_level`). Evento `Clicked`.
- Stati: Default / Hover / Pressed / Disabled (fill `button.disabled`, niente bordo né icona, testo nero). 200×48, `state.active`, bordo 2 mantenuto (CLAUDE §4.2, punto aperto), testo sempre nero.
- Il PANEL_TrainingCard annida un BTN_Primary Light con etichetta `ui.button.start_training`.

### BTN_Secondary — v0.3.0 (`UIButton`)
- Uso: `SetStyle(Secondary|Danger, superficie)`, `SetLabelKey`, `SetIcon`; larghezza secondo il contenuto.
- Stati: Default / Hover (`tile.neutral`) / Pressed / Disabled. Danger = fill `deny`, testo NERO.
- Un solo prefab: il Danger è lo stile (usato nel dialogo con ICN_Exit).

### BTN_Back — v0.3.0 (`BackButton`)
- Uso: nel posto PANEL_BackSlot di PANEL_Menu; il clic chiama `UIScreenManager.Back()`. Nasconderlo sulle radici spetta al pannello.
- Stati: Default (`surface.card`, senza ombra) / Hover (`tile.neutral` + scala + ombra) / Pressed. 40×40, ICN_ArrowBack 24.
- SFX_UIBack: da collegare nel passo audio.

### BTN_Badge — v0.3.0 (`BadgeButton`)
- Uso: `SetData(unlocked, filledIcon)`; `Clicked` apre il dettaglio (non ancora progettato).
- Sbloccato = cerchio `brand.light` + icona piena `brand.dark`; Bloccato = quadrato `tile.neutral` + ICN_Lock + "Bloccato".
- Hover/Pressed standard (UIStateVisual).

### BTN_SettingsTab — v0.3.0 (`SettingsTabButton`)
- Uso: 6 istanze, `SetData(outline, filled, labelKey)`; `Active = true` sulla scheda corrente; il pannello ascolta `Clicked`.
- Stati: Default (trasparente) / Hover (fondo `brand.dark` come Figma + scala + ombra: il solo fondo sarebbe invisibile sul vetro) / Active (fondo `surface.card`, icona piena, Body Bold `brand.dark`, senza bordo) / Disabled.
- Max 7 righe per scheda: lo decide PANEL_Settings.

### BTN_ListRow — v0.3.0 (`ListRowButton`)
- Uso: `SetData(titolo, stelle, punteggio)` o `SetNotPlayed(titolo)`; `Selected = true` per la riga scelta. Stelle = LBL_StarRating annidato. Chiave nuova `ui.format.score_of_100` ("{0}/100").
- Stati: Default / Hover (`tile.neutral`) / Selected (`brand.light` + sollevamento + glow + spunta compatta 22, scelta di progetto oltre il Figma) / Non giocato (testo `text.secondary`).
- 440×40.

### PANEL_ConfirmDialog — v0.3.0 (`ConfirmDialog`)
- Uso: `dialog.Show(titleKey, messageKey, confirmKey, cancelKey, onConfirm, onCancel)`; `Hide()`. Salvato disattivo; il fuoco va su "Annulla".
- Modale: IMG_Backdrop (`color.dialog.backdrop`, 840×627) blocca i raggi; si porta sopra i fratelli del Canvas. Conferma = BTN_Secondary Danger (ICN_Exit), Annulla = BTN_Primary Dark (ICN_Undo).
- Chiavi nuove `ui.dialog.restart.*`, `ui.dialog.reset_defaults.*`. Niente blur in tempo reale; l'ombra riusa `TEX_PanelShadow`.

### LBL_LevelBadge — v0.3.0 (`LevelBadge`)
- Uso: `SetLevel(2)` / `SetLevel(DifficultyLevel.Crisis)`; `SetSurface(LabelSurface.Dark)` su fondo scuro. È l'unico modo di mostrare il livello ("Livello N" Bold).
- Struttura: 3 barre a mesh (`RoundedRectGraphic`, piene = livello), "Livello N" Body Bold + " · Nome" Body Regular, da `ui.level.badge`. Larghezza adattata al testo.
- Varianti Light (`level.bar.on-light/off-light`, testo nero) / Dark (`on-dark/off-dark`, testo bianco).

### LBL_StarRating — v0.3.0 (`StarRating`)
- Uso: `SetStars(4)` oppure `SetStars(4, false)` per nascondere "4/5". Le stelle sono il grado (rank) del punteggio sicurezza.
- 5 × ICN_Star (24, spazio 2, `ICN_Star_Filled` piena / `ICN_Star` vuota, `text.primary`) + valore "N/5". Solo superfici chiare.
- Valore 0–5 (limitato).

### LBL_DimensionBar — v0.3.0 (`DimensionBar`)
- Uso: `SetDimension(DimensionKind.Awareness, 86)`, `SetValue(70)`, `SetSurface(LabelSurface.Light)`. Mai la sigla "SPI".
- Nome (88, `ui.dimension.<nome>`), pista pillola con contorno 1, riempimento = valore/100, valore 0–100 senza %. Larghezza elastica (272), altezza 23.
- Dark (`brand.light` su `level.bar.off-dark`, testo bianco) / Light (`brand.dark` su `off-light`, testo `text.primary`).

### PANEL_StatTile — v0.3.0 (`StatTile`)
- Uso: `SetStat(StatAccent.Positive, 5)` oppure `SetValue(n)`. Sempre icona + etichetta + valore.
- 112×96, padding 12/8; accenti Positive (`confirm`), Partial (`brand.dark`), Negative (`deny`, testo nero), Neutral (`tile.neutral`).
- Etichette da `ui.stats.*`.

### LBL_Outcome — v0.3.0 (`OutcomeLabel`)
- Uso: `SetOutcome(OutcomeType.Partial)`, `SetPresentation(OutcomePresentation.OnGlass)`, `SetSize(LabelSize.Large)`.
- OnCard = colore pieno dell'esito, SOLO su card bianche; OnGlass = pillola bianca + icona colorata + testo nero, per intestazioni vetro (CLAUDE §4.9).
- Small h 32 (Body Bold) / Large h 60 (Title Bold, HUD_OutcomePanel); larghezza adattata al testo.

### LBL_ObjectiveItem — v0.3.0 (`ObjectiveItem`)
- Uso: `SetObjective("testo", ObjectiveState.Current)`, `SetObjectiveKey(key, state)`; con `wristMode` avvisa se una riga supera `Hud.WristObjectiveMaxChars` (~30, CLAUDE §4.10).
- Stati: Done (check pieno, testo `text.on-green` barrato), Current (sfondo `brand.light`, Body Bold nero), Todo (cerchio vuoto, testo bianco).
- Larghezza 260, altezza adattata (min 36).

### PANEL_SettingsToggle / _Slider / _Selector / _Action — v0.3.0 (`SettingsToggleRow`, `SettingsSliderRow`, `SettingsSelectorRow`, `SettingsActionRow`)
- Uso: `SetRowData(id, labelKey, descKey)` + `SetToggleData / SetSliderData / SetSelectorData / SetActionData`, poi `Bind(IUISettingsStore)` (`PlayerPrefsSettingsStore` nel gioco, `MockSettingsStore` in sandbox). `ResetToDefaults()` rilegge tutte le righe. Feedback = evento statico `UISettingsRow.FeedbackRequested` (SFX_UIToggle, SFX_UIError, HAPTIC_Tick).
- Righe 516×48, `surface.card`, `radius.button`; disabilitata = 40%. Toggle ON/OFF scritto (traccia `state.active`); slider con presa alta 40 e tick ogni 10; selettore con frecce 40 (niente TMP_Dropdown); azione Button (con conferma opzionale via PANEL_ConfirmDialog) o Value (sola lettura + lucchetto).
- Il numero di righe per scheda (max 7) lo decide PANEL_Settings. Token `size/padding/gap.settings-*`, `opacity.settings-disabled`, `step.settings-slider.tick`.

### BTN_ScenarioCard — v0.3.0 (`ScenarioCard`, 166×140)
- Uso: `card.Set(new ScenarioCardData { Id, Title | TitleKey, Icon, Image, GradientStart/End, BestStars (null = "Nuovo"), Complexity 1–5, LevelsCompleted 0–3, Unlocked })`; evento `Clicked(string id)`.
- Stati: Default / Hover / Pressed / Disabled (`Unlocked = false`: 40%, pillola con lucchetto + "Non disponibile"). Nessun bordo. Solo stelle, mai il punteggio.
- Gradiente segnaposto = colori della missione, altrimenti `brand.dark` → `level.bar.on-light`.

### PANEL_TrainingCard — v0.3.0 (`TrainingCard`, 712×56)
- Uso: iscriversi a `Clicked` (striscia o pulsante) e aprire `PANEL_TrainingBrief`; contiene BTN_Primary Light annidato ("Avvia training").
- Stati: Default / Hover (1,02) / Pressed (0,99) tramite `HoverScaleOverride/PressedScaleOverride`.
- Punto aperto: è cliccabile, il prefisso corretto sarebbe `BTN_TrainingCard`; si tiene `PANEL_` come da CLAUDE §2.

### BTN_DifficultyCard — v0.3.0 (`DifficultyCard`, 226×117)
- Uso: `card.Set(DifficultyLevel.Operational, "ui.level.operational_desc", locked: false); card.Selected = true;` evento `Clicked(DifficultyLevel)`. La selezione esclusiva la gestisce PANEL_MissionBrief.
- Stati: Default / Hover / Pressed / Selected (sollevamento + glow `brand.light` + cerchio bianco 30 con spunta nel colore del livello, in alto a destra) / Locked (40%, non cliccabile).
- Badge livello = LBL_LevelBadge annidato (Light).

### Correzioni di integrazione (compilazione e prefab)
1. **Ogni classe MonoBehaviour/Graphic nel proprio file** (Unity non risolve lo script di una classe in un file con nome diverso: prefab con "missing script" e salvataggio rifiutato alla seconda esecuzione): `SettingsArrowButton`, `SettingsSliderGrab`, `SettingsHitArea`, `CardLevelBars`, `CardDots` (file rinominato da CardIndicators.cs con lo stesso `.meta`). I builder ora tolgono i componenti con script mancante dai prefab caricati (`ComponentsBuilder.RemoveMissingScripts`).
2. **Sprite senza duplicati** (verifica md5): BTN_Badge bloccato usa `TEX_CardRounded/CardShadowRest/CardShadowLift/CardGlow` (prima copie `TEX_BadgeLocked*` e `TEX_Cards*`); PANEL_ConfirmDialog riusa `TEX_PanelShadow` (prima `TEX_DialogShadow`); le righe Impostazioni riusano `TEX_ButtonRounded` (prima `TEX_SettingsRowRounded`). `TEX_CardRounded` e `TEX_ButtonRounded` sono costruiti da più famiglie sullo stesso percorso (un solo file). Gli sprite nuovi entrano in `TEX_AtlasUI` per cartella (`Sprites/`); `ComponentsBuilder.BuildAll` riallinea l'atlante.
3. PANEL_TrainingCard: il BTN_Primary annidato è impostato con `SetSurface(Light)` e `SetLabelKey` (l'etichetta si imposta sul componente: `UIButton` riscrive il testo a runtime).
4. `Build Base` esegue ora `ComponentsBuilder.BuildAll` (catena: Build Theme → Build Base); `UISandboxDebugSwitcher` gestisce già i tasti 1–9 (7 = galleria).

### Eccezioni accettate e punti aperti (v0.3.0)
- **Misure di layout come costanti pubbliche** nelle etichette (`LevelBadge`, `StarRating`, `DimensionBar`, `StatTile`, `ObjectiveItem`: icone, spazi, padding da Figma): non spostate nei token del progetto in questo blocco; da portare in `tokens_figma.json → project.numbers` se Figma cambia.
- Le didascalie della galleria sono testo di debug fuori da SO_UIStrings_It (solo sandbox).
- Ombra di PANEL_ConfirmDialog = ombra menu (stessa geometria, raggio panel): se il token `dialog` cambia geometria va ricreato `TEX_DialogShadow`.
- Bordo di BTN_Primary (2 unità) e prefisso di PANEL_TrainingCard restano punti aperti; complessità = `level.bar.*` (punto aperto 10 FIGMA_SPECS).
- SFX/aptica delle righe e di BTN_Back: agganciare nel passo audio.

## v0.2.4 — 2026-11-17 — Passo 2: correzioni dalla revisione

Proprietario: **Team UI — Romario Gjoka**. Prefab e scena da **SI81/UI/Build Base**, atlante e sprite da **SI81/UI/Build Theme** (o **SI81/UI/Build Atlas**).
Rieseguibili (verificato: seconda esecuzione di Build Theme + Build Base con prefab, scena, `.meta` e atlante identici byte per byte, GUID invariati, nessun duplicato).
Decisione recepita: **CLAUDE.md §4.13** — hover di BTN_BottomBarItem = pillola `tile.neutral` (Figma), nessuna scala, eccezione a §4.1 solo per questo componente; pressed = scala 0,98; la voce attiva resta senza fondo (§4.3).

### Correzioni
1. **Conflitti di input** (`Runtime/UIInputDiagnostics.cs`): `UIInputDiagnostics.FindConflicts(includeInactive)` restituisce TrackedDeviceRaycaster, EventSystem in più e moduli diversi da `InputSystemUIInputModule`; `WarnOnce()` scrive UN avviso italiano con i percorsi degli oggetti (non lo ripete finché il conflitto è lo stesso). `UIScreenManager` lo chiama a `Start` e a ogni `sceneLoaded`. Il validatore del passo 7 deve riusare `FindConflicts(true)` e `Describe(...)`.
2. **"Indietro" solo a menu visibile**: `UIScreenManager.ShowMenu()` / `HideMenu()` (+ `IsMenuVisible`, evento `MenuVisibilityChanged`) attivano/disattivano CANVAS_Menu e l'azione Back (`UIInputSetup.SetBackEnabled`); `Back()` non fa nulla a menu nascosto. `Show Menu On Start` (default true): una scena che parte già in missione lo mette a false o chiama `HideMenu()`.
3. **Posizionamento**: `PlaceInFront(Transform testa)` / `Recenter()` mettono CANVAS_Menu a `distance.menu` (1,5 m) davanti alla testa, solo imbardata, centro a `height.menu-center` (1,5 m) sopra l'origine del tracciamento, rivolto alla testa; chiamati a `Start` (e di nuovo dopo il primo frame) e a ogni `ShowMenu()` (`Recenter On Show`). Il rig condiviso assegna `SetHead(camera)` e `SetTrackingOrigin(origine)`; senza testa si usa `Camera.main` con UN avviso (`UIRigLocator`, usato anche da `UIInputSetup` e `WorldCanvasCamera`). In sandbox il builder assegna testa e origine.
   Nuovi token (`tokens_figma.json` → `project.numbers`, `NumberTokens`, `UITheme.MenuCanvasSize/MenuDistance/MenuCenterHeight/EyeHeight`): `size.menu-canvas.width` 840, `size.menu-canvas.height` 627, `distance.menu` 1,5 m, `height.menu-center` 1,5 m, `height.eye` 1,6 m (le due altezze e la distanza sono in METRI). Rimosse `UITheme.MENU_DISTANCE_M` / `MENU_CENTER_HEIGHT_M` e le costanti 840/627/1,6 di BaseBuilder.
4. **Prestazioni**: `fillCenter = false` sulle `IMG_Shadow` (PANEL_Menu, PANEL_Side, PANEL_BottomBar: il centro dello sprite ombra è trasparente). Nuovo **Sprite Atlas V2** `Sprites/TEX_AtlasUI.spriteatlasv2` (`Editor/SpriteAtlasBuilder.cs`, menu **SI81/UI/Build Atlas**, eseguito da Build Theme): include le cartelle `Sprites/` e `Icons/` (tutti TEX_ e ICN_, anche futuri), padding 8, nessuna rotazione né tight packing, mipmap, Android ASTC 4×4, incluso nella build. In Play Mode: 1 sola texture 2048×1024 per ombre, vetro e icone. Per questo gli sprite sorgente sono ora **non compressi, senza mipmap, senza override Android** (Unity avvisa altrimenti) e il builder imposta **ProjectSettings/EditorSettings → Sprite Packer Mode = Sprite Atlas V2** (con "Disabled" l'atlante non è usato in Play Mode): unica modifica fuori da `Assets/_Common/UI/`.
5. **Nomi** (migrazione automatica: i builder rinominano i vecchi oggetti, nessun duplicato, stessi fileID): `Screens`→`PANEL_Screens`, `BottomBarSlot`→`PANEL_BottomBarSlot`, `Header`/`BackSlot`/`TitleColumn`/`HeaderRightSlot`/`Content`→`PANEL_Header`/`PANEL_BackSlot`/`PANEL_TitleColumn`/`PANEL_HeaderRightSlot`/`PANEL_Content`, `Items`→`PANEL_Items`, `EventSystem`→`MNG_EventSystem`, `ICN_Icon`→`ICN_ItemIcon`, `LBL_Label`→`LBL_ItemLabel` (bottom bar) e `LBL_DebugLabel` (pulsanti di debug), sandbox `Main Camera`→`CameraHead` (tag MainCamera invariato), `MESH_SandboxBackdrop`→`SandboxBackdrop` (è la sfera integrata, non un asset MESH_). Nessun prefisso in Nomenclatura per camera e parti del rig (`XROrigin`, `CameraOffset`, `CameraHead`, `LeftController`, `RightController`, `Reticle`, `SandboxBackdrop`): restano PascalCase senza spazi, da confermare. Nomi dell'atlante: Nomenclatura non ha un prefisso per gli Sprite Atlas, scelto `TEX_AtlasUI` (è una texture).
6. **Codice duplicato**: nuovo `UITween` (dissolvenza CanvasGroup, scala, colore; tempo non scalato; durate dal tema passate da chi chiama) usato da `UIScreen` e `BottomBarItem` (tolte le tre coroutine copiate); nuovo `UIEditorSupport.ApplyDeferred` al posto del `delayCall` copiato in `OnValidate` di BottomBar, BottomBarItem, MenuPanel, SidePanel; `UISandboxDebugSwitcher` legge `Enum.GetValues` una sola volta.

### Punti aperti (v0.2.4)
- `HideMenu()` disattiva CANVAS_Menu: chi gestisce la missione (VR Interaction System / scenari) deve chiamarlo all'avvio e `ShowMenu()` a fine missione; da confermare dove (SCN_Core).
- A `Start` la posa della testa in visore può non essere ancora valida: il menu viene riposizionato dopo il primo frame; se serve, chiamare `Recenter()` quando il tracciamento è pronto.
- L'atlante contiene anche le icone non ancora usate (76): se pesa, restringere i packable alle icone usate.

## v0.2.3 — 2026-11-17 — Passo 2: raycaster SI81, PANEL_BottomBar e BTN_BottomBarItem

Proprietario: **Team UI — Romario Gjoka**. Sprite da **SI81/UI/Build Theme**, prefab da **SI81/UI/Build Base** (o solo
**SI81/UI/Build Bottom Bar**). Rieseguibili: seconda esecuzione con tutti i file identici byte per byte, GUID invariati, nessun duplicato.

### UIWorldRaycaster — v0.2.3 (`Runtime/UIWorldRaycaster.cs`, sostituisce TrackedDeviceRaycaster + GraphicRaycaster)
- Uso: UN componente su ogni Canvas World Space SI81 (lo mettono i builder: CANVAS_Menu di MNG_UI e CANVAS_SandboxDebug). Non aggiungere mai `TrackedDeviceRaycaster` nella stessa app: se ne esiste uno, Input System gli dà i raggi dei controller e salta questo raycaster.
- Controller: raggio 3D sul piano di ogni grafica con `raycastTarget = true`, rispetta `CanvasGroup.blocksRaycasts`, soglia alfa e maschere (`Graphic.Raycast`), grafiche viste da dietro e oltre `Pointer.RayMaxLength` scartate; mouse in Editor = GraphicRaycaster standard.
- Regola per i prefab: ombre, icone ed etichette dentro i pulsanti `raycastTarget = false`; restano bersagli solo `IMG_Glass` dei pannelli/barra (superficie del pannello: blocca i clic dietro e mostra il reticolo) e le grafiche dei pulsanti. Prova: un raggio attraverso l'ombra di PANEL_Menu sopra PANEL_Side colpisce `PANEL_Side/IMG_Glass` e UIPointer termina nello stesso punto.

### BTN_BottomBarItem — v0.2.3 (`Prefabs/BTN_BottomBarItem.prefab`, script `BottomBarItem`)
- Uso: non istanziarla da sola: vive dentro PANEL_BottomBar; dati per voce = `Screen` (UIScreenId), `Icon Outline` / `Icon Filled` (ICN_X / ICN_X_Filled), `Label Key` (`ui.nav.*`). Larghezza = 10 + 32 + 6 + etichetta Body Regular + 10 (calcolata, uguale anche in stato attivo).
- Stati: Default (contorno + Body, `text.primary`), Hover (pillola `tile.neutral` 44 alta, nessuna scala/ombra, anche sulla voce attiva), Pressed (scala 0,98), Active (icona piena + Body Bold `brand.dark`, nessun fondo). Durate `Motion.HoverDuration` / `PressDuration`, coroutine a tempo non scalato.
- Struttura: `IMG_Hover` (RoundedPanelGraphic su TEX_PillRounded, unico bersaglio dei raggi), `ICN_Icon`, `LBL_Label` (UIStringText); `Button` senza transizione; il clic esce con l'evento `Clicked`.

### PANEL_BottomBar — v0.2.3 (`Prefabs/PANEL_BottomBar.prefab`, script `BottomBar`)
- Uso: UNA istanza, già dentro `MNG_UI/CANVAS_Menu/BottomBarSlot`; contiene 4 istanze annidate `BTN_BottomBarItem_Home/_Profile/_Stats/_Settings`. Voci di primo livello: Home → `GoHome()`, le altre → nuovo `UIScreenManager.ShowRoot(id)` (cronologia svuotata: ← da una scheda della barra non fa nulla).
- Voce attiva automatica da `ScreenChanged`; `Section Aliases`: Mission Brief e Training brief → Home (descrizione Figma "Mission Brief → Home"). Posizione: centrata 16 (`gap.bottom-bar.menu`) sotto il PANEL_Menu della schermata corrente, ricalcolata a ogni cambio (Home y 531, Mission Brief 591 nel frame); `Follow Menu Panel` = false per tenerla ferma.
- Aspetto: pillola 520×56 `surface.glass-light` senza riflesso né sfocatura, ombra `bottomBar` 0/10/30 (TEX_BottomBarShadow), padding 6, voci in space-between. Nessun Layout Group: posizioni calcolate una volta in `Apply()`.

### Codice comune e tema
- `tokens_figma.json` → `project.numbers`: `size.bottom-bar.width` 520, `size.bottom-bar.height` 56, `padding.bottom-bar` 6, `gap.bottom-bar.menu` 16, `size.bottom-bar-item.height` 44, `padding.bottom-bar-item` 10, `size.bottom-bar-item.icon` 32, `gap.bottom-bar-item` 6.
- Sprite: `TEX_PillRounded` (raggio = metà altezza barra, 28; usato ridotto anche per la pillola hover 44) e `TEX_BottomBarShadow` (variante compatta di `BuildShadowSprite`, forma con raggio 26 perché la texture sia alta esattamente 56 + 2 × 48: nessuna compressione dei bordi).
- `UIScreenManager`: `ShowRoot(id)`, `GetScreen(id)`; `GoHome()` = `ShowRoot(homeScreen)`. `UIStringText.SetStyle(stile, colore)`. Builder: `BottomBarBuilder`; `BaseBuilder` costruisce i componenti prima di MNG_UI e mette PANEL_BottomBar nel BottomBarSlot.

### Valori provvisori e punti aperti (v0.2.3)
- Larghezze voci misurate da TMP: 101/105/130/148 contro Figma 101/104/131/148 (Jaldi in TMP ±1 unità); posizioni entro 1 unità da Figma.
- Nomi delle istanze `BTN_BottomBarItem_<Voce>`: suffisso come variante (Nomenclatura), confermato.
- Su Mission Brief la barra (y 591–647) esce dal rettangolo 840×627 di CANVAS_Menu: nessun ritaglio in World Space, ma il frame va rivisto quando arriva la schermata vera.

## v0.2.2 — 2026-11-17 — Passo 2: PANEL_Side

Proprietario: **Team UI — Romario Gjoka**. Prefab da **SI81/UI/Build Base** (o solo **SI81/UI/Build Side Panel**), sprite da
**SI81/UI/Build Theme**. Rieseguibili: PANEL_Menu, TEX_PanelShadow e GUID invariati; scena identica a ogni esecuzione.

### PANEL_Side — v0.2.2 (`Prefabs/PANEL_Side.prefab`, script `SidePanel`, grafica `RoundedPanelGraphic`)
- Uso: istanziare PANEL_Side nella schermata come fratello di PANEL_Menu e assegnare `Attach To` = PANEL_Menu: si mette 24 (`gap.side-panel`) a sinistra, bordi superiori allineati; `Title Key` default `ui.brief.completion_criteria` ("Criteri di completamento").
- Righe sotto `ContentRoot` (larghe 272, altezza propria): dopo averle aggiunte/tolte chiamare `RefreshLayout()`, che le impila (gap 6, `gap.side-panel.rows`) e adatta l'altezza (Mission Brief 161; minimo titolo + padding). Nessun Layout Group né ContentSizeFitter.
- Inclinazione `angle.side-panel.tilt` (20°, `Tilt Angle` < 0 = tema) attorno al bordo destro (pivot in alto a destra): il bordo sinistro viene verso il giocatore. Stesso canvas di PANEL_Menu: `TrackedDeviceRaycaster` interseca il piano di ogni grafica, quindi colpisce anche il pannello inclinato.
- Struttura: `IMG_Shadow` (TEX_PanelShadowCompact, ombra `sidePanel` 0,30), `IMG_Glass` (vetro + riflesso come PANEL_Menu), `LBL_Title` (Body Bold, `text.on-dark`), `Content`.

### TEX_PanelShadowCompact — v0.2.2 (`Sprites/`, `PanelSprites.GetShadowLayout(raggio, ombra, altezzaMinima)`)
- Uso: ombra 9-slice per pannelli bassi; i bordi superiore + inferiore stanno in pannelli alti ≥ `size.shadow.compact-min-height` (120) senza compressione (TEX_PanelShadow richiede ≥ 200). Stessa espansione del RectTransform (96 per lato).
- Tecnica: ombra di una forma alta 120 con la riga stirata al centro della forma spostata; esatta a 120, più in alto le fasce laterali al più ~5% più chiare. Generata da `SpriteBuilder.BuildShadowSprite(..., minShapeHeight)`.

### Codice comune e sandbox
- Nuovo `PanelLayout` (vetro, ombra, `SetRect`, `LineHeight`, `SetActive`) usato da `MenuPanel` e `SidePanel`; nei builder `MenuPanelBuilder.BuildGlassLayers` / `BuildLabel` e `BaseBuilder.RecordInstanceOverrides` condivisi. PANEL_Menu identico byte per byte.
- `tokens_figma.json` → `project.numbers`: `size.side-panel.width` 320, `gap.side-panel` 24, `gap.side-panel.rows` 6, `angle.side-panel.tilt` 20 (gradi), `size.shadow.compact-min-height` 120.
- `SCN_UISandbox`: Mission Brief ha PANEL_Side accanto a PANEL_Menu (che resta centrato, canvasHint Figma) con 3 righe **finte solo per la sandbox** (`LBL_SandboxCriterion*`: icona + "Positivo/Parziale/Negativo" da chiavi esistenti). `CANVAS_SandboxDebug` spostato a DESTRA (1,2 / 1,35 / 1,1 m, 45°) per non coprire PANEL_Side.

### Valori provvisori e punti aperti (v0.2.2)
- `TrackedDeviceRaycaster` (Input System) ignora `raycastTarget`: le ombre (non interattive) intercettano i raggi. Vicino alla cerniera l'ombra di PANEL_Menu copre i 72 unità a destra di PANEL_Side (oggi nessun elemento interattivo lì). **Risolto in v0.2.3** (`UIWorldRaycaster`).
- PANEL_Side sporge a sinistra del rettangolo 840 di CANVAS_Menu (nessun ritaglio in World Space). Altezza minima ombra compatta 120: un pannello senza righe (71) comprime leggermente i bordi.

## v0.2.1 — 2026-11-17 — Passo 2: PANEL_Menu e sprite TEX_

Proprietario: **Team UI — Romario Gjoka**. Sprite da **SI81/UI/Build Sprites** (eseguito anche da **SI81/UI/Build Theme**), prefab da
**SI81/UI/Build Base** (o solo **SI81/UI/Build Menu Panel**). Rieseguibili: PNG riscritti solo se cambiano, prefab e scena aggiornati sul posto, GUID invariati.

### PANEL_Menu — v0.2.1 (`Prefabs/PANEL_Menu.prefab`, script `MenuPanel`, grafica `RoundedPanelGraphic`)
- Uso: istanziare PANEL_Menu dentro la schermata (`PANEL_*` con `UIScreen`); in `MenuPanel` impostare `Title Key`, `Breadcrumb Key` (vuota = nessun percorso), `Show Back` (nascosto su Home) e `Height` (0 = 475 del tema; Mission Brief 535).
- Contenuti sotto `ContentRoot` (712 × resto, da y 76 su Home, da y 99 con percorso), elementi a destra del titolo sotto `HeaderRightSlot`; `BTN_Back` (passo 3) va dentro `BackSlot` 40×40: il suo `Button` viene collegato da solo a `UIScreenManager.Back()`.
- Da codice: `SetTitleKey`, `SetTitleText` (titolo scenario da `SO_Mission_*`), `SetBreadcrumbKey`, `SetHeight`, `ShowBack`. Ancorato in alto al centro del frame 840×627 a `margin.screen` (40). Nessun Layout Group: layout calcolato solo quando cambiano i parametri.
- Struttura: `IMG_Shadow` (TEX_PanelShadow, ombra `menuPanel`), `IMG_Glass` (TEX_PanelRounded, `surface.glass` + riflesso), `Header` (`BackSlot`, `TitleColumn/LBL_Breadcrumb` Body `text.on-green`, `TitleColumn/LBL_Title` Title `text.on-dark`, `HeaderRightSlot`), `Content`.
- Riflesso: cotto nei colori dei vertici di `RoundedPanelGraphic` (centro suddiviso in celle ≤ 32 unità) e già composto sopra il vetro → un solo strato, nessuna texture o maschera in più; la forma arrotondata viene dall'alfa dello sprite. Costo: 2 draw call (ombra + vetro con riflesso) + testo; 1 in meno se i due sprite finiscono in uno Sprite Atlas.

### TEX_PanelRounded / TEX_PanelShadow — v0.2.1 (`Sprites/`, builder `Editor/SpriteBuilder.cs`, regole `Runtime/PanelSprites.cs`)
- Uso: `TEX_PanelRounded` = 9-slice bianco raggio 24 (2 px per unità, 128×128, bordi 48) su `Image` Sliced o `RoundedPanelGraphic`, colorato dal tema. `TEX_PanelShadow` = ombra 9-slice bianca (0,5 px per unità, 200×200) tinta con `Image.color` = colore del token; il suo RectTransform va allargato di `PanelSprites.GetShadowLayout(...).PaddingUnits` (96 unità) per lato.
- Generatori riusabili per card, pulsanti e bottom bar: `SpriteBuilder.BuildRoundedSprite(percorso, raggio)` e `SpriteBuilder.BuildShadowSprite(percorso, raggio, ombraDelTema)`; aggiungere la chiamata in `SpriteBuilder.BuildAll`.
- Ombra: forma spostata dell'offset (0/−20), gaussiana σ = blur/2 = 30, tagliata a 2,5 σ, bucata sotto il pannello (come Figma: ombra non visibile dietro il vetro semitrasparente). Import: Sprite FullRect con bordi, mipmap, Clamp, Bilinear, Android ASTC 4×4 (arrotondato) / 6×6 (ombra).
- `TEX_PanelSheen` NON creato: il riflesso nei vertici costa meno (nessuna texture, nessuna maschera stencil, stessa draw call del vetro).

### Tema e sandbox (aggiornamento)
- `tokens_figma.json` → `project.numbers`: `margin.screen` 40, `size.menu-panel.width` 760, `size.menu-panel.height` 475, `size.menu-panel.height-brief` 535; `project.glassSheen` (`handleStart` 1/7, `handleEnd` 6/7) → `UITheme.GlassSheen.HandleStart/HandleEnd` + `Evaluate(u, v)`.
- `SCN_UISandbox`: le 6 schermate segnaposto usano un'istanza di PANEL_Menu (vecchio fondo e titolo rimossi). Home "Scenari" senza indietro; Mission Brief percorso + indietro + 535; Training brief percorso + indietro.

### Valori provvisori (v0.2.1)
- Altezza Mission Brief 535 (punto aperto). Titolo segnaposto di Mission Brief = `ui.brief.description` (il vero titolo arriverà da `SO_Mission_*` con `SetTitleText`).
- Densità sprite (2 e 0,5 px per unità), estensione ombra 2,5 σ, rientro della bucatura 1 unità; "spread" Figma non gestito (0 in tutti i token).
- Etichette del titolo in modalità Overflow (non Ellipsis): la riga Figma è più bassa della riga naturale di Jaldi e TMP nasconderebbe il testo.
- Senza il 360° pre-sfocato lo sfondo è molto scuro: il vetro all'85% e l'ombra appaiono più scuri/meno visibili che in Figma.

## v0.2.0 — 2026-11-17 — Passo 2: Base (MNG_UI, input, sandbox)

Proprietario: **Team UI — Romario Gjoka**. Generato dal menu **SI81/UI/Build Base** (rieseguibile: trova gli oggetti per nome,
aggiorna sul posto, nessun duplicato, GUID invariati). Richiede prima **SI81/UI/Build Theme**. Nessun XR Interaction Toolkit (§4.12).

### MNG_UI — v0.2.0 (`Prefabs/MNG_UI.prefab`, script `UIScreenManager`)
- Uso: mettere UNA istanza di MNG_UI in `SCN_Core` accanto al rig condiviso; i prefab `PANEL_*` vanno sotto `CANVAS_Menu/Screens` (ognuno con `UIScreen`), `PANEL_BottomBar` sotto `CANVAS_Menu/BottomBarSlot`.
- Navigazione: `UIScreenManager.Instance.Show(UIScreenId.X)` (mette la corrente in cronologia), `Back()` (BTN_Back via UnityEvent, B/Y, Esc, Backspace; nessun effetto sulla radice), `GoHome()` (svuota la cronologia); `ShowById(int)` per gli UnityEvent; evento `ScreenChanged`.
- Un solo MNG_UI: un secondo viene segnalato con un avviso e distrutto. `CANVAS_Menu`: World Space 840×627, 1,5 m davanti, centro a 1,5 m da terra, scala 0,00189, `TrackedDeviceRaycaster` + `GraphicRaycaster` (mouse in Editor); camera assegnata a runtime da `WorldCanvasCamera` (Camera.main).
- **EventSystem**: è l'unico dell'app ed è figlio di MNG_UI, quindi deve vivere in `SCN_Core` con il rig condiviso (**da confermare con VR Interaction System**). Le scene di scenario non devono avere un altro EventSystem.

### UIScreen — v0.2.0 (`Runtime/UIScreen.cs`, enum `UIScreenId`)
- Uso: componente sulla radice di ogni schermata menu (`PANEL_*`), con `Id` e `SO_UITheme`; richiede un `CanvasGroup`.
- `Show()` / `Hide()` con dissolvenza di `Motion.ScreenTransitionDuration` a tempo non scalato (nessun Animator); a fine dissolvenza `OnShown` / `OnHidden`, e la schermata nascosta viene disattivata.
- Nuove schermate: aggiungere il valore IN FONDO a `UIScreenId` con numero esplicito (il numero è salvato nei prefab).

### UIInputSetup / UIInputActions — v0.2.0 (`Runtime/UIInputSetup.cs`, `Runtime/UIInputActions.cs`)
- Uso: già sull'EventSystem di MNG_UI; a runtime crea le azioni nel codice (nessun `.inputactions`) e le assegna a `InputSystemUIInputModule`. Binding come costanti in `UIInputActions`.
- Puntatori (PassThrough): `devicePosition` / `deviceRotation` + `triggerPressed` dei due `XRController`, mouse `position` / `leftButton` / `scroll` per l'Editor. Back: `secondaryButton` sinistro/destro (Y/B), Esc, Backspace → evento `BackPerformed`.
- `xrTrackingOrigin` = genitore di camera e controller del rig (in sandbox `XROrigin/CameraOffset`, impostato dal builder); se vuoto usa il genitore di `Camera.main`; `SetTrackingOrigin()` per rig caricati dopo.

### UIPointer — v0.2.0 (`Runtime/UIPointer.cs`, `Materials/MAT_UIPointer.mat`, `Materials/MAT_UIPointerReticle.mat`)
- Uso: sul GameObject di ogni controller (con `TrackedPoseDriver`), con `LineRenderer` (MAT_UIPointer) e un quad figlio `Reticle` (MAT_UIPointerReticle, icona `ICN_RadioButtonChecked`); scegliere la mano (`Left`/`Right`).
- Non fa raycast propri: legge `InputSystemUIInputModule.GetLastRaycastResult(deviceId)`, quindi il raggio coincide con ciò che riceve gli eventi. Colore `color.pointer.valid` su elementi interattivi, `color.pointer.invalid` sugli altri.
- Si nasconde se non punta la UI o oltre `Pointer.RayMaxLength` (4 m); spessore 4 mm e reticolo 16 mm da `SO_UITheme.Pointer`.

### SCN_UISandbox — v0.2.0 (`Scenes/SCN_UISandbox.unity`, script `UISandboxDebugSwitcher`)
- Uso: aprire la scena e premere Play. Tasti 1–6 = schermate nell'ordine di `UIScreenId`, Esc/Backspace = indietro; in visore i pulsanti di `CANVAS_SandboxDebug` a sinistra (grilletto) e B/Y.
- Contiene: rig minimo con componenti standard (`XROrigin/CameraOffset`, camera a 1,6 m con `TrackedPoseDriver` HMD, `LeftController`/`RightController` con `TrackedPoseDriver` + `UIPointer`), istanza di MNG_UI con 6 schermate segnaposto `PANEL_Sandbox<Id>` (titolo da chiave), sfondo scuro `MESH_SandboxBackdrop` (MAT_SandboxBackdrop + TEX_SandboxBackdrop).
- Solo per prove della UI: non è nelle Build Settings; il rig è un segnaposto (il rig vero lo fornisce VR Interaction System).

### Tema (aggiornamento, Build Theme)
- `tokens_figma.json` → `project.pointer` (`rayWidth` 0,004, `rayMaxLength` 4, `reticleDiameter` 0,016, `reticleSurfaceOffset` 0,002 m) → `UITheme.Pointer`; colori `color.backdrop.top` #123A2D / `color.backdrop.bottom` #050E0B (`ColorTokens.BACKDROP_*`); `UITheme.MENU_CENTER_HEIGHT_M` = 1,5; proprietà `PointerValid` / `PointerInvalid`.
- Nuovo componente `UIStringText` (TMP + chiave di `SO_UIStrings_It` + stile/colore del tema), usato dai segnaposto; assembly `SI81.UI` ora riferisce `Unity.InputSystem`.

### Valori provvisori (v0.2.0)
- Misure del puntatore e colori dello sfondo segnaposto (in attesa di VR Interaction System e del 360° pre-sfocato della base operativa).
- Altezza del centro menu 1,5 m e dimensione `BottomBarSlot` (altezza 0, la decide `PANEL_BottomBar`).
- Posa del raggio = posa "grip" del controller (`devicePosition`); valutare la posa "aim" (`pointerPosition`) in visore.

## v0.1.2 — 2026-11-17 — Passo 1: correzioni dalla revisione

Proprietario: **Team UI — Romario Gjoka**. Nessun nuovo asset; GUID di `SO_*`, `FNT_*`, `ICN_*` invariati.

### Regola: i JSON sono l'unica fonte di verità
- **Testi:** modificare SOLO `Editor/Source/strings_it.json`, poi eseguire **SI81/UI/Build Theme**: le modifiche fatte direttamente a `SO_UIStrings_It` vanno perse (avviso anche nell'Inspector dell'asset).
- **Tema e livelli:** modificare SOLO `Editor/Source/tokens_figma.json`, poi **SI81/UI/Build Theme**: ogni esecuzione riscrive tutti i valori di `SO_UITheme` e `SO_DifficultyStyle` (anche animazioni e regole HUD); i valori ritoccati nell'Inspector vengono riportati a quelli del JSON. Unica eccezione conservata: gli sprite `Icon` dei livelli in `SO_DifficultyStyle`.
- Nuova sezione `"project"` di `tokens_figma.json` (valori derivati, **PROVVISORI**): colori `color.button.disabled` / `-text`, misure `border.primary` / `stroke.level-bar`, `motion` (hover 1,04, pressed 0,98, durate 0,08–0,25 s) e `hud`. Nel codice non resta nessun valore duplicato.

### Correzioni
- Build Theme non passa più `MotionSettings`/`HudSettings` vuoti: legge `project.motion` / `project.hud` e segnala con un errore i valori mancanti; le scale hover/pressed usate per normalizzare le ombre Figma vengono dallo stesso JSON.
- Font: la rigenerazione aggiorna **sul posto** atlas e materiale di `FNT_Jaldi*_SDF` (stessi fileID, nessun "Destroying assets"); `FontBuilder.BuildAll(force: true)` forza la rigenerazione.
- Namespace Editor rinominato `SI81.UI.Editor` → `SI81.UI.EditorTools` (non nasconde più `UnityEditor.Editor`); l'assembly resta `SI81.UI.Editor`.
- `UITheme` / `DifficultyStyle`: nessun colore di default nel codice; ogni campo colore parte da `UITheme.MISSING_COLOR` (magenta), così un dato non generato si vede subito. Anche motion/HUD non hanno più valori nel codice.
- `tokens_figma.json`: descrizioni `color.border.*` corrette ("Solo bordo di BTN_Primary (2 unità, punto aperto)"), niente più "oliva/hover".
- Icona `ICN_Logout` rinominata **`ICN_Exit`** (per funzione, "Esci"): SVG, manifest e PNG spostato con `AssetDatabase.MoveAsset` (stesso GUID).
- `ui.stats.average_line` = "{0} di media · {1} scenari giocati su {2}", dove `{0}` = `strings.Format("ui.format.safety_score", media)`: il testo "Punteggio sicurezza" esiste solo in `ui.format.safety_score`.
- `SourceJson.cs`: `/// <summary>` su ogni campo dei DTO JSON.

## v0.1.1 — 2026-11-17 — Passo 1: Icone

Proprietario: **Team UI — Romario Gjoka**. Generate dal menu **SI81/UI/Build Icons** (eseguito anche da **SI81/UI/Build Theme**). Rieseguibile: riscrive solo i PNG
il cui contenuto cambia, mantiene `.meta`/GUID, non duplica e non cancella nulla (i PNG senza SVG vengono solo segnalati).

### ICN_ — v0.1.1 (`Icons/ICN_*.png`, 76 file; builder `Editor/IconBuilder.cs`, import `Editor/IconImportSettings.cs`)
- Uso: assegnare lo sprite a un `Image` e colorarlo con i token del tema via `Image.color` (es. `theme.GetColor(ColorTokens.BRAND_DARK)`): il PNG è bianco, la forma è nel canale alfa.
- Le varianti piene hanno il suffisso `_Filled` (es. `ICN_Home` / `ICN_Home_Filled`, voce attiva della bottom bar); il resto è contorno.
- Sorgenti SVG in `Editor/Source/Icons~` (Material Symbols Rounded 48px, Apache 2.0, `LICENSE.txt` + `manifest.json`): per una nuova icona aggiungere lì `ICN_<Nome>.svg` e rieseguire il builder.
- Tecnica: SVG analizzato e tessellato con l'API Vector Graphics di Unity (modulo integrato), rasterizzato su CPU 128×128 con 8×8 campioni per pixel (risultato identico su ogni macchina); l'intero viewBox è mappato sul quadrato (margine Material incluso).
- Import: Sprite (2D and UI) Single, Alpha Is Transparency, sRGB, Clamp, Bilinear, max 128, mipmap attive, niente Read/Write; PC CompressedHQ (BC7), **Android ASTC 4×4** (bordi alfa puliti, ≈ 21 KB per icona con mipmap).

### Note (v0.1.1)
- 5 varianti `_Filled` sono identiche al contorno (il glifo non ha differenze sull'asse FILL): `BarChart`, `RadioButtonChecked`, `SelfImprovement`, `AccessibilityNew`, `Tune`.
- Le forme piene possono differire leggermente da Figma, che usa Material Icons Round (non Material Symbols).

## v0.1.0 — 2026-11-17 — Passo 1: Tema

Generato dal menu **SI81/UI/Build Theme** (rieseguibile: non duplica, mantiene i GUID, non modifica asset già aggiornati).
Sorgenti: `Editor/Source/tokens_figma.json` (variabili/effetti Figma) e `Editor/Source/strings_it.json` (testi curati).

### SO_UITheme — v0.1.0 (`Data/SO_UITheme.asset`, classe `UITheme`)
- Colori: 38 token (36 variabili Figma + 2 derivati `color.button.disabled` #ECECEC 40% e `color.button.disabled-text` nero, nota Figma 5); 7 stili Jaldi; 11 ombre/glow; riflesso vetro; 14 misure (12 Figma + `border.primary` 2, `stroke.level-bar` 1,5).
- Uso: riferire l'asset dal componente e leggere `theme.GetColor(ColorTokens.BRAND_DARK)` / `theme.GetTextStyle(TextStyleIds.BODY).ApplyTo(tmp)` / `theme.GetShadow(ShadowRoles.CARD)`; mai valori scritti a mano.
- Scala Canvas: `UITheme.GetCanvasScale(distanza_m)` = 0,00126 × m (menu a 1,5 m → 0,00189). Hover 1,04 / pressed 0,98 in `theme.Motion`; regole HUD (avvisi 3 s, sottotitoli 32×2, polso 30%→100% entro 0,4 m) in `theme.Hud`.

### SO_DifficultyStyle — v0.1.0 (`Data/SO_DifficultyStyle.asset`, classe `DifficultyStyle`)
- Livelli Guided (#0072B2), Operational (#1B5E48), Crisis (#D55E00, solo testo nero); barre icona #168F6B su bianco / #ADDD8E su scuro.
- Uso: `style.Get(DifficultyLevel.Crisis)` per colore, chiavi testo e barre piene; `style.GetBarColor(piena, suScuro)` per l'icona a 3 barre.
- Testo badge: `style.GetBadgeText(strings, livello)` → "Livello 2 · Operational". Slot `Icon` vuoto per ora (le icone arrivano in un passo successivo).

### SO_UIStrings_It — v0.1.0 (`Data/SO_UIStrings_It.asset`, classe `UIStrings`)
- 159 chiavi (formato `^[a-z0-9_]+(\.[a-z0-9_]+)+$`, nessun duplicato, nessuna "SPI"), di cui 7 dedotte (es. `ui.outcome.negative`, `ui.settings.controls.hand_left`).
- Uso: `strings.Get("ui.nav.home")`, `strings.Format("ui.format.safety_score", 86)` → "Punteggio sicurezza 86/100" (unica chiave del punteggio, CLAUDE.md §4.6).
- Chiave mancante → segnaposto visibile `[?chiave]` + un solo avviso per chiave. Testi specifici di scenario NON qui (vanno in `SO_Mission_*`). Escluso il testo "riduzione luce blu" (§8).

### FNT_ — v0.1.0 (`Fonts/FNT_JaldiRegular_SDF.asset`, `Fonts/FNT_JaldiBold_SDF.asset`)
- Jaldi Regular/Bold (SIL OFL 1.1, `Fonts/OFL.txt`, da github.com/google/fonts `ofl/jaldi`); TMP SDF statico, SDFAA, 90 pt, padding 9, atlas 1024×1024, 203 caratteri ciascuno.
- Uso: assegnare il font tramite `UITheme.TextStyle.ApplyTo()` (usa `fontSharedMaterial`, nessun materiale extra); il grassetto è il font Bold, non lo stile sintetico.
- Glifi assenti in entrambi i font: ✓ U+2713, ★ U+2605, ◐ U+25D0 → nella UI usare le icone `ICN_CheckCircle`, `ICN_Star`, `ICN_Contrast`. "₂" non serve (Figma scrive "CO2").

### Valori provvisori (v0.1.0)
- Durate animazione (guideline §3.7 non disponibile): hover 0,12 s, pressed 0,08 s, selezione 0,15 s, dissolvenza 0,2 s, cambio schermata 0,25 s.
- Impostazioni font (guideline §3.3 non disponibile): campionamento 90 pt, padding 9, atlas 1024, SDFAA.
- Interlinea: Figma 125% → TMP `lineSpacing` −43,99 (calcolato dalla metrica Jaldi, line height naturale ≈ 1,69 em).
- `color.surface.glass`, `color.surface.glass-light`, `color.state.active`, `color.pointer.*`: marcati "PROPOSTA" in Figma.
- `border.primary` = 2 (punto aperto di team, CLAUDE.md §4.2).
