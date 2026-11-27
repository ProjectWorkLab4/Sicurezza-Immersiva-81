using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Verifica che sulle polilinee ROUTE_R1_*/ROUTE_R2_* esista una corsia libera larga
    /// routeLaneWidth: cerca uno spostamento laterale in [-1.5, +1.5] m (passo 0.25)
    /// che non intersechi muri né focolai attivi.
    /// </summary>
    public sealed class EvacFireLaneValidator : MonoBehaviour
    {
        private const float LateralMin = -1.5f;
        private const float LateralMax = 1.5f;
        private const float LateralStep = 0.25f;
        private const float SampleStep = 0.25f;
        private const float BoxHeight = 1.8f;
        private const float BoxCenterY = 1f;

        [SerializeField] private Transform markersRoot;
        [SerializeField] private Collider[] wallColliders = new Collider[0];

        private readonly List<Vector3[]> routes = new List<Vector3[]>();

        /// <summary>Polilinee delle rotte ideali, ordinate per nome.</summary>
        public IReadOnlyList<Vector3[]> Routes => routes;

        private void Awake()
        {
            if (markersRoot == null)
            {
                GameObject root = GameObject.Find("ScenarioMarkers");
                if (root != null)
                    markersRoot = root.transform;
            }
            if (wallColliders == null || wallColliders.Length == 0)
            {
                GameObject env = GameObject.Find("Environment");
                if (env != null)
                {
                    Collider[] all = env.GetComponentsInChildren<Collider>(true)
                        .Where(c => !c.isTrigger && c.name.StartsWith("Wall_"))
                        .ToArray();
                    Collider[] lintels = all.Where(c => c.name.Contains("Lintel")).ToArray();
                    // I montanti delle porte (segmenti vicini a un lintel) non sono ostacoli:
                    // la porta si apre, la corsia può passarci attraverso.
                    wallColliders = all.Where(c => !c.name.Contains("Lintel") && !TouchesLintel(c, lintels)).ToArray();
                }
            }
            BuildRoutes();
        }

        private static bool TouchesLintel(Collider segment, Collider[] lintels)
        {
            Bounds b = segment.bounds;
            for (int i = 0; i < lintels.Length; i++)
            {
                Bounds l = lintels[i].bounds;
                bool nearZ = Mathf.Abs(l.min.z - b.min.z) < 0.25f && Mathf.Abs(l.max.z - b.max.z) < 0.25f;
                bool nearX = l.min.x <= b.max.x + 0.01f && l.max.x >= b.min.x - 0.01f;
                if (nearZ && nearX)
                    return true;
            }
            return false;
        }

        private void BuildRoutes()
        {
            routes.Clear();
            if (markersRoot == null)
                return;

            foreach (string prefix in new[] { "ROUTE_R1_", "ROUTE_R2_" })
            {
                Transform[] markers = markersRoot.Cast<Transform>()
                    .Where(t => t.name.StartsWith(prefix))
                    .OrderBy(t => t.name, System.StringComparer.Ordinal)
                    .ToArray();
                if (markers.Length < 2)
                    continue;

                var points = new Vector3[markers.Length];
                for (int i = 0; i < markers.Length; i++)
                    points[i] = markers[i].position;
                routes.Add(points);
            }
        }

        /// <summary>True se ogni segmento ha almeno una corsia laterale libera.</summary>
        public bool IsLaneFree(IList<EvacFireSlot> activeFires, float laneWidth)
        {
            if (routes.Count == 0)
                BuildRoutes();
            float width = Mathf.Max(0.2f, laneWidth);

            for (int r = 0; r < routes.Count; r++)
            {
                Vector3[] points = routes[r];
                for (int s = 0; s < points.Length - 1; s++)
                {
                    if (!SegmentHasFreeLane(points[s], points[s + 1], width, activeFires))
                        return false;
                }
            }
            return true;
        }

        private bool SegmentHasFreeLane(Vector3 from, Vector3 to, float width, IList<EvacFireSlot> activeFires)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.01f)
                return true;

            Vector3 forward = delta / length;
            Vector3 lateral = new Vector3(-forward.z, 0f, forward.x);
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));

            for (float offset = LateralMin; offset <= LateralMax + 0.001f; offset += LateralStep)
            {
                bool free = true;
                for (int i = 0; i <= samples; i++)
                {
                    Vector3 point = from + forward * (length * i / samples) + lateral * offset;
                    point.y = BoxCenterY;
                    var box = new Bounds(point, new Vector3(width, BoxHeight, width));
                    if (Hits(box, activeFires))
                    {
                        free = false;
                        break;
                    }
                }
                if (free)
                    return true;
            }
            return false;
        }

        private bool Hits(Bounds box, IList<EvacFireSlot> activeFires)
        {
            if (wallColliders != null)
            {
                for (int i = 0; i < wallColliders.Length; i++)
                {
                    Collider wall = wallColliders[i];
                    if (wall != null && wall.enabled && wall.bounds.Intersects(box))
                        return true;
                }
            }
            if (activeFires == null)
                return false;
            for (int i = 0; i < activeFires.Count; i++)
            {
                EvacFireSlot fire = activeFires[i];
                if (fire != null && fire.IsActive && fire.ContactBounds.Intersects(box))
                    return true;
            }
            return false;
        }
    }
}
