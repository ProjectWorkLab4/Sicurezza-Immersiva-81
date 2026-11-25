using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] Barriera mobile (sostituto del prefab barriera + snap zone dello scenario 07).
    /// Si afferra dal rack RK-01 e, lasciata vicino a uno slot della linea BL-10, si aggancia da sola.
    /// Blocca solo gli NPC: il giocatore la attraversa.
    /// </summary>
    public class BarrierPanel : TempInteractable
    {
        /// <summary>Linea di barriera che controlla gli slot (assegnata dalla linea all'avvio).</summary>
        public BarrierSnapLine Line { get; set; }
        /// <summary>Slot occupato (-1 = nessuno).</summary>
        public int SlotIndex { get; set; } = -1;

        /// <summary>true: si può afferrare.</summary>
        public override bool IsGrabbable => true;

        /// <summary>Afferrata: libera lo slot.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnGrab(TempHand hand)
        {
            base.OnGrab(hand);
            if (Line != null) Line.Detach(this);
        }

        /// <summary>Lasciata: si aggancia allo slot vicino o resta dritta a terra.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnRelease(TempHand hand)
        {
            base.OnRelease(hand);
            if (Line == null || !Line.TrySnap(this)) DropUpright();
        }

        /// <summary>Posa nella mano: tenuta davanti, di lato.</summary>
        public override void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = new Vector3(0f, -0.45f, 0.25f);
            localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
