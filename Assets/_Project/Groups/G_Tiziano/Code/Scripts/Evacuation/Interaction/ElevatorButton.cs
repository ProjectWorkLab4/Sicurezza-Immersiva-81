using System;
using UnityEngine;
using UnityEngine.Events;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Systems;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Interaction
{
    /// <summary>
    /// Elevator call button: Activated (hover + trigger) reports elevator use to the Director.
    /// In Guided the run continues with danger Down; elsewhere it ends immediately.
    /// </summary>
    [RequireComponent(typeof(ScenarioInteractable))]
    public sealed class ElevatorButton : MonoBehaviour, IResettable
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private string buttonId = "evac.elevator.button";

        [Header("Hooks")]
        [SerializeField] private UnityEvent onPressed = new UnityEvent();

        private bool pressedThisRun;

        /// <summary>Raised when this button is pressed in the current run.</summary>
        public event Action<ElevatorButton> Pressed;

        /// <summary>Stable identifier of the button.</summary>
        public string ButtonId => buttonId;
        /// <summary>True once this button has been pressed in the current run.</summary>
        public bool WasPressed => pressedThisRun;
        /// <summary>The interactable that receives hover and trigger.</summary>
        public ScenarioInteractable Interactable => interactable;
        /// <summary>Hook invoked on press.</summary>
        public UnityEvent OnPressed => onPressed;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.Activated += HandleActivated;
            if (director != null)
                director.RegisterResettable(this);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.Activated -= HandleActivated;
            if (director != null)
                director.UnregisterResettable(this);
        }

        /// <summary>Presses the call button. Ignored outside Action or after the first press of the run.</summary>
        public void Press(string source = "trigger")
        {
            if (pressedThisRun)
                return;
            if (director == null || director.Phase != EvacPhase.Action)
            {
                UDebug.Log($"[EV] Elevator button {buttonId} ignored ({source}): scenario not in Action.");
                return;
            }

            pressedThisRun = true;
            UDebug.Log($"[EV] Elevator button pressed: {buttonId} ({source}).");
            director.ReportElevatorUsed();
            onPressed.Invoke();
            Pressed?.Invoke(this);
        }

        /// <summary>IResettable: clears the per-run press flag.</summary>
        public void ResetState()
        {
            pressedThisRun = false;
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            Press("trigger");
        }
    }
}
