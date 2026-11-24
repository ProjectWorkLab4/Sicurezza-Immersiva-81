using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Calcoli di posizione dell'HUD attorno alla testa (angoli di imbardata/beccheggio, direzione, posa di un pannello
    /// rivolto al giocatore). Un solo posto per questa matematica, usato da AlertPresenter, LazyFollow e da chi piazza
    /// pannelli davanti allo sguardo. Nessuna allocazione. Convenzione: beccheggio positivo = verso l'alto, 0 = orizzonte.
    /// </summary>
    public static class HUDPlacement
    {
        /// <summary>Limite inferiore del beccheggio dei pannelli, in gradi (evita pannelli sotto i piedi).</summary>
        public const float MIN_PITCH = -80f;

        /// <summary>
        /// Imbardata (gradi) di una direzione, misurata attorno all'asse verticale a partire da +Z.
        /// </summary>
        /// <param name="direction">Direzione nel mondo.</param>
        /// <returns>Angolo in gradi.</returns>
        public static float GetYaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Beccheggio (gradi) di una direzione: positivo verso l'alto.
        /// </summary>
        /// <param name="direction">Direzione nel mondo (anche non normalizzata).</param>
        /// <returns>Angolo in gradi.</returns>
        public static float GetPitch(Vector3 direction)
        {
            float length = direction.magnitude;
            if (length < 0.0001f) return 0f;
            return Mathf.Asin(Mathf.Clamp(direction.y / length, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Direzione unitaria da imbardata e beccheggio.
        /// </summary>
        /// <param name="yaw">Imbardata in gradi.</param>
        /// <param name="pitch">Beccheggio in gradi (positivo verso l'alto).</param>
        /// <returns>Direzione unitaria nel mondo.</returns>
        public static Vector3 DirectionFromAngles(float yaw, float pitch)
        {
            float yawRad = yaw * Mathf.Deg2Rad;
            float pitchRad = pitch * Mathf.Deg2Rad;
            float horizontal = Mathf.Cos(pitchRad);
            return new Vector3(Mathf.Sin(yawRad) * horizontal, Mathf.Sin(pitchRad), Mathf.Cos(yawRad) * horizontal);
        }

        /// <summary>
        /// Beccheggio mondo di un pannello che deve stare a <paramref name="relativePitch"/> gradi rispetto allo sguardo,
        /// limitato al massimo consentito dalla HUD (nessun elemento sopra +10°) e al minimo di sicurezza.
        /// </summary>
        /// <param name="gazePitch">Beccheggio dello sguardo in gradi.</param>
        /// <param name="relativePitch">Beccheggio relativo allo sguardo (negativo = sotto), es. -15.</param>
        /// <param name="maxPitch">Beccheggio massimo assoluto (SO_UITheme.Hud.MaxPitch).</param>
        /// <returns>Beccheggio mondo in gradi.</returns>
        public static float ResolveWorldPitch(float gazePitch, float relativePitch, float maxPitch)
        {
            return Mathf.Clamp(gazePitch + relativePitch, MIN_PITCH, maxPitch);
        }

        /// <summary>
        /// Posiziona un pannello alla distanza data, sulla direzione dello sguardo (solo imbardata della testa) abbassata di
        /// <paramref name="relativePitch"/> gradi, rivolto verso la testa e dritto (nessun rollio). Va chiamato UNA volta,
        /// quando il pannello compare: il pannello poi resta fermo nel mondo (non è bloccato alla testa).
        /// </summary>
        /// <param name="target">Pannello da posizionare.</param>
        /// <param name="head">Testa del giocatore (camera).</param>
        /// <param name="distance">Distanza in metri.</param>
        /// <param name="relativePitch">Beccheggio relativo allo sguardo in gradi (negativo = sotto).</param>
        /// <param name="maxPitch">Beccheggio massimo assoluto in gradi.</param>
        public static void PlaceAtGaze(Transform target, Transform head, float distance, float relativePitch, float maxPitch)
        {
            if (target == null || head == null) return;
            Vector3 forward = head.forward;
            float yaw = GetYaw(forward);
            float pitch = ResolveWorldPitch(GetPitch(forward), relativePitch, maxPitch);
            ApplyPose(target, head.position, DirectionFromAngles(yaw, pitch), distance);
        }

        /// <summary>
        /// Scrive posizione e rotazione di un pannello: a <paramref name="distance"/> dalla testa lungo <paramref name="direction"/>,
        /// con l'asse +Z del Canvas (si legge da dietro) orientato come la direzione, in piedi.
        /// </summary>
        /// <param name="target">Pannello.</param>
        /// <param name="headPosition">Posizione della testa.</param>
        /// <param name="direction">Direzione unitaria dalla testa al pannello.</param>
        /// <param name="distance">Distanza in metri.</param>
        public static void ApplyPose(Transform target, Vector3 headPosition, Vector3 direction, float distance)
        {
            target.SetPositionAndRotation(headPosition + direction * distance, Quaternion.LookRotation(direction, Vector3.up));
        }
    }
}
