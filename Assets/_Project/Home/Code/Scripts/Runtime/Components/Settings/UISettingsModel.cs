using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Contenitore dei valori delle impostazioni, indicizzati da un id testuale (es. "audio.master_volume").
    /// Le righe di Impostazioni (PANEL_Settings*) si collegano a uno store con <c>Bind</c>: nessun codice di schermata
    /// duplicato. L'evento <see cref="Changed"/> porta l'id modificato (null o vuoto = potrebbe essere cambiato tutto,
    /// es. dopo il ripristino dei predefiniti).
    /// </summary>
    public interface IUISettingsStore
    {
        /// <summary>Legge un valore booleano.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        bool GetBool(string id);
        /// <summary>Scrive un valore booleano e, se è cambiato, lancia <see cref="Changed"/>.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Nuovo valore.</param>
        void SetBool(string id, bool value);
        /// <summary>Legge un valore decimale.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        float GetFloat(string id);
        /// <summary>Scrive un valore decimale e, se è cambiato, lancia <see cref="Changed"/>.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Nuovo valore.</param>
        void SetFloat(string id, float value);
        /// <summary>Legge un valore intero.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        int GetInt(string id);
        /// <summary>Scrive un valore intero e, se è cambiato, lancia <see cref="Changed"/>.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Nuovo valore.</param>
        void SetInt(string id, int value);
        /// <summary>Lanciato quando un valore cambia; il parametro è l'id (null/vuoto = tutto può essere cambiato).</summary>
        event Action<string> Changed;
    }

    /// <summary>
    /// Parte opzionale di uno store: valori predefiniti per le impostazioni mai scritte e ripristino ("Ripristina predefiniti").
    /// Le righe registrano il proprio predefinito in <c>Bind</c> (se lo store lo supporta).
    /// </summary>
    public interface IUISettingsDefaults
    {
        /// <summary>Registra il valore predefinito di un'impostazione booleana (non sovrascrive un valore già scritto).</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Valore predefinito.</param>
        void RegisterDefaultBool(string id, bool value);
        /// <summary>Registra il valore predefinito di un'impostazione decimale.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Valore predefinito.</param>
        void RegisterDefaultFloat(string id, float value);
        /// <summary>Registra il valore predefinito di un'impostazione intera.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="value">Valore predefinito.</param>
        void RegisterDefaultInt(string id, int value);
        /// <summary>Cancella i valori scritti di tutte le impostazioni con un predefinito e lancia <see cref="IUISettingsStore.Changed"/>.</summary>
        void ResetToDefaults();
    }

    /// <summary>
    /// Base comune degli store: tutti i valori sono float (bool = 0/1, int esatto fino a 2^24), predefiniti registrati
    /// dalle righe, evento <see cref="Changed"/>. Le sottoclassi decidono solo DOVE si scrive (PlayerPrefs, memoria...).
    /// </summary>
    public abstract class UISettingsStoreBase : IUISettingsStore, IUISettingsDefaults
    {
        // Predefiniti registrati (id -> valore grezzo).
        private readonly Dictionary<string, float> defaults = new Dictionary<string, float>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public event Action<string> Changed;

        /// <summary>Legge il valore grezzo scritto, se esiste.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="raw">Valore scritto.</param>
        /// <returns>true se l'impostazione è stata scritta.</returns>
        protected abstract bool TryReadRaw(string id, out float raw);
        /// <summary>Scrive il valore grezzo.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="raw">Valore da scrivere.</param>
        protected abstract void WriteRaw(string id, float raw);
        /// <summary>Cancella il valore scritto (si torna al predefinito).</summary>
        /// <param name="id">Id dell'impostazione.</param>
        protected abstract void DeleteRaw(string id);

        /// <summary>
        /// Valore corrente: quello scritto, altrimenti il predefinito, altrimenti 0.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <returns>Valore grezzo.</returns>
        private float Read(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0f;
            if (TryReadRaw(id, out float raw)) return raw;
            return defaults.TryGetValue(id, out float fallback) ? fallback : 0f;
        }

        /// <summary>
        /// Scrive il valore solo se è diverso da quello corrente e avvisa i collegati.
        /// </summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <param name="raw">Nuovo valore grezzo.</param>
        private void Write(string id, float raw)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (Mathf.Approximately(Read(id), raw) && (TryReadRaw(id, out _) || defaults.ContainsKey(id))) return;
            WriteRaw(id, raw);
            Changed?.Invoke(id);
        }

        /// <inheritdoc/>
        public bool GetBool(string id) { return Read(id) > 0.5f; }
        /// <inheritdoc/>
        public void SetBool(string id, bool value) { Write(id, value ? 1f : 0f); }
        /// <inheritdoc/>
        public float GetFloat(string id) { return Read(id); }
        /// <inheritdoc/>
        public void SetFloat(string id, float value) { Write(id, value); }
        /// <inheritdoc/>
        public int GetInt(string id) { return Mathf.RoundToInt(Read(id)); }
        /// <inheritdoc/>
        public void SetInt(string id, int value) { Write(id, value); }

        /// <inheritdoc/>
        public void RegisterDefaultBool(string id, bool value) { RegisterDefault(id, value ? 1f : 0f); }
        /// <inheritdoc/>
        public void RegisterDefaultFloat(string id, float value) { RegisterDefault(id, value); }
        /// <inheritdoc/>
        public void RegisterDefaultInt(string id, int value) { RegisterDefault(id, value); }

        /// <summary>
        /// Registra un predefinito (il primo registrato vince: il valore dell'ultima riga non cambia i predefiniti già noti).
        /// </summary>
        private void RegisterDefault(string id, float raw)
        {
            if (string.IsNullOrEmpty(id) || defaults.ContainsKey(id)) return;
            defaults.Add(id, raw);
        }

        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            foreach (string id in defaults.Keys) DeleteRaw(id);
            // Id vuoto = "tutto può essere cambiato": le righe si rileggono.
            Changed?.Invoke(string.Empty);
        }
    }

    /// <summary>
    /// Store persistente su PlayerPrefs. Le chiavi hanno il prefisso "si81." (es. "si81.audio.master_volume").
    /// PlayerPrefs scrive su disco da solo alla pausa/uscita dell'app; <see cref="Save"/> forza la scrittura.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : UISettingsStoreBase
    {
        /// <summary>Prefisso di tutte le chiavi PlayerPrefs delle impostazioni.</summary>
        public const string KEY_PREFIX = "si81.";

        /// <summary>Chiave PlayerPrefs di un'impostazione.</summary>
        /// <param name="id">Id dell'impostazione.</param>
        /// <returns>Chiave con prefisso.</returns>
        public static string ToKey(string id) { return KEY_PREFIX + id; }

        /// <inheritdoc/>
        protected override bool TryReadRaw(string id, out float raw)
        {
            string key = ToKey(id);
            if (PlayerPrefs.HasKey(key))
            {
                raw = PlayerPrefs.GetFloat(key);
                return true;
            }
            raw = 0f;
            return false;
        }

        /// <inheritdoc/>
        protected override void WriteRaw(string id, float raw) { PlayerPrefs.SetFloat(ToKey(id), raw); }

        /// <inheritdoc/>
        protected override void DeleteRaw(string id) { PlayerPrefs.DeleteKey(ToKey(id)); }

        /// <summary>Forza la scrittura su disco di PlayerPrefs.</summary>
        public void Save() { PlayerPrefs.Save(); }
    }

    /// <summary>
    /// Store in memoria per la sandbox e le prove: nessuna scrittura su disco, si azzera a ogni avvio.
    /// </summary>
    public sealed class MockSettingsStore : UISettingsStoreBase
    {
        // Valori scritti (id -> valore grezzo).
        private readonly Dictionary<string, float> values = new Dictionary<string, float>(StringComparer.Ordinal);

        /// <inheritdoc/>
        protected override bool TryReadRaw(string id, out float raw) { return values.TryGetValue(id, out raw); }

        /// <inheritdoc/>
        protected override void WriteRaw(string id, float raw) { values[id] = raw; }

        /// <inheritdoc/>
        protected override void DeleteRaw(string id) { values.Remove(id); }
    }
}
