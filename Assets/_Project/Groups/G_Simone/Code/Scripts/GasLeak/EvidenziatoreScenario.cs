using UnityEngine;

/// <summary>
/// Highlights a prop for the guided difficulty (GGD: outline in #E69F00).
///
/// Instead of an extra outline pass, the renderers are tinted with a MaterialPropertyBlock
/// (cheap on Quest and it does not modify the shared materials).
/// </summary>
[DisallowMultipleComponent]
public class EvidenziatoreScenario : MonoBehaviour
{
    [Header("Renderer da evidenziare")]
    [SerializeField] Renderer[] rendererDaEvidenziare = new Renderer[0];

    [Header("Aspetto")]
    [SerializeField] Color coloreEvidenza = new Color(0.902f, 0.624f, 0f, 1f);
    [Range(0f, 1f)]
    [SerializeField] float intensita = 0.65f;
    [SerializeField] bool attivo;

    MaterialPropertyBlock _blocco;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public bool Attivo => attivo;

    void Awake()
    {
        if (rendererDaEvidenziare == null || rendererDaEvidenziare.Length == 0)
        {
            rendererDaEvidenziare = GetComponentsInChildren<Renderer>(true);
        }
    }

    void Start()
    {
        Applica(attivo);
    }

    /// <summary>Turns the highlight on or off.</summary>
    public void ImpostaAttivo(bool valore)
    {
        attivo = valore;
        Applica(attivo);
    }

    /// <summary>Changes the highlight colour.</summary>
    public void ImpostaColore(Color colore)
    {
        coloreEvidenza = colore;

        if (attivo)
        {
            Applica(true);
        }
    }

    void Applica(bool valore)
    {
        if (rendererDaEvidenziare == null)
        {
            return;
        }

        if (_blocco == null)
        {
            _blocco = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in rendererDaEvidenziare)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!valore)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            Color colore = Color.Lerp(OttieniColoreBase(renderer), coloreEvidenza, intensita);

            renderer.GetPropertyBlock(_blocco);
            _blocco.SetColor(BaseColorId, colore);
            _blocco.SetColor(ColorId, colore);
            _blocco.SetColor(EmissionColorId, coloreEvidenza * 0.35f);
            renderer.SetPropertyBlock(_blocco);
        }
    }

    static Color OttieniColoreBase(Renderer renderer)
    {
        Material materiale = renderer.sharedMaterial;

        if (materiale == null)
        {
            return Color.white;
        }

        if (materiale.HasProperty(BaseColorId))
        {
            return materiale.GetColor(BaseColorId);
        }

        if (materiale.HasProperty(ColorId))
        {
            return materiale.GetColor(ColorId);
        }

        return Color.white;
    }
}
