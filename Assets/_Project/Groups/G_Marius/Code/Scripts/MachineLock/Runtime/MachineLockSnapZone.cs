using System.Collections.Generic;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Snap target of the spill containment (GDD §8.3 and §9.8): one cone marker around the puddle or one
    /// absorbent coverage quadrant. The zone owns its occupancy, its proactive Guided baseline floor ring/outline,
    /// and its full Guided snap ghost mesh; the item scripts and <see cref="MachineLockSpillSystem"/> only ask
    /// and report. No physics, no collider and no Meta SDK: the item moves itself to <see cref="SnapPoint"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Snap Zone")]
    public sealed class MachineLockSnapZone : MonoBehaviour
    {
        /// <summary>Kind of item a zone accepts.</summary>
        public enum SnapKind
        {
            /// <summary>Delimitation cone marker around the puddle.</summary>
            Cone,
            /// <summary>Absorbent mat or sock coverage quadrant.</summary>
            Absorbent
        }

        private static readonly List<MachineLockSnapZone> ACTIVE = new List<MachineLockSnapZone>(8);

        [Header("Zone")]
        [Tooltip("Kind of item this zone accepts.")]
        [SerializeField] private SnapKind kind = SnapKind.Cone;
        [Tooltip("Point where the item is placed. Empty: this transform.")]
        [SerializeField] private Transform snapPoint;
        [Tooltip("Subtle floor ring/outline shown proactively in Guided mode once full PPE is worn.")]
        [SerializeField] private GameObject baselineVisual;
        [Tooltip("Bright transparent Guided ghost shown while the player holds a matching item.")]
        [SerializeField] private GameObject ghostVisual;
        [Tooltip("Puddle quadrant covered by an absorbent, 0 to 3. -1 for cone zones, which cover no quadrant.")]
        [SerializeField] private int quadrantIndex = -1;

        /// <summary>Every enabled zone of the open scene, in no particular order.</summary>
        public static IReadOnlyList<MachineLockSnapZone> All => ACTIVE;

        /// <summary>Kind of item this zone accepts.</summary>
        public SnapKind Kind => kind;

        /// <summary>true after an item was placed on this zone. A free zone is never reused.</summary>
        public bool Occupied { get; private set; }

        /// <summary>Point where the item is placed: the configured point, or this transform.</summary>
        public Transform SnapPoint => snapPoint != null ? snapPoint : transform;

        /// <summary>Puddle quadrant covered by an absorbent, 0 to 3; -1 for cone zones.</summary>
        public int QuadrantIndex => quadrantIndex;

        /// <summary>true while the proactive Guided baseline floor marker is currently visible.</summary>
        public bool IsBaselineVisible => baselineVisual != null && baselineVisual.activeSelf;

        /// <summary>true while the bright full snap ghost is currently visible.</summary>
        public bool IsGhostVisible => ghostVisual != null && ghostVisual.activeSelf;

        /// <summary>
        /// Configures the zone from code, for the scene builder or tests. Both visuals start hidden.
        /// </summary>
        public void Configure(SnapKind snapKind, Transform point = null, GameObject ghost = null,
            int quadrant = -1, GameObject baseline = null)
        {
            kind = snapKind;
            snapPoint = point;
            ghostVisual = ghost;
            quadrantIndex = quadrant;
            baselineVisual = baseline;
            SetBaselineVisible(false);
            SetGhostVisible(false);
        }

        /// <summary>
        /// Shows or hides the subtle proactive Guided baseline marker. An occupied zone never shows it.
        /// </summary>
        public void SetBaselineVisible(bool visible)
        {
            if (baselineVisual == null) return;
            bool show = visible && !Occupied;
            if (baselineVisual.activeSelf != show) baselineVisual.SetActive(show);
        }

        /// <summary>
        /// Shows or hides the bright Guided snap ghost. An occupied zone never shows it.
        /// </summary>
        public void SetGhostVisible(bool visible)
        {
            if (ghostVisual == null) return;
            bool show = visible && !Occupied;
            if (ghostVisual.activeSelf != show) ghostVisual.SetActive(show);
        }

        /// <summary>
        /// Marks the zone as taken and hides both its baseline marker and its snap ghost.
        /// </summary>
        public bool Occupy()
        {
            if (Occupied) return false;
            Occupied = true;
            SetBaselineVisible(false);
            SetGhostVisible(false);
            return true;
        }

        /// <summary>
        /// Finds the closest free zone of a kind within a radius, searching the enabled zones of the scene.
        /// </summary>
        public static MachineLockSnapZone FindNearestUnoccupied(Vector3 worldPosition, SnapKind snapKind, float maxRadius)
        {
            MachineLockSnapZone best = null;
            float bestSqr = Mathf.Max(0f, maxRadius) * Mathf.Max(0f, maxRadius);
            for (int i = 0; i < ACTIVE.Count; i++)
            {
                MachineLockSnapZone zone = ACTIVE[i];
                if (zone == null || zone.Occupied || zone.Kind != snapKind) continue;
                float sqr = (zone.SnapPoint.position - worldPosition).sqrMagnitude;
                if (sqr > bestSqr) continue;
                best = zone;
                bestSqr = sqr;
            }
            return best;
        }

        private void Awake()
        {
            SetBaselineVisible(false);
            SetGhostVisible(false);
        }

        private void OnEnable()
        {
            if (!ACTIVE.Contains(this)) ACTIVE.Add(this);
        }

        private void OnDisable()
        {
            ACTIVE.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ACTIVE.Clear();
        }
    }
}
