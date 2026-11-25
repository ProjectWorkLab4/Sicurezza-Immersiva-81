using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder del passo 1 (Tema): genera o aggiorna SO_UITheme, SO_DifficultyStyle, SO_UIStrings_It, i
    /// font asset FNT_Jaldi*_SDF e le icone ICN_ dai file in Editor/Source/. Sicuro da rieseguire: non duplica asset,
    /// mantiene i GUID e non segna come modificato un asset i cui dati non cambiano.
    /// I file JSON di Editor/Source/ sono l'UNICA fonte di verità: ogni esecuzione riscrive tutti i valori degli asset SO_
    /// (anche animazioni, regole HUD e testi), quindi le modifiche fatte nell'Inspector vengono perse.
    /// </summary>
    public static class ThemeBuilder
    {
        /// <summary>Cartella radice della UI comune.</summary>
        public const string ROOT = "Assets/_Project/Home";
        /// <summary>Cartella degli asset SO_.</summary>
        public const string DATA_FOLDER = ROOT + "/UI/Data";
        /// <summary>File sorgente dei token Figma.</summary>
        public const string TOKENS_JSON = ROOT + "/Code/Scripts/Editor/Source/tokens_figma.json";
        /// <summary>File sorgente dei testi italiani.</summary>
        public const string STRINGS_JSON = ROOT + "/Code/Scripts/Editor/Source/strings_it.json";
        /// <summary>Asset del tema.</summary>
        public const string THEME_ASSET = DATA_FOLDER + "/SO_UITheme.asset";
        /// <summary>Asset dello stile dei livelli.</summary>
        public const string DIFFICULTY_ASSET = DATA_FOLDER + "/SO_DifficultyStyle.asset";
        /// <summary>Asset della tabella testi italiana.</summary>
        public const string STRINGS_ASSET = DATA_FOLDER + "/SO_UIStrings_It.asset";

        /// <summary>Interlinea Figma di tutti gli stili Jaldi (125%).</summary>
        private const float DEFAULT_LINE_HEIGHT_PERCENT = 125f;

        /// <summary>Ruoli effetto Figma → ruoli <see cref="ShadowRoles"/>. Gli altri effetti (HUD superata, sfondo simulato) sono ignorati.</summary>
        private static readonly Dictionary<string, string> SHADOW_ROLE_MAP = new Dictionary<string, string>
        {
            { "Card shadow", ShadowRoles.CARD },
            { "selected.lift", ShadowRoles.SELECTED_LIFT },
            { "selected.glow", ShadowRoles.SELECTED_GLOW },
            { "hover", ShadowRoles.HOVER },
            { "pressed", ShadowRoles.PRESSED },
            { "check.badge", ShadowRoles.CHECK_BADGE },
            { "bottomBar", ShadowRoles.BOTTOM_BAR },
            { "menuPanel", ShadowRoles.MENU_PANEL },
            { "sidePanel", ShadowRoles.SIDE_PANEL },
            { "pause/confirmDialog", ShadowRoles.DIALOG },
            { "hudCard (alerts, feedback, world labels)", ShadowRoles.HUD_CARD },
        };

        /// <summary>
        /// Voce di menu: esegue tutto il passo 1 (TMP Essentials, font, tema, livelli, testi, icone).
        /// </summary>
        [MenuItem("SI81/UI/Build Theme", priority = 10)]
        public static void BuildThemeMenu()
        {
            if (!TmpEssentials.EnsureImported(BuildThemeMenu)) return;
            Debug.Log(BuildAll());
        }

        /// <summary>
        /// Esegue tutto il passo 1 e restituisce un riepilogo in italiano (usabile anche da CLI).
        /// Richiede le TMP Essential Resources già importate.
        /// </summary>
        /// <returns>Riepilogo delle operazioni.</returns>
        public static string BuildAll()
        {
            if (!TmpEssentials.IsImported())
                return "[SI81 UI] TMP Essential Resources mancanti: eseguire il menu SI81/UI/Build Theme (le importa).";

            BaseBuilder.EnsureFolder(DATA_FOLDER);
            var report = new StringBuilder("[SI81 UI] Build Theme completato.\n");

            // 1. Font asset TMP.
            List<FontBuilder.FontResult> fonts = FontBuilder.BuildAll();
            foreach (FontBuilder.FontResult f in fonts) report.AppendLine(FontBuilder.Describe(f));
            TMP_FontAsset regular = fonts[0].Asset;
            TMP_FontAsset bold = fonts[1].Asset;

            // 2. Sorgenti JSON.
            TokensFileJson tokens = ReadJson<TokensFileJson>(TOKENS_JSON);
            StringsFileJson strings = ReadJson<StringsFileJson>(STRINGS_JSON);
            if (tokens == null || strings == null) return "[SI81 UI] Build Theme interrotto: file sorgente non leggibili.";
            if (!ValidateProject(tokens.project)) return "[SI81 UI] Build Theme interrotto: sezione \"project\" di tokens_figma.json mancante o incompleta.";

            // 3. Asset SO_ (il JSON è l'unica fonte di verità: le modifiche fatte nell'Inspector vengono sovrascritte).
            report.AppendLine(BuildStrings(strings, out UIStrings stringsAsset));
            report.AppendLine(BuildTheme(tokens, regular, bold));
            report.AppendLine(BuildDifficulty(tokens, stringsAsset));

            // 4. Icone ICN_ (PNG dagli SVG Material Symbols).
            report.AppendLine(IconBuilder.BuildAll());

            // 5. Sprite generati TEX_ (angoli arrotondati e ombre 9-slice) dalle misure del tema appena scritto.
            AssetDatabase.SaveAssets();
            report.AppendLine(SpriteBuilder.BuildAll(AssetDatabase.LoadAssetAtPath<UITheme>(THEME_ASSET)));

            // 6. Sprite Atlas con tutti gli sprite TEX_ e ICN_.
            AssetDatabase.SaveAssets();
            report.AppendLine(SpriteAtlasBuilder.BuildAll());

            AssetDatabase.SaveAssets();
            return report.ToString().TrimEnd();
        }

        // =====================================================================
        // SO_UITheme
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna SO_UITheme dai token Figma e dalla sezione "project" di tokens_figma.json.
        /// Tutti i valori (anche animazioni e regole HUD) vengono riscritti dal JSON a ogni esecuzione.
        /// </summary>
        private static string BuildTheme(TokensFileJson tokens, TMP_FontAsset regular, TMP_FontAsset bold)
        {
            ProjectJson project = tokens.project;

            // Colori Figma + colori derivati (sezione "project").
            var colors = new List<UITheme.ColorToken>();
            AddColors(colors, tokens.colors);
            int figmaColorCount = colors.Count;
            AddColors(colors, project.colors);
            CheckNoDuplicates(colors.Select(c => c.Token), "colore");
            CheckConstantsPresent(typeof(ColorTokens), colors.Select(c => c.Token), "colore");

            // Stili di testo.
            var styles = new List<UITheme.TextStyle>();
            foreach (TextStyleJson t in tokens.textStyles)
            {
                bool isBold = string.Equals(t.weight, "Bold", StringComparison.OrdinalIgnoreCase);
                TMP_FontAsset font = isBold ? bold : regular;
                float lineHeight = ParsePercent(t.lineHeight, DEFAULT_LINE_HEIGHT_PERCENT);
                styles.Add(new UITheme.TextStyle
                {
                    Name = t.name,
                    Font = font,
                    Size = t.size,
                    LineHeightPercent = lineHeight,
                    LineSpacing = ComputeLineSpacing(font, lineHeight),
                    CharacterSpacing = ParsePercent(t.letterSpacing, 0f),
                    Weight = isBold ? FontWeight.Bold : FontWeight.Regular
                });
            }
            CheckConstantsPresent(typeof(TextStyleIds), styles.Select(s => s.Name), "stile di testo");

            // Ombre e glow (solo DROP_SHADOW; le sfocature di sfondo non esistono a runtime su Quest 3).
            var shadows = new List<UITheme.ShadowToken>();
            IEnumerable<EffectJson> effects = (tokens.effects.styles ?? new EffectJson[0]).Concat(tokens.effects.inline ?? new EffectJson[0]);
            UITheme.GlassSheen sheen = null;
            foreach (EffectJson e in effects)
            {
                string figmaRole = !string.IsNullOrEmpty(e.role) ? e.role : e.name;
                if (e.type == "FILL_GRADIENT_LINEAR" && figmaRole == "glassPanel.sheen" && e.stops != null && e.stops.Length >= 2)
                {
                    sheen = new UITheme.GlassSheen
                    {
                        StartColor = ParseColor(e.stops[0].color, e.stops[0].alpha),
                        EndColor = ParseColor(e.stops[e.stops.Length - 1].color, e.stops[e.stops.Length - 1].alpha),
                        EndPosition = e.stops[e.stops.Length - 1].pos,
                        // Le maniglie del gradiente non sono nelle variabili Figma: arrivano da "project.glassSheen".
                        HandleStart = project.glassSheen.handleStart,
                        HandleEnd = project.glassSheen.handleEnd
                    };
                    continue;
                }
                if (e.type != "DROP_SHADOW" || figmaRole == null || !SHADOW_ROLE_MAP.TryGetValue(figmaRole, out string role)) continue;

                // Figma ha "cotto" la scala di hover/pressed nelle ombre: si riportano a scala 1,
                // perché la scala 1,04 / 0,98 viene applicata a runtime a tutto il componente.
                float unscale = role == ShadowRoles.HOVER ? project.motion.hoverScale : role == ShadowRoles.PRESSED ? project.motion.pressedScale : 1f;
                shadows.Add(new UITheme.ShadowToken
                {
                    Role = role,
                    Offset = new Vector2(Round(e.x / unscale), -Round(e.y / unscale)),
                    Blur = Round(e.blur / unscale),
                    Spread = Round(e.spread / unscale),
                    Color = ParseColor(e.color, e.alpha)
                });
            }
            CheckConstantsPresent(typeof(ShadowRoles), shadows.Select(s => s.Role), "ombra");
            if (sheen == null)
            {
                Debug.LogError("[SI81 UI] Effetto \"glassPanel.sheen\" assente in tokens_figma.json: riflesso vetro impostato al colore MISSING.");
                sheen = new UITheme.GlassSheen();
            }

            // Misure Figma + misure derivate (sezione "project").
            var numbers = new List<UITheme.NumberToken>();
            AddNumbers(numbers, tokens.numbers);
            int figmaNumberCount = numbers.Count;
            AddNumbers(numbers, project.numbers);
            CheckNoDuplicates(numbers.Select(n => n.Token), "numero");
            CheckConstantsPresent(typeof(NumberTokens), numbers.Select(n => n.Token), "numero");

            // Animazioni e regole HUD (sezione "project").
            UITheme.MotionSettings motion = ToMotion(project.motion);
            UITheme.HudSettings hud = ToHud(project.hud);
            UITheme.PointerSettings pointer = ToPointer(project.pointer);

            UITheme theme = LoadOrCreate<UITheme>(THEME_ASSET, out bool created);
            string status = ApplyIfChanged(theme, created, () =>
                theme.SetData(colors, styles, shadows, sheen, numbers, motion, hud, pointer));

            return $"SO_UITheme: {status}. Colori {colors.Count} ({figmaColorCount} Figma + {colors.Count - figmaColorCount} derivati), " +
                   $"stili testo {styles.Count}, ombre {shadows.Count}, misure {numbers.Count} ({figmaNumberCount} Figma + {numbers.Count - figmaNumberCount} derivate), " +
                   "animazioni, HUD e puntatore dalla sezione \"project\".";
        }

        /// <summary>
        /// Aggiunge alla lista i token colore di un array JSON (le voci senza "token" sono note e vengono saltate).
        /// </summary>
        private static void AddColors(List<UITheme.ColorToken> target, ColorJson[] source)
        {
            if (source == null) return;
            foreach (ColorJson c in source)
            {
                if (string.IsNullOrEmpty(c.token)) continue;
                target.Add(new UITheme.ColorToken { Token = c.token, Value = ParseColor(c.hex, c.alpha), Description = c.desc });
            }
        }

        /// <summary>
        /// Aggiunge alla lista i token numerici di un array JSON (le voci senza "token" sono note e vengono saltate).
        /// </summary>
        private static void AddNumbers(List<UITheme.NumberToken> target, NumberJson[] source)
        {
            if (source == null) return;
            foreach (NumberJson n in source)
                if (!string.IsNullOrEmpty(n.token)) target.Add(new UITheme.NumberToken { Token = n.token, Value = n.value });
        }

        /// <summary>
        /// Converte la sezione "project.motion" del JSON nei valori di animazione del tema.
        /// </summary>
        private static UITheme.MotionSettings ToMotion(MotionJson m)
        {
            return new UITheme.MotionSettings
            {
                HoverScale = m.hoverScale,
                PressedScale = m.pressedScale,
                HoverDuration = m.hoverDuration,
                PressDuration = m.pressDuration,
                SelectDuration = m.selectDuration,
                FadeDuration = m.fadeDuration,
                ScreenTransitionDuration = m.screenTransitionDuration
            };
        }

        /// <summary>
        /// Converte la sezione "project.hud" del JSON nelle regole HUD del tema.
        /// </summary>
        private static UITheme.HudSettings ToHud(HudJson h)
        {
            return new UITheme.HudSettings
            {
                AlertDuration = h.alertDuration,
                AlertPitch = h.alertPitch,
                SubtitlePitchMin = h.subtitlePitchMin,
                SubtitlePitchMax = h.subtitlePitchMax,
                MaxPitch = h.maxPitch,
                SubtitleMaxChars = h.subtitleMaxChars,
                SubtitleMaxLines = h.subtitleMaxLines,
                WristObjectiveMaxChars = h.wristObjectiveMaxChars,
                WristIdleAlpha = h.wristIdleAlpha,
                WristFocusDistance = h.wristFocusDistance,
                WristPanelSizeMeters = new Vector2(h.wristPanelWidthMeters, h.wristPanelHeightMeters),
                TimerWarningFirst = h.timerWarningFirst,
                TimerWarningSecond = h.timerWarningSecond,
                WorldLabelMinDistance = h.worldLabelMinDistance,
                WorldLabelMaxDistance = h.worldLabelMaxDistance,
                MaxRiskLabels = h.maxRiskLabels
            };
        }

        /// <summary>
        /// Converte la sezione "project.pointer" del JSON nelle misure del raggio di puntamento del tema.
        /// </summary>
        private static UITheme.PointerSettings ToPointer(PointerJson p)
        {
            return new UITheme.PointerSettings
            {
                RayWidth = p.rayWidth,
                RayMaxLength = p.rayMaxLength,
                IdleRayLength = p.idleRayLength,
                ReticleDiameter = p.reticleDiameter,
                ReticleSurfaceOffset = p.reticleSurfaceOffset
            };
        }

        /// <summary>
        /// Controlla che la sezione "project" esista e che i valori obbligatoriamente positivi non siano 0
        /// (JsonUtility mette 0 alle chiavi mancanti). Scrive un errore per ogni valore mancante.
        /// </summary>
        /// <returns>true se la sezione è utilizzabile.</returns>
        private static bool ValidateProject(ProjectJson p)
        {
            if (p == null || p.motion == null || p.hud == null || p.pointer == null || p.glassSheen == null)
            {
                Debug.LogError("[SI81 UI] tokens_figma.json: sezione \"project\" (con \"motion\", \"hud\", \"pointer\" e \"glassSheen\") mancante.");
                return false;
            }
            var required = new (string name, float value)[]
            {
                ("motion.hoverScale", p.motion.hoverScale), ("motion.pressedScale", p.motion.pressedScale),
                ("motion.hoverDuration", p.motion.hoverDuration), ("motion.pressDuration", p.motion.pressDuration),
                ("motion.selectDuration", p.motion.selectDuration), ("motion.fadeDuration", p.motion.fadeDuration),
                ("motion.screenTransitionDuration", p.motion.screenTransitionDuration),
                ("hud.alertDuration", p.hud.alertDuration), ("hud.subtitleMaxChars", p.hud.subtitleMaxChars),
                ("hud.subtitleMaxLines", p.hud.subtitleMaxLines), ("hud.wristObjectiveMaxChars", p.hud.wristObjectiveMaxChars),
                ("hud.wristIdleAlpha", p.hud.wristIdleAlpha), ("hud.wristFocusDistance", p.hud.wristFocusDistance),
                ("hud.wristPanelWidthMeters", p.hud.wristPanelWidthMeters), ("hud.wristPanelHeightMeters", p.hud.wristPanelHeightMeters),
                ("hud.timerWarningFirst", p.hud.timerWarningFirst), ("hud.timerWarningSecond", p.hud.timerWarningSecond),
                ("hud.worldLabelMinDistance", p.hud.worldLabelMinDistance), ("hud.worldLabelMaxDistance", p.hud.worldLabelMaxDistance),
                ("hud.maxRiskLabels", p.hud.maxRiskLabels),
                ("pointer.rayWidth", p.pointer.rayWidth), ("pointer.rayMaxLength", p.pointer.rayMaxLength),
                ("pointer.idleRayLength", p.pointer.idleRayLength),
                ("pointer.reticleDiameter", p.pointer.reticleDiameter), ("pointer.reticleSurfaceOffset", p.pointer.reticleSurfaceOffset),
                ("glassSheen.handleStart", p.glassSheen.handleStart), ("glassSheen.handleEnd", p.glassSheen.handleEnd - p.glassSheen.handleStart)
            };
            bool ok = true;
            foreach (var (name, value) in required)
            {
                if (value > 0f) continue;
                Debug.LogError($"[SI81 UI] tokens_figma.json: \"project.{name}\" mancante o non positivo ({value}).");
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Segnala con un errore i token definiti più di una volta (es. lo stesso token in Figma e in "project").
        /// </summary>
        private static void CheckNoDuplicates(IEnumerable<string> names, string kind)
        {
            foreach (string dup in names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key))
                Debug.LogError($"[SI81 UI] Token {kind} \"{dup}\" definito più volte in tokens_figma.json.");
        }

        // =====================================================================
        // SO_DifficultyStyle
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna SO_DifficultyStyle dalla sezione "difficulty" dei token. Mantiene le icone già assegnate.
        /// </summary>
        private static string BuildDifficulty(TokensFileJson tokens, UIStrings strings)
        {
            DifficultyStyle style = LoadOrCreate<DifficultyStyle>(DIFFICULTY_ASSET, out bool created);
            var colorByToken = tokens.colors.Where(c => !string.IsNullOrEmpty(c.token)).ToDictionary(c => c.token, c => ParseColor(c.hex, c.alpha));

            var levels = new List<DifficultyStyle.Entry>();
            foreach (LevelJson l in tokens.difficulty.levels.OrderBy(l => l.level))
            {
                var level = (DifficultyLevel)l.level;
                string idLower = l.id.ToLowerInvariant();
                DifficultyStyle.Entry previous = style.Levels.FirstOrDefault(e => e != null && e.Level == level);
                var entry = new DifficultyStyle.Entry
                {
                    Level = level,
                    Id = l.id,
                    LabelKey = "ui.level." + idLower,
                    DescriptionKey = "ui.level." + idLower + "_desc",
                    Color = ParseColor(l.color, 1f),
                    TextOnColor = ParseColor(l.textOnColor, 1f),
                    BarsFilled = l.barsFilled,
                    Icon = previous != null ? previous.Icon : null
                };
                if (!strings.Contains(entry.LabelKey) || !strings.Contains(entry.DescriptionKey))
                    Debug.LogError($"[SI81 UI] Chiavi del livello {l.id} assenti in SO_UIStrings_It: {entry.LabelKey}, {entry.DescriptionKey}.");
                levels.Add(entry);
            }

            // Spessore del contorno: stesso valore del token "stroke.level-bar" della sezione "project".
            NumberJson stroke = tokens.project.numbers?.FirstOrDefault(n => n.token == NumberTokens.STROKE_LEVEL_BAR);
            if (stroke == null) Debug.LogError($"[SI81 UI] Token \"{NumberTokens.STROKE_LEVEL_BAR}\" assente in project.numbers di tokens_figma.json.");

            string status = ApplyIfChanged(style, created, () => style.SetData(levels,
                GetColorOrMissing(colorByToken, ColorTokens.LEVEL_BAR_ON_LIGHT), GetColorOrMissing(colorByToken, ColorTokens.LEVEL_BAR_OFF_LIGHT),
                GetColorOrMissing(colorByToken, ColorTokens.LEVEL_BAR_ON_DARK), GetColorOrMissing(colorByToken, ColorTokens.LEVEL_BAR_OFF_DARK),
                stroke != null ? stroke.value : 0f));
            return $"SO_DifficultyStyle: {status}. Livelli {levels.Count}.";
        }

        // =====================================================================
        // SO_UIStrings_It
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna SO_UIStrings_It da strings_it.json e ne valida formato chiavi e duplicati.
        /// strings_it.json è l'unica fonte di verità: le modifiche fatte direttamente all'asset vengono sovrascritte.
        /// </summary>
        private static string BuildStrings(StringsFileJson source, out UIStrings asset)
        {
            var entries = source.strings.Select(s => new UIStrings.Entry { Key = s.key, Text = s.text }).ToList();
            UIStrings strings = LoadOrCreate<UIStrings>(STRINGS_ASSET, out bool created);
            string status = ApplyIfChanged(strings, created, () => strings.SetData("it", entries));
            asset = strings;

            List<string> problems = strings.Validate();
            if (problems.Count > 0)
                Debug.LogError($"[SI81 UI] SO_UIStrings_It: {problems.Count} problemi:\n" + string.Join("\n", problems));
            return $"SO_UIStrings_It: {status}. Chiavi {entries.Count}, problemi di validazione {problems.Count}.";
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        /// <summary>
        /// Carica un asset o lo crea se non esiste (mai duplicati: il percorso è fisso).
        /// </summary>
        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                asset.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        /// <summary>
        /// Applica i dati e segna l'asset come modificato solo se il contenuto serializzato è cambiato.
        /// </summary>
        /// <returns>"creato", "aggiornato" o "invariato".</returns>
        private static string ApplyIfChanged(ScriptableObject asset, bool created, Action apply)
        {
            string before = EditorJsonUtility.ToJson(asset);
            apply();
            string after = EditorJsonUtility.ToJson(asset);
            if (created || before != after) EditorUtility.SetDirty(asset);
            return created ? "creato" : before != after ? "aggiornato" : "invariato";
        }

        /// <summary>
        /// Calcola TMP lineSpacing (unità 1/100 em) per ottenere l'interlinea Figma con il font dato.
        /// </summary>
        private static float ComputeLineSpacing(TMP_FontAsset font, float lineHeightPercent)
        {
            if (font == null || font.faceInfo.pointSize <= 0) return 0f;
            float naturalEm = font.faceInfo.lineHeight * font.faceInfo.scale / font.faceInfo.pointSize;
            return Round((lineHeightPercent / 100f - naturalEm) * 100f);
        }

        /// <summary>
        /// Controlla che ogni costante di una classe di nomi token esista nei dati generati.
        /// </summary>
        private static void CheckConstantsPresent(Type constants, IEnumerable<string> names, string kind)
        {
            var set = new HashSet<string>(names);
            foreach (FieldInfo f in constants.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
                string value = (string)f.GetRawConstantValue();
                if (!set.Contains(value)) Debug.LogError($"[SI81 UI] Token {kind} \"{value}\" ({constants.Name}.{f.Name}) assente nei dati Figma.");
            }
        }

        /// <summary>
        /// Restituisce il colore di un token Figma, oppure <see cref="UITheme.MISSING_COLOR"/> con un errore se manca.
        /// </summary>
        private static Color GetColorOrMissing(Dictionary<string, Color> colorByToken, string token)
        {
            if (colorByToken.TryGetValue(token, out Color c)) return c;
            Debug.LogError($"[SI81 UI] Token colore \"{token}\" assente in tokens_figma.json.");
            return UITheme.MISSING_COLOR;
        }

        /// <summary>
        /// Converte "#RRGGBB" e un alpha 0–1 in Color.
        /// </summary>
        private static Color ParseColor(string hex, float alpha)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                Debug.LogError($"[SI81 UI] Colore non valido: \"{hex}\".");
                return UITheme.MISSING_COLOR;
            }
            c.a = alpha;
            return c;
        }

        /// <summary>
        /// Converte "125%" in 125. Restituisce il valore di riserva se il testo è vuoto o non valido.
        /// </summary>
        private static float ParsePercent(string text, float fallback)
        {
            if (string.IsNullOrEmpty(text)) return fallback;
            return float.TryParse(text.Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        /// <summary>
        /// Arrotonda a 2 decimali (valori stabili tra un'esecuzione e l'altra).
        /// </summary>
        private static float Round(float v)
        {
            return (float)Math.Round(v, 2);
        }

        /// <summary>
        /// Legge un file JSON di Editor/Source con JsonUtility.
        /// </summary>
        private static T ReadJson<T>(string assetPath) where T : class
        {
            string full = Path.GetFullPath(assetPath);
            if (!File.Exists(full))
            {
                Debug.LogError($"[SI81 UI] File sorgente mancante: {assetPath}.");
                return null;
            }
            return JsonUtility.FromJson<T>(File.ReadAllText(full, Encoding.UTF8));
        }
    }
}
