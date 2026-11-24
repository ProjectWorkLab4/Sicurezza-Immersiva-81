using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Lazy follow SI81 (non quello di XRI): tiene un pannello a distanza fissa dalla testa, in basso nella vista, e lo
    /// ri-centra con un movimento morbido SOLO quando lo sguardo si allontana troppo (imbardata oltre la soglia, o beccheggio
    /// relativo fuori dalla fascia). Il beccheggio assoluto non supera mai il massimo della HUD (+10°). Usato da HUD_Subtitles.
    /// Nessuna allocazione per fotogramma; per fermarlo basta disattivare il componente (al riattivarsi riparte dallo sguardo).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Lazy Follow")]
    public class LazyFollow : MonoBehaviour
    {
        [Header("Parametri (assegnati dal builder dal tema)")]
        [Tooltip("Distanza dalla testa in metri (sottotitoli: UITheme.SUBTITLE_DISTANCE_M).")]
        [SerializeField] private float distance = 1f;
        [Tooltip("Beccheggio relativo minimo allo sguardo in gradi (sottotitoli: Hud.SubtitlePitchMin, es. -28).")]
        [SerializeField] private float relativePitchMin = -28f;
        [Tooltip("Beccheggio relativo massimo allo sguardo in gradi (sottotitoli: Hud.SubtitlePitchMax, es. -20).")]
        [SerializeField] private float relativePitchMax = -20f;
        [Tooltip("Beccheggio assoluto massimo in gradi (Hud.MaxPitch, +10).")]
        [SerializeField] private float maxWorldPitch = 10f;

        [Header("Comportamento del ri-centramento")]
        [Tooltip("Soglia di imbardata oltre cui il pannello viene ri-centrato, in gradi.")]
        [SerializeField] private float yawThreshold = 30f;
        [Tooltip("Tolleranza oltre la fascia di beccheggio prima di ri-centrare, in gradi.")]
        [SerializeField] private float pitchTolerance = 6f;
        [Tooltip("Sotto questa distanza angolare il ri-centramento finisce, in gradi.")]
        [SerializeField] private float settleAngle = 2f;
        [Tooltip("Velocità del ri-centramento (1/secondi): più alto = più rapido.")]
        [SerializeField] private float followSpeed = 4f;

        // Testa da seguire.
        private Transform head;
        // Direzione attuale del pannello rispetto alla testa (gradi).
        private float yaw;
        private float pitch;
        // true mentre il pannello si sta ri-centrando.
        private bool following;
        // true dopo la prima posa.
        private bool placed;

        /// <summary>Distanza dalla testa in metri.</summary>
        public float Distance => distance;

        /// <summary>
        /// Imposta la testa da seguire e posiziona subito il pannello sullo sguardo.
        /// </summary>
        /// <param name="newHead">Testa (camera) del giocatore.</param>
        public void SetHead(Transform newHead)
        {
            head = newHead;
            placed = false;
            if (isActiveAndEnabled) SnapToGaze();
        }

        /// <summary>
        /// Porta subito il pannello al centro della fascia, senza animazione (usato quando il pannello compare).
        /// </summary>
        public void SnapToGaze()
        {
            if (head == null) return;
            Vector3 forward = head.forward;
            yaw = HUDPlacement.GetYaw(forward);
            pitch = HUDPlacement.ResolveWorldPitch(HUDPlacement.GetPitch(forward), CenterPitch(), maxWorldPitch);
            following = false;
            placed = true;
            Apply();
        }

        /// <summary>
        /// Al riattivarsi riparte dallo sguardo attuale.
        /// </summary>
        private void OnEnable()
        {
            if (head != null) SnapToGaze();
        }

        /// <summary>
        /// Dopo il movimento della testa nel fotogramma: decide se ri-centrare e aggiorna la posa.
        /// </summary>
        private void LateUpdate()
        {
            if (head == null) return;
            if (!placed)
            {
                SnapToGaze();
                return;
            }

            Vector3 forward = head.forward;
            float gazeYaw = HUDPlacement.GetYaw(forward);
            float gazePitch = HUDPlacement.GetPitch(forward);
            float yawError = Mathf.DeltaAngle(gazeYaw, yaw);
            float relativePitch = pitch - gazePitch;

            bool outside = Mathf.Abs(yawError) > yawThreshold
                || relativePitch < relativePitchMin - pitchTolerance
                || relativePitch > relativePitchMax + pitchTolerance;
            if (outside) following = true;

            if (following)
            {
                float targetPitch = HUDPlacement.ResolveWorldPitch(gazePitch, CenterPitch(), maxWorldPitch);
                float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
                yaw = Mathf.LerpAngle(yaw, gazeYaw, t);
                pitch = Mathf.Lerp(pitch, targetPitch, t);
                if (Mathf.Abs(Mathf.DeltaAngle(gazeYaw, yaw)) < settleAngle && Mathf.Abs(pitch - targetPitch) < settleAngle) following = false;
            }

            // Mai sopra il massimo della HUD.
            if (pitch > maxWorldPitch) pitch = maxWorldPitch;
            Apply();
        }

        /// <summary>
        /// Centro della fascia di beccheggio relativo (gradi).
        /// </summary>
        /// <returns>Beccheggio relativo medio.</returns>
        private float CenterPitch()
        {
            return (relativePitchMin + relativePitchMax) * 0.5f;
        }

        /// <summary>
        /// Scrive la posa del pannello dalla direzione attuale (segue anche gli spostamenti della testa).
        /// </summary>
        private void Apply()
        {
            HUDPlacement.ApplyPose(transform, head.position, HUDPlacement.DirectionFromAngles(yaw, pitch), distance);
        }

        /// <summary>
        /// Imposta i parametri. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(float newDistance, float newPitchMin, float newPitchMax, float newMaxWorldPitch)
        {
            distance = newDistance;
            relativePitchMin = newPitchMin;
            relativePitchMax = newPitchMax;
            maxWorldPitch = newMaxWorldPitch;
        }
    }
}
