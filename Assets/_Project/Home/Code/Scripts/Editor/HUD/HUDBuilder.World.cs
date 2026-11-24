using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "mondo" del builder della HUD (Blocco C · C2): crea o aggiorna i prefab HUD_RiskLabel, HUD_InteractionPrompt,
    /// HUD_ContextHelp (<see cref="BuildWorldLabels"/>) e HUD_DecisionPanel, HUD_OutcomePanel (<see cref="BuildDecisionOutcome"/>)
    /// in Assets/_Project/Home/UI/Prefabs/HUD/. Create-or-update sul posto (stessi GUID, figli cercati per nome, nessun doppione,
    /// sicuro da rieseguire; mai YAML a mano). Misure e colori da docs/FIGMA_SPECS.md §1b e dal tema; testi dinamici (dati
    /// dello scenario) non scritti nei prefab. Gli helper privati iniziano con "World" per non collidere con le altre parti.
    /// Si lanciano da "SI81/UI/Build HUD" (HUDBuilder.BuildAll).
    /// </summary>
    internal static partial class HUDBuilder
    {
        /// <summary>Cartella dei prefab della HUD.</summary>
        private const string WORLD_PREFABS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/HUD";
        private const string WORLD_RISK_NAME = "HUD_RiskLabel";
        private const string WORLD_PROMPT_NAME = "HUD_InteractionPrompt";
        private const string WORLD_CONTEXT_NAME = "HUD_ContextHelp";
        private const string WORLD_DECISION_NAME = "HUD_DecisionPanel";
        private const string WORLD_OUTCOME_NAME = "HUD_OutcomePanel";
        private const string WORLD_MANAGER_NAME = "MNG_WorldLabels";

        // --- Misure Figma (docs/FIGMA_SPECS.md §1b, nodi 101:353, 101:340, 101:374), in unità Canvas ---
        // HUD_RiskLabel 392 x 120: cerchio 120, spazio 12, card 260.
        private const float WORLD_RISK_WIDTH = 392f, WORLD_RISK_HEIGHT = 120f, WORLD_RISK_CIRCLE = 120f, WORLD_RISK_CARD_X = 132f, WORLD_RISK_CARD_W = 260f, WORLD_RISK_CARD_H = 88f;
        // HUD_InteractionPrompt 260 x 126: card 72, linea 6 x 40, punto 14.
        private const float WORLD_PROMPT_WIDTH = 260f, WORLD_PROMPT_HEIGHT = 126f, WORLD_PROMPT_CARD_H = 72f, WORLD_PROMPT_LINE_W = 6f, WORLD_PROMPT_LINE_H = 40f, WORLD_PROMPT_DOT = 14f;
        // HUD_ContextHelp espansa 308 x 85: pallino 40, spazio 8, card 260.
        private const float WORLD_CONTEXT_WIDTH = 308f, WORLD_CONTEXT_HEIGHT = 85f, WORLD_CONTEXT_DOT = 40f, WORLD_CONTEXT_GAP = 8f, WORLD_CONTEXT_CARD_W = 260f;
        // Padding della card (12 / 8), icona 32, spazio 10.
        private const float WORLD_PAD_X = 12f, WORLD_PAD_Y = 8f, WORLD_ICON = 32f, WORLD_GAP = 10f, WORLD_TITLE_H = 23f, WORLD_BODY_H = 46f, WORLD_PILL_H = 24f;
        // Barra del tempo del pannello decisione: raggio angoli.
        private const float WORLD_BAR_RADIUS = 3f;
        // Pannello decisione: altezza predefinita nel prefab (si ricalcola a ogni apertura).
        private const float WORLD_DECISION_DEFAULT_HEIGHT = 300f;

        /// <summary>
        /// Crea o aggiorna HUD_RiskLabel, HUD_InteractionPrompt e HUD_ContextHelp. Non crea nulla se mancano tema, testi o icone.
        /// </summary>
        internal static void BuildWorldLabels()
        {
            if (!WorldLoadContext(out UITheme theme, out UIStrings strings)) return;
            var lines = new List<string>
            {
                WorldBuildRisk(theme, strings),
                WorldBuildInteractionPrompt(theme, strings),
                WorldBuildContextHelp(theme, strings)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[SI81 UI] Build HUD · etichette nel mondo:\n" + string.Join("\n", lines));
        }

        /// <summary>
        /// Crea o aggiorna HUD_DecisionPanel e HUD_OutcomePanel. Richiede BTN_Secondary e LBL_Outcome (Build Components).
        /// </summary>
        internal static void BuildDecisionOutcome()
        {
            if (!WorldLoadContext(out UITheme theme, out UIStrings strings)) return;
            var lines = new List<string>
            {
                WorldBuildDecision(theme, strings),
                WorldBuildOutcome(theme, strings)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[SI81 UI] Build HUD · decisione ed esito:\n" + string.Join("\n", lines));
        }

        /// <summary>
        /// Aggiunge (o ritrova) in MNG_HUD il figlio MNG_WorldLabels con <see cref="WorldLabelManager"/> e i tre prefab delle
        /// etichette. Da chiamare dopo <see cref="BuildWorldLabels"/>, sul contenuto del prefab MNG_HUD (HUDBuilder.Rig).
        /// </summary>
        /// <param name="hudRoot">Radice di MNG_HUD (contenuto del prefab caricato).</param>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <returns>Il gestore configurato (null se i prefab delle etichette mancano: eseguire prima BuildWorldLabels).</returns>
        internal static WorldLabelManager AddWorldLabelManager(GameObject hudRoot, UITheme theme)
        {
            WorldLabel risk = WorldLoadLabelPrefab(WORLD_RISK_NAME);
            WorldLabel prompt = WorldLoadLabelPrefab(WORLD_PROMPT_NAME);
            WorldLabel context = WorldLoadLabelPrefab(WORLD_CONTEXT_NAME);
            if (risk == null || prompt == null || context == null)
            {
                Debug.LogError("[SI81 UI] MNG_WorldLabels: prefab HUD_RiskLabel / HUD_InteractionPrompt / HUD_ContextHelp assenti (eseguire prima BuildWorldLabels).");
                return null;
            }
            Transform child = BaseBuilder.GetOrCreateChild(hudRoot.transform, WORLD_MANAGER_NAME);
            child.gameObject.layer = LayerMask.NameToLayer("UI");
            var manager = BaseBuilder.GetOrAdd<WorldLabelManager>(child.gameObject);
            manager.Configure(theme, risk, prompt, context);
            EditorUtility.SetDirty(manager);
            return manager;
        }

        // =====================================================================
        // Contesto e prefab
        // =====================================================================

        /// <summary>
        /// Carica tema e testi (e rilancia Build Theme una volta se mancano i token WorldTokens); scrive un errore se non va.
        /// </summary>
        private static bool WorldLoadContext(out UITheme theme, out UIStrings strings)
        {
            theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] Build HUD (mondo): SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return false;
            }
            if (!WorldHasTokens(theme))
            {
                Debug.Log("[SI81 UI] Build HUD (mondo): token WorldTokens assenti in SO_UITheme, eseguo Build Theme.");
                Debug.Log(ThemeBuilder.BuildAll());
                AssetDatabase.Refresh();
                theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
                if (!WorldHasTokens(theme))
                {
                    Debug.LogError("[SI81 UI] Build HUD (mondo) interrotto: SO_UITheme non contiene i token del Blocco C · C2 "
                        + "(size.outcome.width, interval.world-label.tick...). Aggiungerli a tokens_figma.json (vedi NOTES_C2.md) e rieseguire Build Theme.");
                    return false;
                }
            }
            BaseBuilder.EnsureFolder(WORLD_PREFABS_FOLDER);
            BaseBuilder.EnsureFolder(SpriteBuilder.SPRITES_FOLDER);
            return true;
        }

        /// <summary>
        /// true se il tema contiene i token delle etichette nel mondo, della decisione e dell'esito.
        /// </summary>
        private static bool WorldHasTokens(UITheme theme)
        {
            if (theme == null) return false;
            return theme.Numbers.Any(n => n != null && n.Token == WorldTokens.OUTCOME_WIDTH)
                && theme.Numbers.Any(n => n != null && n.Token == WorldTokens.LABEL_TICK)
                && theme.Numbers.Any(n => n != null && n.Token == WorldTokens.DECISION_WIDTH);
        }

        /// <summary>
        /// Percorso del prefab HUD.
        /// </summary>
        private static string WorldPrefabPath(string prefabName)
        {
            return WORLD_PREFABS_FOLDER + "/" + prefabName + ".prefab";
        }

        /// <summary>
        /// Carica il componente <see cref="WorldLabel"/> di un prefab già creato (null se manca).
        /// </summary>
        private static WorldLabel WorldLoadLabelPrefab(string prefabName)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath(prefabName));
            return go != null ? go.GetComponent<WorldLabel>() : null;
        }

        /// <summary>
        /// Carica il contenuto del prefab (o crea la radice), imposta il Canvas World Space e il perno. Da chiudere con <see cref="WorldEndPrefab"/>.
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice).</param>
        /// <param name="theme">Tema (distanza massima del raggio).</param>
        /// <param name="size">Dimensione del Canvas in unità Canvas.</param>
        /// <param name="pivot">Perno normalizzato (punto di aggancio nel mondo).</param>
        /// <param name="scale">Scala iniziale del Canvas (0,00126 × distanza tipica).</param>
        /// <param name="interactive">true = UIWorldRaycaster + WorldCanvasCamera (riceve il raggio del controller).</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        private static GameObject WorldBeginPrefab(string prefabName, UITheme theme, Vector2 size, Vector2 pivot, float scale, bool interactive, out bool existed)
        {
            string path = WorldPrefabPath(prefabName);
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            if (existed) ComponentsBuilder.RemoveMissingScripts(root);
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            root.SetActive(true);

            Canvas canvas = BaseBuilder.GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            BaseBuilder.GetOrAdd<CanvasGroup>(root);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * scale;

            // Un solo raycaster, e solo dove serve: HUD_ContextHelp e HUD_DecisionPanel (CLAUDE.md §3).
            foreach (UnityEngine.InputSystem.UI.TrackedDeviceRaycaster tracked in root.GetComponents<UnityEngine.InputSystem.UI.TrackedDeviceRaycaster>())
                Object.DestroyImmediate(tracked);
            foreach (GraphicRaycaster plain in root.GetComponents<GraphicRaycaster>())
                if (plain.GetType() == typeof(GraphicRaycaster)) Object.DestroyImmediate(plain);
            if (interactive)
            {
                BaseBuilder.GetOrAdd<UIWorldRaycaster>(root).Configure(theme.Pointer.RayMaxLength);
                BaseBuilder.GetOrAdd<WorldCanvasCamera>(root);
            }
            else
            {
                foreach (UIWorldRaycaster raycaster in root.GetComponents<UIWorldRaycaster>()) Object.DestroyImmediate(raycaster);
                foreach (WorldCanvasCamera cam in root.GetComponents<WorldCanvasCamera>()) Object.DestroyImmediate(cam);
            }
            return root;
        }

        /// <summary>
        /// Salva il prefab DISATTIVO (si apre da codice) e libera la memoria.
        /// </summary>
        private static string WorldEndPrefab(GameObject root, string prefabName, bool existed)
        {
            string path = WorldPrefabPath(prefabName);
            root.SetActive(false);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                return success ? $"{prefabName}: {(existed ? "aggiornato" : "creato")} ({path})." : $"{prefabName}: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (existed) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Strumenti grafici
        // =====================================================================

        /// <summary>
        /// Imposta un RectTransform ancorato in alto a sinistra del genitore (x verso destra, y verso il basso, come in Figma).
        /// </summary>
        private static void WorldPlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Imposta un RectTransform ancorato in alto al centro del genitore (perno in alto al centro).
        /// </summary>
        private static void WorldPlaceTopCenter(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Sprite 9-slice bianco arrotondato TEX_&lt;Famiglia&gt;Rounded (rieseguibile).
        /// </summary>
        private static Sprite WorldRoundedSprite(string family, float radius)
        {
            string path = $"{SpriteBuilder.SPRITES_FOLDER}/TEX_{family}Rounded.png";
            SpriteBuilder.BuildRoundedSprite(path, radius);
            return ComponentsBuilder.LoadSprite(path);
        }

        /// <summary>
        /// Crea una forma opaca: contenitore con IMG_Shadow (ombra hudCard, opzionale) e IMG_Fill (rettangolo arrotondato col colore
        /// del token). L'ombra non riceve raggi; il riempimento riceve i raggi solo se richiesto.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome del contenitore (PANEL_...).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="family">Famiglia degli sprite (es. "HudCard").</param>
        /// <param name="radius">Raggio degli angoli.</param>
        /// <param name="colorToken">Token colore del riempimento.</param>
        /// <param name="shadow">true = aggiunge l'ombra della card HUD.</param>
        /// <param name="raycast">true = il riempimento riceve i raggi.</param>
        /// <returns>Il contenitore (i figli di contenuto vanno aggiunti dopo).</returns>
        private static RectTransform WorldAddShape(Transform parent, string name, UITheme theme, string family, float radius, string colorToken, bool shadow, bool raycast)
        {
            RectTransform container = BaseBuilder.GetOrCreateRectChild(parent, name);
            Sprite rounded = WorldRoundedSprite(family, radius);
            if (shadow)
            {
                UITheme.ShadowToken token = theme.GetShadow(ShadowRoles.HUD_CARD);
                string shadowPath = $"{SpriteBuilder.SPRITES_FOLDER}/TEX_{family}Shadow.png";
                SpriteBuilder.BuildShadowSprite(shadowPath, radius, token);
                Image shadowImage = ComponentsBuilder.AddImage(container, "IMG_Shadow", ComponentsBuilder.LoadSprite(shadowPath), token.Color, false);
                shadowImage.fillCenter = false;
                float expand = PanelSprites.GetShadowLayout(radius, token).PaddingUnits;
                PanelLayout.SetRect(shadowImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
                shadowImage.transform.SetSiblingIndex(0);
            }
            Image fill = ComponentsBuilder.AddImage(container, "IMG_Fill", rounded, theme.GetColor(colorToken), raycast);
            UIComponentUtil.SetInset(fill.rectTransform, 0f);
            fill.transform.SetSiblingIndex(shadow ? 1 : 0);
            return container;
        }

        /// <summary>
        /// Crea un'etichetta TMP dinamica (testo dato a runtime, nessuna chiave) con stile e colore del tema.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (LBL_...).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        /// <param name="style">Stile di testo (TextStyleIds).</param>
        /// <param name="colorToken">Token colore.</param>
        /// <param name="alignment">Allineamento.</param>
        /// <param name="wrap">true = va a capo e tronca con i puntini, false = una riga.</param>
        /// <returns>Il TMP.</returns>
        private static TMP_Text WorldAddText(Transform parent, string name, UITheme theme, UIStrings strings, string style, string colorToken, TextAlignmentOptions alignment, bool wrap)
        {
            UIStringText label = ComponentsBuilder.AddLabel(parent, name, theme, strings, style, colorToken, string.Empty, alignment);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        /// <summary>
        /// Rettangolo pieno senza sprite (linee e barre), colore dal tema, mai raggi.
        /// </summary>
        private static Image WorldAddBar(Transform parent, string name, Color color)
        {
            return ComponentsBuilder.AddImage(parent, name, null, color, false);
        }

        /// <summary>
        /// Crea o aggiorna lo sprite di un anello (bianco con alfa, tinta dal componente) nel cerchio inscritto: tra due raggi
        /// normalizzati, intero o tratteggiato. Scrive il file solo se cambia. Serve al cerchio del rischio (doppio contorno).
        /// </summary>
        /// <param name="family">Nome (TEX_&lt;family&gt;.png).</param>
        /// <param name="innerRadius">Raggio interno 0–1 (1 = bordo della texture).</param>
        /// <param name="outerRadius">Raggio esterno 0–1.</param>
        /// <param name="dashes">0 = anello continuo; N = N tratti con vuoti uguali.</param>
        /// <returns>Lo sprite caricato.</returns>
        private static Sprite WorldRingSprite(string family, float innerRadius, float outerRadius, int dashes)
        {
            const int SIZE = 256;
            string path = $"{SpriteBuilder.SPRITES_FOLDER}/{family}.png";
            var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            var pixels = new Color32[SIZE * SIZE];
            float radius = SIZE * 0.5f;
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    // Bordi sfumati su 1 px (antialias).
                    float alpha = Mathf.Clamp01(outerRadius * radius - distance + 0.5f) * Mathf.Clamp01(distance - innerRadius * radius + 0.5f);
                    if (dashes > 0 && alpha > 0f)
                    {
                        float turn = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f;
                        float phase = turn * dashes - Mathf.Floor(turn * dashes);
                        // Tratto = prima metà del periodo, con bordi sfumati di circa 1 px sulla circonferenza media.
                        float edge = 1f / (2f * Mathf.PI * radius * (innerRadius + outerRadius) * 0.5f / dashes);
                        alpha *= Mathf.Clamp01(phase / edge) * Mathf.Clamp01((0.5f - phase) / edge);
                    }
                    pixels[y * SIZE + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string fullPath = Path.GetFullPath(path);
            bool same = File.Exists(fullPath) && File.ReadAllBytes(fullPath).SequenceEqual(png);
            if (!same)
            {
                File.WriteAllBytes(fullPath, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
            return ComponentsBuilder.LoadSprite(path);
        }

        // =====================================================================
        // HUD_RiskLabel
        // =====================================================================

        /// <summary>
        /// HUD_RiskLabel 392 × 120 (Figma 101:353): cerchio di rischio a doppio contorno (risk.outer nero fuori, risk.stroke tratteggiato
        /// dentro) + card bianca con ICN_Warning_Filled in deny, titolo Body Bold e istruzione Body. Perno al centro del cerchio.
        /// </summary>
        private static string WorldBuildRisk(UITheme theme, UIStrings strings)
        {
            float scale = UITheme.GetCanvasScale(theme.Hud.WorldLabelMinDistance);
            GameObject root = WorldBeginPrefab(WORLD_RISK_NAME, theme, new Vector2(WORLD_RISK_WIDTH, WORLD_RISK_HEIGHT),
                new Vector2(WORLD_RISK_CIRCLE * 0.5f / WORLD_RISK_WIDTH, 0.5f), scale, false, out bool existed);

            // Cerchio: anello esterno nero continuo, anello interno tratteggiato arancione.
            Image outer = ComponentsBuilder.AddImage(root.transform, "IMG_RiskOuter", WorldRingSprite("TEX_RiskRingOuter", 0.90f, 1f, 0),
                theme.GetColor(ColorTokens.RISK_OUTER), false);
            WorldPlaceTopLeft(outer.rectTransform, 0f, 0f, WORLD_RISK_CIRCLE, WORLD_RISK_CIRCLE);
            Image dashed = ComponentsBuilder.AddImage(root.transform, "IMG_RiskStroke", WorldRingSprite("TEX_RiskRingDashed", 0.74f, 0.88f, 16),
                theme.GetColor(ColorTokens.RISK_STROKE), false);
            WorldPlaceTopLeft(dashed.rectTransform, 0f, 0f, WORLD_RISK_CIRCLE, WORLD_RISK_CIRCLE);

            RectTransform card = WorldAddShape(root.transform, "PANEL_Card", theme, "HudCard", theme.RadiusCard, ColorTokens.SURFACE_CARD, true, false);
            WorldPlaceTopLeft(card, WORLD_RISK_CARD_X, (WORLD_RISK_HEIGHT - WORLD_RISK_CARD_H) * 0.5f, WORLD_RISK_CARD_W, WORLD_RISK_CARD_H);
            Image icon = ComponentsBuilder.AddIcon(card, "ICN_Warning", theme, "ICN_Warning_Filled", WORLD_ICON, ColorTokens.DENY);
            WorldPlaceTopLeft(icon.rectTransform, WORLD_PAD_X, WORLD_PAD_X, WORLD_ICON, WORLD_ICON);
            float textX = WORLD_PAD_X + WORLD_ICON + WORLD_GAP;
            float textW = WORLD_RISK_CARD_W - textX - WORLD_PAD_X;
            TMP_Text title = WorldAddText(card, "LBL_Title", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft, false);
            WorldPlaceTopLeft(title.rectTransform, textX, WORLD_PAD_Y, textW, WORLD_TITLE_H);
            TMP_Text detail = WorldAddText(card, "LBL_Instruction", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.TopLeft, true);
            WorldPlaceTopLeft(detail.rectTransform, textX, WORLD_PAD_Y + WORLD_TITLE_H, textW, WORLD_BODY_H);

            var label = BaseBuilder.GetOrAdd<WorldLabel>(root);
            label.Configure(LabelKind.Risk, theme, strings, ComponentsBuilder.LoadIcon("ICN_Warning_Filled"), root.GetComponent<CanvasGroup>(),
                title, detail, icon, null, null, null, null);
            EditorUtility.SetDirty(label);
            return WorldEndPrefab(root, WORLD_RISK_NAME, existed);
        }

        // =====================================================================
        // HUD_InteractionPrompt
        // =====================================================================

        /// <summary>
        /// HUD_InteractionPrompt 260 × 126 (Figma 101:340): card bianca con icona oggetto (brand.dark), titolo Body Bold e riga
        /// azione (pillola brand.dark col tasto + verbo); sotto la card linea a doppio tratto (grab.outer / grab.inner) e punto
        /// di aggancio. Perno al centro del punto.
        /// </summary>
        private static string WorldBuildInteractionPrompt(UITheme theme, UIStrings strings)
        {
            float scale = UITheme.GetCanvasScale(theme.Hud.WorldLabelMinDistance);
            GameObject root = WorldBeginPrefab(WORLD_PROMPT_NAME, theme, new Vector2(WORLD_PROMPT_WIDTH, WORLD_PROMPT_HEIGHT),
                new Vector2(0.5f, WORLD_PROMPT_DOT * 0.5f / WORLD_PROMPT_HEIGHT), scale, false, out bool existed);

            // Linea e punto sotto la card (doppio contorno nero + bianco, come il cerchio di grab).
            float lineX = (WORLD_PROMPT_WIDTH - WORLD_PROMPT_LINE_W) * 0.5f;
            Image lineOuter = WorldAddBar(root.transform, "IMG_LineOuter", theme.GetColor(ColorTokens.GRAB_OUTER));
            WorldPlaceTopLeft(lineOuter.rectTransform, lineX, WORLD_PROMPT_CARD_H, WORLD_PROMPT_LINE_W, WORLD_PROMPT_LINE_H);
            Image lineInner = WorldAddBar(root.transform, "IMG_LineInner", theme.GetColor(ColorTokens.GRAB_INNER));
            WorldPlaceTopLeft(lineInner.rectTransform, (WORLD_PROMPT_WIDTH - 2f) * 0.5f, WORLD_PROMPT_CARD_H, 2f, WORLD_PROMPT_LINE_H);
            float dotY = WORLD_PROMPT_HEIGHT - WORLD_PROMPT_DOT;
            Sprite dot = WorldRoundedSprite("HudDot", WORLD_PROMPT_DOT * 0.5f);
            Image dotOuter = ComponentsBuilder.AddImage(root.transform, "IMG_AnchorOuter", dot, theme.GetColor(ColorTokens.GRAB_OUTER), false);
            WorldPlaceTopLeft(dotOuter.rectTransform, (WORLD_PROMPT_WIDTH - WORLD_PROMPT_DOT) * 0.5f, dotY, WORLD_PROMPT_DOT, WORLD_PROMPT_DOT);
            float innerDot = WORLD_PROMPT_DOT - 6f;
            Image dotInner = ComponentsBuilder.AddImage(root.transform, "IMG_AnchorInner", WorldRoundedSprite("HudDotInner", innerDot * 0.5f),
                theme.GetColor(ColorTokens.GRAB_INNER), false);
            WorldPlaceTopLeft(dotInner.rectTransform, (WORLD_PROMPT_WIDTH - innerDot) * 0.5f, dotY + 3f, innerDot, innerDot);

            RectTransform card = WorldAddShape(root.transform, "PANEL_Card", theme, "HudCard", theme.RadiusCard, ColorTokens.SURFACE_CARD, true, false);
            WorldPlaceTopLeft(card, 0f, 0f, WORLD_PROMPT_WIDTH, WORLD_PROMPT_CARD_H);
            card.SetSiblingIndex(0);
            Image icon = ComponentsBuilder.AddIcon(card, "ICN_Object", theme, "ICN_BackHand", WORLD_ICON, ColorTokens.BRAND_DARK);
            WorldPlaceTopLeft(icon.rectTransform, WORLD_PAD_X, (WORLD_PROMPT_CARD_H - WORLD_ICON) * 0.5f, WORLD_ICON, WORLD_ICON);
            float textX = WORLD_PAD_X + WORLD_ICON + WORLD_GAP;
            float textW = WORLD_PROMPT_WIDTH - textX - WORLD_PAD_X;
            TMP_Text title = WorldAddText(card, "LBL_Title", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft, false);
            WorldPlaceTopLeft(title.rectTransform, textX, WORLD_PAD_X, textW, WORLD_TITLE_H);

            // Riga azione: pillola col tasto (larghezza adattata a runtime) + verbo.
            float rowY = WORLD_PAD_X + WORLD_TITLE_H + 2f;
            RectTransform pill = WorldAddShape(card, "PANEL_KeyPill", theme, "HudButton", theme.RadiusButton, ColorTokens.BRAND_DARK, false, false);
            WorldPlaceTopLeft(pill, textX, rowY, 56f, WORLD_PILL_H);
            TMP_Text key = WorldAddText(pill, "LBL_Key", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK, TextAlignmentOptions.Midline, false);
            UIComponentUtil.SetInset(key.rectTransform, 0f);
            TMP_Text verb = WorldAddText(card, "LBL_Verb", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft, false);
            WorldPlaceTopLeft(verb.rectTransform, textX + 56f + 6f, rowY, textW - 62f, WORLD_PILL_H);

            var label = BaseBuilder.GetOrAdd<WorldLabel>(root);
            label.Configure(LabelKind.Interaction, theme, strings, ComponentsBuilder.LoadIcon("ICN_BackHand"), root.GetComponent<CanvasGroup>(),
                title, verb, icon, key, pill, null, null);
            EditorUtility.SetDirty(label);
            return WorldEndPrefab(root, WORLD_PROMPT_NAME, existed);
        }

        // =====================================================================
        // HUD_ContextHelp
        // =====================================================================

        /// <summary>
        /// HUD_ContextHelp (Figma 101:374): pallino 40 brand.dark con ICN_Help_Filled (unico elemento con raycast, Button) e, a destra,
        /// scheda espansa 260 × 85 (titolo Body Bold + testo Body su 2 righe), chiusa di default. Perno al centro del pallino.
        /// </summary>
        private static string WorldBuildContextHelp(UITheme theme, UIStrings strings)
        {
            float scale = UITheme.GetCanvasScale(theme.Hud.WorldLabelMinDistance);
            GameObject root = WorldBeginPrefab(WORLD_CONTEXT_NAME, theme, new Vector2(WORLD_CONTEXT_WIDTH, WORLD_CONTEXT_HEIGHT),
                new Vector2(WORLD_CONTEXT_DOT * 0.5f / WORLD_CONTEXT_WIDTH, 0.5f), scale, true, out bool existed);

            // Pallino cliccabile.
            RectTransform dot = WorldAddShape(root.transform, "BTN_ContextDot", theme, "HudCircle", WORLD_CONTEXT_DOT * 0.5f, ColorTokens.BRAND_DARK, true, true);
            WorldPlaceTopLeft(dot, 0f, (WORLD_CONTEXT_HEIGHT - WORLD_CONTEXT_DOT) * 0.5f, WORLD_CONTEXT_DOT, WORLD_CONTEXT_DOT);
            Image icon = ComponentsBuilder.AddIcon(dot, "ICN_Help", theme, "ICN_Help_Filled", WORLD_ICON, ColorTokens.TEXT_ON_DARK);
            UIComponentUtil.SetCentered(icon.rectTransform, Vector2.zero, new Vector2(WORLD_ICON, WORLD_ICON));
            var button = BaseBuilder.GetOrAdd<Button>(dot.gameObject);
            button.transition = Selectable.Transition.None;
            button.targetGraphic = dot.Find("IMG_Fill").GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            // Scheda espansa (spenta di default).
            RectTransform card = WorldAddShape(root.transform, "PANEL_ExpandedCard", theme, "HudCard", theme.RadiusCard, ColorTokens.SURFACE_CARD, true, false);
            WorldPlaceTopLeft(card, WORLD_CONTEXT_DOT + WORLD_CONTEXT_GAP, 0f, WORLD_CONTEXT_CARD_W, WORLD_CONTEXT_HEIGHT);
            float textW = WORLD_CONTEXT_CARD_W - WORLD_PAD_X * 2f;
            TMP_Text title = WorldAddText(card, "LBL_Title", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft, false);
            WorldPlaceTopLeft(title.rectTransform, WORLD_PAD_X, WORLD_PAD_Y, textW, WORLD_TITLE_H);
            TMP_Text detail = WorldAddText(card, "LBL_Body", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.TopLeft, true);
            WorldPlaceTopLeft(detail.rectTransform, WORLD_PAD_X, WORLD_PAD_Y + WORLD_TITLE_H, textW, WORLD_BODY_H);
            card.gameObject.SetActive(false);

            var label = BaseBuilder.GetOrAdd<WorldLabel>(root);
            label.Configure(LabelKind.ContextHelp, theme, strings, null, root.GetComponent<CanvasGroup>(),
                title, detail, null, null, null, card.gameObject, button);
            EditorUtility.SetDirty(label);
            return WorldEndPrefab(root, WORLD_CONTEXT_NAME, existed);
        }

        // =====================================================================
        // HUD_DecisionPanel
        // =====================================================================

        /// <summary>
        /// HUD_DecisionPanel 480 × 300 (Figma 101:375), opaco surface.panel, radius.panel: riga domanda (ICN_RecordVoiceOver in
        /// brand.light + Subtitle Bold on-dark), 4 BTN_Secondary come opzioni (le usate si attivano a runtime) e barra del tempo.
        /// Scala per 1,2 m. Raycaster sul Canvas (UIWorldRaycaster). Salvato disattivo.
        /// </summary>
        private static string WorldBuildDecision(UITheme theme, UIStrings strings)
        {
            string secondaryPath = ComponentsBuilder.PrefabPath("BTN_Secondary");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(secondaryPath) == null)
                return WORLD_DECISION_NAME + ": interrotto, BTN_Secondary mancante (eseguire SI81/UI/Build Components).";

            float width = theme.GetNumber(WorldTokens.DECISION_WIDTH);
            float padX = theme.GetNumber(WorldTokens.DECISION_PADDING_X);
            float padY = theme.GetNumber(WorldTokens.DECISION_PADDING_Y);
            float icon = theme.GetNumber(WorldTokens.DECISION_ICON);
            float gap = theme.GetNumber(NumberTokens.SPACE_S);
            float contentWidth = width - padX * 2f;
            float scale = UITheme.GetCanvasScale(UITheme.DECISION_DISTANCE_M);
            GameObject root = WorldBeginPrefab(WORLD_DECISION_NAME, theme, new Vector2(width, WORLD_DECISION_DEFAULT_HEIGHT),
                new Vector2(0.5f, 0.5f), scale, true, out bool existed);

            // Sfondo opaco con ombra (riempimento che intercetta i raggi: niente "buchi" tra le opzioni).
            RectTransform panel = WorldAddShape(root.transform, "PANEL_Background", theme, "HudPanel", theme.RadiusPanel, ColorTokens.SURFACE_PANEL, true, true);
            UIComponentUtil.SetInset(panel, 0f);
            panel.SetSiblingIndex(0);

            // Riga domanda: icona a sinistra + testo che va a capo.
            RectTransform row = BaseBuilder.GetOrCreateRectChild(root.transform, "PANEL_Question");
            WorldPlaceTopCenter(row, padY, contentWidth, icon);
            Image questionIcon = ComponentsBuilder.AddIcon(row, "ICN_Question", theme, "ICN_RecordVoiceOver", icon, ColorTokens.BRAND_LIGHT);
            WorldPlaceTopLeft(questionIcon.rectTransform, 0f, 0f, icon, icon);
            TMP_Text question = WorldAddText(row, "LBL_Question", theme, strings, TextStyleIds.SUBTITLE_BOLD, ColorTokens.TEXT_ON_DARK, TextAlignmentOptions.TopLeft, true);
            question.overflowMode = TextOverflowModes.Overflow;
            WorldPlaceTopLeft(question.rectTransform, icon + gap, 0f, contentWidth - icon - gap, icon);

            // Opzioni: 4 istanze di BTN_Secondary a tutta larghezza, dall'alto in basso (la posizione la scrive il codice).
            const int OPTION_COUNT = 4;
            var buttons = new UIButton[OPTION_COUNT];
            for (int i = 0; i < OPTION_COUNT; i++)
            {
                GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, secondaryPath, $"BTN_Option_{i + 1:00}");
                instance.layer = LayerMask.NameToLayer("UI");
                var buttonRect = (RectTransform)instance.transform;
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.anchoredPosition = new Vector2(0f, -(padY + icon + gap + i * 58f));
                buttons[i] = instance.GetComponent<UIButton>();
                var serialized = new SerializedObject(buttons[i]);
                serialized.FindProperty("fixedWidth").floatValue = contentWidth;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                buttons[i].SetStyle(UIButtonStyle.Secondary, UIButtonSurface.Dark);
                buttons[i].SetLabelText(string.Empty);
                buttons[i].SetIcon(null);
                BaseBuilder.RecordInstanceOverrides(instance);
            }

            // Barra del tempo (Crisis): binario semitrasparente + riempimento brand.light che si accorcia da destra.
            float barHeight = theme.GetNumber(WorldTokens.DECISION_TIMER_HEIGHT);
            RectTransform track = BaseBuilder.GetOrCreateRectChild(root.transform, "PANEL_Timer");
            WorldPlaceTopCenter(track, WORLD_DECISION_DEFAULT_HEIGHT - padY - barHeight, contentWidth, barHeight);
            Sprite bar = WorldRoundedSprite("HudBar", WORLD_BAR_RADIUS);
            Image trackImage = BaseBuilder.GetOrAdd<Image>(track.gameObject);
            trackImage.sprite = bar;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = UIComponentUtil.WithAlpha(theme.GetColor(ColorTokens.SURFACE_CARD), 0.25f);
            trackImage.raycastTarget = false;
            Image fill = ComponentsBuilder.AddImage(track, "IMG_TimerFill", bar, theme.GetColor(ColorTokens.BRAND_LIGHT), false);
            UIComponentUtil.SetInset(fill.rectTransform, 0f);
            track.gameObject.SetActive(false);

            var panelComponent = BaseBuilder.GetOrAdd<DecisionPanel>(root);
            panelComponent.Configure(theme, root.GetComponent<CanvasGroup>(), row, question, questionIcon.rectTransform, buttons, track, fill.rectTransform);
            EditorUtility.SetDirty(panelComponent);
            return WorldEndPrefab(root, WORLD_DECISION_NAME, existed);
        }

        // =====================================================================
        // HUD_OutcomePanel
        // =====================================================================

        /// <summary>
        /// HUD_OutcomePanel 480 × 160 (Figma 101:410): card bianca (surface.card, radius.panel, ombra hudCard) con LBL_Outcome Large
        /// a colore pieno (OnCard) e il motivo (Subtitle Regular nero, centrato, a capo). Nessun raycaster (nessun input, 3–5 s).
        /// Scala per 1,5 m. Salvato disattivo.
        /// </summary>
        private static string WorldBuildOutcome(UITheme theme, UIStrings strings)
        {
            string outcomePath = ComponentsBuilder.PrefabPath("LBL_Outcome");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(outcomePath) == null)
                return WORLD_OUTCOME_NAME + ": interrotto, LBL_Outcome mancante (eseguire SI81/UI/Build Components).";

            float width = theme.GetNumber(WorldTokens.OUTCOME_WIDTH);
            float height = theme.GetNumber(WorldTokens.OUTCOME_MIN_HEIGHT);
            float pad = theme.GetNumber(WorldTokens.OUTCOME_PADDING);
            float scale = UITheme.GetCanvasScale(UITheme.OUTCOME_DISTANCE_M);
            GameObject root = WorldBeginPrefab(WORLD_OUTCOME_NAME, theme, new Vector2(width, height), new Vector2(0.5f, 0.5f), scale, false, out bool existed);

            RectTransform card = WorldAddShape(root.transform, "PANEL_Background", theme, "HudPanel", theme.RadiusPanel, ColorTokens.SURFACE_CARD, true, false);
            UIComponentUtil.SetInset(card, 0f);
            card.SetSiblingIndex(0);

            // Pillola dell'esito: Large, colore pieno (consentito: card bianca, CLAUDE.md §4.9).
            GameObject pillGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, outcomePath, "LBL_Outcome");
            pillGo.layer = LayerMask.NameToLayer("UI");
            var pillRect = (RectTransform)pillGo.transform;
            pillRect.anchorMin = pillRect.anchorMax = new Vector2(0.5f, 1f);
            pillRect.pivot = new Vector2(0.5f, 1f);
            pillRect.anchoredPosition = new Vector2(0f, -pad);
            var outcomeLabel = pillGo.GetComponent<OutcomeLabel>();
            outcomeLabel.SetSize(LabelSize.Large);
            outcomeLabel.SetPresentation(OutcomePresentation.OnCard);
            BaseBuilder.RecordInstanceOverrides(pillGo);

            // Motivo: una riga (a capo se serve), centrato, nero.
            TMP_Text reason = WorldAddText(root.transform, "LBL_Reason", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.Top, true);
            reason.overflowMode = TextOverflowModes.Overflow;
            WorldPlaceTopCenter(reason.rectTransform, pad + 60f + theme.GetNumber(WorldTokens.OUTCOME_GAP), width - pad * 2f, 30f);

            var panelComponent = BaseBuilder.GetOrAdd<OutcomePanel>(root);
            panelComponent.Configure(theme, root.GetComponent<CanvasGroup>(), outcomeLabel, reason);
            EditorUtility.SetDirty(panelComponent);
            return WorldEndPrefab(root, WORLD_OUTCOME_NAME, existed);
        }
    }
}
