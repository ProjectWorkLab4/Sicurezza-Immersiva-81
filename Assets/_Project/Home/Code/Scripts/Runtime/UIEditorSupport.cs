using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Supporto solo Editor per i componenti SI81 che si riapplicano quando si cambia un campo nell'Inspector.
    /// In OnValidate Unity non permette di attivare oggetti né di cambiare i RectTransform: l'applicazione va rimandata
    /// a fine aggiornamento. Questo è l'UNICO posto con quel codice (prima era copiato in ogni componente).
    /// Fuori dall'Editor la classe è vuota e le chiamate (anch'esse sotto #if UNITY_EDITOR) spariscono.
    /// </summary>
    internal static class UIEditorSupport
    {
#if UNITY_EDITOR
        /// <summary>
        /// Rimanda <paramref name="apply"/> a fine aggiornamento dell'Editor. Ignorato in Play Mode e sugli asset prefab non
        /// aperti (non sono oggetti di scena). Da chiamare da OnValidate.
        /// </summary>
        /// <param name="component">Componente che si riapplica.</param>
        /// <param name="apply">Azione da eseguire (es. il metodo Apply del componente).</param>
        internal static void ApplyDeferred(Component component, Action apply)
        {
            if (Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(component)) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (component != null) apply();
            };
        }
#endif
    }
}
