using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Posizionamento comune dei pannelli world-locked davanti al giocatore (HUD_DecisionPanel 1,2 m, HUD_OutcomePanel 1,5 m)
    /// e ricerca della testa. Il pannello viene piazzato UNA volta all'apertura e poi resta fermo nel mondo (nessun head-lock,
    /// CLAUDE.md §4.11). Nessuna allocazione.
    /// </summary>
    public static class WorldPlacement
    {
        /// <summary>
        /// Restituisce la testa del giocatore: quella assegnata, altrimenti quella di MNG_UI, altrimenti Camera.main
        /// (con un solo avviso, vedi <see cref="UIRigLocator"/>).
        /// </summary>
        /// <param name="assigned">Testa assegnata al componente chiamante (può essere null).</param>
        /// <param name="who">Nome del chiamante, per l'eventuale avviso.</param>
        /// <returns>Il Transform della testa, o null se non c'è.</returns>
        public static Transform ResolveHead(Transform assigned, string who)
        {
            if (assigned != null) return assigned;
            Transform fromUi = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            return UIRigLocator.ResolveHead(fromUi, who);
        }

        /// <summary>
        /// Piazza un Canvas World Space davanti alla testa: direzione dello sguardo proiettata sul piano orizzontale, a
        /// <paramref name="distance"/> metri, all'altezza degli occhi (più l'angolo verticale richiesto, mai oltre l'angolo massimo
        /// della HUD), rivolto verso la testa e dritto. La scala del Canvas è 0,00126 × distanza.
        /// </summary>
        /// <param name="canvasTransform">Transform del Canvas da piazzare.</param>
        /// <param name="head">Testa del giocatore.</param>
        /// <param name="distance">Distanza dagli occhi in metri.</param>
        /// <param name="pitchDegrees">Angolo verticale desiderato in gradi (0 = all'altezza degli occhi, negativo = sotto).</param>
        /// <param name="maxPitchDegrees">Angolo massimo consentito (UITheme.Hud.MaxPitch, +10).</param>
        public static void PlaceInFront(Transform canvasTransform, Transform head, float distance, float pitchDegrees, float maxPitchDegrees)
        {
            if (canvasTransform == null || head == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(head.forward.y < 0f ? head.up : -head.up, Vector3.up);
            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;

            float pitch = Mathf.Min(pitchDegrees, maxPitchDegrees);
            Vector3 position = head.position + forward * distance;
            position.y += Mathf.Tan(pitch * Mathf.Deg2Rad) * distance;
            // Il Canvas guarda lungo +Z: orientato come lo sguardo, il suo fronte è rivolto alla testa.
            canvasTransform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            canvasTransform.localScale = Vector3.one * UITheme.GetCanvasScale(distance);
        }
    }
}
