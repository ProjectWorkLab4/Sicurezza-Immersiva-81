using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// PM-1: primo paramedico, seduto vicino al generatore, sotto shock. Il giocatore lo accompagna al punto sicuro
    /// tenendo il grilletto su di lui per 3 s (poi lo segue); in alternativa lo porta via PM-2.
    /// </summary>
    [RequireComponent(typeof(NpcMover))]
    public class ParamedicInShock : TempInteractable
    {
        [Tooltip("Secondi di grilletto per convincerlo a seguirti.")]
        [SerializeField] private float holdToEscort = 3f;
        [Tooltip("Velocità di accompagnamento (m/s).")]
        [SerializeField] private float escortSpeed = 0.8f;
        [Tooltip("Corpo (si alza quando cammina).")]
        [SerializeField] private Transform body;

        private NpcMover mover;
        private TempHand holdingHand;
        private float holdStart;

        /// <summary>true mentre segue qualcuno.</summary>
        public bool IsEscorted => mover != null && mover.FollowTarget != null;
        /// <summary>true quando è arrivato al punto sicuro.</summary>
        public bool IsSafe { get; private set; }
        /// <summary>Chi lo ha messo in salvo: "player" o "pm2" (vuoto se nessuno).</summary>
        public string SavedBy { get; private set; } = "";
        /// <summary>Ha iniziato a seguire il giocatore.</summary>
        public event Action EscortStarted;

        /// <summary>Riferimento al movimento.</summary>
        private void Awake() => mover = GetComponent<NpcMover>();

        /// <summary>Grilletto premuto su PM-1.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand)
        {
            if (IsSafe || IsEscorted) return;
            holdingHand = hand;
            holdStart = Time.time;
            ElectricShockHud.Line("Tu", "Vieni con me, ti porto al sicuro.");
        }

        /// <summary>Grilletto rilasciato prima dei 3 s: annulla.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseEnd(TempHand hand) => holdingHand = null;

        /// <summary>Inizia a seguire il bersaglio (giocatore o PM-2).</summary>
        /// <param name="target">Chi seguire.</param>
        public void FollowTarget(Transform target)
        {
            if (IsSafe) return;
            if (body != null) body.localScale = Vector3.one;
            mover.Follow(target, 1f, escortSpeed);
        }

        /// <summary>Segna PM-1 come al sicuro e lo ferma.</summary>
        /// <param name="by">"player" o "pm2".</param>
        public void MarkSafe(string by)
        {
            if (IsSafe) return;
            IsSafe = true;
            SavedBy = by;
            mover.Stop();
        }

        /// <summary>Dopo 3 s di grilletto inizia a seguire il giocatore.</summary>
        private void Update()
        {
            if (holdingHand == null || Time.time - holdStart < holdToEscort) return;
            holdingHand = null;
            var rig = TempRig.Instance;
            if (rig == null) return;
            FollowTarget(rig.Head);
            ElectricShockHud.Line("PM-1", "Va bene... non riesco a muovermi bene.");
            EscortStarted?.Invoke();
        }
    }
}
