using SI81.Input;
using SI81.Interaction;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// NPC a terra dello scenario Man Down. Si occupa della sola valutazione della coscienza:
    /// il giocatore avvicina il controller a una spalla e preme GRIP; il componente chiama
    /// <see cref="ManDownScenario.VerifyConsciousness"/> senza muovere l'NPC, senza scuoterlo e senza
    /// introdurre input, raycast, UI o timer propri. L'NPC è sempre incosciente dall'inizio, senza randomizzazione.
    /// Lo stato di verifica resta quello autorevole del controller: il reset di <see cref="ManDownScenario.ResetScenario"/>
    /// ripristina anche la disponibilità della presa su spalla, senza dati locali da sincronizzare.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/NPC")]
    public sealed class ManDownNpc : MonoBehaviour
    {
        /// <summary>Alias del risultato registrato dal controller: l'NPC è incosciente in ogni partita.</summary>
        public const string CONSCIOUSNESS_RESULT = ManDownScenario.CONSCIOUSNESS_RESULT;

        [Header("Riferimenti di scena")]
        [Tooltip("Controller dello scenario su MNG_Scenario.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Zona su una spalla dell'NPC, con Interactable configurato per il GRIP e FollowsHand disattivato.")]
        [SerializeField] private Interactable shoulderGrip;

        /// <summary>True se la valutazione della coscienza è già stata registrata in questa partita.</summary>
        public bool IsConsciousnessAssessed => scenario != null && scenario.IsConsciousnessChecked;

        /// <summary>Risultato della valutazione; è vuoto finché la verifica non è stata effettuata.</summary>
        public string VerifiedResult => IsConsciousnessAssessed ? CONSCIOUSNESS_RESULT : string.Empty;

        /// <summary>Sottoscrive il GRIP sulla spalla una sola volta quando il componente si attiva.</summary>
        private void OnEnable()
        {
            if (shoulderGrip != null) shoulderGrip.GrabEntered += OnShoulderGrip;
        }

        /// <summary>Rimuove la sottoscrizione alla disattivazione, per non lasciare riferimenti alla partita precedente.</summary>
        private void OnDisable()
        {
            if (shoulderGrip != null) shoulderGrip.GrabEntered -= OnShoulderGrip;
        }

        /// <summary>
        /// Valuta la coscienza chiamando il controller. Ignora duplicati, riferimenti mancanti e azioni fuori
        /// dalla missione attiva. ManDownScenario registra l'azione CONSCIOUSNESS_CHECK con timestamp e risultato.
        /// </summary>
        public void AssessConsciousness()
        {
            if (IsConsciousnessAssessed || scenario == null || !scenario.IsRunning) return;

            scenario.VerifyConsciousness();
        }

        /// <summary>Converte il GRIP sulla spalla in una valutazione della coscienza.</summary>
        /// <param name="hand">Mano che ha premuto il GRIP; non è rilevante per la valutazione.</param>
        private void OnShoulderGrip(Hand hand)
        {
            AssessConsciousness();
        }
    }
}
