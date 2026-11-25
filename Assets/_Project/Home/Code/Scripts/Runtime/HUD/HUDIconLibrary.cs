using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Libreria delle icone ICN_ per nome (asset SO_HUDIcons, generato da SI81/UI/Build HUD). Serve a chi riceve un nome
    /// di icona come testo (HUDService.ShowEventAlert, LabelAnchor): a runtime non si carica per nome senza Resources.
    /// Accetta "ICN_Warning_Filled" o "Warning_Filled".
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/HUD Icons", fileName = "SO_HUDIcons")]
    public class HUDIconLibrary : ScriptableObject
    {
        /// <summary>Una voce: nome dell'asset e sprite.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Nome dello sprite, es. ICN_Warning_Filled.</summary>
            public string Name;
            /// <summary>Sprite bianco 128x128.</summary>
            public Sprite Sprite;
        }

        [Tooltip("Generato dal builder: tutte le icone ICN_ della cartella Icons.")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, Sprite> lookup;

        /// <summary>
        /// Cerca uno sprite per nome.
        /// </summary>
        /// <param name="iconName">Nome con o senza prefisso ICN_ (vuoto = null).</param>
        /// <returns>Lo sprite o null.</returns>
        public Sprite Find(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i] != null && !string.IsNullOrEmpty(entries[i].Name)) lookup[entries[i].Name] = entries[i].Sprite;
            }
            if (lookup.TryGetValue(iconName, out Sprite sprite)) return sprite;
            return lookup.TryGetValue("ICN_" + iconName, out sprite) ? sprite : null;
        }

        /// <summary>
        /// Sostituisce le voci. Usato solo dal builder Editor.
        /// </summary>
        internal void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries;
            lookup = null;
        }
    }
}
