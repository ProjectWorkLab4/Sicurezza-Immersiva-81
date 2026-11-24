using System;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>GEN-01: generatore portatile. Acceso all'inizio; si ferma con l'arresto di emergenza PB-01.</summary>
    public class GeneratorUnit : MonoBehaviour
    {
        [Tooltip("Suono del motore (facoltativo, segnaposto).")]
        [SerializeField] private AudioSource engineAudio;
        [Tooltip("Parte che vibra mentre il motore gira (facoltativa).")]
        [SerializeField] private Transform vibratingPart;

        private Vector3 restPosition;

        /// <summary>true finché il generatore è acceso.</summary>
        public bool IsRunning { get; private set; } = true;
        /// <summary>Chiamato quando il generatore si ferma.</summary>
        public event Action Stopped;

        /// <summary>Memorizza la posizione di riposo della parte che vibra.</summary>
        private void Awake()
        {
            if (vibratingPart != null) restPosition = vibratingPart.localPosition;
        }

        /// <summary>Accende il generatore (inizio scenario).</summary>
        public void StartRunning()
        {
            IsRunning = true;
            PlaceholderAudio.EnsureClip(engineAudio, PlaceholderAudio.Kind.EngineHum);
            if (engineAudio != null && engineAudio.clip != null) engineAudio.Play();
        }

        /// <summary>Ferma il generatore e toglie la corrente al cavo.</summary>
        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            if (engineAudio != null) engineAudio.Stop();
            if (vibratingPart != null) vibratingPart.localPosition = restPosition;
            Stopped?.Invoke();
        }

        /// <summary>Piccola vibrazione visiva del motore acceso.</summary>
        private void Update()
        {
            if (!IsRunning || vibratingPart == null) return;
            vibratingPart.localPosition = restPosition + UnityEngine.Random.insideUnitSphere * 0.004f;
        }
    }
}
