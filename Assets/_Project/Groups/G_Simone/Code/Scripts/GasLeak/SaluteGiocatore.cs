using System.Collections;
using SI81.Rig;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Progressive CO poisoning feedback for the player.
///
/// GGD: after a while the heartbeat speeds up and the view gets blurry, then a red
/// vignette appears and the player starts losing hearts.
///
/// The effects are driven by a runtime URP Volume (Vignette, Chromatic Aberration,
/// Lens Distortion, Color Adjustments) so no post-processing profile has to be
/// authored in the project. A custom profile with a real blur can be plugged in
/// through <see cref="profiloOpzionale"/>.
/// </summary>
[DisallowMultipleComponent]
public class SaluteGiocatore : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Testa del giocatore. Se vuoto viene usata quella del rig condiviso.")]
    [SerializeField] Transform testa;
    [SerializeField] AudioSource audioSource;
    [Tooltip("Battito cardiaco in loop. Opzionale.")]
    [SerializeField] AudioClip battito;
    [Tooltip("Profilo Volume opzionale: se assegnato viene usato al posto di quello generato (es. con Depth of Field).")]
    [SerializeField] VolumeProfile profiloOpzionale;

    [Header("Effetti")]
    [SerializeField] bool abilitaEffetti = true;
    [SerializeField] Color coloreVignettatura = new Color(0.45f, 0.02f, 0.02f, 1f);
    [Tooltip("Intensità massima della vignettatura rossa.")]
    [Range(0f, 1f)] [SerializeField] float vignettaturaMax = 0.55f;
    [Tooltip("Intensità massima della distorsione ottica (vista offuscata).")]
    [Range(0f, 1f)] [SerializeField] float distorsioneMax = 0.35f;
    [Tooltip("Intensità massima dell’aberrazione cromatica.")]
    [Range(0f, 1f)] [SerializeField] float aberrazioneMax = 0.7f;
    [Tooltip("Desaturazione massima (valori negativi).")]
    [Range(-100f, 0f)] [SerializeField] float desaturazioneMax = -70f;
    [Tooltip("Variazione massima di esposizione (valori negativi scuriscono).")]
    [Range(-5f, 0f)] [SerializeField] float esposizioneMax = -0.8f;

    [Header("Battito")]
    [Tooltip("Pitch del battito quando il malessere è minimo.")]
    [SerializeField] float pitchLento = 0.85f;
    [Tooltip("Pitch del battito quando il malessere è massimo.")]
    [SerializeField] float pitchVeloce = 1.5f;

    [Header("Danno")]
    [Tooltip("Durata del lampo rosso quando si perde un cuore.")]
    [SerializeField] float durataLampoDanno = 0.6f;
    [SerializeField] float intensitaLampoDanno = 1f;

    public int Cuori { get; private set; }

    Volume _volume;
    Vignette _vignette;
    ChromaticAberration _aberrazione;
    LensDistortion _distorsione;
    ColorAdjustments _colori;

    float _male;
    float _lampo;
    bool _battitoAttivo;
    Coroutine _routineLampo;
    bool _svenuto;

    void Awake()
    {
        if (testa == null && RigRegistration.Instance != null)
        {
            testa = RigRegistration.Instance.Head;
        }

        if (testa == null && Camera.main != null)
        {
            testa = Camera.main.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (audioSource != null && battito != null)
        {
            audioSource.clip = battito;
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.pitch = pitchLento;
        }
    }

    void Start()
    {
        if (abilitaEffetti)
        {
            CreaVolume();
        }
    }

    void OnDestroy()
    {
        if (_volume != null)
        {
            Destroy(_volume.gameObject);
        }
    }

    /// <summary>Sets the number of hearts shown to the player (0..3).</summary>
    public void ImpostaCuori(int cuori)
    {
        Cuori = Mathf.Max(0, cuori);
    }

    /// <summary>
    /// Sets how bad the player feels. <paramref name="male"/> is 0 (fine) .. 1 (about to faint).
    /// </summary>
    public void ImpostaLivelloMale(float male, bool battitoAttivo)
    {
        if (_svenuto)
        {
            return;
        }

        _male = Mathf.Clamp01(male);
        _battitoAttivo = battitoAttivo;

        if (battitoAttivo && !_battitoAttivoPrecedente)
        {
            AvviaBattito();
        }
        else if (!battitoAttivo && _battitoAttivoPrecedente)
        {
            FermaBattito();
        }

        _battitoAttivoPrecedente = battitoAttivo;

        ApplicaEffetti();
    }

    bool _battitoAttivoPrecedente;

    /// <summary>Flashes the screen red: called when a heart is lost.</summary>
    public void MostraDanno()
    {
        if (_routineLampo != null)
        {
            StopCoroutine(_routineLampo);
        }

        _routineLampo = StartCoroutine(LampoDanno());
    }

    /// <summary>Final black-out used when the player runs out of hearts.</summary>
    public void Svenimento()
    {
        _svenuto = true;
        _male = 1f;
        ApplicaEffetti();
        FermaBattito();
    }

    IEnumerator LampoDanno()
    {
        _lampo = intensitaLampoDanno;
        float elapsed = 0f;

        while (elapsed < durataLampoDanno)
        {
            elapsed += Time.deltaTime;
            _lampo = Mathf.Lerp(intensitaLampoDanno, 0f, elapsed / Mathf.Max(0.01f, durataLampoDanno));
            ApplicaEffetti();
            yield return null;
        }

        _lampo = 0f;
        _routineLampo = null;
        ApplicaEffetti();
    }

    void ApplicaEffetti()
    {
        if (!abilitaEffetti || _volume == null)
        {
            return;
        }

        float male = Mathf.Clamp01(_male + _lampo);

        _volume.weight = Mathf.Clamp01(male * 1.15f);

        if (_vignette != null)
        {
            _vignette.color.Override(coloreVignettatura);
            _vignette.intensity.Override(Mathf.Lerp(0f, vignettaturaMax, male));
            _vignette.smoothness.Override(0.45f);
        }

        if (_aberrazione != null)
        {
            _aberrazione.intensity.Override(Mathf.Lerp(0f, aberrazioneMax, male * male));
        }

        if (_distorsione != null)
        {
            _distorsione.intensity.Override(Mathf.Lerp(0f, -distorsioneMax, male * male));
            _distorsione.scale.Override(1f);
        }

        if (_colori != null)
        {
            _colori.saturation.Override(Mathf.Lerp(0f, desaturazioneMax, male));
            _colori.postExposure.Override(Mathf.Lerp(0f, esposizioneMax, male));
        }

        if (audioSource != null && _battitoAttivo)
        {
            audioSource.pitch = Mathf.Lerp(pitchLento, pitchVeloce, male);
            audioSource.volume = Mathf.Lerp(0.25f, 1f, male);
        }
    }

    void AvviaBattito()
    {
        if (audioSource == null || battito == null)
        {
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void FermaBattito()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    void CreaVolume()
    {
        if (profiloOpzionale != null)
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 200f;
            _volume.weight = 0f;
            _volume.profile = profiloOpzionale;

            _vignette = Trova<Vignette>(profiloOpzionale);
            _aberrazione = Trova<ChromaticAberration>(profiloOpzionale);
            _distorsione = Trova<LensDistortion>(profiloOpzionale);
            _colori = Trova<ColorAdjustments>(profiloOpzionale);
            return;
        }

        GameObject host = new GameObject("Volume_SaluteGiocatore");
        host.transform.SetParent(transform, false);

        _volume = host.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 200f;
        _volume.weight = 0f;
        _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.profile.name = "Volume_SaluteGiocatore_Profile";

        _vignette = _volume.profile.Add<Vignette>(true);
        _vignette.color.Override(coloreVignettatura);
        _vignette.center.Override(new Vector2(0.5f, 0.5f));
        _vignette.smoothness.Override(0.45f);
        _vignette.intensity.Override(0f);

        _aberrazione = _volume.profile.Add<ChromaticAberration>(true);
        _aberrazione.intensity.Override(0f);

        _distorsione = _volume.profile.Add<LensDistortion>(true);
        _distorsione.intensity.Override(0f);
        _distorsione.scale.Override(1f);

        _colori = _volume.profile.Add<ColorAdjustments>(true);
        _colori.saturation.Override(0f);
        _colori.postExposure.Override(0f);
    }

    static T Trova<T>(VolumeProfile profilo) where T : VolumeComponent
    {
        return profilo.TryGet(out T componente) ? componente : null;
    }
}
