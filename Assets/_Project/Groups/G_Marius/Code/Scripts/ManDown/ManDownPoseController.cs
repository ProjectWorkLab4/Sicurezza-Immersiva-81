using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Gestisce le cinque pose statiche dell'NPC usando solo le trasformazioni locali delle ossa dello
    /// scheletro già presente nella scena. Non usa Animation Clip, Animator Controller, ragdoll o interpolazioni:
    /// l'applicazione è immediata e la scala delle ossa non viene mai toccata.
    ///
    /// La configurazione delle pose avviene nell'Editor con <c>ManDownPoseControllerEditor</c>; qui resta soltanto
    /// la logica runtime: cattura, validazione, applicazione e ripristino della posa iniziale. Le pose sono
    /// serializzate nel componente, quindi sopravvivono al riavvio di Unity.
    ///
    /// Vengono considerate ossa soltanto quelle referenziate dagli SkinnedMeshRenderer dell'NPC e discendenti
    /// della radice dello scheletro: mesh, trigger, collider e altri oggetti estranei non vengono memorizzati.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/Pose Controller")]
    public sealed class ManDownPoseController : MonoBehaviour
    {
        /// <summary>Percorso riservato alla radice dello scheletro, che non può essere risolta con Transform.Find.</summary>
        private const string ROOT_BONE_PATH = "@root";

        [Header("Scheletro")]
        [Tooltip("Radice dello scheletro (Hips). Va individuata a partire dall'NPC con il comando dell'Editor.")]
        [SerializeField] private Transform skeletonRoot;

        [Header("Pose salvate (configurate dall'Editor)")]
        [SerializeField] private ManDownPoseData initialPose = new ManDownPoseData();
        [SerializeField] private ManDownPoseData supine = new ManDownPoseData();
        [SerializeField] private ManDownPoseData recovery = new ManDownPoseData();
        [SerializeField] private ManDownPoseData legsRaised = new ManDownPoseData();
        [SerializeField] private ManDownPoseData prone = new ManDownPoseData();
        [SerializeField] private ManDownPoseData seated = new ManDownPoseData();

        private readonly HashSet<ManDownPose> warnedMissingPoses = new HashSet<ManDownPose>();
        private ManDownPose currentPose = ManDownPose.Supine;

        /// <summary>Radice dello scheletro usata per risolvere i percorsi delle ossa.</summary>
        public Transform SkeletonRoot => skeletonRoot;

        /// <summary>Ultima posa applicata da questo componente. È uno stato visivo, non la posizione logica dell'NPC.</summary>
        public ManDownPose CurrentPose => currentPose;

        /// <summary>Numero di ossa dello scheletro rilevate, o salvate nella posa iniziale se disponibile.</summary>
        public int BoneCount => initialPose != null && initialPose.bones.Count > 0
            ? initialPose.bones.Count
            : DiscoverBonePaths().Count;

        /// <summary>Nome italiano di una posa, per log e Inspector.</summary>
        public static string GetPoseName(ManDownPose pose)
        {
            switch (pose)
            {
                case ManDownPose.Supine: return "Supina";
                case ManDownPose.Recovery: return "Laterale di sicurezza";
                case ManDownPose.LegsRaised: return "Gambe sollevate";
                case ManDownPose.Prone: return "Prona";
                case ManDownPose.Seated: return "Seduta";
                default: return pose.ToString();
            }
        }

        private void Awake()
        {
            currentPose = ManDownPose.Supine;
        }

        // =================================================================
        // API runtime
        // =================================================================

        /// <summary>True se la posa è stata configurata e contiene almeno un'osso.</summary>
        public bool IsConfigured(ManDownPose pose)
        {
            ManDownPoseData data = GetPoseData(pose);
            return data != null && data.configured && data.bones.Count > 0;
        }

        /// <summary>True se la posa iniziale è stata catturata dall'Editor.</summary>
        public bool HasInitialPose => initialPose != null && initialPose.configured && initialPose.bones.Count > 0;

        /// <summary>
        /// Applica una posa in modo immediato. Se la posa non è configurata o un'osso non è più presente,
        /// non modifica alcuna trasformazione, segnala il problema una sola volta e restituisce false.
        /// </summary>
        public bool ApplyPose(ManDownPose pose)
        {
            ManDownPoseData data = GetPoseData(pose);
            if (data == null || !data.configured || data.bones.Count == 0)
            {
                WarnMissingOnce(pose);
                return false;
            }

            if (!ApplyData(data)) return false;

            currentPose = pose;
            warnedMissingPoses.Remove(pose);
            return true;
        }

        /// <summary>
        /// Ripristina integralmente la posa iniziale catturata prima di qualunque modifica. Non tocca la scala.
        /// </summary>
        public bool RestoreInitialPose()
        {
            if (!HasInitialPose)
            {
                Debug.LogWarning("[SI81 ManDown] Posa iniziale non catturata: ripristino annullato.", this);
                return false;
            }

            if (!ApplyData(initialPose)) return false;

            currentPose = ManDownPose.Supine;
            return true;
        }

        /// <summary>
        /// Cattura le trasformazioni locali correnti di tutte le ossa dello scheletro. Non modifica l'NPC.
        /// </summary>
        /// <returns>Snapshot con le ossa ordinate per percorso; vuoto se lo scheletro non è valido.</returns>
        public ManDownPoseSnapshot CaptureCurrentSnapshot()
        {
            var snapshot = new ManDownPoseSnapshot();
            foreach (string path in DiscoverBonePaths())
            {
                Transform bone = ResolveBone(path);
                if (bone == null) continue;
                snapshot.Bones.Add(new ManDownBonePose(path, bone.localPosition, bone.localRotation));
            }
            return snapshot;
        }

        /// <summary>
        /// Verifica lo scheletro prima di un salvataggio dall'Editor. Restituisce una stringa vuota se tutto è valido.
        /// </summary>
        public string Validate(out int boneCount, out bool allResolved)
        {
            boneCount = 0;
            allResolved = false;

            if (skeletonRoot == null) return "Radice dello scheletro (Hips) non assegnata.";

            List<string> paths = DiscoverBonePaths();
            boneCount = paths.Count;
            if (paths.Count == 0)
                return "Nessun osso trovato sotto la radice: controllare gli SkinnedMeshRenderer dell'NPC.";

            allResolved = true;
            foreach (string path in paths)
                if (ResolveBone(path) == null) allResolved = false;

            return allResolved ? string.Empty : "Alcuni percorsi delle ossa non sono più risolubili.";
        }

        /// <summary>Trasformazioni attualmente risolte, utili all'Editor per registrare l'Undo prima di applicare una posa.</summary>
        public Transform[] GetBoneTransforms()
        {
            List<string> paths = initialPose != null && initialPose.bones.Count > 0
                ? initialPose.bones.ConvertAll(b => b.path)
                : DiscoverBonePaths();

            var result = new List<Transform>(paths.Count);
            foreach (string path in paths)
            {
                Transform bone = ResolveBone(path);
                if (bone != null) result.Add(bone);
            }
            return result.ToArray();
        }

        /// <summary>Azzera la segnalazione di pose mancanti: richiamato a ogni nuova missione.</summary>
        public void ResetMissingPoseWarnings()
        {
            warnedMissingPoses.Clear();
        }

        // =================================================================
        // Scrittura dei dati serializzati (usata dall'Editor, con Undo)
        // =================================================================

        /// <summary>Imposta la radice dello scheletro a partire dall'NPC.</summary>
        public bool SetSkeletonRoot(Transform root)
        {
            if (root == null) return false;
            skeletonRoot = root;
            warnedMissingPoses.Clear();
            return true;
        }

        /// <summary>Scrive una posa configurata. Rifiuta snapshot vuoti o con ossa non risolvibili.</summary>
        public bool SetPoseData(ManDownPose pose, ManDownPoseSnapshot snapshot)
        {
            if (!IsValidSnapshot(snapshot)) return false;

            ManDownPoseData data = GetPoseData(pose);
            if (data == null) return false;

            data.configured = true;
            data.bones = CopyBones(snapshot);
            return true;
        }

        /// <summary>Scrive la posa iniziale. Rifiuta snapshot vuoti o con ossa non risolvibili.</summary>
        public bool SetInitialPose(ManDownPoseSnapshot snapshot)
        {
            if (!IsValidSnapshot(snapshot)) return false;

            initialPose = new ManDownPoseData { configured = true, bones = CopyBones(snapshot) };
            return true;
        }

        /// <summary>Svuota una posa (usata dall'Editor per annullare una configurazione di prova).</summary>
        public void ClearPoseData(ManDownPose pose)
        {
            ManDownPoseData data = GetPoseData(pose);
            if (data == null) return;
            data.configured = false;
            data.bones.Clear();
        }

        // =================================================================
        // Interni
        // =================================================================

        private ManDownPoseData GetPoseData(ManDownPose pose)
        {
            switch (pose)
            {
                case ManDownPose.Supine:
                    return supine ?? (supine = new ManDownPoseData());
                case ManDownPose.Recovery:
                    return recovery ?? (recovery = new ManDownPoseData());
                case ManDownPose.LegsRaised:
                    return legsRaised ?? (legsRaised = new ManDownPoseData());
                case ManDownPose.Prone:
                    return prone ?? (prone = new ManDownPoseData());
                case ManDownPose.Seated:
                    return seated ?? (seated = new ManDownPoseData());
                default:
                    return null;
            }
        }

        private bool IsValidSnapshot(ManDownPoseSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Count == 0 || skeletonRoot == null) return false;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ManDownBonePose bone in snapshot.Bones)
            {
                if (string.IsNullOrEmpty(bone.path)) return false;
                if (ResolveBone(bone.path) == null) return false;
                if (!seen.Add(bone.path)) return false;
            }
            return true;
        }

        private static List<ManDownBonePose> CopyBones(ManDownPoseSnapshot snapshot)
        {
            var copy = new List<ManDownBonePose>(snapshot.Count);
            foreach (ManDownBonePose bone in snapshot.Bones)
                copy.Add(new ManDownBonePose(bone.path, bone.localPosition, bone.localRotation));
            return copy;
        }

        private bool ApplyData(ManDownPoseData data)
        {
            if (data == null || data.bones.Count == 0) return false;

            var bones = new Transform[data.bones.Count];
            for (int i = 0; i < data.bones.Count; i++)
            {
                bones[i] = ResolveBone(data.bones[i].path);
                if (bones[i] == null)
                {
                    Debug.LogWarning($"[SI81 ManDown] Posa non applicata: l'osso '{data.bones[i].path}' non esiste più.", this);
                    return false;
                }
            }

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = data.bones[i].localPosition;
                bones[i].localRotation = data.bones[i].localRotation;
            }
            return true;
        }

        private void WarnMissingOnce(ManDownPose pose)
        {
            if (!warnedMissingPoses.Add(pose)) return;
            Debug.LogWarning(
                $"[SI81 ManDown] La posa '{GetPoseName(pose)}' non è configurata: la scelta logica viene registrata, " +
                "ma la rappresentazione visiva dell'NPC resta invariata.", this);
        }

        private Transform FindNpcRoot()
        {
            Transform candidate = skeletonRoot != null ? skeletonRoot.parent : null;
            while (candidate != null)
            {
                if (candidate.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return candidate;
                candidate = candidate.parent;
            }
            return null;
        }

        /// <summary>
        /// Ossa referenziate dagli SkinnedMeshRenderer e discendenti dalla radice. Esclude esplicitamente
        /// mesh, trigger, collider e qualunque oggetto non usato dallo skinning (per esempio MESH_MouthZone).
        /// </summary>
        private List<string> DiscoverBonePaths()
        {
            var result = new List<string>();
            Transform npcRoot = FindNpcRoot();
            if (npcRoot == null) return result;

            var unique = new HashSet<string>(StringComparer.Ordinal);
            SkinnedMeshRenderer[] renderers = npcRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Transform[] bones = renderer.bones;
                if (bones == null) continue;
                foreach (Transform bone in bones)
                {
                    if (bone == null) continue;
                    string path = BuildPath(bone, skeletonRoot);
                    if (!string.IsNullOrEmpty(path)) unique.Add(path);
                }
            }

            result.AddRange(unique);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static string BuildPath(Transform bone, Transform root)
        {
            if (bone == null || root == null) return string.Empty;
            if (bone == root) return ROOT_BONE_PATH;

            var parts = new List<string>();
            Transform current = bone;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            if (current != root) return string.Empty;

            parts.Reverse();
            return string.Join("/", parts);
        }

        private Transform ResolveBone(string path)
        {
            if (skeletonRoot == null || string.IsNullOrEmpty(path)) return null;
            if (path == ROOT_BONE_PATH) return skeletonRoot;
            return skeletonRoot.Find(path);
        }
    }
}
