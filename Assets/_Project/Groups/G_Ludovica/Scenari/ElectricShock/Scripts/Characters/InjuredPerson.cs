using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] VT-01, il collega folgorato (sostituto del sistema "ferito" dello scenario 07, con respiro sempre regolare:
    /// niente RCP né DAE nello scenario 10). Cosciente ma stordito.
    /// A/X = controlla coscienza e respiro; grilletto breve = posizione (supino, gambe sollevate);
    /// grilletto tenuto = trascinarlo (solo con pericolo imminente). Toccarlo con la corrente presente folgora il giocatore.
    /// </summary>
    public class InjuredPerson : TempInteractable
    {
        [Tooltip("Cavo che lo tiene sotto tensione.")]
        [SerializeField] private LiveCable cable;
        [Tooltip("Gambe (si sollevano con la posizione).")]
        [SerializeField] private Transform legs;
        [Tooltip("Velocità massima di trascinamento (m/s).")]
        [SerializeField] private float dragSpeed = 0.8f;
        [Tooltip("Secondi di grilletto oltre i quali si trascina invece di posizionare.")]
        [SerializeField] private float dragHoldThreshold = 0.5f;

        private TempHand dragHand;
        private float useStart;
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

        /// <summary>Memorizza la posizione iniziale.</summary>
        private void Awake() => startPosition = transform.position;

        /// <summary>Contatto: folgorazione se la corrente è presente.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnContact(TempHand hand)
        {
            if (IsLive) TouchedWhileLive?.Invoke();
        }

        /// <summary>A/X: controlla coscienza e respiro.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnInspect(TempHand hand) => CheckConsciousness();

        /// <summary>Grilletto premuto: inizio di posizione o trascinamento.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand)
        {
            if (IsLive) return;
            dragHand = hand;
            useStart = Time.time;
        }

        /// <summary>Grilletto rilasciato: se breve, mette in posizione.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseEnd(TempHand hand)
        {
            if (dragHand == null) return;
            bool shortPress = Time.time - useStart < dragHoldThreshold;
            dragHand = null;
            if (shortPress) PlaceInPosition();
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

        /// <summary>Trascinamento mentre il grilletto è tenuto.</summary>
        private void Update()
        {
            if (dragHand == null || IsLive || Time.time - useStart < dragHoldThreshold) return;
            Vector3 target = Flat(dragHand.transform.position);
            Vector3 pos = transform.position;
            Vector3 to = target - Flat(pos);
            if (to.magnitude < 0.6f) return;
            Vector3 step = Vector3.ClampMagnitude(to, dragSpeed * Time.deltaTime);
            transform.position = pos + step;
        }

        /// <summary>Proietta un punto sul piano orizzontale.</summary>
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
