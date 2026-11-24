# PROGRESS — Sicurezza Immersiva 81 · UI

Updated at the end of every prompt (CLAUDE.md §10). Newest entry on top.

---

## UI spostata nella struttura del README — 2026-11-20 — done

**Done:** tutta la UI comune spostata da `Assets/_Common/UI/` ad `Assets/_Project/` secondo `PROPOSTA_STRUTTURA_UI.pdf`, con `AssetDatabase.MoveAsset` (GUID invariati, 809 file identici).
- `Home/Code/Scripts/Runtime` + `Editor` (asmdef invariati), `Home/UI/Prefabs/{Base,Components,Screens,HUD}`, `Home/UI/Data` (+ `Missions`), `Home/UI/Fonts`, `Home/Art/Textures/{Icons,UI}` (Sprites + Textures uniti), `Home/Art/Materials`, `Home/Level/Scenes` (+ `Mock`), `Home/Docs` (CHANGELOG, FIGMA_SPECS, PROGRESS, PROMPTS, VERIFICA_FINALE).
- `Shared/Level/ScenarioTemplate/`: `SCN_ScenarioTemplate`, `SO_Mission_Template`.
- `Docs/` (fuori da Assets) -> `Assets/_Project/Docs/`: guida md+pdf, `UI_INTEGRATION_FOR_AI.md`, `Nomenclatura.md`, immagini in `img~/` (non importate).
- Codice: `ThemeBuilder.ROOT` = `Assets/_Project/Home`, nuovo `BaseBuilder.BASE_PREFABS_FOLDER`, cartelle aggiornate in tutti i builder, `ScenarioController.CORE_SCENE_PATH`. Build Settings seguite in automatico.
- Verifica: compilazione ok, Build Base + Crea kit scenario + Valida tutti gli scenari rieseguiti: 0 errori, 9 avvisi attesi (scene di scenario non ancora create). Rigenerati solo MAT_UIPointer/Reticle (render queue) e SCN_UISandbox.
- Aggiornati percorsi in CLAUDE.md, guida (v0.6, PDF rigenerato, 19 pagine), file AI, FIGMA_SPECS. Le voci vecchie di PROGRESS/CHANGELOG restano con i percorsi storici.

**Open:** cartelle `Level/Scenes/` e `UI/Data/` non previste dallo scheletro del README: da concordare con gli altri team. `SO_MissionCatalog` resta l'unico file della UI toccato da tutti (pull prima di push).

## File per assistenti AI + prompt nella guida — 2026-11-20 — done

**Done:** `Docs/UI_INTEGRATION_FOR_AI.md` (inglese, risposte in italiano): fatti del progetto, architettura, casi A–F (scenario nuovo, scena esistente, script, validatore, Quest che non risponde, Training), modello di script, API esatte, regole SO_Mission, aggiunta componenti (team: solo componenti di scenario non-UI nella cartella del gruppo + modulo di richiesta al Team Ludovica; Team Ludovica: procedura con builder), divieti, pulsanti del controller, checklist. Guida v0.6: nuovo §11 con 8 prompt pronti (uno per caso), richiami in "In breve" e indice. PDF 18 pagine.

## Guida v0.5: portare dentro una scena già iniziata — 2026-11-19 — done

**Done:** nuova sezione alla fine del §3 "Hai già iniziato una scena? Portala dentro così": cosa togliere (rig, camera, Canvas, XRI), Modo A (consigliato: "Nuovo scenario..." e spostare l'ambiente, rifare le luci della scena) e Modo B (tenere il file di scena: copiare MNG_Scenario/MNG_SpawnPoint dal modello, duplicare SO_Mission_Template, catalogo, Build Settings), poi script/pannelli/naming/validatore. Richiamo in "In breve", indice e inizio §3. PDF 16 pagine. Corretto anche il testo schiacciato tra le immagini al passo 2.

## Causa del "non si clicca": profili Detached — 2026-11-19 — done (CHANGELOG v0.7.6)

**Log v0.7.5:** build giusta, controller presenti, ma nessuna riga `grilletto` e raggi corti. Il runtime rifiuta il binding `thumbrest/force` del profilo Detached Touch Plus → tutti i binding Touch Plus saltano → nessun dato dei controller nell'Input System (i modelli si muovono perché li traccia OVR). **Fix:** profili Detached spenti su Android + aggiunti alla guardia di build.

**Da verificare sul Quest:** niente `XR_ERROR_PATH_UNSUPPORTED`, righe `[SI81 Input] grilletto ... colpito=...`, hover e clic.

## Log del Quest: APK vecchio, diagnosi aggiunta — 2026-11-19 — done (CHANGELOG v0.7.5)

**Trovato nei log:** il primo APK testato era precedente ai fix (profili mani attivi); il secondo log parte 15 s dopo l'avvio e non contiene righe della UI. Una build Android fatta qui ha riacceso i profili mani da sola. **Done:** UIInputLog (`[SI81 Input]`), ControllersOnlyBuildGuard (`[SI81 Build]`), entrambi provati in Editor. Nessuna build lanciata da qui (l'utente non può testare da questo PC).

**Prossimo passo:** il collega fa pull, build, avvia la registrazione dei log PRIMA di aprire l'app, preme il grilletto su "Profilo" e manda il log (filtrare `SI81`).

## Controllo bug prima del test sul visore — 2026-11-19 — done (CHANGELOG v0.7.4)

**Done:** code review (7 problemi, tutti corretti) + giro completo in Play Mode solo con controller simulato. Trovato e corretto anche "Avvia training" attivo senza scena (errore, pulsante morto). Hand tracking OpenXR disattivato su Android (solo controller). Build Android verificata: SCN_Core indice 0, IL2CPP ARM64, Vulkan, OpenXR + Meta, qualità Mobile.

**Da verificare sul Quest:** raggio aim, hover/click, vibrazione, menu davanti al giocatore all'avvio.

## Fix interazione controller sul visore — 2026-11-19 — done in Editor (CHANGELOG v0.7.3)

**Problema (video Quest):** ci si muove ma non si interagisce: raggi sempre corti (nessun hit), nessun hover, card "Non disponibile". **Cause:** (1) `pointerBehavior` di default (SingleMouseOrPenButMultiTouchAndTrack) cancella i puntatori tracciati quando un mouse/puntatore di sistema manda input; (2) raggio dalla posa grip (punta in alto); (3) UIPointer leggeva `XRController.rightHand`, che con 6 profili OpenXR può essere un dispositivo diverso da quello del modulo. Le card "Non disponibile" sono corrette: le missioni di esempio non hanno scena (giocabile solo 99 Mock Scenario, pagina 2).

**Done:** AllPointersAsIs, binding aim (`pointerPosition/pointerRotation`), UIPointer con scelta del dispositivo e posa aim. Test in Play Mode con controller simulato: hover + click su Profilo OK anche con input del mouse. Console 0 nuovi errori.

**Da verificare sul Quest:** hover/click reali, direzione del raggio, se servono meno profili OpenXR attivi (oggi 6 su Android).

## Guida di integrazione v0.4 con immagini — 2026-11-19 — done

**Done:** guida riscritta passo per passo (stesse sezioni §1–§10), "In breve" + indice, 22 immagini in `Docs/img/`: 8 catture dell'Editor (Hierarchy, Inspector di MNG_Scenario / SO_Mission / catalogo / LabelAnchor, finestra Nuovo scenario, validatore OK e con errori), 10 catture di gioco (Home, Mission Brief, polso, sottotitoli, avviso, decisione, etichetta Risk, esito, Debrief, Score & Replay) e 4 schemi SVG (scene, flusso, zone della vista, mock-up timeline). PDF 15 pagine (`Docs/GUIDA_INTEGRAZIONE_SCENARIO.pdf`). Chiave `si81.progress` creata dalle prove e rimossa.

**Note:** l'unico errore in Console è quello provocato apposta per la cattura del validatore (Camera temporanea nel template, rimossa; scena non salvata). Le catture sono fatte senza visore (camera di cattura temporanea).

---

## SPI N/100 a schermo — 2026-11-19 — done (CHANGELOG v0.7.2)

**Decisione utente:** punteggio = "SPI N/100" + stelle (nota Figma 17), non più "Punteggio sicurezza N/100". **Done:** stringhe (`ui.format.safety_score`, `ui.score.safety_label` = "SPI · Indice di sicurezza"), tolti i controlli anti-SPI (UIStrings, validatore), commenti, CLAUDE.md §4.6/§8, guida + PDF. Build Theme + Build HUD rieseguiti, Console 0 errori. **Da verificare:** etichetta Score & Replay a vista in Play Mode / visore.

---

## Da fare · Timeline delle scelte nel Debrief — 2026-11-19 — documentata, NON implementata

**Richiesta (feedback sul Figma, aggiunto dall'utente):** nel Debrief mostrare la timeline delle scelte, es. "0:45 · Hai aperto la porta senza controllare la temperatura della maniglia → Conseguenza: flashover / ustione". La nota non risulta ancora nel file Figma (controllati Screens, "Note di progetto" 1–19, commenti).

**Spec decisa (guida §10):** `ScenarioController.RecordChoice(string action, string consequence, ChoiceKind kind)`; tempo automatico da `ElapsedSeconds`; kind = Good/Bad/Critical (stesse icone di `DebriefLinePrefix`); azione ≤ 60 caratteri, conseguenza ≤ 40; max 8 voci per partita. Da fare: `ChoiceRecord` (secondi, azione, conseguenza, tipo) in `MissionResult` (lista serializzabile), card "Timeline" in PANEL_Debrief in ordine di tempo (formato m:ss), stringhe in `SO_UIStrings_It` ("Conseguenza:"), validatore/guida aggiornati, CHANGELOG v0.8.0. Le righe `DebriefLines` restano per compatibilità.

**Deciso (stesso giorno):** a schermo "SPI N/100" (vedi voce sotto, CHANGELOG v0.7.2).

---

## Fix rig VR (testa, controller, puntatore) — 2026-11-19 — done (CHANGELOG v0.7.1)

**Problema (visore):** UI ferma rispetto alla testa, mani/controller invisibili, nessun puntatore. **Causa:** `SCN_MockScenario` aveva un secondo `OVRCameraRig` oltre al rig segnaposto (TrackedPoseDriver, senza modelli) di SCN_Core → due camere; il puntatore si vedeva solo colpendo la UI.

**Done:** SCN_Core rig = `OVRCameraRig` (FloorLevel) sotto `MNG_CoreRig` + `MESH_Controller` (OVRControllerPrefab) + UIPointer su Left/RightControllerAnchor; UI tracking origin = TrackingSpace. OVRCameraRig rimosso da SCN_MockScenario. UIPointer: raggio sempre visibile (token `pointer.idleRayLength` 0.6 m), posa dal modulo UI. Validatore: Camera in scena scenario = errore. Build Theme + Crea kit scenario rieseguiti; Play Mode SCN_Core: 1 camera attiva, head = CenterEyeAnchor, Console 0 errori/avvisi.

**Decisions:** rig condiviso = OVRCameraRig di Meta XR Core SDK (già installato; CLAUDE.md §4.12 "oggi OVRCameraRig"). Il puntatore usa la posa grip (`devicePosition`), come il modulo.

**Open:** verificare sul Quest 3 (in Editor nessun visore collegato: XR loader non attivo); grip vs aim del raggio; altezza menu con FloorLevel; confermare il rig con VR Interaction System.

---

## Verifica finale A–D — 2026-11-18 — done (CHANGELOG v0.7.0, Docs/VERIFICA_FINALE.md)

**Done:** full rebuild (Theme → Base → Kit scenario): 45 prefab, GUID and Build Settings unchanged, Console 0 errors. 76 missing summaries added (scan: 0 left). Duplicated helpers merged (PlaceTopLeft, EnsureFolder, LoadIcon, DefaultLevels). SO_Mission_Training has 3 criteria. Validator: sample missions without scene = warning (0 errors, 9 warnings). Guide v0.2 (§9: SCN_Training to be made by the training team). Play Mode smoke test Home → 99 → Guided → Debrief → Score (92/100) → Home passed; `si81.progress` pref removed.

**Open:** English words from Figma (list in VERIFICA_FINALE §6); SCN_Training; real rig; provisional values; Quest 3 checks.

---

## Block D · scenario kit + validator + guide — 2026-11-18 — done (CHANGELOG v0.6.0; its check folded into the final check)

**Done:** `ScenarioController` (base for every scenario: SetObjective, CompleteObjective, SetPhase, Alert, Feedback, Subtitle, AskDecision, EndMission; standalone Play bootstraps SCN_Core), `MockScenario` (~30 s scripted run), `SCN_Core` (placeholder rig `CoreRig` + MNG_UI real services + MNG_HUD + all screens), `SCN_ScenarioTemplate` + `SO_Mission_Template` ("[DA COMPILARE]"), `SCN_MockScenario` + `SO_Mission_MockScenario` (Id 99, in catalog). Menus **SI81/Scenari/Crea kit scenario**, **Nuovo scenario…**, **Valida scenario**, **Valida tutti gli scenari** (+ pre-build warning hook). Build Settings = SCN_Core, SCN_MockScenario (missing SampleScene removed). `Docs/GUIDA_INTEGRAZIONE_SCENARIO.md` (Italian). End to end from SCN_Core with real services verified (incl. Riprova and standalone Play).

**Decisions:** mission ids 01–12 scenarios, 00 Training, 99 mock; objectives 1–4; timer started by HUDRoot on MissionSession.Started (teams don't call StartTimer); MissionFlowDriver core-only; ShowHome recenters the menu.

---

## Block C · HUD + world labels + pause/end screens — 2026-11-18 — done (check PASS, CHANGELOG v0.5.0–v0.5.1)

**Done**
- `MNG_HUD` (Prefabs/HUD/) with `HUDService` (API for scenarios: SetObjective, SetPhase, SetHint, StartTimer, ShowEventAlert, ShowActionFeedback, ShowSubtitle, Pulse, ApplyLevel, SetVisible) + `HUDRoot.SetRig`: HUD_WristPanel (non-dominant hand from `controls.dominant_hand`, live swap, fade 30→100% ≤ 0.4 m, haptics at objective change and 60/30 s), HUD_EventAlert / HUD_ActionFeedback (−15°, 3 s, one at a time, event first, copied to the wrist), HUD_Subtitles (1 m, −20…−28°, own LazyFollow, 32×2 pages), MNG_WorldLabels + HUD_RiskLabel / HUD_InteractionPrompt / HUD_ContextHelp (`LabelAnchor`, 0.75–4 m, max 3 risk, ~10 Hz), HUD_DecisionPanel (1.2 m), HUD_OutcomePanel (1.5 m, 3–5 s).
- PANEL_Pause (left menu button / P), PANEL_Debrief, PANEL_ScoreReplay ("Punteggio sicurezza N/100" + stars + 5 bars), PANEL_TrainingComplete; `MissionFlowDriver` on MNG_UI (End → outcome → Debrief → Score → Home). `MissionSession.Started` event; launcher unloads an old scenario before reload.

**Decisions:** HUD read-only canvases have no raycaster (exception to §3; decision panel + context help use UIWorldRaycaster). HUD design values (wrist pose, angles, follow speeds) are serialized/builder constants (accepted §9 exception). Pause = single press. Interaction prompts on every level. Decision options all BTN_Secondary. Pause/TrainingComplete panels 760 wide (theme width). "Percorso corretto" built from objectives. Settings defaults registered at start (subtitles on).

**Open:** wrist pose/fade and alert pulse to tune on Quest 3; HUD PDF values to confirm; SFX_ clips not created (slots only). "Missing script (Unknown)" warnings come from Meta SDK building-block prefabs (packages not installed), not SI81.

---

## Block B · menu screens + navigation + data — 2026-11-18 — done (check PASS, CHANGELOG v0.4.0–v0.4.1)

**Done**
- Data (`Runtime/Data/`, `Data/Missions/`): SO_MissionCatalog, SO_LevelConfig_Guided/_Operational/_Crisis, 8 sample SO_Mission_* (01,02,03,04,07,08,09,10, "(esempio)" in DesignerNote) + SO_Mission_Training; IProgressService (Mock + PlayerPrefs), IMissionLauncher (Scene + Mock), MissionSession (End records result once, guard against double End; Clear on ReturnToHome), UIServices + UIServicesBootstrap (MNG_UI; sandbox uses mocks), UINavigationContext, ScoreRules (stars from theme tokens 20/40/60/80/90).
- Screens (`Prefabs/Screens/`, built by Build Base → Build Screens): PANEL_Home, PANEL_MissionBrief, PANEL_Stats, PANEL_Profile, PANEL_Settings (6 tabs, 21 rows, SettingsCatalog — `controls.dominant_hand` 0 = right / 1 = left), PANEL_TrainingBrief. Sandbox keys 1–6 open them; Play Mode flow Home → Brief → Start → End → Home verified.

**Decisions:** Stats detail outcome = full-colour LBL_Outcome (white card, §4.9). "Informazioni" stays in Settings → Generale. Home paging > 8 missions. Missions without a scene = unavailable. Scenario scenes load additively (MNG_UI + rig in SCN_Core). Instance names with variant suffix kept (`BTN_DifficultyCard_Guided`, Nomenclatura rule 3).

**Open:** no scroll (Stats > 9 rows, badges > 12). English labels on screen from Figma (Safety…, Guided/Operational/Crisis). Operational risk labels on (Figma's outdated HUD said off) — confirm with the HUD PDF. Badge detail card and Profile info are our proposals.

---

## Speed mode (from 2026-11-17, CLAUDE.md §12) — Block A · base + components — 2026-11-18 — done (check PASS, CHANGELOG v0.3.0–v0.3.1)

**Done**
- Step 2 review fixes (v0.2.4): `UIInputDiagnostics` (warns on TrackedDeviceRaycaster / 2nd EventSystem), Back only while menu shown (`ShowMenu`/`HideMenu`), `PlaceInFront`/`Recenter`/`SetHead`/`SetTrackingOrigin`, shadow fillCenter off, `TEX_AtlasUI` sprite atlas, child prefixes, `UITween` + `UIEditorSupport`.
- `Docs/FIGMA_SPECS.md` written once (20 components, 13 screens).
- 20 components (v0.3.0) in `Prefabs/Components/`, all from **SI81/UI/Build Base** (chains Build Components): BTN_Primary, BTN_Secondary, BTN_Back, BTN_Badge, BTN_SettingsTab, BTN_ListRow, PANEL_ConfirmDialog, LBL_LevelBadge, LBL_StarRating, LBL_DimensionBar, PANEL_StatTile, LBL_Outcome, LBL_ObjectiveItem, PANEL_SettingsToggle/Slider/Selector/Action, BTN_ScenarioCard, PANEL_TrainingCard, BTN_DifficultyCard. Shared `UIStateVisual` (hover 1.04+shadow, pressed 0.98, selected lift+glow+✓, disabled). Settings store `IUISettingsStore` (PlayerPrefs `si81.*` + Mock).
- Sandbox gallery screen (key 7) with every state side by side. Play Mode: all prefabs instantiate, states/slider/dialog callbacks verified.

**Decisions (speed mode, no stop)**
- §4.13 bottom-bar hover = grey pill. BTN_Primary keeps the 2-unit border (open point §4.2). BTN_SettingsTab hover = §4.1 scale+shadow (Figma hover invisible). BTN_ListRow selected = brand.light + ✓ + glow. Danger = style of BTN_Secondary.
- Labels' layout sizes are code constants (accepted exception, CHANGELOG). Gallery captions are debug text (sandbox only).
- Sprite Packer mode = Sprite Atlas V2 (`ProjectSettings/EditorSettings.asset`, only change outside `_Common/UI`). Atlas name `TEX_AtlasUI` (no atlas prefix in Nomenclatura).
- Data contracts (guideline §9 missing) defined by us for Block B: LevelConfig, MissionDefinition, MissionCatalog, MissionResult/Best, ProgressTotals, BadgeInfo, IProgressService, IMissionLauncher, MissionSession, UIServices, UINavigationContext. Stars from score 20/40/60/80/90 (provisional). `MissionSession.End` records results (not training) — nobody else calls RecordResult.

**Open problems**
- PANEL_TrainingCard is clickable (BTN_ would be correct, §2 table). Sound/haptics events exist (`FeedbackRequested`) but nobody plays them yet. Rig ownership + `HideMenu`/`ShowMenu` caller to confirm with VR Interaction System.

**Next:** Block A check + fix → Block B integrate (staged in `_BlockB~`), build screens, check + fix.

---

## Prompt 2 · Base structure — 2026-11-17 — done (review fixes applied in v0.2.4)

**Done** (menu **SI81/UI/Build Theme** then **SI81/UI/Build Base**; CHANGELOG v0.2.0–v0.2.3)
- `MNG_UI` prefab: `UIScreenManager` (history, Back via ← / B·Y / Esc, `ShowRoot`, `GoHome`, single-instance guard), the only EventSystem (`InputSystemUIInputModule`, actions created in code by `UIInputSetup`), `CANVAS_Menu` (World Space, 1.5 m, scale 0.00189, 840×627) with `UIWorldRaycaster`.
- `UIWorldRaycaster` replaces `TrackedDeviceRaycaster` (which ignored `raycastTarget`); decorative graphics have raycastTarget off. `UIPointer` ray per controller.
- `PANEL_Menu` (glass + sheen baked in vertex colours, 2 draw calls), `PANEL_Side` (tilted 20° on the same canvas, auto height), `PANEL_BottomBar` + `BTN_BottomBarItem` (Home/Profilo/Statistiche/Impostazioni; active = filled icon + Bold brand.dark; hover = tile.neutral pill as Figma; pressed 0.98).
- Sprites `TEX_PanelRounded`, `TEX_PanelShadow`, `TEX_PanelShadowCompact`, `TEX_PillRounded`, `TEX_BottomBarShadow`; materials `MAT_UIPointer` etc.
- `SCN_UISandbox` (`Assets/_Common/UI/Scenes/`): Input System rig (TrackedPoseDriver), placeholder screens, debug switcher (keys 1–6, Backspace/Esc, debug buttons). Not in Build Settings.

**Prefabs created:** MNG_UI, PANEL_Menu, PANEL_Side, PANEL_BottomBar, BTN_BottomBarItem.

**Open problems**
1. Review fixes pending: warn when any `TrackedDeviceRaycaster` exists (it silently disables controller rays on SI81 canvases) + validator check; Back (B/Y) enabled only while a menu is shown; `MNG_UI.PlaceInFront(head)` / recenter and rig hand-off of tracking origin (no `Camera.main` reliance); shadow `fillCenter` off (overdraw) + Sprite Atlas for TEX_/ICN_; prefix the unprefixed child GameObjects (`Screens`, `Header`, `Content`, `EventSystem`…), `Main Camera`, `ICN_Icon`/`LBL_Label`; shared tween helper instead of copied coroutines; 840×627 frame and eye height as tokens.
2. Team decision: bottom-bar hover = grey pill (Figma) vs CLAUDE §4.1 scale 1.04 + shadow.
3. Headset checks: ray origin (grip `devicePosition` vs aim `pointerPosition`), trigger click, B/Y back, glass look on the real backdrop.
4. MNG_UI/EventSystem must live with the shared rig in SCN_Core — confirm with VR Interaction System.
5. Empty `Assets/StreamingAssets` created by XR/Meta tooling on Play.

**Next step:** apply the review fixes (if approved), then Prompt 3 · Components.

---

## Prompt 1 · Theme — 2026-11-17 — done (review fixes applied, re-check PASS, CHANGELOG v0.1.2)

**Done** (all under `Assets/_Common/UI/`, asmdefs `SI81.UI` + `SI81.UI.Editor`)
- Menu **SI81/UI/Build Theme** (runs everything; re-run safe) + Build Fonts, Build Icons.
- `Data/SO_UITheme` (38 colours, 7 text styles, 11 shadows/glows, 14 sizes, motion), `Data/SO_DifficultyStyle` (3 levels), `Data/SO_UIStrings_It` (159 keys).
- `Fonts/FNT_JaldiRegular_SDF`, `FNT_JaldiBold_SDF` (static, 1024 atlas, 203 chars; missing ✓ ★ ◐ → icons). Jaldi TTF + OFL from google/fonts.
- `Icons/ICN_*.png` ×76 (Material Symbols Rounded SVG from fonts.gstatic.com → 128 px white, mipmaps, ASTC 4×4). Sources in `Editor/Source/Icons~/`.
- Sources of truth for the builder: `Editor/Source/tokens_figma.json`, `strings_it.json`. `CHANGELOG.md` v0.1.0 + v0.1.1.
- Decisions: Figma wording kept (English words included); one score key `ui.format.safety_score`; scenario texts go to `SO_Mission_*`, not the strings table.

**Prefabs created:** none (step 1 has no prefabs).

**Open problems**
1. ~~Review fixes~~ applied (v0.1.2): `tokens_figma.json` "project" section is the only source for motion/HUD/derived values (Inspector edits are reset by Build Theme — by design, documented); edit texts only in `strings_it.json`; fonts rebuild in place (fileIDs kept); Editor namespace `SI81.UI.EditorTools`; `MISSING_COLOR` (magenta) instead of hex defaults; `ICN_Logout` → `ICN_Exit`. New minor: both font atlases have `isReadable = true` (extra CPU copy on Quest) — check in step 2.
2. Provisional until guideline §3.3/§3.7 arrive: durations (0.08–0.25 s), SDF 90 pt / padding 9, Jaldi line spacing −44 (check in step 2).
3. Icon synonyms to reduce to one per function: RestartAlt/Replay/Refresh/History/Undo, Bolt/ElectricBolt, ArrowBack/ArrowLeft, Check/CheckCircle. 5 filled icons identical to outlined.
4. To confirm: "Punteggio migliore" as heading vs inline; Profilo = stars + score key; full Apache 2.0 text in `Icons~/LICENSE.txt`; `SO_DifficultyStyle` icon slot empty (3-bar level icon built in step 3).
5. Console has 2 old "Destroying assets" errors from an earlier font-builder test (fixed); pre-existing package warnings.

**Next step:** Prompt 2 · Base structure. XRI install was stopped by the user (2026-11-17): **no new packages** unless approved by the team. Nothing was installed (manifest unchanged). Decided: Input System's `InputSystemUIInputModule` + `TrackedDeviceRaycaster` (already in the project); own pointer ray, lazy-follow and wrist-attach scripts. CLAUDE.md updated (§3, §4.12, §4.11 subtitles, §7 step 0).

---

## Prompt 0 · Project check — 2026-11-17 — done (no files changed)

**Done**
- Unity Editor 6000.3.14f1 connected via Unity CLI (state `ready`).
- Read: CLAUDE.md, Docs/Nomenclatura.md, README.md, Figma "Note di progetto" (19 notes) + "Guida HUD · zone".
- Inventory: URP 17.3.0 (Android → `Mobile_RPAsset`), OpenXR 1.16.1 + Meta XR Core SDK 209.0.0, Input System 1.19.0, uGUI 2.0.0 (TMP built in). Editor build target = StandaloneWindows64.
- No scenes, prefabs or asmdefs yet. Console: 0 errors, 4 pre-existing warnings (OpenXR import, Meta Project Setup Tool, AI Assistant).

**Prefabs created:** none.

**Open problems**
1. Missing docs: `Guida_HUD_comune_SI81.pdf`, `PW4_v02_Guideline_UI.md`, `LAB4_ARVR_B2_def.pdf` (needed: guideline §3.3, §3.7 for step 1).
2. XR Interaction Toolkit and Unity OpenXR Meta not installed; project uses Meta XR Core SDK (OVRCameraRig). No EventSystem. Decide with VR Interaction System before step 2.
3. Folder: CLAUDE.md `Assets/_Common/UI/` vs README `Assets/_Project/Shared/` + `Home/`.
4. ~~Jaldi / icons missing~~ → decided 2026-11-17: download Jaldi (Regular, Bold, OFL.txt) from github.com/google/fonts `ofl/jaldi`; export ICN_ icons from the Figma file (128 px, made white by the builder).
5. Build Settings list a missing `Assets/Scenes/SampleScene.unity` (twice). Template leftovers: `Mat.mat`, `Readme.asset`, `SampleSceneProfile.asset`.
6. §2 table doubts: `PANEL_TrainingCard` vs `BTN_`, `SO_Mission_FireStarter`, `PANEL_Pause` vs `HUD_`, `LBL_DimensionBar`, `LBL_Outcome`.
7. Figma note 19 / "Guida HUD · zone" (cluster at 1.2 m) contradicts CLAUDE §4.11 (wrist panel); HUD PDF needed to confirm (step 5).
8. `Docs/` folder is capitalised; CLAUDE.md says `docs/`.

**Next step:** Prompt 1 · Theme — started 2026-11-17, paused for the new §11 rule (local only, no git writes: the user commits by hand). Two read-only Figma reads (tokens/icons, strings) were launched; their output goes to the session scratchpad. Folder: `Assets/_Common/UI/` per CLAUDE.md §3.
