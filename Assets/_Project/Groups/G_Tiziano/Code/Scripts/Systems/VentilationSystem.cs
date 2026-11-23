using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Owns ventilation On/Off state and stops escalation when activated.
    /// </summary>
    public sealed class VentilationSystem : MonoBehaviour, IResettable
    {
        /// <summary>Ventilation power states.</summary>
        public enum State
        {
            Off,
            On
        }

        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private VentTerminal terminal;
        [SerializeField] private VentilationEscalation escalation;
        [SerializeField] private AudioClip audioLoop;

        private State state;

        /// <summary>Gets the current ventilation state.</summary>
        public State CurrentState => state;
        /// <summary>Gets whether ventilation is on.</summary>
        public bool IsOn => state == State.On;
        /// <summary>Gets the optional loop clip for future audio wiring.</summary>
        public AudioClip AudioLoop => audioLoop;

        private void Awake()
        {
            if (director == null)
                director = GetComponent<ChemicalAlertDirector>();
            if (escalation == null)
                escalation = GetComponent<VentilationEscalation>();
        }

        private void OnEnable()
        {
            if (terminal != null)
                terminal.TurnedOn += HandleTerminalTurnedOn;
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (terminal != null)
                terminal.TurnedOn -= HandleTerminalTurnedOn;
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Turns ventilation on and stops the hidden escalation.</summary>
        public void TurnOn()
        {
            if (terminal != null)
            {
                terminal.TurnOn();
                return;
            }

            SetOn();
        }

        /// <summary>Turns ventilation off for reset or controlled debug checks.</summary>
        public void TurnOff()
        {
            state = State.Off;
        }

        /// <summary>Resets terminal, state and escalation.</summary>
        public void ResetState()
        {
            state = State.Off;
            if (terminal != null)
                terminal.ResetTerminal();
            if (escalation != null)
                escalation.ResetState();
        }

        private void HandleTerminalTurnedOn()
        {
            SetOn();
        }

        private void SetOn()
        {
            if (state == State.On)
                return;

            state = State.On;
            if (escalation != null)
                escalation.StopEscalation();
            UnityEngine.Debug.Log($"[CA] VentilationSystem: On; escalation stopped.");
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
