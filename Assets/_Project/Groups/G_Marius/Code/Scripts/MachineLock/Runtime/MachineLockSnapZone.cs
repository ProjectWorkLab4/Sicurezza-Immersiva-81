using System.Collections.Generic;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Snap target of the spill containment (GDD §8.3 and §9.8): one cone marker around the puddle or one
    /// absorbent coverage quadrant. The zone owns its occupancy and its optional Guided ghost mesh; the
    /// item scripts and <see cref="MachineLockSpillSystem"/> only ask and report.
    /// No physics, no collider and no Meta SDK: the item moves itself to <see cref="SnapPoint"/>.
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

        // Registry of the enabled zones, so a builder that forgets to wire the spill system still lets the
        // item find its target without a scene search on every release.
        private static readonly List<MachineLockSnapZone> ACTIVE = new List<MachineLockSnapZone>(8);

        [Header("Zone")]
        [Tooltip("Kind of item this zone accepts.")]
        [SerializeField] private SnapKind kind = SnapKind.Cone;
        [Tooltip("Point where the item is placed. Empty: this transform.")]
        [SerializeField] private Transform snapPoint;
        [Tooltip("Transparent Guided ghost of the item. Shown while the player holds a matching item near this free zone.")]
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

        /// <summary>
        /// Configures the zone from code, for the scene builder. The ghost starts hidden.
        /// </summary>
        /// <param name="snapKind">Kind of item the zone accepts.</param>
        /// <param name="point">Placement point; null uses this transform.</param>
        /// <param name="ghost">Guided ghost mesh; may be null.</param>
        /// <param name="quadrant">Puddle quadrant covered by an absorbent, or -1.</param>
        public void Configure(SnapKind snapKind, Transform point = null, GameObject ghost = null, int quadrant = -1)
        {
            kind = snapKind;
            snapPoint = point;
            ghostVisual = ghost;
            quadrantIndex = quadrant;
            SetGhostVisible(false);
        }

        /// <summary>
        /// Shows or hides the Guided ghost. An occupied zone never shows it, whatever the caller asks.
        /// </summary>
        /// <param name="visible">true to show the ghost on a free zone.</param>
        public void SetGhostVisible(bool visible)
        {
            if (ghostVisual == null) return;
            bool show = visible && !Occupied;
            if (ghostVisual.activeSelf != show) ghostVisual.SetActive(show);
        }

        /// <summary>
        /// Marks the zone as taken and hides its ghost. The first caller wins: a second item cannot take
        /// the same zone.
        /// </summary>
        /// <returns>true when this call occupied the zone, false when it was already occupied.</returns>
        public bool Occupy()
        {
            if (Occupied) return false;
            Occupied = true;
            SetGhostVisible(false);
            return true;
        }

        /// <summary>
        /// Finds the closest free zone of a kind within a radius, searching the enabled zones of the scene.
        /// Fallback used by the items when no <see cref="MachineLockSpillSystem"/> is wired.
        /// </summary>
        /// <param name="worldPosition">Release position of the item.</param>
        /// <param name="snapKind">Kind of zone to look for.</param>
        /// <param name="maxRadius">Maximum distance in metres.</param>
        /// <returns>The closest free zone, or null.</returns>
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

        /// <summary>
        /// Clears the registry before every play session, so a domain reload that is disabled cannot leave
        /// stale zones behind.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ACTIVE.Clear();
        }
    }
}
