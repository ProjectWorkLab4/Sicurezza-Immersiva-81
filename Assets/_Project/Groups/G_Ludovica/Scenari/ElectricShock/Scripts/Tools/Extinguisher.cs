using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>Agente estinguente.</summary>
    public enum ExtinguisherAgent
    {
        /// <summary>Acqua: vietata su apparecchiature in tensione e liquidi infiammabili.</summary>
        Water,
        /// <summary>Polvere: corretta.</summary>
        Powder,
        /// <summary>CO2: corretta.</summary>
        CO2
    }

    /// <summary>
    /// EXT-1/2/3: estintore con etichetta. Grip = afferra, A/X = leggi l'etichetta, grilletto = spruzza davanti all'ugello.
    /// </summary>
    public class Extinguisher : TempInteractable
    {
        [Tooltip("Agente estinguente.")]
        [SerializeField] private ExtinguisherAgent agent = ExtinguisherAgent.Powder;
        [Tooltip("Ugello: lo spruzzo parte da qui verso il suo asse Z.")]
        [SerializeField] private Transform nozzle;
        [Tooltip("Particellare dello spruzzo.")]
        [SerializeField] private ParticleSystem spray;
        [Tooltip("Portata dello spruzzo (m).")]
        [SerializeField] private float range = 3.5f;
        [Tooltip("Raggio del cono di spruzzo (m).")]
        [SerializeField] private float sprayRadius = 0.45f;
        [Tooltip("Secondi di carica.")]
        [SerializeField] private float capacitySeconds = 15f;

        private bool spraying;
        private float charge;
        private readonly Collider[] sprayHits = new Collider[16];

        /// <summary>Agente estinguente.</summary>
        public ExtinguisherAgent Agent => agent;
        /// <summary>true se l'agente è adatto (polvere o CO2).</summary>
        public bool IsCorrectType => agent != ExtinguisherAgent.Water;
        /// <summary>true dopo la prima lettura dell'etichetta.</summary>
        public bool LabelRead { get; private set; }
        /// <summary>true dopo il primo spruzzo.</summary>
        public bool HasBeenUsed { get; private set; }
        /// <summary>Etichetta letta (estintore).</summary>
        public event Action<Extinguisher> LabelInspected;
        /// <summary>Estintore afferrato.</summary>
        public event Action<Extinguisher> Grabbed;
        /// <summary>Prima volta che lo spruzzo colpisce il fuoco.</summary>
        public event Action<Extinguisher> FirstUseOnFire;
        /// <summary>true = l'etichetta si legge da sola quando lo si afferra (Guided).</summary>
        public bool AutoReadLabel { get; set; }

        /// <summary>Testo dell'etichetta in italiano.</summary>
        public string LabelText => agent switch
        {
            ExtinguisherAgent.Water => "Estintore ad ACQUA: classe A. NON usare su apparecchiature in tensione né su liquidi infiammabili.",
            ExtinguisherAgent.Powder => "Estintore a POLVERE: classi A-B-C. Idoneo su apparecchiature in tensione fino a 1000 V a 1 m e su liquidi infiammabili.",
            _ => "Estintore a CO2: classe B. Idoneo su apparecchiature in tensione fino a 1000 V a 1 m e su liquidi infiammabili."
        };

        /// <summary>Nome breve dell'agente ("polvere", "CO2", "acqua").</summary>
        public string AgentName => agent switch
        {
            ExtinguisherAgent.Water => "acqua",
            ExtinguisherAgent.Powder => "polvere",
            _ => "CO2"
        };

        /// <summary>true: si può afferrare.</summary>
        public override bool IsGrabbable => true;

        /// <summary>Carica piena all'avvio.</summary>
        private void Awake() => charge = capacitySeconds;

        /// <summary>Afferrato: in Guided legge l'etichetta da solo.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnGrab(TempHand hand)
        {
            base.OnGrab(hand);
            Grabbed?.Invoke(this);
            if (AutoReadLabel) ReadLabel();
        }

        /// <summary>Lasciato: smette di spruzzare e si appoggia a terra.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnRelease(TempHand hand)
        {
            base.OnRelease(hand);
            SetSpraying(false);
            DropUpright();
        }

        /// <summary>A/X: legge l'etichetta.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnInspect(TempHand hand) => ReadLabel();

        /// <summary>Grilletto premuto: spruzza (solo se tenuto in mano).</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand)
        {
            if (HeldBy != null) SetSpraying(true);
        }

        /// <summary>Grilletto rilasciato: smette di spruzzare.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseEnd(TempHand hand) => SetSpraying(false);

        /// <summary>Posa nella mano: ugello in avanti.</summary>
        public override void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = new Vector3(0f, -0.45f, 0f);
            localRotation = Quaternion.identity;
        }

        /// <summary>Legge l'etichetta (anche da script o test).</summary>
        public void ReadLabel()
        {
            ElectricShockHud.Line("Etichetta", LabelText);
            if (LabelRead) return;
            LabelRead = true;
            LabelInspected?.Invoke(this);
        }

        /// <summary>Spruzza per un intervallo di tempo nella direzione dell'ugello (anche da script o test).</summary>
        /// <param name="deltaTime">Secondi.</param>
        public void SprayTick(float deltaTime)
        {
            if (charge <= 0f) { SetSpraying(false); return; }
            charge -= deltaTime;
            Transform from = nozzle != null ? nozzle : transform;
            var fire = FindFireInSpray(from.position, from.forward);
            if (fire == null) return;
            if (!HasBeenUsed)
            {
                HasBeenUsed = true;
                FirstUseOnFire?.Invoke(this);
            }
            fire.ApplyAgent(agent, deltaTime);
        }

        /// <summary>
        /// Cerca un fuoco acceso lungo lo spruzzo controllando sfere ogni 0,4 m (anche se l'ugello è già dentro
        /// la zona del fuoco, caso in cui uno SphereCast non trova nulla).
        /// </summary>
        private FireSource FindFireInSpray(Vector3 origin, Vector3 direction)
        {
            for (float d = 0f; d <= range; d += 0.4f)
            {
                int n = Physics.OverlapSphereNonAlloc(origin + direction * d, sprayRadius, sprayHits, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++)
                {
                    var fire = sprayHits[i].GetComponentInParent<FireSource>();
                    if (fire != null && fire.IsBurning) return fire;
                }
            }
            return null;
        }

        /// <summary>Accende o spegne lo spruzzo.</summary>
        private void SetSpraying(bool on)
        {
            spraying = on && charge > 0f;
            if (spray == null) return;
            if (spraying) spray.Play();
            else spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Spruzzo continuo mentre il grilletto è premuto.</summary>
        private void Update()
        {
            if (spraying) SprayTick(Time.deltaTime);
        }
    }
}
