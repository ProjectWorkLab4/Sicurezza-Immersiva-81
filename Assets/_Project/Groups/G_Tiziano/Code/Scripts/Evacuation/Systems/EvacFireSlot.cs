using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Focolaio placeholder: root con collider trigger e figlio Visual con VFX particellari
    /// (fuoco + fumo con tetto). Nessuna luce realtime, nessuna animazione.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class EvacFireSlot : MonoBehaviour, IResettable
    {
        [Tooltip("Identificatore stabile del marker (FIRESLOT_01..10).")]
        [SerializeField] private string slotId;
        [SerializeField] private GameObject visual;
        [SerializeField] private ParticleSystem fireParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField, Min(0f)] private float fireRate = 26f;
        [SerializeField, Min(0f)] private float smokeRate = 10f;
        [SerializeField, Min(1)] private int smokeCap = 60;

        private Collider trigger;
        private bool active;

        /// <summary>Identificatore stabile dello slot.</summary>
        public string SlotId => slotId;
        /// <summary>True mentre il focolaio è attivo in questa partita.</summary>
        public bool IsActive => active;
        /// <summary>Bounds del collider di contatto (usato dal validatore della corsia).</summary>
        public Bounds ContactBounds => trigger != null ? trigger.bounds : new Bounds(transform.position, Vector3.one * 0.6f);

        private void Awake()
        {
            trigger = GetComponent<Collider>();
            if (trigger != null)
                trigger.isTrigger = true;
            if (fireParticles == null && visual != null)
                fireParticles = visual.GetComponentInChildren<ParticleSystem>(true);
            if (smokeParticles == null && visual != null)
            {
                ParticleSystem[] systems = visual.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] == fireParticles)
                        continue;
                    smokeParticles = systems[i];
                    break;
                }
            }
        }

        /// <summary>Attiva o disattiva il focolaio; intensity e reduced scalano le emissioni.</summary>
        public void SetActive(bool value, float intensity = 1f, bool reduced = false)
        {
            active = value;
            if (trigger != null)
                trigger.enabled = value;
            if (visual != null)
                visual.SetActive(value);

            float scale = value ? Mathf.Clamp01(intensity) * (reduced ? 0.5f : 1f) : 0f;
            SetRate(fireParticles, fireRate * scale);
            SetRate(smokeParticles, smokeRate * scale, reduced ? Mathf.Max(1, smokeCap / 2) : smokeCap);
        }

        /// <summary>IResettable: spegne il focolaio e ripristina i VFX.</summary>
        public void ResetState()
        {
            SetActive(false, 0f, false);
        }

        private static void SetRate(ParticleSystem particles, float rate, int? cap = null)
        {
            if (particles == null)
                return;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var main = particles.main;
            main.maxParticles = cap.HasValue ? cap.Value : Mathf.Max(1, (int)(rate * 2f) + 8);
        }
    }
}
