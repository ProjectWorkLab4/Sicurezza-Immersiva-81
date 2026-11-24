using System;
using System.Collections;
using System.Text;
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

    [Header("Interazione")]
    [Tooltip("Un pulsante per ogni voce della rubrica, nello stesso ordine.")]
    [SerializeField] VRInteractableButton[] pulsantiOpzione = new VRInteractableButton[0];
    [Tooltip("Cornetta: la prima pressione la solleva, la seconda la riappoggia.")]
    [SerializeField] VRInteractableButton cornetta;
    [Tooltip("Se attivo, riappoggiando la cornetta (fine grab) la chiamata viene chiusa.")]
    [SerializeField] bool riponiAlRilascioCornetta = false;
    [Tooltip("Pulsante che chiude la chiamata in corso (opzionale).")]
    [SerializeField] VRInteractableButton pulsanteChiudi;

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

    Coroutine _chiamata;
    int _version;

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

        PlayClip(suonoSelezione);
        AggiornaInterfaccia();

        ShowOnVRScreen($"Seleziona un numero tra i {rubrica.Length} disponibili.", 4f);
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

        ShowOnVRScreen(
            $"Conversazione con {ContattoCorrente.nome} ({ContattoCorrente.numero}).",
            durataConversazione);

        yield return new WaitForSeconds(durataConversazione);

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
        StatoCorrente = CornettaSollevata ? Stato.Selezione : Stato.Inattivo;
        AggiornaInterfaccia();
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
        bool opzioniAttive = StatoCorrente == Stato.Selezione;

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
                StringBuilder builder = new StringBuilder("SELEZIONA UN NUMERO\n\n");
                for (int i = 0; i < rubrica.Length; i++)
                {
                    builder.Append(i + 1).Append(". ")
                        .Append(rubrica[i].nome)
                        .Append("  (")
                        .Append(rubrica[i].numero)
                        .AppendLine(")");
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
        if (CornettaSollevata)
        {
            RiponiCornetta();
        }
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
