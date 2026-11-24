using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>CB-01: cavo sotto tensione dal generatore alla vittima. Toccarlo con la corrente presente folgora il giocatore.</summary>
    public class LiveCable : TempInteractable
    {
        [Tooltip("Scintille sul cavo (si spengono senza corrente).")]
        [SerializeField] private ParticleSystem sparks;
        [Tooltip("Crepitio elettrico (loop, 3D).")]
        [SerializeField] private AudioSource crackleAudio;

        /// <summary>true se il cavo è sotto tensione.</summary>
        public bool IsPowered { get; private set; } = true;
        /// <summary>Il giocatore ha toccato il cavo sotto tensione.</summary>
        public event Action TouchedWhilePowered;

        /// <summary>Imposta la tensione del cavo.</summary>
        /// <param name="powered">true = sotto tensione.</param>
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
            PlaceholderAudio.EnsureClip(crackleAudio, PlaceholderAudio.Kind.ElectricCrackle);
            if (crackleAudio != null) { if (powered) crackleAudio.Play(); else crackleAudio.Stop(); }
            if (sparks == null) return;
            if (powered) sparks.Play();
            else sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Contatto della mano: folgorazione se sotto tensione.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnContact(TempHand hand)
        {
            if (IsPowered) TouchedWhilePowered?.Invoke();
        }
    }
}
