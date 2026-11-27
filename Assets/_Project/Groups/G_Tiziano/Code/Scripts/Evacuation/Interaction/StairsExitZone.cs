using System;
using UnityEngine;
using UnityEngine.Events;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Systems;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Interaction
{
    /// <summary>
    /// Trigger volume anchored to a stairs marker. Detects the player rig/head even without a rig collider
    /// (position check in Update) and reports the reached exit to the Director once per run.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class StairsExitZone : MonoBehaviour, IResettable
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private BoxCollider zoneCollider;
        [SerializeField] private Transform playerHead;
        [SerializeField] private EvacExit exitKind = EvacExit.Emergency;

        [Header("Hooks")]
        [SerializeField] private UnityEvent onReached = new UnityEvent();

        private bool reachedThisRun;

        /// <summary>Raised when the player reaches this zone in the current run.</summary>
        public event Action<StairsExitZone> Reached;

        /// <summary>Which stairs this zone represents.</summary>
        public EvacExit ExitKind => exitKind;
        /// <summary>True once this zone has been reached in the current run.</summary>
        public bool WasReached => reachedThisRun;
        /// <summary>The trigger collider of the zone.</summary>
        public BoxCollider ZoneCollider => zoneCollider;
        /// <summary>Hook invoked on reach.</summary>
        public UnityEvent OnReached => onReached;

        private void Awake()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<BoxCollider>();
            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (playerHead == null && Camera.main != null)
                playerHead = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (director != null)
                director.RegisterResettable(this);
        }

        private void OnDisable()
        {
            if (director != null)
                director.UnregisterResettable(this);
        }

        private void Update()
        {
            if (reachedThisRun || director == null || director.Phase != EvacPhase.Action)
                return;
            if (ContainsPlayer())
                TryReach("position");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (reachedThisRun || director == null || director.Phase != EvacPhase.Action)
                return;
            if (director.RigRoot != null && other.transform.root == director.RigRoot.root)
                TryReach("trigger");
        }

        /// <summary>
        /// Reports the exit if the zone currently contains the rig or the head. Used by debug commands after a teleport.
        /// </summary>
        public bool TryReach(string source = "debug")
        {
            if (reachedThisRun)
                return false;
            if (director == null || director.Phase != EvacPhase.Action)
            {
                UDebug.Log($"[EV] Exit zone {exitKind} ignored ({source}): scenario not in Action.");
                return false;
            }
            if (!ContainsPlayer())
            {
                UDebug.LogWarning($"[EV] Exit zone {exitKind}: player not inside ({source}).", this);
                return false;
            }

            reachedThisRun = true;
            UDebug.Log($"[EV] Exit reached: {exitKind} ({source}).");
            director.ReportExitReached(exitKind);
            onReached.Invoke();
            Reached?.Invoke(this);
            return true;
        }

        /// <summary>True when the rig root or the player head is inside the zone bounds.</summary>
        public bool ContainsPlayer()
        {
            if (zoneCollider == null || director == null)
                return false;

            // Computed from the transform instead of Collider.bounds: a debug teleport can happen before
            // the physics scene syncs, and the cached bounds would still be at the previous position.
            Vector3 centre = zoneCollider.transform.TransformPoint(zoneCollider.center);
            Vector3 size = Vector3.Scale(zoneCollider.size, zoneCollider.transform.lossyScale);
            Bounds bounds = new Bounds(centre, size);
            if (director.RigRoot != null && bounds.Contains(director.RigRoot.position))
                return true;
            return playerHead != null && bounds.Contains(playerHead.position);
        }

        /// <summary>IResettable: clears the per-run reach flag.</summary>
        public void ResetState()
        {
            reachedThisRun = false;
        }
    }
}
