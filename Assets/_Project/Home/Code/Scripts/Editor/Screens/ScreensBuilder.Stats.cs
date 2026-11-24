using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Statistiche" del builder delle schermate (Blocco B · writer B3): crea o aggiorna il prefab
    /// <c>Prefabs/Screens/PANEL_Stats.prefab</c> (UIScreen + PANEL_Menu annidato + elenco scenari + scheda bianca di riepilogo/dettaglio).
    /// Contiene anche gli strumenti con prefisso <c>Stats…</c> riusati da <c>ScreensBuilder.Profile.cs</c> (stesso writer):
    /// apertura/salvataggio del prefab, rettangoli, etichette, istanze annidate, scheda bianca e collegamento di UIScreen.OnShown.
    /// Create-or-update: GUID invariati, figli cercati per nome, nessun doppione, mai YAML a mano.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Cartella dei prefab delle schermate.</summary>
        private const string STATS_SCREENS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Screens";

        /// <summary>
        /// Crea o aggiorna PANEL_Stats (Figma "Statistiche A/B", FIGMA_SPECS §2). Richiede tema, testi e i prefab dei componenti del Blocco A.
        /// </summary>
        static void BuildStats()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] PANEL_Stats: interrotto, SO_UITheme / SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return;
            }
            string[] needed = { "BTN_ListRow", "BTN_Back", "PANEL_StatTile", "LBL_StarRating", "LBL_Outcome", "LBL_DimensionBar", "LBL_LevelBadge" };
            foreach (string prefab in needed)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ComponentsBuilder.PrefabPath(prefab)) == null)
                {
                    Debug.LogError($"[SI81 UI] PANEL_Stats: interrotto, il prefab {prefab} manca (eseguire SI81/UI/Build Components).");
                    return;
                }

            GameObject root = StatsOpenPrefab("PANEL_Stats", out bool existed);
            try
            {
                var screen = BaseBuilder.GetOrAdd<UIScreen>(root);
                screen.Configure(UIScreenId.Stats, theme);

                // Pannello vetro: titolo "Statistiche", nessun indietro, altezza di default (475).
                GameObject panelGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
                panelGo.transform.SetSiblingIndex(0);
                var panel = panelGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(panel, "ui.stats.title", string.Empty, false, 0f);
                RectTransform content = panel.ContentRoot;
                StatsContentSize(theme, panel, out float contentW, out float contentH);

                // Colonne: elenco (440, size.list-row.width) | gap.grid | scheda bianca (resto = 256).
                float listW = theme.GetNumber(ButtonTokens.ROW_WIDTH);
                float gap = theme.GapGrid;
                float detailW = contentW - listW - gap;

                RectTransform list = StatsRect(content, "PANEL_List", 0f, 0f, listW, contentH);
                ListRowButton rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ComponentsBuilder.PrefabPath("BTN_ListRow")).GetComponent<ListRowButton>();

                RectTransform card = StatsAddCard(content, "PANEL_Detail", theme, listW + gap, 0f, detailW, contentH);
                float pad = theme.GetNumber(NumberTokens.SPACE_M); // padding della scheda = 12
                float innerW = detailW - 2f * pad;
                float innerH = contentH - 2f * pad;

                // ---------- A · Riepilogo ----------
                RectTransform summary = StatsRect(card, "PANEL_Summary", pad, pad, innerW, innerH);
                float titleH = PanelLayout.LineHeight(theme.Subtitle);
                UIStringText summaryTitle = StatsText(summary, "LBL_DetailTitle", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY,
                    "ui.stats.summary", TextAlignmentOptions.MidlineLeft, 0f, 0f, innerW, titleH);
                var tiles = new StatTile[4];
                var accents = new[] { StatAccent.Positive, StatAccent.Partial, StatAccent.Negative, StatAccent.Neutral };
                const float TILE_W = 112f;
                const float TILE_H = 96f;
                float tileGap = theme.GetNumber(NumberTokens.SPACE_S); // gap tra tessere (Figma 8)
                float tilesTop = titleH + tileGap;
                for (int i = 0; i < tiles.Length; i++)
                {
                    GameObject tileGo = StatsInstance(summary, "PANEL_StatTile", "PANEL_StatTile_" + accents[i],
                        (i % 2) * (TILE_W + tileGap), tilesTop + (i / 2) * (TILE_H + tileGap), TILE_W, TILE_H);
                    tiles[i] = tileGo.GetComponent<StatTile>();
                    tiles[i].SetStat(accents[i], 0);
                    BaseBuilder.RecordInstanceOverrides(tileGo);
                }
                float averageTop = tilesTop + 2f * TILE_H + 2f * tileGap;
                UIStringText average = StatsText(summary, "LBL_Average", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                    string.Empty, TextAlignmentOptions.TopLeft, 0f, averageTop, innerW, 3f * PanelLayout.LineHeight(theme.Body));
                average.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;

                // ---------- B · Dettaglio dello scenario ----------
                RectTransform mission = StatsRect(card, "PANEL_MissionDetail", pad, pad, innerW, innerH);
                float backSize = theme.TargetMinSize;
                GameObject backGo = StatsInstance(mission, "BTN_Back", "BTN_Back", 0f, 0f, backSize, backSize);
                StatsMarkInCardBack(backGo);
                UIStringText missionTitle = StatsText(mission, "LBL_MissionTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    string.Empty, TextAlignmentOptions.MidlineLeft, backSize + pad, 0f, innerW - backSize - pad, backSize);
                missionTitle.GetComponent<TMP_Text>().overflowMode = TextOverflowModes.Ellipsis;

                float line = PanelLayout.LineHeight(theme.Body);
                float lineSmall = 2f; // spazio tra punteggio e stelle (Figma gap 2)
                float section = 6f; // spazio tra sezioni (Figma gap 6)

                // Miglior risultato: "SPI N/100" Bold + stelle.
                float bestTop = backSize + section;
                RectTransform best = StatsRect(mission, "PANEL_Best", 0f, 0f, innerW, innerH);
                UIStringText bestScore = StatsText(best, "LBL_BestScore", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    string.Empty, TextAlignmentOptions.MidlineLeft, 0f, bestTop, innerW, line);
                bestScore.GetComponent<TMP_Text>().enableAutoSizing = true;
                bestScore.GetComponent<TMP_Text>().fontSizeMin = 12f;
                bestScore.GetComponent<TMP_Text>().fontSizeMax = theme.BodyBold.Size;
                GameObject starsGo = StatsInstance(best, "LBL_StarRating", "LBL_StarRating", 0f, bestTop + line + lineSmall, -1f, -1f);
                var bestStars = starsGo.GetComponent<StarRating>();
                bestStars.SetStars(0, true);
                BaseBuilder.RecordInstanceOverrides(starsGo);

                // Scenario mai giocato: solo "Non giocato".
                RectTransform notPlayed = StatsRect(mission, "PANEL_NotPlayed", 0f, 0f, innerW, innerH);
                StatsText(notPlayed, "LBL_NotPlayed", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                    "ui.stats.not_played", TextAlignmentOptions.MidlineLeft, 0f, bestTop, innerW, line);

                // Ultimo tentativo: titolo, esito (pillola su card bianca), 5 barre, tentativi e livello massimo.
                RectTransform last = StatsRect(mission, "PANEL_LastAttempt", 0f, 0f, innerW, innerH);
                float lastTop = bestTop + line + lineSmall + 24f + section;
                StatsText(last, "LBL_LastAttempt", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    "ui.stats.last_attempt", TextAlignmentOptions.MidlineLeft, 0f, lastTop, innerW, line);
                float outcomeTop = lastTop + line + 4f;
                GameObject outcomeGo = StatsInstance(last, "LBL_Outcome", "LBL_Outcome", 0f, outcomeTop, -1f, -1f);
                var outcome = outcomeGo.GetComponent<OutcomeLabel>();
                // Il dettaglio sta su una card BIANCA: esito a colori pieni (CLAUDE.md §4.9), non la pillola bianca da intestazione vetro.
                outcome.SetPresentation(OutcomePresentation.OnCard);
                outcome.SetSize(LabelSize.Small);
                outcome.SetOutcome(OutcomeType.Positive);
                BaseBuilder.RecordInstanceOverrides(outcomeGo);

                var bars = new DimensionBar[5];
                float barsTop = outcomeTop + 32f + 4f;
                float barGap = 4f;
                for (int i = 0; i < bars.Length; i++)
                {
                    GameObject barGo = StatsInstance(last, "LBL_DimensionBar", "LBL_DimensionBar_" + (DimensionKind)i,
                        0f, barsTop + i * (line + barGap), innerW, line);
                    bars[i] = barGo.GetComponent<DimensionBar>();
                    bars[i].SetSurface(LabelSurface.Light);
                    bars[i].SetDimension((DimensionKind)i, 0);
                    BaseBuilder.RecordInstanceOverrides(barGo);
                }
                float attemptsTop = barsTop + bars.Length * line + (bars.Length - 1) * barGap + section;
                UIStringText attempts = StatsText(last, "LBL_Attempts", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                    string.Empty, TextAlignmentOptions.MidlineLeft, 0f, attemptsTop, innerW, line);
                GameObject badgeGo = StatsInstance(last, "LBL_LevelBadge", "LBL_LevelBadge", 0f, attemptsTop + line + lineSmall, -1f, -1f);
                var levelBadge = badgeGo.GetComponent<LevelBadge>();
                levelBadge.SetSurface(LabelSurface.Light);
                levelBadge.SetLevel(DifficultyLevel.Guided);
                BaseBuilder.RecordInstanceOverrides(badgeGo);

                // Il dettaglio parte nascosto (parte dal riepilogo).
                mission.gameObject.SetActive(false);
                summary.gameObject.SetActive(true);

                // Componente della schermata.
                var stats = BaseBuilder.GetOrAdd<StatsScreen>(root);
                stats.Configure(theme, strings, rowPrefab, list, summary.gameObject, tiles, average.GetComponent<TMP_Text>(),
                    mission.gameObject, backGo.GetComponent<UIButton>(), missionTitle.GetComponent<TMP_Text>(), notPlayed.gameObject, best.gameObject,
                    bestScore.GetComponent<TMP_Text>(), bestStars, last.gameObject, outcome, bars, attempts.GetComponent<TMP_Text>(), levelBadge);
                StatsWireOnShown(screen, stats, stats.Refresh);
                EditorUtility.SetDirty(stats);
                Debug.Log("[SI81 UI] " + StatsSavePrefab(root, "PANEL_Stats", existed));
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[SI81 UI] PANEL_Stats: errore, prefab non salvato. " + exception);
                StatsDiscard(root, existed);
            }
        }

        // =====================================================================
        // Strumenti condivisi con ScreensBuilder.Profile.cs (prefisso Stats…)
        // =====================================================================

        /// <summary>
        /// Carica il contenuto del prefab di schermata esistente oppure ne crea la radice (RectTransform a tutto schermo, layer UI).
        /// Da chiudere con <see cref="StatsSavePrefab"/> (o <see cref="StatsDiscard"/> in caso di errore).
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice), es. PANEL_Stats.</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        /// <returns>La radice da modificare.</returns>
        private static GameObject StatsOpenPrefab(string prefabName, out bool existed)
        {
            BaseBuilder.EnsureFolder(STATS_SCREENS_FOLDER);
            string path = STATS_SCREENS_FOLDER + "/" + prefabName + ".prefab";
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            BaseBuilder.Stretch((RectTransform)root.transform);
            return root;
        }

        /// <summary>
        /// Salva la radice come prefab di schermata (stesso GUID se esisteva) e libera la memoria.
        /// </summary>
        /// <param name="root">Radice ottenuta da <see cref="StatsOpenPrefab"/>.</param>
        /// <param name="prefabName">Nome del prefab.</param>
        /// <param name="existed">Valore restituito da <see cref="StatsOpenPrefab"/>.</param>
        /// <returns>Riepilogo in italiano.</returns>
        private static string StatsSavePrefab(GameObject root, string prefabName, bool existed)
        {
            string path = STATS_SCREENS_FOLDER + "/" + prefabName + ".prefab";
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                return success ? $"{prefabName}: {(existed ? "aggiornato" : "creato")} ({path})." : $"{prefabName}: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                StatsDiscard(root, existed);
            }
        }

        /// <summary>
        /// Libera la radice senza salvare (prefab esistente: scarica il contenuto; nuovo: distrugge l'oggetto).
        /// </summary>
        /// <param name="root">Radice da liberare.</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        private static void StatsDiscard(GameObject root, bool existed)
        {
            if (root == null) return;
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Dimensioni dell'area contenuti di PANEL_Menu: larghezza pannello − 2 × padding; altezza pannello − 2 × padding − riga del titolo − space.m.
        /// </summary>
        /// <param name="theme">Tema.</param>
        /// <param name="panel">Pannello (per l'altezza).</param>
        /// <param name="width">Larghezza dei contenuti (712).</param>
        /// <param name="height">Altezza dei contenuti (375 con pannello 475).</param>
        private static void StatsContentSize(UITheme theme, MenuPanel panel, out float width, out float height)
        {
            float padding = theme.PaddingPanel;
            width = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * padding;
            height = panel.Height - 2f * padding - PanelLayout.LineHeight(theme.Title) - theme.GetNumber(NumberTokens.SPACE_M);
        }

        /// <summary>
        /// Crea o aggiorna un rettangolo figlio con l'angolo in alto a sinistra a (x, y) (y verso il basso), ancore in alto a sinistra,
        /// pivot al centro.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome del figlio.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        /// <returns>Il RectTransform.</returns>
        private static RectTransform StatsRect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            StatsPlace(rect, x, y, width, height);
            return rect;
        }

        /// <summary>
        /// Posiziona un RectTransform con l'angolo in alto a sinistra a (x, y); se larghezza e altezza sono negative lascia la
        /// dimensione al componente (pivot in alto a sinistra, per elementi che si dimensionano da soli).
        /// </summary>
        /// <param name="rect">Elemento.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza (negativa = lasciala al componente).</param>
        /// <param name="height">Altezza (negativa = lasciala al componente).</param>
        private static void StatsPlace(RectTransform rect, float x, float y, float width, float height)
        {
            if (width < 0f || height < 0f)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -y);
                rect.localScale = Vector3.one;
                return;
            }
            PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(x, -(y + height)), new Vector2(x + width, -y));
        }

        /// <summary>
        /// Crea o aggiorna un'etichetta TMP (stile e colore del tema, chiave opzionale) e la posiziona.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (prefisso LBL_).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        /// <param name="style">Stile di testo (TextStyleIds).</param>
        /// <param name="colorToken">Token colore (ColorTokens).</param>
        /// <param name="key">Chiave del testo (vuota = testo scritto a runtime).</param>
        /// <param name="alignment">Allineamento.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        /// <returns>Il componente UIStringText.</returns>
        private static UIStringText StatsText(Transform parent, string name, UITheme theme, UIStrings strings, string style, string colorToken,
            string key, TextAlignmentOptions alignment, float x, float y, float width, float height)
        {
            UIStringText label = ComponentsBuilder.AddLabel(parent, name, theme, strings, style, colorToken, key, alignment);
            StatsPlace((RectTransform)label.transform, x, y, width, height);
            return label;
        }

        /// <summary>
        /// Crea o recupera un'istanza annidata di un prefab di Prefabs/Components e la posiziona (vedi <see cref="StatsPlace"/>).
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="prefabName">Nome del prefab, es. LBL_StarRating.</param>
        /// <param name="instanceName">Nome dell'istanza in scena.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza (negativa = lasciala al componente).</param>
        /// <param name="height">Altezza (negativa = lasciala al componente).</param>
        /// <returns>L'istanza.</returns>
        private static GameObject StatsInstance(Transform parent, string prefabName, string instanceName, float x, float y, float width, float height)
        {
            GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(parent, ComponentsBuilder.PrefabPath(prefabName), instanceName);
            StatsPlace((RectTransform)instance.transform, x, y, width, height);
            BaseBuilder.RecordInstanceOverrides(instance);
            return instance;
        }

        /// <summary>
        /// Scheda bianca (surface.card, radius.card, ombra card come TEX_Card*): IMG_Shadow + IMG_Background, entrambe senza raycast.
        /// I contenuti vanno aggiunti come figli del rettangolo restituito.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (prefisso PANEL_).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        /// <returns>Il rettangolo della scheda.</returns>
        private static RectTransform StatsAddCard(Transform parent, string name, UITheme theme, float x, float y, float width, float height)
        {
            RectTransform card = StatsRect(parent, name, x, y, width, height);
            ComponentsBuilder.ShapeSprites shape = ComponentsBuilder.EnsureShapeSprites(theme, "Card", theme.RadiusCard);
            UITheme.ShadowToken token = theme.GetShadow(ShadowRoles.CARD);
            Image shadow = ComponentsBuilder.AddImage(card, "IMG_Shadow", shape.ShadowRest, token != null ? token.Color : Color.clear, false);
            shadow.fillCenter = false;
            float expand = PanelSprites.GetShadowLayout(theme.RadiusCard, token).PaddingUnits;
            PanelLayout.SetRect(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            shadow.transform.SetSiblingIndex(0);
            Image background = ComponentsBuilder.AddImage(card, "IMG_Background", shape.Rounded, theme.SurfaceCard, false);
            PanelLayout.SetRect(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            background.transform.SetSiblingIndex(1);
            return card;
        }

        /// <summary>
        /// Prepara un'istanza di BTN_Back per l'uso DENTRO una scheda bianca (torna al riepilogo, non alla schermata precedente):
        /// se BackButton espone il campo <c>inCardLook</c> lo attiva (fondo tile.neutral, Figma 103:591). Senza il campo il pulsante
        /// funziona lo stesso (le schermate della barra sono radici: Back() non fa nulla) ma è bianco su bianco: vedi NOTES_B3.md.
        /// </summary>
        /// <param name="backInstance">Istanza di BTN_Back.</param>
        private static void StatsMarkInCardBack(GameObject backInstance)
        {
            var back = backInstance.GetComponent<BackButton>();
            if (back == null) return;
            var serialized = new SerializedObject(back);
            SerializedProperty property = serialized.FindProperty("inCardLook");
            if (property == null) return;
            property.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Collega (una sola volta) un metodo pubblico senza parametri a <see cref="UIScreen.OnShown"/> come listener persistente.
        /// </summary>
        /// <param name="screen">UIScreen della radice.</param>
        /// <param name="target">Componente che riceve la chiamata.</param>
        /// <param name="action">Metodo da chiamare (es. <c>StatsScreen.Refresh</c>).</param>
        private static void StatsWireOnShown(UIScreen screen, Object target, UnityAction action)
        {
            UnityEvent shown = screen.OnShown;
            for (int i = 0; i < shown.GetPersistentEventCount(); i++)
                if (shown.GetPersistentTarget(i) == target && shown.GetPersistentMethodName(i) == action.Method.Name) return;
            UnityEventTools.AddPersistentListener(shown, action);
            EditorUtility.SetDirty(screen);
        }
    }
}
