using SI81.Input;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] Barriera mobile da 0,75 m (sostituto del prefab barriera + snap zone dello scenario 07).
    /// Grip per prenderla dal deposito; lasciata vicino a uno slot della linea BL-10 si aggancia da sola.
    /// Blocca solo gli NPC: il giocatore la attraversa.
    /// </summary>
    public class BarrierPanel : ScenarioInteractable
    {
        /// <summary>Linea di barriera che controlla gli slot (assegnata dalla linea all'avvio).</summary>
        public BarrierSnapLine Line { get; set; }
        /// <summary>Slot occupato (-1 = nessuno).</summary>
        public int SlotIndex { get; set; } = -1;

        /// <summary>true: si può afferrare.</summary>
        public override bool IsGrabbable => true;
        /// <summary>Il grilletto non fa nulla sulla barriera.</summary>
        public override bool IsSelectable => false;

        /// <summary>Afferrata: libera lo slot.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnGrabStart(Hand hand)
        {
            if (Line != null) Line.Detach(this);
        }

        /// <summary>Lasciata: si aggancia allo slot vicino o resta dritta a terra.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnGrabEnd(Hand hand)
        {
            if (Line == null || !Line.TrySnap(this)) DropUpright();
            else PlayerLocator.Pulse(hand, 0.5f, 0.08f);
        }

        /// <summary>Posa nella mano: tenuta davanti, di lato.</summary>
        public override void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = new Vector3(0f, -0.45f, 0.25f);
            localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
