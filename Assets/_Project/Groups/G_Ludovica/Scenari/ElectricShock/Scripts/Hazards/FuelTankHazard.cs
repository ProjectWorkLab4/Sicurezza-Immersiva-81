using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>FC-01 + serbatoio: rischio nascosto. Si scalda con il fuoco acceso, avvisa 12 s prima e poi esplode (vampata di 7 m).</summary>
    public class FuelTankHazard : MonoBehaviour
    {
        [Tooltip("Parte che si gonfia durante l'avviso.")]
        [SerializeField] private Transform swellingPart;
        [Tooltip("Particellare della vampata.")]
        [SerializeField] private ParticleSystem blast;
        [Tooltip("Suono del sibilo (facoltativo, segnaposto).")]
        [SerializeField] private AudioSource hissAudio;
        [Tooltip("Anello del raggio della vampata (Guided).")]
        [SerializeField] private LineRenderer blastRing;
        [Tooltip("Suono dell'esplosione (una volta, 3D).")]
        [SerializeField] private AudioSource blastAudio;

        private bool warning;
        private bool swelling;
        private float warningStart;
        private float warningLength;
        private Vector3 restScale;

        /// <summary>true dopo l'esplosione.</summary>
        public bool HasExploded { get; private set; }

        /// <summary>Memorizza la scala di riposo e nasconde l'anello.</summary>
        private void Awake()
        {
            if (swellingPart != null) restScale = swellingPart.localScale;
            if (blastRing != null) blastRing.enabled = false;
        }

        /// <summary>Avvio dell'avviso di esplosione.</summary>
        /// <param name="seconds">Durata dell'avviso.</param>
        /// <param name="showSwelling">Gonfiore visibile.</param>
        /// <param name="showRing">Anello del raggio visibile.</param>
        public void StartWarning(float seconds, bool showSwelling, bool showRing)
        {
            if (warning || HasExploded) return;
            warning = true;
            swelling = showSwelling;
            warningStart = Time.time;
            warningLength = Mathf.Max(0.1f, seconds);
            PlaceholderAudio.EnsureClip(hissAudio, PlaceholderAudio.Kind.TankHiss);
            if (hissAudio != null && hissAudio.clip != null) hissAudio.Play();
            if (blastRing != null) blastRing.enabled = showRing;
        }

        /// <summary>Annulla l'avviso (fuoco spento).</summary>
        public void Calm()
        {
            warning = false;
            if (swellingPart != null) swellingPart.localScale = restScale;
            if (hissAudio != null) hissAudio.Stop();
            if (blastRing != null) blastRing.enabled = false;
        }

        /// <summary>Esplosione: vampata e serbatoio distrutto.</summary>
        public void Explode()
        {
            if (HasExploded) return;
            HasExploded = true;
            warning = false;
            if (hissAudio != null) hissAudio.Stop();
            if (blastRing != null) blastRing.enabled = false;
            if (blast != null) blast.Play();
            PlaceholderAudio.EnsureClip(blastAudio, PlaceholderAudio.Kind.Explosion);
            if (blastAudio != null) blastAudio.Play();
            if (swellingPart != null) swellingPart.gameObject.SetActive(false);
        }

        /// <summary>Gonfiore progressivo durante l'avviso.</summary>
        private void Update()
        {
            if (!warning || !swelling || swellingPart == null) return;
            float k = Mathf.Clamp01((Time.time - warningStart) / warningLength);
            float pulse = 1f + 0.03f * Mathf.Sin(Time.time * 20f) * k;
            swellingPart.localScale = restScale * (1f + 0.25f * k) * pulse;
        }
    }
}
