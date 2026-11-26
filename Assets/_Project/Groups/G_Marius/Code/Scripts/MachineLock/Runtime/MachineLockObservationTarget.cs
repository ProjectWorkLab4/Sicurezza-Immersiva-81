using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// One observation target of the Machine Lock scenario (GDD §9.11). Lives on the OBS_ objects of
    /// MNG_ObservationTargets and scores a single Awareness step when the player looks at it long enough.
    /// The dwell rule is pure and testable: <see cref="TryObserve(Vector3, Vector3, ObservationMode, float, float, float, float, float, float)"/>
    /// receives the head pose and the config values, so it can be exercised without a scene or a rig.
    /// Each target reports at most once to <see cref="MachineLockScenario.NotifyObservation"/>; the SPI
    /// still de-duplicates by id, so a repeated report is harmless.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Observation Target")]
    public sealed class MachineLockObservationTarget : MonoBehaviour
    {
        /// <summary>GDD §14.4 default: eye within this distance for a gaze observation.</summary>
        private const float DEFAULT_GAZE_DISTANCE = 3.5f;
        /// <summary>GDD §14.4 default: maximum angle between head forward and the target, in degrees.</summary>
        private const float DEFAULT_GAZE_ANGLE = 25f;
        /// <summary>GDD §14.4 default: seconds of gaze dwell.</summary>
        private const float DEFAULT_GAZE_DWELL = 1.2f;
        /// <summary>GDD §14.4 default: eye within this distance for the proximity fallback.</summary>
        private const float DEFAULT_PROXIMITY_DISTANCE = 1.5f;
        /// <summary>GDD §14.4 default: seconds of proximity dwell.</summary>
        private const float DEFAULT_PROXIMITY_DWELL = 0.5f;
        /// <summary>Below this distance the direction check is skipped (the head is inside the target).</summary>
        private const float MIN_DIRECTION_DISTANCE = 0.001f;

        [Header("Observation target")]
        [Tooltip("Stable identifier reported to the scenario: press, thermometer, display, puddle, panel, ppe_cabinet.")]
        [SerializeField] private string targetId;
        [Tooltip("Scenario that scores the observation. Owner of MNG_Scenario.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Tunable Machine Lock values (SO_MachineLockConfig). Empty = GDD defaults.")]
        [SerializeField] private MachineLockConfig config;
        [Tooltip("Local offset of the point the player has to look at. Zero = the transform position.")]
        [SerializeField] private Vector3 targetOffset;

        // Camera.main is searched once, only when neither the shared rig nor the UI provide a head.
        private Transform fallbackCamera;

        /// <summary>Stable identifier of this target.</summary>
        public string Id { get => targetId; set => targetId = value; }
        /// <summary>true after the target was observed once.</summary>
        public bool Observed { get; private set; }
        /// <summary>Seconds the head spent inside the observation condition during the current streak.</summary>
        public float CurrentDwellSeconds { get; private set; }

        /// <summary>
        /// Assigns the identifier and the references. Used by the scene builder and by the debug menu.
        /// </summary>
        /// <param name="id">Stable target identifier.</param>
        /// <param name="ownerScenario">Scenario that scores the observation.</param>
        /// <param name="machineConfig">Config with the gaze and proximity values, or null for the GDD defaults.</param>
        public void Configure(string id, MachineLockScenario ownerScenario, MachineLockConfig machineConfig)
        {
            targetId = id;
            scenario = ownerScenario;
            config = machineConfig;
            ResetObservation();
        }

        /// <summary>
        /// Clears the observed flag and the dwell streak, for example at the start of a new run.
        /// </summary>
        public void ResetObservation()
        {
            Observed = false;
            CurrentDwellSeconds = 0f;
        }

        /// <summary>
        /// Evaluates one frame of the observation rule against an explicit head pose. Pure: no scene, no
        /// rig and no Time access, so it can be unit tested.
        /// </summary>
        /// <param name="headPosition">World position of the eye.</param>
        /// <param name="headForward">World forward of the head.</param>
        /// <param name="mode">Gaze (distance plus angle) or Proximity (distance only).</param>
        /// <param name="gazeDistance">Gaze mode: maximum distance in metres.</param>
        /// <param name="gazeAngleDeg">Gaze mode: maximum angle in degrees.</param>
        /// <param name="gazeDwellSeconds">Gaze mode: seconds of dwell needed.</param>
        /// <param name="proximityDistance">Proximity mode: maximum distance in metres.</param>
        /// <param name="proximityDwellSeconds">Proximity mode: seconds of dwell needed.</param>
        /// <param name="deltaTime">Frame time in seconds.</param>
        /// <returns>true only on the frame that completes the observation.</returns>
        public bool TryObserve(Vector3 headPosition, Vector3 headForward, ObservationMode mode, float gazeDistance,
            float gazeAngleDeg, float gazeDwellSeconds, float proximityDistance, float proximityDwellSeconds,
            float deltaTime)
        {
            if (Observed) return false;
            if (string.IsNullOrEmpty(targetId)) return false;
            if (deltaTime < 0f) return false;

            Vector3 targetWorld = transform.TransformPoint(targetOffset);
            Vector3 toTarget = targetWorld - headPosition;
            float distance = toTarget.magnitude;

            bool insideCondition;
            float requiredDwell;
            if (mode == ObservationMode.Proximity)
            {
                insideCondition = distance <= proximityDistance;
                requiredDwell = proximityDwellSeconds;
            }
            else
            {
                insideCondition = distance <= gazeDistance
                                  && (distance < MIN_DIRECTION_DISTANCE
                                      || Vector3.Angle(headForward, toTarget.normalized) <= gazeAngleDeg);
                requiredDwell = gazeDwellSeconds;
            }

            CurrentDwellSeconds = insideCondition ? CurrentDwellSeconds + deltaTime : 0f;
            if (CurrentDwellSeconds < requiredDwell) return false;

            Observed = true;
            if (scenario != null) scenario.NotifyObservation(targetId);
            return true;
        }

        /// <summary>
        /// Evaluates one frame using the head transform and the values of the config (GDD defaults when the
        /// config is not assigned).
        /// </summary>
        /// <param name="head">Head transform of the player.</param>
        /// <param name="deltaTime">Frame time in seconds.</param>
        /// <returns>true only on the frame that completes the observation.</returns>
        public bool TryObserve(Transform head, float deltaTime)
        {
            if (head == null) return false;

            ObservationMode mode = ObservationMode.Gaze;
            float gazeDistance = DEFAULT_GAZE_DISTANCE;
            float gazeAngle = DEFAULT_GAZE_ANGLE;
            float gazeDwell = DEFAULT_GAZE_DWELL;
            float proximityDistance = DEFAULT_PROXIMITY_DISTANCE;
            float proximityDwell = DEFAULT_PROXIMITY_DWELL;

            if (config != null)
            {
                mode = config.observationMode;
                gazeDistance = config.gazeDistance;
                gazeAngle = config.gazeAngle;
                gazeDwell = config.gazeDwell;
                proximityDistance = config.proximityDistance;
                proximityDwell = config.proximityDwell;
            }

            return TryObserve(head.position, head.forward, mode, gazeDistance, gazeAngle, gazeDwell,
                proximityDistance, proximityDwell, deltaTime);
        }

        /// <summary>
        /// Watches the player head while the scenario runs and stops for good after the first observation.
        /// </summary>
        private void Update()
        {
            if (Observed) return;
            if (scenario == null || !scenario.IsRunning) return;

            Transform head = ResolveHead();
            if (head == null) return;
            TryObserve(head, Time.deltaTime);
        }

        /// <summary>
        /// Shared rig head, then the UI head, then Camera.main (resolved once and re-resolved only if lost).
        /// </summary>
        /// <returns>Head transform, or null when the scene has no player camera.</returns>
        private Transform ResolveHead()
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig != null && rig.Head != null) return rig.Head;

            UIScreenManager ui = UIScreenManager.Instance;
            if (ui != null && ui.Head != null) return ui.Head;

            if (fallbackCamera == null)
            {
                Camera main = Camera.main;
                if (main != null) fallbackCamera = main.transform;
            }
            return fallbackCamera;
        }

        /// <summary>
        /// Draws the look point in the Scene view so the target offset can be tuned.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.29f, 0.87f, 0.5f, 0.8f);
            Gizmos.DrawWireSphere(transform.TransformPoint(targetOffset), 0.08f);
            Gizmos.color = new Color(0.29f, 0.87f, 0.5f, 0.25f);
            Gizmos.DrawLine(transform.position, transform.TransformPoint(targetOffset));
        }
    }
}
