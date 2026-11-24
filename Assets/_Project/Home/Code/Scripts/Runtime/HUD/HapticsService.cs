using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace SI81.UI
{
    /// <summary>
    /// Impulsi aptici dei controller tramite Input System (XRControllerWithRumble.SendImpulse), senza XRI. Null-safe:
    /// in Editor o senza controller non fa nulla. Rispetta l'impostazione controls.ui_haptics (spenta = nessun impulso).
    /// I modelli (ampiezza, durata) sono campi del componente; proposta di token in NOTES_C1.md (project.hud.haptics).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Haptics Service")]
    public class HapticsService : MonoBehaviour
    {
        /// <summary>Modello di impulso: ampiezza 0..1 e durata in secondi.</summary>
        [Serializable]
        public struct Pattern
        {
            /// <summary>Ampiezza 0..1.</summary>
            public float Amplitude;
            /// <summary>Durata in secondi.</summary>
            public float Duration;

            /// <summary>Crea un modello.</summary>
            /// <param name="amplitude">Ampiezza.</param>
            /// <param name="duration">Durata.</param>
            public Pattern(float amplitude, float duration)
            {
                Amplitude = amplitude;
                Duration = duration;
            }
        }

        [Header("Modelli di impulso")]
        [Tooltip("Cambio di obiettivo (mano non dominante).")]
        [SerializeField] private Pattern objectiveChange = new Pattern(0.4f, 0.12f);
        [Tooltip("Avvisi del timer a 60 s e 30 s (mano non dominante).")]
        [SerializeField] private Pattern timerWarning = new Pattern(0.8f, 0.25f);
        [Tooltip("Avviso evento (entrambe le mani).")]
        [SerializeField] private Pattern eventAlert = new Pattern(1f, 0.3f);
        [Tooltip("Feedback d'azione (mano non dominante).")]
        [SerializeField] private Pattern actionFeedback = new Pattern(0.3f, 0.1f);
        [Tooltip("Tick dello slider delle impostazioni (mano dominante).")]
        [SerializeField] private Pattern tick = new Pattern(0.15f, 0.03f);

        /// <summary>Istanza unica (null se MNG_HUD non è in scena).</summary>
        public static HapticsService Instance { get; private set; }

        /// <summary>Modello: cambio di obiettivo.</summary>
        public Pattern ObjectiveChange => objectiveChange;
        /// <summary>Modello: avviso del timer.</summary>
        public Pattern TimerWarning => timerWarning;
        /// <summary>Modello: avviso evento.</summary>
        public Pattern EventAlert => eventAlert;
        /// <summary>Modello: feedback d'azione.</summary>
        public Pattern ActionFeedback => actionFeedback;
        /// <summary>Modello: tick dello slider.</summary>
        public Pattern Tick => tick;

        /// <summary>
        /// Registra l'istanza.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Toglie l'istanza.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Azzera l'istanza statica all'avvio del Play Mode (Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Instance = null;
        }

        /// <summary>
        /// Impulso su un controller.
        /// </summary>
        /// <param name="hand">Controller.</param>
        /// <param name="amplitude">Ampiezza 0..1.</param>
        /// <param name="duration">Durata in secondi.</param>
        public void Pulse(HUDHand hand, float amplitude, float duration)
        {
            if (UIServices.Settings != null && !UIServices.Settings.GetBool(SettingsCatalog.UI_HAPTICS)) return;
            // Stesso controller del raggio (con più profili OpenXR per mano XRController.leftHand può non avere la vibrazione).
            XRController device = XRHandDevices.Get(hand == HUDHand.Left);
            if (device is XRControllerWithRumble rumble) rumble.SendImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }

        /// <summary>
        /// Impulso con un modello su un controller.
        /// </summary>
        /// <param name="hand">Controller.</param>
        /// <param name="pattern">Modello.</param>
        public void Pulse(HUDHand hand, Pattern pattern)
        {
            Pulse(hand, pattern.Amplitude, pattern.Duration);
        }

        /// <summary>
        /// Impulso con un modello sulla mano non dominante (quella del pannello da polso).
        /// </summary>
        /// <param name="pattern">Modello.</param>
        public void PulseNonDominant(Pattern pattern)
        {
            Pulse(HUDService.GetNonDominantHand(), pattern);
        }

        /// <summary>
        /// Impulso con un modello su entrambe le mani.
        /// </summary>
        /// <param name="pattern">Modello.</param>
        public void PulseBoth(Pattern pattern)
        {
            Pulse(HUDHand.Left, pattern);
            Pulse(HUDHand.Right, pattern);
        }

        /// <summary>
        /// Tick leggero sulla mano dominante (slider delle impostazioni).
        /// </summary>
        public void PulseTick()
        {
            HUDHand nonDominant = HUDService.GetNonDominantHand();
            Pulse(nonDominant == HUDHand.Left ? HUDHand.Right : HUDHand.Left, tick);
        }
    }
}
