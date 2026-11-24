using System;
using UnityEngine;
using UnityEngine.AI;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// FR-01: fuoco sulla pozza di benzina. Si spegne con polvere o CO2; l'acqua lo fa divampare (errore critico).
    /// Effetti: particellari segnaposto (da sostituire con i prefab VFX del Common Core).
    /// </summary>
    public class FireSource : MonoBehaviour
    {
        [Tooltip("Particellare delle fiamme.")]
        [SerializeField] private ParticleSystem flames;
        [Tooltip("Particellare del fumo.")]
        [SerializeField] private ParticleSystem smoke;
        [Tooltip("Collider trigger colpito dallo spruzzo dell'estintore.")]
        [SerializeField] private Collider sprayTarget;
        [Tooltip("Secondi di spruzzo corretto per spegnere il fuoco.")]
        [SerializeField] private float secondsToExtinguish = 4f;
        [Tooltip("Luce del fuoco (facoltativa).")]
        [SerializeField] private Light fireLight;
        [Tooltip("Suono del fuoco (loop, 3D).")]
        [SerializeField] private AudioSource fireAudio;
        [Tooltip("Ostacolo NavMesh attivo solo mentre brucia (gli NPC girano attorno al fuoco).")]
        [SerializeField] private NavMeshObstacle navObstacle;

        private float health;

        /// <summary>true mentre il fuoco brucia.</summary>
        public bool IsBurning { get; private set; }
        /// <summary>true se il fuoco è stato spento.</summary>
        public bool WasExtinguished { get; private set; }
        /// <summary>Il fuoco è stato spento (con l'agente indicato).</summary>
        public event Action<ExtinguisherAgent> Extinguished;
        /// <summary>Acqua sul fuoco: la pozza si allarga e divampa.</summary>
        public event Action WaterApplied;

        /// <summary>Spegne gli effetti all'avvio.</summary>
        private void Awake()
        {
            SetEffects(false);
            if (sprayTarget != null) sprayTarget.enabled = false;
        }

        /// <summary>Accende il fuoco.</summary>
        public void Ignite()
        {
            if (IsBurning || WasExtinguished) return;
            IsBurning = true;
            health = secondsToExtinguish;
            if (sprayTarget != null) sprayTarget.enabled = true;
            SetEffects(true);
        }

        /// <summary>Applica l'agente estinguente per un intervallo di tempo.</summary>
        /// <param name="agent">Tipo di estintore.</param>
        /// <param name="deltaTime">Secondi di spruzzo.</param>
        public void ApplyAgent(ExtinguisherAgent agent, float deltaTime)
        {
            if (!IsBurning) return;
            if (agent == ExtinguisherAgent.Water)
            {
                transform.localScale = Vector3.one * 2.2f;
                WaterApplied?.Invoke();
                return;
            }
            health -= deltaTime;
            float k = Mathf.Clamp01(health / secondsToExtinguish);
            transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, k);
            if (health <= 0f)
            {
                IsBurning = false;
                WasExtinguished = true;
                if (sprayTarget != null) sprayTarget.enabled = false;
                SetEffects(false);
                Extinguished?.Invoke(agent);
            }
        }

        /// <summary>Accende o spegne fiamme, fumo e luce.</summary>
        private void SetEffects(bool on)
        {
            if (flames != null) { if (on) flames.Play(); else flames.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
            if (smoke != null) { if (on) smoke.Play(); else smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
            if (fireLight != null) fireLight.enabled = on;
            if (navObstacle != null) navObstacle.enabled = on;
            PlaceholderAudio.EnsureClip(fireAudio, PlaceholderAudio.Kind.FireRoar);
            if (fireAudio != null) { if (on) fireAudio.Play(); else fireAudio.Stop(); }
        }

        /// <summary>Sfarfallio della luce del fuoco.</summary>
        private void Update()
        {
            if (IsBurning && fireLight != null) fireLight.intensity = 2.5f + Mathf.PerlinNoise(Time.time * 8f, 0f) * 1.5f;
        }
    }
}
