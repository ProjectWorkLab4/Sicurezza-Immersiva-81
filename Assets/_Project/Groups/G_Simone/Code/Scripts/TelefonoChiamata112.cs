using SI81.Input;
using SI81.Interaction;
using UnityEngine;

/// <summary>
/// Flusso telefono semplificato del Blackout: la prima selezione invita a selezionare di nuovo
/// il telefono, la seconda mostra la risposta del 112. Dopo la risposta non risponde più.
/// </summary>
[DisallowMultipleComponent]
public sealed class TelefonoChiamata112 : MonoBehaviour
{
    const string TITOLO = "TELEFONO";
    const string INVITO = "Seleziona di nuovo il telefono per chiamare il 112";
    const string RISPOSTA = "Il 112 risponde: dice di evacuare l'edificio e che i soccorsi stanno arrivando";
    const float DURATA_INVITO = 4f;
    const float DURATA_RISPOSTA = 8f;

    [Tooltip("Display dei messaggi. Se vuoto viene usato/creato VRMessageDisplay.")]
    [SerializeField] private VRMessageDisplay display;

    [Tooltip("Interactable del telefono. Se vuoto viene preso da questo GameObject.")]
    [SerializeField] private Interactable interactable;

    private System.Action<Hand> handler;
    private bool invitoMostrato;
    private bool completato;

    private void Awake()
    {
        if (display == null)
        {
            display = VRMessageDisplay.GetOrCreate();
        }

        if (interactable == null)
        {
            interactable = GetComponent<Interactable>();
        }
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            return;
        }

        handler = _ => GestisciSelezione();
        interactable.SelectEntered += handler;
    }

    private void OnDisable()
    {
        if (interactable != null && handler != null)
        {
            interactable.SelectEntered -= handler;
        }

        handler = null;
    }

    private void GestisciSelezione()
    {
        if (completato || display == null)
        {
            return;
        }

        if (!invitoMostrato)
        {
            invitoMostrato = true;
            display.Show(TITOLO, INVITO, DURATA_INVITO);
            return;
        }

        completato = true;
        display.Show(TITOLO, RISPOSTA, DURATA_RISPOSTA);

        if (interactable != null && handler != null)
        {
            interactable.SelectEntered -= handler;
            handler = null;
        }
    }
}
