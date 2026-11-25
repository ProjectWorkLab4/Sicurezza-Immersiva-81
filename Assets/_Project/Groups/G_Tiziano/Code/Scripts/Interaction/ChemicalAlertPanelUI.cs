using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Presents the approved SDS, transient procedural warnings, and toxic-air vignette.
    /// This component intentionally has no success or victory presentation.
    /// </summary>
    public sealed class ChemicalAlertPanelUI : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private ScenarioInputBindings inputBindings;
        [SerializeField] private CanvasGroup sdsPanel;
        [SerializeField] private TMP_Text sdsText;
        [SerializeField] private Image[] sdsPictograms = new Image[0];
        [SerializeField] private TextAsset formicAcidSds;
        [SerializeField, TextArea(10, 24)] private string sdsDisplayText;
        [SerializeField] private CanvasGroup errorPanel;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Image toxicVignette;
        [SerializeField, Range(0f, 1f)] private float vignetteMaxAlpha = 0.72f;
        [SerializeField, Min(0.1f)] private float errorDisplaySeconds = 4.5f;
        [SerializeField] private AudioSource coughAudioSource;
        [SerializeField] private AudioClip coughAudioClip;

        private float errorRemaining;
        private AudioClip generatedCoughClip;
        private Texture2D generatedVignetteTexture;
        private Sprite generatedVignetteSprite;

        private void OnEnable()
        {
            if (director == null)
                return;

            director.SdsRead += HandleSdsRead;
            director.SdsPanelDismissRequested += HideSds;
            director.ErrorReported += ShowError;
            director.ToxicVignetteChanged += SetToxicVignette;
            director.CoughRequested += PlayCough;
        }

        private void OnDisable()
        {
            if (director == null)
                return;

            director.SdsRead -= HandleSdsRead;
            director.SdsPanelDismissRequested -= HideSds;
            director.ErrorReported -= ShowError;
            director.ToxicVignetteChanged -= SetToxicVignette;
            director.CoughRequested -= PlayCough;
        }

        private void Start()
        {
            SetVisible(sdsPanel, director != null && director.IsSdsPanelOpen);
            SetVisible(errorPanel, false);
            if (coughAudioSource == null)
                coughAudioSource = GetComponent<AudioSource>();
            if (coughAudioClip == null)
                generatedCoughClip = CreateSoftCoughCue();
            if (toxicVignette != null && toxicVignette.sprite == null)
            {
                generatedVignetteSprite = CreateVignetteSprite(out generatedVignetteTexture);
                toxicVignette.sprite = generatedVignetteSprite;
                toxicVignette.type = Image.Type.Simple;
                toxicVignette.preserveAspect = false;
            }
            if (sdsPictograms != null)
                foreach (Image pictogram in sdsPictograms)
                    if (pictogram != null) pictogram.preserveAspect = true;
            if (toxicVignette != null)
                SetToxicVignette(0f, new Color32(150, 195, 90, 255));

            SetSdsText();
        }

        private void Update()
        {
            if (director != null && director.IsSdsPanelOpen &&
                inputBindings != null &&
                inputBindings.GetDismissDown(SI81.Input.Hand.Right))
                director.DismissSdsPanel();

            if (errorRemaining <= 0f)
                return;

            errorRemaining = Mathf.Max(0f, errorRemaining - Time.unscaledDeltaTime);
            if (errorRemaining <= 0f)
                SetVisible(errorPanel, false);
        }

        private void HandleSdsRead(SubstanceVariant variant)
        {
            SetSdsText();
            SetVisible(sdsPanel, true);
        }

        private void SetSdsText()
        {
            if (sdsText == null)
                return;

            if (!string.IsNullOrWhiteSpace(sdsDisplayText))
                sdsText.text = sdsDisplayText;
            else if (formicAcidSds != null)
                sdsText.text = formicAcidSds.text;
        }

        private void HideSds()
        {
            SetVisible(sdsPanel, false);
        }

        private void ShowError(string localizationKey)
        {
            if (errorText != null)
                errorText.text = ResolveErrorMessage(localizationKey);

            errorRemaining = Mathf.Max(0.1f, errorDisplaySeconds);
            SetVisible(errorPanel, true);
        }

        private void SetToxicVignette(float intensity, Color tint)
        {
            if (toxicVignette == null)
                return;

            tint.a = Mathf.Clamp01(intensity) * vignetteMaxAlpha;
            toxicVignette.color = tint;
        }

        private void PlayCough()
        {
            AudioClip clip = coughAudioClip == null ? generatedCoughClip : coughAudioClip;
            if (coughAudioSource != null && clip != null)
                coughAudioSource.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
            if (generatedVignetteSprite != null)
                Destroy(generatedVignetteSprite);
            if (generatedVignetteTexture != null)
                Destroy(generatedVignetteTexture);
            if (generatedCoughClip != null)
                Destroy(generatedCoughClip);
        }

        private static AudioClip CreateSoftCoughCue()
        {
            const int sampleRate = 22050;
            const float duration = 0.48f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(8142);
            float lowPass = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                float pulseA = PulseEnvelope(time, 0.025f, 0.155f);
                float pulseB = PulseEnvelope(time, 0.245f, 0.385f);
                float envelope = Mathf.Max(pulseA, pulseB);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                lowPass = Mathf.Lerp(lowPass, noise, 0.28f);
                float breath = lowPass * 0.72f + Mathf.Sin(time * Mathf.PI * 2f * 92f) * 0.18f;
                samples[i] = breath * envelope * 0.32f;
            }

            AudioClip clip = AudioClip.Create("CA_SoftCoughCue", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float PulseEnvelope(float time, float start, float end)
        {
            if (time < start || time > end)
                return 0f;

            float progress = (time - start) / (end - start);
            float attack = Mathf.Clamp01(progress / 0.12f);
            float release = Mathf.Clamp01((1f - progress) / 0.24f);
            return attack * release;
        }

        private static Sprite CreateVignetteSprite(out Texture2D texture)
        {
            const int size = 128;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "CA_ToxicVignetteTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radius = Mathf.Sqrt(nx * nx * 0.72f + ny * ny);
                    float edge = Mathf.SmoothStep(0.42f, 1.15f, radius);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(edge) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static string ResolveErrorMessage(string key)
        {
            switch (key)
            {
                case "chemical.error.gloves":
                    return "ERRORE — Indossa i guanti chimici prima di usare gli strumenti. Ogni tentativo senza guanti costa una vita.";
                case "chemical.error.no_mask":
                    return "ATTENZIONE — Avvicinarsi alla perdita senza mascherina provoca tosse.";
                case "chemical.error.eye_protection":
                    return "PERICOLO — L'aria è tossica: proteggi gli occhi con occhiali a tenuta o visiera.";
                case "chemical.error.procedure_order":
                    return "PROCEDURA NON RISPETTATA — Puoi continuare, ma l'esito potrebbe essere Parziale.";
                default:
                    return "ATTENZIONE — Procedura non corretta. Puoi continuare, ma l'esito potrebbe essere Parziale.";
            }
        }
    }
}
