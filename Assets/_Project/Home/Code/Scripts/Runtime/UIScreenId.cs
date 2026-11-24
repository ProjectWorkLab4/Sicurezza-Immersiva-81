namespace SI81.UI
{
    /// <summary>
    /// Identificativi delle schermate menu gestite da <see cref="UIScreenManager"/> (una per prefab PANEL_*).
    /// Estendibile: aggiungere i nuovi valori IN FONDO con un numero esplicito, senza rinumerare quelli esistenti,
    /// perché il valore numerico è salvato nei prefab e nelle scene (campo <see cref="UIScreen"/>.Id).
    /// </summary>
    public enum UIScreenId
    {
        /// <summary>PANEL_Home: elenco scenari e training.</summary>
        Home = 0,
        /// <summary>PANEL_MissionBrief: descrizione scenario e scelta del livello.</summary>
        MissionBrief = 1,
        /// <summary>PANEL_Stats: statistiche per scenario.</summary>
        Stats = 2,
        /// <summary>PANEL_Profile: profilo, risultati recenti e badge.</summary>
        Profile = 3,
        /// <summary>PANEL_Settings: impostazioni a 6 schede.</summary>
        Settings = 4,
        /// <summary>PANEL_TrainingBrief: tutorial comune.</summary>
        TrainingBrief = 5,
        /// <summary>Solo SCN_UISandbox: galleria di tutti i componenti con dati finti e ogni stato (non è una schermata di prodotto).</summary>
        ComponentGallery = 6,
        /// <summary>PANEL_Pause: pausa in missione (Blocco C).</summary>
        Pause = 7,
        /// <summary>PANEL_Debrief: riepilogo di fine missione (Blocco C).</summary>
        Debrief = 8,
        /// <summary>PANEL_ScoreReplay: punteggio sicurezza, stelle e rigioca (Blocco C).</summary>
        ScoreReplay = 9,
        /// <summary>PANEL_TrainingComplete: fine del training (Blocco C).</summary>
        TrainingComplete = 10
    }
}
