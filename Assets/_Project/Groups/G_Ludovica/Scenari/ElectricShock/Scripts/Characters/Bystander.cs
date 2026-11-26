using System;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>BY-01/02(/03): collega in zona che, all'evento EV-10-02, tenta di tirare via la vittima.</summary>
    [RequireComponent(typeof(NpcMover))]
    public class Bystander : MonoBehaviour
    {
        /// <summary>Stato del collega.</summary>
        public enum State
        {
            /// <summary>Fermo in zona.</summary>
            Idle,
            /// <summary>Ha gridato e sta per muoversi.</summary>
            Hesitating,
            /// <summary>Cammina verso la vittima.</summary>
            Approaching,
            /// <summary>Ha raggiunto la vittima.</summary>
            AtVictim,
            /// <summary>Esce verso il punto di raccolta.</summary>
            Evacuating,
            /// <summary>Fuori dalla zona (punto di raccolta).</summary>
            Evacuated,
            /// <summary>Ferito.</summary>
            Injured
        }

        private NpcMover mover;
        private float hesitateUntil;
        private Vector3 victimPoint;

        /// <summary>Stato attuale.</summary>
        public State Current { get; private set; } = State.Idle;
        /// <summary>Ha raggiunto la vittima.</summary>
        public event Action<Bystander> ReachedVictim;

        /// <summary>Riferimento al movimento.</summary>
        private void Awake() => mover = GetComponent<NpcMover>();

        /// <summary>Inizio del tentativo: esita, poi cammina verso la vittima.</summary>
        /// <param name="victim">Punto accanto alla vittima.</param>
        /// <param name="hesitation">Secondi di esitazione.</param>
        public void BeginAttempt(Vector3 victim, float hesitation)
        {
            if (Current != State.Idle) return;
            victimPoint = victim;
            hesitateUntil = Time.time + hesitation;
            Current = State.Hesitating;
            var anim = GetComponent<CharacterAnimator>();
            if (anim != null) anim.SetIdleKey("Idle");
        }

        /// <summary>Fermato dal giocatore: esce verso il punto di raccolta.</summary>
        /// <param name="exitPath">Percorso di uscita.</param>
        /// <param name="delay">Secondi di attesa prima di partire (per non partire insieme agli altri).</param>
        public void Evacuate(Vector3[] exitPath, float delay = 0f)
        {
            if (Current == State.Injured || Current == State.Evacuating || Current == State.Evacuated) return;
            Current = State.Evacuating;
            var anim = GetComponent<CharacterAnimator>();
            if (anim != null) anim.SetIdleKey("Idle");
            if (delay > 0f) StartCoroutine(EvacuateAfter(exitPath, delay));
            else mover.WalkPath(exitPath, 0f, () => Current = State.Evacuated);
        }

        /// <summary>Parte dopo l'attesa, se nel frattempo non è stato ferito.</summary>
        private System.Collections.IEnumerator EvacuateAfter(Vector3[] exitPath, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Current == State.Evacuating) mover.WalkPath(exitPath, 0f, () => Current = State.Evacuated);
        }

        /// <summary>Segna il collega come ferito e lo ferma.</summary>
        public void MarkInjured()
        {
            Current = State.Injured;
            mover.Stop();
            var anim = GetComponent<CharacterAnimator>();
            if (anim != null) anim.Force("Hurt");
            else transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);
        }

        /// <summary>Passa dall'esitazione alla camminata.</summary>
        private void Update()
        {
            if (Current != State.Hesitating || Time.time < hesitateUntil) return;
            Current = State.Approaching;
            mover.WalkPath(new[] { victimPoint }, 1f, () =>
            {
                Current = State.AtVictim;
                ReachedVictim?.Invoke(this);
            });
        }
    }
}
