using System;
using SI81.Input;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] VT-01, il collega folgorato (sostituto del sistema "ferito" dello scenario 07, con respiro sempre regolare:
    /// niente RCP né DAE nello scenario 10). Cosciente ma stordito.
    /// Grilletto puntando il collega: la prima volta controlla coscienza e respiro, la seconda lo mette in posizione
    /// (supino, gambe sollevate). Grip tenuto: lo trascina verso di sé (solo con pericolo imminente).
    /// Toccarlo (grilletto o grip) con la corrente presente folgora il giocatore.
    /// </summary>
    public class InjuredPerson : ScenarioInteractable
    {
        [Tooltip("Cavo che lo tiene sotto tensione.")]
        [SerializeField] private LiveCable cable;
        [Tooltip("Gambe (si sollevano con la posizione).")]
        [SerializeField] private Transform legs;
        [Tooltip("Velocità massima di trascinamento (m/s).")]
        [SerializeField] private float dragSpeed = 0.8f;
        [Tooltip("Distanza a cui il collega trascinato resta dalla mano (m).")]
        [SerializeField] private float dragDistance = 0.6f;

        private Vector3 startPosition;

        /// <summary>Sempre true: il collega è cosciente (stordito).</summary>
        public bool IsConscious => true;
        /// <summary>Sempre true: respira regolarmente.</summary>
        public bool IsBreathingRegularly => true;
        /// <summary>true se il cavo è sotto tensione (la vittima è in contatto con la corrente).</summary>
        public bool IsLive => cable != null && cable.IsPowered;
        /// <summary>true dopo il controllo di coscienza e respiro.</summary>
        public bool ConsciousnessChecked { get; private set; }
        /// <summary>true dopo la posizione (supino, gambe sollevate).</summary>
        public bool Positioned { get; private set; }
        /// <summary>true se è stato spostato di oltre 1 m.</summary>
        public bool WasMoved => (Flat(transform.position) - Flat(startPosition)).magnitude > 1f;
        /// <summary>true se è stato ferito dalla vampata o peggiorato.</summary>
        public bool IsWorsened { get; private set; }

        /// <summary>Toccato con la corrente presente.</summary>
        public event Action TouchedWhileLive;
        /// <summary>Coscienza e respiro controllati.</summary>
        public event Action Checked;
        /// <summary>Messo in posizione.</summary>
        public event Action PositionedChanged;

        /// <summary>Si può afferrare (per trascinarlo).</summary>
        public override bool IsGrabbable => true;
        /// <summary>Afferrato non va in mano: viene trascinato a terra.</summary>
        protected override bool AttachToHand => false;

        /// <summary>Memorizza la posizione iniziale.</summary>
        protected override void Awake()
        {
            base.Awake();
            startPosition = transform.position;
        }

        /// <summary>Contatto: folgorazione se la corrente è presente.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnContact(Hand hand)
        {
            if (!IsLive) return;
            PlayerLocator.Pulse(hand, 1f, 0.5f);
            TouchedWhileLive?.Invoke();
        }

        /// <summary>Grilletto: prima coscienza e respiro, poi posizione.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnSelectStart(Hand hand)
        {
            if (IsLive) return;
            if (!ConsciousnessChecked) CheckConsciousness();
            else PlaceInPosition();
        }

        /// <summary>Controlla coscienza e respiro (anche da script o test).</summary>
        public void CheckConsciousness()
        {
            if (IsLive) return;
            ElectricShockHud.Line("Collega", "Sì... ci sono... mi gira tutto.");
            ElectricShockHud.Line("", "[Respira regolarmente]");
            if (ConsciousnessChecked) return;
            ConsciousnessChecked = true;
            Checked?.Invoke();
        }

        /// <summary>Mette il collega supino con le gambe sollevate (anche da script o test).</summary>
        public void PlaceInPosition()
        {
            if (IsLive || Positioned) return;
            Positioned = true;
            if (legs != null) legs.localRotation = Quaternion.Euler(0f, 0f, -20f);
            ElectricShockHud.Line("", "[Collega supino, gambe sollevate]");
            PositionedChanged?.Invoke();
        }

        /// <summary>Segna il collega come peggiorato (vampata).</summary>
        public void MarkWorsened() => IsWorsened = true;

        /// <summary>Trascinamento mentre il grip è tenuto: il collega segue la mano a terra.</summary>
        protected override void Update()
        {
            base.Update();
            if (!IsHeld || IsLive) return;
            var hand = PlayerLocator.HandAnchor(HeldHand);
            if (hand == null) return;
            Vector3 target = Flat(hand.position);
            Vector3 pos = transform.position;
            Vector3 to = target - Flat(pos);
            if (to.magnitude < dragDistance) return;
            Vector3 step = Vector3.ClampMagnitude(to, dragSpeed * Time.deltaTime);
            transform.position = pos + step;
        }

        /// <summary>Proietta un punto sul piano orizzontale.</summary>
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
