using System;
using UnityEditor;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Custom Inspector di <see cref="ManDownPoseController"/>. Permette di configurare le cinque pose
    /// direttamente dalla Scene View: si posizionano le ossa nella scena, si preme Save Pose e si può
    /// verificare il risultato con Apply Pose. Ogni modifica è registrata con Undo e serializzata nella
    /// scena o nel prefab attualmente aperto.
    /// </summary>
    [CustomEditor(typeof(ManDownPoseController))]
    public sealed class ManDownPoseControllerEditor : Editor
    {
        private const string NO_SKELETON =
            "Assegna prima la radice dello scheletro (Hips) con il pulsante qui sopra.";

        private bool showBones;

        /// <summary>Radice dello scheletro cercata tra i figli dell'NPC su cui è attaccato il componente.</summary>
        public static Transform FindHips(ManDownPoseController controller)
        {
            if (controller == null) return null;

            Transform[] transforms = controller.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
                if (transform.name == "Hips") return transform;

            SkinnedMeshRenderer[] renderers = controller.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            return renderers.Length > 0 ? renderers[0].rootBone : null;
        }

        /// <summary>
        /// Cattura lo scheletro corrente e lo scrive sia come posa iniziale sia come Supine.
        /// Non modifica le trasformazioni delle ossa.
        /// </summary>
        public static bool CaptureInitialSupine(ManDownPoseController controller)
        {
            if (controller == null) return false;

            int boneCount;
            bool allResolved;
            if (!IsSkeletonValid(controller, out boneCount, out allResolved)) return false;

            int group = BeginUndo("Capture initial Supine pose");
            ManDownPoseSnapshot snapshot = controller.CaptureCurrentSnapshot();
            Undo.RecordObject(controller, "Capture initial Supine pose");
            bool initialOk = controller.SetInitialPose(snapshot);
            bool supineOk = controller.SetPoseData(ManDownPose.Supine, snapshot);
            EndUndo(group);
            EditorUtility.SetDirty(controller);
            return initialOk && supineOk;
        }

        /// <summary>Salva la configurazione corrente delle ossa in una posa, senza dialoghi.</summary>
        public static bool SavePoseData(ManDownPoseController controller, ManDownPose pose)
        {
            if (controller == null) return false;

            int boneCount;
            bool allResolved;
            if (!IsSkeletonValid(controller, out boneCount, out allResolved)) return false;

            int group = BeginUndo("Save pose " + ManDownPoseController.GetPoseName(pose));
            ManDownPoseSnapshot snapshot = controller.CaptureCurrentSnapshot();
            Undo.RecordObject(controller, "Save pose " + ManDownPoseController.GetPoseName(pose));
            bool ok = controller.SetPoseData(pose, snapshot);
            EndUndo(group);
            EditorUtility.SetDirty(controller);
            return ok;
        }

        /// <summary>Applica una posa alle ossa, registrando ogni trasformazione in Undo.</summary>
        public static bool ApplyPoseWithUndo(ManDownPoseController controller, ManDownPose pose)
        {
            if (controller == null || !controller.IsConfigured(pose)) return false;

            int group = BeginUndo("Apply pose " + ManDownPoseController.GetPoseName(pose));
            RecordBoneTransforms(controller);
            Undo.RecordObject(controller, "Apply pose " + ManDownPoseController.GetPoseName(pose));
            bool ok = controller.ApplyPose(pose);
            EndUndo(group);
            EditorUtility.SetDirty(controller);
            return ok;
        }

        /// <summary>Ripristina la posa iniziale catturata, registrando ogni trasformazione in Undo.</summary>
        public static bool RestoreInitialWithUndo(ManDownPoseController controller)
        {
            if (controller == null || !controller.HasInitialPose) return false;

            int group = BeginUndo("Restore initial pose");
            RecordBoneTransforms(controller);
            Undo.RecordObject(controller, "Restore initial pose");
            bool ok = controller.RestoreInitialPose();
            EndUndo(group);
            EditorUtility.SetDirty(controller);
            return ok;
        }

        /// <summary>Inizia un gruppo Undo autonomo, così ogni comando dell'Inspector è annullabile da solo.</summary>
        private static int BeginUndo(string name)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return Undo.GetCurrentGroup();
        }

        private static void EndUndo(int group)
        {
            Undo.CollapseUndoOperations(group);
        }

        private static bool IsSkeletonValid(ManDownPoseController controller, out int boneCount, out bool allResolved)
        {
            string error = controller.Validate(out boneCount, out allResolved);
            return string.IsNullOrEmpty(error);
        }

        private static void RecordBoneTransforms(ManDownPoseController controller)
        {
            foreach (Transform bone in controller.GetBoneTransforms())
                if (bone != null) Undo.RecordObject(bone, "Change NPC pose");
        }

        public override void OnInspectorGUI()
        {
            ManDownPoseController controller = (ManDownPoseController)target;
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Configura le pose spostando le ossa nella Scene View (modalità Rotate/Move). " +
                "Save Pose memorizza solo posizione e rotazione locali: la scala non viene mai toccata. " +
                "Le modifiche sono salvate nella scena o nel prefab aperto e sono annullabili con Undo.",
                MessageType.Info);

            EditorGUILayout.LabelField("Scheletro", EditorStyles.boldLabel);
            Transform newRoot = (Transform)EditorGUILayout.ObjectField(
                "Radice (Hips)", controller.SkeletonRoot, typeof(Transform), true);
            if (newRoot != controller.SkeletonRoot)
            {
                BeginUndo("Set skeleton root");
                Undo.RecordObject(controller, "Set skeleton root");
                controller.SetSkeletonRoot(newRoot);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Trova Hips sull'NPC"))
                {
                    Transform hips = FindHips(controller);
                    if (hips != null)
                    {
                        BeginUndo("Find skeleton root");
                        Undo.RecordObject(controller, "Find skeleton root");
                        controller.SetSkeletonRoot(hips);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Scheletro non trovato",
                            "Non è stato trovato un osso chiamato 'Hips' tra i figli dell'NPC.", "OK");
                    }
                }

                if (GUILayout.Button("Cattura Supine iniziale"))
                {
                    bool hasAny = controller.HasInitialPose || controller.IsConfigured(ManDownPose.Supine);
                    if (!hasAny || EditorUtility.DisplayDialog(
                            "Sovrascrivere la posa iniziale?",
                            "La posa iniziale/Supine è già configurata. Vuoi sostituirla con la posa attuale delle ossa?",
                            "Sovrascrivi", "Annulla"))
                    {
                        if (!CaptureInitialSupine(controller))
                            ShowValidationError(controller);
                    }
                }
            }

            int boneCount;
            bool allResolved;
            string error = controller.Validate(out boneCount, out allResolved);
            EditorGUILayout.LabelField("Ossa dello scheletro", boneCount.ToString());
            if (string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox("Scheletro valido: solo le ossa usate dagli SkinnedMeshRenderer vengono memorizzate.", MessageType.None);
            else
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            showBones = EditorGUILayout.Foldout(showBones, "Elenco ossa (solo lettura)", true);
            if (showBones)
            {
                foreach (Transform bone in controller.GetBoneTransforms())
                    EditorGUILayout.LabelField(bone != null ? bone.name : "osso mancante");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose", EditorStyles.boldLabel);
            foreach (ManDownPose pose in Enum.GetValues(typeof(ManDownPose)))
                DrawPose(controller, pose);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!controller.HasInitialPose))
            {
                if (GUILayout.Button("Ripristina posa iniziale (Supine)"))
                {
                    if (!RestoreInitialWithUndo(controller))
                        EditorUtility.DisplayDialog("Ripristino non riuscito", NO_SKELETON, "OK");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPose(ManDownPoseController controller, ManDownPose pose)
        {
            bool configured = controller.IsConfigured(pose);
            string name = ManDownPoseController.GetPoseName(pose);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(name + "  (" + pose + ")", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Stato", configured ? "Configurata" : "Non configurata");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save Pose"))
                    {
                        if (configured && !EditorUtility.DisplayDialog(
                                "Sovrascrivere la posa?",
                                $"La posa '{name}' è già configurata. Vuoi sovrascriverla con la configurazione attuale delle ossa?",
                                "Sovrascrivi", "Annulla"))
                            return;

                        if (!SavePoseData(controller, pose)) ShowValidationError(controller);
                    }

                    using (new EditorGUI.DisabledScope(!configured))
                    {
                        if (GUILayout.Button("Apply Pose"))
                        {
                            if (!ApplyPoseWithUndo(controller, pose))
                                ShowValidationError(controller);
                        }
                    }
                }
            }
        }

        private static void ShowValidationError(ManDownPoseController controller)
        {
            int boneCount;
            bool allResolved;
            string error = controller != null ? controller.Validate(out boneCount, out allResolved) : NO_SKELETON;
            EditorUtility.DisplayDialog("Operazione annullata",
                string.IsNullOrEmpty(error) ? "La posa non è configurata o le ossa non sono più valide." : error,
                "OK");
        }
    }
}
