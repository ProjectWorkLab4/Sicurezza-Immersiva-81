using SI81.UI;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Unico punto di uscita verso la HUD comune per i messaggi che non passano da ScenarioController
    /// (suoni, righe di dialogo degli oggetti, vibrazione).
    /// </summary>
    public static class ElectricShockHud
    {
        /// <summary>Sottotitolo per un suono (es. "[Odore di benzina]").</summary>
        /// <param name="text">Testo tra parentesi quadre.</param>
        /// <param name="critical">true = suono critico (mostrato anche con sottotitoli spenti).</param>
        public static void Sound(string text, bool critical = false) => HUDService.ShowSoundEvent(text, critical);

        /// <summary>Riga di dialogo o descrizione detta da un oggetto o personaggio.</summary>
        /// <param name="speaker">Chi parla (vuoto per le descrizioni).</param>
        /// <param name="text">Testo.</param>
        public static void Line(string speaker, string text) => HUDService.ShowSubtitle(speaker, text);

        /// <summary>Vibrazione dei controller.</summary>
        /// <param name="amplitude">Intensità 0-1.</param>
        /// <param name="duration">Durata (s).</param>
        public static void Pulse(float amplitude, float duration) => HUDService.Pulse(amplitude, duration, false);
    }
}
