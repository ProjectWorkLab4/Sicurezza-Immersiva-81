using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Assegna a runtime la camera della testa (UIScreenManager.SetHead, altrimenti Camera.main) come camera di un Canvas World Space (serve al GraphicRaycaster del mouse in Editor
    /// e agli eventi UI). La camera del rig non è nel prefab, quindi il riferimento non può essere salvato.
    /// Se la camera principale cambia (es. caricamento del rig dopo la UI) riprova finché non la trova.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class WorldCanvasCamera : MonoBehaviour
    {
        // Canvas dello stesso GameObject.
        private Canvas canvas;

        /// <summary>
        /// Recupera il Canvas e prova subito ad assegnare la camera.
        /// </summary>
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            TryAssign();
        }

        /// <summary>
        /// Se la camera non è ancora assegnata (o è stata distrutta) riprova; poi si spegne finché serve.
        /// </summary>
        private void LateUpdate()
        {
            if (canvas.worldCamera != null)
            {
                enabled = false;
                return;
            }
            TryAssign();
        }

        /// <summary>
        /// Riattiva il controllo: utile se la camera del rig viene sostituita.
        /// </summary>
        public void Refresh()
        {
            enabled = true;
            TryAssign();
        }

        /// <summary>
        /// Assegna al Canvas la camera della testa di MNG_UI (<see cref="UIScreenManager.SetHead"/>); se manca usa
        /// Camera.main con un avviso (una sola volta).
        /// </summary>
        private void TryAssign()
        {
            if (canvas == null) return;
            Camera assigned = UIScreenManager.Instance != null ? UIScreenManager.Instance.HeadCamera : null;
            Camera camera = UIRigLocator.ResolveCamera(assigned, "WorldCanvasCamera");
            if (camera != null) canvas.worldCamera = camera;
        }
    }
}
