using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space HUD of the Gas Leak scenario.
///
/// It builds its own Canvas at runtime (no prefab to wire), follows the head with a
/// soft smoothing and shows the mission timer, the hearts (or the thumbs feedback of
/// the guided level), the current difficulty and the contextual hints.
///
/// At the end of the level it shows the recap panel with the score and the list of
/// correct / wrong actions.
/// </summary>
[DisallowMultipleComponent]
public class HUDScenario : MonoBehaviour
{
    [Header("Ancoraggio")]
    [Tooltip("Testa del giocatore. Se vuoto viene usata Camera.main.")]
    [SerializeField] Transform testa;
    [SerializeField] bool seguiTesta = true;
    [Tooltip("Distanza in metri dal viso.")]
    [SerializeField] float distanza = 0.6f;
    [Tooltip("Scostamento dal centro dello sguardo (x = destra, y = su).")]
    [SerializeField] Vector3 scostamento = new Vector3(-0.2f, -0.13f, 0f);
    [SerializeField] float scorrevolezza = 10f;

    [Header("Pannello")]
    [SerializeField] Vector2 dimensioneMetri = new Vector2(0.32f, 0.17f);
    [SerializeField] int pixelPerMetro = 1200;
    [SerializeField] int sortingOrder = 120;
    [SerializeField] Color coloreSfondo = new Color(0.03f, 0.04f, 0.07f, 0.82f);
    [SerializeField] Color coloreAccento = new Color(1f, 0.62f, 0.12f, 1f);

    [Header("Colori stato")]
    [SerializeField] Color coloreTempoNormale = new Color(0.95f, 0.96f, 0.98f);
    [SerializeField] Color coloreTempoBasso = new Color(1f, 0.32f, 0.26f);
    [SerializeField] Color coloreCuorePieno = new Color(0.93f, 0.20f, 0.28f);
    [SerializeField] Color coloreCuoreVuoto = new Color(0.22f, 0.22f, 0.26f);
    [SerializeField] Color coloreOk = new Color(0.49f, 0.85f, 0.34f);
    [SerializeField] Color coloreKo = new Color(1f, 0.35f, 0.35f);
    [SerializeField] Color coloreSuggerimento = new Color(0.98f, 0.85f, 0.55f);

    [Header("Tempi")]
    [Tooltip("Sotto questi secondi il timer diventa rosso.")]
    [SerializeField] float sogliaTempoBasso = 30f;

    Canvas _canvas;
    CanvasGroup _gruppo;
    RectTransform _pannello;
    TextMeshProUGUI _testoTempo;
    TextMeshProUGUI _testoDifficolta;
    TextMeshProUGUI _testoSuggerimento;
    TextMeshProUGUI _testoFeedback;
    Image _sfondoFeedback;
    RectTransform _rigaCuori;

    readonly List<Image> _cuori = new List<Image>();
    Sprite _spriteCuore;
    Coroutine _routineSuggerimento;
    float _ultimoTempo = -1f;

    Canvas _canvasEsito;
    bool _esitoVisibile;

    void Awake()
    {
        if (testa == null && Camera.main != null)
        {
            testa = Camera.main.transform;
        }

        CostruisciPannello();
    }

    void LateUpdate()
    {
        if (!seguiTesta || testa == null)
        {
            return;
        }

        Vector3 desiderata = testa.position + testa.rotation * (Vector3.forward * distanza + scostamento);
        float blend = 1f - Mathf.Exp(-scorrevolezza * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiderata, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, testa.rotation, blend);
    }

    // ---------------------------------------------------------------- API

    /// <summary>Updates the timer label (in seconds).</summary>
    public void ImpostaTempo(float secondi)
    {
        if (_testoTempo == null)
        {
            return;
        }

        int totali = Mathf.Max(0, Mathf.CeilToInt(secondi));
        int minuti = totali / 60;
        int resto = totali % 60;

        _testoTempo.text = $"{minuti:00}:{resto:00}";
        _testoTempo.color = secondi <= sogliaTempoBasso ? coloreTempoBasso : coloreTempoNormale;

        if (Mathf.Approximately(_ultimoTempo, secondi) && _testoTempo.color == coloreTempoBasso)
        {
            _testoTempo.rectTransform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 3f, 0.08f));
        }
        else
        {
            _testoTempo.rectTransform.localScale = Vector3.one;
        }

        _ultimoTempo = secondi;
    }

    /// <summary>Shows the hearts row. Pass <paramref name="massimo"/> = 0 to hide it.</summary>
    public void ImpostaCuori(int attuali, int massimo)
    {
        if (_rigaCuori == null)
        {
            return;
        }

        AssicuraCuori(massimo);

        for (int i = 0; i < _cuori.Count; i++)
        {
            bool pieno = i < attuali;
            _cuori[i].color = pieno ? coloreCuorePieno : coloreCuoreVuoto;
            _cuori[i].rectTransform.localScale = Vector3.one * (pieno ? 1f : 0.85f);
        }

        _rigaCuori.gameObject.SetActive(massimo > 0);
    }

    /// <summary>Writes the difficulty name in the top-right corner.</summary>
    public void ImpostaDifficolta(ImpostazioniDifficolta impostazioni)
    {
        if (_testoDifficolta == null || impostazioni == null)
        {
            return;
        }

        _testoDifficolta.text = impostazioni.nome.ToUpperInvariant();
        _testoDifficolta.color = impostazioni.evidenziaOggetti ? coloreAccento : coloreTempoNormale;

        if (_rigaCuori != null)
        {
            _rigaCuori.gameObject.SetActive(impostazioni.mostraCuori && impostazioni.cuori > 0);
        }

        if (_sfondoFeedback != null)
        {
            _sfondoFeedback.gameObject.SetActive(impostazioni.UsaFeedbackPollice);
        }
    }

    /// <summary>Shows a contextual hint for a few seconds.</summary>
    public void MostraSuggerimento(string testo, float durata)
    {
        if (_testoSuggerimento == null || string.IsNullOrEmpty(testo))
        {
            return;
        }

        if (_routineSuggerimento != null)
        {
            StopCoroutine(_routineSuggerimento);
        }

        _routineSuggerimento = StartCoroutine(CicloSuggerimento(testo, Mathf.Max(1f, durata)));
    }

    /// <summary>Shows the thumbs feedback of the guided level (-1 down, 0 intermediate, 1 up).</summary>
    public void MostraFeedbackPollice(int livello)
    {
        if (_testoFeedback == null)
        {
            return;
        }

        switch (livello)
        {
            case 1:
                _testoFeedback.text = "POLLICE SU";
                _testoFeedback.color = coloreOk;
                break;
            case 0:
                _testoFeedback.text = "COSI' COSI'";
                _testoFeedback.color = coloreSuggerimento;
                break;
            default:
                _testoFeedback.text = "POLLICE GIU'";
                _testoFeedback.color = coloreKo;
                break;
        }
    }

    /// <summary>Shows the end-of-level recap.</summary>
    public void MostraEsito(EsitoScenario esito, int punteggio, IReadOnlyList<VoceRecap> recap)
    {
        NascondiEsito();

        Transform target = testa != null ? testa : (Camera.main != null ? Camera.main.transform : transform);

        GameObject host = new GameObject("PannelloEsito");
        host.transform.SetPositionAndRotation(
            target.position + target.rotation * new Vector3(0f, 0f, 0.9f),
            target.rotation);

        _canvasEsito = host.AddComponent<Canvas>();
        _canvasEsito.renderMode = RenderMode.WorldSpace;
        _canvasEsito.sortingOrder = sortingOrder + 10;

        RectTransform radice = (RectTransform)host.transform;
        radice.sizeDelta = new Vector2(700f, 560f);
        radice.localScale = Vector3.one * (1f / Mathf.Max(1, pixelPerMetro));

        Image sfondo = CreaImmagine(radice, "Sfondo", new Color(0.02f, 0.03f, 0.05f, 0.94f));
        Allunga(sfondo.rectTransform);

        Image accento = CreaImmagine(radice, "Accento", coloreAccento);
        RectTransform accentoRect = accento.rectTransform;
        accentoRect.anchorMin = new Vector2(0f, 1f);
        accentoRect.anchorMax = new Vector2(1f, 1f);
        accentoRect.pivot = new Vector2(0.5f, 1f);
        accentoRect.sizeDelta = new Vector2(0f, 8f);
        accentoRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titolo = CreaTesto(radice, "Titolo", 44f, Color.white, FontStyles.Bold);
        RectTransform titoloRect = titolo.rectTransform;
        titoloRect.anchorMin = new Vector2(0f, 1f);
        titoloRect.anchorMax = new Vector2(1f, 1f);
        titoloRect.pivot = new Vector2(0.5f, 1f);
        titoloRect.sizeDelta = new Vector2(-64f, 70f);
        titoloRect.anchoredPosition = new Vector2(0f, -26f);
        titolo.text = TitoloEsito(esito);

        TextMeshProUGUI punteggioTesto = CreaTesto(radice, "Punteggio", 30f, coloreAccento, FontStyles.Bold);
        RectTransform punteggioRect = punteggioTesto.rectTransform;
        punteggioRect.anchorMin = new Vector2(0f, 1f);
        punteggioRect.anchorMax = new Vector2(1f, 1f);
        punteggioRect.pivot = new Vector2(0.5f, 1f);
        punteggioRect.sizeDelta = new Vector2(-64f, 44f);
        punteggioRect.anchoredPosition = new Vector2(0f, -104f);
        punteggioTesto.text = $"Punteggio: {punteggio}";

        TextMeshProUGUI recapTesto = CreaTesto(radice, "Recap", 24f, new Color(0.88f, 0.9f, 0.94f), FontStyles.Normal);
        RectTransform recapRect = recapTesto.rectTransform;
        recapRect.anchorMin = new Vector2(0f, 0f);
        recapRect.anchorMax = new Vector2(1f, 1f);
        recapRect.offsetMin = new Vector2(40f, 70f);
        recapRect.offsetMax = new Vector2(-40f, -160f);
        recapTesto.alignment = TextAlignmentOptions.TopLeft;
        recapTesto.text = CostruisciRecap(recap);

        TextMeshProUGUI piede = CreaTesto(radice, "Piede", 20f, new Color(0.7f, 0.72f, 0.78f), FontStyles.Italic);
        RectTransform piedeRect = piede.rectTransform;
        piedeRect.anchorMin = new Vector2(0f, 0f);
        piedeRect.anchorMax = new Vector2(1f, 0f);
        piedeRect.pivot = new Vector2(0.5f, 0f);
        piedeRect.sizeDelta = new Vector2(-64f, 40f);
        piedeRect.anchoredPosition = new Vector2(0f, 20f);
        piede.text = "Riavvia la scena per riprovare.";

        _esitoVisibile = true;
    }

    /// <summary>Hides the recap panel if it is visible.</summary>
    public void NascondiEsito()
    {
        if (_canvasEsito != null)
        {
            Destroy(_canvasEsito.gameObject);
            _canvasEsito = null;
        }

        _esitoVisibile = false;
    }

    public bool EsitoVisibile => _esitoVisibile;

    // ---------------------------------------------------------------- build

    void CostruisciPannello()
    {
        GameObject pannello = new GameObject("PannelloHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        pannello.transform.SetParent(transform, false);

        _canvas = pannello.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = sortingOrder;

        _gruppo = pannello.GetComponent<CanvasGroup>();
        _gruppo.interactable = false;
        _gruppo.blocksRaycasts = false;

        _pannello = (RectTransform)pannello.transform;
        _pannello.sizeDelta = dimensioneMetri * Mathf.Max(1, pixelPerMetro);
        _pannello.localScale = Vector3.one * (1f / Mathf.Max(1, pixelPerMetro));

        Image sfondo = CreaImmagine(_pannello, "Sfondo", coloreSfondo);
        Allunga(sfondo.rectTransform);

        Image accento = CreaImmagine(_pannello, "Accento", coloreAccento);
        RectTransform accentoRect = accento.rectTransform;
        accentoRect.anchorMin = new Vector2(0f, 1f);
        accentoRect.anchorMax = new Vector2(1f, 1f);
        accentoRect.pivot = new Vector2(0.5f, 1f);
        accentoRect.sizeDelta = new Vector2(0f, 5f);
        accentoRect.anchoredPosition = Vector2.zero;

        _testoTempo = CreaTesto(_pannello, "Tempo", 46f, coloreTempoNormale, FontStyles.Bold);
        RectTransform tempoRect = _testoTempo.rectTransform;
        tempoRect.anchorMin = new Vector2(0f, 1f);
        tempoRect.anchorMax = new Vector2(0f, 1f);
        tempoRect.pivot = new Vector2(0f, 1f);
        tempoRect.sizeDelta = new Vector2(220f, 64f);
        tempoRect.anchoredPosition = new Vector2(18f, -16f);
        _testoTempo.alignment = TextAlignmentOptions.Left;

        _testoDifficolta = CreaTesto(_pannello, "Difficolta", 18f, coloreTempoNormale, FontStyles.Bold);
        RectTransform difficoltaRect = _testoDifficolta.rectTransform;
        difficoltaRect.anchorMin = new Vector2(1f, 1f);
        difficoltaRect.anchorMax = new Vector2(1f, 1f);
        difficoltaRect.pivot = new Vector2(1f, 1f);
        difficoltaRect.sizeDelta = new Vector2(160f, 30f);
        difficoltaRect.anchoredPosition = new Vector2(-18f, -20f);
        _testoDifficolta.alignment = TextAlignmentOptions.Right;

        _rigaCuori = CreaRiga(_pannello, "Cuori", new Vector2(18f, -84f), new Vector2(220f, 30f));

        _sfondoFeedback = CreaImmagine(_pannello, "Feedback", new Color(1f, 1f, 1f, 0.08f));
        RectTransform feedbackRect = _sfondoFeedback.rectTransform;
        feedbackRect.anchorMin = new Vector2(0f, 1f);
        feedbackRect.anchorMax = new Vector2(0f, 1f);
        feedbackRect.pivot = new Vector2(0f, 1f);
        feedbackRect.sizeDelta = new Vector2(150f, 28f);
        feedbackRect.anchoredPosition = new Vector2(18f, -84f);

        _testoFeedback = CreaTesto(feedbackRect, "TestoFeedback", 18f, coloreSuggerimento, FontStyles.Bold);
        Allunga(_testoFeedback.rectTransform);
        _testoFeedback.alignment = TextAlignmentOptions.Left;
        _testoFeedback.margin = new Vector4(8f, 0f, 0f, 0f);
        _sfondoFeedback.gameObject.SetActive(false);

        _testoSuggerimento = CreaTesto(_pannello, "Suggerimento", 17f, coloreSuggerimento, FontStyles.Normal);
        RectTransform suggerimentoRect = _testoSuggerimento.rectTransform;
        suggerimentoRect.anchorMin = new Vector2(0f, 0f);
        suggerimentoRect.anchorMax = new Vector2(1f, 0f);
        suggerimentoRect.pivot = new Vector2(0.5f, 0f);
        suggerimentoRect.sizeDelta = new Vector2(-32f, 58f);
        suggerimentoRect.anchoredPosition = new Vector2(0f, 10f);
        _testoSuggerimento.alignment = TextAlignmentOptions.BottomLeft;
        _testoSuggerimento.text = string.Empty;
    }

    RectTransform CreaRiga(Transform padre, string nome, Vector2 posizione, Vector2 dimensione)
    {
        GameObject riga = new GameObject(nome, typeof(RectTransform));
        riga.transform.SetParent(padre, false);

        RectTransform rect = (RectTransform)riga.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = dimensione;
        rect.anchoredPosition = posizione;

        HorizontalLayoutGroup layout = riga.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rect;
    }

    void AssicuraCuori(int massimo)
    {
        if (_spriteCuore == null)
        {
            _spriteCuore = CreaSpriteCuore(64, Color.white);
        }

        while (_cuori.Count < massimo)
        {
            GameObject cuore = new GameObject($"Cuore{_cuori.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cuore.transform.SetParent(_rigaCuori, false);

            Image immagine = cuore.GetComponent<Image>();
            immagine.sprite = _spriteCuore;
            immagine.type = Image.Type.Simple;
            immagine.preserveAspect = true;
            immagine.raycastTarget = false;

            LayoutElement elemento = cuore.AddComponent<LayoutElement>();
            elemento.preferredWidth = 26f;
            elemento.preferredHeight = 26f;

            _cuori.Add(immagine);
        }
    }

    System.Collections.IEnumerator CicloSuggerimento(string testo, float durata)
    {
        _testoSuggerimento.text = testo;
        _testoSuggerimento.color = coloreSuggerimento;

        yield return new WaitForSeconds(durata);

        _testoSuggerimento.text = string.Empty;
        _routineSuggerimento = null;
    }

    static string TitoloEsito(EsitoScenario esito)
    {
        switch (esito)
        {
            case EsitoScenario.SuccessoCompleto:
                return "SUCCESSO COMPLETO";
            case EsitoScenario.SuccessoParziale:
                return "SUCCESSO PARZIALE";
            case EsitoScenario.FallimentoTempo:
                return "TEMPO SCADUTO";
            case EsitoScenario.FallimentoIntossicazione:
                return "INTOSSICAZIONE";
            default:
                return "ESITO";
        }
    }

    string CostruisciRecap(IReadOnlyList<VoceRecap> recap)
    {
        if (recap == null || recap.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        foreach (VoceRecap voce in recap)
        {
            string colore = ColorUtility.ToHtmlStringRGB(voce.corretta ? coloreOk : coloreKo);
            builder.Append("<color=#").Append(colore).Append('>')
                .Append(voce.corretta ? "[OK]" : "[X]")
                .Append("</color>  ")
                .Append(voce.testo)
                .Append('\n');
        }

        return builder.ToString();
    }

    // ---------------------------------------------------------------- ui helpers

    static void Allunga(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Image CreaImmagine(Transform padre, string nome, Color colore)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);

        Image immagine = go.GetComponent<Image>();
        immagine.color = colore;
        immagine.raycastTarget = false;
        return immagine;
    }

    static TextMeshProUGUI CreaTesto(Transform padre, string nome, float dimensione, Color colore, FontStyles stile)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);

        TextMeshProUGUI testo = go.GetComponent<TextMeshProUGUI>();
        testo.fontSize = dimensione;
        testo.color = colore;
        testo.fontStyle = stile;
        testo.text = string.Empty;
        testo.raycastTarget = false;
        testo.textWrappingMode = TextWrappingModes.Normal;
        testo.overflowMode = TextOverflowModes.Overflow;
        return testo;
    }

    static Sprite CreaSpriteCuore(int dimensione, Color colore)
    {
        Texture2D texture = new Texture2D(dimensione, dimensione, TextureFormat.RGBA32, false);
        texture.name = "CuoreHUD";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixel = new Color[dimensione * dimensione];

        for (int y = 0; y < dimensione; y++)
        {
            for (int x = 0; x < dimensione; x++)
            {
                float nx = (x + 0.5f) / dimensione * 2f - 1f;
                float ny = (y + 0.5f) / dimensione * 2f - 1f;

                float a = nx * 1.15f;
                float b = ny * 1.15f + 0.12f;
                float valore = Mathf.Pow(a * a + b * b - 1f, 3f) - a * a * b * b * b;

                float alpha = Mathf.Clamp01(Mathf.InverseLerp(0.06f, -0.06f, valore));
                pixel[y * dimensione + x] = new Color(colore.r, colore.g, colore.b, alpha);
            }
        }

        texture.SetPixels(pixel);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, dimensione, dimensione), new Vector2(0.5f, 0.5f), 100f);
    }
}
