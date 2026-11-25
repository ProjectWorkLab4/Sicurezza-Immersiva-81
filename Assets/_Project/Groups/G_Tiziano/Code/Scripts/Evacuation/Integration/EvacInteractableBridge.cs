using SI81.Input;
using SI81.Tiziano.ChemicalAlert.Interaction;
using UnityEngine;

namespace SI81.Tiziano.Evacuation.Integration
{
    /// <summary>
    /// Adapter: inoltra gli eventi di SI81.Interaction.Interactable al ScenarioInteractable di gruppo.
    /// La scena non ha raggi, input o RayInteractor locali: usa solo quelli del rig condiviso.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SI81.Interaction.Interactable))]
    [RequireComponent(typeof(ScenarioInteractable))]
    public sealed class EvacInteractableBridge : MonoBehaviour
    {
        [Tooltip("Interactable condiviso (RayInteractor del rig).")]
        [SerializeField] private SI81.Interaction.Interactable interactable;
        [Tooltip("Superficie di interazione di gruppo da alimentare.")]
        [SerializeField] private ScenarioInteractable scenario;
        [Tooltip("true = il select segnala un'ispezione (piante), false = un'attivazione.")]
        [SerializeField] private bool inspectOnSelect;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<SI81.Interaction.Interactable>();
            if (scenario == null)
                scenario = GetComponent<ScenarioInteractable>();
        }

        private void OnEnable()
        {
            if (interactable == null || scenario == null)
                return;

            interactable.Selectable = scenario.Activatable || scenario.Inspectable;
            interactable.Grabbable = scenario.Grabbable;
            interactable.FollowsHand = false;

            interactable.HoverEntered += HandleHoverEnter;
            interactable.HoverExited += HandleHoverExit;
            interactable.SelectEntered += HandleSelectEnter;
            interactable.GrabEntered += HandleGrabEnter;
            interactable.GrabExited += HandleGrabExit;
        }

        private void OnDisable()
        {
            if (interactable == null)
                return;
            interactable.HoverEntered -= HandleHoverEnter;
            interactable.HoverExited -= HandleHoverExit;
            interactable.SelectEntered -= HandleSelectEnter;
            interactable.GrabEntered -= HandleGrabEnter;
            interactable.GrabExited -= HandleGrabExit;
        }

        /// <summary>Inoltra l'inizio hover.</summary>
        private void HandleHoverEnter(Hand hand)
        {
            scenario.RaiseHoverEnter();
        }

        /// <summary>Inoltra la fine hover.</summary>
        private void HandleHoverExit(Hand hand)
        {
            scenario.RaiseHoverExit();
        }

        /// <summary>Trasforma il select del rig in ispezione o attivazione.</summary>
        private void HandleSelectEnter(Hand hand)
        {
            if (inspectOnSelect)
                scenario.RaiseInspected();
            else
                scenario.RaiseActivated();
        }

        /// <summary>Inoltra l'inizio presa.</summary>
        private void HandleGrabEnter(Hand hand)
        {
            scenario.RaiseGrabbed();
        }

        /// <summary>Inoltra la fine presa.</summary>
        private void HandleGrabExit(Hand hand)
        {
            scenario.RaiseReleased();
        }
    }
}
