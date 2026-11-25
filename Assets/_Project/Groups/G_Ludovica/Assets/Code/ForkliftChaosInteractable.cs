using UnityEngine;
using UnityEngine.InputSystem;
using SI81.UI;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>Azione specifica dello scenario 07 richiamata da un oggetto interattivo.</summary>
    public enum ForkliftChaosAction
    {
        // Osservazione: basta guardare l'oggetto (nessun pulsante, nessun pannello).
        ObserveMarco,
        ObserveForklift1,
        ObservePallet,
        ObserveBlindCorner,

        // Azioni fisiche: sguardo + grilletto sul oggetto, oppure tasto funzione in Editor.
        StopForklift1,
        PickupRadio,
        Call112,
        CallForeman,
        PickupBarrier,
        PlaceBarrier,
        PlaceCones,
        KnockPallet,
        TalkToMarco,
        CheckBreathing,
        PressWound,
        CoverMarco,
        StartCPR,
        UseAED,
        MoveMarco,
        StopForklift2
    }

    /// <summary>
    /// Interazione leggera per il scenario Forklift Chaos: raggio dalla testa + grilletto controller (o dwell di sguardo
    /// per le osservazioni), oppure tasto funzione in Editor. Non duplica il sistema di input comune; usa gli stessi
    /// binding XR del rig condiviso e delega sempre l'esito al controller dello scenario.
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
        [SerializeField] private bool lookOnly;
        [SerializeField] private float dwellSeconds = 1.1f;

        // Un solo grilletto per frame: evita che piu' componenti sullo stesso oggetto partano insieme.
        private static int lastTriggerFrame = -1;

        private InputAction triggerAction;
        private ForkliftChaosScenario scenario;
        private bool used;
        private float lookTime;

        public ForkliftChaosAction Action => action;
        public string Label => label;

        public void Configure(ForkliftChaosAction newAction, string newLabel, Key newKey,
            bool needsRadio = false, float newReach = 2.8f)
        {
            action = newAction;
            label = string.IsNullOrEmpty(newLabel) ? newAction.ToString() : newLabel;
            editorKey = newKey;
            requiresRadio = needsRadio;
            reach = Mathf.Max(0.5f, newReach);
            lookOnly = false;
        }

        public void ConfigureLook(ForkliftChaosAction newAction, string newLabel, float newDwell = 1.1f,
            float newReach = 3.5f)
        {
            action = newAction;
            label = string.IsNullOrEmpty(newLabel) ? newAction.ToString() : newLabel;
            editorKey = Key.None;
            reach = Mathf.Max(0.5f, newReach);
            dwellSeconds = Mathf.Max(0.2f, newDwell);
            lookOnly = true;
            requiresRadio = false;
        }

        public void ResetUsed()
        {
            used = false;
            lookTime = 0f;
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

            if (lookOnly)
            {
                if (IsAimedAt())
                {
                    lookTime += Time.deltaTime;
                    if (lookTime >= dwellSeconds) Fire();
                }
                else lookTime = 0f;
                return;
            }

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
            Fire();
        }

        private void Fire()
        {
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
