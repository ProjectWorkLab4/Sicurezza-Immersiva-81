using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>Le 6 schede di PANEL_Settings, nell'ordine in cui compaiono (CLAUDE.md §4.8).</summary>
    public enum SettingsTabId
    {
        /// <summary>Comfort: locomozione, rotazione, vignettatura, altezza, modalità seduto.</summary>
        Comfort = 0,
        /// <summary>Audio: volumi.</summary>
        Audio = 1,
        /// <summary>Sottotitoli: attivazione, dimensione, opacità dello sfondo.</summary>
        Subtitles = 2,
        /// <summary>Controlli: mano dominante, aptica UI.</summary>
        Controls = 3,
        /// <summary>Accessibilità: dimensione testo UI, riduzione lampeggi (nessun filtro luce blu, CLAUDE.md §8).</summary>
        Accessibility = 4,
        /// <summary>Generale: lingua, rivedi training, informazioni, ripristino predefiniti.</summary>
        General = 5
    }

    /// <summary>Tipo di controllo di una riga di Impostazioni (corrisponde a un prefab PANEL_Settings*).</summary>
    public enum SettingsRowType
    {
        /// <summary>PANEL_SettingsToggle (ON/OFF).</summary>
        Toggle,
        /// <summary>PANEL_SettingsSlider (intervallo con passo).</summary>
        Slider,
        /// <summary>PANEL_SettingsSelector (scelta a frecce tra opzioni).</summary>
        Selector,
        /// <summary>PANEL_SettingsAction, tipo Button (pulsante azione).</summary>
        Action,
        /// <summary>PANEL_SettingsAction, tipo Value (valore di sola lettura + lucchetto).</summary>
        Value
    }

    /// <summary>
    /// Definizione dati di UNA riga di Impostazioni: id stabile nello store, tipo, chiave dell'etichetta, valori e opzioni.
    /// Un'unica fonte per schermata, builder e codice futuro (es. HUD_WristPanel legge <see cref="SettingsCatalog.DOMINANT_HAND"/>).
    /// </summary>
    public sealed class SettingsRowDefinition
    {
        /// <summary>Id stabile dell'impostazione nello store (es. "audio.master_volume"). NON cambiare dopo il rilascio: è salvato su disco.</summary>
        public string Id;
        /// <summary>Tipo di controllo.</summary>
        public SettingsRowType Type;
        /// <summary>Chiave dell'etichetta in SO_UIStrings_It.</summary>
        public string LabelKey;
        /// <summary>Predefinito dei Toggle.</summary>
        public bool DefaultBool;
        /// <summary>Predefinito dei Selector (Value di una delle opzioni) e, arrotondato, degli Slider.</summary>
        public int DefaultInt;
        /// <summary>Minimo dello Slider.</summary>
        public float Min;
        /// <summary>Massimo dello Slider.</summary>
        public float Max = 100f;
        /// <summary>Passo dello Slider.</summary>
        public float Step = 1f;
        /// <summary>Chiave di formato del valore dello Slider (es. ui.format.percent); vuota = numero semplice.</summary>
        public string FormatKey = string.Empty;
        /// <summary>Opzioni dei Selector.</summary>
        public SettingsOption[] Options = new SettingsOption[0];
        /// <summary>Id dell'azione (righe Action/Value), es. "reset_defaults".</summary>
        public string ActionId = string.Empty;
        /// <summary>Nome dell'icona ICN_ del pulsante azione (es. "ICN_Refresh").</summary>
        public string ActionIconName = string.Empty;
        /// <summary>Chiave del testo del pulsante azione (es. ui.settings.general.reset).</summary>
        public string ActionTextKey = string.Empty;
        /// <summary>Chiave del valore di sola lettura (righe Value).</summary>
        public string ValueKey = string.Empty;
        /// <summary>true se l'azione chiede conferma (PANEL_ConfirmDialog) prima di partire.</summary>
        public bool RequiresConfirmation;

        /// <summary>
        /// Crea la definizione di un Toggle.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="labelKey">Chiave dell'etichetta.</param>
        /// <param name="defaultValue">Predefinito.</param>
        public static SettingsRowDefinition MakeToggle(string id, string labelKey, bool defaultValue)
        {
            return new SettingsRowDefinition { Id = id, Type = SettingsRowType.Toggle, LabelKey = labelKey, DefaultBool = defaultValue };
        }

        /// <summary>
        /// Crea la definizione di uno Slider.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="labelKey">Chiave dell'etichetta.</param>
        /// <param name="min">Minimo.</param>
        /// <param name="max">Massimo.</param>
        /// <param name="step">Passo.</param>
        /// <param name="defaultValue">Predefinito.</param>
        /// <param name="formatKey">Chiave di formato del valore (vuota = numero).</param>
        public static SettingsRowDefinition MakeSlider(string id, string labelKey, float min, float max, float step, int defaultValue, string formatKey = "")
        {
            return new SettingsRowDefinition
            {
                Id = id, Type = SettingsRowType.Slider, LabelKey = labelKey, Min = min, Max = max, Step = step,
                DefaultInt = defaultValue, FormatKey = formatKey ?? string.Empty
            };
        }

        /// <summary>
        /// Crea la definizione di un Selector.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="labelKey">Chiave dell'etichetta.</param>
        /// <param name="defaultValue">Value dell'opzione predefinita.</param>
        /// <param name="options">Opzioni nell'ordine delle frecce.</param>
        public static SettingsRowDefinition MakeSelector(string id, string labelKey, int defaultValue, params SettingsOption[] options)
        {
            return new SettingsRowDefinition { Id = id, Type = SettingsRowType.Selector, LabelKey = labelKey, DefaultInt = defaultValue, Options = options };
        }

        /// <summary>
        /// Crea la definizione di una riga con pulsante azione.
        /// </summary>
        /// <param name="id">Id della riga.</param>
        /// <param name="labelKey">Chiave dell'etichetta.</param>
        /// <param name="actionId">Id dell'azione.</param>
        /// <param name="iconName">Icona ICN_ del pulsante.</param>
        /// <param name="textKey">Chiave del testo del pulsante.</param>
        /// <param name="requiresConfirmation">true se serve conferma.</param>
        public static SettingsRowDefinition MakeAction(string id, string labelKey, string actionId, string iconName, string textKey, bool requiresConfirmation = false)
        {
            return new SettingsRowDefinition
            {
                Id = id, Type = SettingsRowType.Action, LabelKey = labelKey, ActionId = actionId, ActionIconName = iconName,
                ActionTextKey = textKey, RequiresConfirmation = requiresConfirmation
            };
        }

        /// <summary>
        /// Crea la definizione di una riga di sola lettura (valore + lucchetto).
        /// </summary>
        /// <param name="id">Id della riga.</param>
        /// <param name="labelKey">Chiave dell'etichetta.</param>
        /// <param name="valueKey">Chiave del valore mostrato.</param>
        public static SettingsRowDefinition MakeValue(string id, string labelKey, string valueKey)
        {
            return new SettingsRowDefinition { Id = id, Type = SettingsRowType.Value, LabelKey = labelKey, ValueKey = valueKey, ActionId = id };
        }
    }

    /// <summary>Una scheda di PANEL_Settings: chiave dell'etichetta, icone e righe (massimo <see cref="SettingsCatalog.MAX_ROWS_PER_TAB"/>).</summary>
    public sealed class SettingsTabDefinition
    {
        /// <summary>Identificativo della scheda.</summary>
        public SettingsTabId Id;
        /// <summary>Chiave dell'etichetta (ui.settings.tab.*).</summary>
        public string LabelKey;
        /// <summary>Icona contorno (es. ICN_SelfImprovement).</summary>
        public string IconOutlineName;
        /// <summary>Icona piena, per la scheda attiva (es. ICN_SelfImprovement_Filled).</summary>
        public string IconFilledName;
        /// <summary>Righe della scheda, dall'alto in basso.</summary>
        public SettingsRowDefinition[] Rows;
    }

    /// <summary>
    /// CATALOGO DELLE IMPOSTAZIONI (unica fonte): 6 schede, righe, id stabili, tipi, etichette, opzioni e predefiniti.
    /// Lo usano <see cref="SettingsScreen"/> (collega le righe), il builder (crea le righe dei prefab) e il codice di gioco
    /// (leggere un valore: <c>UIServices.Settings.GetInt(SettingsCatalog.DOMINANT_HAND)</c>). Valori enumerati dei Selector:
    /// vedi le costanti <c>*_RIGHT</c>, <c>*_LEFT</c> ecc. Esclusi per decisione di team (CLAUDE.md §8): impostazioni grafiche,
    /// Account/Logout, difficoltà globale, filtro luce blu.
    /// </summary>
    public static class SettingsCatalog
    {
        /// <summary>Massimo di righe per scheda (CLAUDE.md §4.8).</summary>
        public const int MAX_ROWS_PER_TAB = 7;

        // ---- Comfort ----
        /// <summary>Locomozione: 0 = teletrasporto, 1 = movimento continuo.</summary>
        public const string COMFORT_LOCOMOTION = "comfort.locomotion";
        /// <summary>Modalità di rotazione: 0 = a scatti (snap), 1 = continua.</summary>
        public const string COMFORT_TURN_MODE = "comfort.turn_mode";
        /// <summary>Angolo dello scatto in gradi (30, 45, 60, 90).</summary>
        public const string COMFORT_SNAP_ANGLE = "comfort.snap_angle";
        /// <summary>Vignettatura: 0 = spenta, 1 = bassa, 2 = media, 3 = alta.</summary>
        public const string COMFORT_VIGNETTE = "comfort.vignette";
        /// <summary>Riga azione "Altezza utente" (nessun valore nello store; azione <see cref="ACTION_RECALIBRATE"/>).</summary>
        public const string COMFORT_USER_HEIGHT = "comfort.user_height";
        /// <summary>Modalità seduto (bool).</summary>
        public const string COMFORT_SEATED_MODE = "comfort.seated_mode";

        // ---- Audio (float 0-100) ----
        /// <summary>Volume generale (0-100).</summary>
        public const string AUDIO_MASTER_VOLUME = "audio.master_volume";
        /// <summary>Volume voci (0-100).</summary>
        public const string AUDIO_VOICES = "audio.voices";
        /// <summary>Volume effetti (0-100).</summary>
        public const string AUDIO_EFFECTS = "audio.effects";
        /// <summary>Volume allarmi (0-100).</summary>
        public const string AUDIO_ALARMS = "audio.alarms";
        /// <summary>Volume musica (0-100).</summary>
        public const string AUDIO_MUSIC = "audio.music";

        // ---- Sottotitoli ----
        /// <summary>Sottotitoli attivi (bool). Gli allarmi critici restano sempre sottotitolati.</summary>
        public const string SUBTITLES_ENABLED = "subtitles.enabled";
        /// <summary>Dimensione dei sottotitoli in percentuale (80, 100, 125, 150).</summary>
        public const string SUBTITLES_SIZE = "subtitles.size";
        /// <summary>Opacità dello sfondo dei sottotitoli in percentuale (92-100, minimo 92 per la leggibilità).</summary>
        public const string SUBTITLES_BACKGROUND_OPACITY = "subtitles.background_opacity";

        // ---- Controlli ----
        /// <summary>MANO DOMINANTE (int): <see cref="HAND_RIGHT"/> = 0, <see cref="HAND_LEFT"/> = 1. HUD_WristPanel va sulla mano NON dominante.</summary>
        public const string DOMINANT_HAND = "controls.dominant_hand";
        /// <summary>Aptica della UI attiva (bool).</summary>
        public const string UI_HAPTICS = "controls.ui_haptics";

        // ---- Accessibilità ----
        /// <summary>Dimensione del testo della UI in percentuale (100, 125, 150).</summary>
        public const string ACCESSIBILITY_UI_TEXT_SIZE = "accessibility.ui_text_size";
        /// <summary>Riduci lampeggi (bool): HUD_EventAlert e timer non pulsano.</summary>
        public const string ACCESSIBILITY_REDUCE_FLASHES = "accessibility.reduce_flashes";

        // ---- Generale (righe senza valore nello store) ----
        /// <summary>Riga Lingua (sola lettura: Italiano).</summary>
        public const string GENERAL_LANGUAGE = "general.language";
        /// <summary>Riga "Rivedi training".</summary>
        public const string GENERAL_REPLAY_TRAINING = "general.replay_training";
        /// <summary>Riga "Informazioni".</summary>
        public const string GENERAL_INFO = "general.info";
        /// <summary>Riga "Ripristina impostazioni".</summary>
        public const string GENERAL_RESET_DEFAULTS = "general.reset_defaults";

        // ---- Id delle azioni ----
        /// <summary>Azione: ricalibra l'altezza utente.</summary>
        public const string ACTION_RECALIBRATE = "recalibrate";
        /// <summary>Azione: apri il brief del training.</summary>
        public const string ACTION_REPLAY_TRAINING = "replay_training";
        /// <summary>Azione: apri le informazioni (contenuto non ancora progettato).</summary>
        public const string ACTION_INFO = "info";
        /// <summary>Azione: ripristina i predefiniti (con conferma).</summary>
        public const string ACTION_RESET_DEFAULTS = "reset_defaults";

        // ---- Valori enumerati ----
        /// <summary>Mano dominante destra.</summary>
        public const int HAND_RIGHT = 0;
        /// <summary>Mano dominante sinistra.</summary>
        public const int HAND_LEFT = 1;
        /// <summary>Locomozione a teletrasporto.</summary>
        public const int LOCOMOTION_TELEPORT = 0;
        /// <summary>Locomozione continua.</summary>
        public const int LOCOMOTION_CONTINUOUS = 1;
        /// <summary>Rotazione a scatti.</summary>
        public const int TURN_SNAP = 0;
        /// <summary>Rotazione continua.</summary>
        public const int TURN_CONTINUOUS = 1;

        // Schede e righe (costruite una volta).
        private static readonly SettingsTabDefinition[] TABS = BuildTabs();

        /// <summary>Le 6 schede nell'ordine di visualizzazione.</summary>
        public static IReadOnlyList<SettingsTabDefinition> Tabs => TABS;

        /// <summary>
        /// Registra nello store i predefiniti di TUTTE le righe (anche delle schede non ancora mostrate), così
        /// "Ripristina impostazioni" le riporta tutte ai valori del catalogo. Non fa nulla se lo store non supporta i predefiniti.
        /// </summary>
        /// <param name="store">Store delle impostazioni (UIServices.Settings).</param>
        public static void RegisterDefaults(IUISettingsStore store)
        {
            if (!(store is IUISettingsDefaults defaults)) return;
            foreach (SettingsTabDefinition tab in TABS)
            {
                foreach (SettingsRowDefinition row in tab.Rows)
                {
                    switch (row.Type)
                    {
                        case SettingsRowType.Toggle: defaults.RegisterDefaultBool(row.Id, row.DefaultBool); break;
                        case SettingsRowType.Slider: defaults.RegisterDefaultFloat(row.Id, row.DefaultInt); break;
                        case SettingsRowType.Selector: defaults.RegisterDefaultInt(row.Id, row.DefaultInt); break;
                    }
                }
            }
        }

        /// <summary>
        /// Trova la definizione di una riga dall'id.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="definition">Definizione trovata (null se assente).</param>
        /// <returns>true se esiste.</returns>
        public static bool TryGetRow(string id, out SettingsRowDefinition definition)
        {
            foreach (SettingsTabDefinition tab in TABS)
            {
                foreach (SettingsRowDefinition row in tab.Rows)
                {
                    if (row.Id != id) continue;
                    definition = row;
                    return true;
                }
            }
            definition = null;
            return false;
        }

        /// <summary>
        /// Imposta una riga già istanziata (prefab PANEL_Settings*) con i dati della definizione: dati specifici del tipo, poi id ed
        /// etichetta. Non collega lo store (lo fa <see cref="UISettingsRow.Bind"/>). Usato dal builder e utilizzabile a runtime.
        /// </summary>
        /// <param name="row">Riga del tipo giusto (Toggle/Slider/Selector/Action).</param>
        /// <param name="definition">Definizione dal catalogo.</param>
        /// <param name="actionIcon">Icona del pulsante azione (solo righe Action; null = quella del prefab).</param>
        /// <returns>true se il tipo della riga corrisponde alla definizione.</returns>
        public static bool ConfigureRow(UISettingsRow row, SettingsRowDefinition definition, Sprite actionIcon)
        {
            if (row == null || definition == null) return false;
            switch (definition.Type)
            {
                case SettingsRowType.Toggle:
                    if (!(row is SettingsToggleRow toggle)) return false;
                    toggle.SetToggleData(definition.DefaultBool);
                    break;
                case SettingsRowType.Slider:
                    if (!(row is SettingsSliderRow slider)) return false;
                    slider.SetSliderData(definition.Min, definition.Max, definition.Step, definition.DefaultInt, definition.FormatKey);
                    break;
                case SettingsRowType.Selector:
                    if (!(row is SettingsSelectorRow selector)) return false;
                    selector.SetSelectorData(new List<SettingsOption>(definition.Options), definition.DefaultInt);
                    break;
                case SettingsRowType.Action:
                case SettingsRowType.Value:
                    if (!(row is SettingsActionRow action)) return false;
                    action.SetActionData(definition.Type == SettingsRowType.Action ? SettingsActionType.Button : SettingsActionType.Value,
                        definition.ActionId, definition.RequiresConfirmation, actionIcon, definition.ActionTextKey, definition.ValueKey);
                    break;
            }
            row.SetRowData(definition.Id, definition.LabelKey);
            return true;
        }

        // =====================================================================
        // Dati
        // =====================================================================

        /// <summary>
        /// Costruisce le 6 schede con le loro righe (Figma 104:1011..1439 + CLAUDE.md §4.8/§8).
        /// </summary>
        private static SettingsTabDefinition[] BuildTabs()
        {
            return new[]
            {
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.Comfort, LabelKey = "ui.settings.tab.comfort",
                    IconOutlineName = "ICN_SelfImprovement", IconFilledName = "ICN_SelfImprovement_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeSelector(COMFORT_LOCOMOTION, "ui.settings.comfort.locomotion", LOCOMOTION_TELEPORT,
                            Option(LOCOMOTION_TELEPORT, "ui.settings.comfort.locomotion_teleport"),
                            Option(LOCOMOTION_CONTINUOUS, "ui.settings.comfort.locomotion_continuous")),
                        SettingsRowDefinition.MakeSelector(COMFORT_TURN_MODE, "ui.settings.comfort.turn", TURN_SNAP,
                            Option(TURN_SNAP, "ui.settings.comfort.turn_snap"),
                            Option(TURN_CONTINUOUS, "ui.settings.comfort.turn_continuous")),
                        SettingsRowDefinition.MakeSelector(COMFORT_SNAP_ANGLE, "ui.settings.comfort.snap_angle", 45,
                            Format(30, "ui.format.degrees"), Format(45, "ui.format.degrees"), Format(60, "ui.format.degrees"), Format(90, "ui.format.degrees")),
                        SettingsRowDefinition.MakeSelector(COMFORT_VIGNETTE, "ui.settings.comfort.vignette", 1,
                            Option(0, "ui.settings.comfort.vignette_off"), Option(1, "ui.settings.comfort.vignette_low"),
                            Option(2, "ui.settings.comfort.vignette_medium"), Option(3, "ui.settings.comfort.vignette_high")),
                        SettingsRowDefinition.MakeAction(COMFORT_USER_HEIGHT, "ui.settings.comfort.user_height", ACTION_RECALIBRATE,
                            "ICN_Refresh", "ui.settings.comfort.recalibrate"),
                        SettingsRowDefinition.MakeToggle(COMFORT_SEATED_MODE, "ui.settings.comfort.seated_mode", false),
                    }
                },
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.Audio, LabelKey = "ui.settings.tab.audio",
                    IconOutlineName = "ICN_VolumeUp", IconFilledName = "ICN_VolumeUp_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeSlider(AUDIO_MASTER_VOLUME, "ui.settings.audio.master_volume", 0f, 100f, 1f, 80),
                        SettingsRowDefinition.MakeSlider(AUDIO_VOICES, "ui.settings.audio.voices", 0f, 100f, 1f, 80),
                        SettingsRowDefinition.MakeSlider(AUDIO_EFFECTS, "ui.settings.audio.effects", 0f, 100f, 1f, 80),
                        SettingsRowDefinition.MakeSlider(AUDIO_ALARMS, "ui.settings.audio.alarms", 0f, 100f, 1f, 80),
                        SettingsRowDefinition.MakeSlider(AUDIO_MUSIC, "ui.settings.audio.music", 0f, 100f, 1f, 80),
                    }
                },
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.Subtitles, LabelKey = "ui.settings.tab.subtitles",
                    IconOutlineName = "ICN_Subtitles", IconFilledName = "ICN_Subtitles_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeToggle(SUBTITLES_ENABLED, "ui.settings.subtitles.enabled", true),
                        SettingsRowDefinition.MakeSelector(SUBTITLES_SIZE, "ui.settings.subtitles.size", 100,
                            Format(80, "ui.format.percent"), Format(100, "ui.format.percent"), Format(125, "ui.format.percent"), Format(150, "ui.format.percent")),
                        SettingsRowDefinition.MakeSlider(SUBTITLES_BACKGROUND_OPACITY, "ui.settings.subtitles.background_opacity", 92f, 100f, 1f, 92, "ui.format.percent"),
                    }
                },
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.Controls, LabelKey = "ui.settings.tab.controls",
                    IconOutlineName = "ICN_SportsEsports", IconFilledName = "ICN_SportsEsports_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeSelector(DOMINANT_HAND, "ui.settings.controls.dominant_hand", HAND_RIGHT,
                            Option(HAND_RIGHT, "ui.settings.controls.hand_right"), Option(HAND_LEFT, "ui.settings.controls.hand_left")),
                        SettingsRowDefinition.MakeToggle(UI_HAPTICS, "ui.settings.controls.ui_haptics", true),
                    }
                },
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.Accessibility, LabelKey = "ui.settings.tab.accessibility",
                    IconOutlineName = "ICN_AccessibilityNew", IconFilledName = "ICN_AccessibilityNew_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeSelector(ACCESSIBILITY_UI_TEXT_SIZE, "ui.settings.accessibility.ui_text_size", 100,
                            Format(100, "ui.format.percent"), Format(125, "ui.format.percent"), Format(150, "ui.format.percent")),
                        SettingsRowDefinition.MakeToggle(ACCESSIBILITY_REDUCE_FLASHES, "ui.settings.accessibility.reduce_flashes", false),
                    }
                },
                new SettingsTabDefinition
                {
                    Id = SettingsTabId.General, LabelKey = "ui.settings.tab.general",
                    IconOutlineName = "ICN_Tune", IconFilledName = "ICN_Tune_Filled",
                    Rows = new[]
                    {
                        SettingsRowDefinition.MakeValue(GENERAL_LANGUAGE, "ui.settings.general.language", "ui.settings.general.language_italian"),
                        SettingsRowDefinition.MakeAction(GENERAL_REPLAY_TRAINING, "ui.settings.general.replay_training", ACTION_REPLAY_TRAINING,
                            "ICN_School", "ui.settings.general.start"),
                        SettingsRowDefinition.MakeAction(GENERAL_INFO, "ui.settings.general.info", ACTION_INFO,
                            "ICN_Info", "ui.settings.general.open"),
                        SettingsRowDefinition.MakeAction(GENERAL_RESET_DEFAULTS, "ui.settings.general.reset_defaults", ACTION_RESET_DEFAULTS,
                            "ICN_SettingsBackupRestore", "ui.settings.general.reset", true),
                    }
                },
            };
        }

        /// <summary>
        /// Opzione con testo da chiave fissa.
        /// </summary>
        private static SettingsOption Option(int value, string labelKey)
        {
            return new SettingsOption { Value = value, LabelKey = labelKey };
        }

        /// <summary>
        /// Opzione con testo da formato che contiene il valore (es. "45°").
        /// </summary>
        private static SettingsOption Format(int value, string formatKey)
        {
            return new SettingsOption { Value = value, FormatKey = formatKey };
        }
    }
}
