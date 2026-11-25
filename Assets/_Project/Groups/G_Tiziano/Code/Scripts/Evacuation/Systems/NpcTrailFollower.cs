using System;
using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Interaction;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Breadcrumb trail for the NPC line: the projected head position is sampled every ~0.25 m and
    /// each following NPC walks the same path 0.8-1 m behind the previous one. No NavMesh.
    /// </summary>
    public sealed class NpcTrailFollower : MonoBehaviour, IResettable
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private NpcRegistry registry;
        [SerializeField] private Transform head;
        [SerializeField, Min(0.1f)] private float breadcrumbSpacing = 0.25f;
        [SerializeField, Min(0.5f)] private float lineSpacing = 0.9f;
        [SerializeField, Min(1f)] private float teleportThreshold = 2.5f;

        private readonly List<Vector3> breadcrumbs = new List<Vector3>();

        /// <summary>Gets the number of sampled breadcrumbs in the current run.</summary>
        public int BreadcrumbCount => breadcrumbs.Count;
        /// <summary>Gets the head transform used for breadcrumbs (resolved lazily so resets never leave it null).</summary>
        public Transform Head
        {
            get
            {
                ResolveHead();
                return head;
            }
        }
        /// <summary>Gets the floor height from the spawn marker.</summary>
        public float GroundY => director != null && director.SpawnMarker != null ? director.SpawnMarker.position.y : 0f;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (registry == null)
                registry = FindFirstObjectByType<NpcRegistry>();
            ResolveHead();
        }

        private void ResolveHead()
        {
            if (head != null)
                return;
            GameObject anchor = GameObject.Find("CenterEyeAnchor");
            if (anchor != null)
                head = anchor.transform;
            else if (Camera.main != null)
                head = Camera.main.transform;
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
            if (director == null || director.Phase != EvacPhase.Action)
                return;
            Tick(Time.deltaTime);
        }

        /// <summary>Advances breadcrumb sampling and the NPC line by one step (also used by the smoke test).</summary>
        public void Tick(float deltaTime)
        {
            if (head == null)
                return;
            RecordBreadcrumb();
            UpdateFollowers(deltaTime);
        }

        /// <summary>Clears the trail without touching the NPC line.</summary>
        public void ResetState()
        {
            breadcrumbs.Clear();
            ResolveHead();
        }

        /// <summary>Prints breadcrumbs and every NPC in line through the logger.</summary>
        public void PrintTrail(Action<string> logger)
        {
            if (logger == null)
                return;
            logger($"[EV] NPC trail: crumbs {breadcrumbs.Count}, line {(registry == null ? 0 : registry.Line.Count)}.");
            if (registry == null)
                return;
            for (int i = 0; i < registry.Line.Count; i++)
            {
                EvacNpc npc = registry.Line[i];
                if (npc != null)
                    logger($"[EV]   #{i} {npc.name}: room {npc.Room}, {npc.State}, {npc.transform.position.ToString("F2")}, slot {SlotDistance(i):0.00} m");
            }
        }

        /// <summary>Distance along the trail assigned to the given line position.</summary>
        public float SlotDistance(int lineIndex)
        {
            return (lineIndex + 1) * lineSpacing;
        }

        private void RecordBreadcrumb()
        {
            Transform currentHead = Head;
            if (currentHead == null)
                return;
            Vector3 point = new Vector3(currentHead.position.x, GroundY, currentHead.position.z);
            if (breadcrumbs.Count == 0)
            {
                breadcrumbs.Add(point);
                return;
            }

            Vector3 last = breadcrumbs[breadcrumbs.Count - 1];
            Vector3 delta = point - last;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > teleportThreshold)
            {
                breadcrumbs.Clear();
                breadcrumbs.Add(point);
                return;
            }
            if (distance >= breadcrumbSpacing)
                breadcrumbs.Add(point);
        }

        private Vector3 TargetForOrder(int lineIndex)
        {
            if (breadcrumbs.Count == 0 || lineIndex < 0)
                return Head != null ? Head.position : transform.position;

            float needed = SlotDistance(lineIndex);
            float walked = 0f;
            for (int i = breadcrumbs.Count - 1; i > 0; i--)
            {
                float segment = Vector3.Distance(breadcrumbs[i], breadcrumbs[i - 1]);
                walked += segment;
                if (walked < needed || segment <= 0f)
                    continue;

                float t = 1f - ((walked - needed) / segment);
                return Vector3.Lerp(breadcrumbs[i], breadcrumbs[i - 1], t);
            }
            return breadcrumbs[0];
        }

        private void UpdateFollowers(float deltaTime)
        {
            if (registry == null)
                return;
            for (int i = 0; i < registry.Line.Count; i++)
            {
                EvacNpc npc = registry.Line[i];
                if (npc != null && npc.State == EvacNpcState.Following)
                    npc.Follow(TargetForOrder(i), deltaTime);
            }
        }
    }
}
