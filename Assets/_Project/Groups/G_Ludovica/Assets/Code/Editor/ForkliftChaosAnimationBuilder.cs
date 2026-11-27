#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace G_Ludovica.Editor
{
    /// <summary>
    /// Costruisce le animazioni dello scenario Forklift Chaos con Unity Animator:
    /// respiro di Marco, idle eretto dei soccorritori e rotazione delle ruote dei muletti.
    /// Le clip esistenti dentro gli FBX (altro scenario) non vengono usate: qui si generano asset nuovi.
    /// </summary>
    public static class ForkliftChaosAnimationBuilder
    {
        public const string WorkerPath = "Assets/_Project/Shared/Art/Models/MESH_Worker_Vittima.fbx";
        public const string RescuerPath = "Assets/_Project/Shared/Art/Models/MESH_Soccorritore.fbx";
        public const string ForkliftPath = "Assets/_Project/Shared/Art/Models/Muletto.fbx";

        public const string ClipMarco = AnimationFolder + "/AN_Marco_Respiro.anim";
        public const string ClipRescuer = AnimationFolder + "/AN_Soccorritore_Idle.anim";
        public const string ClipForklift = AnimationFolder + "/AN_Muletto_Ruote.anim";

        public const string ControllerMarco = ControllerFolder + "/AC_Marco.controller";
        public const string ControllerRescuer = ControllerFolder + "/AC_Soccorritore.controller";
        public const string ControllerForklift = ControllerFolder + "/AC_Muletto.controller";

        private const string AnimationFolder = "Assets/_Project/Groups/G_Ludovica/Assets/Art/Animations";
        private const string ControllerFolder = "Assets/_Project/Groups/G_Ludovica/Assets/Art/Animators";

        /// <summary>Genera clip, controller e verifica che le curve si aggancino allo scheletro.</summary>
        public static void BuildAll()
        {
            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);

            ConfigureAvatar(WorkerPath);
            ConfigureAvatar(RescuerPath);
            ConfigureAvatar(ForkliftPath);
            AssetDatabase.Refresh();

            AnimationClip marco = BuildMarcoClip();
            AnimationClip rescuer = BuildRescuerClip();
            AnimationClip forklift = BuildForkliftClip();

            BuildController(ControllerMarco, marco, "Respiro");
            BuildController(ControllerRescuer, rescuer, "Idle");
            BuildController(ControllerForklift, forklift, "Ruote");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Validate(WorkerPath, marco, 1.2f, "Head", "LeftHand");
            Validate(RescuerPath, rescuer, 1.5f, "Head", "LeftFoot");
            Validate(ForkliftPath, forklift, 0.5f, "Ruote_anteriori", "Ruote_posteriori");
            Debug.Log("[FKC-ANIM] Animazioni generate.");
        }

        // =====================================================================
        // Import: avatar generico, così l'Animator può agganciare le clip
        // =====================================================================

        private static void ConfigureAvatar(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[FKC-ANIM] Importer mancante per " + path);
                return;
            }

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();

            Avatar avatar = LoadAvatar(path);
            Debug.Log("[FKC-ANIM] " + path + " avatar=" + (avatar != null ? avatar.name + " human=" + avatar.isHuman : "NESSUNO"));
        }

        /// <summary>Avatar generico generato dall'importatore per il modello.</summary>
        public static Avatar LoadAvatar(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Avatar avatar) return avatar;
            return null;
        }

        // =====================================================================
        // Clip
        // =====================================================================

        private static AnimationClip BuildMarcoClip()
        {
            const float length = 4f;
            GameObject root = Clone(WorkerPath);
            try
            {
                AnimationClip clip = NewClip("AN_Marco_Respiro", length);
                AddBreathing(clip, root, FindBone(root, "Spine2"), 1.8f, length);
                AddBreathing(clip, root, FindBone(root, "Spine1"), 0.8f, length);
                AddBreathing(clip, root, FindBone(root, "Head"), 0.4f, length);
                SaveClip(clip, ClipMarco, length);
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipMarco);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static AnimationClip BuildRescuerClip()
        {
            const float length = 5f;
            GameObject root = Clone(RescuerPath);
            try
            {
                PoseStanding(root);
                AnimationClip clip = NewClip("AN_Soccorritore_Idle", length);

                // Posa eretta: gambe dritte, piedi piatti, bacino alla giusta altezza.
                AddConstantPose(clip, root, FindBone(root, "LeftUpLeg"), length);
                AddConstantPose(clip, root, FindBone(root, "LeftLeg"), length);
                AddConstantPose(clip, root, FindBone(root, "LeftFoot"), length);
                AddConstantPose(clip, root, FindBone(root, "RightUpLeg"), length);
                AddConstantPose(clip, root, FindBone(root, "RightLeg"), length);
                AddConstantPose(clip, root, FindBone(root, "RightFoot"), length);
                AddConstantPosition(clip, root, FindBone(root, "Hips"), length);

                AddBreathing(clip, root, FindBone(root, "Spine2"), 1.4f, length);
                AddBreathing(clip, root, FindBone(root, "Spine1"), 0.6f, length);
                AddBreathing(clip, root, FindBone(root, "Head"), 0.3f, length);

                SaveClip(clip, ClipRescuer, length);
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipRescuer);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static AnimationClip BuildForkliftClip()
        {
            const float length = 1f;
            GameObject root = Clone(ForkliftPath);
            try
            {
                AnimationClip clip = NewClip("AN_Muletto_Ruote", length);
                AddSpin(clip, root, FindExact(root, "Ruote_anteriori"), length);
                AddSpin(clip, root, FindExact(root, "Ruote_posteriori"), length);
                SaveClip(clip, ClipForklift, length);
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipForklift);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Posa eretta del soccorritore (la posa di riposo è accovacciata)
        // =====================================================================

        private static void PoseStanding(GameObject root)
        {
            Transform hips = FindBone(root, "Hips");
            Transform leftUpLeg = FindBone(root, "LeftUpLeg");
            Transform leftLeg = FindBone(root, "LeftLeg");
            Transform leftFoot = FindBone(root, "LeftFoot");
            Transform leftToe = FindBone(root, "LeftToeBase");
            Transform rightUpLeg = FindBone(root, "RightUpLeg");
            Transform rightLeg = FindBone(root, "RightLeg");
            Transform rightFoot = FindBone(root, "RightFoot");
            Transform rightToe = FindBone(root, "RightToeBase");

            if (hips == null || leftUpLeg == null || leftLeg == null || leftFoot == null ||
                rightUpLeg == null || rightLeg == null || rightFoot == null)
            {
                Debug.LogError("[FKC-ANIM] Ossa gambe non trovate: posa eretta non applicata.");
                return;
            }

            Aim(leftUpLeg, leftLeg, new Vector3(-0.05f, -1f, 0.02f));
            Aim(leftLeg, leftFoot, new Vector3(-0.01f, -1f, 0.04f));
            if (leftToe != null) Aim(leftFoot, leftToe, new Vector3(0f, -0.05f, 1f));

            Aim(rightUpLeg, rightLeg, new Vector3(0.05f, -1f, 0.02f));
            Aim(rightLeg, rightFoot, new Vector3(0.01f, -1f, 0.04f));
            if (rightToe != null) Aim(rightFoot, rightToe, new Vector3(0f, -0.05f, 1f));

            // Appoggia i piedi a terra e centra il corpo sull'origine del modello.
            float lowest = float.MaxValue;
            if (leftToe != null) lowest = Mathf.Min(lowest, leftToe.position.y);
            if (rightToe != null) lowest = Mathf.Min(lowest, rightToe.position.y);
            if (lowest == float.MaxValue) lowest = Mathf.Min(leftFoot.position.y, rightFoot.position.y);

            Vector3 mid = (leftFoot.position + rightFoot.position) * 0.5f;
            hips.position += new Vector3(-mid.x, 0.02f - lowest, -mid.z);

            Debug.Log("[FKC-ANIM] Posa soccorritore: piedi a " + lowest.ToString("F3") +
                      " m, bacino a " + hips.position.y.ToString("F3") + " m.");
        }

        /// <summary>Ruota l'osso in spazio mondo perché la direzione osso→figlio coincida con quella voluta.</summary>
        private static void Aim(Transform bone, Transform child, Vector3 worldDirection)
        {
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude < 1e-6f) return;
            Quaternion delta = Quaternion.FromToRotation(current.normalized, worldDirection.normalized);
            bone.rotation = delta * bone.rotation;
        }

        // =====================================================================
        // Curve
        // =====================================================================

        private static AnimationClip NewClip(string name, float length)
        {
            AnimationClip clip = new AnimationClip
            {
                name = name,
                frameRate = 30f,
                legacy = false,
                wrapMode = WrapMode.Loop
            };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.startTime = 0f;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void AddBreathing(AnimationClip clip, GameObject root, Transform bone, float amplitude, float length)
        {
            if (bone == null) return;
            string path = AnimationUtility.CalculateTransformPath(bone, root.transform);
            Quaternion baseRotation = bone.localRotation;

            const int steps = 8;
            float[] times = new float[steps + 1];
            Vector3[] eulers = new Vector3[steps + 1];
            Vector3 previous = Vector3.zero;

            for (int i = 0; i <= steps; i++)
            {
                float t = length * i / steps;
                float angle = amplitude * Mathf.Sin(2f * Mathf.PI * i / steps);
                Vector3 euler = (baseRotation * Quaternion.Euler(angle, 0f, 0f)).eulerAngles;
                if (i > 0) euler = Unwrap(euler, previous);
                times[i] = t;
                eulers[i] = euler;
                previous = euler;
            }

            AddEulerCurves(clip, path, times, eulers);
        }

        private static void AddConstantPose(AnimationClip clip, GameObject root, Transform bone, float length)
        {
            if (bone == null) return;
            string path = AnimationUtility.CalculateTransformPath(bone, root.transform);
            Vector3 euler = bone.localEulerAngles;
            AddEulerCurves(clip, path, new[] { 0f, length }, new[] { euler, euler });
        }

        private static void AddConstantPosition(AnimationClip clip, GameObject root, Transform bone, float length)
        {
            if (bone == null) return;
            string path = AnimationUtility.CalculateTransformPath(bone, root.transform);
            Vector3 local = bone.localPosition;
            string[] axes = { "x", "y", "z" };
            for (int axis = 0; axis < 3; axis++)
            {
                AnimationCurve curve = LinearCurve(new[] { 0f, length }, new[] { local[axis], local[axis] });
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axes[axis]);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static void AddSpin(AnimationClip clip, GameObject root, Transform wheel, float length)
        {
            if (wheel == null) return;
            string path = AnimationUtility.CalculateTransformPath(wheel, root.transform);
            Vector3 baseEuler = wheel.localEulerAngles;

            const int steps = 12;
            float[] times = new float[steps + 1];
            Vector3[] eulers = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = length * i / steps;
                times[i] = t;
                eulers[i] = new Vector3(baseEuler.x + 360f * t / length, baseEuler.y, baseEuler.z);
            }

            AddEulerCurves(clip, path, times, eulers);
        }

        private static void AddEulerCurves(AnimationClip clip, string path, float[] times, Vector3[] values)
        {
            string[] axes = { "x", "y", "z" };
            for (int axis = 0; axis < 3; axis++)
            {
                float[] axisValues = new float[values.Length];
                for (int i = 0; i < values.Length; i++) axisValues[i] = values[i][axis];

                AnimationCurve curve = LinearCurve(times, axisValues);
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    path, typeof(Transform), "localEulerAnglesRaw." + axes[axis]);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static AnimationCurve LinearCurve(float[] times, float[] values)
        {
            Keyframe[] keys = new Keyframe[times.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(times[i], values[i]);

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].inTangent = i > 0
                    ? (keys[i].value - keys[i - 1].value) / Mathf.Max(1e-5f, keys[i].time - keys[i - 1].time)
                    : 0f;
                keys[i].outTangent = i < keys.Length - 1
                    ? (keys[i + 1].value - keys[i].value) / Mathf.Max(1e-5f, keys[i + 1].time - keys[i].time)
                    : 0f;
            }

            return new AnimationCurve(keys);
        }

        /// <summary>Riporta ogni angolo alla rappresentazione più vicina a quella precedente (evita giri di 360°).</summary>
        private static Vector3 Unwrap(Vector3 euler, Vector3 reference)
        {
            return new Vector3(
                Mathf.Repeat(euler.x - reference.x + 180f, 360f) - 180f + reference.x,
                Mathf.Repeat(euler.y - reference.y + 180f, 360f) - 180f + reference.y,
                Mathf.Repeat(euler.z - reference.z + 180f, 360f) - 180f + reference.z);
        }

        // =====================================================================
        // Asset e verifica
        // =====================================================================

        private static void SaveClip(AnimationClip clip, string path, float length)
        {
            // Le impostazioni vanno applicate dopo le curve: aggiungere una curva ricalcola la durata della clip.
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f;
            settings.stopTime = length;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        private static void BuildController(string path, AnimationClip clip, string stateName)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }

        private static void Validate(string modelPath, AnimationClip clip, float time, params string[] boneNames)
        {
            GameObject root = Clone(modelPath);
            try
            {
                clip.SampleAnimation(root, time);
                foreach (string boneName in boneNames)
                {
                    Transform bone = FindBone(root, boneName);
                    if (bone == null)
                    {
                        Debug.LogWarning("[FKC-ANIM] " + clip.name + ": osso " + boneName + " mancante.");
                        continue;
                    }
                    Debug.Log("[FKC-ANIM] " + clip.name + " t=" + time.ToString("F2") + " " + boneName +
                              " euler=" + bone.localEulerAngles.ToString("F1") +
                              " world=" + bone.position.ToString("F3"));
                }
                Debug.Log("[FKC-ANIM] " + clip.name + ": " +
                          AnimationUtility.GetCurveBindings(clip).Length + " curve.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        private static GameObject Clone(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Modello mancante: " + path);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = prefab.name + "_TMP";
            return instance;
        }

        private static Transform FindBone(GameObject root, string boneName)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                string name = transform.name;
                int colon = name.IndexOf(':');
                if (colon >= 0) name = name.Substring(colon + 1);
                if (string.Equals(name, boneName, StringComparison.Ordinal)) return transform;
            }
            return null;
        }

        private static Transform FindExact(GameObject root, string name)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(transform.name, name, StringComparison.Ordinal)) return transform;
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
