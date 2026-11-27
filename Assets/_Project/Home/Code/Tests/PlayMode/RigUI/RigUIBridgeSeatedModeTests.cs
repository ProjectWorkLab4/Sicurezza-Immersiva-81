using System.Collections;
using NUnit.Framework;
using SI81.Input;
using SI81.Rig;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SI81.UI.Rig.Tests
{
    /// <summary>
    /// PlayMode verification suite for global seated mode in <see cref="RigUIBridge"/> and <see cref="UIScreenManager"/>:
    /// covers Comfort toggle, standing/seated dynamic calibration, deferred HMD pose calibration, recalibration via
    /// <see cref="SettingsScreen"/>, reset-to-defaults, menu floor-height compensation, rig re-apply idempotency,
    /// root teleport alignment, disable/re-enable cleanup, and an end-to-end <c>SCN_Core</c> scene smoke test.
    /// </summary>
    public sealed class RigUIBridgeSeatedModeTests
    {
        private const string THEME_ASSET_PATH = "Assets/_Project/Home/UI/Data/SO_UITheme.asset";
        private const string CORE_SCENE_PATH = "Assets/_Project/Home/Level/Scenes/SCN_Core.unity";

        private GameObject rigRoot;
        private GameObject uiRoot;
        private RigRegistration rig;
        private Transform trackingOrigin;
        private Transform head;
        private Transform leftHand;
        private Transform rightHand;
        private RigUIBridge bridge;
        private UIScreenManager screenManager;
        private GameObject menuRoot;
        private MockSettingsStore store;
        private UITheme theme;
        private Scene loadedCoreScene;

        [SetUp]
        public void SetUp()
        {
            InputService.ResetInstance();
            UIServices.Clear();

            store = new MockSettingsStore();
            SettingsCatalog.RegisterDefaults(store);
            UIServices.Configure(null, null, store, null);

#if UNITY_EDITOR
            theme = AssetDatabase.LoadAssetAtPath<UITheme>(THEME_ASSET_PATH);
#endif

            rigRoot = new GameObject("TestRigRoot");
            CharacterController controller = rigRoot.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            rig = rigRoot.AddComponent<RigRegistration>();

            trackingOrigin = new GameObject("TrackingSpace").transform;
            trackingOrigin.SetParent(rigRoot.transform, false);

            head = new GameObject("CenterEyeAnchor").transform;
            head.SetParent(trackingOrigin, false);
            head.localPosition = new Vector3(0f, 1.20f, 0f);

            leftHand = new GameObject("LeftControllerAnchor").transform;
            leftHand.SetParent(trackingOrigin, false);
            leftHand.localPosition = new Vector3(-0.25f, 0.85f, 0.2f);

            rightHand = new GameObject("RightControllerAnchor").transform;
            rightHand.SetParent(trackingOrigin, false);
            rightHand.localPosition = new Vector3(0.25f, 0.85f, 0.2f);

            uiRoot = new GameObject("TestUIRoot");
            GameObject eventSystemGo = new GameObject("MNG_EventSystem");
            eventSystemGo.transform.SetParent(uiRoot.transform, false);
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
            UIInputSetup inputSetup = eventSystemGo.AddComponent<UIInputSetup>();

            menuRoot = new GameObject("CANVAS_Menu");
            menuRoot.transform.SetParent(uiRoot.transform, false);

            screenManager = uiRoot.AddComponent<UIScreenManager>();
#if UNITY_EDITOR
            var serializedManager = new SerializedObject(screenManager);
            serializedManager.FindProperty("inputSetup").objectReferenceValue = inputSetup;
            serializedManager.FindProperty("theme").objectReferenceValue = theme;
            serializedManager.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            serializedManager.FindProperty("showMenuOnStart").boolValue = true;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
#endif

            bridge = uiRoot.AddComponent<RigUIBridge>();
            bridge.Configure(screenManager, null, null, null, null);

            rig.Configure(head, trackingOrigin, leftHand, rightHand);
            rig.RegisterBridge(bridge);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            if (loadedCoreScene.IsValid() && loadedCoreScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedCoreScene);
                while (unload != null && !unload.isDone) yield return null;
                loadedCoreScene = default;
            }
            if (uiRoot != null) Object.DestroyImmediate(uiRoot);
            if (rigRoot != null) Object.DestroyImmediate(rigRoot);
            UIServices.Clear();
            InputService.ResetInstance();
        }

        private void DestroySyntheticRigAndUI()
        {
            if (uiRoot != null) Object.DestroyImmediate(uiRoot);
            if (rigRoot != null) Object.DestroyImmediate(rigRoot);
            UIServices.Clear();
            InputService.ResetInstance();
        }

        [UnityTest]
        public IEnumerator SeatedMode_DefaultsOff_KeepsTrackingOriginAtFloor()
        {
            yield return null;

            Assert.That(store.GetBool(SettingsCatalog.COMFORT_SEATED_MODE), Is.False);
            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(screenManager.SeatedHeightOffset, Is.EqualTo(0f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_ToggleOnWhileSeated_CalibratesToStandingHeight_AndToggleOffRestoresFloor()
        {
            head.localPosition = new Vector3(0f, 1.15f, 0f);
            yield return null;

            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            float expectedOffset = RigUIBridge.TARGET_STANDING_EYE_HEIGHT - 1.15f;
            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(expectedOffset).Within(0.001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(expectedOffset).Within(0.001f));
            Assert.That(head.position.y, Is.EqualTo(RigUIBridge.TARGET_STANDING_EYE_HEIGHT).Within(0.001f));
            Assert.That(screenManager.SeatedHeightOffset, Is.EqualTo(expectedOffset).Within(0.001f));

            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, false);
            yield return null;

            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(head.position.y, Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(screenManager.SeatedHeightOffset, Is.EqualTo(0f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_ToggleOnWhileStanding_UsesDefaultOffset_AndSettingsRecalibrateAdaptsWhenSeated()
        {
            head.localPosition = new Vector3(0f, 1.65f, 0f);
            yield return null;

            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(RigUIBridge.DEFAULT_SEATED_OFFSET).Within(0.001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(RigUIBridge.DEFAULT_SEATED_OFFSET).Within(0.001f));

            // Player sits down and presses "Altezza utente -> Ricalibra" in SettingsScreen.
            head.localPosition = new Vector3(0f, 1.18f, 0f);
            GameObject settingsHost = new GameObject("TestSettingsScreen");
            try
            {
                GameObject rowGroup = new GameObject("ComfortGroup", typeof(RectTransform));
                rowGroup.transform.SetParent(settingsHost.transform, false);

                GameObject actionRowGo = new GameObject("RecalibrateRow", typeof(RectTransform));
                actionRowGo.transform.SetParent(rowGroup.transform, false);
                SettingsActionRow actionRow = actionRowGo.AddComponent<SettingsActionRow>();
                actionRow.SetActionData(SettingsActionType.Button, SettingsCatalog.ACTION_RECALIBRATE, false, null, string.Empty);

                SettingsScreen settingsScreen = settingsHost.AddComponent<SettingsScreen>();
#if UNITY_EDITOR
                var serializedSettings = new SerializedObject(settingsScreen);
                SerializedProperty groupsProp = serializedSettings.FindProperty("rowGroups");
                groupsProp.arraySize = 1;
                groupsProp.GetArrayElementAtIndex(0).objectReferenceValue = rowGroup.GetComponent<RectTransform>();
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
#endif
                // Re-enable after assigning rowGroups so SettingsScreen binds actionRow.
                settingsHost.SetActive(false);
                settingsHost.SetActive(true);
                yield return null;

                // Trigger through RecalibrateSeatedHeight (and verify SettingsScreen.RecalibrateRequested path).
                bool eventFired = false;
                void MarkEvent() { eventFired = true; }
                SettingsScreen.RecalibrateRequested += MarkEvent;
                try
                {
                    // CollectRows ran in Awake before rowGroups was serialized; instantiate a fresh prefab-like copy
                    // via Instantiate so Awake sees rowGroups populated just like MNG_UI.
                    GameObject wiredCopy = Object.Instantiate(settingsHost);
                    try
                    {
                        SettingsActionRow copiedRow = wiredCopy.GetComponentInChildren<SettingsActionRow>(true);
                        copiedRow.PerformAction();
                    }
                    finally
                    {
                        Object.DestroyImmediate(wiredCopy);
                    }
                }
                finally
                {
                    SettingsScreen.RecalibrateRequested -= MarkEvent;
                }

                Assert.That(eventFired, Is.True, "SettingsActionRow with ACTION_RECALIBRATE must raise SettingsScreen.RecalibrateRequested.");
                float expectedOffset = RigUIBridge.TARGET_STANDING_EYE_HEIGHT - 1.18f;
                Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(expectedOffset).Within(0.001f));
                Assert.That(head.position.y, Is.EqualTo(RigUIBridge.TARGET_STANDING_EYE_HEIGHT).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(settingsHost);
            }
        }

        [UnityTest]
        public IEnumerator SeatedMode_PersistedOnBeforeTrackingReady_CalibratesOnceWhenHeadPoseArrives()
        {
            head.localPosition = Vector3.zero;
            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(RigUIBridge.DEFAULT_SEATED_OFFSET).Within(0.001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(RigUIBridge.DEFAULT_SEATED_OFFSET).Within(0.001f));

            // Simulate first real Quest 3 seated HMD pose arriving.
            head.localPosition = new Vector3(0f, 1.10f, 0f);
            yield return null;

            float expectedOffset = RigUIBridge.TARGET_STANDING_EYE_HEIGHT - 1.10f;
            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(expectedOffset).Within(0.001f));
            Assert.That(head.position.y, Is.EqualTo(RigUIBridge.TARGET_STANDING_EYE_HEIGHT).Within(0.001f));

            // Ducking afterwards must not alter the calibrated floor offset.
            head.localPosition = new Vector3(0f, 0.90f, 0f);
            yield return null;
            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(expectedOffset).Within(0.001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(expectedOffset).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_ReapplyingSameRig_DoesNotStackOffset()
        {
            head.localPosition = new Vector3(0f, 1.20f, 0f);
            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            float firstOffset = bridge.AppliedSeatedOffset;
            Assert.That(firstOffset, Is.EqualTo(0.40f).Within(0.001f));

            rig.Apply();
            rig.Apply();
            yield return null;

            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(firstOffset).Within(0.001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(firstOffset).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_ResetToDefaults_RestoresStandingHeight()
        {
            head.localPosition = new Vector3(0f, 1.15f, 0f);
            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;
            Assert.That(bridge.AppliedSeatedOffset, Is.GreaterThan(0.1f));

            store.ResetToDefaults();
            yield return null;

            Assert.That(store.GetBool(SettingsCatalog.COMFORT_SEATED_MODE), Is.False);
            Assert.That(bridge.AppliedSeatedOffset, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_PlaceInFront_KeepsMenuAtFloorRelativeHeight()
        {
            Assert.That(theme, Is.Not.Null, "SO_UITheme must exist for menu height verification.");
            head.localPosition = new Vector3(0f, 1.15f, 0f);
            yield return null;

            screenManager.PlaceInFront(head);
            float standingMenuY = menuRoot.transform.position.y;
            Assert.That(standingMenuY, Is.EqualTo(theme.MenuCenterHeight).Within(0.001f));

            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            screenManager.PlaceInFront(head);
            Assert.That(menuRoot.transform.position.y, Is.EqualTo(standingMenuY).Within(0.001f),
                "Menu center height must remain anchored to physical floorY, not doubly elevated by seated offset.");

            // Verify elevated floor platform (e.g. spawn at y = 2.0 m) also places menu at 2.0 + MenuCenterHeight.
            rig.TeleportTo(new Vector3(3f, 2f, -4f), Quaternion.identity);
            yield return null;

            screenManager.PlaceInFront(head);
            Assert.That(menuRoot.transform.position.y, Is.EqualTo(2f + theme.MenuCenterHeight).Within(0.001f));
            Assert.That(head.position.y, Is.EqualTo(2f + RigUIBridge.TARGET_STANDING_EYE_HEIGHT).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SeatedMode_DisableAndReenableBridge_RestoresAndReappliesOffset()
        {
            head.localPosition = new Vector3(0f, 1.20f, 0f);
            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0.40f).Within(0.001f));

            bridge.enabled = false;
            yield return null;
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(screenManager.SeatedHeightOffset, Is.EqualTo(0f).Within(0.0001f));

            bridge.enabled = true;
            yield return null;
            Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(0.40f).Within(0.001f));
            Assert.That(screenManager.SeatedHeightOffset, Is.EqualTo(0.40f).Within(0.001f));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator SCN_Core_HomeUISeatedToggle_LiftsCoreRigAndPreservesMenuHeight()
        {
            // Clean up synthetic test objects before loading the production SCN_Core scene additively.
            DestroySyntheticRigAndUI();

            string prefKey = PlayerPrefsSettingsStore.ToKey(SettingsCatalog.COMFORT_SEATED_MODE);
            bool hadKey = PlayerPrefs.HasKey(prefKey);
            float previousValue = hadKey ? PlayerPrefs.GetFloat(prefKey) : 0f;
            PlayerPrefs.DeleteKey(prefKey);

            try
            {
                AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    CORE_SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Additive));
                Assert.That(load, Is.Not.Null, "Failed to start loading SCN_Core.");
                while (!load.isDone) yield return null;

                loadedCoreScene = SceneManager.GetSceneByPath(CORE_SCENE_PATH);
                Assert.That(loadedCoreScene.IsValid() && loadedCoreScene.isLoaded, Is.True, "SCN_Core must be loaded.");
                yield return null;
                yield return null;

                RigRegistration coreRig = RigRegistration.Instance;
                UIScreenManager coreScreens = UIScreenManager.Instance;
                RigUIBridge coreBridge = Object.FindFirstObjectByType<RigUIBridge>(FindObjectsInactive.Include);

                Assert.That(coreRig, Is.Not.Null, "SCN_Core must contain MNG_CoreRig.");
                Assert.That(coreScreens, Is.Not.Null, "SCN_Core must contain UIScreenManager.");
                Assert.That(coreBridge, Is.Not.Null, "SCN_Core must contain RigUIBridge on MNG_UI.");
                Assert.That(UIServices.Settings, Is.Not.Null, "SCN_Core must initialize UIServices.Settings.");

                // Open the real Home UI Settings screen (Comfort tab) so rows bind to UIServices.Settings.
                coreScreens.Show(UIScreenId.Settings);
                yield return null;

                SettingsToggleRow seatedToggle = null;
                foreach (SettingsToggleRow row in Object.FindObjectsByType<SettingsToggleRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (row.SettingId == SettingsCatalog.COMFORT_SEATED_MODE)
                    {
                        seatedToggle = row;
                        break;
                    }
                }
                SettingsActionRow recalibrateRow = null;
                foreach (SettingsActionRow row in Object.FindObjectsByType<SettingsActionRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (row.ActionId == SettingsCatalog.ACTION_RECALIBRATE)
                    {
                        recalibrateRow = row;
                        break;
                    }
                }
                Assert.That(seatedToggle, Is.Not.Null, "SCN_Core Settings must contain the COMFORT_SEATED_MODE toggle row.");
                Assert.That(recalibrateRow, Is.Not.Null, "SCN_Core Settings must contain the ACTION_RECALIBRATE row.");

                // Recenter after MNG_CoreRig's CharacterController has settled on SCN_Core's floor collider.
                coreRig.Head.localPosition = new Vector3(0f, 1.20f, 0f);
                coreScreens.Recenter();
                float baselineOriginY = coreRig.TrackingOrigin.localPosition.y;
                float baselineMenuY = coreScreens.MenuRoot.transform.position.y;

                // Toggle "Modalità seduto" ON via the actual Home UI toggle row.
                seatedToggle.Toggle();
                yield return null;

                Assert.That(coreBridge.AppliedSeatedOffset, Is.EqualTo(0.40f).Within(0.001f));
                Assert.That(coreRig.TrackingOrigin.localPosition.y, Is.EqualTo(baselineOriginY + 0.40f).Within(0.001f));

                coreScreens.Recenter();
                Assert.That(coreScreens.MenuRoot.transform.position.y, Is.EqualTo(baselineMenuY).Within(0.02f),
                    "CANVAS_Menu in SCN_Core must stay at standing floor-relative height when seated mode is active.");

                // Adjust seated head height and press the actual Home UI "Ricalibra" action row.
                coreRig.Head.localPosition = new Vector3(0f, 1.12f, 0f);
                recalibrateRow.PerformAction();
                yield return null;

                Assert.That(coreBridge.AppliedSeatedOffset, Is.EqualTo(0.48f).Within(0.001f));
                Assert.That(coreRig.TrackingOrigin.localPosition.y, Is.EqualTo(baselineOriginY + 0.48f).Within(0.001f));
                Assert.That(coreScreens.MenuRoot.transform.position.y, Is.EqualTo(baselineMenuY).Within(0.02f));

                // Toggle OFF via the actual Home UI toggle row and confirm clean restoration.
                seatedToggle.Toggle();
                yield return null;
                Assert.That(coreBridge.AppliedSeatedOffset, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(coreRig.TrackingOrigin.localPosition.y, Is.EqualTo(baselineOriginY).Within(0.001f));
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetFloat(prefKey, previousValue);
                else PlayerPrefs.DeleteKey(prefKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator ScenarioController_WithRigSpawnPoint_DoesNotCorruptTrackingOriginAcrossScenarios()
        {
            head.localPosition = new Vector3(0f, 1.20f, 0f);
            store.SetBool(SettingsCatalog.COMFORT_SEATED_MODE, true);
            yield return null;

            float seatedOffset = bridge.AppliedSeatedOffset;
            Assert.That(seatedOffset, Is.EqualTo(0.40f).Within(0.001f));

            MissionDefinition mission = ScriptableObject.CreateInstance<MissionDefinition>();
            GameObject scenario1SpawnGo = null;
            GameObject scenario1ControllerGo = null;
            GameObject scenario2SpawnGo = null;
            try
            {
                MissionSession.Begin(mission, DifficultyLevel.Guided, false);

                // Scenario 1: wires BOTH SI81.Rig.SpawnPoint and ScenarioController.spawnPoint (like ElectricShock/GasLeak/Evacuation/ManDown).
                scenario1SpawnGo = new GameObject("MNG_SpawnPoint_Scenario1");
                scenario1SpawnGo.transform.SetPositionAndRotation(new Vector3(5f, 0f, -3f), Quaternion.Euler(0f, 180f, 0f));
                scenario1SpawnGo.AddComponent<SpawnPoint>();

                scenario1ControllerGo = new GameObject("MNG_Scenario_Scenario1");
                ScenarioController controller = scenario1ControllerGo.AddComponent<ScenarioController>();
                var so = new SerializedObject(controller);
                so.FindProperty("mission").objectReferenceValue = mission;
                so.FindProperty("spawnPoint").objectReferenceValue = scenario1SpawnGo.transform;
                so.FindProperty("loadCoreInEditor").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();

                yield return null;
                yield return null;

                Assert.That(Vector3.Distance(rigRoot.transform.position, new Vector3(5f, 0f, -3f)), Is.LessThan(0.001f));
                Assert.That(trackingOrigin.localPosition.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(trackingOrigin.localPosition.y, Is.EqualTo(seatedOffset).Within(0.001f));
                Assert.That(trackingOrigin.localPosition.z, Is.EqualTo(0f).Within(0.001f));

                // Unload Scenario 1 (SpawnPoint disables first, then ScenarioController destroys).
                Object.DestroyImmediate(scenario1SpawnGo);
                scenario1SpawnGo = null;
                Object.DestroyImmediate(scenario1ControllerGo);
                scenario1ControllerGo = null;
                MissionSession.Clear();
                yield return null;

                Assert.That(Vector3.Distance(rigRoot.transform.position, Vector3.zero), Is.LessThan(0.001f),
                    "Unloading Scenario 1 must restore rig root to origin.");
                Assert.That(Vector3.Distance(trackingOrigin.localPosition, new Vector3(0f, seatedOffset, 0f)), Is.LessThan(0.001f),
                    "Unloading Scenario 1 must NOT leak world-space offset into TrackingOrigin.localPosition.");
                Assert.That(Quaternion.Angle(trackingOrigin.localRotation, Quaternion.identity), Is.LessThan(0.1f));

                // Scenario 2 (Machine Lock style): relies solely on SI81.Rig.SpawnPoint at (-7.6, 0, 0), yaw 90 deg.
                scenario2SpawnGo = new GameObject("MNG_SpawnPoint_MachineLock");
                scenario2SpawnGo.transform.SetPositionAndRotation(new Vector3(-7.6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                scenario2SpawnGo.AddComponent<SpawnPoint>();
                yield return null;

                Assert.That(Vector3.Distance(rigRoot.transform.position, new Vector3(-7.6f, 0f, 0f)), Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(trackingOrigin.position, new Vector3(-7.6f, seatedOffset, 0f)), Is.LessThan(0.001f),
                    "Scenario 2 must spawn TrackingOrigin cleanly at (-7.6, seatedOffset, 0) with zero drift.");
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(trackingOrigin.eulerAngles.y, 90f)), Is.LessThan(0.1f));
            }
            finally
            {
                if (scenario2SpawnGo != null) Object.DestroyImmediate(scenario2SpawnGo);
                if (scenario1ControllerGo != null) Object.DestroyImmediate(scenario1ControllerGo);
                if (scenario1SpawnGo != null) Object.DestroyImmediate(scenario1SpawnGo);
                MissionSession.Clear();
                Object.DestroyImmediate(mission);
            }
        }
#endif
    }
}
