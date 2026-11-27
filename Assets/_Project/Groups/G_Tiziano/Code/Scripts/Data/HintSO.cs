using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Data
{
    /// <summary>
    /// Localized hint metadata for one ordered Chemical Alert procedure step.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ChemicalAlertHint", menuName = "SI81/Chemical Alert/Hint")]
    public sealed class HintSO : ScriptableObject
    {
        [SerializeField] private string stepId;
        [SerializeField] private string localizationKey;

        /// <summary>Gets the stable procedure step identifier.</summary>
        public string StepId => stepId;
        /// <summary>Gets the stable localization key emitted by the Director.</summary>
        public string LocalizationKey => localizationKey;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(stepId))
                UnityEngine.Debug.LogWarning($"[CA] {name}: stepId is empty.", this);
            if (string.IsNullOrWhiteSpace(localizationKey))
                UnityEngine.Debug.LogWarning($"[CA] {name}: localizationKey is empty.", this);
        }
    }
}
