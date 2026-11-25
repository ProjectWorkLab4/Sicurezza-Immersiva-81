using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================================================
// HELPER CONDIVISI (Blocco A) che le altre famiglie PUOSSONO usare (firme esatte):
//   internal const string COMPONENTS_FOLDER                         // "Assets/_Project/Home/UI/Prefabs/Components"
//   internal struct ShapeSprites { Sprite Rounded, ShadowRest, ShadowLift, Glow; }
//   internal static string PrefabPath(string prefabName)
//   internal static GameObject CreateOrLoadPrefab(string prefabName, out bool existed)
//   internal static string SavePrefab(GameObject root, string prefabName, bool existed)
//   internal static Sprite LoadSprite(string assetPath)
//   internal static Sprite LoadIcon(string iconName)                 // "ICN_Check" -> sprite da Icons/
//   internal static ShapeSprites EnsureShapeSprites(UITheme theme, string family, float radius)
//   internal static Image AddImage(Transform parent, string name, Sprite sprite, Color color, bool raycast)
//   internal static UIStringText AddLabel(Transform parent, string name, UITheme theme, UIStrings strings, string textStyle,
//                                         string colorToken, string key, TextAlignmentOptions alignment)
//   internal static Image AddIcon(Transform parent, string name, UITheme theme, string iconName, float size, string colorToken)
//   internal static UIStateVisual AddStateVisual(GameObject root, UITheme theme, ShapeSprites shape, float radius, bool selectable,
//                                                bool shadowAtRest, bool withGlow, bool withCheck, bool compactCheck)
//   internal static GameObject AddCheckBadge(Transform parent, UITheme theme, bool compact)
// I prefab di Blocco A si trovano in Assets/_Project/Home/UI/Prefabs/Components/<Nome>.prefab.
// =============================================================================================================

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder dei componenti del passo 3 (Blocco A), diviso in file parziali: pulsanti (questo file + .Buttons), etichette (.Labels),
    /// righe impostazioni (.Settings) e card (.Cards). Menu <c>SI81/UI/Build Components</c> → <see cref="BuildAll"/>: crea o aggiorna
    /// tutti i prefab in Assets/_Project/Home/UI/Prefabs/Components/ (create-or-update sul posto: stessi GUID, figli cercati per nome,
    /// nessun doppione, sicuro da rieseguire; mai YAML a mano). Richiede SO_UITheme / SO_UIStrings_It già generati (SI81/UI/Build Theme);
    /// se mancano i token del Blocco A rilancia Build Theme da solo. Questo file contiene il punto di ingresso e gli strumenti comuni
    /// (prefab, etichette, icone, sprite 9-slice di forma, <see cref="UIStateVisual"/>).
    /// </summary>
    internal static partial class ComponentsBuilder
    {
        /// <summary>Cartella dei prefab dei componenti.</summary>
        internal const string COMPONENTS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Components";

        /// <summary>Sprite 9-slice di una forma: riempimento arrotondato, ombra a riposo, ombra di sollevamento e glow di selezione.</summary>
        internal struct ShapeSprites
        {
            /// <summary>Rettangolo arrotondato bianco (TEX_&lt;Famiglia&gt;Rounded).</summary>
            public Sprite Rounded;
            /// <summary>Ombra card/pressed (TEX_&lt;Famiglia&gt;ShadowRest).</summary>
            public Sprite ShadowRest;
            /// <summary>Ombra hover/selezione (TEX_&lt;Famiglia&gt;ShadowLift).</summary>
            public Sprite ShadowLift;
            /// <summary>Glow brand.light della selezione (TEX_&lt;Famiglia&gt;Glow).</summary>
            public Sprite Glow;
        }

        /// <summary>
        /// Voce di menu: crea o aggiorna tutti i prefab dei componenti del Blocco A.
        /// </summary>
        [MenuItem("SI81/UI/Build Components", priority = 24)]
        public static void BuildComponentsMenu()
        {
            BuildAll();
        }

        /// <summary>
        /// Esegue in ordine pulsanti, etichette, riga BTN_ListRow (annida le stelle di LBL_StarRating, che esistono solo dopo le etichette),
        /// righe impostazioni e card, e salva gli asset. Un errore in una famiglia non ferma le altre.
        /// </summary>
        internal static void BuildAll()
        {
            if (!EnsureThemeTokens()) return;
            BaseBuilder.EnsureFolder(COMPONENTS_FOLDER);

            RunStep("Pulsanti", BuildButtons);
            RunStep("Etichette", BuildLabels);
            // BTN_ListRow annida LBL_StarRating: ora che le etichette esistono la riga si completa (rieseguibile).
            RunStep("Riga elenco (stelle)", BuildListRowAfterLabels);
            RunStep("Righe impostazioni", BuildSettingsRows);
            // Le card annidano BTN_Primary e LBL_LevelBadge: vanno per ultime.
            RunStep("Card", BuildCards);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // Gli sprite nuovi (TEX_ dei componenti) stanno in Sprites/: l'atlante TEX_AtlasUI li include per cartella; si riallinea.
            Debug.Log("[SI81 UI] " + SpriteAtlasBuilder.BuildAll());
            Debug.Log("[SI81 UI] Build Components completato (prefab in " + COMPONENTS_FOLDER + ").");
        }

        /// <summary>
        /// Esegue un passo del builder isolando le eccezioni, così le altre famiglie vengono costruite comunque.
        /// </summary>
        /// <param name="stepName">Nome del passo, per i messaggi.</param>
        /// <param name="step">Metodo da eseguire.</param>
        private static void RunStep(string stepName, Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SI81 UI] Build Components · {stepName}: errore, passo interrotto. {exception}");
            }
        }

        /// <summary>
        /// Controlla che SO_UITheme contenga i token del Blocco A (pulsanti, stati, dialogo). Se mancano rilancia Build Theme
        /// (idempotente) e ricontrolla; se mancano ancora segnala l'errore.
        /// </summary>
        /// <returns>true se i token ci sono e si può procedere.</returns>
        private static bool EnsureThemeTokens()
        {
            if (HasButtonTokens(AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET))) return true;
            Debug.Log("[SI81 UI] Build Components: token del Blocco A assenti in SO_UITheme, eseguo Build Theme.");
            Debug.Log(ThemeBuilder.BuildAll());
            AssetDatabase.Refresh();
            if (HasButtonTokens(AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET))) return true;
            Debug.LogError("[SI81 UI] Build Components interrotto: SO_UITheme non contiene i token del Blocco A "
                + "(size.btn.height, color.dialog.backdrop...). Controllare tokens_figma.json e rieseguire SI81/UI/Build Theme.");
            return false;
        }

        /// <summary>
        /// true se il tema esiste e contiene i token aggiunti dal Blocco A · Pulsanti.
        /// </summary>
        private static bool HasButtonTokens(UITheme theme)
        {
            if (theme == null) return false;
            return theme.Numbers.Any(n => n != null && n.Token == ButtonTokens.HEIGHT)
                && theme.TryGetColor(ButtonTokens.DIALOG_BACKDROP, out _);
        }

        // =====================================================================
        // Prefab
        // =====================================================================

        /// <summary>
        /// Percorso del prefab di un componente.
        /// </summary>
        /// <param name="prefabName">Nome del prefab, es. BTN_Primary.</param>
        /// <returns>Percorso sotto Prefabs/Components.</returns>
        internal static string PrefabPath(string prefabName)
        {
            return COMPONENTS_FOLDER + "/" + prefabName + ".prefab";
        }

        /// <summary>
        /// Carica il contenuto del prefab esistente (per aggiornarlo sul posto) oppure crea la radice di un nuovo prefab
        /// (RectTransform, layer UI, nome = nome dell'asset). Da chiudere SEMPRE con <see cref="SavePrefab"/>.
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice).</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        /// <returns>La radice da modificare.</returns>
        internal static GameObject CreateOrLoadPrefab(string prefabName, out bool existed)
        {
            BaseBuilder.EnsureFolder(COMPONENTS_FOLDER);
            string path = PrefabPath(prefabName);
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            if (existed) RemoveMissingScripts(root);
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)root.transform;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return root;
        }

        /// <summary>
        /// Toglie i componenti con script mancante da tutta la gerarchia del prefab caricato: un prefab con uno script mancante
        /// non si può salvare. Capita se una classe MonoBehaviour sta in un file con nome diverso (ora ogni classe ha il suo file).
        /// </summary>
        /// <param name="root">Radice del contenuto del prefab caricato.</param>
        internal static void RemoveMissingScripts(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }

        /// <summary>
        /// Salva la radice come prefab (stesso GUID se esisteva) e libera la memoria.
        /// </summary>
        /// <param name="root">Radice ottenuta da <see cref="CreateOrLoadPrefab"/>.</param>
        /// <param name="prefabName">Nome del prefab.</param>
        /// <param name="existed">Valore restituito da <see cref="CreateOrLoadPrefab"/>.</param>
        /// <returns>Riepilogo in italiano.</returns>
        internal static string SavePrefab(GameObject root, string prefabName, bool existed)
        {
            string path = PrefabPath(prefabName);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                return success ? $"{prefabName}: {(existed ? "aggiornato" : "creato")} ({path})." : $"{prefabName}: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (existed) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Asset
        // =====================================================================

        /// <summary>
        /// Carica uno sprite dal percorso.
        /// </summary>
        /// <param name="assetPath">Percorso dello sprite.</param>
        /// <returns>Lo sprite o null.</returns>
        internal static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        /// <summary>
        /// Carica lo sprite di un'icona ICN_ per nome (null e un errore se manca).
        /// </summary>
        /// <param name="iconName">Nome, es. ICN_Check o ICN_Home_Filled.</param>
        /// <returns>Lo sprite o null.</returns>
        internal static Sprite LoadIcon(string iconName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBuilder.ICONS_FOLDER + "/" + iconName + ".png");
            if (sprite == null) Debug.LogError($"[SI81 UI] Icona {iconName} assente. Eseguire SI81/UI/Build Theme.");
            return sprite;
        }

        /// <summary>
        /// Crea o aggiorna gli sprite 9-slice di una forma con il raggio dato: <c>TEX_&lt;Famiglia&gt;Rounded</c>, <c>ShadowRest</c>
        /// (ruolo card), <c>ShadowLift</c> (ruolo selected.lift) e <c>Glow</c> (ruolo selected.glow). Rieseguibile ("invariato" se uguale).
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="family">Nome della famiglia in PascalCase, es. "Button".</param>
        /// <param name="radius">Raggio degli angoli in unità Canvas (es. radius.button).</param>
        /// <returns>Gli sprite caricati.</returns>
        internal static ShapeSprites EnsureShapeSprites(UITheme theme, string family, float radius)
        {
            string folder = SpriteBuilder.SPRITES_FOLDER;
            string rounded = $"{folder}/TEX_{family}Rounded.png";
            string shadowRest = $"{folder}/TEX_{family}ShadowRest.png";
            string shadowLift = $"{folder}/TEX_{family}ShadowLift.png";
            string glow = $"{folder}/TEX_{family}Glow.png";
            SpriteBuilder.BuildRoundedSprite(rounded, radius);
            SpriteBuilder.BuildShadowSprite(shadowRest, radius, theme.GetShadow(ShadowRoles.CARD));
            SpriteBuilder.BuildShadowSprite(shadowLift, radius, theme.GetShadow(ShadowRoles.SELECTED_LIFT));
            SpriteBuilder.BuildShadowSprite(glow, radius, theme.GetShadow(ShadowRoles.SELECTED_GLOW));
            return new ShapeSprites { Rounded = LoadSprite(rounded), ShadowRest = LoadSprite(shadowRest), ShadowLift = LoadSprite(shadowLift), Glow = LoadSprite(glow) };
        }

        // =====================================================================
        // Figli comuni
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna una Image figlia (nome = nome in scena). 9-slice se lo sprite ha bordi; ombre e glow non ricevono raggi.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome del figlio (prefisso IMG_ / ICN_).</param>
        /// <param name="sprite">Sprite (può essere null).</param>
        /// <param name="color">Colore iniziale (il runtime lo riscrive dal tema).</param>
        /// <param name="raycast">true solo per l'area interattiva.</param>
        /// <returns>La Image.</returns>
        internal static Image AddImage(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var image = BaseBuilder.GetOrAdd<Image>(rect.gameObject);
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>
        /// Crea o aggiorna un'etichetta TMP con UIStringText (stile e colore del tema, chiave del testo, una riga).
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (prefisso LBL_).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        /// <param name="textStyle">Stile di testo del tema (vedi TextStyleIds).</param>
        /// <param name="colorToken">Token colore (vedi ColorTokens).</param>
        /// <param name="key">Chiave del testo (può essere vuota).</param>
        /// <param name="alignment">Allineamento TMP.</param>
        /// <returns>Il componente UIStringText.</returns>
        internal static UIStringText AddLabel(Transform parent, string name, UITheme theme, UIStrings strings, string textStyle,
            string colorToken, string key, TextAlignmentOptions alignment)
        {
            UIStringText label = MenuPanelBuilder.BuildLabel(parent, name, strings, theme, textStyle, colorToken);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            if (!string.IsNullOrEmpty(key))
            {
                if (!strings.Contains(key)) Debug.LogError($"[SI81 UI] Chiave \"{key}\" assente in SO_UIStrings_It ({name}).");
                label.SetKey(key);
            }
            EditorUtility.SetDirty(tmp);
            return label;
        }

        /// <summary>
        /// Crea o aggiorna un'icona figlia (Image decorativa, sprite ICN_, tinta dal tema).
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (prefisso ICN_).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="iconName">Nome dello sprite, es. ICN_PlayArrow (vuoto = nessuno sprite).</param>
        /// <param name="size">Lato in unità Canvas.</param>
        /// <param name="colorToken">Token colore della tinta.</param>
        /// <returns>La Image dell'icona.</returns>
        internal static Image AddIcon(Transform parent, string name, UITheme theme, string iconName, float size, string colorToken)
        {
            Sprite sprite = string.IsNullOrEmpty(iconName) ? null : LoadIcon(iconName);
            Image image = AddImage(parent, name, sprite, theme.GetColor(colorToken), false);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            return image;
        }

        /// <summary>
        /// Aggiunge o aggiorna <see cref="UIStateVisual"/> sulla radice con i figli IMG_Glow e IMG_Shadow (primi due fratelli) e,
        /// se richiesto, il cerchio di selezione IMG_Check (ultimo fratello). Applica subito lo stato Normal.
        /// </summary>
        /// <param name="root">Radice del componente (riceve UIStateVisual).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="shape">Sprite di forma (da <see cref="EnsureShapeSprites"/>).</param>
        /// <param name="radius">Raggio degli angoli della forma.</param>
        /// <param name="selectable">Supporta lo stato Selected.</param>
        /// <param name="shadowAtRest">true = ombra card anche a riposo.</param>
        /// <param name="withGlow">true = crea IMG_Glow (glow brand.light della selezione).</param>
        /// <param name="withCheck">true = crea il cerchio bianco con la spunta della selezione.</param>
        /// <param name="compactCheck">true = cerchio compatto (22) per componenti bassi, false = 30 (Figma).</param>
        /// <returns>Il componente configurato.</returns>
        internal static UIStateVisual AddStateVisual(GameObject root, UITheme theme, ShapeSprites shape, float radius, bool selectable,
            bool shadowAtRest, bool withGlow, bool withCheck, bool compactCheck)
        {
            var visual = BaseBuilder.GetOrAdd<UIStateVisual>(root);
            visual.Theme = theme;
            visual.ScaleTarget = (RectTransform)root.transform;
            visual.Selectable = selectable;
            visual.ShadowAtRest = shadowAtRest;
            visual.ScaleOnHover = true;
            visual.ShadowRestSprite = shape.ShadowRest;
            visual.ShadowLiftSprite = shape.ShadowLift;
            visual.CornerRadius = radius;

            // Ombra (sempre) e glow (opzionale): decorativi, sotto a tutto.
            Image shadow = AddImage(root.transform, "IMG_Shadow", shape.ShadowRest, Color.clear, false);
            shadow.fillCenter = false;
            visual.Shadow = shadow;
            Image glow = null;
            if (withGlow) glow = AddImage(root.transform, "IMG_Glow", shape.Glow, Color.clear, false);
            if (glow != null)
            {
                glow.fillCenter = false;
                glow.transform.SetSiblingIndex(0);
            }
            shadow.transform.SetSiblingIndex(glow != null ? 1 : 0);
            visual.Glow = glow;

            // Cerchio di selezione, sempre l'ultimo (sopra tutto).
            Transform existingCheck = root.transform.Find("IMG_Check");
            if (withCheck)
            {
                GameObject check = AddCheckBadge(root.transform, theme, compactCheck);
                check.transform.SetAsLastSibling();
                visual.CheckMark = check;
            }
            else
            {
                visual.CheckMark = null;
                if (existingCheck != null) UnityEngine.Object.DestroyImmediate(existingCheck.gameObject);
            }

            visual.Refresh(true);
            EditorUtility.SetDirty(visual);
            return visual;
        }

        /// <summary>
        /// Crea o aggiorna il cerchio bianco con la spunta (IMG_Check) nell'angolo in alto a destra, sporgente di offset.state-check:
        /// IMG_CheckShadow (ombra check.badge), IMG_CheckCircle (surface.card) e ICN_Check (ICN_CheckCircle_Filled in brand.dark).
        /// Nessun bordo (CLAUDE.md §4.1). Resta DISATTIVO: lo attiva <see cref="UIStateVisual"/> da selezionato.
        /// </summary>
        /// <param name="parent">Radice del componente.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="compact">true = lato size.state-check.compact (22), false = size.state-check (30).</param>
        /// <returns>Il GameObject IMG_Check.</returns>
        internal static GameObject AddCheckBadge(Transform parent, UITheme theme, bool compact)
        {
            float size = theme.GetNumber(compact ? ButtonTokens.STATE_CHECK_COMPACT : ButtonTokens.STATE_CHECK);
            float offset = theme.GetNumber(ButtonTokens.STATE_CHECK_OFFSET);
            string family = compact ? "CheckBadgeCompact" : "CheckBadge";
            string rounded = $"{SpriteBuilder.SPRITES_FOLDER}/TEX_{family}Rounded.png";
            string shadowPath = $"{SpriteBuilder.SPRITES_FOLDER}/TEX_{family}Shadow.png";
            SpriteBuilder.BuildRoundedSprite(rounded, size * 0.5f);
            UITheme.ShadowToken shadowToken = theme.GetShadow(ShadowRoles.CHECK_BADGE);
            SpriteBuilder.BuildShadowSprite(shadowPath, size * 0.5f, shadowToken);

            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, "IMG_Check");
            // Ancora in alto a destra; il centro del cerchio sta a (size/2 - offset) dentro l'angolo.
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(offset - size * 0.5f, offset - size * 0.5f);

            Image shadow = AddImage(rect, "IMG_CheckShadow", LoadSprite(shadowPath), shadowToken != null ? shadowToken.Color : Color.clear, false);
            shadow.fillCenter = false;
            float expand = PanelSprites.GetShadowLayout(size * 0.5f, shadowToken).PaddingUnits;
            PanelLayout.SetRect(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            Image circle = AddImage(rect, "IMG_CheckCircle", LoadSprite(rounded), theme.SurfaceCard, false);
            UIComponentUtil.SetInset(circle.rectTransform, 0f);
            float iconSize = compact ? size - 4f : 24f;
            Image icon = AddIcon(rect, "ICN_Check", theme, "ICN_CheckCircle_Filled", iconSize, ColorTokens.BRAND_DARK);
            UIComponentUtil.SetCentered(icon.rectTransform, Vector2.zero, new Vector2(iconSize, iconSize));
            shadow.transform.SetSiblingIndex(0);
            circle.transform.SetSiblingIndex(1);
            icon.transform.SetSiblingIndex(2);
            rect.gameObject.SetActive(false);
            return rect.gameObject;
        }
    }
}
