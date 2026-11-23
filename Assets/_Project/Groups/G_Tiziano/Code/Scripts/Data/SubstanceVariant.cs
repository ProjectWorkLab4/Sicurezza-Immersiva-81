using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Data
{
    /// <summary>
    /// Data for one possible unknown chemical substance variant.
    /// </summary>
    [CreateAssetMenu(fileName = "SubstanceVariant", menuName = "SI81/Chemical Alert/Substance Variant")]
    public sealed class SubstanceVariant : ScriptableObject
    {
        [SerializeField] private string variantId;
        [SerializeField] private string sdsTitleKey;
        [SerializeField] private string sdsBodyKey;
        [SerializeField] private PPEItem[] requiredPpe = new PPEItem[0];
        [SerializeField, Min(0f)] private float growthRate = 0.1f;
        [SerializeField, Min(0f)] private float maxRadius = 1f;
        [SerializeField] private Color puddleColor = Color.yellow;

        /// <summary>Gets the stable substance identifier.</summary>
        public string VariantId => variantId;
        /// <summary>Gets the SDS title localization key.</summary>
        public string SdsTitleKey => sdsTitleKey;
        /// <summary>Gets the SDS body localization key.</summary>
        public string SdsBodyKey => sdsBodyKey;
        /// <summary>Gets the required PPE set.</summary>
        public PPEItem[] RequiredPpe => requiredPpe;
        /// <summary>Gets the puddle growth rate.</summary>
        public float GrowthRate => growthRate;
        /// <summary>Gets the maximum puddle radius.</summary>
        public float MaxRadius => maxRadius;
        /// <summary>Gets the placeholder puddle color.</summary>
        public Color PuddleColor => puddleColor;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(variantId))
                UnityEngine.Debug.LogWarning($"[CA] {name}: variantId is empty.", this);

            if (string.IsNullOrWhiteSpace(sdsTitleKey) || string.IsNullOrWhiteSpace(sdsBodyKey))
                UnityEngine.Debug.LogWarning($"[CA] {name}: SDS text keys are incomplete.", this);

            if (requiredPpe == null || requiredPpe.Length == 0)
                UnityEngine.Debug.LogWarning($"[CA] {name}: required PPE set is empty.", this);

            if (maxRadius <= 0f)
                UnityEngine.Debug.LogWarning($"[CA] {name}: maxRadius must be greater than zero.", this);
        }
    }
}
