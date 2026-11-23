# FIGMA_SPECS — Sicurezza Immersiva 81 UI

Single design source for all builders (CLAUDE §12). Read 2026-11-17 from Figma `dB3WqCkkOGTceYTtRM8vat` (Components 3:3, Screens 0:1).
Units: 1 Figma px = 1 Canvas unit; rects `[x,y,w,h]` top-left origin, y down. Tokens by dotted name -> `Assets/_Project/Home/Code/Scripts/Editor/Source/tokens_figma.json`. Text keys -> `strings_it.json` (existing); **NEW:** = proposed key to add (snake_case English).
Shorthands: `shadow.card` = 0/3 blur 8 #000 a0.25 · `shadow.hover` = 0/8 blur 20 #000 a0.35 · `shadow.selected` = glow 0/0 blur 18 `brand.light` + 0/8 blur 20 #000 a0.35 · glass panel = `surface.glass` + sheen + shadow 0/20 blur 60 a0.28.
Generic state rules (CLAUDE §4, every interactive component unless stated): Hover = scale 1.04 + shadow.hover (Figma bakes the scaled size: all dims x1.04, radius x1.04, text x1.04). Pressed = scale 0.98 + shadow.card. Selected = lift (shadow.selected) + brand.light glow + check badge. **No border on any state.** Motion tokens: `project.motion.*`.
Icons: Material Symbols Rounded outline (`ICN_X`); Filled `ICN_X_Filled` only for active/selected. Material -> Unity: snake_case -> PascalCase (`check_circle` -> `ICN_CheckCircle`), exception `logout` -> `ICN_Exit`. Full list in tokens_figma.json `icons.list`.
Shared Figma `Icon` component (5:10): Outline/Filled x 24/32.

## 0. Already done base (from figma_step2.json)

| Unity | Size | Key facts |
|---|---|---|
| PANEL_Menu | 760 x (Home 475 / Mission Brief 535 / others §2), radius.panel | fill surface.glass, baked `TEX_PanelSheen` (alpha 0.18 for d<=1/7, linear to 0 at d=4/7, d=(u+v)/2), shadow 0/20/60 a0.28, padding.panel 24, vertical gap space.m 12, content width 712. No runtime blur. |
| PANEL_Side | 320 x hug (161 on Mission Brief) | same glass; left of PANEL_Menu gap 24, tops aligned; yaw tilt 20 deg toward user (pivot right edge). Content: title `ui.brief.completion_criteria` (Body Bold on-dark) + 3 rows (icon 24 + Body on-green, gap 6): `ICN_CheckCircle` Positivo / `ICN_Contrast` Parziale / `ICN_Cancel` Negativo — texts are mission data (`ui.brief.criterion` "{0}: {1}"). |
| PANEL_BottomBar | 520 x 56, radius.pill | fill surface.glass-light, shadow 0/10/30 a0.25, padding 6, gap 8, items widths 101/104/131/148 (keys ui.nav.home/profile/stats/settings); centred under PANEL_Menu, 16 below. |
| BTN_BottomBarItem | 44 high, pad-x 10, gap 6, icon 32 + Body 18 | Default text.primary; Hover bg tile.neutral pill, no scale (CLAUDE §4.13); Pressed 0.98; Active = filled icon + Body Bold brand.dark, no bg. |
| BTN_Back | 40x40 radius.button | `ICN_ArrowBack` 24; Default fill surface.card no shadow; Hover fill tile.neutral + scale 1.04 + shadow.hover; Pressed scale 0.98. Hidden on Home. SFX_UIBack. |
| Header (Mission Brief) | [24,24,712,63] | BTN_Back + title column (breadcrumb Body on-green `ui.brief.breadcrumb`; title Title on-dark via `ui.format.scenario_title`; complexity 5 dots 10x10 + "2/5") + spacer + pill (brand.light, radius.card, pad 12/6, gap 8: `ui.brief.best_result` Body Bold + LBL_StarRating). |

## 1. Components

### BTN_Primary  [UI_PrimaryButton 6:200]
- Variants: Surface {Dark, Light} x State {Default, Hover, Pressed, Disabled}. 200 x 48, padding-x 16, gap 8, radius.button, centred.
- Children: `ICN_PlayArrow` 24 (swappable: arrow_forward Debrief, replay Score&Replay) + `LBL_Label` Subtitle Bold (Jaldi Bold 24) text.primary.
- Fill `state.active`. Border 2 KEPT (open point): `border.on-dark` (Dark = on glass panel) / `border.on-light` (Light = on white card).
- Default key `ui.button.start_scenario`. Disabled: fill `tile.neutral` @ alpha 0.4, no border, no icon, label = reason (`ui.button.choose_level`).
- Hover 1.04 (208x49.92); Pressed 0.98 (196x47.04). SFX_UIConfirm 0.5/80ms. Right-aligned in action rows (Mission Brief x512 of 712).

### BTN_Secondary  [UI_SecondaryButton 79:208]
- Variants: Style {Neutral, Danger} x State {Default, Hover, Pressed}. Height 48, pad-x 16, gap 8, radius.button, width hug (167 for "Ricomincia"). No border.
- Children: icon 24 (`ICN_RestartAlt` Neutral, `ICN_Exit` Danger) + label Subtitle Bold 24 text.primary (Danger text black, never white).
- Fill Neutral `surface.card` (Hover `tile.neutral`); Danger `deny`. shadow.card default/pressed, shadow.hover on hover. Hover 1.04 / Pressed 0.98.
- Keys: `ui.button.restart`, `ui.button.exit`, `ui.button.exit_to_home`. Used in Pausa, confirm dialog, Score&Replay.

### LBL_LevelBadge  [UI_LevelBadge 5:57]
- Variants: Level {1,2,3} x Surface {Light, Dark}. Hug: 146/180/133 x 23, horizontal gap 6.
- Children: `IMG_LevelBars` 20x20 (3 rects 4 wide, radius 1: bar1 [2,12,4,7], bar2 [8,7,4,12], bar3 [14,2,4,17]; filled count = level) + rich text 18: "Livello N" Bold + " · Name" Regular (`ui.level.badge`, names `ui.level.guided/operational/crisis`).
- Light: filled `level.bar.on-light`, empty = stroke 1.5 `level.bar.off-light` no fill, text.primary. Dark: filled `level.bar.on-dark`, empty stroke `level.bar.off-dark`, text.on-dark. Same bar colour for all levels. Not interactive. Only way to show a level.

### LBL_StarRating  [UI_StarRating 5:130]
- Variants: Value 0..5. 153 x 24, gap 2. 5 x `ICN_Star` 24 (Filled = full, outline = empty, text.primary) + `LBL_Value` Body "N/5" (`ui.format.stars`). Light surfaces only.

### LBL_DimensionBar  [UI_DimensionBar 6:211]
- Variants: Surface {Dark, Light}. 272 x 23, gap 8. Children: `LBL_Name` Body 88 wide (`ui.dimension.safety/awareness/decision/execution/time`); `IMG_Track` flex height 10 radius.pill stroke 1 + `IMG_Fill` height 10 radius.pill; `LBL_Value` Body 32 wide right-aligned (0–100, no %).
- Dark: name text.on-green, track stroke `level.bar.off-dark`, fill `brand.light`, value text.on-dark. Light: name/value text.primary, track stroke `level.bar.off-light`, fill `brand.dark`. Fill width = trackW x value/100. Demo Awareness 86.

### PANEL_StatTile  [UI_StatTile 79:229]
- Variants: Accent {Positive, Partial, Negative, Neutral}. 112 x 96, padding 12/8, radius.card, vertical stack: icon 24, label Body 18, value Title Bold 32.
- Positive: `confirm`, white text, `ICN_CheckCircle`, `ui.stats.positives` (5). Partial: `brand.dark`, white, `ICN_Contrast`, `ui.stats.partials` (2; only on white card). Negative: `deny`, black, `ICN_Cancel`, `ui.stats.negatives` (1). Neutral: `tile.neutral`, black, `ICN_History`, `ui.stats.sessions` (9). Icon+label+value always. Not interactive.

### BTN_Badge  [UI_Badge 79:262]
- Variants: State {Unlocked, Locked}. 64 x 64, centred column.
- Unlocked: `brand.light`, radius 32, filled icon 32 colour `brand.dark` (`ICN_WorkspacePremium_Filled` default; profile uses per-badge filled icon e.g. `ICN_LocalFireDepartment_Filled`, `ICN_Timer_Filled`, `ICN_Bolt_Filled`, `ICN_NotificationsActive_Filled`).
- Locked: `tile.neutral`, radius.card (square), `ICN_Lock` 24 text.secondary + label Body `ui.profile.badge_locked` ("Bloccato") text.primary. Shape + text differ, not only colour.
- Selecting opens a detail card (not designed -> open point). Generic hover/pressed.

### LBL_Outcome  [UI_OutcomeBadge 79:254]
- Variants: Outcome {Positive, Partial, Negative} x Size {Small, Large}. Pill radius.pill. Small: h32, padding L10 R14 T/B4, gap 6, icon 24 + Body Bold 18 (146/148/152 wide). Large: h60, padding L16 R24 T/B10, gap 10, icon 32 + Title Bold 32 (245/249/256 wide).
- Positive `confirm` `ICN_CheckCircle` white `ui.outcome.positive`; Partial `brand.dark` `ICN_Contrast` white `ui.outcome.partial` (white card only); Negative `deny` `ICN_Cancel` black text `ui.outcome.negative`.
- CLAUDE §4.9: on glass header use white pill + coloured icon + black text variant (`onGlass`); full colour only on white cards. Large = HUD_OutcomePanel.

### LBL_ObjectiveItem  [UI_ObjectiveItem 79:275]
- Variants: State {Done, Current, Todo}. 260 x 36 (hug height), padding 8/6, gap 8, radius.button; icon 24 + text Body 18 wrapping.
- Done: no bg, `ICN_CheckCircle_Filled` + text in text.on-green, strikethrough. Current: bg `brand.light`, `ICN_RadioButtonChecked` text.primary, text Body Bold text.primary. Todo: no bg, `ICN_RadioButtonUnchecked` + text in text.on-dark.
- Text = mission data. On dark/glass panels; used in PANEL_Pause list and wrist panel (max ~30 chars/line).


### PANEL_SettingsToggle  [UI_SettingsRow_Toggle 86:220]
- Variants: State {On, Off, Disabled}. Row 516 x 48, fill `surface.card`, radius.button, padding L16 R8, gap 12, horizontal centred. (Row width = content width of the Settings panel.)
- Children: `LBL_Label` Body flex (text.primary); `LBL_State` Body Bold "ON"/"OFF" (`ui.settings.toggle.on/off`, always written); `IMG_Track` 56 x 32 pill + `IMG_Knob` white circle (~24, knob right when On, left when Off).
- On: track `state.active`. Off: track `text.secondary` (#666666). Disabled: Off look at row opacity 0.4. SFX_UIToggle 0.3/30ms. Hover: generic (scale 1.04 + shadow).

### PANEL_SettingsSlider  [UI_SettingsRow_Slider 86:235]
- Variants: State {Default, Hover}. Row 516 x 48 (same chrome as toggle). Children: `LBL_Label` flex; `LBL_Value` Body Bold (number, e.g. 80); `IMG_Track` 200 x 12 radius.pill fill `tile.neutral` + `IMG_Fill` `brand.dark` (width = 200 x v/100; Figma: track padding-right = 200 x (1 - v/100)) + `IMG_Handle` circle `brand.dark` 28 (Hover 32; grab area 40); `end-pad` 8.
- Range 0–100, haptic tick every 10%.

### PANEL_SettingsSelector  [UI_SettingsRow_Selector 86:256]
- Variants: State {Default, AtLimit}. Row 516 x 48 (same chrome). Children: `LBL_Label` flex; selector group gap 4: `BTN_ArrowLeft` 40x40 circle `tile.neutral` + `ICN_ChevronLeft` 24; `LBL_Value` Body Bold 130 wide centred (e.g. "Teletrasporto"); `BTN_ArrowRight` 40x40 (`ICN_ChevronRight`). No dropdown.
- AtLimit: the arrow at the limit alpha 0.4 + SFX_UIError (Figma shows the left arrow dimmed).

### PANEL_SettingsAction  [UI_SettingsRow_Action 86:268]
- Variants: Type {Button, Value}. Row 516 x 48 (same chrome). 
- Button: `LBL_Label` flex + `BTN_Action` h40 radius.button fill `tile.neutral`, padding L12 R16, gap 6, `ICN_Refresh` 24 (swap icon per action: refresh/info/settings_backup_restore) + Body Bold action text. Actions: Ricalibra, Avvia (Rivedi Training), Apri (Informazioni), Ripristina (predefiniti, with PANEL_ConfirmDialog).
- Value (read-only): `LBL_Label` + `LBL_Value` Body text.secondary + `ICN_Lock` 24 text.secondary (Lingua: Italiano).

### BTN_SettingsTab  [UI_SettingsTab 86:281]
- Variants: State {Default, Hover, Active}. 180 x 48, radius.button, padding-left 12, gap 8, icon 24 + label 18, on the glass panel.
- Default: no bg, outline icon + Body, text.on-dark. Hover: bg `brand.dark` (Figma, low contrast on glass) + generic scale 1.04. Active: bg `surface.card`, filled icon `brand.dark`, label Body Bold `brand.dark`. No border.
- Tabs/icons (Settings screen): Comfort `ICN_SelfImprovement`, Audio `ICN_VolumeUp`, Sottotitoli `ICN_Subtitles`, Controlli `ICN_SportsEsports`, Accessibilità `ICN_AccessibilityNew`, Generale `ICN_Tune`; keys `ui.settings.tab.*`.

### BTN_ListRow  [UI_ListRow 86:330]
- Variants: State {Default, Hover, Selected, NotPlayed}. 440 x 40, radius.button, padding-x 12, gap 12.
- Children: `LBL_Title` Body Bold flex ("01 · Fire Starter", `ui.format.scenario_title`); `LBL_StarRating` (153x24, Body "4/5"); `LBL_Score` Body 56 wide right-aligned "86/100" (**NEW:** `ui.format.score_of_100` = "{0}/100").
- Default `surface.card`; Hover `tile.neutral` (+ generic scale); Selected `brand.light` (no border; Figma has no ✓); NotPlayed `surface.card`, text.secondary Regular: title + `ui.stats.not_played` ("Non giocato"), no stars/score.

### BTN_ScenarioCard  [UI_ScenarioCard 63:268]
- Variants: State {Default, Hover, Pressed, Disabled}. 166 x 140, `surface.card`, radius.card, shadow.card, clip. Vertical.
- `IMG_Thumbnail` 166 x 80 (placeholder gradient 147.28deg #C8402A -> #F2994A @71.4%; later mission thumbnail sprite) + `ICN_Scenario` 44 white centred (from mission data, e.g. local_fire_department; others in icons.list) + `PANEL_BestPill` top-right at (6,6): white pill radius.pill, padding L6 R8 T/B1, gap 2: `ICN_Star_Filled` 20 + Body "4/5" (`ui.format.stars`); no star and text `ui.home.new` ("Nuovo") when never played.
- `PANEL_Body` padding 8/6 gap 2: `LBL_Title` Bold 18 ellipsis; row gap 6: `IMG_ComplexityDots` 52x8 (5 dots, N filled) + `LBL_Complexity` Body text.secondary "2/5" + spacer + `IMG_LevelsCompleted` 20x20 (3 bars as LBL_LevelBadge, on-light filled for completed levels, off-light outline otherwise).
- Disabled: thumbnail + icon alpha 0.4, pill = `ICN_Lock` 20 + **NEW** `ui.home.unavailable` ("Non disponibile"), level bars alpha 0.4, not interactive.
- Hover 1.04 + shadow.hover (0/8/21 a0.35); Pressed 0.98 (shadow 0/3/8 a0.25).

### PANEL_TrainingCard  [UI_TrainingCard 63:296]
- Variants: State {Default, Hover, Pressed}. 712 x 56 strip, `surface.card`, radius.card, shadow.card, padding L16 R4, gap 12, centred. (Different style from scenario cards.)
- Children: `ICN_School` 32 `brand.dark`; `LBL_Title` rich "Training" Bold + " · Tutorial comune" Regular 18 (`ui.training.title`); `LBL_Subtitle` Body text.secondary flex ellipsis (`ui.training.card_subtitle`); `BTN_Primary` Light 200x48 "Avvia training" (`ui.button.start_training`, border.on-light).
- Hover scale **1.02** (Figma 726.24x57.12; NOT 1.04 to stay in the 712 content width) + shadow.hover; Pressed 0.99 (Figma 704.88). Opens PANEL_TrainingBrief.

### BTN_DifficultyCard  [UI_DifficultyCard 6:167]
- Variants: Level {1,2,3} x State {Default, Hover, Pressed, Selected}. **226 x 117** (CLAUDE §4.10), `surface.card`, radius.card, padding 10, gap 4, vertical, no border.
- Children: `LBL_LevelBadge` Light (row); `LBL_Description` Body 18 wrapping, text.primary, <=55 chars (`ui.level.guided_desc/operational_desc/crisis_desc`); Selected-only `IMG_CheckBadge` 30x30 white circle at top-right offset (-8,-8) (right -8, top -8), shadow 0/2/6 a0.25, with `ICN_CheckCircle_Filled` 24 (centre) in level colour (`level.guided` / `level.operational` / `level.crisis`).
- Default shadow.card; Hover 1.04 + shadow.hover; Pressed 0.98 + shadow.card; Selected = lift: shadow.selected (glow `brand.light` 0/0/18 + 0/8/20 a0.35) + check badge. Unity must not add a border.

### PANEL_ConfirmDialog  [UI_ConfirmDialog 86:331]
- 480 x 240 glass panel (`surface.glass`, radius.panel, padding 24, gap 12, shadow 0/20/30 a0.35 — Figma had backdrop blur 20: not at runtime). Vertical: `LBL_Title` Title Bold 32 on-dark; `LBL_Body` Body on-green wrapping; spacer; `PANEL_Actions` horizontal gap 12 right-aligned: `BTN_Primary`(Dark, 200x48, `ICN_Undo`, "Annulla", `ui.button.cancel`) = DEFAULT FOCUS + `BTN_Secondary` Danger ("Esci", `ui.button.exit`; icon `ICN_Exit`).
- Parametric (title, body, cancel label, confirm label+icon): Esci missione (`ui.dialog.exit_mission.title/body`), Ricomincia (`ui.button.restart`), Ripristina predefiniti (`ui.settings.general.reset_defaults`; **NEW** `ui.dialog.reset_defaults.title/body`, `ui.dialog.restart.title/body`).

## 1b. HUD components on the Components page (cluster at y 5534+)

Only sizes, colours and texts that survive. Wrist panel, alerts, subtitles follow `docs/Guida_HUD_comune_SI81.pdf` (CLAUDE §4.11) over the Figma cluster; the cluster of Figma is OUTDATED (fixed panel in front of the player).

| Figma set (node) | Unity | Status | Size / structure / tokens / texts |
|---|---|---|---|
| UI_HUD_Cluster 99:427 (Guided/Operational/Crisis x Timer Normal/Warning) | content moves to HUD_WristPanel | **SUPERSEDED by HUD PDF** (fixed panel at 1.2 m / Lazy Follow is forbidden, CLAUDE §4.11, §8). Reuse only content/colours | 420x150 opaque `surface.panel` radius.panel shadow 0/8/12 a0.35, pad 12/10 gap 6. Row1 objective pill (bg `brand.light`, radius.button, pad L8 R10 T/B4, gap 8): `ICN_RadioButtonChecked` 24 + text 24 Regular (Crisis/Operational variants differ in text only) + counter "2/4" HUD-min Bold 21 (`hud.wrist.objective_progress`; Guided & Operational show it, Crisis variant hides it). Row2: timer chip (`ICN_Timer` 32 + "TEMPO" HUD-min 21 + "04:36" Title Bold 32, all text.on-dark; `hud.wrist.time_label`, `hud.wrist.timer`) + `LBL_LevelBadge` Dark with 21 px text. **Warning timer** (Operational/Crisis, 00:28): chip bg `deny`, `ICN_Warning` 32, texts black. Guided: elapsed time, no warning. Row3 phases gap 10, 21px: Scan/Decision done = `ICN_CheckCircle_Filled` + text.on-green; Action = pill `state.active` radius.pill (pad L6 R10) with `ICN_RadioButtonChecked_Filled` + Bold black; Resolution = `ICN_RadioButtonUnchecked` + text.on-green. Keys `hud.phase.scan/decision/action/resolution`. Warning pulse <=1 Hz (reduce-flashes setting). |
| UI_HUD_Hint 99:428 | HUD_WristPanel hint row | in wrist panel (Guided only) | 420x40 `surface.panel`, radius.card, pad 12/8, gap 8: `ICN_Lightbulb` 24 `brand.light` + text 21 on-dark, <=60 chars. Demo: "Il pulsante d'allarme è rosso, accanto alla porta d'uscita". Text = mission data (no key). |
| UI_SubtitleBar 99:448 (Speaker OnScreen/OffScreen) | HUD_Subtitles | kept; placement per HUD PDF (CLAUDE §4.11: ~-20..-28 deg, 1 m, own lazy follow, max 32 chars x 2 lines) — Figma says 460x110 at 1.2 m / 25 deg and ~42 chars: PDF wins | 460 x 110, fill `subtitle.bg` (alpha .92), radius.card, pad 16/10, gap 4. Row event: `ICN_NotificationsActive` 24 + "[Allarme antincendio]" 21, colour `subtitle.event` (`hud.subtitles.sound_event` "[{0}]"). Row dialogue: "Marco:" Subtitle Bold 24 `subtitle.speaker` + text 24 Regular `subtitle.text`, max 2 lines (`hud.subtitles.speaker_line`). OffScreen: leading `ICN_ArrowLeft_Filled` 24 `subtitle.speaker` (rotates toward speaker) gap 6. Demo line "Marco: c'è fumo sotto la scrivania vicino alla finestra, vieni!" (too long for 32x2, shorten in mock). Critical alarms always subtitled. |
| UI_ActionFeedback 101:324 (Ok/Error/Warning) | HUD_ActionFeedback | kept (position: low in view, ~-15 deg, 3 s, one at a time, below alerts in priority) | 420 x 72 (hug), `surface.card`, radius.card, shadow 0/6/18 a0.35, pad L12 R16 T/B10, gap 10: icon 32 filled + text 21 Regular black, <=60 chars. Ok `ICN_CheckCircle_Filled` colour `confirm`; Error `ICN_Cancel_Filled` colour `deny`; Warning `ICN_Warning_Filled` colour `deny`. Demo: "Allarme dato: il coordinatore è stato avvisato" / "Acqua su un impianto elettrico: rischio di folgorazione" / "Il fumo aumenta: allontanati dalla scrivania". Level rule: Guided every action, Operational only critical errors, Crisis none. |
| UI_EventAlert 101:339 (Full/TitleOnly/IconOnly) | HUD_EventAlert | kept (highest priority; low in view -15 deg, 3 s, sound + haptic) | bg `deny`, black text/icon, radius.card, shadow as feedback. Full 420x102: `ICN_NotificationsActive_Filled` 32 + title Subtitle Bold 24 "Il fuoco si sta estendendo" + body 21 "Le fiamme hanno raggiunto il cestino: usa l'estintore ora"; TitleOnly 420x52 (icon + title); IconOnly 56x56 circle-ish (radius 40, pad 12, icon only). Level rule: Guided Full, Operational TitleOnly, Crisis IconOnly + sound (subtitled always). Pulse <=1 Hz. |
| UI_World_InteractionPrompt 101:340 | HUD_InteractionPrompt | kept, world-locked on anchor | card 260x72 `surface.card` radius.card shadow 0/6/18 a0.35, pad 12, gap 10: object icon 32 `brand.dark` (e.g. `ICN_FireExtinguisher`) + col gap 2: title Body Bold ("Estintore CO2") + action row gap 6: key pill (bg `brand.dark`, radius.button, pad-x 8, Body Bold on-dark, e.g. "GRIP" from IInputPromptProvider `hud.prompt.button.grip`) + verb Body ("Afferra", `hud.prompt.action.grab`). Below card: line 6x40 (black `risk.outer` with white `grab.inner` 2 wide centre = double stroke) + anchor dot 14x14. Total 260x126. Guided on aim; Operational after 0.5 s; Crisis never; hidden while held. |
| UI_World_RiskLabel 101:353 | HUD_RiskLabel | kept, max 3, Guided only | 392x120: dashed risk circle 120x120 (`risk.stroke` #D55E00 inside, `risk.outer` black outside; double stroke) + gap 12 + card 260 wide `surface.card` radius.card shadow, pad 12/8, gap 10: `ICN_Warning_Filled` 32 `deny` + col: title Body Bold ("Ciabatta surriscaldata") + instruction Body ("Stacca la corrente prima di intervenire"). Text = mission data. |
| UI_World_ContextHelp 101:374 (Collapsed/Expanded) | HUD_ContextHelp | kept, Guided only | Collapsed 40x40 circle `brand.dark`, shadow, `ICN_Help_Filled` 32 on-dark. Expanded 308x85: dot + gap 8 + card 260 wide `surface.card` radius.card pad 12/8: Body Bold title + Body 2-line text ("Estintore CO2" / "Adatto agli impianti elettrici: non lascia residui."). Pointed + selected opens. Only world label with raycaster. |
| UI_DecisionPanel 101:375 | HUD_DecisionPanel | kept, 1.2 m in front, opaque | 480 x 300 `surface.panel`, radius.panel, shadow 0/6/18 a0.35, pad 24/20, gap 10, vertical. Row question: `ICN_RecordVoiceOver` 32 `brand.light` + Subtitle Bold 24 on-dark ("Cosa dici al collega?"), gap 8. 2–4 option rows h64 full width, radius.button, pad-x 12, gap 10: icon 24 + text 21 black. Default `surface.card` Regular; pointed/hover = bg `brand.light` + Bold, NO border (selection confirm = haptic). Demo icons/text: `ICN_DirectionsRun` "Usciamo subito, chiudi la porta"; `ICN_FireExtinguisher` "Prendi l'estintore CO2 in corridoio" (pointed); `ICN_Call` "Chiama il 112 mentre do l'allarme". Timer keeps running only if mission says so. |
| UI_OutcomePanel 101:410 (Positive/Partial/Negative) | HUD_OutcomePanel | kept, 1.5 m centre, 3–5 s, jingle, fade to Debrief | 480 x 160 `surface.card`, radius.panel, shadow 0/6/18 a0.35, pad 24, gap 12, column centred: `LBL_Outcome` Large (full colour on this white card) + reason Subtitle 24 Regular black centred wrapping. Demo reasons: Positive "Incendio spento in 2:48 e allarme dato"; Partial "Incendio spento, ma allarme dato in ritardo"; Negative "Il fumo ha raggiunto le vie d'esodo". |

No new HUD keys (texts are mission data). Level-dependent visibility from SO_LevelConfig_* (Guided / Operational / Crisis as above).


## 2. Screens

Common to all menu screens: backdrop = pre-blurred skybox ("Scena sfocata (segnaposto)" is a mock-up layer, not UI); `PANEL_Menu` 760 wide at frame (40,40); `PANEL_BottomBar` 520x56 centred on the menu panel, 16 below (rect in Home/Stats/Profile/Settings frames: [160,531,520,56] -> frame 840x627 = menu 475 + 16 + 56 + margins 40). Frame sizes = bbox + 40 margin each side. Title row on top-level tabs = `LBL_Title` Title Bold 32 on-dark (no BTN_Back; Back only in drill-down screens). Content width 712.

### PANEL_Home  [Home · Scenari 63:423]
- Frame 840x627. PANEL_Menu [40,40,760,475] (content 464 +11 slack, top aligned). BottomBar Active=Home [160,531]. No side panel, no BTN_Back.
- Children (y relative to panel): `LBL_Title` "Scenari" (`ui.home.title`) [24,24]; `PANEL_TrainingCard` [24,76,712,56]; `PANEL_ScenarioGrid` [24,144,712,296]: 4 x 2 `BTN_ScenarioCard` 166x140, gap.grid 16 both axes (flow left->right). 
- Mock (8 cards, per CLAUDE brief §1.2 ids 01,02,03,04,07,08,09,10): thumbnail gradient start->end (147.28 deg, 71.4%) / icon / best / complexity / levels completed (filled bars):

| id · title | icon | gradient | best | cx | levels |
|---|---|---|---|---|---|
| 01 · Fire Starter | local_fire_department | #C8402A -> #F2994A | 4/5 | 2/5 | 2 |
| 02 · Blackout | flash_off | #3E1C66 -> #7B5BA6 | 3/5 | 3/5 | 1 |
| 03 · Man Down | ecg_heart | #1C6E80 -> #3FB8A0 | Nuovo | 2/5 | 0 |
| 04 · Chemical Alert | science | #24805C -> #76C98A | Nuovo | 4/5 | 0 |
| 07 · Forklift Chaos | forklift | #2B4A86 -> #5B86B5 | 3/5 | 3/5 | 1 |
| 08 · Machine Lock | precision_manufacturing | #3F4A55 -> #7D8C99 | Nuovo | 4/5 | 0 |
| 09 · Evacuation | door_open | #1B6E35 -> #58B65E | 2/5 | 3/5 | 1 |
| 10 · Electric Shock | electric_bolt | #8C7A12 -> #D9C43A | Nuovo | 4/5 | 0 |

- Click card -> PANEL_MissionBrief (mission), TrainingCard button -> PANEL_TrainingBrief. Cards come from `SO_MissionCatalog`; unavailable missions use the Disabled card variant.

### PANEL_MissionBrief  [Mission Brief · A 10:2 / B 10:331]
- Frame 1184x687. `PANEL_Side` [40,40,320,161] (yaw 20 deg toward user) ; `PANEL_Menu` [384,40,760,535] ; `PANEL_BottomBar` [504,591,520,56] (Active = Home, centred on the menu panel x=764).
- PANEL_Menu children (relative to panel, padding 24, gap 12): 
  1. `PANEL_Header` [24,24,712,63] (see §0): `BTN_Back`, breadcrumb `ui.brief.breadcrumb`, title "01 · Fire Starter" Title on-dark, complexity (5 dots 10x10 gap 4 + Body on-dark "2/5"), spacer, pill "Miglior risultato" + `LBL_StarRating`(4).
  2. `PANEL_Description` [24,99,712,185]: white card radius.card shadow.card padding 12 gap 6: `LBL_DescTitle` Subtitle 24 text.primary "Descrizione" (`ui.brief.description`); `LBL_Context` Body 18 (up to 280 chars, 3 lines; mission data): "Ruolo: addetto alla squadra antincendio di piano. Open space al 2° piano, pausa pranzo: una ciabatta multipresa sovraccarica sotto una scrivania inizia a fumare. Riconosci il principio d'incendio, dai l'allarme e intervieni in sicurezza prima che il fumo raggiunga le vie d'esodo."; `PANEL_Objectives` 2 columns gap 16, rows gap 4, Body 18, numbered objectives (col1: 1,3; col2: 2,4): 1 "Individua la fonte del fumo e valuta il rischio", 2 "Dai l'allarme al coordinatore dell'emergenza", 3 "Togli corrente alla ciabatta multipresa", 4 "Spegni il principio d'incendio con l'estintore".
  3. `PANEL_LevelPicker` [24,296,712,155]: `LBL_ChooseLevel` Subtitle 24 Regular on-dark (`ui.brief.choose_difficulty`) + gap 8 + row gap 16 of 3 `BTN_DifficultyCard` 226x117 at x 0/242/484 (Guided/Operational/Crisis; texts `ui.level.*_desc`).
  4. `PANEL_Actions` [24,463,712,48]: `BTN_Primary` Dark 200x48 right-aligned (x 512).
- Side panel texts: see §0 (criteria rows are mission data).
- State A (no level): no card selected; `BTN_Primary` Disabled: fill tile.neutral 40%, black text "Scegli un livello" (`ui.button.choose_level`), no icon/border (Figma note 5: interpreted as #ECECEC at 40% + full black text, ~6.6:1). Happens only if "last played" preselection is missing (default preselect = last played else Guided).
- State B (Operational selected): card 2 `Selected` (lift + glow + white ✓ circle with `level.operational` check), others Default; primary Enabled "Avvia scenario" with `ICN_PlayArrow`.
- Heights: 24+63+12+185+12+155+12+48+24 = 535 (open point: panel 535 instead of 475, CLAUDE §4.10). Score never shown (stars only).

### PANEL_Stats  [Statistiche · A 103:391 / B 103:591]
- Frame 840x627. PANEL_Menu [40,40,760,475]; BottomBar Active=Statistiche [160,531]. `LBL_Title` "Statistiche" (`ui.stats.title`) Title on-dark.
- `PANEL_StatsContent` [24,76,712,~375] horizontal, gap 16, two columns:
  - Left `PANEL_List` 440 wide, vertical gap 6: 8 `BTN_ListRow` (440x40) (rows: 01 Fire Starter 4/5 86/100; 02 Blackout 3/5 72/100; 03 Man Down NotPlayed; 04 Chemical Alert NotPlayed; 07 Forklift Chaos 3/5 64/100; 08 Machine Lock NotPlayed; 09 Evacuation 2/5 58/100; 10 Electric Shock NotPlayed). Rows come from SO_MissionCatalog.
  - Right `PANEL_Detail` flex (256 wide), full height, white card radius.card shadow.card padding 12:
    - **A Riepilogo** (gap 8): `LBL_DetailTitle` "Riepilogo" Subtitle 24 text.primary (`ui.stats.summary`); `PANEL_Tiles` wrap gap 8: 4 `PANEL_StatTile` (Positive 5, Partial 2, Negative 2, Neutral "Sessioni" 9); `LBL_Average` Body text.secondary "Punteggio sicurezza medio 70/100 · 4 scenari giocati su 8" (`ui.stats.average_line`).
    - **B row selected** (01 row = bg brand.light; gap 6): header row gap 8: `BTN_Back` 40x40 (fill tile.neutral, in-card, returns to summary) + `LBL_Title` "01 · Fire Starter" Body Bold; `PANEL_Best` gap 2: `LBL_Best` Body Bold "Punteggio migliore 86/100" (`ui.stats.best_score` + " " + **NEW** `ui.format.score_of_100` = "{0}/100") + `LBL_StarRating`(4); `LBL_LastAttempt` Body Bold "Ultimo tentativo" (`ui.stats.last_attempt`); 5 x `LBL_DimensionBar` Light (Safety 82, Awareness 74, Decision 90, Execution 68, Time 71) height 23 each; `LBL_Attempts` Body text.secondary "4 tentativi · livello max:" (`ui.stats.attempts_max_level`) + `LBL_LevelBadge` Light (level 2).
- Selecting a row changes A -> B; Back in the detail returns to A. Score text always "Punteggio sicurezza N/100" (never "SPI").

### PANEL_Profile  [Profilo 103:797]
- Frame 840x627. PANEL_Menu [40,40,760,475]; BottomBar Active=Profilo. Title "Profilo" (`ui.profile.title`). Single profile only (no multiple profiles / no Account-Logout: CLAUDE §8).
- Content [24,76,712,375] horizontal gap 16:
  - Left `PANEL_Identity` 240 wide, vertical gap 6: `IMG_Avatar` 120x120 circle `brand.light` + `ICN_Engineering_Filled` 72 `brand.dark`; `LBL_Name` Subtitle Bold 24 on-dark ("Operatore_Luca"; **NEW** profile data, no key); `LBL_MissionsDone` Body on-green "Missioni completate 3/8" (`ui.profile.missions_completed`); 6 gap; `LBL_RecentTitle` Body Bold on-dark (`ui.profile.recent_results`); 3 rows (Body on-dark title, spacer, `ICN_Star_Filled` 20 `brand.light`, Body on-green "4/5 · 86/100"): 01 · Fire Starter 4/5 · 86/100; 02 · Blackout 3/5 · 72/100; 07 · Forklift Chaos 3/5 · 64/100.
  - Right `PANEL_Skills` flex (456 wide), vertical gap 12: `PANEL_Competenze` gap 6: `LBL_SkillsTitle` Body Bold on-dark (`ui.profile.skills`) + 5 `LBL_DimensionBar` Dark (Safety 76, Awareness 70, Decision 81, Execution 64, Time 69); `PANEL_Badges` gap 8: `LBL_BadgesTitle` Body Bold on-dark "Badge · 5 su 12" (`ui.profile.badges_count`) + wrap grid gap 12 of `BTN_Badge`: 5 Unlocked (`ICN_WorkspacePremium_Filled`, `ICN_LocalFireDepartment_Filled`, `ICN_NotificationsActive_Filled`, `ICN_Bolt_Filled`, `ICN_Timer_Filled`) + 7 Locked. 12 badges = 6 per row x 2 rows.
- Gap vs CLAUDE §4.6: Figma Profilo shows per-mission "N/5 · NN/100" and bars but no overall "Punteggio sicurezza N/100" tile (see open points).

### PANEL_Settings  [Impostazioni · 6 frames 104:1011 / 1119 / 1211 / 1290 / 1362 / 1439]
- Frame 840x627. PANEL_Menu [40,40,760,475]; BottomBar Active=Impostazioni. Title "Impostazioni" (`ui.settings.title`) Title on-dark. Content [24,76,712,375] horizontal gap 16: `PANEL_Tabs` 180 wide vertical gap 4 (6 x `BTN_SettingsTab` 180x48 = 308 high) + `PANEL_Rows` 516 wide vertical gap 4 (rows 48 high each; max 7 per tab).
- Active tab: Active variant (white bg, filled icon, brand.dark Bold); others Default. Tab order: Comfort, Audio, Sottotitoli, Controlli, Accessibilità, Generale.

| Tab (key) / icon | Row (type) | Label key | Control values (mock) |
|---|---|---|---|
| Comfort `ui.settings.tab.comfort` / self_improvement | 1 Selector | `ui.settings.comfort.locomotion` | Teletrasporto (`..locomotion_teleport`) / Movimento continuo (`..locomotion_continuous`) |
| | 2 Selector | `ui.settings.comfort.turn` | Snap (`..turn_snap`) / Continua (`..turn_continuous`) |
| | 3 Selector | `ui.settings.comfort.snap_angle` | 45° (`ui.format.degrees`; values 30/45/60/90 proposed) |
| | 4 Selector | `ui.settings.comfort.vignette` | Bassa (`..vignette_off/low/medium/high`) |
| | 5 Action Button | `ui.settings.comfort.user_height` | action "Ricalibra" (`ui.settings.comfort.recalibrate`), icon `ICN_Refresh` |
| | 6 Toggle | `ui.settings.comfort.seated_mode` | OFF |
| Audio `ui.settings.tab.audio` / volume_up | 1-5 Slider | `ui.settings.audio.master_volume`, `.voices`, `.effects`, `.alarms`, `.music` | 80 each (mock), 0–100, tick 10 |
| Sottotitoli `ui.settings.tab.subtitles` / subtitles | 1 Toggle | `ui.settings.subtitles.enabled` | ON |
| | 2 Selector | `ui.settings.subtitles.size` | 100% (`ui.format.percent`; steps e.g. 80/100/125/150) |
| | 3 Slider | `ui.settings.subtitles.background_opacity` | 92 shown "92%" (min 92 per `subtitle.bg` alpha rule; slider writes % text) |
| Controlli `ui.settings.tab.controls` / sports_esports | 1 Selector | `ui.settings.controls.dominant_hand` | Destra / Sinistra (`..hand_right/left`) — swaps HUD_WristPanel hand |
| | 2 Toggle | `ui.settings.controls.ui_haptics` | ON |
| Accessibilità `ui.settings.tab.accessibility` / accessibility_new | 1 Selector | `ui.settings.accessibility.ui_text_size` | 100% |
| | 2 Toggle | `ui.settings.accessibility.reduce_flashes` | OFF |
| | ~~3 Toggle~~ | ~~"Comfort visivo (riduzione luce blu)"~~ | **DO NOT BUILD** (CLAUDE §8; listed in strings_it.json `excluded`) |
| Generale `ui.settings.tab.general` / tune | 1 Action Value (lock) | `ui.settings.general.language` | Italiano (`..language_italian`), read-only |
| | 2 Action Button | `ui.settings.general.replay_training` | "Avvia" (`..start`), icon `ICN_School` |
| | 3 Action Button | `ui.settings.general.info` | "Apri" (`..open`), icon `ICN_Info` |
| | 4 Action Button | `ui.settings.general.reset_defaults` | "Ripristina" (`..reset`), icon `ICN_SettingsBackupRestore` -> opens PANEL_ConfirmDialog (**NEW** `ui.dialog.reset_defaults.title/body`) |

- Rows are `PANEL_Settings*` prefabs instantiated from a data list (`SettingsRowDefinition`); heights: Controlli 100, Accessibilità 152, Generale 204 (stack height = n x 48 + (n-1) x 4). Info "Apri" content not designed (open point).

### PANEL_TrainingBrief  [Training · brief 106:1667]
- Frame 840x627. PANEL_Menu [40,40,760,475]; BottomBar [160,530,520,56] Active=Home. Header: `BTN_Back` + breadcrumb `ui.training.breadcrumb` (Body on-green) + `ui.training.title` (Title on-dark).
- `PANEL_Learn` white card (radius.card, shadow.card, padding 12, gap 10, full width): title Subtitle 24 text.primary (`ui.training.what_you_learn`); wrap grid gap 10 (row) x 16 (col) of 5 items 336x40: `IMG_Icon` circle 40 `brand.light` radius 20 with icon 24 `brand.dark` + col: `LBL_Title` Body Bold + `LBL_Desc` Body text.secondary.
  Items: `ui.training.locomotion.*` directions_walk; `ui.training.turn.*` rotate_right; `ui.training.grab.*` back_hand; `ui.training.select.*` ads_click; `ui.training.pause.*` pause_circle.
- Note line Body on-green (`ui.training.no_score_note`, wraps full width); spacer; `BTN_Primary` Dark 200x48 right-aligned "Avvia training" (`ui.button.start_training`).

### PANEL_TrainingComplete  [Training · completato 106:1748]
- Frame 840x560. Centred panel [140,80,560,400] glass (radius.panel, shadow 0/20/30 a0.30, sheen), padding 24/20, gap 6, column centred (in-scene: 1.5 m, no bottom bar).
- Children: `IMG_Check` circle 60 (radius 30) `brand.light` + `ICN_Check` 48 `brand.dark`; `LBL_Title` Title on-dark (`ui.training.complete_title`); `LBL_Body` Body on-green "Ora conosci i comandi base. Puoi iniziare le missioni." (`ui.training.complete_body`); `PANEL_Learned` white card (radius.card, shadow.card, padding 12, gap 4) with 4 rows (`ICN_CheckCircle_Filled` 24 `confirm` + Body): "Muoverti e girarti" (`ui.training.learned.move`), "Afferrare e rilasciare gli oggetti" (`.grab`), "Selezionare i pannelli con il raggio" (`.select`), "Aprire la Pausa" (**NEW** `ui.training.learned.pause`); spacer; `BTN_Primary` Dark **240** x48 "Torna alla Home" with `ICN_Home` (`ui.button.back_to_home`).

### PANEL_Pause  [Pausa 105:1617 / conferma Esci 105:1676]
- Frame 840x560 (in-scenario, world-locked 1.5 m centre). Scene dimmed (black 40%; timer, NPC, events frozen — `IMG_Dim` full-frame rgba(0,0,0,.4)). Panel `Pausa · 560 x 400` at [140,80]: glass, radius.panel, shadow 0/20/30 a0.35, sheen, padding 24, gap 16. No BottomBar.
- Children: `LBL_Title` Title on-dark "01 · Fire Starter · Pausa" (`ui.pause.title`); `PANEL_Header` [24,80,512,23] gap 12: `LBL_LevelBadge` Dark + `LBL_Phase` pill (fill `state.active`, radius.pill, padding-x 10, Body Bold black "Fase: Action", `ui.pause.phase` + `hud.phase.*`); `PANEL_Content` [24,119,512,257] gap 16: left `PANEL_Objectives` flex (256 wide) gap 4: `LBL_ObjTitle` Body Bold on-dark (`ui.pause.objectives`) + `LBL_ObjectiveItem` list (Done "Individua la fonte del fumo", Done "Valuta il rischio", Current "Dai l'allarme al coordinatore", Todo "Spegni con l'estintore CO2"); right `PANEL_Buttons` 240 wide gap 8: `BTN_Primary` Dark 240x48 "Riprendi" (`ui.button.resume`) ; `BTN_Secondary` Neutral "Ricomincia" (`ui.button.restart`, `ICN_RestartAlt`); `BTN_Secondary` Neutral "Impostazioni rapide" (`ui.button.quick_settings`, `ICN_Tune`); `BTN_Secondary` "Esci alla Home" (`ui.button.exit_to_home`, `ICN_Exit`; **Figma: Neutral white**, Danger only inside the dialog).
- Confirm Esci: same screen + extra dim overlay (`oscuramento dietro la conferma`, full frame) + `PANEL_ConfirmDialog` 480x240 at [180,160] (title `ui.dialog.exit_mission.title`, body `ui.dialog.exit_mission.body`, default focus Annulla). Ricomincia also uses the dialog (**NEW** `ui.dialog.restart.title/body`). Pause opens with Menu-left / B-Y held 1 s (training copy).

### PANEL_Debrief  [Debrief 106:1482]
- Frame 1184x555. `PANEL_Side` [40,40,320,475] (glass, side panel full height 475, tilt as menu): `LBL_Title` Subtitle Bold 24 on-dark "Percorso corretto" (`ui.debrief.correct_path`) + up to 6 steps (gap 12): `LBL_StepNum` 26x26 circle `brand.light` (radius 13) with Body Bold black number + `LBL_Step` Body on-dark wrapping. Mock: 1 "Individua il fumo e la ciabatta surriscaldata"; 2 "Dai l'allarme con il pulsante più vicino"; 3 "Avvisa il coordinatore dell'emergenza"; 4 "Togli corrente dall'interruttore del piano"; 5 "Usa l'estintore CO2, mai acqua sull'impianto"; 6 "Allontana i colleghi verso le vie d'esodo" (mission data).
- `PANEL_Menu` [384,40,760,475]: header row gap 12: `LBL_Title` Title on-dark "Debrief · 01 · Fire Starter" (`ui.debrief.title`) + `LBL_LevelBadge` Dark + spacer + `LBL_Outcome` Small **onGlass** (white pill, coloured icon `ICN_Contrast` brand.dark + Body Bold black "Esito parziale"). Card `PANEL_Decisions` (white, radius.card, shadow.card, padding 12, gap 6, full width): title Subtitle 24 text.primary "Decisioni" (`ui.debrief.decisions`) + rows (icon 24 + Body, gap 8): `ICN_CheckCircle_Filled` `confirm` "Hai dato l'allarme subito → i colleghi sono usciti in tempo" (`ui.debrief.cause_effect` "{0} → {1}"), same for "Hai scelto l'estintore CO2 → nessun rischio elettrico", `ICN_Cancel_Filled` `deny` "Hai avvisato il coordinatore dopo 2 min → squadra arrivata tardi". Card `PANEL_Errors` (same style): "Errori" (`ui.debrief.errors`) + `ICN_Warning_Filled` `deny` "Critico · Ti sei avvicinato al fumo senza abbassarti → esposizione al fumo" (`ui.debrief.critical_prefix`) + `ICN_Cancel_Filled` `deny` "Non hai chiuso la porta dell'ufficio → il fumo è arrivato in corridoio". Spacer; `BTN_Primary` Dark 200x48 right "Avanti" with `ICN_ArrowForward` (`ui.button.next`) -> Score & Replay.
- No BottomBar, no BTN_Back. Panel 475 high (rows must fit; list scroll not designed).

### PANEL_ScoreReplay  [Score & Replay 106:1566]
- Frame 840x555. `PANEL_Menu` [40,40,760,475] glass; no BottomBar. Header: `LBL_Title` Title "Risultato · 01 · Fire Starter" (`ui.score.title`) + `LBL_LevelBadge` Dark + spacer + `LBL_Outcome` Small onGlass ("Esito parziale").
- Content [24,~87,712,~300] horizontal gap 16:
  - Left `PANEL_Score` white card 300 wide, full height, radius.card, shadow.card, padding 16, gap 4: `LBL_ScoreLabel` Body text.secondary "Punteggio sicurezza"; `LBL_ScoreNumber` baseline row gap 6: "78" Display Bold 64 text.primary + "/100" Subtitle 24 text.secondary (=`ui.format.safety_score` split); `LBL_RankLabel` Body text.secondary "Rank" (`ui.score.rank`); `LBL_StarRating` (3); 8 gap; `LBL_NewBadges` Body Bold (`ui.score.new_badges`); row gap 12: `BTN_Badge` Unlocked 64 (`ICN_NotificationsActive_Filled`) + Body "Allarme lampo" (badge name, data).
  - Right `PANEL_Dimensions` flex (~396), gap 8: title Body Bold on-dark (`ui.score.dimensions_vs_previous`) + 5 rows (gap 8): `LBL_DimensionBar` Dark (flex) + `LBL_Delta` Body Bold 36 wide right-aligned (`ui.format.delta_positive` "+{0}" in `brand.light`; `ui.format.delta_negative` "−{0}" (U+2212) in text.on-green). Mock: Safety 80 +6, Awareness 72 +3, Decision 85 +10, Execution 70 −2, Time 64 +1.
  - Actions row right gap 12: `BTN_Secondary` "Home" (`ICN_Home`, `ui.nav.home`), `BTN_Secondary` "Cambia livello" (`ICN_Tune`, `ui.button.change_level`), `BTN_Primary` Dark 200x48 "Riprova" (`ICN_Replay`, `ui.button.retry`).
- Flow: Home -> Mission Brief -> scene -> Resolution -> Debrief -> Score&Replay -> Home.

### HUD_DecisionPanel / HUD_OutcomePanel (Resolution)  [HUD · Decisione 105:1378 / Resolution · esito 105:1451]
- Frames 1100x760 scene mock-ups. Component specs in §1b. DecisionPanel 480x300 in front 1.2 m, opaque; OutcomePanel 480x160 white card, 1.5 m centre, 3–5 s no input, jingle per outcome, fades to Debrief. Both world-locked; nothing above +10 deg.

### HUD screens (Livello 1 Guided 105:1107, Livello 2 Operational 105:1221, Livello 3 Crisis 105:1312)  — OUTDATED (CLAUDE §4.11)
- Cluster at (340,270) 420x150, event alert above (340,160), action feedback (340,190), subtitles (320,432), world labels at top (risk label 40,40; context help 470,60; prompt 800,40). Only reuse: the texts/colours of §1b, the per-level visibility rules (Guided: risk labels, context help, prompt always, alert Full, feedback every action, hint; Operational: prompt after 0.5 s, feedback critical errors only, alert TitleOnly, no risk labels; Crisis: only main objective, alert IconOnly + sound, no feedback/prompts, timer warning, subtitles always), and the wrist panel content (objective + counter, timer mm:ss, level badge, phase pills, hint) moved to HUD_WristPanel (12 x 7 cm world canvas on the non-dominant controller). "Guida HUD · zone" frame 122:1578 is a documentation image: ignore.

## 3. Open points / Figma vs CLAUDE

1. BTN_Primary keeps 2-unit border (on-dark white / on-light brand.dark) — open point in Figma note 18; state variants have none.
2. PANEL_MissionBrief height 535 (not 475) and PANEL_Menu fixed 475 on Home with 11 slack; Training/Score/Debrief/Stats/Profile/Settings all 475.
3. BTN_BottomBarItem hover = tile.neutral pill, no scale (CLAUDE §4.13) while Figma Hover has size 1.0; Pressed not in Figma (use 0.98).
4. BTN_SettingsTab Hover = `brand.dark` fill on a `brand.dark` glass panel: nearly invisible; Unity should add scale 1.04 or a lighter overlay (decision needed).
5. BTN_ListRow Selected = brand.light fill only (no ✓/glow); CLAUDE §4.1 selected = lift + glow + ✓. Used fill-only here because rows sit on a white-card/glass list.
6. PANEL_TrainingCard hover 1.02 (not 1.04) and pressed 0.99 in Figma; keep.
7. HUD cluster/hint/subtitle/feedback placement in Figma (1.2 m, -14/-24/-25/-33 deg, "slot messaggi") is superseded by the HUD PDF (wrist panel, ~-15 deg alerts, -20..-28 deg subtitles, max 32 chars x 2 lines vs Figma ~42 chars). Wrist panel objective max ~30 chars.
8. "Comfort visivo (riduzione luce blu)" row exists in Figma Accessibilità: excluded (CLAUDE §8). Accessibilità therefore has 2 rows.
9. Profile has no overall "Punteggio sicurezza N/100" (CLAUDE §4.6 says shown in Profilo); only per-mission "N/5 · NN/100" + skills bars. Badge detail card (BTN_Badge select), "Informazioni → Apri" content and Training "Avvia" flow details are not designed.
10. Pausa "Esci alla Home" is Neutral white in Figma (icon logout); Danger only in the confirm dialog. Complexity dots (5 x 10, filled/empty SVGs) and ScenarioCard complexity dots (52x8) colours not readable from SVG: use `level.bar.on-dark/off-dark` (dark header) and `level.bar.on-light/off-light` (white card).
11. Figma blur values (20/30/40/60) and `backdrop-blur` in CSS are not reproduced at runtime (CLAUDE §4.4); shadows baked as sprites (no Shadow component).
12. Strikethrough on Done objectives is stated in the description but not drawn in the Figma variant: implement as `~~text~~` / TMP strikethrough.
13. New text keys proposed: `ui.format.score_of_100`, `ui.home.unavailable`, `ui.dialog.reset_defaults.title/body`, `ui.dialog.restart.title/body`, `ui.training.learned.pause`, (profile name and mission-derived texts are data, not keys).
