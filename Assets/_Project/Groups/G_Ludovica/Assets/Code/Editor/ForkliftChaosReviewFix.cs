#if UNITY_EDITOR
using System.Collections.Generic;
using G_Ludovica.ForkliftChaos;
using SI81.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G_Ludovica.Editor
{
    /// <summary>
    /// Review esterna di SCN_ForkliftChaos (rieseguibile, idempotente):
    /// - tre corsie colorate a terra nella corsia esistente: 1 = passaggio (verde, M2), 2 = pallet instabili (giallo),
    ///   3 = Marco (rosso), con scritte a terra; barriera e coni chiudono le corsie 2 e 3, la 1 resta libera;
    /// - radio e barriere con il modello completo a dimensioni reali (prima: un pezzo del modello scalato x4-x7);
    /// - hitbox più grandi sui blocchi d'azione e sui tasti della radio;
    /// - colori per tipo di azione: blu = GRIP (prendi / porta e lascia), verde = GRILLETTO (interagisci), con etichette.
    /// La barriera si posa solo con grab-and-put (rilasciandola sulla zona blu); i coni con il grilletto.
    /// </summary>
    public static class ForkliftChaosReviewFix
    {
        private const string MATERIALS = "Assets/_Project/Groups/G_Ludovica/Assets/Art/Materials/";
        private const string MODELS = "Assets/_Project/Shared/Art/Models/";

        // Corsie (x del mondo) dentro la corsia esistente x 26,1-33,25, z 8,1-25,9.
        private const float L3_MIN = 26.1f, L3_MAX = 28.47f, L2_MAX = 30.93f, L1_MAX = 33.25f;
        private const float LANE_Z_MIN = 8.1f, LANE_Z_MAX = 25.9f;
        private static float L3 => (L3_MIN + L3_MAX) * 0.5f;
        private static float L2 => (L3_MAX + L2_MAX) * 0.5f;
        private static float L1 => (L2_MAX + L1_MAX) * 0.5f;

        private static readonly Color GRIP = new Color(0.15f, 0.5f, 1f);
        private static readonly Color TRIGGER = new Color(0.2f, 0.85f, 0.35f);

        private static Scene scene;

        [MenuItem("SI81/G_Ludovica/Forklift Chaos/Applica review (corsie, dimensioni, interazioni)")]
        public static void Run()
        {
            scene = EditorSceneManager.OpenScene(ForkliftChaosSceneFix.ScenePath, OpenSceneMode.Single);
            var gripMat = Mat("MAT_FKC_ActionGrip", GRIP, true);
            var triggerMat = Mat("MAT_FKC_ActionTrigger", TRIGGER, true);

            LayoutLanes();
            BuildLaneFloor();
            FixRadio(gripMat, triggerMat);
            FixBarrierStack();
            FixPlacedBarrierAndCones(gripMat, triggerMat);
            FixActionBlocks(triggerMat);
            FixMarcoCollider();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ForkliftChaosSceneFix.ScenePath)) Debug.LogError("[FKC-REVIEW] Salvataggio fallito.");
            AssetDatabase.SaveAssets();
            Debug.Log("[FKC-REVIEW] Review applicata a SCN_ForkliftChaos.");
        }

        // =====================================================================
        // Disposizione: Marco in corsia 3, pallet in corsia 2, passaggio (M2) in corsia 1
        // =====================================================================

        private static void LayoutLanes()
        {
            const float marcoX = 27.35f;
            // Marco e i blocchi intorno a lui: spostati in corsia 3 (restano dentro le pareti).
            SetX("MDL_Marco_MC01", marcoX);
            foreach (var (name, origX) in new[] { ("ACT_TalkToMarco", 28.6f), ("ACT_CheckBreathing", 28.9f), ("ACT_PressWound", 30.6f),
                         ("ACT_CoverMarco", 32f), ("ACT_MoveMarco", 27f), ("ACT_StartCPR", 27.2f), ("ACT_UseAED", 32f) })
                SetX(name, Mathf.Clamp(origX - 2.15f, 26.45f, 29.85f));
            // Rack e pallet sopra Marco (PA01), zona di caduta e pavimento scivoloso con lui.
            SetX("MDL_Rack_PA01", marcoX + 0.64f);
            SetX("MDL_Pallet_PA01", marcoX + 0.64f);
            SetX("ZONE_LZB_CadutaPA01", marcoX + 2.0f);
            SetX("SL01_PavimentoScivoloso", 28.25f);
            // Muletto 1 (quello dell'incidente) in corsia 3, davanti a Marco.
            SetX("MDL_Muletto1_VH01", marcoX);

            // Corsia 2: pallet instabili e scaffale (angolo cieco).
            SetXZ("MDL_Scaffale_ZN03", L2, 17f);
            SetXZ("MDL_Pallet_PA02", L2, 13f);
            SetXZ("ZONE_LZC_CadutaPA02", L2, 12f);
            AddDecorPallet();

            // Corsia 1: passaggio libero, dove arriva il Muletto 2.
            SetX("WP_M2_2", L1); SetX("WP_M2_3", L1); SetX("WP_M2_4", L1); SetX("WP_M2_5", L1);
            SetX("GRP_M2_Warning", L1);

            // Rack con radio fuori dalle corsie, nel corridoio contro la parete nord.
            SetXZ("PLACEHOLDER_Rack_RK01", 25.0f, 7.2f);
            SetXZ("MDL_Scaffale_RK01", 23.8f, 7.2f);
            SetXZ("MDL_Radio_RK01", 23.8f, 7.2f);
        }

        /// <summary>Un secondo pallet storto in corsia 2 (solo arredo): la corsia "dei pallet instabili" si riconosce.</summary>
        private static void AddDecorPallet()
        {
            if (Find("DEC_Pallet_Instabile") != null) return;
            var src = Find("MDL_Pallet_PA02");
            if (src == null) return;
            var copy = Object.Instantiate(src);
            copy.name = "DEC_Pallet_Instabile";
            SceneManager.MoveGameObjectToScene(copy, scene);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            copy.transform.SetPositionAndRotation(new Vector3(L2, src.transform.position.y + 0.12f, 22.2f), Quaternion.Euler(6f, 14f, 0f));
        }

        // =====================================================================
        // Pavimento delle corsie
        // =====================================================================

        private static void BuildLaneFloor()
        {
            var old = Find("GRP_Corsie");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("GRP_Corsie");
            SceneManager.MoveGameObjectToScene(root, scene);
            float zc = (LANE_Z_MIN + LANE_Z_MAX) * 0.5f, zl = LANE_Z_MAX - LANE_Z_MIN;
            Plate(root.transform, "FLR_Corsia3_Marco", new Vector3(L3, 0.03f, zc), new Vector3(L3_MAX - L3_MIN, 0.004f, zl), Mat("MAT_FKC_Lane3", new Color(0.62f, 0.16f, 0.13f), false));
            Plate(root.transform, "FLR_Corsia2_Pallet", new Vector3(L2, 0.03f, zc), new Vector3(L2_MAX - L3_MAX, 0.004f, zl), Mat("MAT_FKC_Lane2", new Color(0.8f, 0.58f, 0.1f), false));
            Plate(root.transform, "FLR_Corsia1_Passaggio", new Vector3(L1, 0.03f, zc), new Vector3(L1_MAX - L2_MAX, 0.004f, zl), Mat("MAT_FKC_Lane1", new Color(0.17f, 0.5f, 0.24f), false));
            var white = Mat("MAT_FKC_LaneLine", new Color(0.95f, 0.95f, 0.95f), false);
            Plate(root.transform, "FLR_Linea_2_3", new Vector3(L3_MAX, 0.034f, zc), new Vector3(0.08f, 0.004f, zl), white);
            Plate(root.transform, "FLR_Linea_1_2", new Vector3(L2_MAX, 0.034f, zc), new Vector3(0.08f, 0.004f, zl), white);
            FloorText(root.transform, "TXT_Corsia3", "CORSIA 3\nMARCO", new Vector3(L3, 0.04f, 11.2f));
            FloorText(root.transform, "TXT_Corsia2", "CORSIA 2\nPALLET INSTABILI", new Vector3(L2, 0.04f, 11.2f));
            FloorText(root.transform, "TXT_Corsia1", "CORSIA 1\nPASSAGGIO", new Vector3(L1, 0.04f, 11.2f));
        }

        private static void Plate(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void FloorText(Transform parent, string name, string text, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // Leggibile da chi entra dal corridoio (sud) guardando le corsie (nord).
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(2.3f, 1.2f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.8f;
            tmp.fontSizeMax = 2.4f;
        }

        // =====================================================================
        // Radio: modello completo a misura reale, tasti grandi davanti
        // =====================================================================

        private static void FixRadio(Material gripMat, Material triggerMat)
        {
            var radio = Find("MDL_Radio_RK01");
            if (radio == null) return;
            var t = radio.transform;
            t.SetPositionAndRotation(new Vector3(t.position.x, 1.09f, t.position.z), Quaternion.identity);
            t.localScale = Vector3.one;
            var mr = radio.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var vis = ModelChild(t, "VIS_Radio", MODELS + "MESH_Magazzino_Radio.fbx", 0.385f, 180f);
            var body = Bounds(vis);
            var col = radio.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = t.InverseTransformPoint(body.center);
                col.size = new Vector3(Mathf.Max(body.size.x, 0.12f), body.size.y, Mathf.Max(body.size.z, 0.08f));
            }
            float frontZ = t.InverseTransformPoint(body.center).z - Mathf.Max(body.size.z, 0.08f) * 0.5f;
            Button(t, "BTN_Radio_112", "LBL_112", "112", new Vector3(0f, 0.095f, frontZ - 0.01f), triggerMat);
            Button(t, "BTN_Radio_CAPO", "LBL_CAPO", "CAPO", new Vector3(0f, 0.04f, frontZ - 0.01f), triggerMat);
            Label(t, "LBL_Grip", "GRIP: prendi la radio", new Vector3(0f, 0.36f, 0f), GRIP, 0f);
            SetFciLabel(radio, ForkliftChaosAction.PickupRadio, "GRIP: prendi la radio");
        }

        private static void Button(Transform radio, string btnName, string lblName, string text, Vector3 localPos, Material mat)
        {
            var btn = radio.Find(btnName);
            if (btn == null) return;
            btn.localPosition = localPos;
            btn.localRotation = Quaternion.identity;
            btn.localScale = new Vector3(0.045f, 0.03f, 0.015f);
            var c = btn.GetComponent<BoxCollider>();
            // Tasto visibile 4,5 x 3 cm, hitbox ~10 x 4,8 x 8 cm (i due tasti non si sovrappongono) sporgente davanti alla radio (il raggio prende il tasto prima del corpo).
            if (c != null) { c.center = new Vector3(0f, 0f, -1.5f); c.size = new Vector3(0.1f / 0.045f, 0.048f / 0.03f, 0.08f / 0.015f); }
            var r = btn.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = mat;
            // L'etichetta originale è figlia del tasto: la si porta sotto la radio, accanto al tasto.
            var lbl = btn.Find(lblName) ?? radio.Find(lblName);
            if (lbl != null)
            {
                lbl.SetParent(radio, false);
                lbl.localPosition = localPos + new Vector3(0.17f, 0f, -0.01f);
                lbl.localRotation = Quaternion.identity;
                lbl.localScale = Vector3.one * 0.06f;
                SetText(lbl, "GRILLETTO: " + text, TRIGGER);
            }
            var fci = btn.GetComponent<ForkliftChaosInteractable>();
            if (fci != null) SetFciLabelOn(fci, "GRILLETTO: " + text);
        }

        // =====================================================================
        // Barriere: modello completo a misura reale (~1 m), grab-and-put
        // =====================================================================

        private static void FixBarrierStack()
        {
            var stack = Find("MDL_Barriera_Stack_RK01");
            if (stack == null) return;
            var t = stack.transform;
            t.SetPositionAndRotation(new Vector3(t.position.x, 0f, t.position.z), Quaternion.Euler(0f, 90f, 0f));
            t.localScale = Vector3.one;
            var mr = stack.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var vis = ModelChild(t, "VIS_Barrier", MODELS + "MESH_Magazzino_Barriera.fbx", 1.3f, 0f);
            var b = Bounds(vis);
            var col = stack.GetComponent<BoxCollider>();
            if (col != null) { col.center = t.InverseTransformPoint(b.center); col.size = new Vector3(1.0f, b.size.y, 0.45f); }
            Label(t, "LBL_Grip", "GRIP: prendi la barriera", new Vector3(0f, 1.25f, 0f), GRIP, 180f);
            SetFciLabel(stack, ForkliftChaosAction.PickupBarrier, "GRIP: prendi la barriera");
        }

        private static void FixPlacedBarrierAndCones(Material gripMat, Material triggerMat)
        {
            const float lineZ = 9.3f;
            float lineX = (L3_MIN + L2_MAX) * 0.5f, lineW = L2_MAX - L3_MIN;

            // Linea a terra BL-01 solo davanti alle corsie 2 e 3.
            var line = Find("BL01_LineaBarriera");
            if (line != null) { line.transform.position = new Vector3(lineX, 0.05f, lineZ); line.transform.localScale = new Vector3(lineW, 0.1f, 0.1f); }

            // Barriera posata: 5 barriere vere in fila, da parete a corsia 1.
            var placed = Find("MDL_Barriera_BL01");
            if (placed != null)
            {
                var t = placed.transform;
                t.SetPositionAndRotation(new Vector3(lineX, 0f, lineZ), Quaternion.identity);
                t.localScale = Vector3.one;
                var mr = placed.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                for (int i = t.childCount - 1; i >= 0; i--) if (t.GetChild(i).name.StartsWith("VIS_Barrier")) Object.DestroyImmediate(t.GetChild(i).gameObject);
                const int count = 5;
                float step = lineW / count;
                for (int i = 0; i < count; i++)
                {
                    var v = ModelChild(t, "VIS_Barrier_" + (i + 1), MODELS + "MESH_Magazzino_Barriera.fbx", step / 0.75f, 0f, false);
                    v.transform.localPosition += new Vector3(-lineW * 0.5f + step * (i + 0.5f), 0f, 0f);
                }
                var col = placed.GetComponent<BoxCollider>();
                if (col != null) { col.center = new Vector3(0f, 0.45f, 0f); col.size = new Vector3(lineW, 0.9f, 0.4f); }
            }

            // Coni davanti alla barriera, sulle corsie 2 e 3.
            var cones = Find("GRP_Coni_BL01");
            if (cones != null)
            {
                for (int i = 0; i < cones.transform.childCount; i++)
                {
                    var c = cones.transform.GetChild(i);
                    float x = L3_MIN + 0.6f + i * ((lineW - 1.2f) / Mathf.Max(1, cones.transform.childCount - 1));
                    c.position = new Vector3(x, c.position.y, lineZ - 0.6f);
                }
            }

            // Zona di posa della barriera (grab-and-put): piastra blu a terra, niente grilletto.
            var drop = Find("ACT_PlaceBarrier");
            if (drop != null)
            {
                var t = drop.transform;
                foreach (var fci in drop.GetComponents<ForkliftChaosInteractable>()) Object.DestroyImmediate(fci);
                var inter = drop.GetComponent<Interactable>();
                if (inter != null) Object.DestroyImmediate(inter);
                var col = drop.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
                var mr = drop.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                t.SetPositionAndRotation(new Vector3(lineX, 0.035f, lineZ), Quaternion.identity);
                t.localScale = Vector3.one;
                var plate = t.Find("VIS_DropZone");
                if (plate == null)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.name = "VIS_DropZone";
                    Object.DestroyImmediate(p.GetComponent<Collider>());
                    p.transform.SetParent(t, false);
                    plate = p.transform;
                }
                plate.localPosition = Vector3.zero;
                plate.localScale = new Vector3(lineW, 0.006f, 0.6f);
                plate.GetComponent<MeshRenderer>().sharedMaterial = gripMat;
                foreach (Transform child in t)
                {
                    if (!IsText(child)) continue;
                    child.localPosition = new Vector3(0f, 1.15f, 0f);
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;
                    SetText(child, "GRIP: porta qui la barriera e lasciala", GRIP);
                }
            }

            // Coni: interazione col grilletto, blocco verde sull'ingresso della corsia 2.
            var conesAct = Find("ACT_PlaceCones");
            if (conesAct != null)
            {
                conesAct.transform.position = new Vector3(L2 + 0.4f, 0.35f, lineZ - 1.2f);
                ResizeBlock(conesAct, triggerMat, "GRILLETTO: disponi i coni");
            }
        }

        // =====================================================================
        // Blocchi d'azione: hitbox più grandi, verde = grilletto
        // =====================================================================

        private static void FixActionBlocks(Material triggerMat)
        {
            var texts = new Dictionary<string, string>
            {
                { "ACT_TalkToMarco", "GRILLETTO: VALUTA Marco" },
                { "ACT_CheckBreathing", "GRILLETTO: controlla il respiro" },
                { "ACT_PressWound", "GRILLETTO: TRATTA la ferita" },
                { "ACT_CoverMarco", "GRILLETTO: copri Marco" },
                { "ACT_MoveMarco", "GRILLETTO: sposta Marco (emergenza)" },
                { "ACT_StartCPR", "GRILLETTO: RIANIMA (massaggio e DAE)" },
                { "ACT_UseAED", "GRILLETTO: DAE" }
            };
            foreach (var kv in texts)
            {
                var go = Find(kv.Key);
                if (go != null) ResizeBlock(go, triggerMat, kv.Value);
            }
        }

        /// <summary>Cubo di 28 cm con hitbox di ~45 cm, materiale del tipo d'azione, etichetta a ~40 cm sopra.</summary>
        private static void ResizeBlock(GameObject go, Material mat, string text)
        {
            const float size = 0.28f;
            var t = go.transform;
            float oldScale = t.localScale.x;
            var labels = new List<(Transform tr, float worldY, float worldScale)>();
            foreach (Transform child in t)
                if (IsText(child)) labels.Add((child, child.localPosition.y * oldScale, child.localScale.x * oldScale));
            t.localScale = Vector3.one * size;
            var col = go.GetComponent<BoxCollider>();
            if (col != null) { col.center = Vector3.zero; col.size = Vector3.one * (0.45f / size); }
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = mat;
            if (labels.Count == 0)
            {
                Label(t, "LBL_Action", text, new Vector3(0f, 0.4f / size, 0f), TRIGGER, 0f, 1f / size);
            }
            else
            {
                foreach (var (tr, worldY, worldScale) in labels)
                {
                    tr.localPosition = new Vector3(0f, Mathf.Max(worldY, 0.4f) / size, 0f);
                    tr.localScale = Vector3.one * (worldScale / size);
                    SetText(tr, text, TRIGGER);
                }
            }
            var fci = go.GetComponent<ForkliftChaosInteractable>();
            if (fci != null) SetFciLabelOn(fci, text);
        }

        /// <summary>Marco: hitbox dell'osservazione sul corpo intero (prima 30 cm), bassa per non coprire i blocchi d'azione.</summary>
        private static void FixMarcoCollider()
        {
            var marco = Find("MDL_Marco_MC01");
            if (marco == null) return;
            var col = marco.GetComponent<BoxCollider>();
            if (col == null) return;
            var rs = marco.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var t = marco.transform;
            Vector3 c = t.InverseTransformPoint(new Vector3(b.center.x, 0.18f, b.center.z));
            Vector3 s = t.InverseTransformVector(new Vector3(b.size.x * 0.85f, 0.36f, b.size.z * 0.9f));
            col.center = c;
            col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        // =====================================================================
        // Seconda review: tetto, etichette piccole (UI comune), secondo pallet, colleghi che camminano.
        // Non tocca la disposizione delle corsie (le modifiche fatte a mano restano).
        // =====================================================================

        [MenuItem("SI81/G_Ludovica/Forklift Chaos/Applica tetto, etichette e colleghi")]
        public static void RunRoofLabelsColleagues()
        {
            scene = EditorSceneManager.OpenScene(ForkliftChaosSceneFix.ScenePath, OpenSceneMode.Single);
            BuildRoof();
            ReplaceBigLabels();
            BindSecondPallet();
            BindBlindCorner();
            BuildColleagues();
            RouteForklift2();
            RadioButtonTexts();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ForkliftChaosSceneFix.ScenePath)) Debug.LogError("[FKC-REVIEW] Salvataggio fallito.");
            AssetDatabase.SaveAssets();
            Debug.Log("[FKC-REVIEW] Tetto, etichette e colleghi aggiornati.");
        }

        /// <summary>Tetto sopra corridoio e corsie, appoggiato sulle pareti (3 m); niente ombre, cosi' la luce entra lo stesso.</summary>
        private static void BuildRoof()
        {
            var old = Find("GRP_Tetto");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("GRP_Tetto");
            SceneManager.MoveGameObjectToScene(root, scene);
            var mat = Mat("MAT_FKC_Roof", new Color(0.55f, 0.56f, 0.58f), false);
            RoofSlab(root.transform, "ROOF_Corridoio", new Vector3(31f, 3.05f, 5f), new Vector3(26.2f, 0.1f, 6.2f), mat);
            RoofSlab(root.transform, "ROOF_Corsie", new Vector3(29.675f, 3.05f, 17f), new Vector3(7.55f, 0.1f, 18.2f), mat);
            // Le plafoniere stavano a 3,6 m, sopra le pareti: ora appese sotto il tetto.
            foreach (var n in new[] { "MDL_LuceSoffitto_1", "MDL_LuceSoffitto_2", "MDL_LuceSoffitto_3" })
            {
                var l = Find(n);
                if (l != null) { var pos = l.transform.position; l.transform.position = new Vector3(pos.x, 2.9f, pos.z); }
            }
        }

        private static void RoofSlab(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Toglie le etichette 3D grandi (TextMesh originali e quelle aggiunte nella prima review) e mette le etichette piccole
        /// della UI comune (LabelAnchor): pillola del tasto + verbo, GRIP per prendere, TRIGGER per interagire.
        /// </summary>
        private static void ReplaceBigLabels()
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "GRP_Corsie") continue;
                var doomed = new List<GameObject>();
                foreach (var tm in root.GetComponentsInChildren<TextMesh>(true)) doomed.Add(tm.gameObject);
                foreach (var tp in root.GetComponentsInChildren<TMP_Text>(true))
                    if (tp.name == "LBL_Grip" || tp.name == "LBL_Action") doomed.Add(tp.gameObject);
                foreach (var g in doomed) if (g != null) Object.DestroyImmediate(g);
            }

            Anchor("MDL_Radio_RK01", SI81.UI.LabelKind.Interaction, "Radio", "", false, 0.35f);
            Anchor("MDL_Barriera_Stack_RK01", SI81.UI.LabelKind.Interaction, "Barriera", "", false, 1.15f);
            Anchor("BTN_Radio_112", SI81.UI.LabelKind.Interaction, "Chiama il 112", "", true, 0.12f);
            Anchor("BTN_Radio_CAPO", SI81.UI.LabelKind.Interaction, "Chiama il capo turno", "", true, 0.06f);
            Anchor("ACT_PlaceCones", SI81.UI.LabelKind.Interaction, "Disponi i coni", "", true, 0.35f);
            Anchor("ACT_TalkToMarco", SI81.UI.LabelKind.Interaction, "Valuta Marco", "", true, 0.35f);
            Anchor("ACT_CheckBreathing", SI81.UI.LabelKind.Interaction, "Controlla il respiro", "", true, 0.35f);
            Anchor("ACT_PressWound", SI81.UI.LabelKind.Interaction, "Tratta la ferita", "", true, 0.35f);
            Anchor("ACT_CoverMarco", SI81.UI.LabelKind.Interaction, "Copri Marco", "", true, 0.35f);
            Anchor("ACT_MoveMarco", SI81.UI.LabelKind.Interaction, "Sposta Marco (emergenza)", "", true, 0.35f);
            Anchor("ACT_StartCPR", SI81.UI.LabelKind.Interaction, "Rianima (massaggio e DAE)", "", true, 0.35f);
            Anchor("ACT_UseAED", SI81.UI.LabelKind.Interaction, "DAE", "", true, 0.35f);
            Anchor("ACT_PlaceBarrier", SI81.UI.LabelKind.ContextHelp, "Zona barriera", "GRIP: porta qui la barriera e lasciala", false, 0.6f);
        }

        /// <summary>LabelAnchor della UI comune con offset in metri sopra l'oggetto (compensa la scala della radice).</summary>
        private static void Anchor(string name, SI81.UI.LabelKind kind, string title, string detail, bool trigger, float heightMeters)
        {
            var go = Find(name);
            if (go == null) { Debug.LogWarning("[FKC-REVIEW] Non trovato: " + name); return; }
            var a = go.GetComponent<SI81.UI.LabelAnchor>() ?? go.AddComponent<SI81.UI.LabelAnchor>();
            var so = new SerializedObject(a);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("detail").stringValue = detail;
            so.FindProperty("buttonKey").stringValue = trigger ? "hud.prompt.button.trigger" : "";
            so.FindProperty("actionKey").stringValue = trigger ? "hud.prompt.action.interact" : "";
            Vector3 local = go.transform.InverseTransformVector(Vector3.up * heightMeters);
            so.FindProperty("offset").vector3Value = local;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Il pallet aggiunto sul secondo rack (corsia 2) diventa il "secondo pallet": cade se resti nella sua zona.</summary>
        private static void BindSecondPallet()
        {
            var pallet = Find("MDL_Pallet_PA01 (1)");
            var zone = Find("ZONE_LZC_CadutaPA02");
            var sc = Object.FindFirstObjectByType<ForkliftChaosScenario>();
            if (pallet == null || sc == null) { Debug.LogWarning("[FKC-REVIEW] Secondo pallet o scenario non trovati."); return; }
            var so = new SerializedObject(sc);
            so.FindProperty("pallet2").objectReferenceValue = pallet;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Zona di caduta davanti al pallet, in corsia 2 verso l'ingresso.
            if (zone != null) zone.transform.position = new Vector3(pallet.transform.position.x, zone.transform.position.y, pallet.transform.position.z - 0.7f);
        }

        /// <summary>
        /// Muletto 2: arriva dal corridoio e punta verso le corsie 2-3 (chiuse da barriera e coni); con la barriera posata si
        /// ferma con il muso ~40 cm prima dei coni, cosi' la corsia 1 resta libera per passare.
        /// </summary>
        private static void RouteForklift2()
        {
            var m2 = Find("MDL_Muletto2_MS01");
            var sc = Object.FindFirstObjectByType<ForkliftChaosScenario>();
            if (m2 == null || sc == null) return;
            // Muso del muletto rispetto al suo pivot, misurato con il mezzo rivolto verso +z (verso le corsie).
            var t = m2.transform;
            Quaternion keep = t.rotation;
            t.rotation = Quaternion.LookRotation(Vector3.forward);
            var b = Bounds(m2);
            float front = b.max.z - t.position.z;
            t.rotation = keep;

            var cones = Find("GRP_Coni_BL01");
            float conesZ = 8.7f;
            if (cones != null && cones.transform.childCount > 0) conesZ = cones.transform.GetChild(0).position.z;
            float laneX = (L3_MAX + L2_MAX) * 0.5f;                 // centro della corsia 2
            float stopZ = conesZ - 0.4f - 0.35f - front;            // 0,35 = mezza base del cono
            float turnZ = Mathf.Min(stopZ - 0.6f, 4.4f);            // svolta nel corridoio prima di puntare verso nord

            void Put(string n, float x, float z)
            {
                var g = Find(n);
                if (g == null) { g = new GameObject(n); SceneManager.MoveGameObjectToScene(g, scene); }
                g.transform.position = new Vector3(x, 0f, z);
            }
            Put("WP_M2_2", laneX, turnZ);
            Put("WP_M2_3", laneX, stopZ);    // punto di arresto con la barriera posata (path[2])
            Put("WP_M2_4", laneX, 13f);      // senza barriera entra in corsia: investimento
            var warn = Find("GRP_M2_Warning");
            if (warn != null) warn.transform.position = new Vector3(laneX, warn.transform.position.y, stopZ + front * 0.5f);

            var so = new SerializedObject(sc);
            var path = so.FindProperty("m2Path");
            var names = new[] { "WP_M2_1", "WP_M2_2", "WP_M2_3", "WP_M2_4" };
            path.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++) path.GetArrayElementAtIndex(i).objectReferenceValue = Find(names[i]).transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[FKC-REVIEW] Muletto 2: muso {front:F2} m, arresto a z {stopZ:F2} (coni a z {conesZ:F2}), corsia 2 x {laneX:F2}.");
        }

        /// <summary>Scritte piccole accanto ai tasti della radio: quale tasto chiama chi.</summary>
        private static void RadioButtonTexts()
        {
            var radio = Find("MDL_Radio_RK01");
            if (radio == null) return;
            ButtonText(radio.transform, "BTN_Radio_112", "TXT_Btn112", "112 Soccorsi");
            ButtonText(radio.transform, "BTN_Radio_CAPO", "TXT_BtnCapo", "Capo turno");
        }

        private static void ButtonText(Transform radio, string btnName, string txtName, string text)
        {
            var btn = radio.Find(btnName);
            if (btn == null) return;
            var old = radio.Find(txtName);
            var go = old != null ? old.gameObject : new GameObject(txtName);
            go.transform.SetParent(radio, false);
            // A destra del tasto, sulla stessa faccia (testo leggibile da chi guarda la radio).
            go.transform.localPosition = btn.localPosition + new Vector3(0.075f, 0f, -0.005f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var tmp = go.GetComponent<TextMeshPro>() ?? go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 0.18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = Color.white;
            tmp.rectTransform.pivot = new Vector2(0f, 0.5f);
            tmp.rectTransform.sizeDelta = new Vector2(0.12f, 0.03f);
        }

        /// <summary>
        /// Angolo cieco: prima era lo scaffale in corsia 2 (rimosso). Ora e' l'angolo tra corridoio e corsia 1, da dove arriva il
        /// muletto 2: il giocatore lo "controlla" avvicinandosi entro 3 m nei primi 30 s.
        /// </summary>
        private static void BindBlindCorner()
        {
            var sc = Object.FindFirstObjectByType<ForkliftChaosScenario>();
            if (sc == null) return;
            var old = Find("PT_AngoloCieco");
            if (old != null) Object.DestroyImmediate(old);
            var pt = new GameObject("PT_AngoloCieco");
            SceneManager.MoveGameObjectToScene(pt, scene);
            pt.transform.position = new Vector3(L1_MAX - 0.6f, 0f, LANE_Z_MIN);
            var so = new SerializedObject(sc);
            so.FindProperty("blindCorner").objectReferenceValue = pt.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// I due colleghi: lavoratore animato (stesso modello e clip di Electric Shock) con NpcMover, che camminano dal corridoio
        /// verso la corsia; punti di sosta, corsia e uscita collegati allo scenario.
        /// </summary>
        private static void BuildColleagues()
        {
            var group = Find("GRP_Colleghi_CCS1");
            var sc = Object.FindFirstObjectByType<ForkliftChaosScenario>();
            if (group == null || sc == null) { Debug.LogWarning("[FKC-REVIEW] Colleghi o scenario non trovati."); return; }
            bool wasActive = group.activeSelf;
            group.SetActive(true);
            for (int i = group.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(group.transform.GetChild(i).gameObject);
            for (int i = 0; i < 2; i++)
            {
                var root = new GameObject("CCS" + (i + 1) + "_Collega").transform;
                root.SetParent(group.transform, false);
                root.SetPositionAndRotation(new Vector3(20.6f + i * 0.8f, 0f, 5f + (i == 0 ? -0.4f : 0.4f)), Quaternion.Euler(0f, 90f, 0f));
                var mover = root.gameObject.AddComponent<SI81.Ludovica.ElectricShock.NpcMover>();
                SI81.Ludovica.ElectricShock.EditorTools.ElectricShockSceneBuilder.AddWorkerCharacter(root, mover, "Idle", 0f);
            }
            group.SetActive(wasActive);

            var points = Find("GRP_PuntiColleghi");
            if (points != null) Object.DestroyImmediate(points);
            var pr = new GameObject("GRP_PuntiColleghi").transform;
            SceneManager.MoveGameObjectToScene(pr.gameObject, scene);
            Transform P(string n, float x, float z) { var t = new GameObject(n).transform; t.SetParent(pr, false); t.position = new Vector3(x, 0f, z); return t; }
            var stop = P("WP_CCS_Sosta", 24.4f, 5.2f);
            // Passano a sud del muletto 2 (fermo davanti alla corsia 2) e poi entrano in corsia 1.
            var lane = new[] { P("WP_CCS_Corridoio", L1 - 0.6f, 3.4f), P("WP_CCS_IngressoCorsia1", L1, 7.4f), P("WP_CCS_DentroCorsia1", L1, 12.5f) };
            var exit = P("WP_CCS_Uscita", 19.0f, 5.0f);
            var so = new SerializedObject(sc);
            so.FindProperty("colleagueStopPoint").objectReferenceValue = stop;
            var lp = so.FindProperty("colleagueLanePath");
            lp.arraySize = lane.Length;
            for (int i = 0; i < lane.Length; i++) lp.GetArrayElementAtIndex(i).objectReferenceValue = lane[i];
            so.FindProperty("colleagueExitPoint").objectReferenceValue = exit;
            so.FindProperty("colleagueVisual").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        private static GameObject Find(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t.gameObject;
            }
            return null;
        }

        private static void SetX(string name, float x)
        {
            var go = Find(name);
            if (go != null) { var p = go.transform.position; go.transform.position = new Vector3(x, p.y, p.z); }
            else Debug.LogWarning("[FKC-REVIEW] Non trovato: " + name);
        }

        private static void SetXZ(string name, float x, float z)
        {
            var go = Find(name);
            if (go != null) { var p = go.transform.position; go.transform.position = new Vector3(x, p.y, z); }
            else Debug.LogWarning("[FKC-REVIEW] Non trovato: " + name);
        }

        /// <summary>Istanza del modello come figlio (sostituisce quella precedente con lo stesso nome), base a terra sulla radice.</summary>
        private static GameObject ModelChild(Transform parent, string name, string fbx, float scale, float yaw, bool replace = true)
        {
            if (replace)
            {
                var old = parent.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name;
            go.transform.SetParent(parent, false);
            // Anche la rotazione di importazione della radice (es. radio -90 su X) va mantenuta.
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
            // La radice dell'FBX ha gia' una scala di importazione (es. radio x16): si moltiplica, non si sostituisce.
            go.transform.localScale = prefab.transform.localScale * scale;
            go.transform.localPosition = Vector3.zero;
            var b = Bounds(go);
            Vector3 offset = new Vector3(parent.position.x - b.center.x, parent.position.y - b.min.y, parent.position.z - b.center.z);
            go.transform.position += offset;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return go;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Label(Transform parent, string name, string text, Vector3 localPos, Color color, float yaw, float localScale = 1f)
        {
            var old = parent.Find(name);
            var go = old != null ? old.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * localScale;
            var tmp = go.GetComponent<TextMeshPro>() ?? go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 0.6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
        }

        /// <summary>true se il trasform ha un testo (TextMeshPro o TextMesh legacy, come le etichette originali della scena).</summary>
        private static bool IsText(Transform t) => t.GetComponent<TMP_Text>() != null || t.GetComponent<TextMesh>() != null;

        /// <summary>Scrive testo e colore su TextMeshPro o TextMesh legacy.</summary>
        private static void SetText(Transform t, string text, Color color)
        {
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.text = text; tmp.color = color; return; }
            var tm = t.GetComponent<TextMesh>();
            if (tm != null) { tm.text = text; tm.color = color; tm.anchor = TextAnchor.MiddleCenter; }
        }

        private static void SetFciLabel(GameObject go, ForkliftChaosAction action, string text)
        {
            foreach (var fci in go.GetComponents<ForkliftChaosInteractable>())
                if (fci.Action == action) SetFciLabelOn(fci, text);
        }

        private static void SetFciLabelOn(ForkliftChaosInteractable fci, string text)
        {
            var so = new SerializedObject(fci);
            so.FindProperty("label").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Mat(string name, Color color, bool unlit)
        {
            string path = MATERIALS + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.color = color;
            if (!unlit && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
