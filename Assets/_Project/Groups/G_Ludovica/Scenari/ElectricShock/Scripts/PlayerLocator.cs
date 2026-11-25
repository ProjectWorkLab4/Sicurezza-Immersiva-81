using SI81.Input;
using SI81.Rig;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Dove si trova il giocatore: testa, mani e corpo del rig condiviso di SCN_Core (RigRegistration).
    /// I test possono sostituirli con trasform propri (<see cref="SetOverride"/>), senza visore né input.
    /// </summary>
    public static class PlayerLocator
    {
        private static Transform overrideHead;
        private static Transform overrideLeft;
        private static Transform overrideRight;

        /// <summary>Sostituisce testa e mani del rig (solo test). Null = torna al rig condiviso.</summary>
        /// <param name="head">Testa.</param>
        /// <param name="left">Mano sinistra.</param>
        /// <param name="right">Mano destra.</param>
        public static void SetOverride(Transform head, Transform left, Transform right)
        {
            overrideHead = head;
            overrideLeft = left;
            overrideRight = right;
        }

        /// <summary>Testa del giocatore (null se non c'è nessun rig).</summary>
        public static Transform Head
        {
            get
            {
                if (overrideHead != null) return overrideHead;
                var rig = RigRegistration.Instance;
                if (rig != null && rig.Head != null) return rig.Head;
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

        /// <summary>Ancora della mano indicata (controller), o null.</summary>
        /// <param name="hand">Mano sinistra o destra.</param>
        public static Transform HandAnchor(Hand hand)
        {
            if (overrideHead != null) return hand == Hand.Left ? overrideLeft : overrideRight;
            var rig = RigRegistration.Instance;
            if (rig == null) return null;
            return hand == Hand.Left ? rig.LeftHand : rig.RightHand;
        }

        /// <summary>CharacterController del rig (per ignorare le collisioni con persone e barriere), o null.</summary>
        public static CharacterController Body
        {
            get
            {
                if (overrideHead != null) return null;
                var rig = RigRegistration.Instance;
                return rig != null ? rig.GetComponent<CharacterController>() : null;
            }
        }

        /// <summary>Vibrazione sul controller indicato tramite il rig condiviso (rispetta l'impostazione aptica della UI).</summary>
        /// <param name="hand">Mano.</param>
        /// <param name="amplitude">Intensità 0-1.</param>
        /// <param name="duration">Durata (s).</param>
        public static void Pulse(Hand hand, float amplitude, float duration)
        {
            var rig = RigRegistration.Instance;
            if (rig != null) rig.Pulse(hand, amplitude, duration);
        }
    }
}
