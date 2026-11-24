using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Trova la testa (camera) del giocatore per la UI. Il rig condiviso deve assegnarla a MNG_UI con
    /// <see cref="UIScreenManager.SetHead"/> (e l'origine del tracciamento con <see cref="UIScreenManager.SetTrackingOrigin"/>):
    /// solo se manca viene usata Camera.main, con UN avviso in Console. Un solo posto per questa regola, usato da
    /// UIScreenManager, UIInputSetup e WorldCanvasCamera.
    /// </summary>
    public static class UIRigLocator
    {
        // true dopo il primo avviso (uno solo per sessione di gioco).
        private static bool fallbackWarned;

        /// <summary>
        /// Azzera l'avviso all'avvio di ogni sessione di gioco (utile con il Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            fallbackWarned = false;
        }

        /// <summary>
        /// Restituisce la testa assegnata; se manca usa Camera.main e scrive un avviso (una sola volta).
        /// </summary>
        /// <param name="assigned">Testa assegnata esplicitamente (può essere null).</param>
        /// <param name="who">Nome del chiamante, per l'avviso (es. "MNG_UI").</param>
        /// <returns>Il Transform della testa, o null se non c'è nemmeno Camera.main.</returns>
        public static Transform ResolveHead(Transform assigned, string who)
        {
            if (assigned != null) return assigned;
            Camera main = Camera.main;
            if (main == null) return null;
            WarnFallbackOnce(who);
            return main.transform;
        }

        /// <summary>
        /// Restituisce la camera assegnata; se manca usa Camera.main e scrive un avviso (una sola volta).
        /// </summary>
        /// <param name="assigned">Camera assegnata esplicitamente (può essere null).</param>
        /// <param name="who">Nome del chiamante, per l'avviso.</param>
        /// <returns>La camera, o null.</returns>
        public static Camera ResolveCamera(Camera assigned, string who)
        {
            if (assigned != null) return assigned;
            Camera main = Camera.main;
            if (main != null) WarnFallbackOnce(who);
            return main;
        }

        /// <summary>
        /// Scrive l'avviso "uso Camera.main" la prima volta.
        /// </summary>
        /// <param name="who">Nome del chiamante.</param>
        private static void WarnFallbackOnce(string who)
        {
            if (fallbackWarned) return;
            fallbackWarned = true;
            Debug.LogWarning($"[MNG_UI] {who}: nessuna testa assegnata, uso Camera.main. Il rig condiviso deve chiamare " +
                             "UIScreenManager.Instance.SetHead(camera) e SetTrackingOrigin(origine) appena è pronto (SCN_Core).");
        }
    }
}
