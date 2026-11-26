using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SI81.UI;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Presenta la scheda SDS 3D e il cue audio della tosse; la vignetta vive temporaneamente nell'istanza HUD comune.
    /// </summary>
    public sealed class ChemicalAlertPanelUI : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private Sprite[] sdsPictogramAssets = new Sprite[0];
        [SerializeField] private GameObject sdsWorldPanel;
        [SerializeField] private TMP_Text sdsWorldText;
        [SerializeField] private SpriteRenderer[] sdsWorldPictograms = new SpriteRenderer[0];
        [SerializeField] private TextAsset formicAcidSds;
        [SerializeField, TextArea(10, 24)] private string sdsDisplayText;
        [SerializeField] private AudioSource coughAudioSource;
        [SerializeField] private AudioClip coughAudioClip;
        [SerializeField, Range(0f, 0.4f)] private float maxToxicVignetteOpacity = 0.22f;
        [SerializeField, Min(0.1f)] private float toxicVignetteFadeSpeed = 0.8f;

        private AudioClip generatedCoughClip;
        private Canvas toxicVignetteCanvas;
        private Image toxicVignetteImage;
        private Texture2D toxicVignetteTexture;
        private Sprite toxicVignetteSprite;
        private float targetVignetteIntensity;
        private float shownVignetteIntensity;
        private Color vignetteHazardColor = Color.yellow;

        private void OnEnable()
        {
            if (director == null)
                return;

            director.SdsRead += HandleSdsRead;
            director.SdsPanelDismissRequested += HideSds;
            director.CoughRequested += PlayCough;
            director.ToxicVignetteChanged += HandleToxicVignetteChanged;
            director.ResultReady += HandleResultReady;
            HUDService.Ready += HandleHudReady;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SdsRead -= HandleSdsRead;
                director.SdsPanelDismissRequested -= HideSds;
                director.CoughRequested -= PlayCough;
                director.ToxicVignetteChanged -= HandleToxicVignetteChanged;
                director.ResultReady -= HandleResultReady;
            }

            HUDService.Ready -= HandleHudReady;
            DestroyToxicVignetteOverlay();
        }

        private void Start()
        {
            SetSdsWorldVisible(director != null && director.IsSdsPanelOpen);
            if (coughAudioSource == null)
                coughAudioSource = GetComponent<AudioSource>();
            if (coughAudioClip == null)
                generatedCoughClip = CreateSoftCoughCue();

            SetSdsText();
            SyncSdsPictograms();
        }

        private void Update()
        {
            if (toxicVignetteImage == null && targetVignetteIntensity > 0f && HUDService.IsReady)
                CreateToxicVignetteOverlay();

            if (toxicVignetteImage == null)
                return;

            shownVignetteIntensity = Mathf.MoveTowards(
                shownVignetteIntensity,
                targetVignetteIntensity,
                toxicVignetteFadeSpeed * Time.unscaledDeltaTime);

            Color tint = Color.Lerp(Color.black, vignetteHazardColor, 0.45f);
            tint.a = maxToxicVignetteOpacity * shownVignetteIntensity;
            toxicVignetteImage.color = tint;

            if (!toxicVignetteCanvas.gameObject.activeSelf && tint.a > 0.001f)
                toxicVignetteCanvas.gameObject.SetActive(true);
            else if (toxicVignetteCanvas.gameObject.activeSelf && tint.a <= 0.001f)
                toxicVignetteCanvas.gameObject.SetActive(false);
        }

        private void HandleSdsRead(SubstanceVariant variant)
        {
            SetSdsText();
            SyncSdsPictograms();
            SetSdsWorldVisible(true);
        }

        private void SetSdsText()
        {
            string content = !string.IsNullOrWhiteSpace(sdsDisplayText)
                ? sdsDisplayText
                : formicAcidSds != null ? formicAcidSds.text : string.Empty;

            if (sdsWorldText != null)
                sdsWorldText.text = content;
        }

        private void SyncSdsPictograms()
        {
            if (sdsPictogramAssets == null || sdsWorldPictograms == null)
                return;

            int count = Mathf.Min(sdsPictogramAssets.Length, sdsWorldPictograms.Length);
            for (int i = 0; i < count; i++)
            {
                if (sdsWorldPictograms[i] == null)
                    continue;

                sdsWorldPictograms[i].sprite = sdsPictogramAssets[i];
                sdsWorldPictograms[i].enabled = sdsPictogramAssets[i] != null;
            }
        }

        private void HideSds()
        {
            SetSdsWorldVisible(false);
        }

        private void SetSdsWorldVisible(bool visible)
        {
            if (sdsWorldPanel != null)
                sdsWorldPanel.SetActive(visible);
        }

        private void PlayCough()
        {
            AudioClip clip = coughAudioClip == null ? generatedCoughClip : coughAudioClip;
            if (coughAudioSource != null && clip != null)
                coughAudioSource.PlayOneShot(clip);
        }

        private void HandleToxicVignetteChanged(float intensity, Color hazardColor)
        {
            targetVignetteIntensity = Mathf.Clamp01(intensity);
            vignetteHazardColor = hazardColor;
            if (targetVignetteIntensity > 0f && HUDService.IsReady)
                CreateToxicVignetteOverlay();
        }

        private void HandleResultReady(ScenarioResult result)
        {
            targetVignetteIntensity = 0f;
        }

        private void HandleHudReady()
        {
            if (targetVignetteIntensity > 0f)
                CreateToxicVignetteOverlay();
        }

        /// <summary>
        /// Crea l'effetto soltanto dentro l'istanza HUD condivisa durante Chemical Alert;
        /// non aggiunge Canvas o UI persistenti alla scena né modifica prefab condivisi.
        /// </summary>
        private void CreateToxicVignetteOverlay()
        {
            if (toxicVignetteImage != null)
                return;

            HUDRoot hudRoot = FindFirstObjectByType<HUDRoot>(FindObjectsInactive.Include);
            if (hudRoot == null)
                return;

            GameObject overlay = new GameObject(
                "HUD_ChemicalAlertToxicVignette",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            overlay.transform.SetParent(hudRoot.transform, false);

            RectTransform rect = (RectTransform)overlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            toxicVignetteCanvas = overlay.GetComponent<Canvas>();
            toxicVignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            toxicVignetteCanvas.overrideSorting = true;
            toxicVignetteCanvas.sortingOrder = 100;

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            toxicVignetteImage = overlay.AddComponent<Image>();
            toxicVignetteImage.sprite = CreateToxicVignetteSprite();
            toxicVignetteImage.raycastTarget = false;
            toxicVignetteImage.color = Color.clear;
            overlay.SetActive(false);
        }

        private Sprite CreateToxicVignetteSprite()
        {
            const int resolution = 128;
            const float cornerRadius = 0.70710678f;
            toxicVignetteTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "TEX_ChemicalAlertToxicVignette_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution - 0.5f;
                    float v = (y + 0.5f) / resolution - 0.5f;
                    float radius = new Vector2(u, v).magnitude / cornerRadius;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.98f, radius));
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            toxicVignetteTexture.SetPixels(pixels);
            toxicVignetteTexture.Apply(false, true);
            toxicVignetteSprite = Sprite.Create(
                toxicVignetteTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution);
            toxicVignetteSprite.name = "SPR_ChemicalAlertToxicVignette_Runtime";
            return toxicVignetteSprite;
        }

        private void DestroyToxicVignetteOverlay()
        {
            if (toxicVignetteCanvas != null)
                Destroy(toxicVignetteCanvas.gameObject);
            if (toxicVignetteSprite != null)
                Destroy(toxicVignetteSprite);
            if (toxicVignetteTexture != null)
                Destroy(toxicVignetteTexture);

            toxicVignetteCanvas = null;
            toxicVignetteImage = null;
            toxicVignetteSprite = null;
            toxicVignetteTexture = null;
            targetVignetteIntensity = 0f;
            shownVignetteIntensity = 0f;
        }

        private void OnDestroy()
        {
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

    }
}
