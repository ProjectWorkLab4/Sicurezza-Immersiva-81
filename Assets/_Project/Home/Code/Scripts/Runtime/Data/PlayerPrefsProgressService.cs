using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Servizio dei progressi persistente: salva i risultati come JSON in PlayerPrefs sotto la chiave "si81.progress".
    /// Un solo profilo (CLAUDE.md §8). Se il JSON è illeggibile si riparte da zero senza errori bloccanti.
    /// </summary>
    public sealed class PlayerPrefsProgressService : ProgressServiceBase
    {
        /// <summary>Chiave PlayerPrefs dei risultati.</summary>
        public const string KEY = "si81.progress";

        /// <summary>Contenitore serializzabile da JsonUtility (che non salva liste "nude").</summary>
        [Serializable]
        private class SaveData
        {
            /// <summary>Tutti i risultati, dal più vecchio al più recente.</summary>
            public List<MissionResult> Items = new List<MissionResult>();
        }

        /// <summary>
        /// Crea il servizio e carica i risultati salvati.
        /// </summary>
        /// <param name="catalog">Catalogo con i badge.</param>
        public PlayerPrefsProgressService(MissionCatalog catalog) : base(catalog)
        {
            Load();
        }

        /// <summary>
        /// Cancella i risultati salvati e in memoria (azione di debug / ripristino profilo).
        /// </summary>
        public void ClearAll()
        {
            PlayerPrefs.DeleteKey(KEY);
            SetResults(null);
            RaiseChanged();
        }

        /// <summary>
        /// Salva dopo ogni nuovo risultato.
        /// </summary>
        protected override void OnResultsChanged()
        {
            Save();
        }

        /// <summary>
        /// Legge il JSON da PlayerPrefs.
        /// </summary>
        private void Load()
        {
            if (!PlayerPrefs.HasKey(KEY)) return;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY));
                SetResults(data != null ? data.Items : null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SI81 UI] Progressi illeggibili in PlayerPrefs, si riparte da zero: " + exception.Message);
                SetResults(null);
            }
        }

        /// <summary>
        /// Scrive il JSON in PlayerPrefs.
        /// </summary>
        private void Save()
        {
            var data = new SaveData();
            for (int i = 0; i < AllResults.Count; i++) data.Items.Add(AllResults[i]);
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
