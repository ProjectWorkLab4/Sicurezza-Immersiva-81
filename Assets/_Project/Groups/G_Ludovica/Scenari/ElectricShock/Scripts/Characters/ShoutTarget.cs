using System;
using SI81.Input;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Persona a cui il giocatore può parlare (colleghi, PM-2): grilletto puntando la persona = "Fermi! Non toccatelo!
    /// Allontanatevi!". I tasti A/X e B/Y non sono disponibili nell'input condiviso (B/Y è l'Indietro della UI).
    /// </summary>
    public class ShoutTarget : ScenarioInteractable
    {
        /// <summary>Il giocatore ha parlato: posizione della testa e direzione dello sguardo.</summary>
        public static event Action<Vector3, Vector3> Shouted;

        /// <summary>Grilletto sulla persona: il giocatore parla.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnSelectStart(Hand hand)
        {
            var head = PlayerLocator.Head;
            if (head == null) return;
            PlayerLocator.Pulse(hand, 0.3f, 0.08f);
            Shouted?.Invoke(head.position, head.forward);
        }

        /// <summary>Fa parlare il giocatore anche senza raggio (test).</summary>
        /// <param name="headPosition">Posizione della testa.</param>
        /// <param name="forward">Direzione dello sguardo.</param>
        public static void RaiseShout(Vector3 headPosition, Vector3 forward) => Shouted?.Invoke(headPosition, forward);
    }
}
