using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] Movimento semplice degli NPC: cammina lungo dei punti, oppure segue un bersaglio a distanza fissa.
    /// Ogni tratto passa dalla NavMesh della scena (gira attorno a generatore, rack, collega a terra e fuoco);
    /// se la NavMesh manca, va in linea retta.
    /// </summary>
    public class NpcMover : MonoBehaviour
    {
        [Tooltip("Velocità di camminata (m/s).")]
        [SerializeField] private float walkSpeed = 1.4f;
        [Tooltip("Ogni quanti secondi ricalcolare il percorso verso il bersaglio seguito.")]
        [SerializeField] private float followRepathSeconds = 0.4f;

        private readonly Queue<Vector3> waypoints = new Queue<Vector3>();
        private readonly Queue<Vector3> corners = new Queue<Vector3>();
        private NavMeshPath navPath;
        private Action onArrived;
        private Transform followTarget;
        private float followDistance;
        private float nextRepath;
        private float speed;

        /// <summary>true mentre l'NPC si muove o segue qualcuno.</summary>
        public bool IsMoving => corners.Count > 0 || waypoints.Count > 0 || followTarget != null;
        /// <summary>Bersaglio seguito (null se nessuno).</summary>
        public Transform FollowTarget => followTarget;
        /// <summary>Velocità di camminata di default.</summary>
        public float WalkSpeed => walkSpeed;
        /// <summary>Se impostato, viene chiamato prima di ogni passo: false blocca il passo (es. barriera).</summary>
        public Func<Vector3, Vector3, bool> CanStep { get; set; }

        /// <summary>Cammina lungo i punti indicati.</summary>
        /// <param name="points">Punti in ordine (y ignorata).</param>
        /// <param name="speedOverride">Velocità (0 = di default).</param>
        /// <param name="arrived">Chiamata all'arrivo sull'ultimo punto.</param>
        public void WalkPath(IEnumerable<Vector3> points, float speedOverride = 0f, Action arrived = null)
        {
            Stop();
            foreach (var p in points) waypoints.Enqueue(new Vector3(p.x, transform.position.y, p.z));
            speed = speedOverride > 0f ? speedOverride : walkSpeed;
            onArrived = arrived;
            PlanNextLeg();
            if (corners.Count == 0 && waypoints.Count == 0) Arrive();
        }

        /// <summary>Segue un bersaglio restando a una certa distanza.</summary>
        /// <param name="target">Bersaglio.</param>
        /// <param name="distance">Distanza da mantenere (m).</param>
        /// <param name="speedOverride">Velocità (0 = di default).</param>
        public void Follow(Transform target, float distance, float speedOverride = 0f)
        {
            Stop();
            followTarget = target;
            followDistance = distance;
            speed = speedOverride > 0f ? speedOverride : walkSpeed;
            nextRepath = 0f;
        }

        /// <summary>Si ferma subito.</summary>
        public void Stop()
        {
            waypoints.Clear();
            corners.Clear();
            followTarget = null;
            onArrived = null;
        }

        /// <summary>Muove l'NPC lungo gli angoli del percorso.</summary>
        private void Update()
        {
            if (followTarget != null)
            {
                Vector3 target = Flat(followTarget.position, transform.position.y);
                if ((target - transform.position).magnitude <= followDistance) { corners.Clear(); return; }
                if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + followRepathSeconds;
                    corners.Clear();
                    PlanTo(target);
                }
            }

            if (corners.Count == 0)
            {
                if (followTarget != null) return;
                if (waypoints.Count == 0) return;
                PlanNextLeg();
                if (corners.Count == 0) { Arrive(); return; }
            }

            Vector3 pos = transform.position;
            Vector3 goal = corners.Peek();
            Vector3 to = goal - pos;
            float dist = to.magnitude;
            if (followTarget != null)
            {
                // Non avvicinarsi oltre la distanza di accompagnamento.
                float toTarget = (Flat(followTarget.position, pos.y) - pos).magnitude;
                dist = Mathf.Min(dist, Mathf.Max(0f, toTarget - followDistance) + 0.001f);
            }
            if (dist <= 0.02f)
            {
                corners.Dequeue();
                if (corners.Count == 0 && waypoints.Count == 0 && followTarget == null) Arrive();
                return;
            }

            Vector3 step = to.normalized * Mathf.Min(speed * Time.deltaTime, dist);
            if (CanStep != null && !CanStep(pos, pos + step)) return;
            transform.position = pos + step;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 10f * Time.deltaTime);
        }

        /// <summary>Prende il prossimo punto e calcola il tratto sulla NavMesh.</summary>
        private void PlanNextLeg()
        {
            while (corners.Count == 0 && waypoints.Count > 0)
            {
                Vector3 next = waypoints.Dequeue();
                if ((next - transform.position).sqrMagnitude < 0.0004f) continue;
                PlanTo(next);
            }
        }

        /// <summary>Riempie gli angoli del percorso verso un punto (NavMesh se c'è, altrimenti linea retta).</summary>
        private void PlanTo(Vector3 destination)
        {
            if (navPath == null) navPath = new NavMeshPath();
            Vector3 from = transform.position;
            if (NavMesh.SamplePosition(from, out var a, 1.5f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(destination, out var b, 1.5f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, navPath)
                && navPath.status != NavMeshPathStatus.PathInvalid && navPath.corners.Length > 1)
            {
                for (int i = 1; i < navPath.corners.Length; i++) corners.Enqueue(Flat(navPath.corners[i], from.y));
                // L'ultimo angolo è il punto della NavMesh più vicino: finisce esattamente sulla destinazione se raggiungibile.
                if (navPath.status == NavMeshPathStatus.PathComplete) corners.Enqueue(Flat(destination, from.y));
                return;
            }
            corners.Enqueue(Flat(destination, from.y));
        }

        /// <summary>Arrivo sull'ultimo punto: chiama la callback una sola volta.</summary>
        private void Arrive()
        {
            var cb = onArrived;
            onArrived = null;
            cb?.Invoke();
        }

        /// <summary>Punto alla quota indicata.</summary>
        private static Vector3 Flat(Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
