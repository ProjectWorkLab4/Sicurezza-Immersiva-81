using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Verifica e importa le TMP Essential Resources (TMP è incluso in com.unity.ugui 2.0: NON installare
    /// com.unity.textmeshpro). L'importazione crea Assets/TextMesh Pro/.
    /// </summary>
    public static class TmpEssentials
    {
        /// <summary>Percorso dell'asset TMP Settings creato dalle Essential Resources.</summary>
        public const string SETTINGS_PATH = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>
        /// Indica se le Essential Resources sono già nel progetto.
        /// </summary>
        /// <returns>true se TMP Settings esiste.</returns>
        public static bool IsImported()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_Settings>(SETTINGS_PATH) != null;
        }

        /// <summary>
        /// Se le Essential Resources mancano le importa senza finestre e, a importazione finita,
        /// richiama <paramref name="onImported"/>.
        /// </summary>
        /// <param name="onImported">Azione da rieseguire dopo l'importazione.</param>
        /// <returns>true se erano già presenti (si può procedere subito).</returns>
        public static bool EnsureImported(Action onImported)
        {
            if (IsImported()) return true;

            Debug.Log("[SI81 UI] TMP Essential Resources mancanti: importazione in corso, il builder riparte al termine.");
            AssetDatabase.ImportPackageCallback done = null;
            done = packageName =>
            {
                AssetDatabase.importPackageCompleted -= done;
                if (onImported != null) EditorApplication.delayCall += () => onImported();
            };
            AssetDatabase.importPackageCompleted += done;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            return false;
        }
    }
}
