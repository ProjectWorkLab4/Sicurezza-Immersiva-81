using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Observation mode used by the Machine Lock scenario to score Awareness.
    /// </summary>
    public enum ObservationMode
    {
        /// <summary>Head within gazeDistance, inside gazeAngle, for gazeDwell seconds.</summary>
        Gaze,
        /// <summary>Head within proximityDistance for proximityDwell seconds.</summary>
        Proximity
    }

    /// <summary>
    /// Tunable values of the Machine Lock scenario (asset SO_MachineLockConfig).
    /// Every default in this file is the value approved in the GDD §14.4; content scripts read the
    /// values from this asset instead of hard-coding them. The SPI reads a plain options copy built by
    /// <see cref="CreateSpiOptions"/>, so it stays free of Unity dependencies.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/MachineLock/Config", fileName = "SO_MachineLockConfig")]
    public sealed class MachineLockConfig : ScriptableObject
    {
        [Header("Time limits (seconds)")]
        [Tooltip("Guided time limit. The level continues after it; only the Time score reaches zero.")]
        public float timeLimitGuided = 900f;
        [Tooltip("Realistic (project level Operational) time limit.")]
        public float timeLimitRealistic = 600f;
        [Tooltip("Stress (project level Crisis) time limit.")]
        public float timeLimitStress = 480f;

        [Header("Hard time end")]
        [Tooltip("true when reaching the Realistic limit ends the mission.")]
        public bool hardEndRealistic = true;
        [Tooltip("true when reaching the Stress limit ends the mission.")]
        public bool hardEndStress = true;
        [Tooltip("false: Guided never ends on time.")]
        public bool hardEndGuided = false;

        [Header("Residual energy")]
        [Tooltip("Probability that the oil is energised at start.")]
        [Range(0f, 1f)]
        public float energizedProbability = 0.5f;
        [Tooltip("Seconds needed for the residual charge to decay after main power is off.")]
        public float energyDecaySeconds = 90f;

        [Header("Observation")]
        [Tooltip("Observation mode used to score Awareness.")]
        public ObservationMode observationMode = ObservationMode.Gaze;
        [Tooltip("Gaze mode: maximum distance between head and target, in metres.")]
        public float gazeDistance = 3.5f;
        [Tooltip("Gaze mode: maximum angle between head forward and target, in degrees.")]
        public float gazeAngle = 25f;
        [Tooltip("Gaze mode: seconds of dwell needed.")]
        public float gazeDwell = 1.2f;
        [Tooltip("Proximity fallback: maximum distance between head and target, in metres.")]
        public float proximityDistance = 1.5f;
        [Tooltip("Proximity fallback: seconds of dwell needed.")]
        public float proximityDwell = 0.5f;

        [Header("Containment")]
        [Tooltip("Maximum distance between a released item and its snap zone, in metres.")]
        public float snapRadius = 0.6f;

        [Header("Inaction")]
        [Tooltip("Seconds of press running before smoke stage 1.")]
        public float inactionSmokeSeconds = 90f;
        [Tooltip("Seconds of press running before smoke stage 2.")]
        public float inactionSmokeStage2Seconds = 150f;
        [Tooltip("Seconds of press running before Bad Ending 2.")]
        public float inactionBadEndingSeconds = 240f;

        [Header("Critical events")]
        [Tooltip("Seconds during which a second critical event is ignored.")]
        public float criticalCooldownSeconds = 5f;
        [Tooltip("Seconds between a critical event and the respawn.")]
        public float respawnDelaySeconds = 2f;
        [Tooltip("Safety penalty of a critical contact.")]
        public int criticalSafetyPenalty = 10;
        [Tooltip("Safety penalty of the water extinguisher.")]
        public int waterSafetyPenalty = 5;
        [Tooltip("Safety penalty of a try-start before isolation.")]
        public int tryStartSafetyPenalty = 5;
        [Tooltip("Decision penalty of a critical event.")]
        public int criticalDecisionPenalty = 10;

        [Header("Decision penalties")]
        [Tooltip("Decision penalty when leaving without the alarm.")]
        public int missingAlarmPenalty = 8;
        [Tooltip("Decision penalty when leaving without the main switch open.")]
        public int missingMainPenalty = 2;
        [Tooltip("Decision penalty when leaving without the lockout.")]
        public int missingLockoutPenalty = 2;
        [Tooltip("Decision penalty when leaving without all four cones.")]
        public int missingConesPenalty = 4;
        [Tooltip("Decision penalty when leaving without all four absorbents.")]
        public int missingContainmentPenalty = 5;
        [Tooltip("Decision penalty when the level time limit is reached.")]
        public int timeoutPenalty = 5;

        [Header("Execution rewards")]
        [Tooltip("Execution reward for the E-stop.")]
        public int executionEStop = 2;
        [Tooltip("Execution reward for the alarm.")]
        public int executionAlarm = 2;
        [Tooltip("Execution reward for main off plus lockout.")]
        public int executionIsolation = 3;
        [Tooltip("Execution reward for tester zero plus try-start verified.")]
        public int executionZeroEnergy = 2;
        [Tooltip("Execution reward for the full correct PPE set.")]
        public int executionPpe = 3;
        [Tooltip("Execution reward for containment complete.")]
        public int executionContainment = 2;
        [Tooltip("Execution reward for the exit and handover.")]
        public int executionExit = 1;

        [Header("Dimension caps")]
        [Tooltip("Maximum Awareness points.")]
        public int awarenessMax = 20;
        [Tooltip("Number of observation targets that make Awareness full.")]
        public int awarenessTargets = 6;
        [Tooltip("Maximum Time points.")]
        public int timeMax = 5;

        /// <summary>
        /// Time limit of a project level.
        /// </summary>
        /// <param name="level">Project level.</param>
        /// <returns>Seconds from this config.</returns>
        public float TimeLimitFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Operational:
                    return timeLimitRealistic;
                case DifficultyLevel.Crisis:
                    return timeLimitStress;
                default:
                    return timeLimitGuided;
            }
        }

        /// <summary>
        /// true when reaching the time limit ends the mission for a project level.
        /// </summary>
        /// <param name="level">Project level.</param>
        /// <returns>Hard end flag from this config.</returns>
        public bool HardEndFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Operational:
                    return hardEndRealistic;
                case DifficultyLevel.Crisis:
                    return hardEndStress;
                default:
                    return hardEndGuided;
            }
        }

        /// <summary>
        /// Builds the plain options object the SPI uses for a level.
        /// </summary>
        /// <param name="level">Project level of the running mission.</param>
        /// <returns>Options copy with the values of this config.</returns>
        public MachineLockSpiOptions CreateSpiOptions(DifficultyLevel level)
        {
            return new MachineLockSpiOptions
            {
                TimeLimitSeconds = TimeLimitFor(level),
                HardEndAtLimit = HardEndFor(level),
                AwarenessTargets = awarenessTargets,
                AwarenessMax = awarenessMax,
                TimeMax = timeMax,
                CriticalCooldownSeconds = criticalCooldownSeconds,
                CriticalSafetyPenalty = criticalSafetyPenalty,
                WaterSafetyPenalty = waterSafetyPenalty,
                TryStartSafetyPenalty = tryStartSafetyPenalty,
                CriticalDecisionPenalty = criticalDecisionPenalty,
                MissingAlarmPenalty = missingAlarmPenalty,
                MissingMainPenalty = missingMainPenalty,
                MissingLockoutPenalty = missingLockoutPenalty,
                MissingConesPenalty = missingConesPenalty,
                MissingContainmentPenalty = missingContainmentPenalty,
                TimeoutPenalty = timeoutPenalty,
                ExecutionEStop = executionEStop,
                ExecutionAlarm = executionAlarm,
                ExecutionIsolation = executionIsolation,
                ExecutionZeroEnergy = executionZeroEnergy,
                ExecutionPpe = executionPpe,
                ExecutionContainment = executionContainment,
                ExecutionExit = executionExit
            };
        }
    }
}
