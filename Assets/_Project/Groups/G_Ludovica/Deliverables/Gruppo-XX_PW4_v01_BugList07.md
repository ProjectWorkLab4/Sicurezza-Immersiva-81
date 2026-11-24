# Gruppo-XX_PW4_v01_BugList07 — Forklift Chaos

> Sostituire `XX` con il numero ufficiale del gruppo prima della consegna.
> Elenco dei punti aperti **noti allo stato attuale**. Le voci contrassegnate "da verificare" non sono state ancora provate in visore.

| ID | Gravità | Stato | Descrizione | Azione prevista | Responsabile |
|---|---|---|---|---|---|
| BUG-01 | Media | Da verificare | I sei prefab importati hanno scale approvate "a occhio" (Cono ×76, Luce ×60, Pallet ×10, Radio ×7, Scaffale ×41, Barriera ×4,3). In visore la pila di pallet e lo scaffale possono risultare troppo piccoli rispetto alla graybox. | Ricalibrare le scale in scena dopo il primo playtest T1 e aggiornare i prefab. | G_Ludovica |
| BUG-02 | Media | Da verificare | `MDL_Scaffale_ZN03` è una singola campata (~2 m); il graybox ZN-03 era una fila di 18 m. L'angolo cieco potrebbe non essere percepito come tale. | Valutare la duplicazione di 6–8 campate lungo l'asse Z oppure un prefab dedicated. | G_Ludovica |
| BUG-03 | Bassa | Da verificare | Non esistono file audio propri: clacson, scricchiolio, sirena e voci sono segnalati solo da testo + aptica + HUDService.ShowSoundEvent. | Collegare gli asset audio del Common Core quando disponibili; mantenere i sottotitoli. | G_Ludovica / Audio |
| BUG-04 | Media | Da verificare | Il registro timeline del debrief è ancora dentro `ForkliftChaosScenario.BuildDebrief()` (fallback temporaneo previsto dal brief §9). | Migrare a tabella frasi + registro comune del Game System/UX entro la consegna. | G_Ludovica / Game System |
| BUG-05 | Bassa | Da verificare | I distrattori dei livelli Operational e Crisis (capo turno che interrompe, più voci in radio) non sono ancora differenziati nel contenuto dei sottotitoli. | Aggiungere righe di dialogo specifiche per livello nel controller di scenario. | G_Ludovica |
| BUG-06 | Alta | Da verificare | La scena non è ancora registrata in `EditorBuildSettings` (file condiviso): il lanciatore dalla Home potrebbe non trovare `SCN_ForkliftChaos` nella build finale. | Chiedere accordo al gruppo proprietario e aggiungere la scena alla build. | Common Core |
| BUG-07 | Media | Da verificare | I due NPC colleghi sono capsule graybox e non modelli animati; il numero varia 1–3 ma il dettaglio grafico è minimo. | Sostituire con prefab NPC se disponibile; verificare che il variare del numero sia percepibile. | G_Ludovica |
| BUG-08 | Bassa | Da verificare | `SO_Mission_ForkliftChaos` contiene ancora il `DesignerNote` del template. | Pulire il campo prima della build finale (asset condiviso: serve coordinamento). | Common Core |

## Note

- Nessun playtest è stato ancora eseguito: i difetti sopra sono problemi strutturali o di integrazione, non osservazioni da sessione.
- I nuovi difetti emersi dai playtest T1–T5 vanno aggiunti qui con ID progressivo, gravità, passi per riprodurre e stato.
- L'hook di test `MNG_TestEnd` e il suo script sono stati rimossi: la chiusura missione passa ora solo dalla logica reale.
