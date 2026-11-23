using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Regole comuni del punteggio: corrispondenza punteggio sicurezza (0-100) -> stelle (0-5, rank).
    /// Le soglie vivono nei token numerici di SO_UITheme (<c>score.stars.N.min</c>, PROVVISORI, da confermare con il team):
    /// <see cref="UIServicesBootstrap"/> passa il tema con <see cref="UseTheme"/>. Senza tema (prove in Editor) si usano
    /// i valori di riserva <see cref="FALLBACK_STARS_MIN"/>, uguali a quelli di tokens_figma.json. Un solo punto del codice:
    /// tutti gli scenari e le schermate usano questo.
    /// </summary>
    public static class ScoreRules
    {
        /// <summary>Token della soglia minima per 1 stella.</summary>
        public const string TOKEN_STARS_1_MIN = "score.stars.1.min";
        /// <summary>Token della soglia minima per 2 stelle.</summary>
        public const string TOKEN_STARS_2_MIN = "score.stars.2.min";
        /// <summary>Token della soglia minima per 3 stelle.</summary>
        public const string TOKEN_STARS_3_MIN = "score.stars.3.min";
        /// <summary>Token della soglia minima per 4 stelle.</summary>
        public const string TOKEN_STARS_4_MIN = "score.stars.4.min";
        /// <summary>Token della soglia minima per 5 stelle.</summary>
        public const string TOKEN_STARS_5_MIN = "score.stars.5.min";
        /// <summary>Token della durata massima (secondi) del badge "FastRun".</summary>
        public const string TOKEN_FAST_RUN_SECONDS = "badge.fast_run.seconds";
        /// <summary>Numero di dimensioni del punteggio.</summary>
        public const int DIMENSION_COUNT = 5;

        /// <summary>Valori di riserva (1..5 stelle) se nessun tema è stato passato: tengono lo stesso valore dei token.</summary>
        private static readonly int[] FALLBACK_STARS_MIN = { 20, 40, 60, 80, 90 };
        /// <summary>Valore di riserva del badge FastRun (secondi), uguale al token.</summary>
        private const float FALLBACK_FAST_RUN_SECONDS = 180f;
        /// <summary>Nomi dei token delle soglie, in ordine da 1 a 5 stelle.</summary>
        private static readonly string[] STARS_TOKENS =
            { TOKEN_STARS_1_MIN, TOKEN_STARS_2_MIN, TOKEN_STARS_3_MIN, TOKEN_STARS_4_MIN, TOKEN_STARS_5_MIN };

        // Tema da cui si leggono le soglie (null = valori di riserva).
        private static UITheme theme;

        /// <summary>
        /// Imposta il tema da cui leggere le soglie (<c>score.stars.N.min</c>, <c>badge.fast_run.seconds</c>).
        /// </summary>
        /// <param name="newTheme">SO_UITheme (null = valori di riserva).</param>
        public static void UseTheme(UITheme newTheme)
        {
            theme = newTheme;
        }

        /// <summary>
        /// Punteggio minimo per ottenere un dato numero di stelle.
        /// </summary>
        /// <param name="stars">Stelle da 1 a 5.</param>
        /// <returns>Soglia minima del punteggio sicurezza (0-100).</returns>
        public static int StarsMin(int stars)
        {
            int index = Mathf.Clamp(stars, 1, 5) - 1;
            return theme != null ? Mathf.RoundToInt(theme.GetNumber(STARS_TOKENS[index])) : FALLBACK_STARS_MIN[index];
        }

        /// <summary>
        /// Durata massima (secondi) perché una missione positiva valga il badge "FastRun".
        /// </summary>
        /// <returns>Secondi (token <c>badge.fast_run.seconds</c>).</returns>
        public static float FastRunSeconds()
        {
            return theme != null ? theme.GetNumber(TOKEN_FAST_RUN_SECONDS) : FALLBACK_FAST_RUN_SECONDS;
        }

        /// <summary>
        /// Stelle (0-5) per un punteggio sicurezza.
        /// </summary>
        /// <param name="score">Punteggio 0-100 (valori fuori scala vengono limitati).</param>
        /// <returns>Stelle da 0 a 5.</returns>
        public static int StarsFromScore(int score)
        {
            score = Mathf.Clamp(score, 0, 100);
            for (int stars = 5; stars >= 1; stars--)
                if (score >= StarsMin(stars)) return stars;
            return 0;
        }

        /// <summary>
        /// Punteggio sicurezza come media delle cinque dimensioni (arrotondata).
        /// </summary>
        /// <param name="dimensions">Cinque valori 0-100 (null o vuoto = 0).</param>
        /// <returns>Punteggio 0-100.</returns>
        public static int ScoreFromDimensions(int[] dimensions)
        {
            if (dimensions == null || dimensions.Length == 0) return 0;
            float sum = 0f;
            for (int i = 0; i < dimensions.Length; i++) sum += Mathf.Clamp(dimensions[i], 0, 100);
            return Mathf.Clamp(Mathf.RoundToInt(sum / dimensions.Length), 0, 100);
        }

        /// <summary>
        /// Rende coerente un risultato: limita punteggio e dimensioni, completa le cinque dimensioni, ricalcola le stelle
        /// dal punteggio e, se manca, imposta l'istante di fine.
        /// </summary>
        /// <param name="result">Risultato da sistemare (modificato sul posto).</param>
        public static void Normalize(MissionResult result)
        {
            if (result == null) return;
            result.SafetyScore = Mathf.Clamp(result.SafetyScore, 0, 100);
            var dims = new int[DIMENSION_COUNT];
            if (result.Dimensions != null)
                for (int i = 0; i < DIMENSION_COUNT && i < result.Dimensions.Length; i++) dims[i] = Mathf.Clamp(result.Dimensions[i], 0, 100);
            result.Dimensions = dims;
            result.Stars = StarsFromScore(result.SafetyScore);
            if (result.TimestampUtc <= 0) result.TimestampUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (result.DebriefLines == null) result.DebriefLines = new List<string>();
        }
    }
}
