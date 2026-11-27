using System.Collections.Generic;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Hot oil spill of the Machine Lock scenario (GDD §8.3, §9.7, §9.8, §11.2 and §11.4). It owns the
    /// leak jet, the puddle quadrants, the containment counters, the snap zones of the spill kit and the
    /// two hazards of the puddle: approaching the danger zone and touching the oil.
    /// Contact severity is resolved once per contact, in this order: energised oil, wrong PPE, hot oil
    /// without full PPE. The same contact never stacks; the latch clears when the player steps out.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Spill System")]
    public sealed class MachineLockSpillSystem : MonoBehaviour
    {
        private const int CONE_TARGET = 4;
        private const int ABSORBENT_TARGET = 4;
        private const float CHECK_INTERVAL = 0.1f;

        [Header("Scenario")]
        [Tooltip("Scenario that receives the containment, approach and contact events.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Tunable values. Provides the snap radius fallback when a caller passes no radius.")]
        [SerializeField] private MachineLockConfig config;

        [Header("Hazard sources")]
        [Tooltip("Main panel and residual energy of the oil. null: the oil is treated as not energised.")]
        [SerializeField] private MachineLockEnergyPanel energyPanel;
        [Tooltip("PPE cabinet. null: the player is treated as not protected.")]
        [SerializeField] private MachineLockPpeCabinet ppeCabinet;

        [Header("Leak")]
        [Tooltip("Jet particle object. Deactivated when the valve closes.")]
        [SerializeField] private GameObject oilJetVfx;
        [Tooltip("Looping leak audio. Stopped when the valve closes.")]
        [SerializeField] private AudioSource oilLeakAudio;

        [Header("Puddle")]
        [Tooltip("The four quadrant meshes of the puddle. Each absorbent hides one.")]
        [SerializeField] private GameObject[] puddleQuadrants = new GameObject[4];
        [Tooltip("Hazard trigger over the oil. Disabled once all four absorbents are placed.")]
        [SerializeField] private Collider puddleHazardTrigger;
        [Tooltip("Radius around the puddle centre that raises the approach warning, in metres.")]
        [SerializeField] private float approachRadius = 4f;

        [Header("Snap zones")]
        [Tooltip("Cone markers and absorbent coverage zones. Empty: every zone of the scene is collected.")]
        [SerializeField] private List<MachineLockSnapZone> snapZones = new List<MachineLockSnapZone>();

        private bool approachBeforeEStopApplied;
        private bool approachLiveApplied;
        private bool contactLatched;
        private bool playerInsideApproach;
        private float nextCheck;
        private Transform head;
        private int heldCones;
        private int heldAbsorbents;
        private bool containmentWayfindingActive;

        /// <summary>true after the leak valve was closed.</summary>
        public bool ValveClosed { get; private set; }

        /// <summary>Number of cones placed on the delimitation zones, 0 to 4.</summary>
        public int ConesPlaced { get; private set; }

        /// <summary>Number of absorbents placed on the coverage zones, 0 to 4.</summary>
        public int AbsorbentsPlaced { get; private set; }

        /// <summary>true when four cones and four absorbents are placed (GDD §9.8).</summary>
        public bool ContainmentComplete => ConesPlaced >= CONE_TARGET && AbsorbentsPlaced >= ABSORBENT_TARGET;

        /// <summary>true when proactive Guided baseline floor rings are active.</summary>
        public bool ContainmentWayfindingActive => containmentWayfindingActive;

        // =====================================================================
        // Guided wayfinding and ghosts
        // =====================================================================

        /// <summary>
        /// Activates or deactivates proactive Guided baseline floor rings/outlines on all unoccupied
        /// Containment Zones (4 cone corners + 4 absorbent quadrants). Shown only in Guided mode once full
        /// PPE is worn; hidden individually as zones are occupied and always hidden outside Guided.
        /// </summary>
        /// <param name="active">true once full PPE is worn.</param>
        public void SetContainmentWayfindingActive(bool active)
        {
            bool guided = scenario == null || scenario.Level == DifficultyLevel.Guided;
            containmentWayfindingActive = guided && active && !ContainmentComplete;
            for (int i = 0; i < snapZones.Count; i++)
            {
                MachineLockSnapZone zone = snapZones[i];
                if (zone == null) continue;
                zone.SetBaselineVisible(containmentWayfindingActive);
            }
        }

        /// <summary>
        /// Shows the ghost of every free zone of a kind while the player holds a matching item. Ghosts are
        /// Guided only: in every other level, and when nothing is held, they are hidden.
        /// </summary>
        /// <param name="kind">Kind of the held item.</param>
        /// <param name="holding">true while the item is grabbed.</param>
        public void SetHeldSnapKind(MachineLockSnapZone.SnapKind kind, bool holding)
        {
            // Two hands can hold two items of the same kind: the ghosts stay on until the last one is
            // released, so a release never hides the hint of the item still in the other hand.
            if (kind == MachineLockSnapZone.SnapKind.Cone)
                heldCones = Mathf.Max(0, heldCones + (holding ? 1 : -1));
            else
                heldAbsorbents = Mathf.Max(0, heldAbsorbents + (holding ? 1 : -1));

            int held = kind == MachineLockSnapZone.SnapKind.Cone ? heldCones : heldAbsorbents;
            bool guided = scenario == null || scenario.Level == DifficultyLevel.Guided;
            bool show = guided && held > 0;

            for (int i = 0; i < snapZones.Count; i++)
            {
                MachineLockSnapZone zone = snapZones[i];
                if (zone == null || zone.Kind != kind) continue;
                zone.SetGhostVisible(show);
            }
        }

        // =====================================================================
        // Snap zones
        // =====================================================================

        /// <summary>
        /// Finds the closest free zone of a kind within a radius.
        /// </summary>
        /// <param name="worldPos">Release position of the item.</param>
        /// <param name="kind">Kind of zone to look for.</param>
        /// <param name="maxRadius">Maximum distance in metres. 0 or less uses the config snap radius.</param>
        /// <returns>The closest free zone, or null.</returns>
        public MachineLockSnapZone FindNearestZone(Vector3 worldPos, MachineLockSnapZone.SnapKind kind, float maxRadius)
        {
            float radius = maxRadius > 0f ? maxRadius : (config != null ? config.snapRadius : 0f);
            MachineLockSnapZone best = null;
            float bestSqr = radius * radius;

            for (int i = 0; i < snapZones.Count; i++)
            {
                MachineLockSnapZone zone = snapZones[i];
                if (zone == null || zone.Occupied || zone.Kind != kind) continue;
                float sqr = (zone.SnapPoint.position - worldPos).sqrMagnitude;
                if (sqr > bestSqr) continue;
                best = zone;
                bestSqr = sqr;
            }

            if (best != null || snapZones.Count > 0) return best;
            return MachineLockSnapZone.FindNearestUnoccupied(worldPos, kind, radius);
        }

        // =====================================================================
        // Leak and containment
        // =====================================================================

        /// <summary>
        /// Closes the leak: stops the jet and the leak loop and records the closed valve. Idempotent.
        /// </summary>
        public void CloseValve()
        {
            if (oilJetVfx != null && oilJetVfx.activeSelf) oilJetVfx.SetActive(false);
            if (oilLeakAudio != null) oilLeakAudio.Stop();
            ValveClosed = true;
        }

        /// <summary>
        /// Records a cone on a delimitation zone and reports the new count to the scenario.
        /// </summary>
        /// <param name="zone">Zone that received the cone.</param>
        public void RegisterCone(MachineLockSnapZone zone)
        {
            ConesPlaced = Mathf.Clamp(ConesPlaced + 1, 0, CONE_TARGET);
            if (scenario != null) scenario.NotifyConePlaced(ConesPlaced);
        }

        /// <summary>
        /// Records a mat or sock on a coverage zone, hides the covered quadrant, disables the hazard trigger
        /// at four absorbents and shows the Mat/Sock feedback once.
        /// </summary>
        /// <param name="zone">Zone that received the absorbent.</param>
        /// <param name="kind">Mat or sock, used for the feedback line.</param>
        public void RegisterAbsorbent(MachineLockSnapZone zone, MachineLockAbsorbent.AbsorbentKind kind)
        {
            AbsorbentsPlaced = Mathf.Clamp(AbsorbentsPlaced + 1, 0, ABSORBENT_TARGET);

            HideCoveredQuadrant(zone);
            if (AbsorbentsPlaced >= ABSORBENT_TARGET && puddleHazardTrigger != null) puddleHazardTrigger.enabled = false;

            if (scenario == null) return;
            string line = kind == MachineLockAbsorbent.AbsorbentKind.Mat ? MachineLockTexts.Mat : MachineLockTexts.Sock;
            scenario.Feedback(line, FeedbackKind.Positive);
            scenario.NotifyAbsorbentPlaced(AbsorbentsPlaced);
        }

        // =====================================================================
        // Approach and contact hazards
        // =====================================================================

        /// <summary>
        /// Player entered the danger zone around the puddle before the area was contained. Applies the
        /// Decision penalties of GDD §11.4 once per run: approaching before the E-stop, and approaching live
        /// oil before the main switch is opened.
        /// </summary>
        public void NotifyPlayerEnteredApproachZone()
        {
            if (ContainmentComplete || scenario == null) return;
            MachineLockSpi spi = scenario.Spi;
            if (spi == null) return;

            bool applied = false;

            if (!scenario.TaskEStop && !approachBeforeEStopApplied)
            {
                approachBeforeEStopApplied = true;
                spi.SetElapsed(scenario.ElapsedSeconds);
                spi.ApplyEvent(SpiEvent.ApproachBeforeEStop);
                applied = true;
            }

            if (energyPanel != null && energyPanel.OilEnergized && !scenario.TaskMainOff && !approachLiveApplied)
            {
                approachLiveApplied = true;
                spi.SetElapsed(scenario.ElapsedSeconds);
                spi.ApplyEvent(SpiEvent.ApproachLiveBeforeMainOff);
                applied = true;
            }

            if (applied) scenario.RefreshObjectives();
        }

        /// <summary>
        /// Player touched the oil. Applies the most severe contact only, once per contact: energised oil is a
        /// shock whatever the PPE, wrong PPE is a burn, and hot oil without the full PPE set is a burn.
        /// A contained puddle, or four placed absorbents, remove the hazard.
        /// </summary>
        public void NotifyPuddleContact()
        {
            if (ContainmentComplete || AbsorbentsPlaced >= ABSORBENT_TARGET) return;
            if (contactLatched || scenario == null) return;
            contactLatched = true;

            if (energyPanel != null && energyPanel.OilEnergized)
            {
                scenario.NotifyCritical(CriticalEvent.LiveOilShock);
                return;
            }

            if (ppeCabinet != null && ppeCabinet.HasWrongPpeEquipped)
            {
                scenario.NotifyCritical(CriticalEvent.WrongPpeContact);
                return;
            }

            if (ppeCabinet == null || !ppeCabinet.HasFullPpe)
            {
                scenario.NotifyCritical(CriticalEvent.HotOilBurn);
            }
        }

        private void Awake()
        {
            if (snapZones == null) snapZones = new List<MachineLockSnapZone>();
            if (snapZones.Count == 0) snapZones.AddRange(GetComponentsInChildren<MachineLockSnapZone>(true));
            if (snapZones.Count == 0)
            {
                MachineLockSnapZone[] found = FindObjectsByType<MachineLockSnapZone>(FindObjectsSortMode.None);
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null) snapZones.Add(found[i]);
                }
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CHECK_INTERVAL;

            head = WorldPlacement.ResolveHead(head, "[SI81 MachineLock] MachineLockSpillSystem");
            if (head == null) return;

            Vector3 playerPosition = head.position;
            Vector3 center = PuddleCenter;
            bool contained = ContainmentComplete;

            bool insideApproach = !contained && HorizontalDistance(playerPosition, center) <= approachRadius;
            if (insideApproach && !playerInsideApproach) NotifyPlayerEnteredApproachZone();
            // Do not latch the entry state before the SPI exists, otherwise a player already inside the
            // danger zone at start would never receive the approach penalty.
            playerInsideApproach = insideApproach && scenario != null && scenario.Spi != null;

            if (!contained && IsInsidePuddle(playerPosition)) NotifyPuddleContact();
            else contactLatched = false;
        }

        /// <summary>
        /// Contact fallback for setups where the hazard trigger shares this GameObject and raises the
        /// trigger message here.
        /// </summary>
        /// <param name="other">Collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !IsPlayerCollider(other)) return;
            NotifyPuddleContact();
        }

        /// <summary>
        /// Clears the contact latch when the player leaves the hazard trigger, so a new contact can be
        /// penalised again.
        /// </summary>
        /// <param name="other">Collider that left the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            if (other == null || !IsPlayerCollider(other)) return;
            contactLatched = false;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>Centre of the puddle: the hazard trigger, the first quadrant or this transform.</summary>
        private Vector3 PuddleCenter
        {
            get
            {
                if (puddleHazardTrigger != null) return puddleHazardTrigger.transform.position;
                if (puddleQuadrants != null && puddleQuadrants.Length > 0 && puddleQuadrants[0] != null)
                    return puddleQuadrants[0].transform.position;
                return transform.position;
            }
        }

        /// <summary>
        /// true when the head is inside the hazard volume in the horizontal plane. Used as the contact
        /// fallback when the trigger collider is not on this GameObject.
        /// </summary>
        private bool IsInsidePuddle(Vector3 worldPosition)
        {
            if (puddleHazardTrigger == null) return false;
            Bounds bounds = puddleHazardTrigger.bounds;
            worldPosition.y = bounds.center.y;
            return bounds.Contains(worldPosition);
        }

        private void HideCoveredQuadrant(MachineLockSnapZone zone)
        {
            if (puddleQuadrants == null || puddleQuadrants.Length == 0) return;

            int index = zone != null ? zone.QuadrantIndex : -1;
            if (index >= 0 && index < puddleQuadrants.Length && puddleQuadrants[index] != null)
            {
                puddleQuadrants[index].SetActive(false);
                return;
            }

            for (int i = 0; i < puddleQuadrants.Length; i++)
            {
                if (puddleQuadrants[i] == null || !puddleQuadrants[i].activeSelf) continue;
                puddleQuadrants[i].SetActive(false);
                return;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// true when the collider belongs to the player rig: a CharacterController, the Player tag or a
        /// root that carries the shared <see cref="RigRegistration"/>.
        /// </summary>
        private static bool IsPlayerCollider(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() != null) return true;
            if (other.CompareTag("Player")) return true;
            if (other.GetComponentInParent<RigRegistration>() != null) return true;
            return other.transform.root.GetComponentInChildren<RigRegistration>(true) != null;
        }
    }
}
