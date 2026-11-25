using System;

namespace SI81.UI.EditorTools
{
    // Classi "contenitore" per leggere con JsonUtility i file di Editor/Source/.
    // I nomi dei campi devono coincidere con le chiavi JSON (minuscole, come esportate da Figma):
    // per questo qui non si applica il PascalCase. I campi JSON non elencati vengono ignorati.

    /// <summary>Radice di tokens_figma.json (variabili ed effetti esportati da Figma + valori di progetto).</summary>
    [Serializable]
    internal class TokensFileJson
    {
        /// <summary>Variabili colore Figma (collezione Color).</summary>
        public ColorJson[] colors;
        /// <summary>Stili di testo Figma (Jaldi).</summary>
        public TextStyleJson[] textStyles;
        /// <summary>Effetti Figma: ombre, sfocature, gradienti.</summary>
        public EffectsJson effects;
        /// <summary>Variabili numeriche Figma (collezione Dimension).</summary>
        public NumberJson[] numbers;
        /// <summary>Stile dei tre livelli di difficoltà.</summary>
        public DifficultyJson difficulty;
        /// <summary>Valori di progetto derivati, non presenti come variabili Figma (PROVVISORI).</summary>
        public ProjectJson project;
    }

    /// <summary>Una variabile colore Figma (o un colore derivato della sezione "project").</summary>
    [Serializable]
    internal class ColorJson
    {
        /// <summary>Nome della variabile in Figma (es. "color/brand/dark").</summary>
        public string figma;
        /// <summary>Nome puntato del token in Unity (es. "color.brand.dark").</summary>
        public string token;
        /// <summary>Colore "#RRGGBB".</summary>
        public string hex;
        /// <summary>Opacità 0–1.</summary>
        public float alpha = 1f;
        /// <summary>Variabile Figma di cui questa è un alias (solo informativo).</summary>
        public string alias;
        /// <summary>Descrizione: uso e contrasto.</summary>
        public string desc;
    }

    /// <summary>Uno stile di testo Figma.</summary>
    [Serializable]
    internal class TextStyleJson
    {
        /// <summary>Nome dello stile (es. "Body Bold").</summary>
        public string name;
        /// <summary>Famiglia del font (Jaldi).</summary>
        public string family;
        /// <summary>Peso: "Regular" o "Bold".</summary>
        public string weight;
        /// <summary>Dimensione in px Figma (= unità Canvas).</summary>
        public float size;
        /// <summary>Interlinea in percentuale (es. "125%").</summary>
        public string lineHeight;
        /// <summary>Spaziatura caratteri in percentuale (es. "0%").</summary>
        public string letterSpacing;
    }

    /// <summary>Contenitore degli effetti (stili e valori inline).</summary>
    [Serializable]
    internal class EffectsJson
    {
        /// <summary>Effetti salvati come stili Figma.</summary>
        public EffectJson[] styles;
        /// <summary>Effetti applicati direttamente ai nodi (senza stile).</summary>
        public EffectJson[] inline;
    }

    /// <summary>Un effetto Figma (ombra, sfocatura o gradiente).</summary>
    [Serializable]
    internal class EffectJson
    {
        /// <summary>Nome dello stile effetto (solo per gli stili).</summary>
        public string name;
        /// <summary>Ruolo dell'effetto inline (es. "selected.lift").</summary>
        public string role;
        /// <summary>Tipo Figma (DROP_SHADOW, BACKGROUND_BLUR, FILL_GRADIENT_LINEAR...).</summary>
        public string type;
        /// <summary>Spostamento orizzontale in px.</summary>
        public float x;
        /// <summary>Spostamento verticale in px (verso il basso in Figma).</summary>
        public float y;
        /// <summary>Raggio di sfocatura dell'ombra in px.</summary>
        public float blur;
        /// <summary>Espansione dell'ombra in px.</summary>
        public float spread;
        /// <summary>Colore "#RRGGBB" dell'ombra.</summary>
        public string color;
        /// <summary>Opacità 0–1 dell'ombra.</summary>
        public float alpha = 1f;
        /// <summary>Raggio delle sfocature (BACKGROUND_BLUR / LAYER_BLUR).</summary>
        public float radius;
        /// <summary>Punti del gradiente (solo FILL_GRADIENT_LINEAR).</summary>
        public GradientStopJson[] stops;
    }

    /// <summary>Un punto di un gradiente Figma.</summary>
    [Serializable]
    internal class GradientStopJson
    {
        /// <summary>Colore "#RRGGBB" del punto.</summary>
        public string color;
        /// <summary>Opacità 0–1 del punto.</summary>
        public float alpha = 1f;
        /// <summary>Posizione 0–1 lungo il gradiente.</summary>
        public float pos;
    }

    /// <summary>Una variabile numerica Figma (collezione Dimension) o una misura derivata della sezione "project".</summary>
    [Serializable]
    internal class NumberJson
    {
        /// <summary>Nome della variabile in Figma (es. "radius/card").</summary>
        public string figma;
        /// <summary>Nome puntato del token in Unity (es. "radius.card").</summary>
        public string token;
        /// <summary>Valore in unità Canvas.</summary>
        public float value;
    }

    /// <summary>Sezione "difficulty" di tokens_figma.json.</summary>
    [Serializable]
    internal class DifficultyJson
    {
        /// <summary>I tre livelli, in ordine 1–3.</summary>
        public LevelJson[] levels;
    }

    /// <summary>Un livello di difficoltà.</summary>
    [Serializable]
    internal class LevelJson
    {
        /// <summary>Numero del livello (1, 2, 3).</summary>
        public int level;
        /// <summary>Identificativo inglese ("Guided", "Operational", "Crisis").</summary>
        public string id;
        /// <summary>Testo del badge in Figma (solo informativo: il testo vero è in strings_it.json).</summary>
        public string label;
        /// <summary>Colore del livello "#RRGGBB".</summary>
        public string color;
        /// <summary>Token colore corrispondente (solo informativo).</summary>
        public string token;
        /// <summary>Colore del testo appoggiato sul colore del livello.</summary>
        public string textOnColor;
        /// <summary>Barre piene nell'icona livello.</summary>
        public int barsFilled;
        /// <summary>Descrizione in Figma (solo informativo: il testo vero è in strings_it.json).</summary>
        public string description;
    }

    /// <summary>Sezione "project": valori di progetto derivati (non variabili Figma), unica fonte di verità.</summary>
    [Serializable]
    internal class ProjectJson
    {
        /// <summary>Colori derivati (es. pulsante disabilitato).</summary>
        public ColorJson[] colors;
        /// <summary>Misure derivate (es. bordo del pulsante primario).</summary>
        public NumberJson[] numbers;
        /// <summary>Valori di animazione (scale e durate).</summary>
        public MotionJson motion;
        /// <summary>Regole della HUD in scenario.</summary>
        public HudJson hud;
        /// <summary>Misure del raggio di puntamento UI (UIPointer).</summary>
        public PointerJson pointer;
        /// <summary>Maniglie del riflesso diagonale dei pannelli vetro (glassPanel.sheen).</summary>
        public GlassSheenJson glassSheen;
    }

    /// <summary>Maniglie del gradiente glassPanel.sheen della sezione "project" (vedi UITheme.GlassSheen).</summary>
    [Serializable]
    internal class GlassSheenJson
    {
        /// <summary>Posizione d = (u+v)/2 della maniglia iniziale (stop 0) lungo la diagonale del pannello.</summary>
        public float handleStart;
        /// <summary>Posizione d = (u+v)/2 della maniglia finale (stop 1) lungo la diagonale del pannello.</summary>
        public float handleEnd;
    }

    /// <summary>Misure del raggio di puntamento della sezione "project" (vedi UITheme.PointerSettings).</summary>
    [Serializable]
    internal class PointerJson
    {
        /// <summary>Spessore del raggio in metri.</summary>
        public float rayWidth;
        /// <summary>Lunghezza massima del raggio in metri.</summary>
        public float rayMaxLength;
        /// <summary>Lunghezza del raggio quando non punta la UI, in metri.</summary>
        public float idleRayLength;
        /// <summary>Diametro del reticolo nel punto colpito, in metri.</summary>
        public float reticleDiameter;
        /// <summary>Distacco del reticolo dalla superficie colpita (verso il controller), in metri.</summary>
        public float reticleSurfaceOffset;
    }

    /// <summary>Valori di animazione della sezione "project" (vedi UITheme.MotionSettings).</summary>
    [Serializable]
    internal class MotionJson
    {
        /// <summary>Scala allo stato hover.</summary>
        public float hoverScale;
        /// <summary>Scala allo stato pressed.</summary>
        public float pressedScale;
        /// <summary>Durata transizione hover in secondi.</summary>
        public float hoverDuration;
        /// <summary>Durata transizione pressed in secondi.</summary>
        public float pressDuration;
        /// <summary>Durata transizione selezione in secondi.</summary>
        public float selectDuration;
        /// <summary>Durata dissolvenza pannelli in secondi.</summary>
        public float fadeDuration;
        /// <summary>Durata cambio schermata in secondi.</summary>
        public float screenTransitionDuration;
    }

    /// <summary>Regole HUD della sezione "project" (vedi UITheme.HudSettings).</summary>
    [Serializable]
    internal class HudJson
    {
        /// <summary>Durata di un avviso breve in secondi.</summary>
        public float alertDuration;
        /// <summary>Angolo verticale degli avvisi brevi in gradi.</summary>
        public float alertPitch;
        /// <summary>Angolo verticale minimo dei sottotitoli in gradi.</summary>
        public float subtitlePitchMin;
        /// <summary>Angolo verticale massimo dei sottotitoli in gradi.</summary>
        public float subtitlePitchMax;
        /// <summary>Angolo massimo consentito per qualsiasi elemento, in gradi.</summary>
        public float maxPitch;
        /// <summary>Caratteri massimi per riga dei sottotitoli.</summary>
        public int subtitleMaxChars;
        /// <summary>Righe massime dei sottotitoli.</summary>
        public int subtitleMaxLines;
        /// <summary>Caratteri massimi per riga dell'obiettivo sul polso.</summary>
        public int wristObjectiveMaxChars;
        /// <summary>Opacità del pannello polso a riposo (0–1).</summary>
        public float wristIdleAlpha;
        /// <summary>Distanza in metri entro cui il pannello polso va al 100%.</summary>
        public float wristFocusDistance;
        /// <summary>Larghezza del pannello polso in metri.</summary>
        public float wristPanelWidthMeters;
        /// <summary>Altezza del pannello polso in metri.</summary>
        public float wristPanelHeightMeters;
        /// <summary>Primo avviso timer (secondi rimanenti).</summary>
        public float timerWarningFirst;
        /// <summary>Secondo avviso timer (secondi rimanenti).</summary>
        public float timerWarningSecond;
        /// <summary>Distanza minima delle etichette ancorate agli oggetti, in metri.</summary>
        public float worldLabelMinDistance;
        /// <summary>Distanza massima delle etichette ancorate agli oggetti, in metri.</summary>
        public float worldLabelMaxDistance;
        /// <summary>Numero massimo di HUD_RiskLabel visibili insieme.</summary>
        public int maxRiskLabels;
    }

    /// <summary>Radice di strings_it.json (testi curati dalla schermata Figma Screens).</summary>
    [Serializable]
    internal class StringsFileJson
    {
        /// <summary>Provenienza dei testi e note di curatela.</summary>
        public string source;
        /// <summary>Tutte le righe di testo.</summary>
        public StringJson[] strings;
    }

    /// <summary>Una riga di testo: chiave, testo italiano, schermate dove compare, nota.</summary>
    [Serializable]
    internal class StringJson
    {
        /// <summary>Chiave puntata minuscola (es. "ui.nav.home").</summary>
        public string key;
        /// <summary>Testo italiano, con eventuali segnaposto {0}, {1}.</summary>
        public string text;
        /// <summary>Schermate Figma dove compare il testo.</summary>
        public string[] screens;
        /// <summary>Nota per chi usa la chiave (composizione, origine).</summary>
        public string note;
    }
}
