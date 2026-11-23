using SI81.Input;
using UnityEditor;
using UnityEngine;

namespace SI81.Rig.EditorTools
{
    /// <summary>
    /// Crea o ricrea il prefab MNG_CoreRig: radice con CharacterController + RigRegistration + locomozione,
    /// OVRCameraRig Meta come figlio (TrackingSpace / Floor Level), modelli controller solo visivi allineati alla posa aim.
    /// Le UIPointer per mano le aggiunge l'adattatore del progetto UI: questo assembly non referenzia SI81.UI.
    /// </summary>
    public static class RigPrefabBuilder
    {
        /// <summary>Percorso del prefab condiviso.</summary>
        public const string PrefabPath = "Assets/_Project/Home/Level/Prefabs/MNG_CoreRig.prefab";
        private const string PrefabsFolder = "Assets/_Project/Home/Level/Prefabs";
        private const string OvrRigPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        private const string OvrControllerPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab";

        /// <summary>
        /// Ricrea il prefab. Sicuro da rieseguire: sovrascrive lo stesso percorso e conserva il GUID.
        /// </summary>
        [MenuItem("SI81/Rig/Rebuild MNG_CoreRig Prefab")]
        public static void Build()
        {
            GameObject ovrRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OvrRigPrefabPath);
            GameObject ovrControllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OvrControllerPrefabPath);
            if (ovrRigPrefab == null || ovrControllerPrefab == null)
            {
                Debug.LogError("[SI81 Rig] Prefab Meta non trovati. Verifica com.meta.xr.sdk.core.", null);
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Home/Level", "Prefabs");

            GameObject root = new GameObject("MNG_CoreRig");
            try
            {
                CharacterController controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.slopeLimit = 45f;
                controller.stepOffset = 0.3f;

                RigRegistration registration = root.AddComponent<RigRegistration>();
                root.AddComponent<RigLocomotion>();
                root.AddComponent<RigUIPointerFeed>();

                GameObject ovrRig = (GameObject)PrefabUtility.InstantiatePrefab(ovrRigPrefab);
                ovrRig.name = "OVRCameraRig";
                ovrRig.transform.SetParent(root.transform, false);
                ovrRig.transform.localPosition = Vector3.zero;
                ovrRig.transform.localRotation = Quaternion.identity;
                SetFloorLevelTrackingOrigin(ovrRig);

                Transform trackingSpace = ovrRig.transform.Find("TrackingSpace");
                Transform head = trackingSpace != null ? trackingSpace.Find("CenterEyeAnchor") : null;
                Transform leftHand = trackingSpace != null ? trackingSpace.Find("LeftHandAnchor/LeftControllerAnchor") : null;
                Transform rightHand = trackingSpace != null ? trackingSpace.Find("RightHandAnchor/RightControllerAnchor") : null;
                if (head == null || leftHand == null || rightHand == null)
                {
                    Debug.LogError("[SI81 Rig] Gerarchia OVRCameraRig inattesa: manca TrackingSpace/CenterEyeAnchor/LeftControllerAnchor/RightControllerAnchor.", ovrRig);
                    return;
                }

                registration.Configure(head, trackingSpace, leftHand, rightHand);
                AttachControllerModel(ovrControllerPrefab, leftHand, OVRInput.Controller.LTouch, Hand.Left, registration);
                AttachControllerModel(ovrControllerPrefab, rightHand, OVRInput.Controller.RTouch, Hand.Right, registration);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                Debug.Log($"[SI81 Rig] Prefab aggiornato: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Verifica il prefab salvato: componenti, hand-off, modelli e tracking origin. Lancia un'eccezione se manca
        /// qualcosa, così il controllo può girare in batchmode.
        /// </summary>
        [MenuItem("SI81/Rig/Validate MNG_CoreRig Prefab")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new System.InvalidOperationException($"Prefab mancante: {PrefabPath}");

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                CharacterController controller = instance.GetComponent<CharacterController>();
                RigRegistration registration = instance.GetComponent<RigRegistration>();
                RigLocomotion locomotion = instance.GetComponent<RigLocomotion>();
                RigUIPointerFeed feed = instance.GetComponent<RigUIPointerFeed>();
                if (controller == null || registration == null || locomotion == null || feed == null)
                    throw new System.InvalidOperationException("Radice incompleta: servono CharacterController, RigRegistration, RigLocomotion e RigUIPointerFeed.");
                if (!registration.Handoff.IsComplete)
                    throw new System.InvalidOperationException("RigRegistration non ha testa, origine e due mani.");
                if (instance.transform.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor") == null)
                    throw new System.InvalidOperationException("Manca OVRCameraRig/TrackingSpace/CenterEyeAnchor.");

                int followers = instance.GetComponentsInChildren<ControllerAimFollower>(true).Length;
                if (followers != 2)
                    throw new System.InvalidOperationException($"ControllerAimFollower attesi 2, trovati {followers}.");

                OVRControllerHelper[] helpers = instance.GetComponentsInChildren<OVRControllerHelper>(true);
                bool left = false;
                bool right = false;
                for (int i = 0; i < helpers.Length; i++)
                {
                    if (helpers[i].m_controller == OVRInput.Controller.LTouch) left = true;
                    if (helpers[i].m_controller == OVRInput.Controller.RTouch) right = true;
                }
                if (!left || !right)
                    throw new System.InvalidOperationException("Modelli controller: manca LTouch o RTouch.");

                OVRManager manager = instance.GetComponentInChildren<OVRManager>(true);
                if (manager == null || manager.trackingOriginType != OVRManager.TrackingOrigin.FloorLevel)
                    throw new System.InvalidOperationException("OVRManager assente o tracking origin diverso da FloorLevel.");

                Debug.Log($"[SI81 Rig] Prefab valido: {PrefabPath} (hand-off completo, 2 modelli, FloorLevel).");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Imposta l'OVRManager del rig su Floor Level (il prefab Meta nasce Eye Level). Usa SerializedObject, così
        /// l'override viene salvato nel prefab.
        /// </summary>
        /// <param name="ovrRig">Istanza del prefab OVRCameraRig.</param>
        private static void SetFloorLevelTrackingOrigin(GameObject ovrRig)
        {
            OVRManager manager = ovrRig.GetComponent<OVRManager>();
            if (manager == null) manager = ovrRig.GetComponentInChildren<OVRManager>(true);
            if (manager == null)
            {
                Debug.LogError("[SI81 Rig] OVRManager assente nel prefab OVRCameraRig.", ovrRig);
                return;
            }

            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty origin = serialized.FindProperty("_trackingOriginType");
            if (origin == null)
            {
                Debug.LogError("[SI81 Rig] Campo _trackingOriginType non trovato su OVRManager.", manager);
                return;
            }
            origin.intValue = (int)OVRManager.TrackingOrigin.FloorLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Aggiunge un modello controller visivo sotto l'ancora, con la mano giusta e il follower della posa aim.
        /// </summary>
        /// <param name="controllerPrefab">Prefab OVRControllerPrefab.</param>
        /// <param name="anchor">Ancora del controller.</param>
        /// <param name="controller">Controller OVR da rappresentare.</param>
        /// <param name="hand">Mano logica.</param>
        /// <param name="rig">Rig a cui il modello si riferisce.</param>
        private static void AttachControllerModel(GameObject controllerPrefab, Transform anchor, OVRInput.Controller controller,
            Hand hand, RigRegistration rig)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(controllerPrefab);
            model.name = hand == Hand.Left ? "LeftControllerModel" : "RightControllerModel";
            model.transform.SetParent(anchor, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            OVRControllerHelper helper = model.GetComponentInChildren<OVRControllerHelper>();
            if (helper != null)
            {
                SerializedObject serialized = new SerializedObject(helper);
                SerializedProperty controllerProperty = serialized.FindProperty("m_controller");
                if (controllerProperty != null)
                {
                    controllerProperty.intValue = (int)controller;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            ControllerAimFollower follower = model.AddComponent<ControllerAimFollower>();
            follower.Configure(hand, rig);
        }
    }
}
