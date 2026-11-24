using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Prima di ogni build Android spegne i profili OpenXR che rompono i controller sul Quest 3:
    /// - mani tracciate (HandInteractionProfile, MicrosoftHandInteraction): il progetto è "solo controller" (CLAUDE.md §3),
    ///   con i profili accesi le mani diventano puntatori UI fermi;
    /// - controller "staccati" (Detached...): il loro percorso thumbrest/force non è supportato dal runtime e fa rifiutare
    ///   tutti i binding del profilo Touch Plus, quindi Unity non riceve né posa né grilletto dei controller (log del 19/11).
    /// Qualche passo della build Meta/OpenXR li riaccende: questo controllo gira per ULTIMO (callbackOrder massimo),
    /// quindi vale per l'APK che esce. Scrive in Console cosa ha cambiato.
    /// Usa la reflection per non legare l'assembly SI81.UI.Editor al pacchetto OpenXR.
    /// </summary>
    public sealed class ControllersOnlyBuildGuard : IPreprocessBuildWithReport
    {
        /// <summary>Nomi dei tipi delle feature OpenXR da tenere spente.</summary>
        private static readonly string[] HAND_FEATURES =
        {
            "HandInteractionProfile", "MicrosoftHandInteraction",
            "DetachedMetaQuestTouchPlusControllerProfile", "DetachedMetaQuestTouchProControllerProfile", "DetachedOculusTouchControllerProfile"
        };

        /// <summary>Ultimo tra i passi di preparazione della build.</summary>
        public int callbackOrder => int.MaxValue;

        /// <summary>
        /// Spegne i profili delle mani per Android prima che la build impacchetti le impostazioni OpenXR.
        /// </summary>
        /// <param name="report">Rapporto della build (si usa solo la piattaforma).</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            string changed = DisableHandFeatures(BuildTargetGroup.Android);
            if (!string.IsNullOrEmpty(changed))
                Debug.Log($"[SI81 Build] Progetto solo controller: spenti i profili OpenXR mani/controller staccati per Android ({changed}).");
        }

        /// <summary>
        /// Menu manuale per lo stesso controllo, senza fare la build.
        /// </summary>
        [MenuItem("SI81/Build/Spegni profili mani e controller staccati (Android)", priority = 200)]
        private static void DisableFromMenu()
        {
            string changed = DisableHandFeatures(BuildTargetGroup.Android);
            Debug.Log(string.IsNullOrEmpty(changed)
                ? "[SI81 Build] Profili OpenXR mani/controller staccati già spenti per Android."
                : $"[SI81 Build] Spenti i profili OpenXR mani/controller staccati per Android ({changed}).");
        }

        /// <summary>
        /// Spegne le feature OpenXR delle mani per il gruppo indicato e salva.
        /// </summary>
        /// <param name="group">Gruppo di piattaforme (Android).</param>
        /// <returns>Nomi delle feature spente (vuoto se erano già spente o se OpenXR non c'è).</returns>
        private static string DisableHandFeatures(BuildTargetGroup group)
        {
            Type settingsType = Type.GetType("UnityEngine.XR.OpenXR.OpenXRSettings, Unity.XR.OpenXR");
            // Il metodo ha più versioni: si chiede esplicitamente quella con un BuildTargetGroup.
            MethodInfo getSettings = settingsType?.GetMethod("GetSettingsForBuildTargetGroup", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(BuildTargetGroup) }, null);
            object settings = getSettings?.Invoke(null, new object[] { group });
            // GetFeatures() ha anche una versione generica senza parametri: si sceglie quella normale.
            MethodInfo getFeatures = null;
            if (settings != null)
                foreach (MethodInfo m in settings.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    if (m.Name == "GetFeatures" && !m.IsGenericMethodDefinition && m.GetParameters().Length == 0) { getFeatures = m; break; }
            if (!(getFeatures?.Invoke(settings, null) is IEnumerable features)) return string.Empty;

            string changed = string.Empty;
            foreach (object feature in features)
            {
                if (feature == null || Array.IndexOf(HAND_FEATURES, feature.GetType().Name) < 0) continue;
                PropertyInfo enabled = feature.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
                if (enabled == null || !(bool)enabled.GetValue(feature)) continue;
                enabled.SetValue(feature, false);
                if (feature is UnityEngine.Object asset) EditorUtility.SetDirty(asset);
                changed += (changed.Length > 0 ? ", " : string.Empty) + feature.GetType().Name;
            }
            if (changed.Length > 0)
            {
                if (settings is UnityEngine.Object settingsAsset) EditorUtility.SetDirty(settingsAsset);
                AssetDatabase.SaveAssets();
            }
            return changed;
        }
    }
}
