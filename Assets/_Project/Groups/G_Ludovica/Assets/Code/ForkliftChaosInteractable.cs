using UnityEngine;
using UnityEngine.InputSystem;
using SI81.UI;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>Azione specifica dello scenario 07 richiamata da un oggetto interattivo.</summary>
    public enum ForkliftChaosAction
    {
        StopForklift1,
        PickupRadio,
        Call112,
        CallForeman,
        PlaceBarrier,
        PlaceCones,
        TalkToMarco,
        CheckBreathing,
        PressWound,
        CoverMarco,
        StartCPR,
        UseAED,
        MoveMarco,
        KeepColleaguesOut,
        StopForklift2
    }

    /// <summary>
    /// Interazione leggera per il scenario Forklift Chaos: raggio dalla testa + grilletto controller, oppure tasto
    /// funzione in Editor. Non duplica il sistema di input comune; usa gli stessi binding XR del rig condiviso e
    /// delega sempre l'esito al controller dello scenario.
    /// </summary>
    [AddComponentMenu("SI81/Scenari/Forklift Chaos/Interactable")]
    public sealed class ForkliftChaosInteractable : MonoBehaviour
    {
        [Header("Azione")]
        [SerializeField] private ForkliftChaosAction action;
        [SerializeField] private string label = "Interagisci";
        [SerializeField] private Key editorKey = Key.None;
        [SerializeField] private float reach = 2.8f;
        [SerializeField] private bool once = true;
        [SerializeField] private bool requiresRadio;

        // Evita che un solo grilletto attivi piu' interazioni sulla stesso oggetto nel medesimo frame.
        private static int lastTriggerFrame = -1;

        private InputAction triggerAction;
        private ForkliftChaosScenario scenario;
        private bool used;

        /// <summary>Azione associata all'oggetto.</summary>
        public ForkliftChaosAction Action => action;
        /// <summary>Etichetta mostrata nei messaggi di errore.</summary>
        public string Label => label;

        /// <summary>Configurazione usata dal builder Editor.</summary>
        public void Configure(ForkliftChaosAction newAction, string newLabel, Key newKey, bool needsRadio = false, float newReach = 2.8f)
        {
            action = newAction;
            label = string.IsNullOrEmpty(newLabel) ? newAction.ToString() : newLabel;
            editorKey = newKey;
            requiresRadio = needsRadio;
            reach = Mathf.Max(0.5f, newReach);
        }

        /// <summary>Rende l'oggetto di nuovo utilizzabile (replay senza ricaricare la scena).</summary>
        public void ResetUsed()
        {
            used = false;
        }

        private void OnEnable()
        {
            triggerAction = new InputAction("ForkliftChaosUse", InputActionType.Button);
            triggerAction.AddBinding("<XRController>{LeftHand}/triggerPressed");
            triggerAction.AddBinding("<XRController>{RightHand}/triggerPressed");
            triggerAction.Enable();
        }

        private void OnDisable()
        {
            if (triggerAction == null) return;
            triggerAction.Disable();
            triggerAction.Dispose();
            triggerAction = null;
        }

        private void Update()
        {
            if ((once && used) || triggerAction == null) return;
            if (scenario == null) scenario = FindFirstObjectByType<ForkliftChaosScenario>();
            if (scenario == null || !scenario.IsRunning) return;

            bool pressed = false;
            if (triggerAction.WasPressedThisFrame() && IsAimedAt() && lastTriggerFrame != Time.frameCount)
            {
                lastTriggerFrame = Time.frameCount;
                pressed = true;
            }
            if (!pressed && editorKey != Key.None && Keyboard.current != null)
                pressed = Keyboard.current[editorKey].wasPressedThisFrame;

            if (!pressed) return;

            if (requiresRadio && !scenario.HasRadio)
            {
                scenario.Feedback("Prendi prima la radio dal rack RK-01.", FeedbackKind.Negative);
                return;
            }

            used = true;
            scenario.HandleInteraction(action, this);
        }

        /// <summary>Controlla che il raggio della testa colpisca questo oggetto o un suo figlio.</summary>
        private bool IsAimedAt()
        {
            Transform head = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (head == null) return false;

            Ray ray = new Ray(head.position, head.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Collide)) return false;
            return hit.collider != null && hit.collider.GetComponentInParent<ForkliftChaosInteractable>() == this;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.72f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, 0.18f);
        }
    }
}
