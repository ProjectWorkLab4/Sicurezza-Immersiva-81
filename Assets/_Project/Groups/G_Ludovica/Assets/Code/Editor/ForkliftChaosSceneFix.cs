#if UNITY_EDITOR
using System;
using G_Ludovica.ForkliftChaos;
using SI81.Interaction;
using SI81.Rig;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G_Ludovica.Editor
{
    /// <summary>
    /// Porta SCN_ForkliftChaos sul layer condiviso: rimuove la locomozione locale, mette lo SpawnPoint,
    /// sostituisce i placeholder con i modelli disponibili e collega gli interattivi al componente Interactable.
    /// </summary>
    public static class ForkliftChaosSceneFix
    {
        public const string ScenePath = "Assets/_Project/Groups/G_Ludovica/Assets/Scenes/SCN_ForkliftChaos.unity";

        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveLocalLocomotion();
            ConfigureSpawnPoint();
            GameObject marco = ReplaceMarco();
            ReplaceOperator();
            // ReplaceColleagues(): disattivato, i colleghi ora li costruisce ForkliftChaosReviewFix (camminano e si fermano).
            GameObject rescueTeam = CreateRescueTeam();
            ReplaceForklifts();
            ConfigureInteractables();
            BindScenario(marco, rescueTeam);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                Debug.LogError("[FKC-FIX] Salvataggio scena fallito.");
            AssetDatabase.SaveAssets();
            Debug.Log("[FKC-FIX] Scena aggiornata.");
        }

        // =====================================================================
        // Layer condiviso
        // =====================================================================

        private static void RemoveLocalLocomotion()
        {
            GameObject locomotion = Find("MNG_Locomotion");
            if (locomotion == null)
            {
                Debug.Log("[FKC-FIX] MNG_Locomotion già assente.");
                return;
            }
            UnityEngine.Object.DestroyImmediate(locomotion);
            Debug.Log("[FKC-FIX] MNG_Locomotion rimosso (la locomozione arriva dal rig condiviso).");
        }

        private static void ConfigureSpawnPoint()
        {
            GameObject spawn = Find("MNG_SpawnPoint");
            if (spawn == null)
            {
                Debug.LogError("[FKC-FIX] MNG_SpawnPoint non trovato.");
                return;
            }

            spawn.transform.SetPositionAndRotation(
                new Vector3(29.33381f, 0.523f, 5.04f), Quaternion.identity);

            if (spawn.GetComponent<SpawnPoint>() == null) spawn.AddComponent<SpawnPoint>();
            Debug.Log("[FKC-FIX] MNG_SpawnPoint a " + spawn.transform.position.ToString("F3") + " con SpawnPoint.");
        }

        // =====================================================================
        // Modelli umani
        // =====================================================================

        private static GameObject ReplaceMarco()
        {
            GameObject old = Find("PLACEHOLDER_Marco_MC01");
            if (old == null)
            {
                Debug.LogError("[FKC-FIX] PLACEHOLDER_Marco_MC01 non trovato.");
                return null;
            }

            Vector3 p = old.transform.position;
            Transform parent = old.transform.parent;
            Avatar avatar = ForkliftChaosAnimationBuilder.LoadAvatar(ForkliftChaosAnimationBuilder.WorkerPath);

            // Marco è sdraiato supino: testa verso -Z, schiena a terra.
            GameObject marco = InstantiateModel(
                ForkliftChaosAnimationBuilder.WorkerPath, "MDL_Marco_MC01", parent,
                new Vector3(p.x, 0.2f, p.z + 0.99f), Quaternion.Euler(-90f, 0f, 0f),
                ForkliftChaosAnimationBuilder.ControllerMarco, avatar);

            if (marco != null)
            {
                PlaceOnFloor(marco, 0.01f);

                BoxCollider body = marco.AddComponent<BoxCollider>();
                body.center = new Vector3(0f, 0.98f, 0f);
                body.size = new Vector3(0.8f, 1.95f, 0.6f);

                ForkliftChaosInteractable adapter = marco.AddComponent<ForkliftChaosInteractable>();
                adapter.ConfigureLook(ForkliftChaosAction.ObserveMarco, "Guarda Marco", 1.1f);

                Debug.Log("[FKC-FIX] Marco sostituito: " + marco.transform.position.ToString("F3") +
                          " bounds=" + GetBounds(marco).size.ToString("F3"));
            }

            UnityEngine.Object.DestroyImmediate(old);
            return marco;
        }

        private static void ReplaceOperator()
        {
            GameObject old = Find("PLACEHOLDER_Operatore_OP01");
            if (old == null)
            {
                Debug.Log("[FKC-FIX] PLACEHOLDER_Operatore_OP01 assente.");
                return;
            }

            Vector3 p = old.transform.position;
            Transform parent = old.transform.parent;
            Avatar avatar = ForkliftChaosAnimationBuilder.LoadAvatar(ForkliftChaosAnimationBuilder.RescuerPath);

            GameObject model = InstantiateModel(
                ForkliftChaosAnimationBuilder.RescuerPath, "MDL_Operatore_OP01", parent,
                new Vector3(p.x, 0f, p.z), Quaternion.identity,
                ForkliftChaosAnimationBuilder.ControllerRescuer, avatar);

            if (model != null) AddBodyCollider(model);
            UnityEngine.Object.DestroyImmediate(old);
            Debug.Log("[FKC-FIX] Operatore sostituito.");
        }

        private static void ReplaceColleagues()
        {
            GameObject group = Find("GRP_Colleghi_CCS1");
            if (group == null)
            {
                Debug.Log("[FKC-FIX] GRP_Colleghi_CCS1 assente.");
                return;
            }

            Avatar avatar = ForkliftChaosAnimationBuilder.LoadAvatar(ForkliftChaosAnimationBuilder.RescuerPath);
            string[] names = { "MDL_Collega_CCS1", "MDL_Collega_CCS2" };
            int index = 0;

            foreach (string name in names)
            {
                GameObject old = Find(name);
                if (old == null) continue;

                Vector3 p = old.transform.position;
                Transform parent = old.transform.parent;

                GameObject model = InstantiateModel(
                    ForkliftChaosAnimationBuilder.RescuerPath, name, parent,
                    new Vector3(p.x, 0f, p.z), old.transform.rotation,
                    ForkliftChaosAnimationBuilder.ControllerRescuer, avatar);

                if (model != null) AddBodyCollider(model);
                UnityEngine.Object.DestroyImmediate(old);
                index++;
            }

            Debug.Log("[FKC-FIX] Colleghi sostituiti: " + index + " (gruppo inattivo fino all'ingresso).");
        }

        private static GameObject CreateRescueTeam()
        {
            if (Find("GRP_Soccorsi_SC01") != null) return Find("GRP_Soccorsi_SC01");

            GameObject marker = Find("MARKER_SpawnColleghi_CCS1");
            Vector3 origin = marker != null
                ? marker.transform.position + new Vector3(0f, 0f, -3f)
                : new Vector3(21f, 0f, 2f);

            GameObject group = new GameObject("GRP_Soccorsi_SC01");
            group.transform.SetPositionAndRotation(origin, Quaternion.identity);

            Avatar avatar = ForkliftChaosAnimationBuilder.LoadAvatar(ForkliftChaosAnimationBuilder.RescuerPath);
            for (int i = 0; i < 2; i++)
            {
                GameObject model = InstantiateModel(
                    ForkliftChaosAnimationBuilder.RescuerPath,
                    "MDL_Soccorso_SC0" + (i + 1), group.transform,
                    origin + new Vector3(i == 0 ? -0.6f : 0.6f, 0f, i == 0 ? 0f : 0.4f),
                    Quaternion.identity,
                    ForkliftChaosAnimationBuilder.ControllerRescuer, avatar);
                if (model != null) AddBodyCollider(model);
            }

            group.SetActive(false);
            Debug.Log("[FKC-FIX] Gruppo soccorsi creato a " + origin.ToString("F2") + " (inattivo).");
            return group;
        }

        // =====================================================================
        // Muletti
        // =====================================================================

        private static void ReplaceForklifts()
        {
            Avatar avatar = ForkliftChaosAnimationBuilder.LoadAvatar(ForkliftChaosAnimationBuilder.ForkliftPath);

            foreach (string holderName in new[] { "MDL_Muletto1_VH01", "MDL_Muletto2_MS01" })
            {
                GameObject holder = Find(holderName);
                if (holder == null)
                {
                    Debug.LogError("[FKC-FIX] " + holderName + " non trovato.");
                    continue;
                }

                Transform old = holder.transform.Find("MESH_Placeholder");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                // Idempotente: rimuove modelli già presenti, altrimenti ogni riesecuzione ne aggiunge un altro.
                for (int i = holder.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = holder.transform.GetChild(i);
                    if (child.name.EndsWith("_Model", StringComparison.Ordinal))
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                GameObject model = InstantiateModel(
                    ForkliftChaosAnimationBuilder.ForkliftPath, holderName + "_Model", holder.transform,
                    holder.transform.position, holder.transform.rotation * Quaternion.Euler(0f, 180f, 0f),
                    ForkliftChaosAnimationBuilder.ControllerForklift, avatar);

                if (model == null) continue;

                Animator animator = model.GetComponent<Animator>();
                ForkliftAnimatorDriver driver = model.GetComponent<ForkliftAnimatorDriver>();
                if (driver == null) driver = model.AddComponent<ForkliftAnimatorDriver>();
                driver.Configure(animator, holder.transform);

                BoxCollider box = holder.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Bounds local = GetLocalBounds(model, holder.transform);
                    box.center = local.center;
                    box.size = local.size + new Vector3(0.15f, 0.1f, 0.15f);
                }

                Debug.Log("[FKC-FIX] " + holderName + " usa Muletto.fbx, collider=" +
                          (box != null ? box.size.ToString("F2") : "assente") + ".");
            }
        }

        // =====================================================================
        // Interazioni condivise
        // =====================================================================

        private static void ConfigureInteractables()
        {
            ForkliftChaosInteractable[] adapters = UnityEngine.Object.FindObjectsByType<ForkliftChaosInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int count = 0;
            foreach (ForkliftChaosInteractable adapter in adapters)
            {
                if (adapter == null) continue;

                GameObject go = adapter.gameObject;
                Interactable interactable = go.GetComponent<Interactable>();
                if (interactable == null) interactable = go.AddComponent<Interactable>();

                SerializedObject serialized = new SerializedObject(adapter);
                SerializedProperty useGrab = serialized.FindProperty("useGrab");
                SerializedProperty lookOnly = serialized.FindProperty("lookOnly");

                int action = (int)adapter.Action;
                bool grab = (useGrab != null && useGrab.boolValue) || action == 5 || action == 8;
                bool look = lookOnly != null && lookOnly.boolValue;

                if (useGrab != null && useGrab.boolValue != grab)
                {
                    useGrab.boolValue = grab;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                serialized.Dispose();

                interactable.Selectable = !look && !grab;
                interactable.Grabbable = !look && grab;
                interactable.FollowsHand = !look && grab;
                count++;
            }

            Debug.Log("[FKC-FIX] Interactable configurato su " + count + " oggetti.");
        }

        private static void BindScenario(GameObject marco, GameObject rescueTeam)
        {
            ForkliftChaosScenario scenario = UnityEngine.Object.FindFirstObjectByType<ForkliftChaosScenario>(
                FindObjectsInactive.Include);
            if (scenario == null)
            {
                Debug.LogError("[FKC-FIX] ForkliftChaosScenario non trovato.");
                return;
            }

            SerializedObject serialized = new SerializedObject(scenario);
            SetReference(serialized, "spawnPoint", null);
            if (marco != null)
            {
                SetReference(serialized, "marco", marco);
                SetReference(serialized, "marcoAnimator", marco.GetComponent<Animator>());
            }
            SetReference(serialized, "rescueTeam", rescueTeam);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Dispose();
            Debug.Log("[FKC-FIX] Riferimenti scenario aggiornati.");
        }

        private static void SetReference(SerializedObject serialized, string property, UnityEngine.Object value)
        {
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null)
            {
                Debug.LogWarning("[FKC-FIX] Campo non trovato: " + property);
                return;
            }
            found.objectReferenceValue = value;
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        private static GameObject InstantiateModel(string modelPath, string name, Transform parent,
            Vector3 worldPosition, Quaternion worldRotation, string controllerPath, Avatar avatar)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogError("[FKC-FIX] Modello mancante: " + modelPath);
                return null;
            }

            // Non è un'istanza di prefab: evita override fragili sull'FBX appena reimportato.
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = name;
            if (parent != null) instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(worldPosition, worldRotation);
            instance.transform.localScale = Vector3.one;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            return instance;
        }

        private static void AddBodyCollider(GameObject model)
        {
            CapsuleCollider capsule = model.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.28f;
        }

        private static void PlaceOnFloor(GameObject go, float clearance)
        {
            Bounds bounds = GetBounds(go);
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning("[FKC-FIX] Bounds non disponibili per " + go.name);
                return;
            }
            go.transform.position += new Vector3(0f, clearance - bounds.min.y, 0f);
        }

        private static Bounds GetBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Bounds GetLocalBounds(GameObject go, Transform space)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            Bounds bounds = ToLocal(renderers[0].bounds, space);
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(ToLocal(renderers[i].bounds, space));
            return bounds;
        }

        private static Bounds ToLocal(Bounds world, Transform space)
        {
            Vector3 center = space.InverseTransformPoint(world.center);
            Vector3 extents = space.InverseTransformVector(world.extents);
            extents = new Vector3(Mathf.Abs(extents.x), Mathf.Abs(extents.y), Mathf.Abs(extents.z));
            return new Bounds(center, extents * 2f);
        }

        private static GameObject Find(string name)
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform transform in all)
                if (string.Equals(transform.name, name, StringComparison.Ordinal)) return transform.gameObject;
            return null;
        }
    }
}
#endif
