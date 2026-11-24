# 10 · Electric Shock (Folgorazione) — LITE

Gruppo **G_Ludovica**. Implementazione della specifica `10_Electric_Shock_LITE_specifica_IT.md` (consegna 27/11/2026).
Stato: graybox giocabile, logica completa, **senza UI comune** e **con modelli segnaposto**.

## Cartelle

| Percorso | Contenuto |
| --- | --- |
| `SCN_ElectricShock.unity` | Scena dello scenario (niente camera, rig, Canvas: guida UI §6) |
| `Scripts/` | Logica dello scenario (`ElectricShockScenario` su `MNG_Scenario`), pericoli, attrezzature, personaggi |
| `Scripts/Editor/ElectricShockSceneBuilder.cs` | Menu `SI81/G_Ludovica/Electric Shock/Costruisci scene (graybox)`: rigenera le due scene da codice |
| `Data/SO_ElectricShockSettings.asset` | Tempi e parametri per livello (specifica §5, §7, §9) |
| `Data/ElectricShock_DebriefPhrases.json` | Frasi del debrief LG-01…26 e percorso corretto (si modificano senza toccare il codice) |
| `Art/Materials/` | Materiali segnaposto `MAT_Placeholder*`, `MAT_VFX_Placeholder*` |
| `Temp/` | **[TEMP]** sostituti di UI comune, rig e flusso di SCN_Core (vedi sotto) |
| `Build/BP_ElectricShock_Quest.asset` | Build Profile Android con le sole scene di questo scenario |
| `Data/NAV_ElectricShock.asset` | NavMesh della scena (cotta dal builder con l'agente di default, nessuna modifica a ProjectSettings) |
| `Tests/EditMode`, `Tests/PlayMode` | Test automatici (Window → General → Test Runner) |
| `*.asmdef` | Assembly propri: `SI81.Ludovica.TempCore`, `SI81.Ludovica.ElectricShock`, `.Editor`, `.Tests.*` |

> Attenzione: rieseguire il menu del builder **riscrive** le due scene. Le modifiche fatte a mano nelle scene vanno perse.

## Come provarlo

**Editor:** apri `SCN_ElectricShock` e premi Play. La scena carica da sola `Temp/SCN_ElectricShock_TempCore` (rig) e parte in Guided
(campo `editorLevel` su `MNG_Scenario`). Tutti i messaggi (sottotitoli, avvisi, obiettivi, debrief finale) sono in Console con il tag `[SI81 TempHUD]` / `[SI81 ElectricShock]`.

Comandi desktop (senza visore): WASD muovi · tasto destro + mouse guarda · E afferra/lascia · clic sinistro usa · R ispeziona · Q parla · Tab riavvia al livello successivo.

**Quest (APK):** Build Profiles → `BP_ElectricShock_Quest` → Build. Il profilo usa la propria lista di scene (TempCore all'indice 0, poi lo scenario), quindi **non tocca** `EditorBuildSettings`.
Messaggi: `adb logcat -s Unity | findstr "SI81"`.

Comandi VR: levetta sinistra cammina · levetta destra ruota a scatti · grip afferra · grilletto usa · A/X ispeziona · B/Y parla ("Fermi! Allontanatevi!") · Menu (sinistro) riavvia al livello successivo (1/2/3 vibrazioni = Guided/Operational/Crisis).

## Interazioni (rig temporaneo)

| Oggetto | Azione |
| --- | --- |
| PB-01 arresto di emergenza | grilletto toccandolo |
| Estintori (rack RK-02) | grip = prendi · A/X = leggi etichetta · grilletto = spruzza |
| Barriere (rack RK-01) | grip = prendi · lasciale vicino ai segni gialli sul varco DR-03 (si agganciano) |
| Radio RD-01 | grilletto = chiama il 112 |
| Collega VT-01 | A/X = coscienza e respiro · grilletto breve = posizione · grilletto tenuto = trascina · restare entro 2 m per 10 s = "resta con lui" |
| PM-1 | grilletto tenuto 3 s = ti segue; portalo al cerchio verde SAFE-01 |
| Colleghi, PM-2 | B/Y (parla) entro 15 m |

Toccare cavo o collega con la corrente presente = folgorazione (errore critico).

## Test automatici

Test Runner → **EditMode** (7 test, meno di 1 s) e **PlayMode** (13 test, circa 70 s; il tempo di gioco scorre 3 volte più veloce).
Da riga di comando: `unity command test run --mode editor --filter SI81.Ludovica.ElectricShock.Tests.EditMode --filter-type Assembly`
e `unity command test run --mode playmode --async-tests true --filter SI81.Ludovica.ElectricShock.Tests.PlayMode --filter-type Assembly`.

- **EditMode:** pesi provvisori delle 5 dimensioni, registro senza doppioni, frasi per tutti gli ID LG-01…26 (senza "SPI"),
  formato delle righe di debrief e taglio a 10 righe, valori dei livelli (§9), regole della guida UI sulla scena
  (niente Camera, AudioListener, Canvas, EventSystem; un solo `MNG_Scenario`; obiettivi 1-4 da max 30 caratteri; nessuno script mancante).
- **PlayMode:** percorso corretto → Positivo; collega toccato a corrente inserita → Negativo (LG-08); cavo pericoloso solo con corrente;
  acqua → Negativo (LG-11); spruzzo da vicino; colleghi ignorati → Negativo (LG-15); grido → i colleghi escono (LG-14);
  PM-2 alla barriera chiusa → entra quando è sicuro, barriere che si aprono e richiudono, PM-1 portato via da PM-2;
  varco aperto e zona non sicura → Negativo (LG-23); giocatore nella vampata → Negativo (LG-17); collega trascinato fuori → Parziale (LG-16);
  PM-1 gira attorno al generatore (NavMesh); riavvio tenendo un estintore → nessun doppione.

## Cosa è verificato

Oltre ai test automatici, prove in Play Mode (Editor, rig desktop, azioni chiamate da script):
- percorso corretto in Guided → **Positivo**, punteggio 96/100, 10 righe di debrief corrette;
- PM-2 si ferma alla barriera chiusa con zona non sicura, entra quando la zona è sicura (le barriere si aprono di 80° e si richiudono),
  porta PM-1 al punto sicuro, poi va dal collega; il livello aspetta che il giocatore finisca di assistere → Positivo;
- spruzzo da vicino (ugello dentro la zona del fuoco) → fuoco spento;
- esplosione con il collega trascinato fuori dal raggio durante l'avviso → LG-16 corretto, nessun ferito → **Parziale**;
- collega toccato a corrente inserita (Guided) → Negativo (LG-08);
- estintore ad acqua (Operational, tempo 240 s) → Negativo (LG-11);
- nessuna azione in Crisis (tempo 180 s) → i colleghi toccano la vittima → Negativo (LG-15);
- due riavvii di fila tenendo un estintore in mano → nessun oggetto duplicato, mano vuota.

**Non verificato:** tutto ciò che riguarda il visore (input reali, altezza, mani, leggibilità visiva dei segnaposto, volume dei suoni)
e la build APK. Checklist per il visore: [CHECKLIST_Quest.md](CHECKLIST_Quest.md).

## Limiti noti

- Gli NPC camminano sulla NavMesh (girano attorno a pareti, rack, generatore, collega a terra e fuoco acceso), ma non hanno fisica né animazioni (capsule che scivolano); il giocatore non li urta (collisioni ignorate di proposito).
- Le barriere si aprono da sole per far passare i colleghi che escono e PM-2 autorizzato; per la logica il varco resta chiuso.

## Segnaposto da sostituire (FBX mancanti)

Tutti gli oggetti visivi si chiamano `PLACEHOLDER_*` (cerca "PLACEHOLDER" nella Hierarchy). I collider e gli script stanno sulla radice,
quindi basta sostituire i figli `PLACEHOLDER_*` con il modello.

| Radice | Segnaposto | Modello da fornire |
| --- | --- | --- |
| `ENV_Graybox` | pavimento, pareti | stanza / capannone |
| `GEN01_Generator` | `PLACEHOLDER_Generator` | generatore portatile (con punto per PB-01) |
| `PB01_EmergencyStop` | base + fungo | pulsante di emergenza |
| `CB01_LiveCable` | LineRenderer | cavo |
| `PZ01_FuelPool` | `PLACEHOLDER_FuelPool` | decal pozza di benzina |
| `FC01_FuelTank` | `PLACEHOLDER_FuelTank` | tanica di carburante |
| `EXT1/2/3_*` | corpo, banda colorata, maniglia | 3 estintori con etichetta (acqua, polvere, CO2) |
| `BAR1/2_Barrier` | pannello + gambe | barriera (prefab dello scenario 07) |
| `RD01_Radio` | `PLACEHOLDER_Radio` | radio (prefab dello scenario 07) |
| `PLACEHOLDER_Rack*` | rack | rack estintori, rack barriere/radio |
| `VT01_Victim` | torso + gambe (capsule) | operaio a terra |
| `BY01/02/03_Colleague` | capsule | colleghi |
| `PM1_ParamedicInShock`, `PM2_ArrivingParamedic` | capsule | paramedici |
| `SAFE01_SafePoint`, `AP02_AssemblyPoint` | dischi verdi | segnaletica a terra |
| `VFX_*` | particellari semplici | prefab fuoco / fumo / esplosione del Common Core |
| `SFX_*` (AudioSource 3D) | suoni generati da codice (`PlaceholderAudio`): ronzio, crepitio, fuoco, sibilo, esplosione | SFX del Common Core: basta assegnare il clip all'AudioSource |

## [TEMP] Cosa sostituire quando arrivano i sistemi comuni

| Sostituto temporaneo | Sistema definitivo | Come |
| --- | --- | --- |
| `Temp/Scripts/ScenarioController.cs` + `TempCoreTypes.cs` + `MissionSessionShim.cs` | `SI81.UI.ScenarioController`, `OutcomeType`, `FeedbackKind`, `DifficultyLevel`, `DebriefLinePrefix` | Stessa API: in `ElectricShockScenario.cs` e `DebriefPhraseTable.cs`/`ElectricShockSettings.cs` cambia `using SI81.Ludovica.TempCore;` in `using SI81.UI;`, assegna `SO_Mission_ElectricShock` |
| `Scripts/ElectricShockHud.cs` (corpi) | `HUDService.ShowSoundEvent / ShowSubtitle / Pulse` | Unico punto da cambiare per i messaggi degli oggetti |
| `Temp/SCN_ElectricShock_TempCore.unity`, `TempRig`, `TempHand`, `TempCoreBootstrap` | `SCN_Core` (rig del VR Interaction System, Home → Mission Brief) | Eliminare la scena temporanea; il rig reale arriva da SCN_Core |
| `TempInteractable` (base di estintori, barriere, radio, PB-01, vittima, PM-1, cavo) | componenti del VR Interaction System (grab / use / inspect / activate) | Ricollegare gli eventi: la logica dello scenario ascolta solo gli eventi C# degli oggetti |
| `BarrierPanel` / `BarrierSnapLine`, `EmergencyRadio`, `InjuredPerson` | prefab barriera + snap zone, radio + dialogo 112, sistema "ferito" dello scenario 07 | Stessi eventi (`ClosedChanged`, `Called`, `Checked`…) |
| `ChoiceLog` + `DebriefPhraseTable` | Registro delle scelte dello Score Manager comune + schermata timeline (fallback specifica §11.3) | Stesso formato dati; la tabella JSON resta dello scenario |
| Pesi delle dimensioni (base 60, +20/−20/−25/−60) | Game System comune | Valori provvisori in `ChoiceLog.ComputeDimensions` |
| `Build/BP_ElectricShock_Quest.asset` | build condivisa con `SCN_Core` all'indice 0 | Eliminare il profilo; la scena va aggiunta con `Nuovo scenario...` |

Con la UI comune: creare lo scenario con `SI81/Scenari/Nuovo scenario...` (Id **10**: prima il Team UI toglie la missione di esempio "Electric Shock" dal catalogo),
poi spostare/riusare questa scena come `SCN_ElectricShock` e compilare `SO_Mission_ElectricShock` con i dati di [SO_Mission_ElectricShock_dati.md](SO_Mission_ElectricShock_dati.md).

## Scelte e semplificazioni da confermare

- Il collega respira **sempre** (modifica 1): niente RCP né DAE.
- La pozza è nascosta dal generatore se guardata da P0: per osservarla (LG-01) bisogna spostarsi. Avviso a due canali: sottotitolo "[Odore di benzina]" entro 9 m.
- Le barriere bloccano solo PM-2 (logica); i colleghi che evacuano passano comunque dal varco.
- Con il varco chiuso e la zona non sicura PM-2 aspetta; appena la zona è sicura (corrente tolta, fuoco spento) entra da solo.
- Dopo l'esplosione lo scenario si chiude sempre (se nessuno è ferito l'esito è Parziale).
- Distrattori e allarme di fondo sono solo sottotitoli; i suggerimenti seguono §9 (Guided illimitati dopo 20 s, Operational max 3 dopo 45 s, Crisis nessuno).
- ID aggiunti oltre la tabella §11.1: LG-22 aria nociva, LG-23 PM-2 seconda vittima, LG-24 cavo toccato, LG-25 collega nella vampata, LG-26 altre persone nella vampata.
