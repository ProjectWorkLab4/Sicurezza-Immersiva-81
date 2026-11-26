using SI81.Input;
using SI81.Tiziano.ChemicalAlert.Interaction;
using UnityEngine;

namespace SI81.Tiziano.Evacuation.Integration
{
    /// <summary>
    /// Ponte tra il rig condiviso e `ScenarioInteractable`: la presa con l'impugnatura
    /// equivale all'attivazione, senza far seguire l'oggetto alla mano. Hover e trigger
    /// sono già inoltrati da `ScenarioInteractable`, quindi non vengono duplicati qui.
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
            interactable.GrabEntered += HandleGrabEnter;
        }

        private void OnDisable()
        {
            if (interactable == null)
                return;

            interactable.GrabEntered -= HandleGrabEnter;
        }

        /// <summary>Trasforma la presa dell'impugnatura in attivazione dell'oggetto.</summary>
        private void HandleGrabEnter(Hand hand)
        {
            if (scenario == null)
                return;

            UnityEngine.Debug.Log($"[EV] Grab -> Activate: {name}");
            scenario.RaiseActivated();
        }
    }
}
