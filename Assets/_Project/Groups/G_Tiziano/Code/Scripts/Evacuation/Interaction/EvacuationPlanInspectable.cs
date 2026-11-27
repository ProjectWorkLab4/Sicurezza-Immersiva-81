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
    /// Wall evacuation plan. Inspected by Activated (hover + trigger) or by a steady gaze from the front
    /// within the configured distance and dwell time. The local +Z axis is the readable side.
    /// </summary>
    [RequireComponent(typeof(ScenarioInteractable))]
    public sealed class EvacuationPlanInspectable : MonoBehaviour, IResettable
    {
        private const float OCCLUSION_MARGIN = 0.01f;

        [SerializeField] private EvacuationDirector director;
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private Collider gazeCollider;
        [Tooltip("Player head (CenterEyeAnchor). Falls back to the main camera.")]
        [SerializeField] private Transform head;

        [Header("Identity (stable keys)")]
        [SerializeField] private string planId = "evac.plan";
        [SerializeField] private string textKey = "evac.plan.route";

        [Header("Hooks")]
        [SerializeField] private UnityEvent onInspected = new UnityEvent();

        private float gazeTime;
        private bool gazeLatched;
        private bool isInspected;

        /// <summary>Raised on every inspection of this instance (the Director logs only the first one).</summary>
        public event Action<EvacuationPlanInspectable> Inspected;

        /// <summary>Stable identifier of this plan.</summary>
        public string PlanId => planId;
        /// <summary>Localization key of the plan content.</summary>
        public string TextKey => textKey;
        /// <summary>True once this instance has been inspected in the current run.</summary>
        public bool IsInspected => isInspected;
        /// <summary>Continuous gaze time accumulated, in seconds.</summary>
        public float GazeTime => gazeTime;
        /// <summary>The interactable that receives hover and trigger.</summary>
        public ScenarioInteractable Interactable => interactable;
        /// <summary>Hook invoked on every inspection.</summary>
        public UnityEvent OnInspected => onInspected;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (gazeCollider == null)
                gazeCollider = GetComponent<Collider>();
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
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

        private void Update()
        {
            if (head != null)
                TickGaze(head.position, head.forward, Time.deltaTime);
        }

        /// <summary>
        /// Advances the gaze dwell with the given head pose. Returns true on the tick that inspects the plan.
        /// The player must look away before the gaze can inspect it again.
        /// </summary>
        public bool TickGaze(Vector3 origin, Vector3 direction, float deltaSeconds)
        {
            if (director == null || director.Config == null || director.Phase != EvacPhase.Action || !IsGazedAt(origin, direction))
            {
                gazeTime = 0f;
                gazeLatched = false;
                return false;
            }

            if (gazeLatched)
                return false;

            gazeTime += deltaSeconds;
            if (gazeTime < director.Config.InteractionDwellSeconds)
                return false;

            gazeLatched = true;
            return Inspect("gaze");
        }

        /// <summary>Inspects the plan. Ignored outside the Action phase.</summary>
        public bool Inspect(string source)
        {
            if (director == null || director.Phase != EvacPhase.Action)
            {
                UDebug.Log($"[EV] Plan {planId} ignored ({source}): scenario not in Action.");
                return false;
            }

            isInspected = true;
            UDebug.Log($"[EV] Plan inspected: {planId} ({source}), text {textKey}");
            director.ReportPlanInspected(planId);
            onInspected.Invoke();
            Inspected?.Invoke(this);
            return true;
        }

        /// <summary>IResettable: clears the inspection and the gaze dwell.</summary>
        public void ResetState()
        {
            isInspected = false;
            gazeTime = 0f;
            gazeLatched = false;
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            Inspect("trigger");
        }

        private bool IsGazedAt(Vector3 origin, Vector3 direction)
        {
            if (gazeCollider == null || Vector3.Dot(origin - transform.position, transform.forward) <= 0f)
                return false;

            Ray ray = new Ray(origin, direction);
            if (!gazeCollider.Raycast(ray, out RaycastHit planHit, director.Config.InspectMaxDistance))
                return false;

            float clearDistance = Mathf.Max(0f, planHit.distance - OCCLUSION_MARGIN);
            return !Physics.Raycast(ray, out RaycastHit blocker, clearDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                   || blocker.transform.IsChildOf(transform);
        }
    }
}
