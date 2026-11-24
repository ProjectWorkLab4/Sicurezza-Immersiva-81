using System.Collections.Generic;
using System.Linq;
using SI81.Ludovica.TempCore;
using SI81.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SI81.Ludovica.ElectricShock.EditorTools
{
    /// <summary>
    /// Costruisce da codice il graybox dello scenario 10 (coordinate della specifica §4: griglia 1 m, origine in basso a sinistra,
    /// x della mappa = X di Unity, y della mappa = Z di Unity) e la missione SO_Mission_ElectricShock per la UI comune.
    /// Tutti i modelli mancanti sono primitive con nome "PLACEHOLDER_*", da sostituire con gli FBX a fine lavoro.
    /// Rieseguibile: riscrive SCN_ElectricShock e aggiorna la missione (catalogo e Build Settings hanno un comando a parte).
    /// </summary>
    public static class ElectricShockSceneBuilder
    {
        private const string ROOT = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock";
        private const string MAT_DIR = ROOT + "/Art/Materials";
        private const string DATA_DIR = ROOT + "/Data";
        /// <summary>Percorso della scena dello scenario.</summary>
        public const string SCENARIO_SCENE = ROOT + "/SCN_ElectricShock.unity";
        /// <summary>Percorso della missione dello scenario.</summary>
        public const string MISSION_PATH = ROOT + "/SO_Mission_ElectricShock.asset";
        private const string SAMPLE_MISSION = "Assets/_Project/Home/UI/Data/Missions/SO_Mission_ElectricShock.asset";
        private const string CATALOG = "Assets/_Project/Home/UI/Data/Missions/SO_MissionCatalog.asset";
        private const string ICON = "Assets/_Project/Home/Art/Textures/Icons/ICN_ElectricBolt.png";

        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        /// <summary>Costruisce materiali, dati e le due scene.</summary>
        [MenuItem("SI81/G_Ludovica/Electric Shock/Costruisci scene (graybox)")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMaterials();
            LoadOrCreateSettings();
            LoadOrCreateMission();
            BuildScenarioScene();
            EditorSceneManager.OpenScene(SCENARIO_SCENE, OpenSceneMode.Single);
            Debug.Log("[SI81 ElectricShock] Scena costruita: " + SCENARIO_SCENE);
        }

        // =====================================================================
        // Materiali e dati
        // =====================================================================

        /// <summary>Crea (o riusa) i materiali segnaposto.</summary>
        private static void BuildMaterials()
        {
            mats.Clear();
            Lit("Floor", new Color(0.45f, 0.45f, 0.47f));
            Lit("Wall", new Color(0.78f, 0.78f, 0.76f));
            Lit("Generator", new Color(0.85f, 0.6f, 0.1f));
            Lit("StopButton", new Color(0.85f, 0.05f, 0.05f));
            Lit("StopButtonBase", new Color(0.95f, 0.85f, 0.1f));
            Lit("Cable", new Color(0.05f, 0.05f, 0.05f));
            Lit("FuelPool", new Color(0.12f, 0.08f, 0.05f), 0.95f);
            Lit("FuelTank", new Color(0.7f, 0.1f, 0.05f));
            Lit("Victim", new Color(0.15f, 0.3f, 0.7f));
            Lit("Bystander", new Color(0.95f, 0.45f, 0.1f));
            Lit("Paramedic", new Color(0.9f, 0.15f, 0.15f));
            Lit("Barrier", new Color(0.95f, 0.8f, 0.05f));
            Lit("Rack", new Color(0.35f, 0.35f, 0.38f));
            Lit("Radio", new Color(0.15f, 0.15f, 0.15f));
            Lit("Extinguisher", new Color(0.8f, 0.05f, 0.05f));
            Lit("LabelWater", new Color(0.1f, 0.35f, 0.95f));
            Lit("LabelPowder", new Color(0.95f, 0.95f, 0.95f));
            Lit("LabelCO2", new Color(0.05f, 0.05f, 0.05f));
            Lit("SafeZone", new Color(0.1f, 0.7f, 0.25f));
            Lit("Marker", new Color(1f, 0.85f, 0.1f));
            Lit("Hand", new Color(0.9f, 0.75f, 0.6f));
            Particle("Fire", new Color(1f, 0.55f, 0.15f), true);
            Particle("Smoke", new Color(0.25f, 0.25f, 0.25f, 0.6f), false);
            Particle("Spray", new Color(0.95f, 0.95f, 0.95f, 0.6f), false);
            Particle("Spark", new Color(0.7f, 0.85f, 1f), true);
            Particle("BlastRing", new Color(1f, 0.2f, 0.1f), true);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Materiale URP Lit segnaposto.</summary>
        private static void Lit(string name, Color color, float smoothness = 0.2f)
        {
            string path = $"{MAT_DIR}/MAT_Placeholder{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            // Emissione accesa ma nera: l'evidenziazione (Guided) cambia solo il colore per renderer, senza toccare l'asset.
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(m);
            mats[name] = m;
        }

        /// <summary>Materiale URP Particles Unlit trasparente (additivo o alpha).</summary>
        private static void Particle(string name, Color color, bool additive)
        {
            string path = $"{MAT_DIR}/MAT_VFX_Placeholder{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            mats[name] = m;
        }

        /// <summary>Carica o crea SO_ElectricShockSettings.</summary>
        private static ElectricShockSettings LoadOrCreateSettings()
        {
            string path = DATA_DIR + "/SO_ElectricShockSettings.asset";
            var s = AssetDatabase.LoadAssetAtPath<ElectricShockSettings>(path);
            if (s != null) return s;
            s = ScriptableObject.CreateInstance<ElectricShockSettings>();
            AssetDatabase.CreateAsset(s, path);
            AssetDatabase.SaveAssets();
            return s;
        }

        // =====================================================================
        // Scena dello scenario
        // =====================================================================

        /// <summary>Costruisce SCN_ElectricShock (niente camera, rig, Canvas o EventSystem: guida UI §6).</summary>
        private static void BuildScenarioScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene scarica gli asset non referenziati: ricarica materiali e dati dopo averla creata.
            BuildMaterials();
            var settings = LoadOrCreateSettings();
            var mission = LoadOrCreateMission();

            var light = new GameObject("MNG_Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            var env = new GameObject("ENV_Graybox").transform;
            BuildEnvironment(env);

            var spawn = new GameObject("MNG_SpawnPoint").transform;
            spawn.SetPositionAndRotation(P(8f, 8f), Quaternion.Euler(0f, 90f, 0f));

            var points = new GameObject("PTS_Points").transform;
            var safe = Marker("SAFE01_SafePoint", points, 14.5f, 4f, 3f, 3f, "SafeZone", true);
            var assembly = Marker("AP02_AssemblyPoint", points, 3f, 8f, 4f, 4f, "SafeZone", false);
            var victimApproach = Point("PT_VictimApproach", points, 16f, 6.7f);
            var exit = new[] { Point("PT_Exit_1", points, 14.5f, 6f), Point("PT_Exit_2", points, 13.8f, 8f), Point("PT_Exit_3", points, 12f, 8f), Point("PT_Exit_4", points, 6f, 8f), Point("PT_Exit_5", points, 3f, 8f) };
            var zoneEntry = new[] { Point("PT_ZoneEntry_1", points, 13.8f, 8f), Point("PT_ZoneEntry_2", points, 14.5f, 6f) };
            var pm2Path = new[] { Point("PT_PM2_Spawn", points, 1f, 8f), Point("PT_PM2_DR01", points, 6f, 8f), Point("PT_PM2_Gate", points, 12.3f, 8f) };

            // Generatore, pulsante, cavo
            var hazards = new GameObject("HZD_Hazards").transform;
            var gen = Root("GEN01_Generator", hazards, 20f, 9.5f);
            var genVisual = Box("PLACEHOLDER_Generator", gen, Vector3.up * 0.45f, new Vector3(1.8f, 0.9f, 1.0f), "Generator", true);
            var generator = gen.gameObject.AddComponent<GeneratorUnit>();
            SetField(generator, "vibratingPart", genVisual.transform);
            SetField(generator, "engineAudio", Audio3D("SFX_Engine", gen, Vector3.up * 0.5f, true, 0.5f));

            var pb = new GameObject("PB01_EmergencyStop").transform;
            pb.SetParent(gen, false);
            pb.localPosition = new Vector3(-0.1f, 0.9f, 0.4f);
            Cylinder("PLACEHOLDER_StopButtonBase", pb, Vector3.up * 0.015f, new Vector3(0.14f, 0.015f, 0.14f), "StopButtonBase", false);
            var cap = Cylinder("PLACEHOLDER_StopButtonCap", pb, Vector3.up * 0.05f, new Vector3(0.09f, 0.025f, 0.09f), "StopButton", false);
            var pbCollider = pb.gameObject.AddComponent<BoxCollider>();
            pbCollider.center = Vector3.up * 0.04f;
            pbCollider.size = new Vector3(0.18f, 0.1f, 0.18f);
            var stop = pb.gameObject.AddComponent<EmergencyStopButton>();
            SetField(stop, "cap", cap.transform);
            SetInteractable(stop, "Arresto di emergenza", cap.GetComponent<Renderer>());
            Label(pb.gameObject, LabelKind.Interaction, "Arresto di emergenza", "Grilletto: ferma il generatore", new Vector3(0f, 0.25f, 0f));

            var cableRoot = Root("CB01_LiveCable", hazards, 18.8f, 8.5f);
            var cablePoints = new[] { P(20f, 10f, 0.03f), P(18.8f, 8.5f, 0.03f), P(16.9f, 7.6f, 0.03f) };
            var lr = cableRoot.gameObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = cablePoints.Length;
            lr.SetPositions(cablePoints);
            lr.widthMultiplier = 0.035f;
            lr.sharedMaterial = mats["Cable"];
            for (int i = 0; i < cablePoints.Length - 1; i++) CableSegment(cableRoot, cablePoints[i], cablePoints[i + 1], i);
            var sparks = Sparks("VFX_CableSparks", cableRoot, cablePoints[2]);
            var cableFlame = FireParticles("VFX_CableFlame", sparks.transform, 0.15f, 0.25f, 20f);
            cableFlame.transform.position = cablePoints[1];
            var cable = cableRoot.gameObject.AddComponent<LiveCable>();
            SetField(cable, "sparks", sparks);
            SetField(cable, "crackleAudio", Audio3D("SFX_Crackle", sparks.transform, Vector3.zero, true, 0.6f));
            IgnoreForNavMesh(cableRoot.gameObject);
            SetInteractable(cable, "Cavo sotto tensione", null);
            AddObservable(cableRoot.gameObject, "cable");
            Label(cableRoot.gameObject, LabelKind.Risk, "Cavo sotto tensione", "Non toccarlo: prima togli la corrente", new Vector3(0f, 0.4f, 0f));

            // Pozza e fuoco (si muovono insieme tra le 3 posizioni di replay)
            var pool = Root("PZ01_FuelPool", hazards, 22f, 9f);
            Box("PLACEHOLDER_FuelPool", pool, Vector3.up * 0.006f, new Vector3(2.8f, 0.012f, 1.2f), "FuelPool", false);
            AddObservable(pool.gameObject, "pool");
            Label(pool.gameObject, LabelKind.Risk, "Pozza di benzina", "Una scintilla può incendiarla", new Vector3(0f, 0.3f, 0f));
            var poolPositions = new[] { Point("PT_Pool_A", points, 21.6f, 9.2f), Point("PT_Pool_B", points, 22f, 9f), Point("PT_Pool_C", points, 21.8f, 8.8f) };
            var fireRoot = new GameObject("FR01_Fire").transform;
            fireRoot.SetParent(pool, false);
            fireRoot.localPosition = new Vector3(0.2f, 0f, 0f);
            var flames = FireParticles("VFX_Fire", fireRoot, 0.8f, 1.2f, 80f);
            var smoke = SmokeParticles("VFX_Smoke", fireRoot, new Vector3(0f, 1.5f, 0f), 1.2f, 25f);
            var sprayTarget = fireRoot.gameObject.AddComponent<SphereCollider>();
            sprayTarget.isTrigger = true;
            sprayTarget.radius = 1.1f;
            sprayTarget.center = Vector3.up * 0.4f;
            var fireLight = new GameObject("LGT_Fire").AddComponent<Light>();
            fireLight.transform.SetParent(fireRoot, false);
            fireLight.transform.localPosition = Vector3.up * 1f;
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.2f);
            fireLight.range = 7f;
            var fire = fireRoot.gameObject.AddComponent<FireSource>();
            SetField(fire, "flames", flames);
            SetField(fire, "smoke", smoke);
            SetField(fire, "sprayTarget", sprayTarget);
            SetField(fire, "fireLight", fireLight);
            SetField(fire, "fireAudio", Audio3D("SFX_Fire", fireRoot, Vector3.up * 0.5f, true, 0.9f));
            var fireObstacle = fireRoot.gameObject.AddComponent<NavMeshObstacle>();
            fireObstacle.shape = NavMeshObstacleShape.Capsule;
            fireObstacle.radius = 1.2f;
            fireObstacle.height = 2f;
            fireObstacle.center = Vector3.up;
            fireObstacle.carving = true;
            fireObstacle.enabled = false;
            SetField(fire, "navObstacle", fireObstacle);
            IgnoreForNavMesh(fireRoot.gameObject);
            var crisisSmoke = SmokeParticles("VFX_CrisisSmoke", pool, new Vector3(0f, 0.4f, 0f), 2f, 15f);

            // Tanica e serbatoio (rischio nascosto)
            var tank = Root("FC01_FuelTank", hazards, 23.6f, 11.2f);
            var tankVisual = Box("PLACEHOLDER_FuelTank", tank, Vector3.up * 0.25f, new Vector3(0.6f, 0.5f, 0.6f), "FuelTank", true);
            var blast = BlastParticles("VFX_Blast", tank);
            var ring = new GameObject("VFX_BlastRing").AddComponent<LineRenderer>();
            ring.transform.SetParent(tank, false);
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.widthMultiplier = 0.08f;
            ring.sharedMaterial = mats["BlastRing"];
            const int seg = 64;
            ring.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                ring.SetPosition(i, P(22.2f + Mathf.Cos(a) * settings.BlastRadius, 9f + Mathf.Sin(a) * settings.BlastRadius, 0.05f));
            }
            var tankHazard = tank.gameObject.AddComponent<FuelTankHazard>();
            SetField(tankHazard, "swellingPart", tankVisual.transform);
            SetField(tankHazard, "blast", blast);
            SetField(tankHazard, "blastRing", ring);
            SetField(tankHazard, "hissAudio", Audio3D("SFX_Hiss", tank, Vector3.up * 0.5f, true, 0.7f));
            SetField(tankHazard, "blastAudio", Audio3D("SFX_Blast", tank, Vector3.up * 0.5f, false, 1f, 40f));

            // Zona di fumo ZN-11
            var air = Root("ZN11_HarmfulAir", hazards, 25f, 6.2f);
            var airBox = air.gameObject.AddComponent<BoxCollider>();
            airBox.isTrigger = true;
            airBox.center = Vector3.up * 1.5f;
            airBox.size = new Vector3(4f, 3f, 3f);
            var airSmoke = SmokeParticles("VFX_HarmfulAir", air, new Vector3(0f, 1.2f, 0f), 2f, 12f);
            var airMain = airSmoke.main;
            airMain.playOnAwake = true; // fumo sempre presente nella zona ZN-11

            // Attrezzature
            var tools = new GameObject("TLS_Tools").transform;
            Box("PLACEHOLDER_RackExtinguishers", tools, P(7f, 12.3f, 0.75f), new Vector3(1.6f, 1.5f, 0.1f), "Rack", true);
            var slotRoot = new GameObject("RK02_ExtinguisherSlots").transform;
            var rackHelp = new GameObject("LBL_ExtinguisherRack").transform;
            rackHelp.SetParent(tools, false);
            rackHelp.position = P(7f, 12f, 1.2f);
            Label(rackHelp.gameObject, LabelKind.ContextHelp, "Estintori", "Leggi l'etichetta prima di usarli", Vector3.zero);
            slotRoot.SetParent(tools, false);
            var slots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Point($"PT_ExtSlot_{i + 1}", slotRoot, 6.5f + i * 0.5f, 12f);
                slots[i].rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var extinguishers = new[]
            {
                MakeExtinguisher(tools, "EXT1_Water", ExtinguisherAgent.Water, "LabelWater", slots[0]),
                MakeExtinguisher(tools, "EXT2_Powder", ExtinguisherAgent.Powder, "LabelPowder", slots[1]),
                MakeExtinguisher(tools, "EXT3_CO2", ExtinguisherAgent.CO2, "LabelCO2", slots[2])
            };

            Box("PLACEHOLDER_RackBarriersRadio", tools, P(11.8f, 10.6f, 0.45f), new Vector3(0.9f, 0.9f, 1.2f), "Rack", true);
            var barrierA = MakeBarrier(tools, "BAR1_Barrier", P(12.45f, 11.4f));
            var barrierB = MakeBarrier(tools, "BAR2_Barrier", P(12.7f, 11.4f));
            var radio = MakeRadio(tools, P(11.8f, 10.6f, 0.9f));

            var lineRoot = Root("BL10_BarrierLine", tools, 13f, 8f);
            var lineSlots = new[] { Point("PT_BarrierSlot_1", lineRoot, 13f, 7f), Point("PT_BarrierSlot_2", lineRoot, 13f, 9f) };
            var markers = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                lineSlots[i].rotation = Quaternion.Euler(0f, 90f, 0f);
                markers[i] = Box($"PLACEHOLDER_SlotMarker_{i + 1}", lineSlots[i], Vector3.up * 0.005f, new Vector3(2f, 0.01f, 0.15f), "Marker", false);
            }
            var line = lineRoot.gameObject.AddComponent<BarrierSnapLine>();
            SetArray(line, "slots", lineSlots);
            SetArray(line, "barriers", new Object[] { barrierA, barrierB });
            SetArray(line, "slotMarkers", markers);

            // Persone
            var people = new GameObject("CHR_People").transform;
            var victimRoot = Root("VT01_Victim", people, 16f, 7.5f);
            Capsule("PLACEHOLDER_VictimTorso", victimRoot, new Vector3(0.45f, 0.15f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.4f, 0.5f, 0.3f), "Victim", false);
            var legs = new GameObject("Legs").transform;
            legs.SetParent(victimRoot, false);
            legs.localPosition = new Vector3(-0.05f, 0.15f, 0f);
            Capsule("PLACEHOLDER_VictimLegs", legs, new Vector3(-0.45f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.3f, 0.45f, 0.3f), "Victim", false);
            var victimCollider = victimRoot.gameObject.AddComponent<BoxCollider>();
            victimCollider.center = new Vector3(0f, 0.17f, 0f);
            victimCollider.size = new Vector3(1.8f, 0.35f, 0.5f);
            var victim = victimRoot.gameObject.AddComponent<InjuredPerson>();
            SetField(victim, "cable", cable);
            SetField(victim, "legs", legs);
            SetInteractable(victim, "Collega folgorato", null);
            var victimObstacle = victimRoot.gameObject.AddComponent<NavMeshObstacle>();
            victimObstacle.shape = NavMeshObstacleShape.Box;
            victimObstacle.center = new Vector3(0f, 0.17f, 0f);
            victimObstacle.size = new Vector3(1.8f, 0.35f, 0.5f);
            victimObstacle.carving = true;
            Label(victimRoot.gameObject, LabelKind.ContextHelp, "Collega folgorato", "Prima togli la corrente, poi controlla coscienza e respiro", new Vector3(0f, 0.6f, 0f));

            var by1 = MakeBystander(people, "BY01_Colleague", 14.1f, 4f);
            var by2 = MakeBystander(people, "BY02_Colleague", 15.4f, 4.2f);
            var by3 = MakeBystander(people, "BY03_Colleague", 16.6f, 4.3f);
            AddObservable(by1.gameObject, "people");

            var pm1Root = Root("PM1_ParamedicInShock", people, 22f, 5f);
            pm1Root.rotation = Quaternion.Euler(0f, 180f, 0f);
            var pm1Body = new GameObject("Body").transform;
            pm1Body.SetParent(pm1Root, false);
            pm1Body.localScale = new Vector3(1f, 0.6f, 1f);
            var pm1Capsule = Capsule("PLACEHOLDER_Paramedic", pm1Body, Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.5f, 0.9f, 0.5f), "Paramedic", true);
            pm1Root.gameObject.AddComponent<NpcMover>();
            var pm1 = pm1Root.gameObject.AddComponent<ParamedicInShock>();
            SetField(pm1, "body", pm1Body);
            SetInteractable(pm1, "Paramedico sotto shock", pm1Capsule.GetComponent<Renderer>());

            var pm2Root = Root("PM2_ArrivingParamedic", people, 1f, 8f);
            Capsule("PLACEHOLDER_Paramedic2", pm2Root, Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.5f, 0.9f, 0.5f), "Paramedic", true);
            var pm2 = pm2Root.gameObject.AddComponent<NpcMover>();
            pm2Root.gameObject.SetActive(false);

            // MNG_Scenario
            var mng = new GameObject("MNG_Scenario");
            var scenario = mng.AddComponent<ElectricShockScenario>();
            var so = new SerializedObject(scenario);
            so.FindProperty("mission").objectReferenceValue = mission;
            so.FindProperty("spawnPoint").objectReferenceValue = spawn;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("debriefPhrases").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(DATA_DIR + "/ElectricShock_DebriefPhrases.json");
            so.FindProperty("generator").objectReferenceValue = generator;
            so.FindProperty("emergencyStop").objectReferenceValue = stop;
            so.FindProperty("cable").objectReferenceValue = cable;
            so.FindProperty("fuelPool").objectReferenceValue = pool;
            Assign(so.FindProperty("fuelPoolPositions"), poolPositions);
            so.FindProperty("fire").objectReferenceValue = fire;
            so.FindProperty("fuelTank").objectReferenceValue = tankHazard;
            so.FindProperty("harmfulAirZone").objectReferenceValue = airBox;
            so.FindProperty("crisisSmoke").objectReferenceValue = crisisSmoke;
            Assign(so.FindProperty("extinguishers"), extinguishers);
            Assign(so.FindProperty("extinguisherSlots"), slots);
            so.FindProperty("barrierLine").objectReferenceValue = line;
            so.FindProperty("radio").objectReferenceValue = radio;
            so.FindProperty("victim").objectReferenceValue = victim;
            Assign(so.FindProperty("bystanders"), new Object[] { by1, by2 });
            so.FindProperty("extraBystander").objectReferenceValue = by3;
            so.FindProperty("paramedicInShock").objectReferenceValue = pm1;
            so.FindProperty("arrivingParamedic").objectReferenceValue = pm2;
            so.FindProperty("safePoint").objectReferenceValue = safe;
            so.FindProperty("assemblyPoint").objectReferenceValue = assembly;
            Assign(so.FindProperty("exitPath"), exit);
            Assign(so.FindProperty("paramedicPath"), pm2Path);
            Assign(so.FindProperty("zoneEntryPath"), zoneEntry);
            so.FindProperty("victimApproachPoint").objectReferenceValue = victimApproach;
            Assign(so.FindProperty("observables"), new Object[] { cableRoot.GetComponent<ObservableHazard>(), pool.GetComponent<ObservableHazard>(), by1.GetComponent<ObservableHazard>() });
            so.ApplyModifiedPropertiesWithoutUndo();

            // NavMesh: escluse persone e oggetti mobili (barriere, estintori, radio, cavo, trigger).
            IgnoreForNavMesh(people.gameObject);
            IgnoreForNavMesh(air.gameObject);
            foreach (var e in extinguishers) IgnoreForNavMesh(e.gameObject);
            IgnoreForNavMesh(barrierA.gameObject);
            IgnoreForNavMesh(barrierB.gameObject);
            IgnoreForNavMesh(radio.gameObject);
            BakeNavMesh();

            EditorSceneManager.SaveScene(scene, SCENARIO_SCENE);
        }

        /// <summary>Esclude un oggetto (e i figli) dalla cottura della NavMesh.</summary>
        private static void IgnoreForNavMesh(GameObject go)
        {
            var m = go.AddComponent<NavMeshModifier>();
            m.ignoreFromBuild = true;
        }

        /// <summary>Cuoce la NavMesh della scena con l'agente di default (nessuna modifica a ProjectSettings) e la salva in Data/.</summary>
        private static void BakeNavMesh()
        {
            var go = new GameObject("NAV_Surface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string path = DATA_DIR + "/NAV_ElectricShock.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            surface.AddData();
        }

        /// <summary>Sorgente audio 3D segnaposto (il clip viene generato a runtime se manca).</summary>
        private static AudioSource Audio3D(string name, Transform parent, Vector3 localPos, bool loop, float volume, float maxDistance = 20f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = loop;
            a.volume = volume;
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 1f;
            a.maxDistance = maxDistance;
            return a;
        }

        /// <summary>Pavimento, pareti con i varchi DR-01 e DR-03, marcatori di zona.</summary>
        private static void BuildEnvironment(Transform env)
        {
            Box("PLACEHOLDER_Floor", env, new Vector3(15f, -0.05f, 8f), new Vector3(30f, 0.1f, 16f), "Floor", true);
            const float h = 3f, t = 0.2f;
            Wall(env, "South", 6f, 2f, 30f, 2f, h, t);
            Wall(env, "North", 6f, 14f, 30f, 14f, h, t);
            Wall(env, "East", 30f, 2f, 30f, 14f, h, t);
            Wall(env, "West_A", 6f, 2f, 6f, 6f, h, t);
            Wall(env, "West_B", 6f, 10f, 6f, 14f, h, t);
            Wall(env, "Middle_A", 13f, 2f, 13f, 6f, h, t);
            Wall(env, "Middle_B", 13f, 10f, 13f, 14f, h, t);
        }

        /// <summary>Parete tra due punti della mappa.</summary>
        private static void Wall(Transform parent, string name, float x0, float y0, float x1, float y1, float height, float thickness)
        {
            Vector3 a = P(x0, y0), b = P(x1, y1);
            Vector3 c = (a + b) * 0.5f + Vector3.up * height * 0.5f;
            bool alongX = Mathf.Abs(x1 - x0) > Mathf.Abs(y1 - y0);
            float len = (b - a).magnitude + thickness;
            Box("PLACEHOLDER_Wall_" + name, parent, c, alongX ? new Vector3(len, height, thickness) : new Vector3(thickness, height, len), "Wall", true);
        }

        /// <summary>Estintore segnaposto con banda colorata, ugello e spruzzo.</summary>
        private static Extinguisher MakeExtinguisher(Transform parent, string name, ExtinguisherAgent agent, string labelMat, Transform slot)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(slot.position, slot.rotation);
            var body = Cylinder("PLACEHOLDER_ExtinguisherBody", root, Vector3.up * 0.25f, new Vector3(0.18f, 0.25f, 0.18f), "Extinguisher", false);
            Cylinder("PLACEHOLDER_ExtinguisherLabel", root, Vector3.up * 0.3f, new Vector3(0.185f, 0.06f, 0.185f), labelMat, false);
            Box("PLACEHOLDER_ExtinguisherHandle", root, Vector3.up * 0.55f, new Vector3(0.05f, 0.05f, 0.14f), "Radio", false);
            var nozzle = new GameObject("Nozzle").transform;
            nozzle.SetParent(root, false);
            nozzle.localPosition = new Vector3(0f, 0.55f, 0.1f);
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.3f;
            col.size = new Vector3(0.22f, 0.62f, 0.22f);
            var spray = SprayParticles("VFX_Spray", nozzle, agent == ExtinguisherAgent.Water ? new Color(0.5f, 0.7f, 1f, 0.6f) : agent == ExtinguisherAgent.CO2 ? new Color(0.9f, 0.95f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.8f));
            var ext = root.gameObject.AddComponent<Extinguisher>();
            SetField(ext, "nozzle", nozzle);
            SetField(ext, "spray", spray);
            var so = new SerializedObject(ext);
            so.FindProperty("agent").enumValueIndex = (int)agent;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetInteractable(ext, "Estintore", body.GetComponent<Renderer>());
            return ext;
        }

        /// <summary>Barriera mobile segnaposto (2 m x 1 m).</summary>
        private static BarrierPanel MakeBarrier(Transform parent, string name, Vector3 pos)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, 90f, 0f));
            Box("PLACEHOLDER_BarrierPanel", root, Vector3.up * 0.55f, new Vector3(2f, 0.5f, 0.05f), "Barrier", false);
            Box("PLACEHOLDER_BarrierLegL", root, new Vector3(-0.9f, 0.4f, 0f), new Vector3(0.05f, 0.8f, 0.3f), "Rack", false);
            Box("PLACEHOLDER_BarrierLegR", root, new Vector3(0.9f, 0.4f, 0f), new Vector3(0.05f, 0.8f, 0.3f), "Rack", false);
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.5f;
            col.size = new Vector3(2f, 1f, 0.3f);
            var b = root.gameObject.AddComponent<BarrierPanel>();
            SetInteractable(b, "Barriera", null);
            return b;
        }

        /// <summary>Radio segnaposto per il 112.</summary>
        private static EmergencyRadio MakeRadio(Transform parent, Vector3 pos)
        {
            var root = new GameObject("RD01_Radio").transform;
            root.SetParent(parent, false);
            root.position = pos;
            Box("PLACEHOLDER_Radio", root, Vector3.up * 0.1f, new Vector3(0.08f, 0.2f, 0.05f), "Radio", false);
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.1f;
            col.size = new Vector3(0.12f, 0.24f, 0.1f);
            var r = root.gameObject.AddComponent<EmergencyRadio>();
            SetInteractable(r, "Radio 112", null);
            return r;
        }

        /// <summary>Collega segnaposto.</summary>
        private static Bystander MakeBystander(Transform parent, string name, float x, float y)
        {
            var root = Root(name, parent, x, y);
            Capsule("PLACEHOLDER_Colleague", root, Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.5f, 0.9f, 0.5f), "Bystander", true);
            root.gameObject.AddComponent<NpcMover>();
            return root.gameObject.AddComponent<Bystander>();
        }

        /// <summary>Segmento di collider del cavo (non trigger, per il contatto della mano).</summary>
        private static void CableSegment(Transform parent, Vector3 a, Vector3 b, int index)
        {
            var go = new GameObject($"COL_CableSegment_{index + 1}");
            go.transform.SetParent(parent, true);
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(b - a);
            var c = go.AddComponent<CapsuleCollider>();
            c.direction = 2;
            c.radius = 0.05f;
            c.height = (b - a).magnitude;
        }

        // =====================================================================
        // Missione, catalogo e Build Settings (UI comune)
        // =====================================================================

        /// <summary>Crea o aggiorna SO_Mission_ElectricShock nella cartella dello scenario (dati della specifica, testi in italiano).</summary>
        public static MissionDefinition LoadOrCreateMission()
        {
            var m = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<MissionDefinition>();
                AssetDatabase.CreateAsset(m, MISSION_PATH);
            }
            m.Id = "10";
            m.Title = "Electric Shock";
            m.Context = "Sei un lavoratore del turno. Un collega ha preso la scossa da un generatore portatile ed è a terra, ancora a contatto con il cavo. "
                + "Vicino c'è un paramedico sotto shock e sotto il generatore c'è una pozza di benzina. Isola la zona e metti tutti in salvo.";
            m.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
            m.GradientTop = new Color(0.549f, 0.478f, 0.071f, 1f);
            m.GradientBottom = new Color(0.851f, 0.769f, 0.227f, 1f);
            m.Complexity = 4;
            m.SceneName = "SCN_ElectricShock";
            m.Objectives = new List<string> { "Isola il generatore", "Spegni l'incendio", "Evacua e isola la zona", "Assisti collega e paramedico" };
            m.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = "Generatore spento, tutti in salvo" },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = "Generatore spento, zona non isolata" },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = "Persone ferite o estintore errato" }
            };
            m.Levels = new List<MissionLevel>
            {
                new MissionLevel { Level = DifficultyLevel.Guided, UnlockedByDefault = true, TimeLimitSecondsOverride = 0 },
                new MissionLevel { Level = DifficultyLevel.Operational, UnlockedByDefault = true, TimeLimitSecondsOverride = 240 },
                new MissionLevel { Level = DifficultyLevel.Crisis, UnlockedByDefault = true, TimeLimitSecondsOverride = 180 }
            };
            m.IsTraining = false;
            m.DesignerNote = "Scenario 10 · G_Ludovica. Specifica 10_Electric_Shock_LITE. Missione generata da ElectricShockSceneBuilder.";
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>
        /// Mette la missione nel catalogo comune al posto di quella d'esempio (Id 10), elimina l'esempio e aggiunge la scena alle
        /// Build Settings dopo SCN_Core. Tocca file condivisi: eseguire una volta sola, dopo aver aggiornato il progetto.
        /// </summary>
        [MenuItem("SI81/G_Ludovica/Electric Shock/Registra nel catalogo e nelle Build Settings")]
        public static void RegisterMission()
        {
            var mission = LoadOrCreateMission();
            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CATALOG);
            if (catalog == null) { Debug.LogError("[SI81 ElectricShock] SO_MissionCatalog non trovato: " + CATALOG); return; }
            var sample = AssetDatabase.LoadAssetAtPath<MissionDefinition>(SAMPLE_MISSION);
            int index = sample != null ? catalog.Missions.IndexOf(sample) : -1;
            catalog.Missions.RemoveAll(x => x == null || x == sample);
            if (!catalog.Missions.Contains(mission))
            {
                if (index >= 0 && index <= catalog.Missions.Count) catalog.Missions.Insert(index, mission);
                else catalog.Missions.Add(mission);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            if (sample != null) AssetDatabase.DeleteAsset(SAMPLE_MISSION);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(sc => sc.path == SCENARIO_SCENE))
            {
                scenes.Add(new EditorBuildSettingsScene(SCENARIO_SCENE, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SI81 ElectricShock] Missione 10 nel catalogo, scena nelle Build Settings.");
        }

        /// <summary>Etichetta nel mondo della UI comune (LabelAnchor) con testo in italiano.</summary>
        private static void Label(GameObject go, LabelKind kind, string title, string detail, Vector3 offset)
        {
            var anchor = go.AddComponent<LabelAnchor>();
            var so = new SerializedObject(anchor);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("detail").stringValue = detail;
            so.FindProperty("offset").vector3Value = offset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // Particellari segnaposto (da sostituire con i VFX del Common Core)
        // =====================================================================

        /// <summary>Fiamme.</summary>
        private static ParticleSystem FireParticles(string name, Transform parent, float radius, float size, float rate)
        {
            var ps = NewParticles(name, parent, mats["Fire"]);
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 1.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.05f));
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(1f, 0.7f, 0.2f), new Color(0.8f, 0.1f, 0f));
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            return ps;
        }

        /// <summary>Fumo.</summary>
        private static ParticleSystem SmokeParticles(string name, Transform parent, Vector3 localPos, float radius, float rate)
        {
            var ps = NewParticles(name, parent, mats["Smoke"]);
            ps.transform.localPosition = localPos;
            var main = ps.main;
            main.startLifetime = 4f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(new Color(0.35f, 0.35f, 0.35f), new Color(0.2f, 0.2f, 0.2f));
            return ps;
        }

        /// <summary>Scintille del cavo (attive all'avvio).</summary>
        private static ParticleSystem Sparks(string name, Transform parent, Vector3 worldPos)
        {
            var ps = NewParticles(name, parent, mats["Spark"]);
            ps.transform.position = worldPos;
            var main = ps.main;
            main.playOnAwake = true;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = 0.04f;
            main.gravityModifier = 1f;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 12, -1, 0.4f) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.05f; sh.rotation = new Vector3(-90f, 0f, 0f);
            return ps;
        }

        /// <summary>Spruzzo dell'estintore lungo l'asse Z dell'ugello.</summary>
        private static ParticleSystem SprayParticles(string name, Transform parent, Color color)
        {
            var ps = NewParticles(name, parent, mats["Spray"]);
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 120f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = 0.02f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 2f));
            return ps;
        }

        /// <summary>Vampata dell'esplosione (scoppio singolo).</summary>
        private static ParticleSystem BlastParticles(string name, Transform parent)
        {
            var ps = NewParticles(name, parent, mats["Fire"]);
            ps.transform.localPosition = Vector3.up * 0.5f;
            var main = ps.main;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 1.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.3f, 0f));
            main.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 250) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(new Color(1f, 0.7f, 0.2f), new Color(0.3f, 0.1f, 0.05f));
            return ps;
        }

        /// <summary>Crea un particellare fermo (non parte da solo).</summary>
        private static ParticleSystem NewParticles(string name, Transform parent, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        /// <summary>Gradiente da un colore pieno a trasparente.</summary>
        private static ParticleSystem.MinMaxGradient Fade(Color from, Color to)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        /// <summary>Punto della mappa (x, y) in coordinate Unity (x, h, y).</summary>
        private static Vector3 P(float x, float y, float h = 0f) => new Vector3(x, h, y);

        /// <summary>Oggetto radice a terra in un punto della mappa.</summary>
        private static Transform Root(string name, Transform parent, float x, float y)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = P(x, y);
            return t;
        }

        /// <summary>Punto vuoto in un punto della mappa.</summary>
        private static Transform Point(string name, Transform parent, float x, float y) => Root(name, parent, x, y);

        /// <summary>Marcatore piatto a terra (zona sicura, punto di raccolta).</summary>
        private static Transform Marker(string name, Transform parent, float x, float y, float sx, float sy, string mat, bool round)
        {
            var root = Root(name, parent, x, y);
            if (round) Cylinder("PLACEHOLDER_Marker", root, Vector3.up * 0.005f, new Vector3(sx, 0.005f, sy), mat, false);
            else Box("PLACEHOLDER_Marker", root, Vector3.up * 0.005f, new Vector3(sx, 0.01f, sy), mat, false);
            return root;
        }

        /// <summary>Cubo segnaposto.</summary>
        private static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, string mat, bool collider)
            => Primitive(PrimitiveType.Cube, name, parent, localPos, Quaternion.identity, size, mat, collider);

        /// <summary>Cilindro segnaposto (scala Y = metà altezza).</summary>
        private static GameObject Cylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, string mat, bool collider)
            => Primitive(PrimitiveType.Cylinder, name, parent, localPos, Quaternion.identity, scale, mat, collider);

        /// <summary>Capsula segnaposto (scala Y = metà altezza).</summary>
        private static GameObject Capsule(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale, string mat, bool collider)
            => Primitive(PrimitiveType.Capsule, name, parent, localPos, localRot, scale, mat, collider);

        /// <summary>Primitiva con materiale; collider rimosso se non richiesto.</summary>
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale, string mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mats[mat];
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Aggiunge un ObservableHazard con l'ID indicato.</summary>
        private static void AddObservable(GameObject go, string id)
        {
            var o = go.AddComponent<ObservableHazard>();
            var so = new SerializedObject(o);
            so.FindProperty("hazardId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Imposta nome e renderer evidenziabili di un TempInteractable.</summary>
        private static void SetInteractable(TempInteractable it, string displayName, Renderer highlight)
        {
            var so = new SerializedObject(it);
            so.FindProperty("displayName").stringValue = displayName;
            var arr = so.FindProperty("highlightRenderers");
            arr.arraySize = highlight != null ? 1 : 0;
            if (highlight != null) arr.GetArrayElementAtIndex(0).objectReferenceValue = highlight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Imposta un campo serializzato (anche privato) con un riferimento.</summary>
        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[SI81 ElectricShock] Campo {field} non trovato su {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Imposta un campo array serializzato con dei riferimenti.</summary>
        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            Assign(so.FindProperty(field), values);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Copia i riferimenti in una proprietà array.</summary>
        private static void Assign(SerializedProperty p, Object[] values)
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
