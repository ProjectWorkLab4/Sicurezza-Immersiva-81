using System.Collections;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// VFX del fuoco per Blackout, generati a runtime sui due oggetti di riferimento
/// <c>FireLocation</c> e <c>FireLocation (1)</c>. Nessuna modifica alla scena: si avvia da solo
/// quando una scena contenente quei due oggetti viene caricata in Play Mode.
/// Realistico (Operational): fiamme moderate. Stress (Crisis): fiamme intense e fumo denso che si espande.
/// Guided: nessun VFX. Nessun livello mostra l'effetto di un altro. Il livello viene letto dalla sessione
/// di missione o, se non attiva, dalla selezione della UI (Livello 2 = Realistico) fatta prima del caricamento.
/// </summary>
[DisallowMultipleComponent]
public sealed class BlackoutFireVFX : MonoBehaviour
{
    const string NOME_PRIMO = "FireLocation";
    const string NOME_SECONDO = "FireLocation (1)";
    const string NOME_FIGLIO = "VFX_Fuoco";
    const string NOME_INTENSO = "VFX_Fuoco_Intenso";
    const string NOME_HOST = "MNG_BlackoutFireVFX";
    const float ATTESA_SESSIONE = 15f;

    static bool inAttesa;

    static Texture2D textureMorbida;
    static Material materialFiamma;
    static Material materialFumo;

    Transform[] punti;

    /// <summary>
    /// Registra il callback prima del primo caricamento: AfterSceneLoad vale solo per la prima scena,
    /// mentre sceneLoaded copre anche i caricamenti additivi come SCN_Blackout da SCN_Core.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Inizializza()
    {
        inAttesa = false;
        textureMorbida = null;
        materialFiamma = null;
        materialFumo = null;
        SceneManager.sceneLoaded -= OnScenaCaricata;
        SceneManager.sceneLoaded += OnScenaCaricata;
    }

    /// <summary>Callback per ogni scena caricata, inclusa quella additiva.</summary>
    private static void OnScenaCaricata(Scene scena, LoadSceneMode modalita)
    {
        AllAvvioScena();
    }

    /// <summary>
    /// Crea un host temporaneo solo se trova i punti fuoco e non sono ancora stati generati.
    /// </summary>
    private static void AllAvvioScena()
    {
        if (inAttesa) return;

        List<Transform> trovati = new List<Transform>();
        GameObject[] oggetti = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject oggetto in oggetti)
        {
            if (oggetto.name == NOME_PRIMO || oggetto.name == NOME_SECONDO)
            {
                trovati.Add(oggetto.transform);
            }
        }

        if (trovati.Count == 0) return;

        bool giaPresente = true;
        foreach (Transform punto in trovati)
        {
            if (punto.Find(NOME_FIGLIO) == null && punto.Find(NOME_INTENSO) == null)
            {
                giaPresente = false;
                break;
            }
        }
        if (giaPresente) return;

        inAttesa = true;
        GameObject host = new GameObject(NOME_HOST);
        host.AddComponent<BlackoutFireVFX>().punti = trovati.ToArray();
    }

    /// <summary>
    /// Livello dalla sessione di missione; in mancanza (prova da Editor o avvio senza Home)
    /// si usa la scelta gia' fatta nella UI prima del caricamento della scena.
    /// </summary>
    private IEnumerator Start()
    {
        float scadenza = Time.unscaledTime + ATTESA_SESSIONE;
        while (!MissionSession.IsActive && !UINavigationContext.SelectedLevel.HasValue && Time.unscaledTime < scadenza)
        {
            yield return null;
        }

        DifficultyLevel? livello = MissionSession.IsActive
            ? MissionSession.Level
            : UINavigationContext.SelectedLevel;

        if (livello.HasValue && punti != null)
        {
            bool intenso = livello.Value == DifficultyLevel.Crisis;
            bool moderato = livello.Value == DifficultyLevel.Operational;

            if (intenso || moderato)
            {
                string nomeRadice = intenso ? NOME_INTENSO : NOME_FIGLIO;

                foreach (Transform punto in punti)
                {
                    // In caso di replay con un altro livello, si rimuove la variante vecchia.
                    Transform varianteVecchia = punto.Find(intenso ? NOME_FIGLIO : NOME_INTENSO);
                    if (varianteVecchia != null)
                    {
                        Destroy(varianteVecchia.gameObject);
                    }
                    CreaFuoco(punto, nomeRadice, intenso);
                }

                string fonte = MissionSession.IsActive ? "sessione" : "UI (pre-caricamento)";
                Debug.Log("[Blackout] VFX fuoco " + (intenso ? "intensi (Stress)" : "moderati (Realistico)") +
                          " da " + fonte + " su " + punti.Length + " punti.");
            }
        }

        inAttesa = false;
        Destroy(gameObject);
    }

    /// <summary>Crea i sistemi particellari come figli del punto di riferimento.</summary>
    private static void CreaFuoco(Transform punto, string nomeRadice, bool intenso)
    {
        if (punto == null || punto.Find(nomeRadice) != null) return;

        PreparaRisorse();

        GameObject radice = new GameObject(nomeRadice);
        radice.transform.SetParent(punto, false);
        radice.transform.localPosition = Vector3.zero;
        radice.transform.localRotation = Quaternion.identity;

        // I padri hanno scala 100: si compensa per avere particelle in metri reali.
        Vector3 scalaPadre = punto.lossyScale;
        radice.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, scalaPadre.x),
            1f / Mathf.Max(0.001f, scalaPadre.y),
            1f / Mathf.Max(0.001f, scalaPadre.z));

        CreaFiamme(radice.transform, intenso);
        CreaFumo(radice.transform, intenso);
        CreaLuce(radice.transform, intenso);
    }

    /// <summary>Fiamme: additive, gialle in basso e rosse in alto. Intense nello Stress.</summary>
    private static void CreaFiamme(Transform radice, bool intenso)
    {
        GameObject oggetto = new GameObject("Fiamme");
        oggetto.transform.SetParent(radice, false);
        ParticleSystem sistema = oggetto.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = sistema.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(intenso ? 0.6f : 0.5f, intenso ? 1.3f : 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(intenso ? 0.7f : 0.4f, intenso ? 1.6f : 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(intenso ? 0.3f : 0.15f, intenso ? 0.8f : 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = intenso ? 180 : 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = intenso ? -0.5f : -0.4f;

        ParticleSystem.EmissionModule emissione = sistema.emission;
        emissione.rateOverTime = intenso ? 55f : 20f;
        emissione.rateOverDistance = 0f;

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = intenso ? 20f : 12f;
        forma.radius = intenso ? 0.2f : 0.1f;
        forma.radiusThickness = 0.25f;
        forma.length = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colore = sistema.colorOverLifetime;
        colore.enabled = true;
        Gradient gradiente = new Gradient();
        Color nucleo = intenso ? new Color(1f, 0.98f, 0.78f) : new Color(1f, 0.95f, 0.55f);
        Color medio = intenso ? new Color(1f, 0.65f, 0.12f) : new Color(1f, 0.55f, 0.08f);
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(nucleo, 0f),
                new GradientColorKey(medio, 0.35f),
                new GradientColorKey(new Color(0.95f, 0.18f, 0.03f), 0.7f),
                new GradientColorKey(new Color(0.25f, 0.05f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colore.color = gradiente;

        ParticleSystem.SizeOverLifetimeModule dimensione = sistema.sizeOverLifetime;
        dimensione.enabled = true;
        dimensione.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.55f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule rumore = sistema.noise;
        rumore.enabled = true;
        rumore.strength = intenso ? 0.25f : 0.15f;
        rumore.frequency = 0.5f;
        rumore.scrollSpeed = 0.25f;
        rumore.quality = ParticleSystemNoiseQuality.Low;

        ConfiguraRenderer(oggetto, materialFiamma);
    }

    /// <summary>
    /// Fumo grigio a dissolvenza. Nello Stress e' denso e si espande verso l'esterno
    /// (cono largo, vento laterale e particelle che crescono) invece di salire a colonna.
    /// </summary>
    private static void CreaFumo(Transform radice, bool intenso)
    {
        GameObject oggetto = new GameObject("Fumo");
        oggetto.transform.SetParent(radice, false);
        ParticleSystem sistema = oggetto.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = sistema.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(intenso ? 2.5f : 1.2f, intenso ? 4.5f : 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(intenso ? 0.3f : 0.25f, intenso ? 0.7f : 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(intenso ? 1f : 0.25f, intenso ? 2.2f : 0.55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = intenso ? 140 : 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = intenso ? -0.05f : -0.12f;

        ParticleSystem.EmissionModule emissione = sistema.emission;
        emissione.rateOverTime = intenso ? 26f : 5f;
        emissione.rateOverDistance = 0f;

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = intenso ? 55f : 22f;
        forma.radius = intenso ? 0.5f : 0.15f;
        forma.radiusThickness = intenso ? 0.8f : 0.3f;
        forma.length = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colore = sistema.colorOverLifetime;
        colore.enabled = true;
        Gradient gradiente = new Gradient();
        Color inizioFumo = intenso ? new Color(0.13f, 0.12f, 0.12f) : new Color(0.22f, 0.2f, 0.19f);
        Color fineFumo = intenso ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.16f, 0.15f, 0.15f);
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(inizioFumo, 0f),
                new GradientColorKey(fineFumo, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(intenso ? 0.6f : 0.35f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        colore.color = gradiente;

        ParticleSystem.SizeOverLifetimeModule dimensione = sistema.sizeOverLifetime;
        dimensione.enabled = true;
        dimensione.size = intenso
            ? new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.4f, 1.15f), new Keyframe(1f, 1.8f)))
            : new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.2f)));

        ParticleSystem.NoiseModule rumore = sistema.noise;
        rumore.enabled = true;
        rumore.strength = intenso ? 0.3f : 0.1f;
        rumore.frequency = intenso ? 0.25f : 0.35f;
        rumore.scrollSpeed = intenso ? 0.2f : 0.15f;
        rumore.quality = ParticleSystemNoiseQuality.Low;

        ConfiguraRenderer(oggetto, materialFumo);
    }

    /// <summary>Luce puntiforme tremolante, senza ombre: piu' debole nel Realistico, forte nello Stress.</summary>
    private static void CreaLuce(Transform radice, bool intenso)
    {
        GameObject oggetto = new GameObject("Luce_Fuoco");
        oggetto.transform.SetParent(radice, false);
        oggetto.transform.localPosition = new Vector3(0f, 0.25f, 0f);

        Light luce = oggetto.AddComponent<Light>();
        luce.type = LightType.Point;
        luce.color = new Color(1f, 0.45f, 0.15f);
        luce.intensity = intenso ? 2.2f : 1f;
        luce.range = intenso ? 5f : 3.5f;
        luce.shadows = LightShadows.None;

        BlackoutFireFlicker tremolio = oggetto.AddComponent<BlackoutFireFlicker>();
        tremolio.intensitaBase = intenso ? 2.2f : 1f;
        tremolio.oscillazione = intenso ? 0.4f : 0.225f;
        tremolio.seme = radice.GetInstanceID() * 0.01f;
    }

    /// <summary>Imposta il renderer delle particelle in modalita billboard senza ombre.</summary>
    private static void ConfiguraRenderer(GameObject oggetto, Material materiale)
    {
        ParticleSystemRenderer renderer = oggetto.GetComponent<ParticleSystemRenderer>();
        if (renderer == null) return;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = materiale;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = -5f;
    }

    /// <summary>Textura e materiali condivisi, creati una sola volta.</summary>
    private static void PreparaRisorse()
    {
        if (textureMorbida == null)
        {
            const int lato = 64;
            textureMorbida = new Texture2D(lato, lato, TextureFormat.RGBA32, false, true)
            {
                name = "TEX_Fuoco_Soft_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixel = new Color[lato * lato];
            for (int y = 0; y < lato; y++)
            {
                for (int x = 0; x < lato; x++)
                {
                    float dx = (x + 0.5f) / lato * 2f - 1f;
                    float dy = (y + 0.5f) / lato * 2f - 1f;
                    float distanza = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float alpha = Mathf.Pow(1f - distanza, 1.6f);
                    pixel[y * lato + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            textureMorbida.SetPixels(pixel);
            textureMorbida.Apply(false, true);
        }

        if (materialFiamma == null)
        {
            materialFiamma = CreaMateriale("MAT_Fuoco_Fiamma_Runtime", true);
        }
        if (materialFumo == null)
        {
            materialFumo = CreaMateriale("MAT_Fuoco_Fumo_Runtime", false);
        }
    }

    /// <summary>Materiale URP per particelle: additivo per le fiamme, alpha blending per il fumo.</summary>
    private static Material CreaMateriale(string nome, bool additivo)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material materiale = new Material(shader) { name = nome };

        materiale.SetTexture("_BaseMap", textureMorbida);
        materiale.SetColor("_BaseColor", Color.white);
        materiale.SetFloat("_Surface", 1f);
        materiale.SetFloat("_Blend", additivo ? 2f : 0f);
        materiale.SetFloat("_SrcBlend", additivo ? 1f : 5f);
        materiale.SetFloat("_DstBlend", additivo ? 1f : 10f);
        materiale.SetFloat("_SrcBlendAlpha", 1f);
        materiale.SetFloat("_DstBlendAlpha", additivo ? 1f : 10f);
        materiale.SetFloat("_ZWrite", 0f);
        materiale.SetFloat("_Cull", 2f);
        materiale.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        materiale.SetOverrideTag("RenderType", "Transparent");
        materiale.renderQueue = (int)RenderQueue.Transparent;
        return materiale;
    }
}

/// <summary>Tremolio leggero della luce del fuoco, senza modificare altri script.</summary>
internal sealed class BlackoutFireFlicker : MonoBehaviour
{
    public float intensitaBase = 1f;
    public float oscillazione = 0.225f;
    public float seme;

    private Light luce;

    private void Awake()
    {
        luce = GetComponent<Light>();
    }

    private void Update()
    {
        if (luce == null) return;

        float tempo = Time.time * 7f + seme;
        float rumore = Mathf.PerlinNoise(tempo, seme) * 0.5f + Mathf.PerlinNoise(tempo * 0.37f, seme + 3f) * 0.5f;
        luce.intensity = intensitaBase * Mathf.Lerp(1f - oscillazione, 1f + oscillazione, rumore);
    }
}
