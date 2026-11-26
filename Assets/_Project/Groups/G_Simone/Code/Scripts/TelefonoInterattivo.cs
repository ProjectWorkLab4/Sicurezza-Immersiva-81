using System;
using System.Collections;
using System.Text;
using SI81.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Serializable UnityEvent carrying a string.</summary>
[Serializable]
public class StringEvent : UnityEvent<string>
{
}

/// <summary>
/// Interactive phone placed on the wall / desk.
///
/// The player lifts the handset (grab or poke the "cornetta") and then pokes
/// one of the option buttons; the phone shows the call on its screen and on the
/// VR screen, waits and then ends the call.
///
/// Wiring: assign one <see cref="VRInteractableButton"/> per contact of
/// <see cref="rubrica"/>, in the same order, to <see cref="pulsantiOpzione"/>.
/// Every contact can also be called from code with <see cref="Seleziona(int)"/>.
/// </summary>
[DisallowMultipleComponent]
public class TelefonoInterattivo : MonoBehaviour
{
    [Serializable]
    public class Contatto
    {
        public string nome = "Emergenze";
        public string numero = "112";
        [TextArea(1, 3)]
        public string descrizione = "Numero unico di emergenza europeo";
        [Tooltip("Testo della risposta registrata mostrato durante la chiamata.")]
        [TextArea(2, 5)]
        public string risposta = string.Empty;
        [Tooltip("Durata della conversazione in secondi.")]
        [Min(1f)]
        public float durataRisposta = 8f;
    }

    public enum Stato
    {
        Inattivo,
        Selezione,
        Composizione,
        Conversazione,
        Fine
    }

    [Header("Rubrica")]
    [SerializeField] Contatto[] rubrica = new Contatto[]
    {
        new Contatto { nome = "Emergenze", numero = "112", descrizione = "Numero unico di emergenza" },
        new Contatto { nome = "Vigili del fuoco", numero = "115", descrizione = "Soccorso tecnico urgente" },
        new Contatto { nome = "Manutenzione", numero = "800 123 456", descrizione = "Referente impianti interni" }
    };

    [Header("Elenco chiamanti")]
    [Tooltip("Mostra sul display l’elenco dei numeri disponibili.")]
    [SerializeField] bool mostraElencoChiamanti = true;
    [SerializeField] string intestazioneElenco = "CHIAMANTI DISPONIBILI";
    [Tooltip("Mostra la sequenza di cifre composta con il tastierino.")]
    [SerializeField] bool mostraNumeroComposto = true;

    [Header("Interazione")]
    [Tooltip("Un pulsante per ogni voce della rubrica, nello stesso ordine.")]
    [SerializeField] VRInteractableButton[] pulsantiOpzione = new VRInteractableButton[0];
    [Tooltip("Cornetta: la prima pressione la solleva, la seconda la riappoggia.")]
    [SerializeField] VRInteractableButton cornetta;
    [Tooltip("Vecchia opzione: con la rubrica nella UI comune la cornetta resta sollevata dopo il rilascio.")]
    [SerializeField] bool riponiAlRilascioCornetta = false;
    [Tooltip("Pulsante che chiude la chiamata in corso (opzionale).")]
    [SerializeField] VRInteractableButton pulsanteChiudi;

    [Header("UI comune")]
    [Tooltip("ScenarioController su MNG_Scenario. Se vuoto viene cercato in scena.")]
    [SerializeField] ScenarioController scenario;

    [Header("Visualizzazione")]
    [Tooltip("Testo TMP sullo schermo del telefono. Opzionale.")]
    [SerializeField] TMP_Text schermoTelefono;
    [SerializeField] VRMessageDisplay display;
    [SerializeField] bool mostraSuSchermoVR = true;
    [SerializeField] string titoloMessaggi = "TELEFONO";

    [Header("Tempi")]
    [SerializeField] float attesaComposizione = 1.5f;
    [SerializeField] float durataConversazione = 8f;
    [SerializeField] float durataFine = 3f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoSelezione;
    [SerializeField] AudioClip suonoChiamata;
    [SerializeField] AudioClip suonoFineChiamata;

    [Header("Eventi")]
    [SerializeField] StringEvent onChiamataAvviata = new StringEvent();
    [SerializeField] StringEvent onChiamataTerminata = new StringEvent();

    /// <summary>Fired with the index of the called contact.</summary>
    public event Action<int> ChiamataAvviata;

    /// <summary>Fired with the index of the contact that has been hung up.</summary>
    public event Action<int> ChiamataTerminata;

    public Stato StatoCorrente { get; private set; } = Stato.Inattivo;
    public bool CornettaSollevata { get; private set; }
    public Contatto ContattoCorrente { get; private set; }
    public int IndiceCorrente { get; private set; } = -1;

    /// <summary>Full contact list, in the same order as <see cref="pulsantiOpzione"/>.</summary>
    public Contatto[] Rubrica => rubrica;

    /// <summary>Digits currently typed on the keypad.</summary>
    public string NumeroComposto { get; private set; } = string.Empty;

    /// <summary>Fired every time the typed number changes.</summary>
    public event Action<string> ComposizioneCambiata;

    /// <summary>Controller della UI comune, cercato in scena se non assegnato.</summary>
    ScenarioController Controller
    {
        get
        {
            if (scenario == null)
            {
                scenario = FindFirstObjectByType<ScenarioController>(FindObjectsInactive.Include);
            }

            return scenario;
        }
    }

    /// <summary>Returns the contact at <paramref name="indice"/>, or null.</summary>
    public Contatto PerIndice(int indice)
    {
        if (rubrica == null || indice < 0 || indice >= rubrica.Length)
        {
            return null;
        }

        return rubrica[indice];
    }

    /// <summary>Adds a digit to the number being typed.</summary>
    public void AggiungiCifra(char cifra)
    {
        if (!char.IsDigit(cifra))
        {
            return;
        }

        if (NumeroComposto.Length >= 16)
        {
            return;
        }

        NumeroComposto += cifra;
        ComposizioneCambiata?.Invoke(NumeroComposto);
        AggiornaInterfaccia();
    }

    /// <summary>Removes the last typed digit.</summary>
    public void CancellaCifra()
    {
        if (NumeroComposto.Length == 0)
        {
            return;
        }

        NumeroComposto = NumeroComposto.Substring(0, NumeroComposto.Length - 1);
        ComposizioneCambiata?.Invoke(NumeroComposto);
        AggiornaInterfaccia();
    }

    /// <summary>
    /// Dials <paramref name="numero"/>: if it matches a contact of the address book the
    /// call starts, otherwise the phone reports that the number is not in the list.
    /// </summary>
    public bool ComponiNumero(string numero)
    {
        if (!CornettaSollevata || StatoCorrente != Stato.Selezione)
        {
            return false;
        }

        string pulito = SoloCifre(numero);

        if (string.IsNullOrEmpty(pulito))
        {
            return false;
        }

        for (int i = 0; i < rubrica.Length; i++)
        {
            if (SoloCifre(rubrica[i].numero) == pulito)
            {
                NumeroComposto = pulito;
                Seleziona(i);
                return true;
            }
        }

        NumeroComposto = pulito;
        AggiornaInterfaccia();
        ShowOnVRScreen($"Numero {pulito} non presente nella rubrica.", durataFine);
        return false;
    }

    /// <summary>Clears the typed number.</summary>
    public void PulisciNumero()
    {
        NumeroComposto = string.Empty;
        ComposizioneCambiata?.Invoke(NumeroComposto);
        AggiornaInterfaccia();
    }

    Coroutine _chiamata;
    int _version;
    bool _sceltaTramiteUI;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        Subscribe(cornetta, HandleCornetta);
        Subscribe(pulsanteChiudi, HandleChiudi);
        SubscribeButtons();

        if (riponiAlRilascioCornetta && cornetta != null)
        {
            cornetta.Deselected += HandleCornettaRilasciata;
        }
    }

    void OnDisable()
    {
        Unsubscribe(cornetta, HandleCornetta);
        Unsubscribe(pulsanteChiudi, HandleChiudi);
        UnsubscribeButtons();

        if (riponiAlRilascioCornetta && cornetta != null)
        {
            cornetta.Deselected -= HandleCornettaRilasciata;
        }
    }

    void Start()
    {
        AggiornaInterfaccia();
    }

    /// <summary>Lifts the handset and shows the contact list.</summary>
    public void SollevaCornetta()
    {
        if (CornettaSollevata)
        {
            return;
        }

        _version++;
        CornettaSollevata = true;
        StatoCorrente = Stato.Selezione;
        ContattoCorrente = null;
        IndiceCorrente = -1;
        NumeroComposto = string.Empty;
        _sceltaTramiteUI = false;

        PlayClip(suonoSelezione);
        AggiornaInterfaccia();

        MostraRubrica();
    }

    /// <summary>Mostra la rubrica nel pannello decisione della UI comune.</summary>
    void MostraRubrica()
    {
        if (rubrica == null || rubrica.Length == 0)
        {
            return;
        }

        int count = Mathf.Min(rubrica.Length, 4);
        string[] opzioni = new string[count];
        for (int i = 0; i < count; i++)
        {
            opzioni[i] = $"{i + 1}. {rubrica[i].nome} ({rubrica[i].numero})";
        }

        ScenarioController controller = Controller;
        bool aperto = controller != null &&
                      controller.AskDecision("Quale numero chiami?", opzioni, OnNumeroScelto);

        if (!aperto)
        {
            ShowOnVRScreen($"Seleziona un numero tra i {rubrica.Length} disponibili.", 4f);
            return;
        }

        // La scelta avviene nel pannello comune: i pulsanti fisici non devono lasciarlo aperto.
        _sceltaTramiteUI = true;
        if (pulsantiOpzione != null)
        {
            foreach (VRInteractableButton pulsante in pulsantiOpzione)
            {
                if (pulsante != null)
                {
                    pulsante.SetAvailable(false);
                }
            }
        }
    }

    /// <summary>Chiama il numero scelto nel pannello della UI comune.</summary>
    void OnNumeroScelto(int indice)
    {
        if (indice < 0)
        {
            return;
        }

        Seleziona(indice);
    }

    /// <summary>Hangs up any call and puts the handset back.</summary>
    public void RiponiCornetta()
    {
        if (!CornettaSollevata)
        {
            return;
        }

        bool chiamataInCorso = StatoCorrente == Stato.Composizione || StatoCorrente == Stato.Conversazione;
        int indiceChiamata = IndiceCorrente;

        StopChiamata();
        _version++;
        CornettaSollevata = false;
        StatoCorrente = Stato.Inattivo;
        ContattoCorrente = null;
        IndiceCorrente = -1;
        NumeroComposto = string.Empty;
        _sceltaTramiteUI = false;

        AggiornaInterfaccia();

        if (chiamataInCorso)
        {
            PlayClip(suonoFineChiamata);
            ShowOnVRScreen("Cornetta riappoggiata: chiamata terminata.", durataFine);
            ChiamataTerminata?.Invoke(indiceChiamata);
        }
    }

    /// <summary>Calls the contact at <paramref name="indice"/> (0-based).</summary>
    public void Seleziona(int indice)
    {
        if (!CornettaSollevata || StatoCorrente != Stato.Selezione)
        {
            return;
        }

        if (rubrica == null || indice < 0 || indice >= rubrica.Length)
        {
            return;
        }

        StopChiamata();

        int version = ++_version;
        IndiceCorrente = indice;
        ContattoCorrente = rubrica[indice];
        StatoCorrente = Stato.Composizione;

        PlayClip(suonoSelezione);
        AggiornaInterfaccia();
        ShowOnVRScreen($"Chiamata a {ContattoCorrente.nome} ({ContattoCorrente.numero})...", 3f);

        onChiamataAvviata.Invoke(ContattoCorrente.nome);
        ChiamataAvviata?.Invoke(indice);

        _chiamata = StartCoroutine(CicloChiamata(version));
    }

    /// <summary>Ends the current call, keeping the handset lifted.</summary>
    public void ChiudiChiamata()
    {
        if (StatoCorrente != Stato.Composizione && StatoCorrente != Stato.Conversazione)
        {
            return;
        }

        bool annullata = StatoCorrente == Stato.Composizione;
        TerminaChiamata(annullata);
    }

    IEnumerator CicloChiamata(int version)
    {
        yield return new WaitForSeconds(attesaComposizione);

        if (version != _version)
        {
            yield break;
        }

        StatoCorrente = Stato.Conversazione;
        PlayClip(suonoChiamata);
        AggiornaInterfaccia();

        string risposta = ContattoCorrente != null && !string.IsNullOrEmpty(ContattoCorrente.risposta)
            ? ContattoCorrente.risposta
            : $"Conversazione con {ContattoCorrente?.nome} ({ContattoCorrente?.numero}).";

        ShowOnVRScreen(risposta, durataConversazione);

        float durata = ContattoCorrente != null ? Mathf.Max(1f, ContattoCorrente.durataRisposta) : durataConversazione;
        yield return new WaitForSeconds(durata);

        if (version != _version)
        {
            yield break;
        }

        _chiamata = null;
        TerminaChiamata(annullata: false);
    }

    void TerminaChiamata(bool annullata)
    {
        Contatto calledContact = ContattoCorrente;
        int calledIndex = IndiceCorrente;

        _version++;
        StopChiamata();

        StatoCorrente = Stato.Fine;
        PlayClip(suonoFineChiamata);
        AggiornaInterfaccia();

        if (annullata)
        {
            ShowOnVRScreen("Chiamata annullata.", durataFine);
        }
        else
        {
            ShowOnVRScreen($"Chiamata con {calledContact?.nome} terminata.", durataFine);
        }

        onChiamataTerminata.Invoke(calledContact != null ? calledContact.nome : string.Empty);
        ChiamataTerminata?.Invoke(calledIndex);

        int version = _version;
        StartCoroutine(TornaAllaSelezione(version));
    }

    IEnumerator TornaAllaSelezione(int version)
    {
        yield return new WaitForSeconds(durataFine);

        if (version != _version)
        {
            yield break;
        }

        ContattoCorrente = null;
        IndiceCorrente = -1;
        CornettaSollevata = false;
        StatoCorrente = Stato.Inattivo;
        _sceltaTramiteUI = false;
        AggiornaInterfaccia();
        // Dopo la chiamata la rubrica non si riapre: la apre solo una nuova pressione della cornetta.
    }

    void StopChiamata()
    {
        if (_chiamata != null)
        {
            StopCoroutine(_chiamata);
            _chiamata = null;
        }
    }

    void AggiornaInterfaccia()
    {
        bool opzioniAttive = StatoCorrente == Stato.Selezione && !_sceltaTramiteUI;

        if (pulsantiOpzione != null)
        {
            foreach (VRInteractableButton pulsante in pulsantiOpzione)
            {
                if (pulsante != null)
                {
                    pulsante.SetAvailable(opzioniAttive);
                }
            }
        }

        if (pulsanteChiudi != null)
        {
            pulsanteChiudi.SetAvailable(StatoCorrente == Stato.Composizione || StatoCorrente == Stato.Conversazione);
        }

        if (schermoTelefono != null)
        {
            schermoTelefono.text = CostruisciTestoSchermo();
        }
    }

    string CostruisciTestoSchermo()
    {
        switch (StatoCorrente)
        {
            case Stato.Inattivo:
                return "TELEFONO\n\nSolleva la cornetta\nper effettuare una chiamata";

            case Stato.Selezione:
                StringBuilder builder = new StringBuilder();

                if (mostraElencoChiamanti)
                {
                    builder.Append(intestazioneElenco).AppendLine("\n");

                    for (int i = 0; i < rubrica.Length; i++)
                    {
                        builder.Append(i + 1).Append(". ")
                            .Append(rubrica[i].nome)
                            .Append("  ")
                            .Append(rubrica[i].numero)
                            .AppendLine();
                    }
                }
                else
                {
                    builder.AppendLine("SELEZIONA UN NUMERO");
                }

                if (mostraNumeroComposto)
                {
                    builder.AppendLine();
                    builder.Append("DIGITA: ")
                        .Append(string.IsNullOrEmpty(NumeroComposto) ? "____" : NumeroComposto)
                        .Append('_');
                }

                return builder.ToString();

            case Stato.Composizione:
                return $"CHIAMATA IN CORSO...\n\n{ContattoCorrente?.nome}\n{ContattoCorrente?.numero}";

            case Stato.Conversazione:
                return $"IN CONVERSAZIONE\n\n{ContattoCorrente?.nome}\n{ContattoCorrente?.numero}";

            case Stato.Fine:
                return "CHIAMATA TERMINATA";

            default:
                return string.Empty;
        }
    }

    void SubscribeButtons()
    {
        if (pulsantiOpzione == null)
        {
            return;
        }

        for (int i = 0; i < pulsantiOpzione.Length; i++)
        {
            if (pulsantiOpzione[i] != null)
            {
                pulsantiOpzione[i].Selected += HandleOpzione;
            }
        }
    }

    void UnsubscribeButtons()
    {
        if (pulsantiOpzione == null)
        {
            return;
        }

        foreach (VRInteractableButton pulsante in pulsantiOpzione)
        {
            if (pulsante != null)
            {
                pulsante.Selected -= HandleOpzione;
            }
        }
    }

    void HandleOpzione(VRInteractableButton pulsante)
    {
        int index = Array.IndexOf(pulsantiOpzione, pulsante);

        if (index >= 0)
        {
            Seleziona(index);
        }
    }

    void HandleCornetta(VRInteractableButton button)
    {
        if (CornettaSollevata)
        {
            RiponiCornetta();
        }
        else
        {
            SollevaCornetta();
        }
    }

    void HandleCornettaRilasciata(VRInteractableButton button)
    {
        // La cornetta resta sollevata dopo il rilascio: il numero si sceglie nel pannello
        // della UI comune, che compare dopo aver lasciato il grab. Per riappoggiarla
        // basta premere di nuovo la cornetta.
    }

    void HandleChiudi(VRInteractableButton button)
    {
        ChiudiChiamata();
    }

    void ShowOnVRScreen(string body, float duration)
    {
        if (!mostraSuSchermoVR || string.IsNullOrEmpty(body))
        {
            return;
        }

        VRMessageDisplay target = display != null ? display : VRMessageDisplay.GetOrCreate();
        target.Show(titoloMessaggi, body, duration);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    static string SoloCifre(string valore)
    {
        if (string.IsNullOrEmpty(valore))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(valore.Length);

        foreach (char carattere in valore)
        {
            if (char.IsDigit(carattere))
            {
                builder.Append(carattere);
            }
        }

        return builder.ToString();
    }

    static void Subscribe(VRInteractableButton button, Action<VRInteractableButton> handler)
    {
        if (button != null)
        {
            button.Selected += handler;
        }
    }

    static void Unsubscribe(VRInteractableButton button, Action<VRInteractableButton> handler)
    {
        if (button != null)
        {
            button.Selected -= handler;
        }
    }
}
