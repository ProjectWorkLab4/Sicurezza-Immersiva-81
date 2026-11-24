using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Dove si trova il giocatore: testa del rig temporaneo se presente (test e prove senza visore), altrimenti la camera
    /// principale, cioè il rig condiviso di SCN_Core. Quando arriva il VR Interaction System basta cambiare questa classe.
    /// </summary>
    public static class PlayerLocator
    {
        /// <summary>Testa del giocatore (null se non c'è nessun rig).</summary>
        public static Transform Head
        {
            get
            {
                if (TempRig.Instance != null) return TempRig.Instance.Head;
                var cam = Camera.main;
                return cam != null ? cam.transform : null;
            }
        }

        /// <summary>Posizione dei piedi (testa proiettata a terra); zero se non c'è nessun rig.</summary>
        public static Vector3 Feet
        {
            get
            {
                var h = Head;
                return h != null ? new Vector3(h.position.x, 0f, h.position.z) : Vector3.zero;
            }
        }
    }
}
