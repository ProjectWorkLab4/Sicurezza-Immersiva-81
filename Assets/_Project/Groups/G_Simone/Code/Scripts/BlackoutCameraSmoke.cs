using SI81.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Fumo 3D che segue la camera nel solo livello Stress di Blackout: particelle vicine all'obiettivo,
/// attivate da OnBlackout con densita' che cresce gradualmente. L'effetto rende la visione piu'
/// difficile senza mai bloccarla: emissione, dimensione e opacita' hanno un tetto moderato.
/// Nessuna modifica alla scena: si avvia da solo quando SCN_Blackout viene caricata, anche in modo
/// additivo da SCN_Core. Guidato e Realistico non generano fumo. Tutto viene rimosso a fine missione.
/// </summary>
[DisallowMultipleComponent]
public sealed class BlackoutCameraSmoke : MonoBehaviour
{
    const string NOME_HOST = "MNG_BlackoutCameraSmoke";
    const string NOME_FIGLIO = "VFX_FumoCamera";
    const string NOME_TEXTURE = "TEX_FumoCamera_Soft_Runtime";
    const string NOME_MATERIAL = "MAT_FumoCamera_Runtime";

    static readonly int IdColoreBase = Shader.PropertyToID("_BaseColor");

    static bool inAttesa;
    static Texture2D textureMorbida;
    static Material materialeBase;

    [Tooltip("Secondi dal blackout per raggiungere la densita' massima (timer Stress 90 s, blackout dopo ~5 s).")]
    [SerializeField, Min(1f)] float rampaDurataSecondi = 85f;

    [Tooltip("Particelle/secondo appena scatta il blackout.")]
    [SerializeField, Min(0f)] float emissioneIniziale = 3f;

    [Tooltip("Particelle/secondo alla fine della rampa: fumo nero piu' denso, ma la vista resta utilizzabile.")]
    [SerializeField, Min(0f)] float emissioneMassima = 26f;

    GameManager gameManager;
    GameObject hostParticelle;
    ParticleSystem sistema;
    ParticleSystemRenderer rendererParticelle;
    Material materialeIstanza;
    float scalaCamera = 1f;
    bool blackoutAttivo;
    float tempoDalBlackout;

    /// <summary>
    /// Il callback SubsystemRegistration vale per ogni caricamento di scena, compresi quelli additivi;
    /// AfterSceneLoad invece scatta solo per la prima scena (SCN_Core) e mancherebbe SCN_Blackout.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Inizializza()
    {
        inAttesa = false;
        textureMorbida = null;
        materialeBase = null;
        SceneManager.sceneLoaded -= OnScenaCaricata;
        SceneManager.sceneLoaded += OnScenaCaricata;
    }

    private static void OnScenaCaricata(Scene scena, LoadSceneMode modalita)
    {
        AllAvvioScena();
    }

    /// <summary>Crea l'host solo nella scena che contiene il GameManager del Blackout.</summary>
    private static void AllAvvioScena()
    {
        if (inAttesa) return;
        if (FindFirstObjectByType<GameManager>() == null) return;

        // Un replay ricarica SCN_Blackout: un host vecchio resterebbe iscritto al GameManager
        // della sessione precedente, quindi lo sostituiamo sempre con uno nuovo.
        BlackoutCameraSmoke[] vecchi = FindObjectsByType<BlackoutCameraSmoke>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BlackoutCameraSmoke vecchio in vecchi)
        {
            Destroy(vecchio.gameObject);
        }

        inAttesa = true;
        GameObject host = new GameObject(NOME_HOST);
        host.AddComponent<BlackoutCameraSmoke>();
    }

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnBlackout += HandleBlackout;
        }

        MissionSession.Ended += HandleMissionEnded;
    }

    private void Start()
    {
        inAttesa = false;
    }

    /// <summary>
    /// Aumenta gradualmente densita', dimensione e opacita'. Usa deltaTime: con la pausa a
    /// timeScale = 0 la rampa si ferma insieme al resto del gioco.
    /// </summary>
    private void Update()
    {
        if (!blackoutAttivo || sistema == null)
        {
            return;
        }

        tempoDalBlackout += Time.deltaTime;
        float f = Mathf.Clamp01(tempoDalBlackout / Mathf.Max(1f, rampaDurataSecondi));

        ParticleSystem.EmissionModule emissione = sistema.emission;
        emissione.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(emissioneIniziale, emissioneMassima, f));

        ParticleSystem.MainModule main = sistema.main;
        ParticleSystem.MinMaxCurve dimensione = main.startSize;
        dimensione.constantMin = Mathf.Lerp(0.25f, 0.45f, f) / scalaCamera;
        dimensione.constantMax = Mathf.Lerp(0.6f, 0.95f, f) / scalaCamera;
        main.startSize = dimensione;

        if (materialeIstanza != null)
        {
            Color colore = materialeIstanza.GetColor(IdColoreBase);
            colore.a = Mathf.Lerp(0.65f, 0.95f, f);
            materialeIstanza.SetColor(IdColoreBase, colore);
        }
    }

    /// <summary>Solo Stress: crea le particelle davanti alla camera quando scatta il blackout.</summary>
    private void HandleBlackout()
    {
        DifficultyLevel? livello = MissionSession.IsActive
            ? MissionSession.Level
            : UINavigationContext.SelectedLevel;

        if (!livello.HasValue || livello.Value != DifficultyLevel.Crisis)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] camere = Camera.allCameras;
            if (camere != null && camere.Length > 0)
            {
                camera = camere[0];
            }
        }

        if (camera == null)
        {
            Debug.LogWarning("[Blackout] Nessuna camera trovata: fumo camera non creato.", this);
            return;
        }

        CreaFumo(camera.transform);
        blackoutAttivo = true;
        tempoDalBlackout = 0f;
        Debug.Log("[Blackout] Fumo camera attivo (Stress): particelle vicine all'obiettivo, densita' limitata.");
    }

    /// <summary>Particelle in spazio locale, genitore la camera: seguono la testa del giocatore.</summary>
    private void CreaFumo(Transform camera)
    {
        PreparaRisorse();

        scalaCamera = Mathf.Max(0.001f, camera.lossyScale.x);

        hostParticelle = new GameObject(NOME_FIGLIO);
        hostParticelle.transform.SetParent(camera, false);

        // 0,45 m davanti all'obiettivo, assegnati in coordinate mondo per compensare la scala del rig.
        hostParticelle.transform.position = camera.position + camera.forward * 0.45f;
        hostParticelle.transform.rotation = camera.rotation;

        sistema = hostParticelle.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = sistema.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f / scalaCamera, 0.6f / scalaCamera);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 110;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emissione = sistema.emission;
        emissione.rateOverTime = new ParticleSystem.MinMaxCurve(emissioneIniziale);
        emissione.rateOverDistance = 0f;

        // Volume sottile davanti alla lente: le particelle restano vicine alla camera.
        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(0.7f / scalaCamera, 0.45f / scalaCamera, 0.05f / scalaCamera);

        ParticleSystem.ColorOverLifetimeModule colore = sistema.colorOverLifetime;
        colore.enabled = true;
        Gradient gradiente = new Gradient();
        // Fumo nero: il colore e' quasi nero, quindi la leggibilita' arriva dall'opacita'
        // e dalla densita', non da un tono chiaro (che sarebbe irrealistico per il fumo).
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.04f, 0.04f, 0.04f), 0f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.65f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        colore.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystem.SizeOverLifetimeModule dimensione = sistema.sizeOverLifetime;
        dimensione.enabled = true;
        dimensione.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.3f)));

        ParticleSystem.NoiseModule rumore = sistema.noise;
        rumore.enabled = true;
        rumore.strength = 0.06f / scalaCamera;
        rumore.frequency = 0.3f;
        rumore.scrollSpeed = 0.08f;
        rumore.quality = ParticleSystemNoiseQuality.Low;

        materialeIstanza = new Material(materialeBase) { name = NOME_MATERIAL + "_Istanza" };
        materialeIstanza.SetColor(IdColoreBase, new Color(1f, 1f, 1f, 0.55f));

        rendererParticelle = hostParticelle.GetComponent<ParticleSystemRenderer>();
        rendererParticelle.renderMode = ParticleSystemRenderMode.Billboard;
        // sharedMaterial evita che Unity cloni il materiale: la rampa di alpha in Update
        // modifica cosi' sempre il materiale che la particella rende davvero.
        rendererParticelle.sharedMaterial = materialeIstanza;
        rendererParticelle.shadowCastingMode = ShadowCastingMode.Off;
        rendererParticelle.receiveShadows = false;
        rendererParticelle.sortingFudge = 20f;
    }

    private void HandleMissionEnded(MissionResult result)
    {
        RimuoviFumo();

        // L'host viene ricreato a ogni caricamento della scena: cosi' un replay non eredita
        // la sottoscrizione al GameManager della missione precedente.
        Destroy(gameObject);
    }

    /// <summary>Smoglia tutto a fine missione e in OnDestroy, senza lasciare oggetti nella scena.</summary>
    private void RimuoviFumo()
    {
        blackoutAttivo = false;

        if (sistema != null)
        {
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sistema = null;
        }

        if (hostParticelle != null)
        {
            Destroy(hostParticelle);
            hostParticelle = null;
            rendererParticelle = null;
        }

        if (materialeIstanza != null)
        {
            Destroy(materialeIstanza);
            materialeIstanza = null;
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnBlackout -= HandleBlackout;
        }

        MissionSession.Ended -= HandleMissionEnded;
        RimuoviFumo();
        inAttesa = false;
    }

    /// <summary>Texture soffice e materiale URP condivisi, creati una sola volta.</summary>
    private static void PreparaRisorse()
    {
        if (textureMorbida == null)
        {
            const int lato = 64;
            textureMorbida = new Texture2D(lato, lato, TextureFormat.RGBA32, false, true)
            {
                name = NOME_TEXTURE,
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
                    float alpha = Mathf.Pow(1f - distanza, 1.8f);
                    pixel[y * lato + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            textureMorbida.SetPixels(pixel);
            textureMorbida.Apply(false, true);
        }

        if (materialeBase == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            materialeBase = new Material(shader) { name = NOME_MATERIAL };
            materialeBase.SetTexture("_BaseMap", textureMorbida);
            materialeBase.SetColor("_BaseColor", Color.white);
            materialeBase.SetFloat("_Surface", 1f);
            materialeBase.SetFloat("_Blend", 0f);
            materialeBase.SetFloat("_SrcBlend", 5f);
            materialeBase.SetFloat("_DstBlend", 10f);
            materialeBase.SetFloat("_SrcBlendAlpha", 1f);
            materialeBase.SetFloat("_DstBlendAlpha", 10f);
            materialeBase.SetFloat("_ZWrite", 0f);
            materialeBase.SetFloat("_Cull", 2f);
            materialeBase.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            materialeBase.SetOverrideTag("RenderType", "Transparent");
            materialeBase.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
