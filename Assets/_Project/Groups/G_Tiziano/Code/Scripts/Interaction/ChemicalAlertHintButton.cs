using SI81.Input;
using SI81.Interaction;
using SI81.Tiziano.ChemicalAlert.Systems;
using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Collega la selezione del pulsante 3D al suggerimento manuale Guided.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class ChemicalAlertHintButton : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertScenarioController scenario;
        [SerializeField] private bool showHintButton = true;

        private Interactable interactable;
        private Collider buttonCollider;
        private Renderer[] visualRenderers = new Renderer[0];

        /// <summary>Cache dei riferimenti visivi da mostrare/nascondere insieme al pulsante.</summary>
        private void Awake()
        {
            buttonCollider = GetComponent<Collider>();
            visualRenderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>Collega l'evento di selezione del raggio condiviso.</summary>
        private void OnEnable()
        {
            interactable = GetComponent<Interactable>();
            if (interactable != null)
                interactable.SelectEntered += HandleSelected;
        }

        /// <summary>Rimuove l'evento di selezione.</summary>
        private void OnDisable()
        {
            if (interactable != null)
                interactable.SelectEntered -= HandleSelected;
        }

        /// <summary>Mostra il pulsante solo in Guided; la descrizione dell'obiettivo usa i popup della HUD.</summary>
        private void Update()
        {
            bool visible = showHintButton && scenario != null && scenario.CanRequestHint;

            if (interactable != null)
                interactable.Selectable = visible;
            if (buttonCollider != null)
                buttonCollider.enabled = visible;

            foreach (Renderer renderer in visualRenderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        /// <summary>Richiede il prossimo suggerimento Guided quando il pulsante viene selezionato.</summary>
        private void HandleSelected(Hand hand)
        {
            if (scenario != null)
                scenario.RequestHint();
        }
    }
}
